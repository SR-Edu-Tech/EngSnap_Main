using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
//  ReadingScreen_Panel2_MLDL_Reading
//
//  PHASE A — AUTO-PLAY
//    • Illustration starts as the B&W sprite.
//    • Each sentence highlights (with a bounce pop) and plays its audio.
//    • When a sentence finishes, the paired sprite swaps in with a fun reveal SFX.
//    • After the last sentence → Phase B begins.
//
//  PHASE B — STUDENT COLOURING
//    • Illustration resets to B&W outline.
//    • Colour palette appears (buttons pop in one by one).
//    • Kid selects a colour → taps a region → region fills + SFX plays.
//    • Next button is always visible; tapping it finishes the unit.
// ─────────────────────────────────────────────────────────────────────────────
public class ReadingScreen_Panel2_MLDL_Reading : MonoBehaviour, IUnitCompletable
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
    public class SentenceEntry
    {
        [Header("Text")]
        // Use TMP rich-text for colour words, e.g.:
        //   "I saw a <color=#FF0000>RED</color> dinosaur in the desert."
        public string sentenceText;

        [Header("Audio")]
        public AudioClip audioClip;

        [Header("Sprite Swap — applied AFTER this sentence finishes")]
        // Assign the cumulative illustration sprite for this stage.
        // Leave null for no swap.
        public Sprite resultSprite;
    }

    [Header("─── Sentences ──────────────────────────────────────")]
    public List<SentenceEntry> sentences = new List<SentenceEntry>();

    // ─────────────────────────────────────────────────────────────────────
    //  SCENE REFERENCES
    // ─────────────────────────────────────────────────────────────────────
    [Header("─── Sentence List (left) ───────────────────────────")]
    public Transform  sentenceContainer;  // Vertical layout parent
    public GameObject sentencePrefab;     // Prefab with TextMeshProUGUI

    [Header("─── Highlight Colours ─────────────────────────────")]
    public Color highlightColor = new Color(1f, 0.88f, 0f, 1f);
    public Color normalColor    = Color.white;

    [Header("─── Illustration (right) ──────────────────────────")]
    public Image         illustrationImage;   // the main artwork Image
    public RectTransform illustrationRect;    // its RectTransform (for animation)
    public Sprite        bwSprite;            // black-and-white outline

    [Header("─── Audio ──────────────────────────────────────────")]
    public AudioSource dialogueAudio;
    public Button      replayButton;
    public bool        enableReplay = true;

    [Header("─── SFX ────────────────────────────────────────────")]
    public AudioSource sfxAudio;
    [Space(4)]
    public AudioClip sfxSentenceHighlight; // blip when a sentence activates
    public AudioClip sfxColourReveal;      // pop/reveal when sprite swaps
    public AudioClip sfxPhaseBStart;       // fanfare when colouring mode begins
    public AudioClip sfxColourSelect;      // click when kid picks a colour
    public AudioClip sfxRegionFill;        // splat/pop when a region is filled
    public AudioClip sfxButtonClick;       // generic button SFX
    public AudioClip sfxAllFoodsDone;      // success when all sentences done

    [Header("─── Animation ──────────────────────────────────────")]
    public float         popDuration = 0.4f;
    public AnimationCurve popCurve   = new AnimationCurve(
        new Keyframe(0f,    0f,    0f,  6f),
        new Keyframe(0.65f, 1.12f, 0f,  0f),
        new Keyframe(1f,    1f,    0f,  0f));

    // ── Phase B — Colouring ───────────────────────────────────────────────
    [Header("─── Phase B — Colour Palette ───────────────────────")]
    public GameObject        paletteRoot;   // hidden during Phase A
    public List<ColorButton> colorButtons = new List<ColorButton>();

    [System.Serializable]
    public class ColorButton
    {
        public string        colorName;  // e.g. "Red" (for your reference)
        public Button        button;
        public Color         color;
        public RectTransform buttonRect; // for pop-in animation
    }

    [Header("─── Phase B — Colourable Regions ───────────────────")]
    // Each region is a transparent UI Image sitting over the artwork.
    // The kid selects a colour then taps the region to fill it.
    public List<ColourRegion> colourRegions = new List<ColourRegion>();

    [System.Serializable]
    public class ColourRegion
    {
        public string        regionName;  // e.g. "DinosaurBody" (reference only)
        public Image         regionImage; // overlay Image, starts alpha = 0
        public RectTransform regionRect;  // for fill animation
    }

    [Header("─── Buttons ────────────────────────────────────────")]
    public Button nextButton;

    [Header("─── Colouring Phase ──────────────────────────────")]
    [Tooltip("Enable Phase B (student colouring). " +
             "When OFF, Next button appears immediately after Phase A and goes straight to the unit panel.")]
    public bool enableColouringPhase = false;

    // ─────────────────────────────────────────────────────────────────────
    //  RUNTIME
    // ─────────────────────────────────────────────────────────────────────
    private List<TextMeshProUGUI>  sentenceItems  = new List<TextMeshProUGUI>();
    private List<RectTransform>    sentenceRects  = new List<RectTransform>();
    private Color                  selectedColor  = Color.clear;
    private bool                   hasSelectedColor = false;
    private bool                   phaseB         = false;
    private Coroutine              pulseCo;

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
        phaseB           = false;
        hasSelectedColor = false;

        if (paletteRoot != null) paletteRoot.SetActive(false);
        if (nextButton  != null) nextButton.gameObject.SetActive(false);

        // B&W illustration
        if (illustrationImage != null && bwSprite != null)
            illustrationImage.sprite = bwSprite;

        // Reset region overlays to fully transparent
        foreach (var r in colourRegions)
            if (r.regionImage != null)
                r.regionImage.color = new Color(0f, 0f, 0f, 0f);

        // Replay
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(enableReplay);
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() => { PlaySFX(sfxButtonClick); ReplayFromStart(); });
        }

        // Next
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => { PlaySFX(sfxButtonClick); OnNextClicked(); });
        }

        BuildSentenceList();

        // Illustration pops in
        StartCoroutine(IntroThenPlay());
    }

    IEnumerator IntroThenPlay()
    {
        SetScale(illustrationRect, 0f);
        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(ScalePop(illustrationRect));
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(PlaySequence());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  SENTENCE LIST
    // ─────────────────────────────────────────────────────────────────────
    void BuildSentenceList()
    {
        if (sentenceContainer == null || sentencePrefab == null) return;
        foreach (Transform child in sentenceContainer) Destroy(child.gameObject);
        sentenceItems.Clear();
        sentenceRects.Clear();

        for (int i = 0; i < sentences.Count; i++)
        {
            var go  = Instantiate(sentencePrefab, sentenceContainer);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text  = sentences[i].sentenceText;
                tmp.color = normalColor;
                sentenceItems.Add(tmp);
                sentenceRects.Add(go.GetComponent<RectTransform>());
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PHASE A — AUTO-PLAY SEQUENCE
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator PlaySequence()
    {
        for (int i = 0; i < sentences.Count; i++)
        {
            var entry = sentences[i];

            // Highlight bounce
            PlaySFX(sfxSentenceHighlight);
            ApplyHighlight(i);
            if (i < sentenceRects.Count && sentenceRects[i] != null)
                StartCoroutine(QuickBounce(sentenceRects[i]));

            // Play audio
            if (dialogueAudio != null && entry.audioClip != null)
            {
                dialogueAudio.Stop();
                dialogueAudio.clip  = entry.audioClip;
                dialogueAudio.pitch = 1f;
                dialogueAudio.Play();
                yield return new WaitUntil(() => dialogueAudio.isPlaying);
                while (dialogueAudio.isPlaying) yield return null;
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            ResetHighlights();

            // Sprite swap with reveal SFX
            if (entry.resultSprite != null && illustrationImage != null)
            {
                PlaySFX(sfxColourReveal);
                yield return StartCoroutine(SpriteReveal(entry.resultSprite));
            }

            yield return new WaitForSeconds(0.3f);
        }

        PlaySFX(sfxAllFoodsDone);
        yield return new WaitForSeconds(0.5f);

        if (enableColouringPhase)
        {
            BeginPhaseB();
        }
        else
        {
            // Phase B disabled — just show Next to finish the unit
            if (nextButton != null) nextButton.gameObject.SetActive(true);
        }
    }

    // Brief scale-down/up flash when sprite swaps — makes the reveal feel punchy
    IEnumerator SpriteReveal(Sprite newSprite)
    {
        if (illustrationRect == null) { illustrationImage.sprite = newSprite; yield break; }

        // Squish in
        float dur = 0.12f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0.85f, t / dur);
            illustrationRect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        illustrationImage.sprite = newSprite;

        // Pop back out with overshoot
        yield return StartCoroutine(ScalePop(illustrationRect));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PHASE B — STUDENT COLOURING
    // ─────────────────────────────────────────────────────────────────────
    void BeginPhaseB()
    {
        phaseB = true;

        // Reset to B&W
        if (illustrationImage != null && bwSprite != null)
            illustrationImage.sprite = bwSprite;

        // Reset region tints
        foreach (var r in colourRegions)
            if (r.regionImage != null)
                r.regionImage.color = new Color(0f, 0f, 0f, 0f);

        // Show palette and animate buttons in
        if (paletteRoot != null)
        {
            paletteRoot.SetActive(true);
            StartCoroutine(AnimatePaletteIn());
        }

        // Wire region buttons
        foreach (var region in colourRegions)
        {
            if (region.regionImage == null) continue;
            var btn = region.regionImage.GetComponent<Button>()
                   ?? region.regionImage.gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            var img  = region.regionImage;
            btn.onClick.AddListener(() => FillRegion(img));
        }

        // Wire colour buttons
        foreach (var cb in colorButtons)
        {
            if (cb.button == null) continue;
            Color c   = cb.color;
            var   img = cb.button.GetComponent<Image>();
            if (img != null) img.color = c;
            cb.button.onClick.RemoveAllListeners();
            cb.button.onClick.AddListener(() => SelectColor(c));
        }

        // Next always visible in Phase B
        if (nextButton != null) nextButton.gameObject.SetActive(true);

        PlaySFX(sfxPhaseBStart);
        ResetHighlights();
    }

    // Colour buttons pop in one by one for a delightful entrance
    IEnumerator AnimatePaletteIn()
    {
        foreach (var cb in colorButtons)
        {
            if (cb.buttonRect != null)
            {
                SetScale(cb.buttonRect, 0f);
                StartCoroutine(ScalePop(cb.buttonRect));
                yield return new WaitForSeconds(0.07f);
            }
        }
    }

    void SelectColor(Color color)
    {
        selectedColor    = color;
        hasSelectedColor = true;
        PlaySFX(sfxColourSelect);
        UpdatePaletteVisuals();
    }

    void UpdatePaletteVisuals()
    {
        foreach (var cb in colorButtons)
        {
            if (cb.button == null) continue;
            var img = cb.button.GetComponent<Image>();
            if (img == null) continue;
            // Selected = full brightness; others = dimmed
            bool isSelected = (cb.color == selectedColor);
            img.color = isSelected
                ? cb.color
                : new Color(cb.color.r * 0.55f, cb.color.g * 0.55f, cb.color.b * 0.55f, 1f);

            // Scale selected button up slightly
            if (cb.buttonRect != null)
                cb.buttonRect.localScale = isSelected
                    ? new Vector3(1.15f, 1.15f, 1f)
                    : Vector3.one;
        }
    }

    void FillRegion(Image regionImage)
    {
        if (!hasSelectedColor || !phaseB) return;
        regionImage.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 1f);
        PlaySFX(sfxRegionFill);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HIGHLIGHT
    // ─────────────────────────────────────────────────────────────────────
    void ApplyHighlight(int activeIndex)
    {
        if (pulseCo != null) StopCoroutine(pulseCo);
        for (int i = 0; i < sentenceItems.Count; i++)
        {
            if (sentenceItems[i] == null) continue;
            sentenceItems[i].color = (i == activeIndex) ? highlightColor : normalColor;
            if (sentenceRects.Count > i && sentenceRects[i] != null)
                sentenceRects[i].localScale = Vector3.one;
        }
        if (activeIndex >= 0 && activeIndex < sentenceRects.Count && sentenceRects[activeIndex] != null)
            pulseCo = StartCoroutine(PulseLine(sentenceRects[activeIndex]));
    }

    void ResetHighlights() => ApplyHighlight(-1);

    IEnumerator PulseLine(RectTransform rt)
    {
        float speed = 1.6f, min = 1.00f, max = 1.035f;
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
            float s = popCurve.Evaluate(Mathf.Clamp01(elapsed / popDuration));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // One-shot quick bounce (for sentence labels when they highlight)
    IEnumerator QuickBounce(RectTransform rt)
    {
        if (rt == null) yield break;
        float dur = 0.25f, elapsed = 0f;
        AnimationCurve c = new AnimationCurve(
            new Keyframe(0f,    1f,    0f,  4f),
            new Keyframe(0.5f,  1.08f, 0f,  0f),
            new Keyframe(1f,    1f,    0f,  0f));
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = c.Evaluate(Mathf.Clamp01(elapsed / dur));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    void SetScale(RectTransform rt, float s)
    {
        if (rt != null) rt.localScale = new Vector3(s, s, 1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  UTILITIES
    // ─────────────────────────────────────────────────────────────────────
    void PlaySFX(AudioClip clip)
    {
        if (sfxAudio != null && clip != null)
            sfxAudio.PlayOneShot(clip);
    }

    void ReplayFromStart()
    {
        if (phaseB) return;
        StopAllCoroutines();
        if (dialogueAudio != null) dialogueAudio.Stop();
        ResetHighlights();
        if (illustrationImage != null && bwSprite != null)
            illustrationImage.sprite = bwSprite;
        StartCoroutine(PlaySequence());
    }

    void OnNextClicked()
    {
        StopAllCoroutines();
        if (dialogueAudio != null) dialogueAudio.Stop();
        if (panel != null && unitButton != null) panel.UnitFinished(unitButton);
        else gameObject.SetActive(false);
    }

    public void OnBackClicked()
    {
        StopAllCoroutines();
        if (dialogueAudio != null) dialogueAudio.Stop();
        gameObject.SetActive(false);
        if (panel != null) panel.gameObject.SetActive(true);
    }
}