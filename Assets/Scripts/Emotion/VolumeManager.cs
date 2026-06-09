using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeManager : MonoBehaviour
{
    private Volume volume;
    
    // Start is called before the first frame update
    void Start()
    {
         volume = GetComponent<Volume>();

    }

    // Update is called once per frame
    void Update()
    {
        UpdateVolumeParameters(
            Emotion.Instance.content,
            Emotion.Instance.unease,
            Emotion.Instance.awe
        );
    }
    
    void UpdateVolumeParameters(float content, float unease,  float awe) 
    {
        if (volume.profile.TryGet<Bloom>(out var bloom)) {
            bloom.threshold.value = Mathf.Lerp(1.2f, 0.3f, 
                Mathf.Max(content, awe));
            bloom.intensity.value = content * 3f;
        }
    
        if (volume.profile.TryGet<ColorAdjustments>(out var color)) {
            color.saturation.value  = Mathf.Lerp(-40f, 40f, content - unease);
            color.colorFilter.value = Color.Lerp(
                new Color(0.7f, 0.8f, 1.0f),
                new Color(1.0f, 0.95f, 0.8f),
                0.8f//warmth
            );
        }
    
        if (volume.profile.TryGet<MotionBlur>(out var mb)) {
            mb.intensity.value = awe * 0.4f;
        }
    
        if (volume.profile.TryGet<Vignette>(out var vignette)) {
            vignette.intensity.value = Mathf.Lerp(0.2f, 0.5f, unease);
        }
    
        if (volume.profile.TryGet<ChromaticAberration>(out var ca)) {
            ca.intensity.value = awe * 0.3f;
        }
    }
}
