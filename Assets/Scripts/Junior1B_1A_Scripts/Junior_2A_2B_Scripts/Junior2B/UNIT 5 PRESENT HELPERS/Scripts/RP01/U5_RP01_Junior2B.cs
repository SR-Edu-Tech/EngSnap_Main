using Junior2B;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U5_RP01_Junior2B_QuestionData
{
    public AudioClip TeacherQuestion, TomAnswer;
    public string QuestionText;
    public string[] OptionText;
    public int CorrectAnsIndex;
    public Sprite QuestionImage; 
}

public class U5_RP01_Junior2B : MonoBehaviour, Interfaces_Junior2B
{
    [Header("=== Quiz Configuration ===")]
    [SerializeField] U5_RP01_Junior2B_QuestionData[] _questionData;
    [SerializeField] Color _wrongColor, _correctColor;
    
    [Header("=== Automation Target ===")]
    [Tooltip("Drag your 'button parent' GameObject here!")]
    [SerializeField] private Transform _buttonParentContainer;

    [Header("=== End Screen Configuration ===")]
    [Tooltip("Drag your completed container panel here!")]
    [SerializeField] private GameObject _completedContainer;

    [Header("=== Audio Setup ===")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _correctClip, _wrongClip, _introClip;
    
    [Header("=== Speech Bubble Setups ===")]
    [SerializeField] Transform _reenaObj, _ashokObj;
    [SerializeField] private Image _displayImageComponent; 

    [Header("=== State Trackers ===")]
    [SerializeField] int _currentQuestionIndex = 0;
    [SerializeField] int _currentOptionIndex = 0;
    [SerializeField] bool _isViewed = false;
    [SerializeField] bool _canSelect = false;
    
    private List<GameObject> _dynamicOptionButtons = new List<GameObject>();
    private Coroutine _checkerCoroutine;
    private Coroutine _revealCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        GatherButtonsFromParent();
        StartCoroutine(Starter());
    }

