using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit9 {


    /// <summary>
    /// W01 Complete the Phrase
    /// Cloze fill-in writing controller for Book 2B Unit 9 (Prepositions of Time).
    /// Types the missing word to complete verbatim 'in' phrases, time idioms, and phrasal verbs.
    /// Success condition: Student types the correct word in at least 8 of 10 items (one retry per item).
    /// </summary>
    public class Masters_PrepositionsOfTime_Writing_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class PrepositionsOfTime_WritingW01Item {
        public int itemId;
        public string promptText;           // e.g. "in a _____ — when you have no time to spare [in phrase]"
        public string[] acceptedAnswers;    // e.g. ["hurry"]
        public string firstLetterHint;      // e.g. "Hint: Starts with 'h...'"
        public string completedSentence;    // e.g. "in a hurry — when you have no time to spare"
        public AudioClip readbackAudio;     // Audio voiceover
    }
    
        [Header("W01 13 Cloze Fill-in Items (Pool of 13)")]
        [SerializeField] private PrepositionsOfTime_WritingW01Item[] items;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;
        [SerializeField] private TextMeshProUGUI hintTMP;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Image inputFieldBg;

        [Header("Desk / Card Object")]
        [SerializeField] private GameObject deskCardObject;
        [SerializeField] private Button replayAudioBtn;

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
        [SerializeField] private int passScore = 8;
        [SerializeField] private int totalQuestionsToPlay = 10;

        private int currentItemIndex = 0;
        private int correctCount = 0;
        private int attemptsOnCurrentItem = 0;
        private bool isCheckingAnswer = false;

        private readonly Color defaultInputBgColor = Color.white;
        private readonly Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        private readonly Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Writing;
            base.Awake();

            AutoBindReferences();
            InitItemsIfEmpty();
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
            if (submitButton != null) {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
            }

            if (inputField != null) {
                inputField.onSubmit.RemoveAllListeners();
                inputField.onSubmit.AddListener((val) => OnSubmitClicked());
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

                if ((n.Contains("lessontitle") || pn.Contains("lessontitle") || n == "tmp") && titleTMP == null) titleTMP = t;
                else if ((n.Contains("branch") || n.Contains("header") || pn.Contains("branch") || pn.Contains("header")) && headerTMP == null) headerTMP = t;
                else if ((n.Contains("instruction") || n.Contains("subtitle") || pn.Contains("instruction") || pn.Contains("subtitle")) && subtitleTMP == null) subtitleTMP = t;
                else if ((n.Contains("questions text") || n.Contains("question") || n.Contains("prompt")) && promptTMP == null) promptTMP = t;
                else if ((n.Contains("progression count") || n.Contains("progress") || n.Contains("count")) && progressTMP == null) progressTMP = t;
                else if (n.Contains("score") && scoreTMP == null) scoreTMP = t;
                else if (n.Contains("feedback") && feedbackTMP == null) feedbackTMP = t;
                else if ((n.Contains("hint text") || n.Contains("hint")) && hintTMP == null && !n.Equals("hint")) hintTMP = t;
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

            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns) {
                string bn = b.gameObject.name.ToLower();
                if ((bn.Contains("submit") || bn.Contains("check") || bn.Contains("enter")) && submitButton == null) submitButton = b;
                else if (bn.Contains("replay") && replayAudioBtn == null) replayAudioBtn = b;
                else if (bn.Contains("retry") && retryBtn == null) retryBtn = b;
                else if ((bn.Contains("return") || bn.Contains("hub")) && returnHubBtn == null) returnHubBtn = b;
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel");
                if (rTrans != null) resultPanel = rTrans.gameObject;
            }
        }

        public void RestartLesson() {
            currentItemIndex = 0;
            correctCount = 0;
            attemptsOnCurrentItem = 0;
            isCheckingAnswer = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (deskCardObject != null) deskCardObject.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            ShowCurrentItem();
        }

        private void ShowCurrentItem() {
            if (items == null || items.Length == 0) return;

            if (currentItemIndex >= items.Length || currentItemIndex >= totalQuestionsToPlay) {
                ShowResults();
                return;
            }

            var item = items[currentItemIndex];
            attemptsOnCurrentItem = 0;
            isCheckingAnswer = false;

            if (titleTMP != null) titleTMP.text = "W01 Complete the Phrase";
            if (headerTMP != null) headerTMP.text = "PREPOSITIONS OF TIME ⏰";
            if (subtitleTMP != null) subtitleTMP.text = "Type the missing word to complete the phrase.";

            if (promptTMP != null) promptTMP.text = item.promptText;
            if (progressTMP != null) progressTMP.text = $"{currentItemIndex + 1}/{Mathf.Min(items.Length, totalQuestionsToPlay)}";
            if (hintTMP != null) hintTMP.text = "";
            if (feedbackTMP != null) feedbackTMP.text = "";

            if (inputField != null) {
                inputField.text = "";
                inputField.interactable = true;
                inputField.ActivateInputField();
            }

            if (inputFieldBg != null) {
                inputFieldBg.color = defaultInputBgColor;
            }

            if (submitButton != null) {
                submitButton.interactable = true;
            }
        }

        private void OnSubmitClicked() {
            if (isCheckingAnswer || items == null || currentItemIndex >= items.Length) return;

            string userInput = inputField != null ? inputField.text.Trim().ToLower() : "";
            if (string.IsNullOrEmpty(userInput)) return;

            var item = items[currentItemIndex];
            bool isCorrect = false;

            if (item.acceptedAnswers != null) {
                foreach (var ans in item.acceptedAnswers) {
                    if (string.Equals(userInput, ans.Trim().ToLower(), System.StringComparison.OrdinalIgnoreCase)) {
                        isCorrect = true;
                        break;
                    }
                }
            }

            StartCoroutine(EvaluateAnswerRoutine(isCorrect, item));
        }

        private IEnumerator EvaluateAnswerRoutine(bool isCorrect, PrepositionsOfTime_WritingW01Item item) {
            isCheckingAnswer = true;
            if (inputField != null) inputField.interactable = false;
            if (submitButton != null) submitButton.interactable = false;

            if (isCorrect) {
                correctCount++;
                if (inputFieldBg != null) inputFieldBg.color = correctColor;
                if (feedbackTMP != null) {
                    feedbackTMP.color = correctColor;
                    feedbackTMP.text = "<b>Correct!</b>";
                }

                // Show completed phrase in prompt
                if (promptTMP != null && !string.IsNullOrEmpty(item.completedSentence)) {
                    promptTMP.text = $"<color=#33CC66>{item.completedSentence}</color>";
                }

                if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                }

                yield return new WaitForSeconds(2.0f);

                currentItemIndex++;
                ShowCurrentItem();
            } else {
                attemptsOnCurrentItem++;
                if (inputFieldBg != null) inputFieldBg.color = wrongColor;

                if (attemptsOnCurrentItem == 1) {
                    // Retry with first letter hint
                    if (feedbackTMP != null) {
                        feedbackTMP.color = wrongColor;
                        feedbackTMP.text = "<b>Try again!</b>";
                    }
                    if (hintTMP != null) {
                        hintTMP.text = !string.IsNullOrEmpty(item.firstLetterHint) ? item.firstLetterHint : $"Hint: Starts with '{item.acceptedAnswers[0][0]}...'";
                    }

                    yield return new WaitForSeconds(1.2f);

                    if (inputFieldBg != null) inputFieldBg.color = defaultInputBgColor;
                    if (inputField != null) {
                        inputField.text = "";
                        inputField.interactable = true;
                        inputField.ActivateInputField();
                    }
                    if (submitButton != null) submitButton.interactable = true;
                    isCheckingAnswer = false;
                } else {
                    // Second failure: show correct answer and proceed
                    if (feedbackTMP != null) {
                        feedbackTMP.color = wrongColor;
                        feedbackTMP.text = $"<b>Answer: {item.acceptedAnswers[0]}</b>";
                    }

                    if (promptTMP != null && !string.IsNullOrEmpty(item.completedSentence)) {
                        promptTMP.text = $"<color=#FFAA33>{item.completedSentence}</color>";
                    }

                    if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                    }

                    yield return new WaitForSeconds(2.2f);

                    currentItemIndex++;
                    ShowCurrentItem();
                }
            }
        }

        private void ReplayCurrentAudio() {
            if (items == null || currentItemIndex >= items.Length) return;
            var item = items[currentItemIndex];
            if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
            }
        }

        private void ShowResults() {
            if (deskCardObject != null) deskCardObject.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(true);

            int totalCount = Mathf.Min(items != null ? items.Length : 10, totalQuestionsToPlay);
            bool passed = correctCount >= passScore;

            if (resultTitleTMP != null) resultTitleTMP.text = passed ? "Lesson Complete!" : "Keep Practicing!";
            if (resultScoreTMP != null) resultScoreTMP.text = $"{correctCount}/{totalCount}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? $"Great Job! You scored {correctCount}/{totalCount}!" : $"You got {correctCount}/{totalCount}. Try again to score at least {passScore}!";
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

        private void InitItemsIfEmpty() {
            if (items != null && items.Length > 0) return;

            items = new PrepositionsOfTime_WritingW01Item[] {
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 1,
                    promptText = "in a _____ — when you have no time to spare [in phrase]",
                    acceptedAnswers = new string[] { "hurry" },
                    firstLetterHint = "Hint: Starts with 'h...'",
                    completedSentence = "in a hurry — when you have no time to spare"
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 2,
                    promptText = "in _____ — paying before, not after [in phrase]",
                    acceptedAnswers = new string[] { "advance" },
                    firstLetterHint = "Hint: Starts with 'a...'",
                    completedSentence = "in advance — paying before, not after"
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 3,
                    promptText = "in other _____ — saying the same thing differently [in phrase]",
                    acceptedAnswers = new string[] { "words" },
                    firstLetterHint = "Hint: Starts with 'w...'",
                    completedSentence = "in other words — saying the same thing differently"
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 4,
                    promptText = "in the _____ — while you wait for something else [in phrase]",
                    acceptedAnswers = new string[] { "meantime" },
                    firstLetterHint = "Hint: Starts with 'm...'",
                    completedSentence = "in the meantime — while you wait for something else"
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 5,
                    promptText = "in _____ — speaking face to face, not on the phone [in phrase]",
                    acceptedAnswers = new string[] { "person" },
                    firstLetterHint = "Hint: Starts with 'p...'",
                    completedSentence = "in person — speaking face to face, not on the phone"
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 6,
                    promptText = "in _____ of — being the one responsible [in phrase]",
                    acceptedAnswers = new string[] { "charge" },
                    firstLetterHint = "Hint: Starts with 'c...'",
                    completedSentence = "in charge of — being the one responsible"
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 7,
                    promptText = "in the _____ — not knowing what is going on [in phrase]",
                    acceptedAnswers = new string[] { "dark" },
                    firstLetterHint = "Hint: Starts with 'd...'",
                    completedSentence = "in the dark — not knowing what is going on"
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 8,
                    promptText = "Time _____ . [time idiom]",
                    acceptedAnswers = new string[] { "flies" },
                    firstLetterHint = "Hint: Starts with 'f...'",
                    completedSentence = "Time flies."
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 9,
                    promptText = "In the nick of _____ . [time idiom]",
                    acceptedAnswers = new string[] { "time" },
                    firstLetterHint = "Hint: Starts with 't...'",
                    completedSentence = "In the nick of time."
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 10,
                    promptText = "Beat the _____ . [time idiom]",
                    acceptedAnswers = new string[] { "clock" },
                    firstLetterHint = "Hint: Starts with 'c...'",
                    completedSentence = "Beat the clock."
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 11,
                    promptText = "Call it a _____ . [time idiom]",
                    acceptedAnswers = new string[] { "day" },
                    firstLetterHint = "Hint: Starts with 'd...'",
                    completedSentence = "Call it a day."
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 12,
                    promptText = "Figure _____ — solve something [phrasal verb]",
                    acceptedAnswers = new string[] { "out" },
                    firstLetterHint = "Hint: Starts with 'o...'",
                    completedSentence = "Figure out — solve something"
                },
                new PrepositionsOfTime_WritingW01Item {
                    itemId = 13,
                    promptText = "Calm _____ — relax after being angry [phrasal verb]",
                    acceptedAnswers = new string[] { "down" },
                    firstLetterHint = "Hint: Starts with 'd...'",
                    completedSentence = "Calm down — relax after being angry"
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
