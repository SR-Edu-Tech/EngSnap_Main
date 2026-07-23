using Junior2A;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U6_W01_Junior2A_QuestionData
{
    public string[] OptionTexts;
    public int CorrectOptionIndex;
}

public class U6_W01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _incorrectClip, _correctClip;
    [SerializeField] Transform _spawnBox, _questionParent;
    [SerializeField] TextMeshProUGUI _clickedIndexText, _clickedOptionText;
    [SerializeField] Button _clickedButton;
    [SerializeField] List<string> _defaultText;
    [SerializeField] U6_W01_Junior2A_QuestionData[] _questionData;
    [SerializeField] int _currentQuestionIndex = 0, _currentAnswerIndex = 0, _clickCheckIndex = 0;
    [SerializeField] Color _wrongColor, _correctColor;
    [SerializeField] bool _isViewed = false;

    Coroutine _coroutine;
    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        int _currentDefaultOptionIndex = 0;

        foreach (Transform button in _questionParent)
        {
            if (_currentDefaultOptionIndex < _questionData.Length)
            {
                if (button.TryGetComponent(out Button btn))
                {
                    btn.interactable = true;
                }

                // Safely grab the TextMeshProUGUI component within the button's children hierarchy
                TextMeshProUGUI textMesh = button.GetComponentInChildren<TextMeshProUGUI>();
                TextPopEffect_Junior2A popEffect = button.GetComponentInChildren<TextPopEffect_Junior2A>();

                if (textMesh != null)
                {
                    if (_defaultText.Count == _questionData.Length)
                    {
                        textMesh.text = _defaultText[_currentDefaultOptionIndex];
                    }
                    else
                    {
                        _defaultText.Add(textMesh.text);
                    }
                }

                if (popEffect != null)
                {
                    popEffect.enabled = false;
                }

                _currentDefaultOptionIndex++;
            }
            button.gameObject.SetActive(false);
        }

        foreach (Transform button in _spawnBox) button.gameObject.SetActive(false);

        _coroutine = null;
        _currentQuestionIndex = _currentAnswerIndex = _clickCheckIndex = 0;
        _clickedIndexText.text = $"{_clickCheckIndex}/{_questionData.Length}";

        if (_audioSource && _introClip)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }

        foreach (Transform button in _questionParent)
        {
            yield return new WaitForSeconds(.25f);
            button.gameObject.SetActive(true);
        }
    }

    public void ChooseQuestion(int index)
    {
        _currentQuestionIndex = index;
        _currentAnswerIndex = 0;

        foreach (Transform button in _spawnBox)
        {
            if (button.TryGetComponent(out Image img)) img.color = Color.white;

            TextMeshProUGUI textMesh = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh != null && _currentQuestionIndex < _questionData.Length && _currentAnswerIndex < _questionData[_currentQuestionIndex].OptionTexts.Length)
            {
                textMesh.text = _questionData[_currentQuestionIndex].OptionTexts[_currentAnswerIndex];
            }

            if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            if (button.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;

            button.gameObject.SetActive(true);
            _currentAnswerIndex++;
        }
        _currentAnswerIndex = 0;
    }

    public void SetText(TextMeshProUGUI optionText) => _clickedOptionText = optionText;
    public void SetButton(Button optionButton) => _clickedButton = optionButton;

    public void ChooseOption(int index)
    {
        if (_currentAnswerIndex < _spawnBox.childCount)
        {
            if (_spawnBox.GetChild(_currentAnswerIndex).TryGetComponent(out Image img)) img.color = Color.white;
        }

        _currentAnswerIndex = index;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(CheckOption());
    }

    IEnumerator CheckOption()
    {
        bool isCorrect = _questionData[_currentQuestionIndex].CorrectOptionIndex < 0 || _currentAnswerIndex == _questionData[_currentQuestionIndex].CorrectOptionIndex;

        if (isCorrect)
        {
            if (_spawnBox.GetChild(_currentAnswerIndex).TryGetComponent(out Image img)) img.color = _correctColor;

            foreach (Transform button in _spawnBox)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = false;
            }

            if (_clickedOptionText != null)
            {
                _clickedOptionText.text = _questionData[_currentQuestionIndex].OptionTexts[_currentAnswerIndex];
                if (_clickedOptionText.TryGetComponent(out TextPopEffect_Junior2A textPop)) textPop.enabled = true;
            }

            if (_clickedButton != null) _clickedButton.interactable = false;

            if (_audioSource && _correctClip)
            {
                _audioSource.clip = _correctClip;
                _audioSource.Play();
            }

            _clickCheckIndex++;
            _clickedIndexText.text = $"{_clickCheckIndex}/{_questionData.Length}";

            if (_clickCheckIndex == _questionData.Length)
            {
                if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
                _isViewed = true;
            }
        }
        else
        {
            if (_spawnBox.GetChild(_currentAnswerIndex).TryGetComponent(out Image img)) img.color = _wrongColor;

            if (_audioSource && _incorrectClip)
            {
                _audioSource.clip = _incorrectClip;
                _audioSource.Play();
            }

            if (_spawnBox.GetChild(_currentAnswerIndex).TryGetComponent(out WiggleEffect_Junior2A1 wiggle)) wiggle.enabled = true;

            float waitTime = _incorrectClip != null ? _incorrectClip.length : 0.5f;
            yield return new WaitForSeconds(waitTime);

            foreach (Transform button in _spawnBox)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            }
        }
    }
}