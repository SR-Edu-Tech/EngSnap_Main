using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[System.Serializable]
public class VowelData
{
    [Tooltip("Vowel character name (e.g., 'Aa')")]
    public string vowelName;

    [Tooltip("Short vowel mark character (e.g., 'ă')")]
    public string shortMark;

    [Tooltip("Long vowel mark character (e.g., 'ā')")]
    public string longMark;

    [Tooltip("List of short words (e.g., 'cat', 'apple')")]
    public string[] shortWords;

    [Tooltip("List of long words (e.g., 'acorn', 'whale')")]
    public string[] longWords;

    [Header("Audio Clips")]
    [Tooltip("Vowel introduction clip (e.g., 'Vowel Aa has two sounds')")]
    public AudioClip vowelIntroAudio;

    [Tooltip("Audio clip for the short sound")]
    public AudioClip shortSoundAudio;

    [Tooltip("Audio clips for short words in order")]
    public AudioClip[] shortWordAudios;

    [Tooltip("Audio clip for the long sound")]
    public AudioClip longSoundAudio;

    [Tooltip("Audio clips for long words in order")]
    public AudioClip[] longWordAudios;

    [Header("Sprites (Optional)")]
    [Tooltip("Sprites/images for the short words")]
    public Sprite[] shortWordSprites;

    [Tooltip("Sprites/images for the long words")]
    public Sprite[] longWordSprites;
}

