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
        //emission tinted by room hue
        float emitHue = Mathf.Repeat(roomHue - content * 0.05f, 1f);  //content warms
        float emitSat = Mathf.Clamp01(0.5f - unease * 0.3f);        //unease suppresses
        Color emissionColor = Color.HSVToRGB(emitHue, emitSat, 1f)
            * Mathf.Max(content, awe) * 0.3f; //awe brightens
        roomRenderer.material.EnableKeyword("_EMISSION");
        roomRenderer.material.SetColor("_EmissionColor", emissionColor);
        

        if (volume.profile.TryGet<Bloom>(out var bloom)) {
            bloom.threshold.value = Mathf.Lerp(1.2f, 0.2f, Mathf.Max(content, awe));
            bloom.intensity.value = content * 2f + awe * 5f;
        }
        
        //unease drains the world of color
        //content/awe restore it
        if (volume.profile.TryGet<ColorAdjustments>(out var color)) {
            color.saturation.value = Mathf.Lerp(-60f, 30f, Mathf.Clamp01(Mathf.Max(content, awe) - unease * 0.8f));
            float hue = Mathf.Repeat(roomHue - content * 0.04f, 1f); //content nudges slightly warmer
            float sat = Mathf.Clamp01(0.2f + content * 0.2f - unease * 0.15f - awe * 0.1f);
            float val = Mathf.Clamp01(1f - unease * 0.12f + awe * 0.05f);
            color.colorFilter.value = Color.HSVToRGB(hue, sat, val);
            color.contrast.value = Mathf.Lerp(contrastVal, -contrastVal, unease);
        }
        
        // if (volume.profile.TryGet<Vignette>(out var vignette)) {
        //     vignette.intensity.value = Mathf.Lerp(0.1f, 0.55f, unease);
        //     vignette.color.value = Color.Lerp(
        //         new Color(0.05f, 0.05f, 0.05f), // near-black neutral
        //         new Color(0.0f, 0.03f, 0.06f),  // very subtle cold tint at peak unease
        //         unease
        //     );
        // }
        
        //psychological fringing
        if (volume.profile.TryGet<ChromaticAberration>(out var ca)) {
            ca.intensity.value = unease * 0.4f;
        }
        
        // // unease = anxiety noise
        // if (volume.profile.TryGet<FilmGrain>(out var grain)) {
        //     grain.intensity.value = unease * 0.4f;
        //     grain.response.value = 0.8f;
        // }
        
        //world slightly loses focus at edges with awe
        if (volume.profile.TryGet<DepthOfField>(out var dof)) {
            dof.gaussianStart.value = Mathf.Lerp(8f, 2f, awe);
            dof.gaussianEnd.value   = Mathf.Lerp(20f, 6f, awe);
            dof.gaussianMaxRadius.value = awe * 1.5f;
        }
        
        // //content = subtle barrel (warmth/closeness)
        // //awe = pincushion (vastness)
        // if (volume.profile.TryGet<LensDistortion>(out var lens)) {
        //     lens.intensity.value = Mathf.Lerp(-0.12f, 0.15f, awe) * Mathf.Max(content, awe);
        // }
    }
}
