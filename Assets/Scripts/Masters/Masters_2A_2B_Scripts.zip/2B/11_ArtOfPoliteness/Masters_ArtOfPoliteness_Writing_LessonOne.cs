using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {


    /// <summary>
    /// Core Writing 1 controller for Unit 11: Art of Politeness (Book 2B).
    /// W01 Complete the Polished Sentence:
    /// Student views the blunt sentence above and types the missing softening word
    /// to complete the polite sentence below.
    /// 10 verbatim items across pp.46-49.
    /// Pass threshold: 8 of 10 items (one retry per item).
    /// </summary>
    public class Masters_ArtOfPoliteness_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
    public class ArtOfPoliteness_WritingW01Item {
        public int itemId;
        public string bluntReference;        // e.g. "I want a pizza."
        public string politeWithBlank;       // e.g. "I'll have a pizza, _____ ."
        public string[] acceptedAnswers;     // e.g. ["please"]
        public string hintText;              // e.g. "Hint: Starts with 'P'"
        public string completedPolite;       // e.g. "I'll have a pizza, please."
        public AudioClip readbackAudio;      // Voiceover
    }
    
        [Header("10 Cloze Fill-in Items")]
        [SerializeField] private ArtOfPoliteness_WritingW01Item[] items;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI bluntTMP;
        [SerializeField] private TextMeshProUGUI politeTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;
        [SerializeField] private TextMeshProUGUI hintTMP;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Image inputFieldBg;

        [Header("Audio / Replay Controls")]
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Toggle repeatThisToggle;
        [SerializeField] private Toggle slowToggle;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;

        [Header("Colors & Visuals")]
        [SerializeField] private Color defaultInputBgColor = Color.white;
        [SerializeField] private Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);
        [SerializeField] private int passScore = 8;

        private int currentItemIndex = 0;
        private int score = 0;
        private int attemptsOnCurrentItem = 0;
        private bool isProcessing = false;
        private bool isRepeatMode = false;
        private bool isSlowMode = false;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Writing;

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/11_ArtOfPoliteness/Writing/artofpoliteness_w01_full_intro.mp3");
#endif
            }

            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (items == null || items.Length == 0) {
                PopulateDefaultItems();
            }

            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
            if (hintTMP != null) hintTMP.gameObject.SetActive(false);
            if (feedbackTMP != null) feedbackTMP.text = "";
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Writing;
            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (nextButton != null) {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(false);
            }

            RestartLesson();
        }

        private void AutoBindReferences() {
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
                } else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction") || pn.Contains("subtitle"))) {
                    subtitleTMP = tmp;
                } else if (progressTMP == null && (n.Contains("progress") || n.Contains("progresstmp") || n.Contains("progression count"))) {
                    progressTMP = tmp;
                } else if (scoreTMP == null && (n.Equals("scoretmp") || (n.Contains("score") && !n.Contains("result")))) {
                    scoreTMP = tmp;
                } else if (feedbackTMP == null && n.Contains("feedback")) {
                    feedbackTMP = tmp;
                } else if (hintTMP == null && (n == "hint" || n.Contains("hint"))) {
                    hintTMP = tmp;
                } else if (bluntTMP == null && (n.Contains("blunt") || n.Contains("reference") || n.Contains("faulty") || n.Contains("prompt") || n.Contains("cardtitle"))) {
                    bluntTMP = tmp;
                } else if (politeTMP == null && (n.Contains("questions text") || n.Contains("question") || n.Contains("polite") || n.Contains("cloze") || n.Contains("sentence") || n.Contains("windowsign"))) {
                    politeTMP = tmp;
                }
            }

            if (politeTMP == null && tmps.Length > 0) {
                foreach (var tmp in tmps) {
                    if (tmp != null && tmp != titleTMP && tmp != headerTMP && tmp != progressTMP && tmp != scoreTMP && tmp != hintTMP && tmp != feedbackTMP) {
                        politeTMP = tmp;
                        break;
                    }
                }
            }

            if (politeTMP != null) {
                RectTransform rt = politeTMP.rectTransform;
                if (rt != null && rt.sizeDelta.x < 500f) {
                    rt.sizeDelta = new Vector2(1000f, 150f);
                }
                politeTMP.fontSize = Mathf.Min(politeTMP.fontSize, 36f);
                politeTMP.enableWordWrapping = true;
                politeTMP.alignment = TextAlignmentOptions.Center;
            }

            if (inputField == null) {
                inputField = GetComponentInChildren<TMP_InputField>(true);
            }

            if (inputField != null && inputFieldBg == null) {
                inputFieldBg = inputField.GetComponent<Image>();
                if (inputFieldBg != null) defaultInputBgColor = inputFieldBg.color;
            }

            if (submitButton == null) {
                Button[] allB = GetComponentsInChildren<Button>(true);
                foreach (var b in allB) {
                    if (b != null && b != nextButton && b != retryBtn && b != replayAudioBtn) {
                        string bn = b.name.ToLower();
                        if (bn.Contains("submit") || bn.Contains("check") || bn == "check" || bn == "button_submit") {
                            submitButton = b;
                            break;
                        }
                    }
                }
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
            if (titleTMP != null) titleTMP.text = "W01 Complete the Polished Sentence";
            if (subtitleTMP != null) subtitleTMP.text = "Type the missing softening word to complete the polite sentence.";

            TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps) {
                if (t == null) continue;
                if (t.text.Contains("🎩") || (t.text.Contains("THE ART OF POLITENESS") && t.text.Length > 22)) {
                    t.text = "THE ART OF POLITENESS";
                }
            }
        }

        public void PopulateDefaultItems() {
            items = new ArtOfPoliteness_WritingW01Item[] {
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 1,
                    bluntReference = "I want a pizza.",
                    politeWithBlank = "I'll have a pizza, _____ .",
                    acceptedAnswers = new string[] { "please" },
                    hintText = "Hint: Starts with 'p'",
                    completedPolite = "I'll have a pizza, please."
                },
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 2,
                    bluntReference = "Go away/leave me alone.",
                    politeWithBlank = "_____ – I'm a bit busy right now.",
                    acceptedAnswers = new string[] { "Sorry", "sorry" },
                    hintText = "Hint: Starts with 'S'",
                    completedPolite = "Sorry – I'm a bit busy right now."
                },
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 3,
                    bluntReference = "Go away/leave me alone.",
                    politeWithBlank = "Sorry – I'm a _____ busy right now.",
                    acceptedAnswers = new string[] { "bit" },
                    hintText = "Hint: Starts with 'b'",
                    completedPolite = "Sorry – I'm a bit busy right now."
                },
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 4,
                    bluntReference = "Tell me when you're available.",
                    politeWithBlank = "_____ me know when you're available.",
                    acceptedAnswers = new string[] { "Let", "let" },
                    hintText = "Hint: Starts with 'L'",
                    completedPolite = "Let me know when you're available."
                },
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 5,
                    bluntReference = "Send me the report.",
                    politeWithBlank = "_____ you send me the report?",
                    acceptedAnswers = new string[] { "Could", "could" },
                    hintText = "Hint: Starts with 'C'",
                    completedPolite = "Could you send me the report?"
                },
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 6,
                    bluntReference = "You're wrong.",
                    politeWithBlank = "I _____ you might be mistaken.",
                    acceptedAnswers = new string[] { "think" },
                    hintText = "Hint: Starts with 't'",
                    completedPolite = "I think you might be mistaken."
                },
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 7,
                    bluntReference = "You're wrong.",
                    politeWithBlank = "I think you _____ be mistaken.",
                    acceptedAnswers = new string[] { "might" },
                    hintText = "Hint: Starts with 'm'",
                    completedPolite = "I think you might be mistaken."
                },
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 8,
                    bluntReference = "That's a bad idea.",
                    politeWithBlank = "I'm not so _____ that's a good idea.",
                    acceptedAnswers = new string[] { "sure" },
                    hintText = "Hint: Starts with 's'",
                    completedPolite = "I'm not so sure that's a good idea."
                },
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 9,
                    bluntReference = "Your work isn't good.",
                    politeWithBlank = "I'm not _____ satisfied with this work.",
                    acceptedAnswers = new string[] { "quite" },
                    hintText = "Hint: Starts with 'q'",
                    completedPolite = "I'm not quite satisfied with this work."
                },
                new ArtOfPoliteness_WritingW01Item {
                    itemId = 10,
                    bluntReference = "I don't like the colors in this design.",
                    politeWithBlank = "I'd _____ to use different colours in this design.",
                    acceptedAnswers = new string[] { "prefer" },
                    hintText = "Hint: Starts with 'p'",
                    completedPolite = "I'd prefer to use different colours in this design."
                }
            };

