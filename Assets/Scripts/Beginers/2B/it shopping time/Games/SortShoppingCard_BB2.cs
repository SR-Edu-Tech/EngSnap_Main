using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public enum ShoppingCategory_BB2 { Things, Shops, Clothes }

/// <summary>
/// A single draggable sort card (a shopping item, a shop name, or a
/// clothing item).
/// (Add Component → search "SortShoppingCard_BB2" — drag-and-drop of this
/// file only auto-attaches the file's primary class.)
/// Handles Screen Space - Overlay AND Screen Space - Camera canvases.
/// </summary>
public class SortShoppingCard_BB2 : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI Refs")]
    public TMP_Text label;
    public Image    background;

    public string                DisplayText { get; private set; }
    public ShoppingCategory_BB2  Category    { get; private set; }
    public AudioClip             CardAudio   { get; private set; }
    public bool                  IsPlaced    { get; private set; }

    private RectTransform _rect;
    private CanvasGroup   _canvasGroup;
    private Transform     _trayParent;
    private Vector2       _trayAnchoredPos;
    private Canvas        _canvas;
    private RectTransform _canvasRect;

    public void Initialise(string displayText, ShoppingCategory_BB2 category, AudioClip cardAudio, Color bgColor)
    {
        DisplayText = displayText;
        Category    = category;
        CardAudio   = cardAudio;
        IsPlaced    = false;

        if (label != null)      label.text = displayText;
        if (background != null) background.color = bgColor;

        _rect        = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvas     = GetComponentInParent<Canvas>();
        _canvasRect = _canvas != null ? _canvas.transform as RectTransform : null;
    }

    /// Call right after the card is parented into the tray so a wrong drop
    /// knows where "home" is.
    public void CacheTrayPosition()
    {
        _trayParent      = transform.parent;
        _trayAnchoredPos = _rect.anchoredPosition;
    }

    /// Makes any parent Layout Group (Horizontal/Vertical/Grid) skip this
    /// card when calculating positions — permanently, so our own code has
    /// full control of anchoredPosition for dragging/snap-back/placement.
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
