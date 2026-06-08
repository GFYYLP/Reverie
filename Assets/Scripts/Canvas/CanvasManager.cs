using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    [SerializeField] private RectTransform topVignette;
    [SerializeField] private RectTransform bottomVignette;
    [SerializeField] private RectTransform leftVignette;
    [SerializeField] private RectTransform rightVignette;
    [SerializeField] private Color vignetteColor;
    
    [SerializeField] private Color topVignetteColor;
    [SerializeField] private Color bottomVignetteColor;
    [SerializeField] private Color leftVignetteColor;
    [SerializeField] private Color rightVignetteColor;
    
    //[SerializeField] private RoomManager roomManager;
    
    private float vignetteDuration = 0.2f;
    private bool vignetteAnimationFinished=false;
    
    private Composite composite;
    private bool inCamera = false;
    //private float captureRegionSize=0;
    private float targetHeight = 0;
    private float targetWidth = 0;
    
    public event Action onSnapshot;
    public event Action onProject;

    void Awake()
    {
        composite = FindObjectOfType<Composite>();
        topVignetteColor =  topVignette.GetComponent<UnityEngine.UI.Image>().color;
        bottomVignetteColor =  bottomVignette.GetComponent<UnityEngine.UI.Image>().color;
        leftVignetteColor =  leftVignette.GetComponent<UnityEngine.UI.Image>().color;
        rightVignetteColor =  rightVignette.GetComponent<UnityEngine.UI.Image>().color;

        topVignetteColor = vignetteColor;
        bottomVignetteColor = vignetteColor;
        leftVignetteColor = vignetteColor;
        rightVignetteColor = vignetteColor;
        
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        onSnapshot += FlashVignette;
        onProject += PrintVignette;
    }

    // Update is called once per frame
    void Update()
    {
        float captureRegionSize = composite.CaptureSize * Mathf.Min(Screen.width, Screen.height);
        targetHeight = (Screen.height - captureRegionSize) * 0.5f;
        targetWidth  = (Screen.width  - captureRegionSize) * 0.5f;
        
        if (Input.GetMouseButtonDown(1)) {
            StartCoroutine(AnimateIn());
            inCamera = true;
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            StartCoroutine(AnimateOut());
            inCamera = false;
        }

        if (inCamera)
        {
            if (Input.GetMouseButtonDown(0))
            {
                onSnapshot?.Invoke();
            }
            
            if (Input.GetMouseButtonDown(2))
            {
                onProject?.Invoke();
            }

            if (vignetteAnimationFinished)
            {
                //adjusts vignette size dynamically to capture size
                topVignette.sizeDelta    = new Vector2(0, targetHeight);
                bottomVignette.sizeDelta = new Vector2(0, targetHeight);
                leftVignette.sizeDelta   = new Vector2(targetWidth, 0);
                rightVignette.sizeDelta  = new Vector2(targetWidth, 0);
            }
        }
    }

    void FlashVignette()
    {
        StartCoroutine(DoFlashVignette());
    }
    IEnumerator DoFlashVignette()
    {
        float t = 0f;
        while (t < 1f) {
            t += Time.deltaTime / vignetteDuration;
            
            //brief white flash on vignettes
            topVignette.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Color.white, vignetteColor, t);
            bottomVignette.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Color.white, vignetteColor, t);
            leftVignette.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Color.white, vignetteColor, t);
            rightVignette.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Color.white, vignetteColor, t);
            
            // topVignetteColor = Color.Lerp(Color.white, topVignetteColor, t);
            // bottomVignetteColor = Color.Lerp(Color.white, bottomVignetteColor, t);
            // leftVignetteColor = Color.Lerp(Color.white, leftVignetteColor, t);
            // rightVignetteColor = Color.Lerp(Color.white, rightVignetteColor, t);
            
            yield return null;
        } 
    }
    
    void PrintVignette()
    {
        StartCoroutine(DoPrintVignette());
    }
    IEnumerator DoPrintVignette()
    {
        float t = 0f;
        while (t < 1f) {
            t += Time.deltaTime / vignetteDuration;
            
            //brief white flash on vignettes
            topVignette.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Color.cyan, vignetteColor, t);
            bottomVignette.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Color.cyan, vignetteColor, t);
            leftVignette.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Color.cyan, vignetteColor, t);
            rightVignette.GetComponent<UnityEngine.UI.Image>().color = Color.Lerp(Color.cyan, vignetteColor, t);
            
            // topVignetteColor = Color.Lerp(Color.white, topVignetteColor, t);
            // bottomVignetteColor = Color.Lerp(Color.white, bottomVignetteColor, t);
            // leftVignetteColor = Color.Lerp(Color.white, leftVignetteColor, t);
            // rightVignetteColor = Color.Lerp(Color.white, rightVignetteColor, t);
            
            yield return null;
        } 
    }
    
    IEnumerator AnimateIn()
    {
        //convert to non-normalized screen-space coord
        
        float t = 0f;
        vignetteAnimationFinished = false;

        while (t < 1f) {
            t += Time.deltaTime / vignetteDuration;
            float ease = Mathf.SmoothStep(0f, 1f, t);

            topVignette.sizeDelta    = new Vector2(0, Mathf.Lerp(0, targetHeight, ease));
            bottomVignette.sizeDelta = new Vector2(0, Mathf.Lerp(0, targetHeight, ease));
            leftVignette.sizeDelta   = new Vector2(Mathf.Lerp(0, targetWidth, ease), 0);
            rightVignette.sizeDelta  = new Vector2(Mathf.Lerp(0, targetWidth, ease), 0);

            yield return null;
        }
        
        vignetteAnimationFinished = true;
    }
    
    IEnumerator AnimateOut()
    {
        float t = 0f;
        vignetteAnimationFinished = false;

        while (t < 1f) {
            t += Time.deltaTime / vignetteDuration;
            float ease = Mathf.SmoothStep(0f, 1f, t);

            topVignette.sizeDelta    = new Vector2(0, Mathf.Lerp(topVignette.sizeDelta.y, 0, ease));
            bottomVignette.sizeDelta = new Vector2(0, Mathf.Lerp(bottomVignette.sizeDelta.y, 0, ease));
            leftVignette.sizeDelta   = new Vector2(Mathf.Lerp(leftVignette.sizeDelta.x, 0, ease), 0);
            rightVignette.sizeDelta  = new Vector2(Mathf.Lerp(rightVignette.sizeDelta.x, 0, ease), 0);

            yield return null;
        }
        
        vignetteAnimationFinished = true;
    }
}