    private void GatherButtonsFromParent()
    {
        _dynamicOptionButtons.Clear();

        if (_buttonParentContainer == null)
        {
            Debug.LogError("<color=red>[Quiz Error]</color> You MUST drag your 'button parent' GameObject into the '_buttonParentContainer' slot!");
            return;
        }

        for (int i = 0; i < _buttonParentContainer.childCount; i++)
        {
            GameObject child = _buttonParentContainer.GetChild(i).gameObject;
            _dynamicOptionButtons.Add(child);

            if (child.TryGetComponent(out Button btn))
            {
                int indexBackup = i; 
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnClickCheck(indexBackup));
            }
        }
    }

    IEnumerator Starter()
    {
        // Explicitly hide the completion container on initial startup sequence
        if (_completedContainer != null)
        {
            _completedContainer.SetActive(false);
        }
        else if (transform.childCount > 0)
        {
            // Fallback safety to keep your structural architecture clean
            transform.GetChild(transform.childCount - 1).gameObject.SetActive(false);
        }

        if (_reenaObj != null && _reenaObj.parent != null) _reenaObj.parent.gameObject.SetActive(true);
        if (_ashokObj != null && _ashokObj.parent != null) _ashokObj.parent.gameObject.SetActive(true);
        
        if (_reenaObj != null) _reenaObj.gameObject.SetActive(false);
        if (_ashokObj != null) _ashokObj.gameObject.SetActive(false);
        
        _currentOptionIndex = _currentQuestionIndex = 0;
        _canSelect = false;

        HideQuestionImage();

        foreach (GameObject btnObj in _dynamicOptionButtons)
        {
            if (btnObj != null)
            {
                if (btnObj.TryGetComponent(out Image img)) img.color = Color.white;
                btnObj.SetActive(false);
            }
        }

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length + 0.1f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        LoadQuestionContent();
    }

    private void LoadQuestionContent()
    {
        if (_questionData == null || _questionData.Length == 0 || _currentQuestionIndex >= _questionData.Length) return;

        ShowQuestionImageForCurrentIndex();

        if (_reenaObj != null) _reenaObj.gameObject.SetActive(false);

        if (_ashokObj != null)
        {
            _ashokObj.gameObject.SetActive(true);
            var textComponent = _ashokObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null) textComponent.text = _questionData[_currentQuestionIndex].QuestionText;
        }

        if (_audioSource != null && _questionData[_currentQuestionIndex].TeacherQuestion != null)
        {
            _audioSource.clip = _questionData[_currentQuestionIndex].TeacherQuestion;
            _audioSource.Play();
        }

        if (_revealCoroutine != null) StopCoroutine(_revealCoroutine);
        _revealCoroutine = StartCoroutine(RevealOptionsRoutine());
    }

    private void ShowQuestionImageForCurrentIndex()
    {
        if (_displayImageComponent == null) return;

        if (_questionData != null && _currentQuestionIndex < _questionData.Length && _questionData[_currentQuestionIndex].QuestionImage != null)
        {
            _displayImageComponent.sprite = _questionData[_currentQuestionIndex].QuestionImage;
            _displayImageComponent.gameObject.SetActive(true);
        }
        else
        {
            HideQuestionImage();
        }
    }

    private void HideQuestionImage()
    {
        if (_displayImageComponent == null) return;
        _displayImageComponent.gameObject.SetActive(false);
        _displayImageComponent.sprite = null;
    }

    IEnumerator RevealOptionsRoutine()
    {
        _canSelect = false;
        
        float waitTime = (_audioSource != null && _audioSource.clip != null) ? _audioSource.clip.length : 0.75f;
        yield return new WaitForSeconds(waitTime);

        if (_questionData[_currentQuestionIndex].OptionText != null)
        {
            foreach (GameObject obj in _dynamicOptionButtons) 
            {
                if (obj != null) 
                {
                    obj.SetActive(false);
                    if (obj.TryGetComponent(out Image img)) img.color = Color.white;
                }
            }

            string[] currentOptions = _questionData[_currentQuestionIndex].OptionText;
            int layoutLimit = Mathf.Min(currentOptions.Length, _dynamicOptionButtons.Count);

            for (int i = 0; i < layoutLimit; i++)
            {
                GameObject optionObj = _dynamicOptionButtons[i];
                if (optionObj == null) continue;

                if (optionObj.TryGetComponent(out Button btn)) btn.interactable = false;

                var textComp = optionObj.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null) textComp.text = currentOptions[i];

                optionObj.SetActive(true);
                yield return new WaitForSeconds(0.2f); 
            }

            for (int i = 0; i < layoutLimit; i++)
            {
                if (_dynamicOptionButtons[i] != null && _dynamicOptionButtons[i].TryGetComponent(out Button btn)) 
                    btn.interactable = true;
            }
        }

        _canSelect = true;
    }

    public void OnClickCheck(int Index)
    {
        if (!_canSelect) return;

        _currentOptionIndex = Index;
        if (_checkerCoroutine != null) StopCoroutine(_checkerCoroutine);
        _checkerCoroutine = StartCoroutine(Checker());
    }

    IEnumerator Checker()
    {
        _canSelect = false;

        if (_questionData[_currentQuestionIndex].CorrectAnsIndex == _currentOptionIndex)
        {
            for (int i = 0; i < _dynamicOptionButtons.Count; i++) 
            {
                if (i != _currentOptionIndex && _dynamicOptionButtons[i] != null) 
                    _dynamicOptionButtons[i].SetActive(false);
            }
            
            if (_dynamicOptionButtons[_currentOptionIndex] != null && _dynamicOptionButtons[_currentOptionIndex].TryGetComponent(out Image img)) 
                img.color = _correctColor;
            
            if (_dynamicOptionButtons[_currentOptionIndex] != null && _dynamicOptionButtons[_currentOptionIndex].TryGetComponent(out Popeffect_Junior2B pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }
            
            if (_audioSource != null && _correctClip != null)
            {
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_correctClip.length + 0.1f);
            }

            if (_dynamicOptionButtons[_currentOptionIndex] != null && _dynamicOptionButtons[_currentOptionIndex].TryGetComponent(out Image resetImg)) 
                resetImg.color = Color.white;
            
            foreach (GameObject obj in _dynamicOptionButtons) if (obj != null) obj.SetActive(false);
            
            if (_reenaObj != null)
            {
                var responseText = _reenaObj.GetComponentInChildren<TextMeshProUGUI>();
                if (responseText != null) responseText.text = _questionData[_currentQuestionIndex].OptionText[_currentOptionIndex];
                _reenaObj.gameObject.SetActive(true);
            }
            
            if (_audioSource != null && _questionData[_currentQuestionIndex].TomAnswer != null)
            {
                _audioSource.clip = _questionData[_currentQuestionIndex].TomAnswer;
                _audioSource.Play();
                yield return new WaitForSeconds(_audioSource.clip.length + 0.1f);
            }

            HideQuestionImage();

            if (_currentQuestionIndex < _questionData.Length - 1)
            {
                _currentQuestionIndex++;
                if (_ashokObj != null) _ashokObj.gameObject.SetActive(false);
                if (_reenaObj != null) _reenaObj.gameObject.SetActive(false);
                
                yield return new WaitForSeconds(0.4f);
                LoadQuestionContent();
            }
            else
            {
                // Clean up and disable everything completely
                if (_ashokObj != null) _ashokObj.gameObject.SetActive(false);
                if (_reenaObj != null) _reenaObj.gameObject.SetActive(false);
                
                if (_reenaObj != null && _reenaObj.parent != null) _reenaObj.parent.gameObject.SetActive(false);
                if (_ashokObj != null && _ashokObj.parent != null) _ashokObj.parent.gameObject.SetActive(false);
                
                if (_buttonParentContainer != null) _buttonParentContainer.gameObject.SetActive(false);

                // Enable the specific completed container
                if (_completedContainer != null)
                {
                    _completedContainer.SetActive(true);
                }
                else if (transform.childCount > 0)
                {
                    transform.GetChild(transform.childCount - 1).gameObject.SetActive(true);
                }
                
                _isViewed = true;
                if (GameManager_Junior2B.Instance != null) GameManager_Junior2B.Instance.Next(true);
            }
        }
        else
        {
            if (_audioSource != null && _wrongClip != null)
            {
                _audioSource.clip = _wrongClip;
                _audioSource.Play();
            }
            
            if (_dynamicOptionButtons[_currentOptionIndex] != null && _dynamicOptionButtons[_currentOptionIndex].TryGetComponent(out Image img)) 
                img.color = _wrongColor;
                
            if (_dynamicOptionButtons[_currentOptionIndex] != null && _dynamicOptionButtons[_currentOptionIndex].TryGetComponent(out WiggleEffect_Junior2B wiggle))
            {
                wiggle.enabled = false;
                wiggle.enabled = true;
            }
            
            float wrongWait = (_wrongClip != null) ? _wrongClip.length : 0.5f;
            yield return new WaitForSeconds(wrongWait);
            
            if (_dynamicOptionButtons[_currentOptionIndex] != null)
            {
                if (_dynamicOptionButtons[_currentOptionIndex].TryGetComponent(out Image resetImg)) resetImg.color = Color.white;
                _dynamicOptionButtons[_currentOptionIndex].SetActive(false);
            }

            _canSelect = true; 
        }
    }
}