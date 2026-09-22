using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Rebuilt Game Manager for Unit 6: Colour Your Speech - RP02 ("Free Scene: Colour Your Own Chat").
/// Inherits directly from Masters_Lesson.
/// Manages puppet studio bench conversations across 3 distinct chat cards.
/// Requires 2-turn dialogue per card (Turn 1 with a valid idiom, Friend Reply, Turn 2 with a different valid idiom).
/// Passing all 3 cards marks RP02 complete and evaluates Roleplay branch completion.
/// </summary>
public class Masters_ColourYourSpeech_Roleplay_LessonTwo : Masters_Lesson {

    public enum ChatTurnPhase {
        Turn1_Student,
        FriendReply,
        Turn2_Student,
        Readback,
        CardCompleted
    }

    [System.Serializable]
    public class ChatCard {
        public string cardTitle = "Card";
        [TextArea(2, 4)] public string situationPrompt;
        public AudioClip ariaSetupClip;
        [TextArea(2, 3)] public string friendReplyText;
        public AudioClip friendReplyClip;
        public AudioClip ariaReadbackClip;
    }

    [System.Serializable]
    public class IdiomWordBankItem {
        public string idiomText;
        public string meaning;
        public string[] aliases;

        public IdiomWordBankItem(string text, string mean, string[] aliasList = null) {
            idiomText = text;
            meaning = mean;
            aliases = aliasList ?? new string[0];
        }
    }

    [Header("Header & Navigation UI")]
    [SerializeField] private TextMeshProUGUI lessonTitleText;
    [SerializeField] private TextMeshProUGUI cardCounterText;
    [SerializeField] private Button backButton;

    [Header("Characters / Puppets")]
    [SerializeField] private GameObject leftPuppet;
    [SerializeField] private GameObject rightPuppet;

    [Header("Central Chat Area")]
    [SerializeField] private GameObject chatCardPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI turnIndicatorText;
    [SerializeField] private GameObject friendReplyBubble;
    [SerializeField] private TextMeshProUGUI friendReplyText;
    [SerializeField] private GameObject studentResponseBubble;
    [SerializeField] private TextMeshProUGUI studentResponseText;
    [SerializeField] private TextMeshProUGUI feedbackText;

    [Header("Word Bank Rail UI")]
    [SerializeField] private GameObject wordBankPanel;
    [SerializeField] private Transform wordBankContainer;
    [SerializeField] private Button[] wordBankChips;

    [Header("Input & Action Controls")]
    [SerializeField] private TMP_InputField studentInputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button speechMicButton;

    [Header("Audio & SFX")]
    [SerializeField] private AudioClip ariaIntroClip;
    [SerializeField] private AudioClip sfxMatch;

    [Header("Unit 6 RP02 Data")]
    [SerializeField] private ChatCard[] chatCards;
    [SerializeField] private IdiomWordBankItem[] idiomWordBank;

    // Runtime state tracking
    private int currentCardIndex = 0;
    private ChatTurnPhase currentPhase = ChatTurnPhase.Turn1_Student;
    private int turn1IdiomIndex = -1;
    private int turn2IdiomIndex = -1;
    private int passedCardsCount = 0;
    private bool isProcessingTurn = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;

        EnsureUIBindings();

        if (checkButton != null) {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        }

        if (undoButton != null) {
            undoButton.onClick.RemoveAllListeners();
            undoButton.onClick.AddListener(OnUndoButtonClicked);
        }

        if (speechMicButton != null) {
            speechMicButton.onClick.RemoveAllListeners();
            speechMicButton.onClick.AddListener(OnSpeechMicClicked);
        }

        if (backButton != null) {
            var mb = backButton.GetComponent<Masters_BackButton>() ?? backButton.GetComponentInParent<Masters_BackButton>();
            if (mb == null) {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        InitializeDefaultDataIfEmpty();
    }

    private void EnsureUIBindings() {
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts) {
            string n = t.gameObject.name.ToLower();
            if (lessonTitleText == null && (n.Contains("title") || n.Contains("lessontitle"))) lessonTitleText = t;
            else if (cardCounterText == null && (n.Contains("progress") || n.Contains("count") || n.Contains("cardcount"))) cardCounterText = t;
            else if (promptText == null && (n.Contains("prompt") || n.Contains("situation") || n.Contains("slate"))) promptText = t;
            else if (turnIndicatorText == null && (n.Contains("turn") || n.Contains("hint") || n.Contains("instruction"))) turnIndicatorText = t;
            else if (friendReplyText == null && (n.Contains("friend") || n.Contains("npc") || n.Contains("reply"))) friendReplyText = t;
            else if (studentResponseText == null && (n.Contains("student") || n.Contains("player") || n.Contains("response"))) studentResponseText = t;
            else if (feedbackText == null && n.Contains("feedback")) feedbackText = t;
        }

        var btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns) {
            string bn = b.gameObject.name.ToLower();
            if (checkButton == null && (bn.Contains("check") || bn.Contains("submit") || bn.Contains("confirm"))) checkButton = b;
            else if (undoButton == null && (bn.Contains("undo") || bn.Contains("reset") || bn.Contains("clear"))) undoButton = b;
            else if (nextButton == null && (bn.Contains("next") || bn.Contains("continue"))) nextButton = b;
            else if (backButton == null && (bn.Contains("back") || bn.Contains("return"))) backButton = b;
            else if (speechMicButton == null && (bn.Contains("mic") || bn.Contains("speech"))) speechMicButton = b;
        }

