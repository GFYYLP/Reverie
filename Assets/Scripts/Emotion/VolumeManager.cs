using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeManager : MonoBehaviour
{
    private Volume volume;
    [SerializeField] private Renderer roomRenderer;
    [SerializeField] private float contrastVal=100f;
    
    private float roomHue = 0f;

    public void SetRoom(RoomConfig config) {
        roomHue = config.roomHue;
    }

    void Start()
    {
        volume = GetComponent<Volume>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateVolumeParameters(
            Emotion.Instance.content*0.01f,
            Emotion.Instance.unease*0.01f,
            Emotion.Instance.awe*0.01f
        );
    }
    
    void UpdateVolumeParameters(float content, float unease,  float awe) 
    {
        // Emission tinted by room hue; content warms it (slight hue shift toward warm end),
        // awe brightens it, unease suppresses it.
        float emitHue = Mathf.Repeat(roomHue - content * 0.05f, 1f);
        float emitSat = Mathf.Clamp01(0.5f - unease * 0.3f);
        Color emissionColor = Color.HSVToRGB(emitHue, emitSat, 1f)
            * Mathf.Max(content, awe) * 0.3f;
        roomRenderer.material.EnableKeyword("_EMISSION");
        roomRenderer.material.SetColor("_EmissionColor", emissionColor);
        
        // ── Bloom ──────────────────────────────────────────────────────
        // content = warm soft glow, awe = overwhelming brightness
        if (volume.profile.TryGet<Bloom>(out var bloom)) {
            bloom.threshold.value = Mathf.Lerp(1.2f, 0.2f, Mathf.Max(content, awe));
            bloom.intensity.value = content * 2f + awe * 5f;
        }

        // ── Color grading ──────────────────────────────────────────────
        // unease drains the world of color; content/awe restore it
        if (volume.profile.TryGet<ColorAdjustments>(out var color)) {
            color.saturation.value = Mathf.Lerp(-60f, 30f, Mathf.Clamp01(Mathf.Max(content, awe) - unease * 0.8f));
            // Color filter anchored to room hue; emotions shift hue, saturation, and brightness
            // relative to the room's identity rather than toward fixed warm/cold poles.
            float hue = Mathf.Repeat(roomHue - content * 0.04f, 1f); // content nudges slightly warmer
            float sat = Mathf.Clamp01(0.2f + content * 0.2f - unease * 0.15f - awe * 0.1f);
            float val = Mathf.Clamp01(1f - unease * 0.12f + awe * 0.05f);
            color.colorFilter.value = Color.HSVToRGB(hue, sat, val);
            color.contrast.value = Mathf.Lerp(contrastVal, -contrastVal, unease);
        }

        // ── Vignette ───────────────────────────────────────────────────
        // unease closes in; content/awe open up
        if (volume.profile.TryGet<Vignette>(out var vignette)) {
            vignette.intensity.value = Mathf.Lerp(0.1f, 0.55f, unease);
            vignette.color.value = Color.Lerp(
                new Color(0.05f, 0.05f, 0.05f), // near-black neutral
                new Color(0.0f, 0.03f, 0.06f),  // very subtle cold tint at peak unease
                unease
            );
        }

        // ── Chromatic aberration ───────────────────────────────────────
        // unease = psychological fringing, not awe
        if (volume.profile.TryGet<ChromaticAberration>(out var ca)) {
            ca.intensity.value = unease * 0.4f;
        }

        // // ── Film grain ─────────────────────────────────────────────────
        // // unease = anxiety noise
        // if (volume.profile.TryGet<FilmGrain>(out var grain)) {
        //     grain.intensity.value = unease * 0.4f;
        //     grain.response.value = 0.8f;
        // }

        // ── Depth of field ─────────────────────────────────────────────
        // awe = dreamlike, vast; world slightly loses focus at edges
        if (volume.profile.TryGet<DepthOfField>(out var dof)) {
            dof.gaussianStart.value = Mathf.Lerp(8f, 2f, awe);
            dof.gaussianEnd.value   = Mathf.Lerp(20f, 6f, awe);
            dof.gaussianMaxRadius.value = awe * 1.5f;
        }

        // ── Lens distortion ────────────────────────────────────────────
        // content = subtle barrel (warmth/closeness), awe = pincushion (vastness)
        if (volume.profile.TryGet<LensDistortion>(out var lens)) {
            lens.intensity.value = Mathf.Lerp(-0.12f, 0.15f, awe) * Mathf.Max(content, awe);
        }
    }
}
