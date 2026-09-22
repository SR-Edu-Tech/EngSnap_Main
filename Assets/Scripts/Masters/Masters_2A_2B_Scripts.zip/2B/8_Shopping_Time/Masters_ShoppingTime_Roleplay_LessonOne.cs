using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// RP01 On Stage — Buying the Blue T-Shirt
/// Guided multi-step shopping dialogue for Book 2B Unit 8 (Shopping Time).
/// The Shop Keeper (NPC - Female) speaks the book's lines; Joy (Student - Male) responds with fitting lines.
/// Success condition: Student chooses a fitting line in at least 4 of 5 steps.
/// </summary>
public class Masters_ShoppingTime_Roleplay_LessonOne : Masters_Lesson {

[System.Serializable]
public class ShoppingTime_RoleplayRP01Step {
    public int stepId;
    public string stepTitle;              // e.g. "Step 1: Offering Help"
    public string npcSpeakerName;         // "Shop Keeper"
    public string npcOpeningLine;         // e.g. "Excuse me! May I help you?"
    public string studentFittingAnswer;   // e.g. "Yes, I want a T-Shirt. Can you show me one?"
    public string studentDistractorLine;  // e.g. "Give me shirt."
    public int correctOptionIndex;        // 0 or 1
    public string[] optionChoices;        // 2 options
    public AudioClip npcAudio;
    public AudioClip studentAudio;
}

    [Header("RP01 5 Shopping Steps")]
    [SerializeField] private ShoppingTime_RoleplayRP01Step[] steps;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;

    [Header("Characters & Speech Clouds")]
    [SerializeField] private GameObject npcAndStudentGameObject;
    [SerializeField] private GameObject npcCloud;
    [SerializeField] private TextMeshProUGUI npcDialogueTMP;
    [SerializeField] private GameObject studentCloud;
    [SerializeField] private TextMeshProUGUI studentDialogueTMP;

    [Header("Dialogue Option Buttons")]
    [SerializeField] private GameObject optionsContainer;
    [SerializeField] private Button[] optionButtons; // 2 Buttons
    [SerializeField] private Image[] optionImages;
    [SerializeField] private TextMeshProUGUI[] optionTexts;

    [Header("Audio Controls")]
    [SerializeField] private Button replayAudioBtn;

    [Header("Audio References")]
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private AudioClip recapAudio;

    [Header("Feedback Banner")]
    [SerializeField] private GameObject feedbackBanner;
    [SerializeField] private TextMeshProUGUI feedbackTextTMP;

