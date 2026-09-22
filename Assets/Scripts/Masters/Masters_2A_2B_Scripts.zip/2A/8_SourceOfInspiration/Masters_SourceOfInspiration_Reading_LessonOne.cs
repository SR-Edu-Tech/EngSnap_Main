using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Core Game Manager for Unit 8 (Source of Inspiration / Tongue Twisters) Reading Lesson One:
/// R01 — Read Along: Spot "wish" and "witch".
/// Features:
/// 1. Complete Read-Along twister in big, friendly typography.
/// 2. Audio-sync bouncing word highlight moving from word to word.
/// 3. Round 1: Tap every "wish" and "wishes" (11 target words) with live counter (WISH FOUND: X/11).
/// 4. Round 2: Tap the single "witch" (1 target word) with distinct visual state (WITCH FOUND: 1/1).
/// 5. Post-round Contrast Audio ("wish -> witch") highlighting words back-to-back.
/// 6. Gentle, forgiving experience (no timer, no lives, no failure punishment).
/// 7. Fully responsive layout with word-wrapping UI buttons and Inspector text customization.
/// </summary>
public class Masters_SourceOfInspiration_Reading_LessonOne : Masters_Lesson {

    public enum WordTargetType {
        Normal = 0,
        Wish = 1,
        Witch = 2
    }

    [System.Serializable]
    public class InteractiveWordData {
        public int id;
        public string originalText;
        public string cleanWord;
        public WordTargetType targetType;
        public int targetRound;
        public bool isFound;
        public float startTime;
        public float endTime;
        public GameObject wordObj;
        public Button wordButton;
        public TextMeshProUGUI wordTMP;
        public Image wordBgImage;
    }

