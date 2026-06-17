using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U12_W01_Junior1A_QuestionData
{
    [Tooltip("Text shown in the LEFT character's speech bubble")]
    public string QuestionText;

    [Tooltip("All answer options shown in the bottom spawn box")]
    public string[] OptionTexts;

    [Tooltip("Index of the correct option. Set to -1 to accept any answer.")]
    public int CorrectOptionIndex;
}

public class U12_W01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    // ── Audio ──────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip   _introClip, _incorrectClip, _correctClip;

    // ── Scene References ───────────────────────
    [Header("Conversation Parents")]
    [Tooltip("The RIGHT parent that holds all RighChar objects")]
    [SerializeField] private Transform _rightCharParent;

    [Tooltip("The LEFTCHAR parent that holds all LeftChar objects")]
    [SerializeField] private Transform _leftCharParent;

    [Tooltip("The panel at the bottom holding the 3 answer-option buttons")]
    [SerializeField] private Transform _spawnBox;

    // ── Progress UI ────────────────────────────
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _progressText;

    // ── Question Data ──────────────────────────
    [Header("Data")]
    [SerializeField] private U12_W01_Junior1A_QuestionData[] _questionData;

    // ── Visual Feedback ────────────────────────
    [Header("Colors")]
    [SerializeField] private Color _correctColor = new Color(0.6f, 1f, 0.6f);
    [SerializeField] private Color _wrongColor   = new Color(1f, 0.5f, 0.5f);

    // ── Placeholder shown before the player answers ──
    [Header("Answer Placeholder")]
    [Tooltip("Text displayed in the right char box before it is answered")]
    [SerializeField] private string _placeholderText = "••••••••••";

    // ── Runtime State ──────────────────────────
    private int      _completedCount        = 0;
    private int      _selectedQuestionIndex = -1;
    private int      _selectedOptionIndex   = 0;
    private bool     _isViewed              = false;
    private Coroutine _coroutine;

    private TextMeshProUGUI[] _rightAnswerTexts;
    private Button[]          _rightAnswerButtons;
    private Button[]          _spawnBoxButtons; // 🔧 Added to cache spawnbox buttons cleanly

    public bool IsViewed => _isViewed;

    private void Awake()
    {
        // 🔧 FIXED: Wire spawnbox buttons ONCE here so listeners never double-register or stack up
        int btnCount = _spawnBox.childCount;
        _spawnBoxButtons = new Button[btnCount];
        
        for (int i = 0; i < btnCount; i++)
        {
            _spawnBoxButtons[i] = _spawnBox.GetChild(i).GetComponent<Button>();
            if (_spawnBoxButtons[i] != null)
            {
                int indexCaptured = i;
                _spawnBoxButtons[i].onClick.RemoveAllListeners();
                _spawnBoxButtons[i].onClick.AddListener(() => ChooseOption(indexCaptured));
            }
        }
    }

    private void OnEnable() => StartCoroutine(Starter());

    private IEnumerator Starter()
    {
        _completedCount        = 0;
        _selectedQuestionIndex = -1;
        _coroutine             = null;
        _isViewed              = false;

        GameManager_Junior1A.Instance?.Next(false);

        foreach (Button btn in _spawnBoxButtons)
            if (btn != null) btn.gameObject.SetActive(false);

        List<Transform> leftChars  = new List<Transform>();
        List<Transform> rightChars = new List<Transform>();

        foreach (Transform t in _leftCharParent)  leftChars.Add(t);
        foreach (Transform t in _rightCharParent) rightChars.Add(t);

        int count = Mathf.Min(_questionData.Length, leftChars.Count, rightChars.Count);

        _rightAnswerTexts   = new TextMeshProUGUI[count];
        _rightAnswerButtons = new Button[count];

        for (int i = 0; i < count; i++)
        {
            var leftTMP = leftChars[i].GetComponentInChildren<TextMeshProUGUI>();
            if (leftTMP != null) leftTMP.text = _questionData[i].QuestionText;
            leftChars[i].gameObject.SetActive(false);

            _rightAnswerTexts[i]   = GetAnswerTMP(rightChars[i]);
            _rightAnswerButtons[i] = GetAnswerBtn(rightChars[i]);

            if (_rightAnswerTexts[i]  != null) _rightAnswerTexts[i].text = _placeholderText;

            if (_rightAnswerButtons[i] != null)
            {
                int captured = i;
                _rightAnswerButtons[i].onClick.RemoveAllListeners();
                _rightAnswerButtons[i].onClick.AddListener(() => OnAnswerBoxClicked(captured));
                _rightAnswerButtons[i].interactable = true;
            }

            rightChars[i].gameObject.SetActive(false);
        }

        for (int i = count; i < leftChars.Count;  i++) leftChars[i].gameObject.SetActive(false);
        for (int i = count; i < rightChars.Count; i++) rightChars[i].gameObject.SetActive(false);

        UpdateProgress();
        PlayClip(_introClip);

        for (int i = 0; i < count; i++)
        {
            yield return new WaitForSeconds(0.25f);
            leftChars[i].gameObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            rightChars[i].gameObject.SetActive(true);
        }
    }

    // ══════════════════════════════════════════
    //  PLAYER CLICKS A RIGHT-CHAR ANSWER BOX
    // ══════════════════════════════════════════
    private void OnAnswerBoxClicked(int questionIndex)
    {
        if (_completedCount >= _questionData.Length) return;

        if (_rightAnswerButtons[questionIndex] != null &&
            !_rightAnswerButtons[questionIndex].interactable) return;

        _selectedQuestionIndex = questionIndex;
        _selectedOptionIndex   = 0;

        int optCount = _questionData[questionIndex].OptionTexts.Length;

        // 🔧 FIXED: Only modify text, colors, and visibility here. Do not touch onClick listeners!
        for (int i = 0; i < _spawnBoxButtons.Length; i++)
        {
            Button button = _spawnBoxButtons[i];
            if (button == null) continue;

            if (i < optCount)
            {
                var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = _questionData[questionIndex].OptionTexts[i];

                var img = button.GetComponent<Image>();
                if (img != null) img.color = Color.white;

                button.interactable = true;

                var pop = button.GetComponent<PopEffect_Junior1A>();
                if (pop != null) pop.enabled = true;

                button.gameObject.SetActive(true);
            }
            else
            {
                button.gameObject.SetActive(false);
            }
        }
    }

    // ══════════════════════════════════════════
    //  PLAYER SELECTS AN OPTION
    // ══════════════════════════════════════════
    public void ChooseOption(int index)
    {
        // Safety guard check to ensure multiple physical screen taps don't double fire
        if (_selectedQuestionIndex < 0 || !this.enabled) return; 

        _selectedOptionIndex = index;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(CheckOption());
    }

    // ══════════════════════════════════════════
    //  CHECK ANSWER
    // ══════════════════════════════════════════
    private IEnumerator CheckOption()
    {
        if (_selectedQuestionIndex < 0) yield break;

        U12_W01_Junior1A_QuestionData data = _questionData[_selectedQuestionIndex];
        bool isCorrect = data.CorrectOptionIndex < 0 || _selectedOptionIndex == data.CorrectOptionIndex;

        SetSpawnButtonColor(_selectedOptionIndex, isCorrect ? _correctColor : _wrongColor);

        if (isCorrect)
        {
            SetSpawnButtonsInteractable(false);

            if (_rightAnswerTexts[_selectedQuestionIndex] != null)
            {
                _rightAnswerTexts[_selectedQuestionIndex].text = data.OptionTexts[_selectedOptionIndex];

                var popTMP = _rightAnswerTexts[_selectedQuestionIndex].GetComponent<TextPopEffect_Junior1A>();
                if (popTMP != null) popTMP.enabled = true;
            }

            if (_rightAnswerButtons[_selectedQuestionIndex] != null)
                _rightAnswerButtons[_selectedQuestionIndex].interactable = false;

            PlayClip(_correctClip);

            _completedCount++;
            UpdateProgress();

            // Clear active context index so double submissions cannot trigger
            _selectedQuestionIndex = -1; 

            yield return new WaitForSeconds(0.5f);

            foreach (Button btn in _spawnBoxButtons)
                if (btn != null) btn.gameObject.SetActive(false);

            if (_completedCount >= _questionData.Length)
            {
                _isViewed = true;
                GameManager_Junior1A.Instance?.Next(true);
            }
        }
        else
        {
            PlayClip(_incorrectClip);

            if (_selectedOptionIndex < _spawnBoxButtons.Length && _spawnBoxButtons[_selectedOptionIndex] != null)
            {
                var wiggle = _spawnBoxButtons[_selectedOptionIndex].GetComponent<WiggleEffect_Junior1A1>();
                if (wiggle != null) wiggle.enabled = true;
            }

            SetSpawnButtonsInteractable(false);

            float waitTime = (_incorrectClip != null) ? _incorrectClip.length : 1f;
            yield return new WaitForSeconds(waitTime);

            for (int i = 0; i < _spawnBoxButtons.Length; i++)
            {
                Button button = _spawnBoxButtons[i];
                if (button == null || !button.gameObject.activeSelf) continue;

                var img = button.GetComponent<Image>();
                if (img != null) img.color = Color.white;
                button.interactable = true;
            }
        }
    }

    // ══════════════════════════════════════════
    //  HELPERS & UTILITIES
    // ══════════════════════════════════════════
    private TextMeshProUGUI GetAnswerTMP(Transform rightChar)
    {
        var mask = rightChar.Find("BGGirID/Mask");
        if (mask != null)
        {
            var tmp = mask.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }
        return rightChar.GetComponentInChildren<TextMeshProUGUI>();
    }

    private Button GetAnswerBtn(Transform rightChar)
    {
        var bg = rightChar.Find("BGGirID");
        if (bg != null)
        {
            var btn = bg.GetComponent<Button>();
            if (btn != null) return btn;
        }
        return rightChar.GetComponentInChildren<Button>();
    }

    private void SetSpawnButtonColor(int index, Color color)
    {
        if (index >= 0 && index < _spawnBoxButtons.Length && _spawnBoxButtons[index] != null)
        {
            var img = _spawnBoxButtons[index].GetComponent<Image>();
            if (img != null) img.color = color;
        }
    }

    private void SetSpawnButtonsInteractable(bool state)
    {
        foreach (Button btn in _spawnBoxButtons)
        {
            if (btn != null) btn.interactable = state;
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || _audioSource == null) return;
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    private void UpdateProgress()
    {
        if (_progressText != null)
        {
            _progressText.text = $"{_completedCount}/{_questionData.Length}";
        }
    }
}