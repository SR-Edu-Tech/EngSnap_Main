using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U10_R01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false, _slided = false, _isSlowed = false;
    [SerializeField] RectTransform _tab1, _tab2, _tab3;
    [SerializeField] GameObject _tab1P, _tab2P, _tab3P;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _tab1AudioClips, _tab2AudioClips, _tab3AudioClips;
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex, _currentTabIndex;

    [SerializeField] List<string> _clickCheckIndex = new List<string>();
    [SerializeField] TextMeshProUGUI _clickedIndexText;

    RectTransform[] Tabs => new[] { _tab1, _tab2, _tab3 };
    GameObject[] TabPs => new[] { _tab1P, _tab2P, _tab3P };
    AudioClip[][] TabAudioClips => new[] { _tab1AudioClips, _tab2AudioClips, _tab3AudioClips };

    int TotalClips => (_tab1AudioClips != null ? _tab1AudioClips.Length : 0) + (_tab2AudioClips != null ? _tab2AudioClips.Length : 0) + (_tab3AudioClips != null ? _tab3AudioClips.Length : 0);

    Coroutine _audioCoroutine, _tabChange;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        if (_tab1)
        {
            _tab1.anchoredPosition = Vector3.left * 400;
            _tab1.anchorMin = new Vector2(.5f, .5f);
            _tab1.anchorMax = new Vector2(.5f, .5f);
            _tab1.GetComponent<Image>().color = Color.white;
            _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }
        if (_tab2)
        {
            _tab2.anchoredPosition = Vector3.zero;
            _tab2.anchorMin = new Vector2(.5f, .5f);
            _tab2.anchorMax = new Vector2(.5f, .5f);
            _tab2.GetComponent<Image>().color = Color.white;
            _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }
        if (_tab3)
        {
            _tab3.anchoredPosition = Vector3.right * 400;
            _tab3.anchorMin = new Vector2(.5f, .5f);
            _tab3.anchorMax = new Vector2(.5f, .5f);
            _tab3.GetComponent<Image>().color = Color.white;
            _tab3.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }

        foreach (var tabP in TabPs)
        {
            if (tabP) if (tabP == TabPs[_currentTabIndex]) tabP.GetComponent<CanvasGroup>().alpha = 0;
        }

        _slided = false;

        _clickCheckIndex.Clear();
        _clickedIndexText.text = $"0/{TotalClips}";

        _audioSource.clip = _introClip;
        _audioSource.Play();
    }

    void OnDisable()
    {
        foreach (string clickId in _clickCheckIndex)
        {
            string[] parts = clickId.Split('_');
            int tabIndex = int.Parse(parts[0]);
            int btnIndex = int.Parse(parts[1]);

            if (tabIndex >= 0 && tabIndex < TabPs.Length && TabPs[tabIndex] != null)
            {
                Transform tabP = TabPs[tabIndex].transform;
                if (tabP.childCount > 0 && tabP.GetChild(0).childCount > btnIndex)
                {
                    Transform btnContainer = tabP.GetChild(0);
                    Transform btn = btnContainer.GetChild(btnIndex);
                    if (btn.childCount > 1)
                    {
                        Image img = btn.GetChild(1).GetComponent<Image>();
                        if (img != null)
                        {
                            Color c = img.color;
                            c.r /= 0.85f;
                            c.g /= 0.85f;
                            c.b /= 0.85f;
                            img.color = c;
                        }
                    }
                }
            }
        }
        _clickCheckIndex.Clear();
    }

    public void TabSlideUp(int index)
    {
        foreach (var tab in Tabs)
        {
            if (tab == null) continue;
            if (tab == Tabs[index])
            {
                tab.GetComponent<Image>().color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
                tab.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
            }
            else
            {
                tab.GetComponent<Image>().color = Color.white;
                tab.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            }
        }

        _audioSource.Stop();
        if (_slided)
        {
            if (index == _currentTabIndex) return;
            _currentTabIndex = index;
            if (_currentTabIndex >= 0 && _currentTabIndex < TabPs.Length && TabPs[_currentTabIndex] != null)
            {
                foreach (Transform child in TabPs[_currentTabIndex].transform) child.gameObject.SetActive(false);
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

        foreach (var tabP in TabPs)
        {
            if (tabP != null && tabP == TabPs[_currentTabIndex])
            {
                foreach (Transform child in tabP.transform) child.gameObject.SetActive(false);
            }
        }

        Vector3[] worldPos = new Vector3[Tabs.Length];
        Vector2[] startPos = new Vector2[Tabs.Length];
        Vector2[] targetPos = new Vector2[Tabs.Length];
        CanvasGroup[] cgs = new CanvasGroup[TabPs.Length];

        int tIndex1 = 0;
        foreach (var tab in Tabs)
        {
            if (tab == null) 
            {
                tIndex1++;
                continue;
            }
            worldPos[tIndex1] = tab.position;
            tab.anchorMin = new Vector2(.5f, 1);
            tab.anchorMax = new Vector2(.5f, 1);
            tab.position = worldPos[tIndex1];
            startPos[tIndex1] = tab.anchoredPosition;
            targetPos[tIndex1] = new Vector2(startPos[tIndex1].x, -250f);
            tIndex1++;
        }

        int tIndex2 = 0;
        foreach (var tabP in TabPs)
        {
            if (tabP != null) cgs[tIndex2] = tabP.GetComponent<CanvasGroup>();
            tIndex2++;
        }

        float slideSpeed = 2.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;
            float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

            int tIndex3 = 0;
            foreach (var tab in Tabs)
            {
                if (tab != null) tab.anchoredPosition = Vector2.LerpUnclamped(startPos[tIndex3], targetPos[tIndex3], easedT);
                tIndex3++;
            }

            foreach (var cg in cgs)
            {
                if (cg != null) cg.alpha = easedT;
            }

            yield return null;
        }

        int tIndex4 = 0;
        foreach (var tab in Tabs)
        {
            if (tab != null) tab.anchoredPosition = targetPos[tIndex4];
            tIndex4++;
        }

        foreach (var cg in cgs)
        {
            if (cg != null) cg.alpha = 1;
        }

        _tabChange = StartCoroutine(OnTab());
    }

    IEnumerator OnTab()
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;

        // Deactivate all children of inactive tab panels to prevent overlapping
        foreach (var tabP in TabPs)
        {
            if (tabP == null) continue;
            if (tabP != TabPs[_currentTabIndex])
            {
                foreach (Transform child in tabP.transform)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        if (_currentTabIndex >= 0 && _currentTabIndex < TabPs.Length && TabPs[_currentTabIndex] != null)
        {
            Transform currentTabP = TabPs[_currentTabIndex].transform;

            currentTabP.GetChild(0).GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

            foreach (Transform child in currentTabP.GetChild(0)) child.gameObject.SetActive(false);
            foreach (Transform child in currentTabP) child.gameObject.SetActive(true);

            yield return new WaitForSeconds(1);

            foreach (Transform child in currentTabP.GetChild(0))
            {
                child.gameObject.SetActive(true);
                child.GetComponent<Button>().interactable = true;
                yield return new WaitForSeconds(.25f);
            }
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
        if (_currentTabIndex < 0 || _currentTabIndex >= TabPs.Length || TabPs[_currentTabIndex] == null) return;

        Transform currentTabP = TabPs[_currentTabIndex].transform;

        string clickId = $"{_currentTabIndex}_{index}";
        if (!_clickCheckIndex.Contains(clickId))
        {
            _clickCheckIndex.Add(clickId);
            _clickedIndexText.text = $"{_clickCheckIndex.Count}/{TotalClips}";

            if (currentTabP.childCount > 0 && currentTabP.GetChild(0).childCount > index)
            {
                Transform btnContainer = currentTabP.GetChild(0);
                Transform btn = btnContainer.GetChild(index);
                if (btn.childCount > 1)
                {
                    Image img = btn.GetChild(1).GetComponent<Image>();
                    if (img != null)
                    {
                        Color c = img.color;
                        c.r *= .85f;
                        c.g *= .85f;
                        c.b *= .85f;
                        img.color = c;
                    }
                }
            }

            if (_clickCheckIndex.Count == TotalClips && !_isViewed)
            {
                _isViewed = true;
                GameManager_Junior1A.Instance.Next(true);
            }
        }

        if (currentTabP.childCount > 0 && currentTabP.GetChild(0).childCount > index)
        {
            Transform btnContainer = currentTabP.GetChild(0);
            Transform btn = btnContainer.GetChild(index);
            if (btn.childCount > 0 && btn.GetChild(0).childCount > 0)
            {
                Image btnImg = btn.GetChild(0).GetChild(0).GetComponent<Image>();
                if (btnImg != null)
                {
                    Sprite btnSprite = btnImg.sprite;
                    if (index % 2 == 0)
                    {
                        if (currentTabP.childCount > 1)
                        {
                            Image targetImg = currentTabP.GetChild(1).GetComponent<Image>();
                            if (targetImg != null) targetImg.sprite = btnSprite;
                            PopEffect_Junior1A pop = currentTabP.GetChild(1).GetComponent<PopEffect_Junior1A>();
                            if (pop != null)
                            {
                                pop.enabled = false;
                                pop.enabled = true;
                            }
                        }
                    }
                    else
                    {
                        if (currentTabP.childCount > 2)
                        {
                            Image targetImg = currentTabP.GetChild(2).GetComponent<Image>();
                            if (targetImg != null) targetImg.sprite = btnSprite;
                            PopEffect_Junior1A pop = currentTabP.GetChild(2).GetComponent<PopEffect_Junior1A>();
                            if (pop != null)
                            {
                                pop.enabled = false;
                                pop.enabled = true;
                            }
                        }
                    }
                }
            }
        }

        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex());
    }

    IEnumerator PlayAudioIndex()
    {
        if (_currentTabIndex >= 0 && _currentTabIndex < TabAudioClips.Length)
        {
            AudioClip[] currentClips = TabAudioClips[_currentTabIndex];
            if (currentClips != null && _currentAudioClipIndex >= 0 && _currentAudioClipIndex < currentClips.Length)
            {
                _audioSource.clip = currentClips[_currentAudioClipIndex];
                _audioSource.Play();
                float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                float aL1 = _audioSource.clip.length / pV1;
                yield return new WaitForSeconds(aL1);
            }
        }
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }

    public void Repeat()
    {
        _audioSource.Stop();
        if (_tabChange != null) StopCoroutine(_tabChange);
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);

        _tabChange = StartCoroutine(OnTab());
    }

    public void Slow(TextMeshProUGUI text)
    {
        text.text = _isSlowed ? "    SLOW" : "    FAST";
        _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }
}
