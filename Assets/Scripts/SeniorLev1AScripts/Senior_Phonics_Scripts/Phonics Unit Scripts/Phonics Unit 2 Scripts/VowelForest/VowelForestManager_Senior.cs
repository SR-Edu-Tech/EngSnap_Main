using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class VowelForestWord
{
    [Tooltip("The word text displayed on the stone (e.g., 'had', 'gate')")]
    public string word;

    [Tooltip("Is this a long vowel word? (True = Long, False = Short)")]
    public bool isLongVowel;

    [Tooltip("Pronunciation audio clip of the word")]
    public AudioClip wordAudio;
}

public class VowelForestManager_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("UI Board & Path Setup")]
    [Tooltip("List of words that will be assigned sequentially to the path stones.")]
    [SerializeField] private List<VowelForestWord> pathWords = new List<VowelForestWord>();

    [Tooltip("The RectTransforms representing each visual stone along the winding path.")]
    [SerializeField] private RectTransform[] pathStones;

    [Tooltip("TMP Text components corresponding to each path stone.")]
    [SerializeField] private TMP_Text[] stoneTexts;

    [Tooltip("Visual outlines/completed checkmarks to show active/completed progress on each stone.")]
    [SerializeField] private Image[] stoneCompletedHighlights;

    [Tooltip("Starting platform where the mascot stands before the game begins.")]
    [SerializeField] private RectTransform startPlatform;

    [Tooltip("Goal platform (the Happy Tree) where the mascot lands when the game is won.")]
    [SerializeField] private RectTransform goalPlatform;

    [Header("Mascot & Pawn Token")]
    [Tooltip("The player's character token that moves along the path.")]
    [SerializeField] private RectTransform mascotCharacter;
    [SerializeField] private float jumpHeight = 100f;
    [SerializeField] private float stepDuration = 0.5f;
    [Tooltip("Positional offset applied to the player mascot when positioned on the start platform.")]
    [SerializeField] private Vector2 mascotStartPlatformOffset = Vector2.zero;
    [Tooltip("Default positional offset applied to the player mascot when positioned on path stones.")]
    [SerializeField] private Vector2 mascotStoneOffset = Vector2.zero;
    [Tooltip("Individual positional offsets for each path stone. Index matches stone index. If empty or out of range, falls back to Mascot Stone Offset.")]
    [SerializeField] private Vector2[] mascotStoneOffsets;
    [Tooltip("Positional offset applied to the player mascot when positioned on the goal platform.")]
    [SerializeField] private Vector2 mascotGoalPlatformOffset = Vector2.zero;
    [Tooltip("Enter a stone index here (0 to 14) in the editor to temporarily move the mascot to that stone for offset preview. Set to -1 to disable.")]
    [SerializeField] private int previewStoneIndex = -1;

    [Header("Coin Flip UI")]
    [SerializeField] private Button flipButton;
    [SerializeField] private RectTransform coinTransform;
    [SerializeField] private TMP_Text coinResultText;
    [SerializeField] private Image coinImage;
    [SerializeField] private Sprite headsSprite;
    [SerializeField] private Sprite tailsSprite;
    [SerializeField] private float coinFlipDuration = 1.0f;

    [Header("Evaluation & Choice UI")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button shortVowelButton;
    [SerializeField] private Button longVowelButton;
    
    [Tooltip("Main TMP text component in the read-aloud panel to display the current word.")]
    [SerializeField] private TMP_Text evaluationWordText;

    [Tooltip("Button to replay pronunciation audio of the current word.")]
    [SerializeField] private Button listenButton;

    [Header("Plant Growth & Progress UI")]
    [SerializeField] private Slider plantSlider;
    [SerializeField] private GameObject nextButton;

    [Tooltip("Text component to display progress like '2/6'")]
    [SerializeField] private TMP_Text leafCounterText;

    [Tooltip("Panel displayed on correct answers showing '+1 Leaf' etc.")]
    [SerializeField] private GameObject correctFeedbackPanel;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource mascotAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip introVoice;
    [SerializeField] private AudioClip successVoice;
    [SerializeField] private AudioClip coinFlipSFX;
    [SerializeField] private AudioClip correctSFX;
    [SerializeField] private AudioClip wrongSFX;
    [SerializeField] private AudioClip hopSFX;

    // Runtime state
    private int currentStoneIndex = -1; // -1 represents the START platform
    private bool isGameCompleted = false;
    private int totalStones = 0;
    private int correctCount = 0;

    private void Awake()
    {
        // Try programmatically locating missing references to make setup foolproof
        FindFallbacks();

        // Setup button listeners
        if (flipButton != null)
        {
            flipButton.onClick.RemoveAllListeners();
            flipButton.onClick.AddListener(OnFlipButtonClicked);
        }

        if (shortVowelButton != null)
        {
            shortVowelButton.onClick.RemoveAllListeners();
            shortVowelButton.onClick.AddListener(() => OnChoiceSelected(false));
        }

        if (longVowelButton != null)
        {
            longVowelButton.onClick.RemoveAllListeners();
            longVowelButton.onClick.AddListener(() => OnChoiceSelected(true));
        }

        if (listenButton != null)
        {
            listenButton.onClick.RemoveAllListeners();
            listenButton.onClick.AddListener(PlayCurrentWordAudio);
        }

        if (nextButton != null)
        {
            nextButton.GetComponent<Button>()?.onClick.RemoveAllListeners();
            nextButton.GetComponent<Button>()?.onClick.AddListener(OnNextButtonClicked);
        }
    }

    private void FindFallbacks()
    {
        if (flipButton == null)
        {
            if (coinTransform != null)
            {
                flipButton = coinTransform.GetComponent<Button>();
                if (flipButton == null)
                {
                    flipButton = coinTransform.gameObject.AddComponent<Button>();
                }
            }
            else if (coinImage != null)
            {
                flipButton = coinImage.GetComponent<Button>();
                if (flipButton == null)
                {
                    flipButton = coinImage.gameObject.AddComponent<Button>();
                }
            }
        }

        if (mascotAudioSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 0) mascotAudioSource = sources[0];
            else mascotAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxAudioSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 1) sfxAudioSource = sources[1];
            else if (sources.Length > 0 && sources[0] != mascotAudioSource) sfxAudioSource = sources[0];
            else sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (evaluationWordText == null && choicePanel != null)
        {
            TMP_Text[] texts = choicePanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in texts)
            {
                if (txt.gameObject.GetComponentInParent<Button>() != null) continue;

                string txtName = txt.name.ToLower();
                if (txtName.Contains("short") || txtName.Contains("long") || txtName.Contains("button")) continue;

                Transform parent = txt.transform.parent;
                if (parent != null)
                {
                    string parentName = parent.name.ToLower();
                    if (parentName.Contains("short") || parentName.Contains("long") || parentName.Contains("button")) continue;
                }

                evaluationWordText = txt;
                break;
            }
        }

        if (listenButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                string btnName = btn.name.ToLower();
                if (btnName.Contains("listen") || btnName.Contains("audio") || btnName.Contains("speaker") || btnName.Contains("sound"))
                {
                    listenButton = btn;
                    break;
                }
            }
        }

        if (leafCounterText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in texts)
            {
                string txtName = txt.name.ToLower();
                if (txtName.Contains("leaf") || txtName.Contains("meter") || txtName.Contains("progress") || txtName.Contains("counter"))
                {
                    leafCounterText = txt;
                    break;
                }
            }
        }

        if (correctFeedbackPanel == null)
        {
            Transform[] childTransforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in childTransforms)
            {
                string tName = t.name.ToLower();
                if (tName.Contains("feedback") || tName.Contains("correctpanel") || tName.Contains("correct feedback") || tName.Contains("popup"))
                {
                    correctFeedbackPanel = t.gameObject;
                    break;
                }
            }
        }

        if (startPlatform == null)
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            foreach (var r in rects)
            {
                string rName = r.name.ToLower();
                if (rName == "start" || rName == "startplatform" || rName == "start platform")
                {
                    startPlatform = r;
                    break;
                }
            }
        }

        if (goalPlatform == null)
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            foreach (var r in rects)
            {
                string rName = r.name.ToLower();
                if (rName == "goal" || rName == "goalplatform" || rName == "goal platform" || rName == "tree")
                {
                    goalPlatform = r;
                    break;
                }
            }
        }
    }

    private void OnEnable()
    {
        ResetActivity();
        StartCoroutine(IntroSequence());
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
        FindFallbacks();
        if (mascotCharacter != null)
        {
            totalStones = pathStones != null ? pathStones.Length : 0;
            if (previewStoneIndex >= 0 && previewStoneIndex < totalStones && pathStones[previewStoneIndex] != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(pathStones[previewStoneIndex].position, GetOffsetForStone(previewStoneIndex));
            }
            else if (startPlatform != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(startPlatform.position, mascotStartPlatformOffset);
            }
            else if (totalStones > 0 && pathStones[0] != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(pathStones[0].position, GetOffsetForStone(0));
            }
        }
    }
