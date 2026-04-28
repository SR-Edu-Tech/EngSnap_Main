  using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ButtonPopupSequence : MonoBehaviour
{
  
/// <summary>
/// Attach this to a parent GameObject containing Button children.
/// On enable, buttons pop in one-after-another with a rope-wave (arc) effect.
/// Each button plays its assigned AudioClip on popup.
/// </summary>

    [System.Serializable]
    public class ButtonEntry
    {
        public Button button;
        public AudioClip popSound;
    }

    [Header("Buttons")]
    [Tooltip("Assign buttons in order. Each can have its own pop sound.")]
    public List<ButtonEntry> buttons = new List<ButtonEntry>();

    [Header("Rope Wave Timing")]
    [Tooltip("Delay between each button's pop start (seconds). Lower = faster wave.")]
    public float staggerDelay = 0.12f;

    [Tooltip("Duration of each button's scale-in/out bounce animation.")]
    public float popDuration = 0.45f;

    [Header("Scale Animation")]
    [Tooltip("Overshoot scale — how big the button 'bounces' before settling.")]
    public float overshootScale = 1.35f;

    [Tooltip("Arc peak scale — the mid-wave scale that mimics the rope's peak.")]
    public float arcPeakScale = 1.15f;

    [Header("Audio")]
    public AudioSource audioSource;

    // ---------------------------------------------------------------

    private Vector3[] _originalScales;
    private Coroutine _waveCoroutine;

    void Awake()
    {
        // Cache original scales and hide all buttons
        _originalScales = new Vector3[buttons.Count];
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].button != null)
            {
                _originalScales[i] = buttons[i].button.transform.localScale;
                buttons[i].button.transform.localScale = Vector3.zero;
                buttons[i].button.gameObject.SetActive(false);
            }
        }

        // Auto-create AudioSource if not assigned
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnEnable()
    {
        // Reset all buttons to hidden state first
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i].button != null)
            {
                buttons[i].button.transform.localScale = Vector3.zero;
                buttons[i].button.gameObject.SetActive(false);
            }
        }

        // Start the rope-wave popup sequence
        if (_waveCoroutine != null)
            StopCoroutine(_waveCoroutine);

        _waveCoroutine = StartCoroutine(RopeWaveSequence());
    }

    void OnDisable()
    {
        if (_waveCoroutine != null)
        {
            StopCoroutine(_waveCoroutine);
            _waveCoroutine = null;
        }
    }

    // ---------------------------------------------------------------
    // MAIN WAVE SEQUENCE
    // ---------------------------------------------------------------

    IEnumerator RopeWaveSequence()
    {
        if (buttons.Count == 0) yield break;

        // Single button — just pop it directly
        if (buttons.Count == 1)
        {
            yield return StartCoroutine(PopButton(0, 0f));
            yield break;
        }

        // Multiple buttons — fire them staggered like a rope wiggle
        // The arc magnitude is highest in the middle, tapering at the ends
        for (int i = 0; i < buttons.Count; i++)
        {
            float normalizedPos = (float)i / (buttons.Count - 1);      // 0 → 1
            float arcFactor     = Mathf.Sin(normalizedPos * Mathf.PI); // 0 → 1 → 0 (arc shape)

            // Each coroutine runs independently so the overlap creates the wave
            StartCoroutine(PopButton(i, arcFactor));
            yield return new WaitForSeconds(staggerDelay);
        }
    }

    // ---------------------------------------------------------------
    // PER-BUTTON POP ANIMATION
    // ---------------------------------------------------------------

    IEnumerator PopButton(int index, float arcFactor)
    {
        if (index < 0 || index >= buttons.Count) yield break;

        var entry  = buttons[index];
        if (entry.button == null) yield break;

        Transform t         = entry.button.transform;
        Vector3   targetScale = _originalScales[index];

        // Activate the button but start invisible
        entry.button.gameObject.SetActive(true);
        t.localScale = Vector3.zero;

        // Play the pop sound
        if (entry.popSound != null && audioSource != null)
            audioSource.PlayOneShot(entry.popSound);

        // --- Phase 1: Scale up past target (overshoot with arc influence) ---
        float peakScale = Mathf.Lerp(overshootScale, overshootScale * arcPeakScale, arcFactor);
        yield return StartCoroutine(ScaleTo(t, targetScale * peakScale, popDuration * 0.45f, EaseOutCubic));

        // --- Phase 2: Scale back down slightly (rope settling dip) ---
        float dipScale = Mathf.Lerp(0.88f, 0.82f, arcFactor);
        yield return StartCoroutine(ScaleTo(t, targetScale * dipScale, popDuration * 0.25f, EaseInOutQuad));

        // --- Phase 3: Settle to final scale ---
        yield return StartCoroutine(ScaleTo(t, targetScale, popDuration * 0.30f, EaseOutCubic));
    }

    // ---------------------------------------------------------------
    // GENERIC SCALE TWEEN
    // ---------------------------------------------------------------

    IEnumerator ScaleTo(Transform t, Vector3 to, float duration, System.Func<float, float> ease)
    {
        Vector3 from    = t.localScale;
        float   elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed        += Time.deltaTime;
            float progress  = Mathf.Clamp01(elapsed / duration);
            t.localScale    = Vector3.LerpUnclamped(from, to, ease(progress));
            yield return null;
        }

        t.localScale = to;
    }

    // ---------------------------------------------------------------
    // EASING FUNCTIONS
    // ---------------------------------------------------------------

    static float EaseOutCubic(float t)  => 1f - Mathf.Pow(1f - t, 3f);
    static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

    // ---------------------------------------------------------------
    // PUBLIC API  — call from other scripts if needed
    // ---------------------------------------------------------------

    /// <summary>Manually re-trigger the wave animation.</summary>
    public void TriggerWave()
    {
        OnEnable();
    }

    /// <summary>Instantly hide all buttons (no animation).</summary>
    public void HideAll()
    {
        if (_waveCoroutine != null) StopCoroutine(_waveCoroutine);
        foreach (var entry in buttons)
            if (entry.button != null)
            {
                entry.button.transform.localScale = Vector3.zero;
                entry.button.gameObject.SetActive(false);
            }
    }
}
