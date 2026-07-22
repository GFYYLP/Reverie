using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Snapshot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler {
    
    public Material decalBaseMaterial; // URP/Decal material as template
    public Material mat;
    public List<RenderTexture> capturedFrames;
    
    RawImage display;
    Canvas canvas;
    RectTransform rectTransform;
    Vector2 originalSize;
    bool dropHandled;
    bool inDecalMode;
    GameObject dragProxy;

    [HideInInspector] public RectTransform page;
    [HideInInspector] public RectTransform rightPage;
    [HideInInspector] public ShotProjector shotProjector;
    [HideInInspector] public Collage collage;
    
    public float snapShotSize = 0.5f;

    private float frameRate = 0.33f;
    private int frameIndex = 0;
    private float memoryValue = 1f;
    
    public Material ActiveMaterial() => mat;

    public RenderTexture RepresentativeFrame =>
        capturedFrames != null && capturedFrames.Count > 0 ? capturedFrames[0] : null;
    public bool HasFrames => capturedFrames != null && capturedFrames.Count > 0;

    void Awake() {
        display = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();
        capturedFrames = new List<RenderTexture>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Init(Material baseMaterial) {
        decalBaseMaterial = baseMaterial;
        mat = new Material(baseMaterial);
    }
    
    void Start() { }

    private float currTimer = 0f;
    void Update()
    {
        const float frameRate = 0.33f;
        currTimer += Time.deltaTime;
        if (capturedFrames != null && capturedFrames.Count > 1 && currTimer >= frameRate)
        {
            frameIndex = ((frameIndex + 1) % capturedFrames.Count);
            mat.SetTexture("_BaseMap", capturedFrames[frameIndex]);
            display.texture = capturedFrames[frameIndex];
            
            currTimer -= frameRate;
        }
        
        //fade away over time 
        // memoryValue -= Time.deltaTime * 0.1f;
        //materials[frameIndex].color = new Color(1f, 1f, 1f, memoryValue);
        // if (memoryValue <= 0)
        // {
        //     Destroy(gameObject);
        // }
    }

    public void SetDefault(RenderTexture frame) {
        display.texture = frame;
        mat.SetTexture("_BaseMap", frame);
    }

    public void StoreCapture(RenderTexture frame) {
        capturedFrames.Add(frame);
        mat.SetTexture("_BaseMap", capturedFrames[frameIndex]);  //apply once at init for single-frame photos
        display.texture = frame;
        display.color = Color.white;
    
        // calculate the centered square region in UV space
        // float aspect = (float)rt.width / rt.height;
        // if (aspect > 1f) {
        //     // wider than tall: crop sides
        //     float uvWidth = (float)rt.height / rt.width;
        //     float uvX = (1f - uvWidth) * 0.5f;
        //     display.uvRect = new Rect(uvX, 0f, uvWidth, 1f);
        // } else {
        //     // taller than wide: crop top and bottom
        //     float uvHeight = (float)rt.width / rt.height;
        //     float uvY = (1f - uvHeight) * 0.5f;
        //     display.uvRect = new Rect(0f, uvY, 1f, uvHeight);
        // }
    }

    public void OnBeginDrag(PointerEventData e) {
        if (capturedFrames == null || capturedFrames.Count == 0) return;
        originalSize = rectTransform.sizeDelta;
        dropHandled  = false;
        inDecalMode  = false;

        // proxy follows cursor; this slot stays in place
        dragProxy = new GameObject("DragProxy");
        dragProxy.transform.SetParent(canvas.transform);
        dragProxy.transform.SetAsLastSibling();
        var img = dragProxy.AddComponent<RawImage>();
        img.texture = capturedFrames[frameIndex];
        img.raycastTarget = false;
        var proxyRect = dragProxy.GetComponent<RectTransform>();
        proxyRect.sizeDelta   = originalSize;
        proxyRect.localScale  = Vector3.one;

        display.raycastTarget = false; // slot doesn't intercept drops
    }

    public void OnDrag(PointerEventData e) {
        if (dragProxy == null) return;
        var proxyRect = dragProxy.GetComponent<RectTransform>();

        bool overUI = IsOverUI(e.position, e.pressEventCamera);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            e.position, e.pressEventCamera,
            out Vector2 localPos
        );

        if (overUI) {
            if (inDecalMode) {
                inDecalMode = false;
                collage?.SlideUpFromDecal();
            }
            proxyRect.localPosition = localPos;
            proxyRect.sizeDelta = Vector2.Lerp(proxyRect.sizeDelta, originalSize, Time.deltaTime * 10f);
            proxyRect.sizeDelta = Vector2.Max(proxyRect.sizeDelta, originalSize);
        } else {
            if (!inDecalMode) {
                inDecalMode = true;
                collage?.SlideDownForDecal();
            }
            float screenSize  = shotProjector.CaptureSize * Mathf.Min(Screen.width, Screen.height);
            float canvasUnits = screenSize * (canvas.GetComponent<RectTransform>().rect.width / Screen.width);
            proxyRect.localPosition = Vector2.Lerp(proxyRect.localPosition, Vector2.zero, Time.deltaTime * 8f);
            proxyRect.sizeDelta     = Vector2.Lerp(proxyRect.sizeDelta, new Vector2(canvasUnits, canvasUnits), Time.deltaTime * 8f);
        }
    }

    public void OnEndDrag(PointerEventData e) {
        Destroy(dragProxy);
        display.raycastTarget = true;
        inDecalMode = false;
        collage?.SlideUpFromDecal();
        Album.Instance.EvaluateMatch();

        if (dropHandled) return;

        bool overUI = IsOverUI(e.position, e.pressEventCamera);
        if (!overUI) {
            shotProjector?.ProjectDecalAtCursor(this, e.position);
            Clear();
        }
    }

    public void NotifyDropHandled() => dropHandled = true;

    public void OnDrop(PointerEventData e) { }

    // blank this slot in place
    public void Clear() {
        capturedFrames.Clear();
        frameIndex = 0;
        currTimer  = 0f;
        display.texture = null;
        display.color   = new Color(1, 1, 1, 0); // fully transparent
        var cg = GetComponent<CanvasGroup>();
        if (cg != null) { cg.interactable = false; cg.blocksRaycasts = false; }
    }

    private bool IsOverUI(Vector2 screenPos, Camera cam) {
        // bounds check against the actual drop target, rather than raycasting the whole
        // scene (which picks up unrelated large UI panels and makes the decal-mode
        // threshold feel arbitrarily far from the album)
        if (rightPage == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rightPage, screenPos, cam);
    }

    private void OnDestroy()
    {
        //clean up materials
        Destroy(mat);
        
    }
}