using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum QuestionWord_FrameQuestion_BB2 { What, Where, Who, When, Which, Why, Whose, How }

[System.Serializable]
public class QuestionData_FrameQuestion_BB2
{
    [Tooltip("Question text BEFORE the gap ONLY, e.g. '' (gap is usually first)")]
    public string textBeforeGap;
    [Tooltip("Question text AFTER the gap ONLY, e.g. ' is your name?'")]
    public string textAfterGap;
    [Tooltip("The answer shown beside the build strip, e.g. 'My name is Tom.'")]
    public string answerText;
    [Tooltip("Correct question word for this round")]
    public QuestionWord_FrameQuestion_BB2 correctWord;
    [Tooltip("Optional narrator VO played before the question/answer pop in. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
    [Tooltip("VO of the FULL framed question + answer, e.g. 'What is your name? My name is Tom.'")]
    public AudioClip fullQuestionAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Frame The Question — FrameQuestion_BB2.
/// A half-built question with a gap, and its answer, are shown. 8 FIXED
/// question-word cards (What / Where / Who / When / Which / Why / Whose /
/// How) are always on screen — student taps the one that fits. Wrong taps
/// are never penalised: the tapped card wobbles, a gentle hint plays,
/// student tries again. Correct tap: word flies into the gap, the full
/// question + answer reads aloud, then the next question loads.
/// Fires OnFinished when Next is pressed after all 8 questions.
/// </summary>
public class FrameQuestion_BB2 : MonoBehaviour
{
    [Header("Questions — 8, IN ORDER")]
    public QuestionData_FrameQuestion_BB2[] questions = new QuestionData_FrameQuestion_BB2[8];

    [Header("UI — Build Strip")]
    public TMP_Text questionText;   // full question, e.g. "___ is your name?" → "What is your name?"
    public RectTransform gapSlot;   // invisible marker placed roughly where the blank sits — used for the flying word landing + sparkle VFX position only
    public TMP_Text answerText;

    [Header("UI — Word Cards (8 FIXED, same order as the enum: What/Where/Who/When/Which/Why/Whose/How)")]
    public Button[]   wordButtons = new Button[8];
    public TMP_Text[] wordLabels  = new TMP_Text[8];

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'Hmm, which word fits the answer?'")]
    public AudioClip   wrongTapHintClip;

    [Header("Narration — plays once each")]
    [Tooltip("Plays ONCE at the very start — e.g. 'Frame the question! Look at the answer — which word fits?'")]
    public AudioClip introAudioClip;
    [Tooltip("Plays ONCE after the 8th question — e.g. 'You can frame any question!'")]
    public AudioClip outroAudioClip;

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
        { "What", "Where", "Who", "When", "Which", "Why", "Whose", "How" };

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            int capturedIndex = i;
            if (wordButtons[i] != null)
            {
                wordButtons[i].onClick.RemoveAllListeners();
                wordButtons[i].onClick.AddListener(() => OnWordTapped((QuestionWord_FrameQuestion_BB2)capturedIndex));
            }
            if (i < wordLabels.Length && wordLabels[i] != null)
                wordLabels[i].text = WordText[i];
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[FrameQuestion_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        SetScaleZero(questionText != null ? questionText.rectTransform : null);
        SetScaleZero(answerText   != null ? answerText.rectTransform   : null);
        foreach (var btn in wordButtons)
            SetScaleZero(btn != null ? btn.GetComponent<RectTransform>() : null);
        SetButtonsInteractable(false);

        StartCoroutine(IntroThenLoadQuestion(0));

        Debug.Log("[FrameQuestion_BB2] RestartGame — starting from question 0");
    }

    private IEnumerator IntroThenLoadQuestion(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadQuestionSequence(index, isFirstLoad: true));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Question reveal sequence: narrator → pop content → pop word cards
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadQuestionSequence(int index, bool isFirstLoad)
    {
        SetButtonsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCurrent());

        var data = questions[index];
        if (questionText != null) questionText.text = data.textBeforeGap + "___" + data.textAfterGap;
        if (answerText   != null) answerText.text   = data.answerText;

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
        if (questionText != null) contentRoutines.Add(StartCoroutine(PopIn(questionText.rectTransform)));
        if (answerText   != null) contentRoutines.Add(StartCoroutine(PopIn(answerText.rectTransform)));
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
        if (questionText != null) routines.Add(StartCoroutine(PopOut(questionText.rectTransform)));
        if (answerText   != null) routines.Add(StartCoroutine(PopOut(answerText.rectTransform)));
        foreach (var btn in wordButtons)
            if (btn != null) routines.Add(StartCoroutine(PopOut(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnWordTapped(QuestionWord_FrameQuestion_BB2 tapped)
    {
        var data = questions[_currentIndex];
        if (tapped == data.correctWord)
            StartCoroutine(HandleCorrectTap(tapped));
        else
            StartCoroutine(HandleWrongTap(tapped));
    }

    private IEnumerator HandleCorrectTap(QuestionWord_FrameQuestion_BB2 word)
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        yield return StartCoroutine(FlyWordToGap(word));

        var data = questions[_currentIndex];
        if (questionText != null)
            questionText.text = data.textBeforeGap + WordText[(int)word] + data.textAfterGap;

        if (gapSlot != null) VFXManager.Instance?.SpawnCorrectBurst(gapSlot);

        if (dialogueAudioSource != null && data.fullQuestionAudio != null)
        {
            dialogueAudioSource.clip = data.fullQuestionAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.fullQuestionAudio.length);
        }

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < questions.Length)
            StartCoroutine(LoadQuestionSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllQuestionsComplete());
    }

    private IEnumerator HandleWrongTap(QuestionWord_FrameQuestion_BB2 tapped)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

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

    private IEnumerator FlyWordToGap(QuestionWord_FrameQuestion_BB2 word)
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

    private IEnumerator AllQuestionsComplete()
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
