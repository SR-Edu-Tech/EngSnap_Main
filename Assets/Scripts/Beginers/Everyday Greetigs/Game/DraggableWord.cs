using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class DraggableWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TMP_Text label;
    public DragWordData data;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Vector2 pointerOffset;     // offset in LOCAL canvas space
    private Transform startParent;
    private Vector2 startAnchoredPos;
    private int startSiblingIndex;

    [HideInInspector] public bool isDropped = false;   // locked after a valid drop

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(DragWordData d)
    {
        data = d;
        label.text = d.word;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Prevent re-dragging after a valid drop
        if (isDropped)
        {
            eventData.pointerDrag = null;
            return;
        }

        startParent       = transform.parent;
        startAnchoredPos  = rectTransform.anchoredPosition;
        startSiblingIndex = transform.GetSiblingIndex();

        // Move to root canvas so it renders on top
        transform.SetParent(canvas.rootCanvas.transform, true);

        canvasGroup.blocksRaycasts = false;

        // Calculate pointer offset in canvas local space so the card doesn't jump
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 pointerCanvasPos
        );
        pointerOffset = rectTransform.anchoredPosition - pointerCanvasPos;

        transform.localScale = Vector3.one * 1.1f;
        transform.rotation   = Quaternion.Euler(0, 0, Random.Range(-5f, 5f));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDropped) return;

        // Convert screen point → canvas local point and apply offset
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.rootCanvas.GetComponent<RectTransform>(),
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 canvasPos
        );
        rectTransform.anchoredPosition = canvasPos + pointerOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDropped) return;

        canvasGroup.blocksRaycasts = true;
        transform.localScale = Vector3.one;
        transform.rotation   = Quaternion.identity;

        // If not picked up by a DropContainer → snap back to original spot
        if (transform.parent == canvas.rootCanvas.transform)
        {
            transform.SetParent(startParent, false);
            transform.SetSiblingIndex(startSiblingIndex);
            rectTransform.anchoredPosition = startAnchoredPos;
        }
    }

    // Called by DropContainer once the word lands in a valid slot
    public void LockInPlace()
    {
        isDropped = true;
        canvasGroup.blocksRaycasts = true;  // still visible/raycastable for layout, just not draggable
    }
}