using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// W02 Write the Checking Questions Controller for Book 2B Unit 7 (Question Tags).
/// Displays 3 guided writing note-cards (Checking a Fact, Before the Exam, Asking Gently).
/// Features 2-line input per card with a Word Bank rail.
/// When student types an answer, wrong answers trigger the Hint Box with the correct tag and rules.
/// Completing this lesson unlocks the Speaking branch.
/// </summary>
public class Masters_QuestionTags_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
public class QuestionTags_WritingW02Card {
    public int cardId;
    public string categoryTitle;        // e.g. "CHECKING A FACT"
    public string scenarioText;          // e.g. "A new classmate joins your class. Write TWO tag questions..."
    public AudioClip scenarioAudio;

    [Header("Line 1")]
    public string line1Statement;       // e.g. "You are a student,"
    public string line1AcceptedTag;     // e.g. "aren't you?"
    public string[] line1AltTags;
    public string line1FullEcho;        // e.g. "You are a student, aren't you?"
    public string line1Hint;
    public AudioClip line1Audio;

    [Header("Line 2")]
    public string line2Statement;       // e.g. "You speak English,"
    public string line2AcceptedTag;     // e.g. "don't you?"
    public string[] line2AltTags;
    public string line2FullEcho;        // e.g. "You speak English, don't you?"
    public string line2Hint;
    public AudioClip line2Audio;

