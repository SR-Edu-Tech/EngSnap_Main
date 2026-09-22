using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {

    

    /// <summary>
    /// Unit 10: Common Mistakes with Prepositions — Writing Lesson Two
    /// (W02 Mend the Message)
    /// Student rewrites 3 pinned messages on the Writing Bench with all planted errors mended.
    /// Case-insensitive, punctuation forgiving.
    /// Pass condition: Mends at least 2 of 3 messages fully (one retry each).
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class CommonMistakes_WritingW02Message {
        public int messageId;
        public string categoryTitle;           // e.g. "1) A NOTE TO A FRIEND (3 errors)"
        [TextArea(3, 6)]
        public string wobblyOriginalText;      // The text with errors
        [TextArea(3, 6)]
        public string mendedModelText;         // The fully corrected model text
        public string[] requiredCorrections;   // Substring checks that must all be present
        [TextArea(2, 5)]
        public string errorExplanationHint;    // Detailed hint highlighting each planted error
        public AudioClip wobblyAudio;          // Prompt voiceover (wobbly original readout)
        public AudioClip mendedAudio;          // Correct solution readout
    }
    
        [Header("3 Pinned Messages (W02)")]
        [SerializeField] private CommonMistakes_WritingW02Message[] messages;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;

        [Header("Pinned Message Card Stage")]
        [SerializeField] private GameObject messageCardObject;
        [SerializeField] private TextMeshProUGUI categoryTitleTMP;
        [SerializeField] private TextMeshProUGUI wobblyTextTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;
        [SerializeField] private TextMeshProUGUI hintTMP;
        [SerializeField] private Button replayAudioBtn;

        [Header("Input Field & Submit Button")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitBtn;
        [SerializeField] private Image inputFieldBg;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        [Header("Audio References")]
        [SerializeField] private AudioClip introAudio;

        [Header("Pass Threshold")]
        [SerializeField] private int passScore = 2;

        private int currentMessageIndex = 0;
        private int score = 0;
        private int attemptsOnCurrentMessage = 0;
        private bool isCheckingAnswer = false;

        private readonly Color defaultInputBgColor = Color.white;
        private readonly Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        private readonly Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Writing;
            base.Awake();

            AutoBindReferences();
            InitMessagesIfEmpty();
            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Writing;
            AutoBindReferences();
            EnsureHeaderAndTitle();
            EnsureNextAndBackButtonWired();
            EnsureAspectRatiosAndAnchorsPreserved();

            if (introAudio == null) {
#if UNITY_EDITOR
                introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Writing/cm_w02_intro.mp3");
#endif
            }

            StartCoroutine(InitializeLessonRoutine());
        }

        private IEnumerator InitializeLessonRoutine() {
            RestartLesson(false);

            if (introAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
                yield return new WaitForSeconds(introAudio.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.4f);
            }

            // Play first message wobbly audio
            PlayCurrentMessageAudio();
        }

        private void EnsureHeaderAndTitle() {
            if (headerTMP == null) {
                Transform hTr = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("Branch") ?? transform.Find("UnitHeading");
                if (hTr != null) headerTMP = hTr.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "COMMON MISTAKES WITH PREPOSITIONS";
            }

            if (titleTMP == null) {
                Transform tTr = transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("LessonTitle") ?? transform.Find("HeaderContainer/Title") ?? transform.Find("Title");
                if (tTr != null) titleTMP = tTr.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "W02 Mend the Message";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (subtitleTMP == null) {
                Transform sTr = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Subtitle") ?? transform.Find("Instruction");
                if (sTr != null) subtitleTMP = sTr.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleTMP != null) {
                subtitleTMP.text = "Rewrite each message with every sentence mended.";
            }
        }

        private void AutoBindReferences() {
            EnsureHeaderAndTitle();

            if (categoryTitleTMP == null) {
                Transform cTr = transform.Find("ComicStageCard/StripTitleTMP")
                             ?? transform.Find("CardObject/StripTitleTMP")
                             ?? transform.Find("StripTitleTMP")
                             ?? transform.Find("CardObject/CardTitleTMP")
                             ?? transform.Find("ComicStageCard/CardTitleTMP")
                             ?? transform.Find("DeskCard/CardTitleTMP");
                if (cTr != null) categoryTitleTMP = cTr.GetComponent<TextMeshProUGUI>();
            }

            if (wobblyTextTMP == null) {
                Transform wTr = transform.Find("ComicStageCard/SpeakerAPromptTMP")
                             ?? transform.Find("CardObject/SpeakerAPromptTMP")
                             ?? transform.Find("SpeakerAPromptTMP")
                             ?? transform.Find("ComicStageCard/PromptGuideTMP")
                             ?? transform.Find("CardObject/PromptGuideTMP")
                             ?? transform.Find("PromptGuideTMP");
                if (wTr != null) wobblyTextTMP = wTr.GetComponent<TextMeshProUGUI>();
            }

            if (hintTMP == null) {
                Transform hTr = transform.Find("ComicStageCard/SituationDescTMP")
                             ?? transform.Find("CardObject/SituationDescTMP")
                             ?? transform.Find("SituationDescTMP")
                             ?? transform.Find("ComicStageCard/ScenarioDescTMP")
                             ?? transform.Find("CardObject/ScenarioDescTMP")
                             ?? transform.Find("HintTMP");
                if (hTr != null) hintTMP = hTr.GetComponent<TextMeshProUGUI>();
            }

            if (progressTMP == null) {
                Transform prTr = transform.Find("HeaderContainer/ProgressTMP")
                              ?? transform.Find("ProgressTMP")
                              ?? transform.Find("ProgressCountTMP")
                              ?? transform.Find("ExpressionCountTMP");
                if (prTr != null) {
                    progressTMP = prTr.GetComponent<TextMeshProUGUI>();
                } else {
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in tmps) {
                        string tn = t.name.ToLower();
                        if (tn.Contains("progress") || tn.Contains("expressioncount")) { progressTMP = t; break; }
                    }
                }
            }

            if (scoreTMP == null) {
                Transform scTr = transform.Find("HeaderContainer/ScoreTMP")
                              ?? transform.Find("ScoreTMP")
                              ?? transform.Find("CorrectlyFilledTMP");
                if (scTr != null) {
                    scoreTMP = scTr.GetComponent<TextMeshProUGUI>();
                } else {
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in tmps) {
                        string tn = t.name.ToLower();
                        if ((tn.Contains("score") || tn.Contains("correctlyfilled")) && !tn.Contains("result")) { scoreTMP = t; break; }
                    }
                }
            }

            if (feedbackTMP == null) {
                Transform fTr = transform.Find("FeedbackBanner/FeedbackTextTMP")
                             ?? transform.Find("FeedbackBanner/Text")
                             ?? transform.Find("FeedbackTextTMP")
                             ?? transform.Find("SpeechBubble/Text")
                             ?? transform.Find("FeedbackTMP");
                if (fTr != null) feedbackTMP = fTr.GetComponent<TextMeshProUGUI>();
            }

            if (inputField == null) {
                inputField = GetComponentInChildren<TMP_InputField>(true);
            }

            if (inputField != null && inputFieldBg == null) {
                inputFieldBg = inputField.GetComponent<Image>();
            }

            if (submitBtn == null) {
                Transform sBtnTr = transform.Find("SubmitButton") ?? transform.Find("SubmitBtn") ?? transform.Find("CheckButton");
                if (sBtnTr != null) submitBtn = sBtnTr.GetComponent<Button>();
                if (submitBtn == null) {
                    Button[] btns = GetComponentsInChildren<Button>(true);
                    foreach (var b in btns) {
                        string bn = b.name.ToLower();
                        if (bn.Contains("submit") || bn.Contains("check") || bn.Contains("enter")) { submitBtn = b; break; }
                    }
                }
            }

            if (messageCardObject == null) {
                Transform mc = transform.Find("ComicStageCard") ?? transform.Find("CardObject") ?? transform.Find("MessageCard") ?? transform.Find("DeskCard");
                if (mc != null) messageCardObject = mc.gameObject;
            }

            // Hide unused word bank rail & target badge if present
            Transform wRail = transform.Find("WordBankRail") ?? transform.Find("ChipsRail");
            if (wRail != null) wRail.gameObject.SetActive(false);

            Transform vBox = transform.Find("Validator") ?? transform.Find("Checklist");
            if (vBox != null) vBox.gameObject.SetActive(false);

            Transform badgeTr = transform.Find("ComicStageCard/TargetIdiomBadgeTMP") ?? transform.Find("CardObject/TargetIdiomBadgeTMP") ?? transform.Find("TargetIdiomBadgeTMP");
            if (badgeTr != null) badgeTr.gameObject.SetActive(false);

            if (replayAudioBtn == null) {
                Transform rTr = transform.Find("ReplayAudioBtn") ?? transform.Find("CardObject/ReplayAudioBtn") ?? transform.Find("ComicStageCard/ReplayAudioBtn");
                if (rTr != null) replayAudioBtn = rTr.GetComponent<Button>();
            }

            if (resultPanel == null) {
                Transform rp = transform.Find("ResultPanel") ?? transform.Find("CompletedPanel") ?? transform.Find("ResultsPanel");
                if (rp != null) resultPanel = rp.gameObject;
            }
            if (resultPanel != null) {
                Transform rSc = resultPanel.transform.Find("ResultScore") ?? resultPanel.transform.Find("ScoreTMP") ?? resultPanel.transform.Find("ResultScoreTMP");
                if (rSc != null) resultScoreTMP = rSc.GetComponent<TextMeshProUGUI>();

                Transform rSt = resultPanel.transform.Find("ResultStatus") ?? resultPanel.transform.Find("StatusTMP") ?? resultPanel.transform.Find("ResultStatusTMP");
                if (rSt != null) resultStatusTMP = rSt.GetComponent<TextMeshProUGUI>();

                Transform rBtn = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                if (rBtn != null) retryBtn = rBtn.GetComponent<Button>();

                Transform hBtn = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("NextButton");
                if (hBtn != null) returnHubBtn = hBtn.GetComponent<Button>();
            }
        }

        private void WireEventListeners() {
            if (submitBtn != null) {
                submitBtn.onClick.RemoveAllListeners();
                submitBtn.onClick.AddListener(OnSubmitClicked);
            }

            if (inputField != null) {
                inputField.onSubmit.RemoveAllListeners();
                inputField.onSubmit.AddListener((val) => OnSubmitClicked());
            }

            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(() => RestartLesson(true));
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnReturnToHub);
            }
        }

        public void RestartLesson(bool playAudio = true) {
            currentMessageIndex = 0;
            score = 0;
            attemptsOnCurrentMessage = 0;
            isCheckingAnswer = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            DisplayCurrentMessage(playAudio);
        }

        private void DisplayCurrentMessage(bool playVoiceover = false) {
            if (messages == null || currentMessageIndex < 0 || currentMessageIndex >= messages.Length) return;

            attemptsOnCurrentMessage = 0;
            isCheckingAnswer = false;

            CommonMistakes_WritingW02Message msg = messages[currentMessageIndex];

            if (categoryTitleTMP != null) {
                categoryTitleTMP.text = msg.categoryTitle;
            }

            if (hintTMP != null) {
                hintTMP.text = "Rewrite the message below with all planted errors mended:";
            }

            if (wobblyTextTMP != null) {
                wobblyTextTMP.text = $"\"{msg.wobblyOriginalText}\"";
            }

            if (progressTMP != null) {
                progressTMP.text = $"Message {currentMessageIndex + 1}/{messages.Length}";
            }

            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{messages.Length}";
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "Type the corrected message into the text box below.";
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

            if (playVoiceover) {
                PlayCurrentMessageAudio();
            }
        }

        private void PlayCurrentMessageAudio() {
            if (messages == null || currentMessageIndex < 0 || currentMessageIndex >= messages.Length) return;
            var msg = messages[currentMessageIndex];
            AudioClip clipToPlay = msg.wobblyAudio != null ? msg.wobblyAudio : msg.mendedAudio;
            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            }
        }

        public void OnSubmitClicked() {
            if (isCheckingAnswer) return;
            if (messages == null || currentMessageIndex < 0 || currentMessageIndex >= messages.Length) return;
            if (inputField == null) return;

            string typed = inputField.text;
            if (string.IsNullOrWhiteSpace(typed)) {
                if (feedbackTMP != null) feedbackTMP.text = "Please type the mended message before submitting.";
                return;
            }

            StartCoroutine(CheckMessageRoutine(typed.Trim()));
        }

        private IEnumerator CheckMessageRoutine(string userTyped) {
            isCheckingAnswer = true;
            CommonMistakes_WritingW02Message msg = messages[currentMessageIndex];

            bool isMatch = ValidateMessage(userTyped, msg);

            if (isMatch) {
                // Correct
                if (attemptsOnCurrentMessage == 0) score++;
                if (scoreTMP != null) scoreTMP.text = $"Score: {score}/{messages.Length}";

                if (inputFieldBg != null) inputFieldBg.color = correctColor;
                if (wobblyTextTMP != null) wobblyTextTMP.text = $"<color=#2E7D32><b>MENDED:</b>\n\"{msg.mendedModelText}\"</color>";
                if (feedbackTMP != null) feedbackTMP.text = "MENDED! All planted errors successfully corrected!";

                if (messageCardObject != null) {
                    messageCardObject.transform.DOPunchScale(Vector3.one * 0.1f, 0.35f, 5, 0.5f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (msg.mendedAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(msg.mendedAudio);
                    yield return new WaitForSeconds(msg.mendedAudio.length + 0.4f);
                } else {
                    yield return new WaitForSeconds(1.2f);
                }

                if (currentMessageIndex + 1 < messages.Length) {
                    currentMessageIndex++;
                    DisplayCurrentMessage(true);
                } else {
                    ShowFinalResults();
                }
            } else {
                // Wrong
                attemptsOnCurrentMessage++;

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (inputFieldBg != null) {
                    inputFieldBg.DOColor(wrongColor, 0.2f).OnComplete(() => {
                        if (inputFieldBg != null) inputFieldBg.DOColor(defaultInputBgColor, 0.3f);
                    });
                }

                if (messageCardObject != null) {
                    messageCardObject.transform.DOShakePosition(0.35f, new Vector3(8f, 0f, 0f), 10, 90f);
                }

                if (attemptsOnCurrentMessage == 1) {
                    if (wobblyTextTMP != null) wobblyTextTMP.text = $"<b>Check the planted errors:</b>\n{msg.errorExplanationHint}";
                    if (feedbackTMP != null) feedbackTMP.text = "Some errors were missed. Review the hint and retry!";
                    isCheckingAnswer = false;
                    if (inputField != null) inputField.ActivateInputField();
                } else {
                    // Exhausted retries -> advance
                    if (feedbackTMP != null) feedbackTMP.text = $"Model Correction:\n\"{msg.mendedModelText}\"";
                    if (wobblyTextTMP != null) wobblyTextTMP.text = $"<color=#1565C0>{msg.mendedModelText}</color>";

                    if (msg.mendedAudio != null && Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(msg.mendedAudio);
                        yield return new WaitForSeconds(msg.mendedAudio.length + 0.3f);
                    } else {
                        yield return new WaitForSeconds(1.5f);
                    }

                    if (currentMessageIndex + 1 < messages.Length) {
                        currentMessageIndex++;
                        DisplayCurrentMessage(true);
                    } else {
                        ShowFinalResults();
                    }
                }
            }
        }

        private bool ValidateMessage(string userTyped, CommonMistakes_WritingW02Message msg) {
            if (string.IsNullOrWhiteSpace(userTyped)) return false;

            string normUser = NormalizeText(userTyped);
            string normModel = NormalizeText(msg.mendedModelText);

            // Exact normalized match
            if (normUser.Equals(normModel, System.StringComparison.OrdinalIgnoreCase)) return true;

            // Check if all required corrected clauses are present in the text
            if (msg.requiredCorrections != null && msg.requiredCorrections.Length > 0) {
                bool allPresent = true;
                foreach (string req in msg.requiredCorrections) {
                    string normReq = NormalizeText(req);
                    if (!normUser.Contains(normReq)) {
                        allPresent = false;
                        break;
                    }
                }
                if (allPresent) return true;
            }

            return false;
        }

        private string NormalizeText(string s) {
            if (string.IsNullOrEmpty(s)) return "";
            string lower = s.Trim().ToLowerInvariant();

            // Normalize contractions
            lower = lower.Replace("i'm", "i am");
            lower = lower.Replace("don't", "do not");
            lower = lower.Replace("didn't", "did not");
            lower = lower.Replace("it's", "it is");
            lower = lower.Replace("anyone", "anybody");

            // Strip punctuation
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in lower) {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)) {
                    sb.Append(c);
                }
            }

            // Collapse spaces
            string[] words = sb.ToString().Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words);
        }

        private void ShowFinalResults() {
            if (resultPanel != null) resultPanel.SetActive(true);

            int total = (messages != null) ? messages.Length : 3;
            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {total}";
            }

            bool passed = score >= passScore;
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Great Job! Unit 10 Writing W02 Completed!" : "Keep practicing! Try again!";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(passed ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                NextButtonAnimation();
            }

            if (passed) {
                topic = Masters_Topic.Writing;
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(topic);
                }
            }
        }

        public void ReplayCurrentAudio() {
            PlayCurrentMessageAudio();
        }

        public void OnReturnToHub() {
            topic = Masters_Topic.Writing;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        public void InitMessagesIfEmpty() {
            if (messages != null && messages.Length > 0 && messages[0] != null && messages[0].mendedAudio != null) return;

            string audioDir = "Assets/Audio/2B/10_CommonMistakesWithPrepositions/Writing/";

            messages = new CommonMistakes_WritingW02Message[] {
                new CommonMistakes_WritingW02Message {
                    messageId = 1,
                    categoryTitle = "1) A NOTE TO A FRIEND (3 errors)",
                    wobblyOriginalText = "Have you been in London? My cousin lives there. The office is in the first floor. I am going to home now.",
                    mendedModelText = "Have you been to London? My cousin lives there. The office is on the first floor. I am going home now.",
                    requiredCorrections = new string[] { "been to london", "on the first floor", "going home" },
                    errorExplanationHint = "- <color=#E53935>in London</color> -> <b>to London</b>\n- <color=#E53935>in the first floor</color> -> <b>on the first floor</b>\n- <color=#E53935>going to home</color> -> <b>going home</b>",
#if UNITY_EDITOR
                    wobblyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w02_msg01_wobbly.mp3"),
                    mendedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w02_msg01_mended.mp3")
#endif
                },
                new CommonMistakes_WritingW02Message {
                    messageId = 2,
                    categoryTitle = "2) A MESSAGE ABOUT A PARTY (4 errors)",
                    wobblyOriginalText = "My birthday is on January. Everybody have problems with the date. Please, don't tell nobody. We have not money left for a big party.",
                    mendedModelText = "My birthday is in January. Everybody has problems with the date. Please, don't tell anybody. We have no money left for a big party.",
                    requiredCorrections = new string[] { "in january", "everybody has", "tell anybody", "no money left" },
                    errorExplanationHint = "- <color=#E53935>on January</color> -> <b>in January</b>\n- <color=#E53935>Everybody have</color> -> <b>Everybody has</b>\n- <color=#E53935>tell nobody</color> -> <b>tell anybody</b>\n- <color=#E53935>not money left</color> -> <b>no money left</b>",
#if UNITY_EDITOR
                    wobblyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w02_msg02_wobbly.mp3"),
                    mendedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w02_msg02_mended.mp3")
#endif
                },
                new CommonMistakes_WritingW02Message {
                    messageId = 3,
                    categoryTitle = "3) A REPORT ON A CLASSMATE (4 errors)",
                    wobblyOriginalText = "Joe did not drank water all day. He speaks English very good. Did you wanted to help him? Jim helped me carrying the box for him.",
                    mendedModelText = "Joe did not drink water all day. He speaks English very well. Did you want to help him? Jim helped me carry the box for him.",
                    requiredCorrections = new string[] { "did not drink", "very well", "did you want", "carry the box" },
                    errorExplanationHint = "- <color=#E53935>did not drank</color> -> <b>did not drink</b>\n- <color=#E53935>very good</color> -> <b>very well</b>\n- <color=#E53935>Did you wanted</color> -> <b>Did you want</b>\n- <color=#E53935>carrying the box</color> -> <b>carry the box</b>",
#if UNITY_EDITOR
                    wobblyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w02_msg03_wobbly.mp3"),
                    mendedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w02_msg03_mended.mp3")
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
