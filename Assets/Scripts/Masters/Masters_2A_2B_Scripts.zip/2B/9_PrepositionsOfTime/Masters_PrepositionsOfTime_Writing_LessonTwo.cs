using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit9 {

    

    /// <summary>
    /// W02 Write the Week
    /// Guided preposition writing controller for Book 2B Unit 9 (Prepositions of Time).
    /// Writes 3 pages: WEEK PLAN (in/on/at), NOTE TO A FRIEND ('in' phrases), and STORY (time idiom).
    /// Word-bank rail chips can be tapped to append phrases directly into the text field.
    /// Success condition: Student writes a valid answer for at least 2 of 3 pages (one retry each).
    /// </summary>
    public class Masters_PrepositionsOfTime_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class PrepositionsOfTime_WritingW02Card {
        public int cardId;
        public string cardTitle;              // e.g. "PAGE 1: WEEK PLAN"
        public string scenarioDescription;    // e.g. "Write THREE lines about your week using a long stretch, a day, and an exact time."
        public string promptGuide;            // e.g. "Write your weekly plan with in, on, and at:"
        public string modelAnswer;            // e.g. "I have a music class in the evening. My test is on Tuesday. The bus leaves at 4 o'clock."
        public string[] wordBankChips;        // quick-insert chips
        public string[] requiredKeywords;     // Keywords for validation
        public AudioClip cardAudio;
        public AudioClip modelAudio;
    }
    
        [Header("W02 3 Writing Card Scenarios")]
        [SerializeField] private PrepositionsOfTime_WritingW02Card[] cards;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;

        [Header("Writing Card Stage")]
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
                returnHubBtn.onClick.AddListener(OnReturnToHub);
            }
        }

        public void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps) {
                string n = t.gameObject.name.ToLower();
                Transform p = t.transform.parent;
                string pn = p != null ? p.name.ToLower() : "";

                if ((n.Contains("lessontitle") || pn.Contains("lessontitle") || (n == "tmp" && pn.Contains("title"))) && titleTMP == null) titleTMP = t;
                else if ((n.Contains("branch") || n.Contains("header") || pn.Contains("branch") || pn.Contains("header")) && headerTMP == null) headerTMP = t;
                else if ((n.Contains("instruction") || n.Contains("subtitle") || pn.Contains("instruction") || pn.Contains("subtitle")) && subtitleTMP == null) subtitleTMP = t;
                else if ((n.Contains("progress") || n.Contains("count")) && progressTMP == null) progressTMP = t;
                else if (n.Contains("score") && scoreTMP == null) scoreTMP = t;
                else if (n.Contains("striptitle") && cardTitleTMP == null) cardTitleTMP = t;
                else if (n.Contains("situationdesc") && scenarioDescTMP == null) scenarioDescTMP = t;
                else if (n.Contains("speakera") && promptGuideTMP == null) promptGuideTMP = t;
                else if (n.Contains("feedbacktext") && feedbackTextTMP == null) feedbackTextTMP = t;
                else if (n.Contains("resulttitle") && resultTitleTMP == null) resultTitleTMP = t;
                else if (n.Contains("resultscore") && resultScoreTMP == null) resultScoreTMP = t;
                else if (n.Contains("resultstatus") && resultStatusTMP == null) resultStatusTMP = t;
            }

            if (inputField == null) {
                inputField = GetComponentInChildren<TMP_InputField>(true);
            }
            if (inputField != null && inputFieldBg == null) {
                inputFieldBg = inputField.GetComponent<Image>();
            }

            // Word bank chips
            List<Button> chipBtns = new List<Button>();
            List<TextMeshProUGUI> chipTexts = new List<TextMeshProUGUI>();
            Transform rail = transform.Find("ComicStageCard/WordBankRail");
            if (rail == null) rail = transform.Find("WordBankRail");

            if (rail != null) {
                foreach (Transform child in rail) {
                    Button b = child.GetComponent<Button>();
                    if (b != null) {
                        chipBtns.Add(b);
                        TextMeshProUGUI t = child.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (t != null) chipTexts.Add(t);
                    }
                }
            }

            if (chipBtns.Count > 0) {
                wordBankChipButtons = chipBtns.ToArray();
                wordBankChipTexts = chipTexts.ToArray();
            }

            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns) {
                string bn = b.gameObject.name.ToLower();
                if ((bn.Contains("submit") || bn.Contains("check")) && submitBtn == null) submitBtn = b;
                else if (bn.Contains("replay") && replayAudioBtn == null) replayAudioBtn = b;
                else if (bn.Contains("retry") && retryBtn == null) retryBtn = b;
                else if ((bn.Contains("return") || bn.Contains("hub")) && returnHubBtn == null) returnHubBtn = b;
            }

            if (cardObject == null) {
                Transform cTrans = transform.Find("ComicStageCard");
                if (cTrans != null) cardObject = cTrans.gameObject;
            }

            if (feedbackBanner == null) {
                Transform fbTrans = transform.Find("ComicStageCard/FeedbackBanner");
                if (fbTrans == null) fbTrans = transform.Find("FeedbackBanner");
                if (fbTrans != null) feedbackBanner = fbTrans.gameObject;
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel");
                if (rTrans != null) resultPanel = rTrans.gameObject;
            }
        }

        public void RestartLesson() {
            currentCardIndex = 0;
            score = 0;
            attemptsOnCurrentCard = 0;
            isCheckingAnswer = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (cardObject != null) cardObject.SetActive(true);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            ShowCurrentCard();
        }

        private void ShowCurrentCard() {
            if (cards == null || cards.Length == 0) return;

            if (currentCardIndex >= cards.Length) {
                ShowResults();
                return;
            }

            var card = cards[currentCardIndex];
            attemptsOnCurrentCard = 0;
            isCheckingAnswer = false;

            if (titleTMP != null) titleTMP.text = "W02 Write the Week";
            if (headerTMP != null) headerTMP.text = "PREPOSITIONS OF TIME ⏰";
            if (subtitleTMP != null) subtitleTMP.text = "Write the required lines using the word-bank rail.";

            if (progressTMP != null) progressTMP.text = $"{currentCardIndex + 1}/{cards.Length}";
            if (scoreTMP != null) scoreTMP.text = $"Score: {score}/{cards.Length}";

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

            // Populate word bank rail chips
            if (wordBankChipButtons != null && card.wordBankChips != null) {
                for (int i = 0; i < wordBankChipButtons.Length; i++) {
                    if (i < card.wordBankChips.Length) {
                        wordBankChipButtons[i].gameObject.SetActive(true);
                        string phrase = card.wordBankChips[i];
                        if (i < wordBankChipTexts.Length && wordBankChipTexts[i] != null) {
                            wordBankChipTexts[i].text = phrase;
                        }
                        wordBankChipButtons[i].onClick.RemoveAllListeners();
                        wordBankChipButtons[i].onClick.AddListener(() => AppendChipText(phrase));
                    } else {
                        wordBankChipButtons[i].gameObject.SetActive(false);
                    }
                }
            }

            if (card.cardAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(card.cardAudio);
            }
        }

        private void AppendChipText(string phrase) {
            if (inputField == null || !inputField.interactable) return;

            string current = inputField.text.Trim();
            if (string.IsNullOrEmpty(current)) {
                inputField.text = phrase;
            } else {
                if (!current.EndsWith(".") && !current.EndsWith(",") && !current.EndsWith("!")) {
                    inputField.text = current + " " + phrase;
                } else {
                    inputField.text = current + " " + phrase;
                }
            }
            inputField.ActivateInputField();
            inputField.caretPosition = inputField.text.Length;
        }

        private void OnSubmitClicked() {
            if (isCheckingAnswer || cards == null || currentCardIndex >= cards.Length) return;

            string userInput = inputField != null ? inputField.text.Trim().ToLower() : "";
            if (string.IsNullOrEmpty(userInput)) return;

            var card = cards[currentCardIndex];
            bool isValid = ValidateInputForCard(currentCardIndex, userInput);

            StartCoroutine(EvaluateCardRoutine(isValid, card));
        }

        private bool ValidateInputForCard(int index, string input) {
            if (string.IsNullOrEmpty(input)) return false;

            // Page 1: WEEK PLAN - needs in, on, at (or days/times/periods)
            if (index == 0) {
                bool hasIn = input.Contains(" in ") || input.StartsWith("in ") || input.Contains("morning") || input.Contains("evening") || input.Contains("afternoon");
                bool hasOn = input.Contains(" on ") || input.StartsWith("on ") || input.Contains("monday") || input.Contains("tuesday") || input.Contains("wednesday") || input.Contains("thursday") || input.Contains("friday") || input.Contains("saturday") || input.Contains("sunday");
                bool hasAt = input.Contains(" at ") || input.StartsWith("at ") || input.Contains("o'clock") || input.Contains("pm") || input.Contains("am") || input.Contains("noon") || input.Contains("night");
                return (hasIn && hasOn) || (hasIn && hasAt) || (hasOn && hasAt) || input.Length > 20;
            }
            // Page 2: NOTE TO A FRIEND - needs 'in' phrases
            else if (index == 1) {
                int matchedPhrases = 0;
                string[] validPhrases = { "hurry", "advance", "meantime", "person", "words", "charge", "dark", "time" };
                foreach (var p in validPhrases) {
                    if (input.Contains(p)) matchedPhrases++;
                }
                return matchedPhrases >= 2 || (input.Contains("in ") && input.Length > 25);
            }
            // Page 3: STORY - needs a time idiom & situation explanation
            else if (index == 2) {
                bool hasIdiom = input.Contains("eleventh hour") || input.Contains("time flies") || input.Contains("beat the clock") ||
                                input.Contains("call it a day") || input.Contains("nick of time") || input.Contains("charm") ||
                                input.Contains("better late") || input.Contains("long run") || input.Contains("lost time") ||
                                input.Contains("sailed") || input.Contains("around the clock") || input.Contains("clock");
                bool hasContext = input.Contains("when") || input.Contains("use") || input.Contains("moment") || input.Contains("situation") || input.Contains("because") || input.Length > 25;
                return hasIdiom && hasContext;
            }

            return input.Length > 15;
        }

        private IEnumerator EvaluateCardRoutine(bool isCorrect, PrepositionsOfTime_WritingW02Card card) {
            isCheckingAnswer = true;
            if (inputField != null) inputField.interactable = false;
            if (submitBtn != null) submitBtn.interactable = false;

            if (isCorrect) {
                score++;
                if (inputFieldBg != null) inputFieldBg.color = correctColor;

                if (feedbackBanner != null) {
                    feedbackBanner.SetActive(true);
                    if (feedbackTextTMP != null) {
                        feedbackTextTMP.color = correctColor;
                        feedbackTextTMP.text = $"<b>Great Writing!</b>\n<size=85%>{card.modelAnswer}</size>";
                    }
                }

                if (card.modelAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(card.modelAudio);
                }

                yield return new WaitForSeconds(3.0f);

                currentCardIndex++;
                ShowCurrentCard();
            } else {
                attemptsOnCurrentCard++;
                if (inputFieldBg != null) inputFieldBg.color = wrongColor;

                if (attemptsOnCurrentCard == 1) {
                    // Retry with gentle guidance
                    if (feedbackBanner != null) {
                        feedbackBanner.SetActive(true);
                        if (feedbackTextTMP != null) {
                            feedbackTextTMP.color = wrongColor;
                            feedbackTextTMP.text = "<b>Check your prepositions and word bank chips! Try once more.</b>";
                        }
                    }

                    yield return new WaitForSeconds(2.0f);

                    if (inputFieldBg != null) inputFieldBg.color = defaultInputBgColor;
                    if (inputField != null) {
                        inputField.interactable = true;
                        inputField.ActivateInputField();
                    }
                    if (submitBtn != null) submitBtn.interactable = true;
                    isCheckingAnswer = false;
                } else {
                    // Second failure: show model answer and advance
                    if (feedbackBanner != null) {
                        feedbackBanner.SetActive(true);
                        if (feedbackTextTMP != null) {
                            feedbackTextTMP.color = wrongColor;
                            feedbackTextTMP.text = $"<b>Model Answer:</b>\n<size=85%>{card.modelAnswer}</size>";
                        }
                    }

                    if (card.modelAudio != null && Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(card.modelAudio);
                    }

                    yield return new WaitForSeconds(3.2f);

                    currentCardIndex++;
                    ShowCurrentCard();
                }
            }
        }

        private void ReplayCurrentAudio() {
            if (cards == null || currentCardIndex >= cards.Length) return;
            var card = cards[currentCardIndex];
            if (card.cardAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(card.cardAudio);
            }
        }

        private void ShowResults() {
            if (cardObject != null) cardObject.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(true);

            int totalCards = cards != null ? cards.Length : 3;
            bool passed = score >= passScore;

            if (resultTitleTMP != null) resultTitleTMP.text = passed ? "Writing Complete!" : "Keep Practicing!";
            if (resultScoreTMP != null) resultScoreTMP.text = $"{score}/{totalCards}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? $"Excellent! You successfully wrote {score}/{totalCards} pages!" : $"You wrote {score}/{totalCards} pages. Try again to pass at least {passScore} pages!";
                resultStatusTMP.color = passed ? correctColor : wrongColor;
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
        }

        private void OnReturnToHub() {
            if (nextButton != null) {
                nextButton.onClick.Invoke();
            }
        }

        private void InitCardsIfEmpty() {
            if (cards != null && cards.Length > 0) return;

            cards = new PrepositionsOfTime_WritingW02Card[] {
                new PrepositionsOfTime_WritingW02Card {
                    cardId = 1,
                    cardTitle = "PAGE 1: WEEK PLAN",
                    scenarioDescription = "Write THREE lines about your week using a long stretch (in the evening), a day (on Tuesday) and an exact time (at 4 o'clock).",
                    promptGuide = "Write your weekly plan with in, on, and at:",
                    modelAnswer = "I have a music class in the evening. My test is on Tuesday. The bus leaves at 4 o'clock.",
                    wordBankChips = new string[] { "in the evening", "on Tuesday", "at 4 o'clock", "in the morning", "on Friday", "at 9 am" },
                    requiredKeywords = new string[] { "in", "on", "at" }
                },
                new PrepositionsOfTime_WritingW02Card {
                    cardId = 2,
                    cardTitle = "PAGE 2: NOTE TO A FRIEND",
                    scenarioDescription = "Write THREE lines to a friend using three different phrases from the 'in' bank (in a hurry, in advance, in the meantime).",
                    promptGuide = "Write a note using three distinct 'in' phrases:",
                    modelAnswer = "I'm in a hurry. I'll pay in advance. In the meantime, keep the book with you.",
                    wordBankChips = new string[] { "in a hurry", "in advance", "in the meantime", "in person", "in other words", "in charge of" },
                    requiredKeywords = new string[] { "hurry", "advance", "meantime" }
                },
                new PrepositionsOfTime_WritingW02Card {
                    cardId = 3,
                    cardTitle = "PAGE 3: STORY",
                    scenarioDescription = "Pick one time idiom, write a sentence using it, and write the situation where you would use it.",
                    promptGuide = "Write your sentence with a time idiom and explain the situation:",
                    modelAnswer = "We finished the project at the eleventh hour. — You use it when something is done at the very last moment.",
                    wordBankChips = new string[] { "at the eleventh hour", "time flies", "beat the clock", "call it a day", "in the nick of time", "third time's a charm" },
                    requiredKeywords = new string[] { "eleventh hour", "when", "moment" }
                }
            };
        }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
}
