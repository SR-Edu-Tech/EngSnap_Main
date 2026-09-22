using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit5 {

    /// <summary>
    /// W02 Fill In the Patient Card
    /// Records desk patient card writing controller for Book 2B Unit 5 (Doctor Need Your Help).
    /// Writes 3 health cards: SYMPTOM CARD, APPOINTMENT CARD, and NOTE TO MOM.
    /// Word-bank rail chips can be tapped to append phrases directly into the text field.
    /// Success condition: Student writes a valid answer for at least 2 of 3 cards (one retry each).
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class DoctorWritingW02PatientCard {
        public int cardId;
        public string cardTitle;              // e.g. "Card 1: SYMPTOM CARD"
        public string scenarioDescription;    // e.g. "You are seeing the doctor this afternoon. Write TWO symptoms and say when they started."
        public string promptGuide;            // e.g. "Describe 2 symptoms + when it started:"
        public string modelAnswer;            // e.g. "My throat is dry! I can't stop coughing. My head hurts! It started last night."
        public string[] wordBankChips;        // quick-insert chips
        public string[] requiredKeywords;     // Keywords for validation
        public AudioClip cardAudio;
    }
    
        [Header("W02 3 Patient Card Scenarios")]
        [SerializeField] private DoctorWritingW02PatientCard[] cards;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;

        [Header("Patient Card Stage")]
        [SerializeField] private GameObject patientCardObject;
        [SerializeField] private TextMeshProUGUI cardTitleTMP;
        [SerializeField] private TextMeshProUGUI scenarioDescTMP;
        [SerializeField] private TextMeshProUGUI promptGuideTMP;
        [SerializeField] private Button replayAudioBtn;

        [Header("Word Bank Rail")]
        [SerializeField] private Button[] wordBankChipButtons;
        [SerializeField] private TextMeshProUGUI[] wordBankChipTexts;

        [Header("Input Field & Submit Button")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button submitBtn;
        [SerializeField] private Image inputFieldBg;

        [Header("Feedback / Explanation Banner")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private TextMeshProUGUI feedbackTextTMP;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        [Header("Audio References")]
        [SerializeField] private AudioClip introAudio;
        [SerializeField] private AudioClip recapAudio;
        [SerializeField] private AudioClip sfxCorrect;
        [SerializeField] private AudioClip sfxWrong;

        private int currentCardIndex = 0;
        private int score = 0;
        private int attemptsOnCurrentCard = 0;
        private bool isCheckingAnswer = false;
        private bool isIntroPhase = true;

        private Color defaultInputBgColor = Color.white;
        private Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        private Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Writing;
            base.Awake();

            AutoBindReferences();
            InitCardsIfEmpty();
            WireEventListeners();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Writing;
            AutoBindReferences();
            WireEventListeners();
            EnsureNextAndBackButtonWired();

            if (cards == null || cards.Length == 0) {
                InitCardsIfEmpty();
            }

            SetControlsInteractable(false);
            StartCoroutine(BeginLessonRoutine());
        }

        private IEnumerator BeginLessonRoutine() {
            isIntroPhase = true;
            if (headerTMP != null) headerTMP.text = "RECORDS DESK 📋";
            if (titleTMP != null) {
                titleTMP.text = "Fill In the Patient Card";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Write the required lines using the word-bank chips.";
            if (progressTMP != null) progressTMP.text = "1/3";
            if (scoreTMP != null) scoreTMP.text = "Score: 0/3";

            if (cards != null && cards.Length > 0) {
                if (cardTitleTMP != null) cardTitleTMP.text = cards[0].cardTitle;
                if (scenarioDescTMP != null) scenarioDescTMP.text = cards[0].scenarioDescription;
                if (promptGuideTMP != null) promptGuideTMP.text = cards[0].promptGuide;
            }

            AudioClip clipToPlay = introAudio != null ? introAudio : narratorSpeech;
            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                yield return new WaitForSeconds(clipToPlay.length + 0.3f);
            } else {
                yield return new WaitForSeconds(1.0f);
            }

            isIntroPhase = false;
            RestartLesson();
        }

        private void SetControlsInteractable(bool interactable) {
            if (inputField != null) inputField.interactable = interactable;
            if (submitBtn != null) submitBtn.interactable = interactable;
            if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;

            if (wordBankChipButtons != null) {
                for (int i = 0; i < wordBankChipButtons.Length; i++) {
                    if (wordBankChipButtons[i] != null) {
                        wordBankChipButtons[i].interactable = interactable;
                    }
                }
            }
        }

        private void WireEventListeners() {
            if (submitBtn != null) {
                submitBtn.onClick.RemoveAllListeners();
                submitBtn.onClick.AddListener(OnSubmitClicked);
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

            if (wordBankChipButtons != null) {
                for (int i = 0; i < wordBankChipButtons.Length; i++) {
                    int idx = i;
                    if (wordBankChipButtons[i] != null) {
                        wordBankChipButtons[i].onClick.RemoveAllListeners();
                        wordBankChipButtons[i].onClick.AddListener(() => OnWordBankChipClicked(idx));
                    }
                }
            }
        }

        public void InitCardsIfEmpty() {
            string audioDir = "Assets/Audio/2B/5_DoctorNeedYourHelp/Writing/";

            cards = new DoctorWritingW02PatientCard[] {
                new DoctorWritingW02PatientCard {
                    cardId = 1,
                    cardTitle = "CARD 1: SYMPTOM CARD",
                    scenarioDescription = "You are seeing the doctor this afternoon. Write TWO symptoms and say when they started.",
                    promptGuide = "Describe 2 symptoms + when it started:",
                    modelAnswer = "My throat is dry! I can't stop coughing. My head hurts! It started last night.",
                    wordBankChips = new string[] {
                        "My throat is dry!",
                        "I can't stop coughing.",
                        "My head hurts!",
                        "It started last night."
                    },
                    requiredKeywords = new string[] { "throat", "cough", "head", "hurt", "nose", "runny", "pain", "start", "night", "morning", "yesterday" },
#if UNITY_EDITOR
                    cardAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w02_card1_symptom.mp3")
#endif
                },
                new DoctorWritingW02PatientCard {
                    cardId = 2,
                    cardTitle = "CARD 2: APPOINTMENT CARD",
                    scenarioDescription = "Write what you would say on the phone to book a doctor for a family member: the request, who it is for, whether it is urgent, and agreed time.",
                    promptGuide = "Request + who + urgency + time:",
                    modelAnswer = "I'd like to take an appointment for my mom. It is urgent. 1:30 this afternoon will be fine.",
                    wordBankChips = new string[] {
                        "take an appointment",
                        "for my mom",
                        "It is urgent.",
                        "1:30 this afternoon"
                    },
                    requiredKeywords = new string[] { "appointment", "doctor", "consult", "mom", "mother", "urgent", "1:30", "afternoon", "time", "fine" },
#if UNITY_EDITOR
                    cardAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w02_card2_appointment.mp3")
#endif
                },
                new DoctorWritingW02PatientCard {
                    cardId = 3,
                    cardTitle = "CARD 3: NOTE TO MOM",
                    scenarioDescription = "You woke up unwell after staying up too late. Write how you feel, why, and what you will do differently.",
                    promptGuide = "How you feel + why + what to do:",
                    modelAnswer = "My head is spinning. I think I am running a temperature too. I stayed up late. I'm sorry. I will try to go to bed early.",
                    wordBankChips = new string[] {
                        "My head is spinning.",
                        "running a temperature",
                        "I stayed up late.",
                        "go to bed early."
                    },
                    requiredKeywords = new string[] { "head", "spin", "temperature", "fever", "late", "sorry", "bed", "sleep", "early" },
#if UNITY_EDITOR
                    cardAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w02_card3_notetomom.mp3")
#endif
                }
            };

#if UNITY_EDITOR
            introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w02_intro.mp3");
            recapAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w02_recap.mp3");
            sfxCorrect = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w02_correct.mp3");
            sfxWrong = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_w02_wrong.mp3");
#endif
        }

        public void RestartLesson() {
            currentCardIndex = 0;
            score = 0;
            attemptsOnCurrentCard = 0;
            isCheckingAnswer = false;

            if (headerTMP != null) headerTMP.gameObject.SetActive(true);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (patientCardObject != null) patientCardObject.SetActive(true);

            Transform rail = transform.Find("WordBankRail") ?? transform.Find("WordBankContainer") ?? transform.Find("OptionsContainer");
            if (rail != null) rail.gameObject.SetActive(true);

            UpdateScoreUI();
            ShowCard(0);
        }

        public void ShowCard(int index) {
            if (cards == null || cards.Length == 0) return;
            currentCardIndex = Mathf.Clamp(index, 0, cards.Length - 1);
            attemptsOnCurrentCard = 0;
            isCheckingAnswer = false;

            DoctorWritingW02PatientCard card = cards[currentCardIndex];

            UpdateScoreUI();

            if (headerTMP != null) headerTMP.text = "RECORDS DESK 📋";
            if (titleTMP != null) {
                titleTMP.text = "Fill In the Patient Card";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Write the required lines using the word-bank chips.";

            if (cardTitleTMP != null) cardTitleTMP.text = card.cardTitle;
            if (scenarioDescTMP != null) scenarioDescTMP.text = card.scenarioDescription;
            if (promptGuideTMP != null) promptGuideTMP.text = card.promptGuide;

            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (feedbackTextTMP != null) feedbackTextTMP.text = "";

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

            // Populate Word Bank Chips
            if (wordBankChipButtons != null && card.wordBankChips != null) {
                for (int i = 0; i < wordBankChipButtons.Length; i++) {
                    if (wordBankChipButtons[i] == null) continue;
                    if (i < card.wordBankChips.Length) {
                        wordBankChipButtons[i].gameObject.SetActive(true);
                        wordBankChipButtons[i].interactable = true;
                        if (wordBankChipTexts != null && i < wordBankChipTexts.Length && wordBankChipTexts[i] != null) {
                            wordBankChipTexts[i].text = card.wordBankChips[i];
                        }
                    } else {
                        wordBankChipButtons[i].gameObject.SetActive(false);
                    }
                }
            }

            SetControlsInteractable(true);
        }

        private void UpdateScoreUI() {
            if (progressTMP != null && cards != null) {
                progressTMP.text = $"{currentCardIndex + 1}/{cards.Length}";
            }
            if (scoreTMP != null && cards != null) {
                scoreTMP.text = $"Score: {score}/{cards.Length}";
            }
        }

        public void OnWordBankChipClicked(int chipIndex) {
            if (isIntroPhase || isCheckingAnswer) return;
            if (cards == null || currentCardIndex >= cards.Length) return;
            DoctorWritingW02PatientCard card = cards[currentCardIndex];
            if (card.wordBankChips == null || chipIndex >= card.wordBankChips.Length) return;

            string phrase = card.wordBankChips[chipIndex];
            if (inputField != null) {
                string current = inputField.text.Trim();
                if (string.IsNullOrEmpty(current)) {
                    inputField.text = phrase;
                } else {
                    inputField.text = current + " " + phrase;
                }
                inputField.caretPosition = inputField.text.Length;
                inputField.ActivateInputField();

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                }
            }
        }

        public void OnSubmitClicked() {
            if (isIntroPhase || isCheckingAnswer) return;
            if (cards == null || currentCardIndex >= cards.Length) return;

            string typed = inputField != null ? inputField.text.Trim().ToLower() : "";
            if (string.IsNullOrEmpty(typed) || typed.Length < 6) {
                if (feedbackBanner != null) feedbackBanner.SetActive(true);
                if (feedbackTextTMP != null) feedbackTextTMP.text = "<color=#FFAA33>Please write a sentence or tap word bank chips to complete the card.</color>";
                return;
            }

            DoctorWritingW02PatientCard card = cards[currentCardIndex];
            bool isValid = ValidateAnswer(typed, card);

            StartCoroutine(HandleAnswerEvaluation(isValid));
        }

        private bool ValidateAnswer(string typed, DoctorWritingW02PatientCard card) {
            if (typed.Length >= 20) return true; // Generous length for student writing

            int matchCount = 0;
            if (card.requiredKeywords != null) {
                foreach (var kw in card.requiredKeywords) {
                    if (typed.Contains(kw.ToLower())) matchCount++;
                }
            }
            return matchCount >= 2;
        }

        private IEnumerator HandleAnswerEvaluation(bool isValid) {
            isCheckingAnswer = true;
            attemptsOnCurrentCard++;
            SetControlsInteractable(false);

            DoctorWritingW02PatientCard card = cards[currentCardIndex];

            if (isValid) {
                if (attemptsOnCurrentCard == 1) {
                    score++;
                    UpdateScoreUI();
                }

                if (inputFieldBg != null) inputFieldBg.color = correctColor;
                if (feedbackBanner != null) feedbackBanner.SetActive(true);
                if (feedbackTextTMP != null) {
                    feedbackTextTMP.text = "<color=#33D866><b>✓ Card Completed & Filed!</b></color>";
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    if (sfxCorrect != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
                    } else {
                        PlayCurrentAudio();
                    }
                }

                float waitTime = sfxCorrect != null ? sfxCorrect.length + 0.4f : 2.4f;
                yield return new WaitForSeconds(waitTime);

                // Auto-advance to the next card or results
                if (currentCardIndex < cards.Length - 1) {
                    ShowCard(currentCardIndex + 1);
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
                    if (sfxWrong != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                    }
                }

                if (attemptsOnCurrentCard == 1) {
                    if (feedbackBanner != null) feedbackBanner.SetActive(true);
                    if (feedbackTextTMP != null) {
                        feedbackTextTMP.text = "<color=#FF6666>Card incomplete. Use the word-bank chips below to help!</color>";
                    }
                    float waitTime = sfxWrong != null ? sfxWrong.length + 0.3f : 1.5f;
                    yield return new WaitForSeconds(waitTime);
                    SetControlsInteractable(true);
                    isCheckingAnswer = false;
                } else {
                    if (feedbackBanner != null) feedbackBanner.SetActive(true);
                    if (feedbackTextTMP != null) {
                        feedbackTextTMP.text = $"<color=#FFAA33>Model: {card.modelAnswer}</color>";
                    }

                    if (card.cardAudio != null && Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(card.cardAudio);
                        yield return new WaitForSeconds(card.cardAudio.length + 0.4f);
                    } else {
                        yield return new WaitForSeconds(2.8f);
                    }

                    // Auto-advance to next card or results
                    if (currentCardIndex < cards.Length - 1) {
                        ShowCard(currentCardIndex + 1);
                    } else {
                        ShowResults();
                    }
                }
            }
        }

        public void PlayCurrentAudio() {
            if (cards == null || currentCardIndex >= cards.Length) return;
            AudioClip clip = cards[currentCardIndex].cardAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        public void ReplayCurrentAudio() {
            if (isIntroPhase) return;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            PlayCurrentAudio();
        }

        private void ShowResults() {
            if (patientCardObject != null) patientCardObject.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

            Transform rail = transform.Find("WordBankRail") ?? transform.Find("WordBankContainer") ?? transform.Find("OptionsContainer");
            if (rail != null) rail.gameObject.SetActive(false);

            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }

            bool passed = (score >= 2);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "PATIENT CARDS FILED! 📋" : "KEEP PRACTICING!";
                    resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
                }

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You completed {score} of {cards.Length} health cards correctly!";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed ? "Success! All patient records and messages are accurately filled." : "You need at least 2/3 cards completed to pass this lesson.";
                }

                if (returnHubBtn != null) {
                    returnHubBtn.gameObject.SetActive(passed);
                }

                if (retryBtn != null) {
                    retryBtn.gameObject.SetActive(!passed || score < cards.Length);
                }
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(passed);
            }

            if (passed) {
                if (recapAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                }
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
            if (patientCardObject == null) {
                Transform t = transform.Find("ComicStageCard") ?? transform.Find("PatientCard") ?? transform.Find("DeskCard") ?? transform.Find("StageCard") ?? transform.Find("PromptCard");
                if (t != null) patientCardObject = t.gameObject;
            }
            if (cardTitleTMP == null && patientCardObject != null) {
                Transform t = patientCardObject.transform.Find("StripTitleTMP") ?? patientCardObject.transform.Find("CardTitleTMP") ?? patientCardObject.transform.Find("Title") ?? patientCardObject.transform.Find("NoticeHeaderTMP");
                if (t != null) cardTitleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (scenarioDescTMP == null && patientCardObject != null) {
                Transform t = patientCardObject.transform.Find("SituationDescTMP") ?? patientCardObject.transform.Find("ScenarioDescTMP") ?? patientCardObject.transform.Find("SituationTextTMP") ?? patientCardObject.transform.Find("Text");
                if (t != null) scenarioDescTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (promptGuideTMP == null && patientCardObject != null) {
                Transform t = patientCardObject.transform.Find("SpeakerAPromptTMP") ?? patientCardObject.transform.Find("PromptGuideTMP");
                if (t != null) promptGuideTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (inputField == null) {
                inputField = GetComponentInChildren<TMP_InputField>(true);
            }
            if (inputField != null && inputFieldBg == null) {
                inputFieldBg = inputField.GetComponent<Image>();
            }
            if (submitBtn == null) {
                Transform t = transform.Find("SubmitButton") ?? transform.Find("Check") ?? transform.Find("CheckButton");
                if (t != null) submitBtn = t.GetComponent<Button>();
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

            // Find word bank chips
            if (wordBankChipButtons == null || wordBankChipButtons.Length == 0) {
                Transform rail = transform.Find("WordBankRail") ?? transform.Find("WordBankContainer") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionChipsGrid");
                if (rail != null) {
                    List<Button> btns = new List<Button>();
                    List<TextMeshProUGUI> txts = new List<TextMeshProUGUI>();

                    for (int i = 0; i < rail.childCount; i++) {
                        Button b = rail.GetChild(i).GetComponent<Button>();
                        if (b != null) {
                            btns.Add(b);
                            txts.Add(b.GetComponentInChildren<TextMeshProUGUI>());
                        }
                    }
                    wordBankChipButtons = btns.ToArray();
                    wordBankChipTexts = txts.ToArray();
                }
            }
        }
    }
}
