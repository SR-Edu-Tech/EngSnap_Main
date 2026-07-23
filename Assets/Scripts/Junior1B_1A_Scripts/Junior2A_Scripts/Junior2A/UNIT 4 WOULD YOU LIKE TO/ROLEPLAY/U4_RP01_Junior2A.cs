using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U4_RP01_Junior2A_QuestionData
{
    public AudioClip TeacherQuestion, TomAnswer;
    public string QuestionText;
    public string[] OptionText;
    public int CorrectAnsIndex;
}
public class U4_RP01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] U4_RP01_Junior2A_QuestionData[] _questionData;
    [SerializeField] GameObject[] _optionObjs;
    [SerializeField] Color _wrongColor, _correctColor;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _correctClip, _wrongClip, _introClip;
    [SerializeField] Transform _tomObj, _teacherObj;
    [SerializeField] int _currentQuestionIndex = 0, _currentOptionIndex = 0;
    [SerializeField] bool _isViewed = false, _canSelect = false;
    Coroutine _coroutine;
    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        transform.GetChild(transform.childCount - 1).gameObject.SetActive(false);
        _tomObj.parent.gameObject.SetActive(true);
        _teacherObj.parent.gameObject.SetActive(true);
        _tomObj.gameObject.SetActive(false);
        _teacherObj.gameObject.SetActive(false);
        _currentOptionIndex = _currentQuestionIndex = 0;
        _audioSource.clip = _introClip;
        _audioSource.Play();
        foreach (GameObject obj in _optionObjs)
        {
            obj.SetActive(false);
            obj.GetComponent<Image>().color = Color.white;
        }
        yield return new WaitForSeconds(_introClip.length);
        _teacherObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].QuestionText;
        _teacherObj.gameObject.SetActive(true);
        _audioSource.clip = _questionData[_currentQuestionIndex].TeacherQuestion;
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        foreach (string option in _questionData[_currentQuestionIndex].OptionText)
        {
            _optionObjs[_currentOptionIndex].GetComponent<Button>().interactable = false;
            _optionObjs[_currentOptionIndex].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = option;
            _optionObjs[_currentOptionIndex].SetActive(true);
            _currentOptionIndex++;
            yield return new WaitForSeconds(1f);
        }
        foreach (GameObject option in _optionObjs) option.GetComponent<Button>().interactable = true;
        _canSelect = true;
        _currentOptionIndex = 0;
    }
    public void OnClickCheck(int Index)
    {
        if (!_canSelect) return;
        _optionObjs[_currentOptionIndex].GetComponent<Image>().color = Color.white;
        _currentOptionIndex = Index;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(Checker());
    }
    IEnumerator Checker()
    {
        if (_questionData[_currentQuestionIndex].CorrectAnsIndex == _currentOptionIndex)
        {
            for (int i = 0; i <= _optionObjs.Length - 1; i++) if (i != _currentOptionIndex) _optionObjs[i].SetActive(false);
            _optionObjs[_currentOptionIndex].GetComponent<Image>().color = _correctColor;
            _optionObjs[_currentOptionIndex].GetComponent<PopEffect_Junior2A>().enabled = false;
            _optionObjs[_currentOptionIndex].GetComponent<PopEffect_Junior2A>().enabled = true;
            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);
            _optionObjs[_currentOptionIndex].GetComponent<Image>().color = Color.white;
            foreach (GameObject obj in _optionObjs) obj.SetActive(false);
            _optionObjs[_currentOptionIndex].SetActive(false);
            _tomObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].OptionText[_currentOptionIndex];
            _tomObj.gameObject.SetActive(true);
            _audioSource.clip = _questionData[_currentQuestionIndex].TomAnswer;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);
            if (_currentQuestionIndex < _questionData.Length - 1)
            {
                _currentQuestionIndex++;
                _teacherObj.gameObject.SetActive(false);
                _teacherObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].QuestionText;
                _teacherObj.gameObject.SetActive(true);
                _audioSource.clip = _questionData[_currentQuestionIndex].TeacherQuestion;
                _audioSource.Play();
                _tomObj.gameObject.SetActive(false);
                yield return new WaitForSeconds(_audioSource.clip.length);
                _currentOptionIndex = 0;
                foreach (string option in _questionData[_currentQuestionIndex].OptionText)
                {
                    _optionObjs[_currentOptionIndex].GetComponent<Button>().interactable = false;
                    _optionObjs[_currentOptionIndex].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = option;
                    _optionObjs[_currentOptionIndex].SetActive(true);
                    _currentOptionIndex++;
                    yield return new WaitForSeconds(1f);
                }
                foreach (GameObject option in _optionObjs) option.GetComponent<Button>().interactable = true;
                _currentOptionIndex = 0;
            }
            else
            {
                _tomObj.parent.gameObject.SetActive(false);
                _teacherObj.parent.gameObject.SetActive(false);
                transform.GetChild(transform.childCount - 1).gameObject.SetActive(true);
                _isViewed = true;
                GameManager_Junior2A.Instance.Next(true);
            }
        }
        else
        {
            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            _optionObjs[_currentOptionIndex].GetComponent<Image>().color = _wrongColor;
            _optionObjs[_currentOptionIndex].GetComponent<WiggleEffect_Junior2A1>().enabled = true;
            yield return new WaitForSeconds(_audioSource.clip.length);
            _optionObjs[_currentOptionIndex].GetComponent<Image>().color = Color.white;
        }
    }
}
