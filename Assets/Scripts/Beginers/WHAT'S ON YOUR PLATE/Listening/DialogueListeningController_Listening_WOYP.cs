using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// One line of dialogue.
/// </summary>
[System.Serializable]
public class DialogueLine
{
    public enum Speaker { Bobby, Danny }

    [Header("Content")]
    public Speaker speaker;
    [TextArea] public string text;
    public AudioClip voiceClip;          // line audio (boy or girl voice)

    [Header("Special Event (optional)")]
    public bool showPizzaBubble;         // true only for Line 5
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// DialogueListeningController_Listening_WOYP
///
/// GAMEPLAY FLOW:
///   1. Screen loads — both character illustrations slide up.
///   2. Lines 1-7 auto-play one by one with card highlight + speech bubble.
///   3. Student can tap any card to replay that line.
///   4. Snail button  → slow-pitch (0.78) mode toggle.
///   5. Replay button → restart from Line 1.
///   6. After Line 7 → NEXT button appears → calls _panel.UnitFinished.
///
/// SPECIAL MOMENTS:
///   Line 5 (Bobby "I love pizza.") → pizza thought-bubble floats above Bobby.
///
/// INSPECTOR HIERARCHY (suggested):
///   DialogueListeningRoot
///     ├── Background
///     ├── CharacterGroup
///     │     ├── BobbyRoot          ← bobbyRoot  (Animator: Talk)
///     │     │     └── SpeechBubble_Bobby  ← bobbySpeechBubble
///     │     │           └── BobbyBubbleText ← bobbySpeechText
///     │     ├── DannyRoot          ← dannyRoot
///     │     │     └── SpeechBubble_Danny  ← dannySpeechBubble
///     │     │           └── DannyBubbleText ← dannySpeechText
///     │     └── PizzaBubble        ← pizzaBubble (hidden by default)
///     ├── DialoguePanel
///     │     ├── LineCard_0 … LineCard_6 ← lineCards[0..6] (Button)
///     │     │     ├── SpeakerLabel (TMP)  ← lineSpeakerLabels[i]
///     │     │     └── LineText     (TMP)  ← lineTexts[i]
///     ├── SnailButton    ← snailButton
///     ├── ReplayButton   ← replayButton
///     └── NextButton     ← nextButton (hidden until done)
/// </summary>
public class DialogueListeningController_Listening_WOYP : MonoBehaviour, IUnitCompletable
{
    // ── IUnitCompletable ──────────────────────────────────────────────────
    private SharedUnitPanelController _panel;
    private SharedUnitButton          _unitButton;

