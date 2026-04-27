using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U1_R01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _samClips, _tinaClips;
    [SerializeField] string[] _tinaTextData, _samTextData;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] Transform _buttonParent, _tinaTextObj, _samTextObj;
    [SerializeField] List<int> _clickCheckIndex = new List<int>();
    Coroutine _coroutine, _buttonCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => _coroutine = StartCoroutine(AutoStart());
    IEnumerator AutoStart()
    {
        _tinaTextObj.gameObject.SetActive(false);
        _samTextObj.gameObject.SetActive(false);
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        foreach (Transform button in _buttonParent)
        {
            button.GetComponent<Button>().interactable = false;
            button.GetComponent<Image>().color = Color.white;
        }
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length + .5f);
        foreach (Transform button in _buttonParent) button.GetComponent<Button>().interactable = true;
    }
    public void PlayAudio(int index)
    {
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _currentAudioIndex = index;
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
        if (!_clickCheckIndex.Contains(index)) _clickCheckIndex.Add(index);
        if (_clickCheckIndex.Count == _samClips.Length)
        {
            _isViewed = true;
            GameManager_Junior1A.Instance.Next(true);
        }
    }
    IEnumerator StartButtonAudio()
    {
        _buttonParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1.0f);
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;
        _samTextObj.gameObject.SetActive(false);
        _tinaTextObj.gameObject.SetActive(false);

        _samTextObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _samTextData[_currentAudioIndex];
        _samTextObj.gameObject.SetActive(true);
        _audioSource.clip = _samClips[_currentAudioIndex];
        _audioSource.Play();

        yield return new WaitForSeconds(_samClips[_currentAudioIndex].length);

        _tinaTextObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _tinaTextData[_currentAudioIndex];
        _tinaTextObj.gameObject.SetActive(true);
        _audioSource.clip = _tinaClips[_currentAudioIndex];
        _audioSource.Play();

        yield return new WaitForSeconds(_tinaClips[_currentAudioIndex].length);

        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
    }
}
