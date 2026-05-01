using System;
using System.Text;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U1_SP01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] GameObject _currentLineShowBox, _micObj;
    [SerializeField] TextMeshProUGUI _feedbackText;
    [SerializeField] string _micTextInput;
    [SerializeField] bool _isViewed;
    [SerializeField] Slider _progressBar;

    [Header("── Scoring ─────────────────────")]
    [Range(0f, 1f)]
    public float passThreshold = 0.75f;

    Coroutine _coroutine;
    bool _isProcessingResult = false;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        Debug.Log("[Scene] U1_SP01_Junior1A Enabled.");
        CrossPlatformSpeechManager_junior.OnResultStatic += OnSpeechResult;
        CrossPlatformSpeechManager_junior.OnPartialStatic += OnSpeechPartial;
        StartCoroutine(Starter());
    }

    void OnDisable()
    {
        CrossPlatformSpeechManager_junior.OnResultStatic -= OnSpeechResult;
        CrossPlatformSpeechManager_junior.OnPartialStatic -= OnSpeechPartial;
    }

    IEnumerator Starter()
    {
        _currentLineShowBox.SetActive(false);
        _micObj.SetActive(false);
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        _currentLineShowBox.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _audioClips[_currentAudioIndex].name;
        _currentLineShowBox.SetActive(true);
        _micObj.SetActive(true);
    }

    public void PlayAudioClip()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(AudioPlayer());
    }

    IEnumerator AudioPlayer()
    {
        _audioSource.clip = _audioClips[_currentAudioIndex];
        _audioSource.Play();
        _currentLineShowBox.transform.GetChild(1).GetComponent<Image>().enabled = true;
        yield return new WaitForSeconds(_audioSource.clip.length);
        _currentLineShowBox.transform.GetChild(1).GetComponent<Image>().enabled = false;
    }

    void OnSpeechPartial(string partialText)
    {
        if (_isProcessingResult) return;
        
        _micTextInput = partialText;
        string answer = _audioClips[_currentAudioIndex].name;
        
        float score = SimilarityPercent(answer, partialText);
        
        Debug.Log($"[Speech Checking] Live Partial: Spoken='{partialText}', Target='{answer}', Score={score:P0}");
        
        _feedbackText.text = partialText;
        if (_progressBar != null) 
        {
            _progressBar.value = score;
            float hue = Mathf.Lerp(0f, 0.33f, score);
            _progressBar.fillRect.GetComponent<Image>().color = Color.HSVToRGB(hue, 0.9f, 0.6f);
        }
        
        if (score >= passThreshold)
        {
            _isProcessingResult = true;
            CrossPlatformSpeechManager_junior.Instance.StopListening();
            Debug.Log($"[Partial Match] Spoken: \"{partialText}\" | Answer: \"{answer}\" | Score: {score:P0}");
            StartCoroutine(AudioChecker(true));
        }
    }

    void OnSpeechResult(string spokenText)
    {
        if (_isProcessingResult) return;
        
        _micTextInput = spokenText;
        string answer = _audioClips[_currentAudioIndex].name;
        
        float score = SimilarityPercent(answer, spokenText);
        bool isMatch = score >= passThreshold;
        
        Debug.Log($"[Speech Checking] Final Result: Spoken='{spokenText}', Target='{answer}', Score={score:P0}, Passed={isMatch}");
        
        _feedbackText.text = spokenText;
        if (_progressBar != null) 
        {
            _progressBar.value = score;
            float hue = Mathf.Lerp(0f, 0.33f, score);
            _progressBar.fillRect.GetComponent<Image>().color = Color.HSVToRGB(hue, 0.9f, 0.6f);
        }
        
        Debug.Log($"[Final Match] Spoken: \"{spokenText}\" | Answer: \"{answer}\" | Score: {score:P0} | Match: {isMatch}");
        
        _isProcessingResult = true;
        StartCoroutine(AudioChecker(isMatch));
    }

    IEnumerator AudioChecker(bool isMatch)
    {
        if (isMatch)
        {
            _currentLineShowBox.GetComponent<PopEffect_Junior1A>().enabled = true;
            _audioSource.clip = _correctClip;
            _audioSource.Play();
            if (_currentAudioIndex < _audioClips.Length - 1)
            {
                _currentLineShowBox.GetComponent<Button>().interactable = false;
                yield return new WaitForSeconds(_audioSource.clip.length);
                _currentAudioIndex++;
                _currentLineShowBox.SetActive(false);
                _currentLineShowBox.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _audioClips[_currentAudioIndex].name;
                _currentLineShowBox.SetActive(true);
                _currentLineShowBox.GetComponent<Button>().interactable = true;
                _isProcessingResult = false;
            }
            else
            {
                _isViewed = true;
                GameManager_Junior1A.Instance.Next(true);
            }
        }
        else
        {
            _currentLineShowBox.GetComponent<WiggleEffect_Junior1A1>().enabled = true;
            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            _currentLineShowBox.GetComponent<Button>().interactable = false;
            yield return new WaitForSeconds(_audioSource.clip.length);
            _currentLineShowBox.GetComponent<Button>().interactable = true;
            _isProcessingResult = false;
        }
    }

    // ── Levenshtein Similarity ─────────────────────────────────────────────────

    float SimilarityPercent(string reference, string hypothesis)
    {
        string a = Normalize(reference);
        string b = Normalize(hypothesis);

        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
        if (a == b) return 1f;

        int dist   = Levenshtein(a, b);
        int maxLen = Mathf.Max(a.Length, b.Length);
        return 1f - (float)dist / maxLen;
    }

    string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                sb.Append(c);
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    int Levenshtein(string s, string t)
    {
        int n = s.Length, m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
        {
            char si = s[i - 1];
            for (int j = 1; j <= m; j++)
            {
                int cost = (si == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}