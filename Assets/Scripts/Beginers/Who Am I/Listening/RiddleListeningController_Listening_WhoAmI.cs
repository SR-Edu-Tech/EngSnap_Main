using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// One clue line inside a riddle.
/// </summary>
[System.Serializable]
public class RiddleClue
{
    [TextArea] public string text;
    public AudioClip audio;
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// All data for one riddle card. Fill in Inspector.
/// </summary>
[System.Serializable]
public class RiddleEntry
{
    [Header("Clue Lines")]
    public RiddleClue[] clues;                    // highlighted one by one

    [Header("Question")]
    [TextArea] public string questionText;         // e.g. "Who am I?"
    public AudioClip questionAudio;

    [Header("Answer")]
    public string answerWord;                      // e.g. "MOM"
    public AudioClip answerAudio;                  // played on reveal & on tap
    public Sprite   illustration;                  // revealed when answer appears

    [Header("Hint")]
    public string hintWord;                        // shown when Hint button tapped
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// RiddleListeningController_Listening_WhoAmI
///
/// GAMEPLAY FLOW (per riddle):
///   1. Riddle card slides in. Illustration is blurred/hidden.
///   2. Clue lines highlight yellow one by one as audio plays.
///   3. Question line plays. 2-second thinking pause.
///   4. Illustration un-blurs with sparkle. Answer word appears. Answer audio plays.
///   5. Student can tap the answer banner to re-hear the answer.
///   6. NEXT RIDDLE button appears → advance.
///   7. After all riddles → NEXT button → unit panel.
///
/// BUTTONS:
///   Snail  → slow-pitch replay of the current riddle.
///   Replay → re-play clues + answer for the current riddle.
///   Hint   → reveal hint word for current riddle.
///   Next Riddle (per riddle) / Next (final) → advance.
///
/// INSPECTOR SETUP:
///   - riddleEntries   : fill 4 (or more) entries, one per riddle.
///   - hintLabels      : 4 small TMP labels showing Tree · Car · Giraffe · Mom.
///   - clueTextSlots   : 4-5 TMP_Text objects in the card (for clue lines).
///   - questionSlot    : TMP_Text for the question line.
///   - answerBanner    : Button (tappable) whose child TMP shows the answer word.
///   - illustrationImg : Image; set its material to a blur material when hidden.
///   - sparkleVFX      : optional ParticleSystem played on reveal.
///   - questionMarkObj : the animated '?' GO (just enable/disable — animate via Animator).
///
/// PREFAB / SCENE HIERARCHY (suggested):
///   RiddleListeningRoot
///     ├── QuestionMarkIcon       ← assign to questionMarkObj
///     ├── RiddleCard
///     │     ├── IllustrationImage  ← illustrationImg
///     │     ├── CluePanel
///     │     │     ├── ClueText_0  ← clueTextSlots[0]
///     │     │     ├── ClueText_1
///     │     │     ├── ClueText_2
///     │     │     ├── ClueText_3
///     │     │     └── QuestionText ← questionSlot
///     │     └── AnswerBanner (Button) ← answerBanner
///     │           └── AnswerLabel  ← answerLabel
///     ├── HintPanel
///     │     ├── HintButton        ← hintButton
///     │     └── HintLabel         ← hintLabel (shows hint word)
///     ├── HintRowPanel            ← hintRowPanel (all 4 small labels)
///     │     ├── HintTag_0         ← hintLabels[0]
///     │     ├── HintTag_1
///     │     ├── HintTag_2
///     │     └── HintTag_3
///     ├── SnailButton             ← snailButton
///     ├── ReplayButton            ← replayButton
///     ├── NextRiddleButton        ← nextRiddleButton (hidden until answer revealed)
///     ├── NextButton              ← nextButton       (hidden until all riddles done)
///     └── CompletionPanel        ← completionPanel
/// </summary>
public class RiddleListeningController_Listening_WhoAmI : MonoBehaviour, IUnitCompletable
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
    //  INSPECTOR — RIDDLE DATA
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Riddle Data ──────────────────────────────────")]
    [Tooltip("Fill one entry per riddle (4 riddles for this unit).")]
    public RiddleEntry[] riddleEntries;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — CARD UI
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Riddle Card ─────────────────────────────────")]
    [Tooltip("The Image that shows the illustration. Assign a blur Material to it; " +
             "the script swaps to a clear material on reveal.")]
    public Image          illustrationImg;

