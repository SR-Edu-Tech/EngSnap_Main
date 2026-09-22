using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {

    

    /// <summary>
    /// G02 Quiz Time — Quiz-Show Correction Race
    /// Arcade scoreboard quiz show for Unit 10: Common Mistakes with Prepositions.
    /// Big screen displays wobbly sentence; 3 mended version buttons sit below it.
    /// Student taps the correct mended version.
    /// Correct -> scores point and reveals swapped part explanation; wrong -> reveals correct option.
    /// First to reach 10 points wins!
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class CommonMistakes_GameG02Item {
        public int itemId;
        public string wobblyPrompt;
        public string[] options; // 3 options
        public int correctOptionIndex; // 0, 1, 2
        public string swappedPartExplanation; // e.g. "Preposition: 'in' -> 'to'"
    }
    
        [Header("G02 Quiz Time Items")]
        [SerializeField] private CommonMistakes_GameG02Item[] questions;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;

        [Header("Prompt Stage Card")]
        [SerializeField] private GameObject promptCardObject;
        [SerializeField] private TextMeshProUGUI promptTextTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("3 Option Cards / Buttons")]
        [SerializeField] private Button[] optionButtons; // 3 Option Cards
        [SerializeField] private Image[] optionImages;
        [SerializeField] private TextMeshProUGUI[] optionTexts;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        [Header("Audio References")]
        [SerializeField] private AudioClip introAudio;

        [Header("Game Settings")]
        [SerializeField] private int targetPointsToWin = 10;

        private int score = 0;
        private int currentQuestionIndex = 0;
        private bool isGameActive = false;
        private bool isHandlingAnswer = false;
        private List<int> shuffledIndices = new List<int>();

        private readonly Color defaultOptionColor = new Color(0.12f, 0.35f, 0.65f, 0.95f);
        private readonly Color correctOptionColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private readonly Color wrongOptionColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Game;
            base.Awake();

            AutoBindReferences();
            InitQuestionsIfEmpty();
            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Game;
            AutoBindReferences();
            EnsureHeaderAndTitle();
            EnsureNextAndBackButtonWired();
            EnsureAspectRatiosAndAnchorsPreserved();

            if (introAudio == null) {
#if UNITY_EDITOR
                introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Game/cm_g02_intro.mp3");
#endif
            }

            StartCoroutine(InitializeGameRoutine());
        }

        private IEnumerator InitializeGameRoutine() {
            ResetGameUI();

            if (introAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
                yield return new WaitForSeconds(introAudio.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.5f);
            }

            StartGame();
        }

        private void EnsureHeaderAndTitle() {
            if (headerTMP == null) {
                Transform hTr = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("Branch") ?? transform.Find("HeaderContainer/Branch");
                if (hTr != null) headerTMP = hTr.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "COMMON MISTAKES WITH PREPOSITIONS";
            }

            if (titleTMP == null) {
                Transform tTr = transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("LessonTitle") ?? transform.Find("HeaderContainer/Title") ?? transform.Find("Title");
                if (tTr != null) titleTMP = tTr.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "G02 Quiz Time";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (subtitleTMP == null) {
                Transform sTr = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Subtitle") ?? transform.Find("Instruction");
                if (sTr != null) subtitleTMP = sTr.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleTMP != null) {
                subtitleTMP.text = "Tap the correctly mended sentence as fast as you can!";
            }
        }

        private void AutoBindReferences() {
            EnsureHeaderAndTitle();

            if (scoreTMP == null) {
                Transform scTr = transform.Find("HeaderContainer/ScoreTMP") ?? transform.Find("ScoreTMP") ?? transform.Find("CorrectlyFilledTMP");
                if (scTr != null) scoreTMP = scTr.GetComponent<TextMeshProUGUI>();
            }

            if (progressTMP == null) {
                Transform prTr = transform.Find("HeaderContainer/ProgressTMP") ?? transform.Find("ProgressTMP");
                if (prTr != null) {
                    progressTMP = prTr.GetComponent<TextMeshProUGUI>();
                    progressTMP.gameObject.SetActive(false); // prevent overlap with scoreTMP
                }
            }

            if (promptCardObject == null) {
                Transform pc = transform.Find("IdiomCard") ?? transform.Find("PromptCard") ?? transform.Find("ConveyorCard") ?? transform.Find("CardObject");
                if (pc != null) promptCardObject = pc.gameObject;
            }

            if (promptCardObject != null && promptTextTMP == null) {
                Transform pt = promptCardObject.transform.Find("ExpressionTMP") 
                            ?? promptCardObject.transform.Find("SentenceTMP") 
                            ?? promptCardObject.transform.Find("Text")
                            ?? promptCardObject.transform.Find("StatementTMP");
                if (pt != null) promptTextTMP = pt.GetComponent<TextMeshProUGUI>();
                if (promptTextTMP == null) promptTextTMP = promptCardObject.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (feedbackTMP == null) {
                Transform fTr = transform.Find("FeedbackBanner/FeedbackTextTMP") 
                             ?? transform.Find("FeedbackBanner/Text") 
                             ?? transform.Find("FeedbackTextTMP")
                             ?? transform.Find("FeedbackTMP");
                if (fTr != null) feedbackTMP = fTr.GetComponent<TextMeshProUGUI>();
            }

            // Auto-bind 3 option buttons
            if (optionButtons == null || optionButtons.Length < 3) {
                List<Button> bList = new List<Button>();
                List<Image> iList = new List<Image>();
                List<TextMeshProUGUI> tList = new List<TextMeshProUGUI>();

                Button[] allBtns = GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns) {
                    string bn = b.name.ToLower();
                    if ((bn.Contains("optioncard") || bn.Contains("drawer") || bn.Contains("gate") || bn.Contains("option_")) && !bn.Contains("return") && !bn.Contains("retry")) {
                        bList.Add(b);
                        iList.Add(b.GetComponent<Image>());
                        tList.Add(b.GetComponentInChildren<TextMeshProUGUI>(true));
                    }
                }

                if (bList.Count >= 3) {
                    optionButtons = bList.GetRange(0, 3).ToArray();
                    optionImages = iList.GetRange(0, 3).ToArray();
                    optionTexts = tList.GetRange(0, 3).ToArray();
                }
            }

            if (resultPanel == null) {
                Transform rp = transform.Find("ResultPanel") ?? transform.Find("CompletedPanel") ?? transform.Find("ResultsPanel");
                if (rp != null) resultPanel = rp.gameObject;
            }
            if (resultPanel != null) {
                Transform rSc = resultPanel.transform.Find("ResultScore") ?? resultPanel.transform.Find("ScoreTMP") ?? resultPanel.transform.Find("ResultScoreTMP");
                if (rSc != null) resultScoreTMP = rSc.GetComponent<TextMeshProUGUI>();

                Transform rSt = resultPanel.transform.Find("ResultStatus") ?? resultPanel.transform.Find("StatusTMP") ?? resultPanel.transform.Find("ResultStatusTMP");
                if (rSt != null) resultStatusTMP = rSt.GetComponent<TextMeshProUGUI>();

                Transform rBtn = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                if (rBtn != null) retryBtn = rBtn.GetComponent<Button>();

                Transform hBtn = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("NextButton");
                if (hBtn != null) returnHubBtn = hBtn.GetComponent<Button>();
            }
        }

        private void WireEventListeners() {
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    int optIndex = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionClicked(optIndex));
                }
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartGame);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnReturnToHub);
            }
        }

        private void ResetGameUI() {
            score = 0;
            currentQuestionIndex = 0;
            isGameActive = false;
            isHandlingAnswer = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            UpdateScoreUI();
        }

        public void StartGame() {
            ResetGameUI();
            ShuffleQuestions();
            isGameActive = true;
            currentQuestionIndex = 0;
            DisplayCurrentQuestion();
        }

        public void RestartGame() {
            StartGame();
        }

        private void ShuffleQuestions() {
            shuffledIndices.Clear();
            if (questions == null || questions.Length == 0) return;

            for (int i = 0; i < questions.Length; i++) {
                shuffledIndices.Add(i);
            }

            for (int i = shuffledIndices.Count - 1; i > 0; i--) {
                int r = Random.Range(0, i + 1);
                int tmp = shuffledIndices[i];
                shuffledIndices[i] = shuffledIndices[r];
                shuffledIndices[r] = tmp;
            }
        }

        private void UpdateScoreUI() {
            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score} / {targetPointsToWin}";
            }
        }

        private void DisplayCurrentQuestion() {
            if (questions == null || questions.Length == 0 || shuffledIndices.Count == 0) return;

            int qIdx = shuffledIndices[currentQuestionIndex % shuffledIndices.Count];
            CommonMistakes_GameG02Item q = questions[qIdx];

            isHandlingAnswer = false;

            if (promptTextTMP != null) {
                promptTextTMP.text = $"\"{q.wobblyPrompt}\"";
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "Tap the correctly mended sentence below:";
            }

            if (promptCardObject != null) {
                promptCardObject.transform.localScale = Vector3.one * 0.9f;
                promptCardObject.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }

            ResetOptionColors();
            SetOptionsInteractable(true);

            // Populate option texts
            if (optionTexts != null && q.options != null) {
                for (int i = 0; i < optionTexts.Length && i < q.options.Length; i++) {
                    if (optionTexts[i] != null) {
                        optionTexts[i].text = q.options[i];
                    }
                }
            }
        }

        private void ResetOptionColors() {
            if (optionImages != null) {
                foreach (var img in optionImages) {
                    if (img != null) img.color = defaultOptionColor;
                }
            }
        }

        private void SetOptionsInteractable(bool state) {
            if (optionButtons != null) {
                foreach (var b in optionButtons) {
                    if (b != null) b.interactable = state;
                }
            }
        }

        public void OnOptionClicked(int selectedIndex) {
            if (!isGameActive || isHandlingAnswer) return;
            if (questions == null || questions.Length == 0 || shuffledIndices.Count == 0) return;

            int qIdx = shuffledIndices[currentQuestionIndex % shuffledIndices.Count];
            CommonMistakes_GameG02Item q = questions[qIdx];

            bool isCorrect = (selectedIndex == q.correctOptionIndex);
            StartCoroutine(HandleAnswerRoutine(selectedIndex, q, isCorrect));
        }

        private IEnumerator HandleAnswerRoutine(int selectedIndex, CommonMistakes_GameG02Item q, bool isCorrect) {
            isHandlingAnswer = true;
            SetOptionsInteractable(false);

            if (isCorrect) {
                score++;
                UpdateScoreUI();

                if (selectedIndex >= 0 && optionImages != null && selectedIndex < optionImages.Length && optionImages[selectedIndex] != null) {
                    optionImages[selectedIndex].color = correctOptionColor;
                }

                if (promptTextTMP != null) {
                    promptTextTMP.text = $"<color=#2E7D32><b>MENDED:</b>\n\"{q.options[q.correctOptionIndex]}\"</color>";
                }

                if (feedbackTMP != null) {
                    feedbackTMP.text = $"CORRECT! Swapped: {q.swappedPartExplanation}";
                }

                if (promptCardObject != null) {
                    promptCardObject.transform.DOPunchScale(Vector3.one * 0.12f, 0.3f, 5, 0.5f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                yield return new WaitForSeconds(0.8f);

                if (score >= targetPointsToWin) {
                    EndGame(true);
                } else {
                    currentQuestionIndex++;
                    DisplayCurrentQuestion();
                }
            } else {
                if (selectedIndex >= 0 && optionImages != null && selectedIndex < optionImages.Length && optionImages[selectedIndex] != null) {
                    optionImages[selectedIndex].color = wrongOptionColor;
                }

                // Show correct option
                if (q.correctOptionIndex >= 0 && optionImages != null && q.correctOptionIndex < optionImages.Length && optionImages[q.correctOptionIndex] != null) {
                    optionImages[q.correctOptionIndex].DOColor(correctOptionColor, 0.2f);
                }

                if (feedbackTMP != null) {
                    feedbackTMP.text = $"NOT QUITE! Swapped: {q.swappedPartExplanation}";
                }

                if (promptCardObject != null) {
                    promptCardObject.transform.DOShakePosition(0.35f, new Vector3(8f, 0f, 0f), 10, 90f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                yield return new WaitForSeconds(1.0f);

                currentQuestionIndex++;
                DisplayCurrentQuestion();
            }
        }

        private void EndGame(bool passed) {
            isGameActive = false;
            isHandlingAnswer = false;

            if (resultPanel != null) resultPanel.SetActive(true);

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {targetPointsToWin}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? $"Awesome! You reached {score} points! Quiz Time Completed!"
                    : $"Round Over! Score: {score}/{targetPointsToWin}. Try again!";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(passed ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                NextButtonAnimation();
            }

            if (passed) {
                topic = Masters_Topic.Game;
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(topic);
                }
            }
        }

        public void OnReturnToHub() {
            topic = Masters_Topic.Game;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        public void InitQuestionsIfEmpty() {
            if (questions != null && questions.Length >= 20) return;

            questions = new CommonMistakes_GameG02Item[] {
                new CommonMistakes_GameG02Item { itemId = 1, wobblyPrompt = "Have you been in London?", options = new string[] { "Have you been to London?", "Have you been on London?", "Have you been at London?" }, correctOptionIndex = 0, swappedPartExplanation = "Preposition: 'in' -> 'to'" },
                new CommonMistakes_GameG02Item { itemId = 2, wobblyPrompt = "The office is in the first floor.", options = new string[] { "The office is at the first floor.", "The office is on the first floor.", "The office is to the first floor." }, correctOptionIndex = 1, swappedPartExplanation = "Preposition: 'in' -> 'on'" },
                new CommonMistakes_GameG02Item { itemId = 3, wobblyPrompt = "It depends from you.", options = new string[] { "It depends of you.", "It depends to you.", "It depends on you." }, correctOptionIndex = 2, swappedPartExplanation = "Preposition: 'from' -> 'on'" },
                new CommonMistakes_GameG02Item { itemId = 4, wobblyPrompt = "Who is in the phone?", options = new string[] { "Who is on the phone?", "Who is at the phone?", "Who is to the phone?" }, correctOptionIndex = 0, swappedPartExplanation = "Preposition: 'in' -> 'on'" },
                new CommonMistakes_GameG02Item { itemId = 5, wobblyPrompt = "My birthday is on January.", options = new string[] { "My birthday is at January.", "My birthday is in January.", "My birthday is for January." }, correctOptionIndex = 1, swappedPartExplanation = "Preposition: 'on' -> 'in'" },
                new CommonMistakes_GameG02Item { itemId = 6, wobblyPrompt = "We went at the mall.", options = new string[] { "We went to the mall.", "We went in the mall.", "We went on the mall." }, correctOptionIndex = 0, swappedPartExplanation = "Preposition: 'at' -> 'to'" },
                new CommonMistakes_GameG02Item { itemId = 7, wobblyPrompt = "Joe did not drank water.", options = new string[] { "Joe did not drinks water.", "Joe did not drunk water.", "Joe did not drink water." }, correctOptionIndex = 2, swappedPartExplanation = "Verb Form: 'drank' -> 'drink'" },
                new CommonMistakes_GameG02Item { itemId = 8, wobblyPrompt = "She has left five years ago.", options = new string[] { "She left five years ago.", "She had left five years ago.", "She is leaving five years ago." }, correctOptionIndex = 0, swappedPartExplanation = "Verb Form: 'has left' -> 'left'" },
                new CommonMistakes_GameG02Item { itemId = 9, wobblyPrompt = "Did you wanted to help him?", options = new string[] { "Did you wanting to help him?", "Did you want to help him?", "Did you wants to help him?" }, correctOptionIndex = 1, swappedPartExplanation = "Verb Form: 'wanted' -> 'want'" },
                new CommonMistakes_GameG02Item { itemId = 10, wobblyPrompt = "I must to learn English.", options = new string[] { "I must learning English.", "I must learn English.", "I must to learning English." }, correctOptionIndex = 1, swappedPartExplanation = "Verb Form: 'must to learn' -> 'must learn'" },
                new CommonMistakes_GameG02Item { itemId = 11, wobblyPrompt = "Jim helped me carrying the box.", options = new string[] { "Jim helped me carry the box.", "Jim helped me for carrying the box.", "Jim helped me carried the box." }, correctOptionIndex = 0, swappedPartExplanation = "Verb Form: 'carrying' -> 'carry'" },
                new CommonMistakes_GameG02Item { itemId = 12, wobblyPrompt = "Everybody have problems with the date.", options = new string[] { "Everybody having problems with the date.", "Everybody are having problems with the date.", "Everybody has problems with the date." }, correctOptionIndex = 2, swappedPartExplanation = "Verb Form: 'have' -> 'has'" },
                new CommonMistakes_GameG02Item { itemId = 13, wobblyPrompt = "He speaks English very good.", options = new string[] { "He speaks English very well.", "He speaks English very nice.", "He speaks English very great." }, correctOptionIndex = 0, swappedPartExplanation = "Word Choice: 'good' -> 'well'" },
                new CommonMistakes_GameG02Item { itemId = 14, wobblyPrompt = "Please, don't tell nobody.", options = new string[] { "Please, don't tell someone.", "Please, don't tell anybody.", "Please, don't tell no one." }, correctOptionIndex = 1, swappedPartExplanation = "Word Choice: 'nobody' -> 'anybody'" },
                new CommonMistakes_GameG02Item { itemId = 15, wobblyPrompt = "We have not money left.", options = new string[] { "We have none money left.", "We have no money left.", "We have not any money left." }, correctOptionIndex = 1, swappedPartExplanation = "Word Choice: 'not money' -> 'no money'" },
                new CommonMistakes_GameG02Item { itemId = 16, wobblyPrompt = "I don't use a watch.", options = new string[] { "I don't hold a watch.", "I don't put a watch.", "I don't wear a watch." }, correctOptionIndex = 2, swappedPartExplanation = "Word Choice: 'use' -> 'wear'" },
                new CommonMistakes_GameG02Item { itemId = 17, wobblyPrompt = "Leave me in peace!", options = new string[] { "Leave me alone!", "Leave me lonely!", "Leave me quietly!" }, correctOptionIndex = 0, swappedPartExplanation = "Word Choice: 'in peace' -> 'alone'" },
                new CommonMistakes_GameG02Item { itemId = 18, wobblyPrompt = "Who cooked this lemon pie?", options = new string[] { "Who did this lemon pie?", "Who made this lemon pie?", "Who created this lemon pie?" }, correctOptionIndex = 1, swappedPartExplanation = "Word Choice: 'cooked' -> 'made'" },
                new CommonMistakes_GameG02Item { itemId = 19, wobblyPrompt = "Where has gone Sunil?", options = new string[] { "Where Sunil has gone?", "Where has Sunil gone?", "Where gone has Sunil?" }, correctOptionIndex = 1, swappedPartExplanation = "Word Order: 'has gone Sunil' -> 'has Sunil gone'" },
                new CommonMistakes_GameG02Item { itemId = 20, wobblyPrompt = "I like more July than May.", options = new string[] { "I like July more than May.", "I like than May more July.", "I more like July than May." }, correctOptionIndex = 0, swappedPartExplanation = "Word Order: 'like more July' -> 'like July more'" }
            };
        }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
}
