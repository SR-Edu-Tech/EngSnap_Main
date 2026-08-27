using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum Masters_GrooveOn_QuizQuestionType {
    MCQ,
    Match,
    FillBlank,
    Situation,
    Order,
    OddOneOut,
    TrueFalse
}

[System.Serializable]
public class Masters_GrooveOn_QuizQuestion {
    [Tooltip("Type of question format (MCQ, Match, FillBlank, Situation, Order, OddOneOut, TrueFalse)")]
    public Masters_GrooveOn_QuizQuestionType questionType;
    [Tooltip("Question text displayed at the top prompt.")]
    public string questionText;
    [Tooltip("Voiceover clip played when the question appears.")]
    public AudioClip questionAudioClip;
    [Tooltip("Choice options displayed on the option buttons.")]
    public string[] options;
    [Tooltip("0-based index of the correct option.")]
    public int correctOptionIndex;
    [Tooltip("Optional feedback explanation message.")]
    public string explanation;
}

/// <summary>
/// Modular Game Manager for Unit 6 (Groove On) Quiz Lesson One (Q01 Community Hall Quiz).
/// Features dynamic UI selection based on question format type:
/// - MCQ, Match, FillBlank, Situation, OddOneOut (4 options)
/// - Order, TrueFalse (2 options)
/// Tracks score across 12 questions (requires >= 9/12 to pass and unlock Rewards).
/// </summary>
[ExecuteAlways]
public class Masters_GrooveOn_Quiz_LessonOne : Masters_PolishedCommunication_Quiz_LessonOne {

    protected virtual new void OnEnable() {
        if (Application.isPlaying) {
            EnsureRepeatAudioButton();
        }
    }

    protected virtual new void OnValidate() {
        if (Application.isPlaying) {
            EnsureRepeatAudioButton();
        }
    }

    [Header("Modular Question Bank (Unit 6 Community Hall)")]
    [SerializeField] public Masters_GrooveOn_QuizQuestion[] modularQuestions;

    [Header("Unit 6 Quiz Scoring & Passing Rules")]
    [SerializeField] public int passThreshold = 9;
    [SerializeField] public TextMeshProUGUI questionTypeBadgeTMP;
    [SerializeField] public TextMeshProUGUI feedbackTMP;
    [SerializeField] public TextMeshProUGUI scoreTMP;
    [SerializeField] public TextMeshProUGUI titleTMP;

