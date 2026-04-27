using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class U2_L01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] int _currentClipIndex = 0;
    [SerializeField] Transform _questionParent, _responseParent;
    public bool IsViewed => _isViewed;

    void OnEnable() => StartCoroutine(AutoStart());

    IEnumerator AutoStart()
    {

        foreach (Transform obj in _questionParent) obj.gameObject.SetActive(false);
        foreach (Transform obj in _responseParent) obj.gameObject.SetActive(false);
        yield return new WaitForSeconds(2);
        foreach (AudioClip clip in _audioClips)
        {
            _audioSource.clip = clip;
            _audioSource.Play();
            _questionParent.GetChild(_currentClipIndex).gameObject.SetActive(true);
            _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = true;
            yield return new WaitForSeconds(clip.length / 2);
            _questionParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
            _responseParent.GetChild(_currentClipIndex).gameObject.SetActive(true);
            _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = true;
            yield return new WaitForSeconds(clip.length / 2);
            _responseParent.GetChild(_currentClipIndex).GetChild(1).GetComponent<Image>().enabled = false;
            _currentClipIndex++;
        }
        _currentClipIndex = 0;
    }
}
