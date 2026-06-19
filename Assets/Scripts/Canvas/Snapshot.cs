using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Snapshot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler {
    
    public Material decalBaseMaterial; // URP/Decal material as template
    public List<RenderTexture> capturedFrames;
    public List<Material> materials;
    RawImage display;  //UI display
    GameObject dragProxy; // floating copy while dragging
    Canvas canvas;
    
    public float snapShotSize = 0.5f;
    
    private int frameIndex = 0;
    private float memoryValue = 1f;
    
    public Material ActiveMaterial() => materials[frameIndex];

    void Awake() {
        display = GetComponent<RawImage>();
    }
    
    void Start() {
        canvas = GetComponentInParent<Canvas>(); // hierarchy is ready by Start
    }

    void Update()
    {
        if (capturedFrames != null && capturedFrames.Count > 1)
        {
            frameIndex = ((frameIndex + 1) % capturedFrames.Count);
            materials[frameIndex].SetTexture("_BaseMap", capturedFrames[frameIndex]);
        }
        
        //fade away over time 
        // memoryValue -= Time.deltaTime * 0.1f;
        //materials[frameIndex].color = new Color(1f, 1f, 1f, memoryValue);
        // if (memoryValue <= 0)
        // {
        //     Destroy(gameObject);
        // }
    }

    public void StoreCapture(RenderTexture frame) {
        capturedFrames.Add(frame);
        materials.Add(new Material(decalBaseMaterial));
        materials[frameIndex].SetTexture("_BaseMap", capturedFrames[frameIndex]);
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
        if (capturedFrames[frameIndex] == null) return;
        
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
        // follow cursor in canvas space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            e.position, e.pressEventCamera,
            out Vector2 localPos
        );
        dragProxy.GetComponent<RectTransform>().localPosition = localPos;
    }

    public void OnEndDrag(PointerEventData e) {
        Destroy(dragProxy);
    }

    public void OnDrop(PointerEventData e) {
        // swap with whatever was dragged onto us
        var source = e.pointerDrag.GetComponent<Snapshot>();
        if (source == null) return;
        
        (source.capturedFrames[frameIndex], capturedFrames[frameIndex]) 
            = (capturedFrames[frameIndex], source.capturedFrames[frameIndex]);
        
        source.display.texture = source.capturedFrames[frameIndex];
        display.texture        = capturedFrames[frameIndex];
    }

    private void OnDestroy()
    {
        //clean up materials
        for (int i = 0; i < capturedFrames.Count; i++)
        {
            materials.RemoveAt(i);
        }
        
    }
}