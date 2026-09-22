using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// W01 Complete the Idiom
/// Writing desk with a paint-splattered notebook for Book 2B Unit 4 (Colour Your Speech).
/// Each round shows a verbatim idiom or meaning with one word blanked out and a bracketed hint.
/// Student types the missing word into the input field to complete the idiom or meaning.
/// Success condition: Student types the correct word in at least 8 of 10 items (one retry per item).
/// </summary>
public class Masters_2B_ColourYourSpeech_Writing_LessonThree : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_WritingW01ClozeItem {
    public int itemId;
    public string itemWithBlank;        // e.g. "Bite off more than you can _______."
    public string bracketHint;          // e.g. "[idiom]" or "[meaning]"
    public string acceptedAnswer;        // e.g. "chew"
    public string fullCompletedText;    // e.g. "Bite off more than you can chew."
    public string firstLetterHint;      // e.g. "Hint: Starts with 'c' (c _ _ _)"
    public AudioClip itemAudio;
}

    [Header("W01 12 Verbatim Cloze Items")]
    [SerializeField]
    private ColourYourSpeech_WritingW01ClozeItem[] items;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI subtitleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI scoreTMP;

    [Header("Writing Desk Stage & Prompt Card")]
    [SerializeField]
    private GameObject promptCardObject;
    [SerializeField]
    private TextMeshProUGUI itemPromptTMP;
    [SerializeField]
    private TextMeshProUGUI bracketHintTMP;
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Input Field & Submit Button")]
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private Button submitBtn;

    [Header("Feedback / Explanation Banner")]
    [SerializeField]
    private GameObject feedbackBanner;
    [SerializeField]
    private TextMeshProUGUI feedbackTextTMP;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultTitleTMP;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;
    [SerializeField]
    private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField]
    private AudioClip sfxCorrect;
    [SerializeField]
    private AudioClip sfxWrong;

    private int currentItemIndex = 0;
    private int score = 0;
    private int attemptCountOnCurrentItem = 0;
    private bool isHandlingAnswer = false;

    private Color normalInputBorderColor = new Color(0.2f, 0.45f, 0.75f, 1f);
    private Color correctInputBorderColor = new Color(0.13f, 0.65f, 0.32f, 1f);
    private Color wrongInputBorderColor = new Color(0.82f, 0.2f, 0.2f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Writing;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();

        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnSubmitClicked);
        }

        if (inputField != null) {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener((val) => OnSubmitClicked());
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(StartNewGame);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(() => {
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Writing);
                }
            });
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        if (feedbackBanner != null) {
            feedbackBanner.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (items == null || items.Length == 0) {
            PopulateFailsafeItems();
        }

        StartNewGame();
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (items == null || items.Length == 0) {
            PopulateFailsafeItems();
        }

        if (headerTMP != null) headerTMP.text = "WRITING BRANCH (Writing Desk)";
        if (titleTMP != null) titleTMP.text = "W01 Complete the Idiom";
        if (subtitleTMP != null) subtitleTMP.text = "Type the missing word to complete the idiom or its meaning!";
        if (progressTMP != null) progressTMP.text = "Item 1/10";
        if (scoreTMP != null) scoreTMP.text = "Score: 0/10";

        if (items != null && items.Length > 0) {
            if (itemPromptTMP != null) itemPromptTMP.text = $"\"{items[0].itemWithBlank}\"";
            if (bracketHintTMP != null) bracketHintTMP.text = items[0].bracketHint;
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
            subtitleTMP.text = "Type the missing word to complete the idiom or its meaning!";
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
            else if (itemPromptTMP == null && (n.Contains("prompt") || n.Contains("sentence"))) itemPromptTMP = tmp;
            else if (bracketHintTMP == null && (n.Contains("brackethint") || n.Contains("hintbadge"))) bracketHintTMP = tmp;
            else if (feedbackTextTMP == null && n.Contains("feedback")) feedbackTextTMP = tmp;
        }

        if (inputField == null) {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (submitBtn == null && (n.Contains("submit") || n.Contains("check") || n.Contains("enter"))) submitBtn = btn;
            else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("audio") || n.Contains("speaker"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (returnHubBtn == null && (n.Contains("hub") || n.Contains("home"))) returnHubBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (promptCardObject == null) {
            Transform pcTrans = transform.Find("StageCard") ?? transform.Find("WritingStage") ?? transform.Find("PromptCard");
            if (pcTrans != null) promptCardObject = pcTrans.gameObject;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("ResultPopup");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    public void PopulateFailsafeItems() {
        items = new ColourYourSpeech_WritingW01ClozeItem[] {
            // Item 1: Bite off more than you can chew
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 1,
                itemWithBlank = "Bite off more than you can _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "chew",
                fullCompletedText = "Bite off more than you can chew.",
                firstLetterHint = "Hint: Starts with 'c' (c _ _ _)"
            },
            // Item 2: Piece of cake
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 2,
                itemWithBlank = "_______ of cake.",
                bracketHint = "[idiom]",
                acceptedAnswer = "Piece",
                fullCompletedText = "Piece of cake.",
                firstLetterHint = "Hint: Starts with 'P' (P _ _ _ _)"
            },
            // Item 3: Have a blast
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 3,
                itemWithBlank = "Have a _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "blast",
                fullCompletedText = "Have a blast.",
                firstLetterHint = "Hint: Starts with 'b' (b _ _ _ _)"
            },
            // Item 4: Miss the boat
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 4,
                itemWithBlank = "Miss the _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "boat",
                fullCompletedText = "Miss the boat.",
                firstLetterHint = "Hint: Starts with 'b' (b _ _ _)"
            },
            // Item 5: The cat is out of the bag
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 5,
                itemWithBlank = "The _______ is out of the bag.",
                bracketHint = "[idiom]",
                acceptedAnswer = "cat",
                fullCompletedText = "The cat is out of the bag.",
                firstLetterHint = "Hint: Starts with 'c' (c _ _)"
            },
            // Item 6: Raining cats and dogs
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 6,
                itemWithBlank = "Raining cats and _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "dogs",
                fullCompletedText = "Raining cats and dogs.",
                firstLetterHint = "Hint: Starts with 'd' (d _ _ _)"
            },
            // Item 7: Bury the hatchet
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 7,
                itemWithBlank = "Bury the _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "hatchet",
                fullCompletedText = "Bury the hatchet.",
                firstLetterHint = "Hint: Starts with 'h' (h _ _ _ _ _ _)"
            },
            // Item 8: A close shave
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 8,
                itemWithBlank = "A close _______.",
                bracketHint = "[idiom]",
                acceptedAnswer = "shave",
                fullCompletedText = "A close shave.",
                firstLetterHint = "Hint: Starts with 's' (s _ _ _ _)"
            },
            // Item 9: You scratch my back I will scratch yours
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 9,
                itemWithBlank = "You scratch my _______ I will scratch yours.",
                bracketHint = "[idiom]",
                acceptedAnswer = "back",
                fullCompletedText = "You scratch my back I will scratch yours.",
                firstLetterHint = "Hint: Starts with 'b' (b _ _ _)"
            },
            // Item 10: Piece of cake meaning
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 10,
                itemWithBlank = "Piece of cake = a job task or activity that is _______ or simple to do.",
                bracketHint = "[meaning]",
                acceptedAnswer = "easy",
                fullCompletedText = "Piece of cake = a job task or activity that is easy or simple to do.",
                firstLetterHint = "Hint: Starts with 'e' (e _ _ _)"
            },
            // Item 11: Miss the boat meaning
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 11,
                itemWithBlank = "Miss the boat = to miss a _______.",
                bracketHint = "[meaning]",
                acceptedAnswer = "chance",
                fullCompletedText = "Miss the boat = to miss a chance.",
                firstLetterHint = "Hint: Starts with 'c' (c _ _ _ _ _)"
            },
            // Item 12: Raining cats and dogs meaning
            new ColourYourSpeech_WritingW01ClozeItem {
                itemId = 12,
                itemWithBlank = "Raining cats and dogs = raining _______.",
                bracketHint = "[meaning]",
                acceptedAnswer = "heavily",
                fullCompletedText = "Raining cats and dogs = raining heavily.",
                firstLetterHint = "Hint: Starts with 'h' (h _ _ _ _ _ _)"
            }
        };
    }

    public void StartNewGame() {
        currentItemIndex = 0;
        score = 0;
        attemptCountOnCurrentItem = 0;
        isHandlingAnswer = false;

        if (inputField == null || submitBtn == null) {
            AutoBindReferences();
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        UpdateScoreUI();
        LoadItem(currentItemIndex);
    }

    private void LoadItem(int itemIdx) {
        if (items == null || items.Length == 0) return;

        if (itemIdx >= items.Length || itemIdx >= 10) {
            EndGame();
            return;
        }

        var item = items[itemIdx];
        isHandlingAnswer = false;
        attemptCountOnCurrentItem = 0;

        if (progressTMP != null) progressTMP.text = $"Item {itemIdx + 1}/10";

        if (itemPromptTMP != null) {
            itemPromptTMP.text = $"\"{item.itemWithBlank}\"";
            itemPromptTMP.transform.DOKill();
            itemPromptTMP.transform.localScale = Vector3.zero;
            itemPromptTMP.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        if (bracketHintTMP != null) {
            bracketHintTMP.text = item.bracketHint;
        }

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            inputField.ActivateInputField();

            Image inputBg = inputField.GetComponent<Image>();
            if (inputBg != null) inputBg.color = normalInputBorderColor;
        }

        if (submitBtn != null) {
            submitBtn.interactable = true;
            submitBtn.transform.DOKill();
            submitBtn.transform.localScale = Vector3.one;
        }

        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        PlayItemAudio(item);
    }

    private void PlayItemAudio(ColourYourSpeech_WritingW01ClozeItem item) {
        if (item.itemAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(item.itemAudio);
        }
    }

    private void OnSubmitClicked() {
        if (isHandlingAnswer || currentItemIndex >= items.Length) return;

        if (inputField == null) return;
        string userText = inputField.text.Trim();

        if (string.IsNullOrEmpty(userText)) {
            ShowFeedback("Please type the missing word in the field!", false);
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

            if (itemPromptTMP != null) {
                itemPromptTMP.text = $"\"{item.fullCompletedText}\"";
                itemPromptTMP.color = new Color(0.2f, 1f, 0.4f, 1f);
            }

            Image inputBg = inputField.GetComponent<Image>();
            if (inputBg != null) inputBg.color = correctInputBorderColor;

            if (submitBtn != null) {
                submitBtn.transform.DOKill();
                submitBtn.transform.DOScale(Vector3.one * 1.08f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }

            ShowFeedback($"Correct! \"{item.fullCompletedText}\"", true);
            StartCoroutine(AdvanceAfterDelay(2.0f));
        } else {
            attemptCountOnCurrentItem++;

            if (attemptCountOnCurrentItem == 1) {
                // First retry with hint
                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                Image inputBg = inputField.GetComponent<Image>();
                if (inputBg != null) inputBg.color = wrongInputBorderColor;

                if (inputField != null) {
                    inputField.transform.DOShakePosition(0.25f, new Vector3(8f, 0f, 0f), 10, 90, false, true);
                }

                ShowFeedback($"{item.firstLetterHint}. Try once more!", false);
            } else {
                // Second wrong - show answer and move forward
                isHandlingAnswer = true;

                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                Image inputBg = inputField.GetComponent<Image>();
                if (inputBg != null) inputBg.color = wrongInputBorderColor;

                if (itemPromptTMP != null) {
                    itemPromptTMP.text = $"\"{item.fullCompletedText}\"";
                }

                ShowFeedback($"The missing word is: \"{item.acceptedAnswer}\" ({item.fullCompletedText})", false);
                StartCoroutine(AdvanceAfterDelay(2.4f));
            }
        }
    }

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackBanner != null) {
            feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) feedbackTextTMP.text = msg;
            feedbackBanner.transform.DOKill();
            feedbackBanner.transform.localScale = Vector3.zero;
            feedbackBanner.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }
    }

    private void OnReplayAudioClicked() {
        if (currentItemIndex < items.Length) {
            PlayItemAudio(items[currentItemIndex]);
            if (promptCardObject != null) {
                promptCardObject.transform.DOKill();
                promptCardObject.transform.DOScale(Vector3.one * 1.03f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }
        }
    }

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        currentItemIndex++;
        LoadItem(currentItemIndex);
    }

    private void EndGame() {
        bool isPassed = (score >= 8);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = isPassed ? "CLOZE CHALLENGE COMPLETED!" : "CHALLENGE NOT PASSED";
            resultTitleTMP.color = isPassed ? new Color(0.15f, 0.85f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Completed: {score} / 10 Items (Target: 8)";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = isPassed
                ? "Excellent! You mastered completing idioms and their meanings!"
                : "Try again! Complete at least 8 of 10 items to pass!";
        }

        if (isPassed && Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Writing);
        }
    }

    private void UpdateScoreUI() {
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {score}/10";
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

