using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U5_R02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] List<int> _clickCheckIndex = new List<int>();
    [SerializeField] TextMeshProUGUI _clickedIndexText;
    [SerializeField] Transform _buttonParent, _textObj;
    Coroutine _coroutine, _buttonCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => _coroutine = StartCoroutine(AutoStart());
    IEnumerator AutoStart()
    {
        _currentAudioIndex = 0;
        _clickCheckIndex.Clear();
        _clickedIndexText.text = $"{_currentAudioIndex}/{_audioClips.Length}";
        foreach (Transform button in _buttonParent) button.gameObject.SetActive(false);
        _textObj.gameObject.SetActive(false);
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length);
        foreach (Transform button in _buttonParent)
        {
            button.GetComponent<Button>().interactable = false;
            button.GetComponent<Image>().color = Color.white;
            button.gameObject.SetActive(true);
            yield return new WaitForSeconds(.25f);
        }
        foreach (Transform button in _buttonParent) button.GetComponent<Button>().interactable = true;
    }
    public void PlayAudio(int index)
    {
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _currentAudioIndex = index;
        if(!_clickCheckIndex.Contains(index))
        {
            _clickCheckIndex.Add(index);
            _clickedIndexText.text = $"{_clickCheckIndex.Count}/{_audioClips.Length}";
        }
        if(_clickCheckIndex.Count == _audioClips.Length)
        {
            GameManager_Junior1A.Instance.Next(true);
            _isViewed = true;
        }
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
    }
    IEnumerator StartButtonAudio()
    {
        _buttonParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1.0f);
        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;
        _textObj.gameObject.SetActive(false);

        _textObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = _audioClips[_currentAudioIndex].name;
        _textObj.gameObject.SetActive(true);
        _audioSource.clip = _audioClips[_currentAudioIndex];
        _audioSource.Play();

        yield return new WaitForSeconds(_audioClips[_currentAudioIndex].length);

        _buttonParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
    }
}