#endif

    private void ResetActivity()
    {
        Debug.Log("[VowelForestManager] Resetting Activity...");
        currentStoneIndex = -1;
        correctCount = 0;
        isGameCompleted = false;

        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }

        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }

        if (correctFeedbackPanel != null)
        {
            correctFeedbackPanel.SetActive(false);
        }

        if (plantSlider != null)
        {
            plantSlider.minValue = 0f;
            plantSlider.maxValue = 1f;
            plantSlider.value = 0f;
        }

        // Initialize stones with words
        totalStones = pathStones != null ? pathStones.Length : 0;
        for (int i = 0; i < totalStones; i++)
        {
            if (pathStones[i] == null) continue;

            if (i < pathWords.Count)
            {
                pathStones[i].gameObject.SetActive(true);
                if (stoneTexts != null && i < stoneTexts.Length && stoneTexts[i] != null)
                {
                    stoneTexts[i].text = pathWords[i].word;
                }
            }
            else
            {
                pathStones[i].gameObject.SetActive(false);
            }

            // Reset completion highlights
            if (stoneCompletedHighlights != null && i < stoneCompletedHighlights.Length && stoneCompletedHighlights[i] != null)
            {
                stoneCompletedHighlights[i].gameObject.SetActive(false);
            }
        }

        // Place player mascot on the starting platform
        if (mascotCharacter != null)
        {
            if (startPlatform != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(startPlatform.position, mascotStartPlatformOffset);
                Debug.Log($"[VowelForestManager] Placing mascot on Start Platform '{startPlatform.name}' at World Position {startPlatform.position}. Local position set to {mascotCharacter.localPosition}");
            }
            else if (totalStones > 0 && pathStones[0] != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(pathStones[0].position, GetOffsetForStone(0));
                Debug.LogWarning($"[VowelForestManager] Start Platform is unassigned! Placing mascot on first Stone '{pathStones[0].name}' instead. Local position set to {mascotCharacter.localPosition}");
            }
            else
            {
                Debug.LogError("[VowelForestManager] Cannot place mascot: both Start Platform and Path Stones are unassigned/empty!");
            }
        }

        UpdateLeafProgressUI();

        if (flipButton != null)
        {
            flipButton.interactable = true;
        }

        if (coinResultText != null)
        {
            coinResultText.text = "FLIP!";
        }
    }

    private IEnumerator IntroSequence()
    {
        if (flipButton != null) flipButton.interactable = false;

        if (mascotAudioSource != null && introVoice != null)
        {
            mascotAudioSource.clip = introVoice;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(introVoice.length + 0.5f);
        }
        else
        {
            if (introVoice == null)
            {
                Debug.LogWarning("[VowelForestManager] 'Intro Voice' audio clip is not assigned in the Inspector!");
            }
            yield return new WaitForSeconds(0.5f);
        }

        if (flipButton != null) flipButton.interactable = true;
    }

    private void OnFlipButtonClicked()
    {
        if (isGameCompleted) return;
        StartCoroutine(FlipCoinRoutine());
    }

    private IEnumerator FlipCoinRoutine()
    {
        if (flipButton != null) flipButton.interactable = false;

        if (coinResultText != null)
        {
            coinResultText.gameObject.SetActive(true);
        }

        if (sfxAudioSource != null && coinFlipSFX != null)
        {
            sfxAudioSource.PlayOneShot(coinFlipSFX);
        }
        else if (coinFlipSFX == null)
        {
            Debug.LogWarning("[VowelForestManager] 'Coin Flip SFX' audio clip is not assigned in the Inspector!");
        }

        bool isHeads = Random.value > 0.5f;
        int steps = isHeads ? 3 : 1;

        GameObject animationTarget = coinImage != null ? coinImage.gameObject : (coinTransform != null ? coinTransform.gameObject : null);

        if (animationTarget != null)
        {
            bool animationComplete = false;
            Vector3 originalScale = animationTarget.transform.localScale;

            LeanTween.scaleX(animationTarget, 0f, 0.15f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    animationTarget.transform.localScale = originalScale;
                    if (coinImage != null && headsSprite != null && tailsSprite != null)
                        coinImage.sprite = isHeads ? headsSprite : tailsSprite;
                    if (coinResultText != null)
                        coinResultText.text = isHeads ? "HEADS\n(Move 3)" : "TAILS\n(Move 1)";
                    LeanTween.scale(animationTarget, originalScale * 1.25f, 0.2f)
                        .setEaseOutBack()
                        .setOnComplete(() => LeanTween.scale(animationTarget, originalScale, 0.15f).setOnComplete(() => animationComplete = true));
                });
            yield return new WaitUntil(() => animationComplete);
        }
        else
        {
            if (coinResultText != null)
                coinResultText.text = isHeads ? "HEADS\n(Move 3)" : "TAILS\n(Move 1)";
            yield return new WaitForSeconds(0.8f);
        }

        int targetIndex = Mathf.Min(currentStoneIndex + steps, totalStones - 1);
        yield return StartCoroutine(HopToStoneSequence(targetIndex));
    }

    private IEnumerator HopToStoneSequence(int targetIndex)
    {
        while (currentStoneIndex < targetIndex)
        {
            int nextIndex = currentStoneIndex + 1;
            if (nextIndex >= totalStones || pathStones[nextIndex] == null || mascotCharacter == null) break;

            Vector2 startPos = mascotCharacter.localPosition;
            Vector2 endPos = GetLocalPositionForWorldPoint(pathStones[nextIndex].position, GetOffsetForStone(nextIndex));

            if (sfxAudioSource != null && hopSFX != null)
                sfxAudioSource.PlayOneShot(hopSFX);
            else if (hopSFX == null)
            {
                Debug.LogWarning("[VowelForestManager] 'Hop SFX' audio clip is not assigned in the Inspector!");
            }

            bool stepComplete = false;
            LeanTween.value(gameObject, 0f, 1f, stepDuration)
                .setOnUpdate((float t) => {
                    Vector2 currentPos = Vector2.Lerp(startPos, endPos, t);
                    float peak = Mathf.Sin(t * Mathf.PI) * jumpHeight;
                    currentPos.y += peak;
                    mascotCharacter.localPosition = currentPos;
                })
                .setOnComplete(() => {
                    currentStoneIndex = nextIndex;
                    stepComplete = true;
                });
            yield return new WaitUntil(() => stepComplete);
        }

        yield return new WaitForSeconds(0.2f);
        OnArrivedAtStone();
    }

    private void OnArrivedAtStone()
    {
        if (currentStoneIndex < 0 || currentStoneIndex >= pathWords.Count)
        {
            Debug.LogError("[VowelForestManager] Current stone index exceeds words configured!");
            StartCoroutine(VictorySequence());
            return;
        }

        VowelForestWord currentWord = pathWords[currentStoneIndex];
        if (evaluationWordText != null) evaluationWordText.text = currentWord.word;
        PlayCurrentWordAudio();

        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            choicePanel.transform.localScale = Vector3.zero;
            LeanTween.cancel(choicePanel);
            LeanTween.scale(choicePanel, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        }
    }

    private void PlayCurrentWordAudio()
    {
        if (currentStoneIndex >= 0 && currentStoneIndex < pathWords.Count)
        {
            VowelForestWord currentWord = pathWords[currentStoneIndex];
            if (currentWord.wordAudio != null)
            {
                if (mascotAudioSource != null)
                {
                    mascotAudioSource.clip = currentWord.wordAudio;
                    mascotAudioSource.Play();
                }
            }
            else
            {
                Debug.LogWarning($"[VowelForestManager] No audio clip assigned for the word '{currentWord.word}' (Stone {currentStoneIndex + 1}) in the 'Path Words' list!");
            }
        }
    }

    private void OnChoiceSelected(bool isLongSelected)
    {
        if (currentStoneIndex < 0 || currentStoneIndex >= pathWords.Count) return;

        if (coinResultText != null)
        {
            coinResultText.gameObject.SetActive(false);
        }

        VowelForestWord currentWord = pathWords[currentStoneIndex];
        if (currentWord.isLongVowel == isLongSelected) StartCoroutine(CorrectChoiceRoutine());
        else StartCoroutine(WrongChoiceRoutine());
    }

    private IEnumerator CorrectChoiceRoutine()
    {
        SetChoiceButtonsInteractable(false);
        if (sfxAudioSource != null && correctSFX != null) sfxAudioSource.PlayOneShot(correctSFX);
        else if (correctSFX == null)
        {
            Debug.LogWarning("[VowelForestManager] 'Correct SFX' audio clip is not assigned in the Inspector!");
        }

        if (pathStones.Length > currentStoneIndex && pathStones[currentStoneIndex] != null)
            LeanTween.scale(pathStones[currentStoneIndex].gameObject, Vector3.one * 1.15f, 0.2f).setLoopPingPong(1);

        if (stoneCompletedHighlights != null && currentStoneIndex < stoneCompletedHighlights.Length && stoneCompletedHighlights[currentStoneIndex] != null)
            stoneCompletedHighlights[currentStoneIndex].gameObject.SetActive(true);

        correctCount++;
        UpdateLeafProgressUI();

        if (correctFeedbackPanel != null)
        {
            correctFeedbackPanel.SetActive(true);
            correctFeedbackPanel.transform.localScale = Vector3.zero;
            LeanTween.cancel(correctFeedbackPanel);
            LeanTween.scale(correctFeedbackPanel, Vector3.one, 0.3f).setEase(LeanTweenType.easeOutBack);
        }

        if (plantSlider != null)
        {
            float targetValue = totalStones > 0 ? (float)(currentStoneIndex + 1) / totalStones : 0f;
            LeanTween.cancel(plantSlider.gameObject);
            LeanTween.value(plantSlider.gameObject, plantSlider.value, targetValue, 0.4f).setOnUpdate((float val) => plantSlider.value = val);
        }

        yield return new WaitForSeconds(1.2f);
        if (correctFeedbackPanel != null)
            LeanTween.scale(correctFeedbackPanel, Vector3.zero, 0.2f).setOnComplete(() => correctFeedbackPanel.SetActive(false));

        if (choicePanel != null)
        {
            bool shrinkDone = false;
            LeanTween.scale(choicePanel, Vector3.zero, 0.25f).setOnComplete(() => { choicePanel.SetActive(false); shrinkDone = true; });
            yield return new WaitUntil(() => shrinkDone);
        }

        SetChoiceButtonsInteractable(true);
        if (currentStoneIndex >= totalStones - 1) StartCoroutine(VictorySequence());
        else if (flipButton != null) flipButton.interactable = true;
    }

    private IEnumerator WrongChoiceRoutine()
    {
        SetChoiceButtonsInteractable(false);
        if (sfxAudioSource != null && wrongSFX != null) sfxAudioSource.PlayOneShot(wrongSFX);
        else if (wrongSFX == null)
        {
            Debug.LogWarning("[VowelForestManager] 'Wrong SFX' audio clip is not assigned in the Inspector!");
        }

        if (choicePanel != null)
        {
            Vector3 origPos = choicePanel.transform.localPosition;
            LeanTween.cancel(choicePanel);
            LeanTween.moveLocalX(choicePanel, origPos.x + 15f, 0.05f).setLoopPingPong(2).setOnComplete(() => choicePanel.transform.localPosition = origPos);
        }

        yield return new WaitForSeconds(0.3f);
        PlayCurrentWordAudio();
        yield return new WaitForSeconds(0.5f);
        SetChoiceButtonsInteractable(true);
    }

    private void SetChoiceButtonsInteractable(bool state)
    {
        if (shortVowelButton != null) shortVowelButton.interactable = state;
        if (longVowelButton != null) longVowelButton.interactable = state;
    }

    private void UpdateLeafProgressUI()
    {
        if (leafCounterText != null)
        {
            int progress = Mathf.Clamp(currentStoneIndex + 1, 0, totalStones);
            leafCounterText.text = $"{progress}/{totalStones}";
        }
    }

    private IEnumerator VictorySequence()
    {
        isGameCompleted = true;
        if (flipButton != null) flipButton.interactable = false;

        if (mascotCharacter != null && goalPlatform != null)
        {
            Vector2 startPos = mascotCharacter.localPosition;
            Vector2 endPos = GetLocalPositionForWorldPoint(goalPlatform.position, mascotGoalPlatformOffset);
            if (sfxAudioSource != null && hopSFX != null) sfxAudioSource.PlayOneShot(hopSFX);
            else if (hopSFX == null)
            {
                Debug.LogWarning("[VowelForestManager] 'Hop SFX' audio clip is not assigned in the Inspector!");
            }
            bool jumpComplete = false;
            LeanTween.value(gameObject, 0f, 1f, stepDuration).setOnUpdate((float t) => {
                Vector2 currentPos = Vector2.Lerp(startPos, endPos, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
                mascotCharacter.localPosition = currentPos;
            }).setOnComplete(() => jumpComplete = true);
            yield return new WaitUntil(() => jumpComplete);
        }

        correctCount = totalStones;
        if (leafCounterText != null)
        {
            leafCounterText.text = $"{totalStones}/{totalStones}";
        }
        if (plantSlider != null)
        {
            LeanTween.cancel(plantSlider.gameObject);
            LeanTween.value(plantSlider.gameObject, plantSlider.value, 1.0f, 0.5f).setOnUpdate((float val) => plantSlider.value = val);
        }

        yield return new WaitForSeconds(0.4f);
        if (mascotAudioSource != null && successVoice != null)
        {
            mascotAudioSource.clip = successVoice;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(successVoice.length + 0.2f);
        }
        if (unitCompleteAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(unitCompleteAudio);
            yield return new WaitForSeconds(unitCompleteAudio.length + 0.2f);
        }
        else
        {
            if (successVoice == null)
            {
                Debug.LogWarning("[VowelForestManager] 'Success Voice' audio clip is not assigned in the Inspector!");
            }
            yield return new WaitForSeconds(0.5f);
        }

        if (nextButton != null)
        {
            nextButton.SetActive(true);
            nextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(nextButton);
            LeanTween.scale(nextButton, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);
        }
    }

    private void OnNextButtonClicked()
    {
        GameFlowManager_Senior_Phonics flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();
        if (flowManager != null)
        {
            flowManager.NextGameplay();
        }
        else
        {
            Debug.LogWarning("[VowelForestManager] GameFlowManager_Senior_Phonics not found, resetting activity.");
            ResetActivity();
        }
    }

    private Vector2 GetOffsetForStone(int stoneIndex)
    {
        if (mascotStoneOffsets != null && stoneIndex >= 0 && stoneIndex < mascotStoneOffsets.Length)
        {
            return mascotStoneOffsets[stoneIndex];
        }
        return mascotStoneOffset;
    }

    private Vector3 GetLocalPositionForWorldPoint(Vector3 worldPoint, Vector2 offset)
    {
        if (mascotCharacter == null || mascotCharacter.parent == null) return Vector3.zero;

        RectTransform parentRect = mascotCharacter.parent as RectTransform;
        if (parentRect == null) return Vector3.zero;

        Vector3 localPoint = parentRect.InverseTransformPoint(worldPoint);
        return new Vector3(localPoint.x + offset.x, localPoint.y + offset.y, mascotCharacter.localPosition.z);
    }
}
