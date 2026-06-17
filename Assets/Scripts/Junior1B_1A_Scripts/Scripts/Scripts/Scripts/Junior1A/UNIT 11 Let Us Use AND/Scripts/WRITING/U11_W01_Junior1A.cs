using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U11_W01_Junior1A1_QuestionData
{
    public string[] OptionTexts;
    public int CorrectOptionIndex;
}

// Class name is updated to strictly match the file name 'U11_W01_Junior1A1'
public class U11_W01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip, _incorrectClip, _correctClip;
    [SerializeField] private Transform _spawnBox, _questionParent;
    [SerializeField] private TextMeshProUGUI _clickedIndexText, _clickedOptionText;
    [SerializeField] private Button _clickedButton;
    [SerializeField] private List<string> _defaultText;

    // Fixed mismatched data structure reference name here
    [SerializeField] private U11_W01_Junior1A1_QuestionData[] _questionData;

    [SerializeField] private int _currentQuestionIndex = 0, _currentAnswerIndex = 0, _clickCheckIndex = 0;
    [SerializeField] private Color _wrongColor, _correctColor;
    [SerializeField] private bool _isViewed = false;

    private Coroutine _coroutine;
    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(Starter());

    private IEnumerator Starter()
    {
        int _currentDefaultOptionIndex = 0;
        foreach (Transform button in _questionParent)
        {
            if (_currentDefaultOptionIndex < _questionData.Length)
            {
                button.GetComponent<Button>().interactable = true;
                if (_defaultText.Count == _questionData.Length)
                    button.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = _defaultText[_currentDefaultOptionIndex];
                else
                    _defaultText.Add(button.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text);

                button.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextPopEffect_Junior1A>().enabled = false;
                _currentDefaultOptionIndex++;
            }
            button.gameObject.SetActive(false);
        }
        foreach (Transform button in _spawnBox) button.gameObject.SetActive(false);
        _coroutine = null;
        _currentQuestionIndex = _currentAnswerIndex = _clickCheckIndex = 0;
        _clickedIndexText.text = $"{_clickCheckIndex}/{_questionData.Length}";
        _audioSource.clip = _introClip;
        _audioSource.Play();
        foreach (Transform button in _questionParent)
        {
            yield return new WaitForSeconds(.25f);
            button.gameObject.SetActive(true);
        }
        yield return null;
    }

    public void ChooseQuestion(int index)
    {
        _currentQuestionIndex = index;
        _currentAnswerIndex = 0;
        foreach (Transform button in _spawnBox)
        {
            button.GetComponent<Image>().color = Color.white;
            button.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].OptionTexts[_currentAnswerIndex];
            button.GetComponent<Button>().interactable = true;
            button.GetComponent<PopEffect_Junior1A>().enabled = true;
            button.gameObject.SetActive(true);
            _currentAnswerIndex++;
        }
        _currentAnswerIndex = 0;
    }

    public void SetText(TextMeshProUGUI optionText) => _clickedOptionText = optionText;
    public void SetButton(Button optionButton) => _clickedButton = optionButton;

    public void ChooseOption(int index)
    {
        _spawnBox.GetChild(_currentAnswerIndex).GetComponent<Image>().color = Color.white;
        _currentAnswerIndex = index;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(CheckOption());
    }

    private IEnumerator CheckOption()
    {
        bool isCorrect = _questionData[_currentQuestionIndex].CorrectOptionIndex < 0 || _currentAnswerIndex == _questionData[_currentQuestionIndex].CorrectOptionIndex;

        if (isCorrect)
        {
            _spawnBox.GetChild(_currentAnswerIndex).GetComponent<Image>().color = _correctColor;
            foreach (Transform button in _spawnBox) button.GetComponent<Button>().interactable = false;
            _clickedOptionText.text = _questionData[_currentQuestionIndex].OptionTexts[_currentAnswerIndex];
            _clickedOptionText.GetComponent<TextPopEffect_Junior1A>().enabled = true;
            _clickedButton.interactable = false;
            _audioSource.clip = _correctClip;
            _audioSource.Play();
            _clickCheckIndex++;
            _clickedIndexText.text = $"{_clickCheckIndex}/{_questionData.Length}";
            if (_clickCheckIndex == _questionData.Length)
            {
                GameManager_Junior1A.Instance.Next(true);
                _isViewed = true;
            }
        }
        else
        {
            _spawnBox.GetChild(_currentAnswerIndex).GetComponent<Image>().color = _wrongColor;
            _audioSource.clip = _incorrectClip;
            _audioSource.Play();
            _spawnBox.GetChild(_currentAnswerIndex).GetComponent<WiggleEffect_Junior1A1>().enabled = true;
            yield return new WaitForSeconds(_incorrectClip.length);
            foreach (Transform button in _spawnBox) button.GetComponent<Button>().interactable = true;
        }
    }
}