using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// State Machine enum for Unit 6 Listening L02 "Real or Literal? — Don't Be Fooled".
/// Ensures strict input safety and audio synchronization.
/// </summary>
public enum RoundState
{
    Idle,
    PlayingSentence,
    ShowingOptions,
    AwaitingAnswer,
    ShowingFeedback,
    PlayingReveal,
    Finished
}

/// <summary>
/// Mini-Game Controller for Unit 6 Listening L02 "Real or Literal? — Don't Be Fooled".
/// Manages the two-way literal-vs-figurative discrimination mechanic across 6 rounds:
/// - Left Easel & Chip: LITERAL interpretation (wrong)
/// - Right Easel & Chip: REAL MEANING interpretation (correct)
/// - Audio sequencing: Sentence VO -> Paint Easels -> Wait for Input -> Evaluation -> SFX/Reveal VO.
/// - Scoring: Tracks correctAnswers (>= 5 to pass) and firstTryCorrect.
/// - Progress & Flags: Evaluates Listening = L01 && L02 via M3A_U6_HubProgress and returns to Hub.
/// </summary>
public class RealOrLiteralManager : Masters_Lesson
{
    [Header("DATA — 6 Idiom Rounds")]
    [SerializeField] private RealOrLiteralRoundData[] rounds = new RealOrLiteralRoundData[]
    {
        new RealOrLiteralRoundData
        {
            audioId = "VO_L02_SENT_1",
            sentenceText = "\"Mom will go bananas if she sees my room in this condition.\"",
            realMeaningKey = "Mom will get very cross",
            hintText = "Think about the idiom's figurative meaning! 'Go bananas' means getting very angry or cross, not holding fruit."
        },
        new RealOrLiteralRoundData
        {
            audioId = "VO_L02_SENT_2",
            sentenceText = "\"The exam paper was a piece of cake.\"",
            realMeaningKey = "Very easy",
            hintText = "Think carefully: Was it an actual bakery slice, or was the exam very easy?"
        },
        new RealOrLiteralRoundData
        {
            audioId = "VO_L02_SENT_3",
            sentenceText = "\"She was on cloud nine after being selected as captain.\"",
            realMeaningKey = "Extremely happy",
            hintText = "Focus on figurative meaning: Is she floating in the sky, or feeling extremely happy?"
        },
        new RealOrLiteralRoundData
        {
            audioId = "VO_L02_SENT_4",
            sentenceText = "\"The whole idea is as nutty as a fruitcake.\"",
            realMeaningKey = "Really strange/crazy",
            hintText = "Look beyond the cake! The idiom means an idea is really strange or crazy."
        },
        new RealOrLiteralRoundData
        {
            audioId = "VO_L02_SENT_5",
            sentenceText = "\"She could be all sugar and spice when she wanted to be.\"",
            realMeaningKey = "Kind and sweet",
            hintText = "Remember figurative language: It means behaving in a kind and sweet way!"
        },
        new RealOrLiteralRoundData
        {
            audioId = "VO_L02_SENT_6",
            sentenceText = "\"It was like giving candy to a baby.\"",
            realMeaningKey = "Very easy",
            hintText = "Is someone feeding sweets to an infant, or is the task super easy?"
        }
    };

    [Header("UI — Sentence & Title")]
    [SerializeField] private TextMeshProUGUI sentenceTMP;
    [SerializeField] private TextMeshProUGUI lessonTitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;

    [Header("UI — Left Easel (LITERAL)")]
    [SerializeField] private RectTransform leftEaselTransform;
    [SerializeField] private Image leftEaselImage;
    [SerializeField] private TextMeshProUGUI leftEaselText;
    [SerializeField] private CanvasGroup leftEaselCanvasGroup;
    [SerializeField] private Button leftEaselButton;
    [SerializeField] private Button leftChipButton;

    [Header("UI — Right Easel (REAL MEANING)")]
    [SerializeField] private RectTransform rightEaselTransform;
    [SerializeField] private Image rightEaselImage;
    [SerializeField] private TextMeshProUGUI rightEaselText;
    [SerializeField] private CanvasGroup rightEaselCanvasGroup;
    [SerializeField] private Button rightEaselButton;
    [SerializeField] private Button rightChipButton;

