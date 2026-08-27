using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class MascotActionData_DoWhatISay_BB2
{
    [Tooltip("Animation frames for this mascot's action this round. One sprite = static pose. Several = looping cycle.")]
    public Sprite[] actionFrames = new Sprite[1];
    [Tooltip("True if this mascot is doing the CALLED action this round")]
    public bool isCorrectAction;
}

[System.Serializable]
public class RoundData_DoWhatISay_BB2
{
    [Tooltip("The spoken command, e.g. 'Clap your hands!' — also replayed as the wrong-tap hint")]
    public AudioClip commandAudio;
    [Tooltip("Exactly 3 mascots for this round — one must have isCorrectAction = true")]
    public MascotActionData_DoWhatISay_BB2[] mascots = new MascotActionData_DoWhatISay_BB2[3];
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Do What I Say — DoWhatISay_BB2.
/// A command is called; 3 FIXED mascot slots each loop a different action
/// animation at once (sprite-frame cycling — no Animator). Student taps
/// the mascot doing the called action. Correct: that mascot's loop stops
/// on a bounce/cheer pulse, chime plays. Wrong: that mascot wobbles, the
/// command replays as a hint, no penalty, retry within the same round.
/// Fires OnFinished after 8 rounds.
/// </summary>
public class DoWhatISay_BB2 : MonoBehaviour
{
    [Header("Rounds — 8, IN ORDER")]
    public RoundData_DoWhatISay_BB2[] rounds = new RoundData_DoWhatISay_BB2[8];

    [Header("UI — Mascot Slots (fixed, 3)")]
    public Button[] mascotButtons = new Button[3];
    public Image[]  mascotImages  = new Image[3];

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'Listen again — do what I say!'")]
    public AudioClip   wrongTapHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Tap Feedback SFX")]
    [Tooltip("Played on a correct tap. Leave empty to fall back to AudioManager's default correct sound.")]
    public AudioClip correctTapSfx;
    [Tooltip("Played on a wrong tap. Leave empty to fall back to AudioManager's default wrong sound.")]
    public AudioClip wrongTapSfx;

    [Header("Pop FX")]
    public AudioClip mascotsPopSfx;

    [Header("Animation")]
    [Tooltip("Seconds between frames when a mascot has more than one action frame")]
    [SerializeField] private float frameCycleInterval = 0.3f;

    [Header("Timing")]
    [SerializeField] private float delayAfterCorrect       = 0.8f;
    [SerializeField] private float delayBeforeNextButton   = 0.6f;
    [SerializeField] private float popInDuration            = 0.35f;
    [SerializeField] private float popOutDuration           = 0.2f;
    [SerializeField] private float beatBeforeCommand        = 0.25f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;
    private Coroutine[] _mascotLoopRoutines;

