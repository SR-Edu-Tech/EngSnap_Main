using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[System.Serializable]
public class SoundGlideData
{
    [Tooltip("The phonetic sound symbol, e.g. /ai/")]
    public string soundSymbol;

    [Tooltip("The textual description of the glide, e.g. long i -> long e")]
    public string glideDescription;

    [Header("Visual Vowels")]
    [Tooltip("The first vowel letter/text to display, e.g. 'i'")]
    public string vowel1;

    [Tooltip("The second vowel letter/text to display, e.g. 'e'")]
    public string vowel2;

    [Header("Combined/Result Vowel")]
    [Tooltip("The combined diphthong spelling text, e.g. 'ie'")]
    public string combinedSpelling;

    [Header("Speech Bubble Text")]
    [Tooltip("Speech bubble text, e.g. \"i...e... eye!\"")]
    public string speechBubbleText;

    [Header("Picture Word")]
    [Tooltip("The picture word text, e.g. 'eye'")]
    public string wordText;

    [Tooltip("The picture word image/sprite")]
    public Sprite wordSprite;

    [Header("Audio Clips")]
    [Tooltip("Audio clip for the glide (e.g., 'i... e... eye!')")]
    public AudioClip glideAudio;

    [Tooltip("Audio clip for the example word (e.g., 'eye')")]
    public AudioClip wordAudio;

    [Header("Timings (Relative to Glide Audio Start)")]
    [Tooltip("Delay in seconds before highlighting vowel 1")]
    public float highlightVowel1Delay = 0.1f;

    [Tooltip("Delay in seconds before highlighting vowel 2")]
    public float highlightVowel2Delay = 0.8f;

    [Tooltip("Delay in seconds when the vowels start sliding together (Only used if requireDragToMerge is false)")]
    public float slideStartDelay = 1.5f;

    [Tooltip("Duration of the slide animation in seconds")]
    public float slideDuration = 0.5f;

    [Tooltip("Delay in seconds after the letters merge before showing and reading the picture word")]
    public float wordShowDelay = 0.5f;
}

[System.Serializable]
public class BottomGlideCard
{
    [Tooltip("Button component of the card")]
    public Button button;

    [Tooltip("Highlight border that activates when this card is selected")]
    public Image highlightBorder;

    [Tooltip("Label for sound symbol")]
    public TextMeshProUGUI soundLabel;

    [Tooltip("Label for glide description")]
    public TextMeshProUGUI descriptionLabel;

    [Tooltip("Example word image")]
    public Image wordImage;

    [Tooltip("Example word text label")]
    public TextMeshProUGUI wordTextLabel;
}

[System.Serializable]
public class PhonicsSpellingButton
{
    [Tooltip("Button component of the spelling bubble")]
    public Button button;

    [Tooltip("Highlight background image")]
    public Image highlightBg;

    [Tooltip("Text label showing the spelling (e.g. 'ie', 'y')")]
    public TextMeshProUGUI textLabel;

    [Tooltip("The glide index (0 to 7) associated with this spelling button")]
    public int associatedGlideIndex;
}

