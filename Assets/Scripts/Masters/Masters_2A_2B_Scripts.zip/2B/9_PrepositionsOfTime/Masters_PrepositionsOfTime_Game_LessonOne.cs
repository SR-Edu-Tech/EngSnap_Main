using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit9 {

    

    /// <summary>
    /// G01 Ring Rush — Three Gates, Fast
    /// Timed sorting reaction game for Book 2B Unit 9 (Prepositions of Time).
    /// Time-expression tiles slide down; 3 gates / rings:
    /// Gate 0: IN (Outer Ring - Months, Years, Seasons, Periods of Day)
    /// Gate 1: ON (Middle Ring - Days, Dates, Specific Days)
    /// Gate 2: AT (Centre - Exact Times, Clock, Night/Noon)
    /// Introduction plays first, then 3 lives, 60s total timer starts.
    /// Plays audio feedback for correct and incorrect answers.
    /// Success condition: Student sorts at least 16 time expressions correctly within time / lives.
    /// </summary>
    public class Masters_PrepositionsOfTime_Game_LessonOne : Masters_Lesson {

[System.Serializable]
    public class PrepositionsOfTime_GameG01Item {
        public int itemId;
        public string expressionText;
        public int correctGateIndex; // 0: IN, 1: ON, 2: AT
    }
    
        [Header("G01 Time Expression Items")]
        [SerializeField] private PrepositionsOfTime_GameG01Item[] items;

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

        [Header("Tile Stage Card")]
        [SerializeField] private GameObject tileCardObject;
        [SerializeField] private TextMeshProUGUI expressionTextTMP;

        [Header("3 Gates / Ring Buttons")]
        [SerializeField] private Button[] gateButtons; // 3 Gates: 0: IN, 1: ON, 2: AT
        [SerializeField] private Image[] gateImages;
        [SerializeField] private TextMeshProUGUI[] gateTexts;

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
        [SerializeField] private AudioClip correctAudioClip;
        [SerializeField] private AudioClip incorrectAudioClip;

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
        private AudioSource localAudioSource;

        private readonly string[] gateNames = new string[] {
            "IN",
            "ON",
            "AT"
        };

        private readonly Color defaultGateColor = new Color(0.12f, 0.35f, 0.65f, 0.95f);
        private readonly Color correctGateColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private readonly Color wrongGateColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Game;
            base.Awake();

            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null) {
                localAudioSource = gameObject.AddComponent<AudioSource>();
                localAudioSource.playOnAwake = false;
            }

            AutoBindReferences();
            InitItemsIfEmpty();
            WireEventListeners();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Game;
            AutoBindReferences();
            WireEventListeners();
            EnsureNextAndBackButtonWired();

            // Start introduction routine first, then launch active gameplay
            StartCoroutine(StartGameWithIntroRoutine());
        }

        private IEnumerator StartGameWithIntroRoutine() {
            // 1. Prepare initial state (game paused)
            isGameActive = false;
            isHandlingAnswer = false;
            remainingGameTime = totalGameTime;
            currentLives = maxLives;
            totalSorted = 0;
            currentItemIndex = 0;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (tileCardObject != null) tileCardObject.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            SetupGatesUI();
            SetGatesInteractable(false);
            UpdateHUD();

            if (timerBarFillImage != null) timerBarFillImage.fillAmount = 1f;

            // 2. Play Introduction
            if (expressionTextTMP != null) {
                expressionTextTMP.text = "<color=#FFCC00>Get Ready!</color>";
            }

            float introDuration = 2.5f;
            if (introAudio != null) {
                introDuration = Mathf.Max(2.5f, introAudio.length);
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
                } else if (localAudioSource != null) {
                    localAudioSource.PlayOneShot(introAudio);
                }
            }

            yield return new WaitForSeconds(introDuration);

            // 3. Launch Gameplay after introduction finishes
            if (expressionTextTMP != null) {
                expressionTextTMP.text = "<color=#33FF66>GO!</color>";
            }
            yield return new WaitForSeconds(0.5f);

            SetGatesInteractable(true);
            ShuffleItems();
            isGameActive = true;
            ShowNextItem();
        }

        private void Update() {
            if (!isGameActive) return;

            // Overall 60s countdown timer
            remainingGameTime -= Time.deltaTime;
            if (timerTMP != null) {
                int sec = Mathf.Max(0, Mathf.CeilToInt(remainingGameTime));
                timerTMP.text = $"Time: {sec}s";
            }

            if (remainingGameTime <= 0f) {
                remainingGameTime = 0f;
                EndGame();
                return;
            }

            // Tile fall timer
            if (!isHandlingAnswer) {
                questionTimeRemaining -= Time.deltaTime;
                if (timerBarFillImage != null && currentQuestionDuration > 0f) {
                    timerBarFillImage.fillAmount = Mathf.Clamp01(questionTimeRemaining / currentQuestionDuration);
                }

                if (questionTimeRemaining <= 0f) {
                    StartCoroutine(HandleTimeoutRoutine());
                }
            }
        }

        private void WireEventListeners() {
            if (gateButtons != null) {
                for (int i = 0; i < gateButtons.Length; i++) {
                    int gateIdx = i;
                    if (gateButtons[i] != null) {
                        gateButtons[i].onClick.RemoveAllListeners();
                        gateButtons[i].onClick.AddListener(() => OnGateClicked(gateIdx));
                    }
                }
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartGameQuick);
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

                if ((n.Contains("lessontitle") || (n == "tmp" && pn.Contains("title"))) && titleTMP == null) titleTMP = t;
                else if ((n.Contains("branch") || n.Contains("header")) && headerTMP == null && !n.Contains("timertmp") && !n.Contains("score") && !n.Contains("lives") && !n.Contains("progress")) headerTMP = t;
                else if ((n.Contains("instruction") || n.Contains("subtitle")) && subtitleTMP == null) subtitleTMP = t;
                else if (n == "timertmp" || n.Contains("timer") && timerTMP == null) timerTMP = t;
                else if (n == "livestmp" || n.Contains("lives") && livesTMP == null) livesTMP = t;
                else if (n == "scoretmp" || n.Contains("score") && scoreTMP == null) scoreTMP = t;
                else if (n == "progresstmp" || n.Contains("progress") && progressTMP == null) progressTMP = t;
                else if ((n == "idiomtexttmp" || n.Contains("idiomtext") || n.Contains("expression") || n.Contains("goodstext")) && expressionTextTMP == null) expressionTextTMP = t;
                else if (n.Contains("resulttitle") && resultTitleTMP == null) resultTitleTMP = t;
                else if (n.Contains("resultscore") && resultScoreTMP == null) resultScoreTMP = t;
                else if (n.Contains("resultstatus") && resultStatusTMP == null) resultStatusTMP = t;
            }

            if (tileCardObject == null) {
                Transform cTrans = transform.Find("IdiomCard") ?? transform.Find("GoodsCard") ?? transform.Find("TileCard");
                if (cTrans != null) tileCardObject = cTrans.gameObject;
            }

            if (timerBarFillImage == null) {
                Transform fTrans = transform.Find("TimerBar/Fill");
                if (fTrans != null) timerBarFillImage = fTrans.GetComponent<Image>();
            }

            // Gates
            Transform opts = transform.Find("OptionsContainer");
            if (opts != null) {
                List<Button> bList = new List<Button>();
                List<Image> iList = new List<Image>();
                List<TextMeshProUGUI> tList = new List<TextMeshProUGUI>();

                for (int i = 0; i < opts.childCount; i++) {
                    Transform c = opts.GetChild(i);
                    Button b = c.GetComponent<Button>();
                    if (b != null) {
                        bList.Add(b);
                        iList.Add(c.GetComponent<Image>());
                        tList.Add(c.GetComponentInChildren<TextMeshProUGUI>(true));
                    }
                }

                if (bList.Count >= 3) {
                    gateButtons = new Button[] { bList[0], bList[1], bList[2] };
                    gateImages = new Image[] { iList[0], iList[1], iList[2] };
                    gateTexts = new TextMeshProUGUI[] { tList[0], tList[1], tList[2] };

                    if (bList.Count > 3) {
                        for (int k = 3; k < bList.Count; k++) {
                            bList[k].gameObject.SetActive(false);
                        }
                    }
                }
            }

            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns) {
                string bn = b.gameObject.name.ToLower();
                if (bn.Contains("retry") && retryBtn == null) retryBtn = b;
                else if ((bn.Contains("return") || bn.Contains("hub")) && returnHubBtn == null) returnHubBtn = b;
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel");
                if (rTrans != null) resultPanel = rTrans.gameObject;
            }
        }

        private void SetupGatesUI() {
            if (gateButtons != null) {
                for (int i = 0; i < gateButtons.Length; i++) {
                    if (gateButtons[i] != null) gateButtons[i].gameObject.SetActive(true);
                    if (gateTexts != null && i < gateTexts.Length && gateTexts[i] != null) {
                        gateTexts[i].text = $"<b>{gateNames[i]}</b>";
                    }
                    if (gateImages != null && i < gateImages.Length && gateImages[i] != null) {
                        gateImages[i].color = defaultGateColor;
                    }
                }
            }
        }

        private void SetGatesInteractable(bool interactable) {
            if (gateButtons != null) {
                foreach (var b in gateButtons) {
                    if (b != null) b.interactable = interactable;
                }
            }
        }

        private void ShuffleItems() {
            shuffledIndices.Clear();
            if (items != null && items.Length > 0) {
                for (int i = 0; i < items.Length; i++) shuffledIndices.Add(i);
                for (int i = 0; i < shuffledIndices.Count; i++) {
                    int temp = shuffledIndices[i];
                    int rand = Random.Range(i, shuffledIndices.Count);
                    shuffledIndices[i] = shuffledIndices[rand];
                    shuffledIndices[rand] = temp;
                }
            }
        }

        public void RestartGameQuick() {
            StartCoroutine(RestartGameQuickRoutine());
        }

        private IEnumerator RestartGameQuickRoutine() {
            isGameActive = false;
            isHandlingAnswer = false;
            remainingGameTime = totalGameTime;
            currentLives = maxLives;
            totalSorted = 0;
            currentItemIndex = 0;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (tileCardObject != null) tileCardObject.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            SetupGatesUI();
            SetGatesInteractable(false);
            UpdateHUD();

            if (expressionTextTMP != null) expressionTextTMP.text = "<color=#FFCC00>Ready... 3, 2, 1</color>";
            yield return new WaitForSeconds(1.0f);

            SetGatesInteractable(true);
            ShuffleItems();
            isGameActive = true;
            ShowNextItem();
        }

        private void ShowNextItem() {
            if (!isGameActive || shuffledIndices.Count == 0) return;

            if (currentItemIndex >= shuffledIndices.Count) {
                currentItemIndex = 0;
            }

            int itemIdx = shuffledIndices[currentItemIndex];
            var item = items[itemIdx];

            if (expressionTextTMP != null) {
                expressionTextTMP.text = item.expressionText;
            }

            if (tileCardObject != null) {
                tileCardObject.transform.DOKill();
                tileCardObject.transform.localScale = Vector3.one * 0.8f;
                tileCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }

            currentQuestionDuration = Mathf.Max(2.5f, initialQuestionDuration - (totalSorted * 0.1f));
            questionTimeRemaining = currentQuestionDuration;
            isHandlingAnswer = false;

            ResetGateColors();
        }

        private void OnGateClicked(int gateIndex) {
            if (!isGameActive || isHandlingAnswer || shuffledIndices.Count == 0) return;

            int itemIdx = shuffledIndices[currentItemIndex];
            var item = items[itemIdx];
            bool isCorrect = (gateIndex == item.correctGateIndex);

            StartCoroutine(HandleAnswerRoutine(gateIndex, isCorrect, item.correctGateIndex));
        }

        private void PlaySFX(bool isCorrect) {
            if (isCorrect) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                } else if (localAudioSource != null && correctAudioClip != null) {
                    localAudioSource.PlayOneShot(correctAudioClip);
                }
            } else {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                } else if (localAudioSource != null && incorrectAudioClip != null) {
                    localAudioSource.PlayOneShot(incorrectAudioClip);
                }
            }
        }

        private IEnumerator HandleAnswerRoutine(int chosenGate, bool isCorrect, int correctGate) {
            isHandlingAnswer = true;
            PlaySFX(isCorrect);

            if (isCorrect) {
                totalSorted++;
                if (gateImages != null && chosenGate < gateImages.Length && gateImages[chosenGate] != null) {
                    gateImages[chosenGate].color = correctGateColor;
                    gateImages[chosenGate].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
                }

                UpdateHUD();
                yield return new WaitForSeconds(0.35f);
            } else {
                currentLives--;
                if (gateImages != null && chosenGate < gateImages.Length && gateImages[chosenGate] != null) {
                    gateImages[chosenGate].color = wrongGateColor;
                }
                if (gateImages != null && correctGate < gateImages.Length && gateImages[correctGate] != null) {
                    gateImages[correctGate].color = correctGateColor;
                }

                if (tileCardObject != null) {
                    tileCardObject.transform.DOShakePosition(0.4f, 15f);
                }

                UpdateHUD();
                yield return new WaitForSeconds(0.6f);

                if (currentLives <= 0) {
                    currentLives = 0;
                    EndGame();
                    yield break;
                }
            }

            currentItemIndex++;
            ShowNextItem();
        }

        private IEnumerator HandleTimeoutRoutine() {
            isHandlingAnswer = true;
            PlaySFX(false);
            currentLives--;

            int itemIdx = shuffledIndices[currentItemIndex];
            var item = items[itemIdx];

            if (gateImages != null && item.correctGateIndex < gateImages.Length && gateImages[item.correctGateIndex] != null) {
                gateImages[item.correctGateIndex].color = correctGateColor;
            }

            if (tileCardObject != null) {
                tileCardObject.transform.DOShakePosition(0.4f, 15f);
            }

            UpdateHUD();
            yield return new WaitForSeconds(0.6f);

            if (currentLives <= 0) {
                currentLives = 0;
                EndGame();
                yield break;
            }

            currentItemIndex++;
            ShowNextItem();
        }

        private void ResetGateColors() {
            if (gateImages != null) {
                for (int i = 0; i < gateImages.Length; i++) {
                    if (gateImages[i] != null) gateImages[i].color = defaultGateColor;
                }
            }
        }

        private void UpdateHUD() {
            if (titleTMP != null) titleTMP.text = "G01 Ring Rush — Three Gates, Fast";
            if (headerTMP != null) headerTMP.text = "PREPOSITIONS OF TIME";
            if (subtitleTMP != null) subtitleTMP.text = "Sort each time expression into IN, ON, or AT before time runs out.";

            if (timerTMP != null) {
                int sec = Mathf.Max(0, Mathf.CeilToInt(remainingGameTime));
                timerTMP.text = $"Time: {sec}s";
            }

            if (livesTMP != null) {
                string hearts = "";
                for (int i = 0; i < maxLives; i++) {
                    hearts += (i < currentLives) ? "♥ " : "♡ ";
                }
                livesTMP.text = hearts.Trim();
            }

            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {totalSorted * 100}";
            }

            if (progressTMP != null) {
                progressTMP.text = $"Matches: {totalSorted}/{targetMatchesToWin}";
            }
        }

        private void EndGame() {
            isGameActive = false;
            isHandlingAnswer = false;

            if (tileCardObject != null) tileCardObject.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(true);

            bool passed = totalSorted >= targetMatchesToWin;

            if (resultTitleTMP != null) resultTitleTMP.text = passed ? "Victory! Ring Rush Cleared!" : "Time's Up!";
            if (resultScoreTMP != null) resultScoreTMP.text = $"Matches: {totalSorted}/{targetMatchesToWin} | Score: {totalSorted * 100} pts";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? $"Outstanding! You sorted {totalSorted} expressions into their correct gates!" : $"You sorted {totalSorted}/{targetMatchesToWin} expressions. Try again to reach {targetMatchesToWin}!";
                resultStatusTMP.color = passed ? correctGateColor : wrongGateColor;
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

            items = new PrepositionsOfTime_GameG01Item[] {
                // IN (Outer Ring - 0)
                new PrepositionsOfTime_GameG01Item { itemId = 1, expressionText = "the morning", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 2, expressionText = "the evening", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 3, expressionText = "the afternoon", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 4, expressionText = "January", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 5, expressionText = "the summer", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 6, expressionText = "the winter", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 7, expressionText = "the spring", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 8, expressionText = "the autumn", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 9, expressionText = "2024", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 10, expressionText = "the 21st century", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 11, expressionText = "five minutes", correctGateIndex = 0 },
                new PrepositionsOfTime_GameG01Item { itemId = 12, expressionText = "the future", correctGateIndex = 0 },

                // ON (Middle Ring - 1)
                new PrepositionsOfTime_GameG01Item { itemId = 13, expressionText = "Monday", correctGateIndex = 1 },
                new PrepositionsOfTime_GameG01Item { itemId = 14, expressionText = "Tuesday", correctGateIndex = 1 },
                new PrepositionsOfTime_GameG01Item { itemId = 15, expressionText = "Friday", correctGateIndex = 1 },
                new PrepositionsOfTime_GameG01Item { itemId = 16, expressionText = "Sunday", correctGateIndex = 1 },
                new PrepositionsOfTime_GameG01Item { itemId = 17, expressionText = "my birthday", correctGateIndex = 1 },
                new PrepositionsOfTime_GameG01Item { itemId = 18, expressionText = "Christmas Day", correctGateIndex = 1 },
                new PrepositionsOfTime_GameG01Item { itemId = 19, expressionText = "New Year's Eve", correctGateIndex = 1 },
                new PrepositionsOfTime_GameG01Item { itemId = 20, expressionText = "15th of August", correctGateIndex = 1 },
                new PrepositionsOfTime_GameG01Item { itemId = 21, expressionText = "Tuesday morning", correctGateIndex = 1 },
                new PrepositionsOfTime_GameG01Item { itemId = 22, expressionText = "weekdays", correctGateIndex = 1 },

                // AT (Centre - 2)
                new PrepositionsOfTime_GameG01Item { itemId = 23, expressionText = "4 o'clock", correctGateIndex = 2 },
                new PrepositionsOfTime_GameG01Item { itemId = 24, expressionText = "9:30 am", correctGateIndex = 2 },
                new PrepositionsOfTime_GameG01Item { itemId = 25, expressionText = "noon", correctGateIndex = 2 },
                new PrepositionsOfTime_GameG01Item { itemId = 26, expressionText = "midnight", correctGateIndex = 2 },
                new PrepositionsOfTime_GameG01Item { itemId = 27, expressionText = "night", correctGateIndex = 2 },
                new PrepositionsOfTime_GameG01Item { itemId = 28, expressionText = "lunchtime", correctGateIndex = 2 },
                new PrepositionsOfTime_GameG01Item { itemId = 29, expressionText = "bedtime", correctGateIndex = 2 },
                new PrepositionsOfTime_GameG01Item { itemId = 30, expressionText = "the moment", correctGateIndex = 2 },
                new PrepositionsOfTime_GameG01Item { itemId = 31, expressionText = "sunrise", correctGateIndex = 2 },
                new PrepositionsOfTime_GameG01Item { itemId = 32, expressionText = "sunset", correctGateIndex = 2 }
            };
        }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
}
