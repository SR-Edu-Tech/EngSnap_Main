using Junior2A;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U11_W01_Junior2A1_QuestionData
{
    public string[] OptionTexts;
    public int CorrectOptionIndex;
}

public class U11_W01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip, _incorrectClip, _correctClip;
    [SerializeField] private Transform _spawnBox, _questionParent;
    [SerializeField] private TextMeshProUGUI _clickedIndexText, _clickedOptionText;
    [SerializeField] private Button _clickedButton;
    [SerializeField] private List<string> _defaultText = new List<string>();

    [SerializeField] private U11_W01_Junior2A1_QuestionData[] _questionData;

    [SerializeField] private int _currentQuestionIndex = 0, _currentAnswerIndex = 0, _clickCheckIndex = 0;
    [SerializeField] private Color _wrongColor = Color.red, _correctColor = Color.green;
    [SerializeField] private bool _isViewed = false;

    private Coroutine _coroutine;
    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(Starter());

    private IEnumerator Starter()
    {
        int currentDefaultOptionIndex = 0;

        if (_questionParent != null && _questionData != null)
        {
            foreach (Transform button in _questionParent)
            {
                if (currentDefaultOptionIndex < _questionData.Length)
                {
                    if (button.TryGetComponent(out Button btn)) btn.interactable = true;

                    // Safely locate text component within child hierarchy
                    TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmpText != null)
                    {
                        if (_defaultText.Count == _questionData.Length)
                        {
                            tmpText.text = _defaultText[currentDefaultOptionIndex];
                        }
                        else
                        {
                            _defaultText.Add(tmpText.text);
                        }
                    }

                    // Safely disable pop effect if component exists
                    TextPopEffect_Junior2A popEffect = button.GetComponentInChildren<TextPopEffect_Junior2A>();
                    if (popEffect != null) popEffect.enabled = false;

                    currentDefaultOptionIndex++;
                }

                button.gameObject.SetActive(false);
            }
        }

        if (_spawnBox != null)
        {
            foreach (Transform button in _spawnBox) button.gameObject.SetActive(false);
        }

        _coroutine = null;
        _currentQuestionIndex = _currentAnswerIndex = _clickCheckIndex = 0;

        int totalQuestions = _questionData != null ? _questionData.Length : 0;
        if (_clickedIndexText != null) _clickedIndexText.text = $"{_clickCheckIndex}/{totalQuestions}";

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }

        if (_questionParent != null)
        {
            foreach (Transform button in _questionParent)
            {
                yield return new WaitForSeconds(.25f);
                button.gameObject.SetActive(true);
            }
        }

        yield return null;
    }

    public void ChooseQuestion(int index)
    {
        if (_questionData == null || index < 0 || index >= _questionData.Length) return;

        _currentQuestionIndex = index;
        _currentAnswerIndex = 0;

        if (_spawnBox == null) return;

        foreach (Transform button in _spawnBox)
        {
            if (button.TryGetComponent(out Image img)) img.color = Color.white;

            TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null && _currentAnswerIndex < _questionData[_currentQuestionIndex].OptionTexts.Length)
            {
                tmpText.text = _questionData[_currentQuestionIndex].OptionTexts[_currentAnswerIndex];
            }

            if (button.TryGetComponent(out Button btn)) btn.interactable = true;

            if (button.TryGetComponent(out PopEffect_Junior2A pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }

            button.gameObject.SetActive(true);
            _currentAnswerIndex++;
        }

        _currentAnswerIndex = 0;
    }

    public void SetText(TextMeshProUGUI optionText) => _clickedOptionText = optionText;
    public void SetButton(Button optionButton) => _clickedButton = optionButton;

    public void ChooseOption(int index)
    {
        if (_spawnBox == null || index < 0 || index >= _spawnBox.childCount) return;

        if (_currentAnswerIndex < _spawnBox.childCount)
        {
            if (_spawnBox.GetChild(_currentAnswerIndex).TryGetComponent(out Image img))
            {
                img.color = Color.white;
            }
        }

        _currentAnswerIndex = index;

        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(CheckOption());
    }

    private IEnumerator CheckOption()
    {
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length) yield break;

        bool isCorrect = _questionData[_currentQuestionIndex].CorrectOptionIndex < 0 ||
                         _currentAnswerIndex == _questionData[_currentQuestionIndex].CorrectOptionIndex;

        Transform activeOption = _spawnBox.GetChild(_currentAnswerIndex);

        if (isCorrect)
        {
            if (activeOption.TryGetComponent(out Image img)) img.color = _correctColor;

            foreach (Transform button in _spawnBox)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = false;
            }

            if (_clickedOptionText != null && _currentAnswerIndex < _questionData[_currentQuestionIndex].OptionTexts.Length)
            {
                _clickedOptionText.text = _questionData[_currentQuestionIndex].OptionTexts[_currentAnswerIndex];

                if (_clickedOptionText.TryGetComponent(out TextPopEffect_Junior2A pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
            }

            if (_clickedButton != null) _clickedButton.interactable = false;

            if (_audioSource != null && _correctClip != null)
            {
                _audioSource.clip = _correctClip;
                _audioSource.Play();
            }

            _clickCheckIndex++;

            if (_clickedIndexText != null)
            {
                _clickedIndexText.text = $"{_clickCheckIndex}/{_questionData.Length}";
            }

            if (_clickCheckIndex == _questionData.Length)
            {
                _isViewed = true;
                if (GameManager_Junior2A.Instance != null)
                {
                    GameManager_Junior2A.Instance.Next(true);
                }
            }
        }
        else
        {
            if (activeOption.TryGetComponent(out Image img)) img.color = _wrongColor;

            if (_audioSource != null && _incorrectClip != null)
            {
                _audioSource.clip = _incorrectClip;
                _audioSource.Play();
            }

            if (activeOption.TryGetComponent(out WiggleEffect_Junior2A wiggle))
            {
                wiggle.enabled = false;
                wiggle.enabled = true;
            }

            float waitDuration = (_incorrectClip != null) ? _incorrectClip.length : 0.5f;
            yield return new WaitForSeconds(waitDuration);

            foreach (Transform button in _spawnBox)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            }
        }
    }
}