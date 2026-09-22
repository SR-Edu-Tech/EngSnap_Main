using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {


    /// <summary>
    /// Core Reading 3 controller for Unit 11: Art of Politeness (Book 2B).
    /// R03 Which Polishing Move?
    /// 8 rounds with a rack of six labelled polishing tools:
    /// - ADD PLEASE
    /// - ASK, DON'T ORDER
    /// - INVITE, DON'T INSTRUCT
    /// - SOFTEN THE NO
    /// - HEDGE THE DISAGREEMENT
    /// - SAY IT ABOUT YOURSELF
    /// Student reads the blunt sentence and its polished twin, then taps the tool used to polish it.
    /// Pass threshold: 6 of 8 pairs.
    /// </summary>
    public class Masters_ArtOfPoliteness_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
    public class ArtOfPoliteness_ReadingR03RoundData {
        public string bluntSentence;
        public string polishedSentence;
        public string correctTool; // One of 6 polishing moves
        public string explanationText;
        public AudioClip sentenceAudio;
        public AudioClip feedbackAudio;
    }
    
        [Header("8 Polishing Move Rounds")]
        [SerializeField] private ArtOfPoliteness_ReadingR03RoundData[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI sentenceTMP;
        [SerializeField] private TextMeshProUGUI bluntSentenceTMP;
        [SerializeField] private TextMeshProUGUI polishedSentenceTMP;
        [SerializeField] private TextMeshProUGUI speechBubbleTMP;

        [Header("6 Polishing Tool Option Buttons")]
        [SerializeField] private Button[] optionButtons;

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Toggle repeatThisToggle;
        [SerializeField] private Toggle slowToggle;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;

        [Header("Colors & Visuals")]
        [SerializeField] private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField] private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.85f, 0.22f, 0.22f, 1f);
        [SerializeField] private int passThreshold = 6;

        public static readonly string[] PolishingTools = new string[] {
            "ADD PLEASE",
            "ASK, DON'T ORDER",
            "INVITE, DON'T INSTRUCT",
            "SOFTEN THE NO",
            "HEDGE THE DISAGREEMENT",
            "SAY IT ABOUT YOURSELF"
        };

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool currentRoundHasRetried = false;
        private bool isSlowMode = false;
        private bool isRepeatMode = false;

        private Image[] optionImages;
        private TextMeshProUGUI[] optionTMPs;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/11_ArtOfPoliteness/Reading/artofpoliteness_r03_full_intro.mp3");
