using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controller for Unit 6 (Groove On) Writing Branch - Stage W02: Write a Celebration Card.
/// Features 4 card prompts (Birthday, Belated Birthday, Diwali, Festival), multiline card writing,
/// interactive tap-to-insert word bank rail, verbatim occasion validation, card sealing animation,
/// ARIA voiceover readback, and 3/4 success condition.
/// </summary>
public class Masters_GrooveOn_Writing_LessonTwo : Masters_PolishedCommunication_Writing_LessonTwo {

    [System.Serializable]
    public class W02CardPromptData {
        public int promptId;
        public string occasionId;            // e.g. "birthday", "belated birthday", "Diwali", "festival"
        public string promptDescription;      // e.g. "A birthday card for a classmate..."
        public string[] acceptedGreetings;    // Verbatim greetings required to pass
        public string[] supportingExamples;   // Optional example lines
        public string audioFileName;         // Intro audio clip for prompt
    }

    [System.Serializable]
    public class W02WordBankEntry {
        public string greetingText;
        public string occasionCategory;
    }

    [Header("W02 Card Prompts (4 Items)")]
    [SerializeField] private W02CardPromptData[] cardPrompts;

    [Header("W02 Word Bank Rail (11 Greetings)")]
    [SerializeField] private W02WordBankEntry[] wordBankRail;

    [Header("W02 UI Overrides")]
    [SerializeField] private TextMeshProUGUI promptHeaderTMP;
    [SerializeField] private TextMeshProUGUI occasionPromptTMP;
    [SerializeField] private TextMeshProUGUI feedbackMessageTMP;
    [SerializeField] private TextMeshProUGUI progressCounterTMP;
    [SerializeField] private GameObject cardSealBadgeObj;

    private int currentPromptIndex = 0;
    private int passedPromptCount = 0;
    private bool isCardSealed = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;
        narratorSpeech = null; // Clear base narrator audio to prevent overlap
        InitializePromptsAndWordBank();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Writing;
        InitializePromptsAndWordBank();
        UpdateTitleAndUIComponents();
        SetupUIBindings();

        currentPromptIndex = 0;
        passedPromptCount = 0;

        // Play VO_W02_ARIA intro voiceover
        PlayIntroVoiceover();

