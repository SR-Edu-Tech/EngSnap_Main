using UnityEngine;
using UnityEngine.EventSystems;

public class SC_DraggableLetter_Phonics_Juniors : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private Vector2 startPosition;
    private bool isDroppedOnTarget = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (rectTransform != null)
        {
            startPosition = rectTransform.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        ResetPosition();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (rectTransform != null && !isDroppedOnTarget)
        {
            startPosition = rectTransform.anchoredPosition;
        }

        isDroppedOnTarget = false;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (canvas != null && rectTransform != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        if (isDroppedOnTarget)
            return;

        // Check if pointer is directly over any active SC_BasketDropZone_Phonics_Junior
        SC_BasketDropZone_Phonics_Junior[] baskets = FindObjectsByType<SC_BasketDropZone_Phonics_Junior>(FindObjectsSortMode.None);
        SC_BasketDropZone_Phonics_Junior targetBasket = null;

        Camera eventCamera = eventData != null ? eventData.pressEventCamera : null;

        foreach (var basket in baskets)
        {
            if (basket == null || !basket.gameObject.activeInHierarchy) continue;

            RectTransform basketRect = basket.GetComponent<RectTransform>();
            if (basketRect != null && eventData != null)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(basketRect, eventData.position, eventCamera))
                {
                    targetBasket = basket;
                    break;
                }
            }
        }

        if (targetBasket != null)
        {
            targetBasket.OnDrop(eventData);
            return;
        }

        // If not dropped on any basket, return to original position
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPosition;
        }
    }

    public void SnapTo(Transform target)
    {
        isDroppedOnTarget = true;
        if (target != null)
        {
            transform.position = target.position;
        }
    }

    public void ResetPosition()
    {
        isDroppedOnTarget = false;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = startPosition;
        }
    }
}