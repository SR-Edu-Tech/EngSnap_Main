using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U10_R02_Junior1A_QuestionData
{
    public string[] OptionTexts;
    public int CorrectOptionIndex;
}

public class U10_R02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [SerializeField] bool _isViewed = false, _slided = false;
    
    [Header("General Tabs Setup")]
    [SerializeField] RectTransform _tab1, _tab2, _tab3;
    [SerializeField] GameObject _tab1P, _tab2P, _tab3P;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] Image _currentSpeakerIcon;
    int _currentTabIndex;
    List<int> _clickedTabs = new List<int>();
    HashSet<int> _tab1ClickedAudio = new HashSet<int>();
    int _tab2Matches = 0;
    int _tab3Answers = 0;

    [Header("Tab 1: Read & Listen")]
    [SerializeField] AudioClip[] _tab1AudioClips;
    int _currentAudioClipIndex;
    Coroutine _audioCoroutine, _tabChange;
    Color[] _tab1OriginalColors;

    [Header("Tab 2: Match The Following")]
    [SerializeField] Button[] _leftFishes;
    [SerializeField] Button[] _rightFishes;
    int _selectedLeftIndex = -1;
    LineRenderer _currentLR;
    Coroutine _dragCoroutine;

    [Header("Tab 3: Fill in the Blanks")]
    [SerializeField] AudioClip _incorrectClip;
    [SerializeField] AudioClip _correctClip;
    [SerializeField] Transform _spawnBox, _questionParent;
    [SerializeField] TextMeshProUGUI _clickedOptionText;
    [SerializeField] Button _clickedButton;
    [SerializeField] List<string> _defaultText;
    [SerializeField] U10_R02_Junior1A_QuestionData[] _questionData;
    [SerializeField] Color _wrongColor = Color.red, _correctColor = Color.green;
    int _currentQuestionIndex = 0, _currentAnswerIndex = 0;
    Coroutine _tab3Coroutine;

    RectTransform[] Tabs => new[] { _tab1, _tab2, _tab3 };
    GameObject[] TabPs => new[] { _tab1P, _tab2P, _tab3P };

    public bool IsViewed => _isViewed;

    void Start()
    {
        if (_tab1) _tab1.GetComponent<Button>().interactable = true;
        if (_tab2) _tab2.GetComponent<Button>().interactable = false;
        if (_tab3) _tab3.GetComponent<Button>().interactable = false;

        if (_tab1P != null && _tab1P.transform.childCount > 0)
        {
            Transform btnContainer = _tab1P.transform.GetChild(0);
            _tab1OriginalColors = new Color[btnContainer.childCount];
            for (int i = 0; i < btnContainer.childCount; i++)
            {
                Transform btn = btnContainer.GetChild(i);
                if (btn.childCount > 0)
                {
                    Image bgImg = btn.GetChild(0).GetComponent<Image>();
                    if (bgImg != null)
                        _tab1OriginalColors[i] = bgImg.color;
                }
            }
        }

        if (_questionParent != null && _defaultText.Count == 0)
        {
            foreach (Transform child in _questionParent.transform)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null && child.childCount > 0 && child.GetChild(0).childCount > 0 && child.GetChild(0).GetChild(0).childCount > 0)
                {
                    TextMeshProUGUI tmp = child.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
                    if (tmp != null) _defaultText.Add(tmp.text);
                }
            }
        }
    }

    void OnEnable()
    {
        if (_audioSource && _introClip)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }
    }

    void OnDisable()
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
        _clickedTabs.Clear();
        _tab1ClickedAudio.Clear();
        _tab2Matches = 0;
        _tab3Answers = 0;
        if (_tab1) _tab1.GetComponent<Button>().interactable = true;
        if (_tab2) _tab2.GetComponent<Button>().interactable = false;
        if (_tab3) _tab3.GetComponent<Button>().interactable = false;

        // Tab 1 UI reset
        if (_tab1P != null && _tab1P.transform.childCount > 0 && _tab1OriginalColors != null)
        {
            Transform btnContainer = _tab1P.transform.GetChild(0);
            for (int i = 0; i < btnContainer.childCount; i++)
            {
                if (i >= _tab1OriginalColors.Length) break;
                Transform btn = btnContainer.GetChild(i);
                if (btn.childCount > 0)
                {
                    Image bgImg = btn.GetChild(0).GetComponent<Image>();
                    if (bgImg != null)
                        bgImg.color = _tab1OriginalColors[i];
                }
            }
        }

        // Setup Tab 2: Match The Following listeners
        for (int i = 0; i < _leftFishes.Length; i++)
        {
            int index = i;
            _leftFishes[i].onClick.RemoveAllListeners();
            _leftFishes[i].onClick.AddListener(() => OnLeftClick(index));
            _leftFishes[i].interactable = true;

            Transform lrTransform = _leftFishes[i].transform.childCount > 1 ? _leftFishes[i].transform.GetChild(1) : null;
            if (lrTransform)
            {
                LineRenderer lr = lrTransform.GetComponent<LineRenderer>();
                if (lr) lr.positionCount = 0;
            }
        }

        for (int i = 0; i < _rightFishes.Length; i++)
        {
            int index = i;
            _rightFishes[i].onClick.RemoveAllListeners();
            _rightFishes[i].onClick.AddListener(() => OnRightClick(index));
            _rightFishes[i].interactable = true;
        }

        _selectedLeftIndex = -1;
        if (_currentLR) _currentLR.positionCount = 0;
        _currentLR = null;

        // Setup Tab 3: Fill in the blanks UI reset
        if (_questionParent != null)
        {
            int _currentDefaultOptionIndex = 0;
            foreach (Transform child in _questionParent.transform)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null && _questionData != null && _currentDefaultOptionIndex < _questionData.Length)
                {
                    btn.interactable = true;
                    if (child.childCount > 0 && child.GetChild(0).childCount > 0 && child.GetChild(0).GetChild(0).childCount > 0)
                    {
                        TextMeshProUGUI tmp = child.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
                        if (tmp != null)
                        {
                            if (_defaultText.Count > _currentDefaultOptionIndex) tmp.text = _defaultText[_currentDefaultOptionIndex];
                            
                            MonoBehaviour textPop = tmp.GetComponent("TextPopEffect_Junior1A") as MonoBehaviour;
                            if (textPop != null) textPop.enabled = false;
                        }
                    }
                    _currentDefaultOptionIndex++;
                }
                child.gameObject.SetActive(false);
            }
        }
        if (_spawnBox != null)
        {
            foreach (Transform button in _spawnBox.transform) button.gameObject.SetActive(false);
        }
        _currentQuestionIndex = _currentAnswerIndex = 0;
    }

    // --- GENERAL TABS ANIMATION ---
    public void TabSlideUp(int index)
    {
        if (!_clickedTabs.Contains(index))
        {
            _clickedTabs.Add(index);
        }

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

        if (_audioSource) _audioSource.Stop();

        if (_slided)
        {
            if (index == _currentTabIndex) return;
            _currentTabIndex = index;
            foreach (var tabP in TabPs)
            {
                if (tabP != null && tabP == TabPs[_currentTabIndex])
                {
                    foreach (Transform child in tabP.transform) child.gameObject.SetActive(false);
                }
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
            if (tab == null) { tIndex1++; continue; }
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

            if (currentTabP.childCount > 0)
            {
                Transform container = currentTabP.GetChild(0);
                if (_currentTabIndex == 2 && container.childCount > 0)
                {
                    container = container.GetChild(0);
                    RectTransform rect = container.GetComponent<RectTransform>();
                    if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 0f);
                }

                foreach (Transform child in container)
                {
                    Button btn = child.GetComponent<Button>();
                    if (_currentTabIndex == 1 && btn != null && !btn.interactable)
                    {
                        continue;
                    }
                    child.gameObject.SetActive(false);
                }
            }
            foreach (Transform child in currentTabP) child.gameObject.SetActive(true);

            // Tab 3 UI hide pre-animation
            if (_currentTabIndex == 2 && _questionParent != null)
            {
                foreach (Transform child in _questionParent) child.gameObject.SetActive(false);
            }

            yield return new WaitForSeconds(1);

            if (currentTabP.childCount > 0)
            {
                Transform container = currentTabP.GetChild(0);
                if (_currentTabIndex == 2 && container.childCount > 0)
                {
                    container = container.GetChild(0);
                }

                foreach (Transform child in container)
                {
                    Button btn = child.GetComponent<Button>();
                    if (_currentTabIndex == 1 && btn != null && !btn.interactable)
                    {
                        continue;
                    }
                    child.gameObject.SetActive(true);
                    yield return new WaitForSeconds(.25f);
                }
            }

            // Tab 3 question spawn animation
            if (_currentTabIndex == 2 && _questionParent != null)
            {
                foreach (Transform button in _questionParent)
                {
                    button.gameObject.SetActive(true);
                    yield return new WaitForSeconds(.25f);
                }
            }
        }
    }

    // --- TAB 1: READ/LISTEN ---
    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    public void PlayAudio(int index)
    {
        _currentAudioClipIndex = index;
        if (_currentTabIndex != 0 || _tab1P == null) return;

        _tab1ClickedAudio.Add(index);
        if (_tab1ClickedAudio.Count >= _tab1AudioClips.Length)
        {
            if (_tab2) _tab2.GetComponent<Button>().interactable = true;
        }

        Transform currentTabP = _tab1P.transform;
        if (currentTabP.childCount > 0 && currentTabP.GetChild(0).childCount > index)
        {
            Transform btnContainer = currentTabP.GetChild(0);
            Transform btn = btnContainer.GetChild(index);
            if (btn.childCount > 0)
            {
                Image bgImg = btn.GetChild(0).GetComponent<Image>();
                if (bgImg != null && _tab1OriginalColors != null && index < _tab1OriginalColors.Length)
                {
                    bgImg.color = new Color(_tab1OriginalColors[index].r * 0.5f, _tab1OriginalColors[index].g * 0.5f, _tab1OriginalColors[index].b * 0.5f, _tab1OriginalColors[index].a);
                }

                if (btn.GetChild(0).childCount > 0)
                {
                    Image iconImg = btn.GetChild(0).GetChild(0).GetComponent<Image>();
                    if (iconImg != null)
                    {
                        Sprite btnSprite = iconImg.sprite;
                        if (index % 2 == 0 && currentTabP.childCount > 1)
                        {
                            Image targetImg = currentTabP.GetChild(1).GetComponent<Image>();
                            if (targetImg != null) targetImg.sprite = btnSprite;
                            PopEffect_Junior1A pop = currentTabP.GetChild(1).GetComponent<PopEffect_Junior1A>();
                            if (pop != null) { pop.enabled = false; pop.enabled = true; }
                        }
                        else if (index % 2 != 0 && currentTabP.childCount > 2)
                        {
                            Image targetImg = currentTabP.GetChild(2).GetComponent<Image>();
                            if (targetImg != null) targetImg.sprite = btnSprite;
                            PopEffect_Junior1A pop = currentTabP.GetChild(2).GetComponent<PopEffect_Junior1A>();
                            if (pop != null) { pop.enabled = false; pop.enabled = true; }
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
        if (_tab1AudioClips != null && _currentAudioClipIndex >= 0 && _currentAudioClipIndex < _tab1AudioClips.Length)
        {
            _audioSource.clip = _tab1AudioClips[_currentAudioClipIndex];
            _audioSource.Play();
            yield return new WaitForSeconds(_audioSource.clip.length);
        }
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }

    // --- TAB 2: MATCH THE FOLLOWING ---
    public void OnLeftClick(int index)
    {
        if (_currentTabIndex != 1) return;
        if (_currentLR)
        {
            _currentLR.positionCount = 0;
            if (_selectedLeftIndex != -1) _leftFishes[_selectedLeftIndex].interactable = true;
        }

        _selectedLeftIndex = index;
        _leftFishes[index].interactable = false;
        Transform lrTransform = _leftFishes[index].transform.childCount > 1 ? _leftFishes[index].transform.GetChild(1) : null;
        
        if (lrTransform)
        {
            _currentLR = lrTransform.GetComponent<LineRenderer>();
            _currentLR.positionCount = 2;
            _currentLR.SetPosition(0, lrTransform.position);
            _currentLR.SetPosition(1, lrTransform.position);

            if (_dragCoroutine != null) StopCoroutine(_dragCoroutine);
            _dragCoroutine = StartCoroutine(DragLine());
        }
    }

    IEnumerator DragLine()
    {
        while (_selectedLeftIndex != -1 && _currentLR)
        {
            Vector3 mousePos = Input.mousePosition;
            if (Camera.main != null)
            {
                mousePos.z = Mathf.Abs(Camera.main.transform.position.z - _currentLR.transform.position.z);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                worldPos.z = _currentLR.transform.position.z; 
                _currentLR.SetPosition(1, worldPos);
            }
            yield return null;
        }
    }

    public void OnRightClick(int index)
    {
        if (_currentTabIndex != 1 || _selectedLeftIndex == -1 || !_currentLR) return;

        if (_selectedLeftIndex == index)
        {
            Transform targetLR = _rightFishes[index].transform.childCount > 1 ? _rightFishes[index].transform.GetChild(1) : null;
            Vector3 targetPos = targetLR != null ? targetLR.position : _rightFishes[index].transform.position;
            _currentLR.SetPosition(1, targetPos);
            
            _leftFishes[_selectedLeftIndex].interactable = false;
            _rightFishes[index].interactable = false;

            _selectedLeftIndex = -1;
            _currentLR = null;

            _tab2Matches++;
            if (_tab2Matches >= _leftFishes.Length)
            {
                if (_tab3) _tab3.GetComponent<Button>().interactable = true;
            }

            if (_audioSource && _correctClip)
            {
                _audioSource.clip = _correctClip;
                _audioSource.Play();
            }
        }
        else
        {
            _currentLR.positionCount = 0;
            if (_selectedLeftIndex != -1) _leftFishes[_selectedLeftIndex].interactable = true;
            _selectedLeftIndex = -1;
            _currentLR = null;

            if (_audioSource && _incorrectClip)
            {
                _audioSource.clip = _incorrectClip;
                _audioSource.Play();
            }
        }
    }

    // --- TAB 3: FILL IN THE BLANKS ---
    public void ChooseQuestion(int index)
    {
        if (_currentTabIndex != 2) return;
        _currentQuestionIndex = index;
        _currentAnswerIndex = 0;
        if (_spawnBox != null && _questionData != null && _currentQuestionIndex < _questionData.Length)
        {
            foreach (Transform button in _spawnBox)
            {
                button.GetComponent<Image>().color = Color.white;
                if (_currentAnswerIndex < _questionData[_currentQuestionIndex].OptionTexts.Length)
                {
                    button.GetChild(0).GetComponent<TextMeshProUGUI>().text = _questionData[_currentQuestionIndex].OptionTexts[_currentAnswerIndex];
                }
                button.GetComponent<Button>().interactable = true;
                PopEffect_Junior1A pop = button.GetComponent<PopEffect_Junior1A>();
                if (pop != null) pop.enabled = true;
                button.gameObject.SetActive(true);
                _currentAnswerIndex++;
            }
        }
        _currentAnswerIndex = 0;
    }

    public void SetText(TextMeshProUGUI optionText) => _clickedOptionText = optionText;
    public void SetButton(Button optionButton) => _clickedButton = optionButton;

    public void ChooseOption(int index)
    {
        if (_currentTabIndex != 2 || _spawnBox == null) return;
        _spawnBox.GetChild(_currentAnswerIndex).GetComponent<Image>().color = Color.white;
        _currentAnswerIndex = index;
        if (_tab3Coroutine != null) StopCoroutine(_tab3Coroutine);
        _tab3Coroutine = StartCoroutine(CheckOption());
    }

    IEnumerator CheckOption()
    {
        if (_questionData == null || _currentQuestionIndex >= _questionData.Length) yield break;

        bool isCorrect = _questionData[_currentQuestionIndex].CorrectOptionIndex < 0 || _currentAnswerIndex == _questionData[_currentQuestionIndex].CorrectOptionIndex;

        if (isCorrect)
        {
            _spawnBox.GetChild(_currentAnswerIndex).GetComponent<Image>().color = _correctColor;
            foreach (Transform button in _spawnBox.transform) button.GetComponent<Button>().interactable = false;
            
            if (_clickedOptionText != null)
            {
                _clickedOptionText.text = _questionData[_currentQuestionIndex].OptionTexts[_currentAnswerIndex];
                MonoBehaviour textPop = _clickedOptionText.GetComponent("TextPopEffect_Junior1A") as MonoBehaviour;
                if (textPop != null) textPop.enabled = true;
            }
            if (_clickedButton != null) _clickedButton.interactable = false;
            
            _tab3Answers++;
            if (_tab3Answers >= _questionData.Length && !_isViewed)
            {
                _isViewed = true;
                if (GameManager_Junior1A.Instance != null) GameManager_Junior1A.Instance.Next(true);
            }
            
            if (_audioSource && _correctClip)
            {
                _audioSource.clip = _correctClip;
                _audioSource.Play();
            }
        }
        else
        {
            _spawnBox.GetChild(_currentAnswerIndex).GetComponent<Image>().color = _wrongColor;
            if (_audioSource && _incorrectClip)
            {
                _audioSource.clip = _incorrectClip;
                _audioSource.Play();
            }
            WiggleEffect_Junior1A1 wiggle = _spawnBox.GetChild(_currentAnswerIndex).GetComponent<WiggleEffect_Junior1A1>();
            if (wiggle != null) wiggle.enabled = true;
            
            if (_incorrectClip) yield return new WaitForSeconds(_incorrectClip.length);
            else yield return new WaitForSeconds(1f);
            
            foreach (Transform button in _spawnBox.transform) button.GetComponent<Button>().interactable = true;
        }
    }
}
