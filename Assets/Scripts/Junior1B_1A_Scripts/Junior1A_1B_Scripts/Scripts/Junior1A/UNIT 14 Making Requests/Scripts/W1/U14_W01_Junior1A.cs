using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U14_W01_Junior1A_QuestionData
{
    [Header("Audio Elements")]
    public AudioClip QuestionAudioClip;
    public AudioClip AnswerAudioClip;
    
    [Header("Text Configurations")]
    public string QuestionPromptText;
    public string AnswerText;
    public string[] AnswerTexts;
    public string[] OptionText;

    [Header("Question Target Graphics")]
    public Sprite QuestionIconSprite;
}

public class U14_W01_Junior1A : MonoBehaviour, Interfaces_Junior1A
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
    
    [Header("Text & Graphical UI Elements")]
    [SerializeField] private TextMeshProUGUI _questionTextDisplay;
    [SerializeField] private Image _questionIconDisplay;

    [Header("Unmodified Animation Links")]
    [Tooltip("Drag the TextMeshProUGUI GameObject that has your TextPopEffect_Junior1A attached to it here.")]
    [SerializeField] private GameObject _questionTextObject;

    [Tooltip("Drag your Question Image Icon GameObject that has your PopEffect_Junior1A attached to it here.")]
    [SerializeField] private GameObject _questionIconObject;

    [Header("Data Configurations")]
    [SerializeField] private U14_W01_Junior1A_QuestionData[] _questionData; 
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

    private void OnDisable()
    {
        _coroutine = null;
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
        if (_questionIconDisplay != null) _questionIconDisplay.gameObject.SetActive(false);
        if (_answerBox.TryGetComponent(out Image answerImg)) answerImg.color = Color.white;

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
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length) yield break;

        _currentAnswerIndex = 0;
        _currentAnswerText = string.Empty;

        var currentRoundData = _questionData[_currentQuestionIndex];
        if (currentRoundData == null) yield break;

        // 🔄 Force the image icon to cycle active states to re-trigger its native OnEnable loop
        if (_questionIconObject != null)
        {
            _questionIconObject.SetActive(false);

            if (_questionIconDisplay != null && currentRoundData.QuestionIconSprite != null)
            {
                _questionIconDisplay.sprite = currentRoundData.QuestionIconSprite;
                _questionIconObject.SetActive(true); 
            }
        }
        else if (_questionIconDisplay != null) // Fallback behavior if direct object field isn't assigned
        {
            if (currentRoundData.QuestionIconSprite != null)
            {
                _questionIconDisplay.sprite = currentRoundData.QuestionIconSprite;
                _questionIconDisplay.gameObject.SetActive(false);
                _questionIconDisplay.gameObject.SetActive(true);
            }
            else
            {
                _questionIconDisplay.gameObject.SetActive(false);
            }
        }

        // 🔄 Force the text box to cycle active states to re-trigger its native OnEnable loop
        if (_questionTextObject != null)
        {
            _questionTextObject.SetActive(false);
            
            if (_questionTextDisplay != null)
            {
                _questionTextDisplay.text = currentRoundData.QuestionPromptText;
            }
            
            _questionTextObject.SetActive(true);
        }
        else if (_questionTextDisplay != null) // Fallback behavior if direct object field isn't assigned
        {
            _questionTextDisplay.text = currentRoundData.QuestionPromptText;
            _questionTextDisplay.gameObject.SetActive(false);
            _questionTextDisplay.gameObject.SetActive(true);
        }

        if (currentRoundData.QuestionAudioClip != null && _audioSource != null)
        {
            _audioSource.clip = currentRoundData.QuestionAudioClip;
            _audioSource.Play();
            yield return new WaitForSeconds(currentRoundData.QuestionAudioClip.length + 0.5f);
        }

        if (currentRoundData.OptionText == null) yield break;

        foreach (string data in currentRoundData.OptionText)
        {
            if (_spawnBox != null && _currentAnswerIndex < _spawnBox.childCount)
            {
                Transform targetCard = _spawnBox.GetChild(_currentAnswerIndex);
                if (targetCard == null) continue;
                
                if (targetCard.childCount > 0)
                {
                    TextMeshProUGUI tmpText = targetCard.GetChild(0).GetComponent<TextMeshProUGUI>();
                    if (tmpText != null) tmpText.text = data;
                }

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

        if (_spawnBox != null)
        {
            foreach (Transform option in _spawnBox)
            {
                if (option.TryGetComponent(out Button btn)) btn.interactable = true;
            }
        }
    }

    public void GetData(Transform selectedButtonTransform)
    {
        if (selectedButtonTransform == null || _coroutine != null || _questionData == null) return;
        if (_currentQuestionIndex >= _questionData.Length || _questionData[_currentQuestionIndex] == null) return;

        if (selectedButtonTransform.parent == _spawnBox)
        {
            selectedButtonTransform.SetParent(_answerBox, false);
        }
        else
        {
            selectedButtonTransform.SetParent(_spawnBox, false);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_answerBox as RectTransform);

        var currentRoundData = _questionData[_currentQuestionIndex];
        if (currentRoundData.OptionText == null || _checkBox == null) return;

        if (_answerBox != null && _answerBox.childCount == currentRoundData.OptionText.Length)
        {
            _checkBox.gameObject.SetActive(true);
        }
        else
        {
            _checkBox.gameObject.SetActive(false);
        }
    }

    public void CheckData()
    {
        if (_answerBox == null || _answerBox.childCount <= 0 || _coroutine != null) return;
        
        _currentAnswerIndex = 0;
        _currentAnswerText = "";

        foreach (Transform child in _answerBox)
        {
            if (child == null || child.childCount <= 0) continue;

            TextMeshProUGUI tmp = child.GetChild(0).GetComponent<TextMeshProUGUI>();
            string wordText = tmp != null ? tmp.text : "";

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
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length) yield break;
        var currentData = _questionData[_currentQuestionIndex];
        if (currentData == null) yield break;

        if (_currentAnswerText == currentData.AnswerText)
        {
            if (_checkBox != null) _checkBox.gameObject.SetActive(false);

            if (_answerBox != null)
            {
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
            }

            if (_correctClip != null && _audioSource != null)
            {
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_correctClip.length);
            }

            if (currentData.AnswerAudioClip != null && _audioSource != null)
            {
                _audioSource.clip = currentData.AnswerAudioClip;
                _audioSource.Play();
                yield return new WaitForSeconds(currentData.AnswerAudioClip.length + 0.5f);
            }

            _currentQuestionIndex++;

            if (_currentQuestionIndex < _questionData.Length)
            {
                if (_answerBox != null)
                {
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
                }

                yield return StartCoroutine(LoadCurrentQuestionLayoutRoutine());
            }
            else
            {
                if (_checkBox != null && _checkBox.TryGetComponent(out Button checkBtn)) checkBtn.interactable = false;
                _isViewed = true;
                GameManager_Junior1A.Instance?.Next(true);
            }
        }
        else
        {
            if (_incorrectClip != null && _audioSource != null)
            {
                _audioSource.clip = _incorrectClip;
                _audioSource.Play();
            }

            int checkIndex = 0;
            if (_answerBox != null && currentData.AnswerTexts != null)
            {
                foreach (Transform option in _answerBox)
                {
                    if (option == null || option.childCount <= 0) continue;

                    TextMeshProUGUI tmp = option.GetChild(0).GetComponent<TextMeshProUGUI>();
                    string textVal = tmp != null ? tmp.text : "";
                    
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
            }

            if (_incorrectClip != null) yield return new WaitForSeconds(_incorrectClip.length);
            else yield return new WaitForSeconds(1.0f);

            if (_answerBox != null)
            {
                foreach (Transform option in _answerBox)
                {
                    if (option.TryGetComponent(out Image img)) img.color = Color.white;
                }
            }
            if (_spawnBox != null)
            {
                foreach (Transform option in _spawnBox)
                {
                    if (option.TryGetComponent(out Image img)) img.color = Color.white;
                }
            }
        }

        _coroutine = null;
    }
}