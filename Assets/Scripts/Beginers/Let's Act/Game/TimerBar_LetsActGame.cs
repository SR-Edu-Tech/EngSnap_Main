using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TimerBar — animated progress bar for the speed round.
///
/// Hierarchy:
///   TimerBar (this script)
///     ├─ Background (Image — grey bar)
///     ├─ Fill       (Image — coloured fill, fillMethod = Horizontal)
///     └─ TickIcon   (Image — a tiny clock icon, optional)
///
/// Colors transition green → yellow → red as time depletes.
/// Ticks SFX every second. Shakes when < 3 seconds left.
/// </summary>
public class TimerBar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image fillImage;

    [Header("Gradient: full → empty")]
    [SerializeField] private Color colorFull   = new Color(0.35f, 0.85f, 0.35f, 1f);  // green
    [SerializeField] private Color colorMid    = new Color(1f, 0.85f, 0.2f, 1f);      // yellow
    [SerializeField] private Color colorLow    = new Color(1f, 0.35f, 0.35f, 1f);     // red

    [Header("Shake when low")]
    [SerializeField] private float shakeThreshold = 3f;
    [SerializeField] private float shakeAmount    = 5f;

    private Coroutine _timerCo;
    private Vector3   _origin;

    void Awake()
    {
        _origin = transform.localPosition;
        gameObject.SetActive(false);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// Start the countdown. onExpire fires when time runs out (may be null).
    public void StartTimer(float seconds, Action onExpire)
    {
        gameObject.SetActive(true);
        if (_timerCo != null) StopCoroutine(_timerCo);
        _timerCo = StartCoroutine(RunTimer(seconds, onExpire));
    }

    /// Stop timer early (player answered before time ran out)
    public void StopTimer()
    {
        if (_timerCo != null) StopCoroutine(_timerCo);
        _timerCo = null;
        gameObject.SetActive(false);
        transform.localPosition = _origin;
    }

    // ── Coroutine ─────────────────────────────────────────────────────────────

    private IEnumerator RunTimer(float total, Action onExpire)
    {
        float remaining = total;
        int   lastTick  = Mathf.CeilToInt(total);

        while (remaining > 0f)
        {
            remaining -= Time.deltaTime;
            float t = Mathf.Clamp01(remaining / total);

            // Update fill
            if (fillImage != null)
            {
                fillImage.fillAmount = t;
                fillImage.color = t > 0.5f
                    ? Color.Lerp(colorMid, colorFull, (t - 0.5f) * 2f)
                    : Color.Lerp(colorLow, colorMid,  t * 2f);
            }

            // Tick SFX each second
            int currentTick = Mathf.CeilToInt(remaining);
            if (currentTick < lastTick)
            {
                lastTick = currentTick;
                AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxTimerTick, 0.02f);
            }

            // Shake when low
            if (remaining <= shakeThreshold)
            {
                float shake = Mathf.Sin(Time.time * 20f) * shakeAmount * (1f - remaining / shakeThreshold);
                transform.localPosition = _origin + new Vector3(shake, 0f, 0f);
            }
            else
            {
                transform.localPosition = _origin;
            }

            yield return null;
        }

        // Timer expired
        if (fillImage != null) fillImage.fillAmount = 0f;
        transform.localPosition = _origin;
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxTimerEnd);
        gameObject.SetActive(false);
        onExpire?.Invoke();
    }
}
