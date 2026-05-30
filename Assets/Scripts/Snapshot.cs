using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Snapshot : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler {
    
    public Texture2D capturedTexture; // the type B snapshot stored here
    RawImage display;
    GameObject dragProxy; // floating copy while dragging
    Canvas canvas;

    void Awake() {
        display = GetComponent<RawImage>();
        canvas  = GetComponentInParent<Canvas>();
    }

    public void StoreCapture(Texture2D tex) {
        capturedTexture = tex;
        display.texture = tex;
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