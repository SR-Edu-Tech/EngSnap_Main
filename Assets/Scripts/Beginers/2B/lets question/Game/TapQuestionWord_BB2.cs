using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum QuestionWord6_BB2 { Who, What, Why, How, Where, When }

[System.Serializable]
public class GapQuestionData_QuestionWords_BB2
{
    [Tooltip("Text before the blank ONLY, e.g. '' (blank is usually first)")]
    public string textBeforeGap;
    [Tooltip("Text after the blank ONLY, e.g. ' is he?'")]
    public string textAfterGap;
    [Tooltip("Correct question word for this round")]
    public QuestionWord6_BB2 correctWord;
    [Tooltip("Answer picture cue, e.g. a boy / a pencil / a kitchen")]
    public Sprite pictureCue;
    [Tooltip("Optional narrator VO played before the round pops in. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
    [Tooltip("VO of the FULL question once filled, e.g. 'Who is he?'")]
    public AudioClip fullQuestionAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tap The Question Word — QuestionWords_BB2.
/// A gapped question + answer picture appears each round. 6 FIXED colour-
/// coded buttons (WHO/WHAT/WHY/HOW/WHERE/WHEN) are always on screen —
/// student taps the one that fits. Correct: the word drops into the gap
/// in its colour, the full question reads aloud, chime plays. Wrong:
/// gentle wobble + hint, no penalty, retry within the same round.
/// Fires OnFinished after 8 rounds.
/// </summary>
public class TapQuestionWord_BB2 : MonoBehaviour
{
    [Header("Rounds — 8, IN ORDER")]
    public GapQuestionData_QuestionWords_BB2[] rounds = new GapQuestionData_QuestionWords_BB2[8];

    [Header("UI — Question")]
    public TMP_Text questionText;   // full question, e.g. "___ is he?" → "Who is he?"
    public RectTransform gapSlot;   // invisible marker for the flying word landing + sparkle VFX position only
    public Image pictureCueImage;

    [Header("UI — Word Buttons (6 FIXED, same order as the enum: Who/What/Why/How/Where/When)")]
    public Button[]   wordButtons = new Button[6];
    public TMP_Text[] wordLabels  = new TMP_Text[6];

    [Header("Word Colors — same order as the enum, used for flying word AND filled-in gap text")]
    [SerializeField] private Color[] wordColors = new Color[6]
    {
        new Color(0.3f, 0.6f, 1f),    // Who   - blue
        new Color(0.4f, 0.85f, 0.4f), // What  - green
        new Color(1f, 0.6f, 0.2f),    // Why   - orange
        new Color(0.6f, 0.4f, 0.9f),  // How   - purple
        new Color(0.9f, 0.3f, 0.3f),  // Where - red
        new Color(0.2f, 0.75f, 0.75f) // When  - teal
    };

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'Look at the answer — try again!'")]
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
    public AudioClip contentPopSfx;
    public AudioClip buttonPopSfx;

    [Header("Timing")]
    [SerializeField] private float flyDuration                   = 0.3f;
    [SerializeField] private float delayAfterCorrect              = 0.9f;
    [SerializeField] private float delayBeforeNextButton          = 0.6f;
    [SerializeField] private float popInDuration                  = 0.35f;
    [SerializeField] private float popOutDuration                 = 0.2f;
    [SerializeField] private float beatWithoutNarration           = 0.25f;
    [SerializeField] private float delayBetweenContentAndButtons  = 0.15f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private static readonly string[] WordText =
        { "Who", "What", "Why", "How", "Where", "When" };

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;
    private Color _questionTextOriginalColor;

    void Awake()
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            int capturedIndex = i;
            if (wordButtons[i] != null)
            {
                wordButtons[i].onClick.RemoveAllListeners();
                wordButtons[i].onClick.AddListener(() => OnWordTapped((QuestionWord6_BB2)capturedIndex));
            }
            if (i < wordLabels.Length && wordLabels[i] != null)
                wordLabels[i].text = WordText[i];
        }

        if (questionText != null) _questionTextOriginalColor = questionText.color;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[TapQuestionWord_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;
        if (questionText != null) questionText.color = _questionTextOriginalColor;

        SetScaleZero(questionText != null ? questionText.rectTransform : null);
        SetScaleZero(pictureCueImage != null ? pictureCueImage.rectTransform : null);
        foreach (var btn in wordButtons)
            SetScaleZero(btn != null ? btn.GetComponent<RectTransform>() : null);
        SetButtonsInteractable(false);

        StartCoroutine(IntroThenLoadRound(0));

        Debug.Log("[TapQuestionWord_BB2] RestartGame — starting from round 0");
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
    //  Round reveal sequence: narrator → pop content → pop buttons
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadRoundSequence(int index, bool isFirstLoad)
    {
        SetButtonsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCurrent());

        var data = rounds[index];
        if (questionText != null)
        {
            questionText.text  = data.textBeforeGap + "___" + data.textAfterGap;
            questionText.color = _questionTextOriginalColor;
        }
        if (pictureCueImage != null) pictureCueImage.sprite = data.pictureCue;

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

        if (contentPopSfx != null) AudioManager.Instance?.PlaySFX(contentPopSfx);
        var contentRoutines = new List<Coroutine>();
        if (questionText    != null) contentRoutines.Add(StartCoroutine(PopIn(questionText.rectTransform)));
        if (pictureCueImage != null) contentRoutines.Add(StartCoroutine(PopIn(pictureCueImage.rectTransform)));
        foreach (var r in contentRoutines) yield return r;

        yield return new WaitForSeconds(delayBetweenContentAndButtons);

        if (buttonPopSfx != null) AudioManager.Instance?.PlaySFX(buttonPopSfx);
        var buttonRoutines = new List<Coroutine>();
        foreach (var btn in wordButtons)
            if (btn != null) buttonRoutines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in buttonRoutines) yield return r;

        SetButtonsInteractable(true);
    }

    private IEnumerator PopOutCurrent()
    {
        var routines = new List<Coroutine>();
        if (questionText    != null) routines.Add(StartCoroutine(PopOut(questionText.rectTransform)));
        if (pictureCueImage != null) routines.Add(StartCoroutine(PopOut(pictureCueImage.rectTransform)));
        foreach (var btn in wordButtons)
            if (btn != null) routines.Add(StartCoroutine(PopOut(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnWordTapped(QuestionWord6_BB2 tapped)
    {
        var data = rounds[_currentIndex];
        if (tapped == data.correctWord)
            StartCoroutine(HandleCorrectTap(tapped));
        else
            StartCoroutine(HandleWrongTap(tapped));
    }

    private IEnumerator HandleCorrectTap(QuestionWord6_BB2 word)
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(correctTapSfx != null ? correctTapSfx : AudioManager.Instance.sfxCorrect);

        yield return StartCoroutine(FlyWordToGap(word));

        var data = rounds[_currentIndex];
        int idx = (int)word;
        if (questionText != null)
        {
            questionText.text  = data.textBeforeGap + WordText[idx] + data.textAfterGap;
            questionText.color = wordColors[idx];
        }

        if (gapSlot != null) VFXManager.Instance?.SpawnCorrectBurst(gapSlot);

        if (dialogueAudioSource != null && data.fullQuestionAudio != null)
        {
            dialogueAudioSource.clip = data.fullQuestionAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.fullQuestionAudio.length);
        }

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < rounds.Length)
            StartCoroutine(LoadRoundSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllRoundsComplete());
    }

    private IEnumerator HandleWrongTap(QuestionWord6_BB2 tapped)
    {
        AudioManager.Instance?.PlaySFX(wrongTapSfx != null ? wrongTapSfx : AudioManager.Instance.sfxWrong);

        int idx = (int)tapped;
        if (idx >= 0 && idx < wordButtons.Length && wordButtons[idx] != null)
            yield return StartCoroutine(WobbleButton(wordButtons[idx].GetComponent<RectTransform>()));

        if (dialogueAudioSource != null && wrongTapHintClip != null)
        {
            dialogueAudioSource.clip = wrongTapHintClip;
            dialogueAudioSource.Play();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator FlyWordToGap(QuestionWord6_BB2 word)
    {
        if (gapSlot == null) yield break;

        int idx = (int)word;
        if (idx < 0 || idx >= wordButtons.Length || wordButtons[idx] == null) yield break;
        Button sourceButton = wordButtons[idx];

        var flyingGO = new GameObject("FlyingWord", typeof(RectTransform));
        flyingGO.transform.SetParent(mainCanvasGroup != null ? mainCanvasGroup.transform : transform, false);
        var flyingRect = flyingGO.GetComponent<RectTransform>();
        var flyingText = flyingGO.AddComponent<TextMeshProUGUI>();
        flyingText.text      = WordText[idx];
        flyingText.fontSize  = 40;
        flyingText.alignment = TextAlignmentOptions.Center;
        flyingText.color     = wordColors[idx];

        flyingRect.position = sourceButton.GetComponent<RectTransform>().position;

        Vector3 startPos = flyingRect.position;
        Vector3 endPos   = gapSlot.position;
        float e = 0f;
        while (e < flyDuration)
        {
            e += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, e / flyDuration);
            flyingRect.position = Vector3.Lerp(startPos, endPos, p);
            yield return null;
        }

        Destroy(flyingGO);
    }

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
        foreach (var btn in wordButtons)
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
