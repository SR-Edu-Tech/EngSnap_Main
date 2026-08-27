using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum ActionCategory_FoodActions_BB2 { Cutting, Heating, Mixing }

[System.Serializable]
public class ActionOptionData_FoodActions_BB2
{
    [Tooltip("Action word shown on the button, e.g. 'peel'")]
    public string actionLabel;
    [Tooltip("This option's category — determines its tint colour")]
    public ActionCategory_FoodActions_BB2 category;
    [Tooltip("True if this is the action that goes with the round's food")]
    public bool isCorrectAction;
}

[System.Serializable]
public class RoundData_FoodActions_BB2
{
    [Tooltip("The food for this round, e.g. a banana / onions / eggs")]
    public Sprite foodSprite;
    [Tooltip("Optional — swapped in on a correct tap to show the food 'acted on', e.g. a peeled banana. Leave empty to just keep the original sprite.")]
    public Sprite resultSprite;
    [Tooltip("Exactly 3 action options for this round — one must have isCorrectAction = true")]
    public ActionOptionData_FoodActions_BB2[] actionOptions = new ActionOptionData_FoodActions_BB2[3];
    [Tooltip("Optional narrator VO for this round. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Do The Right Action — FoodActions_BB2.
/// A food appears each round with 3 FIXED action-button slots that get
/// refilled with a new set of action words each round (8 distinct actions
/// appear across 8 rounds, so the correct + 2 other actions are supplied
/// per round). Each button is tinted by its category colour (cut blue /
/// heat orange / mix green) for that round. Correct tap: the food sprite
/// swaps to its result image, chime plays. Wrong tap: gentle wobble +
/// hint, no penalty, retry within the same round.
/// Fires OnFinished after 8 rounds.
/// </summary>
public class FoodActionMatch_BB2 : MonoBehaviour
{
    [Header("Rounds — 8, IN ORDER")]
    public RoundData_FoodActions_BB2[] rounds = new RoundData_FoodActions_BB2[8];

    [Header("UI — Food")]
    public Image foodImage;

    [Header("UI — Action Button Slots (fixed, 3 — refilled with new actions + tint each round)")]
    public Button[]   actionButtons = new Button[3];
    public TMP_Text[] actionLabels  = new TMP_Text[3];

    [Header("Category Colors")]
    [SerializeField] private Color cuttingColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color heatingColor = new Color(1f, 0.65f, 0.3f);
    [SerializeField] private Color mixingColor  = new Color(0.4f, 0.85f, 0.4f);

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'What do we do with this? Try again!'")]
    public AudioClip   wrongTapHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Tap Feedback SFX")]
    [Tooltip("Played on a correct tap. Leave empty to fall back to AudioManager's default correct sound.")]
    public AudioClip correctTapSfx;
    [Tooltip("Played on a wrong tap. Leave empty to fall back to AudioManager's default wrong sound.")]
    public AudioClip wrongTapSfx;

    [Header("Pop FX")]
    public AudioClip foodPopSfx;
    public AudioClip buttonPopSfx;

    [Header("Timing")]
    [SerializeField] private float resultSwapDelay          = 0.3f;
    [SerializeField] private float delayAfterCorrect         = 0.8f;
    [SerializeField] private float delayBeforeNextButton     = 0.6f;
    [SerializeField] private float popInDuration              = 0.35f;
    [SerializeField] private float popOutDuration             = 0.2f;
    [SerializeField] private float beatWithoutNarration       = 0.25f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;
    private Vector2 _foodOriginalAnchoredPos;

    void Awake()
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            int capturedIndex = i;
            if (actionButtons[i] != null)
            {
                actionButtons[i].onClick.RemoveAllListeners();
                actionButtons[i].onClick.AddListener(() => OnActionTapped(capturedIndex));
            }
        }

        if (foodImage != null)
            _foodOriginalAnchoredPos = foodImage.rectTransform.anchoredPosition;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[FoodActionMatch_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        SetScaleZero(foodImage != null ? foodImage.rectTransform : null);
        foreach (var btn in actionButtons)
            SetScaleZero(btn != null ? btn.GetComponent<RectTransform>() : null);
        SetButtonsInteractable(false);

        StartCoroutine(IntroThenLoadRound(0));

        Debug.Log("[FoodActionMatch_BB2] RestartGame — starting from round 0");
    }

    private IEnumerator IntroThenLoadRound(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadRoundSequence(index, isFirstLoad: true));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Round sequence: pop out old (if any) → refill data+colors → pop in
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadRoundSequence(int index, bool isFirstLoad)
    {
        SetButtonsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCurrent());

        var data = rounds[index];
        if (foodImage != null)
        {
            foodImage.sprite = data.foodSprite;
            foodImage.rectTransform.anchoredPosition = _foodOriginalAnchoredPos;
        }

        for (int i = 0; i < actionButtons.Length && i < data.actionOptions.Length; i++)
        {
            var option = data.actionOptions[i];
            if (actionLabels != null && i < actionLabels.Length && actionLabels[i] != null)
                actionLabels[i].text = option.actionLabel;

            var img = actionButtons[i] != null ? actionButtons[i].GetComponent<Image>() : null;
            if (img != null) img.color = ColorForCategory(option.category);
        }

        if (dialogueAudioSource != null && data.promptAudio != null)
        {
            dialogueAudioSource.clip = data.promptAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.promptAudio.length);
        }
        else
        {
            yield return new WaitForSeconds(beatWithoutNarration);
        }

        if (foodPopSfx != null) AudioManager.Instance?.PlaySFX(foodPopSfx);
        if (foodImage != null) yield return StartCoroutine(PopIn(foodImage.rectTransform));

        if (buttonPopSfx != null) AudioManager.Instance?.PlaySFX(buttonPopSfx);
        var routines = new List<Coroutine>();
        foreach (var btn in actionButtons)
            if (btn != null) routines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;

        SetButtonsInteractable(true);
    }

