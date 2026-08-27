using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LetterTile_P_Senior : MonoBehaviour
{
    [Header("Letter Config")]
    public char letter;
    public bool isVowel;

    [Header("Audio")]
    public AudioClip nameAudioClip;  // e.g., "B"
    public AudioClip soundAudioClip; // e.g., "/b/"

    [Header("Visual Elements")]
    public Image tileOutlineOrBackground;
    public Color defaultTileColor = Color.white; // Uniform starting color
    public Color vowelGlowColor = new Color(1f, 0.4f, 0.7f); // Pink
    public Color consonantGlowColor = new Color(0.3f, 0.6f, 1f); // Blue

    [Header("Bounce Settings")]
    public float bounceDuration = 0.25f;
    public float scaleFactor = 1.25f;

    private Button button;
    private Vector3 originalScale;
    private MeetTheLettersManager_P_Senior manager;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
    }

    private void Start()
    {
        // Auto-detect if it's a vowel if not manually set in Inspector
        char upperLetter = char.ToUpper(letter);
        isVowel = (upperLetter == 'A' || upperLetter == 'E' || upperLetter == 'I' || upperLetter == 'O' || upperLetter == 'U');

        // Apply uniform theme color based on type
        if (tileOutlineOrBackground != null)
        {
            tileOutlineOrBackground.color = defaultTileColor;
        }

        // Attach click listener
        button.onClick.AddListener(OnTileClicked);
    }

    public void Setup(MeetTheLettersManager_P_Senior mgr)
    {
        manager = mgr;
    }

    private void OnTileClicked()
    {
        Debug.Log($"[Tile Clicked] Tile: {letter} | IsVowel: {isVowel}");

        if (nameAudioClip == null && soundAudioClip == null)
        {
            Debug.LogWarning($"[LetterTile_P_Senior] Audio clips are missing on tile '{letter}'!");
        }

        if (manager == null)
        {
            manager = FindObjectOfType<MeetTheLettersManager_P_Senior>();
        }

        // Play Bounce
        StopAllCoroutines();
        StartCoroutine(AnimateBounce());

        if (manager != null)
        {
            manager.PlayLetterAudio(nameAudioClip, soundAudioClip);

            Debug.Log($"[Tile Sent] Sending {letter} to Manager!");
            manager.OnLetterClicked(letter, GetComponent<RectTransform>());
        }
        else
        {
            Debug.LogError("Manager reference is missing on tile click!");
        }
    }

    private IEnumerator AnimateBounce()
    {
        Vector3 targetScale = originalScale * scaleFactor;
        float elapsed = 0f;

        // Scale Up
        while (elapsed < bounceDuration / 2f)
        {
            transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / (bounceDuration / 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        // Scale Down back to original
        while (elapsed < bounceDuration / 2f)
        {
            transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / (bounceDuration / 2f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
