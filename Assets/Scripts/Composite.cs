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
    
    private Snapshot[] slots;
    private RenderTexture cameraRT;
    private RenderTexture compositeRT;
    
    private int currentSlot = 0;
    private bool isCapturing = false;

    void Awake() {
        cameraRT = new RenderTexture(Screen.width, Screen.height, 1);
        
        compositeRT = new RenderTexture(canvasSize, canvasSize, 0, 
            RenderTextureFormat.ARGB32);
        
        
        slots = new Snapshot[slotCount];
        for (int i = 0; i < slotCount; i++) {
            GameObject obj = Instantiate(slotPrefab, transform);
            slots[i] = obj.GetComponent<Snapshot>();
        }
    }


    private void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        
            Camera.main.targetTexture = rt;
            Camera.main.Render();        // force immediate render into rt
            Camera.main.targetTexture = null; // restore immediately
        
            slots[currentSlot].StoreCapture(rt);
            ++currentSlot;
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
    }
}
