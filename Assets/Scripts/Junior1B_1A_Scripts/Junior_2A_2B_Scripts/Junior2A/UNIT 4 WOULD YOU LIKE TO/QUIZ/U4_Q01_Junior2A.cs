using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U4_Q01_Junior2A_QuestionData
{
    public AudioClip SamQuestion;
    public string HeadingText, QuestionText;
    public string[] OptionText;
    public int CorrectAnsIndex;
}

public class U4_Q01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _wrongClip, _correctClip;
    [SerializeField] U4_Q01_Junior2A_QuestionData[] _questions;
    [SerializeField] GameObject _headingObj, _buttonParent, _object, _completed, _mainHeading;
    [SerializeField] Color _wrongColor, _correctColor, _wonStar, _loseStar;
    [SerializeField] int _currentQuestionIndex, _currentOptionIndex, _starWon;
    [SerializeField] bool _isViewed = false;
    Coroutine _audioCoroutine, _checkCoroutine;

    void OnEnable() => StartCoroutine(StartQuiz());

    public bool IsViewed => _isViewed;

    IEnumerator StartQuiz()
    {
        _currentQuestionIndex = _currentOptionIndex = _starWon = 0;
        _audioSource.pitch = 1;
        _audioSource.clip = _introClip;
        _audioSource.Play();
        _checkCoroutine = null;
        _completed.SetActive(false);
        _object.SetActive(false);
        _mainHeading.SetActive(true);

        foreach (Transform obj in _buttonParent.transform) obj.GetComponent<PopEffect_Junior2A>().enabled = obj.GetComponent<Button>().enabled = true;
        for (int i = 0; i < _questions.Length; i++) _completed.transform.GetChild(i).gameObject.SetActive(false);

        foreach (Transform obj in _buttonParent.transform)
        {
            obj.GetComponent<Image>().color = Color.white;
            obj.GetComponent<Button>().enabled = true;
            obj.GetComponent<PopEffect_Junior2A>().enabled = true;
        }

        yield return new WaitForSeconds(_introClip.length);
        _currentOptionIndex = 0;

        foreach (Transform obj in _buttonParent.transform)
        {
            obj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questions[_currentQuestionIndex].OptionText[_currentOptionIndex];
            _currentOptionIndex++;
        }

        _mainHeading.SetActive(false);
        _headingObj.GetComponent<TextMeshProUGUI>().text = _questions[0].HeadingText;
        _object.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questions[0].QuestionText;
        _headingObj.SetActive(false);
        _headingObj.SetActive(true);
        _object.SetActive(true);
    }

    public void PlayAudio()
    {
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(StartAudio());
    }

    public void SetIndex(int Index) => _currentOptionIndex = Index;

    public void CheckQuestion()
    {
        if (_checkCoroutine == null) _checkCoroutine = StartCoroutine(CheckAnswer(_currentOptionIndex));
        foreach (Transform obj in _buttonParent.transform) obj.GetComponent<Button>().enabled = false;
    }

    IEnumerator CheckAnswer(int index)
    {
        if (index == _questions[_currentQuestionIndex].CorrectAnsIndex)
        {
            _audioSource.Stop();
            _starWon++;
            _buttonParent.transform.GetChild(index).GetComponent<Image>().color = _correctColor;
            _buttonParent.transform.GetChild(index).GetComponent<PopEffect_Junior2A>().enabled = false;
            _buttonParent.transform.GetChild(index).GetComponent<PopEffect_Junior2A>().enabled = true;
            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);
            _buttonParent.transform.GetChild(index).GetComponent<Image>().color = Color.white;
        }
        else
        {
            _buttonParent.transform.GetChild(index).GetComponent<Image>().color = _wrongColor;
            _buttonParent.transform.GetChild(index).GetComponent<WiggleEffect_Junior2A>().enabled = true;
            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);
            _buttonParent.transform.GetChild(index).GetComponent<Image>().color = Color.white;
        }

        if (_currentQuestionIndex < _questions.Length - 1)
        {
            _object.SetActive(false);
            _currentQuestionIndex++;
            _currentOptionIndex = 0;

            foreach (Transform obj in _buttonParent.transform)
            {
                obj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questions[_currentQuestionIndex].OptionText[_currentOptionIndex];
                obj.GetComponent<PopEffect_Junior2A>().enabled = true;
                _currentOptionIndex++;
            }

            _headingObj.GetComponent<TextMeshProUGUI>().text = _questions[_currentQuestionIndex].HeadingText;
            _object.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questions[_currentQuestionIndex].QuestionText;
            _headingObj.SetActive(false);
            _headingObj.SetActive(true);
            _object.SetActive(true);

            foreach (Transform obj in _buttonParent.transform) obj.GetComponent<Button>().enabled = true;
        }
        else
        {
            _object.SetActive(false);
            _completed.SetActive(true);
            for (int i = 0; i < _questions.Length; i++)
            {
                if (i < _starWon)
                {
                    _completed.transform.GetChild(i).GetComponent<Image>().color = _wonStar;
                    _completed.transform.GetChild(i).GetComponent<PopEffect_Junior2A>().enabled = true;
                    _audioSource.clip = _correctClip;
                    _audioSource.Play();
                    _audioSource.pitch += .1f;
                    _completed.transform.GetChild(i).gameObject.SetActive(true);
                    yield return new WaitForSeconds(_correctClip.length);
                }
                else
                {
                    _completed.transform.GetChild(i).GetComponent<Image>().color = _loseStar;
                    _completed.transform.GetChild(i).GetComponent<PopEffect_Junior2A>().enabled = true;
                    _completed.transform.GetChild(i).gameObject.SetActive(true);
                }
            }
            _isViewed = true;
            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
        }
        _checkCoroutine = null;
    }

    IEnumerator StartAudio()
    {
        _audioSource.clip = _questions[_currentQuestionIndex].SamQuestion;
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
    }

    public void SetColorRight(Image img) => img.color = _correctColor;
    public void SetColorWrong(Image img) => img.color = _wrongColor;
}