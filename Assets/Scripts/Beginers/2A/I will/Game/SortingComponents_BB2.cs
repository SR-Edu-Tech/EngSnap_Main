using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  Chit_BB2 — one draggable picture chit in the tray.
//
//  Dropped on the correct bin → LockIn() shrinks/greys it in place and
//  disables further dragging. Dropped on the wrong bin, or anywhere that
//  isn't a bin → PlayWrongBounce() glides it back to where the drag
//  started. No separate "wrong" signal needed for the "dropped on
//  nothing" case — the return motion IS the feedback.
//
//  IMPORTANT: needs an Image with "Raycast Target" enabled so it can
//  receive the pointer-down that starts the drag.
// ─────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(CanvasGroup))]
public class Chit_BB2 : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image chitImage;
    [SerializeField] private TMP_Text label;

    [Header("Colours")]
    [SerializeField] private Color lockedColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);

    [Header("Timing")]
    [SerializeField] private float bounceDuration = 0.3f;
    [SerializeField] private float lockMoveDuration = 0.25f;
    [SerializeField] private float wobbleAmount = 10f;

    public int ChitIndex { get; private set; }
    public ChitData_BB2 Data { get; private set; }
    public bool IsLocked { get; private set; }

    private RectTransform _rect;
    private RectTransform _parentRect;
    private CanvasGroup _canvasGroup;

    private Vector3 _preDragWorldPos;
    private bool _wasResolvedThisDrag;
    private Coroutine _moveCo;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _parentRect = transform.parent as RectTransform;
    }

    public void Initialise(int index, ChitData_BB2 data)
    {
        ChitIndex = index;
        Data = data;
        IsLocked = false;
        _wasResolvedThisDrag = false;

        if (chitImage != null)
        {
            chitImage.sprite = data.chitSprite;
            chitImage.color = Color.white;
        }
        if (label != null) label.text = data.chitLabel;

        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;
    }

    // ── Drag handling ────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsLocked) return;

        StopMove();
        _preDragWorldPos = _rect.position;
        _wasResolvedThisDrag = false;
        _canvasGroup.blocksRaycasts = false;

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsLocked) return;

        if (_parentRect == null)
        {
            _rect.position = eventData.position;
            return;
        }

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                _parentRect, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
        {
            _rect.position = worldPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsLocked) return;

        _canvasGroup.blocksRaycasts = true;
        StartCoroutine(CheckResolvedNextFrame());
    }

    /// Called by BinDropZone_BB2 the instant OnDrop fires — before the
    /// controller decides correct/wrong — so the "dropped on nothing"
    /// fallback below never double-fires on top of it.
    public void MarkResolved() => _wasResolvedThisDrag = true;

    private IEnumerator CheckResolvedNextFrame()
    {
        yield return null; // let OnDrop (if any) run first
        if (!_wasResolvedThisDrag)
            PlayWrongBounce();
    }

    // ── Called by SortingGame_BB2 ───────────────────────────────────────

    public void PlayWrongBounce()
    {
        StopMove();
        _moveCo = StartCoroutine(WrongBounceCoroutine());
    }

    /// <param name="binContentParent">
    /// The bin's own RectTransform — the one carrying the Grid Layout Group
    /// (e.g. "I will slot"). The chit is reparented into it so the grid
    /// auto-arranges every correctly-sorted chit into its own cell instead
    /// of stacking them all on one point.
    /// </param>
    public void LockIn(RectTransform binContentParent)
    {
        IsLocked = true;
        _canvasGroup.blocksRaycasts = false;
        StopMove();
      //  _moveCo = StartCoroutine(LockInCoroutine(binContentParent));
    }

    // ── Animation ────────────────────────────────────────────────────────

    private IEnumerator WrongBounceCoroutine()
    {
        Vector3 start = _rect.position;
        Vector3 target = _preDragWorldPos;
        float t = 0f;
        while (t < bounceDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / bounceDuration);
            float wobble = Mathf.Sin(p * Mathf.PI * 4f) * wobbleAmount * (1f - p);
            _rect.position = Vector3.Lerp(start, target, p) + new Vector3(wobble, 0f, 0f);
            yield return null;
        }
        _rect.position = target;
        _canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator LockInCoroutine(Vector3 targetWorldPos)
    {
        Vector3 startPos = _rect.position;
        Vector3 startScale = _rect.localScale;
        Vector3 endScale = startScale * 0.7f;
        float t = 0f;
        while (t < lockMoveDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / lockMoveDuration);
            _rect.position = Vector3.Lerp(startPos, targetWorldPos, p);
            _rect.localScale = Vector3.Lerp(startScale, endScale, p);
            yield return null;
        }
        _rect.position = targetWorldPos;
        _rect.localScale = endScale;

        if (chitImage != null) chitImage.color = lockedColor;
    }

    private void StopMove()
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = null;
    }
}

