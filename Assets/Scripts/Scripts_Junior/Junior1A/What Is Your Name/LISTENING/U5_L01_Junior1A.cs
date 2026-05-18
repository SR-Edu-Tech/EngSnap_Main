using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U5_L01_Junior1A : MonoBehaviour
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _clips;
    [SerializeField] int _currentAudioIndex = 0;
    [SerializeField] Transform _cardParent;
    [SerializeField] bool _isViewed = false, _isSlowed = false;
    Coroutine _coroutine, _repeatCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        foreach (Transform button in _cardParent) button.gameObject.SetActive(false);
        transform.GetChild(transform.childCount - 1).GetChild(1).gameObject.SetActive(false);
        _audioSource.clip = _introClip;
        _audioSource.Play();
        yield return new WaitForSeconds(_introClip.length);
        foreach (Transform button in _cardParent)
        {
            button.gameObject.SetActive(true);
            button.GetComponent<PopEffect_Junior1A>().enabled = true;
            yield return new WaitForSeconds(.1f);
        }
        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Button>().onClick.Invoke();
            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _clips[_currentAudioIndex].length / pV1;
            yield return new WaitForSeconds(aL1);
        }
        foreach (Transform button in _cardParent) button.GetComponent<Button>().interactable = true;
        transform.GetChild(transform.childCount - 1).GetChild(1).gameObject.SetActive(true);
        GameManager_Junior1A.Instance.Next(true);
        _isViewed = true;
    }
    public void PlayAudio(int index)
    {
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = Color.white;
        _cardParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
        _currentAudioIndex = index;
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(StartButtonAudio());
    }
    IEnumerator StartButtonAudio()
    {
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
        _cardParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<Image>().enabled = true;
        _audioSource.clip = _clips[_currentAudioIndex];
        _audioSource.Play();

        float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        float aL1 = _clips[_currentAudioIndex].length / pV1;
        yield return new WaitForSeconds(aL1);

        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = Color.white;
        _cardParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
    }
    public void Repeat()
    {
        _cardParent.GetChild(_currentAudioIndex).GetComponent<Image>().color = Color.white;
        _cardParent.GetChild(_currentAudioIndex).GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
        _repeatCoroutine = StartCoroutine(RepeatAudio());
    }
    IEnumerator RepeatAudio()
    {
        _currentAudioIndex = 0;
        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Image>().color = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
            button.GetChild(0).GetChild(0).GetComponent<Image>().enabled = true;
            _audioSource.clip = _clips[_currentAudioIndex];
            _audioSource.Play();

            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _clips[_currentAudioIndex].length / pV1;
            yield return new WaitForSeconds(aL1);

            button.GetComponent<Image>().color = Color.white;
            button.GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
            _currentAudioIndex++;
        }
        _currentAudioIndex = 0;
    }
    public void Slow(TextMeshProUGUI text)
    {
        text.text = _isSlowed ? "    SLOW" : "    FAST";
        _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }
}
