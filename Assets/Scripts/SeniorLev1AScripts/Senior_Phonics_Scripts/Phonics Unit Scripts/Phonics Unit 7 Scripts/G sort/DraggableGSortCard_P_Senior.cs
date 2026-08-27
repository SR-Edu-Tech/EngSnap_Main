using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableGSortCard_P_Senior : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private GSort_P_Senior manager;
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool wasDragged = false;

    public void Setup(GSort_P_Senior m)
    {
        manager = m;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (manager == null || !manager.CanDragCard()) return;
        wasDragged = true;
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

        manager.OnCardDragHover(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (manager == null || !manager.CanDragCard()) return;

        manager.OnCardDragEnd(this, eventData.position);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Only trigger audio replay if the card was tapped/clicked without drag movement
        if (!wasDragged && manager != null)
        {
            manager.PlayCurrentWordAudio();
        }
        wasDragged = false; // Reset
    }
}
