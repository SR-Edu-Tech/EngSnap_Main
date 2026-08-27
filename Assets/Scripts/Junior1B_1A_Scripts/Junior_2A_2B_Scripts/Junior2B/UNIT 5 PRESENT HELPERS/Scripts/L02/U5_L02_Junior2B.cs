using Junior2B;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U5_L02_Junior2B : MonoBehaviour, Interfaces_Junior2B
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
        _tab1.anchoredPosition = Vector3.left * 300;
        _tab2.anchoredPosition = Vector3.right * 300;
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

        _tab1.localScale = Vector3.one;
        _tab2.localScale = Vector3.one;
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

        // FIX 1: Absolutely DO NOT change anchorMin/anchorMax here anymore!
        // This stops Unity from throwing weird layout scaling spikes.

        _tab1.localScale = Vector3.one;
        _tab2.localScale = Vector3.one;

        // Record starting center-anchored positions
        Vector2 startPos1 = _tab1.anchoredPosition;
        Vector2 startPos2 = _tab2.anchoredPosition;

        // FIX 2: Set clean, predictable target heights relative to center anchoring.
        // Adjust the '350f' value up or down if you want them higher or lower on screen!
        Vector2 targetPos1 = new Vector2(startPos1.x, 250f);
        Vector2 targetPos2 = new Vector2(startPos2.x, 250f);

        CanvasGroup cg1 = _tab1P != null ? _tab1P.GetComponent<CanvasGroup>() : null;
        CanvasGroup cg2 = _tab2P != null ? _tab2P.GetComponent<CanvasGroup>() : null;

        float slideSpeed = 2.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;
            float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

            _tab1.anchoredPosition = Vector2.Lerp(startPos1, targetPos1, easedT);
            _tab2.anchoredPosition = Vector2.Lerp(startPos2, targetPos2, easedT);

            // Ensure local scale properties remain perfectly stable
            _tab1.localScale = Vector3.one;
            _tab2.localScale = Vector3.one;

            if (cg1 != null) cg1.alpha = easedT;
            if (cg2 != null) cg2.alpha = easedT;

            yield return null;
        }

        _tab1.anchoredPosition = targetPos1;
        _tab2.anchoredPosition = targetPos2;

        _tab1.localScale = Vector3.one;
        _tab2.localScale = Vector3.one;

        if (cg1 != null) cg1.alpha = 1;
        if (cg2 != null) cg2.alpha = 1;

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
            GameManager_Junior2B.Instance.Next(true);
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

        Transform contentParent = currentTabP.GetChild(0);
        if (index >= contentParent.childCount) return;

        Transform targetButton = contentParent.GetChild(index);
        Sprite btnSprite = null;

        if (targetButton.childCount > 0 && targetButton.GetChild(0).childCount > 0 && targetButton.GetChild(0).GetChild(0).childCount > 0)
        {
            var imgComp = targetButton.GetChild(0).GetChild(0).GetComponent<Image>();
            if (imgComp != null) btnSprite = imgComp.sprite;
        }

        if (index % 2 == 0)
        {
            Transform displayLeft = currentTabP.GetChild(1);
            if (displayLeft != null)
            {
                // CRITICAL FIX: Explicitly break any lingering scale overrides from pop scripts
                displayLeft.localScale = Vector3.one;

                if (btnSprite != null && displayLeft.GetComponent<Image>() != null) displayLeft.GetComponent<Image>().sprite = btnSprite;
                if (displayLeft.TryGetComponent(out Popeffect_Junior2B popLeft))
                {
                    popLeft.enabled = false;
                    popLeft.enabled = true;
                }
            }
        }
        else
        {
            Transform displayRight = currentTabP.GetChild(2);
            if (displayRight != null)
            {
                // CRITICAL FIX: Explicitly break any lingering scale overrides from pop scripts
                displayRight.localScale = Vector3.one;

                if (btnSprite != null && displayRight.GetComponent<Image>() != null) displayRight.GetComponent<Image>().sprite = btnSprite;
                if (displayRight.TryGetComponent(out Popeffect_Junior2B popRight))
                {
                    popRight.enabled = false;
                    popRight.enabled = true;
                }
            }
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

