using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DemonstrativeSlideData
{
    public string SentenceText;
    public Sprite DisplaySprite;
    public AudioClip SentenceAudio;
}

[Serializable]
public class PronounGroup
{
    public string PronounLabel;
    public Button CategoryButton;
    [Tooltip("Spoken audio clip played automatically upon opening this pronoun group category.")]
    public AudioClip ButtonIntroAudio;
    public List<DemonstrativeSlideData> Slides;
}

public class U5_L01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("=== Central Display Components ===")]
    [SerializeField] private Image _centerDisplayImage;
    [SerializeField] private TextMeshProUGUI _centerDisplayText;

    [Header("=== Scalable Category Groups ===")]
    [SerializeField] private List<PronounGroup> _pronounGroups;

    [Header("=== Audio Components ===")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _levelIntroClip;

    [Header("=== Independent Control Buttons ===")]
    [SerializeField] private Button _repeatBtnObject;
    [SerializeField] private Button _slowBtnObject;

    [Header("=== State Trackers ===")]
    [SerializeField] private int _currentGroupIndex = 0;
    [SerializeField] private int _currentSlideIndex = 0;
    [SerializeField] private bool _isViewed = false;
    
    private bool _isSlowed = false; 
    private Coroutine _masterFlowCoroutine;

    // Tracks all completed indices to preserve colors across resets or pitch changes
    private HashSet<int> _completedGroupIndices = new HashSet<int>();

    public bool IsViewed => _isViewed;

    void Start() => InitializeLessonState();
    void OnEnable() => InitializeLessonState();

    private void InitializeLessonState()
    {
        _currentGroupIndex = 0;
        _currentSlideIndex = 0;
        _isSlowed = false;
        _completedGroupIndices.Clear();

        if (_audioSource != null) _audioSource.pitch = 1.0f;

        if (_centerDisplayText != null) _centerDisplayText.text = "";
        if (_centerDisplayImage != null) _centerDisplayImage.gameObject.SetActive(false);
        
        SetupUtilityControlListeners();
        ResetAllButtonVisualStates();
        SetPronounButtonsInteractableState(false); 

        if (_masterFlowCoroutine != null) StopCoroutine(_masterFlowCoroutine);
        _masterFlowCoroutine = StartCoroutine(AutoplayMasterFlow());
    }

    private void SetupUtilityControlListeners()
    {
        if (_repeatBtnObject != null)
        {
            _repeatBtnObject.interactable = true;
            _repeatBtnObject.onClick.RemoveAllListeners();
            _repeatBtnObject.onClick.AddListener(Repeat);
        }

        if (_slowBtnObject != null)
        {
            _slowBtnObject.interactable = true;
            _slowBtnObject.onClick.RemoveAllListeners();
            _slowBtnObject.onClick.AddListener(() => {
                TextMeshProUGUI label = _slowBtnObject.GetComponentInChildren<TextMeshProUGUI>();
                Slow(label);
            });
        }
    }

    private void ResetAllButtonVisualStates()
    {
        for (int i = 0; i < _pronounGroups.Count; i++)
        {
            if (_pronounGroups[i].CategoryButton != null)
            {
                ColorBlock cb = _pronounGroups[i].CategoryButton.colors;
                
                // If the group is current -> Solid Yellow. Completed -> Solid Green. Rest -> White.
                if (i == _currentGroupIndex)
                {
                    cb.normalColor = Color.yellow;
                }
                else if (_completedGroupIndices.Contains(i))
                {
                    cb.normalColor = Color.green;
                }
                else
                {
                    cb.normalColor = Color.white;
                }

                cb.disabledColor = cb.normalColor; // Forces color stability while unclickable
                _pronounGroups[i].CategoryButton.colors = cb;
            }
        }
    }

    private void SetPronounButtonsInteractableState(bool isInteractable)
    {
        foreach (var group in _pronounGroups)
        {
            if (group.CategoryButton != null) group.CategoryButton.interactable = isInteractable;
        }
    }

    IEnumerator AutoplayMasterFlow()
    {
        // 1. Play general introductory lesson brief
        if (_audioSource != null && _levelIntroClip != null)
        {
            _audioSource.clip = _levelIntroClip;
            _audioSource.Play();
            
            float calculatedWaitTime = _levelIntroClip.length / _audioSource.pitch;
            yield return new WaitForSeconds(calculatedWaitTime + 0.2f);
        }

        // 2. Automated group layout timeline progression loops
        while (_currentGroupIndex < _pronounGroups.Count)
        {
            // Forces the active button to instantly turn Solid Yellow
            ResetAllButtonVisualStates();
            PronounGroup currentGroup = _pronounGroups[_currentGroupIndex];

            // Trigger structural pop scale effects on target entry nodes
            if (currentGroup.CategoryButton != null)
            {
                Popeffect_Junior1B buttonPopper = currentGroup.CategoryButton.GetComponent<Popeffect_Junior1B>();
                if (buttonPopper != null)
                {
                    buttonPopper.enabled = false;
                    buttonPopper.enabled = true;
                }
            }

            // Spoken button category overview audio clip
            if (_audioSource != null && currentGroup.ButtonIntroAudio != null)
            {
                _audioSource.clip = currentGroup.ButtonIntroAudio;
                _audioSource.Play();
                
                float calculatedWaitTime = currentGroup.ButtonIntroAudio.length / _audioSource.pitch;
                yield return new WaitForSeconds(calculatedWaitTime + 0.4f);
            }

            // Central sub-slide display sequence loops
            while (currentGroup.Slides != null && _currentSlideIndex < currentGroup.Slides.Count)
            {
                DemonstrativeSlideData activeSlide = currentGroup.Slides[_currentSlideIndex];

                if (_centerDisplayImage != null && activeSlide.DisplaySprite != null)
                {
                    _centerDisplayImage.gameObject.SetActive(false);
                    
                    Popeffect_Junior1B imagePopper = _centerDisplayImage.GetComponent<Popeffect_Junior1B>();
                    if (imagePopper == null) imagePopper = _centerDisplayImage.gameObject.AddComponent<Popeffect_Junior1B>();
                    imagePopper.enabled = false;

                    _centerDisplayImage.sprite = activeSlide.DisplaySprite;
                    _centerDisplayImage.gameObject.SetActive(true);
                    imagePopper.enabled = true;
                }

                if (_centerDisplayText != null)
                {
                    _centerDisplayText.gameObject.SetActive(false);
                    _centerDisplayText.text = activeSlide.SentenceText;

                    yield return new WaitForEndOfFrame();
                    _centerDisplayText.gameObject.SetActive(true);
                }

                if (_audioSource != null && activeSlide.SentenceAudio != null)
                {
                    _audioSource.clip = activeSlide.SentenceAudio;
                    _audioSource.Play();

                    float calculatedWaitTime = activeSlide.SentenceAudio.length / _audioSource.pitch;
                    yield return new WaitForSeconds(calculatedWaitTime + 0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(2.5f);
                }

                _currentSlideIndex++;
            }

            // 🟢 Target group finish checklist node! Change from yellow to solid green
            _completedGroupIndices.Add(_currentGroupIndex); 
            
            _currentSlideIndex = 0; 
            _currentGroupIndex++;
        }

        EndLessonFlow();
    }

    // ==========================================
    // 🛠️ PUBLIC UTILITY CONTROLS
    // ==========================================

    public void Repeat()
    {
        if (_masterFlowCoroutine != null) StopCoroutine(_masterFlowCoroutine);
        if (_audioSource != null) _audioSource.Stop();

        InitializeLessonState();
    }

    public void Slow(TextMeshProUGUI slowButtonText)
    {
        if (_audioSource == null) return;

        if (slowButtonText != null)
        {
            slowButtonText.text = _isSlowed ? "    SLOW" : "    FAST";
        }
        
        _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;

        // Re-renders colors cleanly so active buttons stay yellow and finished stay green
        ResetAllButtonVisualStates();

        if (_audioSource.isPlaying)
        {
            float currentPlaybackTime = _audioSource.time;
            _audioSource.Play();
            _audioSource.time = currentPlaybackTime; 
        }
    }

    private void EndLessonFlow()
    {
        _isViewed = true;
        ResetAllButtonVisualStates();
        if (_centerDisplayText != null) _centerDisplayText.text = "EXCELLENT JOB!";
        if (GameManager_Junior1B.Instance != null) GameManager_Junior1B.Instance.Next(true);
    }
}