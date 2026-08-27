using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class MeetTheLettersManager_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [System.Serializable]
    public struct CollectorGridSettings
    {
        [Tooltip("Overall position shift inside the collector box (X, Y)")]
        public Vector2 centerOffset;

        [Tooltip("Explicit size of collected tiles (Width, Height) [Obsolete: Use collectedTileSize instead]")]
        public Vector2 tileSize; // Default: (150, 100)

        [Tooltip("Horizontal spacing between collected tiles")]
        public float spacingX;

        [Tooltip("Vertical spacing between collected tiles")]
        public float spacingY;

        [Tooltip("Number of columns before moving to the next row")]
        public int columns;

        [Tooltip("Target scale for the collected tiles inside the box [Obsolete: Use collectedTileScale instead]")]
        public float tileScale;
    }

    [Header("UI References")]
    public TMP_Text vowelCounterText;          // Assign 'Vowels Counter' from Hierarchy
    public RectTransform vowelCollectorBox;   // Assign 'Vowel_Collector' from Hierarchy
    public GameObject nextButton;             // Assign 'NextButton' from Hierarchy

    [Header("Audio Source")]
    public AudioSource mascotAudioSource;
    public AudioClip introClip;

    [Header("Game Settings")]
    public int totalVowelsNeeded = 5;

    [Header("Collected Vowels Visuals")]
    [Tooltip("Target size (Width, Height) for the vowel tiles when in the collector")]
    public Vector2 collectedTileSize = new Vector2(150f, 100f);

    [Tooltip("Target scale for the vowel tiles when in the collector")]
    public float collectedTileScale = 0.6f;

    [Tooltip("Position offset inside the collector/holder box (X, Y)")]
    public Vector2 collectedTileOffset = new Vector2(0f, 6f);

    [Tooltip("Color of vowel tiles when in the collector (Pink)")]
    public Color vowelColor = new Color(1f, 0.4f, 0.7f); // Pink

    [Tooltip("Color of consonant tiles when in the collector (Blue)")]
    public Color consonantColor = new Color(0.3f, 0.6f, 1f); // Blue

    [Tooltip("Optional custom sprite for vowel tiles when collected")]
    public Sprite collectedVowelSprite;

    [Tooltip("Optional custom sprite for consonant tiles when collected")]
    public Sprite collectedConsonantSprite;

    [Header("Tween & Collector Setup")]
    public GameObject vowelCollectedPrefab;   // Optional prefab
    public float flyDuration = 0.6f;

    [Header("Collector Grid Layout (Customize in Inspector)")]
    public CollectorGridSettings gridSettings = new CollectorGridSettings
    {
        centerOffset = Vector2.zero,
        tileSize = new Vector2(150f, 100f), // <-- Width = 150, Height = 100
        spacingX = 75f,
        spacingY = 75f,
        columns = 3,
        tileScale = 0.6f
    };

    [Header("Events")]
    public UnityEvent OnAllVowelsCollected;

    private HashSet<char> collectedVowels = new HashSet<char>();
    private Coroutine audioCoroutine;
    private GameObject currentActiveTile = null;
    private Vector3 collectorOriginalScale = Vector3.one;

    private void Start()
    {
        // Play Intro Audio Clip
        if (introClip != null)
        {
            if (mascotAudioSource == null)
            {
                mascotAudioSource = GetComponent<AudioSource>();
                if (mascotAudioSource == null)
                {
                    mascotAudioSource = FindObjectOfType<AudioSource>();
                }
            }

            if (mascotAudioSource != null)
            {
                mascotAudioSource.clip = introClip;
                mascotAudioSource.Play();
            }
        }

        // Disable LayoutGroup on the collector box to allow manual tile sizing and positioning
        if (vowelCollectorBox != null)
        {
            collectorOriginalScale = vowelCollectorBox.localScale;
            LayoutGroup layoutGroup = vowelCollectorBox.GetComponent<LayoutGroup>();
            if (layoutGroup != null)
            {
                layoutGroup.enabled = false;
            }
        }

        // Auto-assign setup to all letter tiles in this screen
        LetterTile_P_Senior[] tiles = FindObjectsOfType<LetterTile_P_Senior>();
        foreach (var tile in tiles)
        {
            tile.Setup(this);
        }

        UpdateVowelCounterText();

        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }
    }

    public void PlayLetterAudio(AudioClip nameClip, AudioClip soundClip)
    {
        if (mascotAudioSource == null)
        {
            mascotAudioSource = GetComponent<AudioSource>();
            if (mascotAudioSource == null)
            {
                mascotAudioSource = FindObjectOfType<AudioSource>();
            }
        }

        if (mascotAudioSource == null)
        {
            Debug.LogError("[MeetTheLettersManager_P_Senior] mascotAudioSource is null! Cannot play letter audio. Please assign an AudioSource in the Inspector.");
            return;
        }

        if (nameClip == null && soundClip == null)
        {
            Debug.LogWarning("[MeetTheLettersManager_P_Senior] PlayLetterAudio called, but both nameClip and soundClip are null.");
            return;
        }

        if (audioCoroutine != null)
        {
            StopCoroutine(audioCoroutine);
        }

        audioCoroutine = StartCoroutine(SequenceLetterAudio(nameClip, soundClip));
    }

    private IEnumerator SequenceLetterAudio(AudioClip nameClip, AudioClip soundClip)
    {
        if (nameClip != null)
        {
            mascotAudioSource.clip = nameClip;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(nameClip.length + 0.1f);
        }

        if (soundClip != null)
        {
            mascotAudioSource.clip = soundClip;
            mascotAudioSource.Play();
        }
    }

    public void OnLetterClicked(char letter, RectTransform sourceTile)
    {
        char upper = char.ToUpper(letter);
        bool isVowel = (upper == 'A' || upper == 'E' || upper == 'I' || upper == 'O' || upper == 'U');

        // Destroy the current active tile in the holder if it exists
        if (currentActiveTile != null)
        {
            Destroy(currentActiveTile);
            currentActiveTile = null;
        }

        // Animate the new letter to the collector
        if (vowelCollectorBox != null && sourceTile != null)
        {
            AnimateTileToCollector(sourceTile, letter, isVowel);
        }

        // Progression logic: count unique vowels
        if (isVowel)
        {
            if (!collectedVowels.Contains(upper))
            {
                collectedVowels.Add(upper);
                UpdateVowelCounterText();

                if (collectedVowels.Count >= totalVowelsNeeded)
                {
                    UnlockNextActivity();
                }
            }
        }
    }

    private void AnimateTileToCollector(RectTransform sourceTile, char letter, bool isVowel)
    {
        if (vowelCollectorBox == null || sourceTile == null) return;

        // 1. Get main Canvas
        Canvas rootCanvas = sourceTile.GetComponentInParent<Canvas>();
        RectTransform canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : sourceTile.root as RectTransform;

        // 2. Instantiate clone under root Canvas initially so it flies freely
        GameObject flyingTile = (vowelCollectedPrefab != null)
            ? Instantiate(vowelCollectedPrefab, canvasRect)
            : Instantiate(sourceTile.gameObject, canvasRect);

        currentActiveTile = flyingTile;

        RectTransform flyingRect = flyingTile.GetComponent<RectTransform>();
        
        // Save actual starting size from sourceTile layout (independent of anchors)
        Vector2 startSize = sourceTile.rect.size;

        // Force center anchors and pivot to align anchoredPosition with Canvas local space
        flyingRect.anchorMin = new Vector2(0.5f, 0.5f);
        flyingRect.anchorMax = new Vector2(0.5f, 0.5f);
        flyingRect.pivot = new Vector2(0.5f, 0.5f);
        flyingRect.sizeDelta = startSize;
        flyingRect.localScale = Vector3.zero; // Start at scale 0 for pop effect

        // 3. Set visual color on clone immediately based on type
        LetterTile_P_Senior cloneScript = flyingTile.GetComponent<LetterTile_P_Senior>();
        Image outlineImage = null;
        if (cloneScript != null)
        {
            outlineImage = cloneScript.tileOutlineOrBackground;
        }
        else
        {
            outlineImage = flyingTile.GetComponentInChildren<Image>();
        }

        if (outlineImage != null)
        {
            outlineImage.color = isVowel ? vowelColor : consonantColor;
            Sprite targetSprite = isVowel ? collectedVowelSprite : collectedConsonantSprite;
            if (targetSprite != null)
            {
                outlineImage.sprite = targetSprite;
            }
        }

        // 4. Disable button & script on clone
        Button cloneBtn = flyingTile.GetComponent<Button>();
        if (cloneBtn != null) cloneBtn.enabled = false;

        if (cloneScript != null) Destroy(cloneScript);

        // 5. Calculate starting position in Canvas local space using InverseTransformPoint (bulletproof for all resolutions/cameras)
        Vector2 startLocalPos = canvasRect.InverseTransformPoint(sourceTile.position);
        flyingRect.anchoredPosition = startLocalPos;

        // 6. Calculate target position in Collector's Local Space (centered + offset)
        // If Y is 0 (default/unassigned in inspector), default to 6f as requested.
        Vector2 targetCollectorLocalPos = new Vector2(collectedTileOffset.x, collectedTileOffset.y == 0f ? 6f : collectedTileOffset.y);

        // Convert target local position to Canvas local space for smooth tweening
        Vector3 targetWorldPoint = vowelCollectorBox.TransformPoint(targetCollectorLocalPos);
        Vector2 targetCanvasLocalPos = canvasRect.InverseTransformPoint(targetWorldPoint);

        Vector2 targetSize = collectedTileSize;

        // 7. LeanTween position and sizeDelta simultaneously
        LeanTween.value(flyingTile, 0f, 1f, flyDuration)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((float val) =>
            {
                if (flyingRect != null)
                {
                    flyingRect.anchoredPosition = Vector2.Lerp(startLocalPos, targetCanvasLocalPos, val);
                    flyingRect.sizeDelta = Vector2.Lerp(startSize, targetSize, val);
                }
            })
            .setOnComplete(() =>
            {
                if (flyingTile != null && vowelCollectorBox != null)
                {
                    // Reparent into vowelCollectorBox and enforce exact size and position
                    flyingTile.transform.SetParent(vowelCollectorBox, false);
                    flyingRect.anchoredPosition = targetCollectorLocalPos;
                    flyingRect.sizeDelta = targetSize;

                    // Trigger bouncing effect on collector
                    BounceCollector();
                }
            });

        // Tween scale from 0 to collectedTileScale using easeOutBack for a springy pop effect
        LeanTween.scale(flyingRect, Vector3.one * collectedTileScale, flyDuration)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                if (flyingRect != null)
                {
                    flyingRect.localScale = Vector3.one * collectedTileScale;
                }
            });
    }

    private void UpdateVowelCounterText()
    {
        if (vowelCounterText != null)
        {
            vowelCounterText.text = $"Vowels:{collectedVowels.Count}/{totalVowelsNeeded}";
        }
    }

    private void UnlockNextActivity()
    {
        if (unitCompleteAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }
        if (nextButton != null)
        {
            nextButton.SetActive(true);
        }
        OnAllVowelsCollected?.Invoke();
    }

    private void BounceCollector()
    {
        if (vowelCollectorBox == null) return;

        // Cancel any active scaling tween on the collector to prevent glitches if clicked quickly
        LeanTween.cancel(vowelCollectorBox.gameObject);
        vowelCollectorBox.localScale = collectorOriginalScale;

        LeanTween.scale(vowelCollectorBox.gameObject, collectorOriginalScale * 1.15f, 0.12f)
            .setEase(LeanTweenType.easeOutBack)
            .setOnComplete(() =>
            {
                LeanTween.scale(vowelCollectorBox.gameObject, collectorOriginalScale, 0.12f)
                    .setEase(LeanTweenType.easeOutQuad);
            });
    }
}