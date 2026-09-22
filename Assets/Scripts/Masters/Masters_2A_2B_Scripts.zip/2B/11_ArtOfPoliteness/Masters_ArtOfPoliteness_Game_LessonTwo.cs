using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {


    /// <summary>
    /// G02 Polish Pairs — Memory Match
    /// Timed concentration / memory match game for Unit 11: The Art of Politeness.
    /// An arcade memory wall of 16 face-down stones: 8 blunt sentences and their 8 polished twins.
    /// Flips two stones at a time to pair each blunt sentence with its polished version.
    /// If matched, both lock face-up, the polished one shines, and ARIA reads them.
    /// Success condition: Student finds all 8 blunt <-> polished pairs.
    /// </summary>
    public class Masters_ArtOfPoliteness_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class ArtOfPoliteness_GameG02MemoryPair {
        public int pairId;
        public string bluntSentence;
        public string polishedSentence;
        public AudioClip pairAudio;
    }
    
        [Header("G02 9 Blunt - Polished Pairs")]
        [SerializeField] private ArtOfPoliteness_GameG02MemoryPair[] memoryPairs;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI flipsTMP;
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

        private readonly Color cardBackBgColor = new Color(0.12f, 0.24f, 0.45f, 0.95f);  // Classic card back
        private readonly Color cardFrontBgColor = new Color(0.18f, 0.42f, 0.72f, 1f);   // Clean uniform card front
        private readonly Color matchedBgColor = new Color(0.15f, 0.75f, 0.35f, 1f);     // Matched green
        private readonly Color wrongBgColor = new Color(0.85f, 0.25f, 0.25f, 1f);       // Wrong red

        protected override void Awake() {
            topic = Masters_Topic.Game;
            base.Awake();

            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null) {
                localAudioSource = gameObject.AddComponent<AudioSource>();
                localAudioSource.playOnAwake = false;
            }

            AutoBindReferences();

            if (memoryPairs == null || memoryPairs.Length == 0) {
                PopulateDefaultPairs();
            }

            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Game;
            AutoBindReferences();
            EnsureHeaderAndTitle();
            WireEventListeners();

            if (nextButton != null) {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(false);
            }

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
            AudioClip clipToPlay = introAudio != null ? introAudio : narratorSpeech;
            if (clipToPlay != null) {
                introDuration = Mathf.Max(2.5f, clipToPlay.length);
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                } else if (localAudioSource != null) {
                    localAudioSource.PlayOneShot(clipToPlay);
                }
            }

            yield return new WaitForSeconds(introDuration + 0.3f);

            SetCardsInteractable(true);
            isGameActive = true;
        }

        private void Update() {
            if (!isGameActive) return;

            gameTime += Time.deltaTime;
            if (timerTMP != null) {
                int min = Mathf.FloorToInt(gameTime / 60f);
                int sec = Mathf.FloorToInt(gameTime % 60f);
                timerTMP.text = $"{min:00}:{sec:00}";
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
                returnHubBtn.onClick.AddListener(OnNextButtonClicked);
            }
        }

        public void AutoBindReferences() {
            EnsureHeaderAndTitle();

            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps) {
                if (t == null) continue;
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

            if (progressTMP != null) {
                progressTMP.gameObject.SetActive(false); // avoid HUD overlap
            }

            if (tilesContainer == null) {
                Transform tc = transform.Find("TilesContainer") ?? transform.Find("CardsContainer") ?? transform.Find("GridContainer");
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

                cardButtons = bList.ToArray();
                cardImages = iList.ToArray();
                cardTexts = tList.ToArray();
            }

            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns) {
                if (b == null) continue;
                string bn = b.gameObject.name.ToLower();
                if (bn.Contains("retry") && retryBtn == null) retryBtn = b;
                else if ((bn.Contains("return") || bn.Contains("hub")) && returnHubBtn == null) returnHubBtn = b;
            }

            if (feedbackBanner == null) {
                Transform fb = transform.Find("FeedbackBanner") ?? transform.Find("FeedbackPanel");
                if (fb != null) feedbackBanner = fb.gameObject;
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel") ?? transform.Find("CompletedPanel");
                if (rTrans != null) resultPanel = rTrans.gameObject;
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
                titleTMP.text = "G02 Polish Pairs — Memory Match";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (subtitleTMP == null) {
                Transform sTr = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Subtitle") ?? transform.Find("Instruction");
                if (sTr != null) subtitleTMP = sTr.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleTMP != null) {
                subtitleTMP.text = "Flip two stones to pair each blunt sentence with its polished version!";
            }
        }

        public void PopulateDefaultPairs() {
            memoryPairs = new ArtOfPoliteness_GameG02MemoryPair[] {
                new ArtOfPoliteness_GameG02MemoryPair { pairId = 1, bluntSentence = "I want a pizza.", polishedSentence = "I'll have a pizza, please." },
                new ArtOfPoliteness_GameG02MemoryPair { pairId = 2, bluntSentence = "Send me the report.", polishedSentence = "Could you send me the report?" },
                new ArtOfPoliteness_GameG02MemoryPair { pairId = 3, bluntSentence = "You're wrong.", polishedSentence = "I think you might be mistaken." },
                new ArtOfPoliteness_GameG02MemoryPair { pairId = 4, bluntSentence = "That's a bad idea.", polishedSentence = "I'm not so sure that's a good idea." },
                new ArtOfPoliteness_GameG02MemoryPair { pairId = 5, bluntSentence = "Tell me when you're available.", polishedSentence = "Let me know when you're available." },
                new ArtOfPoliteness_GameG02MemoryPair { pairId = 6, bluntSentence = "Go away.", polishedSentence = "Sorry – I'm a bit busy right now." },
                new ArtOfPoliteness_GameG02MemoryPair { pairId = 7, bluntSentence = "Give me your notes.", polishedSentence = "Could you lend me your notes, please?" },
                new ArtOfPoliteness_GameG02MemoryPair { pairId = 8, bluntSentence = "Your drawing is untidy.", polishedSentence = "I'm not quite satisfied with the lines here." },
                new ArtOfPoliteness_GameG02MemoryPair { pairId = 9, bluntSentence = "No, I can't come.", polishedSentence = "I'm afraid I can't make it today." }
            };
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

                // Slot A: Blunt
                cardPairIds[slotA] = memoryPairs[p].pairId;
                cardDisplayTexts[slotA] = $"<b>{memoryPairs[p].bluntSentence}</b>";

                // Slot B: Polished (Clean white text without unicode symbols)
                cardPairIds[slotB] = memoryPairs[p].pairId;
                cardDisplayTexts[slotB] = memoryPairs[p].polishedSentence;
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
                    cardTexts[i].fontSize = 20f;
                    cardTexts[i].color = Color.white;
                    cardTexts[i].alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private void SetCardsInteractable(bool interactable) {
            if (cardButtons != null) {
                for (int i = 0; i < cardButtons.Length && i < 18; i++) {
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
                cardImages[idx].transform.DOPunchScale(Vector3.one * 0.10f, 0.25f);
            }
            if (cardTexts != null && idx < cardTexts.Length && cardTexts[idx] != null) {
                cardTexts[idx].text = cardDisplayTexts[idx];
                cardTexts[idx].color = Color.white;
            }
        }

        private void FlipCardDown(int idx) {
            cardIsFaceUp[idx] = false;
            if (cardImages != null && idx < cardImages.Length && cardImages[idx] != null) {
                cardImages[idx].color = cardBackBgColor;
            }
            if (cardTexts != null && idx < cardTexts.Length && cardTexts[idx] != null) {
                cardTexts[idx].text = "?";
                cardTexts[idx].color = Color.white;
            }
        }

        private IEnumerator ProcessTurnRoutine() {
            isProcessingTurn = true;
            SetCardsInteractable(false);

            int idxA = firstFlippedIndex;
            int idxB = secondFlippedIndex;

            bool isMatch = (cardPairIds[idxA] == cardPairIds[idxB]);

            if (isMatch) {
                cardIsMatched[idxA] = true;
                cardIsMatched[idxB] = true;
                matchedPairsCount++;
                score += 10;

                // Visual flash green
                if (cardImages != null) {
                    if (idxA < cardImages.Length && cardImages[idxA] != null) {
                        cardImages[idxA].color = matchedBgColor;
                        cardImages[idxA].transform.DOScale(Vector3.one * 1.05f, 0.2f).SetLoops(2, LoopType.Yoyo);
                    }
                    if (idxB < cardImages.Length && cardImages[idxB] != null) {
                        cardImages[idxB].color = matchedBgColor;
                        cardImages[idxB].transform.DOScale(Vector3.one * 1.05f, 0.2f).SetLoops(2, LoopType.Yoyo);
                    }
                }

                // Show feedback banner
                ArtOfPoliteness_GameG02MemoryPair pair = GetPairById(cardPairIds[idxA]);
                if (feedbackBanner != null && feedbackTextTMP != null && pair != null) {
                    feedbackBanner.SetActive(true);
                    feedbackTextTMP.text = $"<color=#55FF88><b>Matched!</b></color> {pair.bluntSentence} -> {pair.polishedSentence}";
                }

                // Play Audio
                PlaySFX(true);
                if (pair != null && pair.pairAudio != null) {
                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(pair.pairAudio);
                    } else if (localAudioSource != null) {
                        localAudioSource.PlayOneShot(pair.pairAudio);
                    }
                    yield return new WaitForSeconds(pair.pairAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(0.6f);
                }

                if (feedbackBanner != null) feedbackBanner.SetActive(false);
            } else {
                // Visual flash red
                if (cardImages != null) {
                    if (idxA < cardImages.Length && cardImages[idxA] != null) cardImages[idxA].color = wrongBgColor;
                    if (idxB < cardImages.Length && cardImages[idxB] != null) cardImages[idxB].color = wrongBgColor;
                }

                PlaySFX(false);
                yield return new WaitForSeconds(0.8f);

                FlipCardDown(idxA);
                FlipCardDown(idxB);
            }

            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            isProcessingTurn = false;
            UpdateHUD();

            if (matchedPairsCount >= 9) {
                EndGame();
            } else {
                SetCardsInteractable(true);
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

        private ArtOfPoliteness_GameG02MemoryPair GetPairById(int id) {
            if (memoryPairs == null) return null;
            foreach (var p in memoryPairs) {
                if (p.pairId == id) return p;
            }
            return null;
        }

        private void UpdateHUD() {
            if (scoreTMP != null) {
                scoreTMP.text = $"Pairs: {matchedPairsCount}/9";
            }
            if (flipsTMP != null) {
                flipsTMP.text = $"Flips: {totalFlips}";
            }
        }

        private void EndGame() {
            isGameActive = false;
            SetCardsInteractable(false);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            if (resultTitleTMP != null) {
                resultTitleTMP.text = "Well Done!";
            }

            if (resultScoreTMP != null) {
                int min = Mathf.FloorToInt(gameTime / 60f);
                int sec = Mathf.FloorToInt(gameTime % 60f);
                resultScoreTMP.text = $"All 9 Pairs Matched in {totalFlips} flips! ({min:00}:{sec:00})";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = "<color=#55FF88>Great job! You mastered pairing blunt statements with their polite versions.</color>";
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                NextButtonAnimation();
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        }

        public void RestartGame() {
            StartCoroutine(StartGameWithIntroRoutine());
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
