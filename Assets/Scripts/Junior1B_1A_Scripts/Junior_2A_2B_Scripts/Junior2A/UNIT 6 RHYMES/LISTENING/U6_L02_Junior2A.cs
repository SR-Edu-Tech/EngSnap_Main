using Junior2A;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U6_L02_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip _cardClip; // Single clip for the single button

    [Header("Single Card Reference")]
    [SerializeField] Button _cardButton; // Drag your single button GameObject here in the inspector

    [SerializeField] bool _isViewed = false, _isSlowed = false;

    Coroutine _coroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        _isViewed = false;

        if (_cardButton != null)
        {
            _cardButton.gameObject.SetActive(false);
            _cardButton.interactable = false;
        }

        // Hide the options panel (the last child setup)
        transform.GetChild(transform.childCount - 1).GetChild(1).gameObject.SetActive(false);

        // Play Intro Audio
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length);

        // Turn on the single button with the Pop Effect
        if (_cardButton != null)
        {
            _cardButton.gameObject.SetActive(true);
            if (_cardButton.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
            yield return new WaitForSeconds(.1f);
        }

        // --- AUTOPLAY SEQUENCE ---
        if (_cardButton != null)
        {
            // Highlight the card
            _cardButton.GetComponent<Image>().color = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);

            // Turn on Speaker Icon (Hierarchy: Mask -> Speaker)
            if (_cardButton.transform.GetChild(0).childCount > 1)
                _cardButton.transform.GetChild(0).GetChild(1).GetComponent<Image>().enabled = true;

            // Play the clip
            if (_cardClip != null)
            {
                _audioSource.clip = _cardClip;
                _audioSource.Play();

                float pitchVal = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                float duration = _cardClip.length / pitchVal;
                yield return new WaitForSeconds(duration);
            }

            // Reset the look back to white
            _cardButton.GetComponent<Image>().color = Color.white;
            if (_cardButton.transform.GetChild(0).childCount > 1)
                _cardButton.transform.GetChild(0).GetChild(1).GetComponent<Image>().enabled = false;

            // Unlock interaction for manual review clicks later
            _cardButton.interactable = true;
        }

        // --- COMPLETED PLAYING AUDIO ---
        // Show options panel
        transform.GetChild(transform.childCount - 1).GetChild(1).gameObject.SetActive(true);

        // Instantly unlock and pop up the next page arrow/button
        if (GameManager_Junior2A.Instance != null)
        {
            GameManager_Junior2A.Instance.Next(true);
        }
        _isViewed = true;
    }

    // Manual playback callback if they click the single button *after* autoplay completes
    public void PlayAudio()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        ResetButtonVisuals();
        _coroutine = StartCoroutine(StartButtonAudio());
    }

    IEnumerator StartButtonAudio()
    {
        if (_cardButton == null) yield break;

        _cardButton.GetComponent<Image>().color = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);

        if (_cardButton.transform.GetChild(0).childCount > 1)
            _cardButton.transform.GetChild(0).GetChild(1).GetComponent<Image>().enabled = true;

        if (_cardClip != null)
        {
            _audioSource.clip = _cardClip;
            _audioSource.Play();

            float pitchVal = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float duration = _cardClip.length / pitchVal;
            yield return new WaitForSeconds(duration);
        }

        _cardButton.GetComponent<Image>().color = Color.white;
        if (_cardButton.transform.GetChild(0).childCount > 1)
            _cardButton.transform.GetChild(0).GetChild(1).GetComponent<Image>().enabled = false;
    }

    public void Repeat()
    {
        // For a single card, Repeat simply plays it again
        PlayAudio();
    }

    private void ResetButtonVisuals()
    {
        if (_cardButton == null) return;

        _cardButton.GetComponent<Image>().color = Color.white;
        if (_cardButton.transform.childCount > 0 && _cardButton.transform.GetChild(0).childCount > 1)
        {
            var speakerImg = _cardButton.transform.GetChild(0).GetChild(1).GetComponent<Image>();
            if (speakerImg != null) speakerImg.enabled = false;
        }
    }

    public void Slow(TextMeshProUGUI text)
    {
        text.text = _isSlowed ? "    SLOW" : "    FAST";
        _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }
}