using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U11_G01_Junior2A_QuestionData
{
    [Tooltip("Audio clip for the question prompt.")]
    public AudioClip Question;

    [Tooltip("Unique image sprite that pops up for this specific question.")]
    public Sprite QuestionImage;

    [Tooltip("Option text strings for the answer buttons.")]
    public string[] OptionText;

    [Tooltip("Index (0-based) corresponding to the correct answer button.")]
    public int CorrectAnsIndex;
}

public class U11_G01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [Header("Audio Components")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip, _correctClip, _wrongClip;

    [Header("Color Configurations")]
    [SerializeField] private Color _wrongColor = Color.red;
    [SerializeField] private Color _correctColor = Color.green;
    [SerializeField] private Color _wonStar = Color.yellow;
    [SerializeField] private Color _loseStar = Color.gray;

    [Header("Question Data")]
    [SerializeField] private U11_G01_Junior2A_QuestionData[] _questionData;

    [Header("UI Object Links")]
    [SerializeField] private GameObject _fish;
    [SerializeField] private GameObject _buttonParent;
    [SerializeField] private GameObject _starsPanel;

    [Tooltip("Drag the TextMeshProUGUI component that displays the question text here.")]
    [SerializeField] private TextMeshProUGUI _questionText;

    [Tooltip("Drag the Image component where the unique question image should be displayed.")]
    [SerializeField] private Image _questionImageDisplay;

    [Header("End Screen Configuration")]
    [Tooltip("Drag the game over text or completion banner object here.")]
    [SerializeField] private GameObject _completionTextObject;

    [SerializeField] private TextMeshProUGUI _currentQuestionIndexText;

    [Header("Runtime State Tracker")]
    [SerializeField] private bool _isViewed = false;

    private Transform _selectedButton;
    private int _currentQuestionIndex = 0;
    private int _correctAnsCount = 0;

    private Coroutine _coroutineAudioPlayer;
    private Coroutine _coroutineNextFish;

    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(Starter());

    private void OnDisable()
    {
        StopAllCoroutines();
        if (_audioSource != null) _audioSource.Stop();
    }

    private IEnumerator Starter()
    {
        if (_audioSource != null) _audioSource.pitch = 1f;

        if (_buttonParent != null)
        {
            foreach (Transform button in _buttonParent.transform)
            {
                button.gameObject.SetActive(false);
            }
        }

        if (transform.childCount > 0) transform.GetChild(0).gameObject.SetActive(true);

        if (_fish != null) _fish.SetActive(false);
        if (_starsPanel != null) _starsPanel.SetActive(false);
        if (_completionTextObject != null) _completionTextObject.SetActive(false);

        // Ensure image starts hidden
        if (_questionImageDisplay != null) _questionImageDisplay.gameObject.SetActive(false);

        _currentQuestionIndex = 0;
        _correctAnsCount = 0;
        _selectedButton = null;

        if (_currentQuestionIndexText != null && _questionData != null)
        {
            _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
        }

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }

        _coroutineNextFish = StartCoroutine(MoveFish());
    }

    private IEnumerator MoveFish()
    {
        if (_questionData == null || _fish == null || _buttonParent == null) yield break;

        // Fallback search if _questionText isn't assigned in the inspector
        if (_questionText == null)
        {
            _questionText = _fish.GetComponentInChildren<TextMeshProUGUI>();
        }

        foreach (U11_G01_Junior2A_QuestionData item in _questionData)
        {
            // 1. Reset & Slide Fish In
            if (_fish.TryGetComponent(out SlideEffect_Junior2A slide))
            {
                slide.enabled = false;
                RectTransform rt = _fish.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0, -25);
                slide._targetPosition = new Vector3(0, -25, 0);
                slide.enabled = true;
            }
            _fish.SetActive(true);

            // 2. POP UP QUESTION IMAGE AS FISH APPEARS
            if (_questionImageDisplay != null)
            {
                if (item.QuestionImage != null)
                {
                    _questionImageDisplay.sprite = item.QuestionImage;
                    _questionImageDisplay.gameObject.SetActive(true);

                    if (_questionImageDisplay.TryGetComponent(out PopEffect_Junior2A imgPop))
                    {
                        imgPop.enabled = false;
                        imgPop.enabled = true;
                    }
                }
                else
                {
                    _questionImageDisplay.gameObject.SetActive(false);
                }
            }

            yield return new WaitForSeconds(0.5f);

            // 3. Populate Answer Buttons
            int optionIndex = 0;
            foreach (Transform button in _buttonParent.transform)
            {
                if (optionIndex < item.OptionText.Length)
                {
                    if (button.TryGetComponent(out Image btnImg)) btnImg.color = Color.white;

                    TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmpText != null) tmpText.text = item.OptionText[optionIndex];

                    if (button.TryGetComponent(out PopEffect_Junior2A pop))
                    {
                        pop.enabled = false;
                        pop.enabled = true;
                    }

                    button.gameObject.SetActive(true);

                    if (tmpText != null && tmpText.TryGetComponent(out TextPopEffect_Junior2A textPop))
                    {
                        textPop.enabled = false;
                        textPop.enabled = true;
                    }

                    optionIndex++;
                }
            }

            // 4. Update Question Text Prompt
            if (_questionText != null && item.Question != null)
            {
                _questionText.text = item.Question.name + "?";
                _questionText.gameObject.SetActive(true);
            }

            // Enable buttons
            foreach (Transform button in _buttonParent.transform)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            }

            // Wait for user answer selection
            yield return new WaitUntil(() => _selectedButton != null);

            // Lock buttons
            foreach (Transform button in _buttonParent.transform)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = false;
            }

            // 5. Answer Verification
            Transform correctBtnTransform = _buttonParent.transform.GetChild(item.CorrectAnsIndex);
            if (_selectedButton == correctBtnTransform)
            {
                _correctAnsCount++;
                if (_audioSource != null && _correctClip != null)
                {
                    _audioSource.clip = _correctClip;
                    _audioSource.Play();
                }

                if (_selectedButton.TryGetComponent(out Image btnImg)) btnImg.color = _correctColor;
                if (_selectedButton.TryGetComponent(out PopEffect_Junior2A pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
            }
            else
            {
                if (_audioSource != null && _wrongClip != null)
                {
                    _audioSource.clip = _wrongClip;
                    _audioSource.Play();
                }

                if (_selectedButton.TryGetComponent(out Image btnImg)) btnImg.color = _wrongColor;
                if (_selectedButton.TryGetComponent(out WiggleEffect_Junior2A wiggle))
                {
                    wiggle.enabled = false;
                    wiggle.enabled = true;
                }
            }

            if (_audioSource != null && _audioSource.clip != null)
            {
                yield return new WaitForSeconds(_audioSource.clip.length);
            }

            // 6. Slide Out Fish
            if (_fish.TryGetComponent(out SlideEffect_Junior2A slideOut))
            {
                slideOut.enabled = false;
                RectTransform rt = _fish.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(-1600, -25);
                slideOut._targetPosition = new Vector3(-1600, -25, 0);
                slideOut.enabled = true;
            }

            foreach (Transform button in _buttonParent.transform) button.gameObject.SetActive(false);

            yield return new WaitForSeconds(2f);

            if (_questionText != null)
            {
                _questionText.text = "";
                _questionText.gameObject.SetActive(false);
            }

            _selectedButton = null;
            _fish.SetActive(false);
            _currentQuestionIndex++;

            if (_currentQuestionIndexText != null)
            {
                _currentQuestionIndexText.text = $"{_currentQuestionIndex}/{_questionData.Length}";
            }
        }

        // --- ALL QUESTIONS ANSWERED: DISABLE QUESTION IMAGE ---
        if (_questionImageDisplay != null)
        {
            _questionImageDisplay.gameObject.SetActive(false);
        }

        // --- END SCREEN TRANSITION SEQUENCING ---
        if (transform.childCount > 0) transform.GetChild(0).gameObject.SetActive(false);

        if (_starsPanel != null)
        {
            _starsPanel.SetActive(true);

            for (int i = 0; i < _starsPanel.transform.childCount - 1; i++)
            {
                Transform starTransform = _starsPanel.transform.GetChild(i);

                if (starTransform.TryGetComponent(out PopEffect_Junior2A starPop))
                {
                    starPop.enabled = false;
                    starPop.enabled = true;
                }

                starTransform.gameObject.SetActive(true);

                if (i < _correctAnsCount)
                {
                    if (starTransform.TryGetComponent(out Image starImg)) starImg.color = _wonStar;

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
                    if (starTransform.TryGetComponent(out Image starImg)) starImg.color = _loseStar;
                }
            }
        }

        if (_completionTextObject != null)
        {
            _completionTextObject.SetActive(true);

            if (_completionTextObject.TryGetComponent(out PopEffect_Junior2A textPop))
            {
                textPop.enabled = false;
                textPop.enabled = true;
            }

            yield return new WaitForSeconds(1.5f);
        }

        _isViewed = true;
        if (GameManager_Junior2A.Instance != null)
        {
            GameManager_Junior2A.Instance.Next(true);
        }
    }

    public void SelectedObject(Transform button) => _selectedButton = button;

    public void PlayAudio()
    {
        if (_coroutineAudioPlayer != null) StopCoroutine(_coroutineAudioPlayer);
        _coroutineAudioPlayer = StartCoroutine(PlayCurrentAudio());
    }

    private IEnumerator PlayCurrentAudio()
    {
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length) yield break;

        AudioClip currentClip = _questionData[_currentQuestionIndex].Question;
        if (currentClip == null || _audioSource == null) yield break;

        _audioSource.clip = currentClip;
        _audioSource.Play();

        Transform audioIndicator = _fish.transform.childCount > 1 ? _fish.transform.GetChild(1) : null;
        if (audioIndicator != null && audioIndicator.TryGetComponent(out Image img))
        {
            img.enabled = true;
            yield return new WaitForSeconds(currentClip.length);
            img.enabled = false;
        }
    }
}