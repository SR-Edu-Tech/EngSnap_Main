using Junior2A;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U5_R01_Junior2A : MonoBehaviour, Interfaces_Junior2A
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
        foreach (Transform button in _cardParent) button.GetComponent<PopEffect_Junior2A>().enabled = true;
        _clickCheckIndex.Clear();

        // DYNAMIC FIX: Uses childCount instead of hardcoded "/8"
        _clickedIndexText.text = "0/" + _cardParent.childCount.ToString();

        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Image>().color = Color.white;
            button.GetComponent<Button>().interactable = false;
            button.gameObject.SetActive(false);
        }

        ResetAllIcons();

        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length / 2);
        foreach (Transform button in _cardParent) button.gameObject.SetActive(true);
        yield return new WaitForSeconds(_introClip.length / 2);
        foreach (Transform button in _cardParent) button.GetComponent<Button>().interactable = true;
    }

    public void PlayAudio(int index)
    {
        // Safe Cleanup: Reset colors and turn off icons before jumping to the next button
        ResetAllIcons();

        _currentAudioIndex = index;
        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());

        if (!_clickCheckIndex.Contains(index))
        {
            _clickCheckIndex.Add(index);
            // DYNAMIC FIX: Updates display using real element counts
            _clickedIndexText.text = _clickCheckIndex.Count.ToString() + "/" + _cardParent.childCount.ToString();
        }

        if (_clickCheckIndex.Count == _cardParent.childCount)
        {
            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            _isViewed = true;
        }
    }

    IEnumerator StartButtonAudio()
    {
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = new Color(200f / 255f, 200f / 255f, 200f / 255f, 1.0f);
        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = true;

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

        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = Color.white;
        _cardParent.GetChild(_currentAudioIndex).GetChild(1).GetComponent<Image>().enabled = false;
    }

    // Helper method to guarantee your UI updates cleanly when clicks happen rapidly
    private void ResetAllIcons()
    {
        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Image>().color = Color.white;
            if (button.childCount > 1)
            {
                var iconImg = button.GetChild(1).GetComponent<Image>();
                if (iconImg != null) iconImg.enabled = false;
            }
        }
    }
}