    [Header("UI Header & Titles")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI roundInstructionTMP;

    [Header("Progress Counters UI")]
    [SerializeField] private GameObject countersContainer;
    [SerializeField] private TextMeshProUGUI wishCounterTMP;
    [SerializeField] private TextMeshProUGUI witchCounterTMP;
    [SerializeField] private Image wishProgressFillImage;
    [SerializeField] private Image witchProgressFillImage;

    [Header("Reading Area & Word Container")]
    [SerializeField] private RectTransform readingContainer;
    [SerializeField] private GameObject wordPrefab;

    [Header("Completion & Audio Banner")]
    [SerializeField] private GameObject completionBannerObj;
    [SerializeField] private TextMeshProUGUI completionBannerTMP;
    [SerializeField] private GameObject audioContrastBannerObj;
    [SerializeField] private TextMeshProUGUI audioContrastTMP;

    [Header("Customizable Twister Text")]
    [TextArea(3, 6)]
    [SerializeField]
    private string customTwisterText = "\"I wish to wish the wish you wish to wish, but if you wish the witch wishes, I won't wish the wish you wish to wish.\"";

    [Header("Audio Asset References")]
    [SerializeField] private AudioClip voR01AriaIntro;
    [SerializeField] private AudioClip voR01FullTwister;
    [SerializeField] private AudioClip voR01Contrast;
    [SerializeField] private AudioClip sfxTapFind;

    private List<InteractiveWordData> wordDataList = new List<InteractiveWordData>();
    private int currentRound = 1; // 1: Wish hunting, 2: Witch hunting, 3: Contrast & Done
    private int wishFoundCount = 0;
    private int totalWishTargets = 0;
    private bool witchFound = false;
#pragma warning disable 0414
    private bool isReadAlongPlaying = false;
#pragma warning restore 0414
    private Coroutine readAlongCoroutine;
    private Coroutine contrastAudioCoroutine;

    // Visual Style Colors
    private Color colorNormalWord = new Color(0.95f, 0.98f, 1.0f);
    private Color colorNormalBg = new Color(0.15f, 0.20f, 0.32f, 0.85f);
    private Color colorWishFoundWord = new Color(0.1f, 0.1f, 0.1f);
    private Color colorWishFoundBg = new Color(1.0f, 0.82f, 0.2f, 1.0f); // Gold Yellow
    private Color colorWitchFoundWord = new Color(1.0f, 1.0f, 1.0f);
    private Color colorWitchFoundBg = new Color(0.85f, 0.2f, 0.9f, 1.0f); // Magenta/Purple
    private Color colorAudioHighlightBg = new Color(0.2f, 0.75f, 1.0f, 1.0f); // Bright Blue

    protected virtual void OnEnable() {
        // Prevent STT subscriptions
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeAudioReferences();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Reading;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeAudioReferences();
        UpdateTitleAndUIComponents();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (completionBannerObj != null) completionBannerObj.SetActive(false);
        if (audioContrastBannerObj != null) audioContrastBannerObj.SetActive(false);

        BuildInteractiveWordGrid();
        StartCoroutine(StartR01StageSequence());
    }

    private void DeactivateObsoleteBaseUI() {
        string[] obsoleteNames = new string[] {
            "sentence", "ExpressionCountTMP", "UnitHeading", "Sign Board left", "Sign Board Right",
            "Formal", "Informal", "MagnetStations", "WordTiles", "CompletedPhraseGlow",
            "ARIA", "SkipButton", "Continue", "Heading", "Header", "SpeechBubble",
            "DialogueBox", "HubButtons", "Hubs", "ResultPanel"
        };

        foreach (string name in obsoleteNames) {
            Transform t = transform.Find(name);
            if (t != null) {
                t.gameObject.SetActive(false);
            }
        }
    }

    private void AutoFindUIReferences() {
        if (headerTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("Header");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (subtitleTMP == null) {
            Transform t = transform.Find("Subtitle") ?? transform.Find("SubtitleText") ?? transform.Find("Instruction");
            if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (roundInstructionTMP == null) {
            Transform t = transform.Find("RoundInstruction") ?? transform.Find("InstructionText");
            if (t != null) roundInstructionTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (readingContainer == null) {
            Transform t = transform.Find("ReadingContainer") ?? transform.Find("WordContainer") ?? transform.Find("LyricScreen");
            if (t != null) readingContainer = t.GetComponent<RectTransform>();
        }

        if (wishCounterTMP == null) {
            Transform t = transform.Find("CountersContainer/WishCounter") ?? transform.Find("WishCounter");
            if (t != null) wishCounterTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (witchCounterTMP == null) {
            Transform t = transform.Find("CountersContainer/WitchCounter") ?? transform.Find("WitchCounter");
            if (t != null) witchCounterTMP = t.GetComponent<TextMeshProUGUI>();
        }
    }

    private void InitializeAudioReferences() {
#if UNITY_EDITOR
        string audioDir = "Assets/Audio/2A/8_SourceOfInspiration/Reading/";
        if (voR01FullTwister == null) voR01FullTwister = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "R01_FullTwister.mp3");
        if (voR01AriaIntro == null) voR01AriaIntro = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "R01_AriaIntro.mp3");
        if (voR01Contrast == null) voR01Contrast = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "R01_ContrastAudio.mp3");
        if (sfxTapFind == null) sfxTapFind = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
#endif
    }

    private void UpdateTitleAndUIComponents() {
        if (headerTMP != null) headerTMP.text = "R01 — Read Along";
        if (subtitleTMP != null) subtitleTMP.text = "Spot \"wish\" and \"witch\"";
    }

    private void BuildInteractiveWordGrid() {
        if (readingContainer == null) return;

        // Clear existing children
        foreach (Transform child in readingContainer) {
            if (child != null) Destroy(child.gameObject);
        }
        wordDataList.Clear();

        string textToParse = string.IsNullOrEmpty(customTwisterText) ?
            "\"I wish to wish the wish you wish to wish, but if you wish the witch wishes, I won't wish the wish you wish to wish.\"" : customTwisterText;

        string[] tokens = textToParse.Split(' ');
        float estimatedAudioLength = (voR01FullTwister != null) ? voR01FullTwister.length : 4.8f;
        float timePerWord = estimatedAudioLength / Mathf.Max(1, tokens.Length);

        totalWishTargets = 0;

        for (int i = 0; i < tokens.Length; i++) {
            string rawToken = tokens[i];
            string clean = rawToken.ToLower().Replace("\"", "").Replace(".", "").Replace(",", "").Replace("!", "").Replace("?", "");

            WordTargetType targetType = WordTargetType.Normal;
            int targetRound = 0;

            if (clean == "wish" || clean == "wishes") {
                targetType = WordTargetType.Wish;
                targetRound = 1;
                totalWishTargets++;
            } else if (clean == "witch") {
                targetType = WordTargetType.Witch;
                targetRound = 2;
            }

            GameObject wObj = new GameObject($"Word_{i:D2}_{clean}", typeof(RectTransform), typeof(Image), typeof(Button));
            wObj.transform.SetParent(readingContainer, false);

            RectTransform rRect = wObj.GetComponent<RectTransform>();
            rRect.sizeDelta = new Vector2(Mathf.Max(75f, clean.Length * 22f + 25f), 55f);

            Image bgImg = wObj.GetComponent<Image>();
            bgImg.color = colorNormalBg;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(wObj.transform, false);

            RectTransform tRect = txtObj.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = rawToken;
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = colorNormalWord;

            Button btn = wObj.GetComponent<Button>();
            btn.interactable = true;

            InteractiveWordData data = new InteractiveWordData {
                id = i,
                originalText = rawToken,
                cleanWord = clean,
                targetType = targetType,
                targetRound = targetRound,
                isFound = false,
                startTime = i * timePerWord,
                endTime = (i + 1) * timePerWord,
                wordObj = wObj,
                wordButton = btn,
                wordTMP = tmp,
                wordBgImage = bgImg
            };

            int wordIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnWordTapped(wordIndex));

            wordDataList.Add(data);
        }

        UpdateCountersUI();
    }

    private IEnumerator StartR01StageSequence() {
        // Step 1: ARIA Welcome & Read-Along Instructions
        if (roundInstructionTMP != null) {
            roundInstructionTMP.text = "ARIA: \"They sound almost the same — but they're not! Can you hear it?\"";
        }

        if (voR01AriaIntro != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(voR01AriaIntro);
            yield return new WaitForSeconds(voR01AriaIntro.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        // Step 2: Bouncing Word Read-Along Playback
        if (roundInstructionTMP != null) {
            roundInstructionTMP.text = "LISTEN & READ ALONG...";
        }

        yield return StartCoroutine(PlayFullTwisterAudioWithBouncingHighlight());

        // Step 3: Round 1 Start (Find "wish")
        currentRound = 1;
        if (roundInstructionTMP != null) {
            roundInstructionTMP.text = "ROUND 1 — Tap every \"wish\" you see!";
        }
    }

    private IEnumerator PlayFullTwisterAudioWithBouncingHighlight() {
        isReadAlongPlaying = true;
        float duration = (voR01FullTwister != null) ? voR01FullTwister.length : 5.0f;

        if (voR01FullTwister != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(voR01FullTwister);
        }

        float elapsed = 0f;
        int lastHighlightedIdx = -1;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;

            // Find current active word index by time
            int activeIdx = -1;
            for (int i = 0; i < wordDataList.Count; i++) {
                if (elapsed >= wordDataList[i].startTime && elapsed < wordDataList[i].endTime) {
                    activeIdx = i;
                    break;
                }
            }

            if (activeIdx != lastHighlightedIdx) {
                // Clear previous highlight if not found
                if (lastHighlightedIdx >= 0 && lastHighlightedIdx < wordDataList.Count) {
                    ResetWordVisualState(wordDataList[lastHighlightedIdx]);
                }

                // Apply bouncing highlight to current active word
                if (activeIdx >= 0 && activeIdx < wordDataList.Count) {
                    InteractiveWordData data = wordDataList[activeIdx];
                    if (data.wordBgImage != null && !data.isFound) {
                        data.wordBgImage.color = colorAudioHighlightBg;
                        data.wordObj.transform.DOKill(true);
                        data.wordObj.transform.DOPunchScale(Vector3.one * 0.18f, 0.25f);
                    }
                }
                lastHighlightedIdx = activeIdx;
            }

            yield return null;
        }

        // Clear final audio highlight
        if (lastHighlightedIdx >= 0 && lastHighlightedIdx < wordDataList.Count) {
            ResetWordVisualState(wordDataList[lastHighlightedIdx]);
        }
        isReadAlongPlaying = false;
    }

    public void OnWordTapped(int index) {
        if (index < 0 || index >= wordDataList.Count) return;
        InteractiveWordData data = wordDataList[index];
        if (data == null || data.isFound) return;

        if (currentRound == 1) {
            // ROUND 1: Tap "wish" or "wishes"
            if (data.targetType == WordTargetType.Wish) {
                data.isFound = true;
                wishFoundCount++;
                PlayCorrectTapFeedback(data, "✓ " + data.originalText, colorWishFoundBg, colorWishFoundWord);
                UpdateCountersUI();

                if (wishFoundCount >= totalWishTargets) {
                    StartCoroutine(TransitionToRound2Routine());
                }
            } else {
                // Wrong tap in Round 1: Gentle feedback, no harsh penalty
                PlayGentleWrongTapFeedback(data);
            }
        } else if (currentRound == 2) {
            // ROUND 2: Tap "witch"
            if (data.targetType == WordTargetType.Witch) {
                data.isFound = true;
                witchFound = true;
                PlayCorrectTapFeedback(data, "✨ " + data.originalText, colorWitchFoundBg, colorWitchFoundWord);
                UpdateCountersUI();

                StartCoroutine(CompleteStageSequenceRoutine());
            } else {
                // Wrong tap in Round 2: Gentle feedback
                PlayGentleWrongTapFeedback(data);
            }
        }
    }

    private void PlayCorrectTapFeedback(InteractiveWordData data, string newText, Color bgCol, Color txtCol) {
        if (sfxTapFind != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        if (data.wordObj != null) {
            data.wordObj.transform.DOKill(true);
            data.wordObj.transform.DOPunchScale(Vector3.one * 0.22f, 0.3f);
        }

        if (data.wordBgImage != null) {
            data.wordBgImage.color = bgCol;
        }

        if (data.wordTMP != null) {
            data.wordTMP.text = newText;
            data.wordTMP.color = txtCol;
        }
    }

    private void PlayGentleWrongTapFeedback(InteractiveWordData data) {
        if (data.wordObj != null) {
            data.wordObj.transform.DOKill(true);
            data.wordObj.transform.DOShakePosition(0.35f, 8f, 10, 90f);
        }
    }

    private void ResetWordVisualState(InteractiveWordData data) {
        if (data == null) return;
        if (data.isFound) return;

        if (data.wordBgImage != null) data.wordBgImage.color = colorNormalBg;
        if (data.wordTMP != null) data.wordTMP.color = colorNormalWord;
    }

    private void UpdateCountersUI() {
        if (wishCounterTMP != null) {
            wishCounterTMP.text = $"WISH FOUND: {wishFoundCount} / {totalWishTargets}";
        }
        if (wishProgressFillImage != null && totalWishTargets > 0) {
            wishProgressFillImage.fillAmount = (float)wishFoundCount / totalWishTargets;
        }

        if (witchCounterTMP != null) {
            witchCounterTMP.text = witchFound ? "WITCH FOUND: 1 / 1" : "WITCH FOUND: 0 / 1";
        }
        if (witchProgressFillImage != null) {
            witchProgressFillImage.fillAmount = witchFound ? 1f : 0f;
        }
    }

    private IEnumerator TransitionToRound2Routine() {
        yield return new WaitForSeconds(0.4f);

        if (completionBannerObj != null && completionBannerTMP != null) {
            completionBannerTMP.text = "Great! You found all the \"wish\" words.";
            completionBannerObj.SetActive(true);
            completionBannerObj.transform.DOKill(true);
            completionBannerObj.transform.localScale = Vector3.zero;
            completionBannerObj.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(1.2f);

        if (completionBannerObj != null) completionBannerObj.SetActive(false);

        currentRound = 2;
        if (roundInstructionTMP != null) {
            roundInstructionTMP.text = "ROUND 2 — Now find the single \"witch\"!";
        }
    }

    private IEnumerator CompleteStageSequenceRoutine() {
        yield return new WaitForSeconds(0.4f);

        // Step 1: Contrast Audio Playback ("wish -> witch -> wish -> witch")
        if (audioContrastBannerObj != null && audioContrastTMP != null) {
            audioContrastTMP.text = "LISTEN: wish → witch — Did you hear the difference?";
            audioContrastBannerObj.SetActive(true);
        }

        if (voR01Contrast != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(voR01Contrast);

            // Highlight wish and witch back-to-back while contrast audio plays
            StartCoroutine(HighlightContrastWordsRoutine(voR01Contrast.length));
            yield return new WaitForSeconds(voR01Contrast.length + 0.4f);
        } else {
            yield return new WaitForSeconds(2.0f);
        }

        if (audioContrastBannerObj != null) audioContrastBannerObj.SetActive(false);

        // Step 2: Final Stage Completion Banner & Next Button Unlock
        if (completionBannerObj != null && completionBannerTMP != null) {
            completionBannerTMP.text = "Excellent! You spotted the \"wish\" and \"witch\" difference.";
            completionBannerObj.SetActive(true);
            completionBannerObj.transform.DOKill(true);
            completionBannerObj.transform.localScale = Vector3.zero;
            completionBannerObj.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    private IEnumerator HighlightContrastWordsRoutine(float duration) {
        float half = duration * 0.5f;
        float elapsed = 0f;

        // Flash wish words first
        while (elapsed < half) {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Flash witch word next
        for (int i = 0; i < wordDataList.Count; i++) {
            if (wordDataList[i].targetType == WordTargetType.Witch && wordDataList[i].wordObj != null) {
                wordDataList[i].wordObj.transform.DOKill(true);
                wordDataList[i].wordObj.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f);
                break;
            }
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Reading);
        }
    }

#if UNITY_EDITOR
    private void OnValidate() {
        // Do not instantiate GameObjects or call SetParent in OnValidate on Prefab Assets
    }
#endif
}
