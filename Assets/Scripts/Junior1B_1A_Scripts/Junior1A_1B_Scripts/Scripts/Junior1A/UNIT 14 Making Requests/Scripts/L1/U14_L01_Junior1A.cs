using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U14_L01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Audio Settings")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _clips;
    [SerializeField] int _currentAudioIndex = 0;

    [Header("Card Hierarchy Layout")]
    [SerializeField] Transform _cardParent;
    
    [Header("Animation Settings")]
    [Tooltip("Time in seconds between each card button popping into view. Higher numbers = Slower spawning.")]
    [SerializeField] float _spawnDelay = 0.5f; // 🔥 Adjust this value to make spawning slower or faster!

    [Header("State Tracking")]
    [SerializeField] bool _isViewed = false;
    [SerializeField] bool _isSlowed = false;
    Coroutine _coroutine, _repeatCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        foreach (Transform button in _cardParent) button.gameObject.SetActive(false);
        transform.GetChild(transform.childCount - 1).GetChild(1).gameObject.SetActive(false);
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = Color.white;
        _currentAudioIndex = 0;
        _audioSource.clip = _introClip;
        _audioSource.pitch = _isSlowed ? 0.75f : 1f; 
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length);

        // Loops through card rows and scales/pops them down the line
        foreach (Transform button in _cardParent)
        {
            button.gameObject.SetActive(true);
            button.GetComponent<PopEffect_Junior1A>().enabled = true;
            yield return new WaitForSeconds(_spawnDelay); // 🔥 Uses the new customized delay tracking slot
        }

        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Button>().onClick.Invoke();
            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _clips[_currentAudioIndex].length / pV1;
            yield return new WaitForSeconds(aL1);
        }

        foreach (Transform button in _cardParent) button.GetComponent<Button>().interactable = true;
        transform.GetChild(transform.childCount - 1).GetChild(1).gameObject.SetActive(true);
        GameManager_Junior1A.Instance.Next(true);
        _isViewed = true;
    }

    public void PlayAudio(int index)
    {
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = Color.white;
        _cardParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
        _currentAudioIndex = index;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(StartButtonAudio());
    }

    IEnumerator StartButtonAudio()
    {
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
        _cardParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<Image>().enabled = true;
        _audioSource.clip = _clips[_currentAudioIndex];
        _audioSource.Play();

        float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        float aL1 = _clips[_currentAudioIndex].length / pV1;
        yield return new WaitForSeconds(aL1);

        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = Color.white;
        _cardParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
    }

    public void Repeat()
    {
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = Color.white;
        _cardParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
        _repeatCoroutine = StartCoroutine(RepeatAudio());
    }

    IEnumerator RepeatAudio()
    {
        _currentAudioIndex = 0;
        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Image>().color = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
            button.GetChild(0).GetChild(0).GetComponent<Image>().enabled = true;
            _audioSource.clip = _clips[_currentAudioIndex];
            _audioSource.Play();

            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _clips[_currentAudioIndex].length / pV1;
            yield return new WaitForSeconds(aL1);

            button.GetComponent<Image>().color = Color.white;
            button.GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
            _currentAudioIndex++;
        }
        _currentAudioIndex = 0;
    }

    public void Slow(TextMeshProUGUI text)
    {
        _isSlowed = !_isSlowed;
        text.text = _isSlowed ? "    FAST" : "    SLOW";
        _audioSource.pitch = _isSlowed ? 0.75f : 1f;
    }
}