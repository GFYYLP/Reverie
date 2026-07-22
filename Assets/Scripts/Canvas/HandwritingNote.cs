using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Attach to a TextMeshProUGUI object. Hand-author the position in the scene.
// Call SetNote(text) to trigger the writing animation at the current emotional state.
[RequireComponent(typeof(TextMeshProUGUI))]
public class HandwritingNote : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float baseLetterDuration  = 0.18f; // seconds for an "easy" letter's stroke-in
    [SerializeField] private float eraseLetterDuration  = 0.10f; // seconds to scrub out a letter

    [Header("Reveal shape")]
    [SerializeField] private float revealSharpness = 3.5f;      // higher = crisper wipe edge, lower = softer/inkier
    [SerializeField] private bool revealDiagonal    = true;     // top-left -> bottom-right wipe direction

    [Header("Jitter (fast, per-character)")]
    [SerializeField] private float maxPositionJitter = 4f;      // pixels
    [SerializeField] private float maxRotationJitter  = 3f;     // degrees
    [SerializeField] private float jitterSpeed        = 8f;     // noise frequency

    [Header("Drift (slow, per-line — simulates one continuous hand motion)")]
    [SerializeField] private float driftBaselineAmplitude = 2.5f; // pixels of baseline rise/fall
    [SerializeField] private float driftSlantAmplitude    = 2f;   // degrees of slow slant wander
    [SerializeField] private float driftSpeed              = 0.6f; // how quickly the drift evolves

    [Header("Pressure (alpha variance)")]
    [SerializeField] private float minAlpha = 0.6f;

    private TextMeshProUGUI tmp;
    private string targetText    = "";
    private string displayedText = "";
    private Coroutine activeRoutine;

    // emotion snapshot taken at the moment SetNote is called
    private float snapIntensity;
    private float snapUnease;

    // per-character reveal progress, 0 = not started, 1 = fully written.
    // Indexed against displayedText; erasing counts back down from 1 to 0.
    private List<float> charProgress = new List<float>();

    // slow-evolving "pen state" shared across all characters this write pass,
    // so the whole line feels like one continuous hand motion rather than
    // independently-jittering letters.
    private float driftSeed;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        tmp.text = "";
        driftSeed = Random.Range(0f, 1000f);
    }

    public void SetNote(string text)
    {
        snapIntensity = Mathf.Clamp01(Emotion.Instance.intensity / 100f);
        snapUnease    = Mathf.Clamp01(Emotion.Instance.unease    / 100f);

        if (activeRoutine != null) StopCoroutine(activeRoutine);

        if (text.Length < displayedText.Length)
            activeRoutine = StartCoroutine(EraseAndWrite(text));
        else
            activeRoutine = StartCoroutine(Write(text));
    }

    // ── Per-letter duration: complexity-based, not flat ───────────────────────
    // Rough stroke-count buckets. Not linguistically rigorous — just enough
    // variance that speed differences read as "handwriting" rather than typing.
    private static readonly HashSet<char> fastChars   = new HashSet<char>("il1.,'`|!:;");
    private static readonly HashSet<char> slowChars   = new HashSet<char>("mwkgxz&%@#MWKGXZ%@#");
    private static readonly HashSet<char> mediumChars = new HashSet<char>("ocesabdfhjnpqrtuvyOCESABDFHJNPQRTUVY0123456789");

    private float DurationForChar(char c)
    {
        float mult;
        if (c == ' ') mult = 0.4f;
        else if (fastChars.Contains(c))   mult = 0.55f;
        else if (slowChars.Contains(c))   mult = 1.6f;
        else if (mediumChars.Contains(c)) mult = 1.0f;
        else mult = 1.1f; // punctuation/unknown default

        float jitterMult = Random.Range(0.85f, 1.2f);

        // hesitation: occasionally a longer pause under high intensity (thinking, shaking hand)
        if (snapIntensity > 0.5f && Random.value < 0.15f)
            jitterMult *= Random.Range(2f, 4f);

        return baseLetterDuration * mult * jitterMult;
    }

    // ── Write new text, stroking each new character in over its own duration ──

    private IEnumerator Write(string text)
    {
        targetText = text;
        int start  = displayedText.Length; // resume from where we are if appending

        // extend progress list for any newly-appended characters
        while (charProgress.Count < text.Length) charProgress.Add(0f);

        for (int i = start; i < text.Length; i++)
        {
            displayedText = text.Substring(0, i + 1);
            tmp.text      = displayedText;

            float duration = DurationForChar(text[i]);
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                charProgress[i] = Mathf.Clamp01(t / duration);
                ApplyVertexEffects();
                yield return null;
            }
            charProgress[i] = 1f;
        }

        ApplyVertexEffects();
        activeRoutine = null;
    }

    // ── Erase back to common prefix via scrub-out, then write new suffix ──────

    private IEnumerator EraseAndWrite(string text)
    {
        targetText = text;

        int common = 0;
        while (common < text.Length && common < displayedText.Length
               && text[common] == displayedText[common])
            common++;

        // scrub-erase from the end back to the common prefix, one char at a time,
        // reversing its reveal progress 1 -> 0 instead of instantly deleting it
        while (displayedText.Length > common)
        {
            int lastIndex = displayedText.Length - 1;
            float startProgress = charProgress.Count > lastIndex ? charProgress[lastIndex] : 1f;

            float t = 0f;
            while (t < eraseLetterDuration)
            {
                t += Time.deltaTime;
                float frac = t / eraseLetterDuration;
                // small back-and-forth wobble on the way out, like a real scrub, not a clean wipe
                float wobble = Mathf.Sin(frac * Mathf.PI * 3f) * 0.06f * (1f - frac);
                if (charProgress.Count > lastIndex)
                    charProgress[lastIndex] = Mathf.Clamp01(Mathf.Lerp(startProgress, 0f, frac) + wobble);
                ApplyVertexEffects();
                yield return null;
            }

            if (charProgress.Count > lastIndex) charProgress.RemoveAt(lastIndex);
            displayedText = displayedText.Substring(0, displayedText.Length - 1);
            tmp.text      = displayedText;
        }

        activeRoutine = StartCoroutine(Write(text));
    }

    // ── Per-vertex jitter + drift + progressive reveal, applied after layout ──

    private void ApplyVertexEffects()
    {
        tmp.ForceMeshUpdate();
        TMP_TextInfo info = tmp.textInfo;

        // slow, shared drift for this frame — one evolving "hand," not per-char noise
        float driftT      = Time.time * driftSpeed + driftSeed;
        float baselineOff = (Mathf.PerlinNoise(driftT, 0.13f) - 0.5f) * 2f * driftBaselineAmplitude * snapUnease;
        float slantAngle  = (Mathf.PerlinNoise(0.71f, driftT) - 0.5f) * 2f * driftSlantAmplitude * snapUnease;

        for (int c = 0; c < info.characterCount; c++)
        {
            TMP_CharacterInfo ch = info.characterInfo[c];
            if (!ch.isVisible) continue;

            int matIndex  = ch.materialReferenceIndex;
            int vertIndex = ch.vertexIndex;
            Vector3[] verts   = info.meshInfo[matIndex].vertices;
            Color32[] colors  = info.meshInfo[matIndex].colors32;

            float progress = c < charProgress.Count ? charProgress[c] : 1f;

            Vector3 min = verts[vertIndex], max = verts[vertIndex];
            for (int v = 1; v < 4; v++)
            {
                min = Vector3.Min(min, verts[vertIndex + v]);
                max = Vector3.Max(max, verts[vertIndex + v]);
            }

            // fast per-character jitter (only meaningful once the stroke is underway)
            float ft     = Time.time * jitterSpeed + c * 1.37f;
            float jitter = snapIntensity * Mathf.SmoothStep(0f, 1f, progress);
            float nx     = (Mathf.PerlinNoise(ft, c * 0.5f) - 0.5f) * 2f * maxPositionJitter * jitter;
            float ny     = (Mathf.PerlinNoise(c * 0.5f, ft) - 0.5f) * 2f * maxPositionJitter * jitter
                         + baselineOff; // shared slow baseline wander

            float angle  = (Mathf.PerlinNoise(ft * 0.5f, c) - 0.5f) * 2f * maxRotationJitter * snapUnease
                         + slantAngle; // shared slow slant wander
            Quaternion rot = Quaternion.Euler(0, 0, angle);

            Vector3 center = (verts[vertIndex] + verts[vertIndex + 1]
                            + verts[vertIndex + 2] + verts[vertIndex + 3]) * 0.25f;

            for (int v = 0; v < 4; v++)
            {
                Vector3 local = verts[vertIndex + v] - center;
                verts[vertIndex + v] = center + rot * local + new Vector3(nx, ny, 0);

                // progressive reveal: wipe alpha in across the glyph's own bounding box
                float localT = revealDiagonal
                    ? Mathf.InverseLerp((min.x + min.y), (max.x + max.y), (verts[vertIndex + v].x + verts[vertIndex + v].y))
                    : Mathf.InverseLerp(min.x, max.x, verts[vertIndex + v].x);

                float reveal = Mathf.Clamp01((progress - localT) * revealSharpness + 0.5f);

                // pressure: vary alpha per character, gated by reveal
                float pressureAlpha = Mathf.Lerp(1f, minAlpha, snapIntensity * Random.value);
                colors[vertIndex + v].a = (byte)(reveal * pressureAlpha * 255);
            }
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
            tmp.UpdateGeometry(info.meshInfo[m].mesh, m);

        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}