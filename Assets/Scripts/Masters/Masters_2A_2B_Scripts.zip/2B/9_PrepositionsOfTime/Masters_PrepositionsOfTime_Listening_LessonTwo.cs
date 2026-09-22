using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit9 {

    

    /// <summary>
    /// Unit 9: Prepositions of Time — Listening Lesson Two (L02: Hear the Story — Which Time Idiom?)
    /// 8 audio-to-idiom recognition rounds with 4 choices each.
    /// Student listens to the little story and taps the time idiom that fits what happened.
    /// Pass mark = 6 / 8.
    /// </summary>
    public class Masters_PrepositionsOfTime_Listening_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class TimeIdiomStoryRoundData {
        public string storyLineText;              // e.g. "The holidays are over already — it feels like they started yesterday."
        public string correctIdiom;               // "Time flies"
        public string[] distractorIdioms;         // 3 distractors
        public AudioClip promptAudio;             // Story audio
        public AudioClip slowPromptAudio;         // Slow story audio
        public AudioClip ariaConfirmationAudio;   // Idiom meaning explanation
    }
    
        [Header("8 Story Idiom Rounds")]
        [SerializeField]
        private TimeIdiomStoryRoundData[] rounds;

        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI headerTMP;
        [SerializeField]
        private TextMeshProUGUI titleTMP;
        [SerializeField]
        private TextMeshProUGUI progressTMP;
        [SerializeField]
        private TextMeshProUGUI storyTextTMP;

        [Header("4 Idiom Card Buttons")]
        [SerializeField]
        private Button[] idiomCardButtons; // 4 buttons

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
        [SerializeField]
        private Button returnHubBtn;

        [Header("Editor Preview")]
        [Range(0, 7)]
        public int editorPreviewRound = 0;

        [Header("Colors & Styling")]
        [SerializeField]
        private Color defaultCardColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField]
        private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
        [SerializeField]
        private Color wrongColor = new Color(0.85f, 0.22f, 0.22f, 1f);

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool isSlowMode = false;

        private Image[] cardImages;
        private TextMeshProUGUI[] cardTexts;
        private List<string> currentShuffledChoices = new List<string>();
        private int currentCorrectChoiceIndex = 0;

        protected override void Awake() {
            topic = Masters_Topic.Listening;
            base.Awake();

            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitCardButtons();
            WireEventListeners();

            if (resultPanel != null) {
                resultPanel.SetActive(false);
            }
        }

        protected override void Start() {
            topic = Masters_Topic.Listening;
            base.Start();

            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (nextButton != null) {
                nextButton.gameObject.SetActive(false);
            }

            StartCoroutine(BeginFirstRoundAfterIntro());
        }

        private void EnsureHeaderAndTitle() {
            var typeWriters = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var tw in typeWriters) {
                if (tw != null && tw.GetType().Name.Contains("TypeWriter")) {
                    tw.enabled = false;
                }
            }

            if (headerTMP == null) {
                Transform hTrans = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading") ?? transform.Find("Branch");
                if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "PREPOSITIONS OF TIME ⏰";
            }

            if (titleTMP == null) {
                Transform tTrans = transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/Title");
                if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>() ?? tTrans.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (titleTMP != null) {
                titleTMP.text = "L02 Hear the Story — Which Time Idiom?";
            }

            if (progressTMP == null) {
                Transform pTrans = transform.Find("PuzzleCountTMP") ?? transform.Find("ProgressCountTMP") ?? transform.Find("HeaderContainer/Progress") ?? transform.Find("Progress");
                if (pTrans != null) progressTMP = pTrans.GetComponent<TextMeshProUGUI>();
            }
            if (progressTMP != null) {
                progressTMP.text = $"Story {currentRoundIndex + 1}/8";
            }
        }

        private void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI tmp in tmps) {
                string n = tmp.gameObject.name.ToLower();
                string parentName = tmp.transform.parent != null ? tmp.transform.parent.name.ToLower() : "";

                if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading") || n.Contains("unitheading"))) headerTMP = tmp;
                else if (titleTMP == null && (n.Contains("lessontitle") || parentName.Contains("lessontitle") || n.Contains("unittmp") || n == "title")) titleTMP = tmp;
                else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("roundcount") || n.Contains("puzzlecount"))) progressTMP = tmp;
                else if (storyTextTMP == null && (n.Contains("story") || n.Contains("prompt") || n.Contains("statement") || n.Contains("dialogue") || n.Contains("phrasecard") || parentName.Contains("phrasecard"))) storyTextTMP = tmp;
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            List<Button> cards = new List<Button>();
            foreach (Button btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                string parentName = btn.transform.parent != null ? btn.transform.parent.name.ToLower() : "";

                if (n.Contains("option") || n.Contains("chip") || n.Contains("card") || n.Contains("button_0") || n.Contains("bin") || parentName.Contains("bin")) {
                    if (btn != replayAudioBtn && !cards.Contains(btn)) {
                        cards.Add(btn);
                    }
                } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("speaker") || n.Contains("audio") || n.Contains("phrasecard"))) {
                    replayAudioBtn = btn;
                } else if (retryBtn == null && n.Contains("retry")) {
                    retryBtn = btn;
                } else if (returnHubBtn == null && (n.Contains("return") || n.Contains("hub"))) {
                    returnHubBtn = btn;
                } else if (nextButton == null && n == "nextbutton") {
                    nextButton = btn;
                }
            }

            if ((idiomCardButtons == null || idiomCardButtons.Length < 4) && cards.Count >= 4) {
                idiomCardButtons = cards.GetRange(0, 4).ToArray();
            }

            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (Toggle tog in toggles) {
                string n = tog.gameObject.name.ToLower();
                if (repeatThisToggle == null && n.Contains("repeat")) repeatThisToggle = tog;
                else if (slowToggle == null && n.Contains("slow")) slowToggle = tog;
            }

            if (resultPanel == null) {
                Transform resTrans = transform.Find("ResultPanel") ?? transform.Find("CompletionPanel");
                if (resTrans != null) resultPanel = resTrans.gameObject;
            }
        }

        private void InitCardButtons() {
            if (idiomCardButtons == null || idiomCardButtons.Length == 0) return;

            cardImages = new Image[idiomCardButtons.Length];
            cardTexts = new TextMeshProUGUI[idiomCardButtons.Length];

            for (int i = 0; i < idiomCardButtons.Length; i++) {
                if (idiomCardButtons[i] != null) {
                    int optIdx = i;
                    cardImages[i] = idiomCardButtons[i].GetComponent<Image>();
                    cardTexts[i] = idiomCardButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (cardTexts[i] == null && idiomCardButtons[i].transform.parent != null) {
                        cardTexts[i] = idiomCardButtons[i].transform.parent.GetComponentInChildren<TextMeshProUGUI>(true);
                    }

                    idiomCardButtons[i].onClick.RemoveAllListeners();
                    idiomCardButtons[i].onClick.AddListener(() => OnIdiomCardClicked(optIdx));
                }
            }
        }

        private void WireEventListeners() {
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
                retryBtn.onClick.AddListener(RestartLesson);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnReturnHubClicked);
            }

            if (nextButton != null) {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnReturnHubClicked);
            }
        }

        private IEnumerator BeginFirstRoundAfterIntro() {
            EnableCardButtons(false);

            AudioClip clip = narratorSpeech;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
                yield return new WaitForSeconds(clip.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.4f);
            }

            PlayCurrentRound();
        }

        public void PlayCurrentRound() {
            if (rounds == null || rounds.Length == 0) return;

            currentRoundIndex = Mathf.Clamp(currentRoundIndex, 0, rounds.Length - 1);
            isProcessingInput = false;

            EnsureHeaderAndTitle();
            ResetCardVisuals();
            EnableCardButtons(true);

            TimeIdiomStoryRoundData r = rounds[currentRoundIndex];

            if (storyTextTMP != null) {
                storyTextTMP.text = $"\"{r.storyLineText}\"";
            }

            // Prepare 4 choices (1 correct + 3 distractors)
            currentShuffledChoices.Clear();
            currentShuffledChoices.Add(r.correctIdiom);
            if (r.distractorIdioms != null) {
                foreach (var d in r.distractorIdioms) {
                    if (!string.IsNullOrEmpty(d)) currentShuffledChoices.Add(d);
                }
            }

            // Fisher-Yates shuffle
            for (int i = 0; i < currentShuffledChoices.Count; i++) {
                string temp = currentShuffledChoices[i];
                int rnd = Random.Range(i, currentShuffledChoices.Count);
                currentShuffledChoices[i] = currentShuffledChoices[rnd];
                currentShuffledChoices[rnd] = temp;
            }

            currentCorrectChoiceIndex = currentShuffledChoices.IndexOf(r.correctIdiom);

            // Populate cards
            if (idiomCardButtons != null) {
                for (int i = 0; i < idiomCardButtons.Length; i++) {
                    if (idiomCardButtons[i] == null) continue;

                    if (i < currentShuffledChoices.Count) {
                        idiomCardButtons[i].gameObject.SetActive(true);
                        TextMeshProUGUI txt = (cardTexts != null && i < cardTexts.Length) ? cardTexts[i] : null;
                        if (txt == null) {
                            txt = idiomCardButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                            if (txt == null && idiomCardButtons[i].transform.parent != null) {
                                txt = idiomCardButtons[i].transform.parent.GetComponentInChildren<TextMeshProUGUI>(true);
                            }
                            if (cardTexts != null && i < cardTexts.Length) cardTexts[i] = txt;
                        }

                        if (cardImages != null && i < cardImages.Length && cardImages[i] == null) {
                            cardImages[i] = idiomCardButtons[i].GetComponent<Image>();
                        }

                        if (txt != null) {
                            txt.text = currentShuffledChoices[i];
                        }
                    } else {
                        idiomCardButtons[i].gameObject.SetActive(false);
                    }
                }
            }

            PlayCurrentRoundAudio();
        }

        private void ResetCardVisuals() {
            if (cardImages != null) {
                for (int i = 0; i < cardImages.Length; i++) {
                    if (cardImages[i] != null) {
                        cardImages[i].color = defaultCardColor;
                        cardImages[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        private void OnIdiomCardClicked(int clickedIndex) {
            if (isProcessingInput) return;
            if (rounds == null || currentRoundIndex >= rounds.Length) return;

            bool isCorrect = (clickedIndex == currentCorrectChoiceIndex);
            StartCoroutine(HandleRoundEvaluation(isCorrect, clickedIndex));
        }

        private IEnumerator HandleRoundEvaluation(bool isCorrect, int clickedIndex) {
            isProcessingInput = true;
            EnableCardButtons(false);

            TimeIdiomStoryRoundData r = rounds[currentRoundIndex];

            if (isCorrect) {
                score++;
                EnsureHeaderAndTitle();

                if (cardImages != null && clickedIndex < cardImages.Length && cardImages[clickedIndex] != null) {
                    cardImages[clickedIndex].color = correctColor;
                    cardImages[clickedIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (r.ariaConfirmationAudio != null && Masters_AudioManager.Instance != null) {
                    yield return new WaitForSeconds(0.3f);
                    Masters_AudioManager.Instance.PlayVoiceOver(r.ariaConfirmationAudio);
                    yield return new WaitForSeconds(r.ariaConfirmationAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(1.3f);
                }

                if (currentRoundIndex + 1 < rounds.Length) {
                    currentRoundIndex++;
                    PlayCurrentRound();
                } else {
                    EndLesson();
                }
            } else {
                if (cardImages != null && clickedIndex < cardImages.Length && cardImages[clickedIndex] != null) {
                    cardImages[clickedIndex].color = wrongColor;
                    cardImages[clickedIndex].transform.DOShakePosition(0.4f, 8f, 15, 90, false, true);
                }

                // Highlight correct card in green as teaching feedback
                if (cardImages != null && currentCorrectChoiceIndex < cardImages.Length && cardImages[currentCorrectChoiceIndex] != null) {
                    cardImages[currentCorrectChoiceIndex].color = correctColor;
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                yield return new WaitForSeconds(1.8f);

                if (currentRoundIndex + 1 < rounds.Length) {
                    currentRoundIndex++;
                    PlayCurrentRound();
                } else {
                    EndLesson();
                }
            }
        }

        public void PlayCurrentRoundAudio() {
            if (rounds == null || currentRoundIndex >= rounds.Length) return;
            TimeIdiomStoryRoundData r = rounds[currentRoundIndex];

            AudioClip clip = (isSlowMode && r.slowPromptAudio != null) ? r.slowPromptAudio : r.promptAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        public void OnReplayAudioClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            PlayCurrentRoundAudio();
        }

        private void EnableCardButtons(bool enable) {
            if (idiomCardButtons == null) return;
            for (int i = 0; i < idiomCardButtons.Length; i++) {
                if (idiomCardButtons[i] != null) idiomCardButtons[i].interactable = enable;
            }
        }

        public void RestartLesson() {
            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (resultPanel != null) resultPanel.SetActive(false);

            PlayCurrentRound();
        }

        private void EndLesson() {
            isProcessingInput = false;
            bool passed = (score >= 6);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You scored {score}/8! (Pass mark: 6/8)";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed
                        ? "Outstanding! You mastered the common time idioms and their meanings!"
                        : "Keep practicing! Listen carefully to the story context and try again!";
                }

                if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(passed);
                if (retryBtn != null) retryBtn.gameObject.SetActive(!passed || score < 8);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(passed);
            }

            if (passed && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }

        public void OnReturnHubClicked() {
            topic = Masters_Topic.Listening;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        public void PopulateDefaultRounds() {
            string audioDir = "Assets/Audio/2B/9_PrepositionsOfTime/Listening/";

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_intro.mp3");
#endif
            }

            rounds = new TimeIdiomStoryRoundData[] {
                // Round 1
                new TimeIdiomStoryRoundData {
                    storyLineText = "The holidays are over already — it feels like they started yesterday.",
                    correctIdiom = "Time flies",
                    distractorIdioms = new string[] { "Call it a day", "Around the clock", "Third time's a charm" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r01_story.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l02_r01_story.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r01_confirm.mp3")
#endif
                },
                // Round 2
                new TimeIdiomStoryRoundData {
                    storyLineText = "He handed in the project two minutes before the deadline.",
                    correctIdiom = "In the nick of time",
                    distractorIdioms = new string[] { "Better late than never", "Ship has sailed", "Once in a blue moon" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r02_story.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l02_r02_story.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r02_confirm.mp3")
#endif
                },
                // Round 3
                new TimeIdiomStoryRoundData {
                    storyLineText = "She arrived an hour late, but at least she came.",
                    correctIdiom = "Better late than never",
                    distractorIdioms = new string[] { "Ahead of his time", "Time is money", "Not born yesterday" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r03_story.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l02_r03_story.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r03_confirm.mp3")
#endif
                },
                // Round 4
                new TimeIdiomStoryRoundData {
                    storyLineText = "He tried twice and failed; the third attempt worked.",
                    correctIdiom = "Third time's a charm",
                    distractorIdioms = new string[] { "Only time will tell", "Make up for lost time", "In the long run" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r04_story.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l02_r04_story.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r04_confirm.mp3")
#endif
                },
                // Round 5
                new TimeIdiomStoryRoundData {
                    storyLineText = "The auditions closed last week — there's nothing to be done now.",
                    correctIdiom = "Ship has sailed",
                    distractorIdioms = new string[] { "Time flies", "Beat the clock", "Call it a day" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r05_story.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l02_r05_story.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r05_confirm.mp3")
#endif
                },
                // Round 6
                new TimeIdiomStoryRoundData {
                    storyLineText = "The hospital lights stay on for twenty-four hours.",
                    correctIdiom = "Around the clock",
                    distractorIdioms = new string[] { "In the nick of time", "Not born yesterday", "Third time's a charm" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r06_story.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l02_r06_story.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r06_confirm.mp3")
#endif
                },
                // Round 7
                new TimeIdiomStoryRoundData {
                    storyLineText = "We've worked enough today; let's stop here.",
                    correctIdiom = "Call it a day",
                    distractorIdioms = new string[] { "Time is money", "Ship has sailed", "Better late than never" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r07_story.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l02_r07_story.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r07_confirm.mp3")
#endif
                },
                // Round 8
                new TimeIdiomStoryRoundData {
                    storyLineText = "You can't fool me — I've seen this trick before.",
                    correctIdiom = "Not born yesterday",
                    distractorIdioms = new string[] { "Once in a blue moon", "Ahead of his time", "Around the clock" },
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r08_story.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l02_r08_story.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l02_r08_confirm.mp3")
#endif
                }
            };
        }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
}
