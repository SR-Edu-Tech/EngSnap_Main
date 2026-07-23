using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class Masters_2A_FallingSortPhraseCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    [SerializeField] private TextMeshProUGUI expressionTMP;
    
    private RectTransform rectTransform;
    private Canvas canvas;
    private bool isDragging;
    private AudioClip currentAudio;
    private Vector3 originalSpawnPos;
    
    public System.Action<Masters_2A_FallingSortPhraseCard> OnDragEnded;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        originalSpawnPos = transform.position;
    }

    public void SaveSpawnPosition(Vector3 pos) {
        originalSpawnPos = pos;
    }

    public void SetExpression(string expression) {
        if (expressionTMP != null) {
            expressionTMP.text = expression;
        }
    }

    public void SetupCard(string text, AudioClip audio) {
        SetExpression(text);
        currentAudio = audio;
        if (currentAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentAudio);
        }
    }

    public void AnimateToBin(Vector3 binWorldPos, bool isCorrect, System.Action onComplete) {
        transform.DOMove(binWorldPos, 0.35f).SetEase(Ease.InQuad).OnComplete(() => {
            if (isCorrect) {
                transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => {
                    onComplete?.Invoke();
                });
            } else {
                // If incorrect, shake and return to original spawn point so player can tap again
                transform.DOShakePosition(0.3f, new Vector3(15f, 0, 0)).OnComplete(() => {
                    transform.DOMove(originalSpawnPos, 0.25f).SetEase(Ease.OutQuad).OnComplete(() => {
                        onComplete?.Invoke();
                    });
                });
            }
        });
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
