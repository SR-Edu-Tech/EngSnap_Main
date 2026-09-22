using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit5 {


    /// <summary>
    /// W01 Complete the Symptom Sentence
    /// Records desk cloze fill-in writing controller for Book 2B Unit 5 (Doctor Need Your Help).
    /// Types the missing word to complete verbatim symptom sentences and clinic lines.
    /// Success condition: Student types the correct word in at least 8 of 10 items.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
    public class DoctorWritingW01Item {
        public int itemId;
        public string promptText;           // e.g. "My nose is _____ . [symptom]"
        public string[] acceptedAnswers;    // e.g. ["runny"]
        public string firstLetterHint;      // e.g. "r..."
        public string completedSentence;    // e.g. "My nose is runny."
        public AudioClip readbackAudio;     // Audio voiceover
    }
    
        [Header("W01 10-12 Cloze Fill-in Items")]
        [SerializeField] private DoctorWritingW01Item[] items;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;
        [SerializeField] private TextMeshProUGUI hintTMP;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Image inputFieldBg;

        [Header("Patient Card / Desk Object")]
        [SerializeField] private GameObject deskCardObject;
        [SerializeField] private Button replayAudioBtn;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        [Header("Pass Threshold")]
        [SerializeField] private int passScore = 8;

        private int currentItemIndex = 0;
        private int correctCount = 0;
        private int attemptsOnCurrentItem = 0;
        private bool isCheckingAnswer = false;

        private Color defaultInputBgColor = Color.white;
        private Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        private Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Writing;
            base.Awake();

            AutoBindReferences();
            InitItemsIfEmpty();
            WireEventListeners();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Writing;
            AutoBindReferences();
            WireEventListeners();
            EnsureNextAndBackButtonWired();
            RestartLesson();
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
                returnHubBtn.onClick.AddListener(OnNextButtonClicked);
            }
        }

        public void InitItemsIfEmpty() {
            string audioDir = "Assets/Audio/2B/5_DoctorNeedYourHelp/Writing/";

            items = new DoctorWritingW01Item[] {
                new DoctorWritingW01Item {
                    itemId = 1,
                    promptText = "My nose is _____ . [symptom]",
                    acceptedAnswers = new string[] { "runny" },
                    firstLetterHint = "Hint: Starts with 'r...'",
                    completedSentence = "My nose is runny.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item1_runny.mp3")
#endif
                },
                new DoctorWritingW01Item {
                    itemId = 2,
                    promptText = "My eyes are _____ . [symptom]",
                    acceptedAnswers = new string[] { "watery" },
                    firstLetterHint = "Hint: Starts with 'w...'",
                    completedSentence = "My eyes are watery.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item2_watery.mp3")
#endif
                },
                new DoctorWritingW01Item {
                    itemId = 3,
                    promptText = "My throat is dry! I can't stop _____ . [symptom]",
                    acceptedAnswers = new string[] { "coughing", "cough" },
                    firstLetterHint = "Hint: Starts with 'c...'",
                    completedSentence = "My throat is dry! I can't stop coughing.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item3_coughing.mp3")
#endif
                },
                new DoctorWritingW01Item {
                    itemId = 4,
                    promptText = "I have a toothache! I think I have a _____ . [symptom]",
                    acceptedAnswers = new string[] { "cavity" },
                    firstLetterHint = "Hint: Starts with 'c...'",
                    completedSentence = "I have a toothache! I think I have a cavity.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item4_cavity.mp3")
#endif
                },
                new DoctorWritingW01Item {
                    itemId = 5,
                    promptText = "My chest feels _____ ! I can't breathe. [symptom]",
                    acceptedAnswers = new string[] { "tight" },
                    firstLetterHint = "Hint: Starts with 't...'",
                    completedSentence = "My chest feels tight! I can't breathe.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item5_tight.mp3")
#endif
                },
                new DoctorWritingW01Item {
                    itemId = 6,
                    promptText = "My stomach _____ . [symptom]",
                    acceptedAnswers = new string[] { "hurts", "aches", "ache", "hurt" },
                    firstLetterHint = "Hint: Starts with 'h...'",
                    completedSentence = "My stomach hurts.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item6_hurts.mp3")
