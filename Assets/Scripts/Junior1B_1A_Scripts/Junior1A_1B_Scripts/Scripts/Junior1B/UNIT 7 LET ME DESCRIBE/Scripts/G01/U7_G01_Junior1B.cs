using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U7_G01_QuestionData
{
    [Tooltip("The main image displayed to the player.")]
    public Sprite TargetImage;
    [Tooltip("Text labels for the 3 selection buttons.")]
    public string[] OptionTexts = new string[3];
    [Tooltip("Index of the correct button (0, 1, or 2).")]
    public int CorrectOptionIndex;
}

public class U7_G01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("=== UI Components ===")]
    [Tooltip("The UI Image component displaying the item to guess.")]
    [SerializeField] Image _displayImageComponent;
    [Tooltip("The array containing exactly your 3 selection buttons.")]
    [SerializeField] Button[] _optionButtons = new Button[3];
    [Tooltip("The stars container parent panel evaluated at the end of the activity.")]
    [SerializeField] GameObject _starsPanel;
    [Tooltip("The UI layout component tracking exercise advancement.")]
    [SerializeField] TextMeshProUGUI _currentQuestionIndexText;

    [Header("=== Game Data ===")]
    [SerializeField] U7_G01_QuestionData[] _questions;
    [SerializeField] int _currentQuestionIndex = 0;
    [SerializeField] int _correctAnsCount = 0;

    [Header("=== Audio Elements ===")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip _correctClip;
    [SerializeField] AudioClip _incorrectClip;

    [Header("=== Feedback Colors ===")]
    [SerializeField] Color _defaultColor = Color.white;
    [SerializeField] Color _correctColor = Color.green;
    [SerializeField] Color _wrongColor = Color.red;
    [SerializeField] Color _wonStar = Color.yellow;
    [SerializeField] Color _loseStar = Color.gray;

    private bool _isViewed = false;
    private Coroutine _gameCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        if (_audioSource != null) _audioSource.pitch = 1f;

        _currentQuestionIndex = 0;
        _correctAnsCount = 0;
        _isViewed = false;

        if (_starsPanel != null) _starsPanel.SetActive(false);

        // 1. Force the image component to be completely disabled at the very start
        if (_displayImageComponent != null)
            _displayImageComponent.gameObject.SetActive(false);

        UpdateProgressUI();
        ResetButtonsVisuals();
        SetButtonsInteractable(false);

        _gameCoroutine = StartCoroutine(PlayIntroAndStartGame());
    }

    IEnumerator PlayIntroAndStartGame()
    {
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

        LoadQuestion(_currentQuestionIndex);
    }

    private void LoadQuestion(int questionIndex)
    {
        if (_questions == null || questionIndex >= _questions.Length)
        {
            if (_gameCoroutine != null) StopCoroutine(_gameCoroutine);
            _gameCoroutine = StartCoroutine(CompleteGameSequence());
            return;
        }

        UpdateProgressUI();
        U7_G01_QuestionData currentData = _questions[questionIndex];

        // 2. Only enable the image GameObject right here when it changes to the new sprite
        if (_displayImageComponent != null && currentData.TargetImage != null)
        {
            _displayImageComponent.sprite = currentData.TargetImage;
            _displayImageComponent.gameObject.SetActive(true);
        }

        for (int i = 0; i < _optionButtons.Length; i++)
        {
            if (_optionButtons[i] == null) continue;

            TextMeshProUGUI btnText = _optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null && i < currentData.OptionTexts.Length)
            {
                btnText.text = currentData.OptionTexts[i];
            }

            _optionButtons[i].interactable = true;
        }

        ResetButtonsVisuals();
    }

    public void ChooseOption(int selectedButtonIndex)
    {
        if (_questions == null || _currentQuestionIndex >= _questions.Length) return;

        SetButtonsInteractable(false);

        if (_gameCoroutine != null) StopCoroutine(_gameCoroutine);
        _gameCoroutine = StartCoroutine(CheckAnswerSequence(selectedButtonIndex));
    }

    IEnumerator CheckAnswerSequence(int selectedIndex)
    {
        U7_G01_QuestionData currentQuestion = _questions[_currentQuestionIndex];
        bool isCorrect = (selectedIndex == currentQuestion.CorrectOptionIndex);

        if (isCorrect)
        {
            _correctAnsCount++;
            SetButtonColor(_optionButtons[selectedIndex], _correctColor);

            if (_audioSource != null && _correctClip != null)
            {
                _audioSource.clip = _correctClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_correctClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            _currentQuestionIndex++;
            LoadQuestion(_currentQuestionIndex);
        }
        else
        {
            SetButtonColor(_optionButtons[selectedIndex], _wrongColor);

            if (_optionButtons[selectedIndex].TryGetComponent(out WiggleEffect_Junior1B wiggle))
            {
                wiggle.enabled = false;
                wiggle.enabled = true;
            }

            if (_audioSource != null && _incorrectClip != null)
            {
                _audioSource.clip = _incorrectClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_incorrectClip.length);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            _currentQuestionIndex++;
            LoadQuestion(_currentQuestionIndex);
        }
    }

    IEnumerator CompleteGameSequence()
    {
        // 3. Make sure the image turns off when the score panel shows up
        if (_displayImageComponent != null) _displayImageComponent.gameObject.SetActive(false);

        foreach (Button btn in _optionButtons)
        {
            if (btn != null) btn.gameObject.SetActive(false);
        }

        if (_starsPanel != null)
        {
            _starsPanel.SetActive(true);
            int scoreTrackMaxLimit = _starsPanel.transform.childCount - 1;

            for (int i = 0; i < scoreTrackMaxLimit; i++)
            {
                Transform star = _starsPanel.transform.GetChild(i);
                star.gameObject.SetActive(true);

                if (star.TryGetComponent(out Popeffect_Junior1B pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }

                if (i < _correctAnsCount)
                {
                    if (star.TryGetComponent(out Image img)) img.color = _wonStar;

                    if (_audioSource != null && _correctClip != null)
                    {
                        _audioSource.clip = _correctClip;
                        _audioSource.Play();
                        _audioSource.pitch += 0.1f;
                        yield return new WaitForSeconds(_correctClip.length);
                    }
                }
                else
                {
                    if (star.TryGetComponent(out Image img)) img.color = _loseStar;
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }

        if (GameManager_Junior1B.Instance != null)
        {
            GameManager_Junior1B.Instance.Next(true);
        }
        _isViewed = true;
    }

    private void UpdateProgressUI()
    {
        if (_currentQuestionIndexText != null && _questions != null)
        {
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questions.Length}";
        }
    }

    private void SetButtonsInteractable(bool state)
    {
        foreach (Button btn in _optionButtons)
        {
            if (btn != null) btn.interactable = state;
        }
    }

    private void ResetButtonsVisuals()
    {
        foreach (Button btn in _optionButtons)
        {
            if (btn != null)
            {
                btn.gameObject.SetActive(true);
                SetButtonColor(btn, _defaultColor);
            }
        }
    }

    private void SetButtonColor(Button btn, Color targetColor)
    {
        if (btn == null) return;
        if (btn.TryGetComponent(out Image img))
        {
            img.color = targetColor;
        }
    }
}