#endif
            }

            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitOptionButtons();
            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Reading;
            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (nextButton != null) {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(false);
            }

            currentRoundIndex = 0;
            score = 0;
            UpdateScoreUI();
            StartCoroutine(StartLessonRoutine());
        }

        private void AutoBindReferences() {
            if (headerTMP == null || titleTMP == null || progressTMP == null || sentenceTMP == null) {
                TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps) {
                    if (tmp == null) continue;
                    string n = tmp.gameObject.name.ToLower();
                    Transform parent = tmp.transform.parent;
                    string pn = parent != null ? parent.name.ToLower() : "";

                    if (headerTMP == null && (n.Contains("branch") || n.Contains("header") || pn.Contains("branch") || pn.Contains("header"))) {
                        headerTMP = tmp;
                    } else if (titleTMP == null && (n.Contains("lessontitle") || pn.Contains("lessontitle") || (n == "tmp" && pn.Contains("title")))) {
                        titleTMP = tmp;
                    } else if (progressTMP == null && (n.Contains("progresstmp") || n.Contains("progresscount") || n.Contains("progress"))) {
                        progressTMP = tmp;
                    } else if (scoreTMP == null && (n.Equals("scoretmp") || (n.Contains("score") && !n.Contains("result")))) {
                        scoreTMP = tmp;
                    } else if (sentenceTMP == null && (n.Contains("windowsigntmp") || n.Contains("sentencetmp") || n.Contains("maintmp") || n.Contains("dialoguetmp"))) {
                        sentenceTMP = tmp;
                    } else if (speechBubbleTMP == null && (n.Contains("speechbubble") || n.Contains("subtitletmp") || n.Contains("instructiontmp") || n.Contains("subtitle"))) {
                        speechBubbleTMP = tmp;
                    }
                }
            }

            if (optionButtons == null || optionButtons.Length < 6) {
                List<Button> list = new List<Button>();
                Transform container = transform.Find("OptionButtonsContainer") 
                                   ?? transform.Find("OptionsContainer") 
                                   ?? transform.Find("ButtonsContainer")
                                   ?? transform.Find("ChipsContainer");
                if (container != null) {
                    for (int i = 0; i < container.childCount; i++) {
                        Button b = container.GetChild(i).GetComponent<Button>();
                        if (b != null) list.Add(b);
                    }
                }
                if (list.Count == 0) {
                    Button[] allB = GetComponentsInChildren<Button>(true);
                    foreach (var b in allB) {
                        if (b != null && b != nextButton && b != retryBtn && b != replayAudioBtn && b.name.ToLower().Contains("option")) {
                            list.Add(b);
                        }
                    }
                }
                optionButtons = list.ToArray();
            }

            if (replayAudioBtn == null) {
                Transform rTr = transform.Find("ReplayAudioButton") ?? transform.Find("ReplayButton") ?? transform.Find("AudioControls/ReplayButton");
                if (rTr != null) replayAudioBtn = rTr.GetComponent<Button>();
            }

            if (slowToggle == null) {
                Transform sTr = transform.Find("SlowToggle") ?? transform.Find("AudioControls/SlowToggle");
                if (sTr != null) slowToggle = sTr.GetComponent<Toggle>();
            }

            if (repeatThisToggle == null) {
                Transform repTr = transform.Find("RepeatThisToggle") ?? transform.Find("AudioControls/RepeatThisToggle") ?? transform.Find("AudioControls/RepeatToggle");
                if (repTr != null) repeatThisToggle = repTr.GetComponent<Toggle>();
            }
        }

        private void EnsureHeaderAndTitle() {
            if (headerTMP != null) headerTMP.text = "THE ART OF POLITENESS";
            if (titleTMP != null) titleTMP.text = "R03 Which Polishing Move?";

            TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps) {
                if (t == null) continue;
                if (t.text.Contains("🎩") || (t.text.Contains("THE ART OF POLITENESS") && t.text.Length > 22)) {
                    t.text = "THE ART OF POLITENESS";
                }
            }
        }

        public void PopulateDefaultRounds() {
            rounds = new ArtOfPoliteness_ReadingR03RoundData[] {
                new ArtOfPoliteness_ReadingR03RoundData {
                    bluntSentence = "I want a pizza.",
                    polishedSentence = "I'll have a pizza, please.",
                    correctTool = "ADD PLEASE",
                    explanationText = "Add please and offer rather than demand."
                },
                new ArtOfPoliteness_ReadingR03RoundData {
                    bluntSentence = "Go away/leave me alone.",
                    polishedSentence = "Sorry — I'm a bit busy right now.",
                    correctTool = "SOFTEN THE NO",
                    explanationText = "Soften the no with a polite reason."
                },
                new ArtOfPoliteness_ReadingR03RoundData {
                    bluntSentence = "Tell me when you're available.",
                    polishedSentence = "Let me know when you're available.",
                    correctTool = "INVITE, DON'T INSTRUCT",
                    explanationText = "Invite instead of instructing."
                },
                new ArtOfPoliteness_ReadingR03RoundData {
                    bluntSentence = "Send me the report.",
                    polishedSentence = "Could you send me the report?",
                    correctTool = "ASK, DON'T ORDER",
                    explanationText = "Ask instead of giving an order."
                },
                new ArtOfPoliteness_ReadingR03RoundData {
                    bluntSentence = "You're wrong.",
                    polishedSentence = "I think you might be mistaken.",
                    correctTool = "HEDGE THE DISAGREEMENT",
                    explanationText = "Hedge the disagreement to soften the statement."
                },
                new ArtOfPoliteness_ReadingR03RoundData {
                    bluntSentence = "That's a bad idea.",
                    polishedSentence = "I'm not so sure that's a good idea.",
                    correctTool = "HEDGE THE DISAGREEMENT",
                    explanationText = "Hedge the disagreement to sound constructive."
                },
                new ArtOfPoliteness_ReadingR03RoundData {
                    bluntSentence = "Your work isn't good.",
                    polishedSentence = "I'm not quite satisfied with this work.",
                    correctTool = "SAY IT ABOUT YOURSELF",
                    explanationText = "Say it about yourself rather than pointing fingers."
                },
                new ArtOfPoliteness_ReadingR03RoundData {
                    bluntSentence = "I don't like the colors in this design.",
                    polishedSentence = "I'd prefer to use different colours in this design.",
                    correctTool = "SAY IT ABOUT YOURSELF",
                    explanationText = "Say it about yourself and state your preference."
                }
            };

#if UNITY_EDITOR
            string audioDir = "Assets/Audio/2B/11_ArtOfPoliteness/Reading/";
            for (int i = 0; i < rounds.Length; i++) {
                string sName = $"artofpoliteness_r03_r0{i + 1}_sentence.mp3";
                string fName = $"artofpoliteness_r03_r0{i + 1}_feedback.mp3";
                rounds[i].sentenceAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + sName);
                rounds[i].feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + fName);
            }
