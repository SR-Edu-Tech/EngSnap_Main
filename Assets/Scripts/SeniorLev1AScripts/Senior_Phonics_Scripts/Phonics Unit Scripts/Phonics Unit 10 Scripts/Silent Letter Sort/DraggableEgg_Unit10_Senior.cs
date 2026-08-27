using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableEgg_Unit10_Senior : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private EggGameSilentLetter_Unit10_Senior manager;
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool wasDragged = false;

    public void Setup(EggGameSilentLetter_Unit10_Senior m)
    {
        manager = m;
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (manager == null || !manager.CanDragEgg()) return;
        wasDragged = true;
        manager.OnEggDragStart(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (manager == null || !manager.CanDragEgg()) return;

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (canvas != null && rectTransform != null)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }

        manager.OnEggDragHover(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (manager == null || !manager.CanDragEgg()) return;

        manager.OnEggDragEnd(eventData.position);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!wasDragged && manager != null)
        {
            manager.PlayCurrentWordAudio();
        }
        wasDragged = false;
    }
}
