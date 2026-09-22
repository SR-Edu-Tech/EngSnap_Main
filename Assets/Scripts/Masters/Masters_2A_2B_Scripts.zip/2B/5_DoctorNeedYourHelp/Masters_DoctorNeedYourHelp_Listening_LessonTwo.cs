using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit5 {


    /// <summary>
    /// L02 Who Is Speaking?
    /// Listening lesson controller for Book 2B Unit 5 (Doctor Need Your Help).
    /// Three name plates: DOCTOR · RECEPTIONIST · PATIENT.
    /// Student listens to dialogue lines from the clinic and taps who said it.
    /// Success condition: Student identifies at least 8 of 10 speakers correctly.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Listening_LessonTwo : Masters_Lesson {

public enum ClinicSpeaker {
        Doctor = 0,
        Receptionist = 1,
        Patient = 2
    }

    [System.Serializable]
    public class DoctorListeningL02Round {
        public int roundId;
        public string spokenLine;
        public ClinicSpeaker speaker;
        public string contextNote;
        public AudioClip spokenAudio;
    }
    
        [Header("10 Clinic Dialogue Rounds")]
        [SerializeField] private DoctorListeningL02Round[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;

        [Header("Radio Desk & Prompt Card")]
        [SerializeField] private GameObject promptCardObject;
        [SerializeField] private TextMeshProUGUI promptHeaderTMP;
        [SerializeField] private TextMeshProUGUI spokenLineTMP;
        [SerializeField] private Button replayAudioBtn;

        [Header("3 Speaker Name Plates (Doctor, Receptionist, Patient)")]
        [SerializeField] private Button doctorPlateBtn;
        [SerializeField] private Image doctorPlateBg;
        [SerializeField] private TextMeshProUGUI doctorPlateText;

        [SerializeField] private Button receptionistPlateBtn;
        [SerializeField] private Image receptionistPlateBg;
        [SerializeField] private TextMeshProUGUI receptionistPlateText;

        [SerializeField] private Button patientPlateBtn;
        [SerializeField] private Image patientPlateBg;
        [SerializeField] private TextMeshProUGUI patientPlateText;

        [Header("Results & Completion Panel")]
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

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isHandlingAnswer = false;
        private bool isIntroPhase = true;

        private Color defaultPlateColor = new Color(0.12f, 0.40f, 0.70f, 0.95f);
        private Color correctPlateColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private Color wrongPlateColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Listening;
            base.Awake();

            AutoBindReferences();
            InitRoundsIfEmpty();
            WirePlateListeners();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Listening;
            AutoBindReferences();
            WirePlateListeners();
            EnsureNextAndBackButtonWired();

            if (rounds == null || rounds.Length == 0) {
                InitRoundsIfEmpty();
            }

            SetControlsInteractable(false);
            StartCoroutine(BeginLessonRoutine());
        }

        private IEnumerator BeginLessonRoutine() {
            isIntroPhase = true;
            if (progressTMP != null) progressTMP.text = "Round 1/10";
            if (scoreTMP != null) scoreTMP.text = "Score: 0/10";
            if (headerTMP != null) headerTMP.text = "RADIO DESK 📻";
            if (titleTMP != null) {
                titleTMP.text = "Who Is Speaking?";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Listen to the clinic line and tap who said it.";

            if (rounds != null && rounds.Length > 0) {
                if (promptHeaderTMP != null) promptHeaderTMP.text = "CLINIC DIALOGUE #1";
                if (spokenLineTMP != null) spokenLineTMP.text = rounds[0].spokenLine;
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
            if (doctorPlateBtn != null) doctorPlateBtn.interactable = interactable;
            if (receptionistPlateBtn != null) receptionistPlateBtn.interactable = interactable;
            if (patientPlateBtn != null) patientPlateBtn.interactable = interactable;
            if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;
        }

        private void WirePlateListeners() {
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

            if (doctorPlateBtn != null) {
                doctorPlateBtn.onClick.RemoveAllListeners();
                doctorPlateBtn.onClick.AddListener(() => OnSpeakerSelected(ClinicSpeaker.Doctor));
            }

            if (receptionistPlateBtn != null) {
                receptionistPlateBtn.onClick.RemoveAllListeners();
                receptionistPlateBtn.onClick.AddListener(() => OnSpeakerSelected(ClinicSpeaker.Receptionist));
            }

            if (patientPlateBtn != null) {
                patientPlateBtn.onClick.RemoveAllListeners();
                patientPlateBtn.onClick.AddListener(() => OnSpeakerSelected(ClinicSpeaker.Patient));
            }
        }

        public void InitRoundsIfEmpty() {
            string audioDir = "Assets/Audio/2B/5_DoctorNeedYourHelp/Listening/";

            rounds = new DoctorListeningL02Round[] {
                new DoctorListeningL02Round {
                    roundId = 1,
                    spokenLine = "\"How are you doing today?\"",
                    speaker = ClinicSpeaker.Doctor,
                    contextNote = "In the consulting room, greeting the patient.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round1_doc.mp3")
#endif
                },
                new DoctorListeningL02Round {
                    roundId = 2,
                    spokenLine = "\"Have you had any type of cold lately?\"",
                    speaker = ClinicSpeaker.Doctor,
                    contextNote = "Asking diagnostic health questions.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round2_doc.mp3")
#endif
                },
                new DoctorListeningL02Round {
                    roundId = 3,
                    spokenLine = "\"Do you have any allergies that you know of?\"",
                    speaker = ClinicSpeaker.Doctor,
                    contextNote = "Checking medical history before prescription.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round3_doc.mp3")
#endif
                },
                new DoctorListeningL02Round {
                    roundId = 4,
                    spokenLine = "\"I had trouble in breathing.\"",
                    speaker = ClinicSpeaker.Patient,
                    contextNote = "Describing symptoms to the doctor.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round4_pat.mp3")
#endif
                },
                new DoctorListeningL02Round {
                    roundId = 5,
                    spokenLine = "\"It happens a lot when I work out.\"",
                    speaker = ClinicSpeaker.Patient,
                    contextNote = "Explaining when the symptom occurs.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round5_pat.mp3")
#endif
                },
                new DoctorListeningL02Round {
                    roundId = 6,
                    spokenLine = "\"Do you have an appointment?\"",
                    speaker = ClinicSpeaker.Receptionist,
                    contextNote = "Checking the clinic schedule at the front desk.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round6_rec.mp3")