    [Header("Word Bank")]
    public string[] wordBankChips;      // e.g. { "aren't", "don't", "you", "they" }
}

    [Header("3 Note Card Scenarios")]
    [SerializeField]
    private QuestionTags_WritingW02Card[] cards;

    [Header("UI Headers & Counters")]
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
    [SerializeField]
    private TextMeshProUGUI hintTMP;

    [Header("Note Card Container")]
    [SerializeField]
    private GameObject noteCardObject;
    [SerializeField]
    private Image noteCardBg;
    [SerializeField]
    private TextMeshProUGUI categoryTitleTMP;
    [SerializeField]
    private TextMeshProUGUI scenarioTextTMP;
    [SerializeField]
    private Button replayScenarioAudioBtn;

    [Header("Line 1 Inputs")]
    [SerializeField]
    private TextMeshProUGUI line1StatementTMP;
    [SerializeField]
    private TMP_InputField line1InputField;
    [SerializeField]
    private Image line1InputBg;

    [Header("Line 2 Inputs")]
    [SerializeField]
    private TextMeshProUGUI line2StatementTMP;
    [SerializeField]
    private TMP_InputField line2InputField;
    [SerializeField]
    private Image line2InputBg;

    [Header("Word Bank Rail")]
    [SerializeField]
    private GameObject wordBankContainer;
    [SerializeField]
    private TextMeshProUGUI wordBankLabelTMP;
    [SerializeField]
    private Button[] wordBankButtons;
    [SerializeField]
    private TextMeshProUGUI[] wordBankTexts;

    [Header("Submission")]
    [SerializeField]
    private Button submitBtn;

    [Header("Hint Box (Shows Correct Answers & Grammar Rules on Wrong)")]
    [SerializeField]
    private GameObject hintBoxObject;
    [SerializeField]
    private TextMeshProUGUI hintTitleTMP;
    [SerializeField]
    private TextMeshProUGUI hintAnswerTMP;
    [SerializeField]
    private TextMeshProUGUI hintRuleTMP;

    [Header("Results Panel")]
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
    private AudioClip introAudio;

    [Header("Navigation")]
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    [Header("Pass Threshold")]
    [SerializeField]
    private int passThreshold = 2; // At least 2 of 3 cards

    private int currentCardIndex = 0;
    private int score = 0;
    private bool isHandlingSubmission = false;
    private TMP_InputField activeField = null;

    private readonly Color defaultInputColor = new Color(0.12f, 0.16f, 0.28f, 1f);
    private readonly Color correctColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private readonly Color wrongColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Writing;
        base.Awake();

        AutoBindReferences();

        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnSubmitClicked);
        }

        if (replayScenarioAudioBtn != null) {
            replayScenarioAudioBtn.onClick.RemoveAllListeners();
            replayScenarioAudioBtn.onClick.AddListener(PlayCurrentScenarioAudio);
        }

        if (line1InputField != null) {
            line1InputField.onSelect.AddListener((s) => activeField = line1InputField);
        }

        if (line2InputField != null) {
            line2InputField.onSelect.AddListener((s) => activeField = line2InputField);
        }

        if (wordBankButtons != null) {
            for (int i = 0; i < wordBankButtons.Length; i++) {
                int idx = i;
                if (wordBankButtons[i] != null) {
                    wordBankButtons[i].onClick.RemoveAllListeners();
                    wordBankButtons[i].onClick.AddListener(() => OnWordBankChipClicked(idx));
                }
            }
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

    protected override void Start() {
        base.Start();

        EnsureHeaderAndTitle();

        if (cards == null || cards.Length == 0) {
            PopulateDefaultCards();
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        if (hintBoxObject != null) hintBoxObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        StartCoroutine(InitializeLessonRoutine());
    }

    private IEnumerator InitializeLessonRoutine() {
        SetInteractiveState(false);

        if (Masters_AudioManager.Instance != null && introAudio != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(0.3f);
        StartLessonSequence();
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP != null) headerTMP.text = "WRITING BRANCH (Checking Questions)";
        if (titleTMP != null) titleTMP.text = "W02 Write the Checking Questions";
        if (subtitleTMP != null) subtitleTMP.text = "Write tag questions for each situation. Use the word bank to help you.";
        if (hintTMP != null) hintTMP.text = "See-Saw Rule: [+] Statement -> [-] Tag | [-] Statement -> [+] Tag";
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (hintTMP == null && (n.Contains("hint") || n.Contains("seesaw"))) hintTMP = tmp;
            else if (categoryTitleTMP == null && (n.Contains("category") || n.Contains("cardtitle"))) categoryTitleTMP = tmp;
            else if (scenarioTextTMP == null && (n.Contains("scenario") || n.Contains("desc"))) scenarioTextTMP = tmp;
            else if (line1StatementTMP == null && n.Contains("line1statement")) line1StatementTMP = tmp;
            else if (line2StatementTMP == null && n.Contains("line2statement")) line2StatementTMP = tmp;
            else if (hintTitleTMP == null && n.Contains("hinttitle")) hintTitleTMP = tmp;
            else if (hintAnswerTMP == null && n.Contains("hintanswer")) hintAnswerTMP = tmp;
            else if (hintRuleTMP == null && n.Contains("hintrule")) hintRuleTMP = tmp;
        }

        TMP_InputField[] fields = GetComponentsInChildren<TMP_InputField>(true);
        if (fields.Length >= 2) {
            line1InputField = fields[0];
            line1InputBg = fields[0].GetComponent<Image>();
            line2InputField = fields[1];
            line2InputBg = fields[1].GetComponent<Image>();
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chips = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (submitBtn == null && (n.Contains("submit") || n.Contains("check"))) {
                submitBtn = btn;
            } else if (replayScenarioAudioBtn == null && (n.Contains("replay") || n.Contains("audio") || n.Contains("listen"))) {
                replayScenarioAudioBtn = btn;
            } else if (n.Contains("chip") || n.Contains("wordbank")) {
                chips.Add(btn);
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (returnHubBtn == null && (n.Contains("hub") || n.Contains("complete"))) {
                returnHubBtn = btn;
            } else if (nextButton == null && n == "nextbutton") {
                nextButton = btn;
            }
        }

        if (chips.Count > 0) {
            wordBankButtons = chips.ToArray();
            wordBankTexts = new TextMeshProUGUI[chips.Count];
            for (int i = 0; i < chips.Count; i++) {
                wordBankTexts[i] = chips[i].GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    public void PopulateDefaultCards() {
        cards = new QuestionTags_WritingW02Card[] {
            // Card 1
            new QuestionTags_WritingW02Card {
                cardId = 1,
                categoryTitle = "CARD 1: CHECKING A FACT",
                scenarioText = "A new classmate joins your class. Write TWO tag questions to check what you think you know:",
                line1Statement = "You are a student,",
                line1AcceptedTag = "aren't you?",
                line1AltTags = new string[] { "aren't you", "arent you", "arent you?" },
                line1FullEcho = "You are a student, aren't you?",
                line1Hint = "Positive statement [+] 'You are' takes negative tag [-] 'aren't you?'",
                line2Statement = "You speak English,",
                line2AcceptedTag = "don't you?",
                line2AltTags = new string[] { "don't you", "dont you", "dont you?" },
                line2FullEcho = "You speak English, don't you?",
                line2Hint = "Positive statement [+] 'You speak' takes negative tag [-] 'don't you?'",
                wordBankChips = new string[] { "aren't", "don't", "you", "they" }
            },
            // Card 2
            new QuestionTags_WritingW02Card {
                cardId = 2,
                categoryTitle = "CARD 2: BEFORE THE EXAM",
                scenarioText = "You are worried about a friend. Write TWO tag questions checking she is ready:",
                line1Statement = "You studied for the test,",
                line1AcceptedTag = "didn't you?",
                line1AltTags = new string[] { "didn't you", "didnt you", "didnt you?" },
                line1FullEcho = "You studied for the test, didn't you?",
                line1Hint = "Past tense [+] 'studied' takes past negative tag [-] 'didn't you?'",
                line2Statement = "You won't fail the exam,",
                line2AcceptedTag = "will you?",
                line2AltTags = new string[] { "will you", "will you?" },
                line2FullEcho = "You won't fail the exam, will you?",
                line2Hint = "Negative modal [-] 'won't' takes positive tag [+] 'will you?'",
                wordBankChips = new string[] { "didn't", "will", "won't", "you" }
            },
            // Card 3
            new QuestionTags_WritingW02Card {
                cardId = 3,
                categoryTitle = "CARD 3: ASKING GENTLY",
                scenarioText = "You need a favour and don't want to sound rude. Write TWO tag questions that ask softly:",
                line1Statement = "You couldn't do it for me,",
                line1AcceptedTag = "could you?",
                line1AltTags = new string[] { "could you", "could you?" },
                line1FullEcho = "You couldn't do it for me, could you?",
                line1Hint = "Negative modal [-] 'couldn't' takes positive tag [+] 'could you?'",
                line2Statement = "You wouldn't stop me,",
                line2AcceptedTag = "would you?",
                line2AltTags = new string[] { "would you", "would you?" },
                line2FullEcho = "You wouldn't stop me, would you?",
                line2Hint = "Negative modal [-] 'wouldn't' takes positive tag [+] 'would you?'",
                wordBankChips = new string[] { "could", "would", "you", "can" }
            }
        };
    }

    public void StartLessonSequence() {
        currentCardIndex = 0;
        score = 0;
        isHandlingSubmission = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (hintBoxObject != null) hintBoxObject.SetActive(false);
        UpdateScoreDisplay();
        LoadCard(currentCardIndex);
    }

    public void LoadCard(int cardIdx) {
        if (cards == null || cards.Length == 0) return;
        if (cardIdx < 0 || cardIdx >= cards.Length) {
            ShowResults();
            return;
        }

        currentCardIndex = cardIdx;
        isHandlingSubmission = false;
        var card = cards[currentCardIndex];

        if (progressTMP != null) progressTMP.text = $"Card {cardIdx + 1}/{cards.Length}";

        if (categoryTitleTMP != null) categoryTitleTMP.text = card.categoryTitle;
        if (scenarioTextTMP != null) scenarioTextTMP.text = card.scenarioText;

        if (line1StatementTMP != null) line1StatementTMP.text = $"1. {card.line1Statement}";
        if (line2StatementTMP != null) line2StatementTMP.text = $"2. {card.line2Statement}";

        if (line1InputField != null) {
            line1InputField.text = "";
            line1InputField.interactable = true;
            if (line1InputBg != null) line1InputBg.color = defaultInputColor;
        }

        if (line2InputField != null) {
            line2InputField.text = "";
            line2InputField.interactable = true;
            if (line2InputBg != null) line2InputBg.color = defaultInputColor;
        }

        activeField = line1InputField;
        if (line1InputField != null) {
            line1InputField.Select();
            line1InputField.ActivateInputField();
        }

        if (hintBoxObject != null) hintBoxObject.SetActive(false);
        if (submitBtn != null) submitBtn.interactable = true;

        // Word Bank Setup
        if (wordBankButtons != null && card.wordBankChips != null) {
            for (int i = 0; i < wordBankButtons.Length; i++) {
                if (i < card.wordBankChips.Length && wordBankButtons[i] != null) {
                    wordBankButtons[i].gameObject.SetActive(true);
                    wordBankButtons[i].interactable = true;
                    if (wordBankTexts != null && i < wordBankTexts.Length && wordBankTexts[i] != null) {
                        wordBankTexts[i].text = card.wordBankChips[i];
                    }
                } else if (wordBankButtons[i] != null) {
                    wordBankButtons[i].gameObject.SetActive(false);
                }
            }
        }

        if (noteCardObject != null) {
            noteCardObject.transform.DOKill();
            noteCardObject.transform.localScale = Vector3.one * 0.95f;
            noteCardObject.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        PlayCurrentScenarioAudio();
    }

    public void PlayCurrentScenarioAudio() {
        if (cards == null || currentCardIndex >= cards.Length) return;
        var card = cards[currentCardIndex];

        if (card.scenarioAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(card.scenarioAudio);
        }
    }

    public void OnWordBankChipClicked(int chipIndex) {
        if (cards == null || currentCardIndex >= cards.Length || isHandlingSubmission) return;
        var card = cards[currentCardIndex];
        if (card.wordBankChips == null || chipIndex >= card.wordBankChips.Length) return;

        string word = card.wordBankChips[chipIndex];
        if (activeField != null && activeField.interactable) {
            if (string.IsNullOrEmpty(activeField.text)) {
                activeField.text = word;
            } else {
                activeField.text += " " + word;
            }
            activeField.caretPosition = activeField.text.Length;
            activeField.Select();
            activeField.ActivateInputField();
        }
    }

    public void OnSubmitClicked() {
        if (isHandlingSubmission || currentCardIndex >= cards.Length) return;

        string in1 = line1InputField != null ? line1InputField.text.Trim() : "";
        string in2 = line2InputField != null ? line2InputField.text.Trim() : "";

        if (string.IsNullOrEmpty(in1) || string.IsNullOrEmpty(in2)) return;

        isHandlingSubmission = true;
        var card = cards[currentCardIndex];

        bool line1Correct = ValidateTag(in1, card.line1AcceptedTag, card.line1AltTags);
        bool line2Correct = ValidateTag(in2, card.line2AcceptedTag, card.line2AltTags);
        bool bothCorrect = line1Correct && line2Correct;

        if (line1InputField != null) line1InputField.interactable = false;
        if (line2InputField != null) line2InputField.interactable = false;
        if (submitBtn != null) submitBtn.interactable = false;

        if (line1InputBg != null) line1InputBg.color = line1Correct ? correctColor : wrongColor;
        if (line2InputBg != null) line2InputBg.color = line2Correct ? correctColor : wrongColor;

        if (bothCorrect) {
            score++;
            UpdateScoreDisplay();

            if (line1StatementTMP != null) line1StatementTMP.text = $"1. <color=#80FF80>\"{card.line1FullEcho}\"</color>";
            if (line2StatementTMP != null) line2StatementTMP.text = $"2. <color=#80FF80>\"{card.line2FullEcho}\"</color>";

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(PlayAudiosAndAdvanceRoutine(card, true));
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (noteCardObject != null) {
                noteCardObject.transform.DOShakePosition(0.4f, 15f, 20);
            }

            // Show Hint Box with the Correct Answers & Rules
            ShowHintBox(card);

            if (line1StatementTMP != null) line1StatementTMP.text = $"1. <color=#80FF80>\"{card.line1FullEcho}\"</color>";
            if (line2StatementTMP != null) line2StatementTMP.text = $"2. <color=#80FF80>\"{card.line2FullEcho}\"</color>";

            StartCoroutine(PlayAudiosAndAdvanceRoutine(card, false));
        }
    }

    private void ShowHintBox(QuestionTags_WritingW02Card card) {
        if (hintBoxObject != null) {
            hintBoxObject.SetActive(true);
            hintBoxObject.transform.DOKill();
            hintBoxObject.transform.localScale = Vector3.zero;
            hintBoxObject.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (hintTitleTMP != null) {
            hintTitleTMP.text = "HINT & CORRECT ANSWERS";
        }

        if (hintAnswerTMP != null) {
            hintAnswerTMP.text = $"1) <color=#66FF88>\"{card.line1AcceptedTag}\"</color>  |  2) <color=#66FF88>\"{card.line2AcceptedTag}\"</color>";
        }

        if (hintRuleTMP != null) {
            hintRuleTMP.text = $"Rule: {card.line1Hint}  •  {card.line2Hint}";
        }
    }

    private bool ValidateTag(string input, string primary, string[] alts) {
        string normInput = NormalizeTag(input);
        if (normInput == NormalizeTag(primary)) return true;

        if (alts != null) {
            foreach (var alt in alts) {
                if (normInput == NormalizeTag(alt)) return true;
            }
        }
        return false;
    }

    private string NormalizeTag(string str) {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Trim().ToLowerInvariant()
            .Replace("?", "")
            .Replace(".", "")
            .Replace("!", "")
            .Replace(",", "")
            .Replace("’", "'")
            .Replace("`", "'")
            .Replace(" ", "");
    }

    private IEnumerator PlayAudiosAndAdvanceRoutine(QuestionTags_WritingW02Card card, bool wasCorrect) {
        yield return new WaitForSeconds(0.35f);

        if (card.line1Audio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(card.line1Audio);
            yield return new WaitForSeconds(card.line1Audio.length + 0.3f);
        }

        if (card.line2Audio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(card.line2Audio);
            yield return new WaitForSeconds(card.line2Audio.length + 0.5f);
        } else {
            yield return new WaitForSeconds(wasCorrect ? 1.5f : 3.0f);
        }

        AdvanceNext();
    }

    private void AdvanceNext() {
        if (currentCardIndex + 1 < cards.Length) {
            LoadCard(currentCardIndex + 1);
        } else {
            ShowResults();
        }
    }

    private void SetInteractiveState(bool state) {
        if (line1InputField != null) line1InputField.interactable = state;
        if (line2InputField != null) line2InputField.interactable = state;
        if (submitBtn != null) submitBtn.interactable = state;
        if (wordBankButtons != null) {
            foreach (var b in wordBankButtons) if (b != null) b.interactable = state;
        }
    }

    private void UpdateScoreDisplay() {
        if (scoreTMP != null) scoreTMP.text = $"Cards Passed: {score}/{cards.Length}";
    }

    private void ShowResults() {
        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (score >= passThreshold);

        if (resultTitleTMP != null) {
            resultTitleTMP.text = passed ? "WRITING TOPIC COMPLETE!" : "KEEP PRACTICING!";
            resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f) : new Color(1f, 0.65f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Cards Mastered: {score} / {cards.Length}";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed
                ? "You wrote accurate Question Tags for all situations! Speaking branch is now unlocked!"
                : $"You need at least {passThreshold} cards to pass. Tap Retry to try again!";
        }

        if (retryBtn != null) retryBtn.gameObject.SetActive(!passed);
        if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(true);

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
            if (passed) NextButtonAnimation();
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            // Unlock Speaking Branch
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Speaking);
            }
        }
    }

    public void RestartLesson() {
        StartLessonSequence();
    }

    public void OnReturnHubClicked() {
        OnNextButtonClicked();
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        // Unlock Speaking branch
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Speaking);
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
