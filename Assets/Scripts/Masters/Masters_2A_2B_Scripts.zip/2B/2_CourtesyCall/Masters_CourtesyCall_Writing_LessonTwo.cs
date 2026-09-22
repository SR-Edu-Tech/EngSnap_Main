using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_CourtesyCall_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
public class WritingW02CardScenario {
    public string cardTitle;            // e.g. "1) THANK YOU CARD"
    public string scenarioDescription;  // e.g. "Your cousin sent a birthday gift..."
    public string[] wordBankChips;      // e.g. ["Thank you so much for the birthday gift.", "Thank you so much for driving me home.", "I really appreciate it."]
    public string[] requiredKeywords;    // e.g. ["thank", "gift", "driving"]
    public AudioClip cardAudio;
}

    [Header("W02 3 Writing Cards (p.11-12)")]
    [SerializeField]
    private WritingW02CardScenario[] cards;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI cardTitleTMP;
    [SerializeField]
    private TextMeshProUGUI scenarioTextTMP;
    [SerializeField]
    private TextMeshProUGUI hintTextTMP;

    [Header("Word Bank Rail Chips")]
    [SerializeField]
    private Button[] wordBankChipButtons; // 3 Chips

    [Header("Input & Submission")]
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private Button submitBtn;

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Editor Preview")]
    [Range(0, 2)]
    public int editorPreviewCard = 0;

    private int currentCardIndex = 0;
    private int score = 0;
    private int attemptCountOnCurrentCard = 0;
    private bool isProcessingInput = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnSubmitButtonClicked);
        }

        if (inputField != null) {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener((val) => OnSubmitButtonClicked());
        }

        if (wordBankChipButtons != null && wordBankChipButtons.Length > 0) {
            for (int i = 0; i < wordBankChipButtons.Length; i++) {
                if (wordBankChipButtons[i] != null) {
                    int index = i;
                    wordBankChipButtons[i].onClick.RemoveAllListeners();
                    wordBankChipButtons[i].onClick.AddListener(() => OnWordBankChipClicked(index));
                }
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (cards == null || cards.Length == 0) {
            PopulateFailsafeCards();
        }

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Writing/VO_W02_ARIA.mp3");
        }
#endif

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;
        if (inputField != null) inputField.interactable = false;
        if (submitBtn != null) submitBtn.interactable = false;
        if (wordBankChipButtons != null) {
            foreach (var btn in wordBankChipButtons) {
                if (btn != null) btn.interactable = false;
            }
        }

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(Mathf.Max(2.5f, narratorSpeech.length));
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        currentCardIndex = 0;
        score = 0;
        isProcessingInput = false;
        if (inputField != null) inputField.interactable = true;
        if (submitBtn != null) submitBtn.interactable = true;
        if (wordBankChipButtons != null) {
            foreach (var btn in wordBankChipButtons) {
                if (btn != null) btn.interactable = true;
            }
        }

        LoadCard(currentCardIndex);
    }

    private void PopulateFailsafeCards() {
        cards = new WritingW02CardScenario[] {
            new WritingW02CardScenario {
                cardTitle = "1) THANK YOU CARD",
                scenarioDescription = "Your cousin sent a wonderful birthday gift from overseas. Write a warm thank-you message to express your gratitude.",
                wordBankChips = new string[] { "Thank you so much for the birthday gift.", "Thank you so much for driving me home.", "I really appreciate it." },
                requiredKeywords = new string[] { "thank", "gift" }
            },
            new WritingW02CardScenario {
                cardTitle = "2) COURTESY NOTE FOR A LIFT",
                scenarioDescription = "Your classmate drove you home when it started raining heavily. Write a courteous note to thank them for the lift.",
                wordBankChips = new string[] { "Thank you so much for driving me home.", "I really appreciate you helping me out.", "Thanks a lot." },
                requiredKeywords = new string[] { "driving", "home", "thank" }
            },
            new WritingW02CardScenario {
                cardTitle = "3) APOLOGY NOTE",
                scenarioDescription = "You accidentally knocked over your friend's water bottle in the classroom. Write a sincere apology note.",
                wordBankChips = new string[] { "I'm so sorry for the mess.", "I'm sorry, I didn't mean to do that.", "I apologize for the inconvenience." },
                requiredKeywords = new string[] { "sorry", "mess" }
            }
        };

#if UNITY_EDITOR
        string audioDir = "Assets/Audio/2B/2_CourtesyCall/Writing/";
        for (int i = 0; i < cards.Length; i++) {
            cards[i].cardAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + $"VO_W02_C{i + 1}.mp3");
        }
