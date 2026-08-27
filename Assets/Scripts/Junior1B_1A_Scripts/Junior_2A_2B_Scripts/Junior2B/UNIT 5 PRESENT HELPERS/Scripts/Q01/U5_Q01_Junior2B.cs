using Junior2B;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unit5.Q01.Junior2B
{
    [Serializable]
    public class U5_Q01_Junior2B_QuestionData
    {
        public AudioClip SamQuestion;
        public string HeadingText, QuestionText;
        public string[] OptionText;
        public int CorrectAnsIndex;
        
        [Header("Custom Question Visuals")]
        public bool ShowImage; // Check this for Question 4
        public bool UseCustomButtonColors; // Check this for Question 1
        public Color[] CustomButtonColors; // Element 0 = Green, Element 1 = Red, Element 2 = Blue
    }

    public class U5_Q01_Junior2B : MonoBehaviour, Interfaces_Junior2B
    {
        [Header("=== Audio Elements ===")]
        [SerializeField] AudioSource _audioSource;
        [SerializeField] AudioClip _introClip, _wrongClip, _correctClip;
        
        [Header("=== Quiz UI Controls ===")]
        [SerializeField] GameObject _headingObj;
        [SerializeField] GameObject _buttonParent;
        [SerializeField] GameObject _object;
        [SerializeField] GameObject _imageObj;
        [SerializeField] GameObject _completed;
        [SerializeField] GameObject _mainHeading;
        
        [Tooltip("The audio playback button that toggles depending on target image object layouts.")]
        [SerializeField] private GameObject _audioButton;

        [Header("=== Styling Options ===")]
        [SerializeField] Color _wrongColor;
        [SerializeField] Color _correctColor;
        [SerializeField] Color _wonStar;
        [SerializeField] Color _loseStar;
        
        [Header("=== Quiz Configuration ===")]
        [SerializeField] U5_Q01_Junior2B_QuestionData[] _questions;
        [SerializeField] int _currentQuestionIndex, _currentOptionIndex, _starWon;
        [SerializeField] bool _isViewed = false;
        
        private Coroutine _audioCoroutine, _checkCoroutine;

        public bool IsViewed => _isViewed;

        void OnEnable() => StartCoroutine(StartQuiz());

        IEnumerator StartQuiz()
        {
            _currentQuestionIndex = _currentOptionIndex = _starWon = 0;
            _checkCoroutine = null;
            _audioSource.pitch = 1;
            
            if (_completed != null) _completed.SetActive(false);
            if (_object != null) _object.SetActive(false);
            if (_headingObj != null) _headingObj.SetActive(false);
            if (_mainHeading != null) _mainHeading.SetActive(true);
            if (_audioButton != null) _audioButton.SetActive(true); // Default to on during intro
            
            if (_completed != null)
            {
                foreach (Transform star in _completed.transform)
                {
                    star.gameObject.SetActive(false);
                }
            }

            ResetButtonsToDefaultState();

            if (_introClip != null)
            {
                _audioSource.clip = _introClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_introClip.length);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            if (_mainHeading != null) _mainHeading.SetActive(false);
            
            SetupQuestionView();
        }

        private void SetupQuestionView()
        {
            if (_questions == null || _currentQuestionIndex >= _questions.Length) return;

            var currentQuestion = _questions[_currentQuestionIndex];

            // 1. Setup headings and question texts
            if (_headingObj != null && _headingObj.TryGetComponent(out TextMeshProUGUI headingText))
            {
                headingText.text = currentQuestion.HeadingText;
            }
            
            if (_object != null)
            {
                var questionText = _object.GetComponentInChildren<TextMeshProUGUI>(true);
                if (questionText != null) questionText.text = currentQuestion.QuestionText;
            }

            // 2. Populate options and dynamic button coloring
            UpdateQuestionOptionsText();

            // 3. Handle image visibility state alongside its coupled audio button toggle
            if (_imageObj != null)
            {
                _imageObj.SetActive(currentQuestion.ShowImage);
                
                // If the layout image object is active, clear out the audio button. Otherwise, keep it active.
                if (_audioButton != null)
                {
                    _audioButton.SetActive(!currentQuestion.ShowImage);
                }
            }
            else
            {
                // Fallback architecture safety check if no custom visual image panel is preset
                if (_audioButton != null) _audioButton.SetActive(true);
            }

            // 4. Wake panels up
            if (_headingObj != null) _headingObj.SetActive(true);
            if (_object != null) _object.SetActive(true);
        }

        private void UpdateQuestionOptionsText()
        {
            if (_questions == null || _currentQuestionIndex >= _questions.Length || _buttonParent == null) return;

            var currentQuestion = _questions[_currentQuestionIndex];
            int availableDataOptions = currentQuestion.OptionText != null ? currentQuestion.OptionText.Length : 0;
            int physicalButtonsInScene = _buttonParent.transform.childCount;

            for (int i = 0; i < physicalButtonsInScene; i++)
            {
                Transform buttonObj = _buttonParent.transform.GetChild(i);

                if (i >= availableDataOptions)
                {
                    buttonObj.gameObject.SetActive(false);
                    continue;
                }

                var btnTextMesh = buttonObj.GetComponentInChildren<TextMeshProUGUI>(true);
                if (btnTextMesh != null && currentQuestion.OptionText[i] != null)
                {
                    btnTextMesh.text = currentQuestion.OptionText[i];
                }
                
                // Apply custom base color if set, otherwise reset cleanly to white default base
                if (buttonObj.TryGetComponent(out Image img))
                {
                    if (currentQuestion.UseCustomButtonColors && currentQuestion.CustomButtonColors != null && i < currentQuestion.CustomButtonColors.Length)
                    {
                        img.color = currentQuestion.CustomButtonColors[i];
                    }
                    else
                    {
                        img.color = Color.white;
                    }
                }

                buttonObj.gameObject.SetActive(true);
            }
        }

        private void ResetButtonsToDefaultState()
        {
            if (_buttonParent == null) return;

            foreach (Transform obj in _buttonParent.transform)
            {
                if (obj.TryGetComponent(out Image img)) img.color = Color.white;
                if (obj.TryGetComponent(out Button btn)) btn.enabled = true;
                if (obj.TryGetComponent(out Popeffect_Junior2B pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
            }
        }

        public void PlayAudio()
        {
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            _audioCoroutine = StartCoroutine(StartAudio());
        }

        public void SetIndex(int Index) => _currentOptionIndex = Index;

        public void CheckQuestion()
        {
            if (_buttonParent == null) return;
            
            if (_checkCoroutine == null) 
                _checkCoroutine = StartCoroutine(CheckAnswer(_currentOptionIndex));
                
            foreach (Transform obj in _buttonParent.transform)
            {
                if (obj.TryGetComponent(out Button btn)) btn.enabled = false;
            }
        }

        IEnumerator CheckAnswer(int index)
        {
            if (_questions == null || _currentQuestionIndex >= _questions.Length || _buttonParent == null) yield break;

            var currentQuestion = _questions[_currentQuestionIndex];

            if (index < _buttonParent.transform.childCount)
            {
                Transform selectedButton = _buttonParent.transform.GetChild(index);

                if (index == currentQuestion.CorrectAnsIndex)
                {
                    _audioSource.Stop();
                    _starWon++;
                    
                    if (selectedButton.TryGetComponent(out Image img)) img.color = _correctColor;
                    if (selectedButton.TryGetComponent(out Popeffect_Junior2B pop))
                    {
                        pop.enabled = false;
                        pop.enabled = true;
                    }
                    
                    if (_correctClip != null)
                    {
                        _audioSource.clip = _correctClip;
                        _audioSource.Play();
                        yield return new WaitForSeconds(_audioSource.clip.length);
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
                    
                    if (_wrongClip != null)
                    {
                        _audioSource.clip = _wrongClip;
                        _audioSource.Play();
                        yield return new WaitForSeconds(_wrongClip.length);
                    }
                }
            }

            if (_currentQuestionIndex < _questions.Length - 1)
            {
                if (_object != null) _object.SetActive(false);
                
                _currentQuestionIndex++;
                _currentOptionIndex = 0;
                
                ResetButtonsToDefaultState();
                SetupQuestionView();
            }
            else
            {
                if (_object != null) _object.SetActive(false);
                if (_headingObj != null) _headingObj.SetActive(false);
                if (_imageObj != null) _imageObj.SetActive(false);
                if (_audioButton != null) _audioButton.SetActive(false); // Make sure audio button turns off on end page
                if (_completed != null) _completed.SetActive(true);
                
                if (_completed != null)
                {
                    int totalChildStars = _completed.transform.childCount;
                    for (int i = 0; i < totalChildStars; i++)
                    {
                        Transform star = _completed.transform.GetChild(i);
                        
                        if (i < _starWon)
                        {
                            if (star.TryGetComponent(out Image starImg)) starImg.color = _wonStar;
                            if (star.TryGetComponent(out Popeffect_Junior2B starPop))
                            {
                                starPop.enabled = false;
                                starPop.enabled = true;
                            }
                            
                            if (_correctClip != null)
                            {
                                _audioSource.clip = _correctClip;
                                _audioSource.Play();
                                _audioSource.pitch += .1f;
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
                            if (star.TryGetComponent(out Image starImg)) starImg.color = _loseStar;
                            if (star.TryGetComponent(out Popeffect_Junior2B starPop))
                            {
                                starPop.enabled = false;
                                starPop.enabled = true;
                            }
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

        IEnumerator StartAudio()
        {
            if (_questions == null || _currentQuestionIndex >= _questions.Length) yield break;
            
            AudioClip currentClip = _questions[_currentQuestionIndex].SamQuestion;
            if (currentClip != null)
            {
                _audioSource.clip = currentClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_audioSource.clip.length);
            }
        }

        public void SetColorRight(Image img) { if (img != null) img.color = _correctColor; }
        public void SetColorWrong(Image img) { if (img != null) img.color = _wrongColor; }
    }
}