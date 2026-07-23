using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U6_L02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false, _slided = false, _tab1Opened = false, _tab2Opened = false;
    [SerializeField] RectTransform _tab1, _tab2;
    [SerializeField] GameObject _tab1P, _tab2P;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _tab1AudioClips, _tab2AudioClips;
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex, _currentTabIndex;

    Coroutine _audioCoroutine, _tabChange;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        _tab1.anchoredPosition = Vector3.left * 200;
        _tab2.anchoredPosition = Vector3.right * 200;
        _tab1.anchorMin = new Vector2(.5f, .5f);
        _tab1.anchorMax = new Vector2(.5f, .5f);
        _tab2.anchorMin = new Vector2(.5f, .5f);
        _tab2.anchorMax = new Vector2(.5f, .5f);
        _tab1.GetComponent<Image>().color = _tab2.GetComponent<Image>().color = Color.white;
        _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        if (_currentTabIndex == 0) _tab1P.GetComponent<CanvasGroup>().alpha = 0;
        else _tab2P.GetComponent<CanvasGroup>().alpha = 0;
        
        _slided = _tab1Opened = _tab2Opened = false;

        _audioSource.clip = _introClip;
        _audioSource.Play();
    }

    public void TabSlideUp(int index)
    {
        if (index == 0)
        {
            _tab1.GetComponent<Image>().color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
            _tab2.GetComponent<Image>().color = Color.white;
            _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
            _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }
        else
        {
            _tab1.GetComponent<Image>().color = Color.white;
            _tab2.GetComponent<Image>().color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
            _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
        }
        
        _audioSource.Stop();
        if (_slided)
        {
            if (index == _currentTabIndex) return;
            _currentTabIndex = index;
            if (_currentTabIndex == 0) foreach (Transform child in _tab1P.transform) child.gameObject.SetActive(false);
            else foreach (Transform child in _tab2P.transform) child.gameObject.SetActive(false);
            if (_tabChange != null) StopCoroutine(_tabChange);
            _tabChange = StartCoroutine(OnTab());
            return;
        }
        _currentTabIndex = index;
        StartCoroutine(SlideTabUp());
    }

    IEnumerator SlideTabUp()
    {
        _slided = true;

        if (_currentTabIndex == 0) foreach (Transform child in _tab1P.transform) child.gameObject.SetActive(false);
        else foreach (Transform child in _tab2P.transform) child.gameObject.SetActive(false);

        Vector3 worldPos1 = _tab1.position;
        Vector3 worldPos2 = _tab2.position;

        _tab1.anchorMin = new Vector2(.5f, 1);
        _tab1.anchorMax = new Vector2(.5f, 1);
        _tab2.anchorMin = new Vector2(.5f, 1);
        _tab2.anchorMax = new Vector2(.5f, 1);

        _tab1.position = worldPos1;
        _tab2.position = worldPos2;

        Vector2 startPos1 = _tab1.anchoredPosition;
        Vector2 startPos2 = _tab2.anchoredPosition;
        Vector2 targetPos1 = new Vector2(startPos1.x, -250f);
        Vector2 targetPos2 = new Vector2(startPos2.x, -250f);

        CanvasGroup cg1 = _tab1P.GetComponent<CanvasGroup>();
        CanvasGroup cg2 = _tab2P.GetComponent<CanvasGroup>();

        float slideSpeed = 2.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;
            float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
            _tab1.anchoredPosition = Vector2.LerpUnclamped(startPos1, targetPos1, easedT);
            _tab2.anchoredPosition = Vector2.LerpUnclamped(startPos2, targetPos2, easedT);
            cg1.alpha = easedT;
            cg2.alpha = easedT;

            yield return null;
        }

        _tab1.anchoredPosition = targetPos1;
        _tab2.anchoredPosition = targetPos2;

        cg1.alpha = 1;
        cg2.alpha = 1;

        _tabChange = StartCoroutine(OnTab());
    }

    IEnumerator OnTab()
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        Transform currentTabP = _currentTabIndex == 0 ? _tab1P.transform : _tab2P.transform;

        currentTabP.GetChild(0).GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        foreach (Transform child in currentTabP.GetChild(0)) child.gameObject.SetActive(false);
        foreach (Transform child in currentTabP) child.gameObject.SetActive(true);

        yield return new WaitForSeconds(1);

        foreach (Transform child in currentTabP.GetChild(0))
        {
            child.gameObject.SetActive(true);
            child.GetComponent<Button>().interactable = false;
            yield return new WaitForSeconds(.25f);
        }

        yield return new WaitForSeconds(.25f);
        
        _tabChange = StartCoroutine(AutoRunAudios());
    }

    IEnumerator AutoRunAudios()
    {     
        Transform currentTabP = _currentTabIndex == 0 ? _tab1P.transform : _tab2P.transform;
        Transform content = currentTabP.GetChild(0);
        int btnCount = content.childCount;
        AudioClip[] currentClips = _currentTabIndex == 0 ? _tab1AudioClips : _tab2AudioClips;

        foreach (Transform child in content) child.GetComponent<Button>().interactable = false;
        foreach (Transform child in content)
        {
            child.GetComponent<Button>().onClick.Invoke();
            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float waitTime = (_audioSource.clip.length / pV1) + 0.5f;
            yield return new WaitForSeconds(waitTime);
        }

        foreach (Transform child in content) child.GetComponent<Button>().interactable = true;

        if (_currentTabIndex == 0) _tab1Opened = true;
        else _tab2Opened = true;

        if (_tab1Opened && _tab2Opened && !_isViewed)
        {
            _isViewed = true;
            GameManager_Junior1A.Instance.Next(true);
        }
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
        Transform currentTabP = _currentTabIndex == 0 ? _tab1P.transform : _tab2P.transform;

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
        AudioClip[] currentClips = _currentTabIndex == 0 ? _tab1AudioClips : _tab2AudioClips;
        _audioSource.clip = currentClips[_currentAudioClipIndex];
        _audioSource.Play();
        float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        float aL1 = _audioSource.clip.length / pV1;
        yield return new WaitForSeconds(aL1);
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }
}

