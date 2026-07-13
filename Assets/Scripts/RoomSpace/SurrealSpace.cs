using UnityEngine;

public class SurrealSpace : MonoBehaviour
{
    [SerializeField] private Camera surrealCamera;
    [SerializeField] private int renderWidth  = 512;
    [SerializeField] private int renderHeight = 512;

    [SerializeField] private Texture2D[] images;
    [SerializeField] private int         currentImage = 0;
    
    [SerializeField] private Material skyboxMaterial;

    private Skybox        cameraSkybox;
    private Material      skyboxInstance;
    private RenderTexture rt;

    void Awake()
    {
        rt = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
        rt.Create();

        surrealCamera.targetTexture = rt;
        surrealCamera.clearFlags    = CameraClearFlags.Skybox;
        
        cameraSkybox = surrealCamera.gameObject.AddComponent<Skybox>();
        skyboxInstance = new Material(skyboxMaterial);
        cameraSkybox.material = skyboxInstance;

        Shader.SetGlobalTexture("_SurrealRT", rt);

        SetImage(currentImage);
        RandomiseRect();
    }

    public void AdvanceSpace()
    {
        SetImage(++currentImage);
        RandomiseRect();
    }

    public void RandomiseRect()
    {
        Vector2 size   = new Vector2(Random.Range(0.15f, 0.35f), Random.Range(0.15f, 0.35f));
        Vector2 origin = new Vector2(Random.Range(0.1f,  0.9f - size.x),
                                     Random.Range(0.2f,  0.8f - size.y));
        Shader.SetGlobalVector("_SurrealRect", new Vector4(origin.x, origin.y,
                                                           origin.x + size.x, origin.y + size.y));
    }

    public void SetImage(int index)
    {
        if (images == null || images.Length == 0) return;
        currentImage = index % images.Length;

        //set all six faces to the same image
        skyboxInstance.SetTexture("_FrontTex",  images[currentImage]);
        skyboxInstance.SetTexture("_BackTex",   images[currentImage]);
        skyboxInstance.SetTexture("_LeftTex",   images[currentImage]);
        skyboxInstance.SetTexture("_RightTex",  images[currentImage]);
        skyboxInstance.SetTexture("_UpTex",     images[currentImage]);
        skyboxInstance.SetTexture("_DownTex",   images[currentImage]);
    }

    void OnDestroy()
    {
        rt?.Release();
        if (skyboxInstance != null) Destroy(skyboxInstance);
    }
}
