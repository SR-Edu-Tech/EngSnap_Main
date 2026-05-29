using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U2_L01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false, _isSlowed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] int _currentClipIndex = 0;
    [SerializeField] Transform _questionParent, _responseParent, _optionParent;
    Coroutine _audioCoroutine, _repeatAudio;
    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(AutoStart());

    IEnumerator AutoStart()
    {
        _optionParent.GetChild(0).gameObject.SetActive(false);
        foreach (Transform obj in _questionParent) obj.gameObject.SetActive(false);
        foreach (Transform obj in _responseParent) obj.gameObject.SetActive(false);
        yield return new WaitForSeconds(2);
        _currentClipIndex = 0;
        foreach (AudioClip clip in _audioClips)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
            _questionParent.GetChild(_currentClipIndex).GetComponent<PopEffect_Junior1A>().enabled = true;
            _questionParent.GetChild(_currentClipIndex).gameObject.SetActive(true);
            _questionParent.GetChild(_currentClipIndex).GetComponent<Button>().interactable = false;
            _questionParent.GetChild(_currentClipIndex).GetComponent<Image>().color = new Color(0.843f, 1.0f, 0.380f, 1.0f);
            _responseParent.GetChild(_currentClipIndex).GetComponent<Image>().color = new Color(0.843f, 1.0f, 0.380f, 1.0f);
            _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = true;
            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = clip.length / pV1;
            yield return new WaitForSeconds(aL1 / 2);
            _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
            _responseParent.GetChild(_currentClipIndex).GetComponent<PopEffect_Junior1A>().enabled = true;
            _responseParent.GetChild(_currentClipIndex).gameObject.SetActive(true);
            _responseParent.GetChild(_currentClipIndex).GetComponent<Button>().interactable = false;
            _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = true;
            float pV2 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL2 = clip.length / pV2;
            yield return new WaitForSeconds(aL2 / 2);
            _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
            _questionParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
            _responseParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
            _currentClipIndex++;
        }
        _currentClipIndex = 0;
        _isViewed = true;
        GameManager_Junior1A.Instance.Next(true);
        foreach (Transform option in _questionParent) option.GetComponent<Button>().interactable = true;
        foreach (Transform option in _responseParent) option.GetComponent<Button>().interactable = true;
        _optionParent.GetChild(0).gameObject.SetActive(true);

    }
    public void PlayAudio(int index)
    {
        _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _questionParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
        _responseParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
        _currentClipIndex = index;
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(AudioPlayer());
    }
    IEnumerator AudioPlayer()
    {
        if (_repeatAudio != null) StopCoroutine(_repeatAudio);
        _audioSource.clip = _audioClips[_currentClipIndex];
        _audioSource.Play();
        _questionParent.GetChild(_currentClipIndex).gameObject.SetActive(true);
        _questionParent.GetChild(_currentClipIndex).GetComponent<Image>().color = new Color(0.843f, 1.0f, 0.380f, 1.0f);
        _responseParent.GetChild(_currentClipIndex).GetComponent<Image>().color = new Color(0.843f, 1.0f, 0.380f, 1.0f);
        _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = true;
        float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        float aL1 = _audioSource.clip.length / pV1;
        yield return new WaitForSeconds(aL1 / 2);
        _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _responseParent.GetChild(_currentClipIndex).gameObject.SetActive(true);
        _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = true;
        float pV2 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        float aL2 = _audioSource.clip.length / pV2;
        yield return new WaitForSeconds(aL2 / 2);
        _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _questionParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
        _responseParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
    }
    public void Repeat()
    {
        _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
        _questionParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
        _responseParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
        if (_repeatAudio != null) StopCoroutine(_repeatAudio);
        _repeatAudio = StartCoroutine(RepeatAudio());
    }
    IEnumerator RepeatAudio()
    {
        _currentClipIndex = 0;
        foreach (AudioClip clip in _audioClips)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
            _questionParent.GetChild(_currentClipIndex).GetComponent<Image>().color = new Color(0.843f, 1.0f, 0.380f, 1.0f);
            _responseParent.GetChild(_currentClipIndex).GetComponent<Image>().color = new Color(0.843f, 1.0f, 0.380f, 1.0f);
            _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = true;
            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _audioSource.clip.length / pV1;
            yield return new WaitForSeconds(aL1 / 2);
            _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
            _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = true;
            float pV2 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL2 = _audioSource.clip.length / pV2;
            yield return new WaitForSeconds(aL2 / 2);
            _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
            _questionParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
            _responseParent.GetChild(_currentClipIndex).GetComponent<Image>().color = Color.white;
            _currentClipIndex++;
        }
        _currentClipIndex = 0;
        yield return null;
    }
    public void Slow(TextMeshProUGUI text)
    {
        text.text = _isSlowed ? "      SLOW" : "       FAST";
        _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }
}
