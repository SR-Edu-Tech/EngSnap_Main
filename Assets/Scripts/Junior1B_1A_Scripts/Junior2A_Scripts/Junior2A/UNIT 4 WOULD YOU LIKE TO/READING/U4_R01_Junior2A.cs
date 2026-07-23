using Junior2A;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U4_R01_Junior2A : MonoBehaviour, Interfaces_Junior2A
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

    // State Color Definitions
    private Color _defaultColor = Color.white;
    private Color _playingColor = Color.yellow;
    private Color _doneColor = Color.green;

    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(StarterTab1());

    IEnumerator StarterTab1()
    {
        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
        }

        _clickCheckIndex.Clear();
        _clickedIndexText.text = _clickCheckIndex.Count.ToString() + "/" + _clips.Length.ToString();

        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Image>().color = _defaultColor;
            if (button.TryGetComponent(out Button btn)) btn.interactable = false;
            button.gameObject.SetActive(false);
        }

        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length / 2);

        foreach (Transform button in _cardParent) button.gameObject.SetActive(true);
        yield return new WaitForSeconds(_introClip.length / 2);

        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out Button btn)) btn.interactable = true;
        }
    }

    public void PlayAudio(int index)
    {
        // Check if the previous button was completely finished playing before moving off it
        if (_currentAudioIndex < _cardParent.childCount)
        {
            if (_clickCheckIndex.Contains(_currentAudioIndex))
            {
                _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = _doneColor;
            }
            else
            {
                _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = _defaultColor;
            }
        }

        _currentAudioIndex = index;

        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());

        if (!_clickCheckIndex.Contains(index))
        {
            _clickCheckIndex.Add(index);
            _clickedIndexText.text = _clickCheckIndex.Count.ToString() + "/" + _clips.Length.ToString();
        }

        if (_clickCheckIndex.Count == _clips.Length)
        {
            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            _isViewed = true;
        }
    }

    IEnumerator StartButtonAudio()
    {
        // 1. Switch active element frame profile to Yellow immediately
        if (_currentAudioIndex < _cardParent.childCount)
        {
            _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = _playingColor;
        }

        if (_currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null)
        {
            _audioSource.clip = _clips[_currentAudioIndex];
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);
        }
        else
        {
            yield return null;
        }

        // 2. Transition target item directly to Green color map markers when runtime completes
        if (_currentAudioIndex < _cardParent.childCount)
        {
            _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = _doneColor;
        }
    }
}