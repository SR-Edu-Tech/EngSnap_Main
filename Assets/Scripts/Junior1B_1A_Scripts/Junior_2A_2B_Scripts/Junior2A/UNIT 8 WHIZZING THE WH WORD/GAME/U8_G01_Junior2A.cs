using Junior2A;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class U8_G01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [Header("Setup")]
    [SerializeField] Transform _leftButtonParent;
    [SerializeField] Transform _rightButtonParent;

    [Header("Audio Configuration")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    // Element 0 matches Left Button 0, Element 1 matches Left Button 1, etc.
    [SerializeField] AudioClip[] _leftAudioClips = new AudioClip[6];

    // An array of correct pairings. 
    // Element 0 holds the correct Right Button index for Left Button 0, etc.
    [Header("Answer Key Mapping (Left Index -> Right Index)")]
    [SerializeField] int[] _correctMatches = new int[6];

    [Header("Visual Colors")]
    [SerializeField] Color _selectedColor = Color.yellow;
    [SerializeField] Color _correctColor = Color.green;
    [SerializeField] Color _defaultColor = Color.white;

    [Header("State Tracking")]
    [SerializeField] bool _isViewed = false;

    private int _selectedLeftIndex = -1;
    private int _selectedRightIndex = -1;
    private int _successfulMatchesCount = 0;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        StartCoroutine(StarterSequence());
    }

    IEnumerator StarterSequence()
    {
        ResetMatchingGame();

        // Lock out interactions while the intro clip plays
        SetAllButtonsInteractable(false);

        if (_introClip != null && _audioSource != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length + .2f);
        }

        // Unlock buttons for gameplay
        SetAllButtonsInteractable(true);
    }

    public void ResetMatchingGame()
    {
        _selectedLeftIndex = -1;
        _selectedRightIndex = -1;
        _successfulMatchesCount = 0;
        _isViewed = false;

        ResetAllButtonVisuals(_leftButtonParent);
        ResetAllButtonVisuals(_rightButtonParent);
    }

    // Called by Left side buttons on click
    public void SelectLeftButton(int leftIndex)
    {
        if (!_leftButtonParent.GetChild(leftIndex).GetComponent<Button>().interactable) return;

        // Play individual word audio clip for this specific left button item
        PlayIndividualAudio(leftIndex);

        if (_selectedLeftIndex != -1)
            SetButtonColor(_leftButtonParent, _selectedLeftIndex, _defaultColor);

        _selectedLeftIndex = leftIndex;
        SetButtonColor(_leftButtonParent, _selectedLeftIndex, _selectedColor);

        CheckForMatch();
    }

    // Called by Right side buttons on click
    public void SelectRightButton(int rightIndex)
    {
        if (!_rightButtonParent.GetChild(rightIndex).GetComponent<Button>().interactable) return;

        if (_selectedRightIndex != -1)
            SetButtonColor(_rightButtonParent, _selectedRightIndex, _defaultColor);

        _selectedRightIndex = rightIndex;
        SetButtonColor(_rightButtonParent, _selectedRightIndex, _selectedColor);

        CheckForMatch();
    }

    private void PlayIndividualAudio(int index)
    {
        if (_audioSource == null) return;

        _audioSource.Stop();
        if (index >= 0 && index < _leftAudioClips.Length && _leftAudioClips[index] != null)
        {
            _audioSource.clip = _leftAudioClips[index];
            _audioSource.Play();
        }
    }

    private void CheckForMatch()
    {
        if (_selectedLeftIndex == -1 || _selectedRightIndex == -1) return;

        if (_correctMatches[_selectedLeftIndex] == _selectedRightIndex)
        {
            // SUCCESS! 
            GameManager_Junior2A.Instance.Pop();

            SetButtonColor(_leftButtonParent, _selectedLeftIndex, _correctColor);
            SetButtonColor(_rightButtonParent, _selectedRightIndex, _correctColor);

            _leftButtonParent.GetChild(_selectedLeftIndex).GetComponent<Button>().interactable = false;
            _rightButtonParent.GetChild(_selectedRightIndex).GetComponent<Button>().interactable = false;

            _successfulMatchesCount++;

            _selectedLeftIndex = -1;
            _selectedRightIndex = -1;

            if (_successfulMatchesCount >= 6 && !_isViewed)
            {
                _isViewed = true;
                GameManager_Junior2A.Instance.Next(true);
            }
        }
        else
        {
            // MISMATCH!
            GameManager_Junior2A.Instance.Woosh();
            StartCoroutine(ClearMismatchDelayed(_selectedLeftIndex, _selectedRightIndex));

            _selectedLeftIndex = -1;
            _selectedRightIndex = -1;
        }
    }

    IEnumerator ClearMismatchDelayed(int leftIdx, int rightIdx)
    {
        yield return new WaitForSeconds(0.4f);

        if (_leftButtonParent.GetChild(leftIdx).GetComponent<Button>().interactable)
            SetButtonColor(_leftButtonParent, leftIdx, _defaultColor);

        if (_rightButtonParent.GetChild(rightIdx).GetComponent<Button>().interactable)
            SetButtonColor(_rightButtonParent, rightIdx, _defaultColor);
    }

    private void SetButtonColor(Transform parent, int index, Color targetColor)
    {
        if (parent.GetChild(index).GetComponent<Image>() != null)
        {
            parent.GetChild(index).GetComponent<Image>().color = targetColor;
        }
    }

    private void SetAllButtonsInteractable(bool state)
    {
        foreach (Transform child in _leftButtonParent)
        {
            Button b = child.GetComponent<Button>();
            if (b != null) b.interactable = state;
        }
        foreach (Transform child in _rightButtonParent)
        {
            Button b = child.GetComponent<Button>();
            if (b != null) b.interactable = state;
        }
    }

    private void ResetAllButtonVisuals(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null) btn.interactable = true;

            Image img = child.GetComponent<Image>();
            if (img != null) img.color = _defaultColor;
        }
    }
}