    public int correctScoreCount = 0;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;
        CleanNonIntroCharacters();
    }

    private void CleanNonIntroCharacters() {
        string[] charNames = new string[] {
            "Character", "LEO", "Leo", "NPCCharacter", "StudentCharacter",
            "NpcAndStudent", "CharacterImage", "Boy", "BoyCharacter",
            "Avatar", "NpcCloud", "StudentCloud"
        };

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren) {
            if (child == null || child == transform) continue;
            foreach (string name in charNames) {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) {
                    child.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }
    [Header("Unit 6 Quiz Repeat Audio")]
[SerializeField] private Button repeatAudioButton;

private void EnsureRepeatAudioButton() {
    if (repeatAudioButton == null) {
        Transform rTrans = transform.Find("RepeatAudioButton") ?? transform.Find("RepeatButton") ?? transform.Find("ReplayButton");
        if (rTrans != null) repeatAudioButton = rTrans.GetComponent<Button>();
    }

    if (repeatAudioButton != null) {
        repeatAudioButton.onClick.RemoveAllListeners();
        repeatAudioButton.onClick.AddListener(ReplayCurrentQuestionAudio);
    }
}
    private bool introPlayed = false;

    protected override void Start() {
        correctScoreCount = 0;
        SyncModularQuestions();
        UpdateTitleAndUIComponents();
        if (titleTMP != null) titleTMP.text = "Unit 6 Quiz";
        if (feedbackTMP != null) feedbackTMP.text = "";
        UpdateScoreDisplay();

        if (narratorSpeech == null) {
#if UNITY_EDITOR
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Quiz/Welcome to the Unit 6 Quiz Test your celebration greeting skills.mp3");
#endif
        }

        AudioClip introClip = narratorSpeech;
        narratorSpeech = null; // Clear so base.Start() doesn't replay it on retry

        base.Start();
        UpdateQuestionCounter();
        StartCoroutine(StartQuizRoutine(introClip));
    }

    private IEnumerator StartQuizRoutine(AudioClip introClip) {
        if (!introPlayed && introClip != null) {
            introPlayed = true;
            Debug.Log($"[Quiz Intro Audio] Playing intro voiceover first: {introClip.name} ({introClip.length}s)");
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introClip);
            }
            yield return new WaitForSeconds(introClip.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.3f);
        }

        PlayCurrentQuestionAudioOnly();
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text;
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Polished") || textVal.Contains("Q01") || textVal.Contains("Quiz") || textVal.Contains("Community")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "Unit 6 Quiz";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("QUIZ")) {
                tmp.text = "QUIZ BRANCH (Community Hall)";
            }
        }
    }

    private void SyncModularQuestions() {
        // If modularQuestions is populated, sync into base quizArray for full component compatibility
        if (modularQuestions != null && modularQuestions.Length > 0) {
            quizArray = new Quiz[modularQuestions.Length];
            for (int i = 0; i < modularQuestions.Length; i++) {
                var q = modularQuestions[i];
                quizArray[i] = new Quiz {
                    question = q.questionText,
                    questionAudioClip = q.questionAudioClip,
                    options = q.options,
                    correctOptionIndex = q.correctOptionIndex
                };
            }
        }
    }

    private void UpdateQuestionCounter() {
        int total = (modularQuestions != null && modularQuestions.Length > 0) ? modularQuestions.Length : (quizArray?.Length ?? 12);
        if (quizCountTMP != null) {
            int displayIndex = Mathf.Min(currentQuizIndex + 1, total);
            quizCountTMP.text = $"Question {displayIndex}/{total}";
        }
    }

    private bool hasFailedCurrentQuestion = false;

    protected override void SetQuiz() {
        hasFailedCurrentQuestion = false;
        int total = (modularQuestions != null && modularQuestions.Length > 0) ? modularQuestions.Length : (quizArray?.Length ?? 12);

        if (currentQuizIndex >= total) {
            // Quiz Completed - Evaluate Score
            Debug.Log($"[Unit 6 Quiz] Completed! Score: {correctScoreCount}/{total}. Required Pass Threshold: {passThreshold}");

            if (correctScoreCount >= passThreshold) {
                // PASS! Unlock & activate Next Button so player clicks it to proceed
                if (nextButton == null) {
                    Transform nbTrans = transform.Find("NextButton") ?? transform.Find("Next Button") ?? transform.Find("Next");
                    if (nbTrans != null) nextButton = nbTrans.GetComponent<Button>();
                }
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                    nextButton.onClick.RemoveAllListeners();
                    nextButton.onClick.AddListener(OnNextButtonClicked);
                    NextButtonAnimation();
                }
            } else {
                // FAIL (< 9/12) - Restart for Retry
                Debug.LogWarning($"[Unit 6 Quiz] Score {correctScoreCount}/{total} below pass mark {passThreshold}. Restarting for retry...");
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
                currentQuizIndex = 0;
                correctScoreCount = 0;
                hasFailedCurrentQuestion = false;
                base.SetQuiz();
                UpdateQuestionCounter();
                ApplyDynamicFormatUI();
                PlayCurrentQuestionAudioOnly();
            }
            return;
        }

        UpdateQuestionCounter();
        base.SetQuiz();
        ShuffleOptionsForCurrentQuestion();
        ApplyDynamicFormatUI();
        PlayCurrentQuestionAudioOnly();
    }

    private void PlayCurrentQuestionAudioOnly() {
        if (Masters_AudioManager.Instance == null) return;

        // Stop any previous audio to prevent double-audio overlap
        Masters_AudioManager.Instance.StopVoiceOver();

        AudioClip clip = null;
        if (modularQuestions != null && currentQuizIndex >= 0 && currentQuizIndex < modularQuestions.Length) {
            clip = modularQuestions[currentQuizIndex]?.questionAudioClip;
        }
        if (clip == null && currentQuiz != null) {
            clip = currentQuiz.questionAudioClip;
        }

        if (clip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }
    }

    protected virtual new void ReplayCurrentQuestionAudio() {
        PlayCurrentQuestionAudioOnly();
    }

    /// <summary>
    /// Randomizes answer option positions for MCQ/mixed questions so correct answer is not always in same position.
    /// </summary>
    private void ShuffleOptionsForCurrentQuestion() {
        if (currentQuiz == null || currentQuiz.options == null || currentQuiz.options.Length <= 1) return;

        List<(string text, bool isCorrect)> optionPairs = new List<(string, bool)>();
        for (int i = 0; i < currentQuiz.options.Length; i++) {
            optionPairs.Add((currentQuiz.options[i], i == currentQuiz.correctOptionIndex));
        }

        // Fisher-Yates Shuffle
        for (int i = optionPairs.Count - 1; i > 0; i--) {
            int randomIndex = Random.Range(0, i + 1);
            var temp = optionPairs[i];
            optionPairs[i] = optionPairs[randomIndex];
            optionPairs[randomIndex] = temp;
        }

        string[] shuffledOptions = new string[optionPairs.Count];
        int newCorrectIndex = 0;
        for (int i = 0; i < optionPairs.Count; i++) {
            shuffledOptions[i] = optionPairs[i].text;
            if (optionPairs[i].isCorrect) {
                newCorrectIndex = i;
            }
        }

        currentQuiz.options = shuffledOptions;
        currentQuiz.correctOptionIndex = newCorrectIndex;
        if (modularQuestions != null && currentQuizIndex < modularQuestions.Length && modularQuestions[currentQuizIndex] != null) {
            modularQuestions[currentQuizIndex].options = shuffledOptions;
            modularQuestions[currentQuizIndex].correctOptionIndex = newCorrectIndex;
        }
    }

    private void ApplyDynamicFormatUI() {
        if (modularQuestions != null && currentQuizIndex < modularQuestions.Length) {
            var mQ = modularQuestions[currentQuizIndex];
            if (mQ != null) {
                ConfigureUIForQuestionType(mQ.questionType);
            }
        } else if (currentQuiz != null) {
            // Fallback inference based on option count
            Masters_GrooveOn_QuizQuestionType inferredType = (currentQuiz.options != null && currentQuiz.options.Length <= 2) ?
                Masters_GrooveOn_QuizQuestionType.TrueFalse : Masters_GrooveOn_QuizQuestionType.MCQ;
            ConfigureUIForQuestionType(inferredType);
        }
    }

    /// <summary>
    /// Dynamically configures answer controls and visual badge based on question type.
    /// </summary>
    public void ConfigureUIForQuestionType(Masters_GrooveOn_QuizQuestionType qType) {
        if (questionTypeBadgeTMP != null) {
            questionTypeBadgeTMP.gameObject.SetActive(true);
            switch (qType) {
                case Masters_GrooveOn_QuizQuestionType.MCQ:
                    questionTypeBadgeTMP.text = "MCQ";
                    break;
                case Masters_GrooveOn_QuizQuestionType.Match:
                    questionTypeBadgeTMP.text = "MATCH GREETING";
                    if (questionTMP != null && currentQuiz != null) {
                        // Ensure matching prompt header is styled clearly
                        if (!questionTMP.text.StartsWith("MATCH:")) {
                            questionTMP.text = $"MATCH: {currentQuiz.question}";
                        }
                    }
                    break;
                case Masters_GrooveOn_QuizQuestionType.FillBlank:
                    questionTypeBadgeTMP.text = "FILL IN THE BLANK";
                    break;
                case Masters_GrooveOn_QuizQuestionType.Situation:
                    questionTypeBadgeTMP.text = "SITUATION";
                    break;
                case Masters_GrooveOn_QuizQuestionType.Order:
                    questionTypeBadgeTMP.text = "ORDER OF PREPARATION";
                    break;
                case Masters_GrooveOn_QuizQuestionType.OddOneOut:
                    questionTypeBadgeTMP.text = "ODD ONE OUT";
                    break;
                case Masters_GrooveOn_QuizQuestionType.TrueFalse:
                    questionTypeBadgeTMP.text = "TRUE / FALSE";
                    break;
            }
        }
    }

    private void UpdateScoreDisplay() {
        if (scoreTMP != null) {
            int total = (modularQuestions != null && modularQuestions.Length > 0) ? modularQuestions.Length : (quizArray?.Length ?? 12);
            scoreTMP.text = $"Score: {correctScoreCount}/{total}";
        }
    }

    private void SetFeedbackText(string msg, Color color) {
        if (feedbackTMP != null) {
            feedbackTMP.text = msg;
            feedbackTMP.color = color;
            feedbackTMP.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
        }
    }

    protected override void OnConfirmButtonClicked() {
        if (!canClickCheckButton || currentlyPressedQuizButton == null) {
            return;
        }

        int targetCorrectIndex = (modularQuestions != null && currentQuizIndex < modularQuestions.Length) ?
            modularQuestions[currentQuizIndex].correctOptionIndex : (currentQuiz != null ? currentQuiz.correctOptionIndex : 0);

        if (currentlyPressedQuizButton.GetButtonIndex() == targetCorrectIndex) {
            // Correct Answer
            if (!hasFailedCurrentQuestion) {
                correctScoreCount++;
            }
            UpdateScoreDisplay();
            SetFeedbackText("Correct!", correctColor);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            if (currentlyPressedQuizButton.GetButtonImage() != null) {
                currentlyPressedQuizButton.GetButtonImage().color = correctColor;
            }

            currentQuizIndex++;
            canClickCheckButton = false;
            canClickOptionButton = false;

            if (nextButton != null) {
                nextButton.interactable = true;
            }

            Invoke("SetQuiz", timeBetweenEachQuizQuestion);
        } else {
            // Incorrect Answer
            hasFailedCurrentQuestion = true;
            SetFeedbackText("Try Again!", incorrectColor);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            if (currentlyPressedQuizButton.GetButton() != null) {
                currentlyPressedQuizButton.GetButton().interactable = false;
            }
            if (currentlyPressedQuizButton.GetButtonImage() != null) {
                currentlyPressedQuizButton.GetButtonImage().color = incorrectColor;
            }
            currentlyPressedQuizButton = null;

            canClickOptionButton = true;
            canClickCheckButton = false;
        }
    }

    [Header("Multi-Pair Matching System")]
    private string selectedLeftItem = "";
    private Masters_QuizButton selectedLeftButton = null;
    private int matchedPairsCount = 0;
    private int totalPairsInCurrentQuestion = 1;

    /// <summary>
    /// Player selects an item on the left column.
    /// </summary>
    public void OnMatchLeftItemSelected(string itemText, Masters_QuizButton button) {
        selectedLeftItem = itemText;
        selectedLeftButton = button;
        if (button != null && button.GetButtonImage() != null) {
            button.GetButtonImage().color = selectedColor;
        }
    }

    /// <summary>
    /// Player selects a greeting on the right column to match with selected left item.
    /// </summary>
    public void OnMatchRightGreetingSelected(string greetingText, Masters_QuizButton button) {
        if (string.IsNullOrEmpty(selectedLeftItem) || selectedLeftButton == null) {
            SetFeedbackText("Select item on left first!", incorrectColor);
            return;
        }

        bool isCorrectMatch = CheckMatchPair(selectedLeftItem, greetingText);
        if (isCorrectMatch) {
            // Correct Pair Match - Lock both items
            matchedPairsCount++;
            if (selectedLeftButton.GetButtonImage() != null) selectedLeftButton.GetButtonImage().color = correctColor;
            if (button.GetButtonImage() != null) button.GetButtonImage().color = correctColor;

            if (selectedLeftButton.GetButton() != null) selectedLeftButton.GetButton().interactable = false;
            if (button.GetButton() != null) button.GetButton().interactable = false;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            SetFeedbackText("Match Found!", correctColor);

            selectedLeftItem = "";
            selectedLeftButton = null;

            if (matchedPairsCount >= totalPairsInCurrentQuestion) {
                // All pairs matched! Award point and unlock Next
                correctScoreCount++;
                UpdateScoreDisplay();
                SetFeedbackText("All Matches Correct!", correctColor);
                if (nextButton != null) nextButton.interactable = true;
                currentQuizIndex++;
                Invoke("SetQuiz", timeBetweenEachQuizQuestion);
            }
        } else {
            // Incorrect Match - Show feedback & allow retry
            if (selectedLeftButton.GetButtonImage() != null) selectedLeftButton.GetButtonImage().color = incorrectColor;
            if (button.GetButtonImage() != null) button.GetButtonImage().color = incorrectColor;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            SetFeedbackText("Try Again!", incorrectColor);

            selectedLeftItem = "";
            selectedLeftButton = null;
        }
    }

    private bool CheckMatchPair(string leftItem, string rightGreeting) {
        if (string.IsNullOrEmpty(leftItem) || string.IsNullOrEmpty(rightGreeting)) return false;
        if (leftItem.IndexOf("Eid", System.StringComparison.OrdinalIgnoreCase) >= 0 && rightGreeting.IndexOf("Eid Mubarak", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (leftItem.IndexOf("Christmas", System.StringComparison.OrdinalIgnoreCase) >= 0 && rightGreeting.IndexOf("Merry Christmas", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (leftItem.IndexOf("Guru", System.StringComparison.OrdinalIgnoreCase) >= 0 && rightGreeting.IndexOf("Gurupurab", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return leftItem.Equals(rightGreeting, System.StringComparison.OrdinalIgnoreCase);
    }
}
