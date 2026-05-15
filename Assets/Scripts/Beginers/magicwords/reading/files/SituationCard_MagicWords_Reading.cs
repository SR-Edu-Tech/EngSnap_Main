using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// SituationCard_MagicWords_Reading
/// Represents a single situation card in Panel 2.
///
/// Prefab hierarchy expected:
///   SituationCard (RectTransform + CanvasGroup)
///     ├── CardBackground   (Image – rounded card bg)
///     ├── IllustrationArea (Button + Image – top portion)
///     │     └── IllustrationImage (Image)
///     ├── Divider          (Image)
///     └── WordArea         (Button)
///           └── MagicWordText (TMP_Text – large, bold)
///
/// The card slides in from the right with an elastic bounce.
/// Tapping the illustration replays the full situation audio.
/// Tapping the magic word text plays just the word audio.
/// </summary>
public class SituationCard_MagicWords_Reading : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI References")]
    public Image    cardBackground;
    public Image    illustrationImage;
    public Button   illustrationButton;
    public TMP_Text magicWordText;
    public Button   wordTextButton;
    public Image    accentStripe;        // coloured bar at card top/bottom

    [Header("Slide-In Animation")]
    [Tooltip("Card slides in from this offset (pixels to the right)")]
    public float slideFromX   = 800f;
    [Range(0.3f, 1.2f)]
    public float slideDuration = 0.55f;

    [Header("Tap Pulse")]
    [Range(0.06f, 0.25f)]
    public float tapPulseDuration = 0.15f;

    [Header("Visual Feedback")]
    public ParticleSystem tapSparkleEmitter;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────────────────────────────────

    private MagicWordData_MagicWords_Reading _data;
    private Action<MagicWordData_MagicWords_Reading> _onIllustrationTapped;
    private Action<MagicWordData_MagicWords_Reading> _onWordTapped;

    private RectTransform _rect;
    private CanvasGroup   _canvasGroup;
    private Vector2       _finalPosition;

    // ─────────────────────────────────────────────────────────────────────────
    //  Initialise (called by Panel2 controller)
    // ─────────────────────────────────────────────────────────────────────────

    public void Initialise(
        MagicWordData_MagicWords_Reading data,
        Action<MagicWordData_MagicWords_Reading> onIllustrationTapped,
        Action<MagicWordData_MagicWords_Reading> onWordTapped)
    {
        _data                 = data;
        _onIllustrationTapped = onIllustrationTapped;
        _onWordTapped         = onWordTapped;

        _rect        = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Populate visuals
        if (illustrationImage   != null && data.situationIllustration != null)
            illustrationImage.sprite = data.situationIllustration;

        if (magicWordText != null)
            magicWordText.text = data.magicWord;

        if (accentStripe != null)
            accentStripe.color = data.accentColor;

        if (cardBackground != null)
        {
            // Subtle tint: mix white with accent at low opacity
            var tint = Color.Lerp(Color.white, data.accentColor, 0.08f);
            cardBackground.color = tint;
        }

        // Wire buttons
        illustrationButton?.onClick.AddListener(OnIllustrationClicked);
        wordTextButton?.onClick.AddListener(OnWordTextClicked);

        // Start off-screen and invisible
        _finalPosition            = _rect.anchoredPosition;
        _rect.anchoredPosition   += new Vector2(slideFromX, 0f);
        _canvasGroup.alpha         = 0f;
        _canvasGroup.interactable  = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Slide-in
    // ─────────────────────────────────────────────────────────────────────────

    public IEnumerator PlaySlideIn()
    {
        float elapsed = 0f;
        Vector2 startPos = _rect.anchoredPosition;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / slideDuration);
            float e = ElasticOut(p);

            _rect.anchoredPosition = Vector2.Lerp(startPos, _finalPosition, e);
            _canvasGroup.alpha     = Mathf.Lerp(0f, 1f, p * 3f); // fade in fast
            yield return null;
        }

        _rect.anchoredPosition    = _finalPosition;
        _canvasGroup.alpha        = 1f;
        _canvasGroup.interactable = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Tap Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnIllustrationClicked()
    {
        tapSparkleEmitter?.Play();
        StartCoroutine(PulseImage(illustrationImage?.transform));
        _onIllustrationTapped?.Invoke(_data);
    }

    private void OnWordTextClicked()
    {
        tapSparkleEmitter?.Play();
        StartCoroutine(PulseImage(magicWordText?.transform));
        _onWordTapped?.Invoke(_data);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Micro-animations
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator PulseImage(Transform t)
    {
        if (t == null) yield break;
        float e = 0f;
        while (e < tapPulseDuration * 2f)
        {
            e += Time.deltaTime;
            float p = e / (tapPulseDuration * 2f);
            float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.07f;
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static float ElasticOut(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float p = 0.3f;
        return Mathf.Pow(2f, -10f * t)
             * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
    }
}
