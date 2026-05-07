using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SpeakingGameplay_S1A : MonoBehaviour
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioSource _voiceAudioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] string[] _phraseTexts;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] GameObject _currentLineShowBox, _micObj;
    [SerializeField] TextMeshProUGUI _feedbackText;
    [SerializeField] Slider _progressBar;
    [SerializeField, Range(0f, 1f)] float passThreshold = 0.75f;
    [SerializeField] bool _isProcessingResult = false;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private Button _phraseBoxButton;

    [Header("Juicy UI Updates")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text micStateText;
    [SerializeField] AudioClip popClip;
    [SerializeField] AudioClip finishClip;

    Coroutine _coroutine;

    void OnEnable()
    {
        CrossPlatformSpeechManager_S1A.OnResultStatic += OnSpeechResult;
        CrossPlatformSpeechManager_S1A.OnPartialStatic += OnSpeechPartial;
        CrossPlatformSpeechManager_S1A.OnReadyStatic += OnMicReady;

        if (CrossPlatformSpeechManager_S1A.Instance != null)
            CrossPlatformSpeechManager_S1A.Instance.onEnd.AddListener(OnMicEnd);

        ResetUI();
        StartCoroutine(IntroSequence());
    }

    void ResetUI()
    {
        _currentLineShowBox.SetActive(false);
        _micObj.SetActive(false);
        _currentLineShowBox.transform.localScale = Vector3.zero;
        _micObj.transform.localScale = Vector3.zero;
        if (nextButton) nextButton.transform.localScale = Vector3.zero;
        
        if (titleText != null) SetupCanvasGroup(titleText);

        // Hook up the box click
        if (_phraseBoxButton != null)
        {
            _phraseBoxButton.onClick.RemoveAllListeners();
            _phraseBoxButton.onClick.AddListener(PlayAudioClip);
        }
        else
        {
            // Fallback: add Button + ensure raycast target exists
            Button boxBtn = _currentLineShowBox.GetComponent<Button>();
            if (boxBtn == null) boxBtn = _currentLineShowBox.AddComponent<Button>();
            
            Image img = _currentLineShowBox.GetComponent<Image>();
            if (img == null)
            {
                img = _currentLineShowBox.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0); // fully transparent
            }
            img.raycastTarget = true;
            
            boxBtn.onClick.RemoveAllListeners();
            boxBtn.onClick.AddListener(PlayAudioClip);
        }

        SetMicStateIdle();
    }

    void SetupCanvasGroup(TMP_Text text)
    {
        CanvasGroup cg = text.GetComponent<CanvasGroup>();
        if (cg == null) cg = text.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
    }

    void OnDisable()
    {
        CrossPlatformSpeechManager_S1A.OnResultStatic -= OnSpeechResult;
        CrossPlatformSpeechManager_S1A.OnPartialStatic -= OnSpeechPartial;
        CrossPlatformSpeechManager_S1A.OnReadyStatic -= OnMicReady;

        if (CrossPlatformSpeechManager_S1A.Instance != null)
            CrossPlatformSpeechManager_S1A.Instance.onEnd.RemoveListener(OnMicEnd);
    }

    IEnumerator IntroSequence()
    {
        if (_introClip)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }

        StartCoroutine(TitleAnim());
        yield return new WaitForSeconds(0.5f);

        if (popClip) _audioSource.PlayOneShot(popClip);
        _micObj.SetActive(true);
        StartCoroutine(PopIn(_micObj.transform));

        yield return new WaitForSeconds(0.5f);

        ShowTargetWord();
    }
    public void PlayAudioClip()
    {
        Debug.Log($"[Speaking] Box clicked! Audio index: {_currentAudioIndex}, Clips count: {(_audioClips != null ? _audioClips.Length : 0)}");
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(PlayClip());
    }
    IEnumerator PlayClip()
    {
        AudioSource voice = _voiceAudioSource != null ? _voiceAudioSource : _audioSource;

        // If already playing this clip, stop it (interruptable)
        if (voice.isPlaying)
        {
            voice.Stop();
            SetSpeakerIcon(false);
            yield break;
        }

        SetSpeakerIcon(true);
        
        if (_audioClips != null && _currentAudioIndex < _audioClips.Length && _audioClips[_currentAudioIndex] != null)
        {
            voice.clip = _audioClips[_currentAudioIndex];
            voice.Play();
            Debug.Log($"[Speaking] Playing clip: {_audioClips[_currentAudioIndex].name}");
            yield return new WaitForSeconds(voice.clip.length);
        }
        else
        {
            Debug.LogWarning($"[Speaking] No audio clip at index {_currentAudioIndex}. Make sure Audio Clips array is filled in the Inspector!");
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

    // Methods for Mic Event Trigger
    public void SetMicStateListening()
    {
        if (micStateText) micStateText.text = "Listening...";
    }

    public void SetMicStateIdle()
    {
        if (micStateText) micStateText.text = "Hold to talk";
    }

    void OnMicReady()
    {
        SetMicStateListening();
    }

    void OnMicEnd()
    {
        SetMicStateIdle();
    }

    void OnSpeechResult(string spokenText)
    {
        SetMicStateIdle();
        Debug.Log($"[Speaking] Final result: '{spokenText}'");
        if (_isProcessingResult) return;
        EvaluateSpeech(spokenText, true);
    }

    void OnSpeechPartial(string partialText)
    {
        if (_isProcessingResult) return;
        Debug.Log($"[Speaking] Partial: '{partialText}'");
        EvaluateSpeech(partialText, false);
    }

    void EvaluateSpeech(string text, bool final)
    {
        string targetText = "";
        if (_phraseTexts != null && _currentAudioIndex < _phraseTexts.Length && !string.IsNullOrEmpty(_phraseTexts[_currentAudioIndex])) 
            targetText = _phraseTexts[_currentAudioIndex];
        else if (_audioClips != null && _currentAudioIndex < _audioClips.Length && _audioClips[_currentAudioIndex] != null)
            targetText = _audioClips[_currentAudioIndex].name;

        float score = SimilarityPercent(targetText, text);
        _feedbackText.text = text;

        if (_progressBar != null)
        {
            _progressBar.value = score;
            _progressBar.fillRect.GetComponent<Image>().color = Color.HSVToRGB(Mathf.Lerp(0f, 0.33f, score), 0.9f, 0.6f);
        }

        if (score >= passThreshold)
        {
            _isProcessingResult = true;
            CrossPlatformSpeechManager_S1A.Instance.StopListening();
            StartCoroutine(AudioChecker(true));
        }
        else if (final)
        {
            _isProcessingResult = true;
            StartCoroutine(AudioChecker(false));
        }
    }

    IEnumerator AudioChecker(bool isMatch)
    {
        if (isMatch)
        {
            _audioSource.PlayOneShot(_correctClip);
            yield return new WaitForSeconds(1.2f);
            
            int maxLevel = (_phraseTexts != null && _phraseTexts.Length > 0) ? _phraseTexts.Length : (_audioClips != null ? _audioClips.Length : 0);
            
            if (_currentAudioIndex < maxLevel - 1)
            {
                _currentAudioIndex++;
                ShowTargetWord();
            }
            else
            {
                if (finishClip) _audioSource.PlayOneShot(finishClip);
                nextButton.SetActive(true);
                StartCoroutine(PopButton(nextButton.transform));
            }
        }
        else
        {
            _audioSource.PlayOneShot(_wrongClip);

            // Show accuracy feedback on wrong answer
            string targetText = "";
            if (_phraseTexts != null && _currentAudioIndex < _phraseTexts.Length && !string.IsNullOrEmpty(_phraseTexts[_currentAudioIndex])) 
                targetText = _phraseTexts[_currentAudioIndex];
            else if (_audioClips != null && _currentAudioIndex < _audioClips.Length && _audioClips[_currentAudioIndex] != null)
                targetText = _audioClips[_currentAudioIndex].name;

            float score = SimilarityPercent(targetText, _feedbackText.text);
            _feedbackText.text = _feedbackText.text + $"  ({Mathf.RoundToInt(score * 100)}%)";
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

    string Normalize(string s)
    {
        return System.Text.RegularExpressions.Regex.Replace(s.Trim().ToLowerInvariant(), @"[^a-z0-9\s]", "");
    }

    int Levenshtein(string s, string t)
    {
        int n = s.Length, m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    // -------------------------
    // ANIMATIONS
    // -------------------------

    IEnumerator PopIn(Transform target)
    {
        target.localScale = Vector3.zero;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            float scale = Mathf.Lerp(0, 1.1f, t);
            target.localScale = Vector3.one * scale;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator PopButton(Transform btn)
    {
        if (popClip && _audioSource) _audioSource.PlayOneShot(popClip);
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            btn.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.15f, Mathf.Clamp01(t));
            yield return null;
        }
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 10f;
            float smooth = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
            btn.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, smooth);
            yield return null;
        }
        btn.localScale = Vector3.one;
    }

    IEnumerator TitleAnim()
    {
        if (titleText == null) yield break;
        SetupCanvasGroup(titleText);
        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(PopTextPerChar(titleText, 1.5f, 0.05f, 0.7f, 4f));
    }

    IEnumerator PopTextPerChar(TMP_Text tmp, float popDur = 1.2f, float charStagger = 0.03f, float popAmp = 0.5f, float popFreq = 4f)
    {
        if (tmp == null) yield break;
        tmp.maxVisibleCharacters = 99999;
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
            if (!textInfo.characterInfo[i].isVisible) continue;
            int matIdx = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIdx = textInfo.characterInfo[i].vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
            Vector3 mid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;
            for (int v = 0; v < 4; v++) vertices[vertIdx + v] = mid;
        }

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
            tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);

        yield return null;
        CanvasGroup cg = tmp.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        float totalDuration = Mathf.Max(popDur, (charCount * charStagger) + 0.5f);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            if (tmp.text != originalText) break;
            elapsed += Time.deltaTime;
            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;
                int matIdx = charInfo.materialReferenceIndex;
                int vertIdx = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
                Vector3 mid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;

                float delay = i * charStagger;
                float localTime = elapsed - delay;
                float scale = 0f;
                if (localTime > 0f)
                {
                    float lt = Mathf.Clamp01(localTime / 0.25f);
                    float overshoot = 1.70158f * (1f + popAmp);
                    float c3 = overshoot + 1f;
                    scale = 1f + c3 * Mathf.Pow(lt - 1f, 3f) + overshoot * Mathf.Pow(lt - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIdx].vertices[vertIdx + v];
                    vertices[vertIdx + v] = mid + (orig - mid) * scale;
                }
            }
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
                tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            yield return null;
        }
        tmp.maxVisibleCharacters = 99999;
    }
}