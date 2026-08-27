using Junior2B;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U3_G01_Junior2B_QuestionData
{
    public AudioClip Question;
    public string[] OptionText;
    public int CorrectAnsIndex;
}

public class U3_G01_Junior2B : MonoBehaviour, Interfaces_Junior2B
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] Color _wrongColor, _correctColor, _wonStar, _loseStar;
    [SerializeField] U3_G01_Junior2B_QuestionData[] _questionData;
    [SerializeField] GameObject _fish, _buttonParent, _starsPanel;
    [SerializeField] Transform _selectedButton;
    [SerializeField] int _currentQuestionIndex = 0, _correctAnsCount;
    [SerializeField] bool _isViewed = false;
    [SerializeField] TextMeshProUGUI _currentQuestionIndexText;
    
    Coroutine _coroutineAudioPlayer, _coroutineNextFish;

    public bool IsViewed => _isViewed;
    
    void OnEnable() => StartCoroutine(Starter());
    
    IEnumerator Starter()
    {
        if (_audioSource != null) _audioSource.pitch = 1f;
        
        if (_buttonParent != null)
        {
            foreach (Transform button in _buttonParent.transform) button.gameObject.SetActive(false);
        }
        
        if (transform.childCount > 0) transform.GetChild(0).gameObject.SetActive(true);
        if (_fish != null) _fish.SetActive(false);
        if (_starsPanel != null) _starsPanel.SetActive(false);
        
        _currentQuestionIndex = _correctAnsCount = 0;
        _selectedButton = null;

        if (_currentQuestionIndexText != null && _questionData != null)
        {
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
        }

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        _coroutineNextFish = StartCoroutine(MoveFish());
    }
    
    IEnumerator MoveFish()
    {
        if (_questionData == null || _fish == null || _buttonParent == null)
        {
            Debug.LogError("❌ Essential fields (QuestionData, Fish, or ButtonParent) are unassigned in the Inspector!");
            yield break;
        }

        foreach (U3_G01_Junior2B_QuestionData item in _questionData)
        {
            if (item == null) continue;

            // Reset Slide component transitions safely
            if (_fish.TryGetComponent(out SlideEffect_Junior2B slide))
            {
                slide.enabled = false;
                if (_fish.TryGetComponent(out RectTransform rect))
                {
                    rect.anchoredPosition = new Vector2(0, -25);
                }
                slide._targetPosition = new Vector3(0, -25, 0);
                slide.enabled = true;
            }
            
            _fish.SetActive(true);
            int _optionIndex = 0;

            yield return new WaitForSeconds(.5f);

            // Populate choice arrays safely
            foreach (Transform button in _buttonParent.transform)
            {
                if (item.OptionText == null || _optionIndex >= item.OptionText.Length) break;
                
                if (button.TryGetComponent(out Image img)) img.color = Color.white;
                
                if (button.childCount > 0 && button.GetChild(0).TryGetComponent(out TextMeshProUGUI btnTxt))
                {
                    btnTxt.text = item.OptionText[_optionIndex++];
                    
                    if (button.GetChild(0).TryGetComponent(out TextPopEffect_Junior2B textPop))
                    {
                        textPop.enabled = false;
                        textPop.enabled = true;
                    }
                }
                
                if (button.TryGetComponent(out Popeffect_Junior2B pop)) pop.enabled = true;
                button.gameObject.SetActive(true);
            }
            
            // 💡 SAFELY HANDLE QUESTION AUDIO/NAME TEXT PIPELINES
            if (_fish.transform.childCount > 0 && _fish.transform.GetChild(0).TryGetComponent(out TextMeshProUGUI fishText))
            {
                if (item.Question != null)
                {
                    fishText.text = item.Question.name + "?";
                }
                else
                {
                    fishText.text = "Question Audio Missing?";
                    Debug.LogWarning($"⚠️ Question at index {_currentQuestionIndex} has no AudioClip set!");
                }
                fishText.gameObject.SetActive(true);
            }
            
            foreach (Transform button in _buttonParent.transform)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            }

            yield return new WaitUntil(() => _selectedButton != null);

            foreach (Transform button in _buttonParent.transform)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = false;
            }

            // Verify index boundary before accessing children dynamically
            if (item.CorrectAnsIndex < _buttonParent.transform.childCount && 
                _selectedButton == _buttonParent.transform.GetChild(item.CorrectAnsIndex))
            {
                _correctAnsCount++;
                if (_audioSource != null && _correctClip != null)
                {
                    _audioSource.clip = _correctClip;
                    _audioSource.Play();
                }
                
                if (_selectedButton.TryGetComponent(out Image img)) img.color = _correctColor;
                if (_selectedButton.TryGetComponent(out Popeffect_Junior2B pop)) pop.enabled = true;
            }
            else
            {
                if (_audioSource != null && _wrongClip != null)
                {
                    _audioSource.clip = _wrongClip;
                    _audioSource.Play();
                }
                
                if (_selectedButton.TryGetComponent(out Image img)) img.color = _wrongColor;
                if (_selectedButton.TryGetComponent(out WiggleEffect_Junior2B wiggle))
                {
                    wiggle.enabled = false;
                    wiggle.enabled = true;
                }
            }

            float waitTime = (_audioSource != null && _audioSource.clip != null) ? _audioSource.clip.length : 0.5f;
            yield return new WaitForSeconds(waitTime);

            if (_fish.TryGetComponent(out SlideEffect_Junior2B slideOut))
            {
                slideOut.enabled = false;
                if (_fish.TryGetComponent(out RectTransform rect))
                {
                    rect.anchoredPosition = new Vector2(-1600, -25);
                }
                slideOut._targetPosition = new Vector3(-1600, -25, 0);
                slideOut.enabled = true;
            }
            
            foreach (Transform button in _buttonParent.transform) button.gameObject.SetActive(false);

            yield return new WaitForSeconds(2f);

            if (_fish.transform.childCount > 0)
            {
                if (_fish.transform.GetChild(0).TryGetComponent(out TextMeshProUGUI fTxt)) fTxt.text = "";
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
        
        if (transform.childCount > 0) transform.GetChild(0).gameObject.SetActive(false);
        
        if (_starsPanel != null)
        {
            _starsPanel.SetActive(true);
            for (int i = 0; i < _starsPanel.transform.childCount - 1; i++)
            {
                Transform star = _starsPanel.transform.GetChild(i);
                if (star.TryGetComponent(out Popeffect_Junior2B pop)) pop.enabled = true;
                star.gameObject.SetActive(true);
                
                if (i < _correctAnsCount)
                {
                    if (star.TryGetComponent(out Image img)) img.color = _wonStar;
                    if (_audioSource != null && _correctClip != null)
                    {
                        _audioSource.clip = _correctClip;
                        _audioSource.Play();
                        _audioSource.pitch += .1f;
                        yield return new WaitForSeconds(_correctClip.length);
                    }
                }
                else
                {
                    if (star.TryGetComponent(out Image img)) img.color = _loseStar;
                }
            }
        }
        
        if (GameManager_Junior2B.Instance != null)
        {
            GameManager_Junior2B.Instance.Next(true);
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
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length || _audioSource == null) yield break;
        if (_questionData[_currentQuestionIndex].Question == null) yield break;

        _audioSource.clip = _questionData[_currentQuestionIndex].Question;
        _audioSource.Play();
        
        if (_fish != null && _fish.transform.childCount > 1)
        {
            if (_fish.transform.GetChild(1).TryGetComponent(out Image img)) img.enabled = true;
            yield return new WaitForSeconds(_audioSource.clip.length);
            if (_fish.transform.GetChild(1).TryGetComponent(out Image imgEnd)) imgEnd.enabled = false;
        }
    }
}