#endif
        }

        private void InitOptionButtons() {
            if (optionButtons == null) return;
            optionImages = new Image[optionButtons.Length];
            optionTMPs = new TextMeshProUGUI[optionButtons.Length];

            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] == null) continue;
                optionButtons[i].gameObject.SetActive(true);
                optionImages[i] = optionButtons[i].GetComponent<Image>();
                optionTMPs[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

                if (i < PolishingTools.Length && optionTMPs[i] != null) {
                    optionTMPs[i].text = PolishingTools[i];
                }

                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(index));
            }
        }

        private void WireEventListeners() {
            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
            }

            if (slowToggle != null) {
                slowToggle.onValueChanged.RemoveAllListeners();
                slowToggle.onValueChanged.AddListener((val) => {
                    isSlowMode = val;
                });
            }

            if (repeatThisToggle != null) {
                repeatThisToggle.onValueChanged.RemoveAllListeners();
                repeatThisToggle.onValueChanged.AddListener((val) => {
                    isRepeatMode = val;
                });
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartLesson);
            }
        }

        public override void EnsureNextAndBackButtonWired() {
            base.EnsureNextAndBackButtonWired();
        }

        private IEnumerator StartLessonRoutine() {
            isProcessingInput = true;
            SetOptionButtonsInteractable(false);

            if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
                yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.6f);
            }

            isProcessingInput = false;
            LoadRound(0);
        }

        private void LoadRound(int roundIndex) {
            if (rounds == null || roundIndex >= rounds.Length) {
                ShowResultsScreen();
                return;
            }

            currentRoundIndex = roundIndex;
            currentRoundHasRetried = false;
            isProcessingInput = false;

            ArtOfPoliteness_ReadingR03RoundData data = rounds[currentRoundIndex];
            if (progressTMP != null) progressTMP.text = $"{currentRoundIndex + 1}/{rounds.Length}";

            // Format Blunt -> Polished Display (No arrow character)
            if (bluntSentenceTMP != null && polishedSentenceTMP != null) {
                bluntSentenceTMP.text = $"Blunt: <color=#FFAA55>{data.bluntSentence}</color>";
                polishedSentenceTMP.text = $"Polished: <color=#55FF88>{data.polishedSentence}</color>";
            } else if (sentenceTMP != null) {
                sentenceTMP.text = $"<color=#FFD27F><b>{data.bluntSentence}</b></color>\n<color=#70FF90><b>{data.polishedSentence}</b></color>";
            }

            if (speechBubbleTMP != null) {
                speechBubbleTMP.text = "Which polishing tool was used here?";
            }

            // Ensure all 6 option buttons have the tool text and are visible
            if (optionTMPs != null) {
                for (int i = 0; i < optionTMPs.Length && i < PolishingTools.Length; i++) {
                    if (optionTMPs[i] != null) {
                        optionTMPs[i].text = PolishingTools[i];
                    }
                    if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null) {
                        optionButtons[i].gameObject.SetActive(true);
                    }
                }
            }

            ResetOptionButtonVisuals();
            SetOptionButtonsInteractable(true);

            // Play sentence audio if available
            if (data.sentenceAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(data.sentenceAudio);
            }
        }

        private void OnOptionButtonClicked(int clickedIndex) {
            if (isProcessingInput || rounds == null || currentRoundIndex >= rounds.Length) return;
            if (clickedIndex < 0 || clickedIndex >= PolishingTools.Length) return;

            string chosenTool = PolishingTools[clickedIndex];
            ArtOfPoliteness_ReadingR03RoundData data = rounds[currentRoundIndex];

            StartCoroutine(EvaluateAnswerRoutine(clickedIndex, chosenTool, data));
        }

        private IEnumerator EvaluateAnswerRoutine(int clickedIndex, string chosenTool, ArtOfPoliteness_ReadingR03RoundData data) {
            isProcessingInput = true;
            SetOptionButtonsInteractable(false);

            bool isCorrect = (chosenTool.Trim().ToUpper() == data.correctTool.Trim().ToUpper());

            if (isCorrect) {
                // Correct!
                if (!currentRoundHasRetried) {
                    score++;
                    UpdateScoreUI();
                }

                if (optionImages != null && clickedIndex < optionImages.Length && optionImages[clickedIndex] != null) {
                    optionImages[clickedIndex].color = correctColor;
                }

                if (optionButtons != null && clickedIndex < optionButtons.Length && optionButtons[clickedIndex] != null) {
                    optionButtons[clickedIndex].transform.DOKill();
                    optionButtons[clickedIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.35f, 10, 1);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (speechBubbleTMP != null) {
                    speechBubbleTMP.text = $"<color=#55FF88>Correct!</color> {data.explanationText}";
                }

                yield return new WaitForSeconds(0.4f);

                // Play explanation / feedback audio
                if (data.feedbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(data.feedbackAudio);
                    yield return new WaitForSeconds(data.feedbackAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(1.2f);
                }

                // If repeat mode is on, replay
                if (isRepeatMode) {
                    yield return new WaitForSeconds(0.5f);
                    LoadRound(currentRoundIndex);
                } else {
                    LoadRound(currentRoundIndex + 1);
                }

            } else {
                // Incorrect!
                if (optionImages != null && clickedIndex < optionImages.Length && optionImages[clickedIndex] != null) {
                    optionImages[clickedIndex].color = wrongColor;
                }

                if (optionButtons != null && clickedIndex < optionButtons.Length && optionButtons[clickedIndex] != null) {
                    optionButtons[clickedIndex].transform.DOKill(true);
                    optionButtons[clickedIndex].transform.DOShakePosition(0.45f, new Vector3(14f, 0, 0));
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (!currentRoundHasRetried) {
                    currentRoundHasRetried = true;
                    if (speechBubbleTMP != null) {
                        speechBubbleTMP.text = $"<color=#FF6666>Not quite.</color> Try again! Look at the tone change.";
                    }
                    yield return new WaitForSeconds(1.0f);
                    ResetOptionButtonVisuals();
                    SetOptionButtonsInteractable(true);
                    isProcessingInput = false;
                } else {
                    // Second failed attempt: highlight correct answer and move on
                    HighlightCorrectTool(data.correctTool);
                    if (speechBubbleTMP != null) {
                        speechBubbleTMP.text = $"<color=#FFD27F>The move was: {data.correctTool}.</color> {data.explanationText}";
                    }

                    if (data.feedbackAudio != null && Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(data.feedbackAudio);
                        yield return new WaitForSeconds(data.feedbackAudio.length + 0.3f);
                    } else {
                        yield return new WaitForSeconds(1.5f);
                    }

                    LoadRound(currentRoundIndex + 1);
                }
            }
        }

        private void HighlightCorrectTool(string correctTool) {
            for (int i = 0; i < PolishingTools.Length; i++) {
                if (PolishingTools[i].Trim().ToUpper() == correctTool.Trim().ToUpper()) {
                    if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                        optionImages[i].color = correctColor;
                    }
                    break;
                }
            }
        }

        private void ResetOptionButtonVisuals() {
            if (optionImages == null) return;
            for (int i = 0; i < optionImages.Length; i++) {
                if (optionImages[i] != null) {
                    optionImages[i].color = defaultChipColor;
                }
            }
        }

        private void SetOptionButtonsInteractable(bool interactable) {
            if (optionButtons == null) return;
            foreach (var b in optionButtons) {
                if (b != null) b.interactable = interactable;
            }
        }

        private void UpdateScoreUI() {
            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{(rounds != null ? rounds.Length : 8)}";
            }
        }

        private void OnReplayAudioClicked() {
            if (rounds == null || currentRoundIndex >= rounds.Length) return;
            AudioClip clip = rounds[currentRoundIndex].sentenceAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        private void ShowResultsScreen() {
            int total = rounds != null ? rounds.Length : 8;
            bool passed = (score >= passThreshold);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                if (resultScoreTMP != null) resultScoreTMP.text = $"{score} / {total}";
                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed ? "<color=#55FF88>GREAT JOB!</color>" : "<color=#FF6666>NICE TRY!</color>";
                }
            }

            if (passed) {
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                    NextButtonAnimation();
                }
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
            } else {
                if (retryBtn != null) retryBtn.gameObject.SetActive(true);
            }
        }

        public void RestartLesson() {
            if (resultPanel != null) resultPanel.SetActive(false);
            currentRoundIndex = 0;
            score = 0;
            UpdateScoreUI();
            LoadRound(0);
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
