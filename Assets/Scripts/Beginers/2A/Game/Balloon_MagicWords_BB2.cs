using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MagicWord_MagicWords_BB2 { Please, Sorry, ThankYou, ExcuseMe, Welcome }

/// <summary>
/// A single floating balloon with a magic word on it.
/// (Add Component → search "Balloon_MagicWords_BB2" — drag-and-drop of
/// this file only auto-attaches the file's primary class.)
/// </summary>
public class Balloon_MagicWords_BB2 : MonoBehaviour
{
    [Header("UI Refs")]
    public TMP_Text label;
    public Image    balloonImage;
    public Button   tapButton;

    public MagicWord_MagicWords_BB2 Word { get; private set; }

    private RectTransform _rect;
    private System.Action<Balloon_MagicWords_BB2> _onTapped;

    public void Initialise(MagicWord_MagicWords_BB2 word, string displayText, Color color, System.Action<Balloon_MagicWords_BB2> onTapped)
    {
        Word      = word;
        _onTapped = onTapped;
        _rect     = GetComponent<RectTransform>();

        if (label != null)        label.text = displayText;
        if (balloonImage != null) balloonImage.color = color;

        if (tapButton != null)
        {
            tapButton.interactable = true;
            tapButton.onClick.RemoveAllListeners();
            tapButton.onClick.AddListener(() => _onTapped?.Invoke(this));
        }
    }

    public void SetInteractable(bool value)
    {
        if (tapButton != null) tapButton.interactable = value;
    }

    // ── Pop (correct) — scale burst then invisible ─────────────────────
    public IEnumerator PlayPop()
    {
        SetInteractable(false);
        Vector3 start = _rect.localScale;
        float e = 0f, dur = 0.25f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float t = e / dur;
            _rect.localScale = Vector3.Lerp(start, start * 1.4f, t);
            yield return null;
        }
        _rect.localScale = Vector3.zero;
    }

    // ── Wobble + re-inflate (wrong) — never disappears ──────────────────
    public IEnumerator PlayWrongWobble()
    {
        Vector3 originalScale = _rect.localScale;
        float e = 0f, dur = 0.3f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float wobble = Mathf.Sin(e * Mathf.PI * 8f) * 0.1f * (1f - e / dur);
            _rect.localScale = originalScale * (1f + wobble);
            yield return null;
        }
        _rect.localScale = originalScale;
    }

    // ── Pop in / out for reset between situations ───────────────────────
    public IEnumerator PopIn(float duration)
    {
        _rect.localScale = Vector3.zero;
        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            _rect.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, EaseOutBack(e / duration));
            yield return null;
        }
        _rect.localScale = Vector3.one;
        SetInteractable(true);
    }

    public void SetScaleZero()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
        _rect.localScale = Vector3.zero;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
