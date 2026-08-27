using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U9_W01_Junior2A_QuestionData
{
    public AudioClip QuestionClip, AnswerClip;
    public string QuestionText, AnswerText;
    public string[] AnswerTexts;
    public string[] OptionText;

    [Tooltip("If true, treats OptionText elements as full words. If false, breaks words into individual letters.")]
    public bool useWholeWords = false;
}

public class U9_W01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _incorrectClip, _correctClip;
    [SerializeField] Transform _spawnBox, _answerBox, _checkBox;
    [SerializeField] TextMeshProUGUI _questionText;
    [SerializeField] U9_W01_Junior2A_QuestionData[] _questionData;
    [SerializeField] int _currentQuestionIndex = 0, _currentAnswerIndex = 0;
    [SerializeField] Color _wrongColor, _correctColor;
    [SerializeField] string _currentAnswerText = "";
    [SerializeField] bool _isViewed = false;
    Coroutine _coroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        _checkBox.gameObject.SetActive(false);
        _questionText.gameObject.SetActive(false);
        _coroutine = null;
        _currentQuestionIndex = _currentAnswerIndex = 0;
        _currentAnswerText = string.Empty;
        _answerBox.GetComponent<Image>().color = Color.white;

        ResetBoxes();

        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length + 1f);

        SetupQuestionDisplay();
        yield return StartCoroutine(SpawnOptionsSequence());
    }

    private void ResetBoxes()
    {
        while (_answerBox.childCount > 0)
        {
            Transform child = _answerBox.GetChild(0);
            child.GetComponent<Button>().interactable = true;
            child.GetComponent<Image>().color = Color.white;
            child.SetParent(_spawnBox, false);
            child.localScale = Vector3.one;
        }

        foreach (Transform child in _spawnBox)
        {
            child.gameObject.SetActive(false);
            child.localScale = Vector3.one;
            if (child.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
        }
    }

    private void SetupQuestionDisplay()
    {
        _questionText.text = _questionData[_currentQuestionIndex].QuestionText;
        _questionText.gameObject.SetActive(true);
        _audioSource.clip = _questionData[_currentQuestionIndex].QuestionClip;
        _audioSource.Play();
        _checkBox.GetComponent<Button>().interactable = true;
    }

    IEnumerator SpawnOptionsSequence()
    {
        var currentData = _questionData[_currentQuestionIndex];
        _currentAnswerIndex = 0;

        foreach (string option in currentData.OptionText)
        {
            if (currentData.useWholeWords)
            {
                if (_currentAnswerIndex >= _spawnBox.childCount) break;
                SpawnItem(option);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                for (int i = 0; i < option.Length; i++)
                {
                    if (_currentAnswerIndex >= _spawnBox.childCount) break;
                    SpawnItem(option[i].ToString());
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
    }

    private void SpawnItem(string displayContent)
    {
        Transform item = _spawnBox.GetChild(_currentAnswerIndex);
        item.GetChild(0).GetComponent<TextMeshProUGUI>().text = displayContent;
        item.localScale = Vector3.one;
        item.gameObject.SetActive(true);
        if (item.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;

        _currentAnswerIndex++;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
    }

    public void GetData(Transform targetObject)
    {
        if (targetObject.transform.parent == _spawnBox) targetObject.SetParent(_answerBox, false);
        else targetObject.SetParent(_spawnBox, false);

        targetObject.localScale = Vector3.one;

        int expectedLength = 0;
        var currentData = _questionData[_currentQuestionIndex];

        if (currentData.useWholeWords)
        {
            expectedLength = currentData.OptionText.Length;
        }
        else
        {
            foreach (string s in currentData.OptionText) expectedLength += s.Length;
        }

        if (_answerBox.childCount == expectedLength) _checkBox.gameObject.SetActive(true);
        else _checkBox.gameObject.SetActive(false);
    }

    public void CheckData()
    {
        if (_answerBox.childCount <= 0) return;
        _currentAnswerIndex = 0;
        _currentAnswerText = "";

        var currentData = _questionData[_currentQuestionIndex];

        // Rebuild your text checking loop securely
        foreach (Transform child in _answerBox)
        {
            string cleanText = child.GetChild(0).GetComponent<TextMeshProUGUI>().text;

            // If it's a full word mode, add spaces dynamically between array elements to match standard sentences
            if (currentData.useWholeWords && _currentAnswerText.Length > 0)
            {
                _currentAnswerText += " ";
            }

            _currentAnswerText += cleanText;
            _currentAnswerIndex++;
        }

        if (_coroutine == null) _coroutine = StartCoroutine(CheckDataValidity());
    }

    IEnumerator CheckDataValidity()
    {
        var currentData = _questionData[_currentQuestionIndex];

        // Clean up both strings (trim extra spaces, ignore case structure checks)
        string processedAnswer = _currentAnswerText.Trim();
        string processedTarget = currentData.AnswerText.Trim();

        if (string.Equals(processedAnswer, processedTarget, StringComparison.OrdinalIgnoreCase))
        {
            _checkBox.gameObject.SetActive(false);
            foreach (Transform obj in _answerBox) obj.GetComponent<Button>().interactable = false;

            if (_answerBox.TryGetComponent(out PopEffect_Junior2A pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }

            foreach (Transform option in _answerBox) option.GetComponent<Image>().color = _correctColor;

            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_correctClip.length);

            _audioSource.clip = currentData.AnswerClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);

            _currentQuestionIndex++;

            if (_currentQuestionIndex < _questionData.Length)
            {
                _questionText.gameObject.SetActive(false);
                _questionText.text = _questionData[_currentQuestionIndex].QuestionText;
                _questionText.gameObject.SetActive(true);
                _audioSource.clip = _questionData[_currentQuestionIndex].QuestionClip;
                _audioSource.Play();

                foreach (Transform option in _answerBox) option.GetComponent<Image>().color = Color.white;
                while (_answerBox.childCount > 0)
                {
                    Transform child = _answerBox.GetChild(0);
                    child.GetComponent<Button>().interactable = true;
                    child.SetParent(_spawnBox, false);
                    child.localScale = Vector3.one;
                    child.gameObject.SetActive(false);
                }

                yield return StartCoroutine(SpawnOptionsSequence());
            }
            else
            {
                _checkBox.GetComponent<Button>().interactable = false;
                _isViewed = true;
                if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            }
        }
        else
        {
            // Fallback incorrect sequence block logic trigger
            _audioSource.clip = _incorrectClip;
            _audioSource.Play();

            int optionIdx = 0;
            foreach (Transform option in _answerBox)
            {
                string textInButton = option.GetChild(0).GetComponent<TextMeshProUGUI>().text.Trim();

                if (optionIdx < currentData.AnswerTexts.Length &&
                    string.Equals(textInButton, currentData.AnswerTexts[optionIdx].Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    option.GetComponent<Image>().color = _correctColor;
                }
                else
                {
                    option.GetComponent<Image>().color = _wrongColor;
                }
                optionIdx++;
            }

            if (GetComponent<WiggleEffect_Junior2A>() != null) GetComponent<WiggleEffect_Junior2A>().enabled = true;
            else if (_answerBox.GetComponent<WiggleEffect_Junior2A>() != null) _answerBox.GetComponent<WiggleEffect_Junior2A>().enabled = true;

            yield return new WaitForSeconds(_incorrectClip.length);

            foreach (Transform option in _answerBox) option.GetComponent<Image>().color = Color.white;
            foreach (Transform option in _spawnBox) option.GetComponent<Image>().color = Color.white;
        }
        _coroutine = null;
    }
}