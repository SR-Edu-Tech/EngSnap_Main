using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
//  ReadingScreen_Panel1_MLDL_Reading
//
//  Panel 1 of the MLDL Reading gameplay.
//  Shows 5 food items one at a time, each with:
//    • A large illustration (left) that pops in with a bouncy scale animation
//    • A coloured speech bubble (right) that pops in after the illustration
//    • 3 lines that highlight one by one in sync with audio
//  After all 5 foods → Next button appears → Panel 2 activates.
// ─────────────────────────────────────────────────────────────────────────────
public class ReadingScreen_Panel1_MLDL_Reading : MonoBehaviour, IUnitCompletable
{
    // ── IUnitCompletable ─────────────────────────────────────────────────
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton          unitButton;

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        panel      = sharedPanel;
        unitButton = sharedButton;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DATA
    // ─────────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class FoodEntry
    {
        [Header("Identity")]
        public string foodName;                  // e.g. "pizza"

        [Header("Visuals")]
        public Sprite foodIllustration;          // large image shown on the left
        public Color  bubbleColor = Color.white; // speech bubble background colour

        [Header("Audio — one clip per line")]
        public AudioClip line1Audio;             // "Do you like pizza?"
        public AudioClip line2Audio;             // "Yes, I like pizza."
        public AudioClip line3Audio;             // "No, I don't like pizza."

        [Header("Gap between lines (seconds)")]
        public float linePause = 0.25f;          // brief pause after each clip
    }

    [Header("─── Food Data ───────────────────────────────────────")]
    public List<FoodEntry> foods = new List<FoodEntry>();

    // ─────────────────────────────────────────────────────────────────────
    //  SCENE REFERENCES
    // ─────────────────────────────────────────────────────────────────────
    [Header("─── Left — Illustration ────────────────────────────")]
    public Image         foodImage;           // large food illustration Image
    public RectTransform foodImageRect;       // its RectTransform (for animation)

    [Header("─── Right — Speech Bubble ──────────────────────────")]
    public Image            bubbleBackground; // coloured bubble panel Image
    public RectTransform    bubbleRect;       // its RectTransform (for animation)
    public TextMeshProUGUI  line1Text;
    public TextMeshProUGUI  line2Text;
    public TextMeshProUGUI  line3Text;

    [Header("─── Highlight ──────────────────────────────────────")]
    public Color highlightColor = new Color(1f, 0.88f, 0f, 1f); // warm yellow
    public Color normalColor    = Color.white;

    [Header("─── Audio — Dialogue ───────────────────────────────")]
    public AudioSource dialogueAudio;   // plays the per-food dialogue clip

    [Header("─── SFX ────────────────────────────────────────────")]
    public AudioSource sfxAudio;        // separate AudioSource for all SFX
    [Space(4)]
    public AudioClip sfxFoodAppear;     // whoosh/pop when illustration appears
    public AudioClip sfxBubbleAppear;   // softer pop when bubble appears
    public AudioClip sfxLineHighlight;  // tiny blip when a line highlights
    public AudioClip sfxAllFoodsDone;   // fanfare / success when Next appears
    public AudioClip sfxButtonClick;    // generic button press

    [Header("─── Buttons ────────────────────────────────────────")]
    public Button replayButton;
    public Button nextButton;
    public bool   enableReplay = true;

    [Header("─── Animation ──────────────────────────────────────")]
    [Tooltip("How long the scale-pop bounce takes in seconds.")]
    public float popDuration   = 0.45f;
    [Tooltip("AnimationCurve for the bounce. Default: overshoot then settle.")]
    public AnimationCurve popCurve = new AnimationCurve(
        new Keyframe(0f,    0f,    0f,  6f),
        new Keyframe(0.65f, 1.12f, 0f,  0f),
        new Keyframe(1f,    1f,    0f,  0f));

    [Header("─── Panel 2 ─────────────────────────────────────────")]
    public GameObject panel2; // drag ReadingScreen_Panel2_MLDL_Reading root here

