using UnityEngine;
using UnityEngine.EventSystems;

public class U2_SD_DraggableWordCard_Phonics_Junior : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Vector2 startPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        ResetPosition();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (rectTransform != null) startPosition = rectTransform.anchoredPosition;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false; // Allows drop zone below to detect the card
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rectTransform != null)
        {
            float scale = (canvas != null && canvas.scaleFactor > 0f) ? canvas.scaleFactor : 1f;
            rectTransform.anchoredPosition += eventData.delta / scale;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        // Check if pointer is over a basket drop zone
        bool droppedOnBasket = false;
        if (eventData.pointerEnter != null)
        {
            U2_SD_BasketDropZone_Phonics_Junior dropZone = eventData.pointerEnter.GetComponentInParent<U2_SD_BasketDropZone_Phonics_Junior>();
            if (dropZone != null)
            {
                droppedOnBasket = true;
                dropZone.OnDrop(eventData);
            }
        }

        // If dropped outside any basket, snap smoothly back to center
        if (!droppedOnBasket)
        {
            ResetPosition();
        }
    }

    public void ResetPosition()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }
    }
}