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
        content += contentValue* 10f;
        unease += uneaseValue* 10f;
        awe += aweValue* 10f;
        intensity += intensityValue * 10f; 
    }
    
    private void Awake()
    {
        Instance = this;
    }
}
