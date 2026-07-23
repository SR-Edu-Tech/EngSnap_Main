using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U10_R02_Junior1B : MonoBehaviour, Interfaces_Junior1B
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
        if (_tab1 != null)
        {
            _tab1.anchoredPosition = Vector3.left * 300;
            _tab1.anchorMin = new Vector2(.5f, .5f);
            _tab1.anchorMax = new Vector2(.5f, .5f);
            _tab1.localScale = Vector3.one;
            if (_tab1.TryGetComponent(out Image img1)) img1.color = Color.white;
            if (_tab1.childCount > 0 && _tab1.GetChild(0).TryGetComponent(out TextMeshProUGUI txt1))
                txt1.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }

        if (_tab2 != null)
        {
            _tab2.anchoredPosition = Vector3.right * 300;
            _tab2.anchorMin = new Vector2(.5f, .5f);
            _tab2.anchorMax = new Vector2(.5f, .5f);
            _tab2.localScale = Vector3.one;
            if (_tab2.TryGetComponent(out Image img2)) img2.color = Color.white;
            if (_tab2.childCount > 0 && _tab2.GetChild(0).TryGetComponent(out TextMeshProUGUI txt2))
                txt2.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }

        if (_currentTabIndex == 0 && _tab1P != null && _tab1P.TryGetComponent(out CanvasGroup cg1)) cg1.alpha = 0;
        else if (_currentTabIndex != 0 && _tab2P != null && _tab2P.TryGetComponent(out CanvasGroup cg2)) cg2.alpha = 0;

        _slided = _tab1Opened = _tab2Opened = false;

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }
    }

    public void TabSlideUp(int index)
    {
        if (index == 0)
        {
            if (_tab1 != null && _tab1.TryGetComponent(out Image img1)) img1.color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
            if (_tab2 != null && _tab2.TryGetComponent(out Image img2)) img2.color = Color.white;
            if (_tab1 != null && _tab1.childCount > 0 && _tab1.GetChild(0).TryGetComponent(out TextMeshProUGUI txt1)) txt1.color = Color.white;
            if (_tab2 != null && _tab2.childCount > 0 && _tab2.GetChild(0).TryGetComponent(out TextMeshProUGUI txt2)) txt2.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }
        else
        {
            if (_tab1 != null && _tab1.TryGetComponent(out Image img1)) img1.color = Color.white;
            if (_tab2 != null && _tab2.TryGetComponent(out Image img2)) img2.color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
            if (_tab1 != null && _tab1.childCount > 0 && _tab1.GetChild(0).TryGetComponent(out TextMeshProUGUI txt1)) txt1.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            if (_tab2 != null && _tab2.childCount > 0 && _tab2.GetChild(0).TryGetComponent(out TextMeshProUGUI txt2)) txt2.color = Color.white;
        }

        if (_audioSource != null) _audioSource.Stop();

        if (_slided)
        {
            if (index == _currentTabIndex) return;
            _currentTabIndex = index;

            GameObject currentTabGO = _currentTabIndex == 0 ? _tab1P : _tab2P;
            if (currentTabGO != null)
            {
                foreach (Transform child in currentTabGO.transform) child.gameObject.SetActive(false);
            }

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

        GameObject currentTabGO = _currentTabIndex == 0 ? _tab1P : _tab2P;
        if (currentTabGO != null)
        {
            foreach (Transform child in currentTabGO.transform) child.gameObject.SetActive(false);
        }

        if (_tab1 != null) _tab1.localScale = Vector3.one;
        if (_tab2 != null) _tab2.localScale = Vector3.one;

        Vector2 startPos1 = _tab1 != null ? _tab1.anchoredPosition : Vector2.zero;
        Vector2 startPos2 = _tab2 != null ? _tab2.anchoredPosition : Vector2.zero;

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

            if (_tab1 != null) { _tab1.anchoredPosition = Vector2.Lerp(startPos1, targetPos1, easedT); _tab1.localScale = Vector3.one; }
            if (_tab2 != null) { _tab2.anchoredPosition = Vector2.Lerp(startPos2, targetPos2, easedT); _tab2.localScale = Vector3.one; }

            if (cg1 != null) cg1.alpha = easedT;
            if (cg2 != null) cg2.alpha = easedT;

            yield return null;
        }

        if (_tab1 != null) { _tab1.anchoredPosition = targetPos1; _tab1.localScale = Vector3.one; }
        if (_tab2 != null) { _tab2.anchoredPosition = targetPos2; _tab2.localScale = Vector3.one; }

        if (cg1 != null) cg1.alpha = 1;
        if (cg2 != null) cg2.alpha = 1;

        _tabChange = StartCoroutine(OnTab());
    }

    IEnumerator OnTab()
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;

        GameObject currentTabGO = _currentTabIndex == 0 ? _tab1P : _tab2P;
        if (currentTabGO == null) yield break;

        Transform currentTabP = currentTabGO.transform;

        if (currentTabP.childCount > 0)
        {
            Transform firstChild = currentTabP.GetChild(0);
            if (firstChild.TryGetComponent(out RectTransform rect))
            {
                rect.anchoredPosition = Vector2.zero;
            }

            foreach (Transform child in firstChild) child.gameObject.SetActive(false);
        }

        foreach (Transform child in currentTabP) child.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        if (currentTabP.childCount > 0)
        {
            // Cascade elements smoothly into view, making them interactive for your click
            foreach (Transform child in currentTabP.GetChild(0))
            {
                child.gameObject.SetActive(true);
                if (child.TryGetComponent(out Button btn)) btn.interactable = true;
                yield return new WaitForSeconds(.15f);
            }
        }

        // Mark tab completion flags explicitly right after entry display completes
        if (_currentTabIndex == 0) _tab1Opened = true;
        else _tab2Opened = true;

        if (_tab1Opened && _tab2Opened && !_isViewed)
        {
            _isViewed = true;
            GameManager_Junior1B.Instance.Next(true);
        }
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    public void PlayAudio(int index)
    {
        _currentAudioClipIndex = index;
        GameObject currentTabGO = _currentTabIndex == 0 ? _tab1P : _tab2P;
        if (currentTabGO == null) return;

        Transform currentTabP = currentTabGO.transform;
        if (currentTabP.childCount == 0) return;

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
            if (currentTabP.childCount > 1)
            {
                Transform displayLeft = currentTabP.GetChild(1);
                if (displayLeft != null)
                {
                    displayLeft.localScale = Vector3.one;
                    if (btnSprite != null && displayLeft.TryGetComponent(out Image img)) img.sprite = btnSprite;
                    if (displayLeft.TryGetComponent(out Popeffect_Junior1B popLeft))
                    {
                        popLeft.enabled = false;
                        popLeft.enabled = true;
                    }
                }
            }
        }
        else
        {
            if (currentTabP.childCount > 2)
            {
                Transform displayRight = currentTabP.GetChild(2);
                if (displayRight != null)
                {
                    displayRight.localScale = Vector3.one;
                    if (btnSprite != null && displayRight.TryGetComponent(out Image img)) img.sprite = btnSprite;
                    if (displayRight.TryGetComponent(out Popeffect_Junior1B popRight))
                    {
                        popRight.enabled = false;
                        popRight.enabled = true;
                    }
                }
            }
        }

        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex());
    }

    IEnumerator PlayAudioIndex()
    {
        AudioClip[] currentClips = _currentTabIndex == 0 ? _tab1AudioClips : _tab2AudioClips;
        if (currentClips == null || _currentAudioClipIndex >= currentClips.Length || _currentAudioClipIndex < 0) yield break;

        if (_audioSource != null && currentClips[_currentAudioClipIndex] != null)
        {
            _audioSource.clip = currentClips[_currentAudioClipIndex];
            _audioSource.Play();
            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _audioSource.clip.length / pV1;
            yield return new WaitForSeconds(aL1);
        }
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }
}