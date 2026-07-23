using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U7_G01_Junior2A_QuestionData
{
    public AudioClip Question;
    public string[] OptionText;
    public int CorrectAnsIndex;
}

public class U7_G01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] Color _wrongColor, _correctColor, _wonStar, _loseStar;
    [SerializeField] U7_G01_Junior2A_QuestionData[] _questionData;
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

        if (_buttonParent != null)
        {
            foreach (Transform button in _buttonParent.transform)
                button.gameObject.SetActive(false);
        }

        if (transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(true);

        if (_fish != null) _fish.SetActive(false);
        if (_starsPanel != null) _starsPanel.SetActive(false);

        _audioSource.clip = _introClip;
        _audioSource.Play();

        _currentQuestionIndex = _correctAnsCount = 0;
        _selectedButton = null;

        if (_currentQuestionIndexText != null && _questionData != null)
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";

        yield return new WaitForSeconds(_introClip.length);

        _coroutineNextFish = StartCoroutine(MoveFish());
    }

    IEnumerator MoveFish()
    {
        if (_questionData == null || _questionData.Length == 0) yield break;

        foreach (U7_G01_Junior2A_QuestionData item in _questionData)
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

                // Safety check: Don't exceed option array boundaries
                if (item.OptionText != null && _optionIndex < item.OptionText.Length)
                {
                    button.GetChild(0).GetComponent<TextMeshProUGUI>().text = item.OptionText[_optionIndex++];
                }

                // Safety check: Prevent NullReference if PopEffect is missing
                if (button.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;

                button.gameObject.SetActive(true);

                if (button.GetChild(0).TryGetComponent(out TextPopEffect_Junior2A textPop))
                {
                    textPop.enabled = false;
                    textPop.enabled = true;
                }
            }

            // Safety check: Make sure Question clip exists before pulling its name
            string questionText = (item.Question != null) ? item.Question.name : "Question";
            _fish.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = questionText + "?";
            _fish.transform.GetChild(0).gameObject.SetActive(true);

            foreach (Transform button in _buttonParent.transform)
                button.GetComponent<Button>().interactable = true;

            yield return new WaitUntil(() => _selectedButton != null);

            foreach (Transform button in _buttonParent.transform)
                button.GetComponent<Button>().interactable = false;

            if (_selectedButton == _buttonParent.transform.GetChild(item.CorrectAnsIndex))
            {
                _correctAnsCount++;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _correctColor;
                if (_selectedButton.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
            }
            else
            {
                _audioSource.clip = _wrongClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _wrongColor;
                if (_selectedButton.TryGetComponent(out WiggleEffect_Junior2A1 wiggle)) wiggle.enabled = true;
            }

            yield return new WaitForSeconds(_audioSource.clip.length);

            _fish.GetComponent<SlideEffect_Junior2A>().enabled = false;
            _fish.GetComponent<RectTransform>().anchoredPosition = new Vector2(-1600, -25);
            _fish.GetComponent<SlideEffect_Junior2A>()._targetPosition = new Vector3(-1600, -25, 0);
            _fish.GetComponent<SlideEffect_Junior2A>().enabled = true;

            foreach (Transform button in _buttonParent.transform)
                button.gameObject.SetActive(false);

            yield return new WaitForSeconds(2f);

            _fish.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            _fish.transform.GetChild(0).gameObject.SetActive(false);
            _selectedButton = null;
            _fish.SetActive(false);
            _currentQuestionIndex++;
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
        }

        if (transform.childCount > 0)
            transform.GetChild(0).gameObject.SetActive(false);

        _starsPanel.SetActive(true);
        for (int i = 0; i < _starsPanel.transform.childCount - 1; i++)
        {
            var child = _starsPanel.transform.GetChild(i);
            if (child.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
            child.gameObject.SetActive(true);

            if (i < _correctAnsCount)
            {
                child.GetComponent<Image>().color = _wonStar;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _audioSource.pitch += .1f;
                yield return new WaitForSeconds(_correctClip.length);
            }
            else
            {
                child.GetComponent<Image>().color = _loseStar;
            }
        }

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
        if (_currentQuestionIndex < _questionData.Length && _questionData[_currentQuestionIndex].Question != null)
        {
            _audioSource.clip = _questionData[_currentQuestionIndex].Question;
            _audioSource.Play();
            _fish.transform.GetChild(1).GetComponent<Image>().enabled = true;
            yield return new WaitForSeconds(_audioSource.clip.length);
            _fish.transform.GetChild(1).GetComponent<Image>().enabled = false;
        }
    }
}