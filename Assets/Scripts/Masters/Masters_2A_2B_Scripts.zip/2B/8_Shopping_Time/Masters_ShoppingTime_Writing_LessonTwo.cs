using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// W02 Write the Shopping Trip
/// Stationers' counter guided shopping writing controller for Book 2B Unit 8 (Shopping Time).
/// Writes 3 shopping cards: SHOPPING LIST, MESSAGE TO A FRIEND, and COUNTER CONVERSATION.
/// Word-bank rail chips can be tapped to append phrases directly into the text field.
/// Success condition: Student writes a valid answer for at least 2 of 3 cards (one retry each).
/// </summary>
public class Masters_ShoppingTime_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
public class ShoppingTime_WritingW02Card {
    public int cardId;
    public string cardTitle;              // e.g. "CARD 1: SHOPPING LIST"
    public string scenarioDescription;    // e.g. "Your family is expecting guests. Write FOUR lines saying which shop you will go to and what you will buy there."
    public string promptGuide;            // e.g. "Write which shops you will go to and what you will buy:"
    public string modelAnswer;            // e.g. "I will go to the baker's for bread. I will go to the greengrocer's for vegetables..."
    public string[] wordBankChips;        // 4-6 quick-insert chips
    public string[] requiredKeywords;     // Keywords for validation
    public AudioClip cardAudio;
    public AudioClip modelAudio;
}

    [Header("W02 3 Shopping Card Scenarios")]
    [SerializeField] private ShoppingTime_WritingW02Card[] cards;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;

    [Header("Shopping Card Stage")]
    [SerializeField] private GameObject cardObject;
    [SerializeField] private TextMeshProUGUI cardTitleTMP;
    [SerializeField] private TextMeshProUGUI scenarioDescTMP;
    [SerializeField] private TextMeshProUGUI promptGuideTMP;
    [SerializeField] private Button replayAudioBtn;

    [Header("Word Bank Rail")]
    [SerializeField] private Button[] wordBankChipButtons;
    [SerializeField] private TextMeshProUGUI[] wordBankChipTexts;

    [Header("Input Field & Submit Button")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitBtn;
    [SerializeField] private Image inputFieldBg;

    [Header("Feedback / Explanation Banner")]
    [SerializeField] private GameObject feedbackBanner;
    [SerializeField] private TextMeshProUGUI feedbackTextTMP;

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
    [SerializeField] private int passScore = 2;

    private int currentCardIndex = 0;
    private int score = 0;
    private int attemptsOnCurrentCard = 0;
    private bool isCheckingAnswer = false;

    private readonly Color defaultInputBgColor = Color.white;
    private readonly Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
    private readonly Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Writing;
        base.Awake();

        AutoBindReferences();
        InitCardsIfEmpty();
        WireEventListeners();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Writing;
        AutoBindReferences();
        WireEventListeners();
        EnsureNextAndBackButtonWired();
        RestartLesson();

        if (introAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
        }
    }

    private void WireEventListeners() {
        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnSubmitClicked);
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

        if (wordBankChipButtons != null) {
            for (int i = 0; i < wordBankChipButtons.Length; i++) {
                int idx = i;
                if (wordBankChipButtons[i] != null) {
                    wordBankChipButtons[i].onClick.RemoveAllListeners();
                    wordBankChipButtons[i].onClick.AddListener(() => OnWordBankChipClicked(idx));
                }
            }
        }
    }

    public void InitCardsIfEmpty() {
        if (cards != null && cards.Length > 0) return;

        cards = new ShoppingTime_WritingW02Card[] {
            new ShoppingTime_WritingW02Card {
                cardId = 1,
                cardTitle = "CARD 1: SHOPPING LIST",
                scenarioDescription = "Your family is expecting guests. Write FOUR lines saying which shop you will go to and what you will buy there.",
                promptGuide = "Write which shops you will go to and what you will buy:",
                modelAnswer = "I will go to the baker's for bread. I will go to the greengrocer's for vegetables. I will go to the butcher's for meat. I will go to the chemist's for medicine.",
                wordBankChips = new string[] {
                    "baker's for bread",
                    "greengrocer's for vegetables",
                    "butcher's for meat",
                    "chemist's for medicine",
                    "fishmonger's for fish",
                    "stationer's for pens"
                },
                requiredKeywords = new string[] { "baker", "bread", "greengrocer", "vegetable", "fruit", "butcher", "meat", "chemist", "medicine", "fishmonger", "fish", "stationer", "pen", "shop", "buy", "go" }
            },
            new ShoppingTime_WritingW02Card {
                cardId = 2,
                cardTitle = "CARD 2: MESSAGE TO A FRIEND",
                scenarioDescription = "You found something you love in a sale. Write two or three lines about the price and the offer.",
                promptGuide = "Write 2-3 lines about the price and the offer:",
                modelAnswer = "I found a great shirt on sale. It was half price. What a bargain! It's 20% off until Sunday.",
                wordBankChips = new string[] {
                    "half price",
                    "What a bargain!",
                    "good value",
                    "20% off",
                    "buy one get one free",
                    "quite reasonable"
                },
                requiredKeywords = new string[] { "half", "price", "bargain", "value", "off", "sale", "discount", "free", "reasonable", "cost", "rupee", "buy", "shirt", "dress" }
            },
            new ShoppingTime_WritingW02Card {
                cardId = 3,
                cardTitle = "CARD 3: COUNTER CONVERSATION",
                scenarioDescription = "Write a short exchange at a shop counter, modelled on Joy and the Shopkeeper: ask to see something, ask the price, hear the discount, and pay.",
                promptGuide = "Write an exchange: ask to see + price + discount + pay:",
                modelAnswer = "May I help you? I want a T-shirt. How much does it cost? It costs four hundred rupees with 10% discount. Here is the money. Thank you!",
                wordBankChips = new string[] {
                    "May I help you?",
                    "How much does it cost?",
                    "four hundred rupees",
                    "10% discount",
                    "Three hundred and sixty rupees",
                    "Here it is. Thank you."
                },
                requiredKeywords = new string[] { "help", "want", "cost", "much", "price", "rupee", "hundred", "discount", "pay", "money", "here", "thank", "t-shirt", "shirt", "blue" }
            }
        };
    }

    public void RestartLesson() {
        currentCardIndex = 0;
        score = 0;
        attemptsOnCurrentCard = 0;
        isCheckingAnswer = false;

        if (headerTMP != null) headerTMP.gameObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (cardObject != null) cardObject.SetActive(true);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        UpdateScoreUI();
        ShowCard(0);
    }

    public void ShowCard(int index) {
        if (cards == null || cards.Length == 0) return;
        currentCardIndex = Mathf.Clamp(index, 0, cards.Length - 1);
        attemptsOnCurrentCard = 0;
        isCheckingAnswer = false;

        ShoppingTime_WritingW02Card card = cards[currentCardIndex];

        UpdateScoreUI();

        if (headerTMP != null) headerTMP.text = "STATIONERS' COUNTER 🧾";
        if (titleTMP != null) {
            titleTMP.text = "Write the Shopping Trip";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
        }
        if (subtitleTMP != null) subtitleTMP.text = "Write the required shopping lines using the word-bank rail.";

        if (cardTitleTMP != null) cardTitleTMP.text = card.cardTitle;
        if (scenarioDescTMP != null) scenarioDescTMP.text = card.scenarioDescription;
        if (promptGuideTMP != null) promptGuideTMP.text = card.promptGuide;

        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            inputField.ActivateInputField();
        }

        if (inputFieldBg != null) {
            inputFieldBg.color = defaultInputBgColor;
        }

        if (submitBtn != null) {
            submitBtn.interactable = true;
        }

        // Populate Word Bank Chips
        if (wordBankChipButtons != null && card.wordBankChips != null) {
            for (int i = 0; i < wordBankChipButtons.Length; i++) {
                if (wordBankChipButtons[i] == null) continue;
                if (i < card.wordBankChips.Length) {
                    wordBankChipButtons[i].gameObject.SetActive(true);
                    if (wordBankChipTexts != null && i < wordBankChipTexts.Length && wordBankChipTexts[i] != null) {
                        wordBankChipTexts[i].text = card.wordBankChips[i];
                    }
                } else {
                    wordBankChipButtons[i].gameObject.SetActive(false);
                }
            }
        }

        if (card.cardAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(card.cardAudio);
        }
    }

    private void UpdateScoreUI() {
        if (progressTMP != null && cards != null) {
            progressTMP.text = $"Card {currentCardIndex + 1}/{cards.Length}";
        }
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {score}";
        }
    }

    public void OnWordBankChipClicked(int chipIndex) {
        if (cards == null || currentCardIndex >= cards.Length) return;
        ShoppingTime_WritingW02Card card = cards[currentCardIndex];
        if (chipIndex < 0 || chipIndex >= card.wordBankChips.Length) return;

        string chipText = card.wordBankChips[chipIndex];
        if (inputField != null) {
            if (string.IsNullOrWhiteSpace(inputField.text)) {
                inputField.text = chipText;
            } else {
                string current = inputField.text.TrimEnd();
                if (!current.EndsWith(".") && !current.EndsWith("!") && !current.EndsWith("?")) {
                    current += ".";
                }
                inputField.text = current + " " + chipText;
            }
            inputField.caretPosition = inputField.text.Length;
            inputField.ActivateInputField();
        }
    }

    public void OnSubmitClicked() {
        if (isCheckingAnswer) return;
        if (cards == null || currentCardIndex >= cards.Length) return;

        string text = inputField != null ? inputField.text.Trim() : "";
        if (string.IsNullOrEmpty(text)) {
            if (feedbackBanner != null) feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) feedbackTextTMP.text = "<color=#FFAA33>Please write your lines or tap the word bank chips before submitting.</color>";
            return;
        }

        StartCoroutine(HandleSubmission(text));
    }

    private IEnumerator HandleSubmission(string text) {
        isCheckingAnswer = true;
        attemptsOnCurrentCard++;

        ShoppingTime_WritingW02Card card = cards[currentCardIndex];
        bool isPass = ValidateStudentWriting(text, card);

        if (isPass) {
            if (attemptsOnCurrentCard == 1) {
                score++;
                UpdateScoreUI();
            }

            if (inputFieldBg != null) inputFieldBg.color = correctColor;
            if (feedbackBanner != null) feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) {
                feedbackTextTMP.text = "<color=#33D866><b>✓ Card Completed! Great shopping writing!</b></color>";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Play model readback audio
            if (card.modelAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(card.modelAudio);
            }

            yield return new WaitForSeconds(3.0f);

            if (currentCardIndex < cards.Length - 1) {
                ShowCard(currentCardIndex + 1);
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

            if (attemptsOnCurrentCard == 1) {
                if (feedbackBanner != null) feedbackBanner.SetActive(true);
                if (feedbackTextTMP != null) {
                    feedbackTextTMP.text = "<color=#FF6666>Include more shopping details or tap the chips in the word bank!</color>";
                }
                isCheckingAnswer = false;
            } else {
                // Second failure: show model answer & advance
                if (feedbackBanner != null) feedbackBanner.SetActive(true);
                if (feedbackTextTMP != null) {
                    feedbackTextTMP.text = $"<color=#FFAA33><b>Exemplar:</b> {card.modelAnswer}</color>";
                }

                if (card.modelAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(card.modelAudio);
                }

                yield return new WaitForSeconds(3.5f);

                if (currentCardIndex < cards.Length - 1) {
                    ShowCard(currentCardIndex + 1);
                } else {
                    ShowResults();
                }
            }
        }
    }

    private bool ValidateStudentWriting(string text, ShoppingTime_WritingW02Card card) {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string lower = text.ToLower();

        // 1. Check direct chip inclusion
        int chipsMatched = 0;
        if (card.wordBankChips != null) {
            foreach (var chip in card.wordBankChips) {
                if (lower.Contains(chip.ToLower())) chipsMatched++;
            }
        }
        if (chipsMatched >= 2) return true;

        // 2. Keyword match count
        int matched = 0;
        if (card.requiredKeywords != null) {
            foreach (var kw in card.requiredKeywords) {
                if (lower.Contains(kw.ToLower())) matched++;
            }
        }

        // At least 2-3 keywords or minimum 4 words written
        string[] words = text.Split(new char[] { ' ', '\n', '\t', '.', ',', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);
        return (matched >= 2 || words.Length >= 4);
    }

    public void ReplayCurrentAudio() {
        if (cards == null || currentCardIndex >= cards.Length) return;
        AudioClip clip = cards[currentCardIndex].cardAudio;
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }
    }

    private void ShowResults() {
        if (cardObject != null) cardObject.SetActive(false);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        if (resultPanel == null) {
            Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
            if (t != null) resultPanel = t.gameObject;
        }

        bool passed = (score >= passScore);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "SHOPPING TRIP WRITING COMPLETE! 🛍️" : "KEEP PRACTICING!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"You completed {score} of {cards.Length} shopping writing cards!";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Success! All shopping cards have been filed and recorded." : $"You need at least {passScore}/{cards.Length} cards completed to finish this writing topic.";
            }

            if (returnHubBtn != null) {
                returnHubBtn.gameObject.SetActive(passed);
            }

            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(!passed || score < cards.Length);
            }
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
        topic = Masters_Topic.Writing;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Writing;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    public void AutoBindReferences() {
        if (titleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP == null) {
            Transform t = transform.Find("ProgressTMP") ?? transform.Find("progression count") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (scoreTMP == null) {
            Transform t = transform.Find("ScoreTMP") ?? transform.Find("Score") ?? transform.Find("HeaderContainer/ScoreTMP");
            if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
        }

        // Card stage
        if (cardObject == null) {
            Transform t = transform.Find("ComicStageCard") ?? transform.Find("Card") ?? transform.Find("CardObject") ?? transform.Find("DeskCard");
            if (t != null) cardObject = t.gameObject;
        }
        if (cardTitleTMP == null && cardObject != null) {
            Transform t = cardObject.transform.Find("StripTitleTMP") ?? cardObject.transform.Find("CardTitleTMP") ?? cardObject.transform.Find("Title");
            if (t != null) cardTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (scenarioDescTMP == null && cardObject != null) {
            Transform t = cardObject.transform.Find("SituationDescTMP") ?? cardObject.transform.Find("ScenarioDescTMP") ?? cardObject.transform.Find("questions text");
            if (t != null) scenarioDescTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (promptGuideTMP == null && cardObject != null) {
            Transform t = cardObject.transform.Find("SpeakerAPromptTMP") ?? cardObject.transform.Find("PromptGuideTMP") ?? cardObject.transform.Find("PromptTMP");
            if (t != null) promptGuideTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (replayAudioBtn == null && cardObject != null) {
            Transform t = cardObject.transform.Find("ReplayAudioBtn") ?? cardObject.transform.Find("ReplayBtn");
            if (t != null) replayAudioBtn = t.GetComponent<Button>();
        }

        // Input & Submit
        if (inputField == null) {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }
        if (inputField != null && inputFieldBg == null) {
            inputFieldBg = inputField.GetComponent<Image>();
        }
        if (submitBtn == null) {
            Transform t = transform.Find("InputArea/SubmitBtn") ?? transform.Find("SubmitBtn") ?? transform.Find("Check") ?? transform.Find("CheckButton");
            if (t != null) submitBtn = t.GetComponent<Button>();
        }

        // Feedback banner
        if (feedbackBanner == null) {
            Transform t = transform.Find("FeedbackBanner") ?? transform.Find("Feedback");
            if (t != null) feedbackBanner = t.gameObject;
        }
        if (feedbackTextTMP == null && feedbackBanner != null) {
            Transform t = feedbackBanner.transform.Find("FeedbackTextTMP") ?? feedbackBanner.transform.Find("Text");
            if (t != null) feedbackTextTMP = t.GetComponent<TextMeshProUGUI>();
        }

        // Word Bank Rail Chips
        Transform rail = transform.Find("WordBankRail") ?? transform.Find("WordBank");
        if (rail != null) {
            List<Button> chipBtns = new List<Button>();
            List<TextMeshProUGUI> chipTxts = new List<TextMeshProUGUI>();
            for (int i = 1; i <= 6; i++) {
                Transform chipTr = rail.Find($"WordBankChip_{i}") ?? rail.Find($"Chip_{i}") ?? rail.Find($"WordBankChip{i}");
                if (chipTr != null) {
                    Button b = chipTr.GetComponent<Button>();
                    TextMeshProUGUI txt = chipTr.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (b != null) chipBtns.Add(b);
                    if (txt != null) chipTxts.Add(txt);
                }
            }
            if (chipBtns.Count > 0 && (wordBankChipButtons == null || wordBankChipButtons.Length == 0 || wordBankChipButtons[0] == null)) {
                wordBankChipButtons = chipBtns.ToArray();
            }
            if (chipTxts.Count > 0 && (wordBankChipTexts == null || wordBankChipTexts.Length == 0 || wordBankChipTexts[0] == null)) {
                wordBankChipTexts = chipTxts.ToArray();
            }
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
