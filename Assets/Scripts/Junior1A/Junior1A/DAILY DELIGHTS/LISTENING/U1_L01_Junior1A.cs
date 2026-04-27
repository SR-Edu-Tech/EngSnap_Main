using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U1_L01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] Transform _buttonParent;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] Color defaultColor;
    [SerializeField] bool _autoStart, _isViewed, _canChangeAudio, _isSlowed;
    Coroutine _coroutine, _setCoroutine;
    public void SetAudioClip(int index)
    {
        _audioSource.Stop();
        if (index >= 0 && index < _audioClips.Length)
        {
            _audioSource.clip = _audioClips[index];
            _audioSource.Play();
            if (_canChangeAudio)
            {   
                if (_setCoroutine != null) StopCoroutine(_setCoroutine);
                if (_coroutine != null) StopCoroutine(_coroutine);
                _setCoroutine = StartCoroutine(SetText(index));
            }
        }
    }
    void OnEnable() => StartCoroutine(Starter());
    void OnDisable()
    {
        _canChangeAudio = false;
        foreach (Transform child in _buttonParent) child.GetComponent<Button>().interactable = false;
    }
    IEnumerator Starter()
    {
        _audioSource.clip = _introClip;
        _audioSource.Play();
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().color = defaultColor;
        _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
        yield return new WaitForSeconds(_introClip.length);
        _coroutine = StartCoroutine(AutoStart());
    }
    IEnumerator SetText(int index)
    {
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().color = defaultColor;
        _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
        _currentAudioIndex = index;
        _buttonParent.GetChild(index).GetChild(1).GetComponent<Image>().enabled = true;
        _buttonParent.GetChild(index).GetChild(0).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        if (ColorUtility.TryParseHtmlString("#14799E", out Color myColor)) _buttonParent.GetChild(index).GetChild(0).GetComponent<TextMeshProUGUI>().color = myColor;
        yield return new WaitForSeconds(_audioClips[index].length + .5f);
        _buttonParent.GetChild(index).GetChild(1).GetComponent<Image>().enabled = false;
        _buttonParent.GetChild(index).GetChild(0).GetComponent<TextMeshProUGUI>().color = defaultColor;
        _buttonParent.GetChild(index).GetChild(0).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
    }
    IEnumerator AutoStart()
    {
        _currentAudioIndex = 0;
        yield return new WaitForSeconds(1f);
        foreach (AudioClip clip in _audioClips)
        {
            _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;
            _audioSource.clip = clip;
            _audioSource.Play();
            _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            if (ColorUtility.TryParseHtmlString("#14799E", out Color myColor)) _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().color = myColor;
            yield return new WaitForSeconds(clip.length + .5f);
            _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
            _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().color = defaultColor;
            _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
            if (_currentAudioIndex == 7)
            {
                _canChangeAudio = _isViewed = true;
                GameManager_Junior1A.Instance.Next(true);
            }
            _currentAudioIndex++;
        }
        _currentAudioIndex = 0;
        foreach (Transform child in _buttonParent) child.GetComponent<Button>().interactable = true;
    }
    public void Repeat()
    {
        if (!_canChangeAudio) return;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _audioSource.Stop();
        _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().color = defaultColor;
        _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Normal;
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        foreach (Transform child in _buttonParent) child.GetComponent<Button>().interactable = true;
        _coroutine = StartCoroutine(AutoStart());
    }
    public bool IsViewed => _isViewed;
    public void ButtonClick(Image image) => image.color = image.color == Color.white ? Color.gray : Color.white;
}
