using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controller for Unit 7 (Collocations) Writing Branch - Stage W02: Write a Sentence with the Collocation.
/// Features 4 collocation cards (get ready, catch a bus, save water, bright idea), word-bank rail,
/// light presence-and-order sentence validation, multiline text input, card glow/highlight animation,
/// ARIA voiceover readback, live score tracking, and 3/4 success condition.
/// </summary>
public class Masters_Collocations_Writing_LessonTwo : Masters_PolishedCommunication_Writing_LessonTwo {

    [System.Serializable]
    public class W02CollocationCard {
        public int cardId;
        public string collocationText;     // e.g. "get ready"
        public string requiredFirstHalf;   // e.g. "get"
        public string requiredSecondHalf;  // e.g. "ready"
        public string referenceExample;    // e.g. "I get ready for school at seven o'clock."
        public AudioClip referenceAudio;   // reference audio clip
    }

    [System.Serializable]
    public class W02WordBankChipData {
        public string chipText;
    }

    [Header("W02 Collocation Cards (4 Cards)")]
    [SerializeField] private W02CollocationCard[] collocationCards;

    [Header("W02 Word Bank Rail (4 Chips)")]
    [SerializeField] private W02WordBankChipData[] wordBankChips;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI w02TitleTMP;
    [SerializeField] private TextMeshProUGUI w02HeaderTMP;
    [SerializeField] private TextMeshProUGUI w02InstructionTMP;
    [SerializeField] private TextMeshProUGUI w02CardTitleTMP;
    [SerializeField] private TextMeshProUGUI w02ExamplePromptTMP;
    [SerializeField] private TextMeshProUGUI w02ProgressTMP;
    [SerializeField] private TextMeshProUGUI w02ScoreTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI w02FeedbackTMP;
    [SerializeField] private TMP_InputField w02SentenceInputField;
    [SerializeField] private Button w02SubmitButton;

    [Header("Word Bank UI")]
    [SerializeField] private Button[] wordBankChipButtons;
    [SerializeField] private TextMeshProUGUI[] wordBankChipTMPs;

