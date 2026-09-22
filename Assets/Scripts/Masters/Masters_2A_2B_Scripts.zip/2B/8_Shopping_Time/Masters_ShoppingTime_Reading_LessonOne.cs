using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit8 {

    

    /// <summary>
    /// Unit 8: Shopping Time — Reading Lesson One (R01: Read the Window)
    /// Shopfront window sign comprehension with 4 plain-English explanation chips.
    /// 12 rounds of verbatim signs from p.31.
    /// Pass mark = 10 / 12.
    /// </summary>
    public class Masters_ShoppingTime_Reading_LessonOne : Masters_Lesson {

[System.Serializable]
    public class ShoppingReadingWindowRoundData {
        public string windowSignText;
        public string correctMeaningText;
        public string[] distractorMeaningTexts; // 3 distractors
        public AudioClip ariaReadoutAudio;
        public AudioClip wrongHintAudio;
    }
    
        [Header("12 Window Sign Rounds")]
        [SerializeField]
        private ShoppingReadingWindowRoundData[] rounds;

        [Header("UI Display References")]
        [SerializeField]
        private TextMeshProUGUI headerTMP;
        [SerializeField]
        private TextMeshProUGUI titleTMP;
        [SerializeField]
        private TextMeshProUGUI subtitleTMP;
        [SerializeField]
        private TextMeshProUGUI progressTMP;
        [SerializeField]
        private TextMeshProUGUI scoreTMP;
        [SerializeField]
        private TextMeshProUGUI windowSignTMP;

        [Header("4 Explanation Option Buttons")]
        [SerializeField]
        private Button[] optionButtons;

        [Header("Audio Controls")]
        [SerializeField]
        private Button replayAudioBtn;

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
        private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField]
        private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);
        [SerializeField]
        private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool currentRoundHasRetried = false;

        private Image[] optionImages;
        private TextMeshProUGUI[] optionTMPs;
        private int currentCorrectOptionIndex = 0;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;

            PurgeLegacyChildren();
            AutoBindReferences();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitOptionButtons();

            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
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
            topic = Masters_Topic.Reading;

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

        private void InitOptionButtons() {
            if (optionButtons == null || optionButtons.Length == 0) {
                AutoBindReferences();
            }

            if (optionButtons != null && optionButtons.Length > 0) {
                optionImages = new Image[optionButtons.Length];
                optionTMPs = new TextMeshProUGUI[optionButtons.Length];

                for (int i = 0; i < optionButtons.Length; i++) {
                    int index = i;
                    Button btn = optionButtons[i];
                    if (btn != null) {
                        optionImages[i] = btn.GetComponent<Image>();
                        optionTMPs[i] = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnOptionSelected(index));
                    }
                }
            }
        }

        public void LoadRound(int roundIndex) {
            if (rounds == null || roundIndex < 0 || roundIndex >= rounds.Length) return;

            currentRoundIndex = roundIndex;
            currentRoundHasRetried = false;
            isProcessingInput = false;

            ShoppingReadingWindowRoundData round = rounds[currentRoundIndex];

            if (progressTMP != null) {
                progressTMP.text = $"Round {currentRoundIndex + 1}/{rounds.Length}";
            }

            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{rounds.Length}";
            }

            if (windowSignTMP != null) {
                windowSignTMP.text = round.windowSignText;
                windowSignTMP.transform.DOKill();
                windowSignTMP.transform.localScale = Vector3.one * 0.9f;
                windowSignTMP.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }

            // Build choices array (1 correct + distractors)
            List<string> choices = new List<string>();
            choices.Add(round.correctMeaningText);
            if (round.distractorMeaningTexts != null) {
                foreach (string d in round.distractorMeaningTexts) {
                    if (!string.IsNullOrEmpty(d) && !choices.Contains(d)) {
                        choices.Add(d);
                    }
                }
            }

            // Shuffle choices
            for (int i = choices.Count - 1; i > 0; i--) {
                int r = Random.Range(0, i + 1);
                string temp = choices[i];
                choices[i] = choices[r];
                choices[r] = temp;
            }

            currentCorrectOptionIndex = choices.IndexOf(round.correctMeaningText);
            if (currentCorrectOptionIndex < 0) currentCorrectOptionIndex = 0;

            // Setup option buttons
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null) {
                        if (i < choices.Count) {
                            optionButtons[i].gameObject.SetActive(true);
                            optionButtons[i].interactable = true;

                            if (optionTMPs != null && i < optionTMPs.Length && optionTMPs[i] != null) {
                                optionTMPs[i].text = choices[i];
                            }

                            if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                                optionImages[i].color = defaultChipColor;
                            }

                            optionButtons[i].transform.DOKill();
                            optionButtons[i].transform.localScale = Vector3.one * 0.92f;
                            optionButtons[i].transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
                        } else {
                            optionButtons[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void OnOptionSelected(int buttonIndex) {
            if (isProcessingInput) return;
            isProcessingInput = true;

            bool isCorrect = (buttonIndex == currentCorrectOptionIndex);

            if (isCorrect) {
                StartCoroutine(HandleCorrectAnswer(buttonIndex));
            } else {
                StartCoroutine(HandleWrongAnswer(buttonIndex));
            }
        }

        private IEnumerator HandleCorrectAnswer(int buttonIndex) {
            if (!currentRoundHasRetried) {
                score++;
                if (scoreTMP != null) {
                    scoreTMP.text = $"Score: {score}/{rounds.Length}";
                }
            }

            if (optionImages != null && buttonIndex < optionImages.Length && optionImages[buttonIndex] != null) {
                optionImages[buttonIndex].DOColor(correctColor, 0.25f);
            }

            if (optionButtons != null && buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                optionButtons[buttonIndex].transform.DOPunchScale(Vector3.one * 0.12f, 0.35f, 5, 0.5f);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            ShoppingReadingWindowRoundData round = rounds[currentRoundIndex];
            if (round.ariaReadoutAudio != null && Masters_AudioManager.Instance != null) {
                yield return new WaitForSeconds(0.3f);
                Masters_AudioManager.Instance.PlayVoiceOver(round.ariaReadoutAudio);
                yield return new WaitForSeconds(round.ariaReadoutAudio.length + 0.4f);
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

            if (optionImages != null && buttonIndex < optionImages.Length && optionImages[buttonIndex] != null) {
                optionImages[buttonIndex].DOColor(wrongColor, 0.2f);
            }

            if (optionButtons != null && buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                optionButtons[buttonIndex].transform.DOShakePosition(0.35f, new Vector3(12f, 0, 0), 10, 90f);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            ShoppingReadingWindowRoundData round = rounds[currentRoundIndex];
            if (round.wrongHintAudio != null && Masters_AudioManager.Instance != null) {
                yield return new WaitForSeconds(0.2f);
                Masters_AudioManager.Instance.PlayVoiceOver(round.wrongHintAudio);
                yield return new WaitForSeconds(round.wrongHintAudio.length + 0.2f);
            } else {
                yield return new WaitForSeconds(0.6f);
            }

            if (optionImages != null && buttonIndex < optionImages.Length && optionImages[buttonIndex] != null) {
                optionImages[buttonIndex].DOColor(defaultChipColor, 0.2f);
            }

            isProcessingInput = false;
        }

        private void ShowResults() {
            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            bool isPassed = (score >= 10);

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score}/{rounds.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = isPassed ? "Fantastic! You understand shop window signs perfectly!" : "Keep practicing! Try reading the window signs again!";
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
            if (currentRoundIndex < 0 || currentRoundIndex >= rounds.Length) return;
            ShoppingReadingWindowRoundData round = rounds[currentRoundIndex];
            if (round.ariaReadoutAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(round.ariaReadoutAudio);
            }
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Reading;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            } else {
                base.OnNextButtonClicked();
            }
        }

        private void PurgeLegacyChildren() {
            string[] legacyNames = new string[] {
                "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)",
                "FillInTheBlanksPosition", "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition",
                "OptionButtonContainer", "RetrySpriteAndText", "CompletedPanel"
            };

            foreach (string lName in legacyNames) {
                Transform lTrans = transform.Find(lName);
                if (lTrans != null) {
                    lTrans.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(lTrans.gameObject);
                    else DestroyImmediate(lTrans.gameObject);
                }
            }

            // Purge slide animations to avoid layout interference
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
                if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
                else if (titleTMP == null && (n.Contains("lessontitle") || n == "title")) titleTMP = tmp;
                else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
                else if (progressTMP == null && (n.Contains("progress") || n.Contains("roundtmp") || n.Contains("puzzlecount"))) progressTMP = tmp;
                else if (scoreTMP == null && (n.Contains("scoretmp") || n == "score")) scoreTMP = tmp;
                else if (windowSignTMP == null && (n.Contains("sign") || n.Contains("window") || n.Contains("statement") || n.Contains("phrasecard"))) windowSignTMP = tmp;
                else if (resultScoreTMP == null && n.Contains("resultscore")) resultScoreTMP = tmp;
                else if (resultStatusTMP == null && n.Contains("resultstatus")) resultStatusTMP = tmp;
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            List<Button> opts = new List<Button>();
            foreach (Button btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("speaker"))) replayAudioBtn = btn;
                else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
                else if (nextButton == null && n == "nextbutton") nextButton = btn;
                else if (n.Contains("option") || n.Contains("chip") || n.Contains("answer") || n.Contains("card")) {
                    opts.Add(btn);
                }
            }

            if (opts.Count > 0 && (optionButtons == null || optionButtons.Length == 0)) {
                optionButtons = opts.ToArray();
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel") ?? transform.Find("Results") ?? transform.Find("RetryPanel");
                if (rTrans != null) resultPanel = rTrans.gameObject;
            }
        }

        public void PopulateDefaultRounds() {
            string audioDir = "Assets/Audio/2B/8_Shopping_Time/Reading/";

            rounds = new ShoppingReadingWindowRoundData[] {
                // Round 1
                new ShoppingReadingWindowRoundData {
                    windowSignText = "Open 24 hrs a day",
                    correctMeaningText = "You can shop here at any hour, day or night.",
                    distractorMeaningTexts = new string[] {
                        "The shop is open only during the day.",
                        "The shop is closed on weekends.",
                        "You can only enter for 24 minutes."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r01_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 2
                new ShoppingReadingWindowRoundData {
                    windowSignText = "Clearance sale",
                    correctMeaningText = "The shop is selling off its stock cheaply.",
                    distractorMeaningTexts = new string[] {
                        "The shop is cleaning its floors.",
                        "New luxury stock has arrived.",
                        "Items are only for cleared members."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r02_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 3
                new ShoppingReadingWindowRoundData {
                    windowSignText = "Closing down sale",
                    correctMeaningText = "The shop is closing for good and selling everything.",
                    distractorMeaningTexts = new string[] {
                        "The shop is closing only for lunch.",
                        "The shop is opening a new branch.",
                        "The doors are shut for the day."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r03_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 4
                new ShoppingReadingWindowRoundData {
                    windowSignText = "Half price sale",
                    correctMeaningText = "Everything costs half of what it usually costs.",
                    distractorMeaningTexts = new string[] {
                        "You only get half of the item.",
                        "Prices have doubled today.",
                        "Buy half now and pay full later."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r04_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 5
                new ShoppingReadingWindowRoundData {
                    windowSignText = "70% off on all items",
                    correctMeaningText = "Every item is reduced by seventy per cent.",
                    distractorMeaningTexts = new string[] {
                        "Only 70 items are on sale.",
                        "You must pay 70 percent extra.",
                        "The shop takes 70 minutes to open."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r05_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 6
                new ShoppingReadingWindowRoundData {
                    windowSignText = "Buy 1 Get 1 Free",
                    correctMeaningText = "Buy something and get another one free.",
                    distractorMeaningTexts = new string[] {
                        "Everything in the shop is completely free.",
                        "You must buy two items to enter.",
                        "Pay for two and get one item."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r06_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 7
                new ShoppingReadingWindowRoundData {
                    windowSignText = "Fixed price",
                    correctMeaningText = "The price cannot be argued down.",
                    distractorMeaningTexts = new string[] {
                        "The broken items have been fixed.",
                        "The price changes every minute.",
                        "You can bargain for a lower price."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r07_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 8
                new ShoppingReadingWindowRoundData {
                    windowSignText = "No bargains",
                    correctMeaningText = "The shop will not lower its prices.",
                    distractorMeaningTexts = new string[] {
                        "There are no good items in the shop.",
                        "Everything is on huge discount.",
                        "You must bargain with the cashier."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r08_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 9
                new ShoppingReadingWindowRoundData {
                    windowSignText = "Cash on delivery",
                    correctMeaningText = "You pay when the goods arrive, not before.",
                    distractorMeaningTexts = new string[] {
                        "You must pay in advance online.",
                        "Only gold coins are accepted.",
                        "Delivery is free if you pay with card."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r09_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 10
                new ShoppingReadingWindowRoundData {
                    windowSignText = "Credit cards are accepted here",
                    correctMeaningText = "You may pay with a card, not only with cash.",
                    distractorMeaningTexts = new string[] {
                        "You can only pay with cash.",
                        "The shop sells credit cards.",
                        "Cards are not allowed inside."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r10_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 11
                new ShoppingReadingWindowRoundData {
                    windowSignText = "Festival offer",
                    correctMeaningText = "There is a special price because of the festival.",
                    distractorMeaningTexts = new string[] {
                        "The shop is closed for the holiday.",
                        "Only festival dancers can shop here.",
                        "Prices are higher for the festival."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r11_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                },
                // Round 12
                new ShoppingReadingWindowRoundData {
                    windowSignText = "CCTV in operation",
                    correctMeaningText = "Cameras are watching the shop.",
                    distractorMeaningTexts = new string[] {
                        "Television shows are being filmed here.",
                        "You can buy televisions here.",
                        "The shop is open for cinema."
                    },
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_r12_sign.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r01_hint.mp3")
#endif
                }
            };
        }
    }
}
