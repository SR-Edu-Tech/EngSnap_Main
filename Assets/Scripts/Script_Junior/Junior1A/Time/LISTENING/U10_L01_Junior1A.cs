using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class U10_L01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false, _tab1Opened = false;
    [SerializeField] GameObject _tab;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _tab1AudioClips;
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex;

    Coroutine _audioCoroutine, _autoRun;

    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());
    IEnumerator Starter()
    {
        foreach (Transform child in _tab.transform.GetChild(0)) child.GetComponent<Button>().interactable = false;
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length);
        _autoRun = StartCoroutine(AutoRunAudios());
    }

    IEnumerator AutoRunAudios()
    {
        foreach (Transform child in _tab.transform.GetChild(0))
        {
            child.GetComponent<Button>().onClick.Invoke();
            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float waitTime = (_audioSource.clip.length / pV1) + 0.5f;
            yield return new WaitForSeconds(waitTime);
        }

        foreach (Transform child in _tab.transform.GetChild(0)) child.GetComponent<Button>().interactable = true;

        _isViewed = true;
        GameManager_Junior1A.Instance.Next(true);
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    public void PlayAudio(int index)
    {
        _currentAudioClipIndex = index;
        Transform currentTabP = _tab.transform;

        Sprite btnSprite = currentTabP.GetChild(0).GetChild(index).GetChild(0).GetChild(0).GetComponent<Image>().sprite;

        if (index % 2 == 0)
        {
            currentTabP.GetChild(1).GetComponent<Image>().sprite = btnSprite;
            currentTabP.GetChild(1).GetComponent<PopEffect_Junior1A>().enabled = false;
            currentTabP.GetChild(1).GetComponent<PopEffect_Junior1A>().enabled = true;
        }
        else
        {
            currentTabP.GetChild(2).GetComponent<Image>().sprite = btnSprite;
            currentTabP.GetChild(2).GetComponent<PopEffect_Junior1A>().enabled = false;
            currentTabP.GetChild(2).GetComponent<PopEffect_Junior1A>().enabled = true;
        }

        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex());
    }

    IEnumerator PlayAudioIndex()
    {
        _audioSource.clip = _tab1AudioClips[_currentAudioClipIndex];
        _audioSource.Play();
        float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        float aL1 = _audioSource.clip.length / pV1;
        yield return new WaitForSeconds(aL1);
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }
}
