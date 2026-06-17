using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U14_Q01_Junior1A_QuestionData
{
    public AudioClip SamQuestion;
    public string HeadingText, QuestionText;
    public string[] OptionText;
    public int CorrectAnsIndex;
}

public class U14_Q01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Audio Configurations")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip, _wrongClip, _correctClip;

    [Header("Quiz Content Matrix")]
    [SerializeField] private U14_Q01_Junior1A_QuestionData[] _questions;

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
    [SerializeField] private int _currentOptionIndex;  // set only by SetIndex()
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

        foreach (Transform obj in _buttonParent.transform)
        {
            if (obj.TryGetComponent(out PopEffect_Junior1A pop)) pop.enabled = true;
            if (obj.TryGetComponent(out Button btn)) btn.enabled = true;
            if (obj.TryGetComponent(out Image img)) img.color = Color.white;
        }

        for (int i = 0; i < _questions.Length; i++)
        {
            if (i < _completed.transform.childCount)
                _completed.transform.GetChild(i).gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(_introClip.length);

        PopulateButtons(_currentQuestionIndex);

        _mainHeading.SetActive(false);
        _headingObj.GetComponent<TextMeshProUGUI>().text = _questions[0].HeadingText;
        _object.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questions[0].QuestionText;

        _headingObj.SetActive(false);
        _headingObj.SetActive(true);
        _object.SetActive(true);
    }

    // ── Safely populate button labels using a LOCAL counter, never touching _currentOptionIndex
    private void PopulateButtons(int questionIndex)
    {
        int i = 0;
        foreach (Transform obj in _buttonParent.transform)
        {
            if (i < _questions[questionIndex].OptionText.Length)
                obj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questions[questionIndex].OptionText[i];
            if (obj.TryGetComponent(out PopEffect_Junior1A pop)) pop.enabled = true;
            i++;
        }
    }

    public void PlayAudio()
    {
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(StartAudio());
    }

    // Called by each button's OnClick — stores which button the player picked
    public void SetIndex(int index) => _currentOptionIndex = index;

    public void CheckQuestion()
    {
        if (_checkCoroutine != null) return;

        // Guard: index must be valid for the button parent
        if (_currentOptionIndex < 0 || _currentOptionIndex >= _buttonParent.transform.childCount)
        {
            Debug.LogWarning($"CheckQuestion: _currentOptionIndex {_currentOptionIndex} is out of range.");
            return;
        }

        foreach (Transform obj in _buttonParent.transform)
            if (obj.TryGetComponent(out Button btn)) btn.enabled = false;

        _checkCoroutine = StartCoroutine(CheckAnswer(_currentOptionIndex));
    }

    private IEnumerator CheckAnswer(int index)
    {
        // Extra safety — bail if somehow out of bounds
        if (index < 0 || index >= _buttonParent.transform.childCount)
        {
            Debug.LogError($"CheckAnswer: index {index} out of bounds (childCount={_buttonParent.transform.childCount})");
            _checkCoroutine = null;
            yield break;
        }

        bool isCorrect = index == _questions[_currentQuestionIndex].CorrectAnsIndex;
        Transform selectedBtn = _buttonParent.transform.GetChild(index);

        if (isCorrect)
        {
            _audioSource.Stop();
            _starWon++;

            if (selectedBtn.TryGetComponent(out Image img)) img.color = _correctColor;
            if (selectedBtn.TryGetComponent(out PopEffect_Junior1A pop)) { pop.enabled = false; pop.enabled = true; }

            _audioSource.clip = _correctClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);

            if (selectedBtn.TryGetComponent(out Image imgReset)) imgReset.color = Color.white;
        }
        else
        {
            if (selectedBtn.TryGetComponent(out Image img)) img.color = _wrongColor;
            if (selectedBtn.TryGetComponent(out WiggleEffect_Junior1A1 wiggle)) wiggle.enabled = true;

            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);

            if (selectedBtn.TryGetComponent(out Image imgReset)) imgReset.color = Color.white;
        }

        // ── Advance or end ──────────────────────────────────
        if (_currentQuestionIndex < _questions.Length - 1)
        {
            _object.SetActive(false);
            _currentQuestionIndex++;

            PopulateButtons(_currentQuestionIndex);  // uses local counter — safe

            _headingObj.GetComponent<TextMeshProUGUI>().text = _questions[_currentQuestionIndex].HeadingText;
            _object.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questions[_currentQuestionIndex].QuestionText;

            _headingObj.SetActive(false);
            _headingObj.SetActive(true);
            _object.SetActive(true);

            foreach (Transform obj in _buttonParent.transform)
                if (obj.TryGetComponent(out Button btn)) btn.enabled = true;
        }
        else
        {
            _object.SetActive(false);

            // Collect non-star children to show after stars
            System.Collections.Generic.List<GameObject> delayedObjects = new System.Collections.Generic.List<GameObject>();
            for (int i2 = _questions.Length; i2 < _completed.transform.childCount; i2++)
            {
                GameObject childObj = _completed.transform.GetChild(i2).gameObject;
                delayedObjects.Add(childObj);
                childObj.SetActive(false);
            }

            _completed.SetActive(true);

            for (int i2 = 0; i2 < _questions.Length; i2++)
            {
                // Guard against _completed having fewer children than _questions
                if (i2 >= _completed.transform.childCount) break;

                Transform starChild = _completed.transform.GetChild(i2);

                if (i2 < _starWon)
                {
                    if (starChild.TryGetComponent(out Image img)) img.color = _wonStar;
                    if (starChild.TryGetComponent(out PopEffect_Junior1A pop)) pop.enabled = true;

                    _audioSource.clip = _correctClip;
                    _audioSource.Play();
                    _audioSource.pitch += 0.1f;

                    starChild.gameObject.SetActive(true);
                    yield return new WaitForSeconds(_correctClip.length);
                }
                else
                {
                    if (starChild.TryGetComponent(out Image img)) img.color = _loseStar;
                    if (starChild.TryGetComponent(out PopEffect_Junior1A pop)) pop.enabled = true;
                    starChild.gameObject.SetActive(true);
                }
            }

            yield return new WaitForSeconds(0.5f);
            foreach (GameObject obj in delayedObjects) obj.SetActive(true);

            _isViewed = true;
            if (GameManager_Junior1A.Instance != null) GameManager_Junior1A.Instance.Next(true);
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