using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomConfig {
    public float spikeScore;
    public float symmetryScore;
    public float verticalityScore;
    public float roomScale;

    // default for first room: blank slate, monotone world
    public static RoomConfig Default() => new RoomConfig {
        spikeScore        = 0f,
        symmetryScore     = 0.5f,
        verticalityScore  = 0.5f,
        roomScale         = 1f
    };
}