using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// WordBubble_MagicWords_Reading
/// Controls a single word bubble in Panel 1.
///
/// Responsibilities:
///   • Pop-in animation (scale punch with elastic overshoot)
///   • Idle float/bob animation
///   • Sparkle burst on first tap
///   • "Tapped" state change (glow ring, scale up/down, tick icon)
///   • Calls back to Panel1 controller on first tap
///
/// Attach this to the WordBubble prefab root.
/// The prefab hierarchy should be:
///   WordBubble (RectTransform + Image[bg] + Button)
///     ├── BubbleIcon      (Image)
///     ├── WordLabel       (TMP_Text)
///     ├── SparkleEmitter  (ParticleSystem – optional)
///     └── TappedGlow      (Image – starts hidden)
/// </summary>
[RequireComponent(typeof(Button))]
public class WordBubble_MagicWords_Reading : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI References")]
    public Image       bubbleBackground;
    public Image       bubbleIcon;
    public TMP_Text    wordLabel;
    public Image       tappedGlowRing;      // hidden until tapped
    public GameObject  checkmarkObject;     // shown after first tap

    [Header("Pop Animation")]
    [Range(0.2f, 1.5f)]
    public float popDuration = 0.55f;
    public AnimationCurve popCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Idle Bob")]
    [Range(2f, 12f)]
    public float bobSpeed = 5f;
    [Range(2f, 20f)]
    public float bobAmount = 8f;   // pixels
    [Range(0.5f, 6f)]
    public float rotateAmount = 4f;

    [Header("Tap Feedback")]
    [Range(0.05f, 0.3f)]
    public float tapSquishDuration = 0.12f;

    [Header("Sparkle Burst")]
    public ParticleSystem tapSparkleEmitter;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private MagicWordData_MagicWords_Reading _data;
    private int   _index;
    private bool  _tapped        = false;
    private bool  _interactive   = false;
    private float _bobOffset;    // per-bubble phase offset so they don't sync

    private Action<int, MagicWordData_MagicWords_Reading> _onTappedCallback;

    private RectTransform _rect;
    private Vector2       _basePosition;
    private Button        _button;

    // ─────────────────────────────────────────────────────────────────────────
    //  Initialise (called by Panel1 controller)
    // ─────────────────────────────────────────────────────────────────────────

    public void Initialise(
        MagicWordData_MagicWords_Reading data,
        int index,
        Action<int, MagicWordData_MagicWords_Reading> onTappedCallback)
    {
        _data     = data;
        _index    = index;
        _onTappedCallback = onTappedCallback;
        _bobOffset = index * 1.37f; // prime-ish spacing avoids sync

        _rect = GetComponent<RectTransform>();
        _button = GetComponent<Button>();
        _button.interactable = false; // disabled until pop completes

        // Apply data
        wordLabel.text = data.magicWord;
        if (bubbleBackground != null)
            bubbleBackground.color = data.accentColor;
        if (bubbleIcon != null && data.bubbleIcon != null)
            bubbleIcon.sprite = data.bubbleIcon;

        // Hide tapped UI
        if (tappedGlowRing    != null) tappedGlowRing.enabled   = false;
        if (checkmarkObject   != null) checkmarkObject.SetActive(false);

        // Start invisible
        transform.localScale = Vector3.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    public void PlayPopAnimation() => StartCoroutine(PopIn());

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Update()
    {
        if (!_interactive) return;

        // Idle float bob (sinusoidal position + gentle rotation)
        float t     = Time.time * bobSpeed + _bobOffset;
        float yOff  = Mathf.Sin(t) * bobAmount;
        float rot   = Mathf.Sin(t * 0.7f) * rotateAmount;

        _rect.anchoredPosition = _basePosition + new Vector2(0, yOff);
        transform.localRotation = Quaternion.Euler(0f, 0f, rot);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Tap Handling
    // ─────────────────────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData _)
    {
        if (!_interactive) return;
        StartCoroutine(SquishDown());
    }

    public void OnPointerUp(PointerEventData _)
    {
        if (!_interactive) return;
        StartCoroutine(SquishUp());
        HandleTap();
    }

    private void HandleTap()
    {
        // Emit sparkles every tap
        tapSparkleEmitter?.Play();

        if (!_tapped)
        {
            _tapped = true;
            ShowTappedState();
            _onTappedCallback?.Invoke(_index, _data);
        }
        else
        {
            // Re-tap: replay definition audio (handled by Panel1 via callback)
            _onTappedCallback?.Invoke(_index, _data);
        }
    }

    private void ShowTappedState()
    {
        if (tappedGlowRing != null)
        {
            tappedGlowRing.enabled = true;
            StartCoroutine(PulseGlowRing());
        }
        if (checkmarkObject != null)
        {
            checkmarkObject.SetActive(true);
            StartCoroutine(PunchScale(checkmarkObject.transform, 0.3f));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Animations
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator PopIn()
    {
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / popDuration);

            // Elastic overshoot
            float s = ElasticOut(p);
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        transform.localScale = Vector3.one;

        // Cache base position after pop (for bob)
        _basePosition  = _rect.anchoredPosition;
        _interactive   = true;
        _button.interactable = true;
    }

    private IEnumerator SquishDown()
    {
        float e = 0f;
        while (e < tapSquishDuration)
        {
            e += Time.deltaTime;
            float p = e / tapSquishDuration;
            transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(1.15f, 0.88f, 1f), p);
            yield return null;
        }
    }

    private IEnumerator SquishUp()
    {
        float e = 0f;
        while (e < tapSquishDuration)
        {
            e += Time.deltaTime;
            float p = e / tapSquishDuration;
            float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.12f;
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private IEnumerator PunchScale(Transform t, float dur)
    {
        t.localScale = Vector3.zero;
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float p = e / dur;
            float s = ElasticOut(p);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private IEnumerator PulseGlowRing()
    {
        // Brief glow pulse then stabilise
        float dur = 0.6f, e = 0f;
        Color baseCol  = tappedGlowRing.color;
        Color brightCol = new Color(baseCol.r, baseCol.g, baseCol.b, 1f);
        Color fadeCol   = new Color(baseCol.r, baseCol.g, baseCol.b, 0.55f);

        while (e < dur)
        {
            e += Time.deltaTime;
            float p = Mathf.PingPong(e * 3f, 1f);
            tappedGlowRing.color = Color.Lerp(fadeCol, brightCol, p);
            yield return null;
        }
        tappedGlowRing.color = fadeCol;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Math helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static float ElasticOut(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float p = 0.3f;
        return Mathf.Pow(2f, -10f * t)
             * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p)
             + 1f;
    }
}
