using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit9 {

  

    /// <summary>
    /// Unit 9: Prepositions of Time — Reading Lesson One (R01: Fill the Ring — IN, ON or AT?)
    /// Tap-the-right-chip preposition completion across 12-13 verbatim sentences from p.37.
    /// Student reads the sentence and taps IN, ON or AT to complete it.
    /// Pass mark = 10 / 12 (or 10 / 13).
    /// </summary>
    public class Masters_PrepositionsOfTime_Reading_LessonOne : Masters_Lesson {

  [System.Serializable]
    public class PrepositionsOfTimeReadingRoundData {
        public string sentenceWithBlank;      // e.g. "My cousin was born _____ August."
        public string correctPreposition;     // "IN", "ON", or "AT"
        public string completedSentence;      // "My cousin was born in August."
        public AudioClip ariaReadoutAudio;    // Spoken completed sentence
        public AudioClip wrongHintAudio;      // Hint clip
    }
    
        [Header("Reading Rounds")]
        [SerializeField]
        private PrepositionsOfTimeReadingRoundData[] rounds;

        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI headerTMP;
        [SerializeField]
        private TextMeshProUGUI titleTMP;
        [SerializeField]
        private TextMeshProUGUI progressTMP;
        [SerializeField]
        private TextMeshProUGUI scoreTMP;
        [SerializeField]
        private TextMeshProUGUI sentenceTMP;

        [Header("3 Preposition Chips (IN, ON, AT)")]
        [SerializeField]
        private Button[] optionButtons; // 3 buttons for IN, ON, AT

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
        [SerializeField]
        private Button returnHubBtn;

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
        private readonly string[] standardChips = new string[] { "IN", "ON", "AT" };

        protected override void Awake() {
            topic = Masters_Topic.Reading;
            base.Awake();

            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitOptionButtons();
            WireEventListeners();

            if (resultPanel != null) {
                resultPanel.SetActive(false);
            }
        }

        protected override void Start() {
            topic = Masters_Topic.Reading;
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
                titleTMP.text = "R01 Fill the Ring — IN, ON or AT?";
            }

            if (progressTMP == null) {
                Transform pTrans = transform.Find("PuzzleCountTMP") ?? transform.Find("ProgressCountTMP") ?? transform.Find("HeaderContainer/Progress") ?? transform.Find("Progress");
                if (pTrans != null) progressTMP = pTrans.GetComponent<TextMeshProUGUI>();
            }
            if (progressTMP != null) {
                progressTMP.text = $"Sentence {currentRoundIndex + 1}/{((rounds != null && rounds.Length > 0) ? rounds.Length : 13)}";
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
                else if (scoreTMP == null && (n.Contains("scoretmp") || n == "score")) scoreTMP = tmp;
                else if (sentenceTMP == null && (n.Contains("sentence") || n.Contains("statement") || n.Contains("prompt") || n.Contains("phrasecard") || parentName.Contains("phrasecard"))) sentenceTMP = tmp;
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            List<Button> chips = new List<Button>();
            foreach (Button btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                string parentName = btn.transform.parent != null ? btn.transform.parent.name.ToLower() : "";

                if (n.Contains("option") || n.Contains("chip") || n.Contains("button_0") || n.Contains("bin") || parentName.Contains("bin") || parentName.Contains("option")) {
                    if (btn != replayAudioBtn && !chips.Contains(btn)) {
                        chips.Add(btn);
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

            if ((optionButtons == null || optionButtons.Length < 3) && chips.Count >= 3) {
                optionButtons = chips.GetRange(0, 3).ToArray();
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel") ?? transform.Find("Results") ?? transform.Find("RetryPanel");
                if (rTrans != null) resultPanel = rTrans.gameObject;
            }
        }

        private void InitOptionButtons() {
            if (optionButtons == null || optionButtons.Length == 0) return;

            optionImages = new Image[optionButtons.Length];
            optionTMPs = new TextMeshProUGUI[optionButtons.Length];

            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int optIdx = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                    optionTMPs[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (optionTMPs[i] == null && optionButtons[i].transform.parent != null) {
                        optionTMPs[i] = optionButtons[i].transform.parent.GetComponentInChildren<TextMeshProUGUI>(true);
                    }

                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionClicked(optIdx));
                }
            }
        }

        private void WireEventListeners() {
            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
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
            EnableOptionButtons(false);

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
            currentRoundHasRetried = false;
            isProcessingInput = false;

            EnsureHeaderAndTitle();
            ResetOptionVisuals();
            EnableOptionButtons(true);

            PrepositionsOfTimeReadingRoundData r = rounds[currentRoundIndex];

            if (sentenceTMP != null) {
                sentenceTMP.text = r.sentenceWithBlank;
            }

            if (progressTMP != null) {
                progressTMP.text = $"Sentence {currentRoundIndex + 1}/{rounds.Length}";
            }

            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{rounds.Length}";
            }

            // Populate the 3 chips (IN, ON, AT)
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] == null) continue;

                    if (i < standardChips.Length) {
                        optionButtons[i].gameObject.SetActive(true);
                        TextMeshProUGUI txt = (optionTMPs != null && i < optionTMPs.Length) ? optionTMPs[i] : null;
                        if (txt == null) {
                            txt = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                            if (txt == null && optionButtons[i].transform.parent != null) {
                                txt = optionButtons[i].transform.parent.GetComponentInChildren<TextMeshProUGUI>(true);
                            }
                            if (optionTMPs != null && i < optionTMPs.Length) optionTMPs[i] = txt;
                        }

                        if (txt != null) {
                            txt.text = standardChips[i];
                        }
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void ResetOptionVisuals() {
            if (optionImages != null) {
                for (int i = 0; i < optionImages.Length; i++) {
                    if (optionImages[i] != null) {
                        optionImages[i].color = defaultChipColor;
                        optionImages[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        private void OnOptionClicked(int clickedIndex) {
            if (isProcessingInput) return;
            if (rounds == null || currentRoundIndex >= rounds.Length) return;
            if (clickedIndex < 0 || clickedIndex >= standardChips.Length) return;

            string selectedPrep = standardChips[clickedIndex];
            PrepositionsOfTimeReadingRoundData r = rounds[currentRoundIndex];
            bool isCorrect = string.Equals(selectedPrep, r.correctPreposition, System.StringComparison.OrdinalIgnoreCase);

            StartCoroutine(HandleRoundEvaluation(isCorrect, clickedIndex));
        }

        private IEnumerator HandleRoundEvaluation(bool isCorrect, int clickedIndex) {
            isProcessingInput = true;
            PrepositionsOfTimeReadingRoundData r = rounds[currentRoundIndex];

            if (isCorrect) {
                EnableOptionButtons(false);
                if (!currentRoundHasRetried) {
                    score++;
                }

                if (scoreTMP != null) {
                    scoreTMP.text = $"Score: {score}/{rounds.Length}";
                }

                if (sentenceTMP != null) {
                    sentenceTMP.text = r.completedSentence;
                }

                if (optionImages != null && clickedIndex < optionImages.Length && optionImages[clickedIndex] != null) {
                    optionImages[clickedIndex].color = correctColor;
                    optionImages[clickedIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (r.ariaReadoutAudio != null && Masters_AudioManager.Instance != null) {
                    yield return new WaitForSeconds(0.25f);
                    Masters_AudioManager.Instance.PlayVoiceOver(r.ariaReadoutAudio);
                    yield return new WaitForSeconds(r.ariaReadoutAudio.length + 0.35f);
                } else {
                    yield return new WaitForSeconds(1.2f);
                }

                if (currentRoundIndex + 1 < rounds.Length) {
                    currentRoundIndex++;
                    PlayCurrentRound();
                } else {
                    EndLesson();
                }
            } else {
                currentRoundHasRetried = true;

                if (optionImages != null && clickedIndex < optionImages.Length && optionImages[clickedIndex] != null) {
                    optionImages[clickedIndex].color = wrongColor;
                    optionImages[clickedIndex].transform.DOShakePosition(0.35f, 8f, 15, 90, false, true);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (r.wrongHintAudio != null && Masters_AudioManager.Instance != null) {
                    yield return new WaitForSeconds(0.2f);
                    Masters_AudioManager.Instance.PlayVoiceOver(r.wrongHintAudio);
                    yield return new WaitForSeconds(r.wrongHintAudio.length + 0.2f);
                } else {
                    yield return new WaitForSeconds(1.0f);
                }

                if (optionImages != null && clickedIndex < optionImages.Length && optionImages[clickedIndex] != null) {
                    optionImages[clickedIndex].color = defaultChipColor;
                }

                isProcessingInput = false;
            }
        }

        public void OnReplayAudioClicked() {
            if (rounds == null || currentRoundIndex >= rounds.Length) return;
            PrepositionsOfTimeReadingRoundData r = rounds[currentRoundIndex];
            if (r.ariaReadoutAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(r.ariaReadoutAudio);
            }
        }

        private void EnableOptionButtons(bool enable) {
            if (optionButtons == null) return;
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) optionButtons[i].interactable = enable;
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
            bool passed = (score >= 10);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You scored {score}/{rounds.Length}! (Pass mark: 10/{rounds.Length})";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed
                        ? "Outstanding! You mastered IN, ON, and AT for all time expressions!"
                        : "Keep practicing! Remember: IN for long stretches, ON for days, AT for moments!";
                }

                if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(passed);
                if (retryBtn != null) retryBtn.gameObject.SetActive(!passed || score < rounds.Length);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(passed);
            }

            if (passed && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }

        public void OnReturnHubClicked() {
            topic = Masters_Topic.Reading;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        public void PopulateDefaultRounds() {
            string audioDir = "Assets/Audio/2B/9_PrepositionsOfTime/Reading/";

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_intro.mp3");
#endif
            }

            rounds = new PrepositionsOfTimeReadingRoundData[] {
                // 1
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "My cousin was born _____ August.",
                    correctPreposition = "IN",
                    completedSentence = "My cousin was born in August.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r01_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 2
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "India became free _____ 1947.",
                    correctPreposition = "IN",
                    completedSentence = "India became free in 1947.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r02_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 3
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "The flowers come out _____ Spring.",
                    correctPreposition = "IN",
                    completedSentence = "The flowers come out in Spring.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r03_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 4
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "My grandparents were young _____ the 1980s.",
                    correctPreposition = "IN",
                    completedSentence = "My grandparents were young in the 1980s.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r04_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 5
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "We will travel to the moon _____ the next century.",
                    correctPreposition = "IN",
                    completedSentence = "We will travel to the moon in the next century.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r05_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 6
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "Our test is _____ Tuesday.",
                    correctPreposition = "ON",
                    completedSentence = "Our test is on Tuesday.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r06_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 7
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "We give sweets to everyone _____ Christmas Day.",
                    correctPreposition = "ON",
                    completedSentence = "We give sweets to everyone on Christmas Day.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r07_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 8
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "The letter is dated _____ 10th of August.",
                    correctPreposition = "ON",
                    completedSentence = "The letter is dated on 10th of August.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r08_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 9
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "The school reopened _____ 1st Jan 2013.",
                    correctPreposition = "ON",
                    completedSentence = "The school reopened on 1st Jan 2013.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r09_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 10
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "The bus leaves _____ 2.30 pm.",
                    correctPreposition = "AT",
                    completedSentence = "The bus leaves at 2.30 pm.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r10_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 11
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "We eat lunch _____ noon.",
                    correctPreposition = "AT",
                    completedSentence = "We eat lunch at noon.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r11_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 12
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "The birds start singing _____ sunrise.",
                    correctPreposition = "AT",
                    completedSentence = "The birds start singing at sunrise.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r12_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
#endif
                },
                // 13
                new PrepositionsOfTimeReadingRoundData {
                    sentenceWithBlank = "We tell stories _____ dinner time.",
                    correctPreposition = "AT",
                    completedSentence = "We tell stories at dinner time.",
#if UNITY_EDITOR
                    ariaReadoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_r13_sentence.mp3"),
                    wrongHintAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_r01_hint.mp3")
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
