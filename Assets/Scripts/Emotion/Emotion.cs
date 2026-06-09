using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Emotion : MonoBehaviour
{
    public static Emotion Instance;
    
    public float content;
    public float unease;
    public float awe;
    
    public float intensity;


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
