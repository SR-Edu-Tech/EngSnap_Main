using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// TAPPABLE OBJECT — attach to every illustrated sprite in Screen 1.
///
/// ── WHY NO OUTLINE COMPONENT ─────────────────────────────────────
/// Unity's Outline component renders 4 offset sprite copies to fake
/// an outline. At any effectDistance above ~4 px those copies are
/// clearly visible as ghost duplicates of the whole object.
/// This script does NOT use Outline anywhere — all feedback is done
/// with img.color tinting so there are zero ghost copies.
///
/// ── ANIMATION SUMMARY ────────────────────────────────────────────
///   PlayCorrectAnim() — green tint + upward position arc
///   PlayWrongAnim()   — red tint + horizontal shake
///   PlayHintPulse()   — yellow tint + scale-in / hold / scale-out
///   ResetState()      — resets everything to default between rounds
/// </summary>
[RequireComponent(typeof(Image))]
public class TappableObject_MyClass_Game : MonoBehaviour, IPointerDownHandler
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────

    [Header("Feedback Tint Colours  (blended onto the sprite — no Outline)")]
    [Tooltip("Green tint flashed on correct tap")]
    public Color correctTintColor = new Color(0.3f, 1f,   0.4f, 1f);
    [Tooltip("Red tint flashed on wrong tap")]
    public Color wrongTintColor   = new Color(1f,   0.25f, 0.25f, 1f);
    [Tooltip("Yellow tint pulsed during hint")]
    public Color hintTintColor    = new Color(1f,   0.9f,  0.2f, 1f);

    [Header("Tint Blend Strengths  (0 = invisible  1 = full colour)")]
    [Range(0f, 1f)] public float correctTintStrength = 0.55f;
    [Range(0f, 1f)] public float wrongTintStrength   = 0.55f;
    [Range(0f, 0.6f)] public float hintTintStrength  = 0.35f;

    [Header("Feel Tuning")]
    [Range(0.05f, 0.25f)] public float tapSquishX          = 0.12f;
    [Range(0.05f, 0.25f)] public float tapSquishY          = 0.15f;
    public float correctBounceHeight = 24f;
    public float wrongShakeMagnitude = 14f;
    public float wrongShakeCount     = 4f;

    [Header("Hint Pulse")]
    [Tooltip("Peak scale multiplier during hint  (1.10 = 10 % bigger)")]
    public float hintScaleUp          = 1.10f;
    public float hintScaleInDuration  = 0.30f;
    public float hintHoldDuration     = 0.15f;
    public float hintScaleOutDuration = 0.30f;
    public float hintPauseDuration    = 0.55f;

    // ─────────────────────────────────────────────────────────────
    //  RUNTIME
    // ─────────────────────────────────────────────────────────────

    [HideInInspector] public Action OnTapped;

    private Image     img;
    private Vector3   originalScale;
    private Vector3   originalPos;
    private Color     originalColor;

    private bool      initialised   = false;
    private bool      hintRunning   = false;
    private Coroutine hintCoroutine = null;
    private Coroutine animCoroutine = null;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake() => Init();

    void Init()
    {
        if (initialised) return;
        initialised = true;

        img           = GetComponent<Image>();
        originalScale = transform.localScale;
        originalPos   = transform.localPosition;
        originalColor = img.color;

        // Remove any existing Outline — we never want it
        Outline existing = GetComponent<Outline>();
        if (existing != null) Destroy(existing);
    }

    // ─────────────────────────────────────────────────────────────
    //  TAP DETECTION
    // ─────────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        StartCoroutine(TapSquish());
        OnTapped?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC ANIMATION CALLS
    // ─────────────────────────────────────────────────────────────

    public void PlayCorrectAnim()
    {
        Init();
        StopCurrentAnim();
        StopHint();
        animCoroutine = StartCoroutine(CorrectBounce());
    }

    public void PlayWrongAnim()
    {
        Init();
        StopCurrentAnim();
        animCoroutine = StartCoroutine(WrongShake());
    }

    public void PlayHintPulse()
    {
        Init();
        if (hintRunning) return;
        hintCoroutine = StartCoroutine(HintPulse());
    }

    public void ResetState()
    {
        Init();
        StopCurrentAnim();
        StopHint();
        transform.localScale    = originalScale;
        transform.localPosition = originalPos;
        img.color               = originalColor;
    }

    // ─────────────────────────────────────────────────────────────
    //  COROUTINE ANIMATIONS
    // ─────────────────────────────────────────────────────────────

    IEnumerator TapSquish()
    {
        float   dur      = 0.18f;
        float   e        = 0f;
        Vector3 squished = new Vector3(
            originalScale.x * (1f - tapSquishX),
            originalScale.y * (1f + tapSquishY),
            originalScale.z);

        while (e < dur * 0.4f)
        {
            e += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, squished, EaseOut(e / (dur * 0.4f)));
            yield return null;
        }

        e = 0f;
        Vector3 over = new Vector3(originalScale.x * 1.08f, originalScale.y * 0.95f, originalScale.z);
        while (e < dur * 0.35f)
        {
            e += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squished, over, EaseOut(e / (dur * 0.35f)));
            yield return null;
        }

        e = 0f;
        while (e < dur * 0.25f)
        {
            e += Time.deltaTime;
            transform.localScale = Vector3.Lerp(over, originalScale, EaseOut(e / (dur * 0.25f)));
            yield return null;
        }
        transform.localScale = originalScale;
    }

    IEnumerator CorrectBounce()
    {
        // Flash green tint — no Outline, no ghost copies
        Color correctColor = Color.Lerp(originalColor, correctTintColor, correctTintStrength);
        img.color = correctColor;

        // Single upward position arc — localScale untouched
        float dur = 0.45f, e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float p = e / dur;
            transform.localPosition = originalPos + Vector3.up * (correctBounceHeight * Mathf.Sin(p * Mathf.PI));
            yield return null;
        }
        transform.localPosition = originalPos;

        // Fade tint back to original
        yield return new WaitForSeconds(0.2f);
        float fadeTime = 0.4f, fe = 0f;
        while (fe < fadeTime)
        {
            fe       += Time.deltaTime;
            img.color = Color.Lerp(correctColor, originalColor, fe / fadeTime);
            yield return null;
        }
        img.color = originalColor;
    }

    IEnumerator WrongShake()
    {
        // Flash red tint — no Outline, no ghost copies
        Color wrongColor = Color.Lerp(originalColor, wrongTintColor, wrongTintStrength);
        img.color = wrongColor;

        float dur   = 0.05f;
        float mag   = wrongShakeMagnitude;
        int   count = (int)wrongShakeCount * 2;

        for (int i = 0; i < count; i++)
        {
            float   dir = (i % 2 == 0) ? 1f : -1f;
            float   e   = 0f;
            Vector3 tgt = originalPos + Vector3.right * dir * mag;
            while (e < dur)
            {
                e += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(transform.localPosition, tgt, e / dur);
                yield return null;
            }
            mag *= 0.75f;
        }
        transform.localPosition = originalPos;

        // Fade tint back
        yield return new WaitForSeconds(0.3f);
        float fadeTime = 0.3f, fe = 0f;
        while (fe < fadeTime)
        {
            fe       += Time.deltaTime;
            img.color = Color.Lerp(wrongColor, originalColor, fe / fadeTime);
            yield return null;
        }
        img.color = originalColor;
    }

    IEnumerator HintPulse()
    {
        hintRunning = true;

        Color   hintColor = Color.Lerp(originalColor, hintTintColor, hintTintStrength);
        Vector3 bigScale  = originalScale * hintScaleUp;

        while (hintRunning)
        {
            // Scale IN + tint ON
            float e = 0f;
            while (hintRunning && e < hintScaleInDuration)
            {
                e += Time.deltaTime;
                float t = EaseOut(e / hintScaleInDuration);
                transform.localScale = Vector3.Lerp(originalScale, bigScale, t);
                img.color            = Color.Lerp(originalColor,   hintColor, t);
                yield return null;
            }
            if (!hintRunning) break;
            transform.localScale = bigScale;
            img.color            = hintColor;

            // Hold
            float held = 0f;
            while (hintRunning && held < hintHoldDuration)
            { held += Time.deltaTime; yield return null; }
            if (!hintRunning) break;

            // Scale OUT + tint OFF
            e = 0f;
            while (hintRunning && e < hintScaleOutDuration)
            {
                e += Time.deltaTime;
                float t = EaseOut(e / hintScaleOutDuration);
                transform.localScale = Vector3.Lerp(bigScale,    originalScale, t);
                img.color            = Color.Lerp(hintColor,    originalColor,  t);
                yield return null;
            }
            if (!hintRunning) break;
            transform.localScale = originalScale;
            img.color            = originalColor;

            // Pause
            float paused = 0f;
            while (hintRunning && paused < hintPauseDuration)
            { paused += Time.deltaTime; yield return null; }
        }

        transform.localScale = originalScale;
        img.color            = originalColor;
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────

    void StopCurrentAnim()
    {
        if (animCoroutine != null) { StopCoroutine(animCoroutine); animCoroutine = null; }
    }

    void StopHint()
    {
        hintRunning = false;
        if (hintCoroutine != null) { StopCoroutine(hintCoroutine); hintCoroutine = null; }
        if (initialised)
        {
            transform.localScale = originalScale;
            img.color            = originalColor;
        }
    }

    float EaseOut(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}