#endif
                },
                new DoctorWritingW01Item {
                    itemId = 7,
                    promptText = "My knees keep _____ . [symptom]",
                    acceptedAnswers = new string[] { "locking", "lock" },
                    firstLetterHint = "Hint: Starts with 'l...'",
                    completedSentence = "My knees keep locking.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item7_locking.mp3")
#endif
                },
                new DoctorWritingW01Item {
                    itemId = 8,
                    promptText = "I _____ my ankle. [symptom]",
                    acceptedAnswers = new string[] { "twisted", "twist", "sprained", "hurt" },
                    firstLetterHint = "Hint: Starts with 't...'",
                    completedSentence = "I twisted my ankle.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item8_twisted.mp3")
#endif
                },
                new DoctorWritingW01Item {
                    itemId = 9,
                    promptText = "I had trouble in _____ . [at the doctor]",
                    acceptedAnswers = new string[] { "breathing", "breathe" },
                    firstLetterHint = "Hint: Starts with 'b...'",
                    completedSentence = "I had trouble in breathing.",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item9_breathing.mp3")
#endif
                },
                new DoctorWritingW01Item {
                    itemId = 10,
                    promptText = "Do you have any _____ that you know of? [at the doctor]",
                    acceptedAnswers = new string[] { "allergies", "allergy" },
                    firstLetterHint = "Hint: Starts with 'a...'",
                    completedSentence = "Do you have any allergies that you know of?",
#if UNITY_EDITOR
                    readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w01_item10_allergies.mp3")
#endif
                }
            };
        }

        public void RestartLesson() {
            currentItemIndex = 0;
            correctCount = 0;
            attemptsOnCurrentItem = 0;
            isCheckingAnswer = false;

            if (headerTMP != null) headerTMP.gameObject.SetActive(true);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (deskCardObject != null) deskCardObject.SetActive(true);

            UpdateScoreUI();
            ShowItem(0);
        }

        public void ShowItem(int index) {
            if (items == null || items.Length == 0) return;
            currentItemIndex = Mathf.Clamp(index, 0, items.Length - 1);
            attemptsOnCurrentItem = 0;
            isCheckingAnswer = false;

            DoctorWritingW01Item item = items[currentItemIndex];

            UpdateScoreUI();

            if (headerTMP != null) headerTMP.text = "RECORDS DESK 📋";
            if (titleTMP != null) {
                titleTMP.text = "Complete the Symptom Sentence";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Type the missing word to complete the symptom sentence.";

            if (promptTMP != null) promptTMP.text = item.promptText;

            if (feedbackTMP != null) feedbackTMP.text = "";
            if (hintTMP != null) hintTMP.gameObject.SetActive(false);

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

        private void UpdateScoreUI() {
            if (progressTMP != null && items != null) {
                progressTMP.text = $"{correctCount}/{items.Length}";
            }
        }

        public void OnSubmitClicked() {
            if (isCheckingAnswer) return;
            if (items == null || currentItemIndex >= items.Length) return;

            string typed = inputField != null ? inputField.text.Trim().ToLower() : "";
            if (string.IsNullOrEmpty(typed)) return;

            DoctorWritingW01Item item = items[currentItemIndex];
            bool isCorrect = false;

            foreach (var accepted in item.acceptedAnswers) {
                if (string.Equals(typed, accepted.Trim().ToLower())) {
                    isCorrect = true;
                    break;
                }
            }

            StartCoroutine(HandleAnswerEvaluation(isCorrect));
        }

        private IEnumerator HandleAnswerEvaluation(bool isCorrect) {
            isCheckingAnswer = true;
            attemptsOnCurrentItem++;

            DoctorWritingW01Item item = items[currentItemIndex];

            if (isCorrect) {
                if (attemptsOnCurrentItem == 1) {
                    correctCount++;
                    UpdateScoreUI();
                }

                if (inputFieldBg != null) inputFieldBg.color = correctColor;
                if (feedbackTMP != null) {
                    feedbackTMP.text = "<color=#33D866><b>✓ Correct!</b></color>";
                }

                if (promptTMP != null) {
                    promptTMP.text = $"<color=#FFD80D><b>{item.completedSentence}</b></color>";
                }

                if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                yield return new WaitForSeconds(2.0f);

                if (currentItemIndex < items.Length - 1) {
                    ShowItem(currentItemIndex + 1);
                } else {
                    ShowResults();
                }
            } else {
                if (inputFieldBg != null) inputFieldBg.color = wrongColor;

                if (inputField != null) {
                    inputField.transform.DOShakePosition(0.4f, 10f, 14, 90, false, true);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (attemptsOnCurrentItem == 1) {
                    // Show retry hint
                    if (feedbackTMP != null) feedbackTMP.text = "<color=#FF6666>Not quite, try again!</color>";
                    if (hintTMP != null) {
                        hintTMP.gameObject.SetActive(true);
                        hintTMP.text = item.firstLetterHint;
                    }
                    if (inputField != null) {
                        inputField.text = "";
                        inputField.ActivateInputField();
                    }
                    isCheckingAnswer = false;
                } else {
                    // Second failure: show answer and advance
                    if (feedbackTMP != null) {
                        feedbackTMP.text = $"<color=#FFAA33>Answer: {item.acceptedAnswers[0]}</color>";
                    }
                    if (promptTMP != null) {
                        promptTMP.text = $"<color=#FFD80D><b>{item.completedSentence}</b></color>";
                    }

                    if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                    }

                    yield return new WaitForSeconds(2.2f);

                    if (currentItemIndex < items.Length - 1) {
                        ShowItem(currentItemIndex + 1);
                    } else {
                        ShowResults();
                    }
                }
            }
        }

        public void ReplayCurrentAudio() {
            if (items == null || currentItemIndex >= items.Length) return;
            AudioClip clip = items[currentItemIndex].readbackAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        private void ShowResults() {
            if (deskCardObject != null) deskCardObject.SetActive(false);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }

            bool passed = (correctCount >= passScore);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "RECORDS COMPLETE! 📋" : "KEEP PRACTICING!";
                    resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
                }

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You typed {correctCount} of {items.Length} symptom words correctly!";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed ? "Success! All patient symptom sentences have been properly recorded." : "You need at least 8/10 correct to complete this writing topic.";
                }

                if (returnHubBtn != null) {
                    returnHubBtn.gameObject.SetActive(passed);
                }

                if (retryBtn != null) {
                    retryBtn.gameObject.SetActive(!passed || correctCount < items.Length);
                }
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(passed);
            }

            if (passed && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Writing;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        private void AutoBindReferences() {
            if (titleTMP == null) {
                Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/LessonTitle");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (progressTMP == null) {
                Transform t = transform.Find("progression count") ?? transform.Find("ProgressTMP") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP");
                if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (promptTMP == null) {
                Transform t = transform.Find("questions text") ?? transform.Find("DeskCard/PromptTMP") ?? transform.Find("NoticeboardCard/PromptTMP");
                if (t != null) promptTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (hintTMP == null) {
                Transform t = transform.Find("Hint text") ?? transform.Find("hint/Hint text");
                if (t != null) hintTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (submitButton == null) {
                Transform t = transform.Find("Check") ?? transform.Find("CheckButton") ?? transform.Find("SubmitButton");
                if (t != null) submitButton = t.GetComponent<Button>();
            }
            if (inputField == null) {
                inputField = GetComponentInChildren<TMP_InputField>(true);
            }
            if (inputField != null && inputFieldBg == null) {
                inputFieldBg = inputField.GetComponent<Image>();
            }
            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }
            if (resultTitleTMP == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("ResultTitle") ?? resultPanel.transform.Find("Title");
                if (t != null) resultTitleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (resultScoreTMP == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("ResultScore") ?? resultPanel.transform.Find("Score");
                if (t != null) resultScoreTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (resultStatusTMP == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("ResultStatus") ?? resultPanel.transform.Find("Status");
                if (t != null) resultStatusTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (retryBtn == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                if (t != null) retryBtn = t.GetComponent<Button>();
            }
            if (returnHubBtn == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("ReturnHubBtn") ?? resultPanel.transform.Find("NextButton");
                if (t != null) returnHubBtn = t.GetComponent<Button>();
            }
        }
    }
}
