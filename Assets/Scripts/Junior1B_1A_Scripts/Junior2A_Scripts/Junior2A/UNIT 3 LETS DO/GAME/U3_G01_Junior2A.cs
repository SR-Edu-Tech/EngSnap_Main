using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
 // Added to resolve the framework attribute warning
public class U3_G01_Junior2A_QuestionData
{
    public AudioClip Question;
    public string[] OptionText;
    public int CorrectAnsIndex;
}

public class U3_G01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] Color _wrongColor, _correctColor, _wonStar, _loseStar;
    [SerializeField] U3_G01_Junior2A_QuestionData[] _questionData;
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
        _audioSource.pitch = 1;
        foreach (Transform button in _buttonParent.transform) button.gameObject.SetActive(false);
        transform.GetChild(0).gameObject.SetActive(true);
        _fish.SetActive(false);
        _starsPanel.SetActive(false);
        _audioSource.clip = _introClip;
        _audioSource.Play();
        _currentQuestionIndex = _correctAnsCount = 0;
        _selectedButton = null;
        _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";

        yield return new WaitForSeconds(_introClip.length);

        _coroutineNextFish = StartCoroutine(MoveFish());
    }

    IEnumerator MoveFish()
    {
        foreach (U3_G01_Junior2A_QuestionData item in _questionData)
        {
            _fish.GetComponent<SlideEffect_Junior2A>().enabled = false;
            _fish.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -25);
            _fish.GetComponent<SlideEffect_Junior2A>()._targetPosition = new Vector3(0, -25, 0);
            _fish.GetComponent<SlideEffect_Junior2A>().enabled = true;
            _fish.SetActive(true);
            int _optionIndex = 0;

            yield return new WaitForSeconds(.5f);

            foreach (Transform button in _buttonParent.transform)
            {
                button.GetComponent<Image>().color = Color.white;
                button.GetChild(0).GetComponent<TextMeshProUGUI>().text = item.OptionText[_optionIndex++];
                button.GetComponent<PopEffect_Junior2A>().enabled = true;
                button.gameObject.SetActive(true);
                button.GetChild(0).GetComponent<TextPopEffect_Junior2A>().enabled = false;
                button.GetChild(0).GetComponent<TextPopEffect_Junior2A>().enabled = true;
            }
            _fish.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = item.Question.name + "?";
            _fish.transform.GetChild(0).gameObject.SetActive(true);
            foreach (Transform button in _buttonParent.transform) button.GetComponent<Button>().interactable = true;

            yield return new WaitUntil(() => _selectedButton != null);

            foreach (Transform button in _buttonParent.transform) button.GetComponent<Button>().interactable = false;

            if (_selectedButton == _buttonParent.transform.GetChild(item.CorrectAnsIndex))
            {
                _correctAnsCount++;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _correctColor;
                _selectedButton.GetComponent<PopEffect_Junior2A>().enabled = true;
            }
            else
            {
                _audioSource.clip = _wrongClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _wrongColor;
                _selectedButton.GetComponent<WiggleEffect_Junior2A1>().enabled = true;
            }

            yield return new WaitForSeconds(_audioSource.clip.length);

            _fish.GetComponent<SlideEffect_Junior2A>().enabled = false;
            _fish.GetComponent<RectTransform>().anchoredPosition = new Vector2(-1600, -25);
            _fish.GetComponent<SlideEffect_Junior2A>()._targetPosition = new Vector3(-1600, -25, 0);
            _fish.GetComponent<SlideEffect_Junior2A>().enabled = true;
            foreach (Transform button in _buttonParent.transform) button.gameObject.SetActive(false);

            yield return new WaitForSeconds(2f);

            _fish.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            _fish.transform.GetChild(0).gameObject.SetActive(false);
            _selectedButton = null;
            _fish.SetActive(false);
            _currentQuestionIndex++;
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
        }

        _starsPanel.SetActive(true);

        for (int i = 0; i < _starsPanel.transform.childCount - 1; i++)
        {
            Transform star = _starsPanel.transform.GetChild(i);
            star.GetComponent<PopEffect_Junior2A>().enabled = true;
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

        transform.GetChild(0).gameObject.SetActive(false);

        if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
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
        _audioSource.clip = _questionData[_currentQuestionIndex].Question;
        _audioSource.Play();
        _fish.transform.GetChild(1).GetComponent<Image>().enabled = true;
        yield return new WaitForSeconds(_audioSource.clip.length);
        _fish.transform.GetChild(1).GetComponent<Image>().enabled = false;
    }
}