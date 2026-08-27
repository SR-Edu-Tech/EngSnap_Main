using System;
using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U10_W01_Junior1B_QuestionData
{
    public AudioClip QuestionClip, AnswerClip;
    public string QuestionText, AnswerText;
    public string[] AnswerTexts;
    public string[] OptionText;
}

public class U10_W01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _incorrectClip, _correctClip;
    [SerializeField] Transform _spawnBox, _answerBox, _checkBox;
    [SerializeField] TextMeshProUGUI _questionText;
    [SerializeField] U5_W01_Junior1B_QuestionData[] _questionData;
    [SerializeField] int _currentQuestionIndex = 0;
    [SerializeField] string _currentAnswerText = "";
    [SerializeField] bool _isViewed = false;

    // 💡 Keep track of the default color of your button prefabs
    [SerializeField] private Color _defaultButtonColor = Color.white;

    private Coroutine _coroutine;
    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        if (_checkBox != null) _checkBox.gameObject.SetActive(false);
        if (_questionText != null) _questionText.gameObject.SetActive(false);
        _coroutine = null;
        _currentQuestionIndex = 0;
        _currentAnswerText = string.Empty;

        ResetAllButtonsToSpawnBox();

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length + 1f);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        ShowCurrentQuestion();
    }

    private void ShowCurrentQuestion()
    {
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length) return;

        if (_questionText != null)
        {
            _questionText.text = _questionData[_currentQuestionIndex].QuestionText;
            _questionText.gameObject.SetActive(true);
        }

        if (_audioSource != null && _questionData[_currentQuestionIndex].QuestionClip != null)
        {
            _audioSource.clip = _questionData[_currentQuestionIndex].QuestionClip;
            _audioSource.Play();
        }

        if (_checkBox != null && _checkBox.TryGetComponent(out Button btn))
        {
            btn.interactable = true;
        }

        StartCoroutine(SpawnOptionsRoutine());
    }

    IEnumerator SpawnOptionsRoutine()
    {
        if (_spawnBox == null || _questionData == null || _currentQuestionIndex >= _questionData.Length) yield break;

        string[] currentOptions = _questionData[_currentQuestionIndex].OptionText;
        if (currentOptions == null) yield break;

        if (_spawnBox.childCount < currentOptions.Length)
        {
            Debug.LogError("❌ Not enough prefab child buttons instantiated under SpawnBox to display this question's options!");
            yield break;
        }

        for (int i = 0; i < currentOptions.Length; i++)
        {
            Transform choiceButton = _spawnBox.GetChild(i);
            choiceButton.localScale = Vector3.one;

            // Reset box color to default when spawning
            if (choiceButton.TryGetComponent(out Image img))
            {
                img.color = _defaultButtonColor;
            }

            if (choiceButton.childCount > 0)
            {
                var textComponent = choiceButton.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = currentOptions[i];
                }
            }

            choiceButton.gameObject.SetActive(true);

            if (choiceButton.TryGetComponent(out Popeffect_Junior1B pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void GetData(Transform clickedButton)
    {
        if (_spawnBox == null || _answerBox == null || _checkBox == null) return;
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length) return;

        if (clickedButton.parent == _spawnBox)
            clickedButton.SetParent(_answerBox, false);
        else
            clickedButton.SetParent(_spawnBox, false);

        clickedButton.localScale = Vector3.one;

        // 💡 Reset box color back to normal if user deselects/moves the button
        if (clickedButton.TryGetComponent(out Image img))
        {
            img.color = _defaultButtonColor;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_spawnBox as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_answerBox as RectTransform);

        if (_answerBox.childCount > 0)
            _checkBox.gameObject.SetActive(true);
        else
            _checkBox.gameObject.SetActive(false);
    }

    public void CheckData()
    {
        if (_answerBox == null || _answerBox.childCount <= 0 || _coroutine != null) return;
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length) return;

        _currentAnswerText = "";

        for (int i = 0; i < _answerBox.childCount; i++)
        {
            Transform child = _answerBox.GetChild(i);
            if (child.childCount > 0 && child.GetChild(0).TryGetComponent(out TextMeshProUGUI tmp))
            {
                string childText = tmp.text.Trim();
                _currentAnswerText += childText;
            }
        }

        _coroutine = StartCoroutine(CheckDataValidity());
    }

    IEnumerator CheckDataValidity()
    {
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length || _checkBox == null || _answerBox == null) yield break;

        string cleanedUserInput = Regex.Replace(_currentAnswerText, @"\s+", "").Trim();
        string targetAnswerRaw = _questionData[_currentQuestionIndex].AnswerText != null ? _questionData[_currentQuestionIndex].AnswerText : "";
        string cleanedTargetAnswer = Regex.Replace(targetAnswerRaw, @"\s+", "").Trim();

        bool isCorrect = cleanedUserInput.Equals(cleanedTargetAnswer, StringComparison.OrdinalIgnoreCase);

        // 💡 NEW FEATURE: Turn the button BOX images green or red depending on individual correct position
        for (int i = 0; i < _answerBox.childCount; i++)
        {
            Transform child = _answerBox.GetChild(i);
            if (child.TryGetComponent(out Image buttonImage) && child.childCount > 0 && child.GetChild(0).TryGetComponent(out TextMeshProUGUI letterText))
            {
                string currentLetter = letterText.text.Trim();

                // Check if this letter matches the target answer string character at this identical spot
                if (i < cleanedTargetAnswer.Length && currentLetter.Equals(cleanedTargetAnswer[i].ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    buttonImage.color = Color.green; // Correct slot choice -> Turn Box Green
                }
                else
                {
                    buttonImage.color = Color.red; // Mistaken slot choice -> Turn Box Red
                }
            }
        }

        if (isCorrect)
        {
            _checkBox.gameObject.SetActive(false);

            foreach (Transform obj in _answerBox)
            {
                if (obj.TryGetComponent(out Button btn)) btn.interactable = false;
            }

            if (_answerBox.TryGetComponent(out Popeffect_Junior1B pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }

            if (_audioSource != null)
            {
                if (_correctClip != null)
                {
                    _audioSource.clip = _correctClip;
                    _audioSource.Play();
                    yield return new WaitForSeconds(_correctClip.length);
                }

                if (_questionData[_currentQuestionIndex].AnswerClip != null)
                {
                    _audioSource.clip = _questionData[_currentQuestionIndex].AnswerClip;
                    _audioSource.Play();
                    yield return new WaitForSeconds(_audioSource.clip.length);
                }
            }

            _currentQuestionIndex++;
            _currentAnswerText = string.Empty;

            if (_currentQuestionIndex < _questionData.Length)
            {
                ResetAllButtonsToSpawnBox();
                if (_questionText != null) _questionText.gameObject.SetActive(false);
                ShowCurrentQuestion();
            }
            else
            {
                if (_checkBox.TryGetComponent(out Button btn)) btn.interactable = false;
                _isViewed = true;
                if (GameManager_Junior1B.Instance != null)
                {
                    GameManager_Junior1B.Instance.Next(true);
                }
            }
        }
        else
        {
            if (_audioSource != null && _incorrectClip != null)
            {
                _audioSource.clip = _incorrectClip;
                _audioSource.Play();
            }

            if (_answerBox.TryGetComponent(out WiggleEffect_Junior1B wiggle))
            {
                wiggle.enabled = false;
                wiggle.enabled = true;
            }

            if (_incorrectClip != null) yield return new WaitForSeconds(_incorrectClip.length);
            else yield return new WaitForSeconds(0.5f);
        }

        _coroutine = null;
    }

    private void ResetAllButtonsToSpawnBox()
    {
        if (_answerBox == null || _spawnBox == null) return;

        while (_answerBox.childCount > 0)
        {
            Transform child = _answerBox.GetChild(0);
            if (child.TryGetComponent(out Button btn)) btn.interactable = true;

            // Reset box color to default
            if (child.TryGetComponent(out Image img))
            {
                img.color = _defaultButtonColor;
            }

            child.SetParent(_spawnBox);
            child.localScale = Vector3.one;
            child.gameObject.SetActive(false);
        }

        foreach (Transform child in _spawnBox)
        {
            child.gameObject.SetActive(false);
            if (child.TryGetComponent(out Button btn)) btn.interactable = true;
            child.localScale = Vector3.one;

            // Reset box color to default
            if (child.TryGetComponent(out Image img))
            {
                img.color = _defaultButtonColor;
            }
        }
    }
}