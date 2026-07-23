using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U14_R01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Global Audio Settings")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _masterIntroClip;
    [SerializeField] private AudioClip[] _clips; // Assign your 8 audio clips here

    [Header("Button Hierarchy Layout")]
    [Tooltip("Drag your 8 bubble/card GameObjects here in order (0 to 7)")]
    [SerializeField] private GameObject[] _clickableBubbleButtons; 

    [Header("Visual Highlighting Colors")]
    [SerializeField] private Color _activeHighlightColor = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);

    [Header("Optional Progress UI Display")]
    [SerializeField] private TextMeshProUGUI _scoreProgressText;

    [Header("State Tracking")]
    [SerializeField] private bool _isViewed = false;
    [SerializeField] private bool _isSlowed = false;

    private int _currentAudioIndex = -1;
    private Coroutine _introRoutine;
    private Coroutine _audioPlaybackRoutine;
    private HashSet<int> _completedIndices = new HashSet<int>();

    public bool IsViewed => _isViewed;

    private void Awake()
    {
        // Wire up the click listeners dynamically to all 8 assigned buttons
        for (int i = 0; i < _clickableBubbleButtons.Length; i++)
        {
            int capturedIndex = i;
            if (_clickableBubbleButtons[i] != null)
            {
                // Check root or children for the Button component
                Button btn = _clickableBubbleButtons[i].GetComponent<Button>();
                if (btn == null) btn = _clickableBubbleButtons[i].GetComponentInChildren<Button>(true);

                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => PlayAudio(capturedIndex));
                }
            }
        }
    }

    private void OnEnable()
    {
        KillAllAudioRoutines();

        _isViewed = false;
        _currentAudioIndex = -1;
        _completedIndices.Clear();

        if (_audioSource != null) _audioSource.pitch = 1.0f;

        _introRoutine = StartCoroutine(Starter());
    }

    private void OnDisable()
    {
        KillAllAudioRoutines();
    }

    private IEnumerator Starter()
    {
        // Wait for frame end to bypass GameManager initialization issues safely
        yield return new WaitForEndOfFrame();

        if (GameManager_Junior1A.Instance != null)
        {
            GameManager_Junior1A.Instance.Next(false);
        }

        // Clean up and reset visual states across all 8 buttons at launch
        foreach (GameObject bubble in _clickableBubbleButtons)
        {
            if (bubble != null)
            {
                bubble.SetActive(true);
                ClearCardHighlightVisuals(bubble.transform);
                
                Button btn = bubble.GetComponent<Button>();
                if (btn == null) btn = bubble.GetComponentInChildren<Button>(true);
                if (btn != null) btn.interactable = true;
            }
        }

        UpdateScoreUI();
        if (_audioSource != null) _audioSource.pitch = _isSlowed ? 0.75f : 1.0f;

        // Play introduction audio asset logic if assigned
        if (_masterIntroClip != null && _audioSource != null)
        {
            _audioSource.clip = _masterIntroClip;
            _audioSource.Play();
            
            float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            yield return new WaitForSeconds(_masterIntroClip.length / currentPitch);
        }

        _introRoutine = null;
    }

    public void PlayAudio(int index)
    {
        if (index < 0 || index >= _clickableBubbleButtons.Length) return;

        // Cut off any active speech clip playback and reset visuals of the last talking button
        if (_audioPlaybackRoutine != null) StopCoroutine(_audioPlaybackRoutine);
        if (_audioSource != null) _audioSource.Stop();

        if (_currentAudioIndex != -1 && _currentAudioIndex < _clickableBubbleButtons.Length)
        {
            if (_clickableBubbleButtons[_currentAudioIndex] != null)
            {
                ClearCardHighlightVisuals(_clickableBubbleButtons[_currentAudioIndex].transform);
            }
        }

        _currentAudioIndex = index;
        _audioPlaybackRoutine = StartCoroutine(StartButtonAudioSequence(index));
    }

    private IEnumerator StartButtonAudioSequence(int index)
    {
        Transform currentCard = _clickableBubbleButtons[index].transform;

        // Apply active highlight colors
        if (currentCard.TryGetComponent(out Image cardImg)) cardImg.color = _activeHighlightColor;
        
        // Handle child structural frame highlight toggles
        if (currentCard.childCount > 0 && currentCard.GetChild(0).childCount > 0)
        {
            if (currentCard.GetChild(0).GetChild(0).TryGetComponent(out Image structuralFrame))
            {
                structuralFrame.enabled = true;
            }
        }

        // Trigger Pop animation effect cleanly
        if (currentCard.TryGetComponent(out PopEffect_Junior1A pop))
        {
            pop.enabled = false;
            pop.enabled = true;
        }

        // Play clip line
        if (index < _clips.Length && _clips[index] != null && _audioSource != null)
        {
            _audioSource.clip = _clips[index];
            _audioSource.Play();

            float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            yield return new WaitForSeconds(_clips[index].length / currentPitch);
        }

        // Visual clean up back to white
        ClearCardHighlightVisuals(currentCard);

        // Progress score tracking updates
        if (!_completedIndices.Contains(index))
        {
            _completedIndices.Add(index);
            UpdateScoreUI();
            CheckGlobalWinCondition();
        }

        _audioPlaybackRoutine = null;
    }

    private void ClearCardHighlightVisuals(Transform cardTransform)
    {
        if (cardTransform == null) return;

        if (cardTransform.TryGetComponent(out Image cardImg)) cardImg.color = Color.white;
        
        if (cardTransform.childCount > 0 && cardTransform.GetChild(0).childCount > 0)
        {
            if (cardTransform.GetChild(0).GetChild(0).TryGetComponent(out Image structuralFrame))
            {
                structuralFrame.enabled = false;
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (_scoreProgressText != null)
        {
            _scoreProgressText.text = $"{_completedIndices.Count} / {_clickableBubbleButtons.Length}";
        }
    }

    private void CheckGlobalWinCondition()
    {
        if (_isViewed) return;

        // If all 8 discrete button elements have been clicked at least once
        if (_completedIndices.Count >= _clickableBubbleButtons.Length && _clickableBubbleButtons.Length > 0)
        {
            _isViewed = true;
            if (GameManager_Junior1A.Instance != null)
            {
                GameManager_Junior1A.Instance.Next(true);
            }
        }
    }

    public void Slow(TextMeshProUGUI text)
    {
        _isSlowed = !_isSlowed;
        if (text != null) text.text = _isSlowed ? "    FAST" : "    SLOW";
        if (_audioSource != null) _audioSource.pitch = _isSlowed ? 0.75f : 1f;
    }

    private void KillAllAudioRoutines()
    {
        if (_introRoutine != null) StopCoroutine(_introRoutine);
        if (_audioPlaybackRoutine != null) StopCoroutine(_audioPlaybackRoutine);
        if (_audioSource != null) _audioSource.Stop();

        _introRoutine = null;
        _audioPlaybackRoutine = null;
    }
}