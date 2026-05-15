using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class U3_L02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isCurrentLeft = false, _isViewed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] Transform _char1TextObj, _char2TextObj, _buttonParent;
    [SerializeField] int _currentAudioIndex = 0;
    Coroutine _coroutine, _buttonCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => _coroutine = StartCoroutine(AutoStart());
    IEnumerator AutoStart()
    {
        foreach (Transform button in _buttonParent) button.gameObject.SetActive(false);
        _char2TextObj.gameObject.SetActive(false);
        _char1TextObj.gameObject.SetActive(false);
        _audioSource.clip = _introClip;
        _audioSource.Play();
        _currentAudioIndex = 0;

        yield return new WaitForSeconds(_introClip.length);

        foreach (Transform button in _buttonParent)
        {
            button.GetComponent<Button>().interactable = false;
            button.gameObject.SetActive(true);
            button.GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSeconds(_audioClips[_currentAudioIndex].length + .25f);
        }
        _isViewed = true;
        GameManager_Junior1A.Instance.Next(true);
        _currentAudioIndex = 0;
        foreach (Transform button in _buttonParent) button.GetComponent<Button>().interactable = true;
    }
    public void SetSide(bool isLeft) => _isCurrentLeft = isLeft;
    public void PlayAudio(int index)
    {
        _currentAudioIndex = index;
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
    }
    IEnumerator StartButtonAudio()
    {
        yield return null;
        if (_isCurrentLeft)
        {
            _char1TextObj.GetComponent<PopEffect_Junior1A>().enabled = true;
            _char1TextObj.GetComponent<Image>().sprite = _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite;
            _char1TextObj.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text;
            _char1TextObj.gameObject.SetActive(true);
            _char1TextObj.GetChild(0).gameObject.SetActive(true);
            _audioSource.clip = _audioClips[_currentAudioIndex];
            _audioSource.Play();
        }
        else
        {
            _char2TextObj.GetComponent<PopEffect_Junior1A>().enabled = true;
            _char2TextObj.GetComponent<Image>().sprite = _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(1).GetChild(0).GetComponent<Image>().sprite;
            _char2TextObj.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text;
            _char2TextObj.gameObject.SetActive(true);
            _char2TextObj.GetChild(0).gameObject.SetActive(true);
            _audioSource.clip = _audioClips[_currentAudioIndex];
            _audioSource.Play();
        }
    }
}
