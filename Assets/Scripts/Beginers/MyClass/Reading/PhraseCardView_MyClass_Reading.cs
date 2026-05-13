using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls one phrase card in Screen 3 — "CLASSROOM LANGUAGE".
///
/// HIERARCHY expected:
///   PhraseCard (RectTransform + CanvasGroup)
///   ├─ CardBackground (Image)    ← white rounded rectangle; turns yellow when active
///   ├─ PhraseLabel (TMP_Text)    ← phrase text
///   └─ TapButton (Button)        ← full-card tap zone
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class PhraseCardView_MyClass_Reading : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("UI References")]
    [SerializeField] private Image      cardBackground;
    [SerializeField] private TMP_Text   phraseLabel;
    [SerializeField] private Button     tapButton;

    [Header("Colours")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color glowColor   = new Color(1f, 0.92f, 0.15f, 1f);

    [Header("Particles")]
    [SerializeField] private Image          starParticlePrefab;
    [SerializeField] private RectTransform  particleParent;

    // ── Runtime ────────────────────────────────────────────────────────────

    private PhraseCardData_MyClass_Reading  _data;
    private CanvasGroup     _cg;
    private bool            _interactable;
    private Coroutine       _bounceRoutine;

    private Action<PhraseCardView_MyClass_Reading> _onTapped;

    // ── Init ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        tapButton.onClick.AddListener(OnCardTapped);
    }

    public void Init(PhraseCardData_MyClass_Reading data, Action<PhraseCardView_MyClass_Reading> onTapped)
    {
        _data     = data;
        _onTapped = onTapped;

        phraseLabel.text     = data.phraseText;
        cardBackground.color = normalColor;

        SetInteractable(false);
    }

    // ── Reveal Animation ──────────────────────────────────────────────────

    /// <summary>
    /// Slide-reveal from left. Called by Screen3Controller.
    /// Uses cardPopSFX for the slide-in snap (horizontal expand feels like a pop),
    /// matching the same tactile feel as vocabulary card entrances.
    /// </summary>
    public IEnumerator RevealIn()
    {
        AudioManager_MyClass_Reading.Instance.PlayCardPop();   // snap/pop on entry
        yield return StartCoroutine(UIAnimator_MyClass_Reading.PhraseCardReveal(
            GetComponent<RectTransform>(), _cg, 0.32f));
    }

    // ── Glow ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Highlights the card yellow for the given duration then fades back.
    /// Plays cardGlowSFX at the start of the glow so every auto-play and
    /// tap-replay has audible feedback when the card lights up.
    /// </summary>
    public IEnumerator GlowForDuration(float duration)
    {
        AudioManager_MyClass_Reading.Instance.PlayCardGlow();  // ← glow pulse SFX

        float half = 0.2f;
        float t = 0f;

        // Ramp up to glow colour
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            cardBackground.color = Color.Lerp(normalColor, glowColor, UIAnimator_MyClass_Reading.EaseInOut(t));
            yield return null;
        }
        cardBackground.color = glowColor;

        // Hold
        float held = 0f;
        while (held < duration - half * 2f)
        {
            held += Time.deltaTime;
            yield return null;
        }

        // Ramp back to normal
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            cardBackground.color = Color.Lerp(glowColor, normalColor, UIAnimator_MyClass_Reading.EaseInOut(t));
            yield return null;
        }
        cardBackground.color = normalColor;
    }

    // ── Tap ───────────────────────────────────────────────────────────────

    private void OnCardTapped()
    {
        if (!_interactable) return;
        AudioManager_MyClass_Reading.Instance.PlayCardTap();   // tap click SFX
        if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
        _bounceRoutine = StartCoroutine(UIAnimator_MyClass_Reading.TapBounce(transform));
        SpawnParticleBurst();
        _onTapped?.Invoke(this);
    }

    private void SpawnParticleBurst()
    {
        if (starParticlePrefab == null || particleParent == null) return;
        int count = UnityEngine.Random.Range(3, 6);
        for (int i = 0; i < count; i++)
        {
            Image p = Instantiate(starParticlePrefab, particleParent);
            p.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.9f, 1f);
            StartCoroutine(UIAnimator_MyClass_Reading.SpawnParticle(p, Vector2.zero));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    public void SetInteractable(bool value)
    {
        _interactable          = value;
        tapButton.interactable = value;
        _cg.interactable       = value;
        _cg.blocksRaycasts     = value;
    }

    public PhraseCardData_MyClass_Reading Data  => _data;
    public AudioClip      Audio => _data?.phraseAudio;
}