    public void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button)
    {
        _panel      = panel;
        _unitButton = button;
        StartGame();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — DIALOGUE DATA
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Dialogue Lines (fill 7) ──────────────────────")]
    public DialogueLine[] lines;   // 7 entries

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — DIALOGUE CARD UI
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Dialogue Cards ───────────────────────────────")]
    [Tooltip("One Button per dialogue line (7 total). Each is a tappable card.")]
    public Button[]   lineCards;           // 7 cards

    [Tooltip("TMP_Text showing 'Bobby:' or 'Danny:' on each card.")]
    public TMP_Text[] lineSpeakerLabels;   // 7 labels

    [Header("── Card Colors ─────────────────────────────────")]
    public Color cardNormalColor    = new Color(0.95f, 0.95f, 0.95f, 1f);
    public Color cardHighlightBobby = new Color(0.68f, 0.85f, 1f,   1f);  // light blue
    public Color cardHighlightDanny = new Color(1f,   0.72f, 0.72f, 1f);  // light red/pink
    public Color cardHighlightText  = new Color(0.1f, 0.1f,  0.1f, 1f);

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — CHARACTER UI
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Characters ──────────────────────────────────")]
    [Tooltip("Root GameObject of Bobby (has Animator: Idle, Talk, SlowTalk).")]
    public GameObject  bobbyRoot;
    public Animator    bobbyAnimator;

    [Tooltip("Root GameObject of Danny (has Animator: Idle, Talk, Wave).")]
    public GameObject  dannyRoot;
    public Animator    dannyAnimator;

    [Header("── Speech Bubbles ──────────────────────────────")]
    public GameObject  bobbySpeechBubble;   // shown while Bobby speaks
    public TMP_Text    bobbySpeechText;
    public GameObject  dannySpeechBubble;   // shown while Danny speaks
    public TMP_Text    dannySpeechText;

    [Header("── Pizza Bubble ────────────────────────────────")]
    [Tooltip("Thought-bubble with pizza image. Floats above Bobby on Line 5.")]
    public GameObject  pizzaBubble;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — BUTTONS
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Buttons ─────────────────────────────────────")]
    public Button      snailButton;
    public Button      replayButton;
    public Button      nextButton;    // hidden until all lines played

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — AUDIO
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Audio ───────────────────────────────────────")]
    public AudioSource dialogueAudio;   // voice lines
    public AudioSource sfxAudio;        // UI sounds

    public AudioClip   sfx_lineHighlight;   // soft tick / pop when card lights up
    public AudioClip   sfx_bubble;          // bubble appear sound
    public AudioClip   sfx_complete;        // celebration when done
    public AudioClip   sfx_buttonTap;       // card tap feedback

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — ANIMATION TIMING
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Timing ──────────────────────────────────────")]
    [Tooltip("Gap (seconds) between the end of a voice clip and the next line starting.")]
    public float voiceGap    = 0.6f;

    [Tooltip("Seconds to wait after Line 7 before showing NEXT button.")]
    public float endDelay    = 1.0f;

    // ═════════════════════════════════════════════════════════════════════
    //  ANIMATOR TRIGGER NAMES  (match your Animator Controller)
    // ═════════════════════════════════════════════════════════════════════
    private const string TRIG_TALK_ON  = "mouthanimationboy";
    private const string TRIG_TALK_OFF = "mouthanimationboyfalse";

    // ═════════════════════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ═════════════════════════════════════════════════════════════════════
    private int  _currentLine = 0;
    private bool _isPlaying   = false;
    private bool _slowMode    = false;
    private bool _allDone     = false;

    // Cached image references for card background tinting
    private Image[] _cardImages;

    // Original canvas positions — captured once in Awake, never overwritten
    private Vector3 _bobbyOriginalPos;
    private Vector3 _dannyOriginalPos;

    // ═════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════
    void Awake()
    {
        // Capture original canvas positions FIRST — before anything moves them
        _bobbyOriginalPos = bobbyRoot ? bobbyRoot.transform.localPosition : Vector3.zero;
        _dannyOriginalPos = dannyRoot ? dannyRoot.transform.localPosition : Vector3.zero;

        // Cache card background images
        _cardImages = new Image[lineCards.Length];
        for (int i = 0; i < lineCards.Length; i++)
        {
            if (lineCards[i] != null)
                _cardImages[i] = lineCards[i].GetComponent<Image>();
        }

        // Wire up card tap callbacks — capture index for closure
        for (int i = 0; i < lineCards.Length; i++)
        {
            int idx = i;
            lineCards[i].onClick.AddListener(() => OnCardTapped(idx));
        }

        snailButton  .onClick.AddListener(OnSnail);
        replayButton .onClick.AddListener(OnReplay);
        nextButton   .onClick.AddListener(OnNext);
    }

    void Start()
    {
        // If not launched via IUnitCompletable, auto-start in standalone testing
        if (_panel == null) StartGame();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  START GAME
    // ═════════════════════════════════════════════════════════════════════
    void StartGame()
    {
        ResetUI();
        StartCoroutine(CharactersSlideIn());
    }

    void ResetUI()
    {
        _currentLine = 0;
        _allDone     = false;
        _isPlaying   = false;

        // Hide bubbles
        SetBubble(false, false);
        if (pizzaBubble) pizzaBubble.SetActive(false);

        // Hide next button — reset scale first so it never stays at zero
        if (nextButton)
        {
            nextButton.transform.localScale = Vector3.one;
            nextButton.gameObject.SetActive(false);
        }

        // Reset all card scales to one and colors to normal
        for (int i = 0; i < lineCards.Length; i++)
        {
            if (lineCards[i] != null)
                lineCards[i].transform.localScale = Vector3.one;
            SetCardHighlight(i, false);
        }

        // Populate speaker labels from data
        for (int i = 0; i < lines.Length && i < lineCards.Length; i++)
        {
            if (lineSpeakerLabels[i] != null)
                lineSpeakerLabels[i].text = lines[i].speaker == DialogueLine.Speaker.Bobby
                    ? "Bobby:" : "Danny:";
        }

        // Stop any mouth animation on reset
        bobbyAnimator?.SetTrigger(TRIG_TALK_OFF);
        dannyAnimator?.SetTrigger(TRIG_TALK_OFF);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CHARACTER SLIDE-IN INTRO
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator CharactersSlideIn()
    {
        // Slide both characters up from below — always target the original canvas position
        Vector3 bobbyShow = _bobbyOriginalPos;
        Vector3 dannyShow = _dannyOriginalPos;
        Vector3 bobbyHide = bobbyShow + Vector3.down * 300f;
        Vector3 dannyHide = dannyShow + Vector3.down * 300f;

        if (bobbyRoot) bobbyRoot.transform.localPosition = bobbyHide;
        if (dannyRoot) dannyRoot.transform.localPosition = dannyHide;

        float t = 0f, dur = 0.6f;
        while (t < dur)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / dur);
            if (bobbyRoot) bobbyRoot.transform.localPosition = Vector3.Lerp(bobbyHide, bobbyShow, p);
            if (dannyRoot) dannyRoot.transform.localPosition = Vector3.Lerp(dannyHide, dannyShow, p);
            t += Time.deltaTime;
            yield return null;
        }
        if (bobbyRoot) bobbyRoot.transform.localPosition = bobbyShow;
        if (dannyRoot) dannyRoot.transform.localPosition = dannyShow;

        yield return new WaitForSeconds(0.3f);
        StartCoroutine(PlayAllLines());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  AUTO-PLAY ALL LINES SEQUENTIALLY
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator PlayAllLines()
    {
        _isPlaying = true;
        for (int i = 0; i < lines.Length; i++)
        {
            _currentLine = i;
            yield return StartCoroutine(PlayLine(i));
        }
        _isPlaying = false;
        _allDone   = true;

        yield return new WaitForSeconds(endDelay);
        ShowNextButton();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PLAY A SINGLE LINE
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator PlayLine(int idx)
    {
        DialogueLine line = lines[idx];
        bool isBobby = (line.speaker == DialogueLine.Speaker.Bobby);

        // ── 1. Highlight card ──────────────────────────────────────────
        PlaySFX(sfx_lineHighlight);
        yield return StartCoroutine(SpringPopCard(idx));
        SetCardHighlight(idx, true);

        // ── 2. Show speech bubble ──────────────────────────────────────
        PlaySFX(sfx_bubble);
        if (isBobby)
        {
            if (bobbySpeechText)  bobbySpeechText.text = line.text;
            SetBubble(true, false);
            yield return StartCoroutine(BubblePopIn(bobbySpeechBubble));
        }
        else
        {
            if (dannySpeechText) dannySpeechText.text = line.text;
            SetBubble(false, true);
            yield return StartCoroutine(BubblePopIn(dannySpeechBubble));
        }

        // ── 3. Trigger mouth ON + play audio at the same time ────────
        Animator speakingAnimator = isBobby ? bobbyAnimator : dannyAnimator;
        speakingAnimator?.SetTrigger(TRIG_TALK_ON);

        if (line.voiceClip != null)
        {
            dialogueAudio.pitch = _slowMode ? 0.78f : 1f;
            dialogueAudio.clip  = line.voiceClip;
            dialogueAudio.Play();
        }

        // ── 4. Special events mid-line ────────────────────────────────
        if (line.showPizzaBubble && pizzaBubble)
        {
            pizzaBubble.SetActive(true);
            StartCoroutine(FloatIn(pizzaBubble));
        }

        // ── 5. Poll until audio actually finishes — most accurate ─────
        yield return null; // one frame for isPlaying to become true
        yield return new WaitUntil(() => !dialogueAudio.isPlaying);
        speakingAnimator?.SetTrigger(TRIG_TALK_OFF);

        // ── 6. Gap before next line ───────────────────────────────────
        yield return new WaitForSeconds(voiceGap);

        // ── 7. Hide bubble, un-highlight card ────────────────────────
        SetBubble(false, false);
        if (line.showPizzaBubble && pizzaBubble)
        {
            yield return new WaitForSeconds(0.5f);
            pizzaBubble.SetActive(false);
        }

        // Keep card highlighted in a softer tone (already played)
        SetCardHighlight(idx, false);
        SetCardPlayed(idx);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CARD TAP — REPLAY THAT LINE
    // ═════════════════════════════════════════════════════════════════════
    void OnCardTapped(int idx)
    {
        if (_isPlaying) return;   // ignore taps during auto-play
        PlaySFX(sfx_buttonTap);
        StopAllCoroutines();
        _isPlaying = true;
        StartCoroutine(ReplayLine(idx));
    }

    IEnumerator ReplayLine(int idx)
    {
        _currentLine = idx;
        yield return StartCoroutine(PlayLine(idx));
        _isPlaying = false;

        // Restore next button if already done
        if (_allDone) ShowNextButton();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BUTTON HANDLERS
    // ═════════════════════════════════════════════════════════════════════
    void OnSnail()
    {
        _slowMode = !_slowMode;
        dialogueAudio.pitch = _slowMode ? 0.78f : 1f;

        // Visual toggle on snail icon
        if (snailButton != null)
            snailButton.image.color = _slowMode
                ? new Color(0.5f, 1f, 0.5f, 1f)   // green tint = active
                : Color.white;
    }

    void OnReplay()
    {
        if (_isPlaying) return;
        StopAllCoroutines();
        ResetUI();
        StartCoroutine(PlayAllLines());
    }

    void OnNext()
    {
        // Completion flow — hand off to unit panel
        _panel?.UnitFinished(_unitButton);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  UI HELPERS
    // ═════════════════════════════════════════════════════════════════════
    void SetCardHighlight(int idx, bool active)
    {
        if (idx < 0 || idx >= _cardImages.Length || _cardImages[idx] == null) return;
        if (active)
        {
            bool isBobby = lines[idx].speaker == DialogueLine.Speaker.Bobby;
            _cardImages[idx].color = isBobby ? cardHighlightBobby : cardHighlightDanny;
        }
        else
        {
            _cardImages[idx].color = cardNormalColor;
        }
    }

    /// <summary>Dim the card slightly to show it has already played.</summary>
    void SetCardPlayed(int idx)
    {
        if (idx < 0 || idx >= _cardImages.Length || _cardImages[idx] == null) return;
        _cardImages[idx].color = new Color(0.88f, 0.88f, 0.88f, 1f);
    }

    void SetBubble(bool bobby, bool danny)
    {
        if (bobbySpeechBubble) bobbySpeechBubble.SetActive(bobby);
        if (dannySpeechBubble) dannySpeechBubble.SetActive(danny);
    }

    void ShowNextButton()
    {
        PlaySFX(sfx_complete);
        if (nextButton)
        {
            nextButton.transform.localScale = Vector3.one; // reset before SpringPop touches it
            nextButton.gameObject.SetActive(true);
            StartCoroutine(SpringPop(nextButton.transform, 0.4f));
        }
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxAudio != null)
            sfxAudio.PlayOneShot(clip);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  ANIMATION COROUTINES
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Gentle pulse on the card — never zeros the scale.</summary>
    IEnumerator SpringPopCard(int idx)
    {
        if (idx < 0 || idx >= lineCards.Length || lineCards[idx] == null) yield break;
        Transform t = lineCards[idx].transform;
        t.localScale = Vector3.one; // ensure starting from one
        float elapsed = 0f, dur = 0.2f;
        while (elapsed < dur)
        {
            float p = elapsed / dur;
            float s = p < 0.5f
                ? Mathf.Lerp(1f, 1.06f, p / 0.5f)
                : Mathf.Lerp(1.06f, 1f, (p - 0.5f) / 0.5f);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    /// <summary>Reusable spring-pop: scale 0 → 1.2 → 1. Always ends at Vector3.one.</summary>
    IEnumerator SpringPop(Transform t, float duration)
    {
        t.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float p = elapsed / duration;
            float s = p < 0.6f
                ? Mathf.SmoothStep(0f, 1.2f, p / 0.6f)
                : Mathf.Lerp(1.2f, 1f, (p - 0.6f) / 0.4f);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one; // always guaranteed
    }

    /// <summary>Bubble pops in with a quick scale tween.</summary>
    IEnumerator BubblePopIn(GameObject bubble)
    {
        if (bubble == null) yield break;
        bubble.SetActive(true);
        Transform t = bubble.transform;
        t.localScale = Vector3.zero;
        float elapsed = 0f, dur = 0.2f;
        while (elapsed < dur)
        {
            float p = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            t.localScale = Vector3.one * p;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    /// <summary>Pizza bubble floats upward into view.</summary>
    IEnumerator FloatIn(GameObject obj)
    {
        if (obj == null) yield break;
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        Transform   t  = obj.transform;
        Vector3 startPos = t.localPosition + Vector3.down * 40f;
        Vector3 endPos   = t.localPosition;
        t.localPosition = startPos;

        float elapsed = 0f, dur = 0.5f;
        while (elapsed < dur)
        {
            float p = Mathf.SmoothStep(0f, 1f, elapsed / dur);
            t.localPosition = Vector3.Lerp(startPos, endPos, p);
            if (cg) cg.alpha = p;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localPosition = endPos;
        if (cg) cg.alpha = 1f;
    }
}