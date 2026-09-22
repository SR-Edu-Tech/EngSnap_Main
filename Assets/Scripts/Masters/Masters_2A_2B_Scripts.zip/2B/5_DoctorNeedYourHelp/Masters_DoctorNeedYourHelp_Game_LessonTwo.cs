using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit5 {


    /// <summary>
    /// G02 Clinic Pairs — Memory Match
    /// Arcade memory wall of 18 face-down cards (9 symptom sentences + 9 body parts).
    /// Flips two cards at a time to pair each symptom with its body part.
    /// When matched, cards lock face-up and audio reads the sentence.
    /// Success condition: Student finds all 9 symptom-body part pairs.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class DoctorGameG02MemoryPair {
        public int pairId;
        public string symptomText;
        public string bodyPartText;
        public AudioClip pairAudio;
    }
    
        [Header("G02 9 Symptom-BodyPart Pairs")]
        [SerializeField] private DoctorGameG02MemoryPair[] memoryPairs;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI flipsTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI timerTMP;

        [Header("18 Card Grid Container")]
        [SerializeField] private Transform tilesContainer;
        [SerializeField] private Button[] cardButtons; // 18 Card Buttons

        [Header("Feedback Banner")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private TextMeshProUGUI feedbackTextTMP;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        private int totalFlips = 0;
        private int matchedPairsCount = 0;
        private int score = 0;
        private float gameTime = 0f;
        private bool isGameActive = false;
        private bool isProcessingTurn = false;

        private int firstFlippedIndex = -1;
        private int secondFlippedIndex = -1;

        private int[] cardPairIds;
        private string[] cardDisplayTexts;
        private bool[] cardIsFaceUp;
        private bool[] cardIsMatched;

        private Image[] cardImages;
        private TextMeshProUGUI[] cardTexts;

        private Color cardBackBgColor = new Color(0.12f, 0.22f, 0.42f, 0.95f);
        private Color cardFrontBgColor = new Color(0.16f, 0.36f, 0.62f, 1f);
        private Color matchedBgColor = new Color(0.13f, 0.65f, 0.32f, 1f);
        private Color wrongBgColor = new Color(0.82f, 0.2f, 0.2f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Game;
            base.Awake();

            AutoBindReferences();
            InitMemoryPairsIfEmpty();
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
            SetupBoard();

            if (cardButtons != null) {
                for (int i = 0; i < cardButtons.Length; i++) {
                    if (cardButtons[i] != null) cardButtons[i].interactable = false;
                }
            }

            yield return new WaitForSeconds(delay);

            isGameActive = true;
            if (cardButtons != null) {
                for (int i = 0; i < cardButtons.Length; i++) {
                    if (cardButtons[i] != null && !cardIsMatched[i]) cardButtons[i].interactable = true;
                }
            }
        }

        private void Update() {
            if (!isGameActive || matchedPairsCount >= 9) return;

            gameTime += Time.deltaTime;
            if (timerTMP != null) {
                timerTMP.text = $"Time: {Mathf.FloorToInt(gameTime)}s";
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
        }

        public void InitMemoryPairsIfEmpty() {
            string audioDir = "Assets/Audio/2B/5_DoctorNeedYourHelp/Game/";

            memoryPairs = new DoctorGameG02MemoryPair[] {
                new DoctorGameG02MemoryPair {
                    pairId = 1,
                    symptomText = "My head hurts! What's wrong with me?",
                    bodyPartText = "HEAD",
#if UNITY_EDITOR
                    pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_g02_pair1_head.mp3")
#endif
                },
                new DoctorGameG02MemoryPair {
                    pairId = 2,
                    symptomText = "My throat is dry! I can't stop coughing.",
                    bodyPartText = "THROAT",
#if UNITY_EDITOR
                    pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_g02_pair2_throat.mp3")
#endif
                },
                new DoctorGameG02MemoryPair {
                    pairId = 3,
                    symptomText = "My nose is runny.",
                    bodyPartText = "NOSE",
#if UNITY_EDITOR
                    pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_g02_pair3_nose.mp3")
#endif
                },
                new DoctorGameG02MemoryPair {
                    pairId = 4,
                    symptomText = "My eyes are watery.",
                    bodyPartText = "EYES",
#if UNITY_EDITOR
                    pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_g02_pair4_eyes.mp3")
#endif
                },
                new DoctorGameG02MemoryPair {
                    pairId = 5,
                    symptomText = "My ears are itching!",
                    bodyPartText = "EARS",
#if UNITY_EDITOR
                    pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_g02_pair5_ears.mp3")
#endif
                },
                new DoctorGameG02MemoryPair {
                    pairId = 6,
                    symptomText = "I have a toothache! I think I have a cavity.",
                    bodyPartText = "TEETH",
#if UNITY_EDITOR
                    pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_g02_pair6_teeth.mp3")
#endif
                },
                new DoctorGameG02MemoryPair {
                    pairId = 7,
                    symptomText = "My chest feels tight! I can't breathe.",
                    bodyPartText = "CHEST",
#if UNITY_EDITOR
                    pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_g02_pair7_chest.mp3")
#endif
                },
                new DoctorGameG02MemoryPair {
                    pairId = 8,
                    symptomText = "My stomach hurts.",
                    bodyPartText = "STOMACH",
#if UNITY_EDITOR
                    pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_g02_pair8_stomach.mp3")
#endif
                },
                new DoctorGameG02MemoryPair {
                    pairId = 9,
                    symptomText = "I twisted my ankle.",
                    bodyPartText = "ANKLE",
#if UNITY_EDITOR
                    pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_g02_pair9_ankle.mp3")
#endif
                }
            };
        }

        public void RestartGame() {
            isGameActive = true;
            SetupBoard();
        }

        private void SetupBoard() {
            totalFlips = 0;
            matchedPairsCount = 0;
            score = 0;
            gameTime = 0f;
            isProcessingTurn = false;
            firstFlippedIndex = -1;
            secondFlippedIndex = -1;

            if (headerTMP != null) headerTMP.text = "MEMORY WALL 🧩";
            if (titleTMP != null) {
                titleTMP.text = "Clinic Pairs — Memory Match";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Flip two cards at a time to pair each symptom with its body part.";

            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (tilesContainer != null) tilesContainer.gameObject.SetActive(true);

            UpdateScoreAndHUD();

            int cardCount = 18;
            cardPairIds = new int[cardCount];
            cardDisplayTexts = new string[cardCount];
            cardIsFaceUp = new bool[cardCount];
            cardIsMatched = new bool[cardCount];

            // Create 18 card definitions
            List<KeyValuePair<int, string>> deck = new List<KeyValuePair<int, string>>();
            for (int i = 0; i < memoryPairs.Length; i++) {
                deck.Add(new KeyValuePair<int, string>(memoryPairs[i].pairId, memoryPairs[i].symptomText));
                deck.Add(new KeyValuePair<int, string>(memoryPairs[i].pairId, $"<b>{memoryPairs[i].bodyPartText}</b>"));
            }

            // Shuffle deck
            for (int i = 0; i < deck.Count; i++) {
                int rnd = Random.Range(i, deck.Count);
                var temp = deck[i];
                deck[i] = deck[rnd];
                deck[rnd] = temp;
            }

            for (int i = 0; i < cardCount && i < deck.Count; i++) {
                cardPairIds[i] = deck[i].Key;
                cardDisplayTexts[i] = deck[i].Value;
                cardIsFaceUp[i] = false;
                cardIsMatched[i] = false;
            }

            // Cache UI references
            if (cardButtons != null) {
                cardImages = new Image[cardButtons.Length];
                cardTexts = new TextMeshProUGUI[cardButtons.Length];

                for (int i = 0; i < cardButtons.Length; i++) {
                    int idx = i;
                    if (cardButtons[i] == null) continue;

                    cardImages[i] = cardButtons[i].GetComponent<Image>();
                    cardTexts[i] = cardButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

                    cardButtons[i].gameObject.SetActive(true);
                    cardButtons[i].interactable = true;
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardClicked(idx));

                    SetCardVisual(i, false, false);
                }
            }
        }

        public void OnCardClicked(int index) {
            if (isProcessingTurn || !isGameActive) return;
            if (index < 0 || index >= cardIsFaceUp.Length) return;
            if (cardIsFaceUp[index] || cardIsMatched[index]) return;

            StartCoroutine(FlipCard(index));
        }

        private IEnumerator FlipCard(int index) {
            totalFlips++;
            cardIsFaceUp[index] = true;
            UpdateScoreAndHUD();

            if (cardButtons != null && cardButtons[index] != null) {
                cardButtons[index].transform.DOKill();
                cardButtons[index].transform.DOScaleX(0f, 0.12f).OnComplete(() => {
                    SetCardVisual(index, true, false);
                    cardButtons[index].transform.DOScaleX(1f, 0.12f);
                });
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }

            yield return new WaitForSeconds(0.25f);

            if (firstFlippedIndex == -1) {
                firstFlippedIndex = index;
            } else if (secondFlippedIndex == -1) {
                secondFlippedIndex = index;
                StartCoroutine(EvaluateFlippedCards());
            }
        }

        private IEnumerator EvaluateFlippedCards() {
            isProcessingTurn = true;

            int idx1 = firstFlippedIndex;
            int idx2 = secondFlippedIndex;

            bool isMatch = (cardPairIds[idx1] == cardPairIds[idx2]);

            yield return new WaitForSeconds(0.4f);

            if (isMatch) {
                cardIsMatched[idx1] = true;
                cardIsMatched[idx2] = true;
                matchedPairsCount++;
                score += 10;
                UpdateScoreAndHUD();

                SetCardVisual(idx1, true, true);
                SetCardVisual(idx2, true, true);

                if (cardButtons[idx1] != null) cardButtons[idx1].interactable = false;
                if (cardButtons[idx2] != null) cardButtons[idx2].interactable = false;

                if (feedbackBanner != null) feedbackBanner.SetActive(true);
                if (feedbackTextTMP != null) {
                    feedbackTextTMP.text = "<color=#33D866><b>✓ Matched Pair!</b></color>";
                }

                // Play Audio
                PlayPairAudio(cardPairIds[idx1]);

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                yield return new WaitForSeconds(1.8f);

                if (feedbackBanner != null) feedbackBanner.SetActive(false);

                if (matchedPairsCount >= 9) {
                    EndGame();
                }
            } else {
                if (cardImages[idx1] != null) cardImages[idx1].color = wrongBgColor;
                if (cardImages[idx2] != null) cardImages[idx2].color = wrongBgColor;

                if (cardButtons[idx1] != null) cardButtons[idx1].transform.DOShakePosition(0.35f, 8f, 12, 90, false, true);
                if (cardButtons[idx2] != null) cardButtons[idx2].transform.DOShakePosition(0.35f, 8f, 12, 90, false, true);

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                yield return new WaitForSeconds(0.75f);

                // Flip back
                cardIsFaceUp[idx1] = false;
                cardIsFaceUp[idx2] = false;

                if (cardButtons[idx1] != null) {
                    cardButtons[idx1].transform.DOScaleX(0f, 0.12f).OnComplete(() => {
                        SetCardVisual(idx1, false, false);
                        cardButtons[idx1].transform.DOScaleX(1f, 0.12f);
                    });
                }

                if (cardButtons[idx2] != null) {
                    cardButtons[idx2].transform.DOScaleX(0f, 0.12f).OnComplete(() => {
                        SetCardVisual(idx2, false, false);
                        cardButtons[idx2].transform.DOScaleX(1f, 0.12f);
                    });
                }

                yield return new WaitForSeconds(0.25f);
            }

            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            isProcessingTurn = false;
        }

        private void PlayPairAudio(int pairId) {
            if (memoryPairs == null) return;
            foreach (var p in memoryPairs) {
                if (p.pairId == pairId && p.pairAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(p.pairAudio);
                    break;
                }
            }
        }

        private void SetCardVisual(int index, bool isFaceUp, bool isMatched) {
            if (cardImages != null && index < cardImages.Length && cardImages[index] != null) {
                if (isMatched) cardImages[index].color = matchedBgColor;
                else if (isFaceUp) cardImages[index].color = cardFrontBgColor;
                else cardImages[index].color = cardBackBgColor;
            }

            if (cardTexts != null && index < cardTexts.Length && cardTexts[index] != null) {
                if (isFaceUp) {
                    cardTexts[index].text = cardDisplayTexts[index];
                    cardTexts[index].gameObject.SetActive(true);
                } else {
                    cardTexts[index].text = "❓";
                    cardTexts[index].gameObject.SetActive(true);
                }
            }
        }

        private void UpdateScoreAndHUD() {
            if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
            if (progressTMP != null) progressTMP.text = $"Pairs: {matchedPairsCount}/9";
            if (flipsTMP != null) flipsTMP.text = $"Flips: {totalFlips}";
        }

        private void EndGame() {
            isGameActive = false;

            if (tilesContainer != null) tilesContainer.gameObject.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = "MEMORY MASTER! 🧩";
                    resultTitleTMP.color = new Color(0.2f, 0.85f, 0.35f, 1f);
                }

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"All 9 clinic pairs found in {totalFlips} flips ({Mathf.FloorToInt(gameTime)}s)!";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = "Outstanding! You matched all symptom sentences to their correct body parts.";
                }

                if (returnHubBtn != null) {
                    returnHubBtn.gameObject.SetActive(true);
                }

                if (retryBtn != null) {
                    retryBtn.gameObject.SetActive(true);
                }
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
            }

            if (Masters_AudioManager.Instance != null) {
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
            if (flipsTMP == null) {
                Transform t = transform.Find("FlipsTMP") ?? transform.Find("Flips") ?? transform.Find("HeaderContainer/FlipsTMP");
                if (t != null) flipsTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (timerTMP == null) {
                Transform t = transform.Find("TimerTMP") ?? transform.Find("Timer") ?? transform.Find("HeaderContainer/TimerTMP");
                if (t != null) timerTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (tilesContainer == null) {
                tilesContainer = transform.Find("TilesContainer") ?? transform.Find("CardsGrid") ?? transform.Find("GridContainer") ?? transform.Find("OptionsContainer");
            }

            if (cardButtons == null || cardButtons.Length == 0) {
                if (tilesContainer != null) {
                    List<Button> btns = new List<Button>();
                    for (int i = 0; i < tilesContainer.childCount; i++) {
                        Button b = tilesContainer.GetChild(i).GetComponent<Button>();
                        if (b != null) btns.Add(b);
                    }
                    cardButtons = btns.ToArray();
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
