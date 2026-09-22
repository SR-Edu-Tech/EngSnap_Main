using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class Masters_3A_FallingSortPhraseCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [SerializeField] private TextMeshProUGUI expressionTMP;

    public event System.Action<Masters_3A_FallingSortPhraseCard> OnDragEnded;

    public void SetExpression(string expr) {
        if (expressionTMP == null) expressionTMP = GetComponentInChildren<TextMeshProUGUI>(true);
        if (expressionTMP != null) expressionTMP.text = expr;
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData) {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent, eventData.position, eventData.pressEventCamera, out Vector2 localPoint)) {
            ((RectTransform)transform).localPosition = new Vector3(localPoint.x, ((RectTransform)transform).localPosition.y, 0);
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        OnDragEnded?.Invoke(this);
    }
}
