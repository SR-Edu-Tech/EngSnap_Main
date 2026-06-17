using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// SentenceButton_feel
/// ─────────────────────────────────────────────────────────────────────────
/// One of the 3 sentence buttons shown in Step 3 of each round.
/// Juicy: hover scale, press squish, correct green punch + bob, wrong red shake.
///
/// PREFAB HIERARCHY:
///   SentenceButton_feel     ← this script + Button + Image (card bg)
///     └─ SentenceLabel      ← TMP_Text   ← assign in Inspector
/// </summary>
[RequireComponent(typeof(Button))]
public class SentenceButton_feel : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler,  IPointerUpHandler
{
    [Header("UI References")]
    [SerializeField] private TMP_Text sentenceLabel;

    [Header("Colors")]
    [SerializeField] private Color idleColor    = Color.white;
    [SerializeField] private Color hoverColor   = new Color(0.88f, 0.96f, 1f,   1f);
    [SerializeField] private Color correctColor = new Color(0.60f, 1f,    0.65f,1f);
    [SerializeField] private Color wrongColor   = new Color(1f,    0.50f, 0.50f,1f);

    [Header("Hover / Press")]
    [SerializeField] private float hoverScale    = 1.07f;
    [SerializeField] private float hoverDuration = 0.12f;
    [SerializeField] private float pressScale    = 0.93f;
    [SerializeField] private float pressDuration = 0.07f;

    [Header("Correct Punch")]
    [SerializeField] private float correctScale    = 1.18f;
    [SerializeField] private float correctDuration = 0.22f;

    [Header("Wrong Shake")]
    [SerializeField] private float shakeAmount   = 16f;
    [SerializeField] private float shakeDuration = 0.40f;

    [Header("Idle Bob")]
    [SerializeField] private float bobAmount = 0.025f;
    [SerializeField] private float bobSpeed  = 2.0f;

    // ── Runtime ──────────────────────────────────────────────────────────
    private Button   _button;
    private Image    _bgImage;
    private bool     _isCorrect;
    private bool     _isDown;
    private Action<SentenceButton_feel, bool> _onTapped;
    private Coroutine _scaleCo;
    private Coroutine _colorCo;
    private Coroutine _shakeCo;
    private Coroutine _bobCo;
    private readonly Vector3 _baseScale = Vector3.one;

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

    public void Setup(string text, bool isCorrect, Action<SentenceButton_feel, bool> onTapped)
    {
        _isCorrect = isCorrect;
        _onTapped  = onTapped;

        if (sentenceLabel != null) sentenceLabel.text = text;

        StopAllAnims();
        transform.localScale = Vector3.zero;   // starts at 0 — controller pops it in
        SetBg(idleColor);
        SetInteractable(true);
    }

    public void SetInteractable(bool on) { if (_button) _button.interactable = on; }

    public void PlayCorrectAnim()
    {
        StopAllAnims();
        SetBg(correctColor);
        _scaleCo = StartCoroutine(PunchThenBob());
    }

    public void PlayWrongAnim()
    {
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        _shakeCo = StartCoroutine(ShakeThenReset());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Pointer events
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
        yield return ScaleCoroutine(correctScale, correctDuration);
        yield return ScaleCoroutine(1f, correctDuration * 0.5f);
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
            float p      = elapsed / shakeDuration;
            float dampen = 1f - p;
            float offset = Mathf.Sin(p * Mathf.PI * 7f) * shakeAmount * dampen;
            transform.localPosition = origin + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.localPosition = origin;

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
    private void OnClicked()    => _onTapped?.Invoke(this, _isCorrect);
}
