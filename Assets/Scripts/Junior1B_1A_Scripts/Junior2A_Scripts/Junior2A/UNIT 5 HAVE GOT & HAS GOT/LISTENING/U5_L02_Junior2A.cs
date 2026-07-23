using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U5_L02_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;

    [Header("Separate Tab Audio Arrays (Plays sequentially on open)")]
    [SerializeField] AudioClip[] _tab1Audio;
    [SerializeField] AudioClip[] _tab2Audio;

    [Header("Completion Flags")]
    [SerializeField] bool _isViewed = false;
    [SerializeField] bool _didSlided = false;
    [SerializeField] bool _didTab1 = false, _didTab2 = false;

    [Header("2 Individual Content Containers")]
    [SerializeField] GameObject _container1;
    [SerializeField] GameObject _container2;

    [Header("UI RectTransforms for Sliding")]
    [SerializeField] RectTransform _tab1;
    [SerializeField] RectTransform _tab2;

    private int _activeTabIndex = 0;   // 0 for Tab 1, 1 for Tab 2
    Coroutine _coroutine;
    Coroutine _audioCoroutine;

    public bool IsViewed => _isViewed;
    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        _didSlided = _didTab1 = _didTab2 = false;
        _activeTabIndex = 0;

        if (_container1 != null) _container1.SetActive(false);
        if (_container2 != null) _container2.SetActive(false);

        _tab1.anchorMin = _tab1.anchorMax = new Vector2(.5f, .5f);
        _tab2.anchorMin = _tab2.anchorMax = new Vector2(.5f, .5f);

        _tab1.anchoredPosition = Vector3.up * 100;
        _tab2.anchoredPosition = Vector3.down * 100;

        _tab1.gameObject.SetActive(false);
        _tab2.gameObject.SetActive(false);

        if (_tab1.TryGetComponent(out PopEffect_Junior2A pop1)) pop1.enabled = true;
        if (_tab2.TryGetComponent(out PopEffect_Junior2A pop2)) pop2.enabled = true;

        _audioSource.clip = _introClip;
        _audioSource.Play();

        yield return new WaitForSeconds(_audioSource.clip.length);

        _tab1.gameObject.SetActive(true);
        _tab2.gameObject.SetActive(true);
    }

    public void moveslideup(int index)
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(MoveTab(index));
    }

    // Triggered automatically when a tab finishes opening
    public void playaudio()
    {
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAutoplaySequence());
    }

    IEnumerator MoveTab(int index)
    {
        _activeTabIndex = index;

        if (_container1 != null) _container1.SetActive(false);
        if (_container2 != null) _container2.SetActive(false);

        if (index == 0) { _didTab1 = true; if (_container1 != null) _container1.SetActive(true); }
        else if (index == 1) { _didTab2 = true; if (_container2 != null) _container2.SetActive(true); }

        if (_didTab1 && _didTab2)
        {
            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            _isViewed = true;
        }

        if (!_didSlided)
        {
            Vector3 worldPos1 = _tab1.position;
            Vector3 worldPos2 = _tab2.position;

            _tab1.anchorMin = _tab1.anchorMax = new Vector2(1f, .5f);
            _tab2.anchorMin = _tab2.anchorMax = new Vector2(1f, .5f);

            _tab1.position = worldPos1;
            _tab2.position = worldPos2;

            Vector2 startPos1 = _tab1.anchoredPosition;
            Vector2 startPos2 = _tab2.anchoredPosition;

            Vector2 targetPos1 = new Vector2(-250f, startPos1.y);
            Vector2 targetPos2 = new Vector2(-250f, startPos2.y);

            float slideSpeed = 2.5f;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * slideSpeed;
                float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

                _tab1.anchoredPosition = Vector2.LerpUnclamped(startPos1, targetPos1, easedT);
                _tab2.anchoredPosition = Vector2.LerpUnclamped(startPos2, targetPos2, easedT);

                yield return null;
            }

            _tab1.anchoredPosition = targetPos1;
            _tab2.anchoredPosition = targetPos2;

            _didSlided = true;
        }

        // Auto-play all audio sequence items linked to this specific tab view context
        playaudio();
    }

    IEnumerator PlayAutoplaySequence()
    {
        AudioClip[] activeArray = (_activeTabIndex == 0) ? _tab1Audio : _tab2Audio;

        if (activeArray != null)
        {
            for (int i = 0; i < activeArray.Length; i++)
            {
                if (activeArray[i] == null) continue;

                _audioSource.clip = activeArray[i];
                _audioSource.Play();

                // Wait until this specific audio file completely finishes playing before moving to the next
                yield return new WaitForSeconds(activeArray[i].length);
            }
        }
    }
}