using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Emoji Mood Matching Game
/// ─────────────────────────────────────────────────────────────────────────
/// FLOW:
///   1. Panel becomes active → intro audio plays → Round 1 loads.
///   2. Each round: mood word card pops in → question audio plays
///      → 4 emoji buttons pop in (positions randomised) → player taps.
///   3. CORRECT tap  → emoji bounces + glows green → correct audio →
///      1 s pause → next round.
///   4. WRONG tap    → emoji shakes → wrong audio → player retries.
///      After 2 wrong attempts → correct emoji glows yellow as a hint.
///   5. All rounds done → emoji buttons hide → Next button shown.
///
/// PREFAB REQUIREMENTS:
///   • moodCardText        — TMP_Text (the word card label, e.g. "I am happy!")
///   • emojiButtons[4]     — 4 Buttons whose own Image is used for glow/color feedback
///   • emojiDisplayImages[4] — 4 Image components that show the emoji sprite.
///                             Can be the same as the button Image, OR a separate
///                             child Image — just drag whatever shows the emoji into
///                             this array in the Inspector.
///   • audioSource         — AudioSource on any GameObject
///   • introAudio          — AudioClip played once when the panel first opens
///   • nextButton          — Button shown after all rounds complete
///   • nextGamePanel       — sibling panel enabled when Next is pressed
/// ─────────────────────────────────────────────────────────────────────────
/// </summary>
public class EmojiMoodGameController : MonoBehaviour
{
    // ── Data ─────────────────────────────────────────────────────────────
    [Header("Questions")]
    [Tooltip("One entry per round (4 recommended).")]
    public EmojiMoodQuestion[] questions;

    // ── UI ────────────────────────────────────────────────────────────────
    [Header("UI")]
    [Tooltip("TMP_Text that shows the mood phrase word card.")]
    public TMP_Text moodCardText;

    [Tooltip("Exactly 4 buttons. Their own Image component is used for glow/color feedback.")]
    public Button[] emojiButtons;

    [Tooltip("Exactly 4 Image components that display the emoji sprite each round. " +
             "These can be the same Image as the button itself, OR a separate child Image — " +
             "just drag whichever Image shows the emoji into each slot here.")]
    public Image[] emojiDisplayImages;

    // ── Audio ─────────────────────────────────────────────────────────────
    [Header("Audio")]
    public AudioSource audioSource;

    [Tooltip("Plays once when the panel opens, before the first question.")]
    public AudioClip introAudio;

    [Tooltip("Short bounce sound played when the correct emoji bounces.")]
    public AudioClip bounceSFX;

    [Tooltip("Short pop sound played when the emoji buttons appear.")]
    public AudioClip buttonPopSound;

    // ── Navigation ────────────────────────────────────────────────────────
    [Header("Navigation")]
    [Tooltip("Shown after all rounds are complete.")]
    public Button nextButton;

    [Tooltip("Sibling panel enabled when Next is pressed.")]
    public GameObject nextGamePanel;
    public GameObject rootpanel;

    // ── Colors ────────────────────────────────────────────────────────────
    [Header("Colors")]
    public Color normalColor  = Color.white;
    public Color correctColor = new Color(0.3f, 0.9f, 0.3f, 1f);   // green glow
    public Color wrongColor   = new Color(0.95f, 0.3f, 0.3f, 1f);  // red tint
    public Color hintColor    = new Color(1f, 0.9f, 0.2f, 1f);     // yellow hint

    // ── Animation ─────────────────────────────────────────────────────────
    [Header("Animation Settings")]
    public float popDuration    = 0.25f;
    public float popScale       = 1.25f;
    public float bounceDuration = 0.35f;
    public float bounceScale    = 1.35f;
    public float shakeDuration  = 0.28f;
    public float shakeStrength  = 12f;

