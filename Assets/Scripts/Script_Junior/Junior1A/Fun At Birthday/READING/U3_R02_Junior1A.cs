using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U3_R02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false, _slided = false, _tab1Opened = false, _tab2Opened = false;
    [SerializeField] RectTransform _tab1, _tab2;
    [SerializeField] GameObject _tab1P, _tab2P;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] Sprite[] _npcs;
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex, _currentTabIndex;
    [SerializeField] List<int> _clickCheckIndex = new List<int>();
    [SerializeField] TextMeshProUGUI _clickedIndexText;
    Coroutine _audioCoroutine, _tabChange;
    public bool IsViewed => _isViewed;
    void OnEnable()
    {
        _tab1.anchoredPosition = Vector3.left * 200;
        _tab2.anchoredPosition = Vector3.right * 200;
        Debug.Log(_tab1.position + " " + _tab2.position);
        _tab1.anchorMin = new Vector2(.5f, .5f);
        _tab1.anchorMax = new Vector2(.5f, .5f);
        _tab2.anchorMin = new Vector2(.5f, .5f);
        _tab2.anchorMax = new Vector2(.5f, .5f);
        _tab1.GetComponent<Image>().color = _tab2.GetComponent<Image>().color = Color.white;
        _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        if (_currentTabIndex == 0) _tab1P.GetComponent<CanvasGroup>().alpha = 0;
        else _tab2P.GetComponent<CanvasGroup>().alpha = 0;
        _tab1Opened = _tab2Opened = _slided = false;
        _clickCheckIndex.Clear();
        _clickedIndexText.text = $"{_clickCheckIndex.Count.ToString()}/{_audioClips.Length}";
    }
    void OnDisable()
    {
        for (int i = 0; i < _tab1P.transform.GetChild(0).childCount; i++)
        {
            Image img = _tab1P.transform.GetChild(0).GetChild(i).GetChild(1).GetComponent<Image>();

            Color c = img.color;
            c.r /= 0.75f;
            c.g /= 0.75f;
            c.b /= 0.75f;

            img.color = c;
        }

        for (int i = 0; i < _tab2P.transform.GetChild(0).childCount; i++)
        {
            Image img = _tab2P.transform.GetChild(0).GetChild(i).GetChild(1).GetComponent<Image>();

            Color c = img.color;
            c.r /= 0.75f;
            c.g /= 0.75f;
            c.b /= 0.75f;

            img.color = c;
        }
    }
    public void TabSlideUp(int index)
    {
        if (index == 0)
        {
            _tab1Opened = true;
            _tab1.GetComponent<Image>().color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
            _tab2.GetComponent<Image>().color = Color.white;
            _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
            _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }
        else
        {
            _tab2Opened = true;
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
        _slided = true;
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
        if (_currentTabIndex == 0)
        {
            _tab1P.transform.GetChild(_tab1P.transform.childCount - 1).gameObject.SetActive(true);
            _tab1P.transform.GetChild(_tab1P.transform.childCount - 2).gameObject.SetActive(true);

            yield return new WaitForSeconds(1);

            for (int i = 0; i < _tab1P.transform.childCount - 2; i++)
            {
                _tab1P.transform.GetChild(i).gameObject.SetActive(true);
                _tab1P.transform.GetChild(i).GetComponent<Button>().interactable = false;

                yield return new WaitForSeconds(.25f);
            }

            for (int i = 0; i < _tab1P.transform.childCount - 2; i++) _tab1P.transform.GetChild(i).GetComponent<Button>().interactable = true;
        }
        else
        {
            _tab2P.transform.GetChild(_tab2P.transform.childCount - 1).gameObject.SetActive(true);
            _tab2P.transform.GetChild(_tab2P.transform.childCount - 2).gameObject.SetActive(true);

            yield return new WaitForSeconds(1);

            for (int i = 0; i < _tab2P.transform.childCount - 2; i++)
            {
                _tab2P.transform.GetChild(i).gameObject.SetActive(true);
                _tab2P.transform.GetChild(i).GetComponent<Button>().interactable = false;

                yield return new WaitForSeconds(.25f);
            }
            yield return new WaitForSeconds(.25f);
            
            for (int i = 0; i < _tab2P.transform.childCount - 2; i++) _tab2P.transform.GetChild(i).GetComponent<Button>().interactable = true;
        }
    }
    public void OnSpeaker(Image speakerIcon)
    {   
        if(_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }
    public void PlayAudio(int index)
    {
        _currentAudioClipIndex = index;
        if(_currentTabIndex == 0)
        {
            if (index < 3)
            {
                _tab1P.transform.GetChild(_tab1P.transform.childCount - 2).GetComponent<Image>().sprite = _npcs[index];
                _tab1P.transform.GetChild(_tab1P.transform.childCount - 2).GetComponent<PopEffect_Junior1A>().enabled = false;
                _tab1P.transform.GetChild(_tab1P.transform.childCount - 2).GetComponent<PopEffect_Junior1A>().enabled = true;
            }
            else
            {
                _tab1P.transform.GetChild(_tab1P.transform.childCount - 1).GetComponent<Image>().sprite = _npcs[index];
                _tab1P.transform.GetChild(_tab1P.transform.childCount - 1).GetComponent<PopEffect_Junior1A>().enabled = false;
                _tab1P.transform.GetChild(_tab1P.transform.childCount - 1).GetComponent<PopEffect_Junior1A>().enabled = true;
            }
        }
        else
        {
            if (index % 2 == 0)
            {
                _tab2P.transform.GetChild(_tab2P.transform.childCount - 2).GetComponent<Image>().sprite = _npcs[index];
                _tab2P.transform.GetChild(_tab2P.transform.childCount - 2).GetComponent<PopEffect_Junior1A>().enabled = false;
                _tab2P.transform.GetChild(_tab2P.transform.childCount - 2).GetComponent<PopEffect_Junior1A>().enabled = true;
            }
            else
            {
                _tab2P.transform.GetChild(_tab2P.transform.childCount - 1).GetComponent<Image>().sprite = _npcs[index];
                _tab2P.transform.GetChild(_tab2P.transform.childCount - 1).GetComponent<PopEffect_Junior1A>().enabled = false;
                _tab2P.transform.GetChild(_tab2P.transform.childCount - 1).GetComponent<PopEffect_Junior1A>().enabled = true;
            }
        }
        
        if (!_clickCheckIndex.Contains(index))
        {
            _clickCheckIndex.Add(index);
            if(_currentTabIndex == 0)_tab1P.transform.GetChild(0).GetChild(index).GetChild(1).GetComponent<Image>().color *= 0.75f;
            else _tab2P.transform.GetChild(index - 4).transform.GetChild(1).GetComponent<Image>().color *= 0.75f;
        }
        _clickedIndexText.text = $"{_clickCheckIndex.Count.ToString()}/{_audioClips.Length}";
        if(_clickCheckIndex.Count == _audioClips.Length)
        {
            GameManager_Junior1A.Instance.Next(true);
            _isViewed = true;
        }
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex());
    }
    IEnumerator PlayAudioIndex()
    {
        _audioSource.clip = _audioClips[_currentAudioClipIndex];
        _audioSource.Play();
        yield return new WaitForSeconds(_audioSource.clip.length);
        _currentSpeakerIcon.color = Color.white;
    }
}
