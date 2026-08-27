using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class BirthdayBashWordData
{
    [Tooltip("The word text displayed on the choice card (e.g. 'ship')")]
    public string word;

    [Tooltip("Optional audio clip of the mascot pronouncing this word")]
    public AudioClip wordAudio;
}

[System.Serializable]
public class BirthdayBashOptionUI
{
    [Tooltip("The parent GameObject of this option card (to show/hide)")]
    public GameObject container;

    [Tooltip("The main button component for this option card")]
    public Button cardButton;

    [Tooltip("The text component displaying the option word")]
    public TextMeshProUGUI spellingTextLabel;

    [Tooltip("The highlight border image component (for green/red feedback)")]
    public Image highlightBorder;

    [Tooltip("Optional background image component to apply default colors")]
    public Image cardBgImage;
}

public class BirthdayBashBoard_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("UI Board & Path Setup")]
    [Tooltip("The RectTransforms representing each visual stone/tile along the winding path (26 tiles).")]
    public RectTransform[] pathStones = new RectTransform[26];

    [Tooltip("TMP Text components corresponding to each path stone.")]
    public TextMeshProUGUI[] stoneTexts = new TextMeshProUGUI[26];

    [Tooltip("Visual outlines/completed highlights to show active/completed progress on each stone.")]
    public Image[] stoneCompletedHighlights = new Image[26];

    [Tooltip("Starting platform where the mascot stands before the game begins.")]
    public RectTransform startPlatform;

    [Tooltip("Goal platform (Chick & Whale's Party) where the mascot lands when the game is won.")]
    public RectTransform goalPlatform;

    [Header("Mascot & Pawn Token")]
    [Tooltip("The player's character token that moves along the path.")]
    public RectTransform mascotCharacter;
    public float jumpHeight = 100f;
    public float stepDuration = 0.5f;

    [Tooltip("Positional offset applied to the player mascot when positioned on the start platform.")]
    public Vector2 mascotStartPlatformOffset = new Vector2(0f, 40f);

    [Tooltip("Default positional offset applied to the player mascot when positioned on path stones.")]
    public Vector2 mascotStoneOffset = new Vector2(0f, 40f);

    [Tooltip("Individual positional offsets for each path stone. Index matches stone index. If empty or out of range, falls back to Mascot Stone Offset.")]
    public Vector2[] mascotStoneOffsets;

    [Tooltip("Positional offset applied to the player mascot when positioned on the goal platform.")]
    public Vector2 mascotGoalPlatformOffset = new Vector2(0f, 40f);

    [Tooltip("Enter a stone index here (0 to 25) in the editor to temporarily move the mascot to that stone for offset preview. Set to -1 to disable.")]
    public int previewStoneIndex = -1;

    [Header("Flip Card UI")]
    public Button flipButton;
    public RectTransform coinTransform;
    public TextMeshProUGUI coinResultText;
    public Image coinImage;
    public Sprite headsSprite;
    public Sprite tailsSprite;
    public float coinFlipDuration = 1.0f;

    [Header("Word Banks for Digraphs")]
    public List<BirthdayBashWordData> shWords = new List<BirthdayBashWordData>();
    public List<BirthdayBashWordData> chWords = new List<BirthdayBashWordData>();
    public List<BirthdayBashWordData> thWords = new List<BirthdayBashWordData>();
    public List<BirthdayBashWordData> whWords = new List<BirthdayBashWordData>();

    [Header("Evaluation & Choice UI")]
    public GameObject choicePanel;
    public BirthdayBashOptionUI[] optionUIElements = new BirthdayBashOptionUI[3];
    public TextMeshProUGUI instructionLabel;

    [Header("Star Effect & Progress UI")]
    public TextMeshProUGUI scoreLabel;
    public GameObject starEffectObject;
    public GameObject nextButton;

    [Header("Audio Settings")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;
    public AudioClip introVoice;
    public AudioClip successVoice;
    public AudioClip coinFlipSFX;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip hopSFX;
    public AudioClip cheerSFX;
    public AudioClip levelCompleteSFX;

    [Header("Digraph Land Voices")]
    public AudioClip shLandAudio;
    public AudioClip chLandAudio;
    public AudioClip thLandAudio;
    public AudioClip whLandAudio;

    [Header("Feedback Settings")]
    public Color optionNormalColor = Color.white;
    public Color optionCorrectColor = Color.green;
    public Color optionWrongColor = Color.red;

    [Tooltip("The color used for the digraph text in the instruction label")]
    public Color instructionDigraphColor = Color.red;

    public Color[] defaultCardColors = new Color[] {
        new Color(0.47f, 0.72f, 0.31f), // Green
        new Color(0.95f, 0.76f, 0.29f), // Yellow
        new Color(0.24f, 0.51f, 0.82f)  // Blue
    };

    // Sequence of digraphs on the 26 path tiles
    private readonly string[] _pathDigraphs = new string[26] {
        "sh", "ch", "Th", "wh", "sh", "ch", "Th", "sh", "wh", "ch", "Th", "sh", "ch", "wh", "Th", "sh", "ch", "wh", "Th", "sh", "ch", "Th", "ch", "sh", "ch", "Th"
    };

    // Runtime state
    private int _currentStoneIndex = -1; // -1 represents START platform
    private bool _isGameCompleted = false;
    private bool _canTapChoices = false;
    private int _score = 0;
    private int _correctIndex = -1;
    private Coroutine _audioCoroutine;
    private int _pulsingTweenId = -1;
    private Transform _pulsingTarget = null;
    private Dictionary<int, Vector3> _originalCardScales = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> _originalCardPositions = new Dictionary<int, Vector3>();
    private Vector3 _originalMascotScale = Vector3.one;

    private void Awake()
    {
        FindFallbacks();

        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        // Cache card scales and positions
        for (int i = 0; i < optionUIElements.Length; i++)
        {
            var opt = optionUIElements[i];
            if (opt != null)
            {
                Transform targetTransform = opt.container != null ? opt.container.transform : (opt.cardButton != null ? opt.cardButton.transform : null);
                if (targetTransform != null)
                {
                    _originalCardScales[i] = targetTransform.localScale;
                    _originalCardPositions[i] = targetTransform.localPosition;
                }
                else
                {
                    _originalCardScales[i] = Vector3.one;
                    _originalCardPositions[i] = Vector3.zero;
                }
            }
        }

        PopulateDefaultWords();
    }

    private void Start()
    {
        if (flipButton != null)
        {
            flipButton.onClick.RemoveAllListeners();
            flipButton.onClick.AddListener(OnFlipButtonClicked);
        }

        for (int i = 0; i < optionUIElements.Length; i++)
        {
            int index = i;
            var opt = optionUIElements[i];
            if (opt != null && opt.cardButton != null)
            {
                opt.cardButton.onClick.RemoveAllListeners();
                opt.cardButton.onClick.AddListener(() => OnOptionTapped(index));
            }
        }

        if (nextButton != null)
        {
            nextButton.GetComponent<Button>()?.onClick.RemoveAllListeners();
            nextButton.GetComponent<Button>()?.onClick.AddListener(OnNextButtonClicked);
        }

        ResetActivity();
    }

    private void OnEnable()
    {
        ResetActivity();
        StartCoroutine(IntroSequence());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        StopPulsingCorrectButton();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
    }

    private void FindFallbacks()
    {
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
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        FindFallbacks();
        if (mascotCharacter != null)
        {
            if (previewStoneIndex >= 0 && previewStoneIndex < pathStones.Length && pathStones[previewStoneIndex] != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(pathStones[previewStoneIndex].position, GetOffsetForStone(previewStoneIndex));
            }
            else if (startPlatform != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(startPlatform.position, mascotStartPlatformOffset);
            }
            else if (pathStones.Length > 0 && pathStones[0] != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(pathStones[0].position, GetOffsetForStone(0));
            }
        }
    }
#endif

    public void ResetActivity()
    {
        Debug.Log("[BirthdayBashBoard] Resetting Activity...");
        _currentStoneIndex = -1;
        _score = 0;
        _isGameCompleted = false;
        _canTapChoices = false;

        UpdateScoreUI();

        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }

        if (choicePanel != null)
        {
            choicePanel.SetActive(false);
        }

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap Flip to move!";
        }

        // Initialize stones
        for (int i = 0; i < pathStones.Length; i++)
        {
            if (pathStones[i] == null) continue;

            if (stoneTexts != null && i < stoneTexts.Length && stoneTexts[i] != null)
            {
                stoneTexts[i].text = _pathDigraphs[i];
            }

            if (stoneCompletedHighlights != null && i < stoneCompletedHighlights.Length && stoneCompletedHighlights[i] != null)
            {
                stoneCompletedHighlights[i].gameObject.SetActive(false);
            }
        }

        // Place token on starting platform
        if (mascotCharacter != null)
        {
            if (startPlatform != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(startPlatform.position, mascotStartPlatformOffset);
            }
            else if (pathStones.Length > 0 && pathStones[0] != null)
            {
                mascotCharacter.localPosition = GetLocalPositionForWorldPoint(pathStones[0].position, GetOffsetForStone(0));
            }
        }

        if (flipButton != null)
        {
            flipButton.interactable = true;
            flipButton.gameObject.SetActive(true);
        }

        if (coinResultText != null)
        {
            coinResultText.text = "FLIP!";
            coinResultText.gameObject.SetActive(true);
        }
    }

    private IEnumerator IntroSequence()
    {
        if (flipButton != null) flipButton.interactable = false;

        if (mascotAudioSource != null && introVoice != null)
        {
            mascotAudioSource.clip = introVoice;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(introVoice.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (flipButton != null) flipButton.interactable = true;
    }

    private void OnFlipButtonClicked()
    {
        Debug.Log($"[BirthdayBashBoard] OnFlipButtonClicked. _isGameCompleted: {_isGameCompleted}, current index: {_currentStoneIndex}");
        if (_isGameCompleted) return;
        StartCoroutine(FlipRoutine());
    }

    private IEnumerator FlipRoutine()
    {
        Debug.Log("[BirthdayBashBoard] FlipRoutine started.");
        if (flipButton != null) flipButton.interactable = false;

        if (sfxAudioSource != null && coinFlipSFX != null)
        {
            sfxAudioSource.PlayOneShot(coinFlipSFX);
        }

        GameObject animationTarget = coinImage != null ? coinImage.gameObject : (coinTransform != null ? coinTransform.gameObject : null);
        int steps = Random.Range(1, 4); // 1, 2, or 3 steps
        Debug.Log($"[BirthdayBashBoard] Rolled steps: {steps}. Target GameObject for animation: {(animationTarget != null ? animationTarget.name : "null")}");

        if (animationTarget != null)
        {
            bool animDone = false;
            Vector3 originalScale = animationTarget.transform.localScale;

            LeanTween.scaleX(animationTarget, 0f, 0.15f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    animationTarget.transform.localScale = originalScale;
                    if (coinResultText != null)
                    {
                        coinResultText.text = $"+{steps}";
                    }
                    LeanTween.scale(animationTarget, originalScale * 1.25f, 0.2f)
                        .setEaseOutBack()
                        .setOnComplete(() => {
                            LeanTween.scale(animationTarget, originalScale, 0.15f)
                                .setOnComplete(() => {
                                    Debug.Log("[BirthdayBashBoard] Flip card animation fully completed.");
                                    animDone = true;
                                });
                        });
                });

            yield return new WaitUntil(() => animDone);
        }
        else
        {
            if (coinResultText != null)
            {
                coinResultText.text = $"+{steps}";
            }
            yield return new WaitForSeconds(0.8f);
        }

        if (flipButton != null)
        {
            flipButton.gameObject.SetActive(false);
        }
        if (coinResultText != null)
        {
            coinResultText.gameObject.SetActive(false);
        }

        int targetIndex = Mathf.Min(_currentStoneIndex + steps, pathStones.Length - 1);
        Debug.Log($"[BirthdayBashBoard] Starting HopSequence. Current index: {_currentStoneIndex}, Target index: {targetIndex}");
        yield return StartCoroutine(HopSequence(targetIndex));
    }

    private IEnumerator HopSequence(int targetIndex)
    {
        while (_currentStoneIndex < targetIndex)
        {
            int nextIndex = _currentStoneIndex + 1;
            Debug.Log($"[BirthdayBashBoard] Hop step checking. Current: {_currentStoneIndex}, Next: {nextIndex}, PathStones count: {pathStones.Length}");
            if (nextIndex >= pathStones.Length)
            {
                Debug.LogWarning("[BirthdayBashBoard] Break: nextIndex exceeds pathStones count.");
                break;
            }
            if (pathStones[nextIndex] == null)
            {
                Debug.LogError($"[BirthdayBashBoard] Break: pathStones[{nextIndex}] is null!");
                break;
            }
            if (mascotCharacter == null)
            {
                Debug.LogError("[BirthdayBashBoard] Break: mascotCharacter is null!");
                break;
            }

            Vector2 startPos = mascotCharacter.localPosition;
            Vector2 endPos = GetLocalPositionForWorldPoint(pathStones[nextIndex].position, GetOffsetForStone(nextIndex));
            Debug.Log($"[BirthdayBashBoard] Hopping from local {startPos} to local {endPos} (stone '{pathStones[nextIndex].name}')");

            if (sfxAudioSource != null && hopSFX != null)
            {
                sfxAudioSource.PlayOneShot(hopSFX);
            }

            bool stepDone = false;
            LeanTween.value(gameObject, 0f, 1f, stepDuration)
                .setOnUpdate((float t) => {
                    Vector2 currentPos = Vector2.Lerp(startPos, endPos, t);
                    float peak = Mathf.Sin(t * Mathf.PI) * jumpHeight;
                    currentPos.y += peak;
                    mascotCharacter.localPosition = currentPos;
                })
                .setOnComplete(() => {
                    Debug.Log($"[BirthdayBashBoard] Hop step completed for index {nextIndex}.");
                    _currentStoneIndex = nextIndex;
                    stepDone = true;
                });

            yield return new WaitUntil(() => stepDone);
        }

        Debug.Log($"[BirthdayBashBoard] HopSequence completed. Current stone index is now: {_currentStoneIndex}");
        yield return new WaitForSeconds(0.2f);
        OnArrivedAtStone();
    }

    private void OnArrivedAtStone()
    {
        if (_currentStoneIndex < 0 || _currentStoneIndex >= pathStones.Length)
        {
            StartCoroutine(VictorySequence());
            return;
        }

        string digraph = _pathDigraphs[_currentStoneIndex];

        // 1. Play digraph sound
        PlayDigraphLandSound(digraph);

        // 2. Build Choices UI
        BuildChoicesForDigraph(digraph);

        if (instructionLabel != null)
        {
            string hexColor = ColorUtility.ToHtmlStringRGB(instructionDigraphColor);
            instructionLabel.text = $"Find the word with the '<color=#{hexColor}>{digraph}</color>' sound!";
        }

        if (choicePanel != null)
        {
            choicePanel.SetActive(true);
            choicePanel.transform.localScale = Vector3.zero;
            LeanTween.cancel(choicePanel);
            LeanTween.scale(choicePanel, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        }
    }

    private void PlayDigraphLandSound(string digraph)
    {
        AudioClip landClip = null;
        switch (digraph.ToLower())
        {
            case "sh": landClip = shLandAudio; break;
            case "ch": landClip = chLandAudio; break;
            case "th": landClip = thLandAudio; break;
            case "wh": landClip = whLandAudio; break;
        }

        if (landClip != null && mascotAudioSource != null)
        {
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            _audioCoroutine = StartCoroutine(PlayAudioSequence(landClip, null));
        }
    }

    private void BuildChoicesForDigraph(string digraph)
    {
        // 1. Pick correct word
        BirthdayBashWordData correctWordData = GetRandomWordForDigraph(digraph);

        // 2. Pick 2 incorrect words from other digraphs
        List<string> otherDigraphs = new List<string> { "sh", "ch", "th", "wh" };
        otherDigraphs.Remove(digraph);
        // Shuffle other digraphs
        for (int i = 0; i < otherDigraphs.Count; i++)
        {
            string temp = otherDigraphs[i];
            int randIndex = Random.Range(i, otherDigraphs.Count);
            otherDigraphs[i] = otherDigraphs[randIndex];
            otherDigraphs[randIndex] = temp;
        }

        BirthdayBashWordData wrong1 = GetRandomWordForDigraph(otherDigraphs[0]);
        BirthdayBashWordData wrong2 = GetRandomWordForDigraph(otherDigraphs[1]);

        List<BirthdayBashWordData> choices = new List<BirthdayBashWordData> { correctWordData, wrong1, wrong2 };

        // Shuffle choices
        for (int i = 0; i < choices.Count; i++)
        {
            var temp = choices[i];
            int randIndex = Random.Range(i, choices.Count);
            choices[i] = choices[randIndex];
            choices[randIndex] = temp;
        }

        // Find the index of the correct word after shuffling
        _correctIndex = choices.IndexOf(correctWordData);

        // Apply choices to UI
        for (int i = 0; i < optionUIElements.Length; i++)
        {
            var opt = optionUIElements[i];
            if (opt == null) continue;

            if (opt.container != null) opt.container.SetActive(true);
            ResetOptionVisuals(i);

            if (opt.spellingTextLabel != null)
            {
                opt.spellingTextLabel.text = choices[i].word;
            }

            if (opt.cardBgImage != null && i < defaultCardColors.Length)
            {
                opt.cardBgImage.color = defaultCardColors[i];
            }
        }

        _canTapChoices = true;
    }

    private BirthdayBashWordData GetRandomWordForDigraph(string digraph)
    {
        List<BirthdayBashWordData> sourceList = null;
        switch (digraph.ToLower())
        {
            case "sh": sourceList = shWords; break;
            case "ch": sourceList = chWords; break;
            case "th": sourceList = thWords; break;
            case "wh": sourceList = whWords; break;
        }

        if (sourceList != null && sourceList.Count > 0)
        {
            return sourceList[Random.Range(0, sourceList.Count)];
        }

        return new BirthdayBashWordData { word = digraph }; // fallback
    }

    private void OnOptionTapped(int index)
    {
        if (!_canTapChoices) return;

        if (index == _correctIndex)
        {
            StartCoroutine(CorrectChoiceRoutine(index));
        }
        else
        {
            StartCoroutine(WrongChoiceRoutine(index));
        }
    }

    private IEnumerator CorrectChoiceRoutine(int index)
    {
        _canTapChoices = false;
        StopPulsingCorrectButton();

        if (sfxAudioSource != null)
        {
            if (correctSFX != null) sfxAudioSource.PlayOneShot(correctSFX);
            if (cheerSFX != null) StartCoroutine(PlaySFXDelay(cheerSFX, 0.4f));
        }

        // Highlight correct green
        SetOptionBorderColor(index, optionCorrectColor);

        // Highlight visual path stone
        if (stoneCompletedHighlights != null && _currentStoneIndex < stoneCompletedHighlights.Length && stoneCompletedHighlights[_currentStoneIndex] != null)
        {
            stoneCompletedHighlights[_currentStoneIndex].gameObject.SetActive(true);
        }

        // Pop card animation
        var opt = optionUIElements[index];
        Transform targetTransform = opt.container != null ? opt.container.transform : (opt.cardButton != null ? opt.cardButton.transform : null);
        if (targetTransform != null)
        {
            LeanTween.cancel(targetTransform.gameObject);
            LeanTween.scale(targetTransform.gameObject, _originalCardScales[index] * 1.15f, 0.3f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(targetTransform.gameObject, _originalCardScales[index], 0.2f)
                        .setEase(LeanTweenType.easeOutQuad)
                        .setDelay(0.1f);
                });
        }

        // Pop stars
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
            var popEffect = starEffectObject.GetComponent<POPEffect_SeniorLev1A>();
            if (popEffect != null)
            {
                popEffect.enabled = false;
                popEffect.enabled = true;
            }
        }

        _score++;
        UpdateScoreUI();

        // Play word audio if available
        string digraph = _pathDigraphs[_currentStoneIndex];
        BirthdayBashWordData currentCorrectWord = null;
        if (opt.spellingTextLabel != null)
        {
            string wordText = opt.spellingTextLabel.text;
            List<BirthdayBashWordData> wordList = null;
            switch (digraph.ToLower())
            {
                case "sh": wordList = shWords; break;
                case "ch": wordList = chWords; break;
                case "th": wordList = thWords; break;
                case "wh": wordList = whWords; break;
            }
            if (wordList != null)
            {
                currentCorrectWord = wordList.Find(w => w.word == wordText);
            }
        }

        if (currentCorrectWord != null && currentCorrectWord.wordAudio != null && mascotAudioSource != null)
        {
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            _audioCoroutine = StartCoroutine(PlayAudioSequence(currentCorrectWord.wordAudio, null));
        }

        yield return new WaitForSeconds(1.2f);

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        // Hide choice panel
        if (choicePanel != null)
        {
            bool shrinkDone = false;
            LeanTween.scale(choicePanel, Vector3.zero, 0.25f).setOnComplete(() => {
                choicePanel.SetActive(false);
                shrinkDone = true;
            });
            yield return new WaitUntil(() => shrinkDone);
        }

        // Check if finished
        if (_currentStoneIndex >= pathStones.Length - 1)
        {
            StartCoroutine(VictorySequence());
        }
        else
        {
            if (flipButton != null)
            {
                flipButton.interactable = true;
                flipButton.gameObject.SetActive(true);
            }
            if (coinResultText != null)
            {
                coinResultText.text = "FLIP!";
                coinResultText.gameObject.SetActive(true);
            }
            if (instructionLabel != null)
            {
                instructionLabel.text = "Tap Flip to move!";
            }
        }
    }

    private IEnumerator WrongChoiceRoutine(int index)
    {
        _canTapChoices = false;
        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        // Highlight red
        SetOptionBorderColor(index, optionWrongColor);

        // Shake option card
        var opt = optionUIElements[index];
        Transform targetTransform = opt.container != null ? opt.container.transform : (opt.cardButton != null ? opt.cardButton.transform : null);
        if (targetTransform != null)
        {
            LeanTween.cancel(targetTransform.gameObject);
            float shakeAmount = 15f;
            Vector3 origPos = _originalCardPositions[index];
            LeanTween.moveLocalX(targetTransform.gameObject, origPos.x + shakeAmount, 0.05f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    targetTransform.localPosition = origPos;
                    StartCoroutine(ResetVisualsAfterDelay(index, 0.8f));
                });
        }
        else
        {
            StartCoroutine(ResetVisualsAfterDelay(index, 0.8f));
        }

        // Replay land voice as reminder
        string digraph = _pathDigraphs[_currentStoneIndex];
        PlayDigraphLandSound(digraph);

        yield return new WaitForSeconds(0.6f);

        // Pulse correct card
        PulseCorrectButton(_correctIndex);

        _canTapChoices = true;
    }

    private void PulseCorrectButton(int correctIdx)
    {
        StopPulsingCorrectButton();

        if (correctIdx < 0 || correctIdx >= optionUIElements.Length) return;

        var opt = optionUIElements[correctIdx];
        if (opt == null) return;

        Transform targetTransform = opt.container != null ? opt.container.transform : (opt.cardButton != null ? opt.cardButton.transform : null);
        if (targetTransform == null) return;

        _pulsingTarget = targetTransform;
        Vector3 origScale = _originalCardScales[correctIdx];

        _pulsingTweenId = LeanTween.scale(targetTransform.gameObject, origScale * 1.12f, 0.45f)
            .setEase(LeanTweenType.easeInOutQuad)
            .setLoopPingPong()
            .id;
    }

    private void StopPulsingCorrectButton()
    {
        if (_pulsingTweenId != -1)
        {
            LeanTween.cancel(_pulsingTweenId);
            _pulsingTweenId = -1;
        }

        if (_pulsingTarget != null)
        {
            int idx = -1;
            for (int i = 0; i < optionUIElements.Length; i++)
            {
                var opt = optionUIElements[i];
                if (opt != null)
                {
                    Transform t = opt.container != null ? opt.container.transform : (opt.cardButton != null ? opt.cardButton.transform : null);
                    if (t == _pulsingTarget)
                    {
                        idx = i;
                        break;
                    }
                }
            }

            if (idx != -1)
            {
                _pulsingTarget.localScale = _originalCardScales[idx];
            }
            _pulsingTarget = null;
        }
    }

    private IEnumerator ResetVisualsAfterDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetOptionVisuals(index);
    }

    private void ResetOptionVisuals(int index)
    {
        if (index < 0 || index >= optionUIElements.Length) return;

        SetOptionBorderColor(index, optionNormalColor);

        var opt = optionUIElements[index];
        if (opt != null)
        {
            Transform targetTransform = opt.container != null ? opt.container.transform : (opt.cardButton != null ? opt.cardButton.transform : null);
            if (targetTransform != null)
            {
                targetTransform.localScale = _originalCardScales[index];
                targetTransform.localPosition = _originalCardPositions[index];
            }
        }
    }

    private void SetOptionBorderColor(int index, Color color)
    {
        if (index < 0 || index >= optionUIElements.Length) return;

        var opt = optionUIElements[index];
        if (opt == null) return;

        if (opt.highlightBorder != null)
        {
            opt.highlightBorder.color = color;
        }
        else if (opt.cardButton != null)
        {
            var img = opt.cardButton.GetComponent<Image>();
            if (img != null) img.color = color;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = _score.ToString();
            LeanTween.cancel(scoreLabel.gameObject);
            scoreLabel.transform.localScale = Vector3.one * 1.3f;
            LeanTween.scale(scoreLabel.gameObject, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutQuad);
        }
    }

    private IEnumerator PlayAudioSequence(AudioClip clip, System.Action onComplete)
    {
        if (clip != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = clip;
            mascotAudioSource.Play();

            // Mascot talking bounce animation
            if (mascotCharacter != null)
            {
                LeanTween.cancel(mascotCharacter.gameObject);
                LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.05f, 0.25f)
                    .setLoopPingPong(Mathf.CeilToInt(clip.length / 0.5f));
            }

            yield return new WaitForSeconds(clip.length);

            if (mascotCharacter != null)
            {
                LeanTween.cancel(mascotCharacter.gameObject);
                LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
            }
        }
        onComplete?.Invoke();
    }

    private IEnumerator VictorySequence()
    {
        _isGameCompleted = true;
        if (flipButton != null)
        {
            flipButton.interactable = false;
            flipButton.gameObject.SetActive(false);
        }
        if (coinResultText != null)
        {
            coinResultText.gameObject.SetActive(false);
        }

        // Mascot jumps to the Goal Platform (The Birthday Party!)
        if (mascotCharacter != null && goalPlatform != null)
        {
            Vector2 startPos = mascotCharacter.localPosition;
            Vector2 endPos = GetLocalPositionForWorldPoint(goalPlatform.position, mascotGoalPlatformOffset);

            if (sfxAudioSource != null && hopSFX != null)
            {
                sfxAudioSource.PlayOneShot(hopSFX);
            }

            bool jumpDone = false;
            LeanTween.value(gameObject, 0f, 1f, stepDuration)
                .setOnUpdate((float t) => {
                    Vector2 currentPos = Vector2.Lerp(startPos, endPos, t);
                    currentPos.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
                    mascotCharacter.localPosition = currentPos;
                })
                .setOnComplete(() => jumpDone = true);

            yield return new WaitUntil(() => jumpDone);
        }

        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        if (mascotAudioSource != null && successVoice != null)
        {
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            _audioCoroutine = StartCoroutine(PlayAudioSequence(successVoice, null));
            yield return new WaitForSeconds(successVoice.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(0.8f);
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
            Debug.LogWarning("[BirthdayBashBoard] GameFlowManager_Senior_Phonics not found, resetting activity.");
            ResetActivity();
        }
    }

    private IEnumerator PlaySFXDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
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

    public void PopulateDefaultWords()
    {
        string[] rawSh = { "ship", "wish", "sheep", "shelf", "shadow", "shower", "shop", "shell", "cash", "fish", "rush", "shock" };
        string[] rawCh = { "chick", "chest", "much", "rich", "chop", "chat", "change", "chew", "cheese", "chicken", "chair", "chalk" };
        string[] rawTh = { "thumb", "thorn", "that", "thanks", "thin", "moth", "path", "bath", "with", "this" };
        string[] rawWh = { "wheel", "whistle", "whale", "where", "whip", "white", "what", "when", "why" };

        if (shWords == null || shWords.Count == 0)
        {
            shWords = new List<BirthdayBashWordData>();
            foreach (var str in rawSh) shWords.Add(new BirthdayBashWordData { word = str });
        }
        if (chWords == null || chWords.Count == 0)
        {
            chWords = new List<BirthdayBashWordData>();
            foreach (var str in rawCh) chWords.Add(new BirthdayBashWordData { word = str });
        }
        if (thWords == null || thWords.Count == 0)
        {
            thWords = new List<BirthdayBashWordData>();
            foreach (var str in rawTh) thWords.Add(new BirthdayBashWordData { word = str });
        }
        if (whWords == null || whWords.Count == 0)
        {
            whWords = new List<BirthdayBashWordData>();
            foreach (var str in rawWh) whWords.Add(new BirthdayBashWordData { word = str });
        }
    }

    private void Reset()
    {
        PopulateDefaultWords();
    }
}