        LoadCardPrompt(0);
    }

    private void InitializePromptsAndWordBank() {
        if (cardPrompts == null || cardPrompts.Length < 4) {
            // string audioDir = ... (unused)
            cardPrompts = new W02CardPromptData[] {
                new W02CardPromptData {
                    promptId = 1,
                    occasionId = "birthday",
                    promptDescription = "A birthday card for a classmate — e.g. 'Wish you a very happy birthday! May God bless you. Have fun!'",
                    acceptedGreetings = new string[] { "Wish you a very happy birthday!", "Many more happy returns of the day!" },
                    supportingExamples = new string[] { "May God bless you.", "Have fun!" },
                    audioFileName = "A birthday card for a classmate.mp3"
                },
                new W02CardPromptData {
                    promptId = 2,
                    occasionId = "belated birthday",
                    promptDescription = "A belated card (you missed the day) — e.g. 'Belated birthday wishes! May God bless you.'",
                    acceptedGreetings = new string[] { "Belated birthday wishes!" },
                    supportingExamples = new string[] { "May God bless you." },
                    audioFileName = "A belated card you missed the day.mp3"
                },
                new W02CardPromptData {
                    promptId = 3,
                    occasionId = "Diwali",
                    promptDescription = "A Diwali card for a neighbour — e.g. 'Wish you a Happy Diwali! Joy and prosperity this Diwali.'",
                    acceptedGreetings = new string[] { "Wish you a Happy Diwali!", "Joy and prosperity this Diwali!" },
                    supportingExamples = new string[] { "Joy and prosperity this Diwali." },
                    audioFileName = "A Diwali card for a neighbour.mp3"
                },
                new W02CardPromptData {
                    promptId = 4,
                    occasionId = "festival",
                    promptDescription = "A card for any festival your family celebrates — use the right verbatim greeting for that festival.",
                    acceptedGreetings = new string[] {
                        "Eid Mubarak!", "Happy Easter to you!", "Merry Christmas to you!",
                        "Happy New Year!", "Happy Independence Day!", "Happy Gandhi Jayanti!",
                        "Happy Gurupurab! Guru Nanak Jayanti!", "Wish you a Happy Diwali!",
                        "Wish you a very happy birthday!", "Belated birthday wishes!"
                    },
                    supportingExamples = new string[] { "Warm festival wishes to your family!" },
                    audioFileName = "A card for any festival your family celebrates.mp3"
                }
            };
        }

        if (wordBankRail == null || wordBankRail.Length < 11) {
            wordBankRail = new W02WordBankEntry[] {
                new W02WordBankEntry { greetingText = "Wish you a very happy birthday!", occasionCategory = "birthday" },
                new W02WordBankEntry { greetingText = "Belated birthday wishes!", occasionCategory = "belated birthday" },
                new W02WordBankEntry { greetingText = "Wish you a Happy Diwali!", occasionCategory = "Diwali" },
                new W02WordBankEntry { greetingText = "Joy and prosperity this Diwali!", occasionCategory = "Diwali" },
                new W02WordBankEntry { greetingText = "Eid Mubarak!", occasionCategory = "Eid" },
                new W02WordBankEntry { greetingText = "Happy Easter to you!", occasionCategory = "Easter" },
                new W02WordBankEntry { greetingText = "Merry Christmas to you!", occasionCategory = "Christmas" },
                new W02WordBankEntry { greetingText = "Happy New Year!", occasionCategory = "New Year" },
                new W02WordBankEntry { greetingText = "Happy Independence Day!", occasionCategory = "Independence Day" },
                new W02WordBankEntry { greetingText = "Happy Gandhi Jayanti!", occasionCategory = "Gandhi Jayanti" },
                new W02WordBankEntry { greetingText = "Happy Gurupurab! Guru Nanak Jayanti!", occasionCategory = "Gurupurab" }
            };
        }
    }

    private void PlayIntroVoiceover() {
        AudioClip clip = null;
#if UNITY_EDITOR
        clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Writing/Choose the right greeting for the occasion and make your card warm.mp3");
        #endif
        if (clip != null) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
            AudioSource localSource = GetComponent<AudioSource>();
            if (localSource == null) localSource = gameObject.AddComponent<AudioSource>();
            localSource.Stop();
            localSource.clip = clip;
            localSource.volume = 1.0f;
            localSource.spatialBlend = 0f;
            localSource.Play();
        }
    }

    private void SetupUIBindings() {
        if (studentInputField == null) {
            studentInputField = GetComponentInChildren<TMP_InputField>(true);
        }

        if (studentInputField != null) {
            studentInputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
            TMP_Text placeholder = studentInputField.placeholder as TMP_Text;
            if (placeholder != null) {
                placeholder.text = "Write your card message here (2-3 lines)...";
            }
        }

        if (submitButton == null) {
            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (bName.Contains("submit") || bName.Contains("check") || bName.Contains("btn")) {
                    submitButton = b;
                    break;
                }
            }
        }

        if (submitButton != null) {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnCardSubmitted);
        }

        // Setup Word Bank buttons
        SetupWordBankRailUI();
    }

    private void SetupWordBankRailUI() {
        if (starterChipButtons != null && starterChipButtons.Length > 0 && wordBankRail != null) {
            for (int i = 0; i < starterChipButtons.Length; i++) {
                if (starterChipButtons[i] == null) continue;
                int idx = i;
                if (i < wordBankRail.Length) {
                    starterChipButtons[i].gameObject.SetActive(true);
                    if (starterChipTMPs != null && starterChipTMPs.Length > i && starterChipTMPs[i] != null) {
                        starterChipTMPs[i].text = wordBankRail[i].greetingText;
                    }
                    starterChipButtons[i].onClick.RemoveAllListeners();
                    starterChipButtons[i].onClick.AddListener(() => OnWordBankItemTapped(wordBankRail[idx].greetingText));
                } else {
                    starterChipButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void OnWordBankItemTapped(string greetingText) {
        if (isCardSealed || studentInputField == null || string.IsNullOrEmpty(greetingText)) return;

        // Play soft key click SFX
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        string currentText = studentInputField.text ?? "";

        // If greeting is already present, do not duplicate
        if (currentText.Contains(greetingText)) return;

        if (string.IsNullOrWhiteSpace(currentText)) {
            studentInputField.text = greetingText;
        } else {
            studentInputField.text = currentText.TrimEnd() + "\n" + greetingText;
        }

        studentInputField.caretPosition = studentInputField.text.Length;
        studentInputField.Select();
        studentInputField.ActivateInputField();
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("REWRITE") || textVal.Contains("CHANGE THE TONE") || textVal.Contains("Polished") || textVal.Contains("W02") || textVal.Contains("Fill") || textVal.Contains("Blank") || textVal.Contains("INTRODUCE")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "WRITE A CELEBRATION CARD — GROOVE ON";
            }

            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("WRITING")) {
                tmp.text = "WRITING BRANCH (Word Craft)";
            }

            if (textVal.Contains("Target Tone") || textVal.Contains("Tone:") || textVal.Contains("FORMAL") || textVal.Contains("INFORMAL") || lowerName.Contains("tone")) {
                tmp.text = "";
                tmp.gameObject.SetActive(false);
            }
        }
    }

    private void LoadCardPrompt(int index) {
        if (cardPrompts == null || index < 0 || index >= cardPrompts.Length) {
            EvaluateW02Completion();
            return;
        }

        currentPromptIndex = index;
        isCardSealed = false;

        W02CardPromptData prompt = cardPrompts[index];

        // Display Occasion Prompt text
        TMP_Text promptComp = GetOccasionPromptTMP();
        if (promptComp != null) {
            promptComp.gameObject.SetActive(true);
            promptComp.text = prompt.promptDescription;
        }

        // Reset Feedback Message
        if (feedbackMessageTMP != null) {
            feedbackMessageTMP.text = "";
            feedbackMessageTMP.gameObject.SetActive(false);
        }

        // Reset Card Seal Badge
        if (cardSealBadgeObj != null) {
            cardSealBadgeObj.SetActive(false);
        }

        // Update Progress Counter
        TMP_Text progressComp = GetProgressCounterTMP();
        if (progressComp != null) {
            progressComp.gameObject.SetActive(true);
            progressComp.text = $"{index + 1} / {cardPrompts.Length}";
        }

        // Reset Input Field
        if (studentInputField != null) {
            studentInputField.text = "";
            studentInputField.interactable = true;
            if (studentInputFieldBackgroundImage != null) {
                studentInputFieldBackgroundImage.color = defaultInputColor;
            }
            studentInputField.Select();
            studentInputField.ActivateInputField();
        }

        if (submitButton != null) {
            submitButton.interactable = true;
        }

        // Play Prompt Voiceover for subsequent prompts (index > 0) to avoid double audio on start
        if (index > 0) {
            PlayPromptAudio(prompt);
        }
    }

    private void PlayPromptAudio(W02CardPromptData prompt) {
        if (prompt == null || string.IsNullOrEmpty(prompt.audioFileName)) return;
        string path = $"Assets/Audio/2A/6_GrooveOn/Writing/{prompt.audioFileName}";
        AudioClip clip = null;
#if UNITY_EDITOR
        clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        #endif
        if (clip != null) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
            AudioSource localSource = GetComponent<AudioSource>();
            if (localSource == null) localSource = gameObject.AddComponent<AudioSource>();
            localSource.Stop();
            localSource.clip = clip;
            localSource.volume = 1.0f;
            localSource.spatialBlend = 0f;
            localSource.Play();
        }
    }

    private TMP_Text GetOccasionPromptTMP() {
        if (occasionPromptTMP != null) return occasionPromptTMP;
        if (npcSpeechBubbleTMP != null) return npcSpeechBubbleTMP;

        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t == null) continue;
            string n = t.name.ToLower();
            string txt = t.text ?? "";
            if (n.Contains("dialogue") || n.Contains("prompt") || n.Contains("card") || n.Contains("speech") || txt.Contains("card") || txt.Contains("birthday") || txt.Contains("Diwali")) {
                return t;
            }
        }
        return null;
    }

    private TMP_Text GetProgressCounterTMP() {
        if (progressCounterTMP != null) return progressCounterTMP;

        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t == null) continue;
            string n = t.name.ToLower();
            string txt = t.text ?? "";
            if (n.Contains("progress") || n.Contains("count") || txt.Contains("0/") || txt.Contains("/")) {
                return t;
            }
        }
        return null;
    }

    public void OnCardSubmitted() {
        if (isCardSealed || cardPrompts == null || currentPromptIndex >= cardPrompts.Length) return;
        if (studentInputField == null) return;

        string userMessage = studentInputField.text;
        if (string.IsNullOrWhiteSpace(userMessage)) {
            ShowGentleFeedback("Please write a message for the card using the word bank!");
            return;
        }

        W02CardPromptData currentPrompt = cardPrompts[currentPromptIndex];

        // Soft key sound on submit
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        // Validate by scanning for a verbatim greeting matching the prompt's occasion
        bool isValid = ValidateCardGreeting(userMessage, currentPrompt);

        if (isValid) {
            StartCoroutine(HandleCardPassed(currentPrompt, userMessage));
        } else {
            HandleCardFailed(currentPrompt);
        }
    }

    private bool ValidateCardGreeting(string userText, W02CardPromptData prompt) {
        if (string.IsNullOrWhiteSpace(userText) || prompt == null || prompt.acceptedGreetings == null) return false;

        string normalizedUser = NormalizeText(userText);

        foreach (string targetGreeting in prompt.acceptedGreetings) {
            if (string.IsNullOrWhiteSpace(targetGreeting)) continue;
            string normalizedTarget = NormalizeText(targetGreeting);

            if (normalizedUser.Contains(normalizedTarget)) {
                return true;
            }
        }

        return false;
    }

    private string NormalizeText(string rawText) {
        if (string.IsNullOrEmpty(rawText)) return "";
        // Lowercase, trim spaces, normalize punctuation/whitespace
        string clean = rawText.Trim().ToLowerInvariant();
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", " ");
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"[^\w\s]", "");
        return clean.Trim();
    }

    private IEnumerator HandleCardPassed(W02CardPromptData prompt, string fullCardText) {
        isCardSealed = true;
        passedPromptCount++;

        if (submitButton != null) submitButton.interactable = false;
        if (studentInputField != null) studentInputField.interactable = false;

        // Play SFX_KEY & SFX_CARD_SEAL
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        // Card Seal Festive Animation
        if (studentInputFieldBackgroundImage != null) {
            studentInputFieldBackgroundImage.DOColor(new Color(0.13f, 0.77f, 0.36f, 0.4f), 0.35f);
        }

        if (cardSealBadgeObj != null) {
            cardSealBadgeObj.SetActive(true);
            cardSealBadgeObj.transform.localScale = Vector3.zero;
            cardSealBadgeObj.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        // Card Punch Scale
        TMP_Text promptComp = GetOccasionPromptTMP();
        if (promptComp != null) {
            promptComp.transform.DOKill();
            promptComp.transform.DOPunchScale(Vector3.one * 0.10f, 0.35f);
        }

        ShowGentleFeedback("Card Sealed & Sent!");

        yield return new WaitForSeconds(2.2f);

        currentPromptIndex++;
        if (currentPromptIndex < cardPrompts.Length) {
            LoadCardPrompt(currentPromptIndex);
        } else {
            EvaluateW02Completion();
        }
    }

    private void HandleCardFailed(W02CardPromptData prompt) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        if (studentInputFieldBackgroundImage != null) {
            studentInputFieldBackgroundImage.DOKill();
            studentInputFieldBackgroundImage.DOColor(incorrectInputColor, 0.3f).OnComplete(() => {
                studentInputFieldBackgroundImage.DOColor(defaultInputColor, 0.3f);
            });
        }

        if (studentInputField != null) {
            studentInputField.transform.DOKill();
            studentInputField.transform.DOShakePosition(0.35f, new Vector3(12f, 0, 0));
        }

        ShowGentleFeedback("Try adding the greeting from the word bank that matches this occasion.");
    }

    private void ShowGentleFeedback(string message) {
        if (feedbackMessageTMP != null) {
            feedbackMessageTMP.gameObject.SetActive(true);
            feedbackMessageTMP.text = message;
        }
    }

    private void EvaluateW02Completion() {
        Debug.Log($"[W02 Write a Celebration Card] Finished! Score: {passedPromptCount}/{cardPrompts.Length}");

        if (passedPromptCount >= 3) {
            // Success condition: Passed at least 3 out of 4 prompts
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
            // Retry flow if < 3 passed
            Debug.LogWarning($"[W02] Score {passedPromptCount}/4 is below 3/4 requirement. Restarting W02 activity.");
            RestartActivity();
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Writing;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO == null) {
#if UNITY_EDITOR
            nextLessonSO = UnityEditor.AssetDatabase.LoadAssetAtPath<Masters_LessonSO>("Assets/ScriptableObjects/2A/6_GrooveOn/Speaking/GrooveOn_Speaking_LessonOne.asset");
#endif
        }

        if (nextLessonSO != null && Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Speaking);
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    public void RestartActivity() {
        currentPromptIndex = 0;
        passedPromptCount = 0;
        isCardSealed = false;
        LoadCardPrompt(0);
    }

}