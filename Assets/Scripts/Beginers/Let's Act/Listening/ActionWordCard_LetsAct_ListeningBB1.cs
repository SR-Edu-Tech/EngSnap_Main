using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ActionWordCard — Sits on the Card Prefab.
///
/// PREFAB HIERARCHY:
///   CardRoot          ← ActionWordCard + Button component here
///     ├── IllustrationImage  ← drag to illustrationImage  (top coloured area)
///     ├── WordBanner
///     │     └── WordLabel   ← drag to wordLabel (TMP or legacy Text)
///     └── CompletedGlow     ← drag to tappedGlow (optional highlight GO)
///
/// CARD ILLUSTRATION SPRITES:
///   Assign cardSprites in ActionWordsController OR keep a separate
///   CardSpriteLibrary ScriptableObject. The controller calls SetSprite().
///
/// ANIMATIONS:
///   Uses simple DOTween-free approach — LeanTween or Unity Animator optional.
///   Pop anim: scale 0 → 1.1 → 1  via coroutine.
///   Tap anim:  scale 1 → 1.15 → 1 via coroutine.
/// </summary>
public class ActionWordCard_LetsAct_ListeningBB1 : MonoBehaviour
{
    [Header("UI References")]
    public Image    illustrationImage;
    public TMP_Text wordLabel;
    public Image    cardBackground;
    public GameObject tappedGlow;

    [HideInInspector] public Button button;

    private CanvasGroup _cg;
    private string _word;

    private Coroutine _animRoutine;

    void Awake()
    {
        // Auto-add Button if prefab doesn't have one
        button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();

        EnsureCanvasGroup();
    }

    // Safely gets or creates CanvasGroup — called from Awake AND before any use
    private void EnsureCanvasGroup()
    {
        if (_cg != null) return;
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
    }

    // ── Called by ActionWordsController ──────────────────────────────────

    public void SetWord(string word)
    {
        _word = word;
        if (wordLabel != null) wordLabel.text = word;
    }

    public void SetSprite(Sprite sprite)
    {
        if (illustrationImage != null) illustrationImage.sprite = sprite;
    }

    public void SetHidden(bool hidden)
    {
        EnsureCanvasGroup(); // guard against Awake not having run yet
        _cg.alpha          = hidden ? 0f : 1f;
        _cg.interactable   = !hidden;
        _cg.blocksRaycasts = !hidden;
        transform.localScale = hidden ? Vector3.zero : Vector3.one;
    }

    // ── Pop animation (card appears) ──────────────────────────────────────
  public void PlayPopAnim()
{
    if (_animRoutine != null)
        StopCoroutine(_animRoutine);

    _animRoutine = StartCoroutine(PopRoutine());
}

    private System.Collections.IEnumerator PopRoutine()
    {
        float t = 0f;
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
        _cg.interactable   = true;

        // Phase 1: scale 0 → 1.1
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(0f, 1.1f, t / 0.25f);
            transform.localScale = Vector3.one * s;
            yield return null;
        }

        // Phase 2: scale 1.1 → 1.0
        t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1.1f, 1f, t / 0.1f);
            transform.localScale = Vector3.one * s;
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    // ── Tap animation (card feedback) ─────────────────────────────────────
    public void PlayTapAnim()
    {
        if (_animRoutine != null)
            StopCoroutine(_animRoutine);

        _animRoutine = StartCoroutine(TapRoutine());
    }

    private System.Collections.IEnumerator TapRoutine()
    {
        // Show glow
        if (tappedGlow != null) tappedGlow.SetActive(true);

        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, 1.15f, t / 0.12f);
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1.15f, 1f, t / 0.12f);
            transform.localScale = Vector3.one * s;
            yield return null;
        }

        transform.localScale = Vector3.one;

        // Hide glow after delay
        yield return new WaitForSeconds(0.8f);
        if (tappedGlow != null) tappedGlow.SetActive(false);
    }
}