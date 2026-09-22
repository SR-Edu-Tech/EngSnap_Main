using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit8 {

    

    /// <summary>
    /// Unit 8: Shopping Time — Listening Lesson Two (L02: Cheap or Dear?)
    /// 8 rounds of audio-to-attitude recognition.
    /// 4 price-tag chips: ASKING THE PRICE · TOO DEAR · A GOOD PRICE · AN OFFER.
    /// Student listens to price phrase and taps matching tag.
    /// Pass mark = 6 / 8.
    /// </summary>
    public class Masters_ShoppingTime_Listening_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class ShoppingPriceTagRoundData {
        public string phraseText;
        public AudioClip promptAudio;
        public AudioClip slowPromptAudio;
        public string correctTagCategory; // "ASKING THE PRICE", "TOO DEAR", "A GOOD PRICE", "AN OFFER"
        public AudioClip ariaExplanationAudio;
    }
    
        [Header("8 Price Phrase Rounds")]
        [SerializeField]
        private ShoppingPriceTagRoundData[] rounds;

        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI headerTMP;
        [SerializeField]
        private TextMeshProUGUI titleTMP;
        [SerializeField]
        private TextMeshProUGUI progressTMP;
        [SerializeField]
        private TextMeshProUGUI phrasePromptTMP;

        [Header("4 Price Tag Chips / Buttons")]
        [SerializeField]
        private Button[] tagButtons; // ASKING THE PRICE, TOO DEAR, A GOOD PRICE, AN OFFER

        [Header("Audio Controls")]
        [SerializeField]
        private Button replayAudioBtn;
        [SerializeField]
        private Toggle repeatThisToggle;
        [SerializeField]
        private Toggle slowToggle;

        [Header("Results Panel")]
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
        private Color defaultTagColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField]
        private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);
        [SerializeField]
        private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);

        private readonly string[] TAG_CATEGORIES = new string[] {
            "ASKING THE PRICE",
            "TOO DEAR",
            "A GOOD PRICE",
            "AN OFFER"
        };

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool currentRoundHasRetried = false;
        private bool isSlowMode = false;

        private Image[] tagImages;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Listening;

            PurgeLegacyChildren();
            AutoBindReferences();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitTagButtons();

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

        private void InitTagButtons() {
            if (tagButtons == null || tagButtons.Length == 0) {
                AutoBindReferences();
            }

            if (tagButtons != null && tagButtons.Length > 0) {
                tagImages = new Image[tagButtons.Length];

                for (int i = 0; i < tagButtons.Length; i++) {
                    int index = i;
                    Button btn = tagButtons[i];
                    if (btn != null) {
                        tagImages[i] = btn.GetComponent<Image>();
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnTagSelected(index));

                        // Set label on BinTMP
                        if (i < TAG_CATEGORIES.Length) {
                            TextMeshProUGUI btnTMP = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                            if (btnTMP == null && btn.transform.parent != null) {
                                btnTMP = btn.transform.parent.GetComponentInChildren<TextMeshProUGUI>(true);
                            }
                            if (btnTMP != null) {
                                btnTMP.text = TAG_CATEGORIES[i];
                            }
                        }
                    }
                }
            }
        }

        public void LoadRound(int roundIndex) {
            if (rounds == null || roundIndex < 0 || roundIndex >= rounds.Length) return;

            currentRoundIndex = roundIndex;
            currentRoundHasRetried = false;
            isProcessingInput = false;

            ShoppingPriceTagRoundData round = rounds[currentRoundIndex];

            if (progressTMP != null) {
                progressTMP.text = $"{currentRoundIndex + 1}/{rounds.Length}";
            }

            if (phrasePromptTMP != null) {
                phrasePromptTMP.text = $"\"{round.phraseText}\"";
            }

            // Reset tag buttons styling
            if (tagButtons != null) {
                for (int i = 0; i < tagButtons.Length; i++) {
                    if (tagButtons[i] != null) {
                        tagButtons[i].interactable = true;
                        if (tagImages != null && i < tagImages.Length && tagImages[i] != null) {
                            tagImages[i].color = defaultTagColor;
                        }

                        Transform animTarget = tagButtons[i].transform.parent != null && tagButtons[i].transform.parent.name.StartsWith("Bin") 
                            ? tagButtons[i].transform.parent 
                            : tagButtons[i].transform;

                        animTarget.DOKill();
                        animTarget.localScale = Vector3.one * 0.92f;
                        animTarget.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
                    }
                }
            }

            PlayCurrentPromptAudio();
        }

        private void PlayCurrentPromptAudio() {
            if (currentRoundIndex < 0 || currentRoundIndex >= rounds.Length) return;
            ShoppingPriceTagRoundData round = rounds[currentRoundIndex];

            AudioClip clipToPlay = (isSlowMode && round.slowPromptAudio != null) ? round.slowPromptAudio : round.promptAudio;

            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            }
        }

        private void OnTagSelected(int buttonIndex) {
            if (isProcessingInput) return;
            isProcessingInput = true;

            if (buttonIndex < 0 || buttonIndex >= TAG_CATEGORIES.Length) return;

            string selectedCategory = TAG_CATEGORIES[buttonIndex];
            ShoppingPriceTagRoundData round = rounds[currentRoundIndex];

            bool isCorrect = string.Equals(selectedCategory.Trim(), round.correctTagCategory.Trim(), System.StringComparison.OrdinalIgnoreCase);

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

            if (tagImages != null && buttonIndex < tagImages.Length && tagImages[buttonIndex] != null) {
                tagImages[buttonIndex].DOColor(correctColor, 0.25f);
            }

            // Price tag swings animation
            if (tagButtons[buttonIndex] != null) {
                Transform animTarget = tagButtons[buttonIndex].transform.parent != null && tagButtons[buttonIndex].transform.parent.name.StartsWith("Bin") 
                    ? tagButtons[buttonIndex].transform.parent 
                    : tagButtons[buttonIndex].transform;
                animTarget.DOPunchRotation(new Vector3(0, 0, 15f), 0.5f, 6, 0.5f);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // ARIA explains in one line
            ShoppingPriceTagRoundData round = rounds[currentRoundIndex];
            if (round.ariaExplanationAudio != null && Masters_AudioManager.Instance != null) {
                yield return new WaitForSeconds(0.3f);
                Masters_AudioManager.Instance.PlayVoiceOver(round.ariaExplanationAudio);
                yield return new WaitForSeconds(round.ariaExplanationAudio.length + 0.4f);
            } else {
                yield return new WaitForSeconds(1.2f);
            }

            if (currentRoundIndex + 1 < rounds.Length) {
                LoadRound(currentRoundIndex + 1);
            } else {
                ShowResults();
            }
        }

        private IEnumerator HandleWrongAnswer(int buttonIndex) {
            currentRoundHasRetried = true;

            if (tagImages != null && buttonIndex < tagImages.Length && tagImages[buttonIndex] != null) {
                tagImages[buttonIndex].DOColor(wrongColor, 0.2f);
            }

            // Gentle buzz / shake
            if (tagButtons[buttonIndex] != null) {
                Transform animTarget = tagButtons[buttonIndex].transform.parent != null && tagButtons[buttonIndex].transform.parent.name.StartsWith("Bin") 
                    ? tagButtons[buttonIndex].transform.parent 
                    : tagButtons[buttonIndex].transform;
                animTarget.DOShakePosition(0.35f, new Vector3(12f, 0, 0), 10, 90f);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            yield return new WaitForSeconds(0.5f);

            if (tagImages != null && buttonIndex < tagImages.Length && tagImages[buttonIndex] != null) {
                tagImages[buttonIndex].DOColor(defaultTagColor, 0.2f);
            }

            isProcessingInput = false;
        }

        private void ShowResults() {
            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            bool isPassed = (score >= 6);

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score}/{rounds.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = isPassed ? "Excellent! You know your price tags!" : "Keep practicing! Try again!";
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

            // Remove legacy slide animations so user's assigned layout in scene is strictly preserved
            Masters_SlideAnimation[] slideAnims = GetComponentsInChildren<Masters_SlideAnimation>(true);
            foreach (var s in slideAnims) {
                if (s != null) {
                    s.enabled = false;
                    if (Application.isPlaying) Destroy(s);
                    else DestroyImmediate(s);
                }
            }
        }

        private void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI tmp in tmps) {
                string n = tmp.gameObject.name.ToLower();
                if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("unitheading"))) headerTMP = tmp;
                else if (titleTMP == null && (n.Contains("lessontitle") || n == "title")) titleTMP = tmp;
                else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("roundtmp") || n.Contains("puzzlecount"))) progressTMP = tmp;
                else if (phrasePromptTMP == null && (n.Contains("phrasecard") || n.Contains("prompt") || n.Contains("phrase") || n.Contains("speech"))) phrasePromptTMP = tmp;
                else if (resultScoreTMP == null && n.Contains("score")) resultScoreTMP = tmp;
                else if (resultStatusTMP == null && n.Contains("status")) resultStatusTMP = tmp;
            }

            // Fallback for phrase prompt TMP if child of PhraseCard
            if (phrasePromptTMP == null) {
                Transform pCard = transform.Find("PhraseCard");
                if (pCard != null) {
                    phrasePromptTMP = pCard.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            List<Button> cards = new List<Button>();
            foreach (Button btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                if (replayAudioBtn == null && n.Contains("replay")) replayAudioBtn = btn;
                else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
                else if (nextButton == null && n == "nextbutton") nextButton = btn;
                else if (n.Contains("bin") || n.Contains("card") || n.Contains("tag") || n.Contains("option") || n.Contains("button_")) {
                    cards.Add(btn);
                }
            }

            if (cards.Count > 0 && (tagButtons == null || tagButtons.Length == 0)) {
                tagButtons = cards.ToArray();
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

            rounds = new ShoppingPriceTagRoundData[] {
                // Round 1: ASKING THE PRICE
                new ShoppingPriceTagRoundData {
                    phraseText = "Excuse me, how much is this shirt?",
                    correctTagCategory = "ASKING THE PRICE",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r01_phrase.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l02_r01_phrase.mp3"),
                    ariaExplanationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r01_aria.mp3")
#endif
                },
                // Round 2: TOO DEAR
                new ShoppingPriceTagRoundData {
                    phraseText = "That's a bit steep! I can't afford that.",
                    correctTagCategory = "TOO DEAR",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r02_phrase.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l02_r02_phrase.mp3"),
                    ariaExplanationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r02_aria.mp3")
#endif
                },
                // Round 3: AN OFFER
                new ShoppingPriceTagRoundData {
                    phraseText = "Buy one, get one free today only!",
                    correctTagCategory = "AN OFFER",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r03_phrase.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l02_r03_phrase.mp3"),
                    ariaExplanationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r03_aria.mp3")
#endif
                },
                // Round 4: A GOOD PRICE
                new ShoppingPriceTagRoundData {
                    phraseText = "That's a real bargain! It's worth every rupee.",
                    correctTagCategory = "A GOOD PRICE",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r04_phrase.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l02_r04_phrase.mp3"),
                    ariaExplanationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r04_aria.mp3")
#endif
                },
                // Round 5: ASKING THE PRICE
                new ShoppingPriceTagRoundData {
                    phraseText = "What is the price of these shoes?",
                    correctTagCategory = "ASKING THE PRICE",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r05_phrase.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l02_r05_phrase.mp3"),
                    ariaExplanationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r05_aria.mp3")
#endif
                },
                // Round 6: AN OFFER
                new ShoppingPriceTagRoundData {
                    phraseText = "You will get ten percent discount at the counter.",
                    correctTagCategory = "AN OFFER",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r06_phrase.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l02_r06_phrase.mp3"),
                    ariaExplanationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r06_aria.mp3")
#endif
                },
                // Round 7: TOO DEAR
                new ShoppingPriceTagRoundData {
                    phraseText = "That costs an arm and a leg!",
                    correctTagCategory = "TOO DEAR",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r07_phrase.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l02_r07_phrase.mp3"),
                    ariaExplanationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r07_aria.mp3")
#endif
                },
                // Round 8: A GOOD PRICE
                new ShoppingPriceTagRoundData {
                    phraseText = "That's very reasonable and affordable.",
                    correctTagCategory = "A GOOD PRICE",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r08_phrase.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "st_l02_r08_phrase.mp3"),
                    ariaExplanationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_l02_r08_aria.mp3")
#endif
                }
            };
        }
    }
}
