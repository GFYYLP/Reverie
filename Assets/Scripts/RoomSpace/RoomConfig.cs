using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomConfig {
    public float spikeScore;
    public float symmetryScore;
    public float verticalityScore;
    public float densityScore;
    public float fragmentationScore;
    public float radialScore;

    public float roomScale=1f;
    
    public void UpdateScores()
    {
        float content = Emotion.Instance.content;
        float unease = Emotion.Instance.unease;
        float awe = Emotion.Instance.awe;
        float intensity = Emotion.Instance.intensity;

        //adjust room config following the emotions
        float nudge = Mathf.Lerp(0.05f, 0.45f, intensity);
        float noise = 0.08f;
        float N() => (Random.value - 0.5f) * 2f * noise;  // local helper
        symmetryScore     = Mathf.Lerp(symmetryScore,     content * 1.0f + awe * 0.2f,              nudge) + N();
        spikeScore        = Mathf.Lerp(spikeScore,        unease  * 0.8f + awe * 0.5f,              nudge) + N();
        densityScore      = Mathf.Lerp(densityScore,      awe     * 0.9f + content * 0.4f + unease * 0.1f, nudge) + N();
        fragmentationScore= Mathf.Lerp(fragmentationScore,unease  * 0.9f + awe * 0.3f,              nudge) + N();
        radialScore       = Mathf.Lerp(radialScore,       content * 0.8f + awe * 0.7f,              nudge) + N();
        verticalityScore  = Mathf.Lerp(verticalityScore,  awe     * 0.8f,                          nudge) + N();
    }
    
    // default for first room: blank slate, monotone world
    // public static RoomConfig Default() => new RoomConfig {
    //     spikeScore        = 0f,
    //     symmetryScore     = 0.5f,
    //     verticalityScore  = 0.5f,
    //     roomScale         = 1f
    // };
}