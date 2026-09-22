using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_TravelFun_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
public class TravelWritingItem {
    public string sentenceWithBlank;
    public string acceptedAnswer;
    public string[] distractorChoices;
    public AudioClip fullSentenceAudio;
    public AudioClip slowSentenceAudio;
}

    [Header("W01 12 Travel Sentence Cloze Items")]
    [SerializeField]
    private TravelWritingItem[] items;

    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI sentenceTextTMP;
    [SerializeField]
    private TextMeshProUGUI hintTextTMP;

    [Header("Input Controls")]
    [SerializeField]
    private TMP_InputField answerInputField;
    [SerializeField]
    private Button submitBtn;

    [Header("4 Option Choice Buttons (RoundedPillCombined)")]
    [SerializeField]
    private Button[] optionButtons;

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

    [Header("Colors & Styling")]
    [SerializeField]
    private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    [SerializeField]
    private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
    [SerializeField]
    private Color wrongColor = new Color(0.80f, 0.20f, 0.20f, 1f);

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentItemHasRetried = false;

    private Image[] optionImages;
    private TextMeshProUGUI[] optionTexts;
    private string[] currentRoundShuffledChoices;
    private int currentRoundCorrectChoiceIndex;

    private List<int> roundItemIndices;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnSubmitButtonClicked);
        }

        if (answerInputField != null) {
            answerInputField.onSubmit.RemoveAllListeners();
            answerInputField.onSubmit.AddListener((val) => OnSubmitButtonClicked());
        }

        if (optionButtons != null && optionButtons.Length > 0) {
            optionImages = new Image[optionButtons.Length];
            optionTexts = new TextMeshProUGUI[optionButtons.Length];

            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int index = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                    optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
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
        AutoBindReferences();

        StartCoroutine(StartWithIntroRoutine());
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "Sentence tmt", "Rule", "PhraseCardsGrid", "KeepButton", "FixButton",
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter") || n.Contains("expressioncount"))) progressTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
            else if (sentenceTextTMP == null && (n.Contains("sentence") || n.Contains("cloze") || n.Contains("situation"))) sentenceTextTMP = tmp;
            else if (hintTextTMP == null && n.Contains("hint")) hintTextTMP = tmp;
        }

        if (headerTMP != null) headerTMP.text = "WRITING BRANCH (Travel Fun)";
        if (titleTMP != null) titleTMP.text = "W01 Complete the Travel Sentence";

        if (answerInputField == null) {
            answerInputField = GetComponentInChildren<TMP_InputField>(true);
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }

        if (optionButtons == null || optionButtons.Length == 0) {
            List<Button> opts = new List<Button>();
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (bName.Contains("optionbutton") || bName.Contains("chip") || bName.Contains("wordbutton")) {
                    opts.Add(b);
                }
            }
            if (opts.Count > 0) optionButtons = opts.ToArray();
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (submitBtn == null && (n.Contains("submit") || n.Contains("enter") || n.Contains("check"))) submitBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }
    }

    private IEnumerator StartWithIntroRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 3.0f;
            yield return new WaitForSeconds(delay);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        StartLesson();
    }

    private void StartLesson() {
        if (items == null || items.Length == 0) return;

        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        // Select 10 items from pool
        roundItemIndices = new List<int>();
        for (int i = 0; i < items.Length; i++) roundItemIndices.Add(i);
        ShuffleList(roundItemIndices);
        if (roundItemIndices.Count > 10) {
            roundItemIndices = roundItemIndices.GetRange(0, 10);
        }

        LoadRound(0);
    }

    private void LoadRound(int roundIdx) {
        if (roundIdx < 0 || roundIdx >= roundItemIndices.Count) {
            ShowResults();
            return;
        }

        currentRoundIndex = roundIdx;
        currentItemHasRetried = false;
        isProcessingInput = false;

        int itemIdx = roundItemIndices[roundIdx];
        var item = items[itemIdx];

        UpdateProgressDisplay();

        // 1. Display Sentence
        if (sentenceTextTMP != null) {
            sentenceTextTMP.text = item.sentenceWithBlank;
            sentenceTextTMP.transform.DOPunchScale(new Vector3(0.04f, 0.04f, 0f), 0.25f, 4, 0.5f);
        }

        if (hintTextTMP != null) {
            hintTextTMP.text = "";
            hintTextTMP.gameObject.SetActive(false);
        }

        if (answerInputField != null) {
            answerInputField.text = "";
            answerInputField.interactable = true;
            answerInputField.ActivateInputField();
        }

        // 2. Setup 4 Option Buttons
        List<string> choices = new List<string>();
        choices.Add(item.acceptedAnswer);
        if (item.distractorChoices != null) {
            for (int i = 0; i < item.distractorChoices.Length && choices.Count < 4; i++) {
                if (!string.IsNullOrEmpty(item.distractorChoices[i])) {
                    choices.Add(item.distractorChoices[i]);
                }
            }
        }

        ShuffleList(choices);
        currentRoundShuffledChoices = choices.ToArray();
        currentRoundCorrectChoiceIndex = choices.IndexOf(item.acceptedAnswer);

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasChoice = (i < currentRoundShuffledChoices.Length);
                    optionButtons[i].gameObject.SetActive(hasChoice);
                    optionButtons[i].interactable = hasChoice;

                    if (optionImages != null && optionImages[i] != null) {
                        optionImages[i].color = defaultChipColor;
                    }

                    if (optionTexts != null && optionTexts[i] != null && hasChoice) {
                        optionTexts[i].text = currentRoundShuffledChoices[i];
                    }
                }
            }
        }
    }

    private void ShuffleList<T>(IList<T> list) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private void OnOptionSelected(int choiceIndex) {
        if (isProcessingInput || currentRoundShuffledChoices == null || choiceIndex < 0 || choiceIndex >= currentRoundShuffledChoices.Length) return;

        string chosenText = currentRoundShuffledChoices[choiceIndex];
        if (answerInputField != null) {
            answerInputField.text = chosenText;
        }

        EvaluateAnswer(chosenText, choiceIndex);
    }

    private void OnSubmitButtonClicked() {
        if (isProcessingInput) return;

        string typed = answerInputField != null ? answerInputField.text : "";
        EvaluateAnswer(typed, -1);
    }

    private void EvaluateAnswer(string input, int choiceIndex) {
        if (string.IsNullOrWhiteSpace(input)) return;

        int itemIdx = roundItemIndices[currentRoundIndex];
        var item = items[itemIdx];

        string cleanInput = NormalizeText(input);
        string cleanExpected = NormalizeText(item.acceptedAnswer);

        // Check exact match or slash variant (e.g. "pick / up" matches "pick up")
        bool isCorrect = (cleanInput == cleanExpected) ||
                         (cleanExpected.Contains("/") && cleanInput == cleanExpected.Replace("/", "").Replace(" ", "")) ||
                         (cleanInput == cleanExpected.Replace(" ", ""));

        StartCoroutine(ProcessResultRoutine(isCorrect, choiceIndex, item));
    }

    private string NormalizeText(string s) {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Trim().ToLower().Replace("’", "'").Replace("  ", " ");
    }

    private IEnumerator ProcessResultRoutine(bool isCorrect, int choiceIdx, TravelWritingItem item) {
        isProcessingInput = true;

        if (isCorrect) {
            // Correct Answer!
            score++;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Green highlight on chosen button
            if (choiceIdx >= 0 && optionImages != null && choiceIdx < optionImages.Length && optionImages[choiceIdx] != null) {
                optionImages[choiceIdx].color = correctColor;
                optionButtons[choiceIdx].transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.3f, 5, 0.5f);
            }

            // Show full completed sentence
            if (sentenceTextTMP != null) {
                sentenceTextTMP.text = item.sentenceWithBlank.Replace("______", $"<b><color=#40E0D0>{item.acceptedAnswer}</color></b>");
            }

            // Play Full Sentence Audio
            if (item.fullSentenceAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(item.fullSentenceAudio);
                float delay = item.fullSentenceAudio.length > 0 ? item.fullSentenceAudio.length : 2.5f;
                yield return new WaitForSeconds(delay + 0.3f);
            } else {
                yield return new WaitForSeconds(1.2f);
            }

            isProcessingInput = false;
            LoadRound(currentRoundIndex + 1);
        } else {
            // Wrong Answer!
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            // Red flash on chosen button
            if (choiceIdx >= 0 && optionImages != null && choiceIdx < optionImages.Length && optionImages[choiceIdx] != null) {
                optionImages[choiceIdx].color = wrongColor;
                optionButtons[choiceIdx].transform.DOShakePosition(0.35f, new Vector3(10f, 0f, 0f), 10, 90f);
            }

            if (answerInputField != null) {
                answerInputField.transform.DOShakePosition(0.35f, new Vector3(10f, 0f, 0f), 10, 90f);
            }

            yield return new WaitForSeconds(0.45f);

            // Reset chip color
            if (choiceIdx >= 0 && optionImages != null && choiceIdx < optionImages.Length && optionImages[choiceIdx] != null) {
                optionImages[choiceIdx].color = defaultChipColor;
            }

            if (!currentItemHasRetried) {
                // One retry with first-letter hint
                currentItemHasRetried = true;
                if (hintTextTMP != null) {
                    hintTextTMP.gameObject.SetActive(true);
                    char firstChar = char.ToUpper(item.acceptedAnswer[0]);
                    hintTextTMP.text = $"Hint: starts with '<b>{firstChar}</b>' ({item.acceptedAnswer.Length} letters)";
                    hintTextTMP.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.25f, 5, 0.5f);
                }

                if (answerInputField != null) {
                    answerInputField.text = "";
                    answerInputField.ActivateInputField();
                }

                isProcessingInput = false;
            } else {
                // Second mistake -> move to next round
                yield return new WaitForSeconds(0.4f);
                isProcessingInput = false;
                LoadRound(currentRoundIndex + 1);
            }
        }
    }

    private void UpdateProgressDisplay() {
        if (progressTMP != null) {
            int total = (roundItemIndices != null) ? roundItemIndices.Count : 10;
            progressTMP.text = $"{currentRoundIndex + 1}/{total}";
        }
    }

    private void OnReplayAudioClicked() {
        if (isProcessingInput || roundItemIndices == null || currentRoundIndex >= roundItemIndices.Count) return;

        int itemIdx = roundItemIndices[currentRoundIndex];
        var item = items[itemIdx];

        if (item.fullSentenceAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(item.fullSentenceAudio);
        } else if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    private void ShowResults() {
        isProcessingInput = true;

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        int total = (roundItemIndices != null) ? roundItemIndices.Count : 10;

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Score: {score} / {total}";
        }

        bool passed = score >= 8; // Success condition: at least 8 of 10 items

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ?
                "“Terrific! You completed the travel sentences with flying colours!”" :
                "“Good effort! Try again to complete at least 8 of the 10 sentences correctly!”";
            resultStatusTMP.color = passed ? new Color(0.2f, 0.9f, 0.4f) : new Color(1f, 0.6f, 0.2f);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(false);
            }
        } else {
            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(true);
                retryBtn.interactable = true;
            }
            if (nextButton != null) {
                nextButton.gameObject.SetActive(false);
            }
        }
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
        StartLesson();
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

