using Junior2A;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U8_L01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [Header("Audio Engine Setup")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip[] _clips;

    [Header("Layout Target References")]
    [SerializeField] private Transform _cardParent; // Linked to ButtonParent in Inspector

    [Header("Sprite Alternator Integration")]
    [SerializeField] private Image _targetImage;       // The Image component that switches sprites
    [SerializeField] private Sprite _evenSprite;       // Sprite used when index is even (0, 2, 4...)
    [SerializeField] private Sprite _oddSprite;        // Sprite used when index is odd (1, 3, 5...)

    [Header("Runtime State Matrices")]
    [SerializeField] private int _currentAudioIndex = 0;
    [SerializeField] private bool _isViewed = false;
    [SerializeField] private bool _isSlowed = false;

    private Coroutine _coroutine;
    private Coroutine _repeatCoroutine;

    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(Starter());

    private IEnumerator Starter()
    {
        if (_cardParent == null)
        {
            Debug.LogError("❌ Card Parent reference is completely missing in inspector allocation fields!");
            yield break;
        }

        foreach (Transform button in _cardParent) button.gameObject.SetActive(false);

        // Safety check to handle footer navigation layer activation sequences
        if (transform.childCount > 0)
        {
            Transform footer = transform.GetChild(transform.childCount - 1);
            if (footer.childCount > 1) footer.GetChild(1).gameObject.SetActive(false);
        }

        _currentAudioIndex = 0;
        UpdateSpriteByAudioIndex();

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }

        // Sequentially spawn all card layout wrappers into play view matrix
        foreach (Transform button in _cardParent)
        {
            button.gameObject.SetActive(true);

            if (button.TryGetComponent(out PopEffect_Junior2A pop))
            {
                pop.enabled = false; // Reset if already active
                pop.enabled = true;

                // Dynamic Wait: Wait for this card's specific pop animation to finish 
                // plus a 0.15s breathing room delay before starting the next one.
                yield return new WaitForSeconds(pop.PopDuration + 0.15f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f); // Fallback if component is missing
            }
        }

        // Auto-run gameplay pass simulation trigger on onboarding phase
        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out Button btn))
            {
                btn.onClick.Invoke();
            }

            if (_clips != null && _currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null)
            {
                float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                float aL1 = _clips[_currentAudioIndex].length / pV1;
                yield return new WaitForSeconds(aL1);
            }
            else
            {
                yield return new WaitForSeconds(1.0f); // Fallback wait time anchor
            }
        }

        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out Button btn)) btn.interactable = true;
        }

        if (transform.childCount > 0)
        {
            Transform footer = transform.GetChild(transform.childCount - 1);
            if (footer.childCount > 1) footer.GetChild(1).gameObject.SetActive(true);
        }

        if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
        _isViewed = true;
    }

    public void PlayAudio(int index)
    {
        if (_cardParent == null) return;

        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);

        _currentAudioIndex = index;
        UpdateSpriteByAudioIndex();

        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(StartButtonAudio());
    }

    private IEnumerator StartButtonAudio()
    {
        TriggerPopAnimation(_currentAudioIndex);

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

    // Swaps sprites back and forth dynamically based on the active index calculation
    private void UpdateSpriteByAudioIndex()
    {
        if (_targetImage == null || _evenSprite == null || _oddSprite == null) return;

        // Even indices get one sprite, odd indices get the other
        _targetImage.sprite = (_currentAudioIndex % 2 == 0) ? _evenSprite : _oddSprite;
    }
}