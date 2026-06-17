using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// FeelingTile_read  —  one tile. Instantiated at runtime from a single prefab.
///
/// PREFAB HIERARCHY:
///   FeelingTile_read        ← this script + Button + Image (tile bg)
///     ├─ KidImage           ← Image   (assign in prefab Inspector)
///     ├─ WordBubble         ← Image
///     │    └─ WordText      ← TMP_Text
///     └─ StarOverlay        ← Image   (hidden by default)
/// </summary>
[RequireComponent(typeof(Button))]
public class FeelingTile_read : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Prefab Refs — assign once in the prefab")]
    [SerializeField] private Image    kidImage;
    [SerializeField] private TMP_Text wordText;
    [SerializeField] private Image    starOverlay;
    [SerializeField] private Image    highlightOverlay;   // optional yellow tint overlay

    [Header("Colors")]
    [SerializeField] private Color idleColor      = Color.white;
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.5f, 1f);
    [SerializeField] private Color hoverColor     = new Color(0.88f, 0.97f, 1f, 1f);

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverDur   = 0.12f;

    // ── Runtime ──────────────────────────────────────────────────────────
    private Button  _button;
    private Image   _bgImage;
    private int     _index;
    private bool    _locked = true;
    private Action<FeelingTile_read, int> _onTapped;
    private Coroutine _scaleCo;

    void Awake()
    {
        _button  = GetComponent<Button>();
        _bgImage = GetComponent<Image>();
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClicked);

        transform.localScale = Vector3.zero;   // hidden until PopIn
        if (starOverlay      != null) starOverlay.gameObject.SetActive(false);
        if (highlightOverlay != null) highlightOverlay.gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Called by controller after Instantiate to configure this tile.</summary>
    public void Initialise(string word, Sprite sprite, int index, bool locked,
                           Action<FeelingTile_read, int> onTapped)
    {
        _index    = index;
        _onTapped = onTapped;

        if (wordText  != null) wordText.text    = word;
        if (kidImage  != null) kidImage.sprite   = sprite;

        SetBg(idleColor);
        SetLocked(locked);
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (_button) _button.interactable = !locked;
    }

    public void SetHighlight(bool on)
    {
        if (highlightOverlay != null)
            highlightOverlay.gameObject.SetActive(on);
        else if (_bgImage != null)
            _bgImage.color = on ? highlightColor : idleColor;
    }

    public IEnumerator PopIn(float delay = 0f)
    {
        transform.localScale = Vector3.zero;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float t = 0f, dur = 0.28f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(t / dur));
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    public void PlayTapAnim()
    {
        if (starOverlay != null) starOverlay.gameObject.SetActive(true);
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        _scaleCo = StartCoroutine(ScalePunch());
    }

    // ── Hover ────────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData _)
    {
        if (_locked) return;
        ScaleTo(hoverScale, hoverDur);
        if (_bgImage != null) _bgImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData _)
    {
        ScaleTo(1f, hoverDur);
        if (_bgImage != null) _bgImage.color = idleColor;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Private
    // ════════════════════════════════════════════════════════════════════

    private void OnClicked() { if (!_locked) _onTapped?.Invoke(this, _index); }

    private IEnumerator ScalePunch()
    {
        yield return ScaleCoroutine(1.18f, 0.14f);
        yield return ScaleCoroutine(1f,    0.11f);
    }

    private void ScaleTo(float target, float dur)
    {
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        _scaleCo = StartCoroutine(ScaleCoroutine(target, dur));
    }

    private IEnumerator ScaleCoroutine(float target, float dur)
    {
        Vector3 start = transform.localScale;
        Vector3 end   = Vector3.one * target;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.LerpUnclamped(start, end, t / dur);
            yield return null;
        }
        transform.localScale = end;
    }

    private void SetBg(Color c) { if (_bgImage != null) _bgImage.color = c; }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}