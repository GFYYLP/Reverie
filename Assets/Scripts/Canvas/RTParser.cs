using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RTParser : MonoBehaviour
{
    // [SerializeField] private Camera cam;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Material decalBaseMaterial; // URP/Decal material as template
    [SerializeField] private int slotCount = 25;
    [SerializeField] private int canvasSize = 256;

    [Header("Pages")]
    [SerializeField] private RectTransform leftPage;
    [SerializeField] private RectTransform rightPage;
    
    [SerializeField, Range(0.1f, 1f)] private float captureSize = 0.4f;
    [SerializeField] private float shiftTick = 4f;
    
    private EmotionManager emotionManager;
    private Photographic _photographic;
    private Snapshot[] slots;
    private ShotProjector shotProjector;
    private RenderTexture cameraRT;
    private RenderTexture compositeRT;
    
    private RenderTexture defaultRT;
    private int currentSlot = 0;
    private bool wasRecording = false;
    private float timer=0f;

    void Awake()
    {
        _photographic = GetComponentInParent<Photographic>();
        shotProjector =  GetComponent<ShotProjector>();
        
        emotionManager =  FindFirstObjectByType<EmotionManager>();
            
        cameraRT = new RenderTexture(Screen.width, Screen.height, 1);
        
        compositeRT = new RenderTexture(canvasSize, canvasSize, 0, 
            RenderTextureFormat.ARGB32);
        
        
        // shared default white RT
        defaultRT = new RenderTexture(2, 2, 0, RenderTextureFormat.ARGB32);
        defaultRT.wrapMode = TextureWrapMode.Repeat;
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = defaultRT;
        GL.Clear(true, true, Color.white);
        RenderTexture.active = prev;

        slots = new Snapshot[slotCount];
        for (int i = 0; i < slotCount; i++) {
            GameObject obj = Instantiate(slotPrefab, leftPage != null ? leftPage : transform);
            Snapshot snap = obj.GetComponent<Snapshot>();
            if (snap == null) { Debug.LogError($"Slot {i} prefab missing Snapshot component"); continue; }
            snap.Init(decalBaseMaterial);
            snap.page         = leftPage  != null ? leftPage  : transform as RectTransform;
            snap.rightPage    = rightPage != null ? rightPage : transform as RectTransform;
            snap.shotProjector = shotProjector;
            slots[i] = snap;
        }
    }

    void Start()
    {
        foreach (var slot in slots)
            slot.SetDefault(defaultRT);
    }

    private void OnEnable()
    {
        _photographic.onSnapshot += TakeShot;
        _photographic.onProject += DoProjection;
    }

    private float currTimer = 0f;
    const float frameRate = 0.33f;
    private void TakeShot(bool isRecording)
    {
        if (isRecording)
        {
            wasRecording = true;
            currTimer += Time.deltaTime;
            if (currTimer >= frameRate)
            {
                StartCoroutine(CaptureView(slots[currentSlot]));
                currTimer -= frameRate;
            }
        }
        else
        {
            //if previously not recording, take a screenshot
            if (!wasRecording) StartCoroutine(CaptureView(slots[currentSlot])); 

            wasRecording = false;
            currTimer = 0f;
            ++currentSlot;
        }
    }

    private void DoProjection()
    {
        int shotSlot = Math.Max(currentSlot-1, 0); 
        shotProjector?.ProjectDecal(slots[shotSlot]);
    }

    private IEnumerator CaptureView(Snapshot slot)
    {
        yield return new WaitForEndOfFrame();

        int size = Mathf.RoundToInt(captureSize * Mathf.Min(Screen.width, Screen.height));
        int x = (Screen.width  - size) / 2;
        int y = (Screen.height - size) / 2;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(x, y, size, size), 0, 0);
        tex.Apply();

        RenderTexture rt = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32);
        rt.wrapMode = TextureWrapMode.Repeat;
        Graphics.Blit(tex, rt);
        Destroy(tex);

        slot.StoreCapture(rt);
        emotionManager.ParseGrammar(rt);
    }

    public void parseScreenGrammar()
    {
        RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
        
        Camera.main.targetTexture = rt;
        Camera.main.Render();
        Camera.main.targetTexture = null;
        
        emotionManager.ParseGrammar(rt);
        
        RenderTexture.ReleaseTemporary(rt);
    }

    private void LateUpdate()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            captureSize = Mathf.Clamp(captureSize - scroll * 0.1f, 0.1f, 1f);
        }
        
        //parse screen grammar (once every 4 seconds)
        timer  += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space))//timer > shiftTick)
        {
            timer -= shiftTick;
            //parseScreenGrammar();
        }
    }

    // public RenderTexture BuildComposite() {
    //     // clear to black
    //     Graphics.SetRenderTarget(compositeRT);
    //     GL.Clear(true, true, Color.black);
    //
    //     int gridW   = Mathf.CeilToInt(Mathf.Sqrt(slots.Length));
    //     float cellW = 1f / gridW;
    //
    //     for (int i = 0; i < slots.Length; i++) {
    //         if (slots[i].capturedTexture == null) continue;
    //     
    //         // compute normalized position of this slot in the canvas
    //         float x = (i % gridW) * cellW;
    //         float y = (i / gridW) * cellW;
    //     
    //         Rect dest = new Rect(x, y, cellW, cellW);
    //         Graphics.Blit(slots[i].capturedTexture, compositeRT, 
    //             new Vector2(cellW, cellW), 
    //             new Vector2(x, y));
    //     }
    //
    //     return compositeRT;
    // }

    // private void registerSlot()
    // {
    //     //look for the next unoccupied slot
    //     while (slots[currentSlot].capturedTexture != null) {
    //         currentSlot = (currentSlot + 1) % slots.Length;
    //     }
    // }
    
    void OnDestroy() {
        compositeRT?.Release();
        cameraRT?.Release();
        defaultRT?.Release();
        slots = null;
        
        //cleanup target texture
        // Camera.main.targetTexture = null;
    }
    
    public float CaptureSize => captureSize;

    public RenderTexture[] GetCapturedTextures()
    {
        var filled = new System.Collections.Generic.List<RenderTexture>();
        foreach (var slot in slots)
        {
            var frame = slot.RepresentativeFrame;
            if (frame != null) filled.Add(frame);
        }
        return filled.ToArray();
    }
}
