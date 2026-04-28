using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableItems_S1A : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CategorizingPhrases_S1A manager;
    private CategorizingPhrases_S1A.DraggableItem item;

    private Vector2 offset;
    private RectTransform rectTransform;
    private Canvas canvas;

    public void Setup(CategorizingPhrases_S1A m, CategorizingPhrases_S1A.DraggableItem i)
    {
        manager = m;
        item = i;

        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!manager.CanPlay()) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out offset
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!manager.CanPlay()) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            rectTransform.localPosition = localPoint - offset;
        }

        manager.HandleDragHover(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!manager.CanPlay()) return;

        manager.ResetAllPotHighlights();
        manager.HandleDrop(item, eventData.position);
    }
}