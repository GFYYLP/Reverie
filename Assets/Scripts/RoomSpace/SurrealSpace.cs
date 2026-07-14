using UnityEngine;

public class SurrealSpace : MonoBehaviour
{
    [SerializeField] private Camera surrealCamera;
    [SerializeField] private int renderWidth  = 512;
    [SerializeField] private int renderHeight = 512;

    [Header("Camera Follow")]
    [SerializeField, Range(0f, 1f)] private float positionLag    = 0.92f; // higher = slower follow
    [SerializeField, Range(0f, 1f)] private float yawLag         = 0.85f;
    [SerializeField, Range(0f, 1f)] private float pitchInfluence = 0.3f;  // how much pitch is inherited
    [SerializeField]                private float positionOffset  = 2f;    // world-space offset from main cam

    [Header("Skybox")]
    [SerializeField] private Texture2D[] images;
    [SerializeField] private int         currentImage = 0;
    [SerializeField] private Material    skyboxMaterial;

    [Header("Normal Gate")]
    [SerializeField] private Vector3     gateDir     = Vector3.up;
    [SerializeField, Range(-1f, 1f)] private float gateMin = 0.4f;
    [SerializeField, Range(-1f, 1f)] private float gateMax = 1.0f;

    private Skybox        cameraSkybox;
    private Material      skyboxInstance;
    private RenderTexture rt;

    private Vector3 smoothPos;
    private float   smoothYaw;
    private float   smoothPitch;

    void Awake()
    {
        rt = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        surrealCamera.targetTexture = rt;
        surrealCamera.clearFlags    = CameraClearFlags.Skybox;

        cameraSkybox          = surrealCamera.gameObject.AddComponent<Skybox>();
        skyboxInstance        = new Material(skyboxMaterial);
        cameraSkybox.material = skyboxInstance;

        Shader.SetGlobalTexture("_SurrealRT", rt);

        if (Camera.main != null)
        {
            smoothPos   = Camera.main.transform.position;
            smoothYaw   = Camera.main.transform.eulerAngles.y;
            smoothPitch = Camera.main.transform.eulerAngles.x;
        }

        SetImage(currentImage);
        RandomizeGate();
    }

    void Update()
    {
        if (Camera.main == null) return;

        Transform main = Camera.main.transform;

        // lag position — offset slightly sideways so it's a genuinely different vantage
        Vector3 targetPos = main.position + main.right * positionOffset;
        smoothPos = Vector3.Lerp(targetPos, smoothPos, positionLag);

        // lag yaw fully, inherit pitch partially
        float targetYaw   = main.eulerAngles.y;
        float targetPitch = main.eulerAngles.x;

        smoothYaw   = Mathf.LerpAngle(targetYaw,   smoothYaw,   yawLag);
        smoothPitch = Mathf.LerpAngle(targetPitch, smoothPitch, 1f - pitchInfluence);

        surrealCamera.transform.position    = smoothPos;
        surrealCamera.transform.eulerAngles = new Vector3(smoothPitch, smoothYaw, 0f);

        Shader.SetGlobalVector("_SurrealGateDir", new Vector4(gateDir.x, gateDir.y, gateDir.z, 0));
        Shader.SetGlobalFloat ("_SurrealGateMin", gateMin);
        Shader.SetGlobalFloat ("_SurrealGateMax", gateMax);
    }

    public void AdvanceSpace()
    {
        SetImage(++currentImage);
        RandomizeGate();
    }

    public void RandomizeGate()
    {
        gateDir = Random.onUnitSphere;
        gateMin = Random.Range(0.3f, 0.6f);
        gateMax = Mathf.Min(gateMin + Random.Range(0.2f, 0.5f), 1f);

        Shader.SetGlobalVector("_SurrealGateDir", new Vector4(gateDir.x, gateDir.y, gateDir.z, 0));
        Shader.SetGlobalFloat ("_SurrealGateMin", gateMin);
        Shader.SetGlobalFloat ("_SurrealGateMax", gateMax);
    }

    public void SetImage(int index)
    {
        if (images == null || images.Length == 0) return;
        currentImage = index % images.Length;

        skyboxInstance.SetTexture("_FrontTex", images[currentImage]);
        skyboxInstance.SetTexture("_BackTex",  images[currentImage]);
        skyboxInstance.SetTexture("_LeftTex",  images[currentImage]);
        skyboxInstance.SetTexture("_RightTex", images[currentImage]);
        skyboxInstance.SetTexture("_UpTex",    images[currentImage]);
        skyboxInstance.SetTexture("_DownTex",  images[currentImage]);
    }

    void OnDestroy()
    {
        rt?.Release();
        if (skyboxInstance != null) Destroy(skyboxInstance);
    }
}
