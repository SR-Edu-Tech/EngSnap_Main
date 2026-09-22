using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// Unit 3: Household Chores — Writing Lesson One (W01 Complete the Chore Sentence).
/// 13 Cloze fill-in items on verbatim chore sentences with picture clues:
/// 1. Introduction voiceover plays first with input locked.
/// 2. Once introduction finishes, the text input unlocks automatically to allow play.
/// 3. Student types the missing word into the input field (case-insensitive).
/// 4. Correct -> green tick + readback audio; Wrong -> shake + 1st letter hint retry.
/// Pass threshold: >= 8 / 10 items.
/// </summary>
public class Masters_HouseholdChores_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
public class HouseholdChores_WritingW01Item {
    public int itemId;
    public string promptText;           // e.g. "_____ the floor. [picture: broom]"
    public string[] acceptedAnswers;    // e.g. ["sweep", "dust", "mop", "scrub"]
    public string firstLetterHint;      // e.g. "Hint: Starts with 'S...'"
    public string completedSentence;    // e.g. "Sweep the floor."
    public AudioClip readbackAudio;     // Voiceover clip
}

    [Header("13 Cloze Fill-in Items (GDD Table)")]
    [SerializeField] public HouseholdChores_WritingW01Item[] items;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI feedbackTMP;
    [SerializeField] private TextMeshProUGUI hintTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Image inputFieldBg;

    [Header("Chore Desk / Card Object")]
    [SerializeField] private GameObject choreCardObject;
    [SerializeField] private Button replayAudioBtn;

    [Header("Results & Retry Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private AudioClip recapAudio;

    [Header("Pass Threshold")]
    [SerializeField] private int passScore = 8;
    [SerializeField] private int totalQuestionsToPlay = 10;

    private int currentItemIndex = 0;
    private int correctCount = 0;
    private int attemptsOnCurrentItem = 0;
    private bool isCheckingAnswer = false;
    private bool isIntroPhase = true;
    private Coroutine introRoutine;

    private readonly Color defaultInputBgColor = Color.white;
    private readonly Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
    private readonly Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Writing;
        base.Awake();

        AutoBindReferences();
        PopulateDefaultItems();
        WireEventListeners();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Writing;

        AutoBindReferences();
        WireEventListeners();
        EnsureHeaderAndTitle();

        if (items == null || items.Length == 0) {
            PopulateDefaultItems();
        }

        // Lock controls during introduction
        isIntroPhase = true;
        isCheckingAnswer = true;
        SetControlsInteractable(false);

        introRoutine = StartCoroutine(BeginLessonRoutine());
    }

    private IEnumerator BeginLessonRoutine() {
        if (progressTMP != null) progressTMP.text = "Introduction";
        if (promptTMP != null) promptTMP.text = "\"Complete each chore sentence with the missing word!\"";

        AudioClip clipToPlay = introAudio != null ? introAudio : narratorSpeech;
        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            yield return new WaitForSeconds(clipToPlay.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        StartLesson();
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP != null) headerTMP.text = "HOUSEHOLD CHORES 🧹";
        if (titleTMP != null) {
            titleTMP.text = "W01 Complete the Chore Sentence";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f);
        }
        if (subtitleTMP != null) subtitleTMP.text = "Type the missing word to complete each chore sentence!";
    }

    private void SetControlsInteractable(bool interactable) {
        if (inputField != null) inputField.interactable = interactable;
        if (submitButton != null) submitButton.interactable = interactable;
        if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;
    }

    private void WireEventListeners() {
        if (submitButton != null) {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        if (inputField != null) {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener((val) => OnSubmitClicked());
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartLesson);
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

    public void StartLesson() {
        isIntroPhase = false;
        currentItemIndex = 0;
        correctCount = 0;
        attemptsOnCurrentItem = 0;
        isCheckingAnswer = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (choreCardObject != null) choreCardObject.SetActive(true);

        SetControlsInteractable(true);
        ShowItem(0);
    }

    public void RestartLesson() {
        StartLesson();
    }

    public void ShowItem(int index) {
        if (items == null || items.Length == 0) return;
        currentItemIndex = Mathf.Clamp(index, 0, items.Length - 1);
        attemptsOnCurrentItem = 0;
        isCheckingAnswer = false;

        HouseholdChores_WritingW01Item item = items[currentItemIndex];

        UpdateScoreUI();
        EnsureHeaderAndTitle();

        if (promptTMP != null) promptTMP.text = item.promptText;
        if (feedbackTMP != null) feedbackTMP.text = "";
        if (hintTMP != null) hintTMP.gameObject.SetActive(false);

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            inputField.ActivateInputField();
        }

        if (inputFieldBg != null) {
            inputFieldBg.color = defaultInputBgColor;
        }

        if (submitButton != null) {
            submitButton.interactable = true;
        }
    }

    private void UpdateScoreUI() {
        int targetTotal = (totalQuestionsToPlay > 0 && totalQuestionsToPlay <= items.Length) ? totalQuestionsToPlay : items.Length;
        if (progressTMP != null) {
            progressTMP.text = $"{Mathf.Min(currentItemIndex + 1, targetTotal)}/{targetTotal}";
        }
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {correctCount}";
        }
    }

    public void OnSubmitClicked() {
        if (isCheckingAnswer || isIntroPhase) return;
        if (items == null || currentItemIndex >= items.Length) return;

        string typed = inputField != null ? inputField.text.Trim().ToLower() : "";
        if (string.IsNullOrEmpty(typed)) return;

        HouseholdChores_WritingW01Item item = items[currentItemIndex];
        bool isCorrect = false;

        foreach (var accepted in item.acceptedAnswers) {
            if (string.Equals(typed, accepted.Trim().ToLower())) {
                isCorrect = true;
                break;
            }
        }

        StartCoroutine(HandleAnswerEvaluation(isCorrect));
    }

    private IEnumerator HandleAnswerEvaluation(bool isCorrect) {
        isCheckingAnswer = true;
        attemptsOnCurrentItem++;

        HouseholdChores_WritingW01Item item = items[currentItemIndex];

        if (isCorrect) {
            if (attemptsOnCurrentItem == 1) {
                correctCount++;
                UpdateScoreUI();
            }

            if (inputFieldBg != null) inputFieldBg.color = correctColor;
            if (feedbackTMP != null) {
                feedbackTMP.text = "<color=#33D866><b>✓ Correct!</b></color>";
            }

            if (promptTMP != null) {
                promptTMP.text = $"<color=#FFD80D><b>{item.completedSentence}</b></color>";
            }

            if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            yield return new WaitForSeconds(1.8f);

            int targetTotal = (totalQuestionsToPlay > 0 && totalQuestionsToPlay <= items.Length) ? totalQuestionsToPlay : items.Length;
            if (currentItemIndex < targetTotal - 1) {
                ShowItem(currentItemIndex + 1);
            } else {
                ShowResults();
            }
        } else {
            if (inputFieldBg != null) inputFieldBg.color = wrongColor;

            if (inputField != null) {
                inputField.transform.DOShakePosition(0.4f, 10f, 14, 90, false, true);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (attemptsOnCurrentItem == 1) {
                if (feedbackTMP != null) feedbackTMP.text = "<color=#FF6666>Not quite, try again!</color>";
                if (hintTMP != null) {
                    hintTMP.gameObject.SetActive(true);
                    hintTMP.text = item.firstLetterHint;
                }
                if (inputField != null) {
                    inputField.text = "";
                    inputField.ActivateInputField();
                }
                isCheckingAnswer = false;
            } else {
                if (feedbackTMP != null) {
                    feedbackTMP.text = $"<color=#FFAA33>Answer: {item.acceptedAnswers[0]}</color>";
                }

                if (promptTMP != null) {
                    promptTMP.text = $"<color=#FFD80D><b>{item.completedSentence}</b></color>";
                }

                if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                }

                yield return new WaitForSeconds(2.0f);

                int targetTotal = (totalQuestionsToPlay > 0 && totalQuestionsToPlay <= items.Length) ? totalQuestionsToPlay : items.Length;
                if (currentItemIndex < targetTotal - 1) {
                    ShowItem(currentItemIndex + 1);
                } else {
                    ShowResults();
                }
            }
        }
    }

    public void ReplayCurrentAudio() {
        if (isIntroPhase) {
            AudioClip clip = introAudio != null ? introAudio : narratorSpeech;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
            return;
        }

        if (items == null || currentItemIndex >= items.Length) return;
        AudioClip itemClip = items[currentItemIndex].readbackAudio;
        if (itemClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(itemClip);
        }
    }

    private void ShowResults() {
        if (choreCardObject != null) choreCardObject.SetActive(false);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

        int targetTotal = (totalQuestionsToPlay > 0 && totalQuestionsToPlay <= items.Length) ? totalQuestionsToPlay : items.Length;
        bool passed = (correctCount >= passScore);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "WRITING COMPLETE! 🧹" : "KEEP PRACTICING!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"You completed {correctCount} of {targetTotal} chore sentences correctly!";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Success! You know all your household chore sentences." : $"You need at least {passScore}/{targetTotal} correct to pass.";
            }

            if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(passed);
            if (retryBtn != null) retryBtn.gameObject.SetActive(!passed || correctCount < targetTotal);
        }

        if (passed) {
            if (recapAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }

            if (nextButton != null) nextButton.gameObject.SetActive(true);
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    public void OnReturnHubClicked() {
        topic = Masters_Topic.Writing;
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Speaking);
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        if (isIntroPhase) {
            if (introRoutine != null) StopCoroutine(introRoutine);
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.StopVoiceOver();
            StartLesson();
            return;
        }

        OnReturnHubClicked();
    }

    private void AutoBindReferences() {
        if (titleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP == null) {
            Transform t = transform.Find("progression count") ?? transform.Find("ProgressTMP") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (promptTMP == null) {
            Transform t = transform.Find("questions text") ?? transform.Find("DeskCard/PromptTMP") ?? transform.Find("ReceiptCard/PromptTMP") ?? transform.Find("ChoreCard/PromptTMP") ?? transform.Find("PromptTMP");
            if (t != null) promptTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (feedbackTMP == null) {
            Transform t = transform.Find("FeedbackTMP") ?? transform.Find("DeskCard/FeedbackTMP") ?? transform.Find("Feedback");
            if (t != null) feedbackTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (hintTMP == null) {
            Transform t = transform.Find("Hint text") ?? transform.Find("hint/Hint text") ?? transform.Find("HintTMP");
            if (t != null) hintTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (submitButton == null) {
            Transform t = transform.Find("Check") ?? transform.Find("CheckButton") ?? transform.Find("SubmitButton") ?? transform.Find("Image");
            if (t != null) submitButton = t.GetComponent<Button>();
        }
        if (inputField == null) {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }
        if (inputField != null && inputFieldBg == null) {
            inputFieldBg = inputField.GetComponent<Image>();
        }
        if (choreCardObject == null) {
            Transform t = transform.Find("DeskCard") ?? transform.Find("ReceiptCard") ?? transform.Find("QuestionContainer") ?? transform.Find("ChoreCard");
            if (t != null) choreCardObject = t.gameObject;
        }
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

    public void PopulateDefaultItems() {
        string aDir = "Assets/Audio/2B/3_HouseholdChores/Writing/";

        items = new HouseholdChores_WritingW01Item[] {
            new HouseholdChores_WritingW01Item {
                itemId = 1,
                promptText = "_____ the floor. [picture: broom]",
                acceptedAnswers = new string[] { "sweep", "dust", "mop", "scrub" },
                firstLetterHint = "Hint: Starts with 'S...'",
                completedSentence = "Sweep the floor.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item01_sweep.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 2,
                promptText = "Iron the _____ .",
                acceptedAnswers = new string[] { "clothes" },
                firstLetterHint = "Hint: Starts with 'c...'",
                completedSentence = "Iron the clothes.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item02_clothes.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 3,
                promptText = "Make the _____ .",
                acceptedAnswers = new string[] { "bed" },
                firstLetterHint = "Hint: Starts with 'b...'",
                completedSentence = "Make the bed.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item03_bed.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 4,
                promptText = "_____ the table.",
                acceptedAnswers = new string[] { "set" },
                firstLetterHint = "Hint: Starts with 'S...'",
                completedSentence = "Set the table.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item04_set.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 5,
                promptText = "Dry the _____ .",
                acceptedAnswers = new string[] { "dishes" },
                firstLetterHint = "Hint: Starts with 'd...'",
                completedSentence = "Dry the dishes.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item05_dishes.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 6,
                promptText = "Do the _____ . [picture: shopping bags]",
                acceptedAnswers = new string[] { "shopping" },
                firstLetterHint = "Hint: Starts with 's...'",
                completedSentence = "Do the shopping.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item06_shopping.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 7,
                promptText = "Do the _____ . [picture: watering can]",
                acceptedAnswers = new string[] { "gardening" },
                firstLetterHint = "Hint: Starts with 'g...'",
                completedSentence = "Do the gardening.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item07_gardening.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 8,
                promptText = "Take the _____ out.",
                acceptedAnswers = new string[] { "trash" },
                firstLetterHint = "Hint: Starts with 't...'",
                completedSentence = "Take the trash out.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item08_trash.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 9,
                promptText = "_____ the dog.",
                acceptedAnswers = new string[] { "feed" },
                firstLetterHint = "Hint: Starts with 'F...'",
                completedSentence = "Feed the dog.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item09_feed.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 10,
                promptText = "Wash the _____ . [picture: car]",
                acceptedAnswers = new string[] { "car" },
                firstLetterHint = "Hint: Starts with 'c...'",
                completedSentence = "Wash the car.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item10_car.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 11,
                promptText = "Clean the _____ . [picture: window]",
                acceptedAnswers = new string[] { "window" },
                firstLetterHint = "Hint: Starts with 'w...'",
                completedSentence = "Clean the window.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item11_window.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 12,
                promptText = "_____ out the clothes. [picture: washing line]",
                acceptedAnswers = new string[] { "hang" },
                firstLetterHint = "Hint: Starts with 'H...'",
                completedSentence = "Hang out the clothes.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item12_hang.mp3")
#endif
            },
            new HouseholdChores_WritingW01Item {
                itemId = 13,
                promptText = "Wash _____ the dishes.",
                acceptedAnswers = new string[] { "up" },
                firstLetterHint = "Hint: Starts with 'u...'",
                completedSentence = "Wash up the dishes.",
#if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w01_item13_up.mp3")
#endif
            }
        };
    }
}
