//using System.Collections;
//using TMPro;
//using UnityEngine;

//public class 2 : MonoBehaviour, Interfaces_Junior1A
//{
//    [SerializeField] AudioSource _audioSource;
//    [SerializeField] AudioClip[] _samAudioClips, _tinaAudioClips;
//    [SerializeField] string[] _samData, _TinaData;
//    [SerializeField] GameObject _sam, _tina, _tap;
//    [SerializeField] bool _isViewed = false;
//    [SerializeField] int _currentDilogueIndex = 0;
//    Coroutine _coroutine;

//    public bool IsViewed => _isViewed;
//    public void ShowCardButton()
//    {
//        if (_coroutine == null) _coroutine = StartCoroutine(ShowCard());
//    }
//    IEnumerator ShowCard()
//    {
//        if (_sam.activeInHierarchy && _tina.activeInHierarchy)
//        {
//            _sam.SetActive(false);
//            _tina.SetActive(false);
//            _currentDilogueIndex++;
//            yield return new WaitForSeconds(.5f);
//        }

//        _tap.SetActive(false);

//        _sam.SetActive(true);
//        _sam.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _samData[_currentDilogueIndex];
//        if (_samAudioClips.Length > 0)
//        {
//            _audioSource.clip = _samAudioClips[_currentDilogueIndex];
//            _audioSource.Play();
//            yield return new WaitForSeconds(_samAudioClips[_currentDilogueIndex].length + .5f);
//        }
//        else yield return new WaitForSeconds(.5f);

//        _tina.SetActive(true);
//        _tina.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = _TinaData[_currentDilogueIndex];
//        if (_tinaAudioClips.Length > 0)
//        {
//            _audioSource.clip = _tinaAudioClips[_currentDilogueIndex];
//            _audioSource.Play();
//            yield return new WaitForSeconds(_tinaAudioClips[_currentDilogueIndex].length + .5f);
//        }
//        else yield return new WaitForSeconds(.5f);

//        StopCoroutine(_coroutine);
//        _coroutine = null;

//        if (_currentDilogueIndex < _samData.Length - 1) _tap.SetActive(true);
//        else
//        {
//            _isViewed = true;
//            GameManager_Junior1A.Instance.Next(true);
//        }
//    }
//    void OnEnable() => Reset_Restart();
//    public void Reset_Restart()
//    {
//        _currentDilogueIndex = 0;
//        _sam.SetActive(false);
//        _tina.SetActive(false);
//        _tap.SetActive(true);
//        if (_coroutine != null) StopCoroutine(_coroutine);
//        _coroutine = null;
//    }
//}
