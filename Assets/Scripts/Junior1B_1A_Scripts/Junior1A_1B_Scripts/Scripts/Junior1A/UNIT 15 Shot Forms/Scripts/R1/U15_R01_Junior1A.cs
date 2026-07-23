using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ReviewRowData
{
    [Header("Text Configuration")]
    public string LongFormText;
    public string ShortFormText;

    [Header("Audio Tracks")]
    public AudioClip LongFormAudio;
    public AudioClip BothAudio;
    public AudioClip ShortFormAudio;

    [Header("Button References (Horizontal Row)")]
    public Button LongFormButton;
    public Button BothButton;
    public Button ShortFormButton;
}

public class U15_R01_Junior1A : MonoBehaviour
{
    [Header("Audio Components")]
    [SerializeField] private AudioSource _audioSource;
    [Tooltip("Intro clip that plays right when the review screen loads before allowing interaction.")]
    [SerializeField] private AudioClip _introAudio;

    [Header("Review Data Configuration")]
    [Tooltip("Create rows to match your sentences. Each row manages its 3 unique horizontal buttons.")]
    [SerializeField] private ReviewRowData[] _reviewRows;

    [Header("Visual Feedback (Colors)")]
    [SerializeField] private Color _normalColor = Color.white;
    [Tooltip("The default base color for all middle (Both) buttons.")]
    [SerializeField] private Color _middleButtonDefaultColor = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
    [Tooltip("The color a button turns when it is clicked and playing audio.")]
    [SerializeField] private Color _selectedPlayColor = Color.green;
    [Tooltip("The color a button turns after it has been clicked at least once.")]
    [SerializeField] private Color _visitedColor = new Color(0.75f, 0.75f, 0.75f, 1.0f);

    [Header("Score Progression Metrics")]
    [Tooltip("If true, every single button must be clicked. If false, all 3 buttons in each row must be clicked before that row counts.")]
    [SerializeField] private bool _requireEverySingleButton = false;
    [SerializeField] private int _currentScore = 0;
    [SerializeField] private int _targetRequiredScore = 0;

    [Header("Optional Score UI Display")]
    [Tooltip("Optional text field to show current clicked buttons score progress (e.g., '0 / 8')")]
    [SerializeField] private TextMeshProUGUI _scoreProgressText;

    private bool _isSlowed = false;
    private bool _interactionAllowed = false;
    private bool _taskCompleted = false;
    private Coroutine _audioTrackingCoroutine;
    private Transform _currentlyActiveButtonTransform;

    // Tracks which individual buttons have been clicked at least once
    private HashSet<Transform> _completedReviewButtons = new HashSet<Transform>();
    // Tracks which rows have had ALL their buttons clicked
    private HashSet<int> _completedRows = new HashSet<int>();

    // Stores each button's resting color (grey once visited, original if not)
    private Dictionary<Transform, Color> _buttonRestingColors = new Dictionary<Transform, Color>();

    private void Start()
    {
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

        CalculateRequiredScoreTargets();
        InitializeReviewLayout();
        UpdateScoreUI();
        StartCoroutine(PlayIntroThenEnableInteractionRoutine());
    }

    private void CalculateRequiredScoreTargets()
    {
        if (_reviewRows == null) return;

        _currentScore = 0;
        _taskCompleted = false;

        if (_requireEverySingleButton)
        {
            // Every individual button must be clicked
            int totalButtons = 0;
            foreach (var row in _reviewRows)
            {
                if (row == null) continue;
                if (row.LongFormButton != null) totalButtons++;
                if (row.BothButton != null) totalButtons++;
                if (row.ShortFormButton != null) totalButtons++;
            }
            _targetRequiredScore = totalButtons;
        }
        else
        {
            // One point per row, earned only when ALL buttons in that row are clicked
            _targetRequiredScore = _reviewRows.Length;
        }
    }

    private void InitializeReviewLayout()
    {
        if (_reviewRows == null) return;

        for (int i = 0; i < _reviewRows.Length; i++)
        {
            var row = _reviewRows[i];
            if (row == null) continue;

            int rowIndex = i;

            if (row.LongFormButton != null)
            {
                TMP_Text txt = row.LongFormButton.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = row.LongFormText;

                ResetButtonToDefaultVisual(row.LongFormButton.transform, false);
                _buttonRestingColors[row.LongFormButton.transform] = _normalColor;

                row.LongFormButton.onClick.RemoveAllListeners();
                row.LongFormButton.onClick.AddListener(() => OnReviewButtonClicked(row.LongFormButton.transform, row.LongFormAudio, false, rowIndex));
            }

            if (row.BothButton != null)
            {
                ResetButtonToDefaultVisual(row.BothButton.transform, true);
                _buttonRestingColors[row.BothButton.transform] = _middleButtonDefaultColor;

                row.BothButton.onClick.RemoveAllListeners();
                row.BothButton.onClick.AddListener(() => OnReviewButtonClicked(row.BothButton.transform, row.BothAudio, true, rowIndex));
            }

            if (row.ShortFormButton != null)
            {
                TMP_Text txt = row.ShortFormButton.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = row.ShortFormText;

                ResetButtonToDefaultVisual(row.ShortFormButton.transform, false);
                _buttonRestingColors[row.ShortFormButton.transform] = _normalColor;

                row.ShortFormButton.onClick.RemoveAllListeners();
                row.ShortFormButton.onClick.AddListener(() => OnReviewButtonClicked(row.ShortFormButton.transform, row.ShortFormAudio, false, rowIndex));
            }
        }
    }

