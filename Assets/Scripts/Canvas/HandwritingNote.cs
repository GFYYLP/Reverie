using System.Collections;
using UnityEngine;
using TMPro;

// Attach to a TextMeshProUGUI object. Hand-author the position in the scene.
// Call SetNote(text) to trigger the writing animation at the current emotional state.
[RequireComponent(typeof(TextMeshProUGUI))]
public class HandwritingNote : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float baseLetterDelay  = 0.06f;  // seconds per letter at calm
    [SerializeField] private float eraseLetterDelay = 0.03f;  // seconds per letter when erasing

    [Header("Jitter (scaled by emotion intensity)")]
    [SerializeField] private float maxPositionJitter = 4f;    // pixels
    [SerializeField] private float maxRotationJitter = 3f;    // degrees
    [SerializeField] private float jitterSpeed       = 8f;    // noise frequency

    [Header("Pressure (alpha variance)")]
    [SerializeField] private float minAlpha = 0.6f;

    private TextMeshProUGUI tmp;
    private string targetText   = "";
    private string displayedText = "";
    private Coroutine activeRoutine;

    // emotion snapshot taken at the moment SetNote is called
    private float snapIntensity;
    private float snapUnease;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        tmp.text = "";
    }

    public void SetNote(string text)
    {
        // snapshot emotion at this moment
        snapIntensity = Mathf.Clamp01(Emotion.Instance.intensity / 100f);
        snapUnease    = Mathf.Clamp01(Emotion.Instance.unease    / 100f);

        if (activeRoutine != null) StopCoroutine(activeRoutine);

        if (text.Length < displayedText.Length)
            activeRoutine = StartCoroutine(EraseAndWrite(text));
        else
            activeRoutine = StartCoroutine(Write(text));
    }

    // ── Write new text letter by letter ──────────────────────────────────────

    private IEnumerator Write(string text)
    {
        targetText = text;
        int start  = displayedText.Length; // resume from where we are if appending

        for (int i = start; i <= text.Length; i++)
        {
            displayedText = text.Substring(0, i);
            tmp.text      = displayedText;
            ApplyVertexJitter();

            float delay = baseLetterDelay * Random.Range(0.5f, 1.5f);
            // hesitation: occasionally pause longer under high intensity
            if (snapIntensity > 0.5f && Random.value < 0.15f)
                delay *= Random.Range(2f, 4f);

            yield return new WaitForSeconds(delay);
        }
        activeRoutine = null;
    }

    // ── Erase back to common prefix, then write new text ─────────────────────

    private IEnumerator EraseAndWrite(string text)
    {
        targetText = text;

        // find common prefix length
        int common = 0;
        while (common < text.Length && common < displayedText.Length
               && text[common] == displayedText[common])
            common++;

        // erase down to common prefix
        while (displayedText.Length > common)
        {
            displayedText = displayedText.Substring(0, displayedText.Length - 1);
            tmp.text      = displayedText;
            ApplyVertexJitter();
            yield return new WaitForSeconds(eraseLetterDelay);
        }

        // write new suffix
        activeRoutine = StartCoroutine(Write(text));
    }

    // ── Per-vertex jitter applied after TMP layout ────────────────────────────

    private void ApplyVertexJitter()
    {
        tmp.ForceMeshUpdate();
        TMP_TextInfo info = tmp.textInfo;

        for (int c = 0; c < info.characterCount; c++)
        {
            TMP_CharacterInfo ch = info.characterInfo[c];
            if (!ch.isVisible) continue;

            int matIndex = ch.materialReferenceIndex;
            int vertIndex = ch.vertexIndex;
            Vector3[] verts = info.meshInfo[matIndex].vertices;

            // per-character noise offset — each character gets its own seed via index
            float t      = Time.time * jitterSpeed + c * 1.37f;
            float jitter = snapIntensity;
            float nx     = (Mathf.PerlinNoise(t, c * 0.5f) - 0.5f) * 2f * maxPositionJitter * jitter;
            float ny     = (Mathf.PerlinNoise(c * 0.5f, t) - 0.5f) * 2f * maxPositionJitter * jitter;

            // rotation jitter — shaky baseline under unease
            float angle  = (Mathf.PerlinNoise(t * 0.5f, c) - 0.5f) * 2f * maxRotationJitter * snapUnease;
            Quaternion rot = Quaternion.Euler(0, 0, angle);

            Vector3 center = (verts[vertIndex] + verts[vertIndex + 1]
                            + verts[vertIndex + 2] + verts[vertIndex + 3]) * 0.25f;

            for (int v = 0; v < 4; v++)
            {
                Vector3 local = verts[vertIndex + v] - center;
                verts[vertIndex + v] = center + rot * local + new Vector3(nx, ny, 0);
            }

            // pressure: vary alpha per character
            Color32[] colors = info.meshInfo[matIndex].colors32;
            byte alpha = (byte)(Mathf.Lerp(1f, minAlpha, snapIntensity * Random.value) * 255);
            for (int v = 0; v < 4; v++)
                colors[vertIndex + v].a = alpha;
        }

        for (int m = 0; m < info.meshInfo.Length; m++)
            tmp.UpdateGeometry(info.meshInfo[m].mesh, m);

        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}
