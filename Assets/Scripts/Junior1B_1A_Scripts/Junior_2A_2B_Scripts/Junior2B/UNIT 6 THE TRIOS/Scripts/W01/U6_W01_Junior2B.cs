using Junior2B;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U6_W01_Junior2B_QuestionData
{
    public string[] OptionTexts;
    public int CorrectOptionIndex;
}

public class U6_W01_Junior2B : MonoBehaviour, Interfaces_Junior2B
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _incorrectClip, _correctClip;
    [SerializeField] Transform _spawnBox, _questionParent;
    [SerializeField] TextMeshProUGUI _clickedIndexText, _clickedOptionText;
    [SerializeField] Button _clickedButton;
    [SerializeField] List<string> _defaultText;
    [SerializeField] U6_W01_Junior2B_QuestionData[] _questionData;
    [SerializeField] int _currentQuestionIndex = 0, _currentAnswerIndex = 0, _clickCheckIndex = 0;
    [SerializeField] Color _wrongColor, _correctColor;
    [SerializeField] bool _isViewed = false;
    
    Coroutine _coroutine;
    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        if (_questionParent == null || _questionData == null) yield break;

        int _currentDefaultOptionIndex = 0;
        foreach (Transform button in _questionParent.transform)
        {
            if (_currentDefaultOptionIndex < _questionData.Length)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = true;
                
                // Safe lookup for TMPro instead of chain-crashing GetChild checks
                TextMeshProUGUI textMesh = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (textMesh != null)
                {
                    if (_defaultText.Count == _questionData.Length) 
                        textMesh.text = _defaultText[_currentDefaultOptionIndex];
                    else 
                        _defaultText.Add(textMesh.text);

                    if (textMesh.TryGetComponent(out TextPopEffect_Junior2B textPop)) 
                        textPop.enabled = false;
                }

                _currentDefaultOptionIndex++;
            }
            button.gameObject.SetActive(false);
        }

        if (_spawnBox != null)
        {
            foreach (Transform button in _spawnBox.transform) button.gameObject.SetActive(false);
        }
        
        _coroutine = null;
        _currentQuestionIndex = _currentAnswerIndex = _clickCheckIndex = 0;
        
        if (_clickedIndexText != null) 
            _clickedIndexText.text = $"{_clickCheckIndex}/{_questionData.Length}";
            
        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }

        foreach (Transform button in _questionParent.transform)
        {
            yield return new WaitForSeconds(.25f);
            button.gameObject.SetActive(true);
        }
    }

    public void ChooseQuestion(int index)
    {
        if (_spawnBox == null || _questionData == null || index >= _questionData.Length) return;

        _currentQuestionIndex = index;
        _currentAnswerIndex = 0;
        
        foreach (Transform button in _spawnBox)
        {
            if (_currentAnswerIndex >= _questionData[_currentQuestionIndex].OptionTexts.Length) break;

            if (button.TryGetComponent(out Image img)) img.color = Color.white;
            
            TextMeshProUGUI textMesh = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (textMesh != null) 
                textMesh.text = _questionData[_currentQuestionIndex].OptionTexts[_currentAnswerIndex];
                
            if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            if (button.TryGetComponent(out Popeffect_Junior2B pop)) pop.enabled = true;
            
            button.gameObject.SetActive(true);
            _currentAnswerIndex++;
        }
        _currentAnswerIndex = 0;
    }

    public void SetText(TextMeshProUGUI optionText) => _clickedOptionText = optionText;
    public void SetButton(Button optionButton) => _clickedButton = optionButton;

    public void ChooseOption(int index)
    {
        if (_spawnBox == null) return;
        
        if (_currentAnswerIndex < _spawnBox.childCount)
        {
            if (_spawnBox.GetChild(_currentAnswerIndex).TryGetComponent(out Image img)) 
                img.color = Color.white;
        }

        _currentAnswerIndex = index;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(CheckOption());
    }

    IEnumerator CheckOption()
    {
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length || _spawnBox == null) yield break;

        var currentQuestion = _questionData[_currentQuestionIndex];
        bool isCorrect = currentQuestion.CorrectOptionIndex < 0 || _currentAnswerIndex == currentQuestion.CorrectOptionIndex;

        if (_currentAnswerIndex >= _spawnBox.childCount) yield break;
        Transform targetButton = _spawnBox.GetChild(_currentAnswerIndex);

        if (isCorrect)
        {
            if (targetButton.TryGetComponent(out Image img)) img.color = _correctColor;
            
            foreach (Transform button in _spawnBox.transform) 
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = false;
            }
            
            if (_clickedOptionText != null)
            {
                if (_currentAnswerIndex < currentQuestion.OptionTexts.Length)
                    _clickedOptionText.text = currentQuestion.OptionTexts[_currentAnswerIndex];
                    
                if (_clickedOptionText.TryGetComponent(out TextPopEffect_Junior2B textPop)) 
                    textPop.enabled = true;
            }
            
            if (_clickedButton != null) _clickedButton.interactable = false;
            
            if (_audioSource != null && _correctClip != null)
            {
                _audioSource.clip = _correctClip;
                _audioSource.Play();
            }
            
            _clickCheckIndex++;
            if (_clickedIndexText != null) 
                _clickedIndexText.text = $"{_clickCheckIndex}/{_questionData.Length}";
                
            if (_clickCheckIndex == _questionData.Length)
            {
                if (GameManager_Junior2B.Instance != null)
                {
                    GameManager_Junior2B.Instance.Next(true);
                }
                _isViewed = true;
            }
        }
        else
        {
            if (targetButton.TryGetComponent(out Image img)) img.color = _wrongColor;
            
            if (_audioSource != null && _incorrectClip != null)
            {
                _audioSource.clip = _incorrectClip;
                _audioSource.Play();
                
                if (targetButton.TryGetComponent(out WiggleEffect_Junior2B wiggle)) 
                    wiggle.enabled = true;
                    
                yield return new WaitForSeconds(_incorrectClip.length);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            foreach (Transform button in _spawnBox.transform) 
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            }
        }
    }
}