    // ─────────────────────────────────────────────────────────────────────
    //  RUNTIME
    // ─────────────────────────────────────────────────────────────────────
    private int                    currentFoodIndex = 0;
    private TextMeshProUGUI[]      lineTexts;
    private RectTransform[]        lineRects;        // for highlight pulse
    private Coroutine              highlightPulseCoroutine;

    // ─────────────────────────────────────────────────────────────────────
    void OnEnable()  => Setup();
    void OnDisable()
    {
        StopAllCoroutines();
        if (dialogueAudio != null) dialogueAudio.Stop();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  SETUP
    // ─────────────────────────────────────────────────────────────────────
    void Setup()
    {
        currentFoodIndex = 0;

        lineTexts = new TextMeshProUGUI[] { line1Text, line2Text, line3Text };

        // Cache RectTransforms for pulse animation
        lineRects = new RectTransform[lineTexts.Length];
        for (int i = 0; i < lineTexts.Length; i++)
            if (lineTexts[i] != null)
                lineRects[i] = lineTexts[i].GetComponent<RectTransform>();

        if (nextButton   != null) nextButton.gameObject.SetActive(false);
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(enableReplay);
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() => { PlaySFX(sfxButtonClick); ReplayCurrentFood(); });
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => { PlaySFX(sfxButtonClick); OnNextClicked(); });
        }

        // Hide illustration and bubble at start (will pop in)
        SetScale(foodImageRect,  0f);
        SetScale(bubbleRect,     0f);

        ShowFood(currentFoodIndex);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  SHOW FOOD
    // ─────────────────────────────────────────────────────────────────────
    void ShowFood(int index)
    {
        if (foods == null || index >= foods.Count) return;
        StopAllCoroutines();
        ResetHighlights();

        var food = foods[index];

        // Apply sprite and bubble colour immediately (invisible, will animate in)
        if (foodImage       != null && food.foodIllustration != null)
            foodImage.sprite = food.foodIllustration;
        if (bubbleBackground != null)
            bubbleBackground.color = food.bubbleColor;

        SetLineTexts(food.foodName);

        // Kick off the full per-food coroutine
        StartCoroutine(FoodEntranceAndPlay(food));
    }

    IEnumerator FoodEntranceAndPlay(FoodEntry food)
    {
        // 1 — Illustration pops in
        PlaySFX(sfxFoodAppear);
        yield return StartCoroutine(ScalePop(foodImageRect));

        yield return new WaitForSeconds(0.15f);

        // 2 — Bubble pops in
        PlaySFX(sfxBubbleAppear);
        yield return StartCoroutine(ScalePop(bubbleRect));

        yield return new WaitForSeconds(0.2f);

        // 3 — Play audio + highlight lines
        yield return StartCoroutine(PlayDialogueWithHighlights(food));

        yield return new WaitForSeconds(0.4f);

        // 4 — Shrink out both before showing next food
        yield return StartCoroutine(ScaleOut(foodImageRect));
        yield return StartCoroutine(ScaleOut(bubbleRect));

        yield return new WaitForSeconds(0.15f);

        // 5 — Advance
        currentFoodIndex++;
        if (currentFoodIndex < foods.Count)
        {
            ShowFood(currentFoodIndex);
        }
        else
        {
            // All foods done
            PlaySFX(sfxAllFoodsDone);
            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DIALOGUE + HIGHLIGHT
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator PlayDialogueWithHighlights(FoodEntry food)
    {
        if (dialogueAudio == null) yield break;

        AudioClip[] clips = { food.line1Audio, food.line2Audio, food.line3Audio };

        for (int i = 0; i < clips.Length; i++)
        {
            // Highlight this line
            PlaySFX(sfxLineHighlight);
            ApplyHighlight(i);

            // Play the clip if assigned
            if (clips[i] != null)
            {
                dialogueAudio.Stop();
                dialogueAudio.clip  = clips[i];
                dialogueAudio.pitch = 1f;
                dialogueAudio.Play();

                // Wait for playback to begin, then wait for it to finish
                yield return new WaitUntil(() => dialogueAudio.isPlaying);
                while (dialogueAudio.isPlaying) yield return null;
            }
            else
            {
                // No clip assigned — short fallback wait so the game doesn't freeze
                yield return new WaitForSeconds(0.8f);
            }

            // Brief pause between lines
            float pause = Mathf.Max(0f, food.linePause);
            if (pause > 0f) yield return new WaitForSeconds(pause);
        }

        ResetHighlights();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HIGHLIGHT
    // ─────────────────────────────────────────────────────────────────────
    void ApplyHighlight(int activeIndex)
    {
        if (highlightPulseCoroutine != null) StopCoroutine(highlightPulseCoroutine);

        for (int i = 0; i < lineTexts.Length; i++)
        {
            if (lineTexts[i] == null) continue;
            lineTexts[i].color = (i == activeIndex) ? highlightColor : normalColor;

            // Reset scale on all lines
            if (lineRects[i] != null) lineRects[i].localScale = Vector3.one;
        }

        // Pulse the active line
        if (activeIndex >= 0 && activeIndex < lineRects.Length && lineRects[activeIndex] != null)
            highlightPulseCoroutine = StartCoroutine(PulseLine(lineRects[activeIndex]));
    }

    void ResetHighlights() => ApplyHighlight(-1);

    // Gentle breathing scale on the active line
    IEnumerator PulseLine(RectTransform rt)
    {
        float speed = 1.8f;
        float min   = 1.00f;
        float max   = 1.04f;
        while (true)
        {
            float s = Mathf.Lerp(min, max, (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f) * 0.5f);
            if (rt != null) rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ANIMATION HELPERS
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator ScalePop(RectTransform rt)
    {
        if (rt == null) yield break;
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / popDuration);
            float s = popCurve.Evaluate(t);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator ScaleOut(RectTransform rt)
    {
        if (rt == null) yield break;
        float dur     = popDuration * 0.6f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / dur);
            rt.localScale = new Vector3(t, t, 1f);
            yield return null;
        }
        rt.localScale = Vector3.zero;
    }

    void SetScale(RectTransform rt, float s)
    {
        if (rt != null) rt.localScale = new Vector3(s, s, 1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UTILITIES
    // ─────────────────────────────────────────────────────────────────────
    void SetLineTexts(string foodName)
    {
        string n = string.IsNullOrEmpty(foodName) ? "___" : foodName;
        if (line1Text != null) line1Text.text = $"Do you like {n}?";
        if (line2Text != null) line2Text.text = $"Yes, I like {n}.";
        if (line3Text != null) line3Text.text  = $"No, I don't like {n}.";
    }

    void PlaySFX(AudioClip clip)
    {
        if (sfxAudio != null && clip != null)
            sfxAudio.PlayOneShot(clip);
    }

    void ReplayCurrentFood()
    {
        StopAllCoroutines();
        if (dialogueAudio != null) dialogueAudio.Stop();
        ResetHighlights();
        SetScale(foodImageRect, 0f);
        SetScale(bubbleRect,    0f);
        ShowFood(currentFoodIndex);
    }

    void OnNextClicked()
    {
        StopAllCoroutines();
        if (dialogueAudio != null) dialogueAudio.Stop();

        if (panel2 != null)
        {
            panel2.SetActive(true);
            gameObject.SetActive(false);
        }
        else
        {
            if (panel != null && unitButton != null) panel.UnitFinished(unitButton);
            else gameObject.SetActive(false);
        }
    }

    public void OnBackClicked()
    {
        StopAllCoroutines();
        if (dialogueAudio != null) dialogueAudio.Stop();
        gameObject.SetActive(false);
        if (panel != null) panel.gameObject.SetActive(true);
    }
}