using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class DigraphData
{
    [Tooltip("Digraph name (e.g. 'ch')")]
    public string digraphName;

    [Header("Example 1")]
    public string example1Word;
    public Sprite example1Sprite;
    [Tooltip("Audio clip for the digraph sound in Example 1 (e.g. /ch/ sound)")]
    public AudioClip example1SoundAudio;
    [Tooltip("Audio clip for the Example 1 word (e.g. saying 'cheese')")]
    public AudioClip example1WordAudio;

    [Header("Example 2")]
    public string example2Word;
    public Sprite example2Sprite;
    [Tooltip("Audio clip for the digraph sound in Example 2 (e.g. /ch/ sound, or voiced /th/)")]
    public AudioClip example2SoundAudio;
    [Tooltip("Audio clip for the Example 2 word (e.g. saying 'chin' or 'this')")]
    public AudioClip example2WordAudio;
}

public class TeachTwoLettersOneSound_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Digraph Configuration")]
    public List<DigraphData> digraphsList = new List<DigraphData>();

    [Header("General UI Components")]
    public TextMeshProUGUI digraphTitleText;
    public TextMeshProUGUI instructionLabel;
    public RectTransform mascotCharacter;
    public GameObject nextCardButton;
    public GameObject prevButton;
    public GameObject globalNextButton;

    [Header("Dual Example Card UI")]
    public GameObject cardsContainer;
    
    [Space(5)]
    [Header("Left Card (Example 1)")]
    public Button leftCardButton;
    public TextMeshProUGUI leftDigraphText;
    public Image leftImage;
    public TextMeshProUGUI leftWordText;

    [Space(5)]
    [Header("Right Card (Example 2)")]
    public Button rightCardButton;
    public TextMeshProUGUI rightDigraphText;
    public Image rightImage;
    public TextMeshProUGUI rightWordText;

    [Header("Progress Bar Dotted")]
    public RectTransform progressDotsContainer;
    public GameObject progressDotPrefab;
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = new Color32(255, 255, 255, 60);
    public Color dotFilledColor = new Color32(76, 175, 80, 255);
    private List<GameObject> _dotInstances = new List<GameObject>();

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip introScreenAudio;
    public AudioClip popSFX;
    public AudioClip transitionSFX;

    [Header("Events & Transitions")]
    public UnityEvent onTeachComplete;
    public float wordScaleHighlight = 1.15f;
    public float animationSpeed = 4f;

    // Runtime state
    private int _currentIndex = 0;
    private bool _canTap = false;
    private Coroutine _audioSeqCoroutine;
    private Vector3 _originalMascotScale = Vector3.one;

    // Cached scales/rotations for resets
    private Vector3 _origLeftCardScale = Vector3.one;
    private Quaternion _origLeftCardRot = Quaternion.identity;

    private Vector3 _origRightCardScale = Vector3.one;
    private Quaternion _origRightCardRot = Quaternion.identity;

    private GameFlowManager_Senior_Phonics _flowManager;

    private void Awake()
    {
        // Cache original values
        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        if (leftCardButton != null)
        {
            _origLeftCardScale = leftCardButton.transform.localScale;
            _origLeftCardRot = leftCardButton.transform.localRotation;
        }

        if (rightCardButton != null)
        {
            _origRightCardScale = rightCardButton.transform.localScale;
            _origRightCardRot = rightCardButton.transform.localRotation;
        }

        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Dynamically find global next button under the unit panel
        if (globalNextButton == null)
        {
            Transform unitParent = transform.parent != null ? transform.parent.parent : null;
            if (unitParent != null)
            {
                Transform nextBtnTrans = unitParent.Find("NextButton");
                if (nextBtnTrans != null)
                {
                    globalNextButton = nextBtnTrans.gameObject;
                }
            }
        }
    }

    private void Start()
    {
        // Hook button listeners
        if (leftCardButton != null)
        {
            leftCardButton.onClick.RemoveAllListeners();
            leftCardButton.onClick.AddListener(OnLeftCardTapped);
        }

        if (rightCardButton != null)
        {
            rightCardButton.onClick.RemoveAllListeners();
            rightCardButton.onClick.AddListener(OnRightCardTapped);
        }

        if (nextCardButton != null)
        {
            Button btn = nextCardButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextCardTapped);
            }
        }

        if (prevButton != null)
        {
            Button btn = prevButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnPrevTapped);
            }
        }

        SetupProgressDots();
        ResetUI();
    }

    private void OnEnable()
    {
        ResetUI();
        StartCoroutine(IntroFlow());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        _audioSeqCoroutine = null;
    }

    private void ResetUI()
    {
        // Set scales of pop-in objects to zero
        if (mascotCharacter != null) mascotCharacter.localScale = Vector3.zero;
        if (nextCardButton != null) nextCardButton.SetActive(false);
        if (prevButton != null) prevButton.SetActive(false);
        if (globalNextButton != null) globalNextButton.SetActive(false);

        if (cardsContainer != null) cardsContainer.SetActive(false);

        ResetCardVisuals();

        _currentIndex = 0;
        _canTap = false;
        UpdateProgressDots();
    }

    private void ResetCardVisuals()
    {
        if (leftCardButton != null)
        {
            leftCardButton.transform.localScale = _origLeftCardScale;
            leftCardButton.transform.localRotation = _origLeftCardRot;
        }

        if (rightCardButton != null)
        {
            rightCardButton.transform.localScale = _origRightCardScale;
            rightCardButton.transform.localRotation = _origRightCardRot;
        }
    }

    private IEnumerator IntroFlow()
    {
        // 1. Play general screen intro audio if configured
        if (audioSource != null && introScreenAudio != null)
        {
            audioSource.clip = introScreenAudio;
            audioSource.Play();
        }

        // 2. Pop in the Mascot character
        if (mascotCharacter != null)
        {
            yield return StartCoroutine(PopUI(mascotCharacter));
        }

        if (audioSource != null && introScreenAudio != null)
        {
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Load the first digraph
        LoadDigraph(0);
    }

    private void LoadDigraph(int index)
    {
        if (digraphsList == null || index < 0 || index >= digraphsList.Count)
        {
            Debug.LogError("[TeachTwoLettersOneSound] Digraph index out of range: " + index);
            return;
        }

        _currentIndex = index;
        _canTap = false;

        if (nextCardButton != null) nextCardButton.SetActive(false);
        // Show previous button if we are past index 0
        if (prevButton != null) prevButton.SetActive(index > 0);

        if (_audioSeqCoroutine != null)
        {
            StopCoroutine(_audioSeqCoroutine);
            _audioSeqCoroutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        ResetCardVisuals();
        UpdateProgressDots();

        DigraphData data = digraphsList[index];

        if (digraphTitleText != null)
        {
            digraphTitleText.text = data.digraphName;
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap the cards to replay the sounds.";
        }

        // Populate Left Card (Example 1)
        if (leftDigraphText != null) leftDigraphText.text = data.digraphName;
        if (leftWordText != null) leftWordText.text = data.example1Word;
        if (leftImage != null && data.example1Sprite != null)
        {
            leftImage.sprite = data.example1Sprite;
            leftImage.gameObject.SetActive(true);
        }

        // Populate Right Card (Example 2)
        if (rightDigraphText != null) rightDigraphText.text = data.digraphName;
        if (rightWordText != null) rightWordText.text = data.example2Word;
        if (rightImage != null && data.example2Sprite != null)
        {
            rightImage.sprite = data.example2Sprite;
            rightImage.gameObject.SetActive(true);
        }

        // Animate Cards Container Entry
        if (cardsContainer != null)
        {
            cardsContainer.SetActive(true);
            cardsContainer.transform.localScale = Vector3.zero;
            LeanTween.cancel(cardsContainer);
            LeanTween.scale(cardsContainer, Vector3.one, 0.45f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    _audioSeqCoroutine = StartCoroutine(PlayFullSequence(data));
                });
        }
        else
        {
            _audioSeqCoroutine = StartCoroutine(PlayFullSequence(data));
        }
    }

    private IEnumerator PlayFullSequence(DigraphData data)
    {
        _canTap = false;

        // --- PART 1: Left Card (Example 1) ---
        bool leftHasAudio = (data.example1SoundAudio != null || data.example1WordAudio != null);
        if (leftHasAudio)
        {
            if (data.example1SoundAudio != null && audioSource != null)
            {
                audioSource.clip = data.example1SoundAudio;
                audioSource.Play();

                if (leftCardButton != null)
                {
                    StartCoroutine(PopupAnimation(leftCardButton.transform, _origLeftCardScale, data.example1SoundAudio.length));
                }

                yield return StartCoroutine(MascotTalkAnimation(data.example1SoundAudio.length));
                yield return new WaitForSeconds(0.2f);
            }

            if (data.example1WordAudio != null && audioSource != null)
            {
                audioSource.clip = data.example1WordAudio;
                audioSource.Play();

                if (leftCardButton != null)
                {
                    StartCoroutine(PopupAnimation(leftCardButton.transform, _origLeftCardScale, data.example1WordAudio.length));
                }

                yield return StartCoroutine(MascotTalkAnimation(data.example1WordAudio.length));
                yield return new WaitForSeconds(0.4f);
            }
        }
        else
        {
            // Fallback popup for visual testing
            if (leftCardButton != null)
            {
                yield return StartCoroutine(PopupAnimation(leftCardButton.transform, _origLeftCardScale, 1.0f));
            }
            yield return new WaitForSeconds(0.4f);
        }

        // --- PART 2: Right Card (Example 2) ---
        bool rightHasAudio = (data.example2SoundAudio != null || data.example2WordAudio != null);
        if (rightHasAudio)
        {
            if (data.example2SoundAudio != null && audioSource != null)
            {
                audioSource.clip = data.example2SoundAudio;
                audioSource.Play();

                if (rightCardButton != null)
                {
                    StartCoroutine(PopupAnimation(rightCardButton.transform, _origRightCardScale, data.example2SoundAudio.length));
                }

                yield return StartCoroutine(MascotTalkAnimation(data.example2SoundAudio.length));
                yield return new WaitForSeconds(0.2f);
            }

            if (data.example2WordAudio != null && audioSource != null)
            {
                audioSource.clip = data.example2WordAudio;
                audioSource.Play();

                if (rightCardButton != null)
                {
                    StartCoroutine(PopupAnimation(rightCardButton.transform, _origRightCardScale, data.example2WordAudio.length));
                }

                yield return StartCoroutine(MascotTalkAnimation(data.example2WordAudio.length));
                yield return new WaitForSeconds(0.3f);
            }
        }
        else
        {
            // Fallback popup for visual testing
            if (rightCardButton != null)
            {
                yield return StartCoroutine(PopupAnimation(rightCardButton.transform, _origRightCardScale, 1.0f));
            }
            yield return new WaitForSeconds(0.3f);
        }

        _canTap = true;
        ShowNextCardButton();
    }

    private void ShowNextCardButton()
    {
        if (nextCardButton != null && !nextCardButton.activeSelf)
        {
            nextCardButton.SetActive(true);
            
            // Pop SFX
            if (audioSource != null && popSFX != null)
            {
                audioSource.PlayOneShot(popSFX);
            }

            nextCardButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(nextCardButton);
            LeanTween.scale(nextCardButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        }
    }

    // Button callbacks
    private void OnLeftCardTapped()
    {
        if (!_canTap) return;

        if (_audioSeqCoroutine != null) StopCoroutine(_audioSeqCoroutine);
        ResetCardVisuals();

        DigraphData data = digraphsList[_currentIndex];
        _audioSeqCoroutine = StartCoroutine(PlayLeftOnly(data));
    }

    private void OnRightCardTapped()
    {
        if (!_canTap) return;

        if (_audioSeqCoroutine != null) StopCoroutine(_audioSeqCoroutine);
        ResetCardVisuals();

        DigraphData data = digraphsList[_currentIndex];
        _audioSeqCoroutine = StartCoroutine(PlayRightOnly(data));
    }

    private IEnumerator PlayLeftOnly(DigraphData data)
    {
        _canTap = false;
        
        bool leftHasAudio = (data.example1SoundAudio != null || data.example1WordAudio != null);
        if (leftHasAudio)
        {
            if (data.example1SoundAudio != null && audioSource != null)
            {
                audioSource.clip = data.example1SoundAudio;
                audioSource.Play();
                if (leftCardButton != null)
                {
                    StartCoroutine(PopupAnimation(leftCardButton.transform, _origLeftCardScale, data.example1SoundAudio.length));
                }
                yield return StartCoroutine(MascotTalkAnimation(data.example1SoundAudio.length));
                yield return new WaitForSeconds(0.2f);
            }

            if (data.example1WordAudio != null && audioSource != null)
            {
                audioSource.clip = data.example1WordAudio;
                audioSource.Play();
                if (leftCardButton != null)
                {
                    StartCoroutine(PopupAnimation(leftCardButton.transform, _origLeftCardScale, data.example1WordAudio.length));
                }
                yield return StartCoroutine(MascotTalkAnimation(data.example1WordAudio.length));
                yield return new WaitForSeconds(0.2f);
            }
        }
        else
        {
            // Fallback popup if no audio is assigned
            if (leftCardButton != null)
            {
                yield return StartCoroutine(PopupAnimation(leftCardButton.transform, _origLeftCardScale, 1.0f));
            }
        }

        _canTap = true;
    }

    private IEnumerator PlayRightOnly(DigraphData data)
    {
        _canTap = false;

        bool rightHasAudio = (data.example2SoundAudio != null || data.example2WordAudio != null);
        if (rightHasAudio)
        {
            if (data.example2SoundAudio != null && audioSource != null)
            {
                audioSource.clip = data.example2SoundAudio;
                audioSource.Play();
                if (rightCardButton != null)
                {
                    StartCoroutine(PopupAnimation(rightCardButton.transform, _origRightCardScale, data.example2SoundAudio.length));
                }
                yield return StartCoroutine(MascotTalkAnimation(data.example2SoundAudio.length));
                yield return new WaitForSeconds(0.2f);
            }

            if (data.example2WordAudio != null && audioSource != null)
            {
                audioSource.clip = data.example2WordAudio;
                audioSource.Play();
                if (rightCardButton != null)
                {
                    StartCoroutine(PopupAnimation(rightCardButton.transform, _origRightCardScale, data.example2WordAudio.length));
                }
                yield return StartCoroutine(MascotTalkAnimation(data.example2WordAudio.length));
                yield return new WaitForSeconds(0.2f);
            }
        }
        else
        {
            // Fallback popup if no audio is assigned
            if (rightCardButton != null)
            {
                yield return StartCoroutine(PopupAnimation(rightCardButton.transform, _origRightCardScale, 1.0f));
            }
        }

        _canTap = true;
    }

    private void OnNextCardTapped()
    {
        if (transitionSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSFX);
        }

        int nextIndex = _currentIndex + 1;
        if (nextIndex < digraphsList.Count)
        {
            LoadDigraph(nextIndex);
        }
        else
        {
            StartCoroutine(DelayFinishTeachScreen());
        }
    }

    private IEnumerator DelayFinishTeachScreen()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        if (unitCompleteAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(unitCompleteAudio);
            yield return new WaitForSeconds(unitCompleteAudio.length + 0.5f);
        }

        // Hide digraph navigation buttons
        if (nextCardButton != null) nextCardButton.SetActive(false);
        if (prevButton != null) prevButton.SetActive(false);

        // Activate the global NextButton of the unit
        if (globalNextButton != null)
        {
            globalNextButton.SetActive(true);
            globalNextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(globalNextButton);
            LeanTween.scale(globalNextButton, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);
        }

        if (onTeachComplete != null)
        {
            onTeachComplete.Invoke();
        }
        else if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnPrevTapped()
    {
        if (transitionSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSFX);
        }

        int prevIndex = _currentIndex - 1;
        if (prevIndex >= 0)
        {
            LoadDigraph(prevIndex);
        }
    }

    // Animation Helpers
    private IEnumerator PopupAnimation(Transform target, Vector3 origScale, float duration)
    {
        if (target == null) yield break;

        LeanTween.cancel(target.gameObject);

        // Scale up with overshoot/spring (easeOutBack)
        LeanTween.scale(target.gameObject, origScale * wordScaleHighlight, 0.3f)
            .setEase(LeanTweenType.easeOutBack);

        // Wait for the duration of the audio
        yield return new WaitForSeconds(duration);

        // Scale down back to normal (easeOutQuad)
        LeanTween.scale(target.gameObject, origScale, 0.2f)
            .setEase(LeanTweenType.easeOutQuad);

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator MascotTalkAnimation(float duration)
    {
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.05f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt(duration / 0.5f));
        }

        yield return new WaitForSeconds(duration);

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }

    private IEnumerator PopUI(RectTransform target)
    {
        if (target == null) yield break;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            float scale = Mathf.Lerp(0f, 1.15f, 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed * 2f;
            float scale = Mathf.Lerp(1.15f, 1f, Mathf.Clamp01(t));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    private void SetupProgressDots()
    {
        if (progressDotsContainer == null) return;

        // Clear existing children except the prefab template if it's a child
        foreach (Transform child in progressDotsContainer)
        {
            if (progressDotPrefab == null || child.gameObject != progressDotPrefab)
            {
                Destroy(child.gameObject);
            }
        }
        _dotInstances.Clear();

        if (progressDotPrefab != null)
        {
            progressDotPrefab.SetActive(false);
        }

        int count = digraphsList != null ? digraphsList.Count : 0;
        for (int i = 0; i < count; i++)
        {
            GameObject dotObj;
            if (progressDotPrefab != null)
            {
                dotObj = Instantiate(progressDotPrefab, progressDotsContainer);
            }
            else
            {
                dotObj = new GameObject($"Dot_{i}", typeof(RectTransform), typeof(Image));
                dotObj.transform.SetParent(progressDotsContainer, false);
                RectTransform rt = dotObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(24f, 24f);

                Image img = dotObj.GetComponent<Image>();
                if (dotEmptySprite != null) img.sprite = dotEmptySprite;
            }
            dotObj.SetActive(true);
            _dotInstances.Add(dotObj);
        }

        UpdateProgressDots();
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < _dotInstances.Count; i++)
        {
            Image img = _dotInstances[i].GetComponent<Image>();
            if (img == null) img = _dotInstances[i].GetComponentInChildren<Image>();

            if (img != null)
            {
                bool isCompletedOrActive = i <= _currentIndex;
                if (isCompletedOrActive)
                {
                    if (dotFilledSprite != null) img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;

                    if (i == _currentIndex)
                    {
                        _dotInstances[i].transform.localScale = Vector3.one * 1.25f;
                    }
                    else
                    {
                        _dotInstances[i].transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    if (dotEmptySprite != null) img.sprite = dotEmptySprite;
                    img.color = dotEmptyColor;
                    _dotInstances[i].transform.localScale = Vector3.one;
                }
            }
        }
    }
}
