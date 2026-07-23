using Junior2A;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U1_R02_Junior2A : MonoBehaviour, Interfaces_Junior2A
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
        // Deactivate and prep all buttons, hidden until their conversation step arrives
        foreach (Transform button in _buttonParent)
        {
            button.GetComponent<PopEffect_Junior2A>().enabled = true;
            button.GetComponent<Button>().interactable = false;
            button.gameObject.SetActive(false);
        }
        _samTextObj.gameObject.SetActive(false);
        _tinaTextObj.gameObject.SetActive(false);
        _currentAudioIndex = 0;

        // 1. Play master introduction audio
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length + .5f);

        // 2. Play Sam's initial question/prompt sequence
        _samTextObj.gameObject.SetActive(true);
        _audioSource.clip = _samClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_samClip.length + .5f);
        _samTextObj.gameObject.SetActive(false);

        // 3. Set up the first interactive step (Tina Box opens with Clip 0's text)
        SetupConversationStep(0);
    }

    /// <summary>
    /// Configures the text box UI and reveals the clickable button for the current index.
    /// </summary>
    void SetupConversationStep(int index)
    {
        _currentAudioIndex = index;

        // Safety check to ensure we haven't reached past our clip array limits
        if (_currentAudioIndex >= _audioClips.Length)
        {
            _tinaTextObj.gameObject.SetActive(false);
            _samTextObj.gameObject.SetActive(false);
            _isViewed = true;
            GameManager_Junior2A.Instance.Next(true);
            return;
        }

        // Clean up text boxes before switching
        _tinaTextObj.gameObject.SetActive(false);
        _samTextObj.gameObject.SetActive(false);

        AudioClip clip = _audioClips[_currentAudioIndex];

        // Turn-based logic: Even indices = Tina opens, Odd indices = Sam opens
        if (_currentAudioIndex % 2 == 0)
        {
            _tinaTextObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = clip.name;
            _tinaTextObj.gameObject.SetActive(true);
        }
        else
        {
            _samTextObj.GetChild(0).GetComponent<TextMeshProUGUI>().text = clip.name;
            _samTextObj.gameObject.SetActive(true);
        }

        // Calculate visual button placement hierarchy index matching your container logic
        int buttonHierarchyIndex = (_buttonParent.childCount - 1) - _currentAudioIndex;

        if (buttonHierarchyIndex >= 0 && buttonHierarchyIndex < _buttonParent.childCount)
        {
            GameObject currentButton = _buttonParent.GetChild(buttonHierarchyIndex).gameObject;
            currentButton.SetActive(true);
            currentButton.GetComponent<Button>().interactable = true;
            _buttonParent.GetChild(buttonHierarchyIndex).GetChild(1).GetComponent<Image>().enabled = false;
        }
    }

    public void PlayAudio(int index)
    {
        // Block processing if user attempts to click a previously completed button out of sync
        if (index != _currentAudioIndex) return;

        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
    }

    IEnumerator StartButtonAudio()
    {
        int buttonHierarchyIndex = (_buttonParent.childCount - 1) - _currentAudioIndex;

        // Turn off interactable and trigger the visual selector border accent
        _buttonParent.GetChild(buttonHierarchyIndex).GetComponent<Button>().interactable = false;
        _buttonParent.GetChild(buttonHierarchyIndex).GetChild(1).GetComponent<Image>().enabled = true;

        // Play the matched phrase audio clip
        _audioSource.clip = _audioClips[_currentAudioIndex];
        _audioSource.Play();

        yield return new WaitForSeconds(_audioClips[_currentAudioIndex].length + .5f);

        // Turn off selection border highlight
        _buttonParent.GetChild(buttonHierarchyIndex).GetChild(1).GetComponent<Image>().enabled = false;

        // Advance the conversation pipeline forward to the next index slot
        SetupConversationStep(_currentAudioIndex + 1);
    }
}