    [Header("Results & Retry Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button returnHubBtn;

    private int currentStepIndex = 0;
    private int correctStepsCount = 0;
    private int attemptsOnCurrentStep = 0;
    private bool isProcessingTurn = false;

    private readonly Color defaultOptionColor = new Color(0.12f, 0.42f, 0.78f, 0.95f);
    private readonly Color correctOptionColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private readonly Color wrongOptionColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Roleplay;
        base.Awake();

        AutoBindReferences();
        InitStepsIfEmpty();
        WireEventListeners();
    }

    protected override void Start() {
        topic = Masters_Topic.Roleplay;
        AutoBindReferences();
        InitStepsIfEmpty();
        WireEventListeners();
        EnsureNextAndBackButtonWired();
        RestartRoleplay();
    }

    private void WireEventListeners() {
        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(ReplayNpcLine);
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int optIdx = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(optIdx));
                }
            }
        }

        if (npcCloud != null) {
            Button npcBtn = npcCloud.GetComponent<Button>();
            if (npcBtn != null) {
                npcBtn.onClick.RemoveAllListeners();
                npcBtn.onClick.AddListener(ReplayNpcLine);
            }
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartRoleplay);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(OnReturnHubClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    public void RestartRoleplay() {
        currentStepIndex = 0;
        correctStepsCount = 0;
        attemptsOnCurrentStep = 0;
        isProcessingTurn = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        if (headerTMP != null) headerTMP.gameObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);

        UpdateHUD();
        StartCoroutine(PlayIntroThenStart());
    }

    private IEnumerator PlayIntroThenStart() {
        if (npcCloud != null) npcCloud.SetActive(false);
        if (studentCloud != null) studentCloud.SetActive(false);
        if (optionsContainer != null) optionsContainer.SetActive(false);

        AudioClip introClip = introAudio != null ? introAudio : narratorSpeech;
        if (introClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introClip);
            yield return new WaitForSeconds(introClip.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        if (optionsContainer != null) optionsContainer.SetActive(true);
        ShowStep(0);
    }

    private void ShowStep(int stepIndex) {
        if (steps == null || steps.Length == 0) return;
        currentStepIndex = Mathf.Clamp(stepIndex, 0, steps.Length - 1);
        attemptsOnCurrentStep = 0;
        isProcessingTurn = false;

        ShoppingTime_RoleplayRP01Step step = steps[currentStepIndex];

        if (titleTMP != null) titleTMP.text = "ON STAGE — BUYING THE BLUE T-SHIRT";
        if (subtitleTMP != null) subtitleTMP.text = "Choose the fitting line to buy the blue T-shirt.";

        UpdateHUD();

        // Show NPC speech bubble with high-contrast formatting
        if (npcCloud != null) {
            npcCloud.SetActive(true);
            npcCloud.transform.DOKill();
            npcCloud.transform.localScale = Vector3.zero;
            npcCloud.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
        if (npcDialogueTMP != null) {
            npcDialogueTMP.text = $"<color=#0B5394><b>Shop Keeper:</b></color>\n\"{step.npcOpeningLine}\"";
            npcDialogueTMP.color = new Color(0.1f, 0.15f, 0.25f, 1f); // Crisp Dark Navy on White Bubble
            npcDialogueTMP.enableAutoSizing = true;
            npcDialogueTMP.fontSizeMin = 16f;
            npcDialogueTMP.fontSizeMax = 24f;
            npcDialogueTMP.alignment = TextAlignmentOptions.Center;
            npcDialogueTMP.enableWordWrapping = true;
            npcDialogueTMP.ForceMeshUpdate();
        }

        // Hide student speech bubble initially
        if (studentCloud != null) {
            studentCloud.SetActive(false);
        }

        // Play NPC voiceover (Female Shopkeeper)
        PlayNpcLine();

        // Populate and enable Option buttons
        SetupOptions(step);
    }

    private void SetupOptions(ShoppingTime_RoleplayRP01Step step) {
        if (optionsContainer != null) {
            optionsContainer.SetActive(true);
            Transform opt3 = optionsContainer.transform.Find("option3");
            if (opt3 != null) opt3.gameObject.SetActive(false);
        }

        if (optionButtons != null && step.optionChoices != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < step.optionChoices.Length) {
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;

                        if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                            optionTexts[i].text = step.optionChoices[i];
                            optionTexts[i].color = Color.white;
                            optionTexts[i].enableAutoSizing = true;
                            optionTexts[i].fontSizeMin = 14f;
                            optionTexts[i].fontSizeMax = 22f;
                            optionTexts[i].alignment = TextAlignmentOptions.Center;
                            optionTexts[i].enableWordWrapping = true;
                            optionTexts[i].margin = new Vector4(12, 6, 12, 6);
                            optionTexts[i].ForceMeshUpdate();
                        }

                        if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                            optionImages[i].color = defaultOptionColor;
                            optionImages[i].transform.localScale = Vector3.one;
                        }
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    public void OnOptionSelected(int optionIndex) {
        if (isProcessingTurn) return;
        if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Length) return;

        ShoppingTime_RoleplayRP01Step step = steps[currentStepIndex];
        attemptsOnCurrentStep++;

        bool isCorrect = (optionIndex == step.correctOptionIndex);
        StartCoroutine(HandleOptionSelectedCoroutine(isCorrect, optionIndex, step));
    }

    private IEnumerator HandleOptionSelectedCoroutine(bool isCorrect, int optionIndex, ShoppingTime_RoleplayRP01Step step) {
        isProcessingTurn = true;
        EnableOptionButtons(false);

        if (isCorrect) {
            if (attemptsOnCurrentStep == 1) {
                correctStepsCount++;
            }
            UpdateHUD();

            // Highlight chosen option green
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctOptionColor;
                optionImages[optionIndex].transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
            }

            // Show Student speech bubble with styled formatting
            if (studentCloud != null) {
                studentCloud.SetActive(true);
                studentCloud.transform.DOKill();
                studentCloud.transform.localScale = Vector3.zero;
                studentCloud.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
            if (studentDialogueTMP != null) {
                studentDialogueTMP.text = $"<color=#D35400><b>Joy:</b></color>\n\"{step.studentFittingAnswer}\"";
                studentDialogueTMP.color = new Color(0.1f, 0.15f, 0.25f, 1f);
                studentDialogueTMP.enableAutoSizing = true;
                studentDialogueTMP.fontSizeMin = 16f;
                studentDialogueTMP.fontSizeMax = 24f;
                studentDialogueTMP.alignment = TextAlignmentOptions.Center;
                studentDialogueTMP.enableWordWrapping = true;
                studentDialogueTMP.ForceMeshUpdate();
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Play student voice line (Male Joy)
            PlayStudentLine();

            float waitDuration = (step.studentAudio != null && step.studentAudio.length > 0) ? step.studentAudio.length + 0.6f : 2.5f;
            yield return new WaitForSeconds(waitDuration);

            // Advance to next step or end
            if (currentStepIndex + 1 < steps.Length) {
                ShowStep(currentStepIndex + 1);
            } else {
                EndRoleplay();
            }
        } else {
            // Incorrect choice
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = wrongOptionColor;
                optionImages[optionIndex].transform.DOShakePosition(0.4f, 8f, 15, 90, false, true);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            ShowFeedback("That sounds a bit too blunt or vague! Try the polite, fitting response.", 1.8f);
            yield return new WaitForSeconds(1.5f);

            // Reset option colors and re-enable for retry
            ResetOptionColors();
            EnableOptionButtons(true);
            isProcessingTurn = false;
        }
    }

    public void PlayNpcLine() {
        if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Length) return;
        AudioClip clip = steps[currentStepIndex].npcAudio;
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }
    }

    public void PlayStudentLine() {
        if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Length) return;
        AudioClip clip = steps[currentStepIndex].studentAudio;
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }
    }

    public void ReplayNpcLine() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        PlayNpcLine();
    }

    private void ResetOptionColors() {
        if (optionImages == null) return;
        for (int i = 0; i < optionImages.Length; i++) {
            if (optionImages[i] != null) {
                optionImages[i].color = defaultOptionColor;
                optionImages[i].transform.localScale = Vector3.one;
            }
        }
    }

    private void EnableOptionButtons(bool enable) {
        if (optionButtons == null) return;
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] != null) optionButtons[i].interactable = enable;
        }
    }

    private void ShowFeedback(string msg, float duration) {
        if (feedbackBanner == null) return;
        feedbackBanner.SetActive(true);
        if (feedbackTextTMP != null) feedbackTextTMP.text = msg;

        feedbackBanner.transform.DOKill();
        feedbackBanner.transform.localScale = Vector3.zero;
        feedbackBanner.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);

        StartCoroutine(HideFeedbackCoroutine(duration));
    }

    private IEnumerator HideFeedbackCoroutine(float delay) {
        yield return new WaitForSeconds(delay);
        if (feedbackBanner != null) {
            feedbackBanner.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => feedbackBanner.SetActive(false));
        }
    }

    private void EndRoleplay() {
        isProcessingTurn = false;

        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        bool passed = (correctStepsCount >= 4);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "ON STAGE COMPLETE!" : "ROLEPLAY COMPLETE!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"You completed {correctStepsCount}/5 steps on first attempt! (Target: 4/5)";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed 
                    ? "Great job! You purchased the blue T-shirt politely and got a 10% discount!"
                    : "Try again to get at least 4 out of 5 fitting lines right on your first attempt!";
            }

            if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(passed);
            if (retryBtn != null) retryBtn.gameObject.SetActive(!passed || correctStepsCount < 5);
        }

        if (recapAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
        }

        if (passed && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    public void OnReturnHubClicked() {
        topic = Masters_Topic.Roleplay;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Roleplay;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private void UpdateHUD() {
        if (progressTMP != null) progressTMP.text = $"Step {currentStepIndex + 1}/5";
        if (scoreTMP != null) scoreTMP.text = $"Score: {correctStepsCount}/5";
    }

    public void InitStepsIfEmpty() {
        string audioDir = "Assets/Audio/2B/8_Shopping_Time/Roleplay/";

        if (introAudio == null) {
#if UNITY_EDITOR
            introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_intro.mp3");
#endif
            if (narratorSpeech == null) narratorSpeech = introAudio;
        }

        if (recapAudio == null) {
#if UNITY_EDITOR
            recapAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_recap.mp3");
#endif
        }

        if (steps == null || steps.Length != 5) {
            steps = new ShoppingTime_RoleplayRP01Step[] {
                new ShoppingTime_RoleplayRP01Step {
                    stepId = 1,
                    stepTitle = "Step 1: Offering Help",
                    npcSpeakerName = "Shop Keeper",
                    npcOpeningLine = "Excuse me! May I help you?",
                    studentFittingAnswer = "Yes, I want a T-Shirt. Can you show me one?",
                    studentDistractorLine = "Give me shirt.",
                    correctOptionIndex = 0,
                    optionChoices = new string[] {
                        "Yes, I want a T-Shirt. Can you show me one?",
                        "Give me shirt."
                    },
#if UNITY_EDITOR
                    npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s01_npc.mp3"),
                    studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s01_student.mp3")
#endif
                },
                new ShoppingTime_RoleplayRP01Step {
                    stepId = 2,
                    stepTitle = "Step 2: Choosing the Item",
                    npcSpeakerName = "Shop Keeper",
                    npcOpeningLine = "Here are a few. Which one would you like to take?",
                    studentFittingAnswer = "The blue one, just hanging at the side of the red one. How much does it cost?",
                    studentDistractorLine = "That one. How much is it?",
                    correctOptionIndex = 1,
                    optionChoices = new string[] {
                        "That one. How much is it?",
                        "The blue one, just hanging at the side of the red one. How much does it cost?"
                    },
#if UNITY_EDITOR
                    npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s02_npc.mp3"),
                    studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s02_student.mp3")
#endif
                },
                new ShoppingTime_RoleplayRP01Step {
                    stepId = 3,
                    stepTitle = "Step 3: Price & 10% Discount",
                    npcSpeakerName = "Shop Keeper",
                    npcOpeningLine = "Well, it costs four hundred rupees. You will get 10 % discount. That means forty rupees off.",
                    studentFittingAnswer = "Alright! I will buy that.",
                    studentDistractorLine = "Too expensive! Lower it.",
                    correctOptionIndex = 0,
                    optionChoices = new string[] {
                        "Alright! I will buy that.",
                        "Too expensive! Lower it."
                    },
#if UNITY_EDITOR
                    npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s03_npc.mp3"),
                    studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s03_student.mp3")
#endif
                },
                new ShoppingTime_RoleplayRP01Step {
                    stepId = 4,
                    stepTitle = "Step 4: Final Payment",
                    npcSpeakerName = "Shop Keeper",
                    npcOpeningLine = "Three hundred and sixty rupees please.",
                    studentFittingAnswer = "Here it is. Thank you.",
                    studentDistractorLine = "Take money.",
                    correctOptionIndex = 1,
                    optionChoices = new string[] {
                        "Take money.",
                        "Here it is. Thank you."
                    },
#if UNITY_EDITOR
                    npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s04_npc.mp3"),
                    studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s04_student.mp3")
#endif
                },
                new ShoppingTime_RoleplayRP01Step {
                    stepId = 5,
                    stepTitle = "Step 5: Completion & Farewell",
                    npcSpeakerName = "Shop Keeper",
                    npcOpeningLine = "You are welcome.",
                    studentFittingAnswer = "Thank you, have a good day!",
                    studentDistractorLine = "Whatever.",
                    correctOptionIndex = 0,
                    optionChoices = new string[] {
                        "Thank you, have a good day!",
                        "Whatever."
                    },
#if UNITY_EDITOR
                    npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s05_npc.mp3"),
                    studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_rp01_s05_student.mp3")
#endif
                }
            };
        }
    }

    public void AutoBindReferences() {
        if (titleTMP == null) {
            Transform t = transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("CommonHUD/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP == null) {
            Transform t = transform.Find("Select an appropriate response:") ?? transform.Find("Subtitle") ?? transform.Find("Instruction") ?? transform.Find("HeaderContainer/Subtitle") ?? transform.Find("CommonHUD/Subtitle");
            if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP == null) {
            Transform t = transform.Find("Header") ?? transform.Find("BranchHeader") ?? transform.Find("HeaderContainer/Header");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP == null) {
            Transform t = transform.Find("ProgressCountTMP") ?? transform.Find("ProgressTMP") ?? transform.Find("progression count") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP") ?? transform.Find("CommonHUD/ProgressTMP");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (scoreTMP == null) {
            Transform t = transform.Find("ScoreTMP") ?? transform.Find("Score") ?? transform.Find("HeaderContainer/ScoreTMP") ?? transform.Find("CommonHUD/ScoreTMP");
            if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
        }

        // Characters & speech clouds
        if (npcAndStudentGameObject == null) {
            Transform t = transform.Find("NPCAndStudent") ?? transform.Find("Characters") ?? transform.Find("StageSet");
            if (t != null) npcAndStudentGameObject = t.gameObject;
        }

        if (npcCloud == null) {
            Transform t = transform.Find("NPCCloud") ?? transform.Find("DoctorCloud") ?? transform.Find("ShopkeeperCloud") ?? transform.Find("NPCAndStudent/NPCCloud");
            if (t != null) npcCloud = t.gameObject;
        }
        if (npcDialogueTMP == null && npcCloud != null) {
            npcDialogueTMP = npcCloud.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (studentCloud == null) {
            Transform t = transform.Find("StudentCloud") ?? transform.Find("HenryCloud") ?? transform.Find("JoyCloud") ?? transform.Find("NPCAndStudent/StudentCloud");
            if (t != null) studentCloud = t.gameObject;
        }
        if (studentDialogueTMP == null && studentCloud != null) {
            studentDialogueTMP = studentCloud.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Options Container
        if (optionsContainer == null) {
            Transform t = transform.Find("optionscontainer") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid") ?? transform.Find("ResponsesContainer");
            if (t != null) optionsContainer = t.gameObject;
        }

        if (optionsContainer != null) {
            Transform opt3 = optionsContainer.transform.Find("option3");
            if (opt3 != null) opt3.gameObject.SetActive(false);

            Transform opt1Tr = optionsContainer.transform.Find("option1");
            Transform opt2Tr = optionsContainer.transform.Find("option2");

            List<Button> btns = new List<Button>();
            List<Image> imgs = new List<Image>();
            List<TextMeshProUGUI> txts = new List<TextMeshProUGUI>();

            if (opt1Tr != null) {
                Button b1 = opt1Tr.GetComponent<Button>();
                Image im1 = opt1Tr.GetComponent<Image>();
                TextMeshProUGUI tx1 = opt1Tr.GetComponentInChildren<TextMeshProUGUI>(true);
                if (b1 != null) btns.Add(b1);
                if (im1 != null) imgs.Add(im1);
                if (tx1 != null) txts.Add(tx1);
            }

            if (opt2Tr != null) {
                Button b2 = opt2Tr.GetComponent<Button>();
                Image im2 = opt2Tr.GetComponent<Image>();
                TextMeshProUGUI tx2 = opt2Tr.GetComponentInChildren<TextMeshProUGUI>(true);
                if (b2 != null) btns.Add(b2);
                if (im2 != null) imgs.Add(im2);
                if (tx2 != null) txts.Add(tx2);
            }

            if (btns.Count == 2) optionButtons = btns.ToArray();
            if (imgs.Count == 2) optionImages = imgs.ToArray();
            if (txts.Count == 2) optionTexts = txts.ToArray();
        }

        // Replay Audio Button
        if (replayAudioBtn == null) {
            Transform t = transform.Find("ReplayAudioBtn") ?? transform.Find("SpeakerButton") ?? transform.Find("AudioButton");
            if (t != null) replayAudioBtn = t.GetComponent<Button>();
        }

        // Feedback Banner
        if (feedbackBanner == null) {
            Transform t = transform.Find("FeedbackBanner") ?? transform.Find("Feedback");
            if (t != null) feedbackBanner = t.gameObject;
        }
        if (feedbackTextTMP == null && feedbackBanner != null) {
            feedbackTextTMP = feedbackBanner.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Result Panel
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
