using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U1_W01_Junior1B_QuestionData
{
    public AudioClip QuestionClip, AnswerClip;
    public string QuestionText, AnswerText;
    public string[] AnswerTexts;
    public string[] OptionText;
}

public class U1_W01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _incorrectClip, _correctClip;
    [SerializeField] Transform _spawnBox, _answerBox, _checkBox;
    [SerializeField] TextMeshProUGUI _questionText;
    [SerializeField] U1_W01_Junior1B_QuestionData[] _questionData;
    [SerializeField] int _currentQuestionIndex = 0;
    [SerializeField] string _currentAnswerText = "";
    [SerializeField] bool _isViewed = false;
    
    private Coroutine _coroutine;
    public bool IsViewed => _isViewed;
    
    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        _checkBox.gameObject.SetActive(false);
        _questionText.gameObject.SetActive(false);
        _coroutine = null;
        _currentQuestionIndex = 0;
        _currentAnswerText = string.Empty;
        
        ResetAllButtonsToSpawnBox();

        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length + 1f);
        
        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        if (_currentQuestionIndex >= _questionData.Length) return;

        _questionText.text = _questionData[_currentQuestionIndex].QuestionText;
        _questionText.gameObject.SetActive(true);
        
        _audioSource.clip = _questionData[_currentQuestionIndex].QuestionClip;
        _audioSource.Play();
        
        _checkBox.GetComponent<Button>().interactable = true;
        
        StartCoroutine(SpawnOptionsRoutine());
    }

    IEnumerator SpawnOptionsRoutine()
    {
        string[] currentOptions = _questionData[_currentQuestionIndex].OptionText;

        if (_spawnBox.childCount < currentOptions.Length)
        {
            Debug.LogError("❌ Not enough prefab child buttons instantiated under SpawnBox to display this question's options!");
            yield break;
        }

        for (int i = 0; i < currentOptions.Length; i++)
        {
            Transform choiceButton = _spawnBox.GetChild(i);
            
            // 💡 SCALE ENFORCEMENT: Keep the button scaling perfect upon visibility activation
            choiceButton.localScale = Vector3.one;

            if (choiceButton.childCount > 0)
            {
                var textComponent = choiceButton.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (textComponent != null) textComponent.text = currentOptions[i];
            }

            choiceButton.gameObject.SetActive(true);
            
            if (choiceButton.TryGetComponent(out Popeffect_Junior1B pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void GetData(Transform clickedButton)
    {
        if (clickedButton.parent == _spawnBox) 
            clickedButton.SetParent(_answerBox, false);
        else 
            clickedButton.SetParent(_spawnBox, false);
            
        // 💡 SCALE ENFORCEMENT: Force scale back to 1 when shifting parents via user selection clicks
        clickedButton.localScale = Vector3.one;

        LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_answerBox as RectTransform);

        if (_answerBox.childCount == _questionData[_currentQuestionIndex].OptionText.Length) 
            _checkBox.gameObject.SetActive(true);
        else 
            _checkBox.gameObject.SetActive(false);
    }

    public void CheckData()
    {
        if (_answerBox.childCount <= 0 || _coroutine != null) return;

        string targetAnswer = _questionData[_currentQuestionIndex].AnswerText.Trim();
        bool isSentenceMode = targetAnswer.Contains(" ");
        
        _currentAnswerText = "";
        for (int i = 0; i < _answerBox.childCount; i++)
        {
            Transform child = _answerBox.GetChild(i);
            string childText = child.childCount > 0 ? child.GetChild(0).GetComponent<TextMeshProUGUI>().text.Trim() : "";
            
            if (i == 0)
            {
                _currentAnswerText += childText;
            }
            else
            {
                if (isSentenceMode)
                {
                    _currentAnswerText += " " + childText;
                }
                else
                {
                    _currentAnswerText += childText;
                }
            }
        }

        _coroutine = StartCoroutine(CheckDataValidity());
    }

    IEnumerator CheckDataValidity()
    {
        string cleanedUserInput = _currentAnswerText.Trim();
        string cleanedTargetAnswer = _questionData[_currentQuestionIndex].AnswerText.Trim();

        if (cleanedUserInput.Equals(cleanedTargetAnswer, StringComparison.OrdinalIgnoreCase))
        {
            _checkBox.gameObject.SetActive(false);
            
            foreach (Transform obj in _answerBox) 
            {
                if (obj.TryGetComponent(out Button btn)) btn.interactable = false;
            }
            
            if (_answerBox.TryGetComponent(out Popeffect_Junior1B pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }

            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_correctClip.length);
            
            _audioSource.clip = _questionData[_currentQuestionIndex].AnswerClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);

            _currentQuestionIndex++;
            _currentAnswerText = string.Empty;

            if (_currentQuestionIndex < _questionData.Length)
            {
                ResetAllButtonsToSpawnBox();
                _questionText.gameObject.SetActive(false);
                ShowCurrentQuestion();
            }
            else
            {
                _checkBox.GetComponent<Button>().interactable = false;
                _isViewed = true;
                if (GameManager_Junior1B.Instance != null)
                {
                    GameManager_Junior1B.Instance.Next(true);
                }
            }
        }
        else
        {
            _audioSource.clip = _incorrectClip;
            _audioSource.Play();
            
            if (_answerBox.TryGetComponent(out WiggleEffect_Junior1B wiggle))
            {
                wiggle.enabled = false;
                wiggle.enabled = true;
            }
            
            yield return new WaitForSeconds(_incorrectClip.length);
        }
        
        _coroutine = null;
    }

    private void ResetAllButtonsToSpawnBox()
    {
        while (_answerBox.childCount > 0)
        {
            Transform child = _answerBox.GetChild(0);
            if (child.TryGetComponent(out Button btn)) btn.interactable = true;
            child.SetParent(_spawnBox);
            
            // 💡 SCALE ENFORCEMENT: Force scale resetting when returning children to spawn container
            child.localScale = Vector3.one;
            child.gameObject.SetActive(false);
        }

        foreach (Transform child in _spawnBox)
        {
            child.gameObject.SetActive(false);
            if (child.TryGetComponent(out Button btn)) btn.interactable = true;
            
            // 💡 SCALE ENFORCEMENT: Baseline pool maintenance guard
            child.localScale = Vector3.one;
        }
    }
}