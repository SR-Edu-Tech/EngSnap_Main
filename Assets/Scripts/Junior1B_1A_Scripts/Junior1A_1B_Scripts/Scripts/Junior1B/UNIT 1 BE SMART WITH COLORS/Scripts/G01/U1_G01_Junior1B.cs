using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U1_G01_Junior1B_QuestionData
{
    public AudioClip Question;
    public string[] OptionText;
    public int CorrectAnsIndex;
}

public class U1_G01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] Color _wrongColor, _correctColor, _wonStar, _loseStar;
    [SerializeField] U1_G01_Junior1B_QuestionData[] _questionData;
    [SerializeField] GameObject _fish, _buttonParent, _starsPanel;
    [SerializeField] Transform _selectedButton;
    [SerializeField] int _currentQuestionIndex = 0, _correctAnsCount;
    [SerializeField] bool _isViewed = false;
    [SerializeField] TextMeshProUGUI _currentQuestionIndexText;
    
    private Coroutine _coroutineAudioPlayer, _coroutineNextFish;

    public bool IsViewed => _isViewed;
    
    void OnEnable() => StartCoroutine(Starter());
    
    IEnumerator Starter()
    {
        _audioSource.pitch = 1;
        
        foreach (Transform button in _buttonParent.transform) 
            button.gameObject.SetActive(false);
            
        if (transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(true);
            
        _fish.SetActive(false);
        _starsPanel.SetActive(false);
        
        _audioSource.clip = _introClip;
        _audioSource.Play();
        
        _currentQuestionIndex = _correctAnsCount = 0;
        _selectedButton = null;
        
        // Fallback Auto-Assignment Guard if left unlinked in the Unity Inspector
        if (_currentQuestionIndexText == null)
        {
            _currentQuestionIndexText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (_currentQuestionIndexText != null)
        {
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
        }

        yield return new WaitForSeconds(_introClip.length);

        _coroutineNextFish = StartCoroutine(MoveFish());
    }
    
    IEnumerator MoveFish()
    {
        foreach (U1_G01_Junior1B_QuestionData item in _questionData)
        {
            if (_fish.TryGetComponent(out SlideEffect_Junior1B baseSlide))
            {
                baseSlide.enabled = false;
            }
            
            _fish.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -25);
            
            if (baseSlide != null)
            {
                baseSlide._targetPosition = new Vector3(0, -25, 0);
                baseSlide.enabled = true;
            }
            
            _fish.SetActive(true);

            yield return new WaitForSeconds(.5f);

            int totalOptionsAvailable = item.OptionText != null ? item.OptionText.Length : 0;
            int totalChildButtons = _buttonParent.transform.childCount;

            for (int i = 0; i < totalChildButtons; i++)
            {
                Transform button = _buttonParent.transform.GetChild(i);
                
                if (i >= totalOptionsAvailable)
                {
                    button.gameObject.SetActive(false);
                    continue;
                }

                button.GetComponent<Image>().color = Color.white;
                
                if (button.childCount > 0)
                {
                    var textComponent = button.GetChild(0).GetComponent<TextMeshProUGUI>();
                    if (textComponent != null) 
                        textComponent.text = item.OptionText[i];
                        
                    if (button.GetChild(0).TryGetComponent(out TextPopEffect_Junior1B textPop))
                    {
                        textPop.enabled = false;
                        textPop.enabled = true;
                    }
                }
                
                if (button.TryGetComponent(out Popeffect_Junior1B pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
                
                button.gameObject.SetActive(true);
            }
            
            if (_fish.transform.childCount > 0)
            {
                var fishText = _fish.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (fishText != null && item.Question != null)
                {
                    fishText.text = item.Question.name + "?";
                }
                _fish.transform.GetChild(0).gameObject.SetActive(true);
            }
            
            foreach (Transform button in _buttonParent.transform) 
                button.GetComponent<Button>().interactable = true;

            yield return new WaitUntil(() => _selectedButton != null);

            foreach (Transform button in _buttonParent.transform) 
                button.GetComponent<Button>().interactable = false;

            if (item.CorrectAnsIndex < _buttonParent.transform.childCount && 
                _selectedButton == _buttonParent.transform.GetChild(item.CorrectAnsIndex))
            {
                _correctAnsCount++;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _correctColor;
                
                if (_selectedButton.TryGetComponent(out Popeffect_Junior1B clickPop))
                {
                    clickPop.enabled = false;
                    clickPop.enabled = true;
                }
            }
            else
            {
                _audioSource.clip = _wrongClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _wrongColor;
                
                if (_selectedButton.TryGetComponent(out WiggleEffect_Junior1B wiggle))
                {
                    wiggle.enabled = false;
                    wiggle.enabled = true;
                }
            }

            yield return new WaitForSeconds(_audioSource.clip.length);

            if (_fish.TryGetComponent(out SlideEffect_Junior1B exitSlide))
            {
                exitSlide.enabled = false;
            }
            
            _fish.GetComponent<RectTransform>().anchoredPosition = new Vector2(-1600, -25);
            
            if (exitSlide != null)
            {
                exitSlide._targetPosition = new Vector3(-1600, -25, 0);
                exitSlide.enabled = true;
            }
            
            foreach (Transform button in _buttonParent.transform) 
                button.gameObject.SetActive(false);

            yield return new WaitForSeconds(2f);

            if (_fish.transform.childCount > 0)
            {
                var fishText = _fish.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (fishText != null) fishText.text = "";
                _fish.transform.GetChild(0).gameObject.SetActive(false);
            }
            
            _selectedButton = null;
            _fish.SetActive(false);
            _currentQuestionIndex++;
            
            if (_currentQuestionIndexText != null)
            {
                _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
            }
        }
        
        if (transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(false);
            
        _starsPanel.SetActive(true);
        
        for (int i = 0; i < _starsPanel.transform.childCount - 1; i++)
        {
            Transform star = _starsPanel.transform.GetChild(i);
            
            if (star.TryGetComponent(out Popeffect_Junior1B starPop))
            {
                starPop.enabled = false;
                starPop.enabled = true;
            }
            
            star.gameObject.SetActive(true);
            
            if (i < _correctAnsCount)
            {
                star.GetComponent<Image>().color = _wonStar;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _audioSource.pitch += .1f;
                yield return new WaitForSeconds(_correctClip.length);
            }
            else 
            {
                star.GetComponent<Image>().color = _loseStar;
            }
        }
        
        if (GameManager_Junior1B.Instance != null)
        {
            GameManager_Junior1B.Instance.Next(true);
        }
        _isViewed = true;
    }
    
    public void SelectedObject(Transform button) => _selectedButton = button;
    
    public void PlayAudio()
    {
        if (_coroutineAudioPlayer != null) StopCoroutine(_coroutineAudioPlayer);
        _coroutineAudioPlayer = StartCoroutine(PlayCurrentAudio());
    }
    
    IEnumerator PlayCurrentAudio()
    {
        if (_currentQuestionIndex < _questionData.Length && _questionData[_currentQuestionIndex].Question != null)
        {
            _audioSource.clip = _questionData[_currentQuestionIndex].Question;
            _audioSource.Play();
            
            if (_fish.transform.childCount > 1)
            {
                var speechBubble = _fish.transform.GetChild(1).GetComponent<Image>();
                if (speechBubble != null) speechBubble.enabled = true;
                
                yield return new WaitForSeconds(_audioSource.clip.length);
                
                if (speechBubble != null) speechBubble.enabled = false;
            }
        }
    }
}