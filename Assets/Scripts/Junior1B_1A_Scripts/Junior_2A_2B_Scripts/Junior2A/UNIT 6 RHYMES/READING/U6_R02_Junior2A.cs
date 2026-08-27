using Junior2A;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U6_R02_Junior2A : MonoBehaviour, Interfaces_Junior2A
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

        // Turn on the single button with the Pop Effect and unlock it for manual clicking
        if (_cardButton != null)
        {
            _cardButton.gameObject.SetActive(true);
            if (_cardButton.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
            _cardButton.interactable = true;
            yield return new WaitForSeconds(.1f);
        }

        // Show options panel right after intro finishes so Repeat/Slow are available
        transform.GetChild(transform.childCount - 1).GetChild(1).gameObject.SetActive(true);
    }

    // Bind your single card button directly to this function inside OnClick() in the Inspector
    public void PlayAudio()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        ResetButtonVisuals();
        _coroutine = StartCoroutine(StartButtonAudio());
    }

    IEnumerator StartButtonAudio()
    {
        if (_cardButton == null) yield break;

        // Highlight the card visuals on manual click
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

        // Revert visuals back to white
        _cardButton.GetComponent<Image>().color = Color.white;
        if (_cardButton.transform.GetChild(0).childCount > 1)
            _cardButton.transform.GetChild(0).GetChild(1).GetComponent<Image>().enabled = false;

        // --- COMPLETED MANUAL INTERACTION ---
        // Instantly unlock and pop up the next page arrow/button now that they interacted
        if (!_isViewed)
        {
            if (GameManager_Junior2A.Instance != null)
            {
                GameManager_Junior2A.Instance.Next(true);
            }
            _isViewed = true;
        }
    }

    public void Repeat()
    {
        // Re-triggers the manual audio routine
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