using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U9_L02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _char_1, _char_2;
    [SerializeField] Transform _char1TextObj, _char2TextObj, _buttonParent;
    [SerializeField] int _currentAudioIndex = 0;
    Coroutine _coroutine, _buttonCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => _coroutine = StartCoroutine(AutoStart());
    IEnumerator AutoStart()
    {
        transform.GetChild(0).GetComponent<TextPopEffect_Junior1A>().enabled = false;
        transform.GetChild(0).GetComponent<TextPopEffect_Junior1A>().enabled = true;
        foreach (Transform button in _buttonParent) button.gameObject.SetActive(false);
        _char2TextObj.gameObject.SetActive(false);
        _char1TextObj.gameObject.SetActive(false);
        _audioSource.clip = _introClip;
        _audioSource.Play();

        yield return new WaitForSeconds(_introClip.length);

        foreach (Transform button in _buttonParent)
        {
            button.GetComponent<PopEffect_Junior1A>().enabled = true;
            button.GetComponent<Button>().interactable = false;
            button.gameObject.SetActive(true);
        }
        _char1TextObj.gameObject.SetActive(true);
        _char2TextObj.gameObject.SetActive(true);

        _currentAudioIndex = 0;

        foreach (Transform button in _buttonParent)
        {
            _char1TextObj.GetChild(0).gameObject.SetActive(false);
            _char2TextObj.GetChild(0).gameObject.SetActive(false);
            _char1TextObj.gameObject.SetActive(false);
            _char2TextObj.gameObject.SetActive(false);

            button.GetChild(1).GetComponent<Image>().enabled = true;
            _char1TextObj.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = _char_1[_currentAudioIndex].name + "?";
            _char1TextObj.gameObject.SetActive(true);
            _char1TextObj.GetChild(0).GetComponent<Image>().color = new Color(1, 0.8550435f, 0.4386792f, 1);
            _char1TextObj.GetChild(0).gameObject.SetActive(true);
            _audioSource.clip = _char_1[_currentAudioIndex];
            _audioSource.Play();

            yield return new WaitForSeconds(_char_1[_currentAudioIndex].length);

            _char1TextObj.GetChild(0).GetComponent<Image>().color = Color.white;
            _char2TextObj.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = _char_2[_currentAudioIndex].name;
            _char2TextObj.gameObject.SetActive(true);
            _char2TextObj.GetChild(0).GetComponent<Image>().color = new Color(1, 0.8550435f, 0.4386792f, 1);
            _char2TextObj.GetChild(0).gameObject.SetActive(true);
            _audioSource.clip = _char_2[_currentAudioIndex];
            _audioSource.Play();

            yield return new WaitForSeconds(_char_2[_currentAudioIndex].length);
            _char2TextObj.GetChild(0).GetComponent<Image>().color = Color.white;
            button.GetChild(1).GetComponent<Image>().enabled = false;
            _currentAudioIndex++;
        }
        _isViewed = true;
        GameManager_Junior1A.Instance.Next(true);
        _currentAudioIndex = 0;
        foreach (Transform button in _buttonParent) button.GetComponent<Button>().interactable = true;
    }
    public void PlayAudio(int index)
    {
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _currentAudioIndex = index;
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
    }
    IEnumerator StartButtonAudio()
    {
        _char1TextObj.GetChild(0).gameObject.SetActive(false);
        _char2TextObj.GetChild(0).gameObject.SetActive(false);
        _char1TextObj.gameObject.SetActive(false);
        _char2TextObj.gameObject.SetActive(false);

        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;
        _char1TextObj.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = _char_1[_currentAudioIndex].name + "?";
        _char1TextObj.gameObject.SetActive(true);
        _char1TextObj.GetChild(0).GetComponent<Image>().color = new Color(1, 0.8550435f, 0.4386792f, 1);
        _char1TextObj.GetChild(0).gameObject.SetActive(true);
        _audioSource.clip = _char_1[_currentAudioIndex];
        _audioSource.Play();

        yield return new WaitForSeconds(_char_1[_currentAudioIndex].length);

        _char1TextObj.GetChild(0).GetComponent<Image>().color = Color.white;
        _char2TextObj.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = _char_2[_currentAudioIndex].name;
        _char2TextObj.gameObject.SetActive(true);
        _char2TextObj.GetChild(0).GetComponent<Image>().color = new Color(1, 0.8550435f, 0.4386792f, 1);
        _char2TextObj.GetChild(0).gameObject.SetActive(true);
        _audioSource.clip = _char_2[_currentAudioIndex];
        _audioSource.Play();

        yield return new WaitForSeconds(_char_2[_currentAudioIndex].length);
        _char2TextObj.GetChild(0).GetComponent<Image>().color = Color.white;
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
    }
}
