using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BodyPartTargetData
{
    public string TargetNameText;
    public AudioClip PromptAudio;
    public AudioClip SuccessAudio;
}

[Serializable]
public class BodyPartGroup
{
    public string GroupName;
    public List<Button> Buttons;
    public List<SpriteRenderer> Sprites;
}

public class U4_G01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("=== Game Targets Configuration ===")]
    [SerializeField] private BodyPartTargetData[] _gameTargets;

    [Header("=== UI ===")]
    [SerializeField] private TextMeshProUGUI _displayTargetText;

    [Header("=== Mascot ===")]
    [SerializeField] private GameObject _mascotMasterObject;

    [Header("=== Body Part Groups ===")]
    [SerializeField] private BodyPartGroup[] _bodyPartGroups;

    [Header("=== Audio ===")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip _wrongClip;

    [Header("=== Flash Colors ===")]
    [SerializeField] private Color _correctFlashColor = Color.green;
    [SerializeField] private Color _wrongFlashColor = Color.red;

    [Header("=== State ===")]
    [SerializeField] private int _currentTargetIndex = 0;
    [SerializeField] private bool _isViewed;
    [SerializeField] private bool _canClick = false;

    private Dictionary<SpriteRenderer, Color> _originalColors = new Dictionary<SpriteRenderer, Color>();

    public bool IsViewed => _isViewed;

    void Awake()
    {
        Debug.Log("[MASCOT] Awake called");
        ForceActivateMascot();
    }

    void LateUpdate()
    {
        Debug.Log("[MASCOT] LateUpdate activeSelf=" + _mascotMasterObject?.activeSelf);
    }



    void Start()
    {
        StartCoroutine(DelayedInit());
    }

    IEnumerator DelayedInit()
    {
        yield return null;
       // ForceActivateMascot();
        yield return null;

        RegisterButtonListeners();
        ForceActivateMascot();
        CacheOriginalColors();
        StopAllCoroutines();
        StartCoroutine(Starter());
    }

    private void RegisterButtonListeners()
    {
        foreach (var group in _bodyPartGroups)
        {
            BodyPartGroup capturedGroup = group;

            foreach (var btn in group.Buttons)
            {
                if (btn == null) continue;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnBodyPartClicked(capturedGroup));
                Debug.Log($"[REGISTER] {group.GroupName} button registered");
            }
        }
    }

    private void CacheOriginalColors()
    {
        _originalColors.Clear();
        foreach (var group in _bodyPartGroups)
            foreach (var sr in group.Sprites)
                if (sr != null)
                    _originalColors[sr] = sr.color;
    }

    private void OnBodyPartClicked(BodyPartGroup clickedGroup)
    {
        if (!_canClick) return;

        BodyPartTargetData roundData = _gameTargets[_currentTargetIndex];
        string targetKeyWord = roundData.TargetNameText.ToUpper();
        string clickedName = clickedGroup.GroupName.ToUpper();

        Debug.Log($"[CLICK] Target: {targetKeyWord} | Clicked: {clickedName}");

        if (clickedName == targetKeyWord)
            StartCoroutine(CorrectAnswerSequence(clickedGroup, roundData));
        else
            StartCoroutine(WrongAnswerSequence(clickedGroup));
    }

    IEnumerator Starter()
    {
        _currentTargetIndex = 0;
        _canClick = false;

        // if (transform.childCount > 0) transform.GetChild(transform.childCount - 1).gameObject.SetActive(false);

        if (_displayTargetText != null) _displayTargetText.text = "";

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        LoadTargetRound();
    }

    private void LoadTargetRound()
    {
        if (_gameTargets == null || _gameTargets.Length == 0 || _currentTargetIndex >= _gameTargets.Length)
        {
            EndGameChallengeFlow();
            return;
        }

        BodyPartTargetData currentRound = _gameTargets[_currentTargetIndex];

        if (_displayTargetText != null)
            _displayTargetText.text = $"TARGET: {currentRound.TargetNameText.ToUpper()}";

        if (_audioSource != null && currentRound.PromptAudio != null)
        {
            _audioSource.clip = currentRound.PromptAudio;
            _audioSource.Play();
        }

        _canClick = true;
    }

    IEnumerator CorrectAnswerSequence(BodyPartGroup group, BodyPartTargetData data)
    {
        _canClick = false;
        SetGroupColor(group, _correctFlashColor);

        if (_audioSource != null && data.SuccessAudio != null)
        {
            _audioSource.clip = data.SuccessAudio;
            _audioSource.Play();
            yield return new WaitForSeconds(data.SuccessAudio.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }

        ResetGroupColor(group);
        _currentTargetIndex++;
        LoadTargetRound();
    }

    IEnumerator WrongAnswerSequence(BodyPartGroup group)
    {
        _canClick = false;
        SetGroupColor(group, _wrongFlashColor);

        if (_audioSource != null && _wrongClip != null)
        {
            _audioSource.clip = _wrongClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_wrongClip.length + 0.1f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        ResetGroupColor(group);
        _canClick = true;
    }

    private void SetGroupColor(BodyPartGroup group, Color color)
    {
        foreach (var sr in group.Sprites)
            if (sr != null) sr.color = color;
    }

    private void ResetGroupColor(BodyPartGroup group)
    {
        foreach (var sr in group.Sprites)
            if (sr != null && _originalColors.ContainsKey(sr))
                sr.color = _originalColors[sr];
    }

    private void EndGameChallengeFlow()
    {
        _isViewed = true;
        if (_displayTargetText != null) _displayTargetText.text = "EXCELLENT JOB!";

        if (transform.childCount > 0)
            transform.GetChild(transform.childCount - 1).gameObject.SetActive(true);

        if (GameManager_Junior1B.Instance != null)
            GameManager_Junior1B.Instance.Next(true);
    }

    [ContextMenu("Force Activate Mascot")]
    public void ForceActivateMascot()
    {
        if (_mascotMasterObject != null)
        {
            _mascotMasterObject.SetActive(true);
            Debug.Log("[MASCOT] Activated!");
        }
        else
        {
            Debug.LogError("[MASCOT] NULL — assign in Inspector!");
        }
    }
}