#endif
                },
                new DoctorListeningL02Round {
                    roundId = 7,
                    spokenLine = "\"Is it urgent?\"",
                    speaker = ClinicSpeaker.Receptionist,
                    contextNote = "Assessing priority at the reception desk.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round7_rec.mp3")
#endif
                },
                new DoctorListeningL02Round {
                    roundId = 8,
                    spokenLine = "\"May I know your mom's name, please?\"",
                    speaker = ClinicSpeaker.Receptionist,
                    contextNote = "Taking patient details for the doctor.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round8_rec.mp3")
#endif
                },
                new DoctorListeningL02Round {
                    roundId = 9,
                    spokenLine = "\"I'd like to take an appointment with Dr Gupta, for my mom, please.\"",
                    speaker = ClinicSpeaker.Patient,
                    contextNote = "Booking a visit at the reception counter.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round9_pat.mp3")
#endif
                },
                new DoctorListeningL02Round {
                    roundId = 10,
                    spokenLine = "\"Please be here with your mom at 1:30 p.m., Mr. Steve.\"",
                    speaker = ClinicSpeaker.Receptionist,
                    contextNote = "Confirming the appointment time slot.",
#if UNITY_EDITOR
                    spokenAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l02_round10_rec.mp3")
#endif
                }
            };
        }

        public void RestartLesson() {
            currentRoundIndex = 0;
            score = 0;
            isHandlingAnswer = false;

            if (headerTMP != null) headerTMP.gameObject.SetActive(true);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (promptCardObject != null) promptCardObject.SetActive(true);

            Transform platesTr = transform.Find("PlatesContainer") ?? transform.Find("CanvasesContainer");
            if (platesTr != null) platesTr.gameObject.SetActive(true);

            UpdateScoreUI();
            ShowRound(0);
        }

        public void ShowRound(int roundIdx) {
            if (rounds == null || rounds.Length == 0) return;
            currentRoundIndex = Mathf.Clamp(roundIdx, 0, rounds.Length - 1);
            DoctorListeningL02Round round = rounds[currentRoundIndex];

            if (progressTMP != null) progressTMP.text = $"Round {currentRoundIndex + 1}/{rounds.Length}";
            if (headerTMP != null) headerTMP.text = "RADIO DESK 📻";
            if (titleTMP != null) {
                titleTMP.text = "Who Is Speaking?";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Listen to the clinic line and tap who said it.";

            if (promptHeaderTMP != null) promptHeaderTMP.text = $"CLINIC DIALOGUE #{currentRoundIndex + 1}";
            if (spokenLineTMP != null) spokenLineTMP.text = round.spokenLine;

            // Reset plates and enable controls
            ResetPlates();
            SetControlsInteractable(true);
            PlayCurrentAudio();
        }

        private void ResetPlates() {
            if (doctorPlateBg != null) doctorPlateBg.color = defaultPlateColor;
            if (receptionistPlateBg != null) receptionistPlateBg.color = defaultPlateColor;
            if (patientPlateBg != null) patientPlateBg.color = defaultPlateColor;

            if (doctorPlateBtn != null) {
                doctorPlateBtn.interactable = true;
                doctorPlateBtn.transform.localScale = Vector3.one;
            }
            if (receptionistPlateBtn != null) {
                receptionistPlateBtn.interactable = true;
                receptionistPlateBtn.transform.localScale = Vector3.one;
            }
            if (patientPlateBtn != null) {
                patientPlateBtn.interactable = true;
                patientPlateBtn.transform.localScale = Vector3.one;
            }
        }

        public void PlayCurrentAudio() {
            if (rounds == null || currentRoundIndex >= rounds.Length) return;
            AudioClip clip = rounds[currentRoundIndex].spokenAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
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

        public void OnSpeakerSelected(ClinicSpeaker selectedSpeaker) {
            if (isHandlingAnswer || isIntroPhase) return;
            if (rounds == null || currentRoundIndex >= rounds.Length) return;

            DoctorListeningL02Round round = rounds[currentRoundIndex];
            bool isCorrect = (selectedSpeaker == round.speaker);

            StartCoroutine(HandleAnswerCoroutine(selectedSpeaker, isCorrect));
        }

        private IEnumerator HandleAnswerCoroutine(ClinicSpeaker selected, bool isCorrect) {
            isHandlingAnswer = true;

            // Lock plates
            if (doctorPlateBtn != null) doctorPlateBtn.interactable = false;
            if (receptionistPlateBtn != null) receptionistPlateBtn.interactable = false;
            if (patientPlateBtn != null) patientPlateBtn.interactable = false;

            Button selectedBtn = GetButtonForSpeaker(selected);
            Image selectedBg = GetImageForSpeaker(selected);

            if (isCorrect) {
                score++;
                UpdateScoreUI();

                if (selectedBg != null) selectedBg.color = correctPlateColor;
                if (selectedBtn != null) selectedBtn.transform.DOScale(1.1f, 0.2f).SetLoops(2, LoopType.Yoyo);

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
                if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
                }

                yield return new WaitForSeconds(1.5f);
            } else {
                if (selectedBg != null) selectedBg.color = wrongPlateColor;
                if (selectedBtn != null) selectedBtn.transform.DOShakePosition(0.4f, 12f, 14, 90, false, true);

                // Highlight correct plate in green
                Image correctBg = GetImageForSpeaker(rounds[currentRoundIndex].speaker);
                if (correctBg != null) correctBg.color = correctPlateColor;

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                }

                yield return new WaitForSeconds(1.8f);
            }

            isHandlingAnswer = false;

            if (currentRoundIndex < rounds.Length - 1) {
                ShowRound(currentRoundIndex + 1);
            } else {
                ShowResults();
            }
        }

        private Button GetButtonForSpeaker(ClinicSpeaker s) {
            switch (s) {
                case ClinicSpeaker.Doctor: return doctorPlateBtn;
                case ClinicSpeaker.Receptionist: return receptionistPlateBtn;
                case ClinicSpeaker.Patient: return patientPlateBtn;
                default: return doctorPlateBtn;
            }
        }

        private Image GetImageForSpeaker(ClinicSpeaker s) {
            switch (s) {
                case ClinicSpeaker.Doctor: return doctorPlateBg;
                case ClinicSpeaker.Receptionist: return receptionistPlateBg;
                case ClinicSpeaker.Patient: return patientPlateBg;
                default: return doctorPlateBg;
            }
        }

        private void UpdateScoreUI() {
            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{rounds.Length}";
            }
        }

        private void ShowResults() {
            if (promptCardObject != null) promptCardObject.SetActive(false);
            
            // Hide Plates Container during result screen
            Transform platesTr = transform.Find("PlatesContainer") ?? transform.Find("CanvasesContainer");
            if (platesTr != null) platesTr.gameObject.SetActive(false);

            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

            // Ensure ResultPanel reference
            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }

            bool passed = (score >= 8);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "WELL DONE! 🩺" : "KEEP PRACTICING!";
                    resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
                }

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You identified {score} of {rounds.Length} clinic speakers correctly!";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed ? "Success! You know who says what at the clinic." : "You need at least 8/10 to pass this lesson.";
                }

                if (returnHubBtn != null) {
                    returnHubBtn.gameObject.SetActive(passed);
                }

                if (retryBtn != null) {
                    retryBtn.gameObject.SetActive(!passed || score < rounds.Length);
                }
            }

            // Always activate CommonHUD NextButton when passed
            if (nextButton != null) {
                nextButton.gameObject.SetActive(passed);
            }

            if (passed && recapAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
            } else if (passed && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Listening;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        private void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps) {
                string n = tmp.gameObject.name.ToLower();
                if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
                else if (progressTMP == null && n.Contains("progress")) progressTMP = tmp;
                else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
                else if (headerTMP == null && (n.Contains("branch") || n.Contains("header"))) headerTMP = tmp;
                else if (subtitleTMP == null && n.Contains("subtitle")) subtitleTMP = tmp;
                else if (promptHeaderTMP == null && (n.Contains("promptheadertmp") || n.Contains("radioheadertmp"))) promptHeaderTMP = tmp;
                else if (spokenLineTMP == null && (n.Contains("spokensentencetmp") || n.Contains("spokenlinetmp"))) spokenLineTMP = tmp;
            }

            if (promptCardObject == null) {
                Transform t = transform.Find("PromptCard") ?? transform.Find("RadioCard") ?? transform.Find("CenterCard");
                if (t != null) promptCardObject = t.gameObject;
            }

            if (replayAudioBtn == null) {
                Transform t = transform.Find("PromptCard/ReplayAudioBtn") ?? transform.Find("ReplayAudioBtn") ?? transform.Find("SpeakerBtn") ?? transform.Find("AudioBtn");
                if (t != null) replayAudioBtn = t.GetComponent<Button>();
            }

            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }

            if (resultPanel != null) {
                if (retryBtn == null) {
                    Transform t = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                    if (t != null) retryBtn = t.GetComponent<Button>();
                }
                if (returnHubBtn == null) {
                    Transform t = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("ReturnHubBtn") ?? resultPanel.transform.Find("NextBtn") ?? resultPanel.transform.Find("NextButton");
                    if (t != null) returnHubBtn = t.GetComponent<Button>();
                }
                if (resultTitleTMP == null) {
                    Transform t = resultPanel.transform.Find("ResultTitle");
                    if (t != null) resultTitleTMP = t.GetComponent<TextMeshProUGUI>();
                }
                if (resultScoreTMP == null) {
                    Transform t = resultPanel.transform.Find("ResultScore");
                    if (t != null) resultScoreTMP = t.GetComponent<TextMeshProUGUI>();
                }
                if (resultStatusTMP == null) {
                    Transform t = resultPanel.transform.Find("ResultStatus");
                    if (t != null) resultStatusTMP = t.GetComponent<TextMeshProUGUI>();
                }
            }

            // Name plates
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (var btn in allBtns) {
                string bn = btn.gameObject.name.ToLower();
                if (doctorPlateBtn == null && bn.Contains("doctor")) {
                    doctorPlateBtn = btn;
                    doctorPlateBg = btn.GetComponent<Image>();
                    doctorPlateText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                } else if (receptionistPlateBtn == null && bn.Contains("receptionist")) {
                    receptionistPlateBtn = btn;
                    receptionistPlateBg = btn.GetComponent<Image>();
                    receptionistPlateText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                } else if (patientPlateBtn == null && bn.Contains("patient")) {
                    patientPlateBtn = btn;
                    patientPlateBg = btn.GetComponent<Image>();
                    patientPlateText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }
    }
}
