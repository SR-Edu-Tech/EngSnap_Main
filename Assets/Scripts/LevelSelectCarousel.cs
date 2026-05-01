using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// LevelSelectCarousel — Fixed Version
/// 
/// Each button's scale is driven CONTINUOUSLY by its distance from the
/// viewport centre — no index snapping needed for the scale calculation.
/// This means as you drag, the leaving button shrinks and the arriving
/// button grows in real time, frame by frame.
///
/// SETUP
/// ─────
/// 1. Attach this script to your ScrollRect GameObject.
/// 2. ScrollRect → Horizontal scroll, Inertia ON (deceleration ~0.135).
/// 3. Content → Horizontal Layout Group, child force-expand OFF,
///    child alignment: Middle Centre.
/// 4. Assign buttonItems[]  → the 4 button RectTransforms (children of Content).
/// 5. Assign bgImages[]     → 4 full-screen background Images (same order).
/// 6. Assign scrollRect     → the ScrollRect component.
/// </summary>
public class LevelSelectCarousel : MonoBehaviour, IEndDragHandler
{
    [Header("References")]
    public ScrollRect       scrollRect;
    public RectTransform[]  buttonItems;   // 4 buttons
    public Image[]          bgImages;      // 4 full-screen BG images

    [Header("Scale")]
    [Tooltip("Scale when a button is perfectly centred in the viewport")]
    public float focusedScale   = 1.15f;

    [Tooltip("Scale when a button is fully off-screen / at max distance")]
    public float unfocusedScale = 0.80f;

    [Tooltip("How far from centre (in pixels) counts as fully unfocused")]
    public float maxDistancePx  = 500f;

    [Tooltip("Lerp speed for scale smoothing")]
    public float scaleSpeed     = 12f;

    [Header("Background Fade")]
    [Tooltip("Alpha fade speed between backgrounds")]
    public float bgFadeSpeed    = 5f;

    public GameObject contentPanel; // assign the Content GameObject here for external access

    [Header("Snap")]
    public bool  snapOnRelease  = true;
    public float snapSpeed      = 10f;

    // private
    private int   _centreIndex    = 0;
    private bool  _isSnapping     = false;
    private float _snapTargetNorm = 0f;

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

        if (_isSnapping)
            DoSnap();
    }

    void OnEnable()
    {
        contentPanel.transform.localPosition = Vector3.zero; // reset content position when enabled
    }

    // Scale — driven by real-time pixel distance from viewport centre
    private void ApplyScales(bool instant)
    {
        Vector2 viewCentre = GetViewportCentreWorld();

        for (int i = 0; i < buttonItems.Length; i++)
        {
            float dist        = Mathf.Abs(buttonItems[i].position.x - viewCentre.x);
            float t           = Mathf.Clamp01(dist / maxDistancePx);
            float targetScale = Mathf.Lerp(focusedScale, unfocusedScale, t);
            Vector3 target    = Vector3.one * targetScale;

            buttonItems[i].localScale = instant
                ? target
                : Vector3.Lerp(buttonItems[i].localScale, target, Time.deltaTime * scaleSpeed);
        }
    }

    // Background — weighted alpha based on each button's proximity
    private void FadeBackgrounds()
    {
        Vector2 viewCentre = GetViewportCentreWorld();

        float[] weights = new float[buttonItems.Length];
        float   sum     = 0f;

        for (int i = 0; i < buttonItems.Length; i++)
        {
            float dist = Mathf.Abs(buttonItems[i].position.x - viewCentre.x);
            float t    = Mathf.Clamp01(dist / maxDistancePx);
            weights[i] = 1f - t;
            sum       += weights[i];
        }

        for (int i = 0; i < bgImages.Length; i++)
        {
            float targetAlpha = (sum > 0f) ? weights[i] / sum : 0f;
            Color c           = bgImages[i].color;
            c.a               = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * bgFadeSpeed);
            bgImages[i].color = c;
        }
    }

    // Snap
    public void OnEndDrag(PointerEventData eventData)
    {
        if (snapOnRelease) TriggerSnap();
    }

    private void TriggerSnap()
    {
        int   closest  = GetCentreIndex();
        float total    = scrollRect.content.rect.width - scrollRect.viewport.rect.width;
        if (total <= 0f) return;

        float btnX         = buttonItems[closest].anchoredPosition.x;
        float targetX      = btnX - scrollRect.viewport.rect.width * 0.5f;
        _snapTargetNorm    = Mathf.Clamp01(targetX / total);
        _isSnapping        = true;
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

    // Helpers
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

    // Public API
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