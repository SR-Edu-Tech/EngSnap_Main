using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SimonAnswerCard — one of 4 tappable answer cards in the Simon Says panel.
///
/// Hierarchy:
///   AnswerCard (this script + JuicyButton + Image background)
///     ├─ CardIllustration  (Image — action sprite)
///     ├─ ActionWordLabel   (TMP_Text — word beneath illustration)
///     ├─ CorrectOverlay    (Image — green tick, hidden by default)
///     └─ WrongOverlay      (Image — red X,   hidden by default)
///
/// MatchingGameController sets IsCorrect and registers OnTapped callback.
/// </summary>
[RequireComponent(typeof(JuicyButton))]
public class SimonAnswerCard : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image    cardBackground;
    [SerializeField] private Image    cardIllustration;
    [SerializeField] private TMP_Text actionWordLabel;
    [SerializeField] private Image    correctOverlay;
    [SerializeField] private Image    wrongOverlay;
    [SerializeField] private Image    glowBorder;          // outer glow ring

    [Header("Colors")]
    [SerializeField] private Color idleColor    = Color.white;
    [SerializeField] private Color correctColor = new Color(0.65f, 1f, 0.65f, 1f);
    [SerializeField] private Color wrongColor   = new Color(1f, 0.58f, 0.58f, 1f);

    // Runtime
    public bool IsCorrect { get; private set; }
    private Action<SimonAnswerCard> _onTapped;
    private JuicyButton _juicy;
    private bool _answered;

    void Awake()
    {
        _juicy = GetComponent<JuicyButton>();
        correctOverlay?.gameObject.SetActive(false);
        wrongOverlay?.gameObject.SetActive(false);
        if (glowBorder != null) glowBorder.gameObject.SetActive(false);
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public void Initialise(string word, Sprite sprite, bool isCorrect,
                           Action<SimonAnswerCard> onTapped)
    {

        gameObject.SetActive(true); 
        IsCorrect = isCorrect;
        _onTapped = onTapped;
        _answered = false;

        if (actionWordLabel    != null) actionWordLabel.text    = word.ToUpper();
        if (cardIllustration   != null) cardIllustration.sprite = sprite;
        if (cardBackground     != null) cardBackground.color    = idleColor;

        // Entrance: scale from zero
        transform.localScale = Vector3.zero;
    }

    public void PlayEntrance(float delay)
    {
        StartCoroutine(EntranceCoroutine(delay));
    }

    // ── Interaction ──────────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData _)
    {
        if (_answered) return;
        _answered = true;
        _onTapped?.Invoke(this);
    }

    // ── Result Display ───────────────────────────────────────────────────────

    public void ShowCorrect()
    {
        cardBackground.color = correctColor;
        correctOverlay?.gameObject.SetActive(true);
        _juicy.PlayCorrectAnim();
        _juicy.SetDisabled(true);

        if (glowBorder != null) glowBorder.gameObject.SetActive(true);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);
        VFXManager.Instance?.SpawnCorrectBurst(GetComponent<RectTransform>());
        VFXManager.Instance?.SpawnConfetti();
    }

    public void ShowWrong()
    {
        cardBackground.color = wrongColor;
        wrongOverlay?.gameObject.SetActive(true);
        _juicy.PlayWrongAnim();
        _juicy.SetDisabled(true);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);
        VFXManager.Instance?.SpawnWrongPuff(GetComponent<RectTransform>());
    }

    /// Reveal the correct card (after wrong selection — highlight the right answer)
    public void HighlightAsAnswer()
    {
        if (!IsCorrect) return;
        cardBackground.color = correctColor;
        correctOverlay?.gameObject.SetActive(true);
        if (glowBorder != null) glowBorder.gameObject.SetActive(true);
        _juicy.SetDisabled(true);
    }

    public void SetInteractable(bool value) => _juicy.SetDisabled(!value);

    // ── Entrance Animation ───────────────────────────────────────────────────

    private System.Collections.IEnumerator EntranceCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCardFlip, 0.08f);

        float t = 0f, dur = 0.22f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one * EaseOutBack(t / dur);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