    [Header("UI — Hint & Feedback")]
    [SerializeField] private GameObject hintContainer;
    [SerializeField] private TextMeshProUGUI hintTMP;

    [Header("AUDIO & CONTROLS")]
    [SerializeField] private Button speakerButton;
    [SerializeField] private Button backButton;
    [SerializeField] private AudioClip voAriaIntro;

    [Header("PROGRESS & FLAGS")]
    [SerializeField] private int passThreshold = 5;
    [SerializeField] private bool isL01Completed = true;
    [SerializeField] private bool isL02Completed = false;

    // Runtime State & Scoring
    public RoundState CurrentState { get; private set; } = RoundState.Idle;

    private int currentRound = 0;
    private int correctAnswers = 0;
    private int firstTryCorrect = 0;
    private bool hasRetried = false;
    private Coroutine gameLoopCoroutine;

    protected override void Awake()
    {
        topic = Masters_Topic.Listening;
        AutoBindUIReferences();
        AutoLoadAudioClips();
    }

    private void AutoBindUIReferences()
    {
        if (sentenceTMP == null) sentenceTMP = FindChildComponent<TextMeshProUGUI>("SentenceText");
        if (progressTMP == null) progressTMP = FindChildComponent<TextMeshProUGUI>("ExpressionCountTMP");
        if (lessonTitleTMP == null) lessonTitleTMP = FindChildComponent<TextMeshProUGUI>("LessonTitle");

        if (leftEaselTransform == null) leftEaselTransform = FindChildComponent<RectTransform>("LeftEasel");
        if (rightEaselTransform == null) rightEaselTransform = FindChildComponent<RectTransform>("RightEasel");

        if (leftEaselImage == null && leftEaselTransform != null) leftEaselImage = leftEaselTransform.GetComponentInChildren<Image>();
        if (rightEaselImage == null && rightEaselTransform != null) rightEaselImage = rightEaselTransform.GetComponentInChildren<Image>();

        if (leftEaselText == null && leftEaselTransform != null) leftEaselText = leftEaselTransform.GetComponentInChildren<TextMeshProUGUI>();
        if (rightEaselText == null && rightEaselTransform != null) rightEaselText = rightEaselTransform.GetComponentInChildren<TextMeshProUGUI>();

        if (leftEaselCanvasGroup == null && leftEaselTransform != null)
        {
            leftEaselCanvasGroup = leftEaselTransform.GetComponent<CanvasGroup>();
            if (leftEaselCanvasGroup == null) leftEaselCanvasGroup = leftEaselTransform.gameObject.AddComponent<CanvasGroup>();
        }

        if (rightEaselCanvasGroup == null && rightEaselTransform != null)
        {
            rightEaselCanvasGroup = rightEaselTransform.GetComponent<CanvasGroup>();
            if (rightEaselCanvasGroup == null) rightEaselCanvasGroup = rightEaselTransform.gameObject.AddComponent<CanvasGroup>();
        }

        if (leftEaselButton == null && leftEaselTransform != null) leftEaselButton = leftEaselTransform.GetComponent<Button>();
        if (rightEaselButton == null && rightEaselTransform != null) rightEaselButton = rightEaselTransform.GetComponent<Button>();

        if (leftChipButton == null) leftChipButton = FindChildComponent<Button>("LeftChip");
        if (rightChipButton == null) rightChipButton = FindChildComponent<Button>("RightChip");

        if (hintContainer == null)
        {
            Transform hTrans = transform.Find("HintContainer");
            if (hTrans != null) hintContainer = hTrans.gameObject;
        }
        if (hintTMP == null && hintContainer != null) hintTMP = hintContainer.GetComponentInChildren<TextMeshProUGUI>(true);

        if (speakerButton == null) speakerButton = FindChildComponent<Button>("SpeakerIcon");
        if (backButton == null) backButton = FindChildComponent<Button>("BackButton");
        if (nextButton == null) nextButton = FindChildComponent<Button>("NextButton");

        // Wire easel & chip buttons (both easel OR chip tap trigger option selection)
        if (leftEaselButton != null) { leftEaselButton.onClick.RemoveAllListeners(); leftEaselButton.onClick.AddListener(() => OnAnswerSelected(isRealMeaning: false)); }
        if (leftChipButton != null) { leftChipButton.onClick.RemoveAllListeners(); leftChipButton.onClick.AddListener(() => OnAnswerSelected(isRealMeaning: false)); }

        if (rightEaselButton != null) { rightEaselButton.onClick.RemoveAllListeners(); rightEaselButton.onClick.AddListener(() => OnAnswerSelected(isRealMeaning: true)); }
        if (rightChipButton != null) { rightChipButton.onClick.RemoveAllListeners(); rightChipButton.onClick.AddListener(() => OnAnswerSelected(isRealMeaning: true)); }

        if (speakerButton != null) { speakerButton.onClick.RemoveAllListeners(); speakerButton.onClick.AddListener(OnReplayAudioClicked); }
        if (backButton != null) {
            Masters_BackButton mastersBack = backButton.GetComponent<Masters_BackButton>() ?? backButton.GetComponentInParent<Masters_BackButton>();
            if (mastersBack == null) {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }
        if (nextButton != null) { nextButton.onClick.RemoveAllListeners(); nextButton.onClick.AddListener(OnNextButtonClicked); }
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent.name == targetName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, targetName);
            if (result != null) return result;
        }
        return null;
    }

    private void AutoLoadAudioClips()
    {
#if UNITY_EDITOR
        string folder = "Assets/Audio/3A/6_ColourYourSpeech/Listening/";

        if (voAriaIntro == null)
        {
            voAriaIntro = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(folder + "'Listen for the idiom - what does it really mean'.mp3");
        }

        string[] fileNames = new string[]
        {
            "If I'm late my dad will go bananas.mp3",
            "The exam paper was a piece of cake.mp3",
            "He was on cloud nine when he heard the good news.mp3",
            "You can be such a silly fruitcake sometimes.mp3",
            "Moms are always full of sugar and spice.mp3",
            "He made some tongue-in-cheek comment about his teammates.mp3"
        };

        if (rounds != null)
        {
            for (int i = 0; i < rounds.Length && i < fileNames.Length; i++)
            {
                if (rounds[i] != null && rounds[i].sentenceAudio == null)
                {
                    rounds[i].sentenceAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(folder + fileNames[i]);
                }
            }
        }
#endif
    }

    protected override void Start()
    {
        base.Start();
        if (lessonTitleTMP != null)
        {
            lessonTitleTMP.text = "REAL OR LITERAL? \u2014 DON'T BE FOOLED";
        }
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (hintContainer != null) hintContainer.SetActive(false);
        SetEaselsVisibleAndInteractable(false);

        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine);
        gameLoopCoroutine = StartCoroutine(RunGameLoop());
    }

    private void OnDisable()
    {
        if (gameLoopCoroutine != null)
        {
            StopCoroutine(gameLoopCoroutine);
            gameLoopCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine-based Game Loop enforcing strict audio sequencing and state transitions.
    /// </summary>
    private IEnumerator RunGameLoop()
    {
        CurrentState = RoundState.Idle;
        currentRound = 0;
        correctAnswers = 0;
        firstTryCorrect = 0;

        // Intro sequence (if available)
        if (voAriaIntro != null && Masters_AudioManager.Instance != null)
        {
            CurrentState = RoundState.PlayingSentence;
            Masters_AudioManager.Instance.PlayVoiceOver(voAriaIntro);
            yield return null;
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        }

        while (currentRound < rounds.Length)
        {
            RealOrLiteralRoundData round = rounds[currentRound];
            hasRetried = false;

            UpdateProgressUI();
            if (hintContainer != null) hintContainer.SetActive(false);

            if (sentenceTMP != null) sentenceTMP.text = round.sentenceText;
            SetupEasels(round);

            // 1. Disable BOTH easels during question reading and block input
            SetEaselsVisibleAndInteractable(false);
            CurrentState = RoundState.PlayingSentence;
            PlaySentenceAudio(round);
            yield return new WaitForSeconds(0.2f);
            if (Masters_AudioManager.Instance != null)
            {
                yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
            }

            // 2. Question audio finished -> Restore and Fade in options
            SetEaselsVisibleAndInteractable(true, initialAlpha: 0f);
            CurrentState = RoundState.ShowingOptions;
            yield return StartCoroutine(FadeEasels(1.0f));

            // 3. Await answer input
            CurrentState = RoundState.AwaitingAnswer;

            while (CurrentState == RoundState.AwaitingAnswer)
            {
                yield return null;
            }

            // Waiting while Feedback / Reveal audio finishes
            while (CurrentState == RoundState.ShowingFeedback || CurrentState == RoundState.PlayingReveal)
            {
                yield return null;
            }

            currentRound++;
            yield return new WaitForSeconds(0.4f);
        }

        CurrentState = RoundState.Finished;
        EvaluateLessonCompletion();
    }

    private void SetupEasels(RealOrLiteralRoundData round)
    {
        if (leftEaselImage != null)
        {
            if (round.literalArt != null)
            {
                leftEaselImage.sprite = round.literalArt;
                leftEaselImage.gameObject.SetActive(true);
            }
            else
            {
                // Placeholder fallback if sprite missing
                leftEaselImage.gameObject.SetActive(false);
            }
        }
        if (leftEaselText != null) leftEaselText.text = "LITERAL";

        if (rightEaselImage != null)
        {
            if (round.realMeaningArt != null)
            {
                rightEaselImage.sprite = round.realMeaningArt;
                rightEaselImage.gameObject.SetActive(true);
            }
            else
            {
                rightEaselImage.gameObject.SetActive(false);
            }
        }
        if (rightEaselText != null) rightEaselText.text = round.realMeaningKey;
    }

    private IEnumerator FadeEasels(float duration)
    {
        if (leftEaselCanvasGroup != null) leftEaselCanvasGroup.alpha = 0f;
        if (rightEaselCanvasGroup != null) rightEaselCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (leftEaselCanvasGroup != null) leftEaselCanvasGroup.alpha = t;
            if (rightEaselCanvasGroup != null) rightEaselCanvasGroup.alpha = t;
            yield return null;
        }

        if (leftEaselCanvasGroup != null) leftEaselCanvasGroup.alpha = 1f;
        if (rightEaselCanvasGroup != null) rightEaselCanvasGroup.alpha = 1f;
    }

    private void PlaySentenceAudio(RealOrLiteralRoundData round)
    {
        if (round != null && round.sentenceAudio != null && Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlayVoiceOver(round.sentenceAudio);
        }
    }

    /// <summary>
    /// Answer input handler. Only accepts taps when CurrentState == RoundState.AwaitingAnswer.
    /// </summary>
    public void OnAnswerSelected(bool isRealMeaning)
    {
        if (CurrentState != RoundState.AwaitingAnswer) return;

        CurrentState = RoundState.ShowingFeedback;
        StartCoroutine(ProcessAnswerRoutine(isRealMeaning));
    }

    private IEnumerator ProcessAnswerRoutine(bool isRealMeaning)
    {
        RealOrLiteralRoundData round = rounds[currentRound];

        if (isRealMeaning)
        {
            // CORRECT ANSWER (REAL MEANING)
            correctAnswers++;
            if (!hasRetried)
            {
                firstTryCorrect++;
            }

            if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Visual feedback: Fade out literal easel, punch scale real meaning easel
            if (leftEaselCanvasGroup != null) leftEaselCanvasGroup.DOFade(0.2f, 0.4f);
            if (rightEaselTransform != null) rightEaselTransform.DOPunchScale(Vector3.one * 0.18f, 0.4f);

            yield return new WaitForSeconds(0.5f);

            // Play Reveal VO
            CurrentState = RoundState.PlayingReveal;
            if (round.revealAudio != null && Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlayVoiceOver(round.revealAudio);
                yield return null;
                yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }
        }
        else
        {
            // WRONG ANSWER (LITERAL)
            if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (leftEaselTransform != null)
            {
                leftEaselTransform.DOShakePosition(0.4f, new Vector3(14f, 0, 0));
            }

            if (!hasRetried)
            {
                // Attempt 1 Wrong: Show hint and allow exactly 1 retry
                hasRetried = true;
                if (hintContainer != null)
                {
                    hintContainer.SetActive(true);
                    if (hintTMP != null) hintTMP.text = round.hintText;
                }
                yield return new WaitForSeconds(1.5f);

                // Re-enable answer input for retry
                CurrentState = RoundState.AwaitingAnswer;
                yield break;
            }
            else
            {
                // Attempt 2 Wrong: Highlight real meaning and move on
                if (rightEaselTransform != null) rightEaselTransform.DOPunchScale(Vector3.one * 0.18f, 0.4f);
                yield return new WaitForSeconds(1.2f);
            }
        }

        CurrentState = RoundState.Idle;
    }

    public void SetEaselsVisibleAndInteractable(bool active, float initialAlpha = 1f)
    {
        if (leftEaselCanvasGroup != null)
        {
            leftEaselCanvasGroup.alpha = active ? initialAlpha : 0f;
            leftEaselCanvasGroup.interactable = active;
            leftEaselCanvasGroup.blocksRaycasts = active;
        }
        if (leftEaselTransform != null)
        {
            leftEaselTransform.gameObject.SetActive(active);
        }

        if (rightEaselCanvasGroup != null)
        {
            rightEaselCanvasGroup.alpha = active ? initialAlpha : 0f;
            rightEaselCanvasGroup.interactable = active;
            rightEaselCanvasGroup.blocksRaycasts = active;
        }
        if (rightEaselTransform != null)
        {
            rightEaselTransform.gameObject.SetActive(active);
        }
    }

    private void OnReplayAudioClicked()
    {
        if (CurrentState != RoundState.AwaitingAnswer) return;

        if (currentRound < rounds.Length)
        {
            StartCoroutine(ReplayQuestionRoutine());
        }
    }

    private IEnumerator ReplayQuestionRoutine()
    {
        CurrentState = RoundState.PlayingSentence;
        SetEaselsVisibleAndInteractable(false);

        RealOrLiteralRoundData round = rounds[currentRound];
        PlaySentenceAudio(round);
        yield return new WaitForSeconds(0.2f);
        if (Masters_AudioManager.Instance != null)
        {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        }

        SetEaselsVisibleAndInteractable(true, initialAlpha: 0f);
        yield return StartCoroutine(FadeEasels(0.6f));
        CurrentState = RoundState.AwaitingAnswer;
    }

    private void UpdateProgressUI()
    {
        if (progressTMP != null)
        {
            progressTMP.text = $"{currentRound + 1}/{rounds.Length}";
        }
    }

    /// <summary>
    /// Evaluates pass threshold (>= 5 out of 6), sets L02 sub-flag, checks Listening = L01 && L02,
    /// marks M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Listening), and returns to Hub.
    /// </summary>
    private void EvaluateLessonCompletion()
    {
        bool passed = correctAnswers >= passThreshold;

        if (passed)
        {
            isL02Completed = true;

            // Evaluate overarching Listening flag: Listening = L01 && L02
            bool overallListeningPassed = isL01Completed && isL02Completed;

            if (overallListeningPassed)
            {
                M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Listening);
            }

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(true);
                NextButtonAnimation();
            }

            if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        }
        else
        {
            // Fail (<= 4 correct): Restart from Round 1
            if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            gameLoopCoroutine = StartCoroutine(RunGameLoop());
        }
    }

    private void OnBackButtonClicked()
    {
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    protected override void OnNextButtonClicked()
    {
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Listening);

        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Listening);
        }
    }
}
