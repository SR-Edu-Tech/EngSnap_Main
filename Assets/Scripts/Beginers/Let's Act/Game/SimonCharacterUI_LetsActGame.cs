using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SimonCharacterUI — handles the Simon speech bubble and entrance/idle animations.
///
/// No mascot animator required.
/// Instead: Simon is a colourful illustrated character Image that:
///   • Bounces in from off-screen on each round
///   • Has a speech bubble that types out text
///   • Plays idle float animation continuously
///   • Shimmers/pulses when VO is playing
///
/// Hierarchy:
///   SimonArea
///     ├─ SimonImage         (Image — Simon's full illustration)
///     ├─ SpeechBubble       (Image — speech bubble graphic)
///     │   ├─ CommandText    (TMP_Text — the command)
///     │   └─ SpeechTail     (Image — tail pointing at Simon)
///     └─ PulseRing          (Image — glowing ring, enabled while VO plays)
/// </summary>
public class SimonCharacterUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform simonImage;
    [SerializeField] private RectTransform speechBubble;
    [SerializeField] private TMP_Text      commandText;
    [SerializeField] private Image         pulseRing;

    [Header("Entrance")]
    [SerializeField] private Vector2 offscreenOffset = new Vector2(-500f, 0f);
    [SerializeField] private float   entranceDuration = 0.45f;

    [Header("Idle Float")]
    [SerializeField] private float floatAmplitude = 14f;
    [SerializeField] private float floatSpeed     = 1.2f;

    [Header("Speech Bubble Typewriter")]
    [SerializeField] private float charDelay = 0.04f;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 2.2f;
    [SerializeField] private float pulseMin   = 0.3f;
    [SerializeField] private float pulseMax   = 0.85f;

    // Runtime
    private Vector2   _simonOrigin;
    private Coroutine _typewriterCo;
    private Coroutine _pulseCo;
    private bool      _isVOPlaying;

    void Awake()
    {
        _simonOrigin = simonImage.anchoredPosition;
        if (pulseRing != null)
        {
            var c = pulseRing.color;
            c.a = 0f;
            pulseRing.color = c;
        }
        speechBubble?.gameObject.SetActive(false);
    }

    void Update() => DoIdleFloat();

    // ── Public API ───────────────────────────────────────────────────────────

    /// Slide Simon in, show speech bubble with typed command, play VO
    public void ShowCommand(string command, int voIndex, System.Action onDone = null)
    {
        StopAllCoroutines();
        StartCoroutine(ShowCommandSequence(command, voIndex, onDone));
    }

    public void SetVOPlaying(bool playing)
    {
        _isVOPlaying = playing;
        if (_pulseCo != null) StopCoroutine(_pulseCo);
        _pulseCo = playing
            ? StartCoroutine(PulseRing())
            : StartCoroutine(FadeRing(0f, 0.2f));
    }

    // ── Coroutines ───────────────────────────────────────────────────────────

    private IEnumerator ShowCommandSequence(string command, int voIndex, System.Action onDone)
    {
        // 1. Slide Simon in
        yield return SlideIn();

        // 2. Pop speech bubble
        speechBubble?.gameObject.SetActive(true);
        yield return BubbleEntrance();

        // 3. Typewriter
        if (_typewriterCo != null) StopCoroutine(_typewriterCo);
        _typewriterCo = StartCoroutine(Typewriter(command));
        yield return _typewriterCo;

        // 4. Play VO
        AudioManager.Instance?.PlayVO(voIndex);
        SetVOPlaying(true);

        // Wait a beat for VO start
        yield return new WaitForSeconds(0.3f);

        onDone?.Invoke();
    }

    private IEnumerator SlideIn()
    {
        simonImage.anchoredPosition = _simonOrigin + offscreenOffset;
        float t = 0f;
        while (t < entranceDuration)
        {
            t += Time.deltaTime;
            float ease = EaseOutBack(t / entranceDuration);
            simonImage.anchoredPosition = Vector2.Lerp(
                _simonOrigin + offscreenOffset, _simonOrigin, ease);
            yield return null;
        }
        simonImage.anchoredPosition = _simonOrigin;
    }

    private IEnumerator BubbleEntrance()
    {
        speechBubble.localScale = Vector3.zero;
        float t = 0f, dur = 0.22f;
        while (t < dur)
        {
            t += Time.deltaTime;
            speechBubble.localScale = Vector3.one * EaseOutBack(t / dur);
            yield return null;
        }
        speechBubble.localScale = Vector3.one;
    }

    private IEnumerator Typewriter(string text)
    {
        if (commandText == null) yield break;
        commandText.text = "";
        foreach (char c in text)
        {
            commandText.text += c;
            yield return new WaitForSeconds(charDelay);
        }
    }

    private void DoIdleFloat()
    {
        if (simonImage == null) return;
        float y = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        simonImage.anchoredPosition = _simonOrigin + new Vector2(0f, y);
    }

    private IEnumerator PulseRing()
    {
        while (true)
        {
            float alpha = pulseMin + (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f)
                          * (pulseMax - pulseMin);
            SetRingAlpha(alpha);
            yield return null;
        }
    }

    private IEnumerator FadeRing(float target, float dur)
    {
        float start = pulseRing != null ? pulseRing.color.a : 0f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetRingAlpha(Mathf.Lerp(start, target, t / dur));
            yield return null;
        }
        SetRingAlpha(target);
    }

    private void SetRingAlpha(float a)
    {
        if (pulseRing == null) return;
        var c = pulseRing.color; c.a = a; pulseRing.color = c;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
