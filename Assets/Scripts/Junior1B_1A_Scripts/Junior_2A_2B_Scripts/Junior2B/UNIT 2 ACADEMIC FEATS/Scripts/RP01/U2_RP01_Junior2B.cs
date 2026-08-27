using Junior2B;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U2_RP01_Junior2B_QuestionData
{
    public AudioClip TeacherQuestion, TomAnswer;
    public string QuestionText;
    public string[] OptionText;
    public int CorrectAnsIndex;
    public Sprite QuestionImage; 
}

public class U2_RP01_Junior2B : MonoBehaviour, Interfaces_Junior2B
{
    [SerializeField] U2_RP01_Junior2B_QuestionData[] _questionData;
    [SerializeField] GameObject[] _optionObjs;
    [SerializeField] Color _wrongColor, _correctColor;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _correctClip, _wrongClip, _introClip;
    [SerializeField] Transform _reenaObj, _ashokObj;
    [SerializeField] private Image _displayImageComponent; 

    [SerializeField] int _currentQuestionIndex = 0, _currentOptionIndex = 0;
    [SerializeField] bool _isViewed = false, _canSelect = false;
    Coroutine _coroutine;
    
    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        transform.GetChild(transform.childCount - 1).gameObject.SetActive(false);
        _reenaObj.parent.gameObject.SetActive(true);
        _ashokObj.parent.gameObject.SetActive(true);
        _reenaObj.gameObject.SetActive(false);
        _ashokObj.gameObject.SetActive(false);
        _currentOptionIndex = _currentQuestionIndex = 0;
        
        // Ensure image starts completely hidden during intro
        HideQuestionImage();

        _audioSource.clip = _introClip;
        _audioSource.Play();
        
        foreach (GameObject obj in _optionObjs)
        {
            obj.SetActive(false);
            if (obj.TryGetComponent(out Image img)) img.color = Color.white;
        }
        
        yield return new WaitForSeconds(_introClip.length);
        
        LoadQuestionContent();
    }

    private void LoadQuestionContent()
    {
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length) return;

        // 💡 UPDATE: Show the image ONLY right now, exactly when the question comes up!
        ShowQuestionImageForCurrentIndex();

        _ashokObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].QuestionText;
        _ashokObj.gameObject.SetActive(true);
        
        _audioSource.clip = _questionData[_currentQuestionIndex].TeacherQuestion;
        _audioSource.Play();
        
        StartCoroutine(RevealOptionsRoutine());
    }

    // 💡 NEW METHOD: Explicitly turns on and updates the image ONLY when called
    private void ShowQuestionImageForCurrentIndex()
    {
        if (_displayImageComponent == null) return;

        if (_questionData != null && 
            _currentQuestionIndex < _questionData.Length && 
            _questionData[_currentQuestionIndex].QuestionImage != null)
        {
            _displayImageComponent.sprite = _questionData[_currentQuestionIndex].QuestionImage;
            _displayImageComponent.gameObject.SetActive(true);
        }
        else
        {
            HideQuestionImage();
        }
    }

    // 💡 NEW METHOD: Explicitly turns off the image container completely
    private void HideQuestionImage()
    {
        if (_displayImageComponent == null) return;
        _displayImageComponent.gameObject.SetActive(false);
        _displayImageComponent.sprite = null;
    }

    IEnumerator RevealOptionsRoutine()
    {
        _canSelect = false;
        _currentOptionIndex = 0;
        yield return new WaitForSeconds(_audioSource.clip != null ? _audioSource.clip.length : 0.5f);

        foreach (string option in _questionData[_currentQuestionIndex].OptionText)
        {
            if (_currentOptionIndex >= _optionObjs.Length) break;

            GameObject optionObj = _optionObjs[_currentOptionIndex];
            if (optionObj.TryGetComponent(out Button btn)) btn.interactable = false;
            
            optionObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = option;
            optionObj.SetActive(true);
            _currentOptionIndex++;
            yield return new WaitForSeconds(1f);
        }

        foreach (GameObject option in _optionObjs)
        {
            if (option.TryGetComponent(out Button btn)) btn.interactable = true;
        }
        _canSelect = true;
        _currentOptionIndex = 0;
    }

    public void OnClickCheck(int Index)
    {
        if (!_canSelect) return;
        
        if (_currentOptionIndex < _optionObjs.Length && _optionObjs[_currentOptionIndex].TryGetComponent(out Image img))
        {
            img.color = Color.white;
        }

        _currentOptionIndex = Index;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(Checker());
    }

    IEnumerator Checker()
    {
        if (_questionData[_currentQuestionIndex].CorrectAnsIndex == _currentOptionIndex)
        {
            _canSelect = false; 
            
            for (int i = 0; i <= _optionObjs.Length - 1; i++) 
            {
                if (i != _currentOptionIndex) _optionObjs[i].SetActive(false);
            }
            
            if (_optionObjs[_currentOptionIndex].TryGetComponent(out Image img)) img.color = _correctColor;
            
            if (_optionObjs[_currentOptionIndex].TryGetComponent(out Popeffect_Junior2B pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }
            
            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);
            
            if (_optionObjs[_currentOptionIndex].TryGetComponent(out Image resetImg)) resetImg.color = Color.white;
            foreach (GameObject obj in _optionObjs) obj.SetActive(false);
            
            _reenaObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].OptionText[_currentOptionIndex];
            _reenaObj.gameObject.SetActive(true);
            
            _audioSource.clip = _questionData[_currentQuestionIndex].TomAnswer;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);
            
            // 💡 CRITICAL CHANGE: The moment the question finishes and turns off, hide the image instantly!
            HideQuestionImage();

            if (_currentQuestionIndex < _questionData.Length - 1)
            {
                _currentQuestionIndex++;
                _ashokObj.gameObject.SetActive(false);
                _reenaObj.gameObject.SetActive(false);
                
                // Wait an extra brief split second while characters clear out so the image doesn't jump cut too early
                yield return new WaitForSeconds(0.2f);
                
                LoadQuestionContent();
            }
            else
            {
                _reenaObj.parent.gameObject.SetActive(false);
                _ashokObj.parent.gameObject.SetActive(false);
                
                transform.GetChild(transform.childCount - 1).gameObject.SetActive(true);
                _isViewed = true;
                
                if (GameManager_Junior2B.Instance != null)
                {
                    GameManager_Junior2B.Instance.Next(true);
                }
            }
        }
        else
        {
            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            
            if (_optionObjs[_currentOptionIndex].TryGetComponent(out Image img)) img.color = _wrongColor;
            if (_optionObjs[_currentOptionIndex].TryGetComponent(out WiggleEffect_Junior2B wiggle))
            {
                wiggle.enabled = false;
                wiggle.enabled = true;
            }
            
            yield return new WaitForSeconds(_audioSource.clip.length);
            if (_optionObjs[_currentOptionIndex].TryGetComponent(out Image resetImg)) resetImg.color = Color.white;
        }
    }
}