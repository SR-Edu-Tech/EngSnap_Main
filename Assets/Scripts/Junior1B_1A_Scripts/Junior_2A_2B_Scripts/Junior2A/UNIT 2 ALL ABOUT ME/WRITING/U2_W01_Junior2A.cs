using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U2_W01_Junior2A_QuestionData
{
    public AudioClip QuestionClip, AnswerClip;
    public string QuestionText, AnswerText;
    public string[] AnswerTexts; // Matches individual correct letters for validation layouts
    public string[] OptionText;   // Keep this array, but we read letters from elements now
}

public class U2_W01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _incorrectClip, _correctClip;
    [SerializeField] Transform _spawnBox, _answerBox, _checkBox;
    [SerializeField] TextMeshProUGUI _questionText;
    [SerializeField] U2_W01_Junior2A_QuestionData[] _questionData;
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

        while (_answerBox.childCount > 0)
        {
            Transform child = _answerBox.GetChild(0);
            child.GetComponent<Button>().interactable = true;
            child.GetComponent<Image>().color = Color.white;
            child.SetParent(_spawnBox);
        }

        foreach (Transform child in _spawnBox)
        {
            child.gameObject.SetActive(false);
            child.GetComponent<PopEffect_Junior2A>().enabled = true;
        }

        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length + 1f);

        _questionText.text = _questionData[_currentQuestionIndex].QuestionText;
        _questionText.gameObject.SetActive(true);
        _audioSource.clip = _questionData[_currentQuestionIndex].QuestionClip;
        _audioSource.Play();
        _checkBox.GetComponent<Button>().interactable = true;

        // Process letters instead of words
        foreach (string option in _questionData[_currentQuestionIndex].OptionText)
        {
            for (int i = 0; i < option.Length; i++)
            {
                if (_currentAnswerIndex >= _spawnBox.childCount) break;

                string letterData = option[i].ToString();
                _spawnBox.GetChild(_currentAnswerIndex).GetChild(0).GetComponent<TextMeshProUGUI>().text = letterData;
                _spawnBox.GetChild(_currentAnswerIndex).gameObject.SetActive(true);
                _currentAnswerIndex++;

                LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public void GetData(Transform gameObject)
    {
        if (gameObject.transform.parent == _spawnBox) gameObject.SetParent(_answerBox, false);
        else gameObject.SetParent(_spawnBox, false);

        // Dynamic target length assignment for checking data validity limits dynamically
        int expectedLength = 0;
        foreach (string s in _questionData[_currentQuestionIndex].OptionText) expectedLength += s.Length;

        if (_answerBox.childCount == expectedLength) _checkBox.gameObject.SetActive(true);
        else _checkBox.gameObject.SetActive(false);
    }

    public void CheckData()
    {
        if (_answerBox.childCount <= 0) return;
        _currentAnswerIndex = 0;
        _currentAnswerText = "";

        foreach (Transform child in _answerBox)
        {
            // Appends direct raw characters side-by-side without separating spaces
            _currentAnswerText += child.GetChild(0).GetComponent<TextMeshProUGUI>().text;
            _currentAnswerIndex++;
        }

        if (_coroutine == null) _coroutine = StartCoroutine(CheckDataValidity());
    }

    IEnumerator CheckDataValidity()
    {
        if (_currentAnswerText == _questionData[_currentQuestionIndex].AnswerText)
        {
            _checkBox.gameObject.SetActive(false);
            foreach (Transform obj in _answerBox) obj.GetComponent<Button>().interactable = false;

            _answerBox.GetComponent<PopEffect_Junior2A>().enabled = false;
            _answerBox.GetComponent<PopEffect_Junior2A>().enabled = true;
            foreach (Transform option in _answerBox) option.GetComponent<Image>().color = _correctColor;

            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_correctClip.length);

            _audioSource.clip = _questionData[_currentQuestionIndex].AnswerClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);

            _currentQuestionIndex++;
            _currentAnswerIndex = 0;
            _currentAnswerText = string.Empty;

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
                    child.SetParent(_spawnBox);
                    child.gameObject.SetActive(false);
                }

                foreach (string option in _questionData[_currentQuestionIndex].OptionText)
                {
                    for (int i = 0; i < option.Length; i++)
                    {
                        if (_currentAnswerIndex >= _spawnBox.childCount) break;

                        string letterData = option[i].ToString();
                        _spawnBox.GetChild(_currentAnswerIndex).GetChild(0).GetComponent<TextMeshProUGUI>().text = letterData;
                        _spawnBox.GetChild(_currentAnswerIndex).gameObject.SetActive(true);
                        _spawnBox.GetChild(_currentAnswerIndex).GetComponent<PopEffect_Junior2A>().enabled = true;
                        _currentAnswerIndex++;

                        LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
                        yield return new WaitForSeconds(0.5f);
                    }
                }
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
            int _currentOptionIndex = 0;
            _audioSource.clip = _incorrectClip;
            _audioSource.Play();

            foreach (Transform option in _answerBox)
            {
                if (_currentOptionIndex < _questionData[_currentQuestionIndex].AnswerTexts.Length &&
                    _questionData[_currentQuestionIndex].AnswerTexts[_currentOptionIndex] == option.GetChild(0).GetComponent<TextMeshProUGUI>().text)
                {
                    option.GetComponent<Image>().color = _correctColor;
                }
                else
                {
                    option.GetComponent<Image>().color = _wrongColor;
                }
                _currentOptionIndex++;
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