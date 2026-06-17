using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  FamilyPortraitCard
///  A draggable portrait representing a relative (e.g. Father, Mother).
///  Handles its own drag movement, tray float animations, and snap behavior.
/// ════════════════════════════════════════════════════════════════════
///
///  PREFAB STRUCTURE:
///  PortraitCard GameObject [RectTransform] [CanvasGroup] [this script]
///    ├─ PortraitImage       Image
///    └─ LabelText           TMP_Text  (optional, e.g. "Father")
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class FamilyPortraitCard : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Relative Identity")]
    [Tooltip("ID to match with the FamilyTreeFrame (e.g., father, mother)")]
    public string relativeId;
    
    [Header("Visuals Wiring")]
    public Image portraitImage;
    public TMP_Text labelText;
    
    [Header("Audio")]
    [Tooltip("Voice-over clip for this relative (e.g., 'He is my father!')")]
    public AudioClip voiceLine;

    [Header("Animation Settings")]
    public float dragScale = 1.12f;
    public float snapDuration = 0.25f;
    public float floatBackDuration = 0.4f;

    // References
    public RectTransform RectTransform { get; private set; }
    public CanvasGroup CanvasGroup { get; private set; }

    private FamilyTreeGameScreen _screen;
    private RectTransform _dragParent; // The full-screen reference area for dragging
    private Transform _originalParent;
    
    private Vector2 _originalTrayPos;
    private Vector2 _dragOffset;
    
    private bool _isDragging = false;
    private bool _isSnapped = false;
    
    // DOTween Animation references
    private Tween _bobTween;
    private Tween _tiltTween;
    private Tween _moveTween;
    private Tween _scaleTween;

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        CanvasGroup = GetComponent<CanvasGroup>();
        _originalParent = transform.parent;
    }

    /// <summary>
    /// Initialises the card's state, positioning, and starts idle animations.
    /// </summary>
    public void Initialise(FamilyTreeGameScreen screen, RectTransform dragParent, Vector2 trayPosition)
    {
        _screen = screen;
        _dragParent = dragParent;
        _originalTrayPos = trayPosition;
        _isSnapped = false;
        _isDragging = false;
        
        // Re-parent to original parent if it was snapped previously
        transform.SetParent(_originalParent, false);
        RectTransform.anchoredPosition = _originalTrayPos;
        RectTransform.localScale = Vector3.one;
        RectTransform.localRotation = Quaternion.identity;
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.alpha = 1f;

        KillAllTweens();

        // Stagger the idle start slightly so cards don't bob in perfect sync
        float delay = Random.Range(0f, 0.5f);
        StartCoroutine(StartBobbingDelayed(delay));
    }

    private IEnumerator StartBobbingDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!_isDragging && !_isSnapped)
        {
            StartIdleBobbing();
        }
    }

    /// <summary>
    /// Starts a gentle floating up/down and rotation oscillation to look inviting and active.
    /// </summary>
    private void StartIdleBobbing()
    {
        _bobTween?.Kill();
        _tiltTween?.Kill();

        float bobRange = Random.Range(8f, 12f);
        float bobTime = Random.Range(1.2f, 1.8f);

        // Sinusoidal bob up and down
        _bobTween = RectTransform.DOAnchorPosY(_originalTrayPos.y + bobRange, bobTime)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        // Soft rotating back and forth
        float tiltAngle = Random.Range(2.5f, 4f);
        float tiltTime = Random.Range(1.5f, 2.2f);
        RectTransform.localRotation = Quaternion.Euler(0f, 0f, -tiltAngle);

        _tiltTween = RectTransform.DOLocalRotate(new Vector3(0f, 0f, tiltAngle), tiltTime)
            .SetEase(Ease.InOutQuad)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopIdleBobbing()
    {
        _bobTween?.Kill();
        _tiltTween?.Kill();
        RectTransform.localRotation = Quaternion.identity;
    }

    // ── Drag & Event System Handlers ─────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isSnapped) return;
        // Bring to front in current parent
        transform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isSnapped) return;

        _isDragging = true;
        StopIdleBobbing();
        KillAllTweens();

        // Disable raycasts so EventSystem can detect the frame under the mouse
        CanvasGroup.blocksRaycasts = false;

        // Temporarily re-parent to the drag parent (main game panel) so it draws above all card frames
        transform.SetParent(_dragParent, true);

        // Get local position of pointer within _dragParent
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _dragParent, eventData.position, eventData.pressEventCamera, out Vector2 pointerLocalPos);
        
        // Calculate offset between card anchor and mouse position
        _dragOffset = RectTransform.anchoredPosition - pointerLocalPos;

        // Visual pickup scale feedback
        _scaleTween = RectTransform.DOScale(Vector3.one * dragScale, 0.1f).SetEase(Ease.OutQuad);
        // Playful slight tilt while carrying
        _tiltTween = RectTransform.DOLocalRotate(new Vector3(0f, 0f, Random.Range(-5f, 5f)), 0.15f);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _isSnapped) return;

        // Map mouse screen position to local position of drag parent
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _dragParent, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

        RectTransform.anchoredPosition = localPos + _dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        CanvasGroup.blocksRaycasts = true;

        // Hand over drop detection to screen script
        _screen.OnPortraitDropped(this);
    }

    // ── Reactions ────────────────────────────────────────────────────

    /// <summary>
    /// Snaps the card into the center of a correct frame.
    /// </summary>
    public void SnapToFrame(FamilyTreeFrame frame)
    {
        _isSnapped = true;
        CanvasGroup.blocksRaycasts = false;
        StopIdleBobbing();
        KillAllTweens();

        // Reparent to the frame's snapping transform
        transform.SetParent(frame.snapParent, true);

        // Lerp to the exact center (0, 0)
        _moveTween = RectTransform.DOAnchorPos(Vector2.zero, snapDuration)
            .SetEase(Ease.OutCubic);

        // Align rotation back to straight
        _tiltTween = RectTransform.DOLocalRotate(Vector3.zero, snapDuration);

        // Scale back to normal card size
        _scaleTween = RectTransform.DOScale(Vector3.one, snapDuration)
            .SetEase(Ease.OutCubic);
    }

    /// <summary>
    /// Returns the card smoothly to its original tray coordinate slot.
    /// </summary>
    public void ReturnToTray()
    {
        _isSnapped = false;
        _isDragging = false;
        KillAllTweens();

        // Re-parent back to the original tray container
        transform.SetParent(_originalParent, true);

        CanvasGroup.blocksRaycasts = true;

        // Move and scale back to original slot
        _moveTween = RectTransform.DOAnchorPos(_originalTrayPos, floatBackDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                if (!_isDragging && !_isSnapped)
                {
                    StartIdleBobbing();
                }
            });

        _tiltTween = RectTransform.DOLocalRotate(Vector3.zero, floatBackDuration);
        _scaleTween = RectTransform.DOScale(Vector3.one, floatBackDuration);
    }

    private void KillAllTweens()
    {
        _bobTween?.Kill();
        _tiltTween?.Kill();
        _moveTween?.Kill();
        _scaleTween?.Kill();
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }
}
