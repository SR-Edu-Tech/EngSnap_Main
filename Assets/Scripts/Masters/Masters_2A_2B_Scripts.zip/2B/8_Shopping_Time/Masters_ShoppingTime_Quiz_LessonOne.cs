using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// Q01 Manager's Office Quiz — Shopping Time
/// 12 mixed-format questions covering shops, signs, price talk, and the shopping conversation.
/// Pass mark: 9 / 12.
/// </summary>
public class Masters_ShoppingTime_Quiz_LessonOne : Masters_Lesson {

[System.Serializable]
public class ShoppingTimeQuizQuestionData {
    public string questionText;
    public string correctOption;
    public string[] distractorOptions;
    public AudioClip questionAudio;
}

    [Header("Q01 12 Mixed-Format Questions")]
    [SerializeField] private ShoppingTimeQuizQuestionData[] questions;

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
        ShoppingTimeQuizQuestionData q = questions[idx];

        if (progressTMP != null) {
            progressTMP.text = $"Question {idx + 1}/12";
            progressTMP.enableAutoSizing = false;
            progressTMP.fontSize = 30f;
        }

        if (questionTextTMP != null) {
            questionTextTMP.text = q.questionText;
            questionTextTMP.color = Color.white;
            questionTextTMP.enableAutoSizing = false;
            questionTextTMP.fontSize = 30f;
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
                        txt.enableAutoSizing = true;
                        txt.fontSizeMin = 16f;
                        txt.fontSizeMax = 24f;
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
            Transform t = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.gameObject.SetActive(true);
            titleTMP.text = "Q01 Manager's Office Quiz — Shopping Time";
            titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f); // Bright Gold
            titleTMP.enableAutoSizing = false;
            titleTMP.fontSize = 40f;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.fontStyle = FontStyles.Bold;
        }

        if (headerTMP != null) {
            headerTMP.gameObject.SetActive(true);
            headerTMP.text = "MANAGER'S OFFICE QUIZ 🏬";
        }

        if (progressTMP == null) {
            Transform t = transform.Find("HeaderContainer/Progress") ?? transform.Find("Progress");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP != null) {
            progressTMP.gameObject.SetActive(true);
            progressTMP.text = $"Question {currentQuestionIndex + 1}/12";
            progressTMP.enableAutoSizing = false;
            progressTMP.fontSize = 30f;
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
        // 1. Lock all option buttons during intro speech
        EnableOptionButtons(false);

        AudioClip clip = introAudio != null ? introAudio : narratorSpeech;
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
            yield return new WaitForSeconds(clip.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        // 2. Start Question 1 and enable option buttons
        ShowQuestion(0);
    }

    private void ShowQuestion(int index) {
        if (questions == null || questions.Length == 0) return;
        currentQuestionIndex = Mathf.Clamp(index, 0, questions.Length - 1);
        isProcessingInput = false;

        ShoppingTimeQuizQuestionData q = questions[currentQuestionIndex];

        UpdateHUD();

        if (questionTextTMP != null) {
            questionTextTMP.text = q.questionText;
            questionTextTMP.color = Color.white;
            questionTextTMP.enableAutoSizing = false;
            questionTextTMP.fontSize = 30f;
            questionTextTMP.alignment = TextAlignmentOptions.Center;
            questionTextTMP.enableWordWrapping = true;
            questionTextTMP.ForceMeshUpdate();
        }

        // Shuffle choices and determine correct button index
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

        // Populate option buttons and format text cleanly
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
                        optionTexts[i].enableAutoSizing = true;
                        optionTexts[i].fontSizeMin = 16f;
                        optionTexts[i].fontSizeMax = 24f;
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

        // Play Question Audio
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
                optionImages[clickedIndex].transform.DOShakePosition(0.4f, 8f, 15, 90, false, true);
            }

            // Highlight the correct option in green as teaching feedback
            if (optionImages != null && currentCorrectChoiceIndex < optionImages.Length && optionImages[currentCorrectChoiceIndex] != null) {
                optionImages[currentCorrectChoiceIndex].color = correctColor;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            yield return new WaitForSeconds(1.8f);

            if (currentQuestionIndex + 1 < questions.Length) {
                ShowQuestion(currentQuestionIndex + 1);
            } else {
                EndQuiz();
            }
        }
    }

    public void PlayCurrentQuestionAudio() {
        if (questions == null || currentQuestionIndex >= questions.Length) return;
        AudioClip clip = questions[currentQuestionIndex].questionAudio;
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }
    }

    public void ReplayCurrentQuestionAudio() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        PlayCurrentQuestionAudio();
    }

    private void EnableOptionButtons(bool enable) {
        if (optionButtons == null) return;
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] != null) optionButtons[i].interactable = enable;
        }
    }

    private void EndQuiz() {
        isProcessingInput = false;

        bool passed = (score >= 9);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "QUIZ COMPLETE!" : "KEEP PRACTICING!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"You scored {score}/12! (Pass mark: 9/12)";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? "Excellent job! You mastered the Shopping Time vocabulary and price expressions!"
                    : "Try again to get at least 9 out of 12 questions right to earn your reward!";
            }

            if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(passed);
            if (retryBtn != null) retryBtn.gameObject.SetActive(!passed || score < 12);
        }

        if (recapAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
        }

        if (passed && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    public void OnReturnHubClicked() {
        topic = Masters_Topic.Quiz;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Quiz;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private void UpdateHUD() {
        if (progressTMP != null) {
            progressTMP.text = $"Question {currentQuestionIndex + 1}/12";
            progressTMP.enableAutoSizing = false;
            progressTMP.fontSize = 30f;
        }
    }

    public void InitQuestionsIfEmpty() {
        string audioDir = "Assets/Audio/2B/8_Shopping_Time/Quiz/";

        if (introAudio == null) {
#if UNITY_EDITOR
            introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_intro.mp3");
#endif
            if (narratorSpeech == null) narratorSpeech = introAudio;
        }

        if (recapAudio == null) {
#if UNITY_EDITOR
            recapAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_recap.mp3");
#endif
        }

        if (questions == null || questions.Length != 12) {
            questions = new ShoppingTimeQuizQuestionData[] {
                new ShoppingTimeQuizQuestionData {
                    questionText = "Where would you buy medicine?",
                    correctOption = "Chemist / Pharmacy",
                    distractorOptions = new string[] { "Bakery", "Shoe store", "Butcher's" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q01.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "Where would you buy fish?",
                    correctOption = "Seafood store / Fishmonger's",
                    distractorOptions = new string[] { "Stationery shop", "Bookshop", "Florist" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q02.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "What does 'Half price sale' mean?",
                    correctOption = "Everything costs half of what it usually costs",
                    distractorOptions = new string[] { "Only half the shop is open", "You pay double the price", "Buy one item only" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q03.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "What does 'Cash on delivery' mean?",
                    correctOption = "You pay when the goods arrive",
                    distractorOptions = new string[] { "You pay before ordering online", "Goods are completely free", "You pay with coupons only" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q04.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "Fill in the blank: That's a little outside my _____.",
                    correctOption = "budget",
                    distractorOptions = new string[] { "pocket", "house", "window" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q05.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "Fill in the blank: It cost an _____ and a leg.",
                    correctOption = "arm",
                    distractorOptions = new string[] { "ear", "eye", "head" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q06.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "'What a bargain!' — is the shopper pleased or complaining?",
                    correctOption = "Pleased (a good price)",
                    distractorOptions = new string[] { "Complaining (too dear)" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q07.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "'Overpriced!' — is the shopper pleased or complaining?",
                    correctOption = "Complaining (too dear)",
                    distractorOptions = new string[] { "Pleased (a good price)" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q08.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "Which is the polite way to ask?",
                    correctOption = "Excuse me! How much is this?",
                    distractorOptions = new string[] { "Give me this right now!", "What is the price, hurry up!", "Hey you, take my money!" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q09.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "The T-shirt costs four hundred rupees with a 10% discount. What does Joy pay?",
                    correctOption = "Three hundred and sixty rupees",
                    distractorOptions = new string[] { "Four hundred rupees", "Three hundred rupees", "Forty rupees" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q10.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "Who said: 'Here are a few. Which one would you like to take?'",
                    correctOption = "The Shop Keeper",
                    distractorOptions = new string[] { "Joy", "Leo", "The Doctor" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q11.mp3")
#endif
                },
                new ShoppingTimeQuizQuestionData {
                    questionText = "True or False: 'Fixed price' means the shopkeeper will lower the price if you ask.",
                    correctOption = "False",
                    distractorOptions = new string[] { "True" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_q01_q12.mp3")
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
