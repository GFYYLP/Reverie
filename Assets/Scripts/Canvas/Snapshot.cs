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
    Transform originalParent;
    Vector2 originalPosition;
    Vector2 originalSize;
    bool dropHandled;
    bool inDecalMode;

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

    void Awake() {
        display = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();
        capturedFrames = new List<RenderTexture>();
    }

    public void Init(Material baseMaterial) {
        decalBaseMaterial = baseMaterial;
        mat = new Material(baseMaterial);
    }
    
    void Start() {
        canvas = GetComponentInParent<Canvas>(); // hierarchy is ready by Start
        
    }

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
        originalParent   = rectTransform.parent;
        originalPosition = rectTransform.localPosition;
        originalSize     = rectTransform.sizeDelta;
        dropHandled      = false;
        // reparent to canvas root so it renders above everything while dragging
        rectTransform.SetParent(canvas.transform);
        rectTransform.SetAsLastSibling();
        rectTransform.localScale  = Vector3.one;
        display.raycastTarget = false; // don't intercept drops on targets beneath
    }

    public void OnDrag(PointerEventData e) {
        display.raycastTarget = false;
        bool overUI = IsOverUI(e.position, e.pressEventCamera);
        display.raycastTarget = false; // keep off during drag

        if (overUI) {
            if (inDecalMode) {
                inDecalMode = false;
                collage?.SlideUpFromDecal();
            }
            rectTransform.sizeDelta = originalSize;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                e.position, e.pressEventCamera,
                out Vector2 localPos
            );
            rectTransform.localPosition = Vector2.Lerp(rectTransform.localPosition, localPos, Time.deltaTime * 20f);
        } else {
            if (!inDecalMode) {
                inDecalMode = true;
                collage?.SlideDownForDecal();
            }
            float screenSize  = shotProjector.CaptureSize * Mathf.Min(Screen.width, Screen.height);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            float canvasUnits = screenSize * (canvasRect.rect.width / Screen.width);
            rectTransform.sizeDelta     = Vector2.Lerp(rectTransform.sizeDelta, new Vector2(canvasUnits, canvasUnits), Time.deltaTime * 8f);
            rectTransform.localPosition = Vector2.Lerp(rectTransform.localPosition, Vector2.zero, Time.deltaTime * 8f);
        }
    }

    public void OnEndDrag(PointerEventData e) {
        rectTransform.sizeDelta = originalSize;

        inDecalMode = false;
        collage?.SlideUpFromDecal();

        if (dropHandled) {
            display.raycastTarget = true;
            return;
        }

        // temporarily disable our own raycast so IsOverUI sees what's beneath us
        display.raycastTarget = false;
        bool overUI = IsOverUI(e.position, e.pressEventCamera);
        display.raycastTarget = true;

        if (!overUI)
            shotProjector?.ProjectDecalAtCursor(this, e.position);

        rectTransform.SetParent(originalParent);
        rectTransform.localPosition = originalPosition;
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
        var pointer = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);
        return results.Count > 0;
    }

    private void OnDestroy()
    {
        //clean up materials
        Destroy(mat);
        
    }
}