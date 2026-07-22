using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Album : MonoBehaviour, IDropHandler
{
    [SerializeField] private GameObject snapshotPrefab;
    [SerializeField] private Vector2 snapshotSize = new Vector2(120f, 120f);
    [SerializeField] private RectTransform refRecTransform;
    private RectTransform rectTransform;
    private Collage collage;
    
    [Header("Reference")]
    [SerializeField] private RawImage referenceDisplay;
    [SerializeField] private RawImage rawReference;
    [SerializeField, Range(0f, 1f)] private float referenceAlpha = 0.2f;
    
    [Header("Match Evaluation")]
    [SerializeField] private MatchEvaluator matchEvaluator;
    [SerializeField, Range(0f, 1f)] private float matchThreshold = 0.75f;
    [SerializeField] private float matchingCD = 4f;
    
    [Header("Notes")] 
    [SerializeField] private HandwritingNote progressLine;
    [SerializeField] private string[] progressNotes;

    public static Album Instance { get; private set; }

    private float timer=0f;
    
    

    void Awake() {
        Instance = this;
        
        rectTransform = GetComponent<RectTransform>();
        collage = GetComponentInParent(typeof(Collage)) as Collage;
        SetReference();
    }
    
    public void EvaluateMatch()
    {
        //reevalute reference match on set interval
        // timer  += Time.deltaTime;
        // if (collage.isOpen && timer > matchingCD)
        // {
            StartCoroutine(matchEvaluator.Evaluate(refRecTransform, (score, passed) => {
                Debug.Log($"Score: {score:F2} : {(passed ? "matched" : "not yet")}");
                UpdateProgress(score);
            }));
        // }
    }

    private void UpdateProgress(float score)
    {
        int noteIndex = Mathf.Min(
            (int)(score * progressNotes.Length),
            progressNotes.Length - 1
        );
        progressLine.SetNote(progressNotes[noteIndex]);
    }

    public void OnDrop(PointerEventData e)
    {
        Snapshot source = e.pointerDrag?.GetComponent<Snapshot>();
        if (source == null || source.capturedFrames.Count == 0) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, e.position, e.pressEventCamera, out Vector2 localPos);
        Vector2 pageHalf = rectTransform.rect.size * 0.5f;

        bool alreadyOnAlbum = source.page == rectTransform;

        source.NotifyDropHandled();

        if (alreadyOnAlbum) {
            // just reposition within album
            Vector2 half = source.GetComponent<RectTransform>().sizeDelta * 0.5f;
            localPos.x = Mathf.Clamp(localPos.x, -pageHalf.x + half.x, pageHalf.x - half.x);
            localPos.y = Mathf.Clamp(localPos.y, -pageHalf.y + half.y, pageHalf.y - half.y);
            source.transform.SetParent(rectTransform);
            source.GetComponent<RectTransform>().localPosition = localPos;
        } else {
            // coming from contact strip, spawning new snapshot on album
            GameObject obj = Instantiate(snapshotPrefab, rectTransform);
            Snapshot copy  = obj.GetComponent<Snapshot>();
            copy.page      = rectTransform;
            copy.rightPage = rectTransform;
            copy.shotProjector = source.shotProjector;
            copy.collage   = source.collage;
            copy.Init(source.decalBaseMaterial);

            copy.capturedFrames = new List<UnityEngine.RenderTexture>(source.capturedFrames);
            if (copy.capturedFrames.Count > 0)
                copy.SetDefault(copy.capturedFrames[0]);

            Vector2 half = snapshotSize * 0.5f;
            localPos.x = Mathf.Clamp(localPos.x, -pageHalf.x + half.x, pageHalf.x - half.x);
            localPos.y = Mathf.Clamp(localPos.y, -pageHalf.y + half.y, pageHalf.y - half.y);
            obj.GetComponent<RectTransform>().sizeDelta     = snapshotSize;
            obj.GetComponent<RectTransform>().localPosition = localPos;

            source.Clear();
        }
    }

    public void SetReference()
    {
        if (referenceDisplay == null) return;
        referenceDisplay.texture = referenceDisplay.mainTexture;
        referenceDisplay.color   = new Color(1, 1, 1, referenceAlpha);
        
        matchEvaluator.UpdateReference(rawReference.mainTexture);
    }
}
