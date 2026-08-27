using Junior2B;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unit8.G01.Junior2B
{
    [Serializable]
    public class AudioQuestionData
    {
        [Tooltip("The audio clip played when clicking the question button.")]
        public AudioClip QuestionAudio;
        [Tooltip("Text labels for the 3 selection options for this question.")]
        public string[] OptionTexts = new string[3];
        [Tooltip("Index of the correct option button (0, 1, or 2).")]
        public int CorrectOptionIndex;
    }

    public class U8_G01_Junior2B : MonoBehaviour, Interfaces_Junior2B
    {
        [Header("=== UI Components ===")]
        [Tooltip("Parent/Container or array of Question Buttons (e.g., Q1, Q2, Q3).")]
        [SerializeField] private Button[] _questionButtons;

        [Tooltip("The 3 option buttons used to answer the active question.")]
        [SerializeField] private Button[] _optionButtons = new Button[3];

        [Tooltip("The panel containing stars/results displayed at the end.")]
        [SerializeField] private GameObject _completedPanel;

        [Tooltip("Optional progress indicator text.")]
        [SerializeField] private TextMeshProUGUI _currentQuestionIndexText;

        [Header("=== Question Data ===")]
        [SerializeField] private AudioQuestionData[] _questions;

        [Header("=== Audio Elements ===")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _introClip;
        [SerializeField] private AudioClip _correctClip;
        [SerializeField] private AudioClip _incorrectClip;

        [Header("=== Feedback Colors ===")]
        [SerializeField] private Color _defaultColor = Color.white;
        [SerializeField] private Color _correctColor = Color.green;
        [SerializeField] private Color _wrongColor = Color.red;
        [SerializeField] private Color _wonStarColor = Color.yellow;
        [SerializeField] private Color _loseStarColor = Color.gray;

        private int _currentQuestionIndex = 0;
        private int _correctAnsCount = 0;
        private bool _isViewed = false;
        private Coroutine _gameCoroutine;

        public bool IsViewed => _isViewed;

        private void OnEnable()
        {
            if (_audioSource != null) _audioSource.pitch = 1f;

            _currentQuestionIndex = 0;
            _correctAnsCount = 0;
            _isViewed = false;

            if (_completedPanel != null) _completedPanel.SetActive(false);

            // Hide/Disable option buttons until a question is clicked
            SetOptionButtonsActive(false);

            // Initialize Question buttons states
            UpdateQuestionButtonsInteractability();

            _gameCoroutine = StartCoroutine(PlayIntroAndStartGame());
        }

        private IEnumerator PlayIntroAndStartGame()
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

            // Unlock and highlight the first question button
            UpdateQuestionButtonsInteractability();
            UpdateProgressUI();
        }

        /// <summary>
        /// Called when the player clicks on a Question button (Q1, Q2, Q3, etc.)
        /// Assign this method to each Question Button's OnClick event passing index 0, 1, 2...
        /// </summary>
        public void OnQuestionButtonClicked(int questionIndex)
        {
            if (questionIndex != _currentQuestionIndex) return;
            if (_questions == null || questionIndex >= _questions.Length) return;

            if (_gameCoroutine != null) StopCoroutine(_gameCoroutine);
            _gameCoroutine = StartCoroutine(PlayQuestionAudioAndShowOptions(questionIndex));
        }

        private IEnumerator PlayQuestionAudioAndShowOptions(int questionIndex)
        {
            AudioQuestionData currentData = _questions[questionIndex];

            // 1. Play the question audio clip
            if (_audioSource != null && currentData.QuestionAudio != null)
            {
                _audioSource.clip = currentData.QuestionAudio;
                _audioSource.Play();
            }

            // 2. Setup and display option buttons
            SetupOptionButtons(currentData);
            SetOptionButtonsActive(true);
            SetOptionButtonsInteractable(true);
            ResetOptionButtonVisuals();

            yield return null;
        }

        private void SetupOptionButtons(AudioQuestionData currentData)
        {
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                if (_optionButtons[i] == null) continue;

                TextMeshProUGUI tmpText = _optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmpText != null && i < currentData.OptionTexts.Length)
                {
                    tmpText.text = currentData.OptionTexts[i];
                }
            }
        }

        /// <summary>
        /// Called when the player clicks one of the 3 answer option buttons.
        /// Assign to option buttons passing parameters 0, 1, or 2.
        /// </summary>
        public void ChooseOption(int selectedOptionIndex)
        {
            if (_questions == null || _currentQuestionIndex >= _questions.Length) return;

            SetOptionButtonsInteractable(false);

            if (_gameCoroutine != null) StopCoroutine(_gameCoroutine);
            _gameCoroutine = StartCoroutine(CheckAnswerSequence(selectedOptionIndex));
        }

        private IEnumerator CheckAnswerSequence(int selectedIndex)
        {
            AudioQuestionData currentQuestion = _questions[_currentQuestionIndex];
            bool isCorrect = (selectedIndex == currentQuestion.CorrectOptionIndex);

            if (selectedIndex >= 0 && selectedIndex < _optionButtons.Length && _optionButtons[selectedIndex] != null)
            {
                if (isCorrect)
                {
                    _correctAnsCount++;
                    SetButtonColor(_optionButtons[selectedIndex], _correctColor);

                    if (_optionButtons[selectedIndex].TryGetComponent(out Popeffect_Junior2B pop))
                    {
                        pop.enabled = false;
                        pop.enabled = true;
                    }

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
                }
                else
                {
                    SetButtonColor(_optionButtons[selectedIndex], _wrongColor);

                    if (_optionButtons[selectedIndex].TryGetComponent(out WiggleEffect_Junior2B wiggle))
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
                }
            }

            // Hide options after an answer is submitted
            SetOptionButtonsActive(false);

            _currentQuestionIndex++;

            if (_currentQuestionIndex < _questions.Length)
            {
                // Enable next question button
                UpdateQuestionButtonsInteractability();
                UpdateProgressUI();
            }
            else
            {
                // All questions completed
                StartCoroutine(CompleteGameSequence());
            }
        }

        private IEnumerator CompleteGameSequence()
        {
            // Disable all question and option buttons
            foreach (Button btn in _questionButtons)
            {
                if (btn != null) btn.interactable = false;
            }
            SetOptionButtonsActive(false);

            if (_completedPanel != null)
            {
                _completedPanel.SetActive(true);
                int scoreTrackMaxLimit = _completedPanel.transform.childCount;

                for (int i = 0; i < scoreTrackMaxLimit; i++)
                {
                    Transform star = _completedPanel.transform.GetChild(i);
                    star.gameObject.SetActive(true);

                    if (star.TryGetComponent(out Popeffect_Junior2B pop))
                    {
                        pop.enabled = false;
                        pop.enabled = true;
                    }

                    if (i < _correctAnsCount)
                    {
                        if (star.TryGetComponent(out Image img)) img.color = _wonStarColor;

                        if (_audioSource != null && _correctClip != null)
                        {
                            _audioSource.clip = _correctClip;
                            _audioSource.Play();
                            _audioSource.pitch += 0.05f;
                            yield return new WaitForSeconds(_correctClip.length);
                        }
                    }
                    else
                    {
                        if (star.TryGetComponent(out Image img)) img.color = _loseStarColor;
                        yield return new WaitForSeconds(0.2f);
                    }
                }
            }

            _isViewed = true;
            if (GameManager_Junior2B.Instance != null)
            {
                GameManager_Junior2B.Instance.Next(true);
            }
        }

        private void UpdateQuestionButtonsInteractability()
        {
            if (_questionButtons == null) return;

            for (int i = 0; i < _questionButtons.Length; i++)
            {
                if (_questionButtons[i] == null) continue;

                // Make visible
                _questionButtons[i].gameObject.SetActive(true);

                // Only current question is interactable
                _questionButtons[i].interactable = (i == _currentQuestionIndex);
            }
        }

        private void UpdateProgressUI()
        {
            if (_currentQuestionIndexText != null && _questions != null)
            {
                _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questions.Length}";
            }
        }

        private void SetOptionButtonsActive(bool active)
        {
            foreach (Button btn in _optionButtons)
            {
                if (btn != null) btn.gameObject.SetActive(active);
            }
        }

        private void SetOptionButtonsInteractable(bool state)
        {
            foreach (Button btn in _optionButtons)
            {
                if (btn != null) btn.interactable = state;
            }
        }

        private void ResetOptionButtonVisuals()
        {
            foreach (Button btn in _optionButtons)
            {
                if (btn != null) SetButtonColor(btn, _defaultColor);
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
}