    [Tooltip("Clear (non-blur) material used after reveal. Leave null to just swap sprite alpha.")]
    public Material       clearMaterial;

    [Tooltip("Blur/hidden material used before reveal. The image starts with this.")]
    public Material       blurMaterial;

    [Tooltip("TMP_Text slots for clue lines. Make at least as many as your longest riddle (e.g. 4).")]
    public TMP_Text[]     clueTextSlots;

    [Tooltip("TMP_Text for the question line, e.g. 'Who am I?'")]
    public TMP_Text       questionSlot;

    [Tooltip("Button the student taps to re-hear the answer. Make it invisible until reveal.")]
    public Button         answerBanner;

    [Tooltip("TMP_Text child of answerBanner that displays the answer word.")]
    public TMP_Text       answerLabel;

    [Tooltip("Optional ParticleSystem played when the answer is revealed.")]
    public ParticleSystem sparkleVFX;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — HINT
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Hint ────────────────────────────────────────")]
    public Button         hintButton;

    [Tooltip("Label that pops up showing the hint word for the current riddle.")]
    public TMP_Text       hintLabel;

    [Tooltip("Parent panel containing hintLabel — toggled by hintButton.")]
    public GameObject     hintPanel;

    [Tooltip("Row of small static labels showing all answer words (Tree · Car · Giraffe · Mom).")]
    public TMP_Text[]     hintLabels;

    [Tooltip("Parent panel for the hint row labels.")]
    public GameObject     hintRowPanel;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — NAVIGATION BUTTONS
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Buttons ─────────────────────────────────────")]
    public Button         snailButton;
    public Button         replayButton;

    [Tooltip("Shown after each riddle is revealed. Advances to next riddle.")]
    public Button         nextRiddleButton;

    [Tooltip("Shown after ALL riddles complete. Goes to unit panel.")]
    public Button         nextButton;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — MISC UI
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Misc UI ─────────────────────────────────────")]
    [Tooltip("The rotating '?' icon. Enable/disable or drive via Animator.")]
    public GameObject     questionMarkObj;

    [Tooltip("Shown after all riddles are done.")]
    public GameObject     completionPanel;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — AUDIO
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Audio ───────────────────────────────────────")]
    public AudioSource    dialogueAudio;    // clue / question / answer voices
    public AudioSource    sfxAudio;         // UI sounds
    public AudioClip      sfx_cardSlideIn;
    public AudioClip      sfx_clueHighlight;
    public AudioClip      sfx_reveal;
    public AudioClip      sfx_hint;
    public AudioClip      sfx_complete;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — COLORS
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Colors ──────────────────────────────────────")]
    public Color clueNormalColor    = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color clueHighlightColor = new Color(1f,   0.85f, 0f,   1f);  // yellow
    public Color questionColor      = new Color(0.1f, 0.5f,  1f,   1f);  // blue

    // ═════════════════════════════════════════════════════════════════════
    //  PRIVATE STATE
    // ═════════════════════════════════════════════════════════════════════
    private int   _currentRiddle = 0;
    private bool  _isPlaying     = false;
    private bool  _slowMode      = false;
    private bool  _hintVisible   = false;
    private bool  _revealed      = false;   // has current riddle been revealed?

    private float VoiceGap   => _slowMode ? 1.0f : 0.5f;
    private float ThinkPause => _slowMode ? 3.0f : 2.0f;

