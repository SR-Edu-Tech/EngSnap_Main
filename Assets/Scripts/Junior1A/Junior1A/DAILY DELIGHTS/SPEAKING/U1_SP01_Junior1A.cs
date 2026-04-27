using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U1_SP01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip, _correctClip, _wrongClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] GameObject _currentLineShowBox, _micObj;
    [SerializeField] TextMeshProUGUI _feedbackText;
    [SerializeField] string _micTextInput;
    [SerializeField] bool _isViewed;
    Coroutine _coroutine;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
        StartCoroutine(Starter());
    }

    void OnDisable() => CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;

    IEnumerator Starter()
    {
        _currentLineShowBox.SetActive(false);
        _micObj.SetActive(false);
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        _currentLineShowBox.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _audioClips[_currentAudioIndex].name;
        _currentLineShowBox.SetActive(true);
        _micObj.SetActive(true);
    }

    public void PlayAudioClip()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(AudioPlayer());
    }

    IEnumerator AudioPlayer()
    {
        _audioSource.clip = _audioClips[_currentAudioIndex];
        _audioSource.Play();
        _currentLineShowBox.transform.GetChild(1).GetComponent<Image>().enabled = true;
        yield return new WaitForSeconds(_audioSource.clip.length);
        _currentLineShowBox.transform.GetChild(1).GetComponent<Image>().enabled = false;
    }

    void OnSpeechResult(string spokenText)
    {
        _micTextInput = spokenText;
        string spoken = _micTextInput.ToLower().Trim();
        string answer = _audioClips[_currentAudioIndex].name.ToLower().Trim();
        bool isMatch = spoken == answer;
        _feedbackText.text = spokenText;
        Debug.Log($"Spoken: \"{spoken}\" | Answer: \"{answer}\" | Match: {isMatch}");
        StartCoroutine(AudioChecker(isMatch));
    }

    IEnumerator AudioChecker(bool isMatch)
    {
        if (isMatch)
        {
            _audioSource.clip = _correctClip;
            _audioSource.Play();
            if (_currentAudioIndex < _audioClips.Length - 1)
            {
                _currentLineShowBox.GetComponent<Button>().interactable = false;
                yield return new WaitForSeconds(_audioSource.clip.length);
                _currentAudioIndex++;
                _currentLineShowBox.SetActive(false);
                _currentLineShowBox.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _audioClips[_currentAudioIndex].name;
                _currentLineShowBox.SetActive(true);
                _currentLineShowBox.GetComponent<Button>().interactable = true;
            }
            else
            {
                _isViewed = true;
                GameManager_Junior1A.Instance.Next(true);
            }
        }
        else
        {
            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            _currentLineShowBox.GetComponent<Button>().interactable = false;
            yield return new WaitForSeconds(_audioSource.clip.length);
            _currentLineShowBox.GetComponent<Button>().interactable = true;
        }
    }
}