    void Awake()
    {
        _mascotLoopRoutines = new Coroutine[mascotButtons.Length];
        for (int i = 0; i < mascotButtons.Length; i++)
        {
            int capturedIndex = i;
            if (mascotButtons[i] != null)
            {
                mascotButtons[i].onClick.RemoveAllListeners();
                mascotButtons[i].onClick.AddListener(() => OnMascotTapped(capturedIndex));
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[DoWhatISay_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        foreach (var btn in mascotButtons)
            SetScaleZero(btn != null ? btn.GetComponent<RectTransform>() : null);
        SetMascotsInteractable(false);

        StartCoroutine(IntroThenLoadRound(0));

        Debug.Log("[DoWhatISay_BB2] RestartGame — starting from round 0");
    }

    private IEnumerator IntroThenLoadRound(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadRoundSequence(index, isFirstLoad: true));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Round sequence: pop mascots in → play command → start looping
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadRoundSequence(int index, bool isFirstLoad)
    {
        SetMascotsInteractable(false);
        StopAllMascotLoops();

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutMascots());

        var data = rounds[index];
        for (int i = 0; i < mascotImages.Length && i < data.mascots.Length; i++)
        {
            var frames = data.mascots[i].actionFrames;
            if (mascotImages[i] != null && frames != null && frames.Length > 0)
                mascotImages[i].sprite = frames[0];
        }

        if (mascotsPopSfx != null) AudioManager.Instance?.PlaySFX(mascotsPopSfx);
        var routines = new List<Coroutine>();
        foreach (var btn in mascotButtons)
            if (btn != null) routines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;

        yield return new WaitForSeconds(beatBeforeCommand);

        if (dialogueAudioSource != null && data.commandAudio != null)
        {
            dialogueAudioSource.clip = data.commandAudio;
            dialogueAudioSource.Play();
        }

        StartAllMascotLoops(data);
        SetMascotsInteractable(true);
    }

    private IEnumerator PopOutMascots()
    {
        var routines = new List<Coroutine>();
        foreach (var btn in mascotButtons)
            if (btn != null) routines.Add(StartCoroutine(PopOut(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Mascot animation looping (sprite-frame cycling, no Animator)
    // ════════════════════════════════════════════════════════════════════

    private void StartAllMascotLoops(RoundData_DoWhatISay_BB2 data)
    {
        for (int i = 0; i < mascotImages.Length && i < data.mascots.Length; i++)
        {
            var frames = data.mascots[i].actionFrames;
            if (frames == null || frames.Length < 2 || mascotImages[i] == null) continue;
            _mascotLoopRoutines[i] = StartCoroutine(LoopMascotFrames(i, frames));
        }
    }

    private IEnumerator LoopMascotFrames(int index, Sprite[] frames)
    {
        int frameIndex = 0;
        while (true)
        {
            mascotImages[index].sprite = frames[frameIndex];
            frameIndex = (frameIndex + 1) % frames.Length;
            yield return new WaitForSeconds(frameCycleInterval);
        }
    }

    private void StopAllMascotLoops()
    {
        for (int i = 0; i < _mascotLoopRoutines.Length; i++)
        {
            if (_mascotLoopRoutines[i] != null) StopCoroutine(_mascotLoopRoutines[i]);
            _mascotLoopRoutines[i] = null;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnMascotTapped(int index)
    {
        var data = rounds[_currentIndex];
        if (index >= data.mascots.Length) return;

        if (data.mascots[index].isCorrectAction)
            StartCoroutine(HandleCorrectTap(index));
        else
            StartCoroutine(HandleWrongTap(index));
    }

    private IEnumerator HandleCorrectTap(int index)
    {
        SetMascotsInteractable(false);
        AudioManager.Instance?.PlaySFX(correctTapSfx != null ? correctTapSfx : AudioManager.Instance.sfxCorrect);

        // Stop just the winning mascot's loop, others keep going briefly for a beat.
        if (_mascotLoopRoutines[index] != null)
        {
            StopCoroutine(_mascotLoopRoutines[index]);
            _mascotLoopRoutines[index] = null;
        }

        var rect = mascotButtons[index] != null ? mascotButtons[index].GetComponent<RectTransform>() : null;
        if (rect != null)
        {
            yield return StartCoroutine(CheerBounce(rect));
            VFXManager.Instance?.SpawnCorrectBurst(rect);
        }

        StopAllMascotLoops();

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < rounds.Length)
            StartCoroutine(LoadRoundSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllRoundsComplete());
    }

    private IEnumerator HandleWrongTap(int index)
    {
        AudioManager.Instance?.PlaySFX(wrongTapSfx != null ? wrongTapSfx : AudioManager.Instance.sfxWrong);

        var rect = mascotButtons[index] != null ? mascotButtons[index].GetComponent<RectTransform>() : null;
        if (rect != null)
            yield return StartCoroutine(WobbleButton(rect));

        var data = rounds[_currentIndex];
        if (dialogueAudioSource != null && data.commandAudio != null)
        {
            dialogueAudioSource.clip = data.commandAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.commandAudio.length);
        }

        if (dialogueAudioSource != null && wrongTapHintClip != null)
        {
            dialogueAudioSource.clip = wrongTapHintClip;
            dialogueAudioSource.Play();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator CheerBounce(RectTransform t)
    {
        Vector3 original = t.localScale;
        float e = 0f, dur = 0.4f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float bounce = Mathf.Sin(e / dur * Mathf.PI) * 0.25f;
            t.localScale = original * (1f + bounce);
            yield return null;
        }
        t.localScale = original;
    }

    private IEnumerator WobbleButton(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 originalScale = t.localScale;
        float e = 0f, dur = 0.3f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float wobble = Mathf.Sin(e * Mathf.PI * 8f) * 0.08f * (1f - e / dur);
            t.localScale = originalScale * (1f + wobble);
            yield return null;
        }
        t.localScale = originalScale;
    }

    private IEnumerator PopIn(RectTransform t)
    {
        if (t == null) yield break;
        t.localScale = Vector3.zero;
        float e = 0f;
        while (e < popInDuration)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, EaseOutBack(e / popInDuration));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private IEnumerator PopOut(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 start = t.localScale;
        float e = 0f;
        while (e < popOutDuration)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.Lerp(start, Vector3.zero, e / popOutDuration);
            yield return null;
        }
        t.localScale = Vector3.zero;
    }

    private static void SetScaleZero(RectTransform t)
    {
        if (t != null) t.localScale = Vector3.zero;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllRoundsComplete()
    {
        SetMascotsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxGameComplete);
        VFXManager.Instance?.SpawnConfetti();

        if (dialogueAudioSource != null && outroAudioClip != null)
        {
            dialogueAudioSource.clip = outroAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(outroAudioClip.length);
        }
        else
        {
            yield return new WaitForSeconds(delayBeforeNextButton);
        }

        nextButton?.gameObject.SetActive(true);
    }

    private void SetMascotsInteractable(bool value)
    {
        foreach (var btn in mascotButtons)
            if (btn != null) btn.interactable = value;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Next button — wire this to the Button's OnClick() in the Inspector
    // ════════════════════════════════════════════════════════════════════

    public void OnNextButtonPressed()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
        OnFinished?.Invoke();
    }
}
