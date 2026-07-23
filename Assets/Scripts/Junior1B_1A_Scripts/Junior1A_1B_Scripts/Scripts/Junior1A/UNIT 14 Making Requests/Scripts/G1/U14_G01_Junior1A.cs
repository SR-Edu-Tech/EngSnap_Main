using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U14_G01_Junior1A_QuestionData
{
    public AudioClip Question;
    public string[] OptionText;
    public int CorrectAnsIndex;
}

public class U14_G01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] Color _wrongColor, _correctColor, _wonStar, _loseStar;
    [SerializeField] U14_G01_Junior1A_QuestionData[] _questionData;
    [SerializeField] GameObject _fish, _buttonParent, _starsPanel;

    // ── Question Box Layout ────────────────────
    [Header("Question Layout UI")]
    [Tooltip("The parent image/box GameObject containing the question text.")]
    [SerializeField] private GameObject _questionTextBox; // 🔧 Added as requested

    [Tooltip("Drag the TMP text object that sits inside the question text box.")]
    [SerializeField] private TextMeshProUGUI _questionText;

    [Header("End Screen")]
    [SerializeField] private GameObject _completionTextObject;

    [Tooltip("The image on the fish that lights up while audio plays (child of fish).")]
    [SerializeField] private Image _audioIndicatorImage;

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

        // 🔧 Clear and turn off the question box layout initially
        if (_questionTextBox != null) _questionTextBox.SetActive(false);
        if (_questionText != null) _questionText.text = "";

        if (_completionTextObject != null) _completionTextObject.SetActive(false);

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
        foreach (U14_G01_Junior1A_QuestionData item in _questionData)
        {
            // Reset fish position and slide in
            _fish.GetComponent<SlideEffect_Junior1A>().enabled = false;
            _fish.transform.localScale = new Vector3(0.580767f, 0.580767f, 0.580767f);
            _fish.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -147);
            _fish.GetComponent<SlideEffect_Junior1A>()._targetPosition = new Vector3(0, -147, 0);
            _fish.GetComponent<SlideEffect_Junior1A>().enabled = true;
            _fish.SetActive(true);

            int _optionIndex = 0;

            yield return new WaitForSeconds(.5f);

            // Populate answer buttons
            foreach (Transform button in _buttonParent.transform)
            {
                button.GetComponent<Image>().color = Color.white;
                button.GetChild(0).GetComponent<TextMeshProUGUI>().text = item.OptionText[_optionIndex++];
                button.GetComponent<PopEffect_Junior1A>().enabled = true;
                button.gameObject.SetActive(true);
                button.GetChild(0).GetComponent<TextPopEffect_Junior1A>().enabled = false;
                button.GetChild(0).GetComponent<TextPopEffect_Junior1A>().enabled = true;
            }

            // 🔧 Show text, turn on question box layout, and apply pop animations alongside option buttons
            if (_questionText != null)
            {
                _questionText.text = item.Question.name + "?";
            }
            
            if (_questionTextBox != null)
            {
                _questionTextBox.SetActive(true);
                
                // If the box frame container has a Pop component attached, run it
                if (_questionTextBox.TryGetComponent(out PopEffect_Junior1A boxPop))
                {
                    boxPop.enabled = false;
                    boxPop.enabled = true;
                }
                
                // Refresh text pop effect inside the question frame if available
                if (_questionText != null && _questionText.TryGetComponent(out TextPopEffect_Junior1A textPop))
                {
                    textPop.enabled = false;
                    textPop.enabled = true;
                }
            }

            foreach (Transform button in _buttonParent.transform) button.GetComponent<Button>().interactable = true;

            yield return new WaitUntil(() => _selectedButton != null);

            foreach (Transform button in _buttonParent.transform) button.GetComponent<Button>().interactable = false;

            if (_selectedButton == _buttonParent.transform.GetChild(item.CorrectAnsIndex))
            {
                _correctAnsCount++;
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _correctColor;
                _selectedButton.GetComponent<PopEffect_Junior1A>().enabled = true;
            }
            else
            {
                _audioSource.clip = _wrongClip;
                _audioSource.Play();
                _selectedButton.GetComponent<Image>().color = _wrongColor;
                _selectedButton.GetComponent<WiggleEffect_Junior1A1>().enabled = true;
            }

            yield return new WaitForSeconds(_audioSource.clip.length);

            // Slide fish out
            _fish.GetComponent<SlideEffect_Junior1A>().enabled = false;
            _fish.GetComponent<RectTransform>().anchoredPosition = new Vector2(-1600, -147);
            _fish.GetComponent<SlideEffect_Junior1A>()._targetPosition = new Vector3(-1600, -147, 0);
            _fish.GetComponent<SlideEffect_Junior1A>().enabled = true;
            
            foreach (Transform button in _buttonParent.transform) button.gameObject.SetActive(false);

            // 🔧 Turn off the question box container frame cleanly as the fish leaves
            if (_questionTextBox != null)
            {
                _questionTextBox.SetActive(false);
            }

            yield return new WaitForSeconds(2f);

            if (_questionText != null)
            {
                _questionText.text = "";
            }

            _selectedButton = null;
            _fish.SetActive(false);
            _currentQuestionIndex++;
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
        }

        // ── End screen ─────────────────────────────────────
        transform.GetChild(0).gameObject.SetActive(false);
        _starsPanel.SetActive(true);

        for (int i = 0; i < _starsPanel.transform.childCount - 1; i++)
        {
            _starsPanel.transform.GetChild(i).GetComponent<PopEffect_Junior1A>().enabled = true;
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
            if (_completionTextObject.TryGetComponent(out PopEffect_Junior1A textPop))
            {
                textPop.enabled = false;
                textPop.enabled = true;
            }
            yield return new WaitForSeconds(1.5f);
        }

        GameManager_Junior1A.Instance.Next(true);
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
        if (_currentQuestionIndex < _questionData.Length)
        {
            _audioSource.clip = _questionData[_currentQuestionIndex].Question;
            _audioSource.Play();
            if (_audioIndicatorImage != null) _audioIndicatorImage.enabled = true;
            yield return new WaitForSeconds(_audioSource.clip.length);
            if (_audioIndicatorImage != null) _audioIndicatorImage.enabled = false;
        }
    }
}