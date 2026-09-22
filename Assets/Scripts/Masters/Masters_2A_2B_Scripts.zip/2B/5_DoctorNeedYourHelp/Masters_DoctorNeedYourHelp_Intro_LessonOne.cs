using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit5 {


    /// <summary>
    /// INTRO Care Clinic Controller for Book 2B Unit 5 (Doctor Need Your Help).
    /// Step 1: Morning Bedroom - Leo wakes up holding his head: "My head hurts! What's wrong with me?"
    /// Step 2: Scene wipes to Care Clinic - ARIA shows the 3 health communication places.
    /// Step 3: Reception Desk: "I'd like to take an appointment with Dr Gupta, please."
    /// Step 4: Consulting Room & Home: "I had trouble in breathing." / "I think I am running a temperature too."
    /// Step 5: ARIA Goal Preview + START button transitions to the Unit Hub.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Intro_LessonOne : Masters_Lesson {

[System.Serializable]
    public class DoctorIntroStepData {
        public string speakerName;
        public string dialogueLine;
        public string actionDescription;
        public string cardHeadline;
        public string cardDetail;
        public Color cardThemeColor;
        public AudioClip stepAudio;
    }
    
        [Header("5-Step Care Clinic Intro Flow")]
        [SerializeField] private DoctorIntroStepData[] introSteps;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;

        [Header("Speaker & Dialogue Box")]
        [SerializeField] private GameObject dialogueContainer;
        [SerializeField] private TextMeshProUGUI speakerNameTMP;
        [SerializeField] private TextMeshProUGUI dialogueTextTMP;
        [SerializeField] private TextMeshProUGUI actionTextTMP;

        [Header("Clinic Center Stage Card")]
        [SerializeField] private GameObject clinicCardObject;
        [SerializeField] private Image clinicCardBg;
        [SerializeField] private TextMeshProUGUI cardHeadlineTMP;
        [SerializeField] private TextMeshProUGUI cardDetailTMP;

        [Header("Navigation & Call To Action")]
        [SerializeField] private Button startUnitBtn;
        [SerializeField] private Button nextStepBtn;
        [SerializeField] private Button prevStepBtn;
        [SerializeField] private Button ariaReplayBtn;

        private int currentStepIndex = 0;
        private Coroutine autoAdvanceCoroutine;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Intro;

            InitIntroStepsIfEmpty();
            AutoBindReferences();

            if (startUnitBtn != null) {
                startUnitBtn.onClick.RemoveAllListeners();
                startUnitBtn.onClick.AddListener(OnStartUnitClicked);
            }

            if (nextStepBtn != null) {
                nextStepBtn.onClick.RemoveAllListeners();
                nextStepBtn.onClick.AddListener(OnNextStepClicked);
            }

            if (prevStepBtn != null) {
                prevStepBtn.onClick.RemoveAllListeners();
                prevStepBtn.onClick.AddListener(OnPrevStepClicked);
            }

            if (ariaReplayBtn != null) {
                ariaReplayBtn.onClick.RemoveAllListeners();
                ariaReplayBtn.onClick.AddListener(ReplayCurrentStep);
            }
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Intro;
            EnsureNextAndBackButtonWired();
            ShowStep(0);
        }

        public void InitIntroStepsIfEmpty() {
            string audioDir = "Assets/Audio/2B/5_DoctorNeedYourHelp/Intro/";

            introSteps = new DoctorIntroStepData[] {
                new DoctorIntroStepData {
                    speakerName = "LEO",
                    dialogueLine = "\"My head hurts! What's wrong with me?\"",
                    actionDescription = "Scene fades in on the morning bedroom; the clock shows 8:00 AM. LEO sits up slowly holding his head.",
                    cardHeadline = "MORNING BEDROOM — 8:00 AM",
                    cardDetail = "LEO wakes up feeling unwell: \"My head hurts! What's wrong with me?\"",
                    cardThemeColor = new Color(0.12f, 0.28f, 0.48f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_intro_01_leo_head_hurts.mp3")
#endif
                },
                new DoctorIntroStepData {
                    speakerName = "ARIA",
                    dialogueLine = "\"Welcome to the Care Clinic! There are three main places you talk about health: the reception desk, the consulting room, and at home with a parent.\"",
                    actionDescription = "The scene wipes to the Care Clinic; ARIA shows the three health communication spaces.",
                    cardHeadline = "THE CARE CLINIC",
                    cardDetail = "1. The Reception Desk\n2. The Consulting Room\n3. Home with a Parent",
                    cardThemeColor = new Color(0.08f, 0.45f, 0.40f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_intro_02_aria_clinic.mp3")
#endif
                },
                new DoctorIntroStepData {
                    speakerName = "PATIENT (RECEPTION)",
                    dialogueLine = "\"I'd like to take an appointment with Dr Gupta, please.\"",
                    actionDescription = "At the reception counter with a bell, scheduling a doctor visit.",
                    cardHeadline = "1. RECEPTION DESK",
                    cardDetail = "\"I'd like to take an appointment with Dr Gupta, please.\"",
                    cardThemeColor = new Color(0.15f, 0.35f, 0.65f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_intro_03_reception.mp3")
#endif
                },
                new DoctorIntroStepData {
                    speakerName = "LEO & PATIENT",
                    dialogueLine = "\"I had trouble in breathing.\" - \"I think I am running a temperature too.\"",
                    actionDescription = "In the consulting room with Dr Gupta and at home describing symptoms.",
                    cardHeadline = "2. CONSULTING ROOM & 3. HOME",
                    cardDetail = "• \"I had trouble in breathing.\"\n• \"I think I am running a temperature too.\"",
                    cardThemeColor = new Color(0.45f, 0.20f, 0.55f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_intro_04_consulting.mp3")
#endif
                },
                new DoctorIntroStepData {
                    speakerName = "ARIA",
                    dialogueLine = "\"Say what hurts, answer the doctor, book the appointment - let's learn to ask for help!\"",
                    actionDescription = "ARIA previews the unit goals. Tap START to begin the Care Clinic unit!",
                    cardHeadline = "LEARN TO ASK FOR HELP!",
                    cardDetail = "Say what hurts - Answer the doctor - Book the appointment",
                    cardThemeColor = new Color(0.12f, 0.55f, 0.30f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_intro_06_aria_preview_goal.mp3")
#endif
                }
            };
        }

        public void ShowStep(int index) {
            if (introSteps == null || introSteps.Length == 0) return;
            currentStepIndex = Mathf.Clamp(index, 0, introSteps.Length - 1);
            DoctorIntroStepData data = introSteps[currentStepIndex];

            if (progressTMP != null) progressTMP.text = $"{currentStepIndex + 1}/{introSteps.Length}";
            if (headerTMP != null) headerTMP.text = "CARE CLINIC";
            if (titleTMP != null) {
                titleTMP.text = "Ask for Help at the Clinic";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Tap START to enter the Unit. Tap ARIA to replay.";

            if (speakerNameTMP != null) speakerNameTMP.text = data.speakerName;
            if (dialogueTextTMP != null) dialogueTextTMP.text = data.dialogueLine;
            if (actionTextTMP != null) actionTextTMP.text = data.actionDescription;

            if (clinicCardBg != null) clinicCardBg.color = data.cardThemeColor;
            if (cardHeadlineTMP != null) cardHeadlineTMP.text = data.cardHeadline;
            if (cardDetailTMP != null) cardDetailTMP.text = data.cardDetail;

            bool isLastStep = (currentStepIndex == introSteps.Length - 1);
            if (startUnitBtn != null) startUnitBtn.gameObject.SetActive(isLastStep);
            if (nextStepBtn != null) nextStepBtn.gameObject.SetActive(!isLastStep);
            if (prevStepBtn != null) prevStepBtn.gameObject.SetActive(currentStepIndex > 0);

            if (data.stepAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(data.stepAudio);
            }
        }

        public void OnNextStepClicked() {
            if (currentStepIndex < introSteps.Length - 1) {
                if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                ShowStep(currentStepIndex + 1);
            }
        }

        public void OnPrevStepClicked() {
            if (currentStepIndex > 0) {
                if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                ShowStep(currentStepIndex - 1);
            }
        }

        public void ReplayCurrentStep() {
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            ShowStep(currentStepIndex);
        }

        public void OnStartUnitClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            OnNextButtonClicked();
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Intro;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        private void AutoBindReferences() {
            if (titleTMP == null) {
                Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("Header/Title");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (progressTMP == null) {
                Transform t = transform.Find("ProgressTMP") ?? transform.Find("Progress") ?? transform.Find("Header/Progress");
                if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (dialogueContainer == null) {
                Transform t = transform.Find("DialogueContainer") ?? transform.Find("DialogueBox");
                if (t != null) dialogueContainer = t.gameObject;
            }
            if (clinicCardObject == null) {
                Transform t = transform.Find("ClinicCard") ?? transform.Find("StageCard") ?? transform.Find("CenterCard");
                if (t != null) clinicCardObject = t.gameObject;
            }
            if (startUnitBtn == null) {
                Transform t = transform.Find("StartButton") ?? transform.Find("StartBtn") ?? transform.Find("START");
                if (t != null) startUnitBtn = t.GetComponent<Button>();
            }
            if (nextStepBtn == null) {
                Transform t = transform.Find("NextStepBtn") ?? transform.Find("NextButton");
                if (t != null) nextStepBtn = t.GetComponent<Button>();
            }
            if (ariaReplayBtn == null) {
                Transform t = transform.Find("Character") ?? transform.Find("ARIA") ?? transform.Find("AriaReplayBtn");
                if (t != null) ariaReplayBtn = t.GetComponent<Button>();
            }
        }
    }
}
