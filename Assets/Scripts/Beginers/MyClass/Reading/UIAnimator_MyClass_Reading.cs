using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Static helper that runs premium UI animations as coroutines.
/// All methods return IEnumerator – start with StartCoroutine().
/// No external tween library needed; pure Unity coroutines.
/// </summary>
public static class UIAnimator_MyClass_Reading
{
    // ── Card Pop-In ───────────────────────────────────────────────────────

    /// <summary>Card bounces in from scale 0 → overshoot → settle at 1.</summary>
    public static IEnumerator PopIn(Transform card, float duration = 0.35f)
    {
        card.localScale = Vector3.zero;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float scale = EaseOutBack(t);
            card.localScale = Vector3.one * Mathf.Max(0f, scale);
            yield return null;
        }
        card.localScale = Vector3.one;
    }

    /// <summary>Staggered pop-in for a group of cards.</summary>
    public static IEnumerator PopInGroup(Transform[] cards, float stagger = 0.08f, float duration = 0.35f, Action onDone = null)
    {
        foreach (var card in cards)
        {
            card.localScale = Vector3.zero;
            card.gameObject.SetActive(true);
        }

        for (int i = 0; i < cards.Length; i++)
        {
            MonoBehaviour owner = cards[i].GetComponent<MonoBehaviour>();
            if (owner != null)
                owner.StartCoroutine(PopIn(cards[i], duration));

            yield return new WaitForSeconds(stagger);
        }

        // Wait for last card to finish
        yield return new WaitForSeconds(duration);
        onDone?.Invoke();
    }

    // ── Card Pop-Out ──────────────────────────────────────────────────────

    /// <summary>
    /// Staggered pop-out for a group of cards (reverse order so it feels like
    /// they're swept away). Each card shrinks to scale 0 then is left inactive.
    /// Call Destroy on each card after this coroutine finishes.
    /// </summary>
    public static IEnumerator PopOutGroup(Transform[] cards, float stagger = 0.06f, float duration = 0.22f, Action onDone = null)
    {
        // Reverse order for a nice sweep-away feel
        for (int i = cards.Length - 1; i >= 0; i--)
        {
            if (cards[i] == null) continue;
            MonoBehaviour owner = cards[i].GetComponent<MonoBehaviour>();
            if (owner != null)
                owner.StartCoroutine(PopOut(cards[i], duration));

            yield return new WaitForSeconds(stagger);
        }

        // Wait for the first card (last to start) to finish
        yield return new WaitForSeconds(duration);
        onDone?.Invoke();
    }

    /// <summary>Shrinks a single card to scale 0 (inverse of PopIn).</summary>
    public static IEnumerator PopOut(Transform card, float duration = 0.22f)
    {
        Vector3 startScale = card.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            card.localScale = Vector3.Lerp(startScale, Vector3.zero, EaseInBack(t));
            yield return null;
        }
        card.localScale = Vector3.zero;
        card.gameObject.SetActive(false);
    }

    // ── Glow Highlight ────────────────────────────────────────────────────

    /// <summary>Flashes card background to glowColor then back to normalColor.</summary>
    public static IEnumerator GlowCard(Image cardBackground, Color normalColor, Color glowColor, float glowDuration = 0.8f)
    {
        float half = glowDuration * 0.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / half;
            cardBackground.color = Color.Lerp(normalColor, glowColor, EaseInOut(t));
            yield return null;
        }
        cardBackground.color = glowColor;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            cardBackground.color = Color.Lerp(glowColor, normalColor, EaseInOut(t));
            yield return null;
        }
        cardBackground.color = normalColor;
    }

    // ── Card Tap Bounce ───────────────────────────────────────────────────

    /// <summary>Quick squish-and-bounce on tap.</summary>
    public static IEnumerator TapBounce(Transform card, float duration = 0.25f)
    {
        float half = duration * 0.5f;
        Vector3 squish = new Vector3(1.12f, 0.88f, 1f);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / half;
            card.localScale = Vector3.Lerp(Vector3.one, squish, EaseInOut(t));
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            card.localScale = Vector3.Lerp(squish, Vector3.one, EaseOutBack(t));
            yield return null;
        }
        card.localScale = Vector3.one;
    }

    // ── Button Pulse ──────────────────────────────────────────────────────

    /// <summary>Idle breathing pulse on a button — run as a looping coroutine.</summary>
    public static IEnumerator ButtonIdlePulse(Transform btn, float minScale = 0.96f, float maxScale = 1.04f, float period = 1.2f)
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime / period;
            float s = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f);
            btn.localScale = Vector3.one * s;
            yield return null;
        }
    }

    /// <summary>Quick press-down then spring-up feedback for buttons.</summary>
    public static IEnumerator ButtonPress(Transform btn, float pressScale = 0.88f, float duration = 0.18f)
    {
        float half = duration * 0.5f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            btn.localScale = Vector3.one * Mathf.Lerp(1f, pressScale, EaseInOut(t));
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            btn.localScale = Vector3.one * Mathf.Lerp(pressScale, 1f, EaseOutBack(t));
            yield return null;
        }
        btn.localScale = Vector3.one;
    }

    // ── Screen Entrance ───────────────────────────────────────────────────

    /// <summary>Whole panel slides up from below + fades in.</summary>
    public static IEnumerator ScreenSlideIn(RectTransform panel, CanvasGroup cg, float duration = 0.4f)
    {
        Vector2 startPos = panel.anchoredPosition - new Vector2(0f, 80f);
        Vector2 endPos   = panel.anchoredPosition;
        cg.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = EaseOutCubic(t);
            panel.anchoredPosition = Vector2.Lerp(startPos, endPos, e);
            cg.alpha = Mathf.Lerp(0f, 1f, e);
            yield return null;
        }
        panel.anchoredPosition = endPos;
        cg.alpha = 1f;
    }

    // ── Mascot Float ──────────────────────────────────────────────────────

    /// <summary>Gentle float up and down for mascot sprites.</summary>
    public static IEnumerator MascotFloat(Transform mascot, float amplitude = 8f, float period = 2.5f)
    {
        Vector3 origin = mascot.localPosition;
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime / period;
            mascot.localPosition = origin + new Vector3(0f, Mathf.Sin(t * Mathf.PI * 2f) * amplitude, 0f);
            yield return null;
        }
    }

    // ── Phrase Card Reveal ────────────────────────────────────────────────

    /// <summary>Phrase card expands from left edge (width 0 → full width) + fades in.</summary>
    public static IEnumerator PhraseCardReveal(RectTransform card, CanvasGroup cg, float duration = 0.3f)
    {
        float fullWidth = card.sizeDelta.x;
        card.sizeDelta = new Vector2(0f, card.sizeDelta.y);
        cg.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float e = EaseOutBack(t);
            card.sizeDelta = new Vector2(Mathf.Lerp(0f, fullWidth, e), card.sizeDelta.y);
            cg.alpha = Mathf.Lerp(0f, 1f, EaseOutCubic(t));
            yield return null;
        }
        card.sizeDelta = new Vector2(fullWidth, card.sizeDelta.y);
        cg.alpha = 1f;
    }

    // ── Floating Particle Burst ───────────────────────────────────────────

    /// <summary>Spawns and animates a small UI star/sparkle from a world position.</summary>
    public static IEnumerator SpawnParticle(Image particle, Vector2 startPos, float duration = 0.6f)
    {
        RectTransform rt = particle.rectTransform;
        rt.anchoredPosition = startPos;
        particle.gameObject.SetActive(true);

        Vector2 endPos = startPos + new Vector2(UnityEngine.Random.Range(-60f, 60f),
                                                 UnityEngine.Random.Range(40f, 100f));
        Color startColor = particle.color;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, EaseOutCubic(t));
            particle.color = new Color(startColor.r, startColor.g, startColor.b,
                                       Mathf.Lerp(1f, 0f, EaseInOut(t)));
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.2f, EaseInOut(t));
            yield return null;
        }
        particle.gameObject.SetActive(false);
    }

    // ── Section Label Bounce ──────────────────────────────────────────────

    public static IEnumerator LabelDropIn(RectTransform label, float duration = 0.45f)
    {
        Vector2 start = label.anchoredPosition + new Vector2(0f, 60f);
        Vector2 end   = label.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            label.anchoredPosition = Vector2.Lerp(start, end, EaseOutBack(t));
            yield return null;
        }
        label.anchoredPosition = end;
    }

    // ── Easing Functions ──────────────────────────────────────────────────

    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>Ease-in with a slight overshoot pull-back — mirror of EaseOutBack.</summary>
    public static float EaseInBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        t = Mathf.Clamp01(t);
        return c3 * t * t * t - c1 * t * t;
    }

    public static float EaseInOut(float t)
    {
        t = Mathf.Clamp01(t);
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
    }

    public static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}

/// <summary>
/// Tiny MonoBehaviour used by UIAnimator to run coroutines from static context.
/// Auto-created — do NOT place manually in scene.
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    private static CoroutineRunner _instance;
    public static CoroutineRunner Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[CoroutineRunner]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<CoroutineRunner>();
            return _instance;
        }
    }
}