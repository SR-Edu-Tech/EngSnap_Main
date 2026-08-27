using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.Networking;

[System.Serializable]
public class BlendBuilderQuestion
{
    [Tooltip("The full correct word, e.g. 'brain'")]
    public string wordText;

    [Tooltip("The blend letters, e.g. 'br'")]
    public string blendLetters;

    [Tooltip("The ending family letters, e.g. 'ain'")]
    public string endingLetters;

    [Tooltip("The picture sprite for this word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for mascot saying the word normally (or when correct)")]
    public AudioClip wordAudioNormal;

    [Tooltip("Audio clip for mascot saying the word slowly (or repeating on incorrect)")]
    public AudioClip wordAudioSlow;

    [Tooltip("Index of the correct option in the options array (0-based). Configured automatically.")]
    public int correctOptionIndex;

    [Tooltip("The spelling choices shown for this question (usually 7 choices)")]
    [SerializeField]
    private string[] spellingChoices = new string[7];

    public string[] options
    {
        get { return spellingChoices; }
        set { spellingChoices = value; }
    }
}

[System.Serializable]
public class BlendBuilderOptionUI
{
    [Tooltip("The parent GameObject of this option card (to show/hide)")]
    public GameObject container;

    [Tooltip("The main button component for this option card")]
    public Button cardButton;

    [Tooltip("The text component displaying the option spelling")]
    public TextMeshProUGUI spellingTextLabel;

    [Tooltip("The highlight border image component (for green/red feedback)")]
    public Image highlightBorder;

    [Tooltip("Optional background image component to apply default colors")]
    public Image cardBgImage;
}

