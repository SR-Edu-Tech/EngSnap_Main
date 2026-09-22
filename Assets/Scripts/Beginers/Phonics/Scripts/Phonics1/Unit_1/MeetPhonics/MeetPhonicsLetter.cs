using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MeetPhonicsLetter : MonoBehaviour
{
    [Header("Letter Config")]
    [Tooltip("The letter represented by this button (e.g. 'P', 'H', 'O', etc.)")]
    [SerializeField] private string letterChar;

    [Tooltip("The audio clip played when this letter is tapped (phonetic sound).")]
    [SerializeField] private AudioClip soundClip;

    [Header("UI Visuals")]
    [Tooltip("Optional glow/highlight image shown when the letter is interactable.")]
    [SerializeField] private GameObject glowEffect;

    [Header("Confetti")]
    [Tooltip("Confetti prefab to spawn behind this letter when tapped.")]
    [SerializeField] private GameObject confettiPrefab;

    [Tooltip("How many seconds before the spawned confetti is destroyed.")]
    [SerializeField] private float confettiLifetime = 3f;

    [Header("Wiggle Animation")]
    [Tooltip("Duration of the bounce/wiggle animation when the letter is tapped.")]
    [SerializeField] private float wiggleDuration = 0.5f;

    // ── Private state ────────────────────────────────────────────────────────
    private MeetPhonicsController controller;
    private Button button;
    private bool isTapped = false;
    private Coroutine wiggleCoroutine;

    private Vector3 initialScale;
    private Quaternion initialRotation;

    // ── Public accessors ─────────────────────────────────────────────────────
    public string LetterChar => letterChar;
    public AudioClip SoundClip => soundClip;
    public bool IsTapped => isTapped;

    // ────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        button = GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError($"MeetPhonicsLetter: Button component missing on '{gameObject.name}'.");
            return;
        }

        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnLetterTapped);

        // Cache starting transform values for the wiggle reset
        initialScale    = transform.localScale;
        initialRotation = transform.localRotation;

        Debug.Log($"MeetPhonicsLetter Awake: '{letterChar}' ready.");
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>Called by MeetPhonicsController on scene start.</summary>
    public void Initialize(MeetPhonicsController mainController)
    {
        controller = mainController;
        isTapped   = false;
        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }
        if (initialScale != Vector3.zero) transform.localScale = initialScale;
        transform.localRotation = initialRotation;
        SetInteractable(false);
        if (glowEffect != null) glowEffect.SetActive(false);
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>Enables/disables the button and optional glow effect.</summary>
    public void SetInteractable(bool state)
    {
        if (button != null) button.interactable = state;
        if (glowEffect != null) glowEffect.SetActive(state);
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>Fired by Button.onClick when the player taps the letter.</summary>
    private void OnLetterTapped()
    {
        isTapped = true;
        Debug.Log($"Letter '{letterChar}' tapped.");

        // ── Find controller as fallback ──────────────────────────────────────
        if (controller == null)
        {
            controller = FindObjectOfType<MeetPhonicsController>();
            if (controller == null)
            {
                Debug.LogError("MeetPhonicsLetter: No MeetPhonicsController found in scene.");
            }
        }

        // ── Play sound ───────────────────────────────────────────────────────
        if (soundClip == null)
        {
            Debug.LogError($"MeetPhonicsLetter: SoundClip missing for letter '{letterChar}'.");
        }
        else if (controller != null && controller.SfxAudioSource != null)
        {
            controller.SfxAudioSource.Stop();
            controller.SfxAudioSource.loop = false;
            controller.SfxAudioSource.PlayOneShot(soundClip);
        }
        else
        {
            // Last resort: look for an AudioSource on this GameObject
            AudioSource local = GetComponent<AudioSource>();
            if (local != null)
            {
                local.Stop();
                local.loop = false;
                local.PlayOneShot(soundClip);
            }
            else
                Debug.LogWarning($"MeetPhonicsLetter: No AudioSource to play '{letterChar}'.");
        }

        // ── Notify controller ────────────────────────────────────────────────
        if (controller != null)
            controller.OnLetterTapped(this);

        // ── Confetti burst ───────────────────────────────────────────────────
        SpawnConfetti();

        // ── Wiggle feedback ──────────────────────────────────────────────────
        if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
        wiggleCoroutine = StartCoroutine(WiggleCoroutine());
    }

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Public entry point so the controller can trigger confetti on this letter
    /// (e.g. when all letters are completed).
    /// </summary>
    public void TriggerConfetti() => SpawnConfetti();

    // ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Instantiates the confetti prefab at this letter's position and places it
    /// one sibling-index below the letter so it renders behind it in the Canvas.
    /// </summary>
    private void SpawnConfetti()
    {
        if (confettiPrefab == null) return;

        // Spawn as a child of the letter's parent so it stays inside the Canvas
        Transform parent = transform.parent != null ? transform.parent : transform;
        GameObject confetti = Instantiate(confettiPrefab, parent);

        // Match the letter's position
        RectTransform myRect = GetComponent<RectTransform>();
        RectTransform confettiRect = confetti.GetComponent<RectTransform>();

        if (myRect != null && confettiRect != null)
        {
            // Copy anchored position so it lines up in UI space
            confettiRect.anchoredPosition = myRect.anchoredPosition;
            confettiRect.sizeDelta        = myRect.sizeDelta;
            confettiRect.localScale       = Vector3.one;
        }
        else
        {
            // World-space prefab – match world position
            confetti.transform.position = transform.position;
        }

        // Push behind this letter by placing it just before our sibling index
        int myIndex = transform.GetSiblingIndex();
        confetti.transform.SetSiblingIndex(Mathf.Max(0, myIndex - 1));

        // Auto-destroy after lifetime
        Destroy(confetti, confettiLifetime);
    }

    // ────────────────────────────────────────────────────────────────────────
    private IEnumerator WiggleCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < wiggleDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / wiggleDuration;

            // Scale: pop up then return
            float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.25f;
            transform.localScale = initialScale * scaleFactor;

            // Rotation: tilt left then right then back
            float rotZ = Mathf.Sin(percent * Mathf.PI * 2f) * 10f;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            yield return null;
        }

        // Reset to original transform
        transform.localScale    = initialScale;
        transform.localRotation = initialRotation;
        wiggleCoroutine = null;
    }
}
