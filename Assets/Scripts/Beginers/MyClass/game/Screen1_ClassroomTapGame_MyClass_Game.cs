using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SCREEN 1 — CLASSROOM TAP GAME
/// Attach to a GameObject called "GameManager" in your scene.
///
/// ── SCENE SETUP ──────────────────────────────────────────────────
/// 1. Create a Canvas (Screen Space – Overlay, ref resolution 1080×1920).
/// 2. Create two child GameObjects:
///      ClassroomSceneRoot — holds blackboard, chair, desk, window, ruler sprites
///      DeskSceneRoot      — holds pencil, eraser, notebook, sharpener sprites
///    Each sprite needs: Image component + TappableObject_MyClass_Game script.
///    No Button component needed — TappableObject handles tap detection itself.
/// 3. Assign all serialized fields below in the Inspector.
/// 4. Drop AudioClips into the matching slots.
/// 5. Wire the NextButton's onClick → GameManager.OnNextButtonPressed().
///
/// ── ROUND FLOW ───────────────────────────────────────────────────
/// Round 1 — classroom — tap Blackboard
/// Round 2 — classroom — tap Chair
/// Round 3 — desk      — tap Sharpener
/// Round 4 — desk      — tap Notebook
/// Round 5 — classroom — tap Ruler  (SPEED ROUND — timer bar visible)
///
/// After all rounds: "Amazing work!" + Next button → loads Screen 2.
///
/// ── IMPORTANT — SCENE NAME ───────────────────────────────────────
/// The field  nextSceneName  (default "Screen2") must exactly match
/// the scene name you added to File > Build Settings > Scenes In Build.
/// Change it in the Inspector if your scene has a different name.
///
/// ── BUG FIXES ────────────────────────────────────────────────────
/// • nextSceneName is now an Inspector field so you never need to
///   edit code to point to Screen 2.
/// • BeginRound null-guards every obj reference before calling
///   ResetState / wiring OnTapped (belt-and-suspenders alongside
///   the Init() fix in TappableObject_MyClass_Game).
/// </summary>
public class Screen1_ClassroomTapGame_MyClass_Game : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────

    [Header("── PANEL NAVIGATION ──")]
    [Tooltip("Assign the GameFlowManager_MyClass_Game in the scene")]
    public GameFlowManager_MyClass_Game flowManager;

    [Header("── SCENE ROOTS ──")]
    [Tooltip("Parent GameObject that holds Round 1+2+5 (full classroom) objects")]
    public GameObject classroomSceneRoot;
    [Tooltip("Parent GameObject that holds Round 3+4 (desk close-up) objects")]
    public GameObject deskSceneRoot;

    [Header("── TAPPABLE OBJECTS ──")]
    public TappableObject_MyClass_Game obj_Blackboard;
    public TappableObject_MyClass_Game obj_Chair;
    public TappableObject_MyClass_Game obj_Desk;
    public TappableObject_MyClass_Game obj_Window;
    public TappableObject_MyClass_Game obj_Pencil;
    public TappableObject_MyClass_Game obj_Eraser;
    public TappableObject_MyClass_Game obj_Notebook;
    public TappableObject_MyClass_Game obj_Sharpener;
    public TappableObject_MyClass_Game obj_Ruler;

    [Header("── UI ELEMENTS ──")]
    public TextMeshProUGUI instructionLabel;
    public GameObject      timerBarRoot;     // hide/show this whole panel
    public Image           timerBarFill;     // Image Type=Filled, Fill Method=Horizontal
    public GameObject      nextButton;       // shown after Round 5 completes

    [Header("── AUDIO ──")]
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Space]
    public AudioClip voice_TouchBlackboard;
    public AudioClip voice_TouchChair;
    public AudioClip voice_TouchSharpener;
    public AudioClip voice_TouchNotebook;
    public AudioClip voice_TouchRuler;
    public AudioClip voice_Correct;
    public AudioClip voice_Wrong;

    [Space]
    public AudioClip sfx_Pop;
    public AudioClip sfx_Tap;
    public AudioClip sfx_CorrectBounce;
    public AudioClip sfx_WrongShake;
    public AudioClip sfx_RoundTransition;
    public AudioClip sfx_TimerTick;

    [Header("── TIMING ──")]
    [Tooltip("Seconds before the next round starts after a correct tap")]
    public float postCorrectPause   = 1.2f;
    [Tooltip("Show hint after this many wrong taps (1 = first wrong tap shows hint)")]
    public int   hintAfterWrongTaps = 1;
    [Tooltip("Speed round timer duration in seconds")]
    public float speedRoundDuration = 12f;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────

    private int   currentRound  = 0;
    private int   wrongTapCount = 0;
    private bool  roundActive   = false;
    private float timerElapsed  = 0f;
    private bool  speedRound    = false;
    private bool  tickPlayed    = false;

    // ─────────────────────────────────────────────────────────────
    //  ROUND DATA
    // ─────────────────────────────────────────────────────────────

    private struct RoundData
    {
        public TappableObject_MyClass_Game   target;
        public TappableObject_MyClass_Game[] allObjects;
        public AudioClip                     voiceClip;
        public string                        label;
        public bool                          isSpeedRound;
    }

    private RoundData[] rounds;

    // ─────────────────────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    // ── set by ResetAndStart(), consumed by OnEnable ──
    private bool pendingStart = false;

    void Start()
    {
        // First activation is handled by OnEnable which fires right after Start.
        // Nothing needed here.
    }

    void OnEnable()
    {
        // Fired every time the panel is activated (SetActive(true)).
        // GameFlowManager calls ResetAndStart() immediately after SetActive,
        // but we also handle the very first activation here via pendingStart.
        if (pendingStart)
        {
            pendingStart = false;
            StartCoroutine(BeginRound(0));


        }

        ResetAndStart();
    }

    /// <summary>
    /// Fully resets all state and queues a fresh start from Round 0.
    /// Called by GameFlowManager AFTER SetActive(true) so the panel is
    /// guaranteed active and StartCoroutine will not be swallowed.
    /// </summary>
    public void ResetAndStart()
    {
        StopAllCoroutines();

        currentRound  = 0;
        wrongTapCount = 0;
        roundActive   = false;
        timerElapsed  = 0f;
        speedRound    = false;
        tickPlayed    = false;

        BuildRounds();
        nextButton.SetActive(false);
        timerBarRoot.SetActive(false);
        ResetAllTappables();

        // Panel is active at this point (GameFlowManager guarantees it),
        // so StartCoroutine is safe to call directly.
        StartCoroutine(BeginRound(0));
    }

    void ResetAllTappables()
    {
        TappableObject_MyClass_Game[] all = {
            obj_Blackboard, obj_Chair, obj_Desk, obj_Window,
            obj_Pencil, obj_Eraser, obj_Notebook, obj_Sharpener, obj_Ruler
        };
        foreach (var obj in all)
        {
            if (obj != null) obj.ResetState();
        }
    }

    void Update()
    {
        if (!speedRound || !roundActive) return;

        timerElapsed += Time.deltaTime;
        float fraction = 1f - Mathf.Clamp01(timerElapsed / speedRoundDuration);
        timerBarFill.fillAmount = fraction;
        timerBarFill.color = Color.Lerp(Color.red, new Color(0.2f, 0.9f, 0.3f), fraction);

        if (fraction < (3f / speedRoundDuration) && !tickPlayed)
        {
            tickPlayed = true;
            PlaySFX(sfx_TimerTick);
            StartCoroutine(ResetTickFlag());
        }
    }

    IEnumerator ResetTickFlag()
    {
        yield return new WaitForSeconds(1f);
        tickPlayed = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  ROUND SETUP
    // ─────────────────────────────────────────────────────────────

    void BuildRounds()
    {
        TappableObject_MyClass_Game[] classroomAll =
            { obj_Blackboard, obj_Chair, obj_Desk, obj_Window, obj_Ruler };

        TappableObject_MyClass_Game[] deskAll =
            { obj_Pencil, obj_Eraser, obj_Notebook, obj_Sharpener };

        rounds = new RoundData[]
        {
            new RoundData { target=obj_Blackboard, allObjects=classroomAll, voiceClip=voice_TouchBlackboard, label="Touch the BLACKBOARD!", isSpeedRound=false },
            new RoundData { target=obj_Chair,      allObjects=classroomAll, voiceClip=voice_TouchChair,      label="Touch the CHAIR!",       isSpeedRound=false },
            new RoundData { target=obj_Sharpener,  allObjects=deskAll,      voiceClip=voice_TouchSharpener,  label="Touch the SHARPENER!",   isSpeedRound=false },
            new RoundData { target=obj_Notebook,   allObjects=deskAll,      voiceClip=voice_TouchNotebook,   label="Touch the NOTEBOOK!",    isSpeedRound=false },
            new RoundData { target=obj_Ruler,      allObjects=classroomAll, voiceClip=voice_TouchRuler,      label="Touch the RULER!",       isSpeedRound=true  },
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  ROUND FLOW
    // ─────────────────────────────────────────────────────────────

    IEnumerator BeginRound(int index)
    {
        currentRound  = index;
        wrongTapCount = 0;
        roundActive   = false;
        speedRound    = false;
        tickPlayed    = false;

        RoundData round = rounds[index];

        bool isDesk = (index == 2 || index == 3);
        classroomSceneRoot.SetActive(!isDesk);
        deskSceneRoot.SetActive(isDesk);

        if (round.isSpeedRound)
        {
            speedRound              = true;
            timerElapsed            = 0f;
            timerBarRoot.SetActive(true);
            timerBarFill.fillAmount = 1f;
            timerBarFill.color      = new Color(0.2f, 0.9f, 0.3f);
        }
        else
        {
            timerBarRoot.SetActive(false);
        }

        // Reset all tappable objects — null-guard so an unassigned Inspector
        // slot doesn't crash the coroutine (Init() inside ResetState is the
        // deeper fix; this is belt-and-suspenders)
        foreach (var obj in round.allObjects)
        {
            if (obj == null) continue;
            obj.ResetState();
        }

        // Wire tap callbacks with a local capture to avoid closure bug
        foreach (var obj in round.allObjects)
        {
            if (obj == null) continue;
            TappableObject_MyClass_Game captured = obj;
            captured.OnTapped = () => OnObjectTapped(captured);
        }

        PlaySFX(sfx_RoundTransition);
        yield return SceneTransitionFlash();

        instructionLabel.text = round.label;
        StartCoroutine(LabelBounce(instructionLabel.transform));

        yield return new WaitForSeconds(0.3f);
        PlayVoice(round.voiceClip);

        yield return new WaitForSeconds(0.8f);
        roundActive = true;
    }

    // ─────────────────────────────────────────────────────────────
    //  TAP HANDLING
    // ─────────────────────────────────────────────────────────────

    void OnObjectTapped(TappableObject_MyClass_Game tapped)
    {
        if (!roundActive) return;

        PlaySFX(sfx_Tap);
        RoundData round = rounds[currentRound];

        if (tapped == round.target)
        {
            roundActive = false;
            speedRound  = false;
            StartCoroutine(HandleCorrect(tapped));
        }
        else
        {
            wrongTapCount++;
            StartCoroutine(HandleWrong(tapped, round.target));
        }
    }

    IEnumerator HandleCorrect(TappableObject_MyClass_Game obj)
    {
        PlaySFX(sfx_CorrectBounce);
        obj.PlayCorrectAnim();

        yield return new WaitForSeconds(0.35f);
        PlayVoice(voice_Correct);

        yield return new WaitForSeconds(postCorrectPause);

        int next = currentRound + 1;
        if (next < rounds.Length)
            StartCoroutine(BeginRound(next));
        else
            OnAllRoundsComplete();
    }

    IEnumerator HandleWrong(TappableObject_MyClass_Game wrongObj, TappableObject_MyClass_Game correctObj)
    {
        PlaySFX(sfx_WrongShake);
        wrongObj.PlayWrongAnim();

        yield return new WaitForSeconds(0.2f);
        PlayVoice(voice_Wrong);

        if (wrongTapCount >= hintAfterWrongTaps)
        {
            yield return new WaitForSeconds(0.6f);
            correctObj.PlayHintPulse();
        }
    }

    void OnAllRoundsComplete()
    {
        timerBarRoot.SetActive(false);
        instructionLabel.text = "Amazing work! 🌟";
        StartCoroutine(LabelBounce(instructionLabel.transform));
        StartCoroutine(ShowNextButton());
    }

    IEnumerator ShowNextButton()
    {
        yield return new WaitForSeconds(1f);
        nextButton.SetActive(true);
        StartCoroutine(PopIn(nextButton.transform));
    }

    /// <summary>
    /// Called by the Next button's onClick in the Inspector.
    /// Delegates navigation to GameFlowManager — no scene loading needed.
    /// </summary>
    public void OnNextButtonPressed()
    {
        PlaySFX(sfx_Pop);
        if (flowManager != null)
            flowManager.GoToScreen2();
        else
            Debug.LogError("[Screen1] flowManager is not assigned on " + gameObject.name +
                           ". Assign GameFlowManager_MyClass_Game in the Inspector.");
    }

    // ─────────────────────────────────────────────────────────────
    //  AUDIO HELPERS
    // ─────────────────────────────────────────────────────────────

    void PlaySFX(AudioClip clip)
    {
        if (clip && sfxSource) sfxSource.PlayOneShot(clip);
    }

    void PlayVoice(AudioClip clip)
    {
        if (!clip || !voiceSource) return;
        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    // ─────────────────────────────────────────────────────────────
    //  PURE-CODE ANIMATIONS
    // ─────────────────────────────────────────────────────────────

    IEnumerator LabelBounce(Transform t)
    {
        Vector3 origin = t.localScale;
        float   dur    = 0.35f;
        float   e      = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float s = 1f + 0.22f * Mathf.Sin((e / dur) * Mathf.PI);
            t.localScale = origin * s;
            yield return null;
        }
        t.localScale = origin;
    }

    IEnumerator SceneTransitionFlash()
    {
        yield return new WaitForSeconds(0.15f);
    }

    IEnumerator PopIn(Transform t)
    {
        t.localScale = Vector3.zero;
        float dur = 0.5f, e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float p = e / dur;
            float s = p < 0.7f
                ? Mathf.Lerp(0f,    1.15f, p / 0.7f)
                : Mathf.Lerp(1.15f, 1f,    (p - 0.7f) / 0.3f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}