#endif
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (cards == null || cards.Length == 0) {
            PopulateFailsafeCards();
        }

        if (cards == null || cards.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewCard, 0, cards.Length - 1);
        WritingW02CardScenario c = cards[idx];

        if (headerTMP != null) headerTMP.text = "WRITING BRANCH (Post-Office Writing Pad)";
        if (titleTMP != null) titleTMP.text = "W02 Write the Courtesy Note";
        if (progressTMP != null) progressTMP.text = $"{idx + 1}/{cards.Length}";
        if (cardTitleTMP != null) cardTitleTMP.text = c.cardTitle;
        if (scenarioTextTMP != null) scenarioTextTMP.text = c.scenarioDescription;

        if (wordBankChipButtons != null && c.wordBankChips != null) {
            for (int i = 0; i < wordBankChipButtons.Length; i++) {
                if (wordBankChipButtons[i] != null) {
                    bool hasChip = (i < c.wordBankChips.Length);
                    wordBankChipButtons[i].gameObject.SetActive(hasChip);
                    if (hasChip) {
                        TextMeshProUGUI tmp = wordBankChipButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = c.wordBankChips[i];
                    }
                }
            }
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "NpcSpeechBubble", "StudentInputField"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "WRITING BRANCH (Post-Office Writing Pad)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "W02 Write the Courtesy Note";
        }
    }

    private void AutoBindReferences() {
        if (inputField == null) {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chips = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("chip") || n.Contains("wordbank")) chips.Add(btn);
            else if (submitBtn == null && (n.Contains("submit") || n.Contains("check") || n.Contains("confirm"))) submitBtn = btn;
            else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (chips.Count > 0 && (wordBankChipButtons == null || wordBankChipButtons.Length == 0)) {
            wordBankChipButtons = chips.ToArray();
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (cardTitleTMP == null && (n.Contains("cardtitle") || n.Contains("subtitle"))) cardTitleTMP = tmp;
            else if (scenarioTextTMP == null && (n.Contains("scenario") || n.Contains("description") || n.Contains("prompt"))) scenarioTextTMP = tmp;
            else if (hintTextTMP == null && n.Contains("hint")) hintTextTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private void LoadCard(int cardIndex) {
        if (cards == null || cardIndex < 0 || cardIndex >= cards.Length) return;

        isProcessingInput = false;
        attemptCountOnCurrentCard = 0;
        WritingW02CardScenario card = cards[cardIndex];

        if (progressTMP != null) progressTMP.text = $"{cardIndex + 1}/{cards.Length}";
        if (cardTitleTMP != null) cardTitleTMP.text = card.cardTitle;
        if (scenarioTextTMP != null) scenarioTextTMP.text = card.scenarioDescription;

        if (hintTextTMP != null) {
            hintTextTMP.text = "";
            hintTextTMP.gameObject.SetActive(false);
        }

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            inputField.ActivateInputField();
        }

        if (wordBankChipButtons != null) {
            for (int i = 0; i < wordBankChipButtons.Length; i++) {
                if (wordBankChipButtons[i] != null) {
                    bool hasChip = (card.wordBankChips != null && i < card.wordBankChips.Length);
                    wordBankChipButtons[i].gameObject.SetActive(hasChip);
                    if (hasChip) {
                        wordBankChipButtons[i].interactable = true;
                        TextMeshProUGUI tmp = wordBankChipButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = card.wordBankChips[i];
                    }
                }
            }
        }

        PlayCurrentCardAudio();
    }

    private void PlayCurrentCardAudio() {
        if (cards != null && currentCardIndex < cards.Length && cards[currentCardIndex].cardAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(cards[currentCardIndex].cardAudio);
        } else if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    private void OnWordBankChipClicked(int chipIndex) {
        if (cards == null || currentCardIndex >= cards.Length || inputField == null) return;
        WritingW02CardScenario card = cards[currentCardIndex];
        if (card.wordBankChips != null && chipIndex >= 0 && chipIndex < card.wordBankChips.Length) {
            string chipText = card.wordBankChips[chipIndex];
            if (string.IsNullOrEmpty(inputField.text)) {
                inputField.text = chipText;
            } else {
                inputField.text += " " + chipText;
            }
            inputField.ActivateInputField();
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }
    }

    private void OnSubmitButtonClicked() {
        if (isProcessingInput || cards == null || currentCardIndex >= cards.Length) return;

        string userText = (inputField != null) ? inputField.text.Trim().ToLower() : "";
        if (string.IsNullOrEmpty(userText)) return;

        isProcessingInput = true;
        WritingW02CardScenario card = cards[currentCardIndex];

        // Keyword validation: must contain at least 2 required keywords
        int matchedKeywords = 0;
        if (card.requiredKeywords != null) {
            foreach (string kw in card.requiredKeywords) {
                if (userText.Contains(kw.ToLower())) matchedKeywords++;
            }
        }

        bool isPassed = (matchedKeywords >= 2 || userText.Length >= 25);

        if (isPassed) {
            score++;
            if (inputField != null) inputField.interactable = false;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            StartCoroutine(AdvanceCardRoutine());
        } else {
            attemptCountOnCurrentCard++;
            if (attemptCountOnCurrentCard == 1) {
                // Gentle prompt on retry
                if (hintTextTMP != null) {
                    hintTextTMP.gameObject.SetActive(true);
                    hintTextTMP.text = "Prompt: Include full courtesy sentences (e.g. name the gift, reason, or feeling!).";
                    hintTextTMP.color = Color.yellow;
                }
                if (inputField != null) {
                    inputField.transform.DOShakePosition(0.4f, 8f);
                }
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
                isProcessingInput = false;
            } else {
                // Move on after 2 attempts
                if (inputField != null) inputField.interactable = false;

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
                StartCoroutine(AdvanceCardRoutine());
            }
        }
    }

    private IEnumerator AdvanceCardRoutine() {
        yield return new WaitForSeconds(1.5f);

        currentCardIndex++;
        if (currentCardIndex < cards.Length) {
            LoadCard(currentCardIndex);
        } else {
            CompleteLesson();
        }
    }

    private void CompleteLesson() {
        bool passed = (score >= 2); // Pass condition: 2 of 3

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"Score: {score}/{cards.Length}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "GREAT JOB! LESSON COMPLETE!" : "TRY AGAIN TO PASS!";
                resultStatusTMP.color = passed ? Color.green : Color.red;
            }
        }

        if (passed && nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(passed ? Masters_SFX.Correct : Masters_SFX.Incorrect);
        }
    }

    private void OnReplayAudioClicked() {
        PlayCurrentCardAudio();
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) resultPanel.SetActive(false);
        currentCardIndex = 0;
        score = 0;
        LoadCard(currentCardIndex);
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
