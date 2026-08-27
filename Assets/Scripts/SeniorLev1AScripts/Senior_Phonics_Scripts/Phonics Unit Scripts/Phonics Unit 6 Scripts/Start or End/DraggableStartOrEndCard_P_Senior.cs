using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableStartOrEndCard_P_Senior : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private StartOrEnd_P_Senior manager;
    private RectTransform rectTransform;
    private Canvas canvas;

    public void Setup(StartOrEnd_P_Senior m)
    {
        manager = m;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (manager == null || !manager.CanDragCard()) return;

        manager.OnCardDragStart(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (manager == null || !manager.CanDragCard()) return;

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (canvas != null && rectTransform != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        manager.OnCardDragHover(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (manager == null || !manager.CanDragCard()) return;

        manager.OnCardDragEnd(eventData.position);
    }
}
