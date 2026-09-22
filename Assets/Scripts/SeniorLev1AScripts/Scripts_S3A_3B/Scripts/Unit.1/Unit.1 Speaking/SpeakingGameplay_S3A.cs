using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class SpeakingGameplay_S3A : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip _correctClip;
    [SerializeField] private AudioClip _wrongClip;
    [SerializeField] private AudioClip[] _audioClips;

    [Header("Phrase Texts")]
    [SerializeField] private string[] _phraseTexts;
    [SerializeField] private int _currentAudioIndex = 0;

    [Header("UI")]
    [SerializeField] private GameObject _currentLineShowBox;
    [SerializeField] private GameObject _micObj;
    [SerializeField] private TextMeshProUGUI _feedbackText;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private Button nextQuestionButton;
    [SerializeField] private Button skipButton;

    [Header("Attempts")]
    [SerializeField] private int maxWrongAttempts = 2;
    private int wrongAttempts = 0;

    [SerializeField] private Button _phraseBoxButton;

    [Header("Mic UI")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite idleIcon;
    [SerializeField] private Sprite listeningIcon;
    [SerializeField] private Animator listeningAnimator;
    [SerializeField] private TextMeshProUGUI micStatusLabel;
    [SerializeField] private string idleLabel = "Tap to talk";
    [SerializeField] private string listeningLabel = "Listening...";

    [Header("Scoring")]
    [SerializeField, Range(0f, 1f)]
    private float passThreshold = 0.75f;

    [Header("Juicy UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private AudioClip popClip;
    [SerializeField] private AudioClip finishClip;

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float staggerDelay = 0.1f;

    // RUNTIME
    private bool _isProcessingResult = false;
    private bool _isListening = false;
    private bool canPlay = false;
    private string _latestSpokenText = "";

    private Coroutine _coroutine;

    // ---------------------------------------------------
    // UNITY
    // ---------------------------------------------------

    private void OnEnable()
    {
        // Request Microphone Permission for Mobile Platforms
        RequestMicPermission();

        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;

        ResetUI();

        if (_audioSource != null)
            _audioSource.Stop();

        StartCoroutine(IntroSequence());
    }

    private void OnDisable()
    {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;

        StopAllCoroutines();

        if (_audioSource != null)
            _audioSource.Stop();
    }

    private void RequestMicPermission()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#elif UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#endif
    }

    // ---------------------------------------------------
    // RESET
    // ---------------------------------------------------

    private void ResetUI()
    {
        canPlay = false;
        _isProcessingResult = false;
        _isListening = false;
        _latestSpokenText = "";
        wrongAttempts = 0;

        _currentLineShowBox.SetActive(false);
        _micObj.SetActive(false);

        _currentLineShowBox.transform.localScale = Vector3.zero;
        _micObj.transform.localScale = Vector3.zero;

        if (nextButton)
            nextButton.SetActive(false);

        if (_feedbackText != null)
            _feedbackText.text = "";

        SetupMicButton();
        ApplyMicState(false);

        if (_phraseBoxButton != null)
        {
            _phraseBoxButton.onClick.RemoveAllListeners();
            _phraseBoxButton.onClick.AddListener(PlayAudioClip);
        }

        if (nextQuestionButton)
            nextQuestionButton.gameObject.SetActive(false);

        if (skipButton)
        {
            skipButton.gameObject.SetActive(false);
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(LoadNextQuestion);
        }
    }

    private void SetupMicButton()
    {
        Button btn = _micObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnMicButtonClicked);
        }
    }

    // ---------------------------------------------------
    // INTRO
    // ---------------------------------------------------

    private IEnumerator IntroSequence()
    {
        if (_introClip && _audioSource != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }

        if (titleText != null)
        {
            titleText.transform.localScale = Vector3.zero;
            LeanTween.scale(titleText.gameObject, Vector3.one, 0.4f).setEaseOutBack();
        }

        if (_introClip)
        {
            yield return new WaitForSeconds(_introClip.length);
        }

        if (popClip && _audioSource != null)
            _audioSource.PlayOneShot(popClip);

        _micObj.SetActive(true);
        StartCoroutine(PopIn(_micObj.transform));

        yield return new WaitForSeconds(0.2f);

        ShowTargetWord();

        yield return new WaitForSeconds(0.5f);

        canPlay = true;
    }

    // ---------------------------------------------------
    // MIC INTERACTION
    // ---------------------------------------------------

    public void OnMicButtonClicked()
    {
        if (!canPlay)
        {
            Debug.LogWarning("[SpeakingGameplay] Cannot play yet. Intro sequence running.");
            return;
        }

        if (_isProcessingResult)
        {
            Debug.LogWarning("[SpeakingGameplay] Still processing previous speech result.");
            return;
        }

        _isListening = !_isListening;
        Debug.Log($"[SpeakingGameplay] Mic Toggled. Listening State: {_isListening}");

        if (_isListening)
        {
            if (CrossPlatformSpeechManager.Instance != null)
            {
                CrossPlatformSpeechManager.Instance.StartListening();
            }
            else
            {
                Debug.LogError("[SpeakingGameplay] CrossPlatformSpeechManager Instance is null!");
            }
        }
        else
        {
            if (CrossPlatformSpeechManager.Instance != null)
            {
                CrossPlatformSpeechManager.Instance.StopListening();
            }
        }

        ApplyMicState(_isListening);
    }

    private void ApplyMicState(bool listening)
    {
        if (buttonImage != null && listeningIcon != null && idleIcon != null)
        {
            buttonImage.sprite = listening ? listeningIcon : idleIcon;
        }

        if (listeningAnimator != null)
        {
            listeningAnimator.SetBool("IsListening", listening);
        }

        if (micStatusLabel != null)
        {
            micStatusLabel.text = listening ? listeningLabel : idleLabel;
        }
    }

    private void ForceIdleMic()
    {
        _isListening = false;

        if (CrossPlatformSpeechManager.Instance != null)
        {
            CrossPlatformSpeechManager.Instance.StopListening();
        }

        ApplyMicState(false);
    }

    // ---------------------------------------------------
    // AUDIO PLAYBACK
    // ---------------------------------------------------

    public void PlayAudioClip()
    {
        if (!canPlay) return;

        if (_coroutine != null)
            StopCoroutine(_coroutine);

        _coroutine = StartCoroutine(PlayClip());
    }

    private IEnumerator PlayClip()
    {
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
            SetSpeakerIcon(false);
            yield break;
        }

        SetSpeakerIcon(true);

        if (_audioClips != null && _currentAudioIndex < _audioClips.Length && _audioClips[_currentAudioIndex] != null)
        {
            _audioSource.clip = _audioClips[_currentAudioIndex];
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);
        }

        SetSpeakerIcon(false);
    }

    private void SetSpeakerIcon(bool on)
    {
        if (_currentLineShowBox.transform.childCount > 1)
        {
            Image icon = _currentLineShowBox.transform.GetChild(1).GetComponent<Image>();
            if (icon != null)
                icon.enabled = on;
        }
    }

    // ---------------------------------------------------
    // SHOW TARGET
    // ---------------------------------------------------

    private void ShowTargetWord()
    {
        wrongAttempts = 0;

        TextMeshProUGUI tmp = _currentLineShowBox.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        string targetText = "";

        if (_phraseTexts != null && _currentAudioIndex < _phraseTexts.Length)
        {
            targetText = _phraseTexts[_currentAudioIndex];
        }

        tmp.text = targetText;

        if (_progressBar != null)
        {
            _progressBar.value = 0f;
            if (_progressBar.fillRect != null)
            {
                Image fillImage = _progressBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                    fillImage.color = Color.white;
            }
        }

        if (_feedbackText != null)
            _feedbackText.text = "";

        bool needsPop = !_currentLineShowBox.activeSelf || _currentLineShowBox.transform.localScale.sqrMagnitude < 0.1f;

        _currentLineShowBox.SetActive(true);

        if (needsPop)
        {
            if (popClip && _audioSource != null)
                _audioSource.PlayOneShot(popClip);

            StartCoroutine(PopIn(_currentLineShowBox.transform));
        }

        StartCoroutine(PopTextPerChar(tmp));

        _isProcessingResult = false;
        ForceIdleMic();
    }

    // ---------------------------------------------------
    // SPEECH RESULT
    // ---------------------------------------------------

    private void OnSpeechResult(string spokenText)
    {
        Debug.Log($"[SpeakingGameplay] Speech Result Received: {spokenText}");

        if (_isProcessingResult)
            return;

        _latestSpokenText = spokenText;
        EvaluateSpeech(spokenText);
    }

    // ---------------------------------------------------
    // EVALUATION
    // ---------------------------------------------------

    private void EvaluateSpeech(string text)
    {
        if (_phraseTexts == null || _currentAudioIndex >= _phraseTexts.Length)
            return;

        string targetText = _phraseTexts[_currentAudioIndex];
        float score = SimilarityPercent(targetText, text);

        string percentageStr = $"<color=yellow>({Mathf.RoundToInt(score * 100)}%)</color>";

        if (_feedbackText != null)
            _feedbackText.text = text + " " + percentageStr;

        if (_progressBar != null)
            _progressBar.value = score;

        _isProcessingResult = true;

        if (score >= passThreshold)
        {
            StartCoroutine(AudioChecker(true));
        }
        else
        {
            StartCoroutine(AudioChecker(false));
        }

        ForceIdleMic();
    }

    // ---------------------------------------------------
    // RESULT FLOW
    // ---------------------------------------------------

    private IEnumerator AudioChecker(bool isMatch)
    {
        if (isMatch)
        {
            if (_correctClip && _audioSource)
                _audioSource.PlayOneShot(_correctClip);

            yield return new WaitForSeconds(1.2f);

            int maxLevel = _phraseTexts.Length;

            if (_currentAudioIndex < maxLevel - 1)
            {
                if (nextQuestionButton)
                {
                    nextQuestionButton.gameObject.SetActive(true);
                    nextQuestionButton.transform.localScale = Vector3.zero;

                    LeanTween.scale(nextQuestionButton.gameObject, Vector3.one, 0.35f)
                        .setEaseOutBack()
                        .setOnComplete(() =>
                        {
                            LeanTween.scale(nextQuestionButton.gameObject, Vector3.one * 1.1f, 0.6f)
                                .setLoopPingPong();
                        });

                    nextQuestionButton.onClick.RemoveAllListeners();
                    nextQuestionButton.onClick.AddListener(LoadNextQuestion);
                }
            }
            else
            {
                if (finishClip && _audioSource)
                    _audioSource.PlayOneShot(finishClip);

                if (nextButton)
                    nextButton.SetActive(true);
            }
        }
        else
        {
            wrongAttempts++;

            if (_wrongClip && _audioSource)
                _audioSource.PlayOneShot(_wrongClip);

            StartCoroutine(Shake(_currentLineShowBox.transform));

            if (wrongAttempts >= maxWrongAttempts)
            {
                bool isLastQuestion = (_currentAudioIndex >= _phraseTexts.Length - 1);

                if (isLastQuestion)
                {
                    if (finishClip && _audioSource)
                        _audioSource.PlayOneShot(finishClip);

                    if (nextButton)
                        nextButton.SetActive(true);
                }
                else
                {
                    if (skipButton && !skipButton.gameObject.activeSelf)
                    {
                        skipButton.gameObject.SetActive(true);
                        skipButton.transform.localScale = Vector3.zero;

                        LeanTween.scale(skipButton.gameObject, Vector3.one, 0.35f)
                            .setEaseOutBack()
                            .setOnComplete(() =>
                            {
                                LeanTween.scale(skipButton.gameObject, Vector3.one * 1.1f, 0.6f)
                                    .setLoopPingPong();
                            });
                    }
                }
            }

            yield return new WaitForSeconds(2f);

            if (_feedbackText != null)
                _feedbackText.text = "";

            _isProcessingResult = false;
        }

        ForceIdleMic();
    }

    public void LoadNextQuestion()
    {
        wrongAttempts = 0;

        if (nextQuestionButton)
            nextQuestionButton.gameObject.SetActive(false);

        if (skipButton)
            skipButton.gameObject.SetActive(false);

        _currentAudioIndex++;

        if (_currentAudioIndex >= _phraseTexts.Length)
        {
            if (nextButton)
                nextButton.SetActive(true);
            return;
        }

        ShowTargetWord();
    }

    // ---------------------------------------------------
    // SIMILARITY
    // ---------------------------------------------------

    private float SimilarityPercent(string reference, string hypothesis)
    {
        string a = Normalize(reference);
        string b = Normalize(hypothesis);

        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0f;

        int dist = Levenshtein(a, b);

        return 1f - (float)dist / Mathf.Max(a.Length, b.Length);
    }

    private string Normalize(string s)
    {
        return System.Text.RegularExpressions.Regex.Replace(s.Trim().ToLowerInvariant(), @"[^a-z0-9\s]", "");
    }

    private int Levenshtein(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(
                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[n, m];
    }

    // ---------------------------------------------------
    // ANIMATION
    // ---------------------------------------------------

    private IEnumerator PopIn(Transform target)
    {
        target.localScale = Vector3.zero;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            float clamped = Mathf.Clamp01(t);
            float overshoot = 1.70158f;
            float c1 = overshoot + 1f;

            float ease = 1f + c1 * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

            target.localScale = Vector3.one * ease;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private IEnumerator Shake(Transform target)
    {
        Vector3 original = target.localPosition;

        for (int i = 0; i < 10; i++)
        {
            target.localPosition = original + new Vector3(Random.Range(-10f, 10f), 0, 0);
            yield return new WaitForSeconds(0.02f);
        }

        target.localPosition = original;
    }

    private IEnumerator PopTextPerChar(TMP_Text tmp)
    {
        tmp.maxVisibleCharacters = 0;
        int total = tmp.text.Length;

        for (int i = 0; i <= total; i++)
        {
            tmp.maxVisibleCharacters = i;
            yield return new WaitForSeconds(0.02f);
        }

        tmp.maxVisibleCharacters = 99999;
    }
}