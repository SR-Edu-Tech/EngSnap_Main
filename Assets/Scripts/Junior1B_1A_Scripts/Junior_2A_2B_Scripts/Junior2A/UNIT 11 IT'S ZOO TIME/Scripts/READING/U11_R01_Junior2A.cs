using Junior2A;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U11_R01_Junior2A : MonoBehaviour, Interfaces_Junior2A
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
        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
        }

        _clickCheckIndex.Clear();
        _clickedIndexText.text = _clickCheckIndex.Count.ToString() + "/" + _clips.Length;

        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out Image img)) img.color = Color.white;
            if (button.TryGetComponent(out Button btn)) btn.interactable = false;
            button.gameObject.SetActive(false);
        }

        // Safe check for index bounds and child hierarchy structure
        SetSpeakerIconStatus(_currentAudioIndex, false);

        if (_audioSource && _introClip)
        {
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
        else
        {
            foreach (Transform button in _cardParent)
            {
                button.gameObject.SetActive(true);
                if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            }
        }
    }

    public void PlayAudio(int index)
    {
        // Turn off the active icon wrapper using the old index first
        SetSpeakerIconStatus(_currentAudioIndex, false);

        _currentAudioIndex = index;

        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());

        if (!_clickCheckIndex.Contains(index))
        {
            _clickCheckIndex.Add(index);
            _clickedIndexText.text = _clickCheckIndex.Count.ToString() + "/" + _clips.Length;
        }

        if (_clickCheckIndex.Count == _clips.Length)
        {
            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            _isViewed = true;
        }
    }

    IEnumerator StartButtonAudio()
    {
        if (_currentAudioIndex >= 0 && _currentAudioIndex < _cardParent.childCount)
        {
            Transform currentCard = _cardParent.GetChild(_currentAudioIndex);
            if (currentCard.TryGetComponent(out Image img))
            {
                img.color = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1.0f);
            }
        }

        SetSpeakerIconStatus(_currentAudioIndex, true);

        if (_audioSource && _clips != null && _currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null)
        {
            _audioSource.clip = _clips[_currentAudioIndex];
            _audioSource.Play();

            yield return new WaitForSeconds(_audioSource.clip.length);
        }
        else
        {
            yield return null;
        }

        SetSpeakerIconStatus(_currentAudioIndex, false);
    }

    // Helper method to securely manage nested layout changes safely
    private void SetSpeakerIconStatus(int targetIndex, bool state)
    {
        if (targetIndex >= 0 && targetIndex < _cardParent.childCount)
        {
            Transform card = _cardParent.GetChild(targetIndex);
            if (card.childCount > 1)
            {
                Transform subChild = card.GetChild(1);
                if (subChild.TryGetComponent(out Image img))
                {
                    img.enabled = state;
                }
            }
        }
    }
}