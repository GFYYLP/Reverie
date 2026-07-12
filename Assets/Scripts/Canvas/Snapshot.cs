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
    
    RawImage display;  //UI display
    GameObject dragProxy; // floating copy while dragging
    Canvas canvas;

    [HideInInspector] public RectTransform page; // set by Composite, defines drag bounds
    
    public float snapShotSize = 0.5f;

    private float frameRate = 0.33f;
    private int frameIndex = 0;
    private float memoryValue = 1f;
    
    public Material ActiveMaterial() => mat;

    public RenderTexture RepresentativeFrame =>
        capturedFrames != null && capturedFrames.Count > 0 ? capturedFrames[0] : null;

    void Awake() {
        display = GetComponent<RawImage>();
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
        
        // create a floating proxy image that follows the cursor
        dragProxy = new GameObject("DragProxy");
        dragProxy.transform.SetParent(canvas.transform);
        dragProxy.transform.SetAsLastSibling(); // renders on top
        
        var img = dragProxy.AddComponent<RawImage>();
        img.texture = capturedFrames[frameIndex];
        img.raycastTarget = false; // dont block drops on slots beneath
        
        var rect = dragProxy.GetComponent<RectTransform>();
        rect.sizeDelta = GetComponent<RectTransform>().sizeDelta;
    }

    public void OnDrag(PointerEventData e) {
        if (dragProxy == null) return;
        // move proxy in canvas space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            e.position, e.pressEventCamera,
            out Vector2 localPos
        );
        dragProxy.GetComponent<RectTransform>().localPosition = localPos;

        // also move this snapshot, clamped within its page
        if (page != null) {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                page, e.position, e.pressEventCamera, out Vector2 pagePos);
            Vector2 half     = GetComponent<RectTransform>().sizeDelta * 0.5f;
            Vector2 pageHalf = page.rect.size * 0.5f;
            pagePos.x = Mathf.Clamp(pagePos.x, -pageHalf.x + half.x, pageHalf.x - half.x);
            pagePos.y = Mathf.Clamp(pagePos.y, -pageHalf.y + half.y, pageHalf.y - half.y);
            GetComponent<RectTransform>().localPosition = pagePos;
        }
    }

    public void OnEndDrag(PointerEventData e) {
        Destroy(dragProxy);
    }

    public void OnDrop(PointerEventData e) {
        var source = e.pointerDrag.GetComponent<Snapshot>();
        if (source == null || source.capturedFrames.Count == 0) return;

        // swap entire frame lists so multi-frame photos transfer completely
        (source.capturedFrames, capturedFrames) = (capturedFrames, source.capturedFrames);

        source.frameIndex = 0;
        source.currTimer = 0f;
        frameIndex = 0;
        currTimer = 0f;

        RenderTexture srcTex  = source.capturedFrames.Count > 0 ? source.capturedFrames[0] : null;
        RenderTexture destTex = capturedFrames.Count > 0 ? capturedFrames[0] : null;

        source.display.texture = srcTex;
        display.texture        = destTex;

        if (srcTex  != null) source.mat.SetTexture("_BaseMap", srcTex);
        if (destTex != null) mat.SetTexture("_BaseMap", destTex);
    }

    private void OnDestroy()
    {
        //clean up materials
        Destroy(mat);
        
    }
}