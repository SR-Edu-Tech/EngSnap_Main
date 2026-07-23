using Junior2A;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U5_R02_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;

    [Header("Separate Tab Audio Arrays")]
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

    [Header("Score Display Configuration")]
    [SerializeField] TextMeshProUGUI _scoreText; // Drag your TMPro Text component here

    [Header("Tracking Visited Audios")]
    [SerializeField] List<int> _tab1ClickedIndices = new List<int>();
    [SerializeField] List<int> _tab2ClickedIndices = new List<int>();

    private int _activeTabIndex = 0;   // 0 for Tab 1, 1 for Tab 2
    Coroutine _coroutine;
    Coroutine _audioCoroutine;

    public bool IsViewed => _isViewed;
    void OnEnable() => StartCoroutine(Starter());

    IEnumerator Starter()
    {
        _didSlided = _didTab1 = _didTab2 = false;
        _activeTabIndex = 0;
        _tab1ClickedIndices.Clear();
        _tab2ClickedIndices.Clear();

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

        UpdateScoreUI();

        _audioSource.clip = _introClip;
        _audioSource.Play();

        yield return new WaitForSeconds(_introClip.length);

        _tab1.gameObject.SetActive(true);
        _tab2.gameObject.SetActive(true);
    }

    public void moveslideup(int index)
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(MoveTab(index));
    }

    // Bind your content sub-buttons directly to this function inside OnClick()
    public void PlayExplicitAudio(int audioIndex)
    {
        // Increment scoring immediately upon clicking, bypassing audio processing delays
        TrackClickProgress(audioIndex);

        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(StartManualAudio(audioIndex));
    }

    private void TrackClickProgress(int index)
    {
        if (_activeTabIndex == 0)
        {
            if (!_tab1ClickedIndices.Contains(index))
            {
                _tab1ClickedIndices.Add(index);
                UpdateScoreUI();
            }
        }
        else if (_activeTabIndex == 1)
        {
            if (!_tab2ClickedIndices.Contains(index))
            {
                _tab2ClickedIndices.Add(index);
                UpdateScoreUI();
            }
        }

        CheckCompletionProgress();
    }

    private void UpdateScoreUI()
    {
        if (_scoreText != null)
        {
            int currentScore = _tab1ClickedIndices.Count + _tab2ClickedIndices.Count;
            int totalExpected = (_tab1Audio != null ? _tab1Audio.Length : 0) + (_tab2Audio != null ? _tab2Audio.Length : 0);
            _scoreText.text = currentScore.ToString() + "/" + totalExpected.ToString();
        }
    }

    IEnumerator StartManualAudio(int index)
    {
        AudioClip clipToPlay = null;

        if (_activeTabIndex == 0)
        {
            if (_tab1Audio != null && index < _tab1Audio.Length) clipToPlay = _tab1Audio[index];
        }
        else if (_activeTabIndex == 1)
        {
            if (_tab2Audio != null && index < _tab2Audio.Length) clipToPlay = _tab2Audio[index];
        }

        if (clipToPlay != null)
        {
            _audioSource.clip = clipToPlay;
            _audioSource.Play();
            yield return new WaitForSeconds(clipToPlay.length);
        }
    }

    private void CheckCompletionProgress()
    {
        if (_activeTabIndex == 0 && _tab1Audio != null && _tab1ClickedIndices.Count >= _tab1Audio.Length)
        {
            _didTab1 = true;
        }
        else if (_activeTabIndex == 1 && _tab2Audio != null && _tab2ClickedIndices.Count >= _tab2Audio.Length)
        {
            _didTab2 = true;
        }

        if (_didTab1 && _didTab2)
        {
            if (GameManager_Junior2A.Instance != null) GameManager_Junior2A.Instance.Next(true);
            _isViewed = true;
        }
    }

    IEnumerator MoveTab(int index)
    {
        _activeTabIndex = index;

        if (_container1 != null) _container1.SetActive(false);
        if (_container2 != null) _container2.SetActive(false);

        if (index == 0) { if (_container1 != null) _container1.SetActive(true); }
        else if (index == 1) { if (_container2 != null) _container2.SetActive(true); }

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
    }
}