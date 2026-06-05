using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShotProjector : MonoBehaviour
{
    [SerializeField] private Material decalBaseMaterial; // URP/Decal material as template
    [SerializeField] private float projectionDepth = 5f; // how deep the projector volume extends
    [SerializeField] private int maxDecals = 16;         // pool size

    private DecalProjector[] pool;
    private int poolIndex = 0;
    private Composite composite;

    void Awake()
    {
        composite = GetComponent<Composite>();

        pool = new DecalProjector[maxDecals];
        for (int i = 0; i < maxDecals; i++)
        {
            GameObject go = new GameObject($"ShotDecal_{i}");
            DecalProjector dp = go.AddComponent<DecalProjector>();
            dp.enabled = false;
            pool[i] = dp;
        }
    }

    /// <summary>
    /// Call this immediately after takeShot() — pass the same RT and camera state.
    /// </summary>
    public void ProjectDecal(RenderTexture shotRT)
    {
        Camera cam = Camera.main;

        // --- 1. Size: match the cropped square in world space ---
        // captureSize narrowed the FOV by this factor, so the visible half-height
        // at projectionDepth is:
        float fovRad     = cam.fieldOfView * composite.CaptureSize * Mathf.Deg2Rad;
        float halfHeight = Mathf.Tan(fovRad * 0.5f) * projectionDepth;
        float sideLength = halfHeight * 2f; // square, so width == height

        // --- 2. Pose: place projector at camera, pointing forward ---
        DecalProjector dp = pool[poolIndex % maxDecals];
        poolIndex++;

        dp.transform.position = cam.transform.position;
        dp.transform.rotation = cam.transform.rotation 
                                * Quaternion.Euler(90f, 0f, 0f); 
        // DecalProjector projects along its local -Y, so we rotate camera's +Z → -Y

        dp.size = new Vector3(sideLength, sideLength, projectionDepth);
        dp.pivot = new Vector3(0f, 0f, projectionDepth * 0.5f); // origin at camera eye

        // --- 3. Material: stamp the RT onto a decal material instance ---
        Material mat = new Material(decalBaseMaterial);
        mat.SetTexture("_BaseMap", shotRT);
        dp.material = mat;

        dp.enabled = true;
    }
}
