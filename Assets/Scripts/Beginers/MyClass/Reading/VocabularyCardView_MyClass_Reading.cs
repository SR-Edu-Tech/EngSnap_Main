using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls one vocabulary card in the 2-column grid (Screens 1 & 2).
/// 
/// HIERARCHY expected:
///   VocabularyCard (RectTransform + CanvasGroup)
///   ├─ CardBackground (Image)          ← coloured panel, receives glow
///   ├─ TopHalf
///   │   └─ IllustrationImage (Image)   ← the object illustration
///   └─ BottomHalf
///       └─ WordLabel (TMP_Text)        ← bold word name
/// 
/// Assign via Inspector or call Init() from the parent controller.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class VocabularyCardView_MyClass_Reading : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("UI References")]
    [SerializeField] private Image          cardBackground;
    [SerializeField] private Image          illustrationImage;
    [SerializeField] private TMP_Text       wordLabel;
    [SerializeField] private Button         tapButton;

    [Header("Colours")]
    [SerializeField] private Color          normalColor  = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color          glowColor    = new Color(1f, 0.92f, 0.15f, 1f);  // yellow

    [Header("Particle Burst")]
    [SerializeField] private Image          starParticlePrefab; // drag a small star Image prefab
    [SerializeField] private RectTransform  particleParent;

    // ── Runtime ────────────────────────────────────────────────────────────

    private VocabularyCardData_MyClass_Reading _data;
    private CanvasGroup        _cg;
    private Coroutine          _glowRoutine;
    private Coroutine          _bounceRoutine;
    private bool               _interactable = false;

    // Callback set by parent screen controller
    private Action<VocabularyCardView_MyClass_Reading> _onTapped;

    // ── Init ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        tapButton.onClick.AddListener(OnCardTapped);
    }

    /// <summary>Populate this card with data and register tap callback.</summary>
    public void Init(VocabularyCardData_MyClass_Reading data, Action<VocabularyCardView_MyClass_Reading> onTapped)
    {
        _data    = data;
        _onTapped = onTapped;

        illustrationImage.sprite = data.illustration;
        wordLabel.text           = data.wordLabel;
        cardBackground.color     = normalColor;

        SetInteractable(false);
    }

    // ── Animations ─────────────────────────────────────────────────────────

    /// <summary>Play pop-in entrance animation.</summary>
    public IEnumerator AnimateIn()
    {
        AudioManager_MyClass_Reading.Instance.PlayCardPop();
        yield return StartCoroutine(UIAnimator_MyClass_Reading.PopIn(transform));
    }

    /// <summary>Highlight the card with a yellow glow while audio plays.</summary>
    public void StartGlow()
    {
        StopGlow();
        AudioManager_MyClass_Reading.Instance.PlayCardGlow();
        _glowRoutine = StartCoroutine(UIAnimator_MyClass_Reading.GlowCard(cardBackground, normalColor, glowColor, 0.5f));
    }

    /// <summary>Sustain glow for given duration then restore.</summary>
    public IEnumerator GlowForDuration(float duration)
    {
        float half = 0.25f;

        // Ramp up
        float t = 0f;
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

        // Ramp down
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            cardBackground.color = Color.Lerp(glowColor, normalColor, UIAnimator_MyClass_Reading.EaseInOut(t));
            yield return null;
        }
        cardBackground.color = normalColor;
    }

    public void StopGlow()
    {
        if (_glowRoutine != null) StopCoroutine(_glowRoutine);
        cardBackground.color = normalColor;
    }

    public void SetInteractable(bool value)
    {
        _interactable         = value;
        tapButton.interactable = value;
        _cg.interactable      = value;
        _cg.blocksRaycasts    = value;
    }

    // ── Tap Handling ───────────────────────────────────────────────────────

    private void OnCardTapped()
    {
        if (!_interactable) return;
        AudioManager_MyClass_Reading.Instance.PlayCardTap();
        if (_bounceRoutine != null) StopCoroutine(_bounceRoutine);
        _bounceRoutine = StartCoroutine(UIAnimator_MyClass_Reading.TapBounce(transform));
        SpawnParticleBurst();
        _onTapped?.Invoke(this);
    }

    // ── Particle burst on tap ──────────────────────────────────────────────

    private void SpawnParticleBurst()
    {
        if (starParticlePrefab == null || particleParent == null) return;
        int count = UnityEngine.Random.Range(4, 7);
        for (int i = 0; i < count; i++)
        {
            Image p = Instantiate(starParticlePrefab, particleParent);
            // Random pastel colour
            p.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.9f, 1f);
            StartCoroutine(UIAnimator_MyClass_Reading.SpawnParticle(p, Vector2.zero));
        }
    }

    // ── Accessors ──────────────────────────────────────────────────────────

    public VocabularyCardData_MyClass_Reading Data  => _data;
    public AudioClip          Audio => _data?.wordAudio;
}
