using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DiphthongOption
{
    [Tooltip("The spelling representation, e.g. 'oy'")]
    public string spelling;

    [Tooltip("Optional sound symbol, e.g. '/ɔɪ/'")]
    public string soundSymbol;

    [Tooltip("Optional audio clip of the diphthong sound itself")]
    public AudioClip diphthongAudio;
}

[System.Serializable]
public class HearTheGlideQuestion
{
    [Tooltip("The word text, e.g. 'boy'")]
    public string wordText;

    [Tooltip("Audio clip for mascot saying the word normally")]
    public AudioClip wordAudioNormal;

    [Tooltip("Audio clip for mascot saying the word slowly")]
    public AudioClip wordAudioSlow;

    [Tooltip("Index of the correct option in the options array below (0-based)")]
    public int correctOptionIndex;

    [Tooltip("The diphthong choices shown for this question (usually 3 or 4 choices)")]
    public DiphthongOption[] options = new DiphthongOption[3];
}

[System.Serializable]
public class HearTheGlideOptionUI
{
    [Tooltip("The parent GameObject of this option card (to show/hide)")]
    public GameObject container;

    [Tooltip("The main button component for this option card")]
    public Button cardButton;

    [Tooltip("The circular icon button component (optional, can play the sound or act as the main button)")]
    public Button soundCircleButton;

    [Tooltip("The Image component of the circular icon")]
    public Image soundCircleImage;

    [Tooltip("The Text component inside the circular icon")]
    public TextMeshProUGUI circleTextLabel;

    [Tooltip("The spelling Text component below the circular icon")]
    public TextMeshProUGUI spellingTextLabel;

    [Tooltip("The highlight border image component (for green/red feedback)")]
    public Image highlightBorder;
}

public class HearTheGlide_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;

    [Header("Mascot Intro Audio")]
    public AudioClip introAudio;
    [Header("Gameplay Config")]
    public List<HearTheGlideQuestion> questions = new List<HearTheGlideQuestion>();

    [Header("UI Components - Options")]
    [Tooltip("The UI elements mapped for each option slot (usually 3 or 4)")]
    public HearTheGlideOptionUI[] optionUIElements;

    [Header("UI Colors - Circle Options")]
    [Tooltip("The default circle colors applied to cards in order (e.g. Card 1 Green, Card 2 Yellow, Card 3 Blue)")]
    public Color[] defaultCircleColors = new Color[] {
        new Color(0.47f, 0.72f, 0.31f), // Green
        new Color(0.95f, 0.76f, 0.29f), // Yellow
        new Color(0.24f, 0.51f, 0.82f), // Blue
        new Color(0.90f, 0.40f, 0.50f)  // Pink/orange fallback
    };

    [Header("Layout Settings")]
    [Tooltip("If true, the word card is only displayed after selecting the correct answer. If false, it is displayed from the start.")]
    public bool showWordOnlyAfterCorrect = false;

    [Tooltip("If true, automatically advances to the next question after a correct answer delay.")]
    public bool autoAdvance = false;

    [Tooltip("Delay in seconds before auto-advancing to the next question (only active if autoAdvance is true).")]
    public float autoAdvanceDelay = 2.0f;

    [Header("UI Components - General")]
    public Button replayWordButton;
    public Button listenAgainButton; // Under indicator or standard replay button
    public GameObject continueButton;
    public RectTransform mascotCharacter;
    public GameObject starEffectObject; // Activated on correct choice (supports POPEffect_SeniorLev1A)
    public TextMeshProUGUI wordTextLabel; // Displays the word (e.g., "boy")
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI instructionLabel; // Label showing e.g., "Tap the sound you hear."

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

    [Header("Card Highlight Colors")]
    public Color optionNormalColor = Color.white;
    public Color optionCorrectColor = Color.green;
    public Color optionWrongColor = Color.red;

    // Runtime state
    private int _currentIndex = 0;
    private bool _started = false;
    private bool _canTap = false;
    private int _score = 0;
    private List<GameObject> _dotInstances = new List<GameObject>();

    private Vector3 _originalMascotScale = Vector3.one;
    private Dictionary<int, Vector3> _originalCardScales = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> _originalCardPositions = new Dictionary<int, Vector3>();
    private GameFlowManager_Senior_Phonics _flowManager;
    private Coroutine _audioCoroutine;
    private Coroutine _gameplayCoroutine;
    private int _pulsingTweenId = -1;
    private Transform _pulsingTarget = null;
    private bool _isCurrentQuestionAnswered = false;

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
            if (opt != null)
            {
                if (opt.cardButton != null)
                {
                    opt.cardButton.onClick.RemoveAllListeners();
                    opt.cardButton.onClick.AddListener(() => OnOptionTapped(index));
                }
                if (opt.soundCircleButton != null)
                {
                    opt.soundCircleButton.onClick.RemoveAllListeners();
                    opt.soundCircleButton.onClick.AddListener(() => OnOptionTapped(index));
                }
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
        _isCurrentQuestionAnswered = false;
        UpdateScoreUI();
        
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        if (wordTextLabel != null && wordTextLabel.transform.parent != null)
        {
            wordTextLabel.transform.parent.gameObject.SetActive(false);
        }

        SetupProgressDots();
        LoadQuestion(_currentIndex);
    }

    private void LoadQuestion(int index)
    {
        _currentIndex = index;

        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[HearTheGlide] No questions configured!");
            return;
        }

        if (index < 0 || index >= questions.Count)
        {
            OnCompletedAll();
            return;
        }

        var data = questions[index];

        _isCurrentQuestionAnswered = false;
        StopPulsingCorrectButton();
        UpdateProgressLabel();
        UpdateProgressDots();

        // 1. Setup word label text
        if (wordTextLabel != null)
        {
            wordTextLabel.text = data.wordText;
            
            // Set active state based on configuration
            var parentObj = wordTextLabel.transform.parent != null ? wordTextLabel.transform.parent.gameObject : wordTextLabel.gameObject;
            parentObj.SetActive(!showWordOnlyAfterCorrect);
        }

        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap the sound you hear.";
        }

        // 2. Setup Option Cards
        for (int i = 0; i < optionUIElements.Length; i++)
        {
            var opt = optionUIElements[i];
            if (opt == null) continue;

            var containerObj = opt.container != null ? opt.container : (opt.cardButton != null ? opt.cardButton.gameObject : null);

            if (i < data.options.Length)
            {
                if (containerObj != null) containerObj.SetActive(true);
                ResetOptionVisuals(i);

                var optData = data.options[i];
                if (opt.spellingTextLabel != null)
                {
                    opt.spellingTextLabel.text = optData.spelling;
                }
                if (opt.circleTextLabel != null)
                {
                    opt.circleTextLabel.text = optData.spelling; // or soundSymbol based on preference, spelling in circle matches layout
                }
                
                // Set option circle dot to empty color and empty sprite initially
                if (opt.soundCircleImage != null)
                {
                    opt.soundCircleImage.color = dotEmptyColor;
                    if (dotEmptySprite != null) opt.soundCircleImage.sprite = dotEmptySprite;
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

    private void PlayNormalWordAudio(HearTheGlideQuestion data)
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

            // Run mascot scaling/talking animation during audio
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
        if (index < 0 || index >= data.options.Length) return;

        // If the option has custom diphthong audio, play it first as feedback
        var optData = data.options[index];
        if (optData.diphthongAudio != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(optData.diphthongAudio);
        }

        if (index == data.correctOptionIndex)
        {
            HandleCorrectChoice(index);
        }
        else
        {
            HandleIncorrectChoice(index);
        }
    }

    private void HandleCorrectChoice(int index)
    {
        _canTap = false;
        _isCurrentQuestionAnswered = true;
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

        // 2. Highlight border green
        SetOptionBorderColor(index, optionCorrectColor);

        // Highlight circle dot to filled color
        var opt = optionUIElements[index];
        if (opt.soundCircleImage != null)
        {
            opt.soundCircleImage.color = dotFilledColor;
            if (dotFilledSprite != null) opt.soundCircleImage.sprite = dotFilledSprite;
        }

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

        // 4. Pop star effect
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

        // 5. Show/reveal word text card
        if (wordTextLabel != null)
        {
            var parentObj = wordTextLabel.transform.parent != null ? wordTextLabel.transform.parent.gameObject : wordTextLabel.gameObject;
            if (!parentObj.activeSelf)
            {
                parentObj.SetActive(true);
                parentObj.transform.localScale = Vector3.zero;
                LeanTween.scale(parentObj, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
            }
        }

        // 6. Update score
        _score++;
        UpdateScoreUI();

        // 7. Auto-Advance or Show Continue Button
        if (autoAdvance)
        {
            if (_gameplayCoroutine != null) StopCoroutine(_gameplayCoroutine);
            _gameplayCoroutine = StartCoroutine(AutoAdvanceSequence(autoAdvanceDelay));
        }
        else
        {
            if (continueButton != null)
            {
                continueButton.SetActive(true);
                continueButton.transform.localScale = Vector3.zero;
                LeanTween.cancel(continueButton);
                LeanTween.scale(continueButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
            }
        }

        UpdateProgressDots();
    }

    private IEnumerator AutoAdvanceSequence(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnContinueClicked();
    }

    private void HandleIncorrectChoice(int index)
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

        // 2. Flash border red
        SetOptionBorderColor(index, optionWrongColor);

        // 3. Shake incorrect card container
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
                    StartCoroutine(ResetBorderColorAfterDelay(index, 0.5f));
                });
        }
        else
        {
            StartCoroutine(ResetBorderColorAfterDelay(index, 0.5f));
        }

        // 4. Repeat word slowly
        if (_audioCoroutine != null)
        {
            StopCoroutine(_audioCoroutine);
        }

        _audioCoroutine = StartCoroutine(PlayAudioSequence(data.wordAudioSlow, () => {
            _canTap = true;
            if (instructionLabel != null)
            {
                instructionLabel.text = "Tap the sound you hear.";
            }
        }));

        // 5. Pulse correct button card
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

    private IEnumerator ResetBorderColorAfterDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        ResetOptionVisuals(index);
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

            // Reset dot color to empty
            if (opt.soundCircleImage != null)
            {
                opt.soundCircleImage.color = dotEmptyColor;
                if (dotEmptySprite != null) opt.soundCircleImage.sprite = dotEmptySprite;
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
        Debug.Log("[HearTheGlide] Completed all questions!");
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
            Debug.LogError("[HearTheGlide] No progress dot prefab or template found!");
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
                bool isCompleted = i < _currentIndex || (i == _currentIndex && _isCurrentQuestionAnswered);
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

    private IEnumerator IntroAndStartFlow(HearTheGlideQuestion data)
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