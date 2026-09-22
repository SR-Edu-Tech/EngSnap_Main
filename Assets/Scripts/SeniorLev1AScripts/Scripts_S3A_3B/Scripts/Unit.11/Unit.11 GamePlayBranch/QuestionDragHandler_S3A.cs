using UnityEngine;
using UnityEngine.EventSystems;

public class OptionDragHandler_S3A :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    private RectTransform rectTransform;

    private Canvas canvas;

    private MatchingQuizManager_S3A manager;

    void Awake()
    {
        rectTransform =
            GetComponent<RectTransform>();

        canvas =
            GetComponentInParent<Canvas>();

        manager =
            FindObjectOfType<MatchingQuizManager_S3A>();
    }

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        manager.StartDrag(rectTransform);
    }

    public void OnDrag(
        PointerEventData eventData)
    {
        rectTransform.anchoredPosition +=
            eventData.delta /
            canvas.scaleFactor;
    }

    public void OnEndDrag(
        PointerEventData eventData)
    {
        manager.EndDrag(rectTransform);
    }
}