public class SoundGlide_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<SoundGlideData> questions = new List<SoundGlideData>();
    
    [Header("UI Components - Left Panel")]
    public TextMeshProUGUI soundSymbolLabel;
    public TextMeshProUGUI glideDescriptionLabel;

    [Header("UI Components - Glide Area")]
    public RectTransform vowel1Container;
    public TextMeshProUGUI vowel1Label;
    
    public RectTransform vowel2Container;
    public TextMeshProUGUI vowel2Label;

    public RectTransform glideArrow;
    public GameObject speechBubbleContainer;
    public TextMeshProUGUI speechBubbleTextLabel;
    public Button glideSpeakerButton;

    [Header("UI Components - Example Word Card")]
    public RectTransform wordCardContainer;
    public Image wordImage;
    public TextMeshProUGUI wordTextLabel;

    [Header("UI Components - Bottom Row Cards (All 8 Glides)")]
    public BottomGlideCard[] bottomCards = new BottomGlideCard[8];

    [Header("UI Components - Right Column Buttons (Cover Phonics Sounds)")]
    public PhonicsSpellingButton[] rightSpellingButtons;

    [Header("UI Components - Controls")]
    public Button nextButton;
    public TextMeshProUGUI scoreLabel;
    public RectTransform mascotCharacter;

    [Header("UI Components - Progress")]
    public TextMeshProUGUI progressLabel;
    public RectTransform progressDotsContainer;
    public GameObject progressDotPrefab;
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = Color.gray;
    public Color dotFilledColor = Color.green;

    [Header("Audio Sources")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    public AudioClip popSFX;
    public AudioClip slideSFX;
    public AudioClip mergeSFX;
    public AudioClip successSFX;
    public AudioClip levelCompleteSFX;
    public AudioClip introAudio;

    [Header("Animation Settings")]
    [Tooltip("Offset applied to Vowel 2's local position to determine the merge stop position (so Vowel 1 stops side-by-side instead of colliding). E.g. (-120, 0, 0)")]
    public Vector3 mergeOffset = new Vector3(-120f, 0f, 0f);
    [Tooltip("Highlight scale factor for individual vowels")]
    public float vowelHighlightScale = 1.25f;

    [Header("Drag Settings")]
    [Tooltip("If true, the child must drag Vowel 1 into Vowel 2 to merge. If false, it slides automatically.")]
    public bool requireDragToMerge = true;
    [Tooltip("Distance threshold in local units to trigger a merge when drag ends")]
    public float mergeDistanceThreshold = 100f;

    // Runtime state
    private int _currentIndex = 0;
    private bool _started = false;
    private bool _canTap = false;
    private bool _canDrag = false;
    private List<GameObject> _dotInstances = new List<GameObject>();
    private Coroutine _introCoroutine;
    
    private Vector3 _originalMascotScale = Vector3.one;
    private Vector3 _originalVowel1Scale = Vector3.one;
    private Vector3 _originalVowel2Scale = Vector3.one;
    private Vector3 _originalWordCardScale = Vector3.one;
    
    private Vector3 _originalVowel1LocalPos;
    private Vector3 _originalVowel2LocalPos;

    private Coroutine _sequenceCoroutine;
    private GameFlowManager_Senior_Phonics _flowManager;

    private void Awake()
    {
        // Cache original dimensions
        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }
        if (vowel1Container != null)
        {
            _originalVowel1Scale = vowel1Container.localScale;
            _originalVowel1LocalPos = vowel1Container.localPosition;
        }
        if (vowel2Container != null)
        {
            _originalVowel2Scale = vowel2Container.localScale;
            _originalVowel2LocalPos = vowel2Container.localPosition;
        }
        if (wordCardContainer != null)
        {
            _originalWordCardScale = wordCardContainer.localScale;
        }

        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (mascotAudioSource == null)
        {
            mascotAudioSource = GetComponent<AudioSource>();
            if (mascotAudioSource == null)
            {
                mascotAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    private void Start()
    {
        _started = true;

        // Register button actions
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (glideSpeakerButton != null)
        {
            glideSpeakerButton.onClick.RemoveAllListeners();
            glideSpeakerButton.onClick.AddListener(OnReplayClicked);
        }

        // Register interactive selectors
        for (int i = 0; i < bottomCards.Length; i++)
        {
            int index = i; // local copy for closure
            if (bottomCards[i] != null && bottomCards[i].button != null)
            {
                bottomCards[i].button.onClick.RemoveAllListeners();
                bottomCards[i].button.onClick.AddListener(() => OnGlideCardSelected(index));
            }
        }

        for (int i = 0; i < rightSpellingButtons.Length; i++)
        {
            int targetIndex = rightSpellingButtons[i].associatedGlideIndex;
            if (rightSpellingButtons[i] != null && rightSpellingButtons[i].button != null)
            {
                rightSpellingButtons[i].button.onClick.RemoveAllListeners();
                rightSpellingButtons[i].button.onClick.AddListener(() => OnGlideCardSelected(targetIndex));
            }
        }

        // Inject drag handler component dynamically onto vowel 1 container
        if (vowel1Container != null)
        {
            var dragHandler = vowel1Container.gameObject.GetComponent<SoundGlideDragHandler>();
            if (dragHandler == null)
            {
                dragHandler = vowel1Container.gameObject.AddComponent<SoundGlideDragHandler>();
            }
            dragHandler.Initialize(this);
        }

        PopulateStaticUI();
        ResetToStart();
    }

    private void OnEnable()
    {
        if (_started)
        {
            ResetToStart();
        }
    }

    private void OnDisable()
    {
        StopAllSequenceCoroutines();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Spacebar bypass: Simulate continuing to next glide
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (_canTap)
            {
                Debug.Log("[SoundGlide Bypass] Spacebar pressed. Loading next glide.");
                OnNextClicked();
            }
        }
    }
#endif

    public void ResetToStart()
    {
        _currentIndex = 0;
        SetupProgressDots();
        StopIntroCoroutine();
        _introCoroutine = StartCoroutine(IntroSequenceCoroutine());
    }

    private void PopulateStaticUI()
    {
        // Populate the 8 bottom cards automatically from configured questions
        for (int i = 0; i < bottomCards.Length; i++)
        {
            if (bottomCards[i] == null) continue;

            if (i < questions.Count)
            {
                var data = questions[i];
                if (bottomCards[i].soundLabel != null) bottomCards[i].soundLabel.text = data.soundSymbol;
                if (bottomCards[i].descriptionLabel != null) bottomCards[i].descriptionLabel.text = data.glideDescription;
                if (bottomCards[i].wordImage != null)
                {
                    bottomCards[i].wordImage.sprite = data.wordSprite;
                    bottomCards[i].wordImage.gameObject.SetActive(data.wordSprite != null);
                }
                if (bottomCards[i].wordTextLabel != null) bottomCards[i].wordTextLabel.text = data.wordText;

                if (bottomCards[i].button != null) bottomCards[i].button.gameObject.SetActive(true);
            }
            else
            {
                if (bottomCards[i].button != null) bottomCards[i].button.gameObject.SetActive(false);
            }
        }
    }

    private void LoadQuestion(int index)
    {
        StopIntroCoroutine();
        _currentIndex = index;

        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[SoundGlide] No questions configured!");
            return;
        }

        if (index < 0 || index >= questions.Count)
        {
            OnCompletedAll();
            return;
        }

        var data = questions[index];

        // 1. Highlight appropriate cards and spelling buttons
        UpdateHighlights();
        UpdateProgressLabel();
        UpdateProgressDots();

        // 2. Populate main labels
        if (soundSymbolLabel != null)
        {
            soundSymbolLabel.text = data.soundSymbol;
        }
        if (glideDescriptionLabel != null)
        {
            glideDescriptionLabel.text = data.glideDescription;
        }
        if (vowel1Label != null)
        {
            vowel1Label.text = data.vowel1;
        }
        if (vowel2Label != null)
        {
            vowel2Label.text = data.vowel2;
        }
        if (speechBubbleTextLabel != null)
        {
            speechBubbleTextLabel.text = data.speechBubbleText;
        }
        if (wordTextLabel != null)
        {
            wordTextLabel.text = data.wordText;
        }
        if (wordImage != null)
        {
            wordImage.sprite = data.wordSprite;
            wordImage.gameObject.SetActive(data.wordSprite != null);
        }

        // 3. Reset visual elements to start states
        if (vowel1Container != null)
        {
            LeanTween.cancel(vowel1Container.gameObject);
            vowel1Container.localScale = _originalVowel1Scale;
            vowel1Container.localPosition = _originalVowel1LocalPos;
            vowel1Container.gameObject.SetActive(true);
        }
        if (vowel2Container != null)
        {
            LeanTween.cancel(vowel2Container.gameObject);
            vowel2Container.localScale = _originalVowel2Scale;
            vowel2Container.localPosition = _originalVowel2LocalPos;
            vowel2Container.gameObject.SetActive(true);
        }
        if (speechBubbleTextLabel != null)
        {
            LeanTween.cancel(speechBubbleTextLabel.gameObject);
            speechBubbleTextLabel.transform.localScale = Vector3.zero;
            speechBubbleTextLabel.gameObject.SetActive(false);
        }
        if (wordCardContainer != null)
        {
            LeanTween.cancel(wordCardContainer.gameObject);
            wordCardContainer.localScale = Vector3.zero;
            wordCardContainer.gameObject.SetActive(false);
        }
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }

        // 4. Play pop SFX
        if (sfxAudioSource != null && popSFX != null)
        {
            sfxAudioSource.PlayOneShot(popSFX);
        }

        // 5. Pop Mascot entry and play sequence
        _canTap = false;
        _canDrag = false;
        if (mascotCharacter != null)
        {
            if (mascotCharacter.localScale.x < 0.1f)
            {
                mascotCharacter.localScale = Vector3.zero;
                LeanTween.cancel(mascotCharacter.gameObject);
                LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.45f)
                    .setEase(LeanTweenType.easeOutBack)
                    .setOnComplete(() => StartSequence(data));
            }
            else
            {
                StartSequence(data);
            }
        }
        else
        {
            StartSequence(data);
        }
    }

    private void UpdateHighlights()
    {
        // Bottom Cards highlight
        for (int i = 0; i < bottomCards.Length; i++)
        {
            if (bottomCards[i] == null) continue;
            if (bottomCards[i].highlightBorder != null)
            {
                bottomCards[i].highlightBorder.gameObject.SetActive(i == _currentIndex);
            }
        }

        // Right Spelling buttons highlight
        for (int i = 0; i < rightSpellingButtons.Length; i++)
        {
            if (rightSpellingButtons[i] == null) continue;
            if (rightSpellingButtons[i].highlightBg != null)
            {
                bool isMatching = rightSpellingButtons[i].associatedGlideIndex == _currentIndex;
                rightSpellingButtons[i].highlightBg.gameObject.SetActive(isMatching);
            }
        }
    }

    private void StartSequence(SoundGlideData data)
    {
        StopAllSequenceCoroutines();
        _sequenceCoroutine = StartCoroutine(GlideSequenceCoroutine(data));
    }

    private void StopAllSequenceCoroutines()
    {
        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
        }
    }

    private IEnumerator GlideSequenceCoroutine(SoundGlideData data)
    {
        // Phase 1: Play glide voiceover (e.g. "i... e... eye!")
        if (data.glideAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = data.glideAudio;
            mascotAudioSource.Play();
            StartCoroutine(MascotTalkAnimation(data.glideAudio.length));
        }

        // Highlight Vowel 1 Card
        yield return new WaitForSeconds(data.highlightVowel1Delay);
        if (vowel1Container != null)
        {
            LeanTween.cancel(vowel1Container.gameObject);
            LeanTween.scale(vowel1Container.gameObject, _originalVowel1Scale * vowelHighlightScale, 0.15f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => {
                    LeanTween.scale(vowel1Container.gameObject, _originalVowel1Scale, 0.15f)
                        .setEase(LeanTweenType.easeInQuad);
                });
            if (sfxAudioSource != null && popSFX != null)
            {
                sfxAudioSource.PlayOneShot(popSFX);
            }
        }

        // Highlight Vowel 2 Card
        float delayToVowel2 = data.highlightVowel2Delay - data.highlightVowel1Delay;
        if (delayToVowel2 > 0)
        {
            yield return new WaitForSeconds(delayToVowel2);
        }
        if (vowel2Container != null)
        {
            LeanTween.cancel(vowel2Container.gameObject);
            LeanTween.scale(vowel2Container.gameObject, _originalVowel2Scale * vowelHighlightScale, 0.15f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => {
                    LeanTween.scale(vowel2Container.gameObject, _originalVowel2Scale, 0.15f)
                        .setEase(LeanTweenType.easeInQuad);
                });
            if (sfxAudioSource != null && popSFX != null)
            {
                sfxAudioSource.PlayOneShot(popSFX);
            }
        }

        // If drag-to-merge is enabled, let the user interact here
        if (requireDragToMerge)
        {
            _canDrag = true;
            _canTap = true;
            yield break;
        }

        // Slide together (Auto Slide Flow)
        float delayToSlide = data.slideStartDelay - Mathf.Max(data.highlightVowel1Delay, data.highlightVowel2Delay);
        if (delayToSlide > 0)
        {
            yield return new WaitForSeconds(delayToSlide);
        }

        if (sfxAudioSource != null && slideSFX != null)
        {
            sfxAudioSource.PlayOneShot(slideSFX);
        }

        // Vowel 1 slides toward Vowel 2 (merges)
        bool slideComplete = false;
        if (vowel1Container != null)
        {
            Vector3 targetPos = _originalVowel2LocalPos + mergeOffset;
            LeanTween.moveLocal(vowel1Container.gameObject, targetPos, data.slideDuration)
                .setEase(LeanTweenType.easeInOutQuad)
                .setOnComplete(() => slideComplete = true);
        }
        else
        {
            slideComplete = true;
        }

        while (!slideComplete)
        {
            yield return null;
        }

        // Trigger Phase 2 (merge effects) & Phase 3 (word show)
        StartCoroutine(MergeAndShowWordCoroutine(data));
    }

    public void OnVowelDragBegin(PointerEventData eventData)
    {
        if (!_canDrag) return;

        // Interrupt any pending auto-slide animations if dragging
        StopAllSequenceCoroutines();
    }

    public void OnVowelDragging(PointerEventData eventData)
    {
        if (!_canDrag) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            vowel1Container.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        vowel1Container.localPosition = localPoint;
    }

    public void OnVowelDragEnd(PointerEventData eventData)
    {
        if (!_canDrag) return;

        // Check distance to Vowel 2's target merge position
        Vector3 targetPos = _originalVowel2LocalPos + mergeOffset;
        float distance = Vector3.Distance(vowel1Container.localPosition, targetPos);
        if (distance <= mergeDistanceThreshold)
        {
            _canDrag = false;
            
            // Snap Vowel 1 directly to the target merge position (with side-by-side offset)
            vowel1Container.localPosition = targetPos;
            
            // Trigger merge success sequence
            StartCoroutine(MergeAndShowWordCoroutine(questions[_currentIndex]));
        }
        else
        {
            // Snap back to layout position
            _canDrag = false;
            LeanTween.cancel(vowel1Container.gameObject);
            LeanTween.moveLocal(vowel1Container.gameObject, _originalVowel1LocalPos, 0.25f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => _canDrag = true);
        }
    }

    private IEnumerator MergeAndShowWordCoroutine(SoundGlideData data)
    {
        _canDrag = false;
        _canTap = false;

        // Phase 2: Merge complete, play merge effects & pop speech bubble
        if (sfxAudioSource != null && mergeSFX != null)
        {
            sfxAudioSource.PlayOneShot(mergeSFX);
        }

        // Pulse the vowel 2 card
        if (vowel2Container != null)
        {
            LeanTween.cancel(vowel2Container.gameObject);
            LeanTween.scale(vowel2Container.gameObject, _originalVowel2Scale * 1.3f, 0.2f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(vowel2Container.gameObject, _originalVowel2Scale, 0.15f);
                });
        }

        // Show and pop the speech bubble text label
        if (speechBubbleTextLabel != null)
        {
            speechBubbleTextLabel.gameObject.SetActive(true);
            speechBubbleTextLabel.transform.localScale = Vector3.zero;
            LeanTween.scale(speechBubbleTextLabel.gameObject, Vector3.one, 0.35f)
                .setEase(LeanTweenType.easeOutBack);
        }

        // Phase 3: Show example word card and read it
        if (data.wordShowDelay > 0)
        {
            yield return new WaitForSeconds(data.wordShowDelay);
        }

        if (wordCardContainer != null)
        {
            wordCardContainer.gameObject.SetActive(true);
            wordCardContainer.localScale = Vector3.zero;
            LeanTween.scale(wordCardContainer.gameObject, _originalWordCardScale, 0.45f)
                .setEase(LeanTweenType.easeOutBack);
            if (sfxAudioSource != null && successSFX != null)
            {
                sfxAudioSource.PlayOneShot(successSFX);
            }
        }
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(nextButton.gameObject);
            LeanTween.scale(nextButton.gameObject, Vector3.one, 0.35f)
                .setEase(LeanTweenType.easeOutBack);
        }



        // Play the word audio voiceover
        if (data.wordAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = data.wordAudio;
            mascotAudioSource.Play();
            yield return StartCoroutine(MascotTalkAnimation(data.wordAudio.length));
        }

        _canTap = true;
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

    private void OnGlideCardSelected(int index)
    {
        if (index < 0 || index >= questions.Count) return;
        LoadQuestion(index);
    }

    private void OnReplayClicked()
    {
        if (questions != null && _currentIndex < questions.Count)
        {
            // Reset positions
            if (vowel1Container != null)
            {
                vowel1Container.localPosition = _originalVowel1LocalPos;
                vowel1Container.gameObject.SetActive(true);
            }
            if (vowel2Container != null)
            {
                vowel2Container.localPosition = _originalVowel2LocalPos;
                vowel2Container.gameObject.SetActive(true);
            }
            if (speechBubbleTextLabel != null)
            {
                speechBubbleTextLabel.gameObject.SetActive(false);
            }
            if (wordCardContainer != null)
            {
                wordCardContainer.gameObject.SetActive(false);
            }
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }

            StartSequence(questions[_currentIndex]);
        }
    }

    private void OnNextClicked()
    {
        StopAllSequenceCoroutines();

        int nextIndex = _currentIndex + 1;
        if (nextIndex < questions.Count)
        {
            LoadQuestion(nextIndex);
        }
        else
        {
            OnCompletedAll();
        }
    }

    private void OnCompletedAll()
    {
        Debug.Log("[SoundGlide] Completed all glides!");
        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void UpdateProgressLabel()
    {
        if (progressLabel != null)
        {
            progressLabel.text = $"Found Words {_currentIndex + 1} / {questions.Count}";
        }
    }

    private void SetupProgressDots()
    {
        if (progressDotsContainer == null) return;

        List<GameObject> keptDots;
        GameObject activeDotTemplate = PrepareContainer(progressDotsContainer, progressDotPrefab, out keptDots);
        _dotInstances.Clear();

        if (activeDotTemplate == null)
        {
            Debug.LogError("[SoundGlide] No progress dot prefab or template found!");
            return;
        }

        for (int i = 0; i < questions.Count; i++)
        {
            GameObject dotObj = Instantiate(activeDotTemplate, progressDotsContainer);
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
                bool isCompleted = i < _currentIndex;
                if (isCompleted)
                {
                    img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;
                }
                else
                {
                    img.sprite = dotEmptySprite;
                    img.color = dotEmptyColor;
                }
            }
        }
    }

    private GameObject PrepareContainer(RectTransform container, GameObject prefab, out List<GameObject> keptObjects)
    {
        keptObjects = new List<GameObject>();
        if (container == null) return null;

        GameObject template = prefab;

        if (template != null)
        {
            foreach (Transform child in container)
            {
                if (child.gameObject != template)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                else
                {
                    keptObjects.Add(child.gameObject);
                }
            }
        }
        else
        {
            foreach (Transform child in container)
            {
                string nameLower = child.name.ToLower();
                if (nameLower != "bg" && nameLower != "background" && template == null)
                {
                    template = child.gameObject;
                    template.SetActive(false);
                    keptObjects.Add(template);
                }
                else if (nameLower == "bg" || nameLower == "background")
                {
                    keptObjects.Add(child.gameObject);
                }
                else
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            if (template == null && container.parent != null)
            {
                foreach (Transform sibling in container.parent)
                {
                    string nameLower = sibling.name.ToLower();
                    if (sibling.gameObject != container.gameObject && 
                        nameLower != "bg" && 
                        nameLower != "background" && 
                        template == null)
                    {
                        template = sibling.gameObject;
                        template.SetActive(false);
                        break;
                    }
                }
            }
        }

        return template;
    }

    private void StopIntroCoroutine()
    {
        if (_introCoroutine != null)
        {
            StopCoroutine(_introCoroutine);
            _introCoroutine = null;
        }
    }

    private IEnumerator IntroSequenceCoroutine()
    {
        _canTap = false;
        _canDrag = false;

        // Hide main visual elements during intro so it's clean
        if (vowel1Container != null) vowel1Container.gameObject.SetActive(false);
        if (vowel2Container != null) vowel2Container.gameObject.SetActive(false);
        if (speechBubbleTextLabel != null) speechBubbleTextLabel.gameObject.SetActive(false);
        if (wordCardContainer != null) wordCardContainer.gameObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        // Pop in mascot
        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = Vector3.zero;
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.45f)
                .setEase(LeanTweenType.easeOutBack);
        }

        // Play intro audio if assigned
        if (introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();
            yield return StartCoroutine(MascotTalkAnimation(introAudio.length));
            yield return new WaitForSeconds(0.2f);
        }

        // Now proceed to load the first question
        LoadQuestion(_currentIndex);
    }
}

public class SoundGlideDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private SoundGlide_P_Senior _manager;

    public void Initialize(SoundGlide_P_Senior manager)
    {
        _manager = manager;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_manager != null) _manager.OnVowelDragBegin(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_manager != null) _manager.OnVowelDragging(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_manager != null) _manager.OnVowelDragEnd(eventData);
    }
}
