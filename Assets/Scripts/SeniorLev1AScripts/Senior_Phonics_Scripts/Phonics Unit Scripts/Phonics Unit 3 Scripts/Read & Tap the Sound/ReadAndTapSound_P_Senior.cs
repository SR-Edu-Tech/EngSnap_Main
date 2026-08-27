using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TapSentenceData
{
    [Tooltip("The vowel category (e.g., Short a, Long e)")]
    public string vowelCategory;

    [Tooltip("The sentence text to read. Separate words with spaces. E.g. 'A f<color=#A03020>a</color>n h<color=#A03020>a</color>s <color=#A03020>a</color> t<color=#A03020>a</color>n.'")]
    public string sentenceText;

    [Tooltip("Indices of words that contain the target vowel sound (0-based). E.g. [1, 2, 3, 4] for 'fan has a tan'")]
    public int[] targetWordIndices;

    [Tooltip("Fallback: Word strings that contain the target vowel sound (case-insensitive, punctuation-stripped matching).")]
    public string[] targetWords;

    [Tooltip("The audio clip of the mascot reading the entire sentence")]
    public AudioClip sentenceAudio;

    [Tooltip("The audio clip of the mascot reading each individual word. Optional, plays on card tap or highlight.")]
    public AudioClip[] wordAudioClips;

    [Tooltip("The audio clip of the mascot saying each individual word slowly. Optional, plays on wrong taps.")]
    public AudioClip[] slowWordAudioClips;

    [Tooltip("Start times (in seconds) for each word in the audio clip. Optional.")]
    public float[] wordStartTimes;

    [Tooltip("Durations (in seconds) for each word in the audio clip. Optional.")]
    public float[] wordDurations;
}

public class ReadAndTapSound_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;

    [Header("Mascot Intro Audio")]
    public AudioClip introAudio;
    [Header("Gameplay Config")]
    public List<TapSentenceData> sentences = new List<TapSentenceData>();

    [Header("UI Components")]
    public TextMeshProUGUI promptLabel;
    public RectTransform wordsContainer;
    [Tooltip("The word button prefab. If null, the first child of wordsContainer will be used as a template.")]
    public GameObject wordCardPrefab;
    public TextMeshProUGUI progressLabel;
    public RectTransform progressDotsContainer;
    [Tooltip("The progress dot prefab. If null, the first child of progressDotsContainer will be used as a template.")]
    public GameObject progressDotPrefab;
    public GameObject continueButton;
    public Button hearAgainButton;
    public RectTransform mascotCharacter;

    [Header("Vowel Indicator UI")]
    public TextMeshProUGUI indicatorLetterLabel;
    public Image indicatorLetterImage;
    public Sprite[] indicatorVowelSprites;
    public TextMeshProUGUI indicatorNoteLabel;
    public TextMeshProUGUI vowelCategoryLabel;

    [Header("Audio")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;
    public AudioClip correctSFX;
    public AudioClip popSFX;
    public AudioClip wrongSFX;
    public AudioClip levelCompleteSFX;

    [Header("Card Styling")]
    public Color defaultCardColor = Color.white;
    public Color correctCardColor = new Color(0.2f, 0.8f, 0.3f); // Green / glow color
    public Color wrongCardColor = new Color(0.9f, 0.3f, 0.2f); // Temporary wrong highlight color

    [Header("Progress Dot Options")]
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = Color.gray;
    public Color dotFilledColor = Color.green;

    [Header("Vowel Indicator Colors")]
    public string vowelIndicatorRedColorHex = "#A03020";

    // Struct to hold active state of generated cards
    private struct WordCardInstance
    {
        public int index;
        public string originalWord;
        public string cleanedWord;
        public bool isTarget;
        public bool isFound;
        public GameObject cardObj;
        public Button button;
        public Image bgImage;
        public TextMeshProUGUI textLabel;
        public Vector3 originalPosition;
    }

    // Runtime state
    private int _currentIndex = 0;
    private bool _started = false;
    private bool _canTap = false;
    private int _foundTargetsCount = 0;
    private List<WordCardInstance> _cardInstances = new List<WordCardInstance>();
    private List<GameObject> _dotInstances = new List<GameObject>();
    private Vector3 _originalMascotScale = Vector3.one;
    private Coroutine _karaokeCoroutine;
    private GameFlowManager_Senior_Phonics _flowManager;

    private void Awake()
    {
        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
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

        if (continueButton != null)
        {
            var btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnContinueClicked);
            }
            continueButton.SetActive(false);
        }

        if (hearAgainButton != null)
        {
            hearAgainButton.onClick.RemoveAllListeners();
            hearAgainButton.onClick.AddListener(OnHearAgainClicked);
        }

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
        StopAllCoroutines();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Spacebar bypass: Pressing Space in Editor simulates finding all correct words.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[ReadAndTapSound Bypass] Spacebar pressed. Simulating correct taps.");
            SimulateCorrectTaps();
        }
    }

    private void SimulateCorrectTaps()
    {
        if (!_canTap) _canTap = true;
        
        // Find all target cards that aren't found yet
        for (int i = 0; i < _cardInstances.Count; i++)
        {
            var card = _cardInstances[i];
            if (card.isTarget && !card.isFound)
            {
                OnCardClicked(card);
            }
        }
    }
