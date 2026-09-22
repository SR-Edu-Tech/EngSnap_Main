using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit8 {

    

    /// <summary>
    /// Unit 8: Shopping Time — Listening Lesson One (L01: Hear It — Which Shop?)
    /// 10 audio-to-shop recognition rounds with instant repeat & slow speed support.
    /// Student listens to what the shopper needs and taps the shop that sells it.
    /// Pass mark = 8 / 10.
    /// </summary>
    public class Masters_ShoppingTime_Listening_LessonOne : Masters_Lesson {

[System.Serializable]
    public class ShoppingShopRoundData {
        public string shopperLineText;
        public AudioClip promptAudio;
        public AudioClip slowPromptAudio;
        public string correctShopTitle;
        public string[] distractorShopTitles; // 3 distractors
        public AudioClip ariaConfirmationAudio;
    }
    
        [Header("10 Shopping Rounds")]
        [SerializeField]
        private ShoppingShopRoundData[] rounds;

        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI headerTMP;
        [SerializeField]
        private TextMeshProUGUI titleTMP;
        [SerializeField]
        private TextMeshProUGUI progressTMP;
        [SerializeField]
        private TextMeshProUGUI shopperPromptTMP;

        [Header("4 Shop Card Buttons")]
        [SerializeField]
        private Button[] shopCardButtons; // 4 buttons

        [Header("Audio Controls")]
        [SerializeField]
        private Button replayAudioBtn;
        [SerializeField]
        private Toggle repeatThisToggle;
        [SerializeField]
        private Toggle slowToggle;

        [Header("Results & Retry Panel")]
        [SerializeField]
        private GameObject resultPanel;
        [SerializeField]
        private TextMeshProUGUI resultScoreTMP;
        [SerializeField]
        private TextMeshProUGUI resultStatusTMP;
        [SerializeField]
        private Button retryBtn;

        [Header("Colors & Styling")]
        [SerializeField]
        private Color defaultCardColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField]
        private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);
        [SerializeField]
        private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool currentRoundHasRetried = false;
        private bool isSlowMode = false;

        private Image[] cardImages;
        private string[] currentRoundShuffledShops;
        private int currentRoundCorrectChoiceIndex;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Listening;

            PurgeLegacyChildren();
            AutoBindReferences();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitCardButtons();

            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
            }

            if (repeatThisToggle != null) {
                repeatThisToggle.onValueChanged.RemoveAllListeners();
                repeatThisToggle.onValueChanged.AddListener((isOn) => {
                    OnReplayAudioClicked();
                });
            }

            if (slowToggle != null) {
                slowToggle.onValueChanged.RemoveAllListeners();
                slowToggle.onValueChanged.AddListener((isOn) => {
                    isSlowMode = isOn;
                    OnReplayAudioClicked();
                });
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(OnRetryButtonClicked);
            }

            if (resultPanel != null) {
                resultPanel.SetActive(false);
            }
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Listening;

            PurgeLegacyChildren();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (nextButton != null) {
                nextButton.gameObject.SetActive(false);
            }

            StartCoroutine(BeginFirstRoundAfterNarrator());
        }

        private IEnumerator BeginFirstRoundAfterNarrator() {
            if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
                yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.5f);
            }
            LoadRound(0);
        }

        private void InitCardButtons() {
            if (shopCardButtons == null || shopCardButtons.Length == 0) {
                shopCardButtons = GetComponentsInChildren<Button>(true);
            }

            cardImages = new Image[shopCardButtons.Length];

            for (int i = 0; i < shopCardButtons.Length; i++) {
                int index = i;
                Button btn = shopCardButtons[i];
                if (btn != null) {
                    cardImages[i] = btn.GetComponent<Image>();
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnCardSelected(index));
                }
            }
        }

        public void LoadRound(int roundIndex) {
            if (rounds == null || roundIndex < 0 || roundIndex >= rounds.Length) return;

            currentRoundIndex = roundIndex;
            currentRoundHasRetried = false;
            isProcessingInput = false;

            ShoppingShopRoundData round = rounds[currentRoundIndex];

            if (progressTMP != null) {
                progressTMP.text = $"Round {currentRoundIndex + 1}/{rounds.Length}";
            }

            if (shopperPromptTMP != null) {
                shopperPromptTMP.text = $"\"{round.shopperLineText}\"";
            }

            // Shuffle 1 correct + 3 distractors
            List<string> options = new List<string>();
            options.Add(round.correctShopTitle);
            if (round.distractorShopTitles != null) {
                foreach (string d in round.distractorShopTitles) {
                    if (!string.IsNullOrEmpty(d)) options.Add(d);
                }
            }

            // Shuffle
            for (int i = 0; i < options.Count; i++) {
                int rnd = Random.Range(i, options.Count);
                string temp = options[i];
                options[i] = options[rnd];
                options[rnd] = temp;
            }

            currentRoundShuffledShops = options.ToArray();
            currentRoundCorrectChoiceIndex = options.IndexOf(round.correctShopTitle);

            // Populate Buttons
            for (int i = 0; i < shopCardButtons.Length; i++) {
                if (i < currentRoundShuffledShops.Length && shopCardButtons[i] != null) {
                    shopCardButtons[i].gameObject.SetActive(true);
                    shopCardButtons[i].interactable = true;

                    TextMeshProUGUI btnTMP = shopCardButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (btnTMP != null) {
                        btnTMP.text = currentRoundShuffledShops[i];
                    }

                    if (cardImages != null && i < cardImages.Length && cardImages[i] != null) {
                        cardImages[i].color = defaultCardColor;
                    }

                    shopCardButtons[i].transform.DOKill();
                    shopCardButtons[i].transform.localScale = Vector3.one * 0.9f;
                    shopCardButtons[i].transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                } else if (shopCardButtons[i] != null) {
                    shopCardButtons[i].gameObject.SetActive(false);
                }
            }

            // Play Prompt Voice
            PlayCurrentPromptAudio();
        }

        private void PlayCurrentPromptAudio() {
            if (currentRoundIndex < 0 || currentRoundIndex >= rounds.Length) return;
            ShoppingShopRoundData round = rounds[currentRoundIndex];

            AudioClip clipToPlay = (isSlowMode && round.slowPromptAudio != null) ? round.slowPromptAudio : round.promptAudio;

            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            }
        }

        private void OnCardSelected(int buttonIndex) {
            if (isProcessingInput) return;
            isProcessingInput = true;

            bool isCorrect = (buttonIndex == currentRoundCorrectChoiceIndex);

            if (isCorrect) {
                StartCoroutine(HandleCorrectAnswer(buttonIndex));
            } else {
                StartCoroutine(HandleWrongAnswer(buttonIndex));
            }
        }

        private IEnumerator HandleCorrectAnswer(int buttonIndex) {
            if (!currentRoundHasRetried) {
                score++;
            }

            if (cardImages != null && buttonIndex < cardImages.Length && cardImages[buttonIndex] != null) {
                cardImages[buttonIndex].DOColor(correctColor, 0.25f);
            }

            if (shopCardButtons[buttonIndex] != null) {
                shopCardButtons[buttonIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.35f, 5, 0.5f);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Play ARIA naming audio if available
            ShoppingShopRoundData round = rounds[currentRoundIndex];
            if (round.ariaConfirmationAudio != null && Masters_AudioManager.Instance != null) {
                yield return new WaitForSeconds(0.3f);
                Masters_AudioManager.Instance.PlayVoiceOver(round.ariaConfirmationAudio);
                yield return new WaitForSeconds(round.ariaConfirmationAudio.length + 0.5f);
            } else {
                yield return new WaitForSeconds(1.2f);
            }

            // Next round or end
            if (currentRoundIndex + 1 < rounds.Length) {
                LoadRound(currentRoundIndex + 1);
            } else {
                ShowResults();
            }
        }

        private IEnumerator HandleWrongAnswer(int buttonIndex) {
            currentRoundHasRetried = true;

            if (cardImages != null && buttonIndex < cardImages.Length && cardImages[buttonIndex] != null) {
                cardImages[buttonIndex].DOColor(wrongColor, 0.2f);
            }

            if (shopCardButtons[buttonIndex] != null) {
                shopCardButtons[buttonIndex].transform.DOShakePosition(0.4f, new Vector3(15f, 0f, 0f), 10, 90f);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            yield return new WaitForSeconds(0.6f);

            if (cardImages != null && buttonIndex < cardImages.Length && cardImages[buttonIndex] != null) {
                cardImages[buttonIndex].DOColor(defaultCardColor, 0.2f);
            }

            isProcessingInput = false;
        }

        private void ShowResults() {
            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            bool isPassed = (score >= 8);

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score}/{rounds.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = isPassed ? "Great Job! You matched the shops!" : "Keep practicing! Try again!";
                resultStatusTMP.color = isPassed ? Color.green : Color.yellow;
            }

            if (isPassed) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                }
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                }
            } else {
                if (retryBtn != null) {
                    retryBtn.gameObject.SetActive(true);
                }
            }
        }

        private void OnRetryButtonClicked() {
            if (resultPanel != null) {
                resultPanel.SetActive(false);
            }
            score = 0;
            currentRoundIndex = 0;
            LoadRound(0);
        }

        private void OnReplayAudioClicked() {
            PlayCurrentPromptAudio();
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Listening;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            } else {
                base.OnNextButtonClicked();
            }
        }

        private void PurgeLegacyChildren() {
            string[] legacyNames = new string[] {
                "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)",
                "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
                "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer"
            };

            foreach (string lName in legacyNames) {
                Transform lTrans = transform.Find(lName);
                if (lTrans != null) {
                    lTrans.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(lTrans.gameObject);
                    else DestroyImmediate(lTrans.gameObject);
                }
            }
        }

        private void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI tmp in tmps) {
                string n = tmp.gameObject.name.ToLower();
                if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("unitheading"))) headerTMP = tmp;
                else if (titleTMP == null && (n.Contains("lessontitle") || n == "title")) titleTMP = tmp;
                else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("roundtmp"))) progressTMP = tmp;
                else if (shopperPromptTMP == null && (n.Contains("prompt") || n.Contains("shopper") || n.Contains("speech"))) shopperPromptTMP = tmp;
                else if (resultScoreTMP == null && n.Contains("score")) resultScoreTMP = tmp;
                else if (resultStatusTMP == null && n.Contains("status")) resultStatusTMP = tmp;
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            List<Button> cards = new List<Button>();
            foreach (Button btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                if (replayAudioBtn == null && n.Contains("replay")) replayAudioBtn = btn;
                else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
                else if (nextButton == null && n == "nextbutton") nextButton = btn;
                else if (n.Contains("card") || n.Contains("option") || n.Contains("choice") || n.Contains("button_")) {
                    cards.Add(btn);
                }
            }

            if (cards.Count > 0 && (shopCardButtons == null || shopCardButtons.Length == 0)) {
                shopCardButtons = cards.ToArray();
            }

            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (Toggle t in toggles) {
                string n = t.gameObject.name.ToLower();
                if (slowToggle == null && n.Contains("slow")) slowToggle = t;
                else if (repeatThisToggle == null && n.Contains("repeat")) repeatThisToggle = t;
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel") ?? transform.Find("Results") ?? transform.Find("RetryPanel");
                if (rTrans != null) resultPanel = rTrans.gameObject;
            }
        }

        public void PopulateDefaultRounds() {
            string audioDir = "Assets/Audio/2B/8_Shopping_Time/Listening/";
            string slowDir = audioDir + "Slow/";

            rounds = new ShoppingShopRoundData[] {
                // Round 1: Chemist / Pharmacy
                new ShoppingShopRoundData {
                    shopperLineText = "I need some medicine for my cough.",
                    correctShopTitle = "Chemist / Pharmacy",
                    distractorShopTitles = new string[] { "Baker's / Bakery", "Florist", "Shoe shop" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r01_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r01_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r01_aria.mp3")
#endif
                },
                // Round 2: Baker's / Bakery
                new ShoppingShopRoundData {
                    shopperLineText = "I want fresh bread for breakfast.",
                    correctShopTitle = "Baker's / Bakery",
                    distractorShopTitles = new string[] { "Chemist / Pharmacy", "Greengrocer's / Grocery store", "Toy shop / Toy store" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r02_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r02_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r02_aria.mp3")
#endif
                },
                // Round 3: Florist
                new ShoppingShopRoundData {
                    shopperLineText = "I'd like flowers for my grandmother.",
                    correctShopTitle = "Florist",
                    distractorShopTitles = new string[] { "Tailor", "Baker's / Bakery", "Seafood store / Fishmonger's" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r03_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r03_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r03_aria.mp3")
#endif
                },
                // Round 4: Greengrocer's / Grocery store
                new ShoppingShopRoundData {
                    shopperLineText = "I need vegetables for dinner.",
                    correctShopTitle = "Greengrocer's / Grocery store",
                    distractorShopTitles = new string[] { "Florist", "Jeweller's / Jewellery store", "Supermarket" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r04_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r04_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r04_aria.mp3")
#endif
                },
                // Round 5: Seafood store / Fishmonger's
                new ShoppingShopRoundData {
                    shopperLineText = "I want fish for tonight.",
                    correctShopTitle = "Seafood store / Fishmonger's",
                    distractorShopTitles = new string[] { "Baker's / Bakery", "Shoe shop", "Tailor" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r05_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r05_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r05_aria.mp3")
#endif
                },
                // Round 6: Toy shop / Toy store
                new ShoppingShopRoundData {
                    shopperLineText = "I'm looking for a birthday present for my little brother.",
                    correctShopTitle = "Toy shop / Toy store",
                    distractorShopTitles = new string[] { "Chemist / Pharmacy", "Jeweller's / Jewellery store", "Florist" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r06_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r06_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r06_aria.mp3")
#endif
                },
                // Round 7: Shoe shop
                new ShoppingShopRoundData {
                    shopperLineText = "I need new school shoes.",
                    correctShopTitle = "Shoe shop",
                    distractorShopTitles = new string[] { "Greengrocer's / Grocery store", "Seafood store / Fishmonger's", "Supermarket" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r07_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r07_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r07_aria.mp3")
#endif
                },
                // Round 8: Jeweller's / Jewellery store
                new ShoppingShopRoundData {
                    shopperLineText = "I want a chain for my mother.",
                    correctShopTitle = "Jeweller's / Jewellery store",
                    distractorShopTitles = new string[] { "Toy shop / Toy store", "Baker's / Bakery", "Florist" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r08_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r08_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r08_aria.mp3")
#endif
                },
                // Round 9: Tailor
                new ShoppingShopRoundData {
                    shopperLineText = "I need a dress stitched.",
                    correctShopTitle = "Tailor",
                    distractorShopTitles = new string[] { "Shoe shop", "Chemist / Pharmacy", "Greengrocer's / Grocery store" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r09_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r09_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r09_aria.mp3")
#endif
                },
                // Round 10: Supermarket
                new ShoppingShopRoundData {
                    shopperLineText = "I want to do all my week's shopping in one place.",
                    correctShopTitle = "Supermarket",
                    distractorShopTitles = new string[] { "Seafood store / Fishmonger's", "Jeweller's / Jewellery store", "Toy shop / Toy store" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r10_shopper.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l01_r10_shopper.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l01_r10_aria.mp3")
#endif
                }
            };
        }
    }
}
