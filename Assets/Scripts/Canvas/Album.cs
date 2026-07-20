using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Album : MonoBehaviour, IDropHandler
{
    [SerializeField] private RawImage referenceDisplay;
    [SerializeField] private RawImage rawReference;
    [SerializeField, Range(0f, 1f)] private float referenceAlpha = 0.2f;
    [SerializeField] private GameObject snapshotPrefab;
    [SerializeField] private Vector2 snapshotSize = new Vector2(120f, 120f);

    [SerializeField] private MatchEvaluator matchEvaluator;
    
    private RectTransform rectTransform;
    
    [SerializeField] private float shiftTick = 4f;

    private float timer=0f;

    void Awake() {
        rectTransform = GetComponent<RectTransform>();
    }
    
    private void LateUpdate()
    {
        timer  += Time.deltaTime;
        if (timer > shiftTick)
        {
            StartCoroutine(matchEvaluator.Evaluate(rectTransform, (score, passed) => {
                Debug.Log($"Score: {score:F2} — {(passed ? "matched" : "not yet")}");
                // drive whatever feedback follows
            }));
        }
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

    public void SetReference(Texture reference)
    {
        if (referenceDisplay == null) return;
        referenceDisplay.texture = reference;
        referenceDisplay.color   = new Color(1, 1, 1, referenceAlpha);
        
        matchEvaluator.UpdateReference(reference);
    }
}
