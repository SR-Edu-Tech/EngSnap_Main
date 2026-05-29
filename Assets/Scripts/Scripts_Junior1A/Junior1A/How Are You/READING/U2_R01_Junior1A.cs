using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class U2_R01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] GameObject _next, _replay, _tinaTextObj, _samTextObj;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] string[] _samTextData, _tinaTextData;
    [SerializeField] int _currentAudioIndex = 0;
    Coroutine _coroutine, _buttonCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => _coroutine = StartCoroutine(AutoStart());
    IEnumerator AutoStart()
    {
        _tinaTextObj.gameObject.SetActive(false);
        _samTextObj.gameObject.SetActive(false);
        _next.SetActive(false);
        _replay.SetActive(false);
        _currentAudioIndex = 0;

        _audioSource.clip = _introClip;
        _audioSource.Play();
        
        yield return new WaitForSeconds(_introClip.length);
        
        _audioSource.clip = _audioClips[0];
        _audioSource.Play();
        _next.SetActive(true);
        _samTextObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _samTextData[0];
        _samTextObj.SetActive(true);

        yield return new WaitForSeconds(_audioSource.clip.length / 2);

        _tinaTextObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _tinaTextData[0];
        _tinaTextObj.SetActive(true);
        }
    public void Replay()
    {
        _currentAudioIndex = 0;
        _replay.SetActive(false);
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
    }
    public void PlayNext()
    {
        _currentAudioIndex++;
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
        if (_currentAudioIndex == _audioClips.Length - 1) _next.SetActive(false);
    }
    IEnumerator StartButtonAudio()
    {
        _tinaTextObj.gameObject.SetActive(false);
        _samTextObj.gameObject.SetActive(false);

        _audioSource.clip = _audioClips[_currentAudioIndex];
        _audioSource.Play();
        _samTextObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _samTextData[_currentAudioIndex];
        _samTextObj.SetActive(true);

        yield return new WaitForSeconds(_audioSource.clip.length / 2);

        _tinaTextObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _tinaTextData[_currentAudioIndex];
        _tinaTextObj.SetActive(true);

        yield return new WaitForSeconds(_audioSource.clip.length / 2);

        if (_currentAudioIndex == _audioClips.Length - 1)
        {
            _replay.SetActive(true);
            _isViewed = true;
            GameManager_Junior1A.Instance.Next(true);
        }
        else _next.SetActive(true);
    }
}
