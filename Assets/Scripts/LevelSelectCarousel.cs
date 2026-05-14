using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LevelSelectCarousel : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("References")]
    public ScrollRect      scrollRect;
    public RectTransform[] buttonItems;
    public Image[]         bgImages;

    [Header("Scale")]
    public float focusedScale   = 1.15f;
    public float unfocusedScale = 0.80f;
    public float maxDistancePx  = 500f;
    public float scaleSpeed     = 12f;

    [Header("Background Fade")]
    public float bgFadeSpeed = 5f;

    [Header("Content Panel")]
    public GameObject contentPanel;

    [Header("Snap (manual drag)")]
    public bool  snapOnRelease = true;
    public float snapSpeed     = 10f;

    [Header("Auto-scroll")]
    public float autoScrollSpeed = 8f;

    // Index: 0=Beginners  1=Juniors  2=Seniors  3=Masters
    public static readonly float[] ContentXForIndex = { 0f, -1250f, -2500f, -3750f };

    private int   _centreIndex   = 0;
    private bool  _isSnapping    = false;
    private float _snapTargetNorm = 0f;

    // Auto-scroll state
    private bool  _autoScrolling    = false;
    private float _autoScrollTarget = 0f;   // content localPosition.x target
    private float autoVelocity;
    // ─────────────────────────────────────────────────────────────────────────
    private void Start()
    {
        for (int i = 0; i < bgImages.Length; i++)
            SetAlpha(bgImages[i], i == 0 ? 1f : 0f);
        ApplyScales(instant: true);
    }

    private void Update()
    {
        _centreIndex = GetCentreIndex();
        ApplyScales(instant: false);
        FadeBackgrounds();

        if (_autoScrolling)
        {
            DoAutoScroll();
        }
        else if (_isSnapping)
        {
            DoSnap();
        }
    }

private void OnEnable()
{
    _autoScrolling = false;
    _isSnapping = false;

    Canvas.ForceUpdateCanvases();

    // Keep current scroll position
    SyncScrollRect();

    scrollRect.enabled = true;
}
    // ─────────────────────────────────────────────────────────────────────────
    //  Called by GameAuthManager after API sets lock states
    // ─────────────────────────────────────────────────────────────────────────

public void ScrollToIndex(int index)
{
    index = Mathf.Clamp(index,0,
             ContentXForIndex.Length-1);

    _autoScrollTarget = ContentXForIndex[index];

    scrollRect.StopMovement();
    scrollRect.enabled = false;

    autoVelocity = 0;

    _isSnapping = false;
    _autoScrolling = true;
}
    // ─────────────────────────────────────────────────────────────────────────
    //  Auto-scroll: drives localPosition directly, ScrollRect disabled
    // ─────────────────────────────────────────────────────────────────────────

