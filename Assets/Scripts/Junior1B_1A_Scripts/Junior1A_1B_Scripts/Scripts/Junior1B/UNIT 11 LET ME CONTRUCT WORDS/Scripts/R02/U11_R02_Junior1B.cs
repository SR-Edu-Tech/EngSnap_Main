using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U11_R02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] bool _isViewed = false, _slided = false, _tab1Opened = false, _tab2Opened = false;
    [SerializeField] RectTransform _tab1, _tab2;
    [SerializeField] GameObject _tab1P, _tab2P;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _tab1AudioClips, _tab2AudioClips;
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex, _currentTabIndex;

    [Header("Universal Input Configurations (Outside Tabs)")]
    [SerializeField] private GameObject _universalInputBoxParent;
    [SerializeField] private TMP_InputField _universalInputField;
    [SerializeField] private Button _universalSubmitButton;

    [Header("Visual Configuration Customization")]
    [SerializeField] private Color _clickedColor = Color.green;

    Coroutine _audioCoroutine, _tabChange;

    private HashSet<string> _completedElements = new HashSet<string>();
    public static Dictionary<string, string> SavedInputs { get; private set; } = new Dictionary<string, string>();

    private int _activeInputIndex = -1;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        _slided = _tab1Opened = _tab2Opened = false;
        _completedElements.Clear();
        SavedInputs.Clear();

        if (_tab1 != null)
        {
            _tab1.anchoredPosition = Vector3.left * 300;
            _tab1.anchorMin = new Vector2(.5f, .5f);
            _tab1.anchorMax = new Vector2(.5f, .5f);
            _tab1.localScale = Vector3.one;
            if (_tab1.TryGetComponent(out Image img1)) img1.color = Color.white;
            if (_tab1.childCount > 0 && _tab1.GetChild(0).TryGetComponent(out TextMeshProUGUI txt1))
                txt1.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }

        if (_tab2 != null)
        {
            _tab2.anchoredPosition = Vector3.right * 300;
            _tab2.anchorMin = new Vector2(.5f, .5f);
            _tab2.anchorMax = new Vector2(.5f, .5f);
            _tab2.localScale = Vector3.one;
            if (_tab2.TryGetComponent(out Image img2)) img2.color = Color.white;
            if (_tab2.childCount > 0 && _tab2.GetChild(0).TryGetComponent(out TextMeshProUGUI txt2))
                txt2.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }

        if (_currentTabIndex == 0 && _tab1P != null && _tab1P.TryGetComponent(out CanvasGroup cg1)) cg1.alpha = 0;
        else if (_currentTabIndex != 0 && _tab2P != null && _tab2P.TryGetComponent(out CanvasGroup cg2)) cg2.alpha = 0;

        if (_universalInputBoxParent != null) _universalInputBoxParent.SetActive(false);
        ResetAllButtonVisualColors();

        if (_universalSubmitButton != null)
        {
            _universalSubmitButton.onClick.RemoveAllListeners();
            _universalSubmitButton.onClick.AddListener(OnSubmitPlayerInput);
        }

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }
    }

    void OnDisable()
    {
        if (_universalSubmitButton != null)
        {
            _universalSubmitButton.onClick.RemoveListener(OnSubmitPlayerInput);
        }
    }

    public void TabSlideUp(int index)
    {
        if (index == 0)
        {
            if (_tab1 != null && _tab1.TryGetComponent(out Image img1)) img1.color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
            if (_tab2 != null && _tab2.TryGetComponent(out Image img2)) img2.color = Color.white;
            if (_tab1 != null && _tab1.childCount > 0 && _tab1.GetChild(0).TryGetComponent(out TextMeshProUGUI txt1)) txt1.color = Color.white;
            if (_tab2 != null && _tab2.childCount > 0 && _tab2.GetChild(0).TryGetComponent(out TextMeshProUGUI txt2)) txt2.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
        }
        else
        {
            if (_tab1 != null && _tab1.TryGetComponent(out Image img1)) img1.color = Color.white;
            if (_tab2 != null && _tab2.TryGetComponent(out Image img2)) img2.color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
            if (_tab1 != null && _tab1.childCount > 0 && _tab1.GetChild(0).TryGetComponent(out TextMeshProUGUI txt1)) txt1.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            if (_tab2 != null && _tab2.childCount > 0 && _tab2.GetChild(0).TryGetComponent(out TextMeshProUGUI txt2)) txt2.color = Color.white;
        }

        if (_audioSource != null) _audioSource.Stop();
        if (_universalInputBoxParent != null) _universalInputBoxParent.SetActive(false);

        if (_slided)
        {
            if (index == _currentTabIndex) return;
            _currentTabIndex = index;

            if (_tab1P != null) foreach (Transform child in _tab1P.transform) child.gameObject.SetActive(false);
            if (_tab2P != null) foreach (Transform child in _tab2P.transform) child.gameObject.SetActive(false);

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

        if (_tab1P != null) foreach (Transform child in _tab1P.transform) child.gameObject.SetActive(false);
        if (_tab2P != null) foreach (Transform child in _tab2P.transform) child.gameObject.SetActive(false);

        if (_tab1 != null) _tab1.localScale = Vector3.one;
        if (_tab2 != null) _tab2.localScale = Vector3.one;

        Vector2 startPos1 = _tab1 != null ? _tab1.anchoredPosition : Vector2.zero;
        Vector2 startPos2 = _tab2 != null ? _tab2.anchoredPosition : Vector2.zero;

        Vector2 targetPos1 = new Vector2(startPos1.x, 353f);
        Vector2 targetPos2 = new Vector2(startPos2.x, 353f);

        CanvasGroup cg1 = _tab1P != null ? _tab1P.GetComponent<CanvasGroup>() : null;
        CanvasGroup cg2 = _tab2P != null ? _tab2P.GetComponent<CanvasGroup>() : null;

        float slideSpeed = 2.5f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * slideSpeed;
            float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));

            if (_tab1 != null) { _tab1.anchoredPosition = Vector2.Lerp(startPos1, targetPos1, easedT); _tab1.localScale = Vector3.one; }
            if (_tab2 != null) { _tab2.anchoredPosition = Vector2.Lerp(startPos2, targetPos2, easedT); _tab2.localScale = Vector3.one; }

            if (cg1 != null) cg1.alpha = easedT;
            if (cg2 != null) cg2.alpha = easedT;

            yield return null;
        }

        if (_tab1 != null) { _tab1.anchoredPosition = targetPos1; _tab1.localScale = Vector3.one; }
        if (_tab2 != null) { _tab2.anchoredPosition = targetPos2; _tab2.localScale = Vector3.one; }

        if (cg1 != null) cg1.alpha = 1;
        if (cg2 != null) cg2.alpha = 1;

        _tabChange = StartCoroutine(OnTab());
    }

    IEnumerator OnTab()
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;

        GameObject currentTabGO = _currentTabIndex == 0 ? _tab1P : _tab2P;
        if (currentTabGO == null) yield break;

        Transform currentTabP = currentTabGO.transform;

        if (currentTabP.childCount > 0)
        {
            Transform firstChild = currentTabP.GetChild(0);
            if (firstChild.TryGetComponent(out RectTransform rect))
            {
                rect.anchoredPosition = Vector2.zero;
            }
            foreach (Transform child in firstChild) child.gameObject.SetActive(false);
        }

        foreach (Transform child in currentTabP) child.gameObject.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        if (currentTabP.childCount > 0)
        {
            foreach (Transform child in currentTabP.GetChild(0))
            {
                child.gameObject.SetActive(true);
                if (child.TryGetComponent(out Button btn)) btn.interactable = true;
                yield return new WaitForSeconds(.1f);
            }
        }

        if (_currentTabIndex == 0) _tab1Opened = true;
        else _tab2Opened = true;
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    public void PlayAudio(int index)
    {
        _currentAudioClipIndex = index;
        GameObject currentTabGO = _currentTabIndex == 0 ? _tab1P : _tab2P;
        if (currentTabGO == null) return;

        Transform currentTabP = currentTabGO.transform;
        if (currentTabP.childCount == 0) return;

        // Get the Scroll View -> Viewport -> Content layout group
        Transform contentParent = currentTabP.GetComponentInChildren<ScrollRect>() != null ?
            currentTabP.GetComponentInChildren<ScrollRect>().content : currentTabP.GetChild(0);

        AudioClip[] currentClips = _currentTabIndex == 0 ? _tab1AudioClips : _tab2AudioClips;

        if (currentClips == null || index >= currentClips.Length)
        {
            Debug.LogWarning($"[PlayAudio] Index {index} out of bounds or Clips array empty. Opening Input Box.");
            OpenPlayerInputInterface(index);
            return;
        }

        AudioClip TargetClip = currentClips[index];

        if (TargetClip == null)
        {
            Debug.Log($"[PlayAudio] Slot {index} is null. Triggering Input Box.");
            OpenPlayerInputInterface(index);
        }
        else
        {
            Debug.Log($"[PlayAudio] Playing: {TargetClip.name} at index {index}");

            if (index < contentParent.childCount)
            {
                Transform targetRow = contentParent.GetChild(index);

                // --- SAFELY FIND SPEAKER ICON IN NESTED HIERARCHY ---
                Image speakerImg = null;

                // Check if the row container itself has it
                if (targetRow.TryGetComponent(out Image rowImg) && targetRow.name.ToLower().Contains("speaker"))
                    speakerImg = rowImg;

                // Look deeper into StudentD/TeacherD children paths
                if (speakerImg == null)
                {
                    foreach (Transform child in targetRow)
                    {
                        if (child.name.EndsWith("D")) // Matches "StudentD" or "TeacherD"
                        {
                            Transform speakerTransform = child.Find("Speaker");
                            if (speakerTransform != null && speakerTransform.TryGetComponent(out Image sImg))
                            {
                                speakerImg = sImg;
                                break;
                            }
                        }
                    }
                }

                // Fallback: search anywhere down the line for an image component named Speaker
                if (speakerImg == null)
                {
                    foreach (Image img in targetRow.GetComponentsInChildren<Image>(true))
                    {
                        if (img.gameObject.name.ToLower().Contains("speaker"))
                        {
                            speakerImg = img;
                            break;
                        }
                    }
                }

                if (speakerImg != null) OnSpeaker(speakerImg);

                // Pop-effects animations
                if (index % 2 == 0)
                {
                    if (currentTabP.childCount > 1)
                    {
                        Transform displayLeft = currentTabP.GetChild(1);
                        if (displayLeft != null)
                        {
                            displayLeft.localScale = Vector3.one;
                            if (displayLeft.TryGetComponent(out Popeffect_Junior1B popLeft))
                            {
                                popLeft.enabled = false;
                                popLeft.enabled = true;
                            }
                        }
                    }
                }
                else
                {
                    if (currentTabP.childCount > 2)
                    {
                        Transform displayRight = currentTabP.GetChild(2);
                        if (displayRight != null)
                        {
                            displayRight.localScale = Vector3.one;
                            if (displayRight.TryGetComponent(out Popeffect_Junior1B popRight))
                            {
                                popRight.enabled = false;
                                popRight.enabled = true;
                            }
                        }
                    }
                }

                // Tint the main child dialogue background (StudentD / TeacherD) instead of the base root row
                Transform visualPlate = targetRow.Find("StudentD") ?? targetRow.Find("TeacherD");
                if (visualPlate != null && visualPlate.TryGetComponent(out Image plateImg))
                {
                    plateImg.color = _clickedColor;
                }
                else if (targetRow.TryGetComponent(out Image baseImg))
                {
                    baseImg.color = _clickedColor;
                }
            }

            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            _audioCoroutine = StartCoroutine(PlayAudioIndex());

            RegisterElementCompletion(index);
        }
    }

    private void OpenPlayerInputInterface(int buttonIndex)
    {
        _activeInputIndex = buttonIndex;

        if (_universalInputBoxParent != null) _universalInputBoxParent.SetActive(true);

        string saveKey = $"{_currentTabIndex}_{buttonIndex}";
        if (_universalInputField != null)
        {
            _universalInputField.text = SavedInputs.ContainsKey(saveKey) ? SavedInputs[saveKey] : "";
            _universalInputField.ActivateInputField();
        }
    }

    public void OnSubmitPlayerInput()
    {
        if (_universalInputField == null || string.IsNullOrEmpty(_universalInputField.text)) return;

        GameObject currentTabGO = _currentTabIndex == 0 ? _tab1P : _tab2P;
        if (currentTabGO == null) return;

        // Dynamically find the Content container holding your 18 or 8 rows
        Transform contentParent = currentTabGO.GetComponentInChildren<ScrollRect>() != null ?
            currentTabGO.GetComponentInChildren<ScrollRect>().content : currentTabGO.transform.GetChild(0);

        if (_activeInputIndex < 0 || _activeInputIndex >= contentParent.childCount) return;

        string saveKey = $"{_currentTabIndex}_{_activeInputIndex}";
        if (SavedInputs.ContainsKey(saveKey))
            SavedInputs[saveKey] = _universalInputField.text;
        else
            SavedInputs.Add(saveKey, _universalInputField.text);

        Transform targetRow = contentParent.GetChild(_activeInputIndex);

        // Find text component inside StudentD or TeacherD sub-containers
        TextMeshProUGUI rowText = targetRow.GetComponentInChildren<TextMeshProUGUI>();
        if (rowText != null) rowText.text = _universalInputField.text;

        // Color code completed panels
        Transform visualPlate = targetRow.Find("StudentD") ?? targetRow.Find("TeacherD");
        if (visualPlate != null && visualPlate.TryGetComponent(out Image plateImg))
        {
            plateImg.color = _clickedColor;
        }
        else if (targetRow.TryGetComponent(out Image baseImg))
        {
            baseImg.color = _clickedColor;
        }

        if (_universalInputBoxParent != null) _universalInputBoxParent.SetActive(false);

        RegisterElementCompletion(_activeInputIndex);
    }

    private void RegisterElementCompletion(int index)
    {
        string trackingKey = $"{_currentTabIndex}_{index}";
        if (!_completedElements.Contains(trackingKey))
        {
            _completedElements.Add(trackingKey);
        }

        int totalTargetElements = 0;
        if (_tab1P != null && _tab1P.transform.childCount > 0) totalTargetElements += _tab1P.transform.GetChild(0).childCount;
        if (_tab2P != null && _tab2P.transform.childCount > 0) totalTargetElements += _tab2P.transform.GetChild(0).childCount;

        if (_completedElements.Count >= totalTargetElements && !_isViewed)
        {
            _isViewed = true;
            if (GameManager_Junior1B.Instance != null)
            {
                GameManager_Junior1B.Instance.Next(true);
            }
        }
    }

    private void ResetAllButtonVisualColors()
    {
        if (_tab1P != null && _tab1P.transform.childCount > 0)
        {
            foreach (Transform item in _tab1P.transform.GetChild(0))
                if (item.TryGetComponent(out Image img)) img.color = Color.white;
        }
        if (_tab2P != null && _tab2P.transform.childCount > 0)
        {
            foreach (Transform item in _tab2P.transform.GetChild(0))
                if (item.TryGetComponent(out Image img)) img.color = Color.white;
        }
    }

    IEnumerator PlayAudioIndex()
    {
        AudioClip[] currentClips = _currentTabIndex == 0 ? _tab1AudioClips : _tab2AudioClips;
        if (currentClips == null || _currentAudioClipIndex >= currentClips.Length || _currentAudioClipIndex < 0) yield break;

        if (_audioSource != null && currentClips[_currentAudioClipIndex] != null)
        {
            _audioSource.clip = currentClips[_currentAudioClipIndex];
            _audioSource.Play();
            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _audioSource.clip.length / pV1;
            yield return new WaitForSeconds(aL1);
        }
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }
}