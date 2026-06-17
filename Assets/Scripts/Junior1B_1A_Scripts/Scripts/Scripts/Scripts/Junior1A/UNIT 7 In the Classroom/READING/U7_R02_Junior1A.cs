using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U7_R02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false, _slided = false;
    [SerializeField] bool[] _tabOpened;
    [SerializeField] RectTransform[] _tabs;
    [SerializeField] GameObject[] _tabPs;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex, _currentTabIndex;
    [SerializeField] List<int> _clickCheckIndex = new List<int>();
    [SerializeField] TextMeshProUGUI _clickedIndexText;
    Coroutine _audioCoroutine, _tabChange;

    public bool IsViewed => _isViewed;
    int TotalClips => _audioClips != null ? _audioClips.Length : 0;

    void OnEnable()
    {
        if (_tabOpened == null || _tabOpened.Length != _tabs.Length) _tabOpened = new bool[_tabs.Length];

        foreach (RectTransform tab in _tabs)
        {
            if (tab == null) continue;
            tab.GetComponent<PopEffect_Junior1A>().enabled = true;
            tab.GetComponent<Image>().color = Color.white;
            tab.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }

        for (int i = 0; i < _tabPs.Length; i++)
        {
            if (i == _currentTabIndex && _tabPs[i]) _tabPs[i].GetComponent<CanvasGroup>().alpha = 0;
            _tabOpened[i] = false;
        }

        _clickCheckIndex.Clear();
        _clickedIndexText.text = $"0/{TotalClips}";
        _slided = false;
    }

    void OnDisable()
    {
        foreach (int index in _clickCheckIndex)
        {
            Image img = GetButtonImage(index);
            if (img == null) continue;
            Color c = img.color;
            c.r /= 0.75f;
            c.g /= 0.75f;
            c.b /= 0.75f;
            img.color = c;
        }
        _clickCheckIndex.Clear();
    }

    Image GetButtonImage(int globalIndex)
    {
        int currentIndex = 0;
        foreach (var tabP in _tabPs)
        {
            if (tabP == null) continue;
            Transform itemsParent = tabP.transform.GetChild(0).GetChild(0).GetChild(0);
            foreach (Transform child in itemsParent)
            {
                if (currentIndex == globalIndex) return child.childCount > 0 ? child.GetChild(0).GetComponent<Image>() : null;
                currentIndex++;
            }
        }
        return null;
    }

    public void TabSlideUp(int index)
    {
        for (int i = 0; i < _tabs.Length; i++)
        {
            if (_tabs[i] == null) continue;
            if (i == index)
            {
                _tabOpened[i] = true;
                _tabs[i].GetComponent<Image>().color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
                _tabs[i].GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
            }
            else
            {
                _tabs[i].GetComponent<Image>().color = Color.white;
                _tabs[i].GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            }
        }

        _audioSource.Stop();
        
        if (_slided)
        {
            if (index == _currentTabIndex) return;
            _currentTabIndex = index;
            if (_currentTabIndex < _tabPs.Length && _tabPs[_currentTabIndex] != null) foreach (Transform child in _tabPs[_currentTabIndex].transform.GetChild(0).GetChild(0).GetChild(0)) child.gameObject.SetActive(false);
            if (_tabChange != null) StopCoroutine(_tabChange);
            _tabChange = StartCoroutine(OnTab());
            return;
        }

        _slided = true;
        _currentTabIndex = index;
        StartCoroutine(SlideTabUp());
    }

    IEnumerator SlideTabUp()
    {
        _slided = true;

        if (_currentTabIndex < _tabPs.Length && _tabPs[_currentTabIndex] != null) foreach (Transform child in _tabPs[_currentTabIndex].transform.GetChild(0).GetChild(0).GetChild(0)) child.gameObject.SetActive(false);

        Vector3[] worldPos = new Vector3[_tabs.Length];
        Vector2[] startPos = new Vector2[_tabs.Length];
        Vector2[] targetPos = new Vector2[_tabs.Length];
        CanvasGroup[] cgs = new CanvasGroup[_tabPs.Length];

        for (int i = 0; i < _tabs.Length; i++)
        {
            if (_tabs[i] == null) continue;
            worldPos[i] = _tabs[i].position;
            _tabs[i].anchorMin = new Vector2(_tabs[i].anchorMin.x, 1);
            _tabs[i].anchorMax = new Vector2(_tabs[i].anchorMax.x, 1);
            _tabs[i].position = worldPos[i];
            startPos[i] = _tabs[i].anchoredPosition;
            targetPos[i] = new Vector2(startPos[i].x, -285f);
        }

        for (int i = 0; i < _tabPs.Length; i++) if (_tabPs[i] != null) cgs[i] = _tabPs[i].GetComponent<CanvasGroup>();

        float slideSpeed = 2.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;
            float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

            for (int i = 0; i < _tabs.Length; i++) _tabs[i].anchoredPosition = Vector2.LerpUnclamped(startPos[i], targetPos[i], easedT);
            foreach (var cg in cgs) cg.alpha = easedT;

            yield return null;
        }

        for (int i = 0; i < _tabs.Length; i++) _tabs[i].anchoredPosition = targetPos[i];
        foreach (var cg in cgs) cg.alpha = 1;

        _tabChange = StartCoroutine(OnTab());
    }

    IEnumerator OnTab()
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        if (_currentTabIndex >= _tabPs.Length) yield break;

        GameObject currentTabP = _tabPs[_currentTabIndex];
        if (currentTabP == null) yield break;

        currentTabP.transform.GetChild(currentTabP.transform.childCount - 1).gameObject.SetActive(true);
        yield return new WaitForSeconds(1);

        Transform itemsParent = currentTabP.transform.GetChild(0).GetChild(0).GetChild(0);

        foreach (Transform child in itemsParent)
        {
            child.gameObject.SetActive(true);
            child.GetComponent<Button>().interactable = false;
            yield return new WaitForSeconds(.25f);
        }

        yield return new WaitForSeconds(.25f);

        foreach (Transform child in itemsParent) child.GetComponent<Button>().interactable = true;
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
        
        if (!_clickCheckIndex.Contains(index))
        {
            _clickCheckIndex.Add(index);
            _clickedIndexText.text = $"{_clickCheckIndex.Count}/{TotalClips}";
            
            Image img = GetButtonImage(index);
            if (img != null)
            {
                Color c = img.color;
                c.r *= .75f;
                c.g *= .75f;
                c.b *= .75f;
                img.color = c;
            }
            
            if (_clickCheckIndex.Count == TotalClips && !_isViewed)
            {
                _isViewed = true;
                GameManager_Junior1A.Instance.Next(true);
            }
        }
        
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex());
    }

    IEnumerator PlayAudioIndex()
    {
        _audioSource.clip = _audioClips[_currentAudioClipIndex];
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }
}
