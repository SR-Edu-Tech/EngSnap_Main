using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// SentenceCard_read  —  one sentence card in Screen 2.
/// During auto-play it's a display item (highlights yellow).
/// During acting phase it becomes a button (hover scale, tap punch).
///
/// PREFAB HIERARCHY:
///   SentenceCard_read       ← this script + Button + Image (card bg)
///     └─ SentenceText       ← TMP_Text (full sentence)
///                              Use Rich Text for colour:
///                              "<color=#FF9500>I am feeling</color> <color=#FF5C8D>happy</color>."
///                              Or assign plain text and let the controller handle it.
/// </summary>
[RequireComponent(typeof(Button))]
public class SentenceCard_read : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Refs")]
    [SerializeField] private TMP_Text sentenceText;

    [Header("Colors")]
    [SerializeField] private Color idleColor      = Color.white;
    [SerializeField] private Color highlightColor = new Color(1f, 0.95f, 0.5f, 1f);
    [SerializeField] private Color hoverColor     = new Color(0.88f, 0.97f, 1f, 1f);
    [SerializeField] private Color actedColor     = new Color(0.75f, 1f, 0.78f, 1f);

    [Header("Hover Scale")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDur   = 0.1f;

    // ── Runtime ──────────────────────────────────────────────────────────
    private Button   _button;
    private Image    _bgImage;
    private int      _index;
    private bool     _locked = true;
    private bool     _acted;
    private Action<int> _onTapped;
    private Coroutine   _scaleCo;

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

    public void Initialise(int index, string sentence, bool locked, Action<int> onTapped)
    {
        _index   = index;
        _onTapped = onTapped;
        _acted   = false;

        // Build rich-text: orange "I am feeling" + pink feeling word
        if (sentenceText != null)
        {
            // Split on last space before the feeling word
            // sentence format: "I am feeling happy."
            const string prefix = "I am feeling ";
            if (sentence.StartsWith(prefix))
            {
                string rest = sentence.Substring(prefix.Length); // "happy."
                sentenceText.text =
                    $"<color=#FF9500>I am feeling</color> <color=#FF5C8D>{rest}</color>";
            }
            else
            {
                sentenceText.text = sentence;
            }
        }

        SetBg(idleColor);
        SetLocked(locked);
    }

    public void SetLocked(bool locked)
    {
        _locked = locked;
        if (_button) _button.interactable = !locked;
    }

    public void SetHighlight(bool on)
    {
        SetBg(on ? highlightColor : (_acted ? actedColor : idleColor));
        if (on) ScaleTo(1.04f, 0.12f);
        else    ScaleTo(1f,    0.12f);
    }

    /// Pop-in with delay (used during acting phase unlock stagger)
    public IEnumerator PopIn(float delay = 0f)
    {
        transform.localScale = Vector3.one * 0.85f;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float t = 0f, dur = 0.22f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / dur);
            float s = Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(p));
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    /// Tap punch + mark acted (green tint)
    public void PlayTapAnim()
    {
        _acted = true;
        SetBg(actedColor);
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        _scaleCo = StartCoroutine(ScalePunch());
    }

    // ── Hover ────────────────────────────────────────────────────────────
    public void OnPointerEnter(PointerEventData _)
    {
        if (_locked) return;
        ScaleTo(hoverScale, hoverDur);
        if (!_acted) SetBg(hoverColor);
    }

    public void OnPointerExit(PointerEventData _)
    {
        ScaleTo(1f, hoverDur);
        if (!_acted) SetBg(idleColor);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Private
    // ════════════════════════════════════════════════════════════════════

    private void OnClicked()
    {
        if (_locked) return;
        _onTapped?.Invoke(_index);
    }

    private IEnumerator ScalePunch()
    {
        yield return ScaleCoroutine(1.10f, 0.12f);
        yield return ScaleCoroutine(1f,    0.10f);
    }

    private void ScaleTo(float target, float dur)
    {
        if (_scaleCo != null) StopCoroutine(_scaleCo);
        _scaleCo = StartCoroutine(ScaleCoroutine(target, dur));
    }

    private IEnumerator ScaleCoroutine(float target, float dur)
    {
        Vector3 start = transform.localScale;
        Vector3 end   = Vector3.one * target;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.LerpUnclamped(start, end, t / dur);
            yield return null;
        }
        transform.localScale = end;
    }

    private void SetBg(Color c) { if (_bgImage != null) _bgImage.color = c; }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
