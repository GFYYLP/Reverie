using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] private RectTransform topVignette;
    [SerializeField] private RectTransform bottomVignette;
    [SerializeField] private RectTransform leftVignette;
    [SerializeField] private RectTransform rightVignette;

    private Composite composite;
    private bool inCamera = false;

    void Awake()
    {
        composite = FindObjectOfType<Composite>();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            StartCoroutine(AnimateIn());
            inCamera = true;
        }
        
        if (Input.GetMouseButtonUp(0))
        {
            StartCoroutine(AnimateOut());
            inCamera = false;
        }
        

    }
    
    IEnumerator AnimateIn()
    {
        //convert to non-normalized screen-space coord
        float captureRegionSize = composite.CaptureSize * Mathf.Min(Screen.width, Screen.height);
        float duration = 0.2f;
        
        float t = 0f;
        float targetHeight = (Screen.height - captureRegionSize) * 0.5f;
        float targetWidth  = (Screen.width  - captureRegionSize) * 0.5f;

        while (t < 1f) {
            t += Time.deltaTime / duration;
            float ease = Mathf.SmoothStep(0f, 1f, t);

            topVignette.sizeDelta    = new Vector2(0, Mathf.Lerp(0, targetHeight, ease));
            bottomVignette.sizeDelta = new Vector2(0, Mathf.Lerp(0, targetHeight, ease));
            leftVignette.sizeDelta   = new Vector2(Mathf.Lerp(0, targetWidth, ease), 0);
            rightVignette.sizeDelta  = new Vector2(Mathf.Lerp(0, targetWidth, ease), 0);

            yield return null;
        }
    }
    
    IEnumerator AnimateOut()
    {
        float duration = 0.2f;
        float t = 0f;

        while (t < 1f) {
            t += Time.deltaTime / duration;
            float ease = Mathf.SmoothStep(0f, 1f, t);

            topVignette.sizeDelta    = new Vector2(0, Mathf.Lerp(topVignette.sizeDelta.y, 0, ease));
            bottomVignette.sizeDelta = new Vector2(0, Mathf.Lerp(bottomVignette.sizeDelta.y, 0, ease));
            leftVignette.sizeDelta   = new Vector2(Mathf.Lerp(leftVignette.sizeDelta.x, 0, ease), 0);
            rightVignette.sizeDelta  = new Vector2(Mathf.Lerp(rightVignette.sizeDelta.x, 0, ease), 0);

            yield return null;
        }
    }
}
