using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U2_L02_Junior1A : MonoBehaviour
{
    [SerializeField] bool _isViewed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip[] _char_1, _char_2;
    [SerializeField] Transform _char1TextObj, _char2TextObj, _buttonParent;
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
        }
        _char2TextObj.gameObject.SetActive(false);
        _char1TextObj.gameObject.SetActive(false);

        yield return new WaitForSeconds(2f);

        _char1TextObj.gameObject.SetActive(true);
        _char2TextObj.gameObject.SetActive(true);

        _currentAudioIndex = 0;

        foreach (AudioClip clip in _char_1)
        {
            _char1TextObj.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = clip.name;
            _char1TextObj.gameObject.SetActive(true);
            _audioSource.clip = _char_1[_currentAudioIndex];
            _audioSource.Play();

            yield return new WaitForSeconds(_char_1[_currentAudioIndex].length);

            _char2TextObj.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = clip.name;
            _char2TextObj.gameObject.SetActive(true);
            _audioSource.clip = _char_1[_currentAudioIndex];
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
            _currentAudioIndex++;
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
        _char1TextObj.gameObject.SetActive(false);
        _char1TextObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _char_1[_currentAudioIndex].name;
        _char1TextObj.gameObject.SetActive(true);
        _buttonParent.GetChild((_buttonParent.childCount - 1) - _currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;
        _audioSource.clip = _char_1[_currentAudioIndex];
        _audioSource.Play();
        yield return new WaitForSeconds(_char_1[_currentAudioIndex].length + .5f);
        _buttonParent.GetChild((_buttonParent.childCount - 1) - _currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
    }
}
