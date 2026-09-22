using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// Unit 4 (Colour Your Speech): Writing Lesson One (W01 Complete the Idiom).
/// 12 Cloze fill-in items on verbatim idioms and meanings.
/// 1. Introduction voiceover plays first with input locked.
/// 2. Once introduction finishes, the text input unlocks automatically to allow play.
/// 3. Student types the missing word into the input field (case-insensitive).
/// 4. Correct -> green highlight + readback audio; Wrong -> shake + 1st letter hint retry.
/// Pass threshold: >= 8 / 10 items.
/// </summary>
public class Masters_2B_ColourYourSpeech_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_WritingW01Item {
    public int itemId;
    public string itemWithBlank;        // e.g. "Bite off more than you can _______."
    public string bracketHint;          // e.g. "[idiom]" or "[meaning]"
    public string acceptedAnswer;       // e.g. "chew"
    public string fullCompletedText;    // e.g. "Bite off more than you can chew."
    public string firstLetterHint;      // e.g. "Hint: Starts with 'c' (c _ _ _)"
    public AudioClip itemAudio;
}

    [Header("W01 12 Cloze Fill-in Items")]
    [SerializeField] public ColourYourSpeech_WritingW01Item[] items;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private TextMeshProUGUI itemPromptTMP;
    [SerializeField] private TextMeshProUGUI bracketHintTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI feedbackTMP;
    [SerializeField] private TextMeshProUGUI feedbackTextTMP;
    [SerializeField] private TextMeshProUGUI hintTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button submitBtn;
    [SerializeField] private Image inputFieldBg;

    [Header("Writing Card Object")]
    [SerializeField] private GameObject writingCardObject;
    [SerializeField] private GameObject promptCardObject;
    [SerializeField] private Button replayAudioBtn;

    [Header("Feedback Banner")]
    [SerializeField] private GameObject feedbackBanner;

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
    [SerializeField] private AudioClip sfxCorrect;
    [SerializeField] private AudioClip sfxWrong;

    [Header("Pass Threshold")]
    [SerializeField] private int passScore = 8;
    [SerializeField] private int totalQuestionsToPlay = 10;

    private int currentItemIndex = 0;
    private int score = 0;
    private int attemptsOnCurrentItem = 0;
    private bool isHandlingAnswer = false;
    private bool isIntroPhase = true;

    private readonly Color normalInputBorderColor = new Color(0.2f, 0.45f, 0.75f, 1f);
    private readonly Color correctColor = new Color(0.13f, 0.65f, 0.32f, 1f);
    private readonly Color wrongColor = new Color(0.82f, 0.2f, 0.2f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Writing;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();
        PopulateDefaultItems();
        WireEventListeners();

        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Writing;

        PurgeLegacyChildren();
        AutoBindReferences();
        WireEventListeners();
        EnsureHeaderAndTitle();

        if (items == null || items.Length == 0) {
            PopulateDefaultItems();
        }

        // Lock controls during introduction
        isIntroPhase = true;
        isHandlingAnswer = true;
        SetControlsInteractable(false);

        StartCoroutine(BeginLessonRoutine());
    }

    private IEnumerator BeginLessonRoutine() {
        if (progressTMP != null) progressTMP.text = "Introduction";
        SetPromptText("\"Type the missing word into the box to complete each idiom and its meaning!\"");

        AudioClip clipToPlay = introAudio != null ? introAudio : narratorSpeech;
        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            yield return new WaitForSeconds(clipToPlay.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        StartLesson();
    }

    private void StartLesson() {
        isIntroPhase = false;
        currentItemIndex = 0;
        score = 0;
        attemptsOnCurrentItem = 0;
        isHandlingAnswer = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        UpdateScoreUI();
        LoadItem(currentItemIndex);
    }

    private void LoadItem(int itemIdx) {
        if (items == null || items.Length == 0) return;

        if (itemIdx >= items.Length || itemIdx >= totalQuestionsToPlay) {
            EndGame();
            return;
        }

        var item = items[itemIdx];
        isHandlingAnswer = false;
        attemptsOnCurrentItem = 0;

        if (progressTMP != null) progressTMP.text = $"Item {itemIdx + 1}/{totalQuestionsToPlay}";
        SetPromptText(item.itemWithBlank);
        if (bracketHintTMP != null) bracketHintTMP.text = item.bracketHint;
        if (hintTMP != null) hintTMP.text = "";
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            inputField.ActivateInputField();

            Image inBg = inputField.GetComponent<Image>();
            if (inBg != null) inBg.color = normalInputBorderColor;
        }

        SetControlsInteractable(true);
        PlayItemAudio(item);
    }

    private void SetPromptText(string txt) {
        if (promptTMP != null) promptTMP.text = txt;
        if (itemPromptTMP != null) itemPromptTMP.text = txt;
    }

    private void OnSubmitClicked() {
        if (isHandlingAnswer || isIntroPhase || currentItemIndex >= items.Length) return;

        if (inputField == null) return;
        string userText = inputField.text.Trim();

        if (string.IsNullOrEmpty(userText)) {
            ShowFeedback("Please type the missing word!", false);
            return;
        }

        var item = items[currentItemIndex];
        bool isCorrect = string.Equals(userText, item.acceptedAnswer, System.StringComparison.OrdinalIgnoreCase);

        if (isCorrect) {
            isHandlingAnswer = true;
            score++;
            UpdateScoreUI();

            if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            Image inBg = inputField.GetComponent<Image>();
            if (inBg != null) inBg.color = correctColor;

            SetPromptText(item.fullCompletedText);

            ShowFeedback($"Correct! \"{item.fullCompletedText}\"", true);
            PlayItemAudio(item);
            StartCoroutine(AdvanceAfterDelay(2.5f));
        } else {
            attemptsOnCurrentItem++;

            if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            Image inBg = inputField.GetComponent<Image>();
            if (inBg != null) inBg.color = wrongColor;

            if (inputField != null) {
                inputField.transform.DOShakePosition(0.25f, new Vector3(8f, 0f, 0f), 10, 90, false, true);
            }

            if (attemptsOnCurrentItem == 1) {
                if (hintTMP != null) hintTMP.text = item.firstLetterHint;
                ShowFeedback($"Try again! {item.firstLetterHint}", false);
            } else {
                isHandlingAnswer = true;
                SetPromptText(item.fullCompletedText);
                ShowFeedback($"Completed: \"{item.fullCompletedText}\"", false);
                PlayItemAudio(item);
                StartCoroutine(AdvanceAfterDelay(2.8f));
            }
        }
    }

    private void PlayItemAudio(ColourYourSpeech_WritingW01Item item) {
        if (item.itemAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(item.itemAudio);
        }
    }

    private void ReplayCurrentAudio() {
        if (currentItemIndex < items.Length) {
            PlayItemAudio(items[currentItemIndex]);
        }
    }

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        currentItemIndex++;
        LoadItem(currentItemIndex);
    }

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackBanner != null) {
            feedbackBanner.SetActive(true);
            if (feedbackTMP != null) feedbackTMP.text = msg;
            if (feedbackTextTMP != null) feedbackTextTMP.text = msg;
            feedbackBanner.transform.DOKill();
            feedbackBanner.transform.localScale = Vector3.zero;
            feedbackBanner.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }
    }

    private void SetControlsInteractable(bool interactable) {
        if (inputField != null) inputField.interactable = interactable;
        if (submitButton != null) submitButton.interactable = interactable;
        if (submitBtn != null) submitBtn.interactable = interactable;
        if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;
    }

    private void EndGame() {
        bool isPassed = (score >= passScore);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = isPassed ? "WRITING DESK COMPLETED!" : "WRITING PRACTICE OVER";
            resultTitleTMP.color = isPassed ? new Color(0.15f, 0.85f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Completed: {score} / {totalQuestionsToPlay} (Pass: {passScore})";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = isPassed
                ? "Brilliant! You mastered every idiom and meaning in this writing activity!"
                : "Good attempt! Complete at least 8 items correctly to pass!";
        }

        AudioClip celebrationClip = recapAudio != null ? recapAudio : sfxCorrect;
        if (isPassed && celebrationClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(celebrationClip);
        }

        if (isPassed && Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Writing);
        }
    }

    private void UpdateScoreUI() {
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {score}/{totalQuestionsToPlay}";
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch") ?? transform.Find("Heading");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "WRITING BRANCH (Writing Desk)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "W01 Complete the Idiom";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Type the missing word into the box to complete each idiom and its meaning!";
        }
    }

    private void WireEventListeners() {
        Button sub = submitButton != null ? submitButton : submitBtn;
        if (sub != null) {
            sub.onClick.RemoveAllListeners();
            sub.onClick.AddListener(OnSubmitClicked);
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
            retryBtn.onClick.AddListener(StartLesson);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(() => {
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Writing);
                }
            });
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "SpawnArea", "GamePlayGameObject"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(lTrans.gameObject);
                else DestroyImmediate(lTrans.gameObject);
            }
        }
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("count") || n.Contains("roundcount"))) progressTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (promptTMP == null && (n.Contains("prompt") || n.Contains("statement") || n.Contains("question"))) promptTMP = tmp;
            else if (bracketHintTMP == null && (n.Contains("bracket") || n.Contains("hinttype"))) bracketHintTMP = tmp;
            else if (hintTMP == null && n.Contains("hint")) hintTMP = tmp;
            else if (feedbackTMP == null && (n.Contains("feedback") || n.Contains("banner"))) feedbackTMP = tmp;
        }

        if (inputField == null) {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (submitButton == null && (n.Contains("submit") || n.Contains("check") || n.Contains("enter") || n.Contains("done"))) submitButton = btn;
            else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("audio") || n.Contains("speaker"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (returnHubBtn == null && (n.Contains("hub") || n.Contains("home"))) returnHubBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (writingCardObject == null) {
            Transform cardTrans = transform.Find("PromptCard") ?? transform.Find("WritingCard") ?? transform.Find("CardContainer");
            if (cardTrans != null) writingCardObject = cardTrans.gameObject;
        }

        if (feedbackBanner == null) {
            Transform fb = transform.Find("FeedbackBanner") ?? transform.Find("Feedback");
            if (fb != null) feedbackBanner = fb.gameObject;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("ResultPopup");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    public void PopulateDefaultItems() {
        items = new ColourYourSpeech_WritingW01Item[] {
            new ColourYourSpeech_WritingW01Item {
                itemId = 1,
                itemWithBlank = "Bite off more than you can _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "chew",
                fullCompletedText = "Bite off more than you can chew.",
                firstLetterHint = "Hint: Starts with 'c' (c _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 2,
                itemWithBlank = "To take on a commitment you cannot _______.",
                bracketHint = "[meaning]",
                acceptedAnswer = "handle",
                fullCompletedText = "To take on a commitment you cannot handle.",
                firstLetterHint = "Hint: Starts with 'h' (h _ _ _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 3,
                itemWithBlank = "Have a _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "blast",
                fullCompletedText = "Have a blast.",
                firstLetterHint = "Hint: Starts with 'b' (b _ _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 4,
                itemWithBlank = "To have an enjoyable _______.",
                bracketHint = "[meaning]",
                acceptedAnswer = "time",
                fullCompletedText = "To have an enjoyable time.",
                firstLetterHint = "Hint: Starts with 't' (t _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 5,
                itemWithBlank = "The cat is out of the _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "bag",
                fullCompletedText = "The cat is out of the bag.",
                firstLetterHint = "Hint: Starts with 'b' (b _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 6,
                itemWithBlank = "A secret has been _______.",
                bracketHint = "[meaning]",
                acceptedAnswer = "revealed",
                fullCompletedText = "A secret has been revealed.",
                firstLetterHint = "Hint: Starts with 'r' (r _ _ _ _ _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 7,
                itemWithBlank = "Raining cats and _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "dogs",
                fullCompletedText = "Raining cats and dogs.",
                firstLetterHint = "Hint: Starts with 'd' (d _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 8,
                itemWithBlank = "Raining very _______.",
                bracketHint = "[meaning]",
                acceptedAnswer = "heavily",
                fullCompletedText = "Raining very heavily.",
                firstLetterHint = "Hint: Starts with 'h' (h _ _ _ _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 9,
                itemWithBlank = "A close _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "shave",
                fullCompletedText = "A close shave.",
                firstLetterHint = "Hint: Starts with 's' (s _ _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 10,
                itemWithBlank = "A narrow escape from _______.",
                bracketHint = "[meaning]",
                acceptedAnswer = "danger",
                fullCompletedText = "A narrow escape from danger.",
                firstLetterHint = "Hint: Starts with 'd' (d _ _ _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 11,
                itemWithBlank = "You scratch my back I will scratch _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "yours",
                fullCompletedText = "You scratch my back I will scratch yours.",
                firstLetterHint = "Hint: Starts with 'y' (y _ _ _ _)"
            },
            new ColourYourSpeech_WritingW01Item {
                itemId = 12,
                itemWithBlank = "As you make your bed, so you must _______ in it.",
                bracketHint = "[idiom]",
                acceptedAnswer = "lie",
                fullCompletedText = "As you make your bed, so you must lie in it.",
                firstLetterHint = "Hint: Starts with 'l' (l _ _)"
            }
        };
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

