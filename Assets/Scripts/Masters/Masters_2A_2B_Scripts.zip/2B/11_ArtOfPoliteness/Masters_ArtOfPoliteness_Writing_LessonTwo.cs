using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {

    

    /// <summary>
    /// Unit 11: Art of Politeness — Writing Lesson Two (W02 Polish It Yourself).
    /// Student rewrites 3 blunt messages politely using the moves learned in this unit:
    /// Card 1: "Give me your notes." -> Asking instead of ordering
    /// Card 2: "Your drawing is untidy." -> Saying it about yourself
    /// Card 3: "No, I can't come." -> Softening the refusal with a reason
    /// Pass condition: Polishes at least 2 of 3 cards (one retry each).
    /// </summary>
    public class Masters_ArtOfPoliteness_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class ArtOfPoliteness_WritingW02Card {
        public int cardId;
        public string bluntMessage;          // e.g. "Give me your notes."
        [TextArea(2, 4)]
        public string polishedModelAnswer;   // e.g. "Could you lend me your notes, please?"
        public string[] acceptedPhrases;     // Array of acceptable polite sentences
        public string moveDescription;       // e.g. "Asking instead of ordering"
        [TextArea(2, 4)]
        public string hintText;              // e.g. "Try turning this command into a polite question using 'Could you...?'"
        public AudioClip promptAudio;        // Blunt prompt voiceover
        public AudioClip solutionAudio;      // Polite model readout
        public AudioClip feedbackAudio;      // ARIA move explanation
    }
    
        [Header("3 Blunt Message Cards")]
        [SerializeField] private ArtOfPoliteness_WritingW02Card[] cards;

        [Header("UI Header & Titles")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;

        [Header("Card Stage & Texts")]
        [SerializeField] private GameObject cardObject;
        [SerializeField] private TextMeshProUGUI categoryTitleTMP;
        [SerializeField] private TextMeshProUGUI bluntTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;
        [SerializeField] private TextMeshProUGUI hintTMP;

        [Header("Polishing Tool Rack (Right Card)")]
        [SerializeField] private TextMeshProUGUI rackTitleTMP;
        [SerializeField] private TextMeshProUGUI[] rackItemTMPs;

        [Header("Input & Actions")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitBtn;
        [SerializeField] private Image inputFieldBg;
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Toggle slowToggle;
        [SerializeField] private Toggle repeatThisToggle;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;

        [Header("Pass Threshold & Colors")]
        [SerializeField] private int passScore = 2;
        [SerializeField] private Color defaultInputBgColor = Color.white;
        [SerializeField] private Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

        private int currentCardIndex = 0;
        private int score = 0;
        private int attemptsOnCurrentCard = 0;
        private bool isProcessing = false;
        private bool isSlowMode = false;
        private bool isRepeatMode = false;

        protected override void Awake() {
            topic = Masters_Topic.Writing;
            base.Awake();

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/11_ArtOfPoliteness/Writing/artofpoliteness_w02_full_intro.mp3");
#endif
            }

            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (cards == null || cards.Length == 0) {
                PopulateDefaultCards();
            }

            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
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
            Transform validatorTr = transform.Find("Validator") ?? transform.Find("Checklist");

            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps) {
                if (tmp == null) continue;

                // Ignore children of Validator during generic binding
                if (validatorTr != null && tmp.transform.IsChildOf(validatorTr)) {
                    continue;
                }

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
                } else if (bluntTMP == null && (n.Contains("jumbled sentences text") || n.Contains("blunt") || n.Contains("wobbly") || n.Contains("reference") || n.Contains("faulty") || n.Contains("question") || n.Contains("sentence"))) {
                    bluntTMP = tmp;
                }
            }

            if (bluntTMP == null && tmps.Length > 0) {
                foreach (var tmp in tmps) {
                    if (tmp != null && (validatorTr == null || !tmp.transform.IsChildOf(validatorTr)) && tmp != titleTMP && tmp != headerTMP && tmp != progressTMP && tmp != scoreTMP && tmp != hintTMP && tmp != feedbackTMP && tmp != categoryTitleTMP) {
                        bluntTMP = tmp;
                        break;
                    }
                }
            }

            if (bluntTMP != null) {
                RectTransform rt = bluntTMP.rectTransform;
                if (rt != null && rt.sizeDelta.x < 500f) {
                    rt.sizeDelta = new Vector2(1000f, 150f);
                }
                bluntTMP.fontSize = Mathf.Min(bluntTMP.fontSize, 36f);
                bluntTMP.enableWordWrapping = true;
                bluntTMP.alignment = TextAlignmentOptions.Center;
            }

            if (hintTMP != null) {
                hintTMP.gameObject.SetActive(true);
                hintTMP.enableWordWrapping = true;
                hintTMP.alignment = TextAlignmentOptions.Center;
                hintTMP.fontSize = Mathf.Min(hintTMP.fontSize, 36f);
            }

            if (inputField == null) {
                inputField = GetComponentInChildren<TMP_InputField>(true);
            }

            if (inputField != null && inputFieldBg == null) {
                inputFieldBg = inputField.GetComponent<Image>();
                if (inputFieldBg != null) defaultInputBgColor = inputFieldBg.color;
            }

            if (submitBtn == null) {
                Button[] allB = GetComponentsInChildren<Button>(true);
                foreach (var b in allB) {
                    if (b != null && b != nextButton && b != retryBtn && b != replayAudioBtn) {
                        string bn = b.name.ToLower();
                        if (bn.Contains("submit") || bn.Contains("check") || bn == "check" || bn == "button_submit") {
                            submitBtn = b;
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

            // Bind Tool Rack specifically
            if (validatorTr != null) {
                validatorTr.gameObject.SetActive(true);

                Transform borderTr = validatorTr.Find("border") ?? validatorTr;
                if (rackTitleTMP == null) {
                    rackTitleTMP = borderTr.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
                }

                Transform checklistTr = borderTr.Find("Checklist") ?? validatorTr.Find("Checklist");
                if (checklistTr != null) {
                    var list = new List<TextMeshProUGUI>();
                    for (int i = 0; i < checklistTr.childCount; i++) {
                        Transform child = checklistTr.GetChild(i);
                        TextMeshProUGUI t = child.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (t != null) list.Add(t);
                    }
                    rackItemTMPs = list.ToArray();
                }
            }
        }

        private void EnsureHeaderAndTitle() {
            if (headerTMP != null) headerTMP.text = "THE ART OF POLITENESS";
            if (titleTMP != null) titleTMP.text = "W02 Polish It Yourself";
            if (subtitleTMP != null) subtitleTMP.text = "Rewrite each blunt message politely using the moves learned in this unit.";

            if (hintTMP != null) {
                hintTMP.gameObject.SetActive(true);
                if (string.IsNullOrEmpty(hintTMP.text) || hintTMP.text.Contains("Jumbled")) {
                    hintTMP.text = "<color=#D1E8FF><b>Move Needed:</b> Ask instead of ordering</color>";
                }
            }

            if (inputField != null && inputField.placeholder != null) {
                var phTmp = inputField.placeholder.GetComponent<TextMeshProUGUI>();
                if (phTmp != null) phTmp.text = "Rewrite the message politely here...";
            }

            // Update Polishing Tool Rack text
            if (rackTitleTMP != null) {
                rackTitleTMP.text = "<b>Polishing Tool Rack</b>";
            }

            string[] defaultTools = new string[] {
                "<b>1.</b> Ask, don't order",
                "<b>2.</b> Say it about yourself",
                "<b>3.</b> Soften the 'no' with a reason"
            };

            if (rackItemTMPs != null) {
                for (int i = 0; i < rackItemTMPs.Length && i < defaultTools.Length; i++) {
                    if (rackItemTMPs[i] != null) {
                        rackItemTMPs[i].text = defaultTools[i];
                    }
                }
            }

            TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps) {
                if (t == null) continue;
                if (t.text.Contains("🎩") || (t.text.Contains("THE ART OF POLITENESS") && t.text.Length > 22)) {
                    t.text = "THE ART OF POLITENESS";
                }
            }
        }

        public void PopulateDefaultCards() {
            cards = new ArtOfPoliteness_WritingW02Card[] {
                new ArtOfPoliteness_WritingW02Card {
                    cardId = 1,
                    bluntMessage = "Give me your notes.",
                    polishedModelAnswer = "Could you lend me your notes, please?",
                    acceptedPhrases = new string[] {
                        "Could you lend me your notes, please?",
                        "Could you please lend me your notes?",
                        "Could you send me your notes, please?",
                        "Could you give me your notes, please?",
                        "Could I borrow your notes, please?",
                        "Would you mind lending me your notes?",
                        "Would you mind sharing your notes with me?",
                        "Can you lend me your notes, please?",
                        "May I borrow your notes, please?"
                    },
                    moveDescription = "Ask instead of ordering",
                    hintText = "Tool from the rack: Ask instead of ordering (e.g. 'Could you lend me your notes, please?')"
                },
                new ArtOfPoliteness_WritingW02Card {
                    cardId = 2,
                    bluntMessage = "Your drawing is untidy.",
                    polishedModelAnswer = "I'm not quite satisfied with the lines here.",
                    acceptedPhrases = new string[] {
                        "I'm not quite satisfied with the lines here.",
                        "I am not quite satisfied with the lines here.",
                        "I'd prefer a bit more space between them.",
                        "I would prefer a bit more space between them.",
                        "I think the lines could be a bit neater.",
                        "I feel the drawing could be a bit neater.",
                        "I'm not so sure about these lines."
                    },
                    moveDescription = "Say it about yourself rather than criticising directly",
                    hintText = "Tool from the rack: Say it about yourself (e.g. 'I'm not quite satisfied with the lines here.' or 'I'd prefer a bit more space between them.')"
                },
                new ArtOfPoliteness_WritingW02Card {
                    cardId = 3,
                    bluntMessage = "No, I can't come.",
                    polishedModelAnswer = "Sorry – I'm a bit busy right now.",
                    acceptedPhrases = new string[] {
                        "Sorry – I'm a bit busy right now.",
                        "Sorry, I'm a bit busy right now.",
                        "Sorry, I am a bit busy right now.",
                        "Sorry - I'm a bit busy right now.",
                        "I'm afraid I can't make it.",
                        "I am afraid I can't make it.",
                        "Sorry, I have other plans today.",
                        "I'm sorry, I'm a bit busy right now."
                    },
                    moveDescription = "Soften the refusal with a reason",
                    hintText = "Tool from the rack: Soften the 'no' with a reason (e.g. 'Sorry – I'm a bit busy right now.')"
                }
            };
        }

        private void WireEventListeners() {
            if (submitBtn != null) {
                submitBtn.onClick.RemoveAllListeners();
                submitBtn.onClick.AddListener(OnSubmitClicked);
            }

            if (inputField != null) {
                inputField.onSubmit.RemoveAllListeners();
                inputField.onSubmit.AddListener(delegate { OnSubmitClicked(); });
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartLesson);
            }

            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
            }

            if (slowToggle != null) {
                slowToggle.onValueChanged.RemoveAllListeners();
                slowToggle.onValueChanged.AddListener(val => isSlowMode = val);
            }

            if (repeatThisToggle != null) {
                repeatThisToggle.onValueChanged.RemoveAllListeners();
                repeatThisToggle.onValueChanged.AddListener(val => isRepeatMode = val);
            }
        }

        public void RestartLesson() {
            currentCardIndex = 0;
            score = 0;
            attemptsOnCurrentCard = 0;
            isProcessing = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (nextButton != null) {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(false);
            }

            UpdateScoreDisplay();
            LoadCard(currentCardIndex);
        }

        private void LoadCard(int index) {
            if (cards == null || index < 0 || index >= cards.Length) {
                ShowResults();
                return;
            }

            attemptsOnCurrentCard = 0;
            isProcessing = false;

            var card = cards[index];

            if (bluntTMP != null) {
                bluntTMP.text = $"\"{card.bluntMessage}\"";
            }

            if (progressTMP != null) {
                progressTMP.text = $"{index + 1}/{cards.Length}";
            }

            if (inputField != null) {
                inputField.text = "";
                inputField.interactable = true;
                inputField.ActivateInputField();
            }

            if (inputFieldBg != null) {
                inputFieldBg.color = defaultInputBgColor;
            }

            if (submitBtn != null) {
                submitBtn.interactable = true;
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "";
            }

            // Always show move instruction on hintTMP
            if (hintTMP != null) {
                hintTMP.gameObject.SetActive(true);
                hintTMP.text = $"<color=#D1E8FF><b>Move Needed:</b> {card.moveDescription}</color>";
            }

            UpdateScoreDisplay();

            // Play Prompt Audio
            if (card.promptAudio != null) {
                PlayAudio(card.promptAudio);
            }
        }

        public void OnSubmitClicked() {
            if (isProcessing) return;
            if (cards == null || currentCardIndex >= cards.Length) return;

            string rawInput = inputField != null ? inputField.text : "";
            if (string.IsNullOrWhiteSpace(rawInput)) {
                string msg = "<color=#FF6666>Please rewrite the sentence before submitting!</color>";
                if (feedbackTMP != null) feedbackTMP.text = msg;
                if (hintTMP != null) hintTMP.text = msg;
                return;
            }

            isProcessing = true;
            if (submitBtn != null) submitBtn.interactable = false;
            if (inputField != null) inputField.interactable = false;

            var card = cards[currentCardIndex];
            bool isCorrect = ValidateAnswer(rawInput, card);

            StartCoroutine(HandleEvaluationRoutine(isCorrect, card));
        }

        private bool ValidateAnswer(string input, ArtOfPoliteness_WritingW02Card card) {
            if (string.IsNullOrWhiteSpace(input)) return false;

            string cleanInput = CleanText(input);

            // 1. Direct match with model answer or accepted phrases
            if (cleanInput == CleanText(card.polishedModelAnswer)) return true;

            if (card.acceptedPhrases != null) {
                foreach (var phrase in card.acceptedPhrases) {
                    if (cleanInput == CleanText(phrase)) return true;
                }
            }

            // 2. Move-specific keyword checks
            switch (card.cardId) {
                case 1: // "Give me your notes." -> Asking instead of ordering
                    bool hasPoliteModal = cleanInput.Contains("could") || cleanInput.Contains("would") || cleanInput.Contains("may") || cleanInput.Contains("can") || cleanInput.Contains("mind");
                    bool hasNotesAction = cleanInput.Contains("notes") || cleanInput.Contains("lend") || cleanInput.Contains("borrow") || cleanInput.Contains("give") || cleanInput.Contains("send") || cleanInput.Contains("share");
                    if (hasPoliteModal && hasNotesAction) return true;
                    break;

                case 2: // "Your drawing is untidy." -> Saying it about yourself
                    bool hasSelfRef = cleanInput.Contains("satisfied") || cleanInput.Contains("prefer") || cleanInput.Contains("think") || cleanInput.Contains("feel") || cleanInput.Contains("sure");
                    bool hasDrawingContext = cleanInput.Contains("lines") || cleanInput.Contains("space") || cleanInput.Contains("neater") || cleanInput.Contains("drawing") || cleanInput.Contains("tidy") || cleanInput.Contains("bit");
                    if (hasSelfRef || (cleanInput.Contains("not") && cleanInput.Contains("satisfied")) || (cleanInput.Contains("prefer") && hasDrawingContext)) return true;
                    break;

                case 3: // "No, I can't come." -> Soften no with reason
                    bool hasSoftener = cleanInput.Contains("sorry") || cleanInput.Contains("afraid") || cleanInput.Contains("busy") || cleanInput.Contains("plans") || cleanInput.Contains("can't make it") || cleanInput.Contains("cannot make it");
                    if (hasSoftener) return true;
                    break;
            }

            return false;
        }

        private string CleanText(string text) {
            if (string.IsNullOrEmpty(text)) return "";
            string lower = text.Trim().ToLower();
            // Remove common punctuation
            lower = lower.Replace(".", "").Replace(",", "").Replace("?", "").Replace("!", "").Replace("'", "").Replace("’", "").Replace("-", " ").Replace("—", " ");
            while (lower.Contains("  ")) lower = lower.Replace("  ", " ");
            return lower.Trim();
        }

        private IEnumerator HandleEvaluationRoutine(bool isCorrect, ArtOfPoliteness_WritingW02Card card) {
            if (isCorrect) {
                score++;
                UpdateScoreDisplay();

                if (inputFieldBg != null) inputFieldBg.color = correctColor;
                string successMsg = $"<color=#55FF88><b>Polished!</b> {card.moveDescription}</color>";
                if (feedbackTMP != null) feedbackTMP.text = successMsg;
                if (hintTMP != null) hintTMP.text = successMsg;

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                yield return new WaitForSeconds(0.4f);

                // Play solution or feedback audio
                AudioClip clipToPlay = card.feedbackAudio != null ? card.feedbackAudio : card.solutionAudio;
                if (clipToPlay != null) {
                    PlayAudio(clipToPlay);
                    yield return new WaitForSeconds(clipToPlay.length + 0.5f);
                } else {
                    yield return new WaitForSeconds(1.5f);
                }

                if (isRepeatMode) {
                    yield return new WaitForSeconds(0.5f);
                    LoadCard(currentCardIndex);
                } else {
                    currentCardIndex++;
                    if (currentCardIndex < cards.Length) {
                        LoadCard(currentCardIndex);
                    } else {
                        ShowResults();
                    }
                }
            } else {
                attemptsOnCurrentCard++;
                if (inputFieldBg != null) inputFieldBg.color = wrongColor;

                if (inputField != null) {
                    inputField.transform.DOKill(true);
                    inputField.transform.DOShakePosition(0.45f, new Vector3(14f, 0, 0));
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (attemptsOnCurrentCard == 1) {
                    // One retry offered with hint
                    string hintMsg = $"<color=#FFD27F>{card.hintText}</color>";
                    if (hintTMP != null) {
                        hintTMP.gameObject.SetActive(true);
                        hintTMP.text = hintMsg;
                    }
                    if (feedbackTMP != null) {
                        feedbackTMP.text = "<color=#FF6666>Not quite yet. Check the tool rack hint and try again!</color>";
                    }

                    yield return new WaitForSeconds(1.8f);

                    if (inputFieldBg != null) inputFieldBg.color = defaultInputBgColor;
                    if (inputField != null) {
                        inputField.text = "";
                        inputField.interactable = true;
                        inputField.ActivateInputField();
                    }
                    if (submitBtn != null) submitBtn.interactable = true;
                    isProcessing = false;
                } else {
                    // Second failure -> reveal model answer
                    string modelMsg = $"<color=#FFD27F><b>Model Answer:</b> {card.polishedModelAnswer}</color>";
                    if (hintTMP != null) hintTMP.text = modelMsg;
                    if (feedbackTMP != null) feedbackTMP.text = modelMsg;

                    if (card.solutionAudio != null) {
                        PlayAudio(card.solutionAudio);
                        yield return new WaitForSeconds(card.solutionAudio.length + 0.5f);
                    } else {
                        yield return new WaitForSeconds(2.0f);
                    }

                    currentCardIndex++;
                    if (currentCardIndex < cards.Length) {
                        LoadCard(currentCardIndex);
                    } else {
                        ShowResults();
                    }
                }
            }
        }

        private void ShowResults() {
            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            bool passed = score >= passScore;

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "Lesson Complete!" : "Keep Practicing!";
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {cards.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? "<color=#55FF88>Well done! You successfully polished the messages with polite moves.</color>"
                    : "<color=#FFD27F>You need at least 2 polished cards to pass. Give it another try!</color>";
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

        private void UpdateScoreDisplay() {
            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{(cards != null ? cards.Length : 3)}";
            }
        }

        private void PlayAudio(AudioClip clip) {
            if (clip == null) return;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        public void ReplayCurrentAudio() {
            if (cards == null || currentCardIndex >= cards.Length) return;
            var card = cards[currentCardIndex];
            if (card.promptAudio != null) {
                PlayAudio(card.promptAudio);
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
