using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// RuleButton_game
/// ─────────────────────────────────────────────────────────────────────────
/// One of the 3 rule buttons. Juicy: hover scale-up, pop-in on setup,
/// correct punch + glow, wrong shake + red flash.
///
/// PREFAB HIERARCHY:
///   RuleButton_game        ← this script + Button + Image (card bg)
///     ├─ RuleImage         ← Image (rule picture)  ← assign in Inspector
///     └─ RuleLabel         ← TMP_Text              ← assign in Inspector
/// </summary>
[RequireComponent(typeof(Button))]
public class RuleButton_game : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler,  IPointerUpHandler
{
    [Header("UI References")]
    [SerializeField] private Image    ruleImage;
    [SerializeField] private TMP_Text ruleLabel;

    [Header("Colors")]
    [SerializeField] private Color idleColor    = Color.white;
    [SerializeField] private Color hoverColor   = new Color(0.9f,  0.97f, 1f,   1f);
    [SerializeField] private Color correctColor = new Color(0.65f, 1f,    0.68f,1f);
    [SerializeField] private Color wrongColor   = new Color(1f,    0.55f, 0.55f,1f);

    [Header("Hover")]
    [SerializeField] private float hoverScale    = 1.10f;
    [SerializeField] private float hoverDuration = 0.12f;

    [Header("Press")]
    [SerializeField] private float pressScale    = 0.92f;
    [SerializeField] private float pressDuration = 0.07f;

    [Header("Correct Punch")]
    [SerializeField] private float correctScale    = 1.20f;
    [SerializeField] private float correctDuration = 0.20f;

    [Header("Wrong Shake")]
    [SerializeField] private float shakeAmount   = 14f;
    [SerializeField] private float shakeDuration = 0.38f;

    [Header("Idle Bob (after correct)")]
    [SerializeField] private float bobAmount = 0.03f;
    [SerializeField] private float bobSpeed  = 2.2f;

    // ── Runtime ──────────────────────────────────────────────────────────
    private Button    _button;
    private Image     _bgImage;
    private bool      _isCorrect;
    private bool      _isDown;
    private Action<RuleButton_game, bool> _onTapped;
    private Coroutine _scaleCo;
    private Coroutine _colorCo;
    private Coroutine _shakeCo;
    private Coroutine _bobCo;
    private Vector3   _baseScale = Vector3.one;

    void Awake()
    {
        _button  = GetComponent<Button>();
        _bgImage = GetComponent<Image>();
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClicked);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════

    public void Setup(Sprite sprite, string labelText, bool isCorrect,
                      Action<RuleButton_game, bool> onTapped)
    {
        _isCorrect = isCorrect;
        _onTapped  = onTapped;

        if (ruleImage != null) ruleImage.sprite = sprite;
        if (ruleLabel != null) ruleLabel.text   = labelText;

        // Stop leftovers
        StopAllAnims();
        transform.localScale = Vector3.zero;   // starts at 0 — controller pops it in
        _baseScale           = Vector3.one;

        SetBg(idleColor);
        SetInteractable(true);
    }

    public void SetInteractable(bool on) { if (_button) _button.interactable = on; }

    // ── Correct ──────────────────────────────────────────────────────────
    public void PlayCorrectAnim()
    {
        StopAllAnims();
        SetBg(correctColor);
        _scaleCo = StartCoroutine(PunchThenBob());
    }

    // ── Wrong ────────────────────────────────────────────────────────────
    public void PlayWrongAnim()
    {
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        _shakeCo = StartCoroutine(ShakeThenReset());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Pointer events — hover & press juice
    // ════════════════════════════════════════════════════════════════════

    public void OnPointerEnter(PointerEventData _)
    {
        if (!_button.interactable) return;
        ScaleTo(hoverScale, hoverDuration);
        TintTo(hoverColor, hoverDuration);
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_isDown) return;
        ScaleTo(1f, hoverDuration);
        TintTo(idleColor, hoverDuration);
    }

    public void OnPointerDown(PointerEventData _)
    {
        if (!_button.interactable) return;
        _isDown = true;
        ScaleTo(pressScale, pressDuration);
    }

    public void OnPointerUp(PointerEventData _)
    {
        _isDown = false;
        if (!_button.interactable) return;
        ScaleTo(1f, pressDuration * 1.5f);
        TintTo(idleColor, 0.1f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animations
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator PunchThenBob()
    {
        // Punch up
        yield return ScaleCoroutine(correctScale, correctDuration);
        // Settle back
        yield return ScaleCoroutine(1f, correctDuration * 0.6f);
        // Idle bob
        _bobCo = StartCoroutine(IdleBob());
    }

    private IEnumerator IdleBob()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * bobSpeed;
            float s = 1f + Mathf.Sin(t * Mathf.PI * 2f) * bobAmount;
            transform.localScale = _baseScale * s;
            yield return null;
        }
    }

    private IEnumerator ShakeThenReset()
    {
        SetBg(wrongColor);
        Vector3 origin  = transform.localPosition;
        float   elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / shakeDuration;
            float dampen   = 1f - progress;
            float offset   = Mathf.Sin(progress * Mathf.PI * 7f) * shakeAmount * dampen;
            transform.localPosition = origin + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.localPosition = origin;

        // Fade tint back
        float fade = 0f, fadeDur = 0.3f;
        while (fade < fadeDur)
        {
            fade += Time.deltaTime;
            if (_bgImage != null)
                _bgImage.color = Color.Lerp(wrongColor, idleColor, fade / fadeDur);
            yield return null;
        }
        SetBg(idleColor);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Scale & tint helpers
    // ════════════════════════════════════════════════════════════════════

    private void ScaleTo(float target, float dur)
    {
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        _scaleCo = StartCoroutine(ScaleCoroutine(target, dur));
    }

    private IEnumerator ScaleCoroutine(float targetMult, float duration)
    {
        Vector3 start  = transform.localScale;
        Vector3 target = _baseScale * targetMult;
        float   t      = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.LerpUnclamped(start, target, t / duration);
            yield return null;
        }
        transform.localScale = target;
    }

    private void TintTo(Color target, float dur)
    {
        if (_bgImage == null) return;
        if (_colorCo != null) StopCoroutine(_colorCo);
        _colorCo = StartCoroutine(ColorCoroutine(target, dur));
    }

    private IEnumerator ColorCoroutine(Color target, float duration)
    {
        Color start = _bgImage.color;
        float t     = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _bgImage.color = Color.Lerp(start, target, t / duration);
            yield return null;
        }
        _bgImage.color = target;
    }

    private void StopAllAnims()
    {
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        if (_colorCo != null) StopCoroutine(_colorCo);
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        if (_bobCo   != null) StopCoroutine(_bobCo);
        transform.localScale    = _baseScale;
        transform.localPosition = transform.localPosition;
    }

    private void SetBg(Color c) { if (_bgImage != null) _bgImage.color = c; }

    private void OnClicked() => _onTapped?.Invoke(this, _isCorrect);
}