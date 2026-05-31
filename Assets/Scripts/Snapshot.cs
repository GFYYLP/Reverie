using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Snapshot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler {
    
    public RenderTexture capturedTexture;
    RawImage display;
    GameObject dragProxy; // floating copy while dragging
    Canvas canvas;

    void Awake() {
        display = GetComponent<RawImage>();
    }
    
    void Start() {
        canvas = GetComponentInParent<Canvas>(); // hierarchy is ready by Start
    }

    public void StoreCapture(RenderTexture rt) {
        capturedTexture = rt;
        display.texture = rt; // RawImage.texture accepts RenderTexture directly
    }

    public void OnBeginDrag(PointerEventData e) {
        if (capturedTexture == null) return;
        
        // create a floating proxy image that follows the cursor
        dragProxy = new GameObject("DragProxy");
        dragProxy.transform.SetParent(canvas.transform);
        dragProxy.transform.SetAsLastSibling(); // renders on top
        
        var img = dragProxy.AddComponent<RawImage>();
        img.texture = capturedTexture;
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
        
        (source.capturedTexture, capturedTexture) 
            = (capturedTexture, source.capturedTexture);
        
        source.display.texture = source.capturedTexture;
        display.texture        = capturedTexture;
    }
}