    private IEnumerator PopOutCurrent()
    {
        var routines = new List<Coroutine>();
        if (foodImage != null) routines.Add(StartCoroutine(PopOut(foodImage.rectTransform)));
        foreach (var btn in actionButtons)
            if (btn != null) routines.Add(StartCoroutine(PopOut(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    private Color ColorForCategory(ActionCategory_FoodActions_BB2 category) => category switch
    {
        ActionCategory_FoodActions_BB2.Cutting => cuttingColor,
        ActionCategory_FoodActions_BB2.Heating => heatingColor,
        _                                        => mixingColor
    };

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnActionTapped(int index)
    {
        var data = rounds[_currentIndex];
        if (index >= data.actionOptions.Length) return;

        if (data.actionOptions[index].isCorrectAction)
            StartCoroutine(HandleCorrectTap());
        else
            StartCoroutine(HandleWrongTap(index));
    }

    private IEnumerator HandleCorrectTap()
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(correctTapSfx != null ? correctTapSfx : AudioManager.Instance.sfxCorrect);

        var data = rounds[_currentIndex];

        yield return new WaitForSeconds(resultSwapDelay);

        if (foodImage != null && data.resultSprite != null)
            foodImage.sprite = data.resultSprite;

        if (foodImage != null) VFXManager.Instance?.SpawnCorrectBurst(foodImage.rectTransform);

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < rounds.Length)
            StartCoroutine(LoadRoundSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllRoundsComplete());
    }

    private IEnumerator HandleWrongTap(int index)
    {
        AudioManager.Instance?.PlaySFX(wrongTapSfx != null ? wrongTapSfx : AudioManager.Instance.sfxWrong);

        var rect = actionButtons[index] != null ? actionButtons[index].GetComponent<RectTransform>() : null;
        if (rect != null)
            yield return StartCoroutine(WobbleButton(rect));

        if (dialogueAudioSource != null && wrongTapHintClip != null)
        {
            dialogueAudioSource.clip = wrongTapHintClip;
            dialogueAudioSource.Play();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator WobbleButton(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 originalScale = t.localScale;
        float e = 0f, dur = 0.3f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float wobble = Mathf.Sin(e * Mathf.PI * 8f) * 0.08f * (1f - e / dur);
            t.localScale = originalScale * (1f + wobble);
            yield return null;
        }
        t.localScale = originalScale;
    }

    private IEnumerator PopIn(RectTransform t)
    {
        if (t == null) yield break;
        t.localScale = Vector3.zero;
        float e = 0f;
        while (e < popInDuration)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, EaseOutBack(e / popInDuration));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private IEnumerator PopOut(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 start = t.localScale;
        float e = 0f;
        while (e < popOutDuration)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.Lerp(start, Vector3.zero, e / popOutDuration);
            yield return null;
        }
        t.localScale = Vector3.zero;
    }

    private static void SetScaleZero(RectTransform t)
    {
        if (t != null) t.localScale = Vector3.zero;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllRoundsComplete()
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxGameComplete);
        VFXManager.Instance?.SpawnConfetti();

        if (dialogueAudioSource != null && outroAudioClip != null)
        {
            dialogueAudioSource.clip = outroAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(outroAudioClip.length);
        }
        else
        {
            yield return new WaitForSeconds(delayBeforeNextButton);
        }

        nextButton?.gameObject.SetActive(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        foreach (var btn in actionButtons)
            if (btn != null) btn.interactable = value;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Next button — wire this to the Button's OnClick() in the Inspector
    // ════════════════════════════════════════════════════════════════════

    public void OnNextButtonPressed()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
        OnFinished?.Invoke();
    }
}
