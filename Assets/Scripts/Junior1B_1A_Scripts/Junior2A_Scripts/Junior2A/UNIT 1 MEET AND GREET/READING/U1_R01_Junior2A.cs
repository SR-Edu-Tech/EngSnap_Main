using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U1_R01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] Transform _buttonParent, _optionParent;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] Color defaultColor;
    [SerializeField] bool _isViewed, _isSlowed;

    // Track which buttons the user has clicked so we know when they've listened to everything
    private bool[] _hasListenedToClip;
    Coroutine _setCoroutine;

    public bool IsViewed => _isViewed;

    void Awake()
    {
        // Initialize our tracking array based on how many audio clips we have
        _hasListenedToClip = new bool[_audioClips.Length];

        // Safety check: If default color is completely transparent, set a default fallback (e.g., dark grey)
        if (defaultColor.a == 0f)
        {
            defaultColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        }
    }

    void OnEnable()
    {
        // Reset listening tracking when the slide opens
        for (int i = 0; i < _hasListenedToClip.Length; i++) _hasListenedToClip[i] = false;
        StartCoroutine(Starter());
    }

    IEnumerator Starter()
    {
        foreach (Transform button in _buttonParent) button.GetComponent<PopEffect_Junior2A>().enabled = true;
        _optionParent.GetChild(0).GetComponent<PopEffect_Junior2A>().enabled = true;

        _audioSource.clip = _introClip;
        _audioSource.Play();

        _optionParent.GetChild(1).gameObject.SetActive(false);

        // Lock buttons during intro audio
        foreach (Transform child in _buttonParent) child.GetComponent<Button>().interactable = false;

        ResetAllButtons();

        yield return new WaitForSeconds(_introClip.length);

        // Intro done! Enable buttons and show the extra options (Repeat/Slow buttons) right away
        _optionParent.GetChild(1).gameObject.SetActive(true);
        foreach (Transform child in _buttonParent) child.GetComponent<Button>().interactable = true;
    }

    public void SetAudioClip(int index)
    {
        _audioSource.Stop();
        if (index >= 0 && index < _audioClips.Length)
        {
            if (_setCoroutine != null) StopCoroutine(_setCoroutine);

            // Instantly clean up everything before applying the new highlight state
            ResetAllButtons();

            _audioSource.clip = _audioClips[index];
            _audioSource.Play();

            _setCoroutine = StartCoroutine(SetText(index));

            // Track completion tracking
            TrackListeningCompletion(index);
        }
    }

    IEnumerator SetText(int index)
    {
        _currentAudioIndex = index;

        // Set up the CURRENT clicked button highlight
        Transform currentButton = _buttonParent.GetChild(index);
        currentButton.GetChild(1).GetComponent<Image>().enabled = true;

        TextMeshProUGUI textMesh = currentButton.GetChild(0).GetComponent<TextMeshProUGUI>();
        textMesh.fontStyle = FontStyles.Italic;

        if (ColorUtility.TryParseHtmlString("#14799E", out Color myColor))
            textMesh.color = myColor;

        yield return new WaitForSeconds((_audioClips[index].length / _audioSource.pitch) + 0.5f);

        // Revert styling back to normal after audio finishes playing
        currentButton.GetChild(1).GetComponent<Image>().enabled = false;
        textMesh.color = defaultColor;
        textMesh.fontStyle = FontStyles.Normal;
    }

    // Safely resets every button back to its standard inactive visual style
    private void ResetAllButtons()
    {
        foreach (Transform button in _buttonParent)
        {
            // Turn off highlight image overlay
            if (button.childCount > 1)
            {
                var highlightImg = button.GetChild(1).GetComponent<Image>();
                if (highlightImg != null) highlightImg.enabled = false;
            }

            // Reset text styling and color
            if (button.childCount > 0)
            {
                var textMesh = button.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (textMesh != null)
                {
                    textMesh.color = defaultColor;
                    textMesh.fontStyle = FontStyles.Normal;
                }
            }
        }
    }

    void TrackListeningCompletion(int index)
    {
        _hasListenedToClip[index] = true;

        // Check if all clips have been selected at least once
        bool allClipsHeard = true;
        for (int i = 0; i < _hasListenedToClip.Length; i++)
        {
            if (!_hasListenedToClip[i])
            {
                allClipsHeard = false;
                break;
            }
        }

        if (allClipsHeard && !_isViewed)
        {
            _isViewed = true;
            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
        }
    }

    public void Repeat()
    {
        SetAudioClip(_currentAudioIndex);
    }

    public void Slow(TextMeshProUGUI text)
    {
        text.text = _isSlowed ? "    SLOW" : "    FAST";
        _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }
}