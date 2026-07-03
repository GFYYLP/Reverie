using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Emotion : MonoBehaviour
{
    public static Emotion Instance;
    
    public float content=0.3f;
    public float unease=0.3f;
    public float awe=0.3f;
    
    public float intensity=0.3f;


    public void UpdateState(float contentValue, float uneaseValue, float aweValue, float intensityValue)
    {
        content = Mathf.Clamp(content + contentValue * 10f, 0f, 100f);
        unease = Mathf.Clamp(unease + uneaseValue * 1f, 0f, 100f);
        awe = Mathf.Clamp(awe + aweValue * 10f, 0f, 100f);
        intensity = Mathf.Clamp(intensity + intensityValue * 10f, 0f, 100f);
    }
    
    private void Awake()
    {
        Instance = this;
    }
}