public class BlendBuilder_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<BlendBuilderQuestion> questions = new List<BlendBuilderQuestion>();

    [Header("UI Components - Options Tray")]
    [Tooltip("The UI elements mapped for each option slot in the tray (usually 7)")]
    public BlendBuilderOptionUI[] optionUIElements;

    [Header("UI Colors - Option Backgrounds")]
    [Tooltip("The default card background colors in order")]
    public Color[] defaultCardColors = new Color[] {
        new Color(0.47f, 0.72f, 0.31f), // Green
        new Color(0.95f, 0.76f, 0.29f), // Yellow
        new Color(0.24f, 0.51f, 0.82f), // Blue
        new Color(0.90f, 0.40f, 0.50f), // Pink
        new Color(0.58f, 0.40f, 0.74f), // Purple
        new Color(0.95f, 0.53f, 0.22f), // Orange
        new Color(0.18f, 0.70f, 0.72f)  // Teal
    };

    [Header("UI Components - General")]
    public Image wordImage;
    public TextMeshProUGUI wordTextLabel;
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI instructionLabel;

    public GameObject continueButton;
    public RectTransform mascotCharacter;
    public GameObject starEffectObject;

    [Header("Progress & Indicators")]
    public TextMeshProUGUI progressLabel;
    public RectTransform progressDotsContainer;
    public GameObject progressDotPrefab;
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = Color.gray;
    public Color dotFilledColor = Color.green;

    [Header("Audio Sources")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    public AudioClip introVoice;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip cheerSFX;
    public AudioClip levelCompleteSFX;

    [Header("Feedback Settings")]
    public Color optionNormalColor = Color.white;
    public Color optionCorrectColor = Color.green;
    public Color optionWrongColor = Color.red;

    [Tooltip("Hex color code for the filled correct letters in the word text")]
    public string correctColorHex = "#4CAF50";

    [Tooltip("Hex color code for the filled incorrect letters in the word text")]
    public string wrongColorHex = "#F44336";

    [Header("Animation Config")]
    [Tooltip("Horizontal spacing offset when displaying separated blend letters initially")]
    public float letterSpacing = 80f;
    [Tooltip("Duration of the glide-together animation")]
    public float glideDuration = 0.6f;

    // Runtime state
    private int _currentIndex = 0;
    private bool _started = false;
    private bool _canTap = false;
    private int _score = 0;
    private List<GameObject> _dotInstances = new List<GameObject>();
    private bool _isCurrentQuestionCorrect = false;
    private bool _introPlayed = false;

    private Vector3 _originalMascotScale = Vector3.one;
    private Dictionary<int, Vector3> _originalCardScales = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> _originalCardPositions = new Dictionary<int, Vector3>();
    private Dictionary<int, Color> _originalCardColors = new Dictionary<int, Color>();
    private GameFlowManager_Senior_Phonics _flowManager;
    private Coroutine _gameplayCoroutine;
    private int _pulsingTweenId = -1;
    private Transform _pulsingTarget = null;

    private TextMeshProUGUI _tempLetter1;
    private TextMeshProUGUI _tempLetter2;

#if UNITY_EDITOR
    private void OnValidate()
    {
        bool needsRepopulate = false;
        if (questions == null || questions.Count == 0)
        {
            needsRepopulate = true;
        }
        else
        {
            foreach (var q in questions)
            {
                if (string.IsNullOrEmpty(q.wordText) || string.IsNullOrEmpty(q.blendLetters))
                {
                    needsRepopulate = true;
                    break;
                }
            }
        }

        if (needsRepopulate)
        {
            PopulateDefaultQuestions();
        }
        else
        {
            // If the user has questions, let's automatically generate the choices for them
            // if they are not already set up!
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                if (string.IsNullOrEmpty(q.wordText)) continue;

                // Let's check if the options are empty or size 0
                bool optionsEmpty = true;
                if (q.options != null && q.options.Length > 0)
                {
                    foreach (var opt in q.options)
                    {
                        if (!string.IsNullOrEmpty(opt))
                        {
                            optionsEmpty = false;
                            break;
                        }
                    }
                }

                if (optionsEmpty)
                {
                    // Generate 7 spelling choices automatically
                    List<string> choices = new List<string>();
                    string correctEnding = !string.IsNullOrEmpty(q.endingLetters) ? q.endingLetters : "";
                    choices.Add(correctEnding);

                    // Add some dummy/fallback options from other default endings if we don't have enough
                    string[] defaultEndings = { "ain", "ock", "oud", "ab", "og", "ass", "ue", "apes" };
                    List<string> pool = new List<string>(defaultEndings);
                    pool.Remove(correctEnding);

                    // Shuffle pool and take 6
                    for (int k = 0; k < pool.Count; k++)
                    {
                        int tempIdx = (k * 7 + i) % pool.Count;
                        string tempVal = pool[k];
                        pool[k] = pool[tempIdx];
                        pool[tempIdx] = tempVal;
                    }

                    for (int j = 0; j < 6 && j < pool.Count; j++)
                    {
                        choices.Add(pool[j]);
                    }

                    // Shuffle final choices list
                    for (int k = 0; k < choices.Count; k++)
                    {
                        int tempIdx = (k * 3 + i) % choices.Count;
                        string tempVal = choices[k];
                        choices[k] = choices[tempIdx];
                        choices[tempIdx] = tempVal;
                    }

                    q.options = choices.ToArray();
                    q.correctOptionIndex = choices.IndexOf(correctEnding);
                }
                else
                {
                    // If options are already set up, just update correctOptionIndex
                    if (q.options != null)
                    {
                        q.correctOptionIndex = -1;
                        for (int k = 0; k < q.options.Length; k++)
                        {
                            if (q.options[k] == q.endingLetters)
                            {
                                q.correctOptionIndex = k;
                                break;
                            }
                        }
                    }
                }
            }

            UpdateOptionIndexes();
        }
    }

    private void Reset()
    {
        // Auto-wire UI Components from children
        Transform optionsTray = transform.Find("OptionsTray");
        if (optionsTray != null)
        {
            var optList = new List<BlendBuilderOptionUI>();
            for (int i = 0; i < 7; i++)
            {
                Transform cardTrans = optionsTray.Find($"OptionCard_{i}");
                if (cardTrans != null)
                {
                    BlendBuilderOptionUI ui = new BlendBuilderOptionUI();
                    ui.container = cardTrans.gameObject;
                    ui.cardButton = cardTrans.GetComponent<Button>();
                    
                    Transform textTrans = cardTrans.Find("Text");
                    if (textTrans != null)
                    {
                        ui.spellingTextLabel = textTrans.GetComponent<TextMeshProUGUI>();
                    }
                    
                    Transform borderTrans = cardTrans.Find("HighlightBorder");
                    if (borderTrans != null)
                    {
                        ui.highlightBorder = borderTrans.GetComponent<Image>();
                    }

                    ui.cardBgImage = cardTrans.GetComponent<Image>();
                    optList.Add(ui);
                }
            }
            if (optList.Count > 0)
            {
                optionUIElements = optList.ToArray();
            }
        }

        // Auto-wire other general components
        Transform imgTrans = transform.Find("WordImage");
        if (imgTrans != null) wordImage = imgTrans.GetComponent<Image>();

        Transform txtTrans = transform.Find("WordTextLabel");
        if (txtTrans != null) wordTextLabel = txtTrans.GetComponent<TextMeshProUGUI>();

        Transform scoreTrans = transform.Find("ScoreLabel");
        if (scoreTrans != null) scoreLabel = scoreTrans.GetComponent<TextMeshProUGUI>();

        Transform instTrans = transform.Find("InstructionLabel");
        if (instTrans != null) instructionLabel = instTrans.GetComponent<TextMeshProUGUI>();



        Transform continueTrans = transform.Find("ContinueButton");
        if (continueTrans != null) continueButton = continueTrans.gameObject;

        Transform starTrans = transform.Find("StarEffect");
        if (starTrans != null) starEffectObject = starTrans.gameObject;

        Transform progressTextTrans = transform.Find("ProgressTextLabel");
        if (progressTextTrans != null) progressLabel = progressTextTrans.GetComponent<TextMeshProUGUI>();

        Transform dotsTrans = transform.Find("ProgressDotsContainer");
        if (dotsTrans != null)
        {
            progressDotsContainer = dotsTrans.GetComponent<RectTransform>();
            Transform templateTrans = dotsTrans.Find("ProgressDotTemplate");
            if (templateTrans != null) progressDotPrefab = templateTrans.gameObject;
        }

        // Link Mascot Character if it exists in the scene
        GameObject mascotObj = GameObject.Find("Character");
        if (mascotObj == null) mascotObj = GameObject.Find("MascotCharacter");
        if (mascotObj != null) mascotCharacter = mascotObj.GetComponent<RectTransform>();
    }
