using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Subclass Controller for Unit 7 (Collocations) Roleplay Lesson Two (RP02 Free Scene — Tell a Mini Story with Collocations).
/// 3 Everyday Scenes:
/// Scene A (A Busy Morning): get ready, get dressed, catch a bus
/// Scene B (A Rainy Day): catch a cold, get well soon, save energy
/// Scene C (A School Project): bright idea, get permission, get started
/// Features:
/// - Interactive Collocation Rail (3 highlighted chips)
/// - Student TMP_InputField for writing 3-sentence mini story
/// - Robust CollocationValidator scanning student text (case-insensitive, punctuation-tolerant)
/// - Visual DOTween animations on successful match
/// - Edge-TTS ARIA scene intro and readback audio voiceovers
/// - RolePlay-RP02 sub-flag completion tracking
/// </summary>
public class Masters_Collocations_Roleplay_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class RP02SceneData {
        public string sceneId;                  // "A", "B", "C"
        public string sceneTitle;               // "A Busy Morning", etc.
        public string situationPrompt;          // Background prompt
        public string[] requiredCollocations;   // 3 target collocations
        public AudioClip setupAudio;            // ARIA intro audio
        public AudioClip readbackAudio;         // ARIA readback audio
        public GameObject sceneContainer;       // Scene backdrop & graphic elements
    }

    [Header("RP02 Scene Data (3 Scenes)")]
    [SerializeField] private RP02SceneData[] scenes;

    [Header("UI Header & Indicators")]
    [SerializeField] private TextMeshProUGUI rp02TitleTMP;
    [SerializeField] private TextMeshProUGUI progressIndicatorTMP;
    [SerializeField] private TextMeshProUGUI situationPromptTMP;

    [Header("Collocation Rail UI")]
    [SerializeField] private Transform collocationRailParent;
    [SerializeField] private TextMeshProUGUI[] collocationChipTMPs; // 3 chip texts
    [SerializeField] private Image[] collocationChipImages;        // 3 chip background colors

    [Header("Student Input Area")]
    [SerializeField] private TMP_InputField storyInputField;
    [SerializeField] private Button submitButton;

    [Header("Feedback & Prompt UI")]
    [SerializeField] private TextMeshProUGUI feedbackBannerTMP;
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnHubButton;

    [Header("Audio References")]
    [SerializeField] private AudioClip sfxMagnetSnap;
    [SerializeField] private AudioClip sfxCurtain;

    // Runtime state variables
    private int currentSceneIndex = 0;
    private int completedScenesCount = 0;
    private int currentRetryCount = 0;
    private bool isAnsweringActive = false;

    private const int TOTAL_SCENES = 3;

    protected virtual void OnEnable() {
        // Prevent STT subscriptions
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeScenesData();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Roleplay;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeScenesData();
        UpdateTitleAndUIComponents();
        SetupButtonListeners();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (resultPopup != null) resultPopup.SetActive(false);

        StartCoroutine(InitializeRP02Routine());
    }

    private void DeactivateObsoleteBaseUI() {
        Transform skipTrans = transform.Find("SkipButton");
        if (skipTrans != null) skipTrans.gameObject.SetActive(false);

        Transform contTrans = transform.Find("Continue");
        if (contTrans != null) contTrans.gameObject.SetActive(false);

        Transform debugTrans = transform.Find("DebugText");
        if (debugTrans != null) debugTrans.gameObject.SetActive(false);

        Transform heading = transform.Find("Heading") ?? transform.Find("Header");
        if (heading != null) heading.gameObject.SetActive(false);
    }

    public void InitializeScenesData() {
        string audioDir = "Assets/Audio/2A/7_Collocations/Roleplay/RP02/";

        scenes = new RP02SceneData[] {
            // Scene A: A Busy Morning
            new RP02SceneData {
                sceneId = "A",
                sceneTitle = "Scene A: A Busy Morning",
                situationPrompt = "It's a busy morning. Write a short story (approx. 3 sentences) using the three collocations below!",
                requiredCollocations = new string[] { "get ready", "get dressed", "catch a bus" },
                #if UNITY_EDITOR
                setupAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Welcome to Scene A A Busy Morning Use get ready get dressed and catch a bus to tell your story.mp3"),
                #endif
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "It is a busy morning I get ready and get dressed quickly then run to catch a bus.mp3"),
                #endif
                sceneContainer = GetSceneContainerObj("SceneContainer_A")
            },
            // Scene B: A Rainy Day
            new RP02SceneData {
                sceneId = "B",
                sceneTitle = "Scene B: A Rainy Day",
                situationPrompt = "It's a rainy day. Write a short story using the three collocations below!",
                requiredCollocations = new string[] { "catch a cold", "get well soon", "save energy" },
                #if UNITY_EDITOR
                setupAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Welcome to Scene B A Rainy Day Use catch a cold get well soon and save energy to tell your story.mp3"),
                #endif
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "My friend was out in the rain and did catch a cold I told him to get well soon and we stayed in to save energy.mp3"),
                #endif
                sceneContainer = GetSceneContainerObj("SceneContainer_B")
            },
            // Scene C: A School Project
            new RP02SceneData {
                sceneId = "C",
                sceneTitle = "Scene C: A School Project",
                situationPrompt = "You have a school project. Write a short story using the three collocations below!",
                requiredCollocations = new string[] { "bright idea", "get permission", "get started" },
                #if UNITY_EDITOR
                setupAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Welcome to Scene C A School Project Use bright idea get permission and get started to tell your story.mp3"),
                #endif
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "I had a bright idea for our project We had to get permission from our teacher so we could get started.mp3"),
                #endif
                sceneContainer = GetSceneContainerObj("SceneContainer_C")
            }
        };
    }

    private GameObject GetSceneContainerObj(string name) {
        Transform t = transform.Find(name) ?? transform.Find($"StageContainer/{name}");
        return t != null ? t.gameObject : null;
    }

    private IEnumerator InitializeRP02Routine() {
        currentSceneIndex = 0;
        completedScenesCount = 0;

        UpdateScoreUI();
        if (resultPopup != null) resultPopup.SetActive(false);

        yield return StartCoroutine(LoadSceneRoutine(0));
    }

    private IEnumerator LoadSceneRoutine(int index) {
        if (scenes == null || index < 0 || index >= scenes.Length) yield break;

        currentSceneIndex = index;
        currentRetryCount = 0;
        isAnsweringActive = true;

        RP02SceneData currentScene = scenes[index];

        UpdateProgressUI();
        ShowFeedback($"Write a story using: {string.Join(" • ", currentScene.requiredCollocations)}", true);

        // Toggle Scene Visual Containers
        for (int i = 0; i < scenes.Length; i++) {
            if (scenes[i].sceneContainer != null) {
                scenes[i].sceneContainer.SetActive(i == index);
            }
        }

        // Display Situation & Collocation Rail Chips
        if (situationPromptTMP != null) {
            situationPromptTMP.text = $"{currentScene.sceneTitle}\n{currentScene.situationPrompt}";
        }

        SetupCollocationRail(currentScene.requiredCollocations);

        // Reset Input Field
        if (storyInputField != null) {
            storyInputField.text = "";
            storyInputField.interactable = true;
        }

        if (submitButton != null) {
            submitButton.interactable = true;
        }

        // Play ARIA Scene Setup Intro Audio
        if (currentScene.setupAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(currentScene.setupAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(0.4f);
        }
    }

    private void SetupCollocationRail(string[] collocations) {
        Color[] defaultChipColors = new Color[] {
            new Color(0.95f, 0.65f, 0.15f, 1f), // Chip 1: Gold / Yellow-Orange
            new Color(0.12f, 0.40f, 0.85f, 1f), // Chip 2: Royal Blue
            new Color(0.88f, 0.25f, 0.25f, 1f)  // Chip 3: Vibrant Red
        };

        for (int i = 0; i < 3; i++) {
            if (collocationChipTMPs != null && i < collocationChipTMPs.Length && collocationChipTMPs[i] != null) {
                if (i < collocations.Length) {
                    collocationChipTMPs[i].gameObject.SetActive(true);
                    collocationChipTMPs[i].text = collocations[i];
                    collocationChipTMPs[i].color = Color.white;
                } else {
                    collocationChipTMPs[i].gameObject.SetActive(false);
                }
            }

            if (collocationChipImages != null && i < collocationChipImages.Length && collocationChipImages[i] != null) {
                collocationChipImages[i].gameObject.SetActive(i < collocations.Length);
                collocationChipImages[i].color = defaultChipColors[i % defaultChipColors.Length];
            }
        }
    }

    public void OnSubmitStoryClicked() {
        if (!isAnsweringActive || scenes == null || currentSceneIndex >= scenes.Length) return;

        RP02SceneData currentScene = scenes[currentSceneIndex];
        string userText = storyInputField != null ? storyInputField.text : "";

        var (isPass, detected, missing) = CollocationValidator.Validate(userText, currentScene.requiredCollocations);

        if (isPass) {
            OnStoryPassed(currentScene, detected);
        } else {
            OnStoryFailed(currentScene, detected, missing);
        }
    }

    private void OnStoryPassed(RP02SceneData scene, List<string> detected) {
        isAnsweringActive = false;
        completedScenesCount++;
        UpdateScoreUI();

        if (storyInputField != null) storyInputField.interactable = false;
        if (submitButton != null) submitButton.interactable = false;

        // Highlight all chips green
        Color successColor = new Color(0.13f, 0.77f, 0.36f, 1f);
        for (int i = 0; i < 3; i++) {
            if (collocationChipImages != null && i < collocationChipImages.Length && collocationChipImages[i] != null) {
                collocationChipImages[i].color = successColor;
                collocationChipImages[i].transform.DOKill(true);
                collocationChipImages[i].transform.DOPunchScale(Vector3.one * 0.18f, 0.3f);
            }
        }

        // Play Magnet Snap SFX & Correct Sound
        if (sfxMagnetSnap != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxMagnetSnap);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        ShowFeedback($"Great story! All 3 collocations used: {string.Join(", ", scene.requiredCollocations)}", true);

        // Animate Scene Graphics
        if (scene.sceneContainer != null) {
            scene.sceneContainer.transform.DOKill(true);
            scene.sceneContainer.transform.DOPunchScale(Vector3.one * 0.08f, 0.5f, 5, 0.5f);
        }

        StartCoroutine(AdvanceSceneAfterReadbackRoutine(scene));
    }

    private void OnStoryFailed(RP02SceneData scene, List<string> detected, List<string> missing) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        // Highlight missing chips orange/red
        Color missingColor = new Color(0.92f, 0.32f, 0.2f, 1f);
        Color detectedColor = new Color(0.13f, 0.77f, 0.36f, 1f);

        for (int i = 0; i < 3; i++) {
            if (i < scene.requiredCollocations.Length) {
                string col = scene.requiredCollocations[i];
                bool isDet = detected.Contains(col);

                if (collocationChipImages != null && i < collocationChipImages.Length && collocationChipImages[i] != null) {
                    collocationChipImages[i].color = isDet ? detectedColor : missingColor;
                    if (!isDet) {
                        collocationChipImages[i].transform.DOKill(true);
                        collocationChipImages[i].transform.DOShakePosition(0.4f, 12f, 10, 90f);
                    }
                }
            }
        }

        currentRetryCount++;

        if (currentRetryCount == 1) {
            // First retry allowed
            ShowFeedback($"Try to include: {string.Join(", ", missing)}", false);
        } else {
            // Second attempt: show gentle hint and allow student to proceed
            ShowFeedback($"Missing: {string.Join(", ", missing)}. Make sure all 3 are included!", false);
        }
    }

    private IEnumerator AdvanceSceneAfterReadbackRoutine(RP02SceneData scene) {
        // Play Readback Audio
        if (scene.readbackAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(scene.readbackAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.8f);
        }

        if (sfxCurtain != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxCurtain);
        }

        int nextIndex = currentSceneIndex + 1;
        if (nextIndex < scenes.Length) {
            yield return StartCoroutine(LoadSceneRoutine(nextIndex));
        } else {
            EndRP02Activity();
        }
    }

    private void EndRP02Activity() {
        isAnsweringActive = false;

        bool passed = (completedScenesCount >= 1); // Pass threshold: at least 1 scene completed (encouraged all 3)

        if (passed) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Roleplay);
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        ShowResultPopup(passed);
    }

    private void ShowResultPopup(bool passed) {
        if (resultPopup != null) {
            resultPopup.SetActive(true);
            resultPopup.transform.DOKill();
            resultPopup.transform.localScale = Vector3.zero;
            resultPopup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = passed ? "RP02 COMPLETED!" : "TRY AGAIN!";
            resultTitleTMP.color = passed ? new Color(0.13f, 0.77f, 0.36f) : new Color(0.85f, 0.2f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Completed Scenes: {completedScenesCount}/{TOTAL_SCENES}\n{(passed ? "Roleplay Branch Completed (RP01 & RP02 Unlocked)!" : "Complete at least 1 scene to pass!")}";
        }
    }

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackBannerTMP != null) {
            feedbackBannerTMP.gameObject.SetActive(true);
            feedbackBannerTMP.text = msg;
            feedbackBannerTMP.color = isSuccess ? new Color(0.9f, 0.95f, 1f) : new Color(1f, 0.35f, 0.35f);
        }
    }

    private void UpdateProgressUI() {
        if (progressIndicatorTMP != null) {
            progressIndicatorTMP.text = $"Scene {currentSceneIndex + 1}/{TOTAL_SCENES}";
        }
    }

    private void UpdateScoreUI() {
        // Score indicator if needed
    }

    private void UpdateTitleAndUIComponents() {
        if (rp02TitleTMP != null) {
            rp02TitleTMP.gameObject.SetActive(true);
            rp02TitleTMP.text = "RP02 Free Scene — Tell a Mini Story with Collocations";
            RectTransform rt = rp02TitleTMP.GetComponent<RectTransform>();
            if (rt != null) {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(1000f, 60f);
                rt.anchoredPosition = new Vector2(0f, -40f);
            }
        }
    }

    private void SetupButtonListeners() {
        if (submitButton != null) {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitStoryClicked);
        }

        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => StartCoroutine(InitializeRP02Routine()));
        }

        if (returnHubButton != null) {
            returnHubButton.onClick.RemoveAllListeners();
            returnHubButton.onClick.AddListener(ReturnToHub);
        }
    }

    protected override void OnNextButtonClicked() {
        ReturnToHub();
    }

    public void ReturnToHub() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Roleplay);
        }
    }

    private void AutoFindUIReferences() {
        if (rp02TitleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) rp02TitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (progressIndicatorTMP == null) {
            Transform t = transform.Find("RoundProgressText") ?? transform.Find("ProgressIndicator");
            if (t != null) progressIndicatorTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (situationPromptTMP == null) {
            Transform t = transform.Find("SituationPromptText");
            if (t != null) situationPromptTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (collocationRailParent == null) {
            Transform t = transform.Find("CollocationRail");
            if (t != null) collocationRailParent = t;
        }

        if (collocationChipTMPs == null || collocationChipTMPs.Length < 3) {
            collocationChipTMPs = new TextMeshProUGUI[3];
            collocationChipImages = new Image[3];

            if (collocationRailParent != null) {
                for (int i = 0; i < 3; i++) {
                    Transform chipTrans = collocationRailParent.Find($"Chip_{i + 1}");
                    if (chipTrans != null) {
                        collocationChipTMPs[i] = chipTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                        collocationChipImages[i] = chipTrans.GetComponent<Image>();
                    }
                }
            }
        }

        if (storyInputField == null) {
            Transform t = transform.Find("StoryInputField");
            if (t != null) storyInputField = t.GetComponent<TMP_InputField>();
        }

        if (storyInputField != null && storyInputField.textViewport == null) {
            RectTransform rt = storyInputField.GetComponent<RectTransform>();
            Transform ta = storyInputField.transform.Find("Text Area");
            if (ta != null) rt = ta.GetComponent<RectTransform>();
            storyInputField.textViewport = rt;
        }

        if (submitButton == null) {
            Transform t = transform.Find("SubmitButton");
            if (t != null) submitButton = t.GetComponent<Button>();
        }

        if (feedbackBannerTMP == null) {
            Transform t = transform.Find("FeedbackText");
            if (t != null) feedbackBannerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (resultPopup == null) {
            Transform t = transform.Find("ResultPopup") ?? transform.Find("ResultPanel");
            if (t != null) resultPopup = t.gameObject;
        }

        if (resultPopup != null) {
            Button[] resBtns = resultPopup.GetComponentsInChildren<Button>(true);
            foreach (var b in resBtns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (retryButton == null && (bName.Contains("retry") || bName.Contains("again"))) retryButton = b;
                if (returnHubButton == null && (bName.Contains("hub") || bName.Contains("home") || bName.Contains("continue"))) returnHubButton = b;
            }
        }
    }
}

/// <summary>
/// Reusable Collocation Validator scanning student text for target collocations.
/// Checks case-insensitively, normalizes punctuation and spacing.
/// </summary>
public static class CollocationValidator {
    public static (bool isPass, List<string> detected, List<string> missing) Validate(string input, string[] required) {
        List<string> detected = new List<string>();
        List<string> missing = new List<string>();

        if (required == null || required.Length == 0) {
            return (true, detected, missing);
        }

        if (string.IsNullOrWhiteSpace(input)) {
            return (false, detected, new List<string>(required));
        }

        string cleanInput = NormalizeText(input);

        foreach (var req in required) {
            string cleanReq = NormalizeText(req);
            if (cleanInput.Contains(cleanReq)) {
                detected.Add(req);
            } else {
                missing.Add(req);
            }
        }

        bool isPass = (detected.Count == required.Length);
        return (isPass, detected, missing);
    }

    private static string NormalizeText(string text) {
        if (string.IsNullOrEmpty(text)) return "";
        string lower = text.ToLowerInvariant();

        char[] chars = lower.ToCharArray();
        for (int i = 0; i < chars.Length; i++) {
            if (char.IsPunctuation(chars[i])) {
                chars[i] = ' ';
            }
        }

        string cleaned = new string(chars);
        string[] words = cleaned.Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words);
    }
}