    [Header("Result & Navigation UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTMP;
    [SerializeField] private Button retryButton;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip ariaIntroAudio;  // VO_W02_ARIA clip
    [SerializeField] private AudioClip sfxSnap;         // SFX_MAGNET_SNAP clip

    [Header("Pass Threshold")]
    [SerializeField] private int passScore = 3;         // At least 3 of 4 sentences required to pass

    // Runtime state variables
    private int currentCardIndex = 0;
    private int passedCardCount = 0;
    private int attemptsOnCurrentCard = 0;
    private bool isCheckingSentence = false;
    private List<string> studentSubmittedSentences = new List<string>();

    protected override void Awake() {
        // DO NOT call base.Awake() to prevent base class Invoke(StartFirstPrompt) from overriding Unit 7 prompts
        topic = Masters_Topic.Writing;
        narratorSpeech = null;
        CancelInvoke();
        AutoFindUIReferences();
        Initialize4CardsAndWordBank();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        // DO NOT call base.Start() to prevent base class Invoke(StartFirstPrompt) from running
        topic = Masters_Topic.Writing;
        CancelInvoke();
        AutoFindUIReferences();
        Initialize4CardsAndWordBank();
        UpdateTitleAndUIComponents();
        SetupUIBindings();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        currentCardIndex = 0;
        passedCardCount = 0;
        studentSubmittedSentences.Clear();

        UpdateScoreUI();

        // Play intro ARIA voiceover
        PlayIntroVoiceover();

        LoadCard(0);
    }

    public void Initialize4CardsAndWordBank() {
        string audioDir = "Assets/Audio/2A/7_Collocations/Writing/W02/";

        if (collocationCards == null || collocationCards.Length < 4) {
            collocationCards = new W02CollocationCard[] {
                new W02CollocationCard {
                    cardId = 1,
                    collocationText = "get ready",
                    requiredFirstHalf = "get",
                    requiredSecondHalf = "ready",
                    referenceExample = "I get ready for school at seven o'clock.",
                    #if UNITY_EDITOR
                    referenceAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "I get ready for school at seven o'clock.mp3")
                    #endif
                },
                new W02CollocationCard {
                    cardId = 2,
                    collocationText = "catch a bus",
                    requiredFirstHalf = "catch",
                    requiredSecondHalf = "bus",
                    referenceExample = "I catch a bus every morning.",
                    #if UNITY_EDITOR
                    referenceAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "I catch a bus every morning.mp3")
                    #endif
                },
                new W02CollocationCard {
                    cardId = 3,
                    collocationText = "save water",
                    requiredFirstHalf = "save",
                    requiredSecondHalf = "water",
                    referenceExample = "We must save water by closing the tap.",
                    #if UNITY_EDITOR
                    referenceAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "We must save water by closing the tap.mp3")
                    #endif
                },
                new W02CollocationCard {
                    cardId = 4,
                    collocationText = "bright idea",
                    requiredFirstHalf = "bright",
                    requiredSecondHalf = "idea",
                    referenceExample = "My sister had a bright idea for the project.",
                    #if UNITY_EDITOR
                    referenceAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "My sister had a bright idea for the project.mp3")
                    #endif
                }
            };
        }

        if (wordBankChips == null || wordBankChips.Length < 4) {
            wordBankChips = new W02WordBankChipData[] {
                new W02WordBankChipData { chipText = "get ready" },
                new W02WordBankChipData { chipText = "catch a bus" },
                new W02WordBankChipData { chipText = "save water" },
                new W02WordBankChipData { chipText = "bright idea" }
            };
        }
    }

    private void PlayIntroVoiceover() {
        if (ariaIntroAudio == null) {
            #if UNITY_EDITOR
            ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Writing/W02/Use both halves in your own sentence - keep them together.mp3");
            #endif
        }
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
        }
    }

    private void AutoFindUIReferences() {
        if (w02SentenceInputField == null) {
            w02SentenceInputField = GetComponentInChildren<TMP_InputField>(true);
            if (w02SentenceInputField == null) w02SentenceInputField = studentInputField;
        }

        if (w02SubmitButton == null) {
            if (submitButton != null) {
                w02SubmitButton = submitButton;
            } else {
                Button[] btns = GetComponentsInChildren<Button>(true);
                foreach (var b in btns) {
                    if (b == null) continue;
                    string bName = b.name.ToLower();
                    if (bName.Contains("check") || bName.Contains("submit") || bName.Contains("btn")) {
                        w02SubmitButton = b;
                        break;
                    }
                }
            }
        }

        if (w02CardTitleTMP == null) {
            Transform t = transform.Find("CardTitleText") ?? transform.Find("PromptText") ?? transform.Find("Card/Text");
            if (t != null) w02CardTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (w02ExamplePromptTMP == null) {
            Transform t = transform.Find("ExampleText") ?? transform.Find("Card/ExampleText");
            if (t != null) w02ExamplePromptTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (w02ProgressTMP == null) {
            Transform t = transform.Find("ProgressIndicator");
            if (t != null) w02ProgressTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (w02ScoreTMP == null) {
            Transform t = transform.Find("ScoreIndicator") ?? transform.Find("ScoreText") ?? transform.Find("ScoreTMP");
            if (t != null) w02ScoreTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (scoreTMP == null) {
            scoreTMP = w02ScoreTMP;
        }

        if (w02FeedbackTMP == null) {
            Transform t = transform.Find("FeedbackText");
            if (t != null) w02FeedbackTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (resultPanel == null) {
            Transform res = transform.Find("ResultPanel");
            if (res != null) resultPanel = res.gameObject;
        }

        if (retryButton == null && resultPanel != null) {
            retryButton = resultPanel.GetComponentInChildren<Button>(true);
        }

        if (sfxSnap == null) {
            #if UNITY_EDITOR
            sfxSnap = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
            #endif
        }
    }

    private void UpdateScoreUI() {
        int total = (collocationCards != null && collocationCards.Length > 0) ? collocationCards.Length : 4;
        string scoreStr = $"Score: {passedCardCount}/{total}";
        if (w02ScoreTMP != null) {
            w02ScoreTMP.gameObject.SetActive(true);
            w02ScoreTMP.text = scoreStr;
        }
        if (scoreTMP != null && scoreTMP != w02ScoreTMP) {
            scoreTMP.gameObject.SetActive(true);
            scoreTMP.text = scoreStr;
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (lowerName == "title" || lowerName == "lessontitle") {
                tmp.gameObject.SetActive(true);
                tmp.text = "W02 Write a Sentence with the Collocation";
            }
            if (lowerName.Contains("heading") || textVal.Contains("WRITING")) {
                tmp.text = "WRITING BRANCH (Writing Bench)";
            }
            if (lowerName.Contains("instruction") || textVal.Contains("write") || textVal.Contains("sentence")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "Write one sentence for each collocation using both halves together.";
            }
            if (lowerName.Contains("tone")) {
                tmp.text = "";
                tmp.gameObject.SetActive(false);
            }
        }
    }

    private void SetupUIBindings() {
        if (nextButton == null) {
            Transform t = transform.Find("NextButton") ?? transform.Find("Next") ?? transform.Find("Header/NextButton") ?? transform.Find("Canvas/NextButton");
            if (t != null) nextButton = t.GetComponent<Button>();
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (w02SubmitButton != null) {
            w02SubmitButton.onClick.RemoveAllListeners();
            w02SubmitButton.onClick.AddListener(OnSentenceSubmitted);
        }

        if (w02SentenceInputField != null) {
            w02SentenceInputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
            w02SentenceInputField.onSubmit.RemoveAllListeners();
            w02SentenceInputField.onSubmit.AddListener(text => OnSentenceSubmitted());
        }

        // Setup Word Bank Rail buttons
        SetupWordBankRail();
    }

    private void SetupWordBankRail() {
        Button[] chipBtns = (starterChipButtons != null && starterChipButtons.Length > 0) ? starterChipButtons : wordBankChipButtons;
        TextMeshProUGUI[] chipTMPs = (starterChipTMPs != null && starterChipTMPs.Length > 0) ? starterChipTMPs : wordBankChipTMPs;

        if (chipBtns != null && chipBtns.Length > 0 && wordBankChips != null) {
            for (int i = 0; i < chipBtns.Length; i++) {
                if (chipBtns[i] == null) continue;
                int idx = i;
                if (i < wordBankChips.Length) {
                    chipBtns[i].gameObject.SetActive(true);
                    if (chipTMPs != null && i < chipTMPs.Length && chipTMPs[i] != null) {
                        chipTMPs[i].text = wordBankChips[i].chipText;
                    }
                    chipBtns[i].onClick.RemoveAllListeners();
                    chipBtns[i].onClick.AddListener(() => OnWordBankChipTapped(wordBankChips[idx].chipText));
                } else {
                    chipBtns[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void OnWordBankChipTapped(string chipText) {
        if (isCheckingSentence || w02SentenceInputField == null || string.IsNullOrEmpty(chipText)) return;

        // Play soft key click SFX
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        string currentText = w02SentenceInputField.text ?? "";
        if (currentText.Contains(chipText)) return;

        if (string.IsNullOrWhiteSpace(currentText)) {
            w02SentenceInputField.text = chipText;
        } else {
            w02SentenceInputField.text = currentText.TrimEnd() + " " + chipText;
        }

        w02SentenceInputField.caretPosition = w02SentenceInputField.text.Length;
        w02SentenceInputField.Select();
        w02SentenceInputField.ActivateInputField();
    }

    private void LoadCard(int index) {
        if (collocationCards == null || index < 0 || index >= collocationCards.Length) {
            EvaluateLessonCompletion();
            return;
        }

        currentCardIndex = index;
        attemptsOnCurrentCard = 0;
        isCheckingSentence = false;

        W02CollocationCard card = collocationCards[index];

        // Update Score UI
        UpdateScoreUI();

        // Display Prompt / Speech Bubble Text cleanly
        string formattedPromptText = $"Collocation: <b>{card.collocationText}</b>\ne.g. \"{card.referenceExample}\"";

        if (npcSpeechBubbleTMP != null) {
            npcSpeechBubbleTMP.gameObject.SetActive(true);
            npcSpeechBubbleTMP.text = formattedPromptText;
        }

        if (w02CardTitleTMP != null) {
            w02CardTitleTMP.gameObject.SetActive(true);
            w02CardTitleTMP.text = $"Collocation: <b>{card.collocationText}</b>";
        }

        if (w02ExamplePromptTMP != null) {
            w02ExamplePromptTMP.gameObject.SetActive(true);
            w02ExamplePromptTMP.text = $"e.g. \"{card.referenceExample}\"";
        }

        // Update Progress Indicator
        if (w02ProgressTMP != null) {
            w02ProgressTMP.gameObject.SetActive(true);
            w02ProgressTMP.text = $"Sentence {index + 1}/{collocationCards.Length}";
        }

        // Reset Feedback UI
        if (w02FeedbackTMP != null) {
            w02FeedbackTMP.text = "";
            w02FeedbackTMP.gameObject.SetActive(false);
        }

        // Reset Input Field
        if (w02SentenceInputField != null) {
            w02SentenceInputField.gameObject.SetActive(true);
            w02SentenceInputField.text = "";
            w02SentenceInputField.interactable = true;

            TMP_Text placeholder = w02SentenceInputField.placeholder as TMP_Text;
            if (placeholder != null) {
                placeholder.text = $"Write a sentence with '{card.collocationText}'...";
            }

            Image bg = w02SentenceInputField.GetComponent<Image>();
            if (bg != null) bg.color = defaultInputColor;

            w02SentenceInputField.Select();
            w02SentenceInputField.ActivateInputField();
        }

        if (w02SubmitButton != null) {
            w02SubmitButton.gameObject.SetActive(true);
            w02SubmitButton.interactable = true;
        }

        // Update Word Bank Rail with Unit 7 collocations
        SetupWordBankRail();
    }

    public void OnSentenceSubmitted() {
        if (isCheckingSentence || collocationCards == null || currentCardIndex >= collocationCards.Length) return;
        if (w02SentenceInputField == null) return;

        string userSentence = w02SentenceInputField.text;
        if (string.IsNullOrWhiteSpace(userSentence)) {
            ShowFeedback("Please write a sentence for the collocation!", false);
            return;
        }

        W02CollocationCard currentCard = collocationCards[currentCardIndex];

        // Soft key sound on submit
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        // Light presence-and-order validation
        bool isValid = ValidateCollocationInSentence(userSentence, currentCard.requiredFirstHalf, currentCard.requiredSecondHalf);

        if (isValid) {
            StartCoroutine(HandleSentencePassed(currentCard, userSentence));
        } else {
            HandleSentenceFailed(currentCard);
        }
    }

    private bool ValidateCollocationInSentence(string userSentence, string firstHalf, string secondHalf) {
        if (string.IsNullOrWhiteSpace(userSentence) || string.IsNullOrWhiteSpace(firstHalf) || string.IsNullOrWhiteSpace(secondHalf)) return false;

        string cleanSentence = NormalizeText(userSentence);
        string cleanFirst = NormalizeText(firstHalf);
        string cleanSecond = NormalizeText(secondHalf);

        int idxFirst = cleanSentence.IndexOf(cleanFirst);
        if (idxFirst < 0) return false;

        int idxSecond = cleanSentence.IndexOf(cleanSecond, idxFirst);
        if (idxSecond < 0) return false;

        return (idxFirst < idxSecond);
    }

    private string NormalizeText(string rawText) {
        if (string.IsNullOrEmpty(rawText)) return "";
        string clean = rawText.Trim().ToLowerInvariant();
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", " ");
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"[^\w\s]", "");
        return clean.Trim();
    }

    private IEnumerator HandleSentencePassed(W02CollocationCard card, string userSentence) {
        isCheckingSentence = true;
        passedCardCount++;
        studentSubmittedSentences.Add(userSentence);

        UpdateScoreUI();

        if (w02SubmitButton != null) w02SubmitButton.interactable = false;
        if (w02SentenceInputField != null) w02SentenceInputField.interactable = false;

        // Play SFX Correct & Snap effect
        PlaySnapSFX();

        // Highlight input field green
        Image bg = w02SentenceInputField != null ? w02SentenceInputField.GetComponent<Image>() : null;
        if (bg != null) {
            bg.DOColor(new Color(0.4f, 0.9f, 0.4f, 1f), 0.3f);
        }

        ShowFeedback($"Great sentence with '{card.collocationText}'!", true);

        // Play ARIA readback / reference audio
        if (card.referenceAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(card.referenceAudio);
        }

        float waitTime = (card.referenceAudio != null) ? card.referenceAudio.length + 0.3f : 1.8f;
        yield return new WaitForSeconds(Mathf.Max(1.5f, waitTime));

        if (bg != null) {
            bg.DOColor(defaultInputColor, 0.2f);
        }

        currentCardIndex++;
        if (currentCardIndex < collocationCards.Length) {
            LoadCard(currentCardIndex);
        } else {
            EvaluateLessonCompletion();
        }
    }

    private void HandleSentenceFailed(W02CollocationCard card) {
        attemptsOnCurrentCard++;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        // Shake input field
        if (w02SentenceInputField != null) {
            w02SentenceInputField.transform.DOKill();
            w02SentenceInputField.transform.DOShakePosition(0.35f, new Vector3(12f, 0, 0));
        }

        string hintMsg = $"Make sure your sentence includes both '{card.requiredFirstHalf}' and '{card.requiredSecondHalf}' in that order!";
        ShowFeedback(hintMsg, false);
    }

    private void ShowFeedback(string message, bool isSuccess) {
        if (w02FeedbackTMP != null) {
            w02FeedbackTMP.gameObject.SetActive(true);
            w02FeedbackTMP.text = message;
            w02FeedbackTMP.color = isSuccess ? new Color(0.12f, 0.65f, 0.28f) : new Color(0.85f, 0.2f, 0.2f);
        }
    }

    private void PlaySnapSFX() {
        if (sfxSnap != null) {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(sfxSnap, pos);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }
    }

    private void EvaluateLessonCompletion() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        UpdateScoreUI();

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (passedCardCount >= passScore);

        if (resultTMP != null) {
            if (passed) {
                resultTMP.text = $"GREAT JOB! Score: {passedCardCount}/{collocationCards.Length}\nYou completed all the sentences!";
            } else {
                resultTMP.text = $"TRY AGAIN! Score: {passedCardCount}/{collocationCards.Length}\nYou need at least {passScore}/{collocationCards.Length} to pass.";
            }
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
                NextButtonAnimation();
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (retryButton != null) {
                retryButton.gameObject.SetActive(true);
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(RestartActivity);
            }
        }
    }

    public void RestartActivity() {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        currentCardIndex = 0;
        passedCardCount = 0;
        attemptsOnCurrentCard = 0;
        isCheckingSentence = false;
        studentSubmittedSentences.Clear();
        UpdateScoreUI();
        LoadCard(0);
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Writing;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}