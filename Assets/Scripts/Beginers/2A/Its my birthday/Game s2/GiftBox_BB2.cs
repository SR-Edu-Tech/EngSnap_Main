using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
[RequireComponent(typeof(CanvasGroup))]
public class GiftBox_BB2 : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Timing")]
    [SerializeField] private float dockMoveDuration = 0.25f;
    [SerializeField] private float returnMoveDuration = 0.3f;

    private RectTransform _rect;
    private RectTransform _parentRect;
    private CanvasGroup _canvasGroup;

    private Vector3 _preDragWorldPos;
    private bool _wasDockedThisDrag;
    private Coroutine _moveCo;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _parentRect = transform.parent as RectTransform;
    }

    public void ResetToHome(RectTransform homeAnchor)
    {
        StopMove();
        gameObject.SetActive(true);
        if (homeAnchor != null)
            _rect.position = homeAnchor.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        StopMove();
        _preDragWorldPos = _rect.position;
        _wasDockedThisDrag = false;
        _canvasGroup.blocksRaycasts = false;

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
    }

    public void OnDrag(PointerEventData eventData)
    {
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
        _canvasGroup.blocksRaycasts = true;
        StartCoroutine(CheckDockedNextFrame());
    }

    /// Called by WishCircleGame_BB2 (in response to FriendSlot_BB2.OnDrop)
    /// when the gift box has been dropped on a valid friend.
    public void MarkDocked(RectTransform anchor)
    {
        if (anchor == null) return;
        _wasDockedThisDrag = true;
        StopMove();
        _moveCo = StartCoroutine(MoveTo(anchor.position, dockMoveDuration));
    }

    private IEnumerator CheckDockedNextFrame()
    {
        yield return null; // let OnDrop (if any) run first
        if (!_wasDockedThisDrag)
            _moveCo = StartCoroutine(MoveTo(_preDragWorldPos, returnMoveDuration));
    }

    private IEnumerator MoveTo(Vector3 targetWorldPos, float duration)
    {
        Vector3 start = _rect.position;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _rect.position = Vector3.Lerp(start, targetWorldPos, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        _rect.position = targetWorldPos;
    }

    private void StopMove()
    {
        if (_moveCo != null) StopCoroutine(_moveCo);
        _moveCo = null;
    }
}
