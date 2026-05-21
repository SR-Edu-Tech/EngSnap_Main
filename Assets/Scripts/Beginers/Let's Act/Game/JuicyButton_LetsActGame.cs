using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// JuicyButton — Drop on any UI Button to get:
///   • Scale punch on press (squish + bounce)
///   • Tint flash on hover
///   • SFX on click
///   • Shake animation on wrong answer
///   • Celebrate animation on correct answer
///
/// Works alongside Unity's Button component for onClick wiring.
/// </summary>
[RequireComponent(typeof(Button))]
public class JuicyButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Scale Animation")]
    [SerializeField] private float pressScale    = 0.88f;
    [SerializeField] private float bounceScale   = 1.12f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float bounceDuration= 0.10f;
    [SerializeField] private float resetDuration = 0.07f;
    [SerializeField] private float hoverScale    = 1.05f;

    [Header("Tint")]
    [SerializeField] private Color hoverTint    = new Color(1f, 0.95f, 0.85f, 1f);
    [SerializeField] private Color correctTint  = new Color(0.7f, 1f, 0.7f, 1f);
    [SerializeField] private Color wrongTint    = new Color(1f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color disabledTint = new Color(0.75f, 0.75f, 0.75f, 1f);

    [Header("Wobble / Shake")]
    [SerializeField] private float shakeAmount   = 8f;
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private int   shakeLoops    = 3;

    [Header("Celebration Punch")]
    [SerializeField] private float celebrateScale = 1.25f;
    [SerializeField] private float celebrateDuration = 0.18f;

    // ── Internal ─────────────────────────────────────────────────────────────
    private Vector3 _originalScale;
    private Color   _originalColor = Color.white;
    private Coroutine _scaleCo;
    private Coroutine _colorCo;
    private Coroutine _shakeCo;
    private Button _btn;
    private Image  _img;
    private bool _isDown;

    void Awake()
    {
        _btn           = GetComponent<Button>();
        _img           = GetComponent<Image>();
        _originalScale = transform.localScale;
        if (_img != null) _originalColor = _img.color;

        // Hook into onClick for the tap SFX
        _btn.onClick.AddListener(OnClickSFX);
    }

    void OnDestroy() => _btn?.onClick.RemoveListener(OnClickSFX);

    // ── Pointer events ───────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        if (!_btn.interactable) return;
        ScaleTo(hoverScale, 0.1f);
        TintTo(hoverTint, 0.1f);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonHover, 0f);
    }

    public void OnPointerExit(PointerEventData _)
    {
        if (_isDown) return;
        ScaleTo(1f, 0.12f);
        TintTo(_originalColor, 0.12f);
    }

    public void OnPointerDown(PointerEventData _)
    {
        if (!_btn.interactable) return;
        _isDown = true;
        ScaleTo(pressScale, pressDuration);
        TintTo(_originalColor * 0.85f, pressDuration);
    }

    public void OnPointerUp(PointerEventData _)
    {
        _isDown = false;
        if (!_btn.interactable) return;
        // Bounce then settle
        StartCoroutine(BounceSequence());
        TintTo(_originalColor, 0.1f);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// Call this when the player chose the correct answer
    public void PlayCorrectAnim()
    {
        StopAll();
        StartCoroutine(CelebrateSequence());
        TintTo(correctTint, 0.1f, () => TintTo(_originalColor, 0.4f));
    }

    /// Call this when the player chose the wrong answer
    public void PlayWrongAnim()
    {
        StopAll();
        TintTo(wrongTint, 0.08f, () => TintTo(_originalColor, 0.4f));
        _shakeCo = StartCoroutine(ShakeSequence());
    }

    /// Lock visually (greyed out, non-interactive)
    public void SetDisabled(bool disabled)
    {
        _btn.interactable = !disabled;
        TintTo(disabled ? disabledTint : _originalColor, 0.15f);
    }

    // ── Coroutines ───────────────────────────────────────────────────────────

    private void OnClickSFX() =>
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);

    private IEnumerator BounceSequence()
    {
        yield return ScaleCoroutine(bounceScale, bounceDuration);
        yield return ScaleCoroutine(1f, resetDuration);
    }

    private IEnumerator CelebrateSequence()
    {
        yield return ScaleCoroutine(celebrateScale, celebrateDuration);
        yield return ScaleCoroutine(0.95f, celebrateDuration * 0.6f);
        yield return ScaleCoroutine(1f, celebrateDuration * 0.4f);
    }

    private IEnumerator ShakeSequence()
    {
        float elapsed = 0f;
        Vector3 origin = transform.localPosition;
        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            float dampen   = 1f - progress;            // fade shake out
            float angle    = Mathf.Sin(progress * Mathf.PI * shakeLoops * 2f) * shakeAmount * dampen;
            transform.localPosition = origin + new Vector3(angle, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = origin;
    }

    // ── Scale helpers ────────────────────────────────────────────────────────

    private void ScaleTo(float target, float duration)
    {
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        _scaleCo = StartCoroutine(ScaleCoroutineRaw(target, duration));
    }

    private IEnumerator ScaleCoroutine(float targetMult, float duration)
    {
        Vector3 target = _originalScale * targetMult;
        Vector3 start  = transform.localScale;
        float   t      = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.LerpUnclamped(start, target, t / duration);
            yield return null;
        }
        transform.localScale = target;
    }

    private IEnumerator ScaleCoroutineRaw(float targetMult, float duration)
    {
        yield return ScaleCoroutine(targetMult, duration);
    }

    // ── Color helpers ────────────────────────────────────────────────────────

    private void TintTo(Color target, float duration, Action onDone = null)
    {
        if (_img == null) return;
        if (_colorCo != null) StopCoroutine(_colorCo);
        _colorCo = StartCoroutine(ColorCoroutine(target, duration, onDone));
    }

    private IEnumerator ColorCoroutine(Color target, float duration, Action onDone)
    {
        Color start = _img.color;
        float t     = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _img.color = Color.Lerp(start, target, t / duration);
            yield return null;
        }
        _img.color = target;
        onDone?.Invoke();
    }

    private void StopAll()
    {
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        if (_colorCo != null) StopCoroutine(_colorCo);
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        transform.localScale = _originalScale;
    }
}
