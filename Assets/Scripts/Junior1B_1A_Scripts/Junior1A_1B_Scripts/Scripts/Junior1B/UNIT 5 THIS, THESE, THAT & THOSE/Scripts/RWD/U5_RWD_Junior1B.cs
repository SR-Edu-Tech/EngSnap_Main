using System.Collections;
using TMPro;
using UnityEngine;

public class U5_RWD_Junior1B : MonoBehaviour
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _starClip, _overClip;
    [SerializeField] GameObject _starParent, _topicObj, _nextAdv, _replay;
    [SerializeField] int _currentTopicIndex = 0;

    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        _nextAdv.SetActive(false);
        _replay.SetActive(false);
        foreach (Transform obj in _starParent.transform) obj.gameObject.SetActive(false);
        foreach (Transform obj in _starParent.transform)
        {
            _audioSource.clip = _starClip;
            _audioSource.Play();
            _topicObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = ((Topics)_currentTopicIndex).ToString();
            _topicObj.SetActive(false);
            _topicObj.SetActive(true);
            obj.gameObject.SetActive(true);
            yield return new WaitForSeconds(_starClip.length);
            _currentTopicIndex++;
            _audioSource.pitch += .1f;
        }
        _currentTopicIndex = 0;
        _audioSource.pitch = 1;
        _audioSource.clip = _overClip;
        _audioSource.Play();
        _topicObj.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "YOU KNOW YOUR SINGULAR & PLURAL NOUNS! YOU ARE AMAZING!!";
        _topicObj.SetActive(false);
        _topicObj.SetActive(true);
        _nextAdv.SetActive(true);
        _replay.SetActive(true);
    }
    public void Replay() { }
}
