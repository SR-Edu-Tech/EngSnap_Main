using Junior2A;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U2_R01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [Header("Audio Engine Setup")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip[] _clips;

    [Header("Layout Target References")]
    [SerializeField] private Transform _cardParent; // Linked to ButtonParent in Inspector

    [Header("Runtime State Matrices")]
    [SerializeField] private int _currentAudioIndex = 0;
    [SerializeField] private bool _isViewed = false;
    [SerializeField] private bool _isSlowed = false;

    private bool _canInteract = false;

    private Coroutine _coroutine;
    private Coroutine _repeatCoroutine;

    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(Starter());

    private IEnumerator Starter()
    {
        _canInteract = false; // Block interaction during initial setup animations

        if (_cardParent == null)
        {
            Debug.LogError("❌ Card Parent reference is completely missing in inspector allocation fields!");
            yield break;
        }

        foreach (Transform button in _cardParent) button.gameObject.SetActive(false);

        if (transform.childCount > 0)
        {
            Transform footer = transform.GetChild(transform.childCount - 1);
            if (footer.childCount > 1) footer.GetChild(1).gameObject.SetActive(false);
        }

        ResetCardVisualState(_currentAudioIndex);
        _currentAudioIndex = 0; // The player must start with Card 0

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
                pop.enabled = false;
                pop.enabled = true;
                yield return new WaitForSeconds(pop.PopDuration + 0.15f);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        // Ensure all UI buttons components are interactable
        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out Button btn)) btn.interactable = true;
        }

        // NO MORE AUTOPLAY! Simply turn on interaction and let the player click Card 0.
        _canInteract = true;
    }

    public void PlayAudio(int index)
    {
        // Guard clause: player MUST click the expected index in sequence order
        if (!_canInteract || index != _currentAudioIndex || _cardParent == null) return;

        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
        if (_coroutine != null) StopCoroutine(_coroutine);

        _coroutine = StartCoroutine(StartButtonAudio());
    }

    private IEnumerator StartButtonAudio()
    {
        _canInteract = false; // Lock out inputs while the clicked card's audio is playing
        SetCardActiveVisualState(_currentAudioIndex);

        if (_clips != null && _currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null && _audioSource != null)
        {
            _audioSource.clip = _clips[_currentAudioIndex];
            _audioSource.Play();

            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _clips[_currentAudioIndex].length / pV1;
            yield return new WaitForSeconds(aL1);
        }

        ResetCardVisualState(_currentAudioIndex);

        // Move expectation window to the next card index in line
        _currentAudioIndex++;

        // If all cards have been successfully pressed in order, complete the lesson segment
        if (_currentAudioIndex >= _cardParent.childCount)
        {
            if (transform.childCount > 0)
            {
                Transform footer = transform.GetChild(transform.childCount - 1);
                if (footer.childCount > 1) footer.GetChild(1).gameObject.SetActive(true);
            }

            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            _isViewed = true;
            _canInteract = false; // Finished completely
        }
        else
        {
            _canInteract = true; // Open window for the next required card sequence button
        }
    }

    public void Repeat()
    {
        if (_cardParent == null) return;

        ResetCardVisualState(_currentAudioIndex);

        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);

        _repeatCoroutine = StartCoroutine(RepeatAudio());
    }

    private IEnumerator RepeatAudio()
    {
        _canInteract = false;
        _currentAudioIndex = 0;

        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;

            SetCardActiveVisualState(_currentAudioIndex);

            if (_clips != null && _currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null && _audioSource != null)
            {
                _audioSource.clip = _clips[_currentAudioIndex];
                _audioSource.Play();

                float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                float aL1 = _clips[_currentAudioIndex].length / pV1;
                yield return new WaitForSeconds(aL1);
            }

            ResetCardVisualState(_currentAudioIndex);
            _currentAudioIndex++;
        }

        _currentAudioIndex = 0; // Reset expectation target index back to the beginning card
        _canInteract = true;
    }

    public void Slow(TextMeshProUGUI text)
    {
        if (text != null) text.text = _isSlowed ? "    SLOW" : "    FAST";
        if (_audioSource != null) _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }

    // --- UI VISUAL HIGHLIGHT METHOD HANDLING ---

    private void SetCardActiveVisualState(int index)
    {
        if (_cardParent == null || index >= _cardParent.childCount) return;

        Transform targetCard = _cardParent.GetChild(index);
        if (targetCard.TryGetComponent(out Image cardBg))
        {
            cardBg.color = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
        }

        if (targetCard.childCount > 0)
        {
            Transform innerContainer = targetCard.GetChild(0);
            if (innerContainer.childCount > 0)
            {
                Transform highlightObj = innerContainer.GetChild(0);
                if (highlightObj.TryGetComponent(out Image highlightImg)) highlightImg.enabled = true;
            }
        }
    }

    private void ResetCardVisualState(int index)
    {
        if (_cardParent == null || index >= _cardParent.childCount) return;

        Transform targetCard = _cardParent.GetChild(index);
        if (targetCard.TryGetComponent(out Image cardBg))
        {
            cardBg.color = Color.white;
        }

        if (targetCard.childCount > 0)
        {
            Transform innerContainer = targetCard.GetChild(0);
            if (innerContainer.childCount > 0)
            {
                Transform highlightObj = innerContainer.GetChild(0);
                if (highlightObj.TryGetComponent(out Image highlightImg)) highlightImg.enabled = false;
            }
        }
    }
}