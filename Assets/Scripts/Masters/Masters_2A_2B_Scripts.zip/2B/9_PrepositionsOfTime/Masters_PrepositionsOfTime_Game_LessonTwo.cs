using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit9 {

    

    /// <summary>
    /// G02 Time Pairs — Memory Match
    /// Timed concentration / memory match game for Book 2B Unit 9 (Prepositions of Time).
    /// An arcade memory wall of 18 face-down pocket watches: 9 time idioms and their 9 book meanings.
    /// Flips two cards at a time to pair each idiom with its meaning.
    /// If matched, cards lock face-up and audio reads the pair.
    /// Success condition: Student finds all 9 idiom-meaning pairs.
    /// </summary>
    public class Masters_PrepositionsOfTime_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class PrepositionsOfTime_GameG02MemoryPair {
        public int pairId;
        public string idiomText;
        public string meaningText;
        public AudioClip pairAudio;
    }
    
        [Header("G02 9 Time Idiom - Meaning Pairs")]
        [SerializeField] private PrepositionsOfTime_GameG02MemoryPair[] memoryPairs;

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

        [Header("Audio References")]
        [SerializeField] private AudioClip introAudio;
        [SerializeField] private AudioClip recapAudio;
        [SerializeField] private AudioClip correctAudioClip;
        [SerializeField] private AudioClip incorrectAudioClip;

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
        private AudioSource localAudioSource;

        private readonly Color cardBackBgColor = new Color(0.12f, 0.24f, 0.45f, 0.95f);
        private readonly Color cardFrontBgColor = new Color(0.18f, 0.42f, 0.72f, 1f);
        private readonly Color matchedBgColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private readonly Color wrongBgColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Game;
            base.Awake();

            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null) {
                localAudioSource = gameObject.AddComponent<AudioSource>();
                localAudioSource.playOnAwake = false;
            }

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

            StartCoroutine(StartGameWithIntroRoutine());
        }

        private IEnumerator StartGameWithIntroRoutine() {
            isGameActive = false;
            isProcessingTurn = false;
            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            totalFlips = 0;
            matchedPairsCount = 0;
            score = 0;
            gameTime = 0f;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (tilesContainer != null) tilesContainer.gameObject.SetActive(true);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            SetupCardGrid();
            SetCardsInteractable(false);
            UpdateHUD();

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

            SetCardsInteractable(true);
            isGameActive = true;
        }

        private void Update() {
            if (!isGameActive) return;

            gameTime += Time.deltaTime;
            if (timerTMP != null) {
                int min = Mathf.FloorToInt(gameTime / 60f);
                int sec = Mathf.FloorToInt(gameTime % 60f);
                timerTMP.text = $"Time: {min:00}:{sec:00}";
            }
        }

        private void WireEventListeners() {
            if (cardButtons != null) {
                for (int i = 0; i < cardButtons.Length; i++) {
                    int cardIdx = i;
                    if (cardButtons[i] != null) {
                        cardButtons[i].onClick.RemoveAllListeners();
                        cardButtons[i].onClick.AddListener(() => OnCardClicked(cardIdx));
                    }
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

        public void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps) {
                string n = t.gameObject.name.ToLower();
                Transform p = t.transform.parent;
                string pn = p != null ? p.name.ToLower() : "";

                if ((n.Contains("lessontitle") || (n == "tmp" && pn.Contains("title"))) && titleTMP == null) titleTMP = t;
                else if ((n.Contains("branch") || n.Contains("header")) && headerTMP == null && !n.Contains("progress") && !n.Contains("score") && !n.Contains("flips") && !n.Contains("timer")) headerTMP = t;
                else if ((n.Contains("instruction") || n.Contains("subtitle")) && subtitleTMP == null) subtitleTMP = t;
                else if ((n.Contains("progress") || n.Contains("count")) && progressTMP == null) progressTMP = t;
                else if (n.Contains("flips") && flipsTMP == null) flipsTMP = t;
                else if (n.Contains("score") && scoreTMP == null) scoreTMP = t;
                else if (n.Contains("timer") && timerTMP == null) timerTMP = t;
                else if (n.Contains("feedbacktext") && feedbackTextTMP == null) feedbackTextTMP = t;
                else if (n.Contains("resulttitle") && resultTitleTMP == null) resultTitleTMP = t;
                else if (n.Contains("resultscore") && resultScoreTMP == null) resultScoreTMP = t;
                else if (n.Contains("resultstatus") && resultStatusTMP == null) resultStatusTMP = t;
            }

            if (tilesContainer == null) {
                Transform tc = transform.Find("TilesContainer");
                if (tc != null) tilesContainer = tc;
            }

            if (tilesContainer != null) {
                List<Button> bList = new List<Button>();
                List<Image> iList = new List<Image>();
                List<TextMeshProUGUI> tList = new List<TextMeshProUGUI>();

                for (int i = 0; i < tilesContainer.childCount; i++) {
                    Transform child = tilesContainer.GetChild(i);
                    Button b = child.GetComponent<Button>();
                    if (b != null) {
                        bList.Add(b);
                        iList.Add(child.GetComponent<Image>());
                        tList.Add(child.GetComponentInChildren<TextMeshProUGUI>(true));
                    }
                }

                if (bList.Count >= 18) {
                    cardButtons = bList.ToArray();
                    cardImages = iList.ToArray();
                    cardTexts = tList.ToArray();
                }
            }

            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns) {
                string bn = b.gameObject.name.ToLower();
                if (bn.Contains("retry") && retryBtn == null) retryBtn = b;
                else if ((bn.Contains("return") || bn.Contains("hub")) && returnHubBtn == null) returnHubBtn = b;
            }

            if (feedbackBanner == null) {
                Transform fb = transform.Find("FeedbackBanner");
                if (fb != null) feedbackBanner = fb.gameObject;
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel");
                if (rTrans != null) resultPanel = rTrans.gameObject;
            }
        }

        private void SetupCardGrid() {
            int totalCards = 18;
            cardPairIds = new int[totalCards];
            cardDisplayTexts = new string[totalCards];
            cardIsFaceUp = new bool[totalCards];
            cardIsMatched = new bool[totalCards];

            List<int> slots = new List<int>();
            for (int i = 0; i < totalCards; i++) slots.Add(i);

            // Fisher-Yates Shuffle
            for (int i = 0; i < slots.Count; i++) {
                int temp = slots[i];
                int rand = Random.Range(i, slots.Count);
                slots[i] = slots[rand];
                slots[rand] = temp;
            }

            int pairCount = Mathf.Min(9, memoryPairs.Length);
            for (int p = 0; p < pairCount; p++) {
                int slotA = slots[p * 2];
                int slotB = slots[p * 2 + 1];

                cardPairIds[slotA] = memoryPairs[p].pairId;
                cardDisplayTexts[slotA] = $"<b>{memoryPairs[p].idiomText}</b>";

                cardPairIds[slotB] = memoryPairs[p].pairId;
                cardDisplayTexts[slotB] = memoryPairs[p].meaningText;
            }

            for (int i = 0; i < totalCards; i++) {
                cardIsFaceUp[i] = false;
                cardIsMatched[i] = false;

                if (cardButtons != null && i < cardButtons.Length && cardButtons[i] != null) {
                    cardButtons[i].gameObject.SetActive(true);
                    cardButtons[i].interactable = true;
                }

                if (cardImages != null && i < cardImages.Length && cardImages[i] != null) {
                    cardImages[i].color = cardBackBgColor;
                }

                if (cardTexts != null && i < cardTexts.Length && cardTexts[i] != null) {
                    cardTexts[i].text = "?";
                }
            }
        }

        private void SetCardsInteractable(bool interactable) {
            if (cardButtons != null) {
                for (int i = 0; i < cardButtons.Length; i++) {
                    if (cardButtons[i] != null && (!cardIsMatched[i] || !interactable)) {
                        cardButtons[i].interactable = interactable;
                    }
                }
            }
        }

        private void OnCardClicked(int cardIndex) {
            if (!isGameActive || isProcessingTurn || cardIndex < 0 || cardIndex >= 18) return;
            if (cardIsMatched[cardIndex] || cardIsFaceUp[cardIndex]) return;

            FlipCardUp(cardIndex);

            if (firstFlippedIndex == -1) {
                firstFlippedIndex = cardIndex;
            } else if (secondFlippedIndex == -1) {
                secondFlippedIndex = cardIndex;
                totalFlips++;
                UpdateHUD();
                StartCoroutine(ProcessTurnRoutine());
            }
        }

        private void FlipCardUp(int idx) {
            cardIsFaceUp[idx] = true;
            if (cardImages != null && idx < cardImages.Length && cardImages[idx] != null) {
                cardImages[idx].color = cardFrontBgColor;
                cardImages[idx].transform.DOPunchScale(Vector3.one * 0.12f, 0.25f);
            }
            if (cardTexts != null && idx < cardTexts.Length && cardTexts[idx] != null) {
                cardTexts[idx].text = cardDisplayTexts[idx];
            }
        }

        private void FlipCardDown(int idx) {
            cardIsFaceUp[idx] = false;
            if (cardImages != null && idx < cardImages.Length && cardImages[idx] != null) {
                cardImages[idx].color = cardBackBgColor;
            }
            if (cardTexts != null && idx < cardTexts.Length && cardTexts[idx] != null) {
                cardTexts[idx].text = "?";
            }
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

        private IEnumerator ProcessTurnRoutine() {
            isProcessingTurn = true;

            int idx1 = firstFlippedIndex;
            int idx2 = secondFlippedIndex;

            bool isMatch = (cardPairIds[idx1] == cardPairIds[idx2]);
            PlaySFX(isMatch);

            if (isMatch) {
                cardIsMatched[idx1] = true;
                cardIsMatched[idx2] = true;
                matchedPairsCount++;
                score += 100;

                if (cardImages != null) {
                    if (idx1 < cardImages.Length && cardImages[idx1] != null) cardImages[idx1].color = matchedBgColor;
                    if (idx2 < cardImages.Length && cardImages[idx2] != null) cardImages[idx2].color = matchedBgColor;
                }

                if (cardButtons != null) {
                    if (idx1 < cardButtons.Length && cardButtons[idx1] != null) cardButtons[idx1].interactable = false;
                    if (idx2 < cardButtons.Length && cardButtons[idx2] != null) cardButtons[idx2].interactable = false;
                }

                int pairId = cardPairIds[idx1];
                var pairData = GetPairById(pairId);
                if (pairData != null) {
                    if (feedbackBanner != null) {
                        feedbackBanner.SetActive(true);
                        if (feedbackTextTMP != null) feedbackTextTMP.text = $"<b>{pairData.idiomText}</b>\n<size=85%>{pairData.meaningText}</size>";
                    }

                    if (pairData.pairAudio != null) {
                        if (Masters_AudioManager.Instance != null) {
                            Masters_AudioManager.Instance.PlayVoiceOver(pairData.pairAudio);
                        } else if (localAudioSource != null) {
                            localAudioSource.PlayOneShot(pairData.pairAudio);
                        }
                    }
                }

                UpdateHUD();
                yield return new WaitForSeconds(1.5f);

                if (feedbackBanner != null) feedbackBanner.SetActive(false);

                if (matchedPairsCount >= 9) {
                    EndGame();
                    yield break;
                }
            } else {
                if (cardImages != null) {
                    if (idx1 < cardImages.Length && cardImages[idx1] != null) {
                        cardImages[idx1].color = wrongBgColor;
                        cardImages[idx1].transform.DOShakePosition(0.35f, 10f);
                    }
                    if (idx2 < cardImages.Length && cardImages[idx2] != null) {
                        cardImages[idx2].color = wrongBgColor;
                        cardImages[idx2].transform.DOShakePosition(0.35f, 10f);
                    }
                }

                yield return new WaitForSeconds(1.0f);

                FlipCardDown(idx1);
                FlipCardDown(idx2);
            }

            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            isProcessingTurn = false;
        }

        private PrepositionsOfTime_GameG02MemoryPair GetPairById(int id) {
            if (memoryPairs == null) return null;
            foreach (var p in memoryPairs) {
                if (p.pairId == id) return p;
            }
            return null;
        }

        private void UpdateHUD() {
            if (titleTMP != null) titleTMP.text = "G02 Time Pairs — Memory Match";
            if (headerTMP != null) headerTMP.text = "PREPOSITIONS OF TIME";
            if (subtitleTMP != null) subtitleTMP.text = "Flip two pocket watches to match each time idiom with its meaning.";

            if (progressTMP != null) progressTMP.text = $"Pairs: {matchedPairsCount}/9";
            if (flipsTMP != null) flipsTMP.text = $"Flips: {totalFlips}";
            if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
        }

        public void RestartGame() {
            StartCoroutine(StartGameWithIntroRoutine());
        }

        private void EndGame() {
            isGameActive = false;
            isProcessingTurn = false;

            if (tilesContainer != null) tilesContainer.gameObject.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(true);

            int min = Mathf.FloorToInt(gameTime / 60f);
            int sec = Mathf.FloorToInt(gameTime % 60f);

            if (resultTitleTMP != null) resultTitleTMP.text = "Memory Mastered!";
            if (resultScoreTMP != null) resultScoreTMP.text = $"All 9 Pairs Matched! | Time: {min:00}:{sec:00} | Flips: {totalFlips}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = $"Outstanding! You matched all 9 time idiom pairs in {totalFlips} flips!";
                resultStatusTMP.color = matchedBgColor;
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

        private void InitMemoryPairsIfEmpty() {
            if (memoryPairs != null && memoryPairs.Length > 0) return;

            memoryPairs = new PrepositionsOfTime_GameG02MemoryPair[] {
                new PrepositionsOfTime_GameG02MemoryPair { pairId = 1, idiomText = "Time flies", meaningText = "Time passes quickly." },
                new PrepositionsOfTime_GameG02MemoryPair { pairId = 2, idiomText = "Beat the clock", meaningText = "Finish something before time is up." },
                new PrepositionsOfTime_GameG02MemoryPair { pairId = 3, idiomText = "Better late than never", meaningText = "Doing something late is better than not doing it at all." },
                new PrepositionsOfTime_GameG02MemoryPair { pairId = 4, idiomText = "Around the clock", meaningText = "For 24 hours, without stopping." },
                new PrepositionsOfTime_GameG02MemoryPair { pairId = 5, idiomText = "Call it a day", meaningText = "To finish working on something." },
                new PrepositionsOfTime_GameG02MemoryPair { pairId = 6, idiomText = "At the eleventh hour", meaningText = "Almost too late or at the last possible moment." },
                new PrepositionsOfTime_GameG02MemoryPair { pairId = 7, idiomText = "In the long run", meaningText = "In the long term, over a long period of time." },
                new PrepositionsOfTime_GameG02MemoryPair { pairId = 8, idiomText = "Make up for lost time", meaningText = "To catch up intensely after losing time." },
                new PrepositionsOfTime_GameG02MemoryPair { pairId = 9, idiomText = "Ship has sailed", meaningText = "A lost opportunity, missed shot." }
            };
        }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
}
