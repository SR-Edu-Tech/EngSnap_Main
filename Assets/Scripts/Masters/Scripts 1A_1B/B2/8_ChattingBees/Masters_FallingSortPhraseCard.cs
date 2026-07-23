using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class Masters_FallingSortPhraseCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [SerializeField] private TextMeshProUGUI expressionTMP;
    
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool isDragging;
    
    public System.Action<Masters_FallingSortPhraseCard> OnDragEnded;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void SetExpression(string expression) {
        if (expressionTMP != null) {
            expressionTMP.text = expression;
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData) {
        if (!isDragging || canvas == null) return;
        
        // Move only horizontally based on finger swipe/drag
        rectTransform.anchoredPosition += new Vector2(eventData.delta.x / canvas.scaleFactor, 0);
    }

    public void OnEndDrag(PointerEventData eventData) {
        isDragging = false;
        OnDragEnded?.Invoke(this); 
    }
    
    public bool IsDragging() {
        return isDragging;
    }
}
