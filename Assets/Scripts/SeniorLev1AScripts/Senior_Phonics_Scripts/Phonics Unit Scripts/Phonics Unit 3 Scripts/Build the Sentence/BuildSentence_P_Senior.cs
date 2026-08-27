using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BuildSentenceData
{
    [Tooltip("The vowel category (e.g., Short a, Long e)")]
    public string vowelCategory;

    [Tooltip("The sentence text to build. E.g. 'Jim is six.'")]
    public string sentenceText;

    [Tooltip("The audio clip of the mascot reading the entire sentence")]
    public AudioClip sentenceAudio;

    [Tooltip("The audio clip of the mascot reading each individual word.")]
    public AudioClip[] wordAudioClips;

    [Tooltip("Start times (in seconds) for each word in the audio clip. Optional.")]
    public float[] wordStartTimes;

    [Tooltip("Durations (in seconds) for each word in the audio clip. Optional.")]
    public float[] wordDurations;
}

public class BuildSentence_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;

    [Header("Mascot Intro Audio")]
    public AudioClip introAudio;
    [Header("Gameplay Config")]
    public List<BuildSentenceData> sentences = new List<BuildSentenceData>();
    public float snapDistanceThreshold = 120f; // Screen pixels

    [Header("UI Components")]
    public TextMeshProUGUI promptLabel;
    public RectTransform slotsContainer;
    public GameObject slotPrefab;
    public RectTransform tilesContainer;
    public GameObject tilePrefab;
    public TextMeshProUGUI progressLabel;
    public RectTransform progressDotsContainer;
    public GameObject progressDotPrefab;
    public GameObject continueButton;
    public Button replaySentenceButton;
    public RectTransform mascotCharacter;

    [Header("Vowel Indicator UI")]
    public TextMeshProUGUI indicatorLetterLabel;
    public Image indicatorLetterImage;
    public Sprite[] indicatorVowelSprites;
    public TextMeshProUGUI vowelCategoryLabel;

    [Header("Audio")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip popSFX;
    public AudioClip levelCompleteSFX;

    [Header("Tile Styling")]
    public Color defaultTileColor = Color.white;
    public Color placedTileColor = Color.white;
    public Color highlightTileColor = new Color(1f, 0.85f, 0.4f); // Gold highlight during playback
    public Color wrongTileColor = new Color(0.9f, 0.3f, 0.2f); // Red flash color for incorrect placement

    [Header("Progress Dot Options")]
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = Color.gray;
    public Color dotFilledColor = Color.green;

    [Header("Vowel Indicator Colors")]
    public string vowelIndicatorRedColorHex = "#A03020";

    // Represent slots at runtime
    public class SlotInstance
    {
        public int index;
        public GameObject gameObject;
        public Vector3 worldPosition;
        public bool isFilled;
        public BuildSentenceTile_P_Senior filledTile;
    }

    // Runtime state
    private int _currentIndex = 0;
    private bool _started = false;
    private bool _canPlay = false;
    private int _placedTilesCount = 0;
    private List<SlotInstance> _slotInstances = new List<SlotInstance>();
    private List<BuildSentenceTile_P_Senior> _tileInstances = new List<BuildSentenceTile_P_Senior>();
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

        if (replaySentenceButton != null)
        {
            replaySentenceButton.onClick.RemoveAllListeners();
            replaySentenceButton.onClick.AddListener(OnReplaySentenceClicked);
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
        // Spacebar bypass: Simulate placing all word tiles in their correct positions.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[BuildSentence Bypass] Spacebar pressed. Simulating sentence completion.");
            SimulateCorrectPlacement();
        }
    }

    private void SimulateCorrectPlacement()
    {
        if (!_canPlay) _canPlay = true;

        for (int i = 0; i < _tileInstances.Count; i++)
        {
            var tile = _tileInstances[i];
            if (!tile.IsPlaced())
            {
                int correctIdx = tile.correctSlotIndex;
                SlotInstance slot = _slotInstances[correctIdx];
                PlaceTileInSlot(tile, slot, playFeedback: false);
            }
        }

        OnSentenceCompleted();
    }
#endif

    public void ResetToStart()
    {
        _currentIndex = 0;
        LoadSentence(_currentIndex);
    }

    public bool CanPlay()
    {
        return _canPlay;
    }

    private void LoadSentence(int index)
    {
        _currentIndex = index;

        if (sentences == null || sentences.Count == 0)
        {
            Debug.LogWarning("[BuildSentence] No sentences configured!");
            return;
        }

        if (index < 0 || index >= sentences.Count)
        {
            OnCompletedAll();
            return;
        }

        var data = sentences[index];

        // Enable layouts before spawning
        EnableLayoutGroups(true);

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
            vowelCategoryLabel.text = data.vowelCategory;
        }

        if (promptLabel != null)
        {
            promptLabel.text = "Listen and build the sentence. Drag the words into the correct order.";
        }

        // Reset state
        _placedTilesCount = 0;
        _canPlay = false;

        // Setup Slots and Scrambled Tiles
        SetupSlotsAndTiles(data);

        // Setup Progress UI
        SetupProgressDots();
        UpdateProgressLabel();

        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        // Play pop sound
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

            // Fallback: If no template found and the container is empty, check its parent for sibling templates
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

    private void SetupSlotsAndTiles(BuildSentenceData data)
    {
        List<GameObject> keptSlots;
        GameObject activeSlotTemplate = PrepareContainer(slotsContainer, slotPrefab, out keptSlots);
        _slotInstances.Clear();

        List<GameObject> keptTiles;
        GameObject activeTileTemplate = PrepareContainer(tilesContainer, tilePrefab, out keptTiles);
        _tileInstances.Clear();

        if (activeSlotTemplate == null)
        {
            Debug.LogError("[BuildSentence] No slot prefab or template found!");
            return;
        }
        if (activeTileTemplate == null)
        {
            Debug.LogError("[BuildSentence] No tile prefab or template found!");
            return;
        }

        string[] words = data.sentenceText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        int N = words.Length;

        // Create slots
        for (int i = 0; i < N; i++)
        {
            GameObject slotObj = Instantiate(activeSlotTemplate, slotsContainer);
            slotObj.SetActive(true);
            slotObj.transform.localScale = Vector3.one;

            // Clear any text in slots so they appear empty (useful if slot template is same as tile template)
            var slotText = slotObj.GetComponentInChildren<TextMeshProUGUI>();
            if (slotText != null)
            {
                slotText.text = "";
            }

            SlotInstance slot = new SlotInstance
            {
                index = i,
                gameObject = slotObj,
                isFilled = false,
                filledTile = null
            };
            _slotInstances.Add(slot);
        }

        // Scramble indices
        List<int> shuffledIndices = new List<int>();
        for (int i = 0; i < N; i++) shuffledIndices.Add(i);

        if (N > 1)
        {
            // Shuffle
            for (int i = 0; i < N; i++)
            {
                int temp = shuffledIndices[i];
                int rand = UnityEngine.Random.Range(i, N);
                shuffledIndices[i] = shuffledIndices[rand];
                shuffledIndices[rand] = temp;
            }

            // Verify they aren't fully sorted
            bool isSorted = true;
            for (int i = 0; i < N; i++)
            {
                if (shuffledIndices[i] != i) { isSorted = false; break; }
            }
            if (isSorted)
            {
                int temp = shuffledIndices[0];
                shuffledIndices[0] = shuffledIndices[1];
                shuffledIndices[1] = temp;
            }
        }

        // Create tiles in scrambled order
        for (int i = 0; i < N; i++)
        {
            int correctIndex = shuffledIndices[i];
            string wordVal = words[correctIndex];

            GameObject tileObj = Instantiate(activeTileTemplate, tilesContainer);
            tileObj.SetActive(true);
            tileObj.transform.localScale = Vector3.one;

            BuildSentenceTile_P_Senior tileComponent = tileObj.GetComponent<BuildSentenceTile_P_Senior>();
            if (tileComponent == null)
            {
                tileComponent = tileObj.AddComponent<BuildSentenceTile_P_Senior>();
            }

            tileComponent.Setup(this, correctIndex, wordVal);
            if (tileComponent.bgImage != null)
            {
                tileComponent.bgImage.color = defaultTileColor;
            }

            _tileInstances.Add(tileComponent);
        }

        // Start capture coroutine
        StartCoroutine(InitializePositionsNextFrame());
    }

    private IEnumerator InitializePositionsNextFrame()
    {
        // Force Immediate Layout updates
        LayoutRebuilder.ForceRebuildLayoutImmediate(slotsContainer);
        LayoutRebuilder.ForceRebuildLayoutImmediate(tilesContainer);

        yield return new WaitForEndOfFrame();

        // Capture slot positions (world position)
        for (int i = 0; i < _slotInstances.Count; i++)
        {
            _slotInstances[i].worldPosition = _slotInstances[i].gameObject.transform.position;
        }

        // Capture tile starting positions (world position)
        for (int i = 0; i < _tileInstances.Count; i++)
        {
            _tileInstances[i].startWorldPosition = _tileInstances[i].gameObject.transform.position;
        }

        // Disable layout components so elements can be dragged freely
        EnableLayoutGroups(false);

        _canPlay = true;
    }

    private void EnableLayoutGroups(bool enable)
    {
        if (slotsContainer.TryGetComponent<LayoutGroup>(out var slotsLayout))
        {
            slotsLayout.enabled = enable;
        }
        if (tilesContainer.TryGetComponent<LayoutGroup>(out var tilesLayout))
        {
            tilesLayout.enabled = enable;
        }
        if (slotsContainer.TryGetComponent<ContentSizeFitter>(out var slotsFitter))
        {
            slotsFitter.enabled = enable;
        }
        if (tilesContainer.TryGetComponent<ContentSizeFitter>(out var tilesFitter))
        {
            tilesFitter.enabled = enable;
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
            Debug.LogError("[BuildSentence] No progress dot prefab or template found!");
            return;
        }

        for (int i = 0; i < sentences.Count; i++)
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
                if (i < _currentIndex)
                {
                    img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;
                }
                else if (i == _currentIndex && _placedTilesCount == _slotInstances.Count)
                {
                    // If current completed but continue not clicked yet
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

    private void UpdateProgressLabel()
    {
        if (progressLabel != null)
        {
            progressLabel.text = $"Sentence {_currentIndex + 1} / {sentences.Count}";
        }
    }

    private void PlayMascotReading(BuildSentenceData data)
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
            // Fallback word highlighting
            _karaokeCoroutine = StartCoroutine(KaraokeFallbackFlow(data.sentenceText));
        }
    }

    private IEnumerator KaraokeSyncFlow(BuildSentenceData data)
    {
        int wordCount = _slotInstances.Count;
        float[] starts = data.wordStartTimes;
        float[] durs = data.wordDurations;

        // Fallback timings
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
                if (lastWordIndex >= 0 && lastWordIndex < _slotInstances.Count)
                {
                    HighlightPlacedTile(_slotInstances[lastWordIndex].filledTile, false);
                }
                if (activeIndex >= 0 && activeIndex < _slotInstances.Count)
                {
                    HighlightPlacedTile(_slotInstances[activeIndex].filledTile, true);
                }
                lastWordIndex = activeIndex;
            }

            yield return null;
        }

        // Reset
        if (lastWordIndex >= 0 && lastWordIndex < _slotInstances.Count)
        {
            HighlightPlacedTile(_slotInstances[lastWordIndex].filledTile, false);
        }

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }

    private IEnumerator KaraokeFallbackFlow(string sentenceText)
    {
        int wordCount = _slotInstances.Count;
        float delayPerWord = 0.4f;

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.06f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt((wordCount * delayPerWord) / 0.5f));
        }

        for (int i = 0; i < wordCount; i++)
        {
            BuildSentenceTile_P_Senior tile = GetTileInSlot(i);
            if (tile != null) HighlightPlacedTile(tile, true);
            yield return new WaitForSeconds(delayPerWord);
            if (tile != null) HighlightPlacedTile(tile, false);
        }

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }

    private BuildSentenceTile_P_Senior GetTileInSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _slotInstances.Count)
        {
            return _slotInstances[slotIndex].filledTile;
        }
        return null;
    }

    private void HighlightPlacedTile(BuildSentenceTile_P_Senior tile, bool highlight)
    {
        if (tile == null) return;

        LeanTween.cancel(tile.gameObject);
        if (highlight)
        {
            LeanTween.scale(tile.gameObject, Vector3.one * 1.12f, 0.15f).setEase(LeanTweenType.easeOutQuad);
            if (tile.bgImage != null)
            {
                tile.bgImage.color = highlightTileColor;
            }
        }
        else
        {
            LeanTween.scale(tile.gameObject, Vector3.one, 0.15f).setEase(LeanTweenType.easeOutQuad);
            if (tile.bgImage != null)
            {
                tile.bgImage.color = placedTileColor;
            }
        }
    }

    // ── Tile Drag Callbacks ───────────────────────────────────────────────────

    public void OnTileDragBegin(BuildSentenceTile_P_Senior tile)
    {
        if (mascotAudioSource.isPlaying)
        {
            mascotAudioSource.Stop();
        }
    }

    public void OnTileDropped(BuildSentenceTile_P_Senior tile)
    {
        SlotInstance closestSlot = null;
        float minDistance = float.MaxValue;

        Vector2 tileScreenPos = RectTransformUtility.WorldToScreenPoint(null, tile.transform.position);

        // Find the closest slot in the slots container
        for (int i = 0; i < _slotInstances.Count; i++)
        {
            Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(null, _slotInstances[i].worldPosition);
            float dist = Vector2.Distance(tileScreenPos, slotScreenPos);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestSlot = _slotInstances[i];
            }
        }

        bool snapped = false;
        // Check if the tile is close enough to the closest slot
        if (closestSlot != null && minDistance <= snapDistanceThreshold)
        {
            // Only snap if it is the correct slot for this word and it's not already filled
            if (closestSlot.index == tile.correctSlotIndex && !closestSlot.isFilled)
            {
                snapped = true;
                PlaceTileInSlot(tile, closestSlot, playFeedback: true);
            }
        }

        if (!snapped)
        {
            tile.AnimateBackToStartWithColor(wrongTileColor, defaultTileColor);
            if (sfxAudioSource != null && wrongSFX != null)
            {
                sfxAudioSource.PlayOneShot(wrongSFX);
            }
        }
    }

    private void PlaceTileInSlot(BuildSentenceTile_P_Senior tile, SlotInstance slot, bool playFeedback)
    {
        tile.SetPlaced(true);
        slot.isFilled = true;
        slot.filledTile = tile;

        _placedTilesCount++;

        if (tile.bgImage != null)
        {
            tile.bgImage.color = placedTileColor;
        }

        // Animate tile snap to slot position
        tile.AnimateToTargetWorld(slot.worldPosition, () => {
            // Once snapped, check completion
            if (_placedTilesCount >= _slotInstances.Count)
            {
                OnSentenceCompleted();
            }
        });

        if (playFeedback)
        {
            if (sfxAudioSource != null && correctSFX != null)
            {
                sfxAudioSource.PlayOneShot(correctSFX);
            }

            // Play single word audio clip on place
            var data = sentences[_currentIndex];
            if (data.wordAudioClips != null && tile.correctSlotIndex < data.wordAudioClips.Length && data.wordAudioClips[tile.correctSlotIndex] != null)
            {
                StartCoroutine(PlayWordAudioDelay(data.wordAudioClips[tile.correctSlotIndex], 0.15f));
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

    public void OnTileTapped(BuildSentenceTile_P_Senior tile)
    {
        if (!_canPlay) return;

        var data = sentences[_currentIndex];
        if (data.wordAudioClips != null && tile.correctSlotIndex < data.wordAudioClips.Length && data.wordAudioClips[tile.correctSlotIndex] != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = data.wordAudioClips[tile.correctSlotIndex];
            mascotAudioSource.Play();
        }
    }

    private void OnSentenceCompleted()
    {
        _canPlay = false;

        // Update progress dots
        UpdateProgressDots();

        // Play level complete / complete SFX
        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        }

        // Play readback animation
        var data = sentences[_currentIndex];
        PlayMascotReading(data);

        // Show Continue button
        if (continueButton != null)
        {
            continueButton.SetActive(true);
            continueButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(continueButton);
            LeanTween.scale(continueButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        }
    }

    // ── Button Callbacks ──────────────────────────────────────────────────────

    private void OnReplaySentenceClicked()
    {
        if (sentences == null || _currentIndex >= sentences.Count) return;
        PlayMascotReading(sentences[_currentIndex]);
    }

    private void OnContinueClicked()
    {
        int nextIndex = _currentIndex + 1;
        if (nextIndex < sentences.Count)
        {
            LoadSentence(nextIndex);
        }
        else
        {
            OnCompletedAll();
        }
    }

    private void OnCompletedAll()
    {
        Debug.Log("[BuildSentence] Completed all sentences!");
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

    // ── Vowel Helper Utilities ────────────────────────────────────────────────

    private string GetVowelLetter(string category)
    {
        if (string.IsNullOrEmpty(category)) return "A";
        string lower = category.ToLowerInvariant().Trim();
        
        string[] parts = lower.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            if (part == "a") return "A";
            if (part == "e") return "E";
            if (part == "i") return "I";
            if (part == "o") return "O";
            if (part == "u") return "U";
        }
        
        if (lower.Contains("short a") || lower.Contains("long a") || lower.EndsWith("a")) return "A";
        if (lower.Contains("short e") || lower.Contains("long e") || lower.EndsWith("e")) return "E";
        if (lower.Contains("short i") || lower.Contains("long i") || lower.EndsWith("i")) return "I";
        if (lower.Contains("short o") || lower.Contains("long o") || lower.EndsWith("o")) return "O";
        if (lower.Contains("short u") || lower.Contains("long u") || lower.EndsWith("u")) return "U";

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

    private IEnumerator IntroAndStartFlow(BuildSentenceData data)
    {
        _canPlay = false;
        if (_currentIndex == 0 && introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();
            yield return StartCoroutine(MascotTalkAnimation(introAudio.length));
        }
        PlayMascotReading(data);
        _canPlay = true;
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