#endif

    private void Awake()
    {
        // Cache original dimensions/states
        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

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

                Image cardBg = opt.cardBgImage != null ? opt.cardBgImage : (opt.cardButton != null ? opt.cardButton.GetComponent<Image>() : null);
                if (cardBg != null)
                {
                    _originalCardColors[i] = cardBg.color;
                }
                else
                {
                    _originalCardColors[i] = Color.white;
                }
            }
        }

        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (mascotAudioSource == null)
        {
            mascotAudioSource = GetComponent<AudioSource>();
            if (mascotAudioSource == null)
            {
                mascotAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        bool needsRepopulate = false;
        if (questions == null || questions.Count == 0)
        {
            needsRepopulate = true;
        }
        else
        {
            foreach (var q in questions)
            {
                if (string.IsNullOrEmpty(q.wordText) || string.IsNullOrEmpty(q.blendLetters))
                {
                    needsRepopulate = true;
                    break;
                }
            }
        }

        if (needsRepopulate)
        {
            PopulateDefaultQuestions();
        }
        else
        {
            UpdateOptionIndexes();
        }
        
        StartPreloadingLetterSounds();
    }

    private void Start()
    {
        _started = true;



        if (continueButton != null)
        {
            var btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnContinueClicked);
            }
        }

        // Setup individual option click listeners
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

        ResetToStart();
    }

    private void OnEnable()
    {
        if (_started)
        {
            ResetToStart();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        StopPulsingCorrectButton();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
        CleanupTempLetters();
    }

    private void CleanupTempLetters()
    {
        if (_tempLetter1 != null) { Destroy(_tempLetter1.gameObject); _tempLetter1 = null; }
        if (_tempLetter2 != null) { Destroy(_tempLetter2.gameObject); _tempLetter2 = null; }
    }

    public void ResetToStart()
    {
        _currentIndex = 0;
        _score = 0;
        _introPlayed = false;
        UpdateScoreUI();

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        SetupProgressDots();
        LoadQuestion(_currentIndex);
    }

    private void UpdateOptionIndexes()
    {
        if (questions == null) return;
        foreach (var q in questions)
        {
            if (q.options != null)
            {
                q.correctOptionIndex = -1;
                for (int i = 0; i < q.options.Length; i++)
                {
                    if (q.options[i] == q.endingLetters)
                    {
                        q.correctOptionIndex = i;
                        break;
                    }
                }
            }
        }
    }

    private void PopulateDefaultQuestions()
    {
        questions = new List<BlendBuilderQuestion>();

        // Define the 8 words, blends, and endings
        string[] words = { "brain", "clock", "cloud", "crab", "frog", "glass", "glue", "grapes" };
        string[] blends = { "br", "cl", "cl", "cr", "fr", "gl", "gl", "gr" };
        string[] endings = { "ain", "ock", "oud", "ab", "og", "ass", "ue", "apes" };

        for (int i = 0; i < words.Length; i++)
        {
            BlendBuilderQuestion q = new BlendBuilderQuestion();
            q.wordText = words[i];
            q.blendLetters = blends[i];
            q.endingLetters = endings[i];
            q.wordSprite = null;
            q.wordAudioNormal = null;
            q.wordAudioSlow = null;

            // Generate options
            List<string> choices = new List<string>();
            choices.Add(endings[i]); // Correct ending

            // Add other unique endings
            List<string> otherEndings = new List<string>(endings);
            otherEndings.Remove(endings[i]);
            
            // Simple deterministic shuffle fallback
            for (int k = 0; k < otherEndings.Count; k++)
            {
                int tempIdx = (k * 7 + i) % otherEndings.Count;
                string tempVal = otherEndings[k];
                otherEndings[k] = otherEndings[tempIdx];
                otherEndings[tempIdx] = tempVal;
            }

            for (int j = 0; j < 6 && j < otherEndings.Count; j++)
            {
                choices.Add(otherEndings[j]);
            }

            // Shuffle final choices list
            for (int k = 0; k < choices.Count; k++)
            {
                int tempIdx = (k * 3 + i) % choices.Count;
                string tempVal = choices[k];
                choices[k] = choices[tempIdx];
                choices[tempIdx] = tempVal;
            }

            q.options = choices.ToArray();
            q.correctOptionIndex = choices.IndexOf(endings[i]);

            questions.Add(q);
        }
    }

    private void LoadQuestion(int index)
    {
        _currentIndex = index;
        _isCurrentQuestionCorrect = false;
        _canTap = false;
        CleanupTempLetters();

        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[BlendBuilder] No questions configured!");
            return;
        }

        if (index < 0 || index >= questions.Count)
        {
            OnCompletedAll();
            return;
        }

        var data = questions[index];

        StopPulsingCorrectButton();
        UpdateProgressLabel();
        UpdateProgressDots();

        // 1. Initially clear word text label (we will show temporary glide letters)
        if (wordTextLabel != null)
        {
            wordTextLabel.text = "";
            wordTextLabel.gameObject.SetActive(true);
        }

        // 2. Setup image representation
        if (wordImage != null)
        {
            wordImage.gameObject.SetActive(false); // Hide image during glide build-up
        }

        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Watch the letters glide together!";
        }

        // Hide Options Tray during glide build-up
        SetOptionsTrayActive(false);

        // Animate mascot entry and start build-up sequence
        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = Vector3.zero;
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.45f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => StartGameplaySequence(data));
        }
        else
        {
            StartGameplaySequence(data);
        }
    }

    private void SetOptionsTrayActive(bool active)
    {
        var data = questions[_currentIndex];
        string[] currentChoices = data.options;

        for (int i = 0; i < optionUIElements.Length; i++)
        {
            var opt = optionUIElements[i];
            if (opt == null) continue;

            var containerObj = opt.container != null ? opt.container : (opt.cardButton != null ? opt.cardButton.gameObject : null);
            if (containerObj != null)
            {
                if (active && i < currentChoices.Length)
                {
                    containerObj.SetActive(true);
                    ResetOptionVisuals(i);

                    if (opt.spellingTextLabel != null)
                    {
                        opt.spellingTextLabel.text = currentChoices[i];
                    }


                }
                else
                {
                    containerObj.SetActive(false);
                }
            }
        }
    }

    private void StartGameplaySequence(BlendBuilderQuestion data)
    {
        if (_gameplayCoroutine != null)
        {
            StopCoroutine(_gameplayCoroutine);
        }
        _gameplayCoroutine = StartCoroutine(WordBuildUpSequence(data));
    }

    private IEnumerator WordBuildUpSequence(BlendBuilderQuestion data)
    {
        _canTap = false;

        if (_currentIndex == 0 && !_introPlayed && introVoice != null && mascotAudioSource != null)
        {
            _introPlayed = true;
            mascotAudioSource.clip = introVoice;
            mascotAudioSource.Play();
            yield return StartCoroutine(MascotTalkAnimation(introVoice.length));
            yield return new WaitForSeconds(0.5f);
        }

        // Try load assets dynamically if not pre-configured
        yield return StartCoroutine(LoadAssetsIfNeeded(data));

        if (wordTextLabel == null) yield break;

        // 1. Create two temporary letter objects cloned from wordTextLabel
        GameObject go1 = Instantiate(wordTextLabel.gameObject, wordTextLabel.transform.parent);
        GameObject go2 = Instantiate(wordTextLabel.gameObject, wordTextLabel.transform.parent);

        _tempLetter1 = go1.GetComponent<TextMeshProUGUI>();
        _tempLetter2 = go2.GetComponent<TextMeshProUGUI>();

        string bl = !string.IsNullOrEmpty(data.blendLetters) ? data.blendLetters : "sp";
        char c1 = bl[0];
        char c2 = bl.Length > 1 ? bl[1] : ' ';

        _tempLetter1.text = c1.ToString();
        _tempLetter2.text = c2 != ' ' ? c2.ToString() : "";

        _tempLetter1.gameObject.SetActive(false);
        _tempLetter2.gameObject.SetActive(false);

        // Position them left and right of center
        Vector3 centerPos = wordTextLabel.transform.localPosition;
        _tempLetter1.transform.localPosition = centerPos + new Vector3(-letterSpacing, 0f, 0f);
        _tempLetter2.transform.localPosition = centerPos + new Vector3(letterSpacing, 0f, 0f);

        yield return new WaitForSeconds(0.3f);

        // --- STEP A: Show and sound Letter 1 ---
        _tempLetter1.gameObject.SetActive(true);
        _tempLetter1.transform.localScale = Vector3.zero;
        LeanTween.scale(_tempLetter1.gameObject, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        
        AudioClip clip1 = GetLetterSound(c1);
        float clip1Len = 0.8f;
        if (clip1 != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(clip1);
            clip1Len = clip1.length;
        }
        StartCoroutine(WiggleAnimation(_tempLetter1.transform, Vector3.one, Quaternion.identity, clip1Len));
        yield return StartCoroutine(MascotTalkAnimation(clip1Len));
        yield return new WaitForSeconds(0.2f);

        // --- STEP B: Show and sound Letter 2 ---
        AudioClip clip2 = null;
        if (c2 != ' ')
        {
            _tempLetter2.gameObject.SetActive(true);
            _tempLetter2.transform.localScale = Vector3.zero;
            LeanTween.scale(_tempLetter2.gameObject, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);

            clip2 = GetLetterSound(c2);
            float clip2Len = 0.8f;
            if (clip2 != null && mascotAudioSource != null)
            {
                mascotAudioSource.PlayOneShot(clip2);
                clip2Len = clip2.length;
            }
            StartCoroutine(WiggleAnimation(_tempLetter2.transform, Vector3.one, Quaternion.identity, clip2Len));
            yield return StartCoroutine(MascotTalkAnimation(clip2Len));
            yield return new WaitForSeconds(0.3f);
        }

        // --- STEP C: Glide them together ---
        if (sfxAudioSource != null && cheerSFX != null)
        {
            sfxAudioSource.PlayOneShot(cheerSFX, 0.3f);
        }

        float elapsed = 0f;
        Vector3 startPos1 = _tempLetter1.transform.localPosition;
        Vector3 startPos2 = _tempLetter2.transform.localPosition;
        Vector3 targetPos1 = centerPos + new Vector3(-12f, 0f, 0f);
        Vector3 targetPos2 = centerPos + new Vector3(12f, 0f, 0f);

        while (elapsed < glideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / glideDuration);
            t = t * (2f - t);

            if (_tempLetter1 != null) _tempLetter1.transform.localPosition = Vector3.Lerp(startPos1, targetPos1, t);
            if (_tempLetter2 != null) _tempLetter2.transform.localPosition = Vector3.Lerp(startPos2, targetPos2, t);
            yield return null;
        }

        // --- STEP D: Form the Blend ---
        CleanupTempLetters();

        if (wordTextLabel != null)
        {
            wordTextLabel.text = data.blendLetters + "__";
            wordTextLabel.transform.localScale = Vector3.one * 1.15f;
            LeanTween.scale(wordTextLabel.gameObject, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutQuad);
        }

        if (sfxAudioSource != null && correctSFX != null)
        {
            sfxAudioSource.PlayOneShot(correctSFX, 0.6f);
        }

        AudioClip blendClip = GetBlendSound(data.blendLetters);
        float blendLen = 0.8f;
        if (blendClip != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(blendClip);
            blendLen = blendClip.length;
        }
        else
        {
            // Sequential fallback
            if (clip1 != null && mascotAudioSource != null)
            {
                mascotAudioSource.PlayOneShot(clip1);
                yield return new WaitForSeconds(clip1.length * 0.5f);
            }
            if (clip2 != null && mascotAudioSource != null)
            {
                mascotAudioSource.PlayOneShot(clip2);
                yield return new WaitForSeconds(clip2.length * 0.5f);
            }
        }

        if (wordTextLabel != null)
        {
            StartCoroutine(WiggleAnimation(wordTextLabel.transform, Vector3.one, Quaternion.identity, blendLen));
        }
        yield return StartCoroutine(MascotTalkAnimation(blendLen));
        yield return new WaitForSeconds(0.3f);

        // --- STEP E: Reveal options tray and image ---
        if (wordImage != null && data.wordSprite != null)
        {
            wordImage.sprite = data.wordSprite;
            wordImage.gameObject.SetActive(true);
            wordImage.transform.localScale = Vector3.zero;
            LeanTween.scale(wordImage.gameObject, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap the correct ending to finish the word.";
        }

        SetOptionsTrayActive(true);
        _canTap = true;
    }

    private void OnOptionTapped(int index)
    {
        if (!_canTap) return;

        var data = questions[_currentIndex];
        if (index < 0 || index >= data.options.Length) return;

        if (index == data.correctOptionIndex)
        {
            HandleCorrectChoice(index);
        }
        else
        {
            HandleIncorrectChoice(index, data.options[index]);
        }
    }

    private void HandleCorrectChoice(int index)
    {
        _canTap = false;
        _isCurrentQuestionCorrect = true;
        StopPulsingCorrectButton();

        var data = questions[_currentIndex];

        if (instructionLabel != null)
        {
            instructionLabel.text = "Well done!";
        }

        if (sfxAudioSource != null)
        {
            if (correctSFX != null) sfxAudioSource.PlayOneShot(correctSFX);
            if (cheerSFX != null) StartCoroutine(PlaySFXDelay(cheerSFX, 0.4f));
        }

        SetOptionBorderColor(index, optionCorrectColor);

        var opt = optionUIElements[index];
        Transform targetTransform = opt.container != null ? opt.container.transform : (opt.cardButton != null ? opt.cardButton.transform : null);
        if (targetTransform != null)
        {
            LeanTween.cancel(targetTransform.gameObject);
            LeanTween.scale(targetTransform.gameObject, _originalCardScales[index] * 1.15f, 0.3f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(targetTransform.gameObject, _originalCardScales[index], 0.2f)
                        .setEase(LeanTweenType.easeOutQuad);
                });
        }

        if (wordTextLabel != null)
        {
            string endingColored = $"<color={correctColorHex}>{data.endingLetters}</color>";
            wordTextLabel.text = data.blendLetters + endingColored;
        }

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

        if (_gameplayCoroutine != null)
        {
            StopCoroutine(_gameplayCoroutine);
        }
        _gameplayCoroutine = StartCoroutine(PlayFinalWordAudioSequence(data));

        UpdateProgressDots();
    }

    private IEnumerator PlayFinalWordAudioSequence(BlendBuilderQuestion data)
    {
        AudioClip endingClip = GetEndingSound(data.endingLetters);
        if (endingClip != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(endingClip);
            yield return StartCoroutine(MascotTalkAnimation(endingClip.length));
            yield return new WaitForSeconds(0.2f);
        }

        if (data.wordAudioNormal != null && mascotAudioSource != null)
        {
            mascotAudioSource.clip = data.wordAudioNormal;
            mascotAudioSource.Play();
            if (wordTextLabel != null)
            {
                StartCoroutine(WiggleAnimation(wordTextLabel.transform, Vector3.one, Quaternion.identity, data.wordAudioNormal.length));
            }
            yield return StartCoroutine(MascotTalkAnimation(data.wordAudioNormal.length));
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        if (continueButton != null)
        {
            continueButton.SetActive(true);
            continueButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(continueButton);
            LeanTween.scale(continueButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        }
    }

    private void HandleIncorrectChoice(int index, string incorrectSpelling)
    {
        _canTap = false;
        var data = questions[_currentIndex];

        if (instructionLabel != null)
        {
            instructionLabel.text = "Try again!";
        }

        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        if (wordTextLabel != null)
        {
            string spellingColored = $"<color={wrongColorHex}>{incorrectSpelling}</color>";
            wordTextLabel.text = data.blendLetters + spellingColored;
        }

        SetOptionBorderColor(index, optionWrongColor);

        if (wordTextLabel != null)
        {
            LeanTween.cancel(wordTextLabel.gameObject);
            float shakeAmount = 15f;
            Vector3 origPos = wordTextLabel.transform.localPosition;
            LeanTween.moveLocalX(wordTextLabel.gameObject, origPos.x + shakeAmount, 0.05f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    wordTextLabel.transform.localPosition = origPos;
                });
        }

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

        if (mascotAudioSource != null)
        {
            AudioClip repeatClip = data.wordAudioSlow != null ? data.wordAudioSlow : data.wordAudioNormal;
            if (repeatClip != null)
            {
                mascotAudioSource.PlayOneShot(repeatClip);
                StartCoroutine(MascotTalkAnimation(repeatClip.length));
            }
        }

        StartCoroutine(ReEnableTapsAfterDelay(1.2f));
        PulseCorrectButton(data.correctOptionIndex);
    }

    private IEnumerator ReEnableTapsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _canTap = true;
        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap the correct ending to finish the word.";
        }
    }

    private void PulseCorrectButton(int correctIndex)
    {
        StopPulsingCorrectButton();

        if (correctIndex < 0 || correctIndex >= optionUIElements.Length) return;

        var opt = optionUIElements[correctIndex];
        if (opt == null) return;

        Transform targetTransform = opt.container != null ? opt.container.transform : (opt.cardButton != null ? opt.cardButton.transform : null);
        if (targetTransform == null) return;

        _pulsingTarget = targetTransform;
        Vector3 origScale = _originalCardScales[correctIndex];

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

        if (_currentIndex < questions.Count && _canTap)
        {
            wordTextLabel.text = questions[_currentIndex].blendLetters + "__";
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

            // Restore original background color
            Image cardBg = opt.cardBgImage != null ? opt.cardBgImage : (opt.cardButton != null ? opt.cardButton.GetComponent<Image>() : null);
            if (cardBg != null && _originalCardColors.ContainsKey(index))
            {
                cardBg.color = _originalCardColors[index];
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



    private void OnContinueClicked()
    {
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        int nextIndex = _currentIndex + 1;
        if (nextIndex < questions.Count)
        {
            LoadQuestion(nextIndex);
        }
        else
        {
            OnCompletedAll();
        }
    }

    private void OnCompletedAll()
    {
        Debug.Log("[BlendBuilder] Completed all questions!");
        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            gameObject.SetActive(false);
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

    private void UpdateProgressLabel()
    {
        if (progressLabel != null)
        {
            progressLabel.text = $"Question {_currentIndex + 1} / {questions.Count}";
        }
    }

    private void SetupProgressDots()
    {
        if (progressDotsContainer == null) return;

        foreach (Transform child in progressDotsContainer)
        {
            if (progressDotPrefab == null || child.gameObject != progressDotPrefab)
            {
                Destroy(child.gameObject);
            }
        }
        _dotInstances.Clear();

        if (progressDotPrefab != null)
        {
            progressDotPrefab.SetActive(false);
        }

        int count = questions != null ? questions.Count : 0;
        for (int i = 0; i < count; i++)
        {
            GameObject dotObj;
            if (progressDotPrefab != null)
            {
                dotObj = Instantiate(progressDotPrefab, progressDotsContainer);
            }
            else
            {
                dotObj = new GameObject($"Dot_{i}", typeof(RectTransform), typeof(Image));
                dotObj.transform.SetParent(progressDotsContainer, false);
                RectTransform rt = dotObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(24f, 24f);
            }
            dotObj.SetActive(true);
            _dotInstances.Add(dotObj);
        }

        UpdateProgressDots();
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < _dotInstances.Count; i++)
        {
            Image img = _dotInstances[i].GetComponent<Image>();
            if (img == null) img = _dotInstances[i].GetComponentInChildren<Image>();

            if (img != null)
            {
                bool isCompletedOrActive = i <= _currentIndex;
                if (isCompletedOrActive)
                {
                    if (dotFilledSprite != null) img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;

                    if (i == _currentIndex)
                    {
                        _dotInstances[i].transform.localScale = Vector3.one * 1.25f;
                    }
                    else
                    {
                        _dotInstances[i].transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    if (dotEmptySprite != null) img.sprite = dotEmptySprite;
                    img.color = dotEmptyColor;
                    _dotInstances[i].transform.localScale = Vector3.one;
                }
            }
        }
    }

    private IEnumerator LoadAssetsIfNeeded(BlendBuilderQuestion data)
    {
        string audioBasePath = Application.dataPath + "/Phonics/Audio/Unit 6 Phonics/Beginning Blend/";
        string spriteBasePath = Application.dataPath + "/Phonics/Assets/Phonics_Assets/Phonics_Unit 6/";

        if (data.wordAudioNormal == null)
        {
            AudioClip clip = null;
            string path = "file://" + audioBasePath + data.wordText + ".mp3";
            yield return StartCoroutine(LoadAudioClipFromUrl(path, (loaded) => clip = loaded));
            
            if (clip == null)
            {
                path = "file://" + audioBasePath + data.wordText.ToLower() + ".mp3";
                yield return StartCoroutine(LoadAudioClipFromUrl(path, (loaded) => clip = loaded));
            }
            data.wordAudioNormal = clip;
        }

        if (data.wordSprite == null)
        {
            string path = spriteBasePath + data.wordText + ".png";
            if (!File.Exists(path))
            {
                path = spriteBasePath + data.wordText.ToLower() + ".png";
            }
            if (!File.Exists(path))
            {
                string capWord = char.ToUpper(data.wordText[0]) + data.wordText.Substring(1);
                path = spriteBasePath + capWord + ".png";
            }

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    data.wordSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
        }
    }

    private IEnumerator LoadAudioClipFromUrl(string url, System.Action<AudioClip> callback)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(DownloadHandlerAudioClip.GetContent(www));
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }

    private AudioClip GetLetterSound(char letter)
    {
        AudioClip loadedClip = null;
        PreloadedLetterAudios.TryGetValue(letter.ToString().ToLower(), out loadedClip);
        return loadedClip;
    }

    private Dictionary<string, AudioClip> PreloadedLetterAudios = new Dictionary<string, AudioClip>();

    private void StartPreloadingLetterSounds()
    {
        PreloadedLetterAudios.Clear();
        string[] letters = { "b", "c", "f", "g", "l", "p", "r", "s", "a", "d", "e", "i", "n", "o", "t", "u" };
        foreach (var let in letters)
        {
            StartCoroutine(PreloadAudioClip(let));
        }
    }

    private IEnumerator PreloadAudioClip(string letter)
    {
        string path = "file://" + Application.dataPath + "/Phonics/Audio/Unit 1 Phonics/Listening/Meet the Letters/Letter Sounds/" + letter + ".mp3";
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                PreloadedLetterAudios[letter] = clip;
            }
        }
    }

    private AudioClip GetBlendSound(string blend)
    {
        return null;
    }

    private AudioClip GetEndingSound(string ending)
    {
        return null;
    }

    // Animation Helpers
    private IEnumerator WiggleAnimation(Transform target, Vector3 origScale, Quaternion origRot, float duration)
    {
        float elapsed = 0f;
        float wiggleSpeed = 24f;
        float wiggleAngle = 10f;

        while (target != null && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * wiggleSpeed) * wiggleAngle;
            target.localRotation = origRot * Quaternion.Euler(0f, 0f, angle);

            float scaleProgress = Mathf.Min(elapsed / 0.15f, 1f);
            float baseScaleMult = Mathf.Lerp(1.0f, 1.15f, scaleProgress);
            float scalePulseX = 1f + Mathf.Sin(elapsed * wiggleSpeed) * 0.06f;
            float scalePulseY = 1f - Mathf.Sin(elapsed * wiggleSpeed) * 0.06f;

            target.localScale = new Vector3(
                origScale.x * baseScaleMult * scalePulseX,
                origScale.y * baseScaleMult * scalePulseY,
                origScale.z
            );

            yield return null;
        }

        if (target != null)
        {
            float t = 0f;
            Vector3 currentScale = target.localScale;
            Quaternion currentRotation = target.localRotation;
            while (target != null && t < 1f)
            {
                t += Time.deltaTime * 4f;
                target.localScale = Vector3.Lerp(currentScale, origScale, t);
                target.localRotation = Quaternion.Lerp(currentRotation, origRot, t);
                yield return null;
            }
            if (target != null)
            {
                target.localScale = origScale;
                target.localRotation = origRot;
            }
        }
    }

    private IEnumerator MascotTalkAnimation(float duration)
    {
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.05f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt(duration / 0.5f));
        }

        yield return new WaitForSeconds(duration);

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }
}