#if UNITY_EDITOR
            string audioDir = "Assets/Audio/2B/11_ArtOfPoliteness/Writing/";
            for (int i = 0; i < items.Length; i++) {
                string rName = $"artofpoliteness_w01_r{i + 1:D2}_readback.mp3";
                items[i].readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + rName);
            }
#endif
        }

        private void WireEventListeners() {
            if (submitButton != null) {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
            }

            if (inputField != null) {
                inputField.onSubmit.RemoveAllListeners();
                inputField.onSubmit.AddListener((val) => OnSubmitClicked());
            }

            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
            }

            if (slowToggle != null) {
                slowToggle.onValueChanged.RemoveAllListeners();
                slowToggle.onValueChanged.AddListener((val) => isSlowMode = val);
            }

            if (repeatThisToggle != null) {
                repeatThisToggle.onValueChanged.RemoveAllListeners();
                repeatThisToggle.onValueChanged.AddListener((val) => isRepeatMode = val);
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartLesson);
            }
        }

        public override void EnsureNextAndBackButtonWired() {
            base.EnsureNextAndBackButtonWired();
        }

        public void RestartLesson() {
            if (resultPanel != null) resultPanel.SetActive(false);
            currentItemIndex = 0;
            score = 0;
            UpdateScoreUI();
            LoadItem(0);
        }

        private void LoadItem(int itemIndex) {
            if (items == null || itemIndex >= items.Length) {
                ShowResultsScreen();
                return;
            }

            currentItemIndex = itemIndex;
            attemptsOnCurrentItem = 0;
            isProcessing = false;

            ArtOfPoliteness_WritingW01Item item = items[currentItemIndex];

            if (progressTMP != null) progressTMP.text = $"{currentItemIndex + 1}/{items.Length}";

            if (bluntTMP != null && politeTMP != null && bluntTMP != politeTMP) {
                bluntTMP.text = $"<color=#FFAA55><b>Blunt:</b> \"{item.bluntReference}\"</color>";
                politeTMP.text = $"<color=#FFFFFF><b>Polite:</b> {item.politeWithBlank}</color>";
            } else if (politeTMP != null) {
                politeTMP.text = $"<color=#FFD27F><b>\"{item.bluntReference}\"</b></color>\n<color=#FFFFFF>{item.politeWithBlank}</color>";
            } else if (bluntTMP != null) {
                bluntTMP.text = $"<color=#FFD27F><b>\"{item.bluntReference}\"</b></color>\n<color=#FFFFFF>{item.politeWithBlank}</color>";
            }

            if (hintTMP != null) hintTMP.gameObject.SetActive(false);
            if (feedbackTMP != null) feedbackTMP.text = "";

            if (inputField != null) {
                inputField.text = "";
                inputField.interactable = true;
                inputField.ActivateInputField();
            }

            if (inputFieldBg != null) {
                inputFieldBg.color = defaultInputBgColor;
            }

            if (submitButton != null) {
                submitButton.interactable = true;
            }
        }

        private void OnSubmitClicked() {
            if (isProcessing || items == null || currentItemIndex >= items.Length || inputField == null) return;

            string userText = inputField.text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            ArtOfPoliteness_WritingW01Item item = items[currentItemIndex];
            StartCoroutine(EvaluateAnswerRoutine(userText, item));
        }

        private IEnumerator EvaluateAnswerRoutine(string userText, ArtOfPoliteness_WritingW01Item item) {
            isProcessing = true;
            if (submitButton != null) submitButton.interactable = false;
            if (inputField != null) inputField.interactable = false;

            bool isCorrect = IsAnswerCorrect(userText, item);

            if (isCorrect) {
                // Correct!
                if (attemptsOnCurrentItem == 0) {
                    score++;
                    UpdateScoreUI();
                }

                if (inputFieldBg != null) inputFieldBg.color = correctColor;
                if (feedbackTMP != null) feedbackTMP.text = $"<color=#55FF88><b>Correct!</b></color> {item.completedPolite}";

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                yield return new WaitForSeconds(0.4f);

                if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                    yield return new WaitForSeconds(item.readbackAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(1.2f);
                }

                if (isRepeatMode) {
                    yield return new WaitForSeconds(0.5f);
                    LoadItem(currentItemIndex);
                } else {
                    LoadItem(currentItemIndex + 1);
                }

            } else {
                // Wrong!
                attemptsOnCurrentItem++;

                if (inputFieldBg != null) inputFieldBg.color = wrongColor;
                if (inputField != null) {
                    inputField.transform.DOKill(true);
                    inputField.transform.DOShakePosition(0.45f, new Vector3(14f, 0, 0));
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (attemptsOnCurrentItem == 1) {
                    // Show 1-retry hint
                    if (hintTMP != null) {
                        hintTMP.text = $"<color=#FFD27F>{item.hintText}</color>";
                        hintTMP.gameObject.SetActive(true);
                    }
                    if (feedbackTMP != null) {
                        feedbackTMP.text = "<color=#FF6666>Try again! Look at the first letter.</color>";
                    }

                    yield return new WaitForSeconds(0.9f);

                    if (inputFieldBg != null) inputFieldBg.color = defaultInputBgColor;
                    if (inputField != null) {
                        inputField.text = "";
                        inputField.interactable = true;
                        inputField.ActivateInputField();
                    }
                    if (submitButton != null) submitButton.interactable = true;
                    isProcessing = false;

                } else {
                    // Second failed attempt: show completed polite sentence & move on
                    if (feedbackTMP != null) {
                        feedbackTMP.text = $"<color=#FFD27F><b>Answer:</b> {item.completedPolite}</color>";
                    }

                    if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                        yield return new WaitForSeconds(item.readbackAudio.length + 0.3f);
                    } else {
                        yield return new WaitForSeconds(1.5f);
                    }

                    LoadItem(currentItemIndex + 1);
                }
            }
        }

        private bool IsAnswerCorrect(string userText, ArtOfPoliteness_WritingW01Item item) {
            if (string.IsNullOrEmpty(userText) || item == null || item.acceptedAnswers == null) return false;

            string cleanedUser = CleanString(userText);
            foreach (var acc in item.acceptedAnswers) {
                if (CleanString(acc) == cleanedUser) return true;
            }

            return false;
        }

        private string CleanString(string input) {
            if (string.IsNullOrEmpty(input)) return "";
            return input.Trim().ToLower().Replace(".", "").Replace(",", "").Replace("!", "").Replace("?", "");
        }

        private void UpdateScoreUI() {
            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{(items != null ? items.Length : 10)}";
            }
        }

        private void OnReplayAudioClicked() {
            if (items == null || currentItemIndex >= items.Length) return;
            AudioClip clip = items[currentItemIndex].readbackAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        private void ShowResultsScreen() {
            int total = items != null ? items.Length : 10;
            bool passed = (score >= passScore);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                if (resultScoreTMP != null) resultScoreTMP.text = $"{score} / {total}";
                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed ? "<color=#55FF88>EXCELLENT WORK!</color>" : "<color=#FF6666>NICE TRY!</color>";
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