#endif

    public void ResetToStart()
    {
        _currentIndex = 0;
        LoadSentence(_currentIndex);
    }

    private void LoadSentence(int index)
    {
        _currentIndex = index;

        if (sentences == null || sentences.Count == 0)
        {
            Debug.LogWarning("[ReadAndTapSound] No sentences configured!");
            return;
        }

        if (index < 0 || index >= sentences.Count)
        {
            OnCompletedAll();
            return;
        }

        var data = sentences[index];

        // Update vowel indicator panel
        string vowelLetter = GetVowelLetter(data.vowelCategory);
        if (indicatorLetterLabel != null)
        {
            indicatorLetterLabel.text = vowelLetter;
        }
        if (indicatorLetterImage != null && indicatorVowelSprites != null && indicatorVowelSprites.Length > 0)
        {
            int spriteIndex = GetVowelSpriteIndex(vowelLetter);
            if (spriteIndex >= 0 && spriteIndex < indicatorVowelSprites.Length)
            {
                indicatorLetterImage.sprite = indicatorVowelSprites[spriteIndex];
            }
        }
        if (vowelCategoryLabel != null)
        {
            string formattedCategory = FormatCategoryForNote(data.vowelCategory);
            vowelCategoryLabel.text = $"Tap all the words with <color={vowelIndicatorRedColorHex}>{formattedCategory}</color> sound.";
        }
        if (indicatorNoteLabel != null)
        {
            string formattedCategory = FormatCategoryForNote(data.vowelCategory);
            indicatorNoteLabel.text = $"We are learning <color={vowelIndicatorRedColorHex}>{formattedCategory}</color> sound.";
        }

        if (promptLabel != null)
        {
            string formattedCategory = FormatCategoryForNote(data.vowelCategory);
            promptLabel.text = $"Tap every word with the <color={vowelIndicatorRedColorHex}>{formattedCategory}</color> sound.";
        }

        // Setup word cards
        SetupWordCards(data);

        // Reset state
        _foundTargetsCount = 0;
        _canTap = false;

        // Setup progress dots
        SetupProgressDots(data);

        // Update found label
        UpdateProgressLabel();

        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        // Play popup SFX
        if (sfxAudioSource != null && popSFX != null)
        {
            sfxAudioSource.PlayOneShot(popSFX);
        }

        // Mascot scale-in and read sentence
        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = Vector3.zero;
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.45f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => StartCoroutine(IntroAndStartFlow(data)));
        }
        else
        {
            StartCoroutine(IntroAndStartFlow(data));
        }
    }

    private void SetupWordCards(TapSentenceData data)
    {
        if (wordsContainer == null) return;
        List<GameObject> toDestroy = new List<GameObject>();
        GameObject template = null;

        if (wordCardPrefab != null)
        {
            foreach (Transform child in wordsContainer)
            {
                toDestroy.Add(child.gameObject);
            }
        }
        else if (wordsContainer.childCount > 0)
        {
            template = wordsContainer.GetChild(0).gameObject;
            template.SetActive(false);
            
            for (int i = 1; i < wordsContainer.childCount; i++)
            {
                toDestroy.Add(wordsContainer.GetChild(i).gameObject);
            }
        }

        foreach (var obj in toDestroy)
        {
            Destroy(obj);
        }

        _cardInstances.Clear();

        string[] words = data.sentenceText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            GameObject cardObj = null;
            if (wordCardPrefab != null)
            {
                cardObj = Instantiate(wordCardPrefab, wordsContainer);
            }
            else if (template != null)
            {
                cardObj = Instantiate(template, wordsContainer);
            }

            if (cardObj == null) continue;

            cardObj.SetActive(true);
            cardObj.transform.localScale = Vector3.one;

            // Fetch components
            TextMeshProUGUI label = cardObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = words[i];
            }

            Button btn = cardObj.GetComponent<Button>();
            if (btn == null)
            {
                btn = cardObj.GetComponentInChildren<Button>();
            }

            Image bg = cardObj.GetComponent<Image>();
            if (bg == null)
            {
                bg = cardObj.GetComponentInChildren<Image>();
            }

            // Determine if target
            string cleaned = CleanWord(words[i]);
            bool isTarget = false;
            if (data.targetWordIndices != null && data.targetWordIndices.Length > 0)
            {
                isTarget = Array.Exists(data.targetWordIndices, idx => idx == i);
            }
            else if (data.targetWords != null && data.targetWords.Length > 0)
            {
                isTarget = Array.Exists(data.targetWords, tw => string.Equals(CleanWord(tw), cleaned, StringComparison.OrdinalIgnoreCase));
            }

            WordCardInstance inst = new WordCardInstance
            {
                index = i,
                originalWord = words[i],
                cleanedWord = cleaned,
                isTarget = isTarget,
                isFound = false,
                cardObj = cardObj,
                button = btn,
                bgImage = bg,
                textLabel = label,
                originalPosition = cardObj.transform.localPosition
            };

            if (bg != null)
            {
                bg.color = defaultCardColor;
            }

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnCardClicked(inst));
            }

            _cardInstances.Add(inst);
        }

        // Force rebuild layout immediately
        LayoutRebuilder.ForceRebuildLayoutImmediate(wordsContainer);
        
        // Update original positions after layout has calculated them in the layout group
        StartCoroutine(UpdateOriginalPositionsNextFrame());
    }

    private IEnumerator UpdateOriginalPositionsNextFrame()
    {
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < _cardInstances.Count; i++)
        {
            var inst = _cardInstances[i];
            if (inst.cardObj != null)
            {
                inst.originalPosition = inst.cardObj.transform.localPosition;
                _cardInstances[i] = inst;
            }
        }
    }

    private void SetupProgressDots(TapSentenceData data)
    {
        if (progressDotsContainer == null) return;
        List<GameObject> toDestroy = new List<GameObject>();
        GameObject template = null;

        if (progressDotPrefab != null)
        {
            foreach (Transform child in progressDotsContainer)
            {
                toDestroy.Add(child.gameObject);
            }
        }
        else if (progressDotsContainer.childCount > 0)
        {
            template = progressDotsContainer.GetChild(0).gameObject;
            template.SetActive(false);
            
            for (int i = 1; i < progressDotsContainer.childCount; i++)
            {
                toDestroy.Add(progressDotsContainer.GetChild(i).gameObject);
            }
        }

        foreach (var obj in toDestroy)
        {
            Destroy(obj);
        }

        _dotInstances.Clear();

        int totalTargets = GetTotalTargets(data);
        for (int i = 0; i < totalTargets; i++)
        {
            GameObject dotObj = null;
            if (progressDotPrefab != null)
            {
                dotObj = Instantiate(progressDotPrefab, progressDotsContainer);
            }
            else if (template != null)
            {
                dotObj = Instantiate(template, progressDotsContainer);
            }

            if (dotObj == null) continue;

            dotObj.SetActive(true);
            _dotInstances.Add(dotObj);
        }

        UpdateProgressDots();
    }

    private void PlayMascotReading(TapSentenceData data)
    {
        if (_karaokeCoroutine != null) StopCoroutine(_karaokeCoroutine);

        if (data.sentenceAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = data.sentenceAudio;
            mascotAudioSource.Play();
            
            _karaokeCoroutine = StartCoroutine(KaraokeSyncFlow(data));
        }
        else
        {
            _karaokeCoroutine = StartCoroutine(KaraokeFallbackFlow(data.sentenceText));
        }
    }

    private IEnumerator KaraokeSyncFlow(TapSentenceData data)
    {
        int wordCount = _cardInstances.Count;
        float[] starts = data.wordStartTimes;
        float[] durs = data.wordDurations;

        // Fallback timings if unassigned
        if (starts == null || starts.Length < wordCount || durs == null || durs.Length < wordCount)
        {
            float totalLen = data.sentenceAudio != null ? data.sentenceAudio.length : 2.0f;
            float perWord = totalLen / wordCount;
            starts = new float[wordCount];
            durs = new float[wordCount];
            for (int i = 0; i < wordCount; i++)
            {
                starts[i] = i * perWord;
                durs[i] = perWord;
            }
        }

        // Mascot animation bounce
        if (mascotCharacter != null && data.sentenceAudio != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.06f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt(data.sentenceAudio.length / 0.5f));
        }

        int lastWordIndex = -1;

        while (mascotAudioSource != null && mascotAudioSource.isPlaying)
        {
            float time = mascotAudioSource.time;
            int activeIndex = -1;

            for (int i = 0; i < wordCount; i++)
            {
                if (time >= starts[i] && time <= (starts[i] + durs[i]))
                {
                    activeIndex = i;
                    break;
                }
            }

            if (activeIndex == -1 && lastWordIndex != -1)
            {
                activeIndex = lastWordIndex;
            }

            if (activeIndex != lastWordIndex)
            {
                if (lastWordIndex >= 0 && lastWordIndex < _cardInstances.Count)
                {
                    HighlightCard(_cardInstances[lastWordIndex], false);
                }
                if (activeIndex >= 0 && activeIndex < _cardInstances.Count)
                {
                    HighlightCard(_cardInstances[activeIndex], true);
                }
                lastWordIndex = activeIndex;
            }

            yield return null;
        }

        // Reset highlights
        if (lastWordIndex >= 0 && lastWordIndex < _cardInstances.Count)
        {
            HighlightCard(_cardInstances[lastWordIndex], false);
        }

        // Animate mascot back to normal
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }

        _canTap = true;
    }

    private IEnumerator KaraokeFallbackFlow(string sentenceText)
    {
        int wordCount = _cardInstances.Count;
        float delayPerWord = 0.4f;

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.06f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt((wordCount * delayPerWord) / 0.5f));
        }

        for (int i = 0; i < wordCount; i++)
        {
            if (i >= 0 && i < _cardInstances.Count)
            {
                HighlightCard(_cardInstances[i], true);
            }
            yield return new WaitForSeconds(delayPerWord);
            if (i >= 0 && i < _cardInstances.Count)
            {
                HighlightCard(_cardInstances[i], false);
            }
        }

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }

        _canTap = true;
    }

    private void HighlightCard(WordCardInstance card, bool highlight)
    {
        if (card.cardObj == null) return;
        
        LeanTween.cancel(card.cardObj);
        
        if (highlight)
        {
            LeanTween.scale(card.cardObj, Vector3.one * 1.12f, 0.15f).setEase(LeanTweenType.easeOutQuad);
        }
        else
        {
            float targetScale = card.isFound ? 1.05f : 1.0f;
            LeanTween.scale(card.cardObj, Vector3.one * targetScale, 0.15f).setEase(LeanTweenType.easeOutQuad);
        }
    }

    private void OnCardClicked(WordCardInstance card)
    {
        if (!_canTap) return;
        if (card.isFound) return;

        if (card.isTarget)
        {
            // Correct tap
            card.isFound = true;
            
            // Need to update the struct in the list
            for (int i = 0; i < _cardInstances.Count; i++)
            {
                if (_cardInstances[i].index == card.index)
                {
                    var updated = _cardInstances[i];
                    updated.isFound = true;
                    _cardInstances[i] = updated;
                    break;
                }
            }

            _foundTargetsCount++;

            // Visual feedback: Light up card
            if (card.bgImage != null)
            {
                card.bgImage.color = correctCardColor;
            }
            
            LeanTween.cancel(card.cardObj);
            card.cardObj.transform.localScale = Vector3.one;
            LeanTween.scale(card.cardObj, Vector3.one * 1.15f, 0.15f)
                .setLoopPingPong(1)
                .setOnComplete(() => {
                    if (card.cardObj != null)
                    {
                        card.cardObj.transform.localScale = Vector3.one * 1.05f;
                    }
                });

            // Play correct SFX
            if (sfxAudioSource != null && correctSFX != null)
            {
                sfxAudioSource.PlayOneShot(correctSFX);
            }

            // Play pronunciation audio if assigned
            var data = sentences[_currentIndex];
            if (data.wordAudioClips != null && card.index < data.wordAudioClips.Length && data.wordAudioClips[card.index] != null)
            {
                StartCoroutine(PlayWordAudioDelay(data.wordAudioClips[card.index], 0.2f));
            }

            // Update UI progress
            UpdateProgressDots();
            UpdateProgressLabel();

            // Check completion
            int totalTargets = GetTotalTargets(data);
            if (_foundTargetsCount >= totalTargets)
            {
                OnSentenceCompleted();
            }
        }
        else
        {
            // Incorrect tap
            ShakeCard(card);

            if (sfxAudioSource != null && wrongSFX != null)
            {
                sfxAudioSource.PlayOneShot(wrongSFX);
            }

            var data = sentences[_currentIndex];
            if (data.slowWordAudioClips != null && card.index < data.slowWordAudioClips.Length && data.slowWordAudioClips[card.index] != null)
            {
                PlayMascotSlowReading(data.slowWordAudioClips[card.index]);
            }
            else if (data.wordAudioClips != null && card.index < data.wordAudioClips.Length && data.wordAudioClips[card.index] != null)
            {
                PlayMascotSlowReading(data.wordAudioClips[card.index]);
            }
        }
    }

    private IEnumerator PlayWordAudioDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (mascotAudioSource != null && clip != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = clip;
            mascotAudioSource.Play();
        }
    }

    private void PlayMascotSlowReading(AudioClip slowClip)
    {
        if (slowClip == null) return;

        if (mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = slowClip;
            mascotAudioSource.Play();
        }

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.05f, 0.2f)
                .setLoopPingPong(Mathf.CeilToInt(slowClip.length / 0.4f))
                .setOnComplete(() => {
                    if (mascotCharacter != null)
                    {
                        LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.15f);
                    }
                });
        }
    }

    private void ShakeCard(WordCardInstance card)
    {
        if (card.cardObj == null) return;

        // Flash temporary wrong color
        if (card.bgImage != null)
        {
            Color origColor = card.bgImage.color;
            card.bgImage.color = wrongCardColor;
            
            LeanTween.value(card.cardObj, 0f, 1f, 0.4f)
                .setOnUpdate((float val) => {
                    if (card.bgImage != null)
                    {
                        card.bgImage.color = Color.Lerp(wrongCardColor, origColor, val);
                    }
                });
        }

        LeanTween.cancel(card.cardObj);
        card.cardObj.transform.localRotation = Quaternion.identity;

        float shakeAngle = 8f;
        LeanTween.rotateZ(card.cardObj, shakeAngle, 0.05f)
            .setLoopPingPong(3)
            .setOnComplete(() => {
                if (card.cardObj != null)
                {
                    card.cardObj.transform.localRotation = Quaternion.identity;
                }
            });
    }

    private int GetTotalTargets(TapSentenceData data)
    {
        if (data.targetWordIndices != null && data.targetWordIndices.Length > 0)
        {
            return data.targetWordIndices.Length;
        }

        if (data.targetWords == null || data.targetWords.Length == 0) return 0;

        string[] words = data.sentenceText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        int count = 0;
        foreach (var word in words)
        {
            string cleaned = CleanWord(word);
            bool isTarget = Array.Exists(data.targetWords, tw => string.Equals(CleanWord(tw), cleaned, StringComparison.OrdinalIgnoreCase));
            if (isTarget) count++;
        }
        return count;
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < _dotInstances.Count; i++)
        {
            GameObject dotObj = _dotInstances[i];
            if (dotObj == null) continue;

            bool isFilled = i < _foundTargetsCount;

            Image img = dotObj.GetComponent<Image>();
            if (img != null)
            {
                if (isFilled && dotFilledSprite != null)
                {
                    img.sprite = dotFilledSprite;
                    img.color = Color.white;
                }
                else if (!isFilled && dotEmptySprite != null)
                {
                    img.sprite = dotEmptySprite;
                    img.color = Color.white;
                }
                else
                {
                    img.color = isFilled ? dotFilledColor : dotEmptyColor;
                }
            }

            Transform filledChild = dotObj.transform.Find("Filled");
            Transform emptyChild = dotObj.transform.Find("Empty");
            if (filledChild != null) filledChild.gameObject.SetActive(isFilled);
            if (emptyChild != null) emptyChild.gameObject.SetActive(!isFilled);
        }
    }

    private void UpdateProgressLabel()
    {
        if (progressLabel != null)
        {
            var data = sentences[_currentIndex];
            int totalTargets = GetTotalTargets(data);
            progressLabel.text = $"Found <color=#A03020>{_foundTargetsCount}</color> / <color=#A03020>{totalTargets}</color> Words";
        }
    }

    private void OnSentenceCompleted()
    {
        _canTap = false;

        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        }

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.12f, 0.25f)
                .setEase(LeanTweenType.easeOutBack)
                .setLoopPingPong(2);
        }

        if (continueButton != null)
        {
            continueButton.SetActive(true);
            continueButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(continueButton);
            LeanTween.scale(continueButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        }
    }

    private void OnContinueClicked()
    {
        if (sfxAudioSource != null && popSFX != null)
        {
            sfxAudioSource.PlayOneShot(popSFX);
        }

        _currentIndex++;
        if (_currentIndex < sentences.Count)
        {
            LoadSentence(_currentIndex);
        }
        else
        {
            OnCompletedAll();
        }
    }

    private void OnHearAgainClicked()
    {
        if (sentences == null || _currentIndex < 0 || _currentIndex >= sentences.Count) return;

        var data = sentences[_currentIndex];
        
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (_karaokeCoroutine != null) StopCoroutine(_karaokeCoroutine);
        
        if (data.sentenceAudio != null)
        {
            mascotAudioSource.clip = data.sentenceAudio;
            mascotAudioSource.Play();
            _karaokeCoroutine = StartCoroutine(KaraokeSyncFlow(data));
        }
        else
        {
            _karaokeCoroutine = StartCoroutine(KaraokeFallbackFlow(data.sentenceText));
        }
    }

    private void OnCompletedAll()
    {
        Debug.Log("[ReadAndTapSound] Completed!");
        if (unitCompleteAudio != null)
        {
            if (mascotAudioSource != null)
            {
                mascotAudioSource.PlayOneShot(unitCompleteAudio);
            }
            StartCoroutine(DelayNextGameplay(unitCompleteAudio.length + 0.5f));
        }
        else
        {
            TriggerNextGameplay();
        }
    }

    private IEnumerator DelayNextGameplay(float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerNextGameplay();
    }

    private void TriggerNextGameplay()
    {
        if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private string CleanWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return "";
        string noHtml = Regex.Replace(word, @"<[^>]*>", "");
        StringBuilder sb = new StringBuilder();
        foreach (char c in noHtml)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }
        return sb.ToString().ToLowerInvariant();
    }

    private string GetVowelLetter(string category)
    {
        if (string.IsNullOrEmpty(category)) return "A";
        string lower = category.ToLowerInvariant();
        if (lower.Contains("a")) return "A";
        if (lower.Contains("e")) return "E";
        if (lower.Contains("i")) return "I";
        if (lower.Contains("o")) return "O";
        if (lower.Contains("u")) return "U";
        return "A";
    }

    private int GetVowelSpriteIndex(string vowelLetter)
    {
        switch (vowelLetter.ToUpperInvariant())
        {
            case "A": return 0;
            case "E": return 1;
            case "I": return 2;
            case "O": return 3;
            case "U": return 4;
            default: return 0;
        }
    }

    private string FormatCategoryForNote(string category)
    {
        if (string.IsNullOrEmpty(category)) return "";
        string[] parts = category.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                if (parts[i].Length == 1)
                {
                    parts[i] = parts[i].ToUpperInvariant();
                }
                else
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
                    if (i == parts.Length - 1)
                    {
                        parts[i] = parts[i].ToUpperInvariant();
                    }
                }
            }
        }
        return string.Join(" ", parts);
    }

    private IEnumerator IntroAndStartFlow(TapSentenceData data)
    {
        _canTap = false;
        if (_currentIndex == 0 && introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();
            yield return StartCoroutine(MascotTalkAnimation(introAudio.length));
        }
        PlayMascotReading(data);
    }

    private IEnumerator MascotTalkAnimation(float duration)
    {
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.06f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt(duration / 0.5f));
        }

        yield return new WaitForSeconds(duration);

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }

}