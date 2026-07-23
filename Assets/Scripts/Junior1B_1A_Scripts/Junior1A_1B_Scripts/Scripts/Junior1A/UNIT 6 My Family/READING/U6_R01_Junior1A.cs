using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U6_R01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _clips;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] Transform _cardParent;
    [SerializeField] List<int> _clickCheckIndex = new List<int>();
    [SerializeField] bool _isViewed = false;    
    [SerializeField] TextMeshProUGUI _clickedIndexText;
    Coroutine _buttonCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(StarterTab1());

    IEnumerator StarterTab1()
    {
        foreach (Transform button in _cardParent) button.GetComponent<PopEffect_Junior1A>().enabled = true;
        _clickCheckIndex.Clear();
        _clickedIndexText.text = _clickCheckIndex.Count.ToString() + "/" + _clips.Length;
        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Image>().color = Color.white;
            button.GetComponent<Button>().interactable = false;
            button.gameObject.SetActive(false);
        }
        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length / 2);
        foreach (Transform button in _cardParent) button.gameObject.SetActive(true);
        yield return new WaitForSeconds(_introClip.length / 2);
        foreach (Transform button in _cardParent) button.GetComponent<Button>().interactable = true;
    }
    public void PlayAudio(int index)
    {
        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _currentAudioIndex = index;
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
        if (!_clickCheckIndex.Contains(index))
        {
            _clickedIndexText.text = (_clickCheckIndex.Count + 1).ToString() + "/" + _clips.Length;
            _clickCheckIndex.Add(index);
        }
        if (_clickCheckIndex.Count == _clips.Length)
        {
            GameManager_Junior1A.Instance.Next(true);
            _isViewed = true;
        }
    }
    IEnumerator StartButtonAudio()
    {
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1.0f);
        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;
        _audioSource.clip = _clips[_currentAudioIndex];
        _audioSource.Play();

        yield return new WaitForSeconds(_audioSource.clip.length);

        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
    }
}