public class TeachTwoSounds_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Vowels Configuration")]
    public List<VowelData> vowelsList = new List<VowelData>();

    [Header("Editor Preview")]
    [Tooltip("Change this index (0 for A, 1 for E, 2 for I, etc.) to preview different vowels in the Editor Scene view.")]
    public int previewVowelIndex = 0;

    [Header("UI Components")]
    public TextMeshProUGUI vowelTitleText;
    
    [Header("Short Vowel UI")]
    public Button shortMarkButton;
    public TextMeshProUGUI shortMarkText;
    public RectTransform shortWordsContainer;
    public TextMeshProUGUI[] shortWordTexts;
    public Image[] shortWordImages;
    [Tooltip("Optional: Parent GameObjects of each short word item (contains both text and image to animate/toggle together).")]
    public RectTransform[] shortWordItemContainers;

    [Header("Long Vowel UI")]
    public Button longMarkButton;
    public TextMeshProUGUI longMarkText;
    public RectTransform longWordsContainer;
    public TextMeshProUGUI[] longWordTexts;
    public Image[] longWordImages;
    [Tooltip("Optional: Parent GameObjects of each long word item (contains both text and image to animate/toggle together).")]
    public RectTransform[] longWordItemContainers;

    [Header("Mascot & Rule UI")]
    public RectTransform mascotCharacter;
    public RectTransform ruleCard;
    public GameObject nextButton;

    [Header("Selection Menu UI (Optional)")]
    [Tooltip("The main container for the teaching UI (usually the 'Context' GameObject).")]
    public GameObject teachingUIContext;

    [Tooltip("The menu panel where the player selects a vowel (A, E, I, O, U).")]
    public GameObject selectionPanel;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip introScreenAudio;
    public AudioClip popSFX;

    [Header("Events & Transitions")]
    public UnityEvent onTeachComplete;
    public float wordScaleHighlight = 1.15f;
    public float animationSpeed = 4f;

    // Runtime state tracking
    private int currentVowelIndex = 0;
    private bool hasTappedShort = false;
    private bool hasTappedLong = false;
    private bool isIntroPlayed = false;
    
    // Coroutines tracking
    private Coroutine introFlowCoroutine;
    private Coroutine audioSeqCoroutine;
    private Vector3[] origShortWordScales;
    private Vector3[] origLongWordScales;
    private Quaternion[] origShortWordRotations;
    private Quaternion[] origLongWordRotations;

    public Transform GetShortWordAnimationTarget(int index)
    {
        if (shortWordItemContainers != null && index < shortWordItemContainers.Length && shortWordItemContainers[index] != null)
        {
            return shortWordItemContainers[index];
        }
        if (shortWordTexts != null && index < shortWordTexts.Length && shortWordTexts[index] != null)
        {
            return shortWordTexts[index].transform;
        }
        if (shortWordImages != null && index < shortWordImages.Length && shortWordImages[index] != null)
        {
            return shortWordImages[index].transform;
        }
        return null;
    }

    public Transform GetLongWordAnimationTarget(int index)
    {
        if (longWordItemContainers != null && index < longWordItemContainers.Length && longWordItemContainers[index] != null)
        {
            return longWordItemContainers[index];
        }
        if (longWordTexts != null && index < longWordTexts.Length && longWordTexts[index] != null)
        {
            return longWordTexts[index].transform;
        }
        if (longWordImages != null && index < longWordImages.Length && longWordImages[index] != null)
        {
            return longWordImages[index].transform;
        }
        return null;
    }

    private void Awake()
    {
        // Store original scales & rotations of targets to reset them properly
        int shortCount = Mathf.Max(
            shortWordTexts != null ? shortWordTexts.Length : 0,
            shortWordItemContainers != null ? shortWordItemContainers.Length : 0
        );
        origShortWordScales = new Vector3[shortCount];
        origShortWordRotations = new Quaternion[shortCount];
        for (int i = 0; i < shortCount; i++)
        {
            Transform target = GetShortWordAnimationTarget(i);
            if (target != null)
            {
                origShortWordScales[i] = target.localScale;
                origShortWordRotations[i] = target.localRotation;
            }
            else
            {
                origShortWordScales[i] = Vector3.one;
                origShortWordRotations[i] = Quaternion.identity;
            }
        }

        int longCount = Mathf.Max(
            longWordTexts != null ? longWordTexts.Length : 0,
            longWordItemContainers != null ? longWordItemContainers.Length : 0
        );
        origLongWordScales = new Vector3[longCount];
        origLongWordRotations = new Quaternion[longCount];
        for (int i = 0; i < longCount; i++)
        {
            Transform target = GetLongWordAnimationTarget(i);
            if (target != null)
            {
                origLongWordScales[i] = target.localScale;
                origLongWordRotations[i] = target.localRotation;
            }
            else
            {
                origLongWordScales[i] = Vector3.one;
                origLongWordRotations[i] = Quaternion.identity;
            }
        }
    }

    private void OnEnable()
    {
        ResetUI();
        isIntroPlayed = false;
        if (selectionPanel != null)
        {
            OpenSelectionPanel();
        }
        else
        {
            if (teachingUIContext != null) teachingUIContext.SetActive(true);
            introFlowCoroutine = StartCoroutine(IntroFlow());
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        introFlowCoroutine = null;
        audioSeqCoroutine = null;
    }

    private void ResetUI()
    {
        // Set scales of pop-in objects to zero
        if (mascotCharacter != null) mascotCharacter.localScale = Vector3.zero;
        if (ruleCard != null) ruleCard.localScale = Vector3.zero;
        if (nextButton != null) nextButton.SetActive(false);

        // Deactivate word containers/panels initially
        if (shortWordsContainer != null) shortWordsContainer.gameObject.SetActive(false);
        if (longWordsContainer != null) longWordsContainer.gameObject.SetActive(false);

        // Clear vowel text until intro sequence finishes or starts loading
        if (vowelTitleText != null) vowelTitleText.text = "";
        if (shortMarkText != null) shortMarkText.text = "";
        if (longMarkText != null) longMarkText.text = "";

        // Reset scales of example texts/images
        ResetWordHighlights();

        currentVowelIndex = 0;
        hasTappedShort = false;
        hasTappedLong = false;
    }

    private void ResetWordHighlights()
    {
        if (origShortWordScales != null)
        {
            for (int i = 0; i < origShortWordScales.Length; i++)
            {
                Transform target = GetShortWordAnimationTarget(i);
                if (target != null && i < origShortWordScales.Length)
                {
                    target.localScale = origShortWordScales[i];
                    if (origShortWordRotations != null && i < origShortWordRotations.Length)
                        target.localRotation = origShortWordRotations[i];
                }
            }
        }
        if (origLongWordScales != null)
        {
            for (int i = 0; i < origLongWordScales.Length; i++)
            {
                Transform target = GetLongWordAnimationTarget(i);
                if (target != null && i < origLongWordScales.Length)
                {
                    target.localScale = origLongWordScales[i];
                    if (origLongWordRotations != null && i < origLongWordRotations.Length)
                        target.localRotation = origLongWordRotations[i];
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Screen Introduction Flow
    // ──────────────────────────────────────────────────────────────────────────
    private IEnumerator IntroFlow()
    {
        isIntroPlayed = true;
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

        // 3. Pop in the Rule Card ("Short = quick sound. Long = says its name.")
        if (ruleCard != null)
        {
            yield return StartCoroutine(PopUI(ruleCard));
        }

        // Wait a brief moment or until the intro clip completes
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

        // 4. Load the first vowel
        LoadVowel(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Vowel Setup / Loading
    // ──────────────────────────────────────────────────────────────────────────
    private void LoadVowel(int index)
    {
        if (vowelsList == null || index < 0 || index >= vowelsList.Count)
        {
            Debug.LogError("Vowel index out of range: " + index);
            return;
        }

        currentVowelIndex = index;
        hasTappedShort = false;
        hasTappedLong = false;

        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }

        // Stop any running animations and audio sequence
        if (audioSeqCoroutine != null)
        {
            StopCoroutine(audioSeqCoroutine);
            audioSeqCoroutine = null;
        }
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        ResetWordHighlights();

        VowelData vowel = vowelsList[index];

        // Bind data to UI Text fields
        if (vowelTitleText != null) vowelTitleText.text = vowel.vowelName;
        if (shortMarkText != null) shortMarkText.text = vowel.shortMark;
        if (longMarkText != null) longMarkText.text = vowel.longMark;

        // Populate Short Example Words
        if (shortWordsContainer != null) shortWordsContainer.gameObject.SetActive(true);
        for (int i = 0; i < shortWordTexts.Length; i++)
        {
            bool hasWord = vowel.shortWords != null && i < vowel.shortWords.Length;

            // Toggle parent container if available
            if (shortWordItemContainers != null && i < shortWordItemContainers.Length && shortWordItemContainers[i] != null)
            {
                shortWordItemContainers[i].gameObject.SetActive(hasWord);
            }

            if (shortWordTexts[i] != null)
            {
                if (hasWord)
                {
                    if (shortWordItemContainers == null || i >= shortWordItemContainers.Length || shortWordItemContainers[i] == null)
                    {
                        shortWordTexts[i].gameObject.SetActive(true);
                    }
                    shortWordTexts[i].text = vowel.shortWords[i];
                }
                else
                {
                    if (shortWordItemContainers == null || i >= shortWordItemContainers.Length || shortWordItemContainers[i] == null)
                    {
                        shortWordTexts[i].gameObject.SetActive(false);
                    }
                }
            }
        }
        for (int i = 0; i < shortWordImages.Length; i++)
        {
            if (shortWordImages[i] != null)
            {
                bool hasSprite = vowel.shortWordSprites != null && i < vowel.shortWordSprites.Length && vowel.shortWordSprites[i] != null;
                if (hasSprite)
                {
                    if (shortWordItemContainers == null || i >= shortWordItemContainers.Length || shortWordItemContainers[i] == null)
                    {
                        shortWordImages[i].gameObject.SetActive(true);
                    }
                    shortWordImages[i].sprite = vowel.shortWordSprites[i];
                }
                else
                {
                    if (shortWordItemContainers == null || i >= shortWordItemContainers.Length || shortWordItemContainers[i] == null)
                    {
                        shortWordImages[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        // Populate Long Example Words
        if (longWordsContainer != null) longWordsContainer.gameObject.SetActive(true);
        for (int i = 0; i < longWordTexts.Length; i++)
        {
            bool hasWord = vowel.longWords != null && i < vowel.longWords.Length;

            // Toggle parent container if available
            if (longWordItemContainers != null && i < longWordItemContainers.Length && longWordItemContainers[i] != null)
            {
                longWordItemContainers[i].gameObject.SetActive(hasWord);
            }

            if (longWordTexts[i] != null)
            {
                if (hasWord)
                {
                    if (longWordItemContainers == null || i >= longWordItemContainers.Length || longWordItemContainers[i] == null)
                    {
                        longWordTexts[i].gameObject.SetActive(true);
                    }
                    longWordTexts[i].text = vowel.longWords[i];
                }
                else
                {
                    if (longWordItemContainers == null || i >= longWordItemContainers.Length || longWordItemContainers[i] == null)
                    {
                        longWordTexts[i].gameObject.SetActive(false);
                    }
                }
            }
        }
        for (int i = 0; i < longWordImages.Length; i++)
        {
            if (longWordImages[i] != null)
            {
                bool hasSprite = vowel.longWordSprites != null && i < vowel.longWordSprites.Length && vowel.longWordSprites[i] != null;
                if (hasSprite)
                {
                    if (longWordItemContainers == null || i >= longWordItemContainers.Length || longWordItemContainers[i] == null)
                    {
                        longWordImages[i].gameObject.SetActive(true);
                    }
                    longWordImages[i].sprite = vowel.longWordSprites[i];
                }
                else
                {
                    if (longWordItemContainers == null || i >= longWordItemContainers.Length || longWordItemContainers[i] == null)
                    {
                        longWordImages[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        // Trigger vowel intro audio clip
        if (audioSource != null && vowel.vowelIntroAudio != null)
        {
            audioSource.clip = vowel.vowelIntroAudio;
            audioSource.Play();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Button Event Handlers
    // ──────────────────────────────────────────────────────────────────────────
    public void OnShortMarkTapped()
    {
        if (currentVowelIndex < 0 || currentVowelIndex >= vowelsList.Count) return;

        if (audioSeqCoroutine != null)
        {
            StopCoroutine(audioSeqCoroutine);
        }
        ResetWordHighlights();

        audioSeqCoroutine = StartCoroutine(PlaySoundAndWordsSequence(true));
    }

    public void OnLongMarkTapped()
    {
        if (currentVowelIndex < 0 || currentVowelIndex >= vowelsList.Count) return;

        if (audioSeqCoroutine != null)
        {
            StopCoroutine(audioSeqCoroutine);
        }
        ResetWordHighlights();

        audioSeqCoroutine = StartCoroutine(PlaySoundAndWordsSequence(false));
    }

    public void OnNextTapped()
    {
        int nextIndex = currentVowelIndex + 1;
        if (nextIndex < vowelsList.Count)
        {
            // Transition to the next vowel
            LoadVowel(nextIndex);
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
        if (onTeachComplete != null)
        {
            onTeachComplete.Invoke();
        }
        else
        {
            GameFlowManager_Senior_Phonics flowManager = FindObjectOfType<GameFlowManager_Senior_Phonics>();
            if (flowManager != null)
            {
                flowManager.NextGameplay();
            }
            else
            {
                Debug.LogWarning("No completion action hooked up and GameFlowManager_Senior_Phonics not found in scene.");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Audio Sequencing Coroutine
    // ──────────────────────────────────────────────────────────────────────────
    private IEnumerator PlaySoundAndWordsSequence(bool isShort)
    {
        VowelData vowel = vowelsList[currentVowelIndex];
        AudioClip soundClip = isShort ? vowel.shortSoundAudio : vowel.longSoundAudio;
        AudioClip[] wordClips = isShort ? vowel.shortWordAudios : vowel.longWordAudios;
        TextMeshProUGUI[] wordTexts = isShort ? shortWordTexts : longWordTexts;
        Image[] wordImages = isShort ? shortWordImages : longWordImages;
        Vector3[] origScales = isShort ? origShortWordScales : origLongWordScales;
        Quaternion[] origRots = isShort ? origShortWordRotations : origLongWordRotations;

        // 1. Play the short/long vowel sound
        if (audioSource != null && soundClip != null)
        {
            audioSource.clip = soundClip;
            audioSource.Play();
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.2f);
        }

        // 2. Play each word sound and animate its wiggle & pulse highlight
        if (wordClips != null)
        {
            for (int i = 0; i < wordClips.Length; i++)
            {
                if (wordClips[i] == null) continue;

                // Determine transform target to animate
                Transform targetTransform = isShort ? GetShortWordAnimationTarget(i) : GetLongWordAnimationTarget(i);

                Vector3 originalScale = Vector3.one;
                if (origScales != null && i < origScales.Length)
                {
                    originalScale = origScales[i];
                }

                Quaternion originalRotation = Quaternion.identity;
                if (origRots != null && i < origRots.Length)
                {
                    originalRotation = origRots[i];
                }

                // Determine duration of wiggle
                float duration = 1.0f; // fallback
                if (wordClips[i] != null)
                {
                    duration = wordClips[i].length;
                }

                // Play word audio
                if (audioSource != null && wordClips[i] != null)
                {
                    audioSource.clip = wordClips[i];
                    audioSource.Play();
                }

                // Wiggle animation inline
                if (targetTransform != null)
                {
                    float elapsed = 0f;
                    float wiggleSpeed = 8f;   // Slower, smoother wiggle speed
                    float wiggleAngle = 5f;   // Gentler tilt angle

                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        
                        // Z-axis tilt wiggle
                        float angle = Mathf.Sin(elapsed * wiggleSpeed) * wiggleAngle;
                        targetTransform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, angle);
                        
                        // Smoothly scale up over the first 0.15s, then hold the highlighted scale constant
                        float scaleProgress = Mathf.Min(elapsed / 0.15f, 1f);
                        float currentTargetScale = Mathf.Lerp(1.0f, wordScaleHighlight, scaleProgress);
                        targetTransform.localScale = originalScale * currentTargetScale;
                        
                        yield return null;
                    }

                    // Smooth return to normal scale and rotation
                    float t = 0f;
                    Vector3 currentScale = targetTransform.localScale;
                    Quaternion currentRotation = targetTransform.localRotation;
                    while (t < 1f)
                    {
                        t += Time.deltaTime * animationSpeed;
                        targetTransform.localScale = Vector3.Lerp(currentScale, originalScale, t);
                        targetTransform.localRotation = Quaternion.Lerp(currentRotation, originalRotation, t);
                        yield return null;
                    }

                    targetTransform.localScale = originalScale;
                    targetTransform.localRotation = originalRotation;
                }
                else
                {
                    yield return new WaitForSeconds(duration);
                }

                yield return new WaitForSeconds(0.15f);
            }
        }

        // Mark tap complete
        if (isShort)
            hasTappedShort = true;
        else
            hasTappedLong = true;

        // Check if both short and long vowel marks have been explored
        if (hasTappedShort && hasTappedLong && nextButton != null && !nextButton.activeSelf)
        {
            nextButton.SetActive(true);
            
            // Play popup SFX
            if (audioSource != null && popSFX != null)
            {
                audioSource.PlayOneShot(popSFX);
            }

            // Animate next button entry
            yield return StartCoroutine(PopUI(nextButton.GetComponent<RectTransform>()));
        }

        audioSeqCoroutine = null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Animation Helpers
    // ──────────────────────────────────────────────────────────────────────────
    private IEnumerator PopUI(RectTransform target)
    {
        if (target == null) yield break;

        float t = 0f;
        // Phase 1: Scale from 0 to 1.15
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            float scale = Mathf.Lerp(0f, 1.15f, 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        // Phase 2: Settle back to 1.0
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

    // ──────────────────────────────────────────────────────────────────────────
    // Selection Menu UI Methods
    // ──────────────────────────────────────────────────────────────────────────
    public void OpenSelectionPanel()
    {
        if (selectionPanel != null) selectionPanel.SetActive(true);
        if (teachingUIContext != null) teachingUIContext.SetActive(false);
        if (nextButton != null) nextButton.SetActive(false);
    }

    public void OnSelectVowel(int index)
    {
        if (vowelsList == null || index < 0 || index >= vowelsList.Count) return;

        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (teachingUIContext != null) teachingUIContext.SetActive(true);

        // Stop any running intro flow or gameplay coroutines
        if (introFlowCoroutine != null)
        {
            StopCoroutine(introFlowCoroutine);
            introFlowCoroutine = null;
        }
        if (audioSeqCoroutine != null)
        {
            StopCoroutine(audioSeqCoroutine);
            audioSeqCoroutine = null;
        }
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        ResetWordHighlights();

        if (!isIntroPlayed)
        {
            introFlowCoroutine = StartCoroutine(PlaySelectedVowelIntro(index));
        }
        else
        {
            // Just update the vowel in both panels immediately
            if (mascotCharacter != null) mascotCharacter.localScale = Vector3.one;
            if (ruleCard != null) ruleCard.localScale = Vector3.one;
            LoadVowel(index);
        }
    }

    private IEnumerator PlaySelectedVowelIntro(int index)
    {
        isIntroPlayed = true;

        // Set mascot and rule card scale to zero so they pop in
        if (mascotCharacter != null) mascotCharacter.localScale = Vector3.zero;
        if (ruleCard != null) ruleCard.localScale = Vector3.zero;
        if (nextButton != null) nextButton.SetActive(false);

        // Load the selected vowel immediately so both panels are updated and visible right away
        LoadVowel(index);

        // 1. Play general screen intro audio if configured
        if (audioSource != null && introScreenAudio != null)
        {
            audioSource.clip = introScreenAudio;
            audioSource.Play();
            while (audioSource.isPlaying)
            {
                yield return null;
            }

            // Now trigger the vowel intro audio since general intro has finished
            VowelData vowel = vowelsList[index];
            if (vowel.vowelIntroAudio != null)
            {
                audioSource.clip = vowel.vowelIntroAudio;
                audioSource.Play();
            }
        }

        // 2. Pop in the Mascot character
        if (mascotCharacter != null)
        {
            yield return StartCoroutine(PopUI(mascotCharacter));
        }

        // 3. Pop in the Rule Card
        if (ruleCard != null)
        {
            yield return StartCoroutine(PopUI(ruleCard));
        }

        introFlowCoroutine = null;
    }

    public void OnBackToSelectionTapped()
    {
        if (introFlowCoroutine != null)
        {
            StopCoroutine(introFlowCoroutine);
            introFlowCoroutine = null;
        }
        if (audioSeqCoroutine != null)
        {
            StopCoroutine(audioSeqCoroutine);
            audioSeqCoroutine = null;
        }
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        OpenSelectionPanel();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Only run when not in play mode, so it doesn't conflict with runtime LoadVowel() calls
        if (!Application.isPlaying && vowelsList != null)
        {
            UnityEditor.EditorApplication.delayCall += () => 
            {
                if (this != null) 
                {
                    PreviewVowelInEditor();
                }
            };
        }
    }

    [ContextMenu("Preview Selected Vowel")]
    public void PreviewVowelInEditor()
    {
        if (vowelsList == null || previewVowelIndex < 0 || previewVowelIndex >= vowelsList.Count)
        {
            return;
        }

        VowelData vowel = vowelsList[previewVowelIndex];

        // Update title and marks
        if (vowelTitleText != null) vowelTitleText.text = vowel.vowelName;
        if (shortMarkText != null) shortMarkText.text = vowel.shortMark;
        if (longMarkText != null) longMarkText.text = vowel.longMark;

        // Update short vowel words and sprites
        if (shortWordTexts != null)
        {
            for (int i = 0; i < shortWordTexts.Length; i++)
            {
                bool hasWord = vowel.shortWords != null && i < vowel.shortWords.Length;

                if (shortWordItemContainers != null && i < shortWordItemContainers.Length && shortWordItemContainers[i] != null)
                {
                    shortWordItemContainers[i].gameObject.SetActive(hasWord);
                }

                if (shortWordTexts[i] != null)
                {
                    if (hasWord)
                    {
                        if (shortWordItemContainers == null || i >= shortWordItemContainers.Length || shortWordItemContainers[i] == null)
                        {
                            shortWordTexts[i].gameObject.SetActive(true);
                        }
                        shortWordTexts[i].text = vowel.shortWords[i];
                    }
                    else
                    {
                        if (shortWordItemContainers == null || i >= shortWordItemContainers.Length || shortWordItemContainers[i] == null)
                        {
                            shortWordTexts[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        if (shortWordImages != null)
        {
            for (int i = 0; i < shortWordImages.Length; i++)
            {
                if (shortWordImages[i] != null)
                {
                    bool hasSprite = vowel.shortWordSprites != null && i < vowel.shortWordSprites.Length && vowel.shortWordSprites[i] != null;
                    if (hasSprite)
                    {
                        if (shortWordItemContainers == null || i >= shortWordItemContainers.Length || shortWordItemContainers[i] == null)
                        {
                            shortWordImages[i].gameObject.SetActive(true);
                        }
                        shortWordImages[i].sprite = vowel.shortWordSprites[i];
                    }
                    else
                    {
                        if (shortWordItemContainers == null || i >= shortWordItemContainers.Length || shortWordItemContainers[i] == null)
                        {
                            shortWordImages[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        // Update long vowel words and sprites
        if (longWordTexts != null)
        {
            for (int i = 0; i < longWordTexts.Length; i++)
            {
                bool hasWord = vowel.longWords != null && i < vowel.longWords.Length;

                if (longWordItemContainers != null && i < longWordItemContainers.Length && longWordItemContainers[i] != null)
                {
                    longWordItemContainers[i].gameObject.SetActive(hasWord);
                }

                if (longWordTexts[i] != null)
                {
                    if (hasWord)
                    {
                        if (longWordItemContainers == null || i >= longWordItemContainers.Length || longWordItemContainers[i] == null)
                        {
                            longWordTexts[i].gameObject.SetActive(true);
                        }
                        longWordTexts[i].text = vowel.longWords[i];
                    }
                    else
                    {
                        if (longWordItemContainers == null || i >= longWordItemContainers.Length || longWordItemContainers[i] == null)
                        {
                            longWordTexts[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        if (longWordImages != null)
        {
            for (int i = 0; i < longWordImages.Length; i++)
            {
                if (longWordImages[i] != null)
                {
                    bool hasSprite = vowel.longWordSprites != null && i < vowel.longWordSprites.Length && vowel.longWordSprites[i] != null;
                    if (hasSprite)
                    {
                        if (longWordItemContainers == null || i >= longWordItemContainers.Length || longWordItemContainers[i] == null)
                        {
                            longWordImages[i].gameObject.SetActive(true);
                        }
                        longWordImages[i].sprite = vowel.longWordSprites[i];
                    }
                    else
                    {
                        if (longWordItemContainers == null || i >= longWordItemContainers.Length || longWordItemContainers[i] == null)
                        {
                            longWordImages[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        // Set dirty so changes are saved in scene
        UnityEditor.EditorUtility.SetDirty(this);
        if (vowelTitleText != null) UnityEditor.EditorUtility.SetDirty(vowelTitleText);
        if (shortMarkText != null) UnityEditor.EditorUtility.SetDirty(shortMarkText);
        if (longMarkText != null) UnityEditor.EditorUtility.SetDirty(longMarkText);
        
        if (shortWordTexts != null)
        {
            foreach (var txt in shortWordTexts) if (txt != null) UnityEditor.EditorUtility.SetDirty(txt);
        }
        if (longWordTexts != null)
        {
            foreach (var txt in longWordTexts) if (txt != null) UnityEditor.EditorUtility.SetDirty(txt);
        }
        if (shortWordImages != null)
        {
            foreach (var img in shortWordImages) if (img != null) UnityEditor.EditorUtility.SetDirty(img);
        }
        if (longWordImages != null)
        {
            foreach (var img in longWordImages) if (img != null) UnityEditor.EditorUtility.SetDirty(img);
        }
        if (shortWordItemContainers != null)
        {
            foreach (var container in shortWordItemContainers) if (container != null) UnityEditor.EditorUtility.SetDirty(container);
        }
        if (longWordItemContainers != null)
        {
            foreach (var container in longWordItemContainers) if (container != null) UnityEditor.EditorUtility.SetDirty(container);
        }
    }
#endif
}
