using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U12_R02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] bool _isViewed = false, _slided = false, _tab1Opened = false, _tab2Opened = false;
    [SerializeField] RectTransform _tab1, _tab2;
    [SerializeField] GameObject _tab1P, _tab2P;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _tab1AudioClips, _tab2AudioClips;
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

        Vector2 targetPos1 = new Vector2(startPos1.x, 353f);
        Vector2 targetPos2 = new Vector2(startPos2.x, 353f);

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
        GameObject currentTabGO = _currentTabIndex == 0 ? _tab1P : _tab2P;
        if (currentTabGO == null) yield break;

        Transform currentTabP = currentTabGO.transform;

        // Bypasses old nested layouts and directly targets the ScrollView Content holder
        ScrollRect scrollComponent = currentTabGO.GetComponentInChildren<ScrollRect>();
        Transform targetContentHolder = (scrollComponent != null) ? scrollComponent.content : currentTabP.GetChild(0);

        if (targetContentHolder != null)
        {
            if (scrollComponent != null && scrollComponent.gameObject.TryGetComponent(out RectTransform scrollRect))
            {
                scrollRect.anchoredPosition = Vector2.zero;
            }
            // Temporarily hide all ISLAND button instances
            foreach (Transform child in targetContentHolder) child.gameObject.SetActive(false);
        }

        foreach (Transform child in currentTabP) child.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // Linearly fade/stagger-reveal each ISLAND button row smoothly
        if (targetContentHolder != null)
        {
            foreach (Transform child in targetContentHolder)
            {
                child.gameObject.SetActive(true);
                if (child.TryGetComponent(out Button btn)) btn.interactable = true;
                yield return new WaitForSeconds(.15f);
            }
        }

        if (_currentTabIndex == 0) _tab1Opened = true;
        else _tab2Opened = true;

        if (_tab1Opened && _tab2Opened && !_isViewed)
        {
            _isViewed = true;
            GameManager_Junior1B.Instance.Next(true);
        }
    }

    public void PlayAudio(int index)
    {
        _currentAudioClipIndex = index;
        AudioClip[] currentClips = _currentTabIndex == 0 ? _tab1AudioClips : _tab2AudioClips;

        // Guard validation clauses for 12 clip arrays
        if (currentClips == null || index < 0 || index >= currentClips.Length) return;

        GameObject currentTabGO = _currentTabIndex == 0 ? _tab1P : _tab2P;
        if (currentTabGO != null && currentTabGO.transform.childCount > 0)
        {
            Transform container = currentTabGO.transform;
            int bounceTargetIndex = (index % 2 == 0) ? 1 : 2;

            if (container.childCount > bounceTargetIndex)
            {
                Transform sidePanel = container.GetChild(bounceTargetIndex);
                if (sidePanel != null)
                {
                    sidePanel.localScale = Vector3.one;
                    if (sidePanel.TryGetComponent(out Popeffect_Junior1B pop))
                    {
                        pop.enabled = false;
                        pop.enabled = true;
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

        AudioClip activeClip = currentClips[_currentAudioClipIndex];

        if (_audioSource != null && activeClip != null)
        {
            _audioSource.clip = activeClip;
            _audioSource.Play();
            float speedFactor = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            yield return new WaitForSeconds(activeClip.length / speedFactor);
        }
    }
}