private void DoAutoScroll()
{
    float cur = contentPanel.transform.localPosition.x;

    float next = Mathf.SmoothDamp(
        cur,
        _autoScrollTarget,
        ref autoVelocity,
        0.25f,      // smaller = faster
        5000f,      // max speed
        Time.deltaTime
    );

    SetContentX(next);

    if (Mathf.Abs(next - _autoScrollTarget) < 1f)
    {
        SetContentX(_autoScrollTarget);

        autoVelocity = 0;
        _autoScrolling = false;

        SyncScrollRect();
        scrollRect.enabled = true;
    }
}
    // Writes x into content localPosition (ScrollRect is OFF during auto-scroll
    // so this won't be fought over).
    private void SetContentX(float x)
    {
        Vector3 p = contentPanel.transform.localPosition;
        p.x = x;
        contentPanel.transform.localPosition = p;
    }

    // After auto-scroll finishes, push the correct normalised value into the
    // ScrollRect before re-enabling it, so drag/inertia start from the right place.
    private void SyncScrollRect()
    {
        Canvas.ForceUpdateCanvases();
        float total = scrollRect.content.rect.width - scrollRect.viewport.rect.width;
        if (total > 0f)
        {
            float norm = Mathf.Clamp01(-contentPanel.transform.localPosition.x / total);
            // Temporarily enable just to set the value, then it stays enabled.
            scrollRect.enabled = true;
            scrollRect.horizontalNormalizedPosition = norm;
            scrollRect.enabled = false; // will be set true by caller right after
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Drag — player takes control
    // ─────────────────────────────────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Cancel auto-scroll immediately; ScrollRect is already enabled
        // (OnBeginDrag only fires when ScrollRect is interactable).
        _autoScrolling = false;
        _isSnapping    = false;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (snapOnRelease) TriggerSnap();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Manual snap after drag (ScrollRect is ON, use normalised position)
    // ─────────────────────────────────────────────────────────────────────────

    private void TriggerSnap()
    {
        int   closest = GetCentreIndex();
        float total   = scrollRect.content.rect.width - scrollRect.viewport.rect.width;
        if (total <= 0f) return;

        float btnX      = buttonItems[closest].anchoredPosition.x;
        float targetX   = btnX - scrollRect.viewport.rect.width * 0.5f;
        _snapTargetNorm = Mathf.Clamp01(targetX / total);
        _isSnapping     = true;
    }

    private void DoSnap()
    {
        float cur  = scrollRect.horizontalNormalizedPosition;
        float next = Mathf.Lerp(cur, _snapTargetNorm, Time.deltaTime * snapSpeed);
        scrollRect.horizontalNormalizedPosition = next;

        if (Mathf.Abs(next - _snapTargetNorm) < 0.0005f)
        {
            scrollRect.horizontalNormalizedPosition = _snapTargetNorm;
            _isSnapping = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Scale
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyScales(bool instant)
    {
        Vector2 viewCentre = GetViewportCentreWorld();
        for (int i = 0; i < buttonItems.Length; i++)
        {
            float   dist        = Mathf.Abs(buttonItems[i].position.x - viewCentre.x);
            float   t           = Mathf.Clamp01(dist / maxDistancePx);
            float   targetScale = Mathf.Lerp(focusedScale, unfocusedScale, t);
            Vector3 target      = Vector3.one * targetScale;
            buttonItems[i].localScale = instant
                ? target
                : Vector3.Lerp(buttonItems[i].localScale, target, Time.deltaTime * scaleSpeed);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Background fade
    // ─────────────────────────────────────────────────────────────────────────

    private void FadeBackgrounds()
    {
        Vector2 viewCentre = GetViewportCentreWorld();
        float[] weights    = new float[buttonItems.Length];
        float   sum        = 0f;

        for (int i = 0; i < buttonItems.Length; i++)
        {
            float dist = Mathf.Abs(buttonItems[i].position.x - viewCentre.x);
            float t    = Mathf.Clamp01(dist / maxDistancePx);
            weights[i] = 1f - t;
            sum        += weights[i];
        }

        for (int i = 0; i < bgImages.Length; i++)
        {
            float targetAlpha = (sum > 0f) ? weights[i] / sum : 0f;
            Color c           = bgImages[i].color;
            c.a               = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * bgFadeSpeed);
            bgImages[i].color = c;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private int GetCentreIndex()
    {
        Vector2 viewCentre = GetViewportCentreWorld();
        int   closest = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < buttonItems.Length; i++)
        {
            float d = Mathf.Abs(buttonItems[i].position.x - viewCentre.x);
            if (d < minDist) { minDist = d; closest = i; }
        }
        return closest;
    }

    private Vector2 GetViewportCentreWorld()
        => scrollRect.viewport.TransformPoint(scrollRect.viewport.rect.center);

    private static void SetAlpha(Image img, float a)
    {
        Color c = img.color; c.a = a; img.color = c;
    }

    public void SnapToIndex(int index)
    {
        index = Mathf.Clamp(index, 0, buttonItems.Length - 1);
        float total = scrollRect.content.rect.width - scrollRect.viewport.rect.width;
        if (total <= 0f) return;
        float btnX      = buttonItems[index].anchoredPosition.x;
        float targetX   = btnX - scrollRect.viewport.rect.width * 0.5f;
        _snapTargetNorm = Mathf.Clamp01(targetX / total);
        _isSnapping     = true;
    }
}