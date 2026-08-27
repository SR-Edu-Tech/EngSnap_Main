using UnityEngine;
using UnityEngine.EventSystems;

public class AddEMagic_DraggableE : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    [SerializeField] private float dragScaleFactor = 1.1f;
    [SerializeField] private float returnDuration = 0.3f;

    private AddEMagic_Senior manager;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 startAnchoredPosition;
    private Vector3 originalScale;
    private bool isDragging = false;
    private bool interactable = true;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            if (rectTransform != null)
            {
                originalScale = rectTransform.localScale;
                startAnchoredPosition = rectTransform.anchoredPosition;
            }
        }
    }

    public void Setup(AddEMagic_Senior mgr)
    {
        manager = mgr;
        EnsureInitialized();
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
    }

    public void ResetToStart()
    {
        EnsureInitialized();
        isDragging = false;
        interactable = true;
        LeanTween.cancel(gameObject);
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
            rectTransform.anchoredPosition = startAnchoredPosition;
        }
    }

    private Vector2 pointerOffset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!interactable || manager == null || !manager.CanPlay()) return;

        isDragging = true;
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, originalScale * dragScaleFactor, 0.15f).setEase(LeanTweenType.easeOutQuad);
        
        // Calculate offset relative to parent RectTransform
        if (transform.parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPos))
        {
            pointerOffset = rectTransform.anchoredPosition - localPointerPos;
        }
        else
        {
            pointerOffset = Vector2.zero;
        }

        // Bring to front
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!interactable || !isDragging || transform.parent == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint + pointerOffset;
        }

        // Notify manager of current dragging position for real-time hover highlight checking
        if (manager != null)
        {
            manager.OnETileDragged(transform.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!interactable || !isDragging) return;

        isDragging = false;
        
        // Notify manager of drop
        if (manager != null)
        {
            manager.OnETileDropped(transform.position);
        }
    }

    public void AnimateBackToStart()
    {
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, originalScale, returnDuration).setEase(LeanTweenType.easeOutQuad);
        LeanTween.value(gameObject, rectTransform.anchoredPosition, startAnchoredPosition, returnDuration)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((Vector2 val) => {
                rectTransform.anchoredPosition = val;
            });
    }

    public void AnimateToTarget(Vector2 targetAnchoredPos, System.Action onComplete)
    {
        interactable = false;
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, originalScale, returnDuration).setEase(LeanTweenType.easeOutQuad);
        LeanTween.value(gameObject, rectTransform.anchoredPosition, targetAnchoredPos, returnDuration)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((Vector2 val) => {
                rectTransform.anchoredPosition = val;
            })
            .setOnComplete(() => {
                onComplete?.Invoke();
            });
    }
}
