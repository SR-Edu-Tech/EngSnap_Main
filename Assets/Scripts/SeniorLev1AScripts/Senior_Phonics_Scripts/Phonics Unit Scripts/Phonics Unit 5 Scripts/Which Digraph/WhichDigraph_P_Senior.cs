using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WhichDigraphQuestion
{
    [Tooltip("The full correct word, e.g. 'shop'")]
    public string wordText;

    [Tooltip("The text representation with gap, e.g. '__op'. The script will search for '__' to replace with feedback.")]
    public string gappedWordText;

    [Tooltip("The correct spelling to fill the gap, e.g. 'sh'")]
    public string correctSpelling;

    [Tooltip("The picture sprite for this word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for mascot saying the word normally (or when correct)")]
    public AudioClip wordAudioNormal;

    [Tooltip("Audio clip for mascot saying the word slowly (or repeating on incorrect)")]
    public AudioClip wordAudioSlow;

    [Tooltip("Index of the correct option in the options array (0-based)")]
    public int correctOptionIndex;

    [Tooltip("The spelling choices shown for this question (usually 7 choices: ch, sh, th, wh, ck, ng, nk). If empty, the default choices will be used.")]
    public string[] options;
}

[System.Serializable]
public class WhichDigraphOptionUI
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

public class WhichDigraph_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;

    [Header("Mascot Intro Audio")]
    public AudioClip introAudio;
    [Header("Gameplay Config")]
    public List<WhichDigraphQuestion> questions = new List<WhichDigraphQuestion>();

    [Header("Default Option Options (Used if question options are empty)")]
    public string[] defaultOptions = new string[] { "ch", "sh", "th", "wh", "ck", "ng", "nk" };

    [Header("UI Components - Options Tray")]
    [Tooltip("The UI elements mapped for each option slot in the tray (usually 7)")]
    public WhichDigraphOptionUI[] optionUIElements;

    [Header("UI Colors - Option Backgrounds")]
    [Tooltip("The default card background colors in order")]
    public Color[] defaultCardColors = new Color[] {
        new Color(0.47f, 0.72f, 0.31f), // Green
        new Color(0.95f, 0.76f, 0.29f), // Yellow
        new Color(0.24f, 0.51f, 0.82f), // Blue
        new Color(0.90f, 0.40f, 0.50f), // Pink/Reddish
        new Color(0.58f, 0.40f, 0.74f), // Purple
        new Color(0.95f, 0.53f, 0.22f), // Orange
        new Color(0.18f, 0.70f, 0.72f)  // Teal
    };

    [Header("UI Components - General")]
    public Image wordImage;
    public TextMeshProUGUI wordTextLabel;
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI instructionLabel;
    public Button replayWordButton;
    public Button listenAgainButton;
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

    // Runtime state
    private int _currentIndex = 0;
    private bool _started = false;
    private bool _canTap = false;
    private int _score = 0;
    private List<GameObject> _dotInstances = new List<GameObject>();
    private bool _isCurrentQuestionCorrect = false;

    private Vector3 _originalMascotScale = Vector3.one;
    private Dictionary<int, Vector3> _originalCardScales = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> _originalCardPositions = new Dictionary<int, Vector3>();
    private GameFlowManager_Senior_Phonics _flowManager;
    private Coroutine _audioCoroutine;
    private int _pulsingTweenId = -1;
    private Transform _pulsingTarget = null;

    private void Awake()
    {
        // Ensure defaultOptions is populated
        bool defaultOptionsValid = defaultOptions != null && defaultOptions.Length > 0;
        if (defaultOptionsValid)
        {
            defaultOptionsValid = false;
            foreach (var opt in defaultOptions)
            {
                if (!string.IsNullOrEmpty(opt))
                {
                    defaultOptionsValid = true;
                    break;
                }
            }
        }
        if (!defaultOptionsValid)
        {
            defaultOptions = new string[] { "ch", "sh", "th", "wh", "ck", "ng", "nk" };
        }

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
    }

    private string[] GetQuestionChoices(WhichDigraphQuestion data)
    {
        if (data.options != null && data.options.Length > 0)
        {
            bool hasValidOption = false;
            for (int i = 0; i < data.options.Length; i++)
            {
                if (!string.IsNullOrEmpty(data.options[i]))
                {
                    hasValidOption = true;
                    break;
                }
            }
            if (hasValidOption)
            {
                return data.options;
            }
        }
        return defaultOptions;
    }

    private void Start()
    {
        _started = true;

        if (replayWordButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string name = b.gameObject.name.ToLower();
                if (name.Contains("replay") || name.Contains("speaker") || name.Contains("sound") || name.Contains("listen"))
                {
                    replayWordButton = b;
                    break;
                }
            }
        }

        if (replayWordButton != null)
        {
            replayWordButton.onClick.RemoveAllListeners();
            replayWordButton.onClick.AddListener(OnReplayClicked);
        }

        if (listenAgainButton != null)
        {
            listenAgainButton.onClick.RemoveAllListeners();
            listenAgainButton.onClick.AddListener(OnReplayClicked);
        }

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
    }

    public void ResetToStart()
    {
        _currentIndex = 0;
        _score = 0;
        UpdateScoreUI();

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        SetupProgressDots();
        LoadQuestion(_currentIndex);
    }

    private void LoadQuestion(int index)
    {
        _currentIndex = index;
        _isCurrentQuestionCorrect = false;

        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[WhichDigraph] No questions configured!");
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

        // 1. Setup word label text with gap representation
        if (wordTextLabel != null)
        {
            wordTextLabel.text = data.gappedWordText;
        }

        // 2. Setup image representation
        if (wordImage != null && data.wordSprite != null)
        {
            wordImage.sprite = data.wordSprite;
            wordImage.gameObject.SetActive(true);
        }

        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap the correct digraph to complete the word.";
        }

        // 3. Setup Option Cards
        string[] currentChoices = GetQuestionChoices(data);
        Debug.Log($"[WhichDigraph] LoadQuestion index {index}. Word: '{data.wordText}', Gapped: '{data.gappedWordText}'. spelling choices: {string.Join(", ", currentChoices)}");

        for (int i = 0; i < optionUIElements.Length; i++)
        {
            var opt = optionUIElements[i];
            if (opt == null)
            {
                continue;
            }

            var containerObj = opt.container != null ? opt.container : (opt.cardButton != null ? opt.cardButton.gameObject : null);

            if (i < currentChoices.Length)
            {
                if (containerObj != null)
                {
                    containerObj.SetActive(true);
                }
                ResetOptionVisuals(i);

                if (opt.spellingTextLabel != null)
                {
                    opt.spellingTextLabel.text = currentChoices[i];
                }

                // Apply default option background colors if cardBgImage is present
                if (opt.cardBgImage != null && i < defaultCardColors.Length)
                {
                    opt.cardBgImage.color = defaultCardColors[i];
                }
            }
            else
            {
                if (containerObj != null) containerObj.SetActive(false);
            }
        }

        // Animate mascot entry and play normal word audio
        _canTap = false;
        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = Vector3.zero;
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.45f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => StartCoroutine(IntroAndStartFlow(data)));
        }
        else
        {
            StartCoroutine(IntroAndStartFlow(data));
        }
    }

    private void PlayNormalWordAudio(WhichDigraphQuestion data)
    {
        if (data == null) return;

        if (_audioCoroutine != null)
        {
            StopCoroutine(_audioCoroutine);
        }

        _audioCoroutine = StartCoroutine(PlayAudioSequence(data.wordAudioNormal, () => {
            _canTap = true;
        }));
    }

    private IEnumerator PlayAudioSequence(AudioClip clip, System.Action onComplete)
    {
        if (clip != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = clip;
            mascotAudioSource.Play();

            // Run mascot talk animation during audio
            yield return StartCoroutine(MascotTalkAnimation(clip.length));
        }
        else
        {
            yield return null;
        }

        onComplete?.Invoke();
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

    private void OnOptionTapped(int index)
    {
        if (!_canTap) return;

        var data = questions[_currentIndex];
        string[] currentChoices = GetQuestionChoices(data);
        if (index < 0 || index >= currentChoices.Length) return;

        if (index == data.correctOptionIndex)
        {
            HandleCorrectChoice(index);
        }
        else
        {
            HandleIncorrectChoice(index, currentChoices[index]);
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

        // 1. Play SFX
        if (sfxAudioSource != null)
        {
            if (correctSFX != null) sfxAudioSource.PlayOneShot(correctSFX);
            if (cheerSFX != null) StartCoroutine(PlaySFXDelay(cheerSFX, 0.4f));
        }

        // 2. Complete the gap with styled green correct option text
        if (wordTextLabel != null)
        {
            string spellingColored = $"<color={correctColorHex}>{data.correctSpelling}</color>";
            wordTextLabel.text = data.gappedWordText.Replace("__", spellingColored);
        }

        // 3. Highlight border green
        SetOptionBorderColor(index, optionCorrectColor);

        // 4. Scale animation on correct button
        var opt = optionUIElements[index];
        Transform targetTransform = opt.container != null ? opt.container.transform : (opt.cardButton != null ? opt.cardButton.transform : null);
        if (targetTransform != null)
        {
            LeanTween.cancel(targetTransform.gameObject);
            LeanTween.scale(targetTransform.gameObject, _originalCardScales[index] * 1.15f, 0.35f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(targetTransform.gameObject, _originalCardScales[index], 0.2f)
                        .setEase(LeanTweenType.easeOutQuad)
                        .setDelay(0.2f);
                });
        }

        // 5. Pop star effect
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

        // 6. Update score
        _score++;
        UpdateScoreUI();

        // 7. Play completed word audio, then show continue button (self-paced)
        if (_audioCoroutine != null)
        {
            StopCoroutine(_audioCoroutine);
        }
        _audioCoroutine = StartCoroutine(PlayAudioSequence(data.wordAudioNormal, () => {
            if (continueButton != null)
            {
                continueButton.SetActive(true);
                continueButton.transform.localScale = Vector3.zero;
                LeanTween.cancel(continueButton);
                LeanTween.scale(continueButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
            }
        }));

        UpdateProgressDots();
    }

    private void HandleIncorrectChoice(int index, string incorrectSpelling)
    {
        _canTap = false;
        var data = questions[_currentIndex];

        if (instructionLabel != null)
        {
            instructionLabel.text = "Try again!";
        }

        // 1. Play wrong SFX
        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        // 2. Temporarily show red incorrect spelling in gap
        if (wordTextLabel != null)
        {
            string spellingColored = $"<color={wrongColorHex}>{incorrectSpelling}</color>";
            wordTextLabel.text = data.gappedWordText.Replace("__", spellingColored);
        }

        // 3. Highlight border red
        SetOptionBorderColor(index, optionWrongColor);

        // 4. Shake word container / image & text
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

        // 5. Shake option button
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

        // 6. Repeat word slowly
        if (_audioCoroutine != null)
        {
            StopCoroutine(_audioCoroutine);
        }

        _audioCoroutine = StartCoroutine(PlayAudioSequence(data.wordAudioSlow, () => {
            _canTap = true;
            if (instructionLabel != null)
            {
                instructionLabel.text = "Tap the correct digraph to complete the word.";
            }
        }));

        // 7. Pulse correct button card as visual hint
        PulseCorrectButton(data.correctOptionIndex);
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
            // Restore original scale
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

        // Reset word text gap
        if (_currentIndex < questions.Count && _canTap)
        {
            wordTextLabel.text = questions[_currentIndex].gappedWordText;
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

    private void OnReplayClicked()
    {
        if (questions == null || _currentIndex >= questions.Count) return;

        StopPulsingCorrectButton();
        ResetOptionVisuals(questions[_currentIndex].correctOptionIndex);

        _canTap = false;
        PlayNormalWordAudio(questions[_currentIndex]);
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
        Debug.Log("[WhichDigraph] Completed all questions!");
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

            // Pop animation on score change
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

        List<GameObject> keptDots;
        GameObject activeDotTemplate = PrepareContainer(progressDotsContainer, progressDotPrefab, out keptDots);
        _dotInstances.Clear();

        if (activeDotTemplate == null)
        {
            Debug.LogError("[WhichDigraph] No progress dot prefab or template found!");
            return;
        }

        for (int i = 0; i < questions.Count; i++)
        {
            GameObject dotObj = Instantiate(activeDotTemplate, progressDotsContainer);
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
                bool isCompleted = i < _currentIndex || (i == _currentIndex && _isCurrentQuestionCorrect);
                if (isCompleted)
                {
                    img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;
                }
                else
                {
                    img.sprite = dotEmptySprite;
                    img.color = dotEmptyColor;
                }
            }
        }
    }

    private GameObject PrepareContainer(RectTransform container, GameObject prefab, out List<GameObject> keptObjects)
    {
        keptObjects = new List<GameObject>();
        if (container == null) return null;

        GameObject template = prefab;

        if (template != null)
        {
            foreach (Transform child in container)
            {
                if (child.gameObject != template)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                else
                {
                    keptObjects.Add(child.gameObject);
                }
            }
        }
        else
        {
            foreach (Transform child in container)
            {
                string nameLower = child.name.ToLower();
                if (nameLower != "bg" && nameLower != "background" && template == null)
                {
                    template = child.gameObject;
                    template.SetActive(false);
                    keptObjects.Add(template);
                }
                else if (nameLower == "bg" || nameLower == "background")
                {
                    keptObjects.Add(child.gameObject);
                }
                else
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            if (template == null && container.parent != null)
            {
                foreach (Transform sibling in container.parent)
                {
                    string nameLower = sibling.name.ToLower();
                    if (sibling.gameObject != container.gameObject &&
                        nameLower != "bg" &&
                        nameLower != "background" &&
                        template == null)
                    {
                        template = sibling.gameObject;
                        template.SetActive(false);
                        break;
                    }
                }
            }
        }

        return template;
    }

    private void Reset()
    {
        if (questions == null || questions.Count == 0)
        {
            questions = new List<WhichDigraphQuestion>()
            {
                new WhichDigraphQuestion { wordText = "shop", gappedWordText = "__op", correctSpelling = "sh", correctOptionIndex = 1 },
                new WhichDigraphQuestion { wordText = "chin", gappedWordText = "__in", correctSpelling = "ch", correctOptionIndex = 0 },
                new WhichDigraphQuestion { wordText = "thin", gappedWordText = "__in", correctSpelling = "th", correctOptionIndex = 2 },
                new WhichDigraphQuestion { wordText = "whale", gappedWordText = "__ale", correctSpelling = "wh", correctOptionIndex = 3 },
                new WhichDigraphQuestion { wordText = "duck", gappedWordText = "du__", correctSpelling = "ck", correctOptionIndex = 4 },
                new WhichDigraphQuestion { wordText = "ring", gappedWordText = "ri__", correctSpelling = "ng", correctOptionIndex = 5 },
                new WhichDigraphQuestion { wordText = "sink", gappedWordText = "si__", correctSpelling = "nk", correctOptionIndex = 6 },
                new WhichDigraphQuestion { wordText = "ship", gappedWordText = "__ip", correctSpelling = "sh", correctOptionIndex = 1 },
                new WhichDigraphQuestion { wordText = "chick", gappedWordText = "__ick", correctSpelling = "ch", correctOptionIndex = 0 },
                new WhichDigraphQuestion { wordText = "king", gappedWordText = "ki__", correctSpelling = "ng", correctOptionIndex = 5 }
            };
        }
    }

    private IEnumerator IntroAndStartFlow(WhichDigraphQuestion data)
    {
        _canTap = false;
        if (_currentIndex == 0 && introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();
            yield return StartCoroutine(MascotTalkAnimation(introAudio.length));
        }
        PlayNormalWordAudio(data);
    }

}