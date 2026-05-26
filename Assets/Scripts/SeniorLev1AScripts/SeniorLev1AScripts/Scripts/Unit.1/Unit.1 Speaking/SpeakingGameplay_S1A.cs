using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SpeakingGameplay_S1A : MonoBehaviour
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] string[] _phraseTexts;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] GameObject _currentLineShowBox, _micObj;
    [SerializeField] TextMeshProUGUI _feedbackText;
    [SerializeField] Slider _progressBar;
    [SerializeField, Range(0f, 1f)] float passThreshold = 0.75f;
    // NOTE: removed [SerializeField]  was causing isProcessingResult to persist across sessions
    private bool _isProcessingResult = false;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private Button _phraseBoxButton;

    [Header("Juicy UI Updates")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text micStateText;
    [SerializeField] AudioClip popClip;
    [SerializeField] AudioClip finishClip;

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float staggerDelay = 0.1f;
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    Coroutine _coroutine;
    Coroutine _evalFallback;
    private bool canPlay = false;
    private bool _isListeningToggled = false;
    private bool _waitingForFinalEvaluation = false;
    private string _latestSpokenText = "";

    void OnEnable()
    {
        // Subscribe to the ONE shared manager
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
        CrossPlatformSpeechManager.OnPartialStatic += OnSpeechPartial;
        CrossPlatformSpeechManager.OnReadyStatic += OnMicReady;
        CrossPlatformSpeechManager.OnEndStatic += OnMicEnd;

        ResetUI();
        if (_audioSource != null) _audioSource.Stop();
        StartCoroutine(IntroSequence());
    }

    void OnDisable()
    {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
        CrossPlatformSpeechManager.OnPartialStatic -= OnSpeechPartial;
        CrossPlatformSpeechManager.OnReadyStatic -= OnMicReady;
        CrossPlatformSpeechManager.OnEndStatic -= OnMicEnd;

        StopAllCoroutines();
        if (_audioSource) _audioSource.Stop();
    }

    void ResetUI()
    {
        canPlay = false;
        _isListeningToggled = false;
        _waitingForFinalEvaluation = false;
        _isProcessingResult = false;   // always reset on rentry
        _latestSpokenText = "";

        _currentLineShowBox.SetActive(false);
        _micObj.SetActive(false);
        _currentLineShowBox.transform.localScale = Vector3.zero;
        _micObj.transform.localScale = Vector3.zero;

        if (nextButton) nextButton.SetActive(false);
        if (_feedbackText != null) _feedbackText.text = "";

        if (titleText != null) SetupCanvasGroup(titleText);

        if (_phraseBoxButton != null)
        {
            _phraseBoxButton.onClick.RemoveAllListeners();
            _phraseBoxButton.onClick.AddListener(PlayAudioClip);
        }
        else
        {
            Button boxBtn = _currentLineShowBox.GetComponent<Button>();
            if (boxBtn == null) boxBtn = _currentLineShowBox.AddComponent<Button>();

            Image img = _currentLineShowBox.GetComponent<Image>();
            if (img == null)
            {
                img = _currentLineShowBox.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
            }
            img.raycastTarget = true;

            boxBtn.onClick.RemoveAllListeners();
            boxBtn.onClick.AddListener(PlayAudioClip);
        }

        SetMicStateIdle();
        SetMicInteractable(false);
    }

    void SetupCanvasGroup(TMP_Text text)
    {
        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null) cg = text.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
    }

    void SetMicInteractable(bool state)
    {
        Button micBtn = _micObj.GetComponent<Button>();
        if (micBtn != null) micBtn.interactable = state;
    }

    IEnumerator IntroSequence()
    {
        if (_introClip)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }

        StartCoroutine(TitleAnim());

        // Timeoutguarded wait  never hangs if audio was left playing by another lesson
        if (_introClip)
        {
            float timeout = _introClip.length + 1f;
            float waited = 0f;
            while (_audioSource.isPlaying && waited < timeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            _audioSource.Stop();
        }

        if (popClip) _audioSource.PlayOneShot(popClip);
        _micObj.SetActive(true);
        StartCoroutine(PopIn(_micObj.transform));

        yield return new WaitForSeconds(0.2f);

        ShowTargetWord();

        yield return new WaitForSeconds(0.5f);
        canPlay = true;
        SetMicInteractable(true);
    }

    public void PlayAudioClip()
    {
        if (!canPlay) return;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(PlayClip());
    }

    IEnumerator PlayClip()
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
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        SetSpeakerIcon(false);
    }

    void SetSpeakerIcon(bool on)
    {
        if (_currentLineShowBox.transform.childCount > 1)
        {
            Image icon = _currentLineShowBox.transform.GetChild(1).GetComponent<Image>();
            if (icon != null) icon.enabled = on;
        }
    }

    void ShowTargetWord()
    {
        TextMeshProUGUI tmp = _currentLineShowBox.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

        string targetText = "";
        if (_phraseTexts != null && _currentAudioIndex < _phraseTexts.Length && !string.IsNullOrEmpty(_phraseTexts[_currentAudioIndex]))
            targetText = _phraseTexts[_currentAudioIndex];
        else if (_audioClips != null && _currentAudioIndex < _audioClips.Length && _audioClips[_currentAudioIndex] != null)
            targetText = _audioClips[_currentAudioIndex].name;

        tmp.text = targetText;

        if (_progressBar != null)
        {
            _progressBar.value = 0f;
            if (_progressBar.fillRect != null)
            {
                Image fillImage = _progressBar.fillRect.GetComponent<Image>();
                if (fillImage != null) fillImage.color = Color.white;
            }
        }
        if (_feedbackText != null) _feedbackText.text = "";

        bool needsPop = !_currentLineShowBox.activeSelf || _currentLineShowBox.transform.localScale.sqrMagnitude < 0.1f;
        _currentLineShowBox.SetActive(true);
        if (needsPop)
        {
            if (popClip) _audioSource.PlayOneShot(popClip);
            StartCoroutine(PopIn(_currentLineShowBox.transform));
        }

        StartCoroutine(PopTextPerChar(tmp));
        _isProcessingResult = false;
    }

    public void SetMicStateListening()
    {
        if (micStateText) micStateText.text = "Listening...";
    }

    public void SetMicStateIdle()
    {
        if (micStateText) micStateText.text = "Tap to talk";
    }

    public void OnMicToggleClicked()
    {
        if (!canPlay || _isProcessingResult) return;

        if (!_isListeningToggled)
        {
            _isListeningToggled = true;
            _latestSpokenText = "";
            if (_feedbackText != null) _feedbackText.text = "";
            if (_progressBar != null) _progressBar.value = 0f;
            if (micStateText) micStateText.text = "Starting Mic...";
            CrossPlatformSpeechManager.Instance?.StartListening();
        }
        else
        {
            _isListeningToggled = false;
            _waitingForFinalEvaluation = true;
            if (micStateText) micStateText.text = "Evaluating...";
            CrossPlatformSpeechManager.Instance?.StopListening();

            if (_evalFallback != null) StopCoroutine(_evalFallback);
            _evalFallback = StartCoroutine(ForceEvaluateFallback());
        }
    }

    IEnumerator ForceEvaluateFallback()
    {
        yield return new WaitForSeconds(1.5f);
        if (_waitingForFinalEvaluation)
        {
            _waitingForFinalEvaluation = false;
            Debug.LogWarning("[Speaking] OS took too long to finalize. Forcing evaluation.");
            SetMicStateIdle();
            EvaluateSpeech(_latestSpokenText, true);
        }
    }

    void OnMicReady()
    {
        if (_isListeningToggled) SetMicStateListening();
    }

    void OnMicEnd()
    {
        if (_waitingForFinalEvaluation)
        {
            _waitingForFinalEvaluation = false;
            SetMicStateIdle();
            EvaluateSpeech(_latestSpokenText, true);
        }
    }

    void OnSpeechResult(string spokenText)
    {
        Debug.Log($"[Speaking] Final result: '{spokenText}'");
        if (_isProcessingResult) return;
        if (!_isListeningToggled && !_waitingForFinalEvaluation) return;
        _latestSpokenText = spokenText;
    }

    void OnSpeechPartial(string partialText)
    {
        if (_isProcessingResult) return;
        if (!_isListeningToggled && !_waitingForFinalEvaluation) return;
        _latestSpokenText = partialText;
    }

    void EvaluateSpeech(string text, bool final)
    {
        string targetText = "";
        if (_phraseTexts != null && _currentAudioIndex < _phraseTexts.Length && !string.IsNullOrEmpty(_phraseTexts[_currentAudioIndex]))
            targetText = _phraseTexts[_currentAudioIndex];
        else if (_audioClips != null && _currentAudioIndex < _audioClips.Length && _audioClips[_currentAudioIndex] != null)
            targetText = _audioClips[_currentAudioIndex].name;

        float score = SimilarityPercent(targetText, text);

        string percentageStr = $"<color=yellow>({Mathf.RoundToInt(score * 100)}%)</color>";
        _feedbackText.text = text + " " + percentageStr;

        if (_progressBar != null)
        {
            _progressBar.value = score;
            _progressBar.fillRect.GetComponent<Image>().color =
                Color.HSVToRGB(Mathf.Lerp(0f, 0.33f, score), 0.9f, 0.6f);
        }

        if (score >= passThreshold)
        {
            _isProcessingResult = true;
            _isListeningToggled = false;
            _waitingForFinalEvaluation = false;
            SetMicStateIdle();
            CrossPlatformSpeechManager.Instance?.StopListening();
            StartCoroutine(AudioChecker(true));
        }
        else if (final)
        {
            _isProcessingResult = true;
            _isListeningToggled = false;
            _waitingForFinalEvaluation = false;
            StartCoroutine(AudioChecker(false));
        }
    }

    IEnumerator AudioChecker(bool isMatch)
    {
        if (isMatch)
        {
            _audioSource.PlayOneShot(_correctClip);
            yield return new WaitForSeconds(1.2f);

            int maxLevel = (_phraseTexts != null && _phraseTexts.Length > 0)
                ? _phraseTexts.Length
                : (_audioClips != null ? _audioClips.Length : 0);

            if (_currentAudioIndex < maxLevel - 1)
            {
                _currentAudioIndex++;
                ShowTargetWord();
            }
            else
            {
                if (finishClip) _audioSource.PlayOneShot(finishClip);
                if (nextButton) nextButton.SetActive(true);
            }
        }
        else
        {
            if (_wrongClip && _audioSource) _audioSource.PlayOneShot(_wrongClip);
            StartCoroutine(Shake(_currentLineShowBox.transform));
            _feedbackText.color = Color.red;
            yield return new WaitForSeconds(2f);
            _feedbackText.text = "";
            _feedbackText.color = Color.white;
            _isProcessingResult = false;
        }
    }

    float SimilarityPercent(string reference, string hypothesis)
    {
        string a = Normalize(reference);
        string b = Normalize(hypothesis);
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
        int dist = Levenshtein(a, b);
        return 1f - (float)dist / Mathf.Max(a.Length, b.Length);
    }

    string Normalize(string s) =>
        System.Text.RegularExpressions.Regex.Replace(s.Trim().ToLowerInvariant(), @"[^a-z0-9\s]", "");

    int Levenshtein(string s, string t)
    {
        int n = s.Length, m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        return d[n, m];
    }

    //  Animations (unchanged) 

    IEnumerator PopIn(Transform target)
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

    IEnumerator Shake(Transform target)
    {
        Vector3 original = target.localPosition;
        for (int i = 0; i < 10; i++)
        {
            target.localPosition = original + new Vector3(UnityEngine.Random.Range(-10f, 10f), 0, 0);
            yield return new WaitForSeconds(0.02f);
        }
        target.localPosition = original;
    }

    IEnumerator TitleAnim()
    {
        if (titleText == null) yield break;

        CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();
        if (titleCG == null) titleCG = titleText.gameObject.AddComponent<CanvasGroup>();
        titleCG.alpha = 0f;

        yield return new WaitForEndOfFrame();
        yield return null;
        titleText.ForceMeshUpdate();
        yield return null;
        titleText.ForceMeshUpdate();

        string originalText = titleText.text;
        TMP_TextInfo textInfo = titleText.textInfo;
        int charCount = textInfo.characterCount;
        if (charCount == 0) yield break;

        titleText.maxVisibleCharacters = charCount;
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        bool revealed = false;
        float elapsed = 0f;
        float expectedTime = (charCount * titlePopStagger) + Mathf.Max(0.5f, 1f / titlePopFrequency);
        float totalDuration = Mathf.Max(titlePopDuration, expectedTime);

        while (elapsed < totalDuration)
        {
            if (titleText.text != originalText) break;
            elapsed += Time.deltaTime;
            textInfo = titleText.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;
                int matIndex = charInfo.materialReferenceIndex;
                int vertIndex = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[matIndex].vertices;
                Vector3 charMid = (vertices[vertIndex] + vertices[vertIndex + 2]) / 2f;
                float localTime = elapsed - i * titlePopStagger;
                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterDur = Mathf.Max(0.1f, 1f / titlePopFrequency);
                    float t = Mathf.Clamp01(localTime / letterDur);
                    float overshoot = 1.70158f * (1f + titlePopAmplitude);
                    float c3 = overshoot + 1f;
                    scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                }
                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIndex].vertices[vertIndex + v];
                    vertices[vertIndex + v] = charMid + (orig - charMid) * scale;
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                titleText.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }

            yield return null;

            if (!revealed && titleCG != null) { titleCG.alpha = 1f; revealed = true; }
        }

        if (titleText.text == originalText)
        {
            textInfo = titleText.textInfo;
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
                titleText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        titleText.maxVisibleCharacters = 99999;
    }

    IEnumerator PopTextPerChar(TMP_Text tmp, float popDur = 1.2f, float charStagger = 0.04f, float popAmp = 0.6f, float popFreq = 4f)
    {
        if (tmp == null) yield break;
        tmp.ForceMeshUpdate();
        yield return null;
        tmp.ForceMeshUpdate();

        string originalText = tmp.text;
        TMP_TextInfo textInfo = tmp.textInfo;
        int charCount = textInfo.characterCount;
        if (charCount == 0) yield break;

        tmp.maxVisibleCharacters = charCount;
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;
            int matIdx = charInfo.materialReferenceIndex;
            int vertIdx = charInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
            Vector3 charMid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;
            for (int v = 0; v < 4; v++) vertices[vertIdx + v] = charMid;
        }
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
            tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
        }

        yield return null;

        CanvasGroup cg = tmp.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        float expectedTime = (charCount * charStagger) + Mathf.Max(0.5f, 1f / popFreq);
        float totalDuration = Mathf.Max(popDur, expectedTime);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            if (tmp.text != originalText) break;
            elapsed += Time.deltaTime;
            textInfo = tmp.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;
                int matIdx = charInfo.materialReferenceIndex;
                int vertIdx = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
                Vector3 charMid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;
                float localTime = elapsed - i * charStagger;
                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterDur = Mathf.Max(0.1f, 1f / popFreq);
                    float lt = Mathf.Clamp01(localTime / letterDur);
                    float overshoot = 1.70158f * (1f + popAmp);
                    float c3 = overshoot + 1f;
                    scale = 1f + c3 * Mathf.Pow(lt - 1f, 3f) + overshoot * Mathf.Pow(lt - 1f, 2f);
                }
                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIdx].vertices[vertIdx + v];
                    vertices[vertIdx + v] = charMid + (orig - charMid) * scale;
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }
            yield return null;
        }

        if (tmp.text == originalText)
        {
            textInfo = tmp.textInfo;
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
                tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        tmp.maxVisibleCharacters = 99999;
    }
}