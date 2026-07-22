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

    public float roomScale = 1f;
    [HideInInspector] public float seed;
    [HideInInspector] public float roomHue;

    public void UpdateScores()
    {
        float content = Emotion.Instance.content*0.05f;
        float unease = Emotion.Instance.unease*1.0f;
        float awe = Emotion.Instance.awe*1.0f;
        float intensity = Emotion.Instance.intensity;

        seed = Random.value * 9973f; // fresh scatter each room

        // Derive hue from seed, excluding yellow-green band (70°–130° = 0.194–0.361)
        // which reads as sickly/unnatural when used as ambient neutral tint.
        float t = Mathf.Repeat(seed * 0.731f, 0.833f);
        roomHue = 0f;// < 0.194f ? t : t + 0.167f;

        //adjust room config following the emotions
        float nudge = Mathf.Lerp(0.05f, 0.45f, intensity);
        float noise = 0.08f;
        float N() => (Random.value - 0.5f) * 2f * noise;  // local helper
        // target values are normalized so competing emotions don't sum past 1
        float C01(float v) => (v);
        symmetryScore      = Mathf.Lerp(symmetryScore,      C01(content * 0.8f + awe * 0.2f),                   nudge) + N();
        spikeScore         = Mathf.Lerp(spikeScore,         C01(unease  * 0.7f + awe * 0.4f),                   nudge) + N();
        densityScore       = Mathf.Lerp(densityScore,       C01(awe     * 0.6f + content * 0.4f + unease * 0.1f), nudge) + N();
        fragmentationScore = Mathf.Lerp(fragmentationScore, C01(unease  * 0.7f + awe * 0.2f),                   nudge) + N();
        radialScore        = Mathf.Lerp(radialScore,        C01(content * 0.8f + awe * 0.6f),                   nudge) + N();
        verticalityScore   = Mathf.Lerp(verticalityScore,   C01(awe     * 0.7f),                                nudge) + N();
    }
    
    // default for first room: blank slate, monotone world
    // public static RoomConfig Default() => new RoomConfig {
    //     spikeScore        = 0f,
    //     symmetryScore     = 0.5f,
    //     verticalityScore  = 0.5f,
    //     roomScale         = 1f
    // };
}