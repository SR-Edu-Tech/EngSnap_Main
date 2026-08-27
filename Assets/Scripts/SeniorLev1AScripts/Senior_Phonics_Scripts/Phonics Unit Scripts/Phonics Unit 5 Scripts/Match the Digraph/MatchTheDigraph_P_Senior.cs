using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MatchTheDigraphQuestion
{
    [Tooltip("The target word shown on the left, e.g. 'wheel'")]
    public string targetWord;

    [Tooltip("The digraph we are matching, e.g. 'wh'")]
    public string targetDigraph;

    [Tooltip("The picture sprite for the target word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for mascot saying the word normally (or when correct)")]
    public AudioClip wordAudioNormal;

    [Tooltip("Audio clip for mascot saying the word slowly (or repeating on incorrect)")]
    public AudioClip wordAudioSlow;

    [Tooltip("The 3 option words shown on the right (e.g. clown, whistle, hat)")]
    public string[] options = new string[3];

    [Tooltip("Audio clips for the 3 option words (optional)")]
    public AudioClip[] optionAudios = new AudioClip[3];

    [Tooltip("Index of the correct option word (0-based)")]
    public int correctOptionIndex;
}

[System.Serializable]
public class MatchTheDigraphOptionUI
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

public class MatchTheDigraph_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;

    [Header("Mascot Intro Audio")]
    public AudioClip introAudio;
    [Header("Gameplay Config")]
    public List<MatchTheDigraphQuestion> questions = new List<MatchTheDigraphQuestion>();

    [Header("UI Components - Options Tray")]
    [Tooltip("The UI elements mapped for each option slot in the tray (usually 3)")]
    public MatchTheDigraphOptionUI[] optionUIElements;

    [Header("UI Colors - Option Backgrounds")]
    [Tooltip("The default card background colors in order (usually Green, Yellow, Blue)")]
    public Color[] defaultCardColors = new Color[] {
        new Color(0.47f, 0.72f, 0.31f), // Green
        new Color(0.95f, 0.76f, 0.29f), // Yellow
        new Color(0.24f, 0.51f, 0.82f)  // Blue
    };

    [Header("UI Components - General")]
    public Image wordImage;
    public TextMeshProUGUI wordTextLabel;
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI instructionLabel;
    public Button replayWordButton;
    public Button listenAgainButton;
    public GameObject continueButton;
    public GameObject globalNextButton;
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
        Debug.Log($"[MatchTheDigraph] Awake started. Initial questions count in inspector: {(questions != null ? questions.Count.ToString() : "null")}");

        // Cache original dimensions/states
        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        // Ensure default questions are populated for any empty slots in the list at runtime
        PopulateDefaultQuestions();

        Debug.Log($"[MatchTheDigraph] Questions list after population: {questions.Count} items.");
        for (int i = 0; i < questions.Count; i++)
        {
            Debug.Log($"[MatchTheDigraph] Loaded Question {i}: targetWord='{questions[i].targetWord}', correctIndex={questions[i].correctOptionIndex}, options={(questions[i].options != null ? string.Join(", ", questions[i].options) : "null")}");
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
        if (globalNextButton == null)
        {
            Transform parent = transform.parent;
            if (parent != null && parent.parent != null)
            {
                Transform nextBtnTrans = parent.parent.Find("NextButton");
                if (nextBtnTrans != null)
                {
                    globalNextButton = nextBtnTrans.gameObject;
                }
            }
        }
        Debug.Log($"[MatchTheDigraph] Awake completed. _flowManager found: {_flowManager != null}. globalNextButton found: {globalNextButton != null}. Final questions count: {questions.Count}");

        if (mascotAudioSource == null)
        {
            mascotAudioSource = GetComponent<AudioSource>();
            if (mascotAudioSource == null)
            {
                mascotAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private string[] GetQuestionChoices(MatchTheDigraphQuestion data)
    {
        if (data.options != null && data.options.Length > 0)
        {
            return data.options;
        }
        return new string[] { "", "", "" };
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

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(false);
        }

        SetupProgressDots();
        LoadQuestion(_currentIndex);
    }

    private void LoadQuestion(int index)
    {
        Debug.Log($"[MatchTheDigraph] LoadQuestion called with index: {index}. Total questions: {questions.Count}");
        _currentIndex = index;
        _isCurrentQuestionCorrect = false;

        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[MatchTheDigraph] No questions configured!");
            return;
        }

        if (index < 0 || index >= questions.Count)
        {
            Debug.Log($"[MatchTheDigraph] index ({index}) out of bounds. Calling OnCompletedAll()");
            OnCompletedAll();
            return;
        }

        var data = questions[index];

        StopPulsingCorrectButton();
        UpdateProgressLabel();
        UpdateProgressDots();

        // 1. Setup word label text
        if (wordTextLabel != null)
        {
            wordTextLabel.text = data.targetWord;
        }

        // 2. Setup image representation
        if (wordImage != null && data.wordSprite != null)
        {
            wordImage.sprite = data.wordSprite;
            wordImage.gameObject.SetActive(true);
        }

        if (continueButton != null)
        {
            Debug.Log("[MatchTheDigraph] Deactivating continueButton");
            continueButton.SetActive(false);
        }

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(false);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap the word sharing the digraph.";
        }

        // 3. Setup Option Cards
        string[] currentChoices = GetQuestionChoices(data);
        Debug.Log($"[MatchTheDigraph] LoadQuestion index {index}. Target Word: '{data.targetWord}', Digraph: '{data.targetDigraph}'. Choices: {string.Join(", ", currentChoices)}");

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

    private void PlayNormalWordAudio(MatchTheDigraphQuestion data)
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
        Debug.Log($"[MatchTheDigraph] OnOptionTapped called: index={index}, _canTap={_canTap}, _currentIndex={_currentIndex}");
        if (!_canTap) return;

        var data = questions[_currentIndex];
        string[] currentChoices = GetQuestionChoices(data);
        if (index < 0 || index >= currentChoices.Length) return;

        Debug.Log($"[MatchTheDigraph] Option tapped text: '{currentChoices[index]}'. Correct index is {data.correctOptionIndex}");

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
        Debug.Log($"[MatchTheDigraph] HandleCorrectChoice index={index}");
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

        // 2. Highlight border green
        SetOptionBorderColor(index, optionCorrectColor);

        // 3. Scale animation on correct button
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

        // 5. Update score
        _score++;
        UpdateScoreUI();

        // 6. Play correct word audio, then show continue button (self-paced)
        if (_audioCoroutine != null)
        {
            StopCoroutine(_audioCoroutine);
        }

        AudioClip correctClip = (data.optionAudios != null && index < data.optionAudios.Length && data.optionAudios[index] != null) 
            ? data.optionAudios[index] 
            : data.wordAudioNormal;

        Debug.Log($"[MatchTheDigraph] Playing correctClip: {(correctClip != null ? correctClip.name : "null")}");

        _audioCoroutine = StartCoroutine(PlayAudioSequence(correctClip, () => {
            if (_currentIndex < questions.Count - 1)
            {
                if (continueButton != null)
                {
                    Debug.Log("[MatchTheDigraph] Activating continueButton after audio completion");
                    continueButton.SetActive(true);
                    continueButton.transform.localScale = Vector3.zero;
                    LeanTween.cancel(continueButton);
                    LeanTween.scale(continueButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
                }
            }
            else
            {
                Debug.Log("[MatchTheDigraph] Last question answered correctly. Activating global next button.");
                OnCompletedAll();
            }
        }));

        UpdateProgressDots();
    }

    private void HandleIncorrectChoice(int index, string incorrectWord)
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

        // 2. Highlight border red
        SetOptionBorderColor(index, optionWrongColor);

        // 3. Shake word container / image & text
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

        // 4. Shake option button
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

        // 5. Repeat word slowly
        if (_audioCoroutine != null)
        {
            StopCoroutine(_audioCoroutine);
        }

        AudioClip incorrectClip = (data.optionAudios != null && index < data.optionAudios.Length && data.optionAudios[index] != null)
            ? data.optionAudios[index]
            : null;

        if (incorrectClip != null)
        {
            _audioCoroutine = StartCoroutine(PlayAudioSequence(incorrectClip, () => {
                _audioCoroutine = StartCoroutine(PlayAudioSequence(data.wordAudioSlow, () => {
                    _canTap = true;
                    if (instructionLabel != null)
                    {
                        instructionLabel.text = "Tap the word sharing the digraph.";
                    }
                }));
            }));
        }
        else
        {
            _audioCoroutine = StartCoroutine(PlayAudioSequence(data.wordAudioSlow, () => {
                _canTap = true;
                if (instructionLabel != null)
                {
                    instructionLabel.text = "Tap the word sharing the digraph.";
                }
            }));
        }

        // 6. Pulse correct button card as visual hint
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
        Debug.Log($"[MatchTheDigraph] OnContinueClicked. Current _currentIndex: {_currentIndex}");
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        int nextIndex = _currentIndex + 1;
        if (nextIndex < questions.Count)
        {
            Debug.Log($"[MatchTheDigraph] Loading next question: {nextIndex}");
            LoadQuestion(nextIndex);
        }
        else
        {
            Debug.Log("[MatchTheDigraph] No more questions. Completing activity...");
            OnCompletedAll();
        }
    }

    private void OnCompletedAll()
    {
        Debug.Log("[MatchTheDigraph] Completed all questions!");
        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(true);
            globalNextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(globalNextButton);
            LeanTween.scale(globalNextButton, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);
        }
        else if (_flowManager != null)
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
            Debug.LogError("[MatchTheDigraph] No progress dot prefab or template found!");
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

    private void PopulateDefaultQuestions()
    {
        var defaultQuestions = new List<MatchTheDigraphQuestion>()
        {
            new MatchTheDigraphQuestion { targetWord = "<u>wh</u>eel", targetDigraph = "wh", correctOptionIndex = 1, options = new string[] { "clown", "whistle", "hat" } },
            new MatchTheDigraphQuestion { targetWord = "<u>sh</u>elf", targetDigraph = "sh", correctOptionIndex = 2, options = new string[] { "spin", "dog", "wish" } },
            new MatchTheDigraphQuestion { targetWord = "<u>ch</u>eese", targetDigraph = "ch", correctOptionIndex = 2, options = new string[] { "hand", "sun", "chair" } },
            new MatchTheDigraphQuestion { targetWord = "<u>th</u>umb", targetDigraph = "th", correctOptionIndex = 2, options = new string[] { "pie", "flute", "thorn" } },
            new MatchTheDigraphQuestion { targetWord = "<u>sh</u>eep", targetDigraph = "sh", correctOptionIndex = 1, options = new string[] { "bone", "ship", "kite" } },
            new MatchTheDigraphQuestion { targetWord = "<u>wh</u>ale", targetDigraph = "wh", correctOptionIndex = 2, options = new string[] { "nose", "cook", "where" } },
            new MatchTheDigraphQuestion { targetWord = "<u>ch</u>icken", targetDigraph = "ch", correctOptionIndex = 0, options = new string[] { "chalk", "book", "bag" } },
            new MatchTheDigraphQuestion { targetWord = "<u>th</u>at", targetDigraph = "th", correctOptionIndex = 2, options = new string[] { "mat", "up", "thanks" } },
            new MatchTheDigraphQuestion { targetWord = "<u>sh</u>adow", targetDigraph = "sh", correctOptionIndex = 1, options = new string[] { "beet", "shower", "doll" } },
            new MatchTheDigraphQuestion { targetWord = "<u>ch</u>ange", targetDigraph = "ch", correctOptionIndex = 2, options = new string[] { "pig", "hike", "chew" } }
        };

        if (questions == null)
        {
            questions = defaultQuestions;
        }
        else
        {
            // Pad the list to have at least defaultQuestions.Count items
            while (questions.Count < defaultQuestions.Count)
            {
                questions.Add(new MatchTheDigraphQuestion());
            }

            // Inspect each element and fill in empty fields
            for (int i = 0; i < defaultQuestions.Count; i++)
            {
                if (questions[i] == null)
                {
                    questions[i] = defaultQuestions[i];
                }
                else if (string.IsNullOrEmpty(questions[i].targetWord) || questions[i].targetWord.Trim() == "" || questions[i].options == null || questions[i].options.Length == 0)
                {
                    questions[i].targetWord = defaultQuestions[i].targetWord;
                    questions[i].targetDigraph = defaultQuestions[i].targetDigraph;
                    questions[i].correctOptionIndex = defaultQuestions[i].correctOptionIndex;
                    questions[i].options = defaultQuestions[i].options;
                }
            }
        }
    }

    private void Reset()
    {
        PopulateDefaultQuestions();
    }

    private IEnumerator IntroAndStartFlow(MatchTheDigraphQuestion data)
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