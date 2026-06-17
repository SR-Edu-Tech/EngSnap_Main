using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U13_W01_Junior1A_QuestionData
{
    [Header("Audio Elements")]
    [Tooltip("The audio clip for the question prompt that plays as soon as the round loads.")]
    public AudioClip QuestionAudioClip;

    [Tooltip("The audio clip read out loud when the correct answer is verified.")]
    public AudioClip AnswerAudioClip;
    
    [Header("Text Configurations")]
    [Tooltip("The actual question string that will appear in your Question Text UI box.")]
    public string QuestionPromptText;

    [Tooltip("The complete correct string match (e.g., 'Thank You'). Case and space sensitive!")]
    public string AnswerText;
    
    [Tooltip("The correct sequential broken down items used to cross-reference highlighting styles on individual buttons.")]
    public string[] AnswerTexts;
    
    [Tooltip("The scrambled words distributed across buttons for the child player selection pool.")]
    public string[] OptionText;
}

public class U13_W01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Global Audio Components")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip _incorrectClip;
    [SerializeField] private AudioClip _correctClip;

    [Header("Core UI Container Parents")]
    [SerializeField] private Transform _spawnBox;
    [SerializeField] private Transform _answerBox;
    [SerializeField] private Transform _checkBox;
    
    [Header("Text UI Elements")]
    [Tooltip("Drag the TextMeshProUGUI component that displays the question prompt text here.")]
    [SerializeField] private TextMeshProUGUI _questionTextDisplay;

    [Header("Data Configurations")]
    [SerializeField] private U13_W01_Junior1A_QuestionData[] _questionData;
    [SerializeField] private Color _wrongColor = Color.red;
    [SerializeField] private Color _correctColor = Color.green;

    [Header("Runtime State Monitor")]
    [SerializeField] private int _currentQuestionIndex = 0;
    [SerializeField] private int _currentAnswerIndex = 0;
    [SerializeField] private string _currentAnswerText = "";
    [SerializeField] private bool _isViewed = false;

    private Coroutine _coroutine;
    
    public bool IsViewed => _isViewed;

    private void OnEnable()
    {
        _isViewed = false;
        StartCoroutine(Starter());
    }

    private IEnumerator Starter()
    {
        GameManager_Junior1A.Instance?.Next(false);

        _checkBox.gameObject.SetActive(false);
        _coroutine = null;
        _currentQuestionIndex = 0;
        _currentAnswerIndex = 0;
        _currentAnswerText = string.Empty;
        
        if (_questionTextDisplay != null) _questionTextDisplay.text = string.Empty;
        if (_answerBox.TryGetComponent(out Image answerImg)) answerImg.color = Color.white;

        // Reset elements and return them to the spawn container layout pool
        while (_answerBox.childCount > 0)
        {
            Transform child = _answerBox.GetChild(0);
            if (child.TryGetComponent(out Button btn)) btn.interactable = true;
            if (child.TryGetComponent(out Image img)) img.color = Color.white;
            child.SetParent(_spawnBox);
        }

        foreach (Transform child in _spawnBox)
        {
            child.gameObject.SetActive(false);
            if (child.TryGetComponent(out PopEffect_Junior1A pop)) pop.enabled = true;
        }

        // Play overall game lesson introduction clip first
        if (_introClip != null && _audioSource != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length + 0.5f);
        }

        if (_checkBox.TryGetComponent(out Button checkBtn)) checkBtn.interactable = true;

        yield return StartCoroutine(LoadCurrentQuestionLayoutRoutine());
    }

    private IEnumerator LoadCurrentQuestionLayoutRoutine()
    {
        if (_currentQuestionIndex >= _questionData.Length) yield break;

        _currentAnswerIndex = 0;
        _currentAnswerText = string.Empty;

        var currentRoundData = _questionData[_currentQuestionIndex];

        // Update the question text UI right as the round starts
        if (_questionTextDisplay != null)
        {
            _questionTextDisplay.text = currentRoundData.QuestionPromptText;
        }

        // Play the question prompt audio clip right before showing word button choices
        if (currentRoundData.QuestionAudioClip != null && _audioSource != null)
        {
            _audioSource.clip = currentRoundData.QuestionAudioClip;
            _audioSource.Play();
            yield return new WaitForSeconds(currentRoundData.QuestionAudioClip.length + 0.5f);
        }

        // Pop words on screen inside the spawn box layout container sequential loop
        foreach (string data in currentRoundData.OptionText)
        {
            if (_currentAnswerIndex < _spawnBox.childCount)
            {
                Transform targetCard = _spawnBox.GetChild(_currentAnswerIndex);
                
                TextMeshProUGUI tmpText = targetCard.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (tmpText != null) tmpText.text = data;

                targetCard.gameObject.SetActive(true);
                
                if (targetCard.TryGetComponent(out Button btn)) btn.interactable = false;
                if (targetCard.TryGetComponent(out PopEffect_Junior1A pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }

                _currentAnswerIndex++;
                LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
                yield return new WaitForSeconds(0.4f);
            }
        }

        // Reactivate button mechanics after spawning wraps up
        foreach (Transform option in _spawnBox)
        {
            if (option.TryGetComponent(out Button btn)) btn.interactable = true;
        }
    }

    /// <summary>
    /// Hook this up to the OnClick event of each word button template inside your SPAWN_BOX!
    /// Pass the button's own Transform using the Inspector parameter field.
    /// </summary>
    public void GetData(Transform selectedButtonTransform)
    {
        if (_coroutine != null) return; // Block input modifications while validation checks play

        // Toggles button automatically between boxes depending on its current parent context
        if (selectedButtonTransform.parent == _spawnBox)
        {
            selectedButtonTransform.SetParent(_answerBox, false);
        }
        else
        {
            selectedButtonTransform.SetParent(_spawnBox, false);
        }

        // Instant visual layout updates
        LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_answerBox as RectTransform);

        // Turn on check confirmation box once selecting options equal to the answer target criteria size
        if (_answerBox.childCount == _questionData[_currentQuestionIndex].OptionText.Length)
        {
            _checkBox.gameObject.SetActive(true);
        }
        else
        {
            _checkBox.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Connect this method to your main Submit/Check verification Button element!
    /// </summary>
    public void CheckData()
    {
        if (_answerBox.childCount <= 0 || _coroutine != null) return;
        
        _currentAnswerIndex = 0;
        _currentAnswerText = "";

        // Formulate output string phrase compilation matrix
        foreach (Transform child in _answerBox)
        {
            string wordText = child.GetChild(0).GetComponent<TextMeshProUGUI>().text;
            if (_currentAnswerIndex == _answerBox.childCount - 1)
            {
                _currentAnswerText += wordText;
            }
            else
            {
                _currentAnswerText += wordText + " ";
            }
            _currentAnswerIndex++;
        }

        _coroutine = StartCoroutine(CheckDataValidity());
    }

    private IEnumerator CheckDataValidity()
    {
        var currentData = _questionData[_currentQuestionIndex];

        if (_currentAnswerText == currentData.AnswerText)
        {
            _checkBox.gameObject.SetActive(false);

            foreach (Transform obj in _answerBox)
            {
                if (obj.TryGetComponent(out Button btn)) btn.interactable = false;
                if (obj.TryGetComponent(out Image img)) img.color = _correctColor;
            }

            if (_answerBox.TryGetComponent(out PopEffect_Junior1A boxPop))
            {
                boxPop.enabled = false;
                boxPop.enabled = true;
            }

            if (_correctClip != null && _audioSource != null)
            {
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_correctClip.length);
            }

            // Play the distinct answer reinforcement clip upon successful matching verification
            if (currentData.AnswerAudioClip != null && _audioSource != null)
            {
                _audioSource.clip = currentData.AnswerAudioClip;
                _audioSource.Play();
                yield return new WaitForSeconds(currentData.AnswerAudioClip.length + 0.5f);
            }

            _currentQuestionIndex++;

            if (_currentQuestionIndex < _questionData.Length)
            {
                // Clean graphics and pivot focus tracking states forward to process the next round
                foreach (Transform option in _answerBox)
                {
                    if (option.TryGetComponent(out Image img)) img.color = Color.white;
                }
                
                while (_answerBox.childCount > 0)
                {
                    Transform child = _answerBox.GetChild(0);
                    child.SetParent(_spawnBox);
                    child.gameObject.SetActive(false);
                }

                yield return StartCoroutine(LoadCurrentQuestionLayoutRoutine());
            }
            else
            {
                if (_checkBox.TryGetComponent(out Button checkBtn)) checkBtn.interactable = false;
                _isViewed = true;
                GameManager_Junior1A.Instance?.Next(true);
            }
        }
        else
        {
            // Execution block handling player sentence mismatch errors
            if (_incorrectClip != null && _audioSource != null)
            {
                _audioSource.clip = _incorrectClip;
                _audioSource.Play();
            }

            int checkIndex = 0;
            foreach (Transform option in _answerBox)
            {
                string textVal = option.GetChild(0).GetComponent<TextMeshProUGUI>().text;
                
                // Safe index bounds safety protection logic block
                if (checkIndex < currentData.AnswerTexts.Length && currentData.AnswerTexts[checkIndex] == textVal)
                {
                    if (option.TryGetComponent(out Image img)) img.color = _correctColor;
                }
                else
                {
                    if (option.TryGetComponent(out Image img)) img.color = _wrongColor;
                }
                checkIndex++;
            }

            if (_answerBox.TryGetComponent(out WiggleEffect_Junior1A1 wiggle))
            {
                wiggle.enabled = false;
                wiggle.enabled = true;
            }

            if (_incorrectClip != null) yield return new WaitForSeconds(_incorrectClip.length);
            else yield return new WaitForSeconds(1.0f);

            // Revert cards back to white base coloring standard layout frames so user can fix placement choices
            foreach (Transform option in _answerBox)
            {
                if (option.TryGetComponent(out Image img)) img.color = Color.white;
            }
            foreach (Transform option in _spawnBox)
            {
                if (option.TryGetComponent(out Image img)) img.color = Color.white;
            }
        }

        _coroutine = null;
    }
}