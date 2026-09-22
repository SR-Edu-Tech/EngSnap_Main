using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit8 {

    

    /// <summary>
    /// INTRO: The Bazaar Opens — Lesson Controller for Book 2B Unit 8 (Shopping Time).
    /// 5-Step Cinematic Shopping Intro:
    /// Step 1: Bazaar Opens — Shutters roll up along little shops (baker's, chemist, florist, boutique, toy shop, jeweller's). Window signs blink on.
    /// Step 2: ARIA reads 3 window signs as they flash: 'Open' · 'Clearance sale' · 'Buy one get one free'.
    /// Step 3: LEO arrives with shopping bag and list, stops at a shop window and asks: 'Excuse me! How much is this?'
    /// Step 4: Shopkeeper answers: 'Well, it costs four hundred rupees. You will get 10% discount.' — price flips from 400 to 360.
    /// Step 5: ARIA previews goal: 'Find the right shop, read the window, ask the price and get a bargain!' + START button to enter Hub.
    /// </summary>
    public class Masters_ShoppingTime_Intro_LessonOne : Masters_Lesson {

[System.Serializable]
    public class ShoppingTime_IntroStepData {
        public string speakerName;         // e.g. "NARRATOR", "ARIA", "LEO", "SHOPKEEPER"
        public string dialogueLine;        // Spoken dialogue / subtitle
        public string actionDescription;   // Stage action description
        public string cardHeadline;        // Center card headline
        public string cardDetail;          // Center card detail text
        public Color cardThemeColor;       // Card highlight theme color
        public AudioClip stepAudio;        // Voiceover clip
    }
    
        [Header("5-Step Bazaar Intro Flow")]
        [SerializeField] private ShoppingTime_IntroStepData[] introSteps;

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

        [Header("Center Stage Display Card")]
        [SerializeField] private GameObject stageCardObject;
        [SerializeField] private Image stageCardBg;
        [SerializeField] private TextMeshProUGUI cardHeadlineTMP;
        [SerializeField] private TextMeshProUGUI cardDetailTMP;

        [Header("Navigation & Call-To-Action")]
        [SerializeField] private Button startUnitBtn;
        [SerializeField] private Button nextStepBtn;
        [SerializeField] private Button prevStepBtn;
        [SerializeField] private Button replayBtn;
        [SerializeField] private Button ariaReplayBtn;

        private int currentStepIndex = 0;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Intro;

            PurgeLegacyChildren();
            WireIntroButtons();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Intro;

            PurgeLegacyChildren();
            EnsureHeaderAndTitle();
            WireIntroButtons();

            if (introSteps == null || introSteps.Length == 0) {
                PopulateDefaultSteps();
            }

            StartIntroSequence();
        }

        public override void EnsureNextAndBackButtonWired() {
            // 1. Wire BackButton safely
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons) {
                if (b != null && b.name.ToLower().Contains("back")) {
                    b.gameObject.SetActive(true);
                    b.interactable = true;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => {
                        if (Masters_AudioManager.Instance != null) {
                            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                            Masters_AudioManager.Instance.StopVoiceOver();
                        }
                        if (Masters_LevelManager.Instance != null) {
                            Masters_LevelManager.Instance.OnBackButtonClicked();
                        }
                    });
                }
            }

            // 2. Prevent base class from hijacking NextStepBtn as nextButton
            nextButton = null;

            // 3. Explicitly wire intro navigation buttons
            WireIntroButtons();
        }

        private void WireIntroButtons() {
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

            if (replayBtn != null) {
                replayBtn.onClick.RemoveAllListeners();
                replayBtn.onClick.AddListener(ReplayCurrentStep);
            }

            if (ariaReplayBtn != null) {
                ariaReplayBtn.onClick.RemoveAllListeners();
                ariaReplayBtn.onClick.AddListener(ReplayCurrentStep);
            }
        }

        public void EnsureHeaderAndTitle() {
            if (headerTMP != null) headerTMP.text = "SHOPPING TIME";
            if (titleTMP != null) titleTMP.text = "The Bazaar Opens";
            if (subtitleTMP != null) subtitleTMP.text = "Find the right shop, read the window, ask the price and get a bargain!";
            if (progressTMP != null) progressTMP.text = "Step 1/5";
        }

        [ContextMenu("Update Editor Preview")]
        public void UpdateEditorPreview() {
            WireIntroButtons();
            EnsureHeaderAndTitle();

            if (introSteps == null || introSteps.Length == 0) {
                PopulateDefaultSteps();
            }

            if (introSteps != null && introSteps.Length > 0) {
                DisplayStepData(introSteps[0], 0);
            }
        }

        private void PurgeLegacyChildren() {
            string[] legacyNames = new string[] {
                "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)",
                "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
                "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
                "SpawnArea", "GamePlayGameObject"
            };

            foreach (string lName in legacyNames) {
                Transform lTrans = transform.Find(lName);
                if (lTrans != null) {
                    lTrans.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(lTrans.gameObject);
                    else DestroyImmediate(lTrans.gameObject);
                }
            }
        }

        private void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI tmp in tmps) {
                string n = tmp.gameObject.name.ToLower();
                if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading") || n.Contains("unitheading"))) headerTMP = tmp;
                else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("unittmp") || n == "title")) titleTMP = tmp;
                else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
                else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("stepcount"))) progressTMP = tmp;
                else if (speakerNameTMP == null && n.Contains("speaker")) speakerNameTMP = tmp;
                else if (dialogueTextTMP == null && (n.Contains("dialogue") || n.Contains("line"))) dialogueTextTMP = tmp;
                else if (actionTextTMP == null && (n.Contains("action") || n.Contains("description"))) actionTextTMP = tmp;
                else if (cardHeadlineTMP == null && (n.Contains("headline") || n.Contains("cardtitle"))) cardHeadlineTMP = tmp;
                else if (cardDetailTMP == null && (n.Contains("carddetail") || n.Contains("cardsub"))) cardDetailTMP = tmp;
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                if (startUnitBtn == null && (n.Contains("start") || n.Contains("enter") || n.Contains("hub"))) startUnitBtn = btn;
                else if (nextStepBtn == null && (n.Contains("nextstep") || n.Contains("stepnext") || n == "nextstepbtn")) nextStepBtn = btn;
                else if (prevStepBtn == null && (n.Contains("prevstep") || n.Contains("stepprev") || n.Contains("backstep"))) prevStepBtn = btn;
                else if (replayBtn == null && n.Contains("replay")) replayBtn = btn;
                else if (ariaReplayBtn == null && (n.Contains("aria") || n.Contains("character"))) ariaReplayBtn = btn;
            }

            if (stageCardObject == null) {
                Transform cardTrans = transform.Find("StudioCard") ?? transform.Find("CenterCard") ?? transform.Find("StageCard") ?? transform.Find("Card");
                if (cardTrans != null) {
                    stageCardObject = cardTrans.gameObject;
                    stageCardBg = cardTrans.GetComponent<Image>();
                }
            }
        }

        public void PopulateDefaultSteps() {
            string audioDir = "Assets/Audio/2B/8_Shopping_Time/Intro/";

            introSteps = new ShoppingTime_IntroStepData[] {
                // Step 1: Bazaar Opens
                new ShoppingTime_IntroStepData {
                    speakerName = "NARRATOR",
                    dialogueLine = "\"Welcome to Unit 8 Shopping Time! The Bazaar Opens. The shutters roll up and the shop signs light up one after another.\"",
                    actionDescription = "Scene fades in on the closed bazaar; the shutters roll up and the signs light one after another.",
                    cardHeadline = "THE BAZAAR OPENS",
                    cardDetail = "Shutters roll up along a row of little shops:\nBaker's | Chemist | Florist | Boutique | Toy Shop | Jeweller's\nWindow signs blink on: OPEN | SALE | 70% OFF ON ALL ITEMS",
                    cardThemeColor = new Color(0.12f, 0.42f, 0.78f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "shoppingtime_intro_01_bazaar_opens.mp3")
#endif
                },
                // Step 2: ARIA reads 3 window signs
                new ShoppingTime_IntroStepData {
                    speakerName = "ARIA",
                    dialogueLine = "\"Open! Clearance sale! Buy one get one free!\"",
                    actionDescription = "ARIA reads three windows as they flash: 'Open' - 'Clearance sale' - 'Buy one get one free'.",
                    cardHeadline = "WINDOW SIGNS FLASH",
                    cardDetail = "1. 'Open'\n2. 'Clearance sale'\n3. 'Buy one get one free'",
                    cardThemeColor = new Color(0.85f, 0.40f, 0.12f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "shoppingtime_intro_02_aria_windows.mp3")
#endif
                },
                // Step 3: LEO asks the price
                new ShoppingTime_IntroStepData {
                    speakerName = "LEO",
                    dialogueLine = "\"Excuse me! How much is this?\"",
                    actionDescription = "LEO stops at a shop window with his shopping bag and list and asks the price.",
                    cardHeadline = "ASKING THE PRICE",
                    cardDetail = "\"Excuse me! How much is this?\"\nLeo arrives with a shopping bag and list to shop at the bazaar.",
                    cardThemeColor = new Color(0.20f, 0.60f, 0.85f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "shoppingtime_intro_03_leo_how_much.mp3")
#endif
                },
                // Step 4: Shopkeeper answers with discount
                new ShoppingTime_IntroStepData {
                    speakerName = "SHOPKEEPER",
                    dialogueLine = "\"Well, it costs four hundred rupees. You will get 10% discount.\"",
                    actionDescription = "The shopkeeper answers with the discount - and the price tag flips from 400 to 360 rupees!",
                    cardHeadline = "DISCOUNT AND BARGAIN",
                    cardDetail = "\"Well, it costs four hundred rupees. You will get 10% discount.\"\nOriginal: Rs. 400 -> Discounted: Rs. 360 (10% OFF!)",
                    cardThemeColor = new Color(0.58f, 0.28f, 0.78f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "shoppingtime_intro_04_shopkeeper_discount.mp3")
#endif
                },
                // Step 5: ARIA previews unit goal + START button
                new ShoppingTime_IntroStepData {
                    speakerName = "ARIA",
                    dialogueLine = "\"Find the right shop, read the window, ask the price and get a bargain! Tap START to begin!\"",
                    actionDescription = "ARIA previews the unit goal. A large START button appears!",
                    cardHeadline = "EXPLORE THE BAZAAR!",
                    cardDetail = "Find the right shop - Read the window - Ask the price - Get a bargain!\nTap START to enter the unit!",
                    cardThemeColor = new Color(0.12f, 0.75f, 0.42f, 0.95f),
#if UNITY_EDITOR
                    stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "shoppingtime_intro_05_aria_goal_preview.mp3")
#endif
                }
            };
        }

        public void StartIntroSequence() {
            currentStepIndex = 0;
            LoadStep(currentStepIndex);
        }

        public void LoadStep(int stepIndex) {
            if (introSteps == null || introSteps.Length == 0) return;

            currentStepIndex = Mathf.Clamp(stepIndex, 0, introSteps.Length - 1);
            var step = introSteps[currentStepIndex];

            DisplayStepData(step, currentStepIndex);
        }

        private void DisplayStepData(ShoppingTime_IntroStepData step, int index) {
            if (progressTMP != null) {
                progressTMP.text = $"Step {index + 1}/{introSteps.Length}";
            }

            if (speakerNameTMP != null) {
                speakerNameTMP.text = step.speakerName;
                if (step.speakerName.Contains("ARIA")) {
                    speakerNameTMP.color = new Color(1f, 0.88f, 0.25f);
                } else if (step.speakerName.Contains("SHOPKEEPER")) {
                    speakerNameTMP.color = new Color(0.95f, 0.55f, 0.95f);
                } else if (step.speakerName.Contains("LEO")) {
                    speakerNameTMP.color = new Color(0.3f, 0.85f, 1f);
                } else {
                    speakerNameTMP.color = new Color(1f, 1f, 1f);
                }
            }

            if (dialogueTextTMP != null) {
                dialogueTextTMP.text = step.dialogueLine;
            }

            if (actionTextTMP != null) {
                actionTextTMP.text = step.actionDescription;
            }

            if (cardHeadlineTMP != null) {
                cardHeadlineTMP.text = step.cardHeadline;
            }

            if (cardDetailTMP != null) {
                cardDetailTMP.text = step.cardDetail;
            }

            if (stageCardBg != null) {
                stageCardBg.DOColor(step.cardThemeColor, 0.25f);
            }

            if (stageCardObject != null) {
                stageCardObject.transform.DOKill();
                stageCardObject.transform.localScale = Vector3.one * 0.95f;
                stageCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }

            bool isLastStep = (index == introSteps.Length - 1);

            if (startUnitBtn != null) {
                startUnitBtn.gameObject.SetActive(true);
                if (isLastStep) {
                    startUnitBtn.transform.DOKill();
                    startUnitBtn.transform.DOScale(Vector3.one * 1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
                } else {
                    startUnitBtn.transform.DOKill();
                    startUnitBtn.transform.localScale = Vector3.one;
                }
            }

            if (prevStepBtn != null) {
                prevStepBtn.interactable = (index > 0);
            }

            if (nextStepBtn != null) {
                nextStepBtn.gameObject.SetActive(!isLastStep);
            }

            if (step.stepAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(step.stepAudio);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            }
        }

        private void OnNextStepClicked() {
            if (currentStepIndex < introSteps.Length - 1) {
                if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                LoadStep(currentStepIndex + 1);
            }
        }

        private void OnPrevStepClicked() {
            if (currentStepIndex > 0) {
                if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                LoadStep(currentStepIndex - 1);
            }
        }

        public void ReplayCurrentStep() {
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            LoadStep(currentStepIndex);
        }

        public void OnStartUnitClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                Masters_AudioManager.Instance.StopVoiceOver();
            }

            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Listening);
            }

            OnNextButtonClicked();
        }

        protected override void OnNextButtonClicked() {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Intro);
            }
        }
    }
}
