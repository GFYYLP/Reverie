using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeManager : MonoBehaviour
{
    private Volume volume;
    [SerializeField] private Renderer roomRenderer;
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float contrastVal = 60f;

    private float roomHue = 0f;
    private Color baseOutlineColor = Color.black;

    public void SetRoom(RoomConfig config) {
        roomHue = config.roomHue;
    }

    void Start()
    {
        volume = GetComponent<Volume>();
        if (outlineMaterial != null) baseOutlineColor = outlineMaterial.GetColor("_OutlineColor");
    }

    // Update is called once per frame
    void Update()
    {
        float content = Emotion.Instance.content * 0.01f;
        float unease = Emotion.Instance.unease * 0.01f;
        float awe = Emotion.Instance.awe * 0.01f;
        
        UpdateVolumeParameters(content, unease, awe);
        UpdateOutlineParameters(content, unease, awe);
    }

    void UpdateVolumeParameters(float content, float unease,  float awe)
    {
        //emission tinted by room hue 
        // float emitHue = Mathf.Repeat(roomHue - content * 0.05f, 1f);  //content warms
        // float emitSat = Mathf.Clamp01(0.5f - unease * 0.3f);        //unease suppresses
        // Color emissionColor = Color.HSVToRGB(emitHue, emitSat, 1f) * content * 0.3f;
        // roomRenderer.material.EnableKeyword("_EMISSION");
        // roomRenderer.material.SetColor("_EmissionColor", emissionColor);


        if (volume.profile.TryGet<Bloom>(out var bloom)) {
            bloom.threshold.value = Mathf.Lerp(1.2f, 0.2f, content);
            bloom.intensity.value = content * 2.5f;
            bloom.scatter.value = Mathf.Lerp(0.4f, 0.9f, content); //wider/softer glow rather than just brighter
        }

        //unease drains the world of color
        //content restores it, then keeps climbing past "cozy" into something oversaturated and artificial
        if (volume.profile.TryGet<ColorAdjustments>(out var color)) {
            color.saturation.value = Mathf.Lerp(-100f, 100f, Mathf.Clamp01(content - unease * 0.8f));

            float hue = roomHue;//Mathf.Repeat(roomHue - content * 0.04f, 1f); //content nudges slightly warmer
            float sat = Mathf.Clamp01(0.2f + content * 0.2f - unease * 0.15f);
            float val = Mathf.Clamp01(1f - unease * 0.12f + content * 0.1f);
            //color.colorFilter.value = Color.HSVToRGB(hue, sat, val);
            
            color.contrast.value = Mathf.Lerp(0f, contrastVal, unease);
            color.postExposure.value = content * 0.35f; 
        }

        //fear
        //highlights turned inside out, flickering
        if (volume.profile.TryGet<ShadowsMidtonesHighlights>(out var smh)) {
            float flicker = 1f - Mathf.PerlinNoise(Time.time, 0f) * unease * 0.5f;
            float highlightScalar = Mathf.Clamp01(1f - unease);// * flicker);
            smh.highlights.value = new Vector4(highlightScalar, highlightScalar, highlightScalar, 0f);
            smh.highlightsEnd.value = Mathf.Lerp(1f, 0.45f, unease * 0.8f);
        }

        //psychological fringing
        if (volume.profile.TryGet<ChromaticAberration>(out var ca)) {
            ca.intensity.value = unease * 0.4f;
        }

        //awe
        //motion blur, depth of field and lens distortion 
        if (volume.profile.TryGet<MotionBlur>(out var blur)) {
            blur.intensity.value = awe * 0.8f;
            blur.clamp.value = Mathf.Lerp(0.05f, 0.2f, awe);
        }
        if (volume.profile.TryGet<DepthOfField>(out var dof)) {
            dof.aperture.value = Mathf.Lerp(16f, 1f, awe);   
            dof.focalLength.value = Mathf.Lerp(50f, 12f, awe);
        }
        if (volume.profile.TryGet<LensDistortion>(out var lens)) {
            lens.intensity.value = Mathf.Lerp(0f, 0.6f, awe);   
            lens.scale.value = Mathf.Lerp(1f, 1.15f, awe);  //hides the resulting edge stretching
        }

        UpdateOutlineParameters(content, unease, awe);
    }

    void UpdateOutlineParameters(float content, float unease, float awe)
    {
        if (outlineMaterial == null) return;
        
        outlineMaterial.SetFloat("_EdgeSoftness", content);     //content: feather into a glow
        outlineMaterial.SetFloat("_JitterAmount", unease * 6f);  //unease: tremble
        outlineMaterial.SetFloat("_WarpAmount", awe);             //awe: billow
        outlineMaterial.SetFloat("_WarpFrequency", Mathf.Lerp(0.4f, 1.5f, awe));

        Color outlineColor = baseOutlineColor;
        outlineColor.a = Mathf.Lerp(baseOutlineColor.a, 0.3f, content); //never fully vanishes
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
    }
}
