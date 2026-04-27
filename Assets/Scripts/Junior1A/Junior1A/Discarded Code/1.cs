//using System.Collections;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class 1 : MonoBehaviour, Interfaces_Junior1A
//{
//    [SerializeField] AudioSource _audioSource, _audioSourceTabB;
//    [SerializeField] AudioClip[] _audioClips, _audioClipsTabB;
//    [SerializeField] string[] _audioClipData, _audioClipDataTabB;
//    [SerializeField] bool _isViewed = false;
//    [SerializeField] Transform _buttonParent, _buttonParentTabB;
//    [SerializeField] GameObject _nextButton, TabA, TabB;
//    [SerializeField] int _currentAudioIndex = 0, _previousIndex = 0;
//    [SerializeField] TextMeshProUGUI _dataText, _dataTextTabB;
//    Coroutine _coroutine, _audioSettingCoroutine;
//    public bool IsViewed => _isViewed;
//    public void SetAudioClip(int index)
//    {
//        _audioSource.Stop();
//        if (index >= 0 && index < _audioClips.Length)
//        {
//            _audioSource.clip = _audioClips[index];
//            _audioSource.Play();
//            if (_audioSettingCoroutine != null) StopCoroutine(_audioSettingCoroutine);
//            _audioSettingCoroutine = StartCoroutine(AudioSetting(index));
//        }
//        if (index == 7) _nextButton.SetActive(true);
//        _previousIndex = index;
//    }
//    public void SetAudioClipTabB(int index)
//    {
//        _buttonParentTabB.GetChild(_previousIndex).GetChild(1).GetComponent<Image>().enabled = false;
//        _dataTextTabB.gameObject.SetActive(false);
//        _audioSourceTabB.Stop();
//        if (index >= 0 && index < _audioClipsTabB.Length)
//        {
//            _buttonParentTabB.GetChild(index).GetChild(1).GetComponent<Image>().enabled = true;
//            _dataTextTabB.text = _audioClipDataTabB[index];
//            _audioSourceTabB.clip = _audioClipsTabB[index];
//            _dataTextTabB.gameObject.SetActive(true);
//            _audioSourceTabB.Play();
//        }
//        if (index == 5)
//        {
//            GameManager_Junior1A.Instance.Next(true);
//            _isViewed = true;
//        }
//        _previousIndex = index;
//    }
//    public void CloseTabA()
//    {
//        _audioSource.Stop();
//        StopCoroutine(_coroutine);
//        _dataTextTabB.text = "Tap on the Question to know more !";
//        _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<Image>().enabled = false;
//        foreach (Transform child in _buttonParent) child.GetComponent<Button>().interactable = false;
//        foreach (Transform child in _buttonParentTabB) child.GetChild(1).GetComponent<Image>().enabled = false;
//        _previousIndex = 0;
//    }
//    IEnumerator AudioSetting(int index)
//    {
//        _dataText.gameObject.SetActive(false);
//        _buttonParent.GetChild(_previousIndex).GetChild(0).GetComponent<Image>().enabled = false;
//        _buttonParent.GetChild(index).GetChild(0).GetComponent<Image>().enabled = true;
//        _dataText.gameObject.SetActive(true);
//        _dataText.text = _audioClipData[index];
//        yield return new WaitForSeconds(_audioClips[index].length + .5f);
//        _buttonParent.GetChild(index).GetChild(0).GetComponent<Image>().enabled = false;
//        _dataText.gameObject.SetActive(false);
//    }
//    void OnEnable()
//    {
//        _buttonParent.GetChild(_previousIndex).GetChild(0).GetComponent<Image>().enabled = false;
//        _dataText.gameObject.SetActive(false);
//        _coroutine = StartCoroutine(AutoStart());
//        TabA.SetActive(true);
//        TabB.SetActive(false);
//    }
//    IEnumerator AutoStart()
//    {
//        _currentAudioIndex = 0;
//        yield return new WaitForSeconds(2.5f);
//        foreach (AudioClip clip in _audioClips)
//        {
//            _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<Image>().enabled = true;
//            _dataText.gameObject.SetActive(true);
//            _dataText.text = _audioClipData[_currentAudioIndex];
//            _buttonParent.GetChild(_currentAudioIndex).GetComponent<Button>().onClick.Invoke();
//            yield return new WaitForSeconds(clip.length + .5f);
//            _buttonParent.GetChild(_currentAudioIndex).GetChild(0).GetComponent<Image>().enabled = false;
//            _dataText.gameObject.SetActive(false);
//            if (_currentAudioIndex == 7) _nextButton.SetActive(true);
//            _currentAudioIndex++;
//        }
//        _currentAudioIndex = 0;
//        foreach (Transform child in _buttonParent) child.GetComponent<Button>().interactable = true;
//    }
//}
