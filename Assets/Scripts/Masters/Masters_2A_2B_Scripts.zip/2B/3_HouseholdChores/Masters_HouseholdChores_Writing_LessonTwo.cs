using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// Unit 3: Household Chores — Writing Lesson Two (W02 Write the Diwali Job Chart).
/// Guided chore writing with 3 task cards:
/// 1. MY SUNDAY PLAN (5 different chores for Diwali preparation).
/// 2. OFFER TO HELP (Two lines telling Mom what you will do and when).
/// 3. THE FAMILY CHART (Fill 3 columns: ME, MOM, SISTER + thank you).
/// 
/// Audio Flow Fix:
/// - Controls locked while intro audio is speaking; auto-unlocked once intro finishes.
/// - Controls locked while each question card prompt audio speaks; auto-unlocked once the prompt finishes.
/// - When card is submitted, model voiceover plays for its FULL duration before advancing to the next card. No dialogue overlap.
/// </summary>
public class Masters_HouseholdChores_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
public class HouseholdChores_WritingW02Card {
    public int cardId;
    public string cardTitle;              // e.g. "CARD 1: MY SUNDAY PLAN"
    public string scenarioDescription;    // e.g. "Diwali is three days away. Write FIVE different chores you will do."
    public string promptGuide;            // e.g. "Write 5 different chore sentences using the word bank:"
    public string modelAnswer;            // e.g. "Sweep the floor. Wash the car. Take the trash out. Hang out the clothes. Feed the dog."
    public string[] wordBankChips;        // Quick-insert chips
    public string[] requiredChoreKeywords;// Keywords for validation
    public AudioClip cardAudio;
    public AudioClip modelAudio;
}

    [Header("W02 3 Chore Card Scenarios")]
    [SerializeField] public HouseholdChores_WritingW02Card[] cards;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;

    [Header("Chore Card Stage")]
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
    private bool isIntroPhase = true;
    private Coroutine introRoutine;
    private Coroutine cardRoutine;

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
        EnsureHeaderAndTitle();

        if (cards == null || cards.Length == 0) {
            InitCardsIfEmpty();
        }

        // Lock controls during introduction
        isIntroPhase = true;
        isCheckingAnswer = true;
        SetControlsInteractable(false);

        introRoutine = StartCoroutine(BeginLessonRoutine());
    }

    private IEnumerator BeginLessonRoutine() {
        if (progressTMP != null) progressTMP.text = "Introduction";
        if (scenarioDescTMP != null) scenarioDescTMP.text = "Diwali is three days away! Write the chore sentences for each card using the word bank rail.";

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
            titleTMP.text = "W02 Write the Diwali Job Chart";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f);
        }
        if (subtitleTMP != null) subtitleTMP.text = "Write the required chore sentences using the word-bank rail.";
    }

    private void SetControlsInteractable(bool interactable) {
        if (inputField != null) inputField.interactable = interactable;
        if (submitBtn != null) submitBtn.interactable = interactable;
        if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;

        if (wordBankChipButtons != null) {
            foreach (var btn in wordBankChipButtons) {
                if (btn != null) btn.interactable = interactable;
            }
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

        string aDir = "Assets/Audio/2B/3_HouseholdChores/Writing/";

        cards = new HouseholdChores_WritingW02Card[] {
            new HouseholdChores_WritingW02Card {
                cardId = 1,
                cardTitle = "CARD 1: MY SUNDAY PLAN",
                scenarioDescription = "Diwali is three days away. Write FIVE different chores you will do.",
                promptGuide = "Write 5 different chore sentences using the word bank:",
                modelAnswer = "Sweep the floor. Wash the car. Take the trash out. Hang out the clothes. Feed the dog.",
                wordBankChips = new string[] {
                    "Sweep the floor",
                    "Wash the car",
                    "Take the trash out",
                    "Hang out the clothes",
                    "Feed the dog",
                    "Make the bed"
                },
                requiredChoreKeywords = new string[] { "sweep", "dust", "mop", "wash", "car", "trash", "hang", "clothes", "feed", "dog", "bed", "floor", "dishes", "table", "shopping", "gardening", "window" }
#if UNITY_EDITOR
                , cardAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w02_card1_prompt.mp3"),
                modelAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w02_card1_model.mp3")
#endif
            },
            new HouseholdChores_WritingW02Card {
                cardId = 2,
                cardTitle = "CARD 2: OFFER TO HELP",
                scenarioDescription = "Write two lines telling Mom what you will do and when, modelled on Suho.",
                promptGuide = "Write what you will do and when (e.g. 'I will... as soon as...'):",
                modelAnswer = "I will wash the car and do a bit of gardening, too. I'll dust my room as soon as I finish washing the car.",
                wordBankChips = new string[] {
                    "I will wash the car",
                    "do a bit of gardening, too",
                    "I'll dust my room",
                    "as soon as I finish",
                    "wash up the dishes",
                    "set the table"
                },
                requiredChoreKeywords = new string[] { "will", "i'll", "wash", "car", "gardening", "dust", "room", "soon", "finish", "dishes", "table", "help", "mom" }
#if UNITY_EDITOR
                , cardAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w02_card2_prompt.mp3"),
                modelAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w02_card2_model.mp3")
#endif
            },
            new HouseholdChores_WritingW02Card {
                cardId = 3,
                cardTitle = "CARD 3: THE FAMILY CHART",
                scenarioDescription = "Fill the three columns: write one chore for yourself, one for Mom and one for your brother or sister, then thank them.",
                promptGuide = "Write chores for ME, MOM, and SISTER/BROTHER, then add a thank you line:",
                modelAnswer = "ME: I will sweep the floor. MOM: Mom will make the bed. SISTER: My sister will dry the dishes. Thank you. I really don't know what I would have done without your help.",
                wordBankChips = new string[] {
                    "I will sweep the floor",
                    "Mom will make the bed",
                    "My sister will dry the dishes",
                    "Thank you",
                    "without your help",
                    "I really don't know"
                },
                requiredChoreKeywords = new string[] { "me", "mom", "sister", "brother", "sweep", "bed", "dishes", "thank", "help", "floor", "wash", "will", "done", "know" }
#if UNITY_EDITOR
                , cardAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w02_card3_prompt.mp3"),
                modelAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_w02_card3_model.mp3")
#endif
            }
        };
    }

    public void StartLesson() {
        isIntroPhase = false;
        currentCardIndex = 0;
        score = 0;
        attemptsOnCurrentCard = 0;
        isCheckingAnswer = false;

        if (headerTMP != null) headerTMP.gameObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (cardObject != null) cardObject.SetActive(true);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        ShowCard(0);
    }

    public void RestartLesson() {
        if (cardRoutine != null) StopCoroutine(cardRoutine);
        if (introRoutine != null) StopCoroutine(introRoutine);
        if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.StopVoiceOver();
        StartLesson();
    }

    public void ShowCard(int index) {
        if (cardRoutine != null) StopCoroutine(cardRoutine);
        cardRoutine = StartCoroutine(ShowCardRoutine(index));
    }

    private IEnumerator ShowCardRoutine(int index) {
        if (cards == null || cards.Length == 0) yield break;
        currentCardIndex = Mathf.Clamp(index, 0, cards.Length - 1);
        attemptsOnCurrentCard = 0;
        isCheckingAnswer = false;

        HouseholdChores_WritingW02Card card = cards[currentCardIndex];

        UpdateScoreUI();
        EnsureHeaderAndTitle();

        if (cardTitleTMP != null) cardTitleTMP.text = card.cardTitle;
        if (scenarioDescTMP != null) scenarioDescTMP.text = card.scenarioDescription;
        if (promptGuideTMP != null) promptGuideTMP.text = card.promptGuide;

        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        if (inputField != null) {
            inputField.text = "";
        }

        if (inputFieldBg != null) {
            inputFieldBg.color = defaultInputBgColor;
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

        // Lock controls while question dialogue speaks
        SetControlsInteractable(false);

        if (card.cardAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(card.cardAudio);
            yield return new WaitForSeconds(card.cardAudio.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        // Unlock controls and allow student to type/play
        SetControlsInteractable(true);
        if (inputField != null) {
            inputField.ActivateInputField();
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
        if (isIntroPhase || isCheckingAnswer) return;
        if (cards == null || currentCardIndex >= cards.Length) return;
        HouseholdChores_WritingW02Card card = cards[currentCardIndex];
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
        if (isCheckingAnswer || isIntroPhase) return;
        if (cards == null || currentCardIndex >= cards.Length) return;

        string text = inputField != null ? inputField.text.Trim() : "";
        if (string.IsNullOrEmpty(text)) {
            if (feedbackBanner != null) feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) feedbackTextTMP.text = "<color=#FFAA33>Please write your chore sentences or tap the word bank chips before submitting.</color>";
            return;
        }

        StartCoroutine(HandleSubmission(text));
    }

    private IEnumerator HandleSubmission(string text) {
        isCheckingAnswer = true;
        attemptsOnCurrentCard++;

        HouseholdChores_WritingW02Card card = cards[currentCardIndex];
        bool isPass = ValidateStudentWriting(text, card);

        if (isPass) {
            if (attemptsOnCurrentCard == 1) {
                score++;
                UpdateScoreUI();
            }

            if (inputFieldBg != null) inputFieldBg.color = correctColor;
            if (feedbackBanner != null) feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) {
                feedbackTextTMP.text = "<color=#33D866><b>✓ Card Completed! Great chore writing!</b></color>";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Lock controls while model readback audio plays
            SetControlsInteractable(false);

            // Play model readback audio and WAIT for its full duration before advancing to next question
            if (card.modelAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(card.modelAudio);
                yield return new WaitForSeconds(card.modelAudio.length + 0.5f);
            } else {
                yield return new WaitForSeconds(2.0f);
            }

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
                    feedbackTextTMP.text = "<color=#FF6666>Include more chore details or tap the chips in the word bank!</color>";
                }
                isCheckingAnswer = false;
                SetControlsInteractable(true);
                if (inputField != null) {
                    inputField.ActivateInputField();
                }
            } else {
                // Second failure: show model answer & wait for full audio before advancing
                if (feedbackBanner != null) feedbackBanner.SetActive(true);
                if (feedbackTextTMP != null) {
                    feedbackTextTMP.text = $"<color=#FFAA33><b>Exemplar:</b> {card.modelAnswer}</color>";
                }

                SetControlsInteractable(false);

                if (card.modelAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(card.modelAudio);
                    yield return new WaitForSeconds(card.modelAudio.length + 0.5f);
                } else {
                    yield return new WaitForSeconds(3.0f);
                }

                if (currentCardIndex < cards.Length - 1) {
                    ShowCard(currentCardIndex + 1);
                } else {
                    ShowResults();
                }
            }
        }
    }

    private bool ValidateStudentWriting(string text, HouseholdChores_WritingW02Card card) {
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
        if (card.requiredChoreKeywords != null) {
            foreach (var kw in card.requiredChoreKeywords) {
                if (lower.Contains(kw.ToLower())) matched++;
            }
        }

        string[] words = text.Split(new char[] { ' ', '\n', '\t', '.', ',', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);
        return (matched >= 2 || words.Length >= 4);
    }

    public void ReplayCurrentAudio() {
        if (isIntroPhase) {
            AudioClip clip = introAudio != null ? introAudio : narratorSpeech;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
            return;
        }

        if (cards == null || currentCardIndex >= cards.Length) return;
        AudioClip cardClip = cards[currentCardIndex].cardAudio;
        if (cardClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(cardClip);
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
                resultTitleTMP.text = passed ? "DIWALI JOB CHART COMPLETE! 🧹" : "KEEP PRACTICING!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"You completed {score} of {cards.Length} chore writing cards!";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Success! All chore cards have been written and recorded." : $"You need at least {passScore}/{cards.Length} cards completed to pass.";
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

        if (cardRoutine != null) {
            StopCoroutine(cardRoutine);
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.StopVoiceOver();
            SetControlsInteractable(true);
            if (inputField != null) inputField.ActivateInputField();
            return;
        }

        OnReturnHubClicked();
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
