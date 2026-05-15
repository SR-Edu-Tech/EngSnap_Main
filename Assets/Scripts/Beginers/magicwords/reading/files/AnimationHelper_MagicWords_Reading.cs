using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AnimationHelper_MagicWords_Reading
/// Static utility class providing reusable animation coroutines
/// for bubbles, cards, buttons, and UI elements throughout the
/// Magic Words reading unit.
///
/// All methods return IEnumerators to be started via StartCoroutine().
/// Designed for 3–4 year old UX: exaggerated, bouncy, joyful motion.
/// </summary>
public static class AnimationHelper_MagicWords_Reading
{
    // ═══════════════════════════════════════════════════════════════
    //  Scale Animations
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Elastic pop-in from zero to normal scale.</summary>
    public static IEnumerator ElasticPopIn(Transform t, float duration = 0.5f)
    {
        t.localScale = Vector3.zero;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float p = Mathf.Clamp01(e / duration);
            t.localScale = Vector3.one * ElasticOut(p);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    /// <summary>Bounce out – shrink to zero with overshoot then snap.</summary>
    public static IEnumerator ElasticPopOut(Transform t, float duration = 0.35f)
    {
        Vector3 start = t.localScale;
        float   e     = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float p = Mathf.Clamp01(e / duration);
            t.localScale = start * (1f - ElasticIn(p));
            yield return null;
        }
        t.localScale = Vector3.zero;
    }

    /// <summary>Quick squish (press down) then bounce back up.</summary>
    public static IEnumerator Squish(Transform t, float dur = 0.2f,
        float xScale = 1.15f, float yScale = 0.88f)
    {
        Vector3 squished = new Vector3(xScale, yScale, 1f);
        float   e        = 0f;

        // Squish down
        while (e < dur * 0.5f)
        {
            e += Time.deltaTime;
            float p = e / (dur * 0.5f);
            t.localScale = Vector3.Lerp(Vector3.one, squished, p);
            yield return null;
        }
        // Bounce back
        e = 0f;
        while (e < dur * 0.5f)
        {
            e += Time.deltaTime;
            float p = e / (dur * 0.5f);
            float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.12f;
            t.localScale = Vector3.Lerp(squished, Vector3.one * s, p);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    /// <summary>Continuous idle bob (run forever – stop via StopCoroutine).</summary>
    public static IEnumerator IdleBob(Transform t,
        float speed = 1.5f, float pixels = 8f, float phase = 0f)
    {
        Vector3 origin = t.localPosition;
        while (true)
        {
            float y = Mathf.Sin(Time.time * speed + phase) * pixels;
            t.localPosition = origin + new Vector3(0f, y, 0f);
            yield return null;
        }
    }

    /// <summary>Gentle idle rotation swing (run forever).</summary>
    public static IEnumerator IdleSwing(Transform t,
        float speed = 1.2f, float degrees = 5f, float phase = 0f)
    {
        while (true)
        {
            float angle = Mathf.Sin(Time.time * speed + phase) * degrees;
            t.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  Slide Animations
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Slide a RectTransform from startPos to endPos with elastic ease.</summary>
    public static IEnumerator SlideIn(RectTransform rt,
        Vector2 startPos, Vector2 endPos, float duration = 0.55f)
    {
        rt.anchoredPosition = startPos;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            float p = Mathf.Clamp01(e / duration);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, ElasticOut(p));
            yield return null;
        }
        rt.anchoredPosition = endPos;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Alpha / Fade
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Fade a CanvasGroup alpha from 0 to 1.</summary>
    public static IEnumerator FadeIn(CanvasGroup cg, float duration = 0.3f)
    {
        cg.alpha = 0f;
        float e  = 0f;
        while (e < duration)
        {
            e        += Time.deltaTime;
            cg.alpha  = Mathf.Clamp01(e / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    /// <summary>Fade a CanvasGroup alpha from 1 to 0.</summary>
    public static IEnumerator FadeOut(CanvasGroup cg, float duration = 0.3f)
    {
        cg.alpha = 1f;
        float e  = 0f;
        while (e < duration)
        {
            e        += Time.deltaTime;
            cg.alpha  = Mathf.Clamp01(1f - e / duration);
            yield return null;
        }
        cg.alpha = 0f;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Colour Flash
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Flash an Image from its current colour to flashColor and back.</summary>
    public static IEnumerator FlashColor(
        Image img, Color flashColor, float duration = 0.25f)
    {
        Color original = img.color;
        float half     = duration * 0.5f;
        float e        = 0f;

        while (e < half)
        {
            e += Time.deltaTime;
            img.color = Color.Lerp(original, flashColor, e / half);
            yield return null;
        }
        e = 0f;
        while (e < half)
        {
            e += Time.deltaTime;
            img.color = Color.Lerp(flashColor, original, e / half);
            yield return null;
        }
        img.color = original;
    }

    // ═══════════════════════════════════════════════════════════════
    //  Easing Functions
    // ═══════════════════════════════════════════════════════════════

    public static float ElasticOut(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float p = 0.3f;
        return Mathf.Pow(2f, -10f * t)
             * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
    }

    public static float ElasticIn(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float p = 0.3f;
        return -Mathf.Pow(2f, 10f * (t - 1f))
             * Mathf.Sin((t - 1f - p / 4f) * (2f * Mathf.PI) / p);
    }

    public static float BounceOut(float t)
    {
        if (t < 1f / 2.75f)       return 7.5625f * t * t;
        if (t < 2f / 2.75f)  { t -= 1.5f / 2.75f;   return 7.5625f * t * t + 0.75f; }
        if (t < 2.5f / 2.75f){ t -= 2.25f / 2.75f;  return 7.5625f * t * t + 0.9375f; }
        t -= 2.625f / 2.75f;      return 7.5625f * t * t + 0.984375f;
    }
}
