using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Composite : MonoBehaviour
{
    public Snapshot[] slots;

    public GameObject slotPrefab;
    public int slotCount = 25;
    public int canvasSize = 256;
    
    RenderTexture compositeRT;

    void Awake() {
        compositeRT = new RenderTexture(canvasSize, canvasSize, 0, 
            RenderTextureFormat.ARGB32);
        
        
        slots = new Snapshot[slotCount];
        for (int i = 0; i < slotCount; i++) {
            GameObject obj = Instantiate(slotPrefab, transform);
            slots[i] = obj.GetComponent<Snapshot>();
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

    void OnDestroy() {
        compositeRT?.Release();
    }
}
