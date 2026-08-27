using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U4_L02_TabAudioData
{
    public AudioClip[] QuestionClips;
    public AudioClip[] ResponseClips;
}

public class U4_L02_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;

    [Header("Tab Specific Audio Data (6 Elements)")]
    [SerializeField] U4_L02_TabAudioData[] _tabAudioData;

    [Header("Completion Flags")]
    [SerializeField] bool _isViewed = false;
    [SerializeField] bool _didSlided = false;
    [SerializeField] bool _didTab1 = false, _didTab2 = false, _didTab3 = false, _didTab4 = false, _didTab5 = false, _didTab6 = false;

    [Header("6 Individual Content Containers")]
    [SerializeField] GameObject _container1;
    [SerializeField] GameObject _container2;
    [SerializeField] GameObject _container3;
    [SerializeField] GameObject _container4;
    [SerializeField] GameObject _container5;
    [SerializeField] GameObject _container6;

    [Header("UI RectTransforms for Sliding")]
    [SerializeField] RectTransform _tab1;
    [SerializeField] RectTransform _tab2;
    [SerializeField] RectTransform _tab3;
    [SerializeField] RectTransform _tab4;
    [SerializeField] RectTransform _tab5;
    [SerializeField] RectTransform _tab6;

    private int _lastActiveIndex = 0;
    Coroutine _coroutine;

    public bool IsViewed => _isViewed;
    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        _didSlided = _didTab1 = _didTab2 = _didTab3 = _didTab4 = _didTab5 = _didTab6 = false;
        _lastActiveIndex = 0;

        if (_container1 != null) _container1.SetActive(false);
        if (_container2 != null) _container2.SetActive(false);
        if (_container3 != null) _container3.SetActive(false);
        if (_container4 != null) _container4.SetActive(false);
        if (_container5 != null) _container5.SetActive(false);
        if (_container6 != null) _container6.SetActive(false);

        _tab1.anchorMin = _tab1.anchorMax = new Vector2(.5f, .5f);
        _tab2.anchorMin = _tab2.anchorMax = new Vector2(.5f, .5f);
        _tab3.anchorMin = _tab3.anchorMax = new Vector2(.5f, .5f);
        _tab4.anchorMin = _tab4.anchorMax = new Vector2(.5f, .5f);
        _tab5.anchorMin = _tab5.anchorMax = new Vector2(.5f, .5f);
        _tab6.anchorMin = _tab6.anchorMax = new Vector2(.5f, .5f);

        _tab1.anchoredPosition = Vector3.up * 250;
        _tab2.anchoredPosition = Vector3.up * 150;
        _tab3.anchoredPosition = Vector3.up * 50;
        _tab4.anchoredPosition = Vector3.down * 50;
        _tab5.anchoredPosition = Vector3.down * 150;
        _tab6.anchoredPosition = Vector3.down * 250;

        _tab1.gameObject.SetActive(false);
        _tab2.gameObject.SetActive(false);
        _tab3.gameObject.SetActive(false);
        _tab4.gameObject.SetActive(false);
        _tab5.gameObject.SetActive(false);
        _tab6.gameObject.SetActive(false);

        _tab1.GetComponent<PopEffect_Junior2A>().enabled = true;
        _tab2.GetComponent<PopEffect_Junior2A>().enabled = true;
        _tab3.GetComponent<PopEffect_Junior2A>().enabled = true;
        _tab4.GetComponent<PopEffect_Junior2A>().enabled = true;
        _tab5.GetComponent<PopEffect_Junior2A>().enabled = true;
        _tab6.GetComponent<PopEffect_Junior2A>().enabled = true;

        _audioSource.clip = _introClip;
        _audioSource.Play();

        yield return new WaitForSeconds(_audioSource.clip.length);

        _tab1.gameObject.SetActive(true);
        _tab2.gameObject.SetActive(true);
        _tab3.gameObject.SetActive(true);
        _tab4.gameObject.SetActive(true);
        _tab5.gameObject.SetActive(true);
        _tab6.gameObject.SetActive(true);
    }

    public void moveslideup(int index)
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(MoveTab(index));
    }

    public void playaudio()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(PlaySequencedAudio(_lastActiveIndex));
    }

    IEnumerator MoveTab(int index)
    {
        _lastActiveIndex = index;

        if (_container1 != null) _container1.SetActive(false);
        if (_container2 != null) _container2.SetActive(false);
        if (_container3 != null) _container3.SetActive(false);
        if (_container4 != null) _container4.SetActive(false);
        if (_container5 != null) _container5.SetActive(false);
        if (_container6 != null) _container6.SetActive(false);

        if (index == 0) { _didTab1 = true; if (_container1 != null) _container1.SetActive(true); }
        else if (index == 1) { _didTab2 = true; if (_container2 != null) _container2.SetActive(true); }
        else if (index == 2) { _didTab3 = true; if (_container3 != null) _container3.SetActive(true); }
        else if (index == 3) { _didTab4 = true; if (_container4 != null) _container4.SetActive(true); }
        else if (index == 4) { _didTab5 = true; if (_container5 != null) _container5.SetActive(true); }
        else if (index == 5) { _didTab6 = true; if (_container6 != null) _container6.SetActive(true); }

        if (_didTab1 && _didTab2 && _didTab3 && _didTab4 && _didTab5 && _didTab6)
        {
            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            _isViewed = true;
        }

        if (!_didSlided)
        {
            Vector3 worldPos1 = _tab1.position;
            Vector3 worldPos2 = _tab2.position;
            Vector3 worldPos3 = _tab3.position;
            Vector3 worldPos4 = _tab4.position;
            Vector3 worldPos5 = _tab5.position;
            Vector3 worldPos6 = _tab6.position;

            _tab1.anchorMin = _tab1.anchorMax = new Vector2(1f, .5f);
            _tab2.anchorMin = _tab2.anchorMax = new Vector2(1f, .5f);
            _tab3.anchorMin = _tab3.anchorMax = new Vector2(1f, .5f);
            _tab4.anchorMin = _tab4.anchorMax = new Vector2(1f, .5f);
            _tab5.anchorMin = _tab5.anchorMax = new Vector2(1f, .5f);
            _tab6.anchorMin = _tab6.anchorMax = new Vector2(1f, .5f);

            _tab1.position = worldPos1;
            _tab2.position = worldPos2;
            _tab3.position = worldPos3;
            _tab4.position = worldPos4;
            _tab5.position = worldPos5;
            _tab6.position = worldPos6;

            Vector2 startPos1 = _tab1.anchoredPosition;
            Vector2 startPos2 = _tab2.anchoredPosition;
            Vector2 startPos3 = _tab3.anchoredPosition;
            Vector2 startPos4 = _tab4.anchoredPosition;
            Vector2 startPos5 = _tab5.anchoredPosition;
            Vector2 startPos6 = _tab6.anchoredPosition;

            Vector2 targetPos1 = new Vector2(-250f, startPos1.y);
            Vector2 targetPos2 = new Vector2(-250f, startPos2.y);
            Vector2 targetPos3 = new Vector2(-250f, startPos3.y);
            Vector2 targetPos4 = new Vector2(-250f, startPos4.y);
            Vector2 targetPos5 = new Vector2(-250f, startPos5.y);
            Vector2 targetPos6 = new Vector2(-250f, startPos6.y);

            float slideSpeed = 2.5f;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * slideSpeed;
                float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

                _tab1.anchoredPosition = Vector2.LerpUnclamped(startPos1, targetPos1, easedT);
                _tab2.anchoredPosition = Vector2.LerpUnclamped(startPos2, targetPos2, easedT);
                _tab3.anchoredPosition = Vector2.LerpUnclamped(startPos3, targetPos3, easedT);
                _tab4.anchoredPosition = Vector2.LerpUnclamped(startPos4, targetPos4, easedT);
                _tab5.anchoredPosition = Vector2.LerpUnclamped(startPos5, targetPos5, easedT);
                _tab6.anchoredPosition = Vector2.LerpUnclamped(startPos6, targetPos6, easedT);

                yield return null;
            }

            _tab1.anchoredPosition = targetPos1;
            _tab2.anchoredPosition = targetPos2;
            _tab3.anchoredPosition = targetPos3;
            _tab4.anchoredPosition = targetPos4;
            _tab5.anchoredPosition = targetPos5;
            _tab6.anchoredPosition = targetPos6;

            _didSlided = true;
        }

        yield return StartCoroutine(PlaySequencedAudio(index));
    }

    IEnumerator PlaySequencedAudio(int index)
    {
        if (index < _tabAudioData.Length && _tabAudioData[index] != null)
        {
            var currentAudio = _tabAudioData[index];

            if (currentAudio.QuestionClips != null)
            {
                foreach (AudioClip clip in currentAudio.QuestionClips)
                {
                    if (clip == null) continue;
                    _audioSource.clip = clip;
                    _audioSource.Play();
                    yield return new WaitForSeconds(clip.length);
                }
            }

            if (currentAudio.ResponseClips != null)
            {
                foreach (AudioClip clip in currentAudio.ResponseClips)
                {
                    if (clip == null) continue;
                    _audioSource.clip = clip;
                    _audioSource.Play();
                    yield return new WaitForSeconds(clip.length);
                }
            }
        }
    }
}