using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U1_L02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip _samClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] Transform _tinaTextObj, _samTextObj, _buttonParent;
    [SerializeField] int _currentAudioIndex = 0;
    Coroutine _coroutine, _buttonCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => _coroutine = StartCoroutine(AutoStart());
    IEnumerator AutoStart()
    {
        foreach (Transform button in _buttonParent)
        {
            button.GetComponent<PopEffect_Junior1A>().enabled = true;
            button.GetComponent<Button>().interactable = false;
            button.gameObject.SetActive(false);
        }
        _samTextObj.gameObject.SetActive(false);
        _tinaTextObj.gameObject.SetActive(false);
        _currentAudioIndex = 0;

        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length + .5f);
        _samTextObj.gameObject.SetActive(true);
        _audioSource.clip = _samClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_samClip.length + .5f);
        _currentAudioIndex = _buttonParent.childCount;
        foreach (AudioClip clip in _audioClips)
        {
            _currentAudioIndex--;
            _tinaTextObj.gameObject.SetActive(false);
            _tinaTextObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = clip.name;
            _tinaTextObj.gameObject.SetActive(true);
            _audioSource.clip = clip;
            _audioSource.Play();
            _buttonParent.GetChild(_currentAudioIndex).gameObject.SetActive(true);
            _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;
            yield return new WaitForSeconds(clip.length + .5f);
            _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
            if (_currentAudioIndex <= 0)
            {
                _isViewed = true;
                GameManager_Junior1A.Instance.Next(true);
            }
        }
        foreach (Transform button in _buttonParent) button.GetComponent<Button>().interactable = true;
    }
    public void PlayAudio(int index)
    {
        _buttonParent.GetChild((_buttonParent.childCount - 1) - _currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _currentAudioIndex = index;
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
    }
    IEnumerator StartButtonAudio()
    {
        _tinaTextObj.gameObject.SetActive(false);
        _tinaTextObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _audioClips[_currentAudioIndex].name;
        _tinaTextObj.gameObject.SetActive(true);
        _buttonParent.GetChild((_buttonParent.childCount - 1) - _currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;
        _audioSource.clip = _audioClips[_currentAudioIndex];
        _audioSource.Play();
        yield return new WaitForSeconds(_audioClips[_currentAudioIndex].length + .5f);
        _buttonParent.GetChild((_buttonParent.childCount - 1) - _currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
    }
}
