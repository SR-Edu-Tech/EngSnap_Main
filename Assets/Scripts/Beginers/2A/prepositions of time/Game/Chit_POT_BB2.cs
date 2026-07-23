using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single draggable time-phrase chit.
/// (Add Component → search "Chit_POT_BB2" — drag-and-drop of this file
/// only auto-attaches the file's primary class.)
/// Assumes a Screen Space - Overlay OR Screen Space - Camera Canvas (both
/// handled automatically via the camera-aware drag conversion below).
/// </summary>
public class Chit_POT_BB2 : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Refs")]
    public TMP_Text label;
    public Image    icon;
    public Image    background;

    public ChitData_POT_BB2 Data     { get; private set; }
    public bool              IsPlaced { get; private set; }

    private RectTransform _rect;
    private CanvasGroup   _canvasGroup;
    private Transform     _trayParent;
    private Vector2       _trayAnchoredPos;
    private Canvas        _canvas;
    private RectTransform _canvasRect;

    public void Initialise(ChitData_POT_BB2 data, Color bgColor, System.Action<Chit_POT_BB2> OnChitTapped)
    {
        Data     = data;
        IsPlaced = false;

        if (label != null)      label.text = data.phraseText;
        if (icon != null)       icon.sprite = data.chitIcon;
        if (background != null) background.color = bgColor;

        _rect        = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvas     = GetComponentInParent<Canvas>();
        _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
    }

    /// Call right after the chit is parented into the tray so a wrong drop
    /// knows where "home" is.
    public void CacheTrayPosition()
    {
        _trayParent      = transform.parent;
        _trayAnchoredPos = _rect.anchoredPosition;
    }

    /// Makes any parent Layout Group (Horizontal/Vertical/Grid) skip this
    /// chit when calculating positions — permanently, regardless of which
    /// parent it's later reparented under (tray or basket).
    public void SetIgnoreLayout(bool ignore)
    {
        var le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = ignore;
    }

    public void MarkPlaced() => IsPlaced = true;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsPlaced) return;
        _canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsPlaced) return;
        if (_canvasRect == null) { _rect.position = eventData.position; return; }

        Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvasRect, eventData.position, cam, out Vector3 worldPoint))
            _rect.position = worldPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (IsPlaced) { _canvasGroup.blocksRaycasts = true; return; }

        // Not placed correctly (or dropped nowhere) — snap back to the tray.
        _canvasGroup.blocksRaycasts = true;
        transform.SetParent(_trayParent, true);
        StartCoroutine(SnapBack());
    }

    private IEnumerator SnapBack()
    {
        Vector2 start = _rect.anchoredPosition;
        float e = 0f, dur = 0.25f;
        while (e < dur)
        {
            e += Time.deltaTime;
            _rect.anchoredPosition = Vector2.Lerp(start, _trayAnchoredPos, e / dur);
            yield return null;
        }
        _rect.anchoredPosition = _trayAnchoredPos;
    }
}