        if (studentInputField == null) {
            studentInputField = GetComponentInChildren<TMP_InputField>(true);
        }

        if (wordBankContainer == null) {
            var scroll = transform.Find("WordBankArea") ?? transform.Find("WordBankScrollView") ?? transform.Find("bank scroll");
            if (scroll != null) {
                var content = scroll.Find("Viewport/Content") ?? scroll.Find("bank") ?? scroll.Find("Content");
                if (content != null) {
                    wordBankContainer = content;
                    wordBankPanel = scroll.gameObject;
                }
            }
        }
    }

    protected override void Start() {
        passedCardsCount = 0;
        currentCardIndex = 0;
        isProcessingTurn = false;

        if (lessonTitleText != null) {
            lessonTitleText.text = "Free Scene — Colour Your Own Chat";
        }

        PopulateWordBankChips();

        if (ariaIntroClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroClip);
        }

        LoadChatCard(0);
    }

    private void InitializeDefaultDataIfEmpty() {
        if (idiomWordBank == null || idiomWordBank.Length == 0) {
            idiomWordBank = new IdiomWordBankItem[] {
                new IdiomWordBankItem("Cloud nine", "In a state of happiness or extreme excitement.", new[] { "cloud 9", "on cloud nine" }),
                new IdiomWordBankItem("A piece of cake", "Very easy.", new[] { "piece of cake" }),
                new IdiomWordBankItem("Going bananas", "Becoming crazy, losing temper.", new[] { "go bananas", "gone bananas" }),
                new IdiomWordBankItem("Sugar and spice", "Kind and friendly; very sweet and nice.", new[] { "sugar & spice" }),
                new IdiomWordBankItem("Fruitcake", "Really strange or crazy.", new[] { "fruit cake", "silly fruitcake" }),
                new IdiomWordBankItem("Nutty as a fruitcake", "Funny, kind of crazy.", new[] { "nutty", "as nutty as a fruitcake" }),
                new IdiomWordBankItem("Tongue-in-cheek", "Saying something as a joke, not serious.", new[] { "tongue in cheek" }),
                new IdiomWordBankItem("My way or the highway", "You have to listen to me.", new[] { "my way" }),
                new IdiomWordBankItem("Easy as pie", "Very easy.", new[] { "as easy as pie" }),
                new IdiomWordBankItem("Giving candy to a baby", "Very easy.", new[] { "like giving candy to a baby" })
            };
        }

        if (chatCards == null || chatCards.Length == 0) {
            chatCards = new ChatCard[] {
                new ChatCard {
                    cardTitle = "Card A",
                    situationPrompt = "Chat about hearing great news and feeling ready for anything!",
                    friendReplyText = "That sounds wonderful! How was the rest of your day?"
                },
                new ChatCard {
                    cardTitle = "Card B",
                    situationPrompt = "Chat about a messy room and how someone will react!",
                    friendReplyText = "Oh no! Is she going to be mad at you all evening?"
                },
                new ChatCard {
                    cardTitle = "Card C",
                    situationPrompt = "Chat about a wild, funny idea your friend suggested!",
                    friendReplyText = "Really? Did they actually mean it seriously?"
                }
            };
        }
    }

    private void PopulateWordBankChips() {
        if (wordBankContainer == null || idiomWordBank == null) return;

        int count = Mathf.Min(wordBankContainer.childCount, idiomWordBank.Length);
        for (int i = 0; i < count; i++) {
            var child = wordBankContainer.GetChild(i);
            child.gameObject.SetActive(true);

            var tmps = child.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (tmps.Length > 0) tmps[0].text = idiomWordBank[i].idiomText;
            if (tmps.Length > 1) tmps[1].text = idiomWordBank[i].meaning;

            int idx = i;
            var btn = child.GetComponent<Button>();
            if (btn != null) {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnIdiomChipClicked(idx));
            }
        }
    }

    public void OnIdiomChipClicked(int idiomIdx) {
        if (isProcessingTurn || idiomIdx < 0 || idiomIdx >= idiomWordBank.Length) return;

        string idiomName = idiomWordBank[idiomIdx].idiomText;
        if (studentInputField != null) {
            studentInputField.text = idiomName;
            studentInputField.ActivateInputField();
        }
    }

    private void LoadChatCard(int cardIdx) {
        if (chatCards == null || cardIdx >= chatCards.Length) {
            FinishRP02Activity();
            return;
        }

        currentCardIndex = cardIdx;
        currentPhase = ChatTurnPhase.Turn1_Student;
        turn1IdiomIndex = -1;
        turn2IdiomIndex = -1;
        isProcessingTurn = false;

        ChatCard card = chatCards[cardIdx];

        if (lessonTitleText != null) {
            lessonTitleText.text = "Free Scene — Colour Your Own Chat";
        }

        if (cardCounterText != null) {
            cardCounterText.text = $"Card {cardIdx + 1}/{chatCards.Length}";
        }

        if (promptText != null) {
            promptText.text = card.situationPrompt;
        }

        if (turnIndicatorText != null) {
            turnIndicatorText.text = "Turn 1: Describe what happened using an idiom from the word bank.";
        }

        if (friendReplyBubble != null) friendReplyBubble.SetActive(false);
        if (studentResponseBubble != null) studentResponseBubble.SetActive(false);
        if (feedbackText != null) feedbackText.text = "";
        if (studentInputField != null) {
            studentInputField.text = "";
            studentInputField.interactable = true;
        }

        if (wordBankPanel != null) wordBankPanel.SetActive(true);
        if (checkButton != null) checkButton.interactable = true;
        if (undoButton != null) undoButton.interactable = true;
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        if (card.ariaSetupClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(card.ariaSetupClip);
        }
    }

    private void OnCheckButtonClicked() {
        if (isProcessingTurn || studentInputField == null) return;

        string inputText = studentInputField.text;
        if (string.IsNullOrWhiteSpace(inputText)) {
            ShowFeedback("Please enter or select an idiom first!", false);
            return;
        }

        EvaluateStudentInput(inputText);
    }

    private void OnUndoButtonClicked() {
        if (isProcessingTurn || studentInputField == null) return;
        studentInputField.text = "";
        if (feedbackText != null) feedbackText.text = "";
    }

    private void OnSpeechMicClicked() {
        if (isProcessingTurn) return;

        if (CrossPlatformSpeechManager.Instance != null) {
            CrossPlatformSpeechManager.Instance.StartListening();
        }
    }

    private void OnBackButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
    }

    private void OnEnable() {
        CrossPlatformSpeechManager.OnResultStatic += HandleSpeechResult;
    }

    private void OnDisable() {
        CrossPlatformSpeechManager.OnResultStatic -= HandleSpeechResult;
    }

    private void HandleSpeechResult(string speechText) {
        if (isProcessingTurn || string.IsNullOrWhiteSpace(speechText)) return;

        if (studentInputField != null) {
            studentInputField.text = speechText;
        }

        EvaluateStudentInput(speechText);
    }

    private void EvaluateStudentInput(string input) {
        int detectedIdx = DetectIdiomInText(input);

        if (detectedIdx < 0) {
            ShowFeedback("No matching idiom detected. Choose one from the word bank!", false);
            PlaySound(Masters_SFX.Incorrect);
            return;
        }

        if (currentPhase == ChatTurnPhase.Turn1_Student) {
            // Turn 1 Passed!
            turn1IdiomIndex = detectedIdx;
            isProcessingTurn = true;
            PlaySound(Masters_SFX.Correct);
            PlayMatchSFX();

            if (studentResponseText != null) studentResponseText.text = input;
            if (studentResponseBubble != null) studentResponseBubble.SetActive(true);
            if (studentInputField != null) studentInputField.text = "";
            ShowFeedback("Great Turn 1!", true);

            StartCoroutine(FriendReplySequence());
        } else if (currentPhase == ChatTurnPhase.Turn2_Student) {
            if (detectedIdx == turn1IdiomIndex) {
                // Duplicate idiom
                ShowFeedback("Turn 2 must use a DIFFERENT idiom from Turn 1!", false);
                PlaySound(Masters_SFX.Incorrect);
                return;
            }

            // Turn 2 Passed!
            turn2IdiomIndex = detectedIdx;
            isProcessingTurn = true;
            PlaySound(Masters_SFX.Correct);
            PlayMatchSFX();

            if (studentResponseText != null) studentResponseText.text = input;
            if (studentResponseBubble != null) studentResponseBubble.SetActive(true);
            if (studentInputField != null) studentInputField.text = "";
            ShowFeedback("Excellent Turn 2!", true);

            StartCoroutine(ReadbackSequence());
        }
    }

    private int DetectIdiomInText(string text) {
        if (string.IsNullOrWhiteSpace(text) || idiomWordBank == null) return -1;

        string normalized = NormalizeText(text);

        for (int i = 0; i < idiomWordBank.Length; i++) {
            var item = idiomWordBank[i];
            if (item == null) continue;

            string canonNorm = NormalizeText(item.idiomText);
            if (normalized.Contains(canonNorm)) return i;

            if (item.aliases != null) {
                foreach (var alias in item.aliases) {
                    if (!string.IsNullOrEmpty(alias) && normalized.Contains(NormalizeText(alias))) {
                        return i;
                    }
                }
            }
        }

        return -1;
    }

    private string NormalizeText(string text) {
        if (string.IsNullOrEmpty(text)) return "";
        string clean = text.ToLowerInvariant();
        clean = clean.Replace("-", " ").Replace("&", "and");
        clean = clean.Replace(".", "").Replace(",", "").Replace("!", "").Replace("?", "").Replace("\"", "").Replace("'", "");
        return clean.Trim();
    }

    private IEnumerator FriendReplySequence() {
        currentPhase = ChatTurnPhase.FriendReply;
        yield return new WaitForSeconds(0.6f);

        ChatCard card = chatCards[currentCardIndex];

        if (friendReplyText != null) friendReplyText.text = card.friendReplyText;
        if (friendReplyBubble != null) friendReplyBubble.SetActive(true);

        if (card.friendReplyClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(card.friendReplyClip);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        // Enable Turn 2
        currentPhase = ChatTurnPhase.Turn2_Student;
        isProcessingTurn = false;

        if (turnIndicatorText != null) {
            turnIndicatorText.text = $"Turn 2: React to your friend with a DIFFERENT idiom (Turn 1 used: {idiomWordBank[turn1IdiomIndex].idiomText}).";
        }
    }

    private IEnumerator ReadbackSequence() {
        currentPhase = ChatTurnPhase.Readback;
        yield return new WaitForSeconds(0.6f);

        ChatCard card = chatCards[currentCardIndex];

        if (card.ariaReadbackClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(card.ariaReadbackClip);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        } else {
            yield return new WaitForSeconds(2.0f);
        }

        passedCardsCount++;

        yield return new WaitForSeconds(0.5f);
        LoadChatCard(currentCardIndex + 1);
    }

    private void FinishRP02Activity() {
        currentPhase = ChatTurnPhase.CardCompleted;

        // All 3 cards completed -> mark RP02 complete
        if (passedCardsCount >= 3) {
            M3A_U6_HubProgress.MarkRP02Complete();
        }

        if (promptText != null) {
            promptText.text = "Great job! You've coloured your chats with idioms.";
        }

        if (turnIndicatorText != null) turnIndicatorText.text = "Roleplay Complete!";
        if (studentInputField != null) studentInputField.gameObject.SetActive(false);
        if (checkButton != null) checkButton.gameObject.SetActive(false);
        if (undoButton != null) undoButton.gameObject.SetActive(false);
        if (speechMicButton != null) speechMicButton.gameObject.SetActive(false);

        if (nextButton != null) {
            nextButton.interactable = true;
            nextButton.gameObject.SetActive(true);
            NextButtonAnimation();
        }
    }

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackText != null && feedbackText.gameObject != null) {
            feedbackText.text = msg;
            feedbackText.color = isSuccess ? new Color(0.2f, 0.9f, 0.4f) : new Color(1f, 0.35f, 0.35f);
            if (feedbackText.gameObject.activeInHierarchy) {
                feedbackText.transform.DOKill(false);
                feedbackText.transform.localScale = Vector3.one;
                feedbackText.transform.DOPunchScale(Vector3.one * 0.08f, 0.25f, 5, 0.5f);
            }
        }
    }

    private void PlaySound(Masters_SFX sfx) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(sfx);
        }
    }

    private void PlayMatchSFX() {
        if (sfxMatch != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(sfxMatch);
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) topic = Masters_Topic.Roleplay;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (passedCardsCount >= 3) {
            M3A_U6_HubProgress.MarkRP02Complete();
        }

        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Quiz);
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private void OnDestroy() {
        StopAllCoroutines();
        DOTween.Kill(this);
        if (feedbackText != null && feedbackText.gameObject != null) {
            DOTween.Kill(feedbackText.transform);
        }
    }
}
