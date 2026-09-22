using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit5 {

    /// <summary>
    /// G01 Symptom Sort — Four Doors, Fast
    /// Timed sorting reaction game for Book 2B Unit 5 (Doctor Need Your Help).
    /// Symptom tiles appear at the records corridor; 4 doors at the bottom:
    /// Door 0: HEAD & FACE
    /// Door 1: CHEST & BODY
    /// Door 2: ARMS & HANDS
    /// Door 3: LEGS & FEET
    /// 3 lives, 60s total timer, speeds up over time.
    /// Success condition: Student sorts at least 16 symptoms correctly within time/lives.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Game_LessonOne : Masters_Lesson {

[System.Serializable]
    public class DoctorGameG01Symptom {
        public int symptomId;
        public string symptomText;
        public int correctDoorIndex; // 0: HEAD & FACE, 1: CHEST & BODY, 2: ARMS & HANDS, 3: LEGS & FEET
    }
    
        [Header("G01 15 Verbatim Symptoms")]
        [SerializeField] private DoctorGameG01Symptom[] symptoms;

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

        [Header("Symptom Corridor Tile Card")]
        [SerializeField] private GameObject symptomCardObject;
        [SerializeField] private TextMeshProUGUI symptomTextTMP;

        [Header("4 Body Area Doors")]
        [SerializeField] private Button[] doorButtons; // 4 Doors
        [SerializeField] private Image[] doorImages;
        [SerializeField] private TextMeshProUGUI[] doorTexts;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        [Header("Game Settings")]
        [SerializeField] private float totalGameTime = 60f;
        [SerializeField] private int maxLives = 3;
        [SerializeField] private int targetMatchesToWin = 16;
        [SerializeField] private float initialQuestionDuration = 8.0f;

        private float remainingGameTime;
        private int currentLives;
        private int totalSorted = 0;
        private bool isGameActive = false;
        private bool isHandlingAnswer = false;
        private int currentSymptomIndex = 0;
        private float currentQuestionDuration = 8.0f;
        private float questionTimeRemaining = 8.0f;

        private Color defaultDoorColor = new Color(0.12f, 0.35f, 0.65f, 0.95f);
        private Color correctDoorColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private Color wrongDoorColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Game;
            base.Awake();

            AutoBindReferences();
            InitSymptomsIfEmpty();
            WireEventListeners();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Game;
            AutoBindReferences();
            WireEventListeners();
            EnsureNextAndBackButtonWired();

            float introDuration = (narratorSpeech != null && narratorSpeech.length > 0) ? narratorSpeech.length + 0.3f : 1.0f;
            StartCoroutine(StartGameAfterIntro(introDuration));
        }

        private IEnumerator StartGameAfterIntro(float delay) {
            isGameActive = false;
            remainingGameTime = totalGameTime;
            currentLives = maxLives;
            totalSorted = 0;
            isHandlingAnswer = false;

            if (headerTMP != null) headerTMP.gameObject.SetActive(true);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (symptomCardObject != null) symptomCardObject.SetActive(true);

            Transform doorsGrid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
            if (doorsGrid != null) doorsGrid.gameObject.SetActive(true);

            UpdateScoreAndHUD();
            SetDoorLabels();

            // Keep doors disabled and timebar full during intro
            if (doorButtons != null) {
                for (int i = 0; i < doorButtons.Length; i++) {
                    if (doorButtons[i] != null) doorButtons[i].interactable = false;
                }
            }
            if (timerBarFillImage != null) {
                timerBarFillImage.fillAmount = 1f;
            }
            if (timerTMP != null) {
                timerTMP.text = $"Time: {Mathf.CeilToInt(totalGameTime)}s";
            }
            if (symptoms != null && symptoms.Length > 0 && symptomTextTMP != null) {
                symptomTextTMP.text = $"\"{symptoms[0].symptomText}\"";
            }

            yield return new WaitForSeconds(delay);

            RestartGame();
        }

        private void Update() {
            if (!isGameActive || isHandlingAnswer) return;

            // Total game countdown
            remainingGameTime -= Time.deltaTime;
            if (timerTMP != null) {
                timerTMP.text = $"Time: {Mathf.Max(0f, Mathf.Ceil(remainingGameTime))}s";
            }

            // Per-question time bar countdown
            questionTimeRemaining -= Time.deltaTime;
            if (timerBarFillImage != null && currentQuestionDuration > 0f) {
                timerBarFillImage.fillAmount = Mathf.Clamp01(questionTimeRemaining / currentQuestionDuration);
            }

            // Check triggers
            if (questionTimeRemaining <= 0f) {
                StartCoroutine(HandleAnswer(false, -1));
            } else if (remainingGameTime <= 0f || currentLives <= 0) {
                EndGame();
            }
        }

        private void WireEventListeners() {
            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartGame);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnNextButtonClicked);
            }

            if (doorButtons != null) {
                for (int i = 0; i < doorButtons.Length; i++) {
                    int idx = i;
                    if (doorButtons[i] != null) {
                        doorButtons[i].onClick.RemoveAllListeners();
                        doorButtons[i].onClick.AddListener(() => OnDoorClicked(idx));
                    }
                }
            }
        }

        public void InitSymptomsIfEmpty() {
            symptoms = new DoctorGameG01Symptom[] {
                new DoctorGameG01Symptom { symptomId = 1, symptomText = "My head hurts! What's wrong with me?", correctDoorIndex = 0 },
                new DoctorGameG01Symptom { symptomId = 2, symptomText = "My nose is runny.", correctDoorIndex = 0 },
                new DoctorGameG01Symptom { symptomId = 3, symptomText = "My eyes are watery.", correctDoorIndex = 0 },
                new DoctorGameG01Symptom { symptomId = 4, symptomText = "My ears are itching!", correctDoorIndex = 0 },
                new DoctorGameG01Symptom { symptomId = 5, symptomText = "I have a toothache! I think I have a cavity.", correctDoorIndex = 0 },
                new DoctorGameG01Symptom { symptomId = 6, symptomText = "I cut my tongue.", correctDoorIndex = 0 },
                new DoctorGameG01Symptom { symptomId = 7, symptomText = "My throat is dry! I can't stop coughing.", correctDoorIndex = 0 },
                new DoctorGameG01Symptom { symptomId = 8, symptomText = "My chest feels tight! I can't breathe.", correctDoorIndex = 1 },
                new DoctorGameG01Symptom { symptomId = 9, symptomText = "My stomach hurts.", correctDoorIndex = 1 },
                new DoctorGameG01Symptom { symptomId = 10, symptomText = "I had trouble in breathing.", correctDoorIndex = 1 },
                new DoctorGameG01Symptom { symptomId = 11, symptomText = "My arm is hurt.", correctDoorIndex = 2 },
                new DoctorGameG01Symptom { symptomId = 12, symptomText = "I cut my finger! The bleeding doesn't stop.", correctDoorIndex = 2 },
                new DoctorGameG01Symptom { symptomId = 13, symptomText = "I twisted my ankle.", correctDoorIndex = 3 },
                new DoctorGameG01Symptom { symptomId = 14, symptomText = "My legs feel weak.", correctDoorIndex = 3 },
                new DoctorGameG01Symptom { symptomId = 15, symptomText = "My knees keep locking.", correctDoorIndex = 3 }
            };
        }

        public void RestartGame() {
            remainingGameTime = totalGameTime;
            currentLives = maxLives;
            totalSorted = 0;
            isGameActive = true;
            isHandlingAnswer = false;

            if (headerTMP != null) headerTMP.gameObject.SetActive(true);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (symptomCardObject != null) symptomCardObject.SetActive(true);

            Transform doorsGrid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
            if (doorsGrid != null) doorsGrid.gameObject.SetActive(true);

            UpdateScoreAndHUD();
            SetDoorLabels();

            SpawnNextSymptom();
        }

        private void SetDoorLabels() {
            string[] doorNames = new string[] { "HEAD & FACE", "CHEST & BODY", "ARMS & HANDS", "LEGS & FEET" };
            if (doorTexts != null) {
                for (int i = 0; i < doorTexts.Length && i < doorNames.Length; i++) {
                    if (doorTexts[i] != null) doorTexts[i].text = $"<b>{doorNames[i]}</b>";
                }
            }
        }

        private void SpawnNextSymptom() {
            if (!isGameActive || currentLives <= 0 || remainingGameTime <= 0) return;

            if (symptoms == null || symptoms.Length == 0) return;
            currentSymptomIndex = Random.Range(0, symptoms.Length);
            DoctorGameG01Symptom sym = symptoms[currentSymptomIndex];

            if (symptomTextTMP != null) {
                symptomTextTMP.text = $"\"{sym.symptomText}\"";
            }

            if (symptomCardObject != null) {
                symptomCardObject.transform.DOKill();
                symptomCardObject.transform.localScale = Vector3.one * 0.85f;
                symptomCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }

            // Reset Doors
            if (doorButtons != null) {
                for (int i = 0; i < doorButtons.Length; i++) {
                    if (doorButtons[i] == null) continue;
                    doorButtons[i].interactable = true;
                    if (doorImages != null && i < doorImages.Length && doorImages[i] != null) {
                        doorImages[i].color = defaultDoorColor;
                    }
                }
            }

            // Calculate question duration based on progress
            float speedFactor = Mathf.Clamp01((totalGameTime - remainingGameTime) / totalGameTime);
            currentQuestionDuration = Mathf.Lerp(initialQuestionDuration, 5.0f, speedFactor);
            questionTimeRemaining = currentQuestionDuration;

            if (timerBarFillImage != null) {
                timerBarFillImage.fillAmount = 1f;
            }
        }

        public void OnDoorClicked(int doorIndex) {
            if (isHandlingAnswer || !isGameActive) return;

            if (symptoms == null || currentSymptomIndex >= symptoms.Length) return;
            DoctorGameG01Symptom sym = symptoms[currentSymptomIndex];

            bool isCorrect = (doorIndex == sym.correctDoorIndex);
            StartCoroutine(HandleAnswer(isCorrect, doorIndex));
        }

        private IEnumerator HandleAnswer(bool isCorrect, int clickedDoorIndex) {
            isHandlingAnswer = true;

            if (doorButtons != null) {
                for (int i = 0; i < doorButtons.Length; i++) {
                    if (doorButtons[i] != null) doorButtons[i].interactable = false;
                }
            }

            DoctorGameG01Symptom sym = symptoms[currentSymptomIndex];

            if (isCorrect) {
                totalSorted++;
                UpdateScoreAndHUD();

                if (clickedDoorIndex >= 0 && doorImages != null && clickedDoorIndex < doorImages.Length && doorImages[clickedDoorIndex] != null) {
                    doorImages[clickedDoorIndex].color = correctDoorColor;
                }

                if (clickedDoorIndex >= 0 && doorButtons[clickedDoorIndex] != null) {
                    doorButtons[clickedDoorIndex].transform.DOScale(1.1f, 0.15f).SetLoops(2, LoopType.Yoyo);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                yield return new WaitForSeconds(0.45f);
            } else {
                currentLives--;
                UpdateScoreAndHUD();

                if (clickedDoorIndex >= 0 && doorImages != null && clickedDoorIndex < doorImages.Length && doorImages[clickedDoorIndex] != null) {
                    doorImages[clickedDoorIndex].color = wrongDoorColor;
                }

                // Highlight correct door
                int correctIdx = sym.correctDoorIndex;
                if (doorImages != null && correctIdx < doorImages.Length && doorImages[correctIdx] != null) {
                    doorImages[correctIdx].color = correctDoorColor;
                }

                if (symptomCardObject != null) {
                    symptomCardObject.transform.DOShakePosition(0.35f, 12f, 14, 90, false, true);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                yield return new WaitForSeconds(0.65f);
            }

            isHandlingAnswer = false;

            if (currentLives <= 0 || remainingGameTime <= 0) {
                EndGame();
            } else {
                SpawnNextSymptom();
            }
        }

        private void UpdateScoreAndHUD() {
            if (scoreTMP != null) scoreTMP.text = $"Sorted: {totalSorted}/{targetMatchesToWin}";
            if (progressTMP != null) progressTMP.text = $"{totalSorted}/{targetMatchesToWin}";
            if (livesTMP != null) {
                livesTMP.text = $"Lives: {currentLives}/{maxLives}";
            }
        }

        private void EndGame() {
            isGameActive = false;

            if (symptomCardObject != null) symptomCardObject.SetActive(false);
            
            Transform doorsGrid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
            if (doorsGrid != null) doorsGrid.gameObject.SetActive(false);

            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }

            bool passed = (totalSorted >= targetMatchesToWin);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "SORT MASTER! 🚪" : "TIME'S UP!";
                    resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
                }

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You sorted {totalSorted} symptoms into the correct body doors!";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed ? "Success! You sorted at least 16 symptoms into their body doors fast." : "You need at least 16 sorted symptoms within 60s to pass.";
                }

                if (returnHubBtn != null) {
                    returnHubBtn.gameObject.SetActive(passed);
                }

                if (retryBtn != null) {
                    retryBtn.gameObject.SetActive(true);
                }
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(passed);
            }

            if (passed && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Game;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        private void AutoBindReferences() {
            if (titleTMP == null) {
                Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/LessonTitle");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (progressTMP == null) {
                Transform t = transform.Find("ProgressTMP") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP");
                if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (scoreTMP == null) {
                Transform t = transform.Find("ScoreTMP") ?? transform.Find("Score") ?? transform.Find("HeaderContainer/ScoreTMP");
                if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (timerTMP == null) {
                Transform t = transform.Find("TimerTMP") ?? transform.Find("Timer") ?? transform.Find("HeaderContainer/TimerTMP");
                if (t != null) timerTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (livesTMP == null) {
                Transform t = transform.Find("LivesTMP") ?? transform.Find("Lives") ?? transform.Find("HeaderContainer/LivesTMP");
                if (t != null) livesTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (symptomCardObject == null) {
                Transform t = transform.Find("SymptomCard") ?? transform.Find("CorridorCard") ?? transform.Find("NoticeboardCard") ?? transform.Find("PromptCard");
                if (t != null) symptomCardObject = t.gameObject;
            }
            if (symptomTextTMP == null && symptomCardObject != null) {
                Transform t = symptomCardObject.transform.Find("SituationTextTMP") ?? symptomCardObject.transform.Find("Text") ?? symptomCardObject.transform.Find("PromptTMP");
                if (t != null) symptomTextTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (timerBarFillImage == null) {
                Transform t = transform.Find("TimerBar/Fill") ?? transform.Find("TimerBarFill") ?? transform.Find("HeaderContainer/TimerBar/Fill") ?? transform.Find("TimerBar");
                if (t != null) {
                    timerBarFillImage = t.GetComponent<Image>() ?? t.GetComponentInChildren<Image>();
                }
            }
            if (timerBarFillImage != null) {
                timerBarFillImage.type = Image.Type.Filled;
                timerBarFillImage.fillMethod = Image.FillMethod.Horizontal;
                timerBarFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                if (timerBarFillImage.sprite == null) {
                    timerBarFillImage.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
                }
            }

            // Find 4 door buttons
            if (doorButtons == null || doorButtons.Length == 0) {
                Transform grid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
                if (grid != null) {
                    List<Button> btns = new List<Button>();
                    List<Image> imgs = new List<Image>();
                    List<TextMeshProUGUI> txts = new List<TextMeshProUGUI>();

                    for (int i = 0; i < grid.childCount; i++) {
                        Button b = grid.GetChild(i).GetComponent<Button>();
                        if (b != null) {
                            btns.Add(b);
                            imgs.Add(grid.GetChild(i).GetComponent<Image>());
                            txts.Add(grid.GetChild(i).GetComponentInChildren<TextMeshProUGUI>());
                        }
                    }
                    doorButtons = btns.ToArray();
                    doorImages = imgs.ToArray();
                    doorTexts = txts.ToArray();
                }
            }

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
}
