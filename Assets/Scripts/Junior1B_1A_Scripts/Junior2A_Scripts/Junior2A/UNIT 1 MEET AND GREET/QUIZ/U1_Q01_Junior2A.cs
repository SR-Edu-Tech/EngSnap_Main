using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U1_Q01_Junior2A_QuestionData
{
    public AudioClip SamQuestion;
    public string HeadingText, QuestionText;
    public string[] OptionText;
    public int CorrectAnsIndex;
}

public class U1_Q01_Junior2A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Audio Configurations")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip, _wrongClip, _correctClip;

    [Header("Quiz Content Matrix")]
    [SerializeField] private U1_Q01_Junior2A_QuestionData[] _questions;

    [Header("Hierarchy UI Targets")]
    [SerializeField] private GameObject _headingObj;
    [SerializeField] private GameObject _buttonParent;
    [SerializeField] private GameObject _object;
    [SerializeField] private GameObject _completed;
    [SerializeField] private GameObject _mainHeading;

    [Header("Visual Styling Options")]
    [SerializeField] private Color _wrongColor;
    [SerializeField] private Color _correctColor;
    [SerializeField] private Color _wonStar;
    [SerializeField] private Color _loseStar;

    [Header("Runtime State Tracking")]
    [SerializeField] private int _currentQuestionIndex;
    [SerializeField] private int _currentOptionIndex;
    [SerializeField] private int _starWon;
    [SerializeField] private bool _isViewed = false;

    private Coroutine _audioCoroutine, _checkCoroutine;

    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(StartQuiz());

    private IEnumerator StartQuiz()
    {
        _currentQuestionIndex = _currentOptionIndex = _starWon = 0;
        _audioSource.pitch = 1f;
        _audioSource.clip = _introClip;
        _audioSource.Play();

        _checkCoroutine = null;
        _completed.SetActive(false);
        _object.SetActive(false);
        _mainHeading.SetActive(true);

        // Reset and prepare choice selection layouts
        foreach (Transform obj in _buttonParent.transform)
        {
            if (obj.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
            if (obj.TryGetComponent(out Button btn)) btn.enabled = true;
            if (obj.TryGetComponent(out Image img)) img.color = Color.white;
        }

        // Hide star nodes initially inside score layout container
        for (int i = 0; i < _questions.Length; i++)
        {
            if (i < _completed.transform.childCount)
            {
                _completed.transform.GetChild(i).gameObject.SetActive(false);
            }
        }

        yield return new WaitForSeconds(_introClip.length);

        // Map and assign string values for Question 1 choices
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
        foreach (Transform obj in _buttonParent.transform)
        {
            if (obj.TryGetComponent(out Button btn)) btn.enabled = false;
        }
    }

    private IEnumerator CheckAnswer(int index)
    {
        // 1. Evaluate selection correctness match
        if (index == _questions[_currentQuestionIndex].CorrectAnsIndex)
        {
            _audioSource.Stop();
            _starWon++;

            Transform correctBtn = _buttonParent.transform.GetChild(index);
            if (correctBtn.TryGetComponent(out Image img)) img.color = _correctColor;

            if (correctBtn.TryGetComponent(out PopEffect_Junior2A pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }

            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);

            if (correctBtn.TryGetComponent(out Image structuralImg)) structuralImg.color = Color.white;
        }
        else
        {
            Transform wrongBtn = _buttonParent.transform.GetChild(index);
            if (wrongBtn.TryGetComponent(out Image img)) img.color = _wrongColor;
            if (wrongBtn.TryGetComponent(out WiggleEffect_Junior2A1 wiggle)) wiggle.enabled = true;

            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);

            if (wrongBtn.TryGetComponent(out Image structuralImg)) structuralImg.color = Color.white;
        }

        // 2. State Progression: Advance quiz rounds step-by-step
        if (_currentQuestionIndex < _questions.Length - 1)
        {
            _object.SetActive(false);
            _currentQuestionIndex++;

            // Re-populate choice string variations dynamically across options
            _currentOptionIndex = 0;
            foreach (Transform obj in _buttonParent.transform)
            {
                obj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questions[_currentQuestionIndex].OptionText[_currentOptionIndex];
                if (obj.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
                _currentOptionIndex++;
            }

            _headingObj.GetComponent<TextMeshProUGUI>().text = _questions[_currentQuestionIndex].HeadingText;
            _object.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questions[_currentQuestionIndex].QuestionText;

            _headingObj.SetActive(false);
            _headingObj.SetActive(true);
            _object.SetActive(true);

            foreach (Transform obj in _buttonParent.transform)
            {
                if (obj.TryGetComponent(out Button btn)) btn.enabled = true;
            }
        }
        else // 3. End Game Sequence: Render Star Performance Metric Results
        {
            _object.SetActive(false);

            // Collect and temporarily deactivate all non-star children (like completed text/UI)
            System.Collections.Generic.List<GameObject> delayedObjects = new System.Collections.Generic.List<GameObject>();
            for (int i = _questions.Length; i < _completed.transform.childCount; i++)
            {
                GameObject childObj = _completed.transform.GetChild(i).gameObject;
                delayedObjects.Add(childObj);
                childObj.SetActive(false);
            }

            _completed.SetActive(true);

            for (int i = 0; i < _questions.Length; i++)
            {
                Transform starChild = _completed.transform.GetChild(i);

                if (i < _starWon)
                {
                    if (starChild.TryGetComponent(out Image img)) img.color = _wonStar;
                    if (starChild.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;

                    _audioSource.clip = _correctClip;
                    _audioSource.Play();
                    _audioSource.pitch += 0.1f;

                    starChild.gameObject.SetActive(true);
                    yield return new WaitForSeconds(_correctClip.length);
                }
                else
                {
                    if (starChild.TryGetComponent(out Image img)) img.color = _loseStar;
                    if (starChild.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;

                    starChild.gameObject.SetActive(true);
                }
            }

            // Wait a moment after all stars are activated, then show the completed text
            yield return new WaitForSeconds(0.5f);
            foreach (GameObject obj in delayedObjects)
            {
                obj.SetActive(true);
            }

            _isViewed = true;
            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
        }

        _checkCoroutine = null;
    }

    private IEnumerator StartAudio()
    {
        _audioSource.clip = _questions[_currentQuestionIndex].SamQuestion;
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
    }

    public void SetColorRight(Image img) => img.color = _correctColor;
    public void SetColorWrong(Image img) => img.color = _wrongColor;
}