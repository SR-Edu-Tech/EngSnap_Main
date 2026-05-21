using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// DragCard_WhoAmI — attach to the drag card prefab root.
///
/// On drop into a valid zone:
///   1. Card snaps to zone centre.
///   2. Brief pause so the student sees it land.
///   3. Card fades out and destroys itself.
///   4. Callback fires to the controller.
///
/// PREFAB ROOT needs:
///   • This component
///   • CanvasGroup  (for alpha fade on drop)
///   • Image        (card visual)
///   • TMP_Text child (item label)
/// </summary>
public class DragCard_WhoAmI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _likeZone;
    private RectTransform _dislikeZone;
    private Action<GameObject, bool> _onDropped;

    private RectTransform _rt;
    private Canvas        _canvas;
    private CanvasGroup   _cg;
    private Vector2       _startPos;
    private bool          _dropped;   // prevent double-fire

    public void Init(RectTransform likeZone, RectTransform dislikeZone,
                     Action<GameObject, bool> onDropped)
    {
        _likeZone    = likeZone;
        _dislikeZone = dislikeZone;
        _onDropped   = onDropped;

        _rt     = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _cg     = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();

        _startPos = _rt.anchoredPosition;
        _dropped  = false;
    }

    public void OnBeginDrag(PointerEventData e)
    {
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData e)
    {
        _rt.anchoredPosition += e.delta / _canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_dropped) return;

        _cg.blocksRaycasts = true;

        bool inLike    = RectOverlaps(_rt, _likeZone);
        bool inDislike = RectOverlaps(_rt, _dislikeZone);

        if (inLike || inDislike)
        {
            _dropped = true;
            bool liked = inLike;

            // Snap to zone centre
            RectTransform zone = liked ? _likeZone : _dislikeZone;
            _rt.position = zone.position;

            // Fire callback immediately so controller can update mascot/audio
            _onDropped?.Invoke(gameObject, liked);

            // Fade and self-destroy — card stays visible briefly so student sees the snap
            StartCoroutine(FadeAndDestroy());
        }
        else
        {
            // Missed both zones — snap back so student can try again
            _rt.anchoredPosition = _startPos;
        }
    }

    System.Collections.IEnumerator FadeAndDestroy()
    {
        // Hold in place for a moment so the snap feels satisfying
        yield return new WaitForSeconds(0.35f);

        // Fade out
        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.deltaTime;
            _cg.alpha = Mathf.Lerp(1f, 0f, t / dur);
            yield return null;
        }

        Destroy(gameObject);
    }

    // ── Overlap helper ────────────────────────────────────────────────────
    bool RectOverlaps(RectTransform a, RectTransform b)
    {
        return GetWorldRect(a).Overlaps(GetWorldRect(b));
    }

    Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return new Rect(corners[0].x, corners[0].y,
                        corners[2].x - corners[0].x,
                        corners[2].y - corners[0].y);
    }
}