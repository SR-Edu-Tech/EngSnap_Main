using Junior2B;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U9_L02_Junior2B : MonoBehaviour, Interfaces_Junior2B
{
    [SerializeField] private bool _isViewed = false;

    [Header("=== UI Layout Elements ===")]
    [SerializeField] private GameObject _contentContainerPanel; // The main active child container panel
    [SerializeField] private ScrollRect _scrollRect;            // Drag your ScrollRect component here

    [Header("=== Audio Setup Elements ===")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;             // Drag your intro audio clip here
    [SerializeField] private AudioClip[] _audioClips;

    [Header("=== Tracking States ===")]
    [SerializeField] private Image _currentSpeakerIcon;
    [SerializeField] private int _currentAudioClipIndex;
    [SerializeField] private List<int> _clickCheckIndex = new List<int>();
    [SerializeField] private TextMeshProUGUI _clickedIndexText;

    private Coroutine _automationSequence;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        _clickCheckIndex.Clear();
        if (_clickedIndexText) _clickedIndexText.text = $"0/{_audioClips.Length}";

        _isViewed = false;

        // Reset scroll position directly to top on start
        if (_scrollRect != null)
        {
            _scrollRect.verticalNormalizedPosition = 1f;
        }

        if (_contentContainerPanel != null)
        {
            if (_automationSequence != null) StopCoroutine(_automationSequence);
            _automationSequence = StartCoroutine(RunAutoPlaybackAndScroll());
        }
    }

    void OnDisable()
    {
        if (_contentContainerPanel == null) return;

        // Clean up color adjustments
        foreach (int index in _clickCheckIndex)
        {
            int displayCountOffset = _contentContainerPanel.transform.childCount - 2;
            if (index >= 0 && index < displayCountOffset)
            {
                Transform btnTrans = _contentContainerPanel.transform.GetChild(index);
                if (btnTrans != null)
                {
                    Image img = GetButtonImage(btnTrans);
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
        _clickCheckIndex.Clear();
    }

    Image GetButtonImage(Transform buttonTrans)
    {
        if (buttonTrans == null) return null;
        if (buttonTrans.childCount > 1)
        {
            Image img = buttonTrans.GetChild(1).GetComponent<Image>();
            if (img != null) return img;
        }
        return buttonTrans.GetComponent<Image>();
    }

    IEnumerator RunAutoPlaybackAndScroll()
    {
        int elementButtonCount = _contentContainerPanel.transform.childCount - 2;

        // 1. Activate background overlay elements
        _contentContainerPanel.transform.GetChild(_contentContainerPanel.transform.childCount - 1).gameObject.SetActive(true);
        _contentContainerPanel.transform.GetChild(_contentContainerPanel.transform.childCount - 2).gameObject.SetActive(true);

        // 2. Clear content elements out of view initially
        for (int i = 0; i < elementButtonCount; i++)
        {
            _contentContainerPanel.transform.GetChild(i).gameObject.SetActive(false);
        }

        // 3. Play Intro Audio clip and wait for it to complete natively
        if (_introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            float introPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            yield return new WaitForSeconds((_introClip.length / introPitch) + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 4. Autoplay and Autoscroll loop sequence 
        for (int i = 0; i < elementButtonCount; i++)
        {
            // Safety: Ensure we don't exceed the assigned audio clips array bounds
            if (i >= _audioClips.Length) break;

            Transform currentChild = _contentContainerPanel.transform.GetChild(i);
            currentChild.gameObject.SetActive(true);

            Button btn = currentChild.GetComponent<Button>();
            if (btn != null) btn.interactable = false; // Keep interaction disabled while autoplaying

            // Smoothly auto-scroll to focus on the active playing item row
            if (_scrollRect != null && elementButtonCount > 1)
            {
                float targetNormalizedPos = 1f - ((float)i / (elementButtonCount - 1));
                float t = 0f;
                float initialPos = _scrollRect.verticalNormalizedPosition;

                while (t < 1f)
                {
                    t += Time.deltaTime * 3f; // Smooth scroll movement speed
                    _scrollRect.verticalNormalizedPosition = Mathf.Lerp(initialPos, targetNormalizedPos, t);
                    yield return null;
                }
                _scrollRect.verticalNormalizedPosition = targetNormalizedPos;
            }

            // Update UI/Progress data metrics for this entry
            UpdateProgressUI(i);

            // Play this button's assigned audio item explicitly inside this main loop
            _audioSource.clip = _audioClips[i];
            _audioSource.Play();

            // Calculate precise audio playback wait times directly from the source clip asset
            float speedAdjustmentFactor = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float completePlaybackWaitTime = (_audioClips[i].length / speedAdjustmentFactor) + 0.3f;

            yield return new WaitForSeconds(completePlaybackWaitTime);
        }

        // Reset the speaker layout indicator color highlight back to white
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;

        // Unlock all items for manual user review post-playback sequence run completion
        for (int i = 0; i < elementButtonCount; i++)
        {
            Button btn = _contentContainerPanel.transform.GetChild(i).GetComponent<Button>();
            if (btn != null) btn.interactable = true;
        }
    }

    private void UpdateProgressUI(int index)
    {
        _currentAudioClipIndex = index;

        if (!_clickCheckIndex.Contains(index))
        {
            _clickCheckIndex.Add(index);
            if (_clickedIndexText) _clickedIndexText.text = $"{_clickCheckIndex.Count}/{_audioClips.Length}";

            int displayCountOffset = _contentContainerPanel.transform.childCount - 2;
            if (index >= 0 && index < displayCountOffset)
            {
                Transform btnTrans = _contentContainerPanel.transform.GetChild(index);
                if (btnTrans != null)
                {
                    Image img = GetButtonImage(btnTrans);
                    if (img != null)
                    {
                        Color c = img.color;
                        c.r *= 0.85f;
                        c.g *= 0.85f;
                        c.b *= 0.85f;
                        img.color = c;
                    }
                }
            }

            if (_clickCheckIndex.Count == _audioClips.Length && !_isViewed)
            {
                _isViewed = true;
                GameManager_Junior2B.Instance.Next(true);
            }
        }
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    // Maintained for Interface structure requirements 
    public void PlayAudio(int index)
    {
        UpdateProgressUI(index);
        if (index >= 0 && index < _audioClips.Length)
        {
            _audioSource.clip = _audioClips[index];
            _audioSource.Play();
        }
    }
}