    private IEnumerator PlayIntroThenEnableInteractionRoutine()
    {
        _interactionAllowed = false;
        GameManager_Junior1A.Instance?.Next(false);

        if (_introAudio != null && _audioSource != null)
        {
            _audioSource.clip = _introAudio;
            _audioSource.pitch = _isSlowed ? 0.75f : 1.0f;
            _audioSource.Play();

            float pitchVal = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            yield return new WaitForSeconds((_introAudio.length / pitchVal) + 0.2f);
        }

        _interactionAllowed = true;
    }

    private void OnReviewButtonClicked(Transform clickedButton, AudioClip targetClip, bool isMiddleButton, int rowIndex)
    {
        if (!_interactionAllowed || targetClip == null || _audioSource == null) return;

        // Score tracked before any yield so fast re-clicks can't skip it
        EvaluateScoreTrackingProgress(clickedButton, rowIndex);

        if (_audioTrackingCoroutine != null)
        {
            StopCoroutine(_audioTrackingCoroutine);
            if (_currentlyActiveButtonTransform != null)
                RestoreButtonToRestingColor(_currentlyActiveButtonTransform);
        }

        _currentlyActiveButtonTransform = clickedButton;
        _audioTrackingCoroutine = StartCoroutine(PlayAudioAndTrackVisualsRoutine(clickedButton, targetClip));
    }

    private void EvaluateScoreTrackingProgress(Transform clickedButton, int rowIndex)
    {
        if (_taskCompleted) return;

        // Always mark the individual button as visited for grey coloring
        if (!_completedReviewButtons.Contains(clickedButton))
        {
            _completedReviewButtons.Add(clickedButton);
            _buttonRestingColors[clickedButton] = _visitedColor;
        }

        if (_requireEverySingleButton)
        {
            // In this mode each newly visited button is worth 1 point
            // We count directly from the HashSet size to avoid double-counting
            int newScore = _completedReviewButtons.Count;
            if (newScore != _currentScore)
            {
                _currentScore = newScore;
                UpdateScoreUI();
            }
        }
        else
        {
            // Row only scores once ALL 3 buttons in that row have been clicked
            if (!_completedRows.Contains(rowIndex) && IsRowFullyClicked(rowIndex))
            {
                _completedRows.Add(rowIndex);
                _currentScore++;
                UpdateScoreUI();
            }
        }

        if (_currentScore >= _targetRequiredScore)
        {
            _taskCompleted = true;
            GameManager_Junior1A.Instance?.Next(true);
        }
    }

    /// <summary>
    /// Returns true only when every assigned button in the given row has been visited.
    /// </summary>
    private bool IsRowFullyClicked(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _reviewRows.Length) return false;

        var row = _reviewRows[rowIndex];
        if (row == null) return false;

        if (row.LongFormButton != null && !_completedReviewButtons.Contains(row.LongFormButton.transform)) return false;
        if (row.BothButton != null    && !_completedReviewButtons.Contains(row.BothButton.transform))    return false;
        if (row.ShortFormButton != null && !_completedReviewButtons.Contains(row.ShortFormButton.transform)) return false;

        return true;
    }

    private IEnumerator PlayAudioAndTrackVisualsRoutine(Transform buttonTransform, AudioClip clip)
    {
        if (buttonTransform.TryGetComponent(out Image btnImg))
            btnImg.color = _selectedPlayColor;

        SetNestedHighlightActive(buttonTransform, true);

        _audioSource.Stop();
        _audioSource.clip = clip;
        _audioSource.pitch = _isSlowed ? 0.75f : 1.0f;
        _audioSource.Play();

        float pitchVal = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        yield return new WaitForSeconds(clip.length / pitchVal);

        // Restore to resting color — grey if visited, original if not
        RestoreButtonToRestingColor(buttonTransform);

        _audioTrackingCoroutine = null;
        _currentlyActiveButtonTransform = null;
    }

    private void RestoreButtonToRestingColor(Transform buttonTransform)
    {
        if (buttonTransform.TryGetComponent(out Image btnImg))
        {
            btnImg.color = _buttonRestingColors.TryGetValue(buttonTransform, out Color restingColor)
                ? restingColor
                : _normalColor;
        }
        SetNestedHighlightActive(buttonTransform, false);
    }

    private void UpdateScoreUI()
    {
        if (_scoreProgressText != null)
            _scoreProgressText.text = $"{_currentScore} / {_targetRequiredScore}";
    }

    public void Slow(TextMeshProUGUI textElement)
    {
        _isSlowed = !_isSlowed;
        if (textElement != null)
            textElement.text = _isSlowed ? "    FAST" : "    SLOW";

        if (_audioSource != null && _audioSource.isPlaying)
            _audioSource.pitch = _isSlowed ? 0.75f : 1.0f;
    }

    private void ResetButtonToDefaultVisual(Transform buttonTransform, bool isMiddleButton)
    {
        if (buttonTransform.TryGetComponent(out Image btnImg))
            btnImg.color = isMiddleButton ? _middleButtonDefaultColor : _normalColor;

        SetNestedHighlightActive(buttonTransform, false);
    }

    private void SetNestedHighlightActive(Transform targetCard, bool isEnabled)
    {
        if (targetCard.childCount > 0)
        {
            Transform firstChild = targetCard.GetChild(0);
            if (firstChild.childCount > 0)
            {
                Transform nestedIndicator = firstChild.GetChild(0);
                if (nestedIndicator.TryGetComponent(out Image img))
                    img.enabled = isEnabled;
            }
        }
    }
}