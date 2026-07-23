using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U8_R02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] bool _isViewed = false, _slided = false;
    [SerializeField] bool _tab1Opened = false, _tab2Opened = false, _tab3Opened = false;

    [Header("=== Tab Button UI Rects ===")]
    [SerializeField] RectTransform _tab1;
    [SerializeField] RectTransform _tab2;
    [SerializeField] RectTransform _tab3;

    [Header("=== Tab Content Container Panels ===")]
    [SerializeField] GameObject _tab1P;
    [SerializeField] GameObject _tab2P;
    [SerializeField] GameObject _tab3P;

    [Header("=== Audio Setup Elements ===")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _tab1AudioClips, _tab2AudioClips, _tab3AudioClips;

    [Header("=== Tracking States ===")]
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex, _currentTabIndex;

    Coroutine _audioCoroutine, _tabChange;

    // Vector field enforcing custom uniform 0.8 scale restrictions
    private readonly Vector3 targetScale80 = new Vector3(0.8f, 0.8f, 0.8f);

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        // 1. Distribute three tabs evenly horizontally from the center
        _tab1.anchoredPosition = Vector3.left * 350f;
        _tab2.anchoredPosition = Vector3.zero;
        _tab3.anchoredPosition = Vector3.right * 350f;

        // 2. Lock anchoring properties safely to eliminate dynamic canvas spikes
        _tab1.anchorMin = _tab1.anchorMax = new Vector2(.5f, .5f);
        _tab2.anchorMin = _tab2.anchorMax = new Vector2(.5f, .5f);
        _tab3.anchorMin = _tab3.anchorMax = new Vector2(.5f, .5f);

        // 3. Clear colors to default states
        _tab1.GetComponent<Image>().color = Color.white;
        _tab2.GetComponent<Image>().color = Color.white;
        _tab3.GetComponent<Image>().color = Color.white;

        Color textDarkGray = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = textDarkGray;
        _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = textDarkGray;
        _tab3.GetChild(0).GetComponent<TextMeshProUGUI>().color = textDarkGray;

        // 4. Initialize panel content canvas groups
        if (_tab1P != null && _tab1P.TryGetComponent(out CanvasGroup cg1)) cg1.alpha = (_currentTabIndex == 0) ? 1f : 0f;
        if (_tab2P != null && _tab2P.TryGetComponent(out CanvasGroup cg2)) cg2.alpha = (_currentTabIndex == 1) ? 1f : 0f;
        if (_tab3P != null && _tab3P.TryGetComponent(out CanvasGroup cg3)) cg3.alpha = (_currentTabIndex == 2) ? 1f : 0f;

        _slided = _tab1Opened = _tab2Opened = _tab3Opened = false;

        _audioSource.clip = _introClip;
        _audioSource.Play();

        // Enforce 0.8 scale at launch
        _tab1.localScale = targetScale80;
        _tab2.localScale = targetScale80;
        _tab3.localScale = targetScale80;
    }

    public void TabSlideUp(int index)
    {
        Color selectedBlue = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
        Color unselectedText = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);

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
        Transform currentTabP = GetActiveContainerTransform();
        if (currentTabP == null) yield break;

        Transform content = currentTabP.GetChild(0);

        // Turn interactable ON directly so user can click to play audio manually
        foreach (Transform child in content)
        {
            child.GetComponent<Button>().interactable = true;
        }

        if (_currentTabIndex == 0) _tab1Opened = true;
        else if (_currentTabIndex == 1) _tab2Opened = true;
        else if (_currentTabIndex == 2) _tab3Opened = true;

        // Check verification states cleanly for all 3 panels
        if (_tab1Opened && _tab2Opened && _tab3Opened && !_isViewed)
        {
            _isViewed = true;
            GameManager_Junior1B.Instance.Next(true);
        }

        yield return null;
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
                displayLeft.localScale = Vector3.one;
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
                displayRight.localScale = Vector3.one;
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