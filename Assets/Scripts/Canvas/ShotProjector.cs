using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShotProjector : MonoBehaviour
{
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
            
            //dp.material = new Material(decalBaseMaterial);
            
            dp.enabled = false;
            pool[i] = dp;
        }
    }
    
    public void ProjectDecal(Snapshot snap)
    {
        Camera cam = Camera.main;

        // Size (of projected frustum(?)): match the cropped square in world space
        float fovRad     = cam.fieldOfView * composite.CaptureSize * Mathf.Deg2Rad;
        float halfHeight = Mathf.Tan(fovRad * 0.5f);
        float sideLength = halfHeight * 2f; // square, so width == height

        //place projector at camera, pointing forward 
        DecalProjector dp = pool[poolIndex % maxDecals];
        poolIndex++;

        dp.transform.position = cam.transform.position;
        dp.transform.rotation = cam.transform.rotation;  // DecalProjector projects along its local -Y, so we rotate camera's +Z → -Y

        dp.size = new Vector3(sideLength, sideLength, projectionDepth);
        dp.pivot = new Vector3(0f, 0f, projectionDepth * 0.5f); //offset this from object's centre to camera's eye

        // stamp the RT onto a decal material instance
        
        // mat.SetTexture("_BaseMap", shotRT);
        
        
        dp.material = snap.ActiveMaterial();

        dp.enabled = true;
        
        Debug.Log($"Projected decal at {dp.transform.position} with size {dp.size}");
    }
}
