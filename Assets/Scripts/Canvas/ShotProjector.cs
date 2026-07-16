using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShotProjector : MonoBehaviour
{
    [SerializeField] private float projectionDepth = 0.2f; // thickness of the decal volume around the hit surface
    [SerializeField] private float fallbackDistance = 1f;  // used when no surface is hit
    [SerializeField] private int maxDecals = 16;            // pool size
    
    private DecalProjector[] pool;
    private int poolIndex = 0;
    private RTParser rtParser;

    void Awake()
    {
        rtParser = GetComponent<RTParser>();

        pool = new DecalProjector[maxDecals];
        for (int i = 0; i < maxDecals; i++)
        {
            GameObject go = new GameObject($"ShotDecal_{i}");
            go.transform.SetParent(transform);
            DecalProjector dp = go.AddComponent<DecalProjector>();
            
            //dp.material = new Material(decalBaseMaterial);
            
            dp.enabled = false;
            pool[i] = dp;
        }
    }
    
    public void ProjectDecalAtCursor(Snapshot snap, Vector2 screenPos)
    {
        Camera cam = Camera.main;
        Ray ray = cam.ScreenPointToRay(screenPos);

        float hitDistance = fallbackDistance;
        Quaternion rotation = cam.transform.rotation;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            hitDistance = hit.distance;
            rotation    = Quaternion.LookRotation(-hit.normal); // project along surface normal
        }

        float fovRad     = cam.fieldOfView * rtParser.CaptureSize * Mathf.Deg2Rad;
        float halfHeight = Mathf.Tan(fovRad * 0.5f);
        float sideLength = halfHeight * 2f * hitDistance;
        float depth      = hitDistance + projectionDepth;

        DecalProjector dp = pool[poolIndex % maxDecals];
        poolIndex++;

        dp.transform.position = cam.transform.position + ray.direction * (hitDistance - projectionDepth);
        dp.transform.rotation = rotation;
        dp.size               = new Vector3(sideLength, sideLength, depth);
        dp.pivot              = Vector3.zero;
        dp.material           = snap.ActiveMaterial();
        dp.enabled            = true;
    }

    public void ProjectDecal(Snapshot snap)
    {
        Camera cam = Camera.main;

        // Raycast to find the actual surface distance so we can scale the decal to match the captured FOV
        float hitDistance = fallbackDistance;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit))
            hitDistance = hit.distance;

        float fovRad     = cam.fieldOfView * rtParser.CaptureSize * Mathf.Deg2Rad;
        float halfHeight = Mathf.Tan(fovRad * 0.5f);
        float sideLength = halfHeight * 2f * hitDistance; // scale by distance: orthographic box must match the perspective frustum at the hit point

        DecalProjector dp = pool[poolIndex % maxDecals];
        poolIndex++;

        // Extend the box from the camera to just past the hit point so angled surfaces are always inside the volume
        float depth = hitDistance + projectionDepth;

        dp.transform.position = cam.transform.position;
        dp.transform.rotation = cam.transform.rotation;

        dp.size  = new Vector3(sideLength, sideLength, depth);
        dp.pivot = new Vector3(0f, 0f, depth * 0.5f);
        dp.material = snap.ActiveMaterial();
        dp.enabled = true;
        
        Debug.Log($"Projected decal at {dp.transform.position} with size {dp.size}");
    }
}
