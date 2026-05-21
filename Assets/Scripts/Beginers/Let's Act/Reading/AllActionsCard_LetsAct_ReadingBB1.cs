using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// AllActionsCard_LetsAct_ReadingBB1
/// Sits on each card prefab used in AllActionsReviewController.
/// Identical in behaviour to ActionWordCard — separate class so Unity
/// keeps the prefab references clean and independent.
///
/// PREFAB HIERARCHY:
///   CardRoot                  ← AllActionsCard_LetsAct_ReadingBB1 here
///     ├── IllustrationImage   ← drag to illustrationImage
///     ├── WordBanner
///     │     └── WordLabel     ← drag to wordLabel (TMP_Text)
///     └── TappedGlow          ← drag to tappedGlow (optional highlight GO)
///
/// No Button component needed on prefab — it is added automatically.
/// No Animator component needed — all animation is script-driven.
/// </summary>
public class AllActionsCard_LetsAct_ReadingBB1 : MonoBehaviour
{
    [Header("UI References")]
    public Image    illustrationImage;
    public TMP_Text wordLabel;
    public GameObject tappedGlow;        // optional glow/highlight overlay

    [HideInInspector] public Button button;

    private CanvasGroup _cg;

    // ═════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════
    void Awake()
    {
        // Auto-add Button if missing on prefab
        button = GetComponent<Button>();
        if (button == null) button = gameObject.AddComponent<Button>();

        EnsureCanvasGroup();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PUBLIC API — called by AllActionsReviewController
    // ═════════════════════════════════════════════════════════════════════

    public void SetWord(string word)
    {
        if (wordLabel != null) wordLabel.text = word;
    }

    public void SetSprite(Sprite sprite)
    {
        if (illustrationImage != null && sprite != null)
            illustrationImage.sprite = sprite;
    }

    /// <summary>
    /// Instantly hides or shows the card via CanvasGroup only.
    /// IMPORTANT: scale is intentionally never set to zero here.
    /// Setting scale=0 collapses the RectTransform size which causes
    /// Layout Groups (HorizontalLayoutGroup / GridLayoutGroup inside a
    /// ScrollRect) to stack every card at the same position.
    /// Pop animations handle scale independently once the card is shown.
    /// Safe to call even if Awake has not run yet (parent inactive).
    /// </summary>
    public void SetHidden(bool hidden)
    {
        EnsureCanvasGroup();
        _cg.alpha          = hidden ? 0f : 1f;
        _cg.interactable   = !hidden;
        _cg.blocksRaycasts = !hidden;
        // NOTE: do NOT touch localScale here — scale=0 breaks Layout Groups.
        // PlayPopAnim() starts from scale 0 → 1 and handles its own reset.
        // When showing for the first time via PlayPopAnim we pre-set scale
        // to 0 there, not here.
    }

    // ═════════════════════════════════════════════════════════════════════
    //  POP ANIMATION — card appears (scale 0 → 1.1 → 1)
    // ═════════════════════════════════════════════════════════════════════
    public void PlayPopAnim()
    {
        StopAllCoroutines();
        // Pre-set scale to 0 here (not in SetHidden) so Layout Group always
        // knows the card's real size even while it is invisible.
        transform.localScale = Vector3.zero;
        StartCoroutine(PopRoutine());
    }

    IEnumerator PopRoutine()
    {
        EnsureCanvasGroup();
        _cg.alpha          = 1f;
        _cg.interactable   = true;
        _cg.blocksRaycasts = true;

        // Scale 0 → 1.1
        float t = 0f, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(0f, 1.1f, t / dur);
            yield return null;
        }

        // Scale 1.1 → 1.0
        t = 0f; dur = 0.1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, t / dur);
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAP ANIMATION — bounce + glow feedback (scale 1 → 1.15 → 1)
    // ═════════════════════════════════════════════════════════════════════
    public void PlayTapAnim()
    {
        StopAllCoroutines();
        StartCoroutine(TapRoutine());
    }

    IEnumerator TapRoutine()
    {
        if (tappedGlow != null) tappedGlow.SetActive(true);

        // Scale up
        float t = 0f, dur = 0.1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.18f, t / dur);
            yield return null;
        }

        // Scale down
        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(1.18f, 1f, t / dur);
            yield return null;
        }

        transform.localScale = Vector3.one;

        // Glow lingers briefly then hides
        yield return new WaitForSeconds(0.7f);
        if (tappedGlow != null) tappedGlow.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════
    void EnsureCanvasGroup()
    {
        if (_cg != null) return;
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
    }
}