using Junior2A;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U2_L02_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] bool _isViewed = false, _slided = false, _isSlowed = false;

    [Header("Two Tab Configuration")]
    [SerializeField] RectTransform _tab1;
    [SerializeField] RectTransform _tab2;
    [SerializeField] GameObject _tab1P;
    [SerializeField] GameObject _tab2P;

    [Header("Audio Settings")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _tab1AudioClips;
    [SerializeField] AudioClip[] _tab2AudioClips;

    [Header("State Tracking")]
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex, _currentTabIndex;

    [SerializeField] List<string> _clickCheckIndex = new List<string>();
    [SerializeField] TextMeshProUGUI _clickedIndexText;

    RectTransform[] Tabs => new[] { _tab1, _tab2 };
    GameObject[] TabPs => new[] { _tab1P, _tab2P };
    AudioClip[][] TabAudioClips => new[] { _tab1AudioClips, _tab2AudioClips };

    int TotalClips => (_tab1AudioClips != null ? _tab1AudioClips.Length : 0) + (_tab2AudioClips != null ? _tab2AudioClips.Length : 0);

    Coroutine _audioCoroutine, _tabChange, _autoPlaySequenceCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        if (_tab1)
        {
            _tab1.anchoredPosition = Vector3.left * 200f;
            _tab1.anchorMin = new Vector2(.5f, .5f);
            _tab1.anchorMax = new Vector2(.5f, .5f);
            _tab1.GetComponent<Image>().color = Color.white;
            _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }
        if (_tab2)
        {
            _tab2.anchoredPosition = Vector3.right * 200f;
            _tab2.anchorMin = new Vector2(.5f, .5f);
            _tab2.anchorMax = new Vector2(.5f, .5f);
            _tab2.GetComponent<Image>().color = Color.white;
            _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }

        foreach (var tabP in TabPs)
        {
            if (tabP != null && _currentTabIndex >= 0 && _currentTabIndex < TabPs.Length)
            {
                if (tabP == TabPs[_currentTabIndex]) tabP.GetComponent<CanvasGroup>().alpha = 0;
            }
        }

        _slided = false;
        _clickCheckIndex.Clear();
        _clickedIndexText.text = $"0/{TotalClips}";

        if (_audioSource && _introClip)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
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
        for (int i = 0; i < Tabs.Length; i++)
        {
            if (Tabs[i] == null) continue;
            if (i == index)
            {
                Tabs[i].GetComponent<Image>().color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
                Tabs[i].GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
            }
            else
            {
                Tabs[i].GetComponent<Image>().color = Color.white;
                Tabs[i].GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            }
        }

        if (_audioSource) _audioSource.Stop();
        if (_autoPlaySequenceCoroutine != null) StopCoroutine(_autoPlaySequenceCoroutine);

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

        if (_currentTabIndex >= 0 && _currentTabIndex < TabPs.Length && TabPs[_currentTabIndex] != null)
        {
            foreach (Transform child in TabPs[_currentTabIndex].transform) child.gameObject.SetActive(false);
        }

        Vector3[] worldPos = new Vector3[Tabs.Length];
        Vector2[] startPos = new Vector2[Tabs.Length];
        Vector2[] targetPos = new Vector2[Tabs.Length];
        CanvasGroup[] cgs = new CanvasGroup[TabPs.Length];

        for (int i = 0; i < Tabs.Length; i++)
        {
            if (Tabs[i] == null) continue;
            worldPos[i] = Tabs[i].position;
            Tabs[i].anchorMin = new Vector2(.5f, 1);
            Tabs[i].anchorMax = new Vector2(.5f, 1);
            Tabs[i].position = worldPos[i];
            startPos[i] = Tabs[i].anchoredPosition;
            targetPos[i] = new Vector2(startPos[i].x, -250f);
        }

        for (int i = 0; i < TabPs.Length; i++)
        {
            if (TabPs[i] != null) cgs[i] = TabPs[i].GetComponent<CanvasGroup>();
        }

        float slideSpeed = 2.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;
            float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

            for (int i = 0; i < Tabs.Length; i++)
            {
                if (Tabs[i] != null) Tabs[i].anchoredPosition = Vector2.LerpUnclamped(startPos[i], targetPos[i], easedT);
            }

            foreach (var cg in cgs)
            {
                if (cg != null) cg.alpha = easedT;
            }

            yield return null;
        }

        for (int i = 0; i < Tabs.Length; i++)
        {
            if (Tabs[i] != null) Tabs[i].anchoredPosition = targetPos[i];
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

        for (int i = 0; i < TabPs.Length; i++)
        {
            if (TabPs[i] == null) continue;
            if (i != _currentTabIndex)
            {
                foreach (Transform child in TabPs[i].transform)
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

            // 🔁 START FULL AUTOPLAY SEQUENCE FOR THE WHOLE TAB 🔁
            if (_autoPlaySequenceCoroutine != null) StopCoroutine(_autoPlaySequenceCoroutine);
            _autoPlaySequenceCoroutine = StartCoroutine(AutoPlayTabClipsSequence());
        }
    }

    // New sequence loop logic handles running through every clip automatically
    IEnumerator AutoPlayTabClipsSequence()
    {
        if (_currentTabIndex < 0 || _currentTabIndex >= TabAudioClips.Length) yield break;

        AudioClip[] clipsInThisTab = TabAudioClips[_currentTabIndex];
        if (clipsInThisTab == null) yield break;

        for (int i = 0; i < clipsInThisTab.Length; i++)
        {
            // Triggers visuals, text changes, pop effect, and registers the track
            PlayAudio(i);

            // Wait until the audio logic completely finishes playing that track item
            if (_audioCoroutine != null)
            {
                yield return _audioCoroutine;
            }

            // Subtle padding break space between clips
            yield return new WaitForSeconds(0.2f);
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
                if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
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
                            PopEffect_Junior2A pop = currentTabP.GetChild(1).GetComponent<PopEffect_Junior2A>();
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
                            PopEffect_Junior2A pop = currentTabP.GetChild(2).GetComponent<PopEffect_Junior2A>();
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
        if (_audioSource) _audioSource.Stop();
        if (_tabChange != null) StopCoroutine(_tabChange);
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        if (_autoPlaySequenceCoroutine != null) StopCoroutine(_autoPlaySequenceCoroutine);

        _tabChange = StartCoroutine(OnTab());
    }

    public void Slow(TextMeshProUGUI text)
    {
        text.text = _isSlowed ? "    SLOW" : "    FAST";
        if (_audioSource) _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }
}