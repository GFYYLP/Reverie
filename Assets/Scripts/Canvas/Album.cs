using UnityEngine;
using UnityEngine.EventSystems;

public class Album : MonoBehaviour, IDropHandler
{
    [SerializeField] private UnityEngine.UI.RawImage referenceDisplay;
    [SerializeField, Range(0f, 1f)] private float referenceAlpha = 0.2f;
    
    public void OnDrop(PointerEventData e)
    {
        Snapshot source = e.pointerDrag?.GetComponent<Snapshot>();
        if (source == null || source.capturedFrames.Count == 0) return;
        source.PlaceOnRightPage(e.position);
    }
    
    public void SetReference(Texture reference)
    {
        if (referenceDisplay == null) return;
        referenceDisplay.texture = reference;
        referenceDisplay.color   = new Color(1, 1, 1, referenceAlpha);
    }

}
