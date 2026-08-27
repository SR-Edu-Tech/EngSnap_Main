using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U11_G01_Junior1B_QuestionData
{
    public AudioClip Question;
    public string[] OptionText;
    public int CorrectAnsIndex;
}

public class U11_G01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] Color _wrongColor, _correctColor, _wonStar, _loseStar;
    [SerializeField] U11_G01_Junior1B_QuestionData[] _questionData;
    [SerializeField] GameObject _fish, _buttonParent, _starsPanel;

    [Header("End Screen Text Configuration")]
    [Tooltip("Drag the game over text or completion banner object here.")]
    [SerializeField] private GameObject _completionTextObject;

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

        if (_completionTextObject != null) _completionTextObject.SetActive(false);

        _audioSource.clip = _introClip;
        _audioSource.Play();
        _currentQuestionIndex = _correctAnsCount = 0;
        _selectedButton = null;

        if (_currentQuestionIndexText != null)
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";

        yield return new WaitForSeconds(_introClip.length);

        _coroutineNextFish = StartCoroutine(MoveFish());
    }

    IEnumerator MoveFish()
    {
        foreach (U11_G01_Junior1B_QuestionData item in _questionData)
        {
            _fish.GetComponent<SlideEffect_Junior1B>().enabled = false;
            _fish.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -25);
            _fish.GetComponent<SlideEffect_Junior1B>()._targetPosition = new Vector3(0, -25, 0);
            _fish.GetComponent<SlideEffect_Junior1B>().enabled = true;
            _fish.SetActive(true);

            int _optionIndex = 0;

            yield return new WaitForSeconds(.5f);

            foreach (Transform button in _buttonParent.transform)
            {
                button.GetComponent<Image>().color = Color.white;

                // FIXED: Safety check ensures your hierarchy button count doesn't exceed array limits
                if (item.OptionText != null && _optionIndex < item.OptionText.Length)
                {
                    button.GetChild(0).GetComponent<TextMeshProUGUI>().text = item.OptionText[_optionIndex++];
                }
                else
                {
                    button.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
                }

                button.GetComponent<Popeffect_Junior1B>().enabled = true;
                button.gameObject.SetActive(true);
                button.GetChild(0).GetComponent<TextPopEffect_Junior1B>().enabled = false;
                button.GetChild(0).GetComponent<TextPopEffect_Junior1B>().enabled = true;
            }

            if (item.Question != null)
            {
                _fish.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = item.Question.name + "?";
            }
            _fish.transform.GetChild(0).gameObject.SetActive(true);
            foreach (Transform button in _buttonParent.transform) button.GetComponent<Button>().interactable = true;

            yield return new WaitUntil(() => _selectedButton != null);

            foreach (Transform button in _buttonParent.transform) button.GetComponent<Button>().interactable = false;

            if (item.CorrectAnsIndex < _buttonParent.transform.childCount && _selectedButton == _buttonParent.transform.GetChild(item.CorrectAnsIndex))
            {
                _correctAnsCount++;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _correctColor;
                _selectedButton.GetComponent<Popeffect_Junior1B>().enabled = true;
            }
            else
            {
                _audioSource.clip = _wrongClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _wrongColor;
                _selectedButton.GetComponent<WiggleEffect_Junior1B>().enabled = true;
            }

            yield return new WaitForSeconds(_audioSource.clip.length);

            _fish.GetComponent<SlideEffect_Junior1B>().enabled = false;
            _fish.GetComponent<RectTransform>().anchoredPosition = new Vector2(-1600, -25);
            _fish.GetComponent<SlideEffect_Junior1B>()._targetPosition = new Vector3(-1600, -25, 0);
            _fish.GetComponent<SlideEffect_Junior1B>().enabled = true;
            foreach (Transform button in _buttonParent.transform) button.gameObject.SetActive(false);

            yield return new WaitForSeconds(2f);

            _fish.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";
            _fish.transform.GetChild(0).gameObject.SetActive(false);
            _selectedButton = null;
            _fish.SetActive(false);
            _currentQuestionIndex++;

            if (_currentQuestionIndexText != null)
                _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
        }

        // --- END SCREEN TRANSITION SEQUENCING ---
        transform.GetChild(0).gameObject.SetActive(false);
        _starsPanel.SetActive(true);

        for (int i = 0; i < _starsPanel.transform.childCount - 1; i++)
        {
            _starsPanel.transform.GetChild(i).GetComponent<Popeffect_Junior1B>().enabled = true;
            _starsPanel.transform.GetChild(i).gameObject.SetActive(true);
            if (i < _correctAnsCount)
            {
                _starsPanel.transform.GetChild(i).GetComponent<Image>().color = _wonStar;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _audioSource.pitch += .1f;
                yield return new WaitForSeconds(_correctClip.length);
            }
            else
            {
                _starsPanel.transform.GetChild(i).GetComponent<Image>().color = _loseStar;
            }
        }

        if (_completionTextObject != null)
        {
            _completionTextObject.SetActive(true);

            if (_completionTextObject.TryGetComponent(out Popeffect_Junior1B textPop))
            {
                textPop.enabled = false;
                textPop.enabled = true;
            }

            yield return new WaitForSeconds(1.5f);
        }

        GameManager_Junior1B.Instance.Next(true);
        _isViewed = true;
    }

    public void SelectedObject(Transform button) => _selectedButton = button;

    public void PlayAudio()
    {
        // FIXED: Safety guard condition prevents processing if out-of-bounds index parameters occur
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length || _currentQuestionIndex < 0) return;

        if (_coroutineAudioPlayer != null) StopCoroutine(_coroutineAudioPlayer);
        _coroutineAudioPlayer = StartCoroutine(PlayCurrentAudio());
    }

    IEnumerator PlayCurrentAudio()
    {
        if (_questionData[_currentQuestionIndex].Question != null)
        {
            _audioSource.clip = _questionData[_currentQuestionIndex].Question;
            _audioSource.Play();

            if (_fish.transform.childCount > 1)
                _fish.transform.GetChild(1).GetComponent<Image>().enabled = true;

            yield return new WaitForSeconds(_audioSource.clip.length);

            if (_fish.transform.childCount > 1)
                _fish.transform.GetChild(1).GetComponent<Image>().enabled = false;
        }
    }
}