using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// Unit 3 (Household Chores): Quiz Lesson One (Q01 Front Hall Quiz — Household Chores).
/// 12 mixed-format questions covering chore sentences, verb + object pairs, pictures, and dialogue from p.16.
/// Pass mark: 9 / 12.
/// Flow:
/// 1. Intro audio plays -> option buttons are locked.
/// 2. Once intro finishes -> buttons are unlocked and Question 1 begins.
/// 3. Immediate feedback per question.
/// 4. Pass mark >= 9/12 -> Result screen with reward unlock. Fail -> ARIA offers a retry.
/// </summary>
public class Masters_HouseholdChores_Quiz_LessonOne : Masters_Lesson {

[System.Serializable]
public class HouseholdChoresQuizQuestionData {
    public string questionText;
    public string correctOption;
    public string[] distractorOptions;
    public AudioClip questionAudio;
}

    [Header("Q01 12 Mixed-Format Questions")]
    [SerializeField] private HouseholdChoresQuizQuestionData[] questions;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI questionTextTMP;

    [Header("4 Option Buttons")]
    [SerializeField] private Button[] optionButtons; // 4 buttons

    [Header("Audio Controls")]
    [SerializeField] private Button replayAudioBtn;

    [Header("Audio References")]
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private AudioClip recapAudio;

    [Header("Results & Retry Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button returnHubBtn;

    [Header("Next Lesson SO")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Editor Preview")]
    [Range(0, 11)]
    public int editorPreviewRound = 0;

    [Header("Colors & Styling")]
    [SerializeField] private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    [SerializeField] private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
    [SerializeField] private Color wrongColor = new Color(0.85f, 0.22f, 0.22f, 1f);

    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;

    private Image[] optionImages;
    private TextMeshProUGUI[] optionTexts;
    private List<string> currentShuffledChoices = new List<string>();
    private int currentCorrectChoiceIndex = 0;

    protected override void Awake() {
        topic = Masters_Topic.Quiz;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();
        InitQuestionsIfEmpty();
        EnsureHeaderAndTitle();
        WireEventListeners();
    }

    protected override void Start() {
        topic = Masters_Topic.Quiz;
        AutoBindReferences();
        InitQuestionsIfEmpty();
        EnsureHeaderAndTitle();
        WireEventListeners();
        EnsureNextAndBackButtonWired();
        RestartQuiz();
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();
        InitQuestionsIfEmpty();
        EnsureHeaderAndTitle();

        if (questions == null || questions.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, questions.Length - 1);
        HouseholdChoresQuizQuestionData q = questions[idx];

        if (progressTMP != null) {
            progressTMP.text = $"Question {idx + 1}/12";
            progressTMP.enableAutoSizing = false;
            progressTMP.fontSize = 30f;
        }

        if (questionTextTMP != null) {
            questionTextTMP.text = q.questionText;
            questionTextTMP.color = Color.white;
            questionTextTMP.enableAutoSizing = false;
            questionTextTMP.fontSize = 28f;
            questionTextTMP.alignment = TextAlignmentOptions.Center;
            questionTextTMP.enableWordWrapping = true;
        }

        if (optionButtons != null && optionButtons.Length >= 4) {
            string[] previewChoices = new string[4];
            previewChoices[0] = q.correctOption;
            previewChoices[1] = (q.distractorOptions != null && q.distractorOptions.Length > 0) ? q.distractorOptions[0] : "Option B";
            previewChoices[2] = (q.distractorOptions != null && q.distractorOptions.Length > 1) ? q.distractorOptions[1] : "Option C";
            previewChoices[3] = (q.distractorOptions != null && q.distractorOptions.Length > 2) ? q.distractorOptions[2] : "Option D";

            for (int i = 0; i < 4; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].gameObject.SetActive(true);
                    var txt = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) {
                        txt.text = previewChoices[i];
                        txt.color = Color.white;
                        txt.enableAutoSizing = false;
                        txt.fontSize = 22f;
                        txt.alignment = TextAlignmentOptions.Center;
                        txt.enableWordWrapping = true;
                        txt.margin = new Vector4(12, 6, 12, 6);
                    }
                }
            }
        }
    }

    private void PurgeLegacyChildren() {
        for (int i = 0; i < transform.childCount; i++) {
            Transform c = transform.GetChild(i);
            if (c.name.Contains("WordBank") || c.name.Contains("Jumbled") || c.name.Contains("SlateWords")) {
                c.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (titleTMP == null) {
            Transform t = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle") ?? transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("CommonHUD/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.gameObject.SetActive(true);
            titleTMP.text = "Q01 Front Hall Quiz — Household Chores";
            titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f); // Bright Gold
            titleTMP.enableAutoSizing = false;
            titleTMP.fontSize = 36f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontStyle = FontStyles.Bold;
        }

        if (headerTMP == null) {
            Transform t = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("BranchHeader");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.gameObject.SetActive(true);
            headerTMP.text = "FRONT HALL QUIZ";
        }

        if (progressTMP == null) {
            Transform t = transform.Find("HeaderContainer/Progress") ?? transform.Find("ProgressCountTMP") ?? transform.Find("ProgressTMP") ?? transform.Find("progression count") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP") ?? transform.Find("CommonHUD/ProgressTMP");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP != null) {
            progressTMP.gameObject.SetActive(true);
            progressTMP.text = $"Question {currentQuestionIndex + 1}/12";
            progressTMP.enableAutoSizing = false;
            progressTMP.fontSize = 28f;
        }
    }

    private void WireEventListeners() {
        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(ReplayCurrentQuestionAudio);
        }

        if (optionButtons != null && optionButtons.Length > 0) {
            optionImages = new Image[optionButtons.Length];
            optionTexts = new TextMeshProUGUI[optionButtons.Length];

            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int optIdx = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                    optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(optIdx));
                }
            }
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartQuiz);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(OnReturnHubClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    public void RestartQuiz() {
        currentQuestionIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        EnsureHeaderAndTitle();
        UpdateHUD();

        StartCoroutine(PlayIntroThenStart());
    }

    private IEnumerator PlayIntroThenStart() {
        EnableOptionButtons(false);

        AudioClip clip = introAudio != null ? introAudio : narratorSpeech;
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
            yield return new WaitForSeconds(clip.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        ShowQuestion(0);
    }

    private void ShowQuestion(int index) {
        if (questions == null || questions.Length == 0) return;
        currentQuestionIndex = Mathf.Clamp(index, 0, questions.Length - 1);
        isProcessingInput = false;

        HouseholdChoresQuizQuestionData q = questions[currentQuestionIndex];

        UpdateHUD();

        if (questionTextTMP != null) {
            questionTextTMP.text = q.questionText;
            questionTextTMP.color = Color.white;
            questionTextTMP.enableAutoSizing = false;
            questionTextTMP.fontSize = 28f;
            questionTextTMP.alignment = TextAlignmentOptions.Center;
            questionTextTMP.enableWordWrapping = true;
            questionTextTMP.ForceMeshUpdate();
        }

        // Shuffle choices
        currentShuffledChoices.Clear();
        currentShuffledChoices.Add(q.correctOption);
        if (q.distractorOptions != null) {
            foreach (var d in q.distractorOptions) {
                if (!string.IsNullOrEmpty(d)) currentShuffledChoices.Add(d);
            }
        }

        // Fisher-Yates shuffle
        for (int i = 0; i < currentShuffledChoices.Count; i++) {
            string temp = currentShuffledChoices[i];
            int rnd = Random.Range(i, currentShuffledChoices.Count);
            currentShuffledChoices[i] = currentShuffledChoices[rnd];
            currentShuffledChoices[rnd] = temp;
        }

        currentCorrectChoiceIndex = currentShuffledChoices.IndexOf(q.correctOption);

        // Populate option buttons
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] == null) continue;

                if (i < currentShuffledChoices.Count) {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].interactable = true;

                    if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                        optionImages[i].color = defaultChipColor;
                        optionImages[i].transform.localScale = Vector3.one;
                    }

                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                        optionTexts[i].text = currentShuffledChoices[i];
                        optionTexts[i].color = Color.white;
                        optionTexts[i].enableAutoSizing = false;
                        optionTexts[i].fontSize = 22f;
                        optionTexts[i].alignment = TextAlignmentOptions.Center;
                        optionTexts[i].enableWordWrapping = true;
                        optionTexts[i].margin = new Vector4(12, 6, 12, 6);
                        optionTexts[i].ForceMeshUpdate();
                    }
                } else {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        PlayCurrentQuestionAudio();
    }

    private void OnOptionButtonClicked(int clickedIndex) {
        if (isProcessingInput) return;
        if (questions == null || currentQuestionIndex >= questions.Length) return;

        bool isCorrect = (clickedIndex == currentCorrectChoiceIndex);
        StartCoroutine(HandleOptionEvaluation(isCorrect, clickedIndex));
    }

    private IEnumerator HandleOptionEvaluation(bool isCorrect, int clickedIndex) {
        isProcessingInput = true;
        EnableOptionButtons(false);

        if (isCorrect) {
            score++;
            UpdateHUD();

            if (optionImages != null && clickedIndex < optionImages.Length && optionImages[clickedIndex] != null) {
                optionImages[clickedIndex].color = correctColor;
                optionImages[clickedIndex].transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            yield return new WaitForSeconds(1.3f);

            if (currentQuestionIndex + 1 < questions.Length) {
                ShowQuestion(currentQuestionIndex + 1);
            } else {
                EndQuiz();
            }
        } else {
            if (optionImages != null && clickedIndex < optionImages.Length && optionImages[clickedIndex] != null) {
                optionImages[clickedIndex].color = wrongColor;
                optionImages[clickedIndex].transform.DOShakePosition(0.4f, 10f, 20);
            }

            // Highlight the correct answer in green
            if (optionImages != null && currentCorrectChoiceIndex < optionImages.Length && optionImages[currentCorrectChoiceIndex] != null) {
                optionImages[currentCorrectChoiceIndex].color = correctColor;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            yield return new WaitForSeconds(1.5f);

            if (currentQuestionIndex + 1 < questions.Length) {
                ShowQuestion(currentQuestionIndex + 1);
            } else {
                EndQuiz();
            }
        }
    }

    private void PlayCurrentQuestionAudio() {
        if (questions != null && currentQuestionIndex < questions.Length) {
            AudioClip qClip = questions[currentQuestionIndex].questionAudio;
            if (qClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(qClip);
            }
        }
    }

    public void ReplayCurrentQuestionAudio() {
        if (isProcessingInput) return;
        PlayCurrentQuestionAudio();
    }

    private void EnableOptionButtons(bool enable) {
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.interactable = enable;
            }
        }
    }

    private void EndQuiz() {
        isProcessingInput = true;
        EnableOptionButtons(false);

        bool passed = (score >= 9);

        if (resultPanel != null) {
            resultPanel.SetActive(true);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "QUIZ COMPLETE! 🌟" : "KEEP PRACTICING!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.9f, 0.3f, 1f) : new Color(1f, 0.8f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Your Score: {score} / 12";
                resultScoreTMP.enableAutoSizing = false;
                resultScoreTMP.fontSize = 32f;
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? "Great work! You scored 9 or more and unlocked the Reward!"
                    : "You need 9/12 to pass. Tap Retry to try again!";
                resultStatusTMP.enableAutoSizing = false;
                resultStatusTMP.fontSize = 24f;
            }

            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(!passed);
            }

            if (returnHubBtn != null) {
                returnHubBtn.gameObject.SetActive(passed);
            }
        }

        if (passed) {
            if (recapAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
            }
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }
    }

    public void OnReturnHubClicked() {
        topic = Masters_Topic.Quiz;
        if (nextLessonSO != null && Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Quiz;
        if (nextLessonSO != null && Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private void UpdateHUD() {
        if (progressTMP != null) {
            progressTMP.text = $"Question {currentQuestionIndex + 1}/12";
            progressTMP.enableAutoSizing = false;
            progressTMP.fontSize = 28f;
        }
    }

    public void InitQuestionsIfEmpty() {
        string audioDir = "Assets/Audio/2B/3_HouseholdChores/Quiz/";

        if (introAudio == null) {
#if UNITY_EDITOR
            introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_intro.mp3");
#endif
            if (narratorSpeech == null) narratorSpeech = introAudio;
        }

        if (recapAudio == null) {
#if UNITY_EDITOR
            recapAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_recap.mp3");
#endif
        }

        if (questions == null || questions.Length != 12) {
            questions = new HouseholdChoresQuizQuestionData[] {
                // Q1 (Picture)
                new HouseholdChoresQuizQuestionData {
                    questionText = "A pile of creased shirts — which chore?",
                    correctOption = "Iron the clothes.",
                    distractorOptions = new string[] { "Wash the dishes.", "Mop the floor.", "Set the table." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q01.mp3")
#endif
                },
                // Q2 (Picture)
                new HouseholdChoresQuizQuestionData {
                    questionText = "An empty dog bowl — which chore?",
                    correctOption = "Feed the dog.",
                    distractorOptions = new string[] { "Walk the dog.", "Sweep the floor.", "Dust the shelves." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q02.mp3")
#endif
                },
                // Q3 (Match)
                new HouseholdChoresQuizQuestionData {
                    questionText = "Which completes the phrase: 'Set _____ '?",
                    correctOption = "the table",
                    distractorOptions = new string[] { "the floor", "the clothes", "the dog" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q03.mp3")
#endif
                },
                // Q4 (Match)
                new HouseholdChoresQuizQuestionData {
                    questionText = "Which completes the phrase: 'Hang out _____ '?",
                    correctOption = "the clothes",
                    distractorOptions = new string[] { "the dishes", "the table", "the trash" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q04.mp3")
#endif
                },
                // Q5 (Fill)
                new HouseholdChoresQuizQuestionData {
                    questionText = "Fill in the blank: 'Take the _______ out.'",
                    correctOption = "trash",
                    distractorOptions = new string[] { "bed", "dishes", "room" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q05.mp3")
#endif
                },
                // Q6 (Fill)
                new HouseholdChoresQuizQuestionData {
                    questionText = "Fill in the blank: 'Wash _______ the dishes.'",
                    correctOption = "up",
                    distractorOptions = new string[] { "out", "off", "down" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q06.mp3")
#endif
                },
                // Q7 (MCQ)
                new HouseholdChoresQuizQuestionData {
                    questionText = "Which verb goes with 'the shopping'?",
                    correctOption = "Do",
                    distractorOptions = new string[] { "Make", "Wash", "Set" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q07.mp3")
#endif
                },
                // Q8 (MCQ)
                new HouseholdChoresQuizQuestionData {
                    questionText = "Which of these is NOT said about the floor in the book?",
                    correctOption = "'Polish the floor.'",
                    distractorOptions = new string[] { "'Sweep the floor.'", "'Mop the floor.'", "'Scrub the floor.'" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q08.mp3")
#endif
                },
                // Q9 (Odd one out)
                new HouseholdChoresQuizQuestionData {
                    questionText = "Which is NOT a household chore from the book?",
                    correctOption = "'Do the homework.'",
                    distractorOptions = new string[] { "'Make the bed.'", "'Dry the dishes.'", "'Wash the car.'" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q09.mp3")
#endif
                },
                // Q10 (Who said it)
                new HouseholdChoresQuizQuestionData {
                    questionText = "Who said: 'I will wash the car and do a bit of gardening, too.'?",
                    correctOption = "Suho",
                    distractorOptions = new string[] { "Mom", "Neha", "Dad" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q10.mp3")
#endif
                },
                // Q11 (Who said it)
                new HouseholdChoresQuizQuestionData {
                    questionText = "Who said: 'Mom, I will help you in mopping the floor.'?",
                    correctOption = "Neha",
                    distractorOptions = new string[] { "Suho", "Dad", "Grandma" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q11.mp3")
#endif
                },
                // Q12 (T/F)
                new HouseholdChoresQuizQuestionData {
                    questionText = "True or False: In the book, Mom takes the kitchen because it needs a very thorough cleaning.",
                    correctOption = "True",
                    distractorOptions = new string[] { "False" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_q01_q12.mp3")
#endif
                }
            };
        }
    }

    public void AutoBindReferences() {
        if (titleTMP == null) {
            Transform t = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle") ?? transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("CommonHUD/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP == null) {
            Transform t = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("BranchHeader");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP == null) {
            Transform t = transform.Find("HeaderContainer/Progress") ?? transform.Find("ProgressCountTMP") ?? transform.Find("ProgressTMP") ?? transform.Find("progression count") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP") ?? transform.Find("CommonHUD/ProgressTMP");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (questionTextTMP == null) {
            Transform t = transform.Find("QuestionBox/QuestionText") ?? transform.Find("QuestionPrompt/TMP") ?? transform.Find("QuestionText") ?? transform.Find("QuestionTextTMP") ?? transform.Find("PromptTMP") ?? transform.Find("QuestionPrompt") ?? transform.Find("Question");
            if (t != null) questionTextTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Option Buttons
        if (optionButtons == null || optionButtons.Length < 4 || optionButtons[0] == null) {
            List<Button> btns = new List<Button>();
            Transform container = transform.Find("OptionsContainer") ?? transform.Find("optionscontainer") ?? transform.Find("OptionsGrid") ?? transform.Find("ButtonsParent");
            if (container != null) {
                for (int i = 0; i < 4; i++) {
                    Transform c = container.Find($"OptionButton_0{i + 1}") ?? container.Find($"option{i + 1}") ?? container.Find($"Option_{i}") ?? ((i < container.childCount) ? container.GetChild(i) : null);
                    if (c != null) {
                        Button b = c.GetComponent<Button>();
                        if (b != null) btns.Add(b);
                    }
                }
            } else {
                Button[] allBtns = GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns) {
                    if (b.name.ToLower().Contains("option")) btns.Add(b);
                }
            }
            if (btns.Count >= 4) optionButtons = btns.GetRange(0, 4).ToArray();
        }

        // Replay Audio Button
        if (replayAudioBtn == null) {
            Transform t = transform.Find("Audio/ReplayAudioButton") ?? transform.Find("QuestionBox/ReplayAudioButton") ?? transform.Find("ReplayAudioButton") ?? transform.Find("ReplayAudioBtn") ?? transform.Find("SpeakerButton") ?? transform.Find("AudioButton") ?? transform.Find("QuestionPrompt/AudioButton");
            if (t != null) replayAudioBtn = t.GetComponent<Button>();
        }

        // Result Panel
        if (resultPanel == null) {
            Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
            if (t != null) resultPanel = t.gameObject;
        }
        if (resultTitleTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultTitle") ?? resultPanel.transform.Find("Title");
            if (t != null) resultTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (resultScoreTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultScore") ?? resultPanel.transform.Find("Score");
            if (t != null) resultScoreTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (resultStatusTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultStatus") ?? resultPanel.transform.Find("Status");
            if (t != null) resultStatusTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (retryBtn == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
            if (t != null) retryBtn = t.GetComponent<Button>();
        }
        if (returnHubBtn == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("ReturnHubBtn") ?? resultPanel.transform.Find("NextButton");
            if (t != null) returnHubBtn = t.GetComponent<Button>();
        }
    }
}
