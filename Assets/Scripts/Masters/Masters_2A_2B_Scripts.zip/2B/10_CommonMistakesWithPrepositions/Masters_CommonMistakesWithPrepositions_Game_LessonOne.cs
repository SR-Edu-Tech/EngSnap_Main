using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {

    

    /// <summary>
    /// G01 Drawer Dash — Four Drawers, Fast
    /// Timed sorting reaction arcade game for Unit 10: Common Mistakes with Prepositions.
    /// Wobbly sentences slide past on the conveyor stage card.
    /// 4 Drawers along the side / bottom:
    /// Drawer 0: PREPOSITION
    /// Drawer 1: VERB FORM
    /// Drawer 2: WORD CHOICE
    /// Drawer 3: WORD ORDER
    /// 3 lives, 60-second total countdown.
    /// Speed increases gradually.
    /// Success condition: Sort at least 16 sentences correctly within time/lives.
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Game_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class CommonMistakes_GameG01Item {
        public int itemId;
        public string wobblySentence;
        public string mendedSentence;
        public int correctDrawerIndex; // 0: PREPOSITION, 1: VERB FORM, 2: WORD CHOICE, 3: WORD ORDER
    }
    
        [Header("G01 Wobbly Sentence Items")]
        [SerializeField] private CommonMistakes_GameG01Item[] items;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI timerTMP;
        [SerializeField] private TextMeshProUGUI livesTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;

        [Header("Question Timer Bar")]
        [SerializeField] private Image timerBarFillImage;

        [Header("Conveyor Stage Card")]
        [SerializeField] private GameObject conveyorCardObject;
        [SerializeField] private TextMeshProUGUI sentenceTextTMP;
        [SerializeField] private TextMeshProUGUI hintTMP;

        [Header("4 Drawer Buttons")]
        [SerializeField] private Button[] drawerButtons; // 0: PREPOSITION, 1: VERB FORM, 2: WORD CHOICE, 3: WORD ORDER
        [SerializeField] private Image[] drawerImages;
        [SerializeField] private TextMeshProUGUI[] drawerTexts;

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
        [SerializeField] private float totalGameTime = 60f;
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int targetMatchesToWin = 16;
        [SerializeField] private float initialQuestionDuration = 5.0f;

        private float remainingGameTime;
        private int currentLives;
        private int totalSorted = 0;
        private bool isGameActive = false;
        private bool isHandlingAnswer = false;
        private int currentItemIndex = 0;
        private float currentQuestionDuration = 5.0f;
        private float questionTimeRemaining = 5.0f;
        private List<int> shuffledIndices = new List<int>();

        private readonly string[] drawerNames = new string[] {
            "PREPOSITION",
            "VERB FORM",
            "WORD CHOICE",
            "WORD ORDER"
        };

        private readonly Color defaultDrawerColor = new Color(0.12f, 0.35f, 0.65f, 0.95f);
        private readonly Color correctDrawerColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private readonly Color wrongDrawerColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Game;
            base.Awake();

            AutoBindReferences();
            InitItemsIfEmpty();
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
                introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Game/cm_g01_intro.mp3");
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
                Transform hTr = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("Branch");
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
                titleTMP.text = "G01 Drawer Dash";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (subtitleTMP == null) {
                Transform sTr = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Subtitle") ?? transform.Find("Instruction");
                if (sTr != null) subtitleTMP = sTr.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleTMP != null) {
                subtitleTMP.text = "Sort each wobbly sentence into its correct drawer!";
            }
        }

        private void AutoBindReferences() {
            EnsureHeaderAndTitle();

            if (timerTMP == null) {
                Transform tTr = transform.Find("HeaderContainer/TimerTMP") ?? transform.Find("TimerTMP") ?? transform.Find("Timer");
                if (tTr != null) timerTMP = tTr.GetComponent<TextMeshProUGUI>();
            }

            if (livesTMP == null) {
                Transform lTr = transform.Find("HeaderContainer/LivesTMP") ?? transform.Find("LivesTMP") ?? transform.Find("Lives");
                if (lTr != null) livesTMP = lTr.GetComponent<TextMeshProUGUI>();
            }

            if (scoreTMP == null) {
                Transform scTr = transform.Find("HeaderContainer/ScoreTMP") ?? transform.Find("ScoreTMP") ?? transform.Find("CorrectlyFilledTMP");
                if (scTr != null) scoreTMP = scTr.GetComponent<TextMeshProUGUI>();
            }

            if (progressTMP == null) {
                Transform prTr = transform.Find("HeaderContainer/ProgressTMP") ?? transform.Find("ProgressTMP");
                if (prTr != null) {
                    progressTMP = prTr.GetComponent<TextMeshProUGUI>();
                    // Hide redundant ProgressTMP to prevent overlap with ScoreTMP
                    progressTMP.gameObject.SetActive(false);
                }
            }

            if (timerBarFillImage == null) {
                Transform fTr = transform.Find("TimerBar/Fill") ?? transform.Find("QuestionTimerBar/Fill") ?? transform.Find("TimerBar");
                if (fTr != null) {
                    Image img = fTr.GetComponent<Image>();
                    if (img != null) timerBarFillImage = img;
                    else {
                        Image[] allImgs = fTr.GetComponentsInChildren<Image>(true);
                        foreach (var i in allImgs) {
                            if (i.gameObject.name.ToLower().Contains("fill")) { timerBarFillImage = i; break; }
                        }
                    }
                }
            }

            if (conveyorCardObject == null) {
                Transform cc = transform.Find("IdiomCard") ?? transform.Find("ConveyorCard") ?? transform.Find("TileCardObject") ?? transform.Find("StageCard") ?? transform.Find("CardObject");
                if (cc != null) conveyorCardObject = cc.gameObject;
            }

            if (conveyorCardObject != null && sentenceTextTMP == null) {
                Transform st = conveyorCardObject.transform.Find("IdiomTextTMP")
                            ?? conveyorCardObject.transform.Find("ExpressionTMP") 
                            ?? conveyorCardObject.transform.Find("SentenceTMP") 
                            ?? conveyorCardObject.transform.Find("Text")
                            ?? conveyorCardObject.transform.Find("StatementTMP");
                if (st != null) sentenceTextTMP = st.GetComponent<TextMeshProUGUI>();
                if (sentenceTextTMP == null) sentenceTextTMP = conveyorCardObject.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (conveyorCardObject != null && hintTMP == null) {
                Transform ht = conveyorCardObject.transform.Find("HintTMP") ?? conveyorCardObject.transform.Find("CategoryHintTMP");
                if (ht != null) hintTMP = ht.GetComponent<TextMeshProUGUI>();
            }

            // Auto-bind 4 drawer buttons
            if (drawerButtons == null || drawerButtons.Length < 4) {
                List<Button> bList = new List<Button>();
                List<Image> iList = new List<Image>();
                List<TextMeshProUGUI> tList = new List<TextMeshProUGUI>();

                Button[] allBtns = GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns) {
                    string bn = b.name.ToLower();
                    if ((bn.Contains("optioncard") || bn.Contains("drawer") || bn.Contains("gate")) && !bn.Contains("return") && !bn.Contains("retry")) {
                        bList.Add(b);
                        iList.Add(b.GetComponent<Image>());
                        tList.Add(b.GetComponentInChildren<TextMeshProUGUI>(true));
                    }
                }

                if (bList.Count >= 4) {
                    drawerButtons = bList.GetRange(0, 4).ToArray();
                    drawerImages = iList.GetRange(0, 4).ToArray();
                    drawerTexts = tList.GetRange(0, 4).ToArray();
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
            if (drawerButtons != null) {
                for (int i = 0; i < drawerButtons.Length; i++) {
                    int drawerIndex = i;
                    drawerButtons[i].onClick.RemoveAllListeners();
                    drawerButtons[i].onClick.AddListener(() => OnDrawerClicked(drawerIndex));
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
            remainingGameTime = totalGameTime;
            currentLives = maxLives;
            totalSorted = 0;
            isGameActive = false;
            isHandlingAnswer = false;
            currentQuestionDuration = initialQuestionDuration;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            UpdateScoreAndLivesUI();
            UpdateTimerUI();

            if (drawerTexts != null && drawerTexts.Length >= 4) {
                for (int i = 0; i < 4; i++) {
                    if (drawerTexts[i] != null) drawerTexts[i].text = drawerNames[i];
                }
            }
        }

        public void StartGame() {
            ResetGameUI();
            ShuffleItems();
            isGameActive = true;
            currentItemIndex = 0;
            DisplayCurrentQuestion();
        }

        public void RestartGame() {
            StartGame();
        }

        private void ShuffleItems() {
            shuffledIndices.Clear();
            if (items == null || items.Length == 0) return;

            for (int i = 0; i < items.Length; i++) {
                shuffledIndices.Add(i);
            }

            // Fisher-Yates shuffle
            for (int i = shuffledIndices.Count - 1; i > 0; i--) {
                int r = Random.Range(0, i + 1);
                int tmp = shuffledIndices[i];
                shuffledIndices[i] = shuffledIndices[r];
                shuffledIndices[r] = tmp;
            }
        }

        private void Update() {
            if (!isGameActive) return;

            // Overall round timer
            remainingGameTime -= Time.deltaTime;
            if (remainingGameTime <= 0f) {
                remainingGameTime = 0f;
                UpdateTimerUI();
                EndGame(totalSorted >= targetMatchesToWin);
                return;
            }
            UpdateTimerUI();

            // Question per-item timer
            if (!isHandlingAnswer) {
                questionTimeRemaining -= Time.deltaTime;
                if (timerBarFillImage != null) {
                    timerBarFillImage.fillAmount = Mathf.Clamp01(questionTimeRemaining / currentQuestionDuration);
                }

                if (questionTimeRemaining <= 0f) {
                    OnQuestionTimeout();
                }
            }
        }

        private void UpdateTimerUI() {
            if (timerTMP != null) {
                int sec = Mathf.CeilToInt(remainingGameTime);
                timerTMP.text = $"Time: {sec}s";
            }
        }

        private void UpdateScoreAndLivesUI() {
            if (scoreTMP != null) {
                scoreTMP.text = $"Sorted: {totalSorted} / {targetMatchesToWin}";
            }

            if (livesTMP != null) {
                if (livesTMP.font == null || livesTMP.font.name.Contains("Howdybun")) {
                    TMP_FontAsset libFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    if (libFont != null) livesTMP.font = libFont;
                }
                string hearts = "";
                for (int i = 0; i < currentLives; i++) hearts += "<color=#E53935>\u2665 </color>";
                for (int i = currentLives; i < maxLives; i++) hearts += "<color=#424242>\u2665 </color>";
                livesTMP.text = hearts.Trim();
            }

            if (progressTMP != null) {
                progressTMP.text = $"Target: {targetMatchesToWin}";
            }
        }

        private void DisplayCurrentQuestion() {
            if (items == null || items.Length == 0 || shuffledIndices.Count == 0) return;

            int itemIdx = shuffledIndices[currentItemIndex % shuffledIndices.Count];
            CommonMistakes_GameG01Item item = items[itemIdx];

            isHandlingAnswer = false;
            questionTimeRemaining = currentQuestionDuration;

            if (sentenceTextTMP != null) {
                sentenceTextTMP.text = $"\"{item.wobblySentence}\"";
            }

            if (hintTMP != null) {
                hintTMP.text = "Send to the correct drawer!";
            }

            if (conveyorCardObject != null) {
                conveyorCardObject.transform.localScale = Vector3.one * 0.9f;
                conveyorCardObject.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }

            ResetDrawerColors();
            SetDrawersInteractable(true);
        }

        private void ResetDrawerColors() {
            if (drawerImages != null) {
                foreach (var img in drawerImages) {
                    if (img != null) img.color = defaultDrawerColor;
                }
            }
        }

        private void SetDrawersInteractable(bool state) {
            if (drawerButtons != null) {
                foreach (var b in drawerButtons) {
                    if (b != null) b.interactable = state;
                }
            }
        }

        public void OnDrawerClicked(int selectedDrawerIndex) {
            if (!isGameActive || isHandlingAnswer) return;
            if (items == null || items.Length == 0 || shuffledIndices.Count == 0) return;

            int itemIdx = shuffledIndices[currentItemIndex % shuffledIndices.Count];
            CommonMistakes_GameG01Item item = items[itemIdx];

            bool isCorrect = (selectedDrawerIndex == item.correctDrawerIndex);
            StartCoroutine(HandleAnswerRoutine(selectedDrawerIndex, item, isCorrect));
        }

        private void OnQuestionTimeout() {
            if (!isGameActive || isHandlingAnswer) return;
            if (items == null || items.Length == 0 || shuffledIndices.Count == 0) return;

            int itemIdx = shuffledIndices[currentItemIndex % shuffledIndices.Count];
            CommonMistakes_GameG01Item item = items[itemIdx];

            StartCoroutine(HandleAnswerRoutine(-1, item, false));
        }

        private IEnumerator HandleAnswerRoutine(int selectedIndex, CommonMistakes_GameG01Item item, bool isCorrect) {
            isHandlingAnswer = true;
            SetDrawersInteractable(false);

            if (isCorrect) {
                totalSorted++;
                UpdateScoreAndLivesUI();

                // Highlight correct drawer
                if (selectedIndex >= 0 && drawerImages != null && selectedIndex < drawerImages.Length && drawerImages[selectedIndex] != null) {
                    drawerImages[selectedIndex].color = correctDrawerColor;
                }

                if (sentenceTextTMP != null) {
                    sentenceTextTMP.text = $"<color=#2E7D32><b>MENDED:</b>\n\"{item.mendedSentence}\"</color>";
                }

                if (conveyorCardObject != null) {
                    conveyorCardObject.transform.DOPunchScale(Vector3.one * 0.12f, 0.3f, 5, 0.5f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                // Gradually increase conveyor speed
                currentQuestionDuration = Mathf.Max(2.5f, currentQuestionDuration - 0.1f);

                yield return new WaitForSeconds(0.6f);

                currentItemIndex++;
                DisplayCurrentQuestion();
            } else {
                currentLives--;
                UpdateScoreAndLivesUI();

                if (selectedIndex >= 0 && drawerImages != null && selectedIndex < drawerImages.Length && drawerImages[selectedIndex] != null) {
                    drawerImages[selectedIndex].color = wrongDrawerColor;
                }

                // Show which drawer was correct
                if (item.correctDrawerIndex >= 0 && drawerImages != null && item.correctDrawerIndex < drawerImages.Length && drawerImages[item.correctDrawerIndex] != null) {
                    drawerImages[item.correctDrawerIndex].DOColor(correctDrawerColor, 0.2f);
                }

                if (sentenceTextTMP != null) {
                    sentenceTextTMP.text = $"<color=#C62828><b>WRONG! Needs:</b>\n{drawerNames[item.correctDrawerIndex]}</color>";
                }

                if (conveyorCardObject != null) {
                    conveyorCardObject.transform.DOShakePosition(0.35f, new Vector3(8f, 0f, 0f), 10, 90f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                yield return new WaitForSeconds(0.7f);

                if (currentLives <= 0) {
                    EndGame(false);
                } else {
                    currentItemIndex++;
                    DisplayCurrentQuestion();
                }
            }
        }

        private void EndGame(bool passed) {
            isGameActive = false;
            isHandlingAnswer = false;

            if (resultPanel != null) resultPanel.SetActive(true);

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Sorted: {totalSorted} / {targetMatchesToWin}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? $"Awesome! You sorted {totalSorted} sentences! Lesson Completed!"
                    : $"Round over! You sorted {totalSorted} / {targetMatchesToWin}. Try again!";
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

        public void InitItemsIfEmpty() {
            if (items != null && items.Length >= 24) return;

            items = new CommonMistakes_GameG01Item[] {
                // 0: PREPOSITION (Items 1-6)
                new CommonMistakes_GameG01Item { itemId = 1, wobblySentence = "Have you been in London?", mendedSentence = "Have you been to London?", correctDrawerIndex = 0 },
                new CommonMistakes_GameG01Item { itemId = 2, wobblySentence = "The office is in the first floor.", mendedSentence = "The office is on the first floor.", correctDrawerIndex = 0 },
                new CommonMistakes_GameG01Item { itemId = 3, wobblySentence = "It depends from you.", mendedSentence = "It depends on you.", correctDrawerIndex = 0 },
                new CommonMistakes_GameG01Item { itemId = 4, wobblySentence = "Who is in the phone?", mendedSentence = "Who is on the phone?", correctDrawerIndex = 0 },
                new CommonMistakes_GameG01Item { itemId = 5, wobblySentence = "My birthday is on January.", mendedSentence = "My birthday is in January.", correctDrawerIndex = 0 },
                new CommonMistakes_GameG01Item { itemId = 6, wobblySentence = "We went at the mall.", mendedSentence = "We went to the mall.", correctDrawerIndex = 0 },

                // 1: VERB FORM (Items 7-12)
                new CommonMistakes_GameG01Item { itemId = 7, wobblySentence = "Joe did not drank water.", mendedSentence = "Joe did not drink water.", correctDrawerIndex = 1 },
                new CommonMistakes_GameG01Item { itemId = 8, wobblySentence = "She has left five years ago.", mendedSentence = "She left five years ago.", correctDrawerIndex = 1 },
                new CommonMistakes_GameG01Item { itemId = 9, wobblySentence = "Did you wanted to help him?", mendedSentence = "Did you want to help him?", correctDrawerIndex = 1 },
                new CommonMistakes_GameG01Item { itemId = 10, wobblySentence = "I must to learn English.", mendedSentence = "I must learn English.", correctDrawerIndex = 1 },
                new CommonMistakes_GameG01Item { itemId = 11, wobblySentence = "Jim helped me carrying the box.", mendedSentence = "Jim helped me carry the box.", correctDrawerIndex = 1 },
                new CommonMistakes_GameG01Item { itemId = 12, wobblySentence = "Everybody have problems with the date.", mendedSentence = "Everybody has problems with the date.", correctDrawerIndex = 1 },

                // 2: WORD CHOICE (Items 13-18)
                new CommonMistakes_GameG01Item { itemId = 13, wobblySentence = "He speaks English very good.", mendedSentence = "He speaks English very well.", correctDrawerIndex = 2 },
                new CommonMistakes_GameG01Item { itemId = 14, wobblySentence = "Please don't tell nobody.", mendedSentence = "Please don't tell anybody.", correctDrawerIndex = 2 },
                new CommonMistakes_GameG01Item { itemId = 15, wobblySentence = "We have not money left.", mendedSentence = "We have no money left.", correctDrawerIndex = 2 },
                new CommonMistakes_GameG01Item { itemId = 16, wobblySentence = "I don't use a watch.", mendedSentence = "I don't wear a watch.", correctDrawerIndex = 2 },
                new CommonMistakes_GameG01Item { itemId = 17, wobblySentence = "Leave me in peace!", mendedSentence = "Leave me alone!", correctDrawerIndex = 2 },
                new CommonMistakes_GameG01Item { itemId = 18, wobblySentence = "Who cooked this lemon pie?", mendedSentence = "Who made this lemon pie?", correctDrawerIndex = 2 },

                // 3: WORD ORDER (Items 19-24)
                new CommonMistakes_GameG01Item { itemId = 19, wobblySentence = "Where has gone Sunil?", mendedSentence = "Where has Sunil gone?", correctDrawerIndex = 3 },
                new CommonMistakes_GameG01Item { itemId = 20, wobblySentence = "I like more July than May.", mendedSentence = "I like July more than May.", correctDrawerIndex = 3 },
                new CommonMistakes_GameG01Item { itemId = 21, wobblySentence = "I am going to home now.", mendedSentence = "I am going home now.", correctDrawerIndex = 3 },
                new CommonMistakes_GameG01Item { itemId = 22, wobblySentence = "I did it by my own.", mendedSentence = "I did it on my own.", correctDrawerIndex = 3 },
                new CommonMistakes_GameG01Item { itemId = 23, wobblySentence = "I have 26 years.", mendedSentence = "I'm 26 years old.", correctDrawerIndex = 3 },
                new CommonMistakes_GameG01Item { itemId = 24, wobblySentence = "Yes, I like very much.", mendedSentence = "Yes, I like it very much.", correctDrawerIndex = 3 }
            };
        }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
}
