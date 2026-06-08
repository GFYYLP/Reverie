using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Emotion : MonoBehaviour
{
    public float spikeScore;
    public float symmetryScore;
    public float verticalityScore;
    public float roomScale;
    public float densityScore;
    public float fragmentationScore;
    public float radialScore;

    public static Emotion Instance;
    
    [HideInInspector] public float content;
    [HideInInspector] public float unease;
    [HideInInspector] public float awe;
    
    private float intensity;


    public void UpdateState(float contentValue, float uneaseValue, float aweValue, float intensityValue)
    {
        content = contentValue;
        unease = uneaseValue;
        awe = aweValue;
        intensity = intensityValue; 
    }
    
    private void Awake()
    {
        Instance = this;
    }
}