    // ═════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════
    void Awake()
    {
        // Ensure AudioSources exist
        if (dialogueAudio == null) dialogueAudio = gameObject.AddComponent<AudioSource>();
        if (sfxAudio      == null) sfxAudio      = gameObject.AddComponent<AudioSource>();

        // Button wiring
        if (snailButton)       snailButton.onClick.AddListener(OnSnail);
        if (replayButton)      replayButton.onClick.AddListener(OnReplay);
        if (nextRiddleButton)  nextRiddleButton.onClick.AddListener(OnNextRiddle);
        if (nextButton)        nextButton.onClick.AddListener(OnNext);
        if (hintButton)        hintButton.onClick.AddListener(OnHint);
        if (answerBanner)      answerBanner.onClick.AddListener(OnAnswerTapped);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GAME START
    // ═════════════════════════════════════════════════════════════════════
    void StartGame()
    {
        _currentRiddle = 0;
        _slowMode      = false;
        _isPlaying     = false;

        if (completionPanel) completionPanel.SetActive(false);
        SetHintRowLabels();
        ShowRiddle(_currentRiddle);
    }

    // Populates the small static hint-row labels from riddleEntry.hintWord
    void SetHintRowLabels()
    {
        if (hintLabels == null) return;
        for (int i = 0; i < hintLabels.Length; i++)
        {
            if (hintLabels[i] == null) continue;
            if (i < riddleEntries.Length)
                hintLabels[i].text = riddleEntries[i].hintWord;
            else
                hintLabels[i].gameObject.SetActive(false);
        }
        if (hintRowPanel) hintRowPanel.SetActive(true);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  SHOW RIDDLE  (load UI for riddle[index], then auto-play)
    // ═════════════════════════════════════════════════════════════════════
    void ShowRiddle(int index)
    {
        if (index >= riddleEntries.Length) { ShowCompletion(); return; }

        _revealed = false;

        RiddleEntry entry = riddleEntries[index];

        // Reset illustration
        if (illustrationImg != null)
        {
            illustrationImg.sprite = entry.illustration;
            illustrationImg.material = blurMaterial;
            illustrationImg.color = new Color(1f, 1f, 1f, 0.35f); // dim overlay
        }

        // Reset clue slots
        for (int i = 0; i < clueTextSlots.Length; i++)
        {
            if (clueTextSlots[i] == null) continue;
            if (i < entry.clues.Length)
            {
                clueTextSlots[i].text  = entry.clues[i].text;
                clueTextSlots[i].color = clueNormalColor;
                clueTextSlots[i].gameObject.SetActive(true);
            }
            else
            {
                clueTextSlots[i].gameObject.SetActive(false);
            }
        }

        // Question slot
        if (questionSlot != null)
        {
            questionSlot.text  = entry.questionText;
            questionSlot.color = clueNormalColor;
            questionSlot.gameObject.SetActive(true);
        }

        // Answer banner — hidden until reveal
        if (answerBanner)     answerBanner.gameObject.SetActive(false);
        if (answerLabel)      answerLabel.text = entry.answerWord;

        // Hint
        _hintVisible = false;
        if (hintPanel)  hintPanel.SetActive(false);
        if (hintLabel)  hintLabel.text = entry.hintWord;

        // Buttons
        if (nextRiddleButton) nextRiddleButton.gameObject.SetActive(false);
        if (nextButton)       nextButton.gameObject.SetActive(false);
        if (questionMarkObj)  questionMarkObj.SetActive(true);

        PlaySFX(sfx_cardSlideIn);
        StartCoroutine(PlayRiddle(entry));
    }


    // ═════════════════════════════════════════════════════════════════════
    //  PLAY RIDDLE  — clues highlight one by one, then question, then reveal
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator PlayRiddle(RiddleEntry entry)
    {
        _isPlaying = true;

        yield return new WaitForSeconds(0.3f); // brief pause before clues start

        // ── Clue lines ──────────────────────────────────────────────────
        for (int i = 0; i < entry.clues.Length; i++)
        {
            if (i < clueTextSlots.Length && clueTextSlots[i] != null)
            {
                // Highlight this clue
                clueTextSlots[i].color = clueHighlightColor;
                PlaySFX(sfx_clueHighlight);
            }

            AudioClip clip = entry.clues[i].audio;
            if (clip != null)
            {
                dialogueAudio.pitch = _slowMode ? 0.78f : 1f;
                dialogueAudio.clip  = clip;
                dialogueAudio.Play();
                yield return new WaitForSeconds(clip.length + VoiceGap);
            }
            else
            {
                yield return new WaitForSeconds(1.5f + VoiceGap);
            }

            // Un-highlight after audio
            if (i < clueTextSlots.Length && clueTextSlots[i] != null)
                clueTextSlots[i].color = clueNormalColor;
        }

        // ── Question line ────────────────────────────────────────────────
        if (questionSlot != null) questionSlot.color = questionColor;

        if (entry.questionAudio != null)
        {
            dialogueAudio.pitch = _slowMode ? 0.78f : 1f;
            dialogueAudio.clip  = entry.questionAudio;
            dialogueAudio.Play();
            yield return new WaitForSeconds(entry.questionAudio.length + VoiceGap);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // ── Thinking pause ───────────────────────────────────────────────
        yield return new WaitForSeconds(ThinkPause);

        // ── Reveal ──────────────────────────────────────────────────────
        yield return StartCoroutine(RevealAnswer(entry));

        _isPlaying = false;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  REVEAL ANSWER
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator RevealAnswer(RiddleEntry entry)
    {
        _revealed = true;

        PlaySFX(sfx_reveal);

        // Un-blur illustration
        if (illustrationImg != null)
        {
            // Fade from dim/blurred to full colour
            float elapsed = 0f, duration = 0.5f;
            while (elapsed < duration)
            {
                float a = Mathf.Lerp(0.35f, 1f, elapsed / duration);
                illustrationImg.color = new Color(1f, 1f, 1f, a);
                elapsed += Time.deltaTime;
                yield return null;
            }
            illustrationImg.color    = Color.white;
            illustrationImg.material = clearMaterial;  // swap to clear material
        }

        // Sparkle
        if (sparkleVFX != null) sparkleVFX.Play();

        // Show answer banner with a spring-pop
        if (answerBanner != null)
        {
            answerBanner.gameObject.SetActive(true);
            yield return StartCoroutine(SpringPop(answerBanner.transform, 0.35f));
        }

        // Play answer audio
        if (entry.answerAudio != null)
        {
            dialogueAudio.pitch = _slowMode ? 0.78f : 1f;
            dialogueAudio.clip  = entry.answerAudio;
            dialogueAudio.Play();
            yield return new WaitForSeconds(entry.answerAudio.length + 0.3f);
        }

        // Hide '?' once answer revealed
        if (questionMarkObj) questionMarkObj.SetActive(false);

        // Show appropriate advance button
        bool isLast = (_currentRiddle >= riddleEntries.Length - 1);
        if (isLast)
        {
            if (nextButton)       nextButton.gameObject.SetActive(true);
        }
        else
        {
            if (nextRiddleButton) nextRiddleButton.gameObject.SetActive(true);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  SPRING-POP ANIMATION HELPER
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator SpringPop(Transform t, float duration)
    {
        t.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float p = elapsed / duration;
            float s = p < 0.6f
                ? Mathf.SmoothStep(0f, 1.25f, p / 0.6f)
                : Mathf.Lerp(1.25f, 1f, (p - 0.6f) / 0.4f);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BUTTON HANDLERS
    // ═════════════════════════════════════════════════════════════════════

    // Student taps the answer banner to re-hear
    void OnAnswerTapped()
    {
        if (!_revealed) return;
        RiddleEntry entry = riddleEntries[_currentRiddle];
        if (entry.answerAudio != null)
        {
            dialogueAudio.pitch = _slowMode ? 0.78f : 1f;
            dialogueAudio.clip  = entry.answerAudio;
            dialogueAudio.Play();
        }
        StartCoroutine(SpringPop(answerBanner.transform, 0.2f));
    }

    void OnHint()
    {
        if (_isPlaying) return;

        _hintVisible = !_hintVisible;
        if (hintPanel) hintPanel.SetActive(_hintVisible);
        if (_hintVisible) PlaySFX(sfx_hint);
    }

    void OnSnail()
    {
        _slowMode = !_slowMode;
        dialogueAudio.pitch = _slowMode ? 0.78f : 1f;

        Color c = _slowMode ? Color.green : Color.white;
        if (snailButton) snailButton.image.color = c;
    }

    void OnReplay()
    {
        if (_isPlaying) return;

        StopAllCoroutines();
        _isPlaying = false;

        // Re-show the current riddle from the top
        ShowRiddle(_currentRiddle);
    }

    void OnNextRiddle()
    {
        if (_isPlaying) return;
        _currentRiddle++;
        ShowRiddle(_currentRiddle);
    }

    void OnNext()
    {
        if (_isPlaying) return;
        ShowCompletion();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  COMPLETION
    // ═════════════════════════════════════════════════════════════════════
    void ShowCompletion()
    {
        if (nextButton)       nextButton.gameObject.SetActive(false);
        if (nextRiddleButton) nextRiddleButton.gameObject.SetActive(false);
        if (completionPanel)  completionPanel.SetActive(true);
        PlaySFX(sfx_complete);
    }

    // Called by the Done/Next button inside completionPanel
    public void OnDone()
    {
        if (completionPanel) completionPanel.SetActive(false);
        _panel?.UnitFinished(_unitButton);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  AUDIO HELPER
    // ═════════════════════════════════════════════════════════════════════
    void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxAudio != null)
            sfxAudio.PlayOneShot(clip);
    }
}