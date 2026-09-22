using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {

    

    /// <summary>
    /// Unit 10: Common Mistakes with Prepositions — Writing Lesson One
    /// (W01 Type the Repair)
    /// Student types the mended version of the faulty sentence into the repair slip.
    /// Case-insensitive, punctuation forgiving.
    /// Pass condition: at least 8 of 10 items (or 10 of 13).
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
    public class CommonMistakes_WritingW01Item {
        public int itemId;
        public string faultySentence;        // e.g. "It depends from you."
        public string[] acceptedAnswers;     // e.g. ["It depends on you.", "it depends on you"]
        public string highlightedFaultyHint; // e.g. "It depends <color=#E53935><u>from</u></color> you. (Hint: 'from' ➔ 'on')"
        public string completedSentence;     // e.g. "It depends on you."
        public AudioClip readbackAudio;      // Voiceover
    }
    
        [Header("13 Repair Slip Items")]
        [SerializeField] private CommonMistakes_WritingW01Item[] items;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;
        [SerializeField] private TextMeshProUGUI hintTMP;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Image inputFieldBg;

        [Header("Card / Desk Object")]
        [SerializeField] private GameObject repairSlipCard;
        [SerializeField] private Button replayAudioBtn;

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
        [SerializeField] private int passScore = 8;

        private int currentItemIndex = 0;
        private int score = 0;
        private int attemptsOnCurrentItem = 0;
        private bool isCheckingAnswer = false;

        private readonly Color defaultInputBgColor = Color.white;
        private readonly Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        private readonly Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Writing;
            base.Awake();

            AutoBindReferences();
            InitItemsIfEmpty();
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
            RestartLesson();

            if (introAudio == null) {
#if UNITY_EDITOR
                introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Writing/cm_w01_intro.mp3");
#endif
            }

            if (introAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
            }
        }

        private void EnsureHeaderAndTitle() {
            if (headerTMP == null) {
                Transform hTr = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("Branch") ?? transform.Find("UnitHeading");
                if (hTr != null) headerTMP = hTr.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "COMMON MISTAKES WITH PREPOSITIONS 🔍";
            }

            if (titleTMP == null) {
                Transform tTr = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
                if (tTr != null) titleTMP = tTr.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "W01 Type the Repair";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (subtitleTMP == null) {
                Transform sTr = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Subtitle") ?? transform.Find("Instruction");
                if (sTr != null) subtitleTMP = sTr.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleTMP != null) {
                subtitleTMP.text = "Type the corrected sentence into the repair slip.";
            }
        }

        private void AutoBindReferences() {
            EnsureHeaderAndTitle();

            if (promptTMP == null) {
                Transform pTr = transform.Find("RepairSlip/PromptTMP")
                             ?? transform.Find("DeskCard/PromptTMP")
                             ?? transform.Find("PromptTMP")
                             ?? transform.Find("QuestionPanel/QuestionTMP")
                             ?? transform.Find("SentenceContainer/SentenceTMP")
                             ?? transform.Find("WindowSignContainer/WindowSignTMP");
                if (pTr != null) promptTMP = pTr.GetComponent<TextMeshProUGUI>();
            }

            if (progressTMP == null) {
                Transform prTr = transform.Find("HeaderContainer/ProgressTMP")
                              ?? transform.Find("ProgressCountTMP")
                              ?? transform.Find("ExpressionCountTMP")
                              ?? transform.Find("ProgressTMP");
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
                Transform fTr = transform.Find("SpeechBubble/Text")
                             ?? transform.Find("SpeechBubbleTMP")
                             ?? transform.Find("FeedbackTMP");
                if (fTr != null) feedbackTMP = fTr.GetComponent<TextMeshProUGUI>();
            }

            if (hintTMP == null) {
                Transform hTr = transform.Find("HintTMP") ?? transform.Find("HintContainer/HintTMP");
                if (hTr != null) hintTMP = hTr.GetComponent<TextMeshProUGUI>();
            }

            if (inputField == null) {
                inputField = GetComponentInChildren<TMP_InputField>(true);
            }

            if (inputField != null && inputFieldBg == null) {
                inputFieldBg = inputField.GetComponent<Image>();
            }

            if (submitButton == null) {
                Transform sBtnTr = transform.Find("SubmitButton") ?? transform.Find("SubmitBtn") ?? transform.Find("CheckButton");
                if (sBtnTr != null) submitButton = sBtnTr.GetComponent<Button>();
                if (submitButton == null) {
                    Button[] btns = GetComponentsInChildren<Button>(true);
                    foreach (var b in btns) {
                        string bn = b.name.ToLower();
                        if (bn.Contains("submit") || bn.Contains("check") || bn.Contains("enter")) { submitButton = b; break; }
                    }
                }
            }

            if (repairSlipCard == null) {
                Transform rc = transform.Find("RepairSlip") ?? transform.Find("DeskCard") ?? transform.Find("CardObject");
                if (rc != null) repairSlipCard = rc.gameObject;
            }

            if (replayAudioBtn == null) {
                Transform rTr = transform.Find("Controls/ReplayButton") ?? transform.Find("AudioControls/AudioButton") ?? transform.Find("ReplayAudioBtn");
                if (rTr != null) replayAudioBtn = rTr.GetComponent<Button>();
            }

            if (resultPanel == null) {
                Transform rp = transform.Find("ResultPanel") ?? transform.Find("CompletedPanel") ?? transform.Find("ResultsPanel");
                if (rp != null) resultPanel = rp.gameObject;
            }
            if (resultPanel != null) {
                Transform rSc = resultPanel.transform.Find("ScoreTMP") ?? resultPanel.transform.Find("ResultScoreTMP");
                if (rSc != null) resultScoreTMP = rSc.GetComponent<TextMeshProUGUI>();

                Transform rSt = resultPanel.transform.Find("StatusTMP") ?? resultPanel.transform.Find("ResultStatusTMP");
                if (rSt != null) resultStatusTMP = rSt.GetComponent<TextMeshProUGUI>();

                Transform rBtn = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                if (rBtn != null) retryBtn = rBtn.GetComponent<Button>();

                Transform hBtn = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("NextButton");
                if (hBtn != null) returnHubBtn = hBtn.GetComponent<Button>();
            }
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
                replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartLesson);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnReturnToHub);
            }
        }

        public void RestartLesson() {
            currentItemIndex = 0;
            score = 0;
            attemptsOnCurrentItem = 0;
            isCheckingAnswer = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            DisplayCurrentItem();
        }

        private void DisplayCurrentItem() {
            if (items == null || currentItemIndex < 0 || currentItemIndex >= items.Length) return;

            attemptsOnCurrentItem = 0;
            isCheckingAnswer = false;

            CommonMistakes_WritingW01Item item = items[currentItemIndex];

            if (promptTMP != null) {
                promptTMP.text = $"❌ {item.faultySentence}";
            }

            if (progressTMP != null) {
                progressTMP.text = $"Sentence {currentItemIndex + 1}/{items.Length}";
            }

            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{items.Length}";
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "Type the corrected sentence into the repair slip below.";
            }

            if (hintTMP != null) {
                hintTMP.gameObject.SetActive(false);
            }

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

        public void OnSubmitClicked() {
            if (isCheckingAnswer) return;
            if (items == null || currentItemIndex < 0 || currentItemIndex >= items.Length) return;
            if (inputField == null) return;

            string typed = inputField.text;
            if (string.IsNullOrWhiteSpace(typed)) {
                if (feedbackTMP != null) feedbackTMP.text = "Please type the corrected sentence before submitting.";
                return;
            }

            StartCoroutine(CheckAnswerRoutine(typed.Trim()));
        }

        private IEnumerator CheckAnswerRoutine(string userTyped) {
            isCheckingAnswer = true;
            CommonMistakes_WritingW01Item item = items[currentItemIndex];

            bool isMatch = ValidateAnswer(userTyped, item.acceptedAnswers);

            if (isMatch) {
                // Correct
                if (attemptsOnCurrentItem == 0) score++;
                if (scoreTMP != null) scoreTMP.text = $"Score: {score}/{items.Length}";

                if (inputFieldBg != null) inputFieldBg.color = correctColor;
                if (promptTMP != null) promptTMP.text = $"<color=#2E7D32><b>{item.completedSentence}</b></color>";
                if (feedbackTMP != null) feedbackTMP.text = "MENDED! Excellent repair!";

                if (repairSlipCard != null) {
                    repairSlipCard.transform.DOPunchScale(Vector3.one * 0.1f, 0.35f, 5, 0.5f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                    yield return new WaitForSeconds(item.readbackAudio.length + 0.4f);
                } else {
                    yield return new WaitForSeconds(1.0f);
                }

                if (currentItemIndex + 1 < items.Length) {
                    currentItemIndex++;
                    DisplayCurrentItem();
                } else {
                    ShowFinalResults();
                }
            } else {
                // Wrong
                attemptsOnCurrentItem++;

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (inputFieldBg != null) {
                    inputFieldBg.DOColor(wrongColor, 0.2f).OnComplete(() => {
                        if (inputFieldBg != null) inputFieldBg.DOColor(defaultInputBgColor, 0.3f);
                    });
                }

                if (repairSlipCard != null) {
                    repairSlipCard.transform.DOShakePosition(0.35f, new Vector3(8f, 0f, 0f), 10, 90f);
                }

                if (attemptsOnCurrentItem == 1) {
                    if (promptTMP != null) promptTMP.text = item.highlightedFaultyHint;
                    if (feedbackTMP != null) feedbackTMP.text = "Not quite! Check the highlighted loose word and try again.";
                    if (hintTMP != null) {
                        hintTMP.text = $"Target: \"{item.completedSentence}\"";
                        hintTMP.gameObject.SetActive(true);
                    }
                    isCheckingAnswer = false;
                    if (inputField != null) inputField.ActivateInputField();
                } else {
                    // Exhausted retries -> advance
                    if (feedbackTMP != null) feedbackTMP.text = $"Correction: \"{item.completedSentence}\"";
                    if (promptTMP != null) promptTMP.text = $"<color=#1565C0>{item.completedSentence}</color>";

                    if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                        yield return new WaitForSeconds(item.readbackAudio.length + 0.3f);
                    } else {
                        yield return new WaitForSeconds(1.2f);
                    }

                    if (currentItemIndex + 1 < items.Length) {
                        currentItemIndex++;
                        DisplayCurrentItem();
                    } else {
                        ShowFinalResults();
                    }
                }
            }
        }

        private bool ValidateAnswer(string userTyped, string[] accepted) {
            if (string.IsNullOrWhiteSpace(userTyped)) return false;
            string normUser = NormalizeText(userTyped);

            if (accepted == null || accepted.Length == 0) return false;

            foreach (string ans in accepted) {
                if (string.IsNullOrWhiteSpace(ans)) continue;
                string normAns = NormalizeText(ans);
                if (normUser.Equals(normAns, System.StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private string NormalizeText(string s) {
            if (string.IsNullOrEmpty(s)) return "";
            string lower = s.Trim().ToLowerInvariant();

            // Replace contractions to standard variants
            lower = lower.Replace("i'm", "i am");
            lower = lower.Replace("don't", "do not");
            lower = lower.Replace("didn't", "did not");
            lower = lower.Replace("she's", "she has");
            lower = lower.Replace("it's", "it is");

            // Strip common punctuation
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in lower) {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)) {
                    sb.Append(c);
                }
            }

            // Collapse multiple whitespaces
            string[] words = sb.ToString().Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words);
        }

        private void ShowFinalResults() {
            if (resultPanel != null) resultPanel.SetActive(true);

            int total = (items != null) ? items.Length : 13;
            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {total}";
            }

            bool passed = score >= passScore;
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Great Job! Writing W01 Completed!" : "Keep practicing! Try again!";
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
            if (items != null && currentItemIndex >= 0 && currentItemIndex < items.Length) {
                var it = items[currentItemIndex];
                if (it.readbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(it.readbackAudio);
                }
            }
        }

        public void OnReturnToHub() {
            topic = Masters_Topic.Writing;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        public void InitItemsIfEmpty() {
            if (items != null && items.Length > 0 && items[0] != null && items[0].readbackAudio != null) return;

            string audioDir = "Assets/Audio/2B/10_CommonMistakesWithPrepositions/Writing/";

            items = new CommonMistakes_WritingW01Item[] {
                new CommonMistakes_WritingW01Item {
                    itemId = 1,
                    faultySentence = "It depends from you.",
                    acceptedAnswers = new string[] { "It depends on you.", "it depends on you" },
                    highlightedFaultyHint = "It depends <color=#E53935><u>from</u></color> you. (Hint: from -> on)",
                    completedSentence = "It depends on you.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item01.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 2,
                    faultySentence = "Who is in the phone?",
                    acceptedAnswers = new string[] { "Who is on the phone?", "who is on the phone", "Who's on the phone?" },
                    highlightedFaultyHint = "Who is <color=#E53935><u>in</u></color> the phone? (Hint: in -> on)",
                    completedSentence = "Who is on the phone?",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item02.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 3,
                    faultySentence = "I don't use a watch",
                    acceptedAnswers = new string[] { "I don't wear a watch.", "I do not wear a watch.", "i don't wear a watch" },
                    highlightedFaultyHint = "I don't <color=#E53935><u>use</u></color> a watch. (Hint: use -> wear)",
                    completedSentence = "I don't wear a watch.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item03.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 4,
                    faultySentence = "Leave me in peace!",
                    acceptedAnswers = new string[] { "Leave me alone!", "leave me alone" },
                    highlightedFaultyHint = "Leave me <color=#E53935><u>in peace</u></color>! (Hint: in peace -> alone)",
                    completedSentence = "Leave me alone!",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item04.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 5,
                    faultySentence = "I did it by my own",
                    acceptedAnswers = new string[] { "I did it on my own.", "I did it by myself.", "i did it on my own", "i did it by myself" },
                    highlightedFaultyHint = "I did it <color=#E53935><u>by my own</u></color>. (Hint: on my own / by myself)",
                    completedSentence = "I did it on my own.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item05.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 6,
                    faultySentence = "I like more July than May",
                    acceptedAnswers = new string[] { "I like July more than May.", "i like july more than may" },
                    highlightedFaultyHint = "I like <color=#E53935><u>more July than May</u></color>. (Hint: I like July more than May)",
                    completedSentence = "I like July more than May.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item06.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 7,
                    faultySentence = "How much money do they win?",
                    acceptedAnswers = new string[] { "How much money do they make?", "how much money do they make" },
                    highlightedFaultyHint = "How much money do they <color=#E53935><u>win</u></color>? (Hint: win -> make)",
                    completedSentence = "How much money do they make?",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item07.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 8,
                    faultySentence = "Jim helped me carrying the box",
                    acceptedAnswers = new string[] { "Jim helped me carry the box.", "jim helped me carry the box" },
                    highlightedFaultyHint = "Jim helped me <color=#E53935><u>carrying</u></color> the box. (Hint: carrying -> carry)",
                    completedSentence = "Jim helped me carry the box.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item08.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 9,
                    faultySentence = "He speaks English very good.",
                    acceptedAnswers = new string[] { "He speaks English very well.", "he speaks english very well" },
                    highlightedFaultyHint = "He speaks English very <color=#E53935><u>good</u></color>. (Hint: good -> well)",
                    completedSentence = "He speaks English very well.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item09.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 10,
                    faultySentence = "Yes, I like very much.",
                    acceptedAnswers = new string[] { "Yes, I like it very much.", "yes i like it very much", "Yes I like it very much." },
                    highlightedFaultyHint = "Yes, I like <color=#E53935><u>very much</u></color>. (Hint: add 'it')",
                    completedSentence = "Yes, I like it very much.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item10.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 11,
                    faultySentence = "We have not money left.",
                    acceptedAnswers = new string[] { "We have no money left.", "we have no money left" },
                    highlightedFaultyHint = "We have <color=#E53935><u>not money</u></color> left. (Hint: not -> no)",
                    completedSentence = "We have no money left.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item11.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 12,
                    faultySentence = "She has left five years ago.",
                    acceptedAnswers = new string[] { "She left five years ago.", "she left five years ago" },
                    highlightedFaultyHint = "She <color=#E53935><u>has left</u></color> five years ago. (Hint: has left -> left)",
                    completedSentence = "She left five years ago.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item12.mp3")
#endif
                },
                new CommonMistakes_WritingW01Item {
                    itemId = 13,
                    faultySentence = "I have 26 years",
                    acceptedAnswers = new string[] { "I'm 26 years old.", "I am 26 years old.", "i'm 26 years old", "i am 26 years old" },
                    highlightedFaultyHint = "I <color=#E53935><u>have 26 years</u></color>. (Hint: I'm 26 years old)",
                    completedSentence = "I'm 26 years old.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_w01_item13.mp3")
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