    // ── Private state ─────────────────────────────────────────────────────
    private int   currentIndex   = 0;
    private bool  canAnswer      = false;
    private bool  listenersWired = false;
    private int   wrongAttempts  = 0;

    // Per-button cached refs
    private Image[] buttonBgImages;   // Button's own Image (glow/color bg)

    // Mapping: display slot → question's emojiSprites index (set each round)
    private int[] slotToSpriteIndex;

    // Which display slot currently holds the correct emoji (set each round)
    private int correctSlot = -1;

    // ═════════════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════

    void Awake()
    {
        // Cache button background Images (the Button's own Image — used for glow/color)
        buttonBgImages    = new Image[emojiButtons.Length];
        slotToSpriteIndex = new int[emojiButtons.Length];

        for (int i = 0; i < emojiButtons.Length; i++)
            buttonBgImages[i] = emojiButtons[i].GetComponent<Image>();

        // emojiDisplayImages is assigned directly in the Inspector.
        // Validate counts so misconfiguration is caught early.
        if (emojiDisplayImages == null || emojiDisplayImages.Length != emojiButtons.Length)
            Debug.LogError("[EmojiMood] emojiDisplayImages must have the same number of entries " +
                           "as emojiButtons (" + emojiButtons.Length + "). " +
                           "Drag the Image that shows each emoji into each slot in the Inspector.");

        // Wire Next button once
        if (!listenersWired)
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextPressed);
            listenersWired = true;
        }
    }

    void OnEnable()
    {
        // Resets and starts fresh every time this panel becomes visible
        ResetAndStart();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  RESET & START
    // ═════════════════════════════════════════════════════════════════════

    void ResetAndStart()
    {
        StopAllCoroutines();

        canAnswer    = false;
        currentIndex = 0;
        wrongAttempts = 0;

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        SetOptionsVisible(false);
        ResetAllButtonColors();

        if (moodCardText != null)
            moodCardText.text = "";

        StartCoroutine(PlayIntroThenLoad());
    }

    IEnumerator PlayIntroThenLoad()
    {
        // Play the intro/gameplay audio first
        if (introAudio != null && audioSource != null)
        {
            audioSource.clip = introAudio;
            audioSource.Play();
            yield return new WaitForSeconds(introAudio.length);
        }

        if (questions != null && questions.Length > 0)
            LoadQuestion(0);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  LOAD QUESTION
    // ═════════════════════════════════════════════════════════════════════

    void LoadQuestion(int index)
    {
        StopAllCoroutines();   // stop any lingering coroutines from previous round

        currentIndex  = index;
        wrongAttempts = 0;
        canAnswer     = false;

        var q = questions[index];

        // ── Mood word card ────────────────────────────────────────────────
        if (moodCardText != null)
        {
            moodCardText.text = q.moodText;
            moodCardText.transform.localScale = Vector3.zero;
            StartCoroutine(PopIn(moodCardText.transform));
        }

        // ── Hide buttons while audio plays ────────────────────────────────
        SetOptionsVisible(false);
        ResetAllButtonColors();

        // ── Randomise emoji positions ─────────────────────────────────────
        // Build a shuffled mapping of display-slot → sprite index
        List<int> indices = new List<int>();
        for (int i = 0; i < q.emojiSprites.Length && i < emojiButtons.Length; i++)
            indices.Add(i);
        Shuffle(indices);

        for (int slot = 0; slot < emojiButtons.Length; slot++)
        {
            int spriteIdx = indices[slot];
            slotToSpriteIndex[slot] = spriteIdx;

            // Assign the emoji sprite to the display Image
            if (emojiDisplayImages != null && slot < emojiDisplayImages.Length
                && emojiDisplayImages[slot] != null && spriteIdx < q.emojiSprites.Length)
            {
                emojiDisplayImages[slot].sprite = q.emojiSprites[spriteIdx];
                emojiDisplayImages[slot].enabled = true;   // ensure visible
            }

            // Track which slot the correct emoji landed in
            if (spriteIdx == q.correctIndex)
                correctSlot = slot;

            // Wire click listener
            int captured = slot;
            emojiButtons[slot].onClick.RemoveAllListeners();
            emojiButtons[slot].onClick.AddListener(() => OnEmojiTapped(captured));
        }

        StartCoroutine(PlayQuestionAudioThenShow(q.questionAudio));
    }

    IEnumerator PlayQuestionAudioThenShow(AudioClip clip)
    {
        canAnswer = false;

        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }

        ShowButtonsWithPop();
        SetOptionsInteractable(true);
        canAnswer = true;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TAP HANDLER
    // ═════════════════════════════════════════════════════════════════════

    void OnEmojiTapped(int slot)
    {
        if (!canAnswer) return;

        canAnswer = false;
        SetOptionsInteractable(false);

        var q = questions[currentIndex];

        if (slot == correctSlot)
            StartCoroutine(HandleCorrect(slot, q));
        else
            StartCoroutine(HandleWrong(slot, q));
    }

    IEnumerator HandleCorrect(int slot, EmojiMoodQuestion q)
    {
        // Green glow on correct button
        if (buttonBgImages[slot] != null)
            buttonBgImages[slot].color = correctColor;

        // Bounce animation + bounce SFX
        PlaySFX(bounceSFX);
        yield return StartCoroutine(BounceScale(emojiButtons[slot].transform));

        // Correct feedback audio
        if (q.correctAudio != null && audioSource != null)
        {
            audioSource.clip = q.correctAudio;
            audioSource.Play();
            yield return new WaitForSeconds(q.correctAudio.length);
        }

        yield return new WaitForSeconds(1f);
        GoNext();
    }

    IEnumerator HandleWrong(int slot, EmojiMoodQuestion q)
    {
        wrongAttempts++;

        // Red tint on wrong button
        if (buttonBgImages[slot] != null)
            buttonBgImages[slot].color = wrongColor;

        yield return StartCoroutine(Shake(emojiButtons[slot].transform));

        // Wrong feedback audio
        if (q.wrongAudio != null && audioSource != null)
        {
            audioSource.clip = q.wrongAudio;
            audioSource.Play();
            yield return new WaitForSeconds(q.wrongAudio.length);
        }

        yield return new WaitForSeconds(0.2f);

        // Reset tapped button back to normal
        if (buttonBgImages[slot] != null)
            buttonBgImages[slot].color = normalColor;

        // After 2 wrong attempts, highlight the correct emoji as a hint
        if (wrongAttempts >= 2 && correctSlot >= 0)
        {
            if (buttonBgImages[correctSlot] != null)
                buttonBgImages[correctSlot].color = hintColor;
        }

        canAnswer = true;
        SetOptionsInteractable(true);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  NAVIGATION
    // ═════════════════════════════════════════════════════════════════════

    void GoNext()
    {
        int next = currentIndex + 1;

        if (next < questions.Length)
        {
            LoadQuestion(next);
        }
        else
        {
            // All rounds complete — hide emojis, show Next button
            SetOptionsVisible(false);
            if (nextButton != null)
                nextButton.gameObject.SetActive(true);
        }
    }

    void OnNextPressed()
    {
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        gameObject.SetActive(false);

        if (nextGamePanel != null)
            nextGamePanel.SetActive(true);
            rootpanel.SetActive(false); 
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BUTTON HELPERS
    // ═════════════════════════════════════════════════════════════════════

    void ShowButtonsWithPop()
    {
        PlaySFX(buttonPopSound);

        foreach (var btn in emojiButtons)
        {
            btn.gameObject.SetActive(true);
            btn.transform.localScale = Vector3.zero;
            StartCoroutine(PopIn(btn.transform));
        }
    }

    void SetOptionsVisible(bool value)
    {
        foreach (var btn in emojiButtons)
            btn.gameObject.SetActive(value);
    }

    void SetOptionsInteractable(bool value)
    {
        foreach (var btn in emojiButtons)
            btn.interactable = value;
    }

    void ResetAllButtonColors()
    {
        foreach (var img in buttonBgImages)
            if (img != null) img.color = normalColor;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  ANIMATIONS
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Scale from zero → popScale → 1 (pop-in entrance).</summary>
    IEnumerator PopIn(Transform target)
    {
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / popDuration);
            target.localScale = Vector3.one * Mathf.LerpUnclamped(0f, popScale, EaseOutBack(p));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    /// <summary>Bounce: scale up to bounceScale then back to 1 (correct feedback).</summary>
    IEnumerator BounceScale(Transform target)
    {
        float half = bounceDuration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            target.localScale = Vector3.one * Mathf.Lerp(1f, bounceScale, EaseOutQuad(p));
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            target.localScale = Vector3.one * Mathf.Lerp(bounceScale, 1f, EaseOutQuad(p));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    /// <summary>Horizontal shake (wrong feedback).</summary>
    IEnumerator Shake(Transform target)
    {
        Vector3 origin = target.localPosition;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float progress  = t / shakeDuration;
            float amplitude = shakeStrength * (1f - progress); // dampen toward end
            target.localPosition = origin + new Vector3(
                Random.Range(-1f, 1f) * amplitude, 0f, 0f);
            yield return null;
        }
        target.localPosition = origin;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  EASING HELPERS
    // ═════════════════════════════════════════════════════════════════════

    static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

    // ═════════════════════════════════════════════════════════════════════
    //  AUDIO HELPER
    // ═════════════════════════════════════════════════════════════════════

    void PlaySFX(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  UTILITY
    // ═════════════════════════════════════════════════════════════════════

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  UNITY PREFAB / SCENE SETUP GUIDE
// ═══════════════════════════════════════════════════════════════════════════
//
//  [EmojiMoodPanel]  ← this GameObject holds EmojiMoodGameController
//    ├── MoodCard               ← Panel / Image background for the word card
//    │     └── MoodCardText     ← TMP_Text  → assign to moodCardText
//    │
//    ├── EmojiGrid              ← GridLayoutGroup (2×2 recommended)
//    │     ├── EmojiButton_0    ← Button
//    │     │     └── EmojiImage ← Image that shows the emoji sprite
//    │     ├── EmojiButton_1
//    │     │     └── EmojiImage
//    │     ├── EmojiButton_2
//    │     │     └── EmojiImage
//    │     └── EmojiButton_3
//    │           └── EmojiImage
//    │
//    ├── NextButton             ← Button → assign to nextButton (disabled by default)
//    └── AudioSource            ← AudioSource component → assign to audioSource
//
//  INSPECTOR FIELDS TO FILL:
//    • emojiButtons[4]       — drag the 4 Button GameObjects here
//    • emojiDisplayImages[4] — drag the 4 Image components that SHOW the emoji
//                              sprite here. This can be:
//                              (a) a separate child Image inside each button, OR
//                              (b) the Button's own Image if you use it for sprites.
//                              Just drag whichever Image you want the sprite on.
//    • introAudio            — the main gameplay audio that plays before round 1
//    • bounceSFX             — short bounce sound for correct tap
//    • buttonPopSound        — short pop for when emoji buttons appear
//    • nextGamePanel         — the next sibling panel to activate on Next press
//
//  PER QUESTION (EmojiMoodQuestion):
//    • moodText          — "I am happy!"
//    • emojiSprites[4]   — assign 4 emoji sprites (positions auto-randomised)
//    • correctIndex      — index in emojiSprites[] of the correct answer (0-3)
//    • questionAudio     — audio clip for this round's mood phrase
//    • correctAudio      — correct feedback clip
//    • wrongAudio        — wrong feedback clip
//
// ═══════════════════════════════════════════════════════════════════════════