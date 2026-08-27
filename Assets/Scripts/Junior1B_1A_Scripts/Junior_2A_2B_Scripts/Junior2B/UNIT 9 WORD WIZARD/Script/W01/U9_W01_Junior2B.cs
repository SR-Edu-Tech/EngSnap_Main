using Junior2B;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unit9.W01.Junior2B
{
    [Serializable]
    public class RoomQuizQuestionData
    {
        public Sprite RoomSprite;
        public AudioClip QuestionClip;
        public int CorrectAnswerIndex; // 0 = BEDROOM, 1 = BATHROOM, 2 = KITCHEN, 3 = LIVING ROOM
    }

    public class U9_W01_Junior2B : MonoBehaviour, Interfaces_Junior2B
    {
        [Header("=== Audio Elements ===")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _introClip, _wrongClip, _correctClip;

        [Header("=== UI Elements ===")]
        [SerializeField] private Image _roomDisplayImage;
        [SerializeField] private GameObject _buttonParent;
        [SerializeField] private GameObject _headingTitle; // Always visible throughout the quiz
        [SerializeField] private GameObject _completedPanel;

        [Header("=== Color Feedback ===")]
        [SerializeField] private Color _defaultButtonColor = Color.white;
        [SerializeField] private Color _correctColor = Color.green;
        [SerializeField] private Color _wrongColor = Color.red;
        [SerializeField] private Color _wonStarColor = Color.yellow;
        [SerializeField] private Color _loseStarColor = Color.gray;

        [Header("=== Question Setup ===")]
        [SerializeField] private RoomQuizQuestionData[] _questions;

        private readonly string[] _roomOptions = new string[] { "BEDROOM", "BATHROOM", "KITCHEN", "LIVING ROOM" };

        private int _currentQuestionIndex;
        private int _selectedOptionIndex;
        private int _starsWon;
        private bool _isViewed = false;

        private Coroutine _checkCoroutine;

        public bool IsViewed => _isViewed;

        private void OnEnable() => StartCoroutine(StartQuiz());

        private IEnumerator StartQuiz()
        {
            _currentQuestionIndex = 0;
            _selectedOptionIndex = 0;
            _starsWon = 0;
            _checkCoroutine = null;

            // Keep heading title visible at all times
            if (_headingTitle != null) _headingTitle.SetActive(true);
            if (_completedPanel != null) _completedPanel.SetActive(false);

            if (_completedPanel != null)
            {
                foreach (Transform star in _completedPanel.transform)
                {
                    star.gameObject.SetActive(false);
                }
            }

            ResetButtonsToDefaultState();

            if (_introClip != null && _audioSource != null)
            {
                _audioSource.clip = _introClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_introClip.length);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            SetupQuestionView();
        }

        private void SetupQuestionView()
        {
            if (_questions == null || _currentQuestionIndex >= _questions.Length) return;

            var currentQuestion = _questions[_currentQuestionIndex];

            // 1. Update image display
            if (_roomDisplayImage != null && currentQuestion.RoomSprite != null)
            {
                _roomDisplayImage.sprite = currentQuestion.RoomSprite;
                _roomDisplayImage.gameObject.SetActive(true);
            }

            // 2. Refresh option buttons
            SetupOptionButtons();

            // 3. Play question audio if available
            if (_audioSource != null && currentQuestion.QuestionClip != null)
            {
                _audioSource.clip = currentQuestion.QuestionClip;
                _audioSource.Play();
            }
        }

        private void SetupOptionButtons()
        {
            if (_buttonParent == null) return;

            int totalChildButtons = _buttonParent.transform.childCount;

            for (int i = 0; i < totalChildButtons; i++)
            {
                Transform buttonObj = _buttonParent.transform.GetChild(i);

                if (i >= _roomOptions.Length)
                {
                    buttonObj.gameObject.SetActive(false);
                    continue;
                }

                // Assign option label
                var textObj = buttonObj.GetComponentInChildren<TextMeshProUGUI>(true);
                if (textObj != null)
                {
                    textObj.text = _roomOptions[i];
                }

                // Reset button visuals & interactability
                if (buttonObj.TryGetComponent(out Image img)) img.color = _defaultButtonColor;
                if (buttonObj.TryGetComponent(out Button btn)) btn.interactable = true;

                buttonObj.localScale = Vector3.one;
                buttonObj.gameObject.SetActive(true);

                if (buttonObj.TryGetComponent(out Popeffect_Junior2B pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
            }
        }

        private void ResetButtonsToDefaultState()
        {
            if (_buttonParent == null) return;

            foreach (Transform child in _buttonParent.transform)
            {
                if (child.TryGetComponent(out Image img)) img.color = _defaultButtonColor;
                if (child.TryGetComponent(out Button btn)) btn.interactable = true;
                child.localScale = Vector3.one;
            }
        }

        // Attached directly to Option Buttons (or called by Unity UI events)
        public void SetIndex(int optionIndex)
        {
            _selectedOptionIndex = optionIndex;
            CheckQuestion(); // Automatically triggers answer checking on click
        }

        public void CheckQuestion()
        {
            if (_buttonParent == null || _checkCoroutine != null) return;

            // Lock all buttons during evaluation
            foreach (Transform child in _buttonParent.transform)
            {
                if (child.TryGetComponent(out Button btn)) btn.interactable = false;
            }

            _checkCoroutine = StartCoroutine(CheckAnswerRoutine(_selectedOptionIndex));
        }

        private IEnumerator CheckAnswerRoutine(int chosenIndex)
        {
            if (_questions == null || _currentQuestionIndex >= _questions.Length || _buttonParent == null) yield break;

            var currentQuestion = _questions[_currentQuestionIndex];

            if (chosenIndex < _buttonParent.transform.childCount)
            {
                Transform selectedButton = _buttonParent.transform.GetChild(chosenIndex);

                if (chosenIndex == currentQuestion.CorrectAnswerIndex)
                {
                    if (_audioSource != null) _audioSource.Stop();
                    _starsWon++;

                    if (selectedButton.TryGetComponent(out Image img)) img.color = _correctColor;
                    if (selectedButton.TryGetComponent(out Popeffect_Junior2B pop))
                    {
                        pop.enabled = false;
                        pop.enabled = true;
                    }

                    if (_audioSource != null && _correctClip != null)
                    {
                        _audioSource.clip = _correctClip;
                        _audioSource.Play();
                        yield return new WaitForSeconds(_correctClip.length);
                    }
                }
                else
                {
                    if (selectedButton.TryGetComponent(out Image img)) img.color = _wrongColor;
                    if (selectedButton.TryGetComponent(out WiggleEffect_Junior2B wiggle))
                    {
                        wiggle.enabled = false;
                        wiggle.enabled = true;
                    }

                    if (_audioSource != null && _wrongClip != null)
                    {
                        _audioSource.clip = _wrongClip;
                        _audioSource.Play();
                        yield return new WaitForSeconds(_wrongClip.length);
                    }
                }
            }

            // Proceed to next question or complete game
            if (_currentQuestionIndex < _questions.Length - 1)
            {
                _currentQuestionIndex++;
                _selectedOptionIndex = 0;

                ResetButtonsToDefaultState();
                SetupQuestionView();
            }
            else
            {
                // Quiz Completed
                if (_roomDisplayImage != null) _roomDisplayImage.gameObject.SetActive(false);
                if (_buttonParent != null) _buttonParent.SetActive(false);
                if (_completedPanel != null) _completedPanel.SetActive(true);

                if (_completedPanel != null)
                {
                    int totalChildStars = _completedPanel.transform.childCount;
                    for (int i = 0; i < totalChildStars; i++)
                    {
                        Transform star = _completedPanel.transform.GetChild(i);

                        if (i < _starsWon)
                        {
                            if (star.TryGetComponent(out Image starImg)) starImg.color = _wonStarColor;
                            if (star.TryGetComponent(out Popeffect_Junior2B starPop))
                            {
                                starPop.enabled = false;
                                starPop.enabled = true;
                            }

                            if (_audioSource != null && _correctClip != null)
                            {
                                _audioSource.clip = _correctClip;
                                _audioSource.Play();
                                star.gameObject.SetActive(true);
                                yield return new WaitForSeconds(_correctClip.length);
                            }
                            else
                            {
                                star.gameObject.SetActive(true);
                                yield return new WaitForSeconds(0.3f);
                            }
                        }
                        else
                        {
                            if (star.TryGetComponent(out Image starImg)) starImg.color = _loseStarColor;
                            star.gameObject.SetActive(true);
                        }
                    }
                }

                _isViewed = true;
                if (GameManager_Junior2B.Instance != null)
                {
                    GameManager_Junior2B.Instance.Next(true);
                }
            }

            _checkCoroutine = null;
        }

        public void PlayAudio()
        {
            if (_questions == null || _currentQuestionIndex >= _questions.Length) return;

            AudioClip clip = _questions[_currentQuestionIndex].QuestionClip;
            if (clip != null && _audioSource != null)
            {
                _audioSource.clip = clip;
                _audioSource.Play();
            }
        }

        public void SetColorRight(Image img) { if (img != null) img.color = _correctColor; }
        public void SetColorWrong(Image img) { if (img != null) img.color = _wrongColor; }
    }
}