using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuildSentenceTile_P_Senior : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Settings")]
    [SerializeField] private float dragScaleFactor = 1.1f;
    [SerializeField] private float returnDuration = 0.3f;

    [Header("UI Components")]
    public TextMeshProUGUI wordTextLabel;
    public Image bgImage;

    [HideInInspector] public int correctSlotIndex;
    [HideInInspector] public string wordValue;
    [HideInInspector] public Vector3 startWorldPosition;

    private BuildSentence_P_Senior _manager;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Vector3 _originalScale;
    private Vector2 _pointerOffset;
    private bool _isDragging = false;
    private bool _isPlaced = false;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_rectTransform == null)
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            if (_rectTransform != null)
            {
                _originalScale = _rectTransform.localScale;
            }
        }
        if (wordTextLabel == null)
        {
            wordTextLabel = GetComponentInChildren<TextMeshProUGUI>();
        }
        if (bgImage == null)
        {
            bgImage = GetComponent<Image>();
            if (bgImage == null) bgImage = GetComponentInChildren<Image>();
        }
    }

    public void Setup(BuildSentence_P_Senior manager, int correctIdx, string word)
    {
        _manager = manager;
        correctSlotIndex = correctIdx;
        wordValue = word;
        _isPlaced = false;
        _isDragging = false;

        EnsureInitialized();

        if (wordTextLabel != null)
        {
            wordTextLabel.text = word;
        }

        if (_rectTransform != null)
        {
            _rectTransform.localScale = _originalScale;
        }
    }

    public void SetPlaced(bool placed)
    {
        _isPlaced = placed;
    }

    public bool IsPlaced()
    {
        return _isPlaced;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isPlaced || _manager == null || !_manager.CanPlay()) return;

        _isDragging = true;
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, _originalScale * dragScaleFactor, 0.15f).setEase(LeanTweenType.easeOutQuad);

        // Calculate offset relative to parent RectTransform in local space
        if (transform.parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPos))
        {
            _pointerOffset = _rectTransform.anchoredPosition - localPointerPos;
        }
        else
        {
            _pointerOffset = Vector2.zero;
        }

        // Bring to front
        transform.SetAsLastSibling();

        _manager.OnTileDragBegin(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isPlaced || !_isDragging || transform.parent == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            _rectTransform.anchoredPosition = localPoint + _pointerOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_isPlaced || !_isDragging) return;

        _isDragging = false;

        if (_manager != null)
        {
            _manager.OnTileDropped(this);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Only trigger click (tap to replay) if the tile is placed and wasn't just dragged
        if (_isPlaced && !_isDragging && _manager != null)
        {
            _manager.OnTileTapped(this);
        }
    }

    public void AnimateBackToStart()
    {
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, _originalScale, returnDuration).setEase(LeanTweenType.easeOutQuad);
        LeanTween.value(gameObject, transform.position, startWorldPosition, returnDuration)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((Vector3 val) => {
                transform.position = val;
            });
    }

    public void AnimateBackToStartWithColor(Color wrongCol, Color defaultCol)
    {
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, _originalScale, returnDuration).setEase(LeanTweenType.easeOutQuad);
        
        LeanTween.value(gameObject, transform.position, startWorldPosition, returnDuration)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((Vector3 val) => {
                transform.position = val;
            });

        if (bgImage != null)
        {
            bgImage.color = wrongCol;
            LeanTween.value(gameObject, 0f, 1f, 0.6f)
                .setOnUpdate((float val) => {
                    if (bgImage != null)
                    {
                        bgImage.color = Color.Lerp(wrongCol, defaultCol, val);
                    }
                });
        }
    }

    public void AnimateToTargetWorld(Vector3 targetWorldPos, Action onComplete)
    {
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, _originalScale, returnDuration).setEase(LeanTweenType.easeOutQuad);
        LeanTween.value(gameObject, transform.position, targetWorldPos, returnDuration)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((Vector3 val) => {
                transform.position = val;
            })
            .setOnComplete(() => {
                onComplete?.Invoke();
            });
    }
}
