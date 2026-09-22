using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class ColourYourSpeech_PaintPot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler {
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI idiomTMP;
    [SerializeField] private Image potImage;
    [SerializeField] private Image splatImage;

    [Header("Runtime State")]
    private string idiomText;
    private int correctBucketIndex;
    private bool isResolved = false;
    private bool isDragging = false;
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    public string IdiomText => idiomText;
    public int CorrectBucketIndex => correctBucketIndex;
    public bool IsResolved => isResolved;
    public bool IsDragging => isDragging;

    public event Action<ColourYourSpeech_PaintPot> OnPotDragStarted;
    public event Action<ColourYourSpeech_PaintPot, Vector2> OnPotDragged;
    public event Action<ColourYourSpeech_PaintPot> OnPotDragEnded;
    public event Action<ColourYourSpeech_PaintPot> OnPotClicked;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (idiomTMP == null) idiomTMP = GetComponentInChildren<TextMeshProUGUI>(true);
        if (splatImage != null) splatImage.gameObject.SetActive(false);
    }

    public void Initialize(string idiom, int bucketIndex, Color color) {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        idiomText = idiom;
        correctBucketIndex = bucketIndex;
        isResolved = false;
        isDragging = false;

        if (idiomTMP != null) {
            idiomTMP.text = idiom;
        }

        if (potImage != null) {
            potImage.color = color;
        }

        if (splatImage != null) {
            splatImage.gameObject.SetActive(false);
        }

        transform.localScale = Vector3.one;
    }

    public void MoveDown(float deltaY) {
        if (isResolved || isDragging) return;
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null) {
            rectTransform.anchoredPosition += Vector2.down * deltaY;
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (isResolved) return;
        OnPotClicked?.Invoke(this);
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (isResolved) return;
        isDragging = true;
        transform.SetAsLastSibling();
        transform.DOScale(Vector3.one * 1.15f, 0.15f);
        OnPotDragStarted?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData) {
        if (isResolved || !isDragging) return;

        if (rectTransform != null && parentCanvas != null) {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint)) {
                rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
        }
        OnPotDragged?.Invoke(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (isResolved || !isDragging) return;
        isDragging = false;
        transform.DOScale(Vector3.one, 0.15f);
        OnPotDragEnded?.Invoke(this);
    }

    /// <summary>
    /// Mark pot as resolved. Prevents double-resolution, scoring, or life-loss.
    /// </summary>
    public bool MarkResolved() {
        if (isResolved) return false;
        isResolved = true;
        isDragging = false;
        return true;
    }

    /// <summary>
    /// Immediately deactivates and destroys the entire root GameObject.
    /// Guarantees that all child elements (Border, TMP, Image, Colliders) disappear synchronously.
    /// </summary>
    public void DisappearImmediate() {
        MarkResolved();
        isDragging = false;
        DOTween.Kill(transform);
        if (gameObject != null) {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Plays correct splat placement cleanup. Immediately deactivates root pot and destroys it.
    /// </summary>
    public void AnimateCorrectSplat(Vector3 targetBucketPos, Action onComplete) {
        DisappearImmediate();
        onComplete?.Invoke();
    }

    /// <summary>
    /// Plays wrong placement cleanup. Immediately deactivates root pot and destroys it.
    /// </summary>
    public void AnimateWrongFeedback(Action onComplete) {
        DisappearImmediate();
        onComplete?.Invoke();
    }

    /// <summary>
    /// Plays miss placement cleanup. Immediately deactivates root pot and destroys it.
    /// </summary>
    public void AnimateMissedFeedback(Action onComplete) {
        DisappearImmediate();
        onComplete?.Invoke();
    }

    private void OnDestroy() {
        DOTween.Kill(transform);
        if (splatImage != null) DOTween.Kill(splatImage.transform);
    }
}
