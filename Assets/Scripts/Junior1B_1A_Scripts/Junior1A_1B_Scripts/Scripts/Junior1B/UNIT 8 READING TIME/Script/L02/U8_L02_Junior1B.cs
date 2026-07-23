using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U8_L02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] bool _isViewed = false, _slided = false;
    [SerializeField] bool _tab1Opened = false, _tab2Opened = false, _tab3Opened = false;

    [Header("=== Tab Button UI Rects ===")]
    [SerializeField] private RectTransform _tab1;
    [SerializeField] private RectTransform _tab2;
    [SerializeField] private RectTransform _tab3;

    [Header("=== Tab Content Container Panels ===")]
    [SerializeField] private GameObject _tab1P;
    [SerializeField] private GameObject _tab2P;
    [SerializeField] private GameObject _tab3P;

    [Header("=== Audio Setup Elements ===")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip[] _tab1AudioClips;
    [SerializeField] private AudioClip[] _tab2AudioClips;
    [SerializeField] private AudioClip[] _tab3AudioClips;

    [Header("=== Tracking States ===")]
    [SerializeField] private Image _currentSpeakerIcon;
    [SerializeField] private int _currentAudioClipIndex;
    [SerializeField] private int _currentTabIndex;

    private Coroutine _audioCoroutine;
    private Coroutine _tabChange;

    // Custom scale vector to enforce 0.8 size uniformly
    private readonly Vector3 targetScale80 = new Vector3(0.8f, 0.8f, 0.8f);

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        // 1. Distribute three tabs across the screen cleanly relative to center
        _tab1.anchoredPosition = Vector3.left * 350f;
        _tab2.anchoredPosition = Vector3.zero;
        _tab3.anchoredPosition = Vector3.right * 350f;

        // 2. Uniform center-anchoring setup to ensure zero scale distortion
        _tab1.anchorMin = _tab1.anchorMax = new Vector2(.5f, .5f);
        _tab2.anchorMin = _tab2.anchorMax = new Vector2(.5f, .5f);
        _tab3.anchorMin = _tab3.anchorMax = new Vector2(.5f, .5f);

        // 3. Clear colors back to unselected default states
        _tab1.GetComponent<Image>().color = Color.white;
        _tab2.GetComponent<Image>().color = Color.white;
        _tab3.GetComponent<Image>().color = Color.white;

        Color textDarkGray = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f);
        _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = textDarkGray;
        _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = textDarkGray;
        _tab3.GetChild(0).GetComponent<TextMeshProUGUI>().color = textDarkGray;

        // 4. Hide all container alphas initially depending on starting index selections
        if (_tab1P != null && _tab1P.TryGetComponent(out CanvasGroup cg1)) cg1.alpha = (_currentTabIndex == 0) ? 1f : 0f;
        if (_tab2P != null && _tab2P.TryGetComponent(out CanvasGroup cg2)) cg2.alpha = (_currentTabIndex == 1) ? 1f : 0f;
        if (_tab3P != null && _tab3P.TryGetComponent(out CanvasGroup cg3)) cg3.alpha = (_currentTabIndex == 2) ? 1f : 0f;

        _slided = _tab1Opened = _tab2Opened = _tab3Opened = false;

        _audioSource.clip = _introClip;
        _audioSource.Play();

        // FIX: Set initial scale to 0.8
        _tab1.localScale = targetScale80;
        _tab2.localScale = targetScale80;
        _tab3.localScale = targetScale80;
    }

    public void TabSlideUp(int index)
    {
        Color selectedBlue = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1f);
        Color unselectedText = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f);

        // Manage button selected coloring states across 3 tabs completely
        _tab1.GetComponent<Image>().color = (index == 0) ? selectedBlue : Color.white;
        _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = (index == 0) ? Color.white : unselectedText;

        _tab2.GetComponent<Image>().color = (index == 1) ? selectedBlue : Color.white;
        _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = (index == 1) ? Color.white : unselectedText;

        _tab3.GetComponent<Image>().color = (index == 2) ? selectedBlue : Color.white;
        _tab3.GetChild(0).GetComponent<TextMeshProUGUI>().color = (index == 2) ? Color.white : unselectedText;

        _audioSource.Stop();

        if (_slided)
        {
            if (index == _currentTabIndex) return;
            _currentTabIndex = index;

            ClearContainerChildren(_tab1P);
            ClearContainerChildren(_tab2P);
            ClearContainerChildren(_tab3P);

            if (_tabChange != null) StopCoroutine(_tabChange);
            _tabChange = StartCoroutine(OnTab());
            return;
        }

        _currentTabIndex = index;
        StartCoroutine(SlideTabUp());
    }

    private void ClearContainerChildren(GameObject parentObj)
    {
        if (parentObj != null)
        {
            foreach (Transform child in parentObj.transform) child.gameObject.SetActive(false);
        }
    }

    IEnumerator SlideTabUp()
    {
        _slided = true;

        ClearContainerChildren(_tab1P);
        ClearContainerChildren(_tab2P);
        ClearContainerChildren(_tab3P);

        // FIX: Enforce 0.8 scale before sliding animation loop begins
        _tab1.localScale = targetScale80;
        _tab2.localScale = targetScale80;
        _tab3.localScale = targetScale80;

        Vector2 startPos1 = _tab1.anchoredPosition;
        Vector2 startPos2 = _tab2.anchoredPosition;
        Vector2 startPos3 = _tab3.anchoredPosition;

        Vector2 targetPos1 = new Vector2(startPos1.x, 250f);
        Vector2 targetPos2 = new Vector2(startPos2.x, 250f);
        Vector2 targetPos3 = new Vector2(startPos3.x, 250f);

        CanvasGroup cg1 = _tab1P != null ? _tab1P.GetComponent<CanvasGroup>() : null;
        CanvasGroup cg2 = _tab2P != null ? _tab2P.GetComponent<CanvasGroup>() : null;
        CanvasGroup cg3 = _tab3P != null ? _tab3P.GetComponent<CanvasGroup>() : null;

        float slideSpeed = 2.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;
            float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

            _tab1.anchoredPosition = Vector2.Lerp(startPos1, targetPos1, easedT);
            _tab2.anchoredPosition = Vector2.Lerp(startPos2, targetPos2, easedT);
            _tab3.anchoredPosition = Vector2.Lerp(startPos3, targetPos3, easedT);

            // FIX: Keep all 3 element scales protected at 0.8 during movement loop
            _tab1.localScale = targetScale80;
            _tab2.localScale = targetScale80;
            _tab3.localScale = targetScale80;

            if (cg1 != null) cg1.alpha = (_currentTabIndex == 0) ? easedT : 0f;
            if (cg2 != null) cg2.alpha = (_currentTabIndex == 1) ? easedT : 0f;
            if (cg3 != null) cg3.alpha = (_currentTabIndex == 2) ? easedT : 0f;

            yield return null;
        }

        _tab1.anchoredPosition = targetPos1;
        _tab2.anchoredPosition = targetPos2;
        _tab3.anchoredPosition = targetPos3;

        // FIX: Final clamp lock at 0.8 scale properties
        _tab1.localScale = targetScale80;
        _tab2.localScale = targetScale80;
        _tab3.localScale = targetScale80;

        if (cg1 != null) cg1.alpha = (_currentTabIndex == 0) ? 1f : 0f;
        if (cg2 != null) cg2.alpha = (_currentTabIndex == 1) ? 1f : 0f;
        if (cg3 != null) cg3.alpha = (_currentTabIndex == 2) ? 1f : 0f;

        _tabChange = StartCoroutine(OnTab());
    }

    IEnumerator OnTab()
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;

        Transform currentTabP = GetActiveContainerTransform();
        if (currentTabP == null) yield break;

        currentTabP.GetChild(0).GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        foreach (Transform child in currentTabP.GetChild(0)) child.gameObject.SetActive(false);
        foreach (Transform child in currentTabP) child.gameObject.SetActive(true);

        yield return new WaitForSeconds(1f);

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
        Transform currentTabP = GetActiveContainerTransform();
        if (currentTabP == null) yield break;

        Transform content = currentTabP.GetChild(0);

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
        else if (_currentTabIndex == 1) _tab2Opened = true;
        else if (_currentTabIndex == 2) _tab3Opened = true;

        if (_tab1Opened && _tab2Opened && _tab3Opened && !_isViewed)
        {
            _isViewed = true;
            GameManager_Junior1B.Instance.Next(true);
        }
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1f);
    }

    public void PlayAudio(int index)
    {
        _currentAudioClipIndex = index;
        Transform currentTabP = GetActiveContainerTransform();
        if (currentTabP == null) return;

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
                // FIX: Force sub-container elements to stay clean at 0.8 scale too if needed
                displayLeft.localScale = targetScale80;

                if (btnSprite != null && displayLeft.GetComponent<Image>() != null) displayLeft.GetComponent<Image>().sprite = btnSprite;
                if (displayLeft.TryGetComponent(out Popeffect_Junior1B popLeft))
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
                // FIX: Force sub-container elements to stay clean at 0.8 scale too if needed
                displayRight.localScale = targetScale80;

                if (btnSprite != null && displayRight.GetComponent<Image>() != null) displayRight.GetComponent<Image>().sprite = btnSprite;
                if (displayRight.TryGetComponent(out Popeffect_Junior1B popRight))
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
        AudioClip[] currentClips = _tab1AudioClips;
        if (_currentTabIndex == 1) currentClips = _tab2AudioClips;
        else if (_currentTabIndex == 2) currentClips = _tab3AudioClips;

        if (currentClips != null && _currentAudioClipIndex < currentClips.Length)
        {
            _audioSource.clip = currentClips[_currentAudioClipIndex];
            _audioSource.Play();
            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _audioSource.clip.length / pV1;
            yield return new WaitForSeconds(aL1);
        }

        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }

    private Transform GetActiveContainerTransform()
    {
        if (_currentTabIndex == 0 && _tab1P != null) return _tab1P.transform;
        if (_currentTabIndex == 1 && _tab2P != null) return _tab2P.transform;
        if (_currentTabIndex == 2 && _tab3P != null) return _tab3P.transform;
        return null;
    }
}