using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Composite : MonoBehaviour
{
    // [SerializeField] private Camera cam;
    [SerializeField] private GameObject slotPrefab;
    
    [SerializeField] private int slotCount = 25;
    [SerializeField] private int canvasSize = 256;
    
    [SerializeField, Range(0.1f, 1f)] private float captureSize = 0.4f;
    [SerializeField] private float shiftTick = 4f;
    
    private EmotionManager emotionManager;
    private CanvasManager canvasManager;
    private Snapshot[] slots;
    private ShotProjector shotProjector;
    private RenderTexture cameraRT;
    private RenderTexture compositeRT;
    
    private int currentSlot = 0;
    private bool isCapturing = false;
    private float timer=0f;

    void Awake()
    {
        canvasManager = GetComponentInParent<CanvasManager>();
        shotProjector =  GetComponent<ShotProjector>();
        
        emotionManager =  FindFirstObjectByType<EmotionManager>();
            
        cameraRT = new RenderTexture(Screen.width, Screen.height, 1);
        
        compositeRT = new RenderTexture(canvasSize, canvasSize, 0, 
            RenderTextureFormat.ARGB32);
        
        
        slots = new Snapshot[slotCount];
        for (int i = 0; i < slotCount; i++) {
            GameObject obj = Instantiate(slotPrefab, transform);
            Snapshot snap = obj.GetComponent<Snapshot>();
            if (snap == null) Debug.LogError($"Slot {i} prefab missing Snapshot component");
            else Debug.Log("i hate this");
            slots[i] = snap;
        }
    }

    private void OnEnable()
    {
        canvasManager.onSnapshot += TakeShot;
        canvasManager.onProject += DoProjection;
    }

    private void TakeShot()
    {
        RenderTexture rt = CaptureView();
        
        slots[currentSlot].StoreCapture(rt);
        ++currentSlot;
    }

    private void DoProjection()
    {
        int shotSlot = Math.Max(currentSlot-1, 0); 
        shotProjector?.ProjectDecal(slots[shotSlot].capturedTexture);
    }

    private RenderTexture CaptureView()
    {
        RenderTexture rt = new RenderTexture(512, 512, 24);

        // store original state
        float originalAspect = Camera.main.aspect;
        float originalFOV    = Camera.main.fieldOfView;

        // force square aspect to constrain the frustum
        Camera.main.aspect = 1f;

        // optionally narrow FOV to match captureSize crop feeling
        // smaller captureSize = more zoomed in
        Camera.main.fieldOfView = originalFOV * captureSize;

        Camera.main.targetTexture = rt;
        Camera.main.Render();
        Camera.main.targetTexture = null;

        // restore
        Camera.main.aspect         = originalAspect;
        Camera.main.fieldOfView    = originalFOV;

        //parse grammar
        emotionManager.ParseGrammar(rt);
        
        return rt;
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
            parseScreenGrammar();
        }
    }

    public RenderTexture BuildComposite() {
        // clear to black
        Graphics.SetRenderTarget(compositeRT);
        GL.Clear(true, true, Color.black);
    
        int gridW   = Mathf.CeilToInt(Mathf.Sqrt(slots.Length));
        float cellW = 1f / gridW;
    
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i].capturedTexture == null) continue;
        
            // compute normalized position of this slot in the canvas
            float x = (i % gridW) * cellW;
            float y = (i / gridW) * cellW;
        
            Rect dest = new Rect(x, y, cellW, cellW);
            Graphics.Blit(slots[i].capturedTexture, compositeRT, 
                new Vector2(cellW, cellW), 
                new Vector2(x, y));
        }
    
        return compositeRT;
    }

    private void registerSlot()
    {
        //look for the next unoccupied slot
        while (slots[currentSlot].capturedTexture != null) {
            currentSlot = (currentSlot + 1) % slots.Length;
        }
    }

    void OnDestroy() {
        compositeRT?.Release();
        cameraRT?.Release();
        slots = null;
        
        //cleanup target texture
        // Camera.main.targetTexture = null;
    }
    
    public float CaptureSize => captureSize;
}
