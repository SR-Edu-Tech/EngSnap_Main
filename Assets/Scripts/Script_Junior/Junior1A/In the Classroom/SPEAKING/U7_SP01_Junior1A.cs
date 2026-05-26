using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U7_SP01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] AudioClip[] _questionClips;
    [SerializeField] string[] _answerText;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] GameObject _currentLineShowBox, _micObj;
    [SerializeField] TextMeshProUGUI _feedbackText, _percentageText;
    [SerializeField] Slider _progressBar;
    [SerializeField, Range(0f, 1f)] float passThreshold = 0.75f;
    [SerializeField] bool _isProcessingResult = false, _isViewed = false, _isMicOn = false;
    Coroutine _coroutine;

    public bool IsViewed => _isViewed;
    void OnEnable()
    {
        _micObj.GetComponent<Button>().interactable = true;
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
        StartCoroutine(Starter());
    }

    void OnDisable() => CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;

    public void MicToogle()
    {
        _isMicOn = !_isMicOn;
        if (_isMicOn)
        {
            _micObj.transform.GetChild(0).GetComponent<Image>().color = Color.red;
            CrossPlatformSpeechManager.Instance?.StartListening();
            _progressBar.gameObject.SetActive(true);
            _feedbackText.text = "Listening...";
            _percentageText.text = "";
        }
        else
        {
            _micObj.transform.GetChild(0).GetComponent<Image>().color = Color.black;
            CrossPlatformSpeechManager.Instance?.StopListening();
        }
    }

    IEnumerator Starter()
    {
        _progressBar.gameObject.SetActive(false);
        _currentAudioIndex = 0;
        _feedbackText.text = "";
        _currentLineShowBox.SetActive(false);
        _micObj.SetActive(false);
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        ShowTargetWord();
    }
    public void PlayAudioClip()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(PlayClip());
    }
    IEnumerator PlayClip()
    {
        _currentLineShowBox.transform.GetChild(1).GetComponent<Image>().enabled = true;
        _audioSource.clip = _questionClips[_currentAudioIndex];
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        _currentLineShowBox.transform.GetChild(1).GetComponent<Image>().enabled = false;
    }

    void ShowTargetWord()
    {
        _feedbackText.text = _percentageText.text = "";
        _currentLineShowBox.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _answerText[_currentAudioIndex];
        _currentLineShowBox.SetActive(true);
        _micObj.SetActive(true);
        _isProcessingResult = false;
    }

    void OnSpeechResult(string spokenText)
    {
        if (_isProcessingResult) return;
        EvaluateSpeech(spokenText, true);
        if (_isMicOn) MicToogle();
    }

    void EvaluateSpeech(string text, bool final)
    {
        float score = SimilarityPercent(_answerText[_currentAudioIndex], text);
        _feedbackText.text = text;
        _percentageText.text = Mathf.RoundToInt(score * 100) + "%";
        _progressBar.value = score;
        _progressBar.fillRect.GetComponent<Image>().color = Color.HSVToRGB(Mathf.Lerp(0f, 0.33f, score), 0.9f, 0.6f);

        if (score >= passThreshold)
        {
            _isProcessingResult = true;
            CrossPlatformSpeechManager.Instance.StopListening();
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
            if (_currentAudioIndex < _questionClips.Length - 1)
            {
                _currentAudioIndex++;
                ShowTargetWord();
            }
            else
            {
                _micObj.GetComponent<Button>().interactable = false;
                GameManager_Junior1A.Instance.Next(true);
                _isViewed = true;
            }
        }
        else
        {
            _audioSource.PlayOneShot(_wrongClip);
            yield return new WaitForSeconds(1.2f);
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
}
