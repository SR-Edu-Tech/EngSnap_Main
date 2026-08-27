using Junior2A;
using System.Collections;
using System.Collections.Generic; // Required for HashSet tracking
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U3_R01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [Header("Audio Engine Setup")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip[] _clips;

    [Header("Layout Target References")]
    [SerializeField] private Transform _cardParent; // Linked to ButtonParent in Inspector
    [SerializeField] private TextMeshProUGUI _scoreText; // TextMeshPro field to display score visibility

    [Header("Sprite Alternator Integration")]
    [SerializeField] private Image _targetImage;       // The Image component that switches sprites
    [SerializeField] private Sprite _evenSprite;       // Sprite used when index is even (0, 2, 4...)
    [SerializeField] private Sprite _oddSprite;        // Sprite used when index is odd (1, 3, 5...)

    [Header("Score and Progression Matrices")]
    [SerializeField] private int _totalClickedScore = 0; // Increments on each unique button press

    [Header("Runtime State Matrices")]
    [SerializeField] private int _currentAudioIndex = 0;
    [SerializeField] private bool _isViewed = false;
    [SerializeField] private bool _isSlowed = false;

    private Coroutine _coroutine;
    private Coroutine _repeatCoroutine;

    // Tracks unique card index registrations to prevent clicking the same card twice to cheat the progression
    private HashSet<int> _clickedIndicesIndices = new HashSet<int>();

    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(Starter());

    private IEnumerator Starter()
    {
        if (_cardParent == null)
        {
            Debug.LogError("❌ Card Parent reference is completely missing in inspector allocation fields!");
            yield break;
        }

        // Reset score trackers
        _totalClickedScore = 0;
        _clickedIndicesIndices.Clear();
        UpdateScoreDisplay();

        // Hide all cards initially
        foreach (Transform button in _cardParent) button.gameObject.SetActive(false);

        // Explicitly hide footer target layouts on game execution start
        if (transform.childCount > 0)
        {
            Transform footer = transform.GetChild(transform.childCount - 1);
            if (footer.childCount > 1) footer.GetChild(1).gameObject.SetActive(false);
        }

        _currentAudioIndex = 0;
        UpdateSpriteByAudioIndex();

        // Play introduction audio if assigned
        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }

        // Sequentially spawn all card layout wrappers into play view matrix with a pop effect
        foreach (Transform button in _cardParent)
        {
            button.gameObject.SetActive(true);

            if (button.TryGetComponent(out PopEffect_Junior2A pop))
            {
                pop.enabled = false; // Reset if already active
                pop.enabled = true;
                yield return new WaitForSeconds(pop.PopDuration + 0.15f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f); // Fallback if component is missing
            }
        }

        // Directly enable interactions so the user has to click buttons manually
        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out Button btn)) btn.interactable = true;
        }
    }

    public void PlayAudio(int index)
    {
        if (_cardParent == null) return;

        // 1. INSTANT VISUALS: Pop the button and flip the sprite immediately on click
        TriggerPopAnimation(index);
        _currentAudioIndex = index;
        UpdateSpriteByAudioIndex();

        // 2. INSTANT SCORE: Add score immediately if it's a new button click
        if (!_clickedIndicesIndices.Contains(index))
        {
            _clickedIndicesIndices.Add(index);
            _totalClickedScore++;
            UpdateScoreDisplay();

            // Check win condition instantly
            CheckCompletionStatus();
        }

        // 3. AUDIO MANAGEMENT: Handle the audio playback non-blocking stream
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(StartButtonAudio());
    }

    private void CheckCompletionStatus()
    {
        // Only run when the complete unique button child array count matches our score sequence
        if (_totalClickedScore >= _cardParent.childCount)
        {
            if (transform.childCount > 0)
            {
                Transform footer = transform.GetChild(transform.childCount - 1);
                if (footer.childCount > 1)
                {
                    GameObject nextButton = footer.GetChild(1).gameObject;

                    // Trigger pop effect logic on next button appearance if applicable
                    if (nextButton.TryGetComponent(out PopEffect_Junior2A pop))
                    {
                        pop.enabled = false;
                        pop.enabled = true;
                    }

                    nextButton.SetActive(true);
                }
            }

            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            _isViewed = true;
        }
    }

    private void UpdateScoreDisplay()
    {
        if (_scoreText != null && _cardParent != null)
        {
            _scoreText.text = $"Score: {_totalClickedScore} / {_cardParent.childCount}";
        }
    }

    private IEnumerator StartButtonAudio()
    {
        if (_clips != null && _currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null && _audioSource != null)
        {
            _audioSource.clip = _clips[_currentAudioIndex];
            _audioSource.Play();

            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _clips[_currentAudioIndex].length / pV1;
            yield return new WaitForSeconds(aL1);
        }
    }

    public void Repeat()
    {
        if (_cardParent == null) return;

        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);

        _repeatCoroutine = StartCoroutine(RepeatAudio());
    }

    private IEnumerator RepeatAudio()
    {
        _currentAudioIndex = 0;
        foreach (Transform button in _cardParent)
        {
            TriggerPopAnimation(_currentAudioIndex);
            UpdateSpriteByAudioIndex();

            if (_clips != null && _currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null && _audioSource != null)
            {
                _audioSource.clip = _clips[_currentAudioIndex];
                _audioSource.Play();

                float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                float aL1 = _clips[_currentAudioIndex].length / pV1;
                yield return new WaitForSeconds(aL1);
            }

            _currentAudioIndex++;
        }
        _currentAudioIndex = 0;
        UpdateSpriteByAudioIndex();
    }

    public void Slow(TextMeshProUGUI text)
    {
        if (text != null) text.text = _isSlowed ? "    SLOW" : "    FAST";
        if (_audioSource != null) _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }

    private void TriggerPopAnimation(int index)
    {
        if (_cardParent == null || index >= _cardParent.childCount) return;

        Transform targetCard = _cardParent.GetChild(index);
        if (targetCard.TryGetComponent(out PopEffect_Junior2A pop))
        {
            pop.enabled = false;
            pop.enabled = true;
        }
    }

    private void UpdateSpriteByAudioIndex()
    {
        if (_targetImage == null || _evenSprite == null || _oddSprite == null) return;

        _targetImage.sprite = (_currentAudioIndex % 2 == 0) ? _evenSprite : _oddSprite;
    }
}