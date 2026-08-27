using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class ActionChitData_GuessAction_BB2
{
    [Tooltip("The action word, e.g. 'catch' — shown on the chit only AFTER a correct guess")]
    public string actionLabel;
    [Tooltip("Mime animation frames, played SILENTLY (no audio) when this chit is tapped. One sprite = static pose. Several = looping cycle.")]
    public Sprite[] mimeFrames = new Sprite[1];
    [Tooltip("VO of the word, played once guessed correctly, e.g. 'Catch!'")]
    public AudioClip revealAudio;
    [Tooltip("This word's button tint colour when it appears as a guess option")]
    public Color wordTint = Color.white;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Guess The Action — GuessAction_BB2.
/// 8 FIXED face-down chits sit in a grid. Tapping an unguessed chit plays
/// its mime animation SILENTLY (sprite-frame cycling, no Animator, no
/// word or audio revealed). Then 3 word buttons appear — the correct word
/// plus 2 random distractors from the other 7 words. Correct guess: the
/// word appears on the chit, VO plays, chit locks, chime plays. Wrong
/// guess: word button wobbles, hint plays, mime replays — no penalty,
/// retry the same chit. Fires OnFinished after all 8 chits are guessed.
/// </summary>
public class GuessTheAction_BB2 : MonoBehaviour
{
    [Header("Chits — 8 total")]
    public ActionChitData_GuessAction_BB2[] chits = new ActionChitData_GuessAction_BB2[8];

    [Header("UI — Chit Grid (fixed, 8)")]
    public Button[]   chitButtons = new Button[8];
    public Image[]    chitImages  = new Image[8];   // face-down icon by default, swapped to actionLabel text reveal isn't needed here since chitLabels handles text
    public TMP_Text[] chitLabels  = new TMP_Text[8]; // shows "?" until guessed, then the word

    [Header("UI — Mime Stage")]
    public Image mimeStageImage;

    [Header("UI — Guess Buttons (3, fixed)")]
    public Button[]   guessButtons = new Button[3];
    public TMP_Text[] guessLabels  = new TMP_Text[3];

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong guess, e.g. 'Watch again — what is he doing?'")]
    public AudioClip   wrongGuessHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Tap Feedback SFX")]
    public AudioClip correctGuessSfx;
    public AudioClip wrongGuessSfx;

    [Header("Pop FX")]
    [Tooltip("Played when the 8 face-down chits first pop into the grid")]
    public AudioClip chitPopSfx;
    [Tooltip("Played when the 3 guess buttons pop in after the mime plays")]
    public AudioClip guessButtonPopSfx;

    [Header("Animation")]
    [Tooltip("Seconds between frames when a mime has more than one frame")]
    [SerializeField] private float frameCycleInterval = 0.3f;
    [Tooltip("How long the mime plays before guess buttons appear (ignored once frames loop — this is a minimum watch time)")]
    [SerializeField] private float mimeWatchDuration = 1.5f;

    [Header("Timing")]
    [SerializeField] private float delayAfterCorrect      = 0.8f;
    [SerializeField] private float delayBeforeNextButton  = 0.6f;
    [SerializeField] private float popInDuration           = 0.3f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly bool[] _guessedFlags = new bool[8];
    private int _currentChitIndex = -1;
    private int[] _currentGuessOptionChitIndices = new int[3]; // which chit index each of the 3 guess buttons represents
    private Coroutine _mimeLoopRoutine;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        for (int i = 0; i < chitButtons.Length; i++)
        {
            int capturedIndex = i;
            if (chitButtons[i] != null)
            {
                chitButtons[i].onClick.RemoveAllListeners();
                chitButtons[i].onClick.AddListener(() => OnChitTapped(capturedIndex));
            }
        }
        for (int i = 0; i < guessButtons.Length; i++)
        {
            int capturedIndex = i;
            if (guessButtons[i] != null)
            {
                guessButtons[i].onClick.RemoveAllListeners();
                guessButtons[i].onClick.AddListener(() => OnGuessTapped(capturedIndex));
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
            Debug.LogWarning("[GuessTheAction_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentChitIndex = -1;
        for (int i = 0; i < _guessedFlags.Length; i++) _guessedFlags[i] = false;

        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;
        SetGuessButtonsActive(false);
        if (mimeStageImage != null) mimeStageImage.gameObject.SetActive(false);

        for (int i = 0; i < chitButtons.Length; i++)
        {
            if (chitLabels != null && i < chitLabels.Length && chitLabels[i] != null)
                chitLabels[i].text = "?";
            if (chitButtons[i] != null) chitButtons[i].interactable = true;
            SetScaleZero(chitButtons[i] != null ? chitButtons[i].GetComponent<RectTransform>() : null);
        }

        StartCoroutine(IntroThenRevealChits());

        Debug.Log("[GuessTheAction_BB2] RestartGame — fresh grid of 8 chits");
    }

    private IEnumerator IntroThenRevealChits()
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        var routines = new List<Coroutine>();
        foreach (var btn in chitButtons)
            if (btn != null) routines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Chit tap → silent mime → guess buttons appear
    // ════════════════════════════════════════════════════════════════════

    private void OnChitTapped(int chitIndex)
    {
        if (_guessedFlags[chitIndex]) return;
        StartCoroutine(PlayMimeThenShowGuesses(chitIndex));
    }

    private IEnumerator PlayMimeThenShowGuesses(int chitIndex)
    {
        _currentChitIndex = chitIndex;
        SetAllChitsInteractable(false);
        SetGuessButtonsActive(false);

        yield return StartCoroutine(PlayMime(chitIndex));

        BuildGuessOptions(chitIndex);
        yield return StartCoroutine(ShowGuessButtons());
    }

    private IEnumerator PlayMime(int chitIndex)
    {
        if (mimeStageImage == null) yield break;

        mimeStageImage.gameObject.SetActive(true);
        mimeStageImage.rectTransform.localScale = Vector3.zero;
        yield return StartCoroutine(PopIn(mimeStageImage.rectTransform));

        var frames = chits[chitIndex].mimeFrames;
        if (frames != null && frames.Length > 0)
            mimeStageImage.sprite = frames[0];

        if (frames != null && frames.Length > 1)
            _mimeLoopRoutine = StartCoroutine(LoopMimeFrames(frames));

        yield return new WaitForSeconds(mimeWatchDuration);
    }

    private IEnumerator LoopMimeFrames(Sprite[] frames)
    {
        int frameIndex = 0;
        while (true)
        {
            mimeStageImage.sprite = frames[frameIndex];
            frameIndex = (frameIndex + 1) % frames.Length;
            yield return new WaitForSeconds(frameCycleInterval);
        }
    }

    private void StopMimeLoop()
    {
        if (_mimeLoopRoutine != null)
        {
            StopCoroutine(_mimeLoopRoutine);
            _mimeLoopRoutine = null;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Guess options: correct + 2 random distractors from the other chits
    // ════════════════════════════════════════════════════════════════════

    private void BuildGuessOptions(int correctChitIndex)
    {
        var otherIndices = new List<int>();
        for (int i = 0; i < chits.Length; i++)
            if (i != correctChitIndex) otherIndices.Add(i);
        Shuffle(otherIndices);

        var optionChitIndices = new List<int> { correctChitIndex };
        for (int i = 0; i < otherIndices.Count && optionChitIndices.Count < 3; i++)
            optionChitIndices.Add(otherIndices[i]);
        Shuffle(optionChitIndices);

        for (int i = 0; i < guessButtons.Length; i++)
        {
            int chitIdx = i < optionChitIndices.Count ? optionChitIndices[i] : -1;
            _currentGuessOptionChitIndices[i] = chitIdx;

            if (chitIdx < 0) { guessButtons[i]?.gameObject.SetActive(false); continue; }

            guessButtons[i]?.gameObject.SetActive(true);
            if (guessLabels != null && i < guessLabels.Length && guessLabels[i] != null)
                guessLabels[i].text = chits[chitIdx].actionLabel;

            var img = guessButtons[i] != null ? guessButtons[i].GetComponent<Image>() : null;
            if (img != null) img.color = chits[chitIdx].wordTint;
        }
    }

    private IEnumerator ShowGuessButtons()
    {
        foreach (var btn in guessButtons)
            SetScaleZero(btn != null ? btn.GetComponent<RectTransform>() : null);

        var routines = new List<Coroutine>();
        foreach (var btn in guessButtons)
            if (btn != null && btn.gameObject.activeSelf) routines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;

        SetGuessButtonsInteractable(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Guess handling
    // ════════════════════════════════════════════════════════════════════

    private void OnGuessTapped(int guessButtonIndex)
    {
        int guessedChitIndex = _currentGuessOptionChitIndices[guessButtonIndex];
        if (guessedChitIndex == _currentChitIndex)
            StartCoroutine(HandleCorrectGuess(guessButtonIndex));
        else
            StartCoroutine(HandleWrongGuess(guessButtonIndex));
    }

    private IEnumerator HandleCorrectGuess(int guessButtonIndex)
    {
        SetGuessButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(correctGuessSfx != null ? correctGuessSfx : AudioManager.Instance.sfxCorrect);

        StopMimeLoop();

        int chitIndex = _currentChitIndex;
        var data = chits[chitIndex];

        if (chitLabels != null && chitIndex < chitLabels.Length && chitLabels[chitIndex] != null)
            chitLabels[chitIndex].text = data.actionLabel;

        _guessedFlags[chitIndex] = true;
        if (chitButtons[chitIndex] != null) chitButtons[chitIndex].interactable = false;

        if (mimeStageImage != null) VFXManager.Instance?.SpawnCorrectBurst(mimeStageImage.rectTransform);

        if (dialogueAudioSource != null && data.revealAudio != null)
        {
            dialogueAudioSource.clip = data.revealAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.revealAudio.length);
        }

        yield return new WaitForSeconds(delayAfterCorrect);

        SetGuessButtonsActive(false);
        if (mimeStageImage != null) mimeStageImage.gameObject.SetActive(false);
        _currentChitIndex = -1;
        SetAllChitsInteractable(true, skipGuessed: true);

        if (AllChitsGuessed())
            StartCoroutine(AllChitsComplete());
    }

    private IEnumerator HandleWrongGuess(int guessButtonIndex)
    {
        AudioManager.Instance?.PlaySFX(wrongGuessSfx != null ? wrongGuessSfx : AudioManager.Instance.sfxWrong);

        var rect = guessButtons[guessButtonIndex] != null ? guessButtons[guessButtonIndex].GetComponent<RectTransform>() : null;
        if (rect != null)
            yield return StartCoroutine(WobbleButton(rect));

        if (dialogueAudioSource != null && wrongGuessHintClip != null)
        {
            dialogueAudioSource.clip = wrongGuessHintClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(wrongGuessHintClip.length);
        }

        // Replay the mime as a hint before letting them guess again.
        SetGuessButtonsInteractable(false);
        yield return StartCoroutine(PlayMime(_currentChitIndex));
        SetGuessButtonsInteractable(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private bool AllChitsGuessed()
    {
        foreach (var g in _guessedFlags) if (!g) return false;
        return true;
    }

    private IEnumerator AllChitsComplete()
    {
        SetAllChitsInteractable(false);
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

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

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
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private void SetAllChitsInteractable(bool value, bool skipGuessed = false)
    {
        for (int i = 0; i < chitButtons.Length; i++)
        {
            if (chitButtons[i] == null) continue;
            if (skipGuessed && _guessedFlags[i]) continue;
            chitButtons[i].interactable = value;
        }
    }

    private void SetGuessButtonsActive(bool value)
    {
        foreach (var btn in guessButtons)
            btn?.gameObject.SetActive(value);
    }

    private void SetGuessButtonsInteractable(bool value)
    {
        foreach (var btn in guessButtons)
            if (btn != null) btn.interactable = value;
    }

    private static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
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