using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {

    

    /// <summary>
    /// G01 Polish Sort — Two Trays, Fast
    /// Timed sorting reaction arcade game for Unit 11: The Art of Politeness.
    /// Sentence stones slide down from the top.
    /// Two Trays at the bottom:
    /// Tray 0: BLUNT (rough grey)
    /// Tray 1: POLISHED (velvet purple)
    /// 3 lives, 60-second countdown.
    /// Speed increases gradually.
    /// Success condition: Sort at least 16 sentences correctly within time/lives.
    /// </summary>
    public class Masters_ArtOfPoliteness_Game_LessonOne : Masters_Lesson {

[System.Serializable]
    public class ArtOfPoliteness_GameG01Item {
        public int itemId;
        public string sentenceText;
        public int correctTrayIndex; // 0: BLUNT, 1: POLISHED
        public string moveUsed;
    }
    
        [Header("24 Sentence Items")]
        [SerializeField] private ArtOfPoliteness_GameG01Item[] items;

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

        [Header("Sentence Card Stage")]
        [SerializeField] private GameObject sentenceCardObject;
        [SerializeField] private TextMeshProUGUI sentenceTextTMP;
        [SerializeField] private TextMeshProUGUI hintTMP;

        [Header("2 Tray Buttons")]
        [SerializeField] private Button[] trayButtons; // 0: BLUNT, 1: POLISHED
        [SerializeField] private Image[] trayImages;
        [SerializeField] private TextMeshProUGUI[] trayTexts;

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
        [SerializeField] private float initialQuestionDuration = 20.0f;

        private float remainingGameTime;
        private int currentLives;
        private int totalSorted = 0;
        private bool isGameActive = false;
        private bool isHandlingAnswer = false;
        private int currentItemIndex = 0;
        private float currentQuestionDuration = 20.0f;
        private float questionTimeRemaining = 20.0f;
        private List<int> shuffledIndices = new List<int>();

        private readonly string[] trayNames = new string[] {
            "BLUNT",
            "POLISHED"
        };

        private readonly Color bluntTrayColor = new Color(0.27f, 0.35f, 0.39f, 1f);   // #455A64 Slate
        private readonly Color polishedTrayColor = new Color(0.42f, 0.11f, 0.60f, 1f); // #6A1B9A Purple
        private readonly Color correctTrayColor = new Color(0.18f, 0.80f, 0.44f, 1f);  // #2ECC71 Green
        private readonly Color wrongTrayColor = new Color(0.91f, 0.30f, 0.24f, 1f);    // #E74C3C Red

        protected override void Awake() {
            topic = Masters_Topic.Game;
            base.Awake();

            AutoBindReferences();

            if (items == null || items.Length == 0) {
                PopulateDefaultItems();
            }

            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Game;
            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (introAudio == null && narratorSpeech != null) {
                introAudio = narratorSpeech;
            }
            if (narratorSpeech == null && introAudio != null) {
                narratorSpeech = introAudio;
            }

            if (nextButton != null) {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(false);
            }

            StartCoroutine(InitializeGameRoutine());
        }

        private IEnumerator InitializeGameRoutine() {
            ResetGameUI();

            // Display introductory prompt on the sentence card while listening
            if (sentenceTextTMP != null) {
                sentenceTextTMP.text = "<color=#FFE082>Listen to the instructions...</color>";
            }
            if (hintTMP != null) {
                hintTMP.text = "Get ready to sort!";
            }

            // Play voiceover introduction and wait until complete
            AudioClip clipToPlay = introAudio != null ? introAudio : narratorSpeech;
            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                yield return new WaitForSeconds(clipToPlay.length + 0.3f);
            } else {
                yield return new WaitForSeconds(1.0f);
            }

            StartGame();
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
            if (timerBarFillImage != null) timerBarFillImage.fillAmount = 1f;

            UpdateScoreAndLivesUI();
            UpdateTimerUI();
            ConfigureTrayVisuals();
        }

        public void StartGame() {
            ResetGameUI();
            ShuffleItems();
            isGameActive = true;
            currentItemIndex = 0;
            LoadCurrentQuestion();
        }

        public void RestartGame() {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (nextButton != null) {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(false);
            }

            StartGame();
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
                }
            }
            if (progressTMP != null) {
                // Hide redundant progressTMP to prevent overlap with scoreTMP
                progressTMP.gameObject.SetActive(false);
            }

            if (sentenceCardObject == null) {
                Transform cTr = transform.Find("IdiomCard") ?? transform.Find("ConveyorCard") ?? transform.Find("SentenceCard") ?? transform.Find("TileCardObject");
                if (cTr != null) sentenceCardObject = cTr.gameObject;
            }

            if (sentenceCardObject != null && sentenceTextTMP == null) {
                Transform txtTr = sentenceCardObject.transform.Find("IdiomTextTMP") ?? sentenceCardObject.transform.Find("ExpressionTMP") ?? sentenceCardObject.transform.Find("SentenceTMP") ?? sentenceCardObject.transform.Find("Text");
                if (txtTr != null) sentenceTextTMP = txtTr.GetComponent<TextMeshProUGUI>();
                if (sentenceTextTMP == null) sentenceTextTMP = sentenceCardObject.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (sentenceCardObject != null && hintTMP == null) {
                Transform ht = sentenceCardObject.transform.Find("HintTMP") ?? sentenceCardObject.transform.Find("CategoryHintTMP");
                if (ht != null) hintTMP = ht.GetComponent<TextMeshProUGUI>();
            }

            if (timerBarFillImage == null) {
                Transform fTr = transform.Find("TimerBar/Fill") ?? transform.Find("ProgressBar/Fill") ?? transform.Find("TimeBar/Fill");
                if (fTr != null) timerBarFillImage = fTr.GetComponent<Image>();
            }

            // Auto-bind 2 Tray Buttons
            if (trayButtons == null || trayButtons.Length < 2) {
                List<Button> bList = new List<Button>();
                List<Image> iList = new List<Image>();
                List<TextMeshProUGUI> tList = new List<TextMeshProUGUI>();

                Transform optCont = transform.Find("OptionsContainer") ?? transform.Find("DrawersHolder") ?? transform;
                Button[] allB = optCont.GetComponentsInChildren<Button>(true);
                foreach (var b in allB) {
                    if (b != null && b != nextButton && b != retryBtn && b != returnHubBtn) {
                        string bn = b.name.ToLower();
                        if (bn.Contains("tray") || bn.Contains("drawer") || bn.Contains("optioncard") || bn.Contains("button_")) {
                            bList.Add(b);
                            iList.Add(b.GetComponent<Image>());
                            tList.Add(b.GetComponentInChildren<TextMeshProUGUI>(true));
                        }
                    }
                }
                if (bList.Count >= 2) {
                    trayButtons = new Button[] { bList[0], bList[1] };
                    trayImages = new Image[] { iList[0], iList[1] };
                    trayTexts = new TextMeshProUGUI[] { tList[0], tList[1] };
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

        private void EnsureHeaderAndTitle() {
            if (headerTMP == null) {
                Transform hTr = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("Branch");
                if (hTr != null) headerTMP = hTr.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "THE ART OF POLITENESS";
            }

            if (titleTMP == null) {
                Transform tTr = transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("LessonTitle") ?? transform.Find("HeaderContainer/Title") ?? transform.Find("Title");
                if (tTr != null) titleTMP = tTr.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "G01 Polish Sort — Two Trays, Fast";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (subtitleTMP == null) {
                Transform sTr = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Subtitle") ?? transform.Find("Instruction");
                if (sTr != null) subtitleTMP = sTr.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleTMP != null) {
                subtitleTMP.text = "Sort each sentence onto the BLUNT or POLISHED tray before time runs out!";
            }

            ConfigureTrayVisuals();
        }

        public void ConfigureTrayVisuals() {
            if (trayButtons != null) {
                for (int i = 0; i < trayButtons.Length && i < 2; i++) {
                    if (trayButtons[i] == null) continue;
                    TextMeshProUGUI t = (trayTexts != null && i < trayTexts.Length && trayTexts[i] != null) 
                        ? trayTexts[i] 
                        : trayButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (t != null) {
                        t.text = $"<b>{trayNames[i]}</b>";
                        t.fontSize = 28f;
                        t.color = Color.white;
                    }

                    Image img = (trayImages != null && i < trayImages.Length && trayImages[i] != null) 
                        ? trayImages[i] 
                        : trayButtons[i].GetComponent<Image>();
                    if (img != null) {
                        img.color = (i == 0) ? bluntTrayColor : polishedTrayColor;
                    }
                }
            }
        }

        public void PopulateDefaultItems() {
            items = new ArtOfPoliteness_GameG01Item[] {
                // 0: BLUNT (12 items)
                new ArtOfPoliteness_GameG01Item { itemId = 1, sentenceText = "I want a pizza.", correctTrayIndex = 0, moveUsed = "Blunt demand" },
                new ArtOfPoliteness_GameG01Item { itemId = 2, sentenceText = "Send me the report.", correctTrayIndex = 0, moveUsed = "Direct order" },
                new ArtOfPoliteness_GameG01Item { itemId = 3, sentenceText = "You're wrong.", correctTrayIndex = 0, moveUsed = "Direct contradiction" },
                new ArtOfPoliteness_GameG01Item { itemId = 4, sentenceText = "That's a bad idea.", correctTrayIndex = 0, moveUsed = "Harsh criticism" },
                new ArtOfPoliteness_GameG01Item { itemId = 5, sentenceText = "Tell me when you're available.", correctTrayIndex = 0, moveUsed = "Direct instruction" },
                new ArtOfPoliteness_GameG01Item { itemId = 6, sentenceText = "Go away.", correctTrayIndex = 0, moveUsed = "Blunt dismissal" },
                new ArtOfPoliteness_GameG01Item { itemId = 7, sentenceText = "Give me your notes.", correctTrayIndex = 0, moveUsed = "Direct command" },
                new ArtOfPoliteness_GameG01Item { itemId = 8, sentenceText = "Your drawing is untidy.", correctTrayIndex = 0, moveUsed = "Direct criticism" },
                new ArtOfPoliteness_GameG01Item { itemId = 9, sentenceText = "No, I can't come.", correctTrayIndex = 0, moveUsed = "Unsoftened refusal" },
                new ArtOfPoliteness_GameG01Item { itemId = 10, sentenceText = "I don't like this color.", correctTrayIndex = 0, moveUsed = "Blunt preference" },
                new ArtOfPoliteness_GameG01Item { itemId = 11, sentenceText = "Your work isn't good.", correctTrayIndex = 0, moveUsed = "Harsh review" },
                new ArtOfPoliteness_GameG01Item { itemId = 12, sentenceText = "Leave me alone.", correctTrayIndex = 0, moveUsed = "Blunt order" },

                // 1: POLISHED (12 items)
                new ArtOfPoliteness_GameG01Item { itemId = 13, sentenceText = "I'll have a pizza, please.", correctTrayIndex = 1, moveUsed = "Added 'please'" },
                new ArtOfPoliteness_GameG01Item { itemId = 14, sentenceText = "Could you send me the report?", correctTrayIndex = 1, moveUsed = "Asked instead of ordering" },
                new ArtOfPoliteness_GameG01Item { itemId = 15, sentenceText = "I think you might be mistaken.", correctTrayIndex = 1, moveUsed = "Hedged disagreement" },
                new ArtOfPoliteness_GameG01Item { itemId = 16, sentenceText = "I'm not so sure that's a good idea.", correctTrayIndex = 1, moveUsed = "Softened opinion" },
                new ArtOfPoliteness_GameG01Item { itemId = 17, sentenceText = "Let me know when you're available.", correctTrayIndex = 1, moveUsed = "Invited politely" },
                new ArtOfPoliteness_GameG01Item { itemId = 18, sentenceText = "Sorry – I'm a bit busy right now.", correctTrayIndex = 1, moveUsed = "Softened refusal with reason" },
                new ArtOfPoliteness_GameG01Item { itemId = 19, sentenceText = "Could you lend me your notes, please?", correctTrayIndex = 1, moveUsed = "Polite modal question" },
                new ArtOfPoliteness_GameG01Item { itemId = 20, sentenceText = "I'm not quite satisfied with the lines here.", correctTrayIndex = 1, moveUsed = "Said it about yourself" },
                new ArtOfPoliteness_GameG01Item { itemId = 21, sentenceText = "I'd prefer a bit more space between them.", correctTrayIndex = 1, moveUsed = "Polite self-statement" },
                new ArtOfPoliteness_GameG01Item { itemId = 22, sentenceText = "I'm afraid I can't make it today.", correctTrayIndex = 1, moveUsed = "Softened with 'afraid'" },
                new ArtOfPoliteness_GameG01Item { itemId = 23, sentenceText = "I'd prefer to use different colours in this design.", correctTrayIndex = 1, moveUsed = "Gentle suggestion" },
                new ArtOfPoliteness_GameG01Item { itemId = 24, sentenceText = "I'm not quite satisfied with this work.", correctTrayIndex = 1, moveUsed = "Subtle feedback" }
            };
        }

        private void WireEventListeners() {
            if (trayButtons != null) {
                for (int i = 0; i < trayButtons.Length; i++) {
                    int index = i;
                    if (trayButtons[i] != null) {
                        trayButtons[i].onClick.RemoveAllListeners();
                        trayButtons[i].onClick.AddListener(() => OnTraySelected(index));
                    }
                }
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartGame);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnNextButtonClicked);
            }
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
                int temp = shuffledIndices[i];
                shuffledIndices[i] = shuffledIndices[r];
                shuffledIndices[r] = temp;
            }
        }

        private void LoadCurrentQuestion() {
            if (!isGameActive) return;

            if (currentItemIndex >= shuffledIndices.Count) {
                // Reshuffle and continue until time runs out
                ShuffleItems();
                currentItemIndex = 0;
            }

            int itemIdx = shuffledIndices[currentItemIndex];
            var item = items[itemIdx];

            if (sentenceTextTMP != null) {
                sentenceTextTMP.text = $"\"{item.sentenceText}\"";
            }

            if (sentenceCardObject != null) {
                sentenceCardObject.transform.DOKill();
                sentenceCardObject.transform.localScale = Vector3.one * 0.85f;
                sentenceCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }

            if (hintTMP != null) {
                hintTMP.text = "";
            }

            // 20-second question timer
            currentQuestionDuration = initialQuestionDuration; // 20 seconds
            questionTimeRemaining = currentQuestionDuration;
            if (timerBarFillImage != null) {
                timerBarFillImage.fillAmount = 1f;
            }
            isHandlingAnswer = false;

            UpdateScoreAndLivesUI();
            UpdateTimerUI();
        }

        private void Update() {
            if (!isGameActive || isHandlingAnswer) return;

            // Overall round countdown
            remainingGameTime -= Time.deltaTime;
            if (remainingGameTime <= 0f) {
                remainingGameTime = 0f;
                UpdateTimerUI();
                EndGame(totalSorted >= targetMatchesToWin);
                return;
            }

            // Question countdown: 20 seconds
            questionTimeRemaining -= Time.deltaTime;
            if (timerBarFillImage != null && currentQuestionDuration > 0f) {
                timerBarFillImage.fillAmount = Mathf.Clamp01(questionTimeRemaining / currentQuestionDuration);
            }

            if (questionTimeRemaining <= 0f) {
                // Time out on question -> lose life
                StartCoroutine(HandleTimeOutRoutine());
            }

            UpdateTimerUI();
        }

        public void OnTraySelected(int selectedTrayIndex) {
            if (!isGameActive || isHandlingAnswer) return;
            if (currentItemIndex >= shuffledIndices.Count) return;

            isHandlingAnswer = true;
            int itemIdx = shuffledIndices[currentItemIndex];
            var item = items[itemIdx];

            bool isCorrect = (selectedTrayIndex == item.correctTrayIndex);
            StartCoroutine(HandleAnswerRoutine(isCorrect, selectedTrayIndex, item));
        }

        private IEnumerator HandleAnswerRoutine(bool isCorrect, int trayIndex, ArtOfPoliteness_GameG01Item item) {
            // Visual feedback on selected tray
            if (trayButtons != null && trayIndex < trayButtons.Length && trayButtons[trayIndex] != null) {
                Image img = (trayImages != null && trayIndex < trayImages.Length && trayImages[trayIndex] != null)
                    ? trayImages[trayIndex]
                    : trayButtons[trayIndex].GetComponent<Image>();

                if (img != null) {
                    img.DOKill();
                    Color origCol = (trayIndex == 0) ? bluntTrayColor : polishedTrayColor;
                    img.color = isCorrect ? correctTrayColor : wrongTrayColor;
                    img.DOColor(origCol, 0.4f);
                }
            }

            if (isCorrect) {
                totalSorted++;
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (hintTMP != null) {
                    hintTMP.text = $"<color=#55FF88><b>Correct!</b> {item.moveUsed}</color>";
                }

                if (sentenceCardObject != null) {
                    sentenceCardObject.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
                }

                UpdateScoreAndLivesUI();
                yield return new WaitForSeconds(0.3f);
            } else {
                currentLives--;
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                string correctName = trayNames[item.correctTrayIndex];
                if (hintTMP != null) {
                    hintTMP.text = $"<color=#FF6666>Wrong tray! That was <b>{correctName}</b></color>";
                }

                if (sentenceCardObject != null) {
                    sentenceCardObject.transform.DOShakePosition(0.4f, new Vector3(18f, 0, 0));
                }

                UpdateScoreAndLivesUI();
                yield return new WaitForSeconds(0.6f);

                if (currentLives <= 0) {
                    currentLives = 0;
                    UpdateScoreAndLivesUI();
                    EndGame(false);
                    yield break;
                }
            }

            currentItemIndex++;
            LoadCurrentQuestion();
        }

        private IEnumerator HandleTimeOutRoutine() {
            isHandlingAnswer = true;
            currentLives--;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (hintTMP != null) {
                hintTMP.text = "<color=#FFD27F>Time's up for this sentence!</color>";
            }

            if (sentenceCardObject != null) {
                sentenceCardObject.transform.DOShakePosition(0.4f, new Vector3(18f, 0, 0));
            }

            UpdateScoreAndLivesUI();
            yield return new WaitForSeconds(0.6f);

            if (currentLives <= 0) {
                currentLives = 0;
                UpdateScoreAndLivesUI();
                EndGame(false);
                yield break;
            }

            currentItemIndex++;
            LoadCurrentQuestion();
        }

        private void EndGame(bool won) {
            isGameActive = false;
            UpdateScoreAndLivesUI();
            UpdateTimerUI();

            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            bool passed = totalSorted >= targetMatchesToWin;

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "Well Done!" : "Keep Practicing!";
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Sorted: {totalSorted} / {targetMatchesToWin}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? "<color=#55FF88>Great sorting! You mastered the difference between blunt and polished sentences.</color>"
                    : "<color=#FFD27F>Try to sort at least 16 sentences before time runs out!</color>";
            }

            if (passed) {
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                    NextButtonAnimation();
                }
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
            } else {
                if (retryBtn != null) retryBtn.gameObject.SetActive(true);
            }
        }

        private void UpdateTimerUI() {
            if (timerTMP != null) {
                int seconds = Mathf.CeilToInt(remainingGameTime);
                timerTMP.text = $"{seconds}s";
            }
        }

        private void UpdateScoreAndLivesUI() {
            if (livesTMP != null) {
                string hearts = "";
                for (int i = 0; i < currentLives; i++) {
                    hearts += "<color=#E53935>\u2665 </color>";
                }
                for (int i = currentLives; i < maxLives; i++) {
                    hearts += "<color=#757575>\u2661 </color>";
                }
                livesTMP.text = hearts.TrimEnd();
            }

            if (scoreTMP != null) {
                scoreTMP.text = $"Sorted: {totalSorted}/{targetMatchesToWin}";
            }

            if (progressTMP != null) {
                progressTMP.gameObject.SetActive(false);
            }
        }

        protected override void OnNextButtonClicked() {
            if (topic == Masters_Topic.None) return;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
            }

            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}

