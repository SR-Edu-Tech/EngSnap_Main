using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class AddEMagicPair
{
    [Tooltip("The short vowel word (e.g., 'cap')")]
    public string shortWord;

    [Tooltip("The long vowel word (e.g., 'cape')")]
    public string longWord;

    [Tooltip("The 0-based index of the vowel letter in the short word (e.g., 1 for 'cap')")]
    public int vowelIndex;

    [Tooltip("Audio clip for the short word (e.g., 'cap')")]
    public AudioClip shortWordAudio;

    [Tooltip("Audio clip for the long word (e.g., 'cape')")]
    public AudioClip longWordAudio;
}

public class AddEMagic_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("UI Hierarchy References")]
    [SerializeField] private RectTransform wordContainer;
    [SerializeField] private GameObject letterTileTemplate;
    [SerializeField] private RectTransform targetSlot;
    [SerializeField] private AddEMagic_DraggableE draggableETile;
    [SerializeField] private CanvasGroup mainCanvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject nextButton;

    [Header("Mascot & Interaction Visuals")]
    [SerializeField] private RectTransform mascotCharacter;
    [SerializeField] private TMP_Text targetSlotText;
    [SerializeField] private Image targetSlotOutline;

    [Header("Downside Comparison Visuals")]
    [SerializeField] private GameObject comparisonPanel;
    [SerializeField] private RectTransform shortCardContainer;
    [SerializeField] private RectTransform longCardContainer;
    [SerializeField] private Button shortBadgeButton;
    [SerializeField] private Button longBadgeButton;
    [SerializeField] private TMP_Text shortBadgeText;
    [SerializeField] private TMP_Text longBadgeText;

    [Header("Styling Configuration")]
    [SerializeField] private Color letterNormalColor = new Color32(40, 40, 40, 255);
    [SerializeField] private Color letterHighlightColor = new Color32(236, 64, 122, 255); // Pink glow
    [SerializeField] private float targetLetterFontSize = 90f;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource mascotAudioSource;
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private AudioClip successSFX;
    [SerializeField] private AudioClip dragWorksSFX;

    [Header("Word Pairs Configuration")]
    [SerializeField] private List<AddEMagicPair> wordPairs = new List<AddEMagicPair>();

    [Header("Editor Preview")]
    [Tooltip("Change this index to preview different word pairs in the Editor Scene view.")]
    [SerializeField] private int previewPairIndex = 0;

    [Header("Gameplay Variables")]
    [SerializeField] private float detectionRadius = 150f; // Canvas local space
    [SerializeField] private float audioPause = 0.5f;
    [SerializeField] private float transitionDelay = 1.5f;

    // Runtime state
    private int currentPairIndex = 0;
    private bool canPlay = false;
    private Coroutine gameplayCoroutine;
    private Vector3 originalMascotScale = Vector3.one;

    public bool CanPlay() => canPlay;

    private void Awake()
    {
        Debug.Log("[AddEMagic] Awake called.");
        if (mascotCharacter != null)
        {
            originalMascotScale = mascotCharacter.localScale;
        }

        // Always find and update the AudioSource dynamically at runtime to prevent stale editor reference issues
        AudioSource foundSource = GetComponent<AudioSource>();
        if (foundSource == null)
        {
            foundSource = FindFirstObjectByType<AudioSource>();
        }
        if (foundSource != null)
        {
            mascotAudioSource = foundSource;
            Debug.Log($"[AddEMagic] Awake: Found mascotAudioSource dynamically: {mascotAudioSource.gameObject.name}");
        }

        // Always dynamically check/find NextButton at runtime to ensure reference validity
        if (nextButton == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Transform nextBtnTrans = canvas.transform.Find("NextButton");
                if (nextBtnTrans != null)
                {
                    nextButton = nextBtnTrans.gameObject;
                    Debug.Log("[AddEMagic] Awake: Found nextButton dynamically.");
                }
            }
        }

        // Setup default word pairs if none are configured in Inspector
        if (wordPairs == null || wordPairs.Count == 0)
        {
            wordPairs = new List<AddEMagicPair>
            {
                new AddEMagicPair { shortWord = "cap", longWord = "cape", vowelIndex = 1 },
                new AddEMagicPair { shortWord = "hat", longWord = "hate", vowelIndex = 1 },
                new AddEMagicPair { shortWord = "hop", longWord = "hope", vowelIndex = 1 },
                new AddEMagicPair { shortWord = "cub", longWord = "cube", vowelIndex = 1 },
                new AddEMagicPair { shortWord = "tub", longWord = "tube", vowelIndex = 1 },
                new AddEMagicPair { shortWord = "pin", longWord = "pine", vowelIndex = 1 },
                new AddEMagicPair { shortWord = "kit", longWord = "kite", vowelIndex = 1 },
                new AddEMagicPair { shortWord = "not", longWord = "note", vowelIndex = 1 },
                new AddEMagicPair { shortWord = "man", longWord = "mane", vowelIndex = 1 },
                new AddEMagicPair { shortWord = "rid", longWord = "ride", vowelIndex = 1 }
            };
        }
    }

    private void Start()
    {
        Debug.Log("[AddEMagic] Start called.");
        
        // Bind programmatic Next Button listener
        if (nextButton != null)
        {
            Button btn = nextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextButtonClicked);
            }
        }

        // Bind Badge Button listeners
        if (shortBadgeButton != null)
        {
            shortBadgeButton.onClick.RemoveAllListeners();
            shortBadgeButton.onClick.AddListener(OnShortBadgeClicked);
        }
        if (longBadgeButton != null)
        {
            longBadgeButton.onClick.RemoveAllListeners();
            longBadgeButton.onClick.AddListener(OnLongBadgeClicked);
        }

        // Bind Target Slot click listener
        if (targetSlot != null)
        {
            Button slotBtn = targetSlot.GetComponent<Button>();
            if (slotBtn == null)
            {
                slotBtn = targetSlot.gameObject.AddComponent<Button>();
            }
            slotBtn.onClick.RemoveAllListeners();
            slotBtn.onClick.AddListener(OnTargetSlotClicked);
        }
    }

    private void OnEnable()
    {
        Debug.Log("[AddEMagic] OnEnable called.");
        ResolveDynamicReferences();
        ResetActivity();
        StartCoroutine(IntroSequence());
    }

    private void OnDisable()
    {
        Debug.Log("[AddEMagic] OnDisable called.");
        StopAllCoroutines();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (gameplayCoroutine != null) StopCoroutine(gameplayCoroutine);
    }

    private void ResolveDynamicReferences()
    {
        if (mascotAudioSource == null)
        {
            mascotAudioSource = GetComponent<AudioSource>();
            if (mascotAudioSource == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    mascotAudioSource = mainCam.GetComponent<AudioSource>();
                }
            }
            if (mascotAudioSource == null)
            {
                mascotAudioSource = FindFirstObjectByType<AudioSource>();
            }
            if (mascotAudioSource != null)
            {
                Debug.Log($"[AddEMagic] ResolveDynamicReferences: Re-assigned mascotAudioSource to {mascotAudioSource.gameObject.name}");
            }
        }

        if (nextButton == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Transform nextBtnTrans = canvas.transform.Find("NextButton");
                if (nextBtnTrans != null)
                {
                    nextButton = nextBtnTrans.gameObject;
                    Debug.Log("[AddEMagic] ResolveDynamicReferences: Re-assigned nextButton dynamically.");
                }
            }
        }
    }

    private void ResetTargetSlotVisuals()
    {
        if (targetSlotOutline != null)
        {
            targetSlotOutline.color = new Color32(236, 64, 122, 100); // Transparent pink border outline
            targetSlotOutline.enabled = true;

            Transform faceTrans = targetSlotOutline.transform.Find("TileFace");
            if (faceTrans != null)
            {
                Image faceImg = faceTrans.GetComponent<Image>();
                if (faceImg != null)
                {
                    faceImg.color = new Color32(251, 245, 233, 50); // Semi-transparent cream face
                }
            }
        }
    }

    private void ResetActivity()
    {
        currentPairIndex = 0;
        canPlay = false;
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;
        if (draggableETile != null) draggableETile.ResetToStart();
        if (targetSlotText != null)
        {
            targetSlotText.fontSize = targetLetterFontSize;
            targetSlotText.color = new Color(targetSlotText.color.r, targetSlotText.color.g, targetSlotText.color.b, 0f);
        }
        ResetTargetSlotVisuals();
        if (comparisonPanel != null) comparisonPanel.SetActive(false);
        if (nextButton != null) nextButton.SetActive(false);
    }

    private IEnumerator IntroSequence()
    {
        Debug.Log($"[AddEMagic] IntroSequence started. mascotAudioSource: {mascotAudioSource != null}, introAudio: {introAudio != null}");
        // Wait one frame to let the audio engine and scene settle
        yield return null;

        // Animate title and mascot pop-in
        if (titleText != null)
        {
            titleText.transform.localScale = Vector3.zero;
            LeanTween.scale(titleText.gameObject, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
        }

        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = Vector3.zero;
            LeanTween.scale(mascotCharacter.gameObject, originalMascotScale, 0.5f).setEase(LeanTweenType.easeOutBack);
        }

        // Play general activity intro clip
        if (mascotAudioSource != null && introAudio != null)
        {
            Debug.Log($"[AddEMagic] Playing Intro Audio: {introAudio.name} on source: {mascotAudioSource.gameObject.name}. Volume: {mascotAudioSource.volume}, Mute: {mascotAudioSource.mute}, Enabled: {mascotAudioSource.enabled}, GameObject Active: {mascotAudioSource.gameObject.activeInHierarchy}");
            mascotAudioSource.Stop();
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(introAudio.length + 0.2f);
        }
        else
        {
            Debug.LogWarning($"[AddEMagic] Cannot play intro audio! mascotAudioSource is null: {mascotAudioSource == null}, introAudio is null: {introAudio == null}");
            yield return new WaitForSeconds(0.5f);
        }

        LoadPair(0);
    }

    private void LoadPair(int index)
    {
        if (index < 0 || index >= wordPairs.Count)
        {
            FinishActivity();
            return;
        }

        currentPairIndex = index;
        canPlay = true;

        // Hide comparison panel during dragging
        if (comparisonPanel != null)
        {
            comparisonPanel.SetActive(false);
        }

        AddEMagicPair pair = wordPairs[index];

        // 1. Clear top container dynamic letter tiles (preserving targetSlot)
        if (wordContainer != null)
        {
            for (int i = wordContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = wordContainer.GetChild(i);
                if (child != targetSlot && child.gameObject != letterTileTemplate)
                {
                    Destroy(child.gameObject);
                }
            }

            // 2. Spawn dynamic letter tiles for short word
            if (letterTileTemplate != null)
            {
                for (int i = 0; i < pair.shortWord.Length; i++)
                {
                    GameObject newTile = Instantiate(letterTileTemplate, wordContainer);
                    newTile.SetActive(true);
                    newTile.name = "Tile_" + pair.shortWord[i];

                    TMP_Text t = newTile.GetComponentInChildren<TMP_Text>();
                    if (t != null)
                    {
                        t.text = pair.shortWord[i].ToString();
                        t.color = letterNormalColor;
                    }

                    // Programmatically register independent button click
                    char letterChar = pair.shortWord[i];
                    Button btn = newTile.GetComponent<Button>();
                    if (btn == null)
                    {
                        btn = newTile.AddComponent<Button>();
                    }
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnLetterTileClicked(newTile, letterChar));
                }
            }

            // Move TargetSlot to the very end
            if (targetSlot != null)
            {
                targetSlot.SetAsLastSibling();
            }
        }

        // Reset target slot state (empty outline, invisible 'e')
        if (targetSlotText != null)
        {
            targetSlotText.text = "e";
            targetSlotText.fontSize = targetLetterFontSize;
            Color slotColor = targetSlotOutline != null ? targetSlotOutline.color : Color.white;
            targetSlotText.color = new Color(slotColor.r, slotColor.g, slotColor.b, 0f);
        }
        ResetTargetSlotVisuals();

        // Reset and trigger pulsing on the draggable tile
        if (draggableETile != null)
        {
            draggableETile.Setup(this);
            draggableETile.ResetToStart();
            draggableETile.gameObject.SetActive(true);
            
            // Subtle pulse scaling animation to highlight interactivity
            LeanTween.cancel(draggableETile.gameObject);
            draggableETile.transform.localScale = Vector3.one;
            LeanTween.scale(draggableETile.gameObject, Vector3.one * 1.06f, 0.7f)
                .setLoopPingPong()
                .setEase(LeanTweenType.easeInOutSine);
        }
    }

    public void OnETileDragged(Vector3 worldPosition)
    {
        if (!canPlay || targetSlot == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 tileLocalPos = canvasRect.InverseTransformPoint(worldPosition);
        Vector2 slotLocalPos = canvasRect.InverseTransformPoint(targetSlot.position);
        float distance = Vector2.Distance(tileLocalPos, slotLocalPos);

        if (distance <= detectionRadius)
        {
            // Hover highlight: scale up target slot and brighten outline color
            if (targetSlotOutline != null)
            {
                targetSlotOutline.color = new Color32(236, 64, 122, 220); // Brighter pink
            }
            targetSlot.localScale = Vector3.one * 1.12f;
        }
        else
        {
            // Reset hover highlights
            if (targetSlotOutline != null)
            {
                targetSlotOutline.color = new Color32(236, 64, 122, 100); // Standard pink
            }
            targetSlot.localScale = Vector3.one;
        }
    }

    public void OnETileDropped(Vector3 worldPosition)
    {
        if (!canPlay || targetSlot == null || draggableETile == null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 tileLocalPos = canvasRect.InverseTransformPoint(worldPosition);
        Vector2 slotLocalPos = canvasRect.InverseTransformPoint(targetSlot.position);
        float distance = Vector2.Distance(tileLocalPos, slotLocalPos);

        if (distance <= detectionRadius)
        {
            // Drop Succeeded
            canPlay = false;

            // Reset hover highlights
            targetSlot.localScale = Vector3.one;

            // Calculate slot's local position in the parent space of draggableETile
            Vector2 localSlotPos = draggableETile.transform.parent.InverseTransformPoint(targetSlot.position);

            // Cancel active tween
            LeanTween.cancel(draggableETile.gameObject);

            draggableETile.AnimateToTarget(localSlotPos, () => {
                // Drag successful behavior
                draggableETile.gameObject.SetActive(false);

                // Show target slot letter 'e' and outline complete
                if (targetSlotText != null)
                {
                    targetSlotText.color = letterHighlightColor;
                    targetSlotText.transform.localScale = Vector3.zero;
                    LeanTween.scale(targetSlotText.gameObject, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutBack);
                }

                // Convert TargetSlot into a solid card visually to show completion
                if (targetSlotOutline != null)
                {
                    targetSlotOutline.color = new Color32(93, 64, 55, 255); // Solid dark brown border
                    targetSlotOutline.enabled = true;

                    Transform faceTrans = targetSlotOutline.transform.Find("TileFace");
                    if (faceTrans != null)
                    {
                        Image faceImg = faceTrans.GetComponent<Image>();
                        if (faceImg != null)
                        {
                            faceImg.color = new Color32(251, 245, 233, 255); // Solid cream background face
                        }
                    }
                }

                // Play Snap / Success Sound
                if (mascotAudioSource != null && successSFX != null)
                {
                    mascotAudioSource.PlayOneShot(successSFX);
                }

                // Start sequence animation
                gameplayCoroutine = StartCoroutine(WordSequenceFlow());
            });
        }
        else
        {
            // Reset visual highlights
            targetSlot.localScale = Vector3.one;
            if (targetSlotOutline != null)
            {
                targetSlotOutline.color = new Color32(236, 64, 122, 100);
            }

            // Reset position
            draggableETile.AnimateBackToStart();
        }
    }

    private IEnumerator WordSequenceFlow()
    {
        AddEMagicPair pair = wordPairs[currentPairIndex];

        // 1. Highlight constructed top word tiles
        if (wordContainer != null)
        {
            for (int i = 0; i < wordContainer.childCount; i++)
            {
                Transform child = wordContainer.GetChild(i);
                if (child.gameObject != letterTileTemplate)
                {
                    TMP_Text t = child.GetComponentInChildren<TMP_Text>();
                    if (t != null)
                    {
                        t.color = letterHighlightColor;
                    }
                    LeanTween.scale(child.gameObject, Vector3.one * 1.08f, 0.15f).setLoopPingPong(1);
                }
            }
        }

        yield return new WaitForSeconds(0.3f);

        // 2. Populate and Pop in downside comparison panel
        if (comparisonPanel != null)
        {
            // Clear old downside cards
            if (shortCardContainer != null)
            {
                foreach (Transform child in shortCardContainer)
                {
                    if (child.gameObject != letterTileTemplate) Destroy(child.gameObject);
                }
            }
            if (longCardContainer != null)
            {
                foreach (Transform child in longCardContainer)
                {
                    if (child.gameObject != letterTileTemplate) Destroy(child.gameObject);
                }
            }

            // Spawn short card tiles
            if (shortCardContainer != null && letterTileTemplate != null)
            {
                for (int i = 0; i < pair.shortWord.Length; i++)
                {
                    GameObject tile = Instantiate(letterTileTemplate, shortCardContainer);
                    tile.SetActive(true);
                    TMP_Text t = tile.GetComponentInChildren<TMP_Text>();
                    if (t != null)
                    {
                        t.text = pair.shortWord[i].ToString();
                        t.color = letterNormalColor;
                    }

                    // Register independent button click
                    char letterChar = pair.shortWord[i];
                    Button btn = tile.GetComponent<Button>();
                    if (btn == null)
                    {
                        btn = tile.AddComponent<Button>();
                    }
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnLetterTileClicked(tile, letterChar));
                }
            }

            // Spawn long card tiles
            if (longCardContainer != null && letterTileTemplate != null)
            {
                for (int i = 0; i < pair.longWord.Length; i++)
                {
                    GameObject tile = Instantiate(letterTileTemplate, longCardContainer);
                    tile.SetActive(true);
                    TMP_Text t = tile.GetComponentInChildren<TMP_Text>();
                    if (t != null)
                    {
                        t.text = pair.longWord[i].ToString();
                        
                        // Highlight 'e' in green color
                        if (pair.longWord[i] == 'e')
                        {
                            t.color = new Color32(76, 175, 80, 255); // Green
                            Image[] imgs = tile.GetComponentsInChildren<Image>();
                            foreach (var img in imgs)
                            {
                                if (img.gameObject.name == "TileFace")
                                {
                                    img.color = new Color32(232, 245, 233, 255); // Green tint face
                                }
                            }
                        }
                        else
                        {
                            t.color = letterNormalColor;
                        }
                    }

                    // Register independent button click
                    char letterChar = pair.longWord[i];
                    Button btn = tile.GetComponent<Button>();
                    if (btn == null)
                    {
                        btn = tile.AddComponent<Button>();
                    }
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnLetterTileClicked(tile, letterChar));
                }
            }

            // Set badge button texts
            if (shortBadgeText != null)
            {
                shortBadgeText.text = pair.shortWord;
                shortBadgeText.color = Color.white;
            }
            if (longBadgeText != null)
            {
                longBadgeText.text = pair.longWord;
                longBadgeText.color = Color.white;
            }

            comparisonPanel.SetActive(true);
            comparisonPanel.transform.localScale = Vector3.zero;

            // Spring pop-in animation
            LeanTween.scale(comparisonPanel, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Play audio sequence and pop-up badge buttons on their respective audio
        // Bounce Mascot slightly on speak
        if (mascotCharacter != null)
        {
            LeanTween.scale(mascotCharacter.gameObject, originalMascotScale * 1.08f, 0.2f).setLoopPingPong(1);
        }

        if (mascotAudioSource != null && pair.shortWordAudio != null)
        {
            // Pop Short Badge Button at bottom
            if (shortBadgeButton != null)
            {
                LeanTween.cancel(shortBadgeButton.gameObject);
                if (shortBadgeText != null) shortBadgeText.color = letterHighlightColor;
                LeanTween.scale(shortBadgeButton.gameObject, Vector3.one * 1.25f, 0.2f).setLoopPingPong(1);
            }

            mascotAudioSource.clip = pair.shortWordAudio;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(pair.shortWordAudio.length + audioPause);

            if (shortBadgeText != null) shortBadgeText.color = Color.white;
        }
        else
        {
            Debug.Log($"Mascot says short word: {pair.shortWord}");
            yield return new WaitForSeconds(1.0f);
        }

        // Bounce Mascot again on second word
        if (mascotCharacter != null)
        {
            LeanTween.scale(mascotCharacter.gameObject, originalMascotScale * 1.08f, 0.2f).setLoopPingPong(1);
        }

        if (mascotAudioSource != null && pair.longWordAudio != null)
        {
            // Pop Long Badge Button at bottom
            if (longBadgeButton != null)
            {
                LeanTween.cancel(longBadgeButton.gameObject);
                if (longBadgeText != null) longBadgeText.color = letterHighlightColor;
                LeanTween.scale(longBadgeButton.gameObject, Vector3.one * 1.25f, 0.2f).setLoopPingPong(1);
            }

            mascotAudioSource.clip = pair.longWordAudio;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(pair.longWordAudio.length + transitionDelay);

            if (longBadgeText != null) longBadgeText.color = Color.white;
        }
        else
        {
            Debug.Log($"Mascot says long word: {pair.longWord}");
            yield return new WaitForSeconds(1.2f);
        }

        // 4. Load next pair
        LoadPair(currentPairIndex + 1);
    }

    private void OnLetterTileClicked(GameObject tile, char letter)
    {
        // Tactile bounce pop
        LeanTween.cancel(tile);
        tile.transform.localScale = Vector3.one;
        LeanTween.scale(tile, Vector3.one * 1.15f, 0.12f).setLoopPingPong(1);

        // Play standard click sound
        if (mascotAudioSource != null && dragWorksSFX != null)
        {
            mascotAudioSource.PlayOneShot(dragWorksSFX);
        }

        Debug.Log($"[AddEMagic] Letter tile clicked: {letter}");
    }

    private void OnTargetSlotClicked()
    {
        if (!canPlay) return;

        // Bounce the empty slot
        LeanTween.cancel(targetSlot.gameObject);
        targetSlot.localScale = Vector3.one;
        LeanTween.scale(targetSlot.gameObject, Vector3.one * 1.1f, 0.12f).setLoopPingPong(1);

        if (mascotAudioSource != null && dragWorksSFX != null)
        {
            mascotAudioSource.PlayOneShot(dragWorksSFX);
        }

        Debug.Log("[AddEMagic] Empty slot clicked.");
    }

    private void OnShortBadgeClicked()
    {
        if (wordPairs == null || currentPairIndex >= wordPairs.Count) return;

        AddEMagicPair pair = wordPairs[currentPairIndex];
        if (mascotAudioSource != null && pair.shortWordAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = pair.shortWordAudio;
            mascotAudioSource.Play();

            if (shortBadgeButton != null)
            {
                LeanTween.cancel(shortBadgeButton.gameObject);
                if (shortBadgeText != null) shortBadgeText.color = letterHighlightColor;
                LeanTween.scale(shortBadgeButton.gameObject, Vector3.one * 1.25f, 0.15f).setLoopPingPong(1).setOnComplete(() => {
                    if (shortBadgeText != null) shortBadgeText.color = Color.white;
                });
            }
        }
    }

    private void OnLongBadgeClicked()
    {
        if (wordPairs == null || currentPairIndex >= wordPairs.Count) return;

        AddEMagicPair pair = wordPairs[currentPairIndex];
        if (mascotAudioSource != null && pair.longWordAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = pair.longWordAudio;
            mascotAudioSource.Play();

            if (longBadgeButton != null)
            {
                LeanTween.cancel(longBadgeButton.gameObject);
                if (longBadgeText != null) longBadgeText.color = letterHighlightColor;
                LeanTween.scale(longBadgeButton.gameObject, Vector3.one * 1.25f, 0.15f).setLoopPingPong(1).setOnComplete(() => {
                    if (longBadgeText != null) longBadgeText.color = Color.white;
                });
            }
        }
    }

    private void FinishActivity()
    {
        Debug.Log("Add 'e' Magic Activity Completed! Activating NextButton.");
        if (unitCompleteAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }
        if (nextButton != null)
        {
            nextButton.SetActive(true);
            nextButton.transform.localScale = Vector3.zero;
            LeanTween.scale(nextButton, Vector3.one, 0.40f).setEase(LeanTweenType.easeOutBack);
        }
    }

    private void OnNextButtonClicked()
    {
        // Proceed to next level via GameFlowManager
        GameFlowManager_Senior_Phonics flowManager = FindObjectOfType<GameFlowManager_Senior_Phonics>();
        if (flowManager != null)
        {
            flowManager.NextGameplay();
        }
        else
        {
            Debug.LogWarning("GameFlowManager_Senior_Phonics not found. Restarting level.");
            ResetActivity();
            StartCoroutine(IntroSequence());
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Only run when not in play mode
        if (!Application.isPlaying && wordPairs != null)
        {
            UnityEditor.EditorApplication.delayCall += () => 
            {
                if (this != null) 
                {
                    PreviewPairInEditor();
                }
            };
        }
    }

    private void PreviewPairInEditor()
    {
        if (wordPairs == null || previewPairIndex < 0 || previewPairIndex >= wordPairs.Count)
        {
            return;
        }

        AddEMagicPair pair = wordPairs[previewPairIndex];

        // Setup top layout preview
        if (wordContainer != null && letterTileTemplate != null)
        {
            letterTileTemplate.SetActive(false);
        }

        // Setup visual comparison preview
        if (comparisonPanel != null)
        {
            comparisonPanel.SetActive(true);
            if (shortBadgeText != null) shortBadgeText.text = pair.shortWord;
            if (longBadgeText != null) longBadgeText.text = pair.longWord;
        }

        // Reset target slot state visually in scene preview
        if (targetSlotText != null)
        {
            targetSlotText.text = "e";
            targetSlotText.fontSize = targetLetterFontSize;
            targetSlotText.color = new Color(letterHighlightColor.r, letterHighlightColor.g, letterHighlightColor.b, 0.2f);
        }
        if (targetSlotOutline != null)
        {
            targetSlotOutline.enabled = true;
        }

        // Mark dirty to save in scene serialization
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
