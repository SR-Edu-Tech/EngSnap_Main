using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum DirectionArrow_MapDirections_BB2 { Left, Right, Straight, UTurn, Stop }

[System.Serializable]
public class DirectionStepData_MapDirections_BB2
{
    [Tooltip("Correct arrow for this step")]
    public DirectionArrow_MapDirections_BB2 correctArrow;
    [Tooltip("Spoken direction for this step, e.g. 'Turn right.' — played on load AND replayed as the wrong-tap hint")]
    public AudioClip directionAudio;
    [Tooltip("Where the kid token moves to on a correct tap for this step")]
    public RectTransform tokenWaypoint;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Follow The Directions — MapDirections_BB2.
/// A direction is spoken (e.g. "Turn right."). Student taps the matching
/// arrow (LEFT / RIGHT / STRAIGHT / U-TURN — STOP only appears for steps
/// that need it). Correct tap moves the kid token to that step's waypoint
/// (simple scripted position tween, no Animator). Wrong tap never
/// penalises: the tapped arrow wobbles, the kid stays put, the direction
/// replays as a hint, then a generic "try again" line plays.
/// Fires OnFinished when the 8th step is completed.
/// </summary>
public class DirectionSteps_MapDirections_BB2 : MonoBehaviour
{
    [Header("Steps — 8, IN ORDER")]
    public DirectionStepData_MapDirections_BB2[] steps = new DirectionStepData_MapDirections_BB2[8];

    [Header("UI — Token")]
    [Tooltip("The kid token that moves along the path")]
    public RectTransform kidToken;
    [Tooltip("Token's position before step 0 — RestartGame() resets it here")]
    public RectTransform startWaypoint;

    [Header("UI — Arrow Buttons (fixed)")]
    public Button leftButton;
    public Button rightButton;
    public Button straightButton;
    public Button uturnButton;
    [Tooltip("Only shown/enabled for steps whose correctArrow is Stop — hidden otherwise")]
    public Button stopButton;

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'Which way? Try again!'")]
    public AudioClip   wrongTapHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Pop FX")]
    public AudioClip buttonPopSfx;

    [Header("Timing")]
    [SerializeField] private float tokenMoveDuration      = 0.5f;
    [SerializeField] private float delayAfterCorrect      = 0.7f;
    [SerializeField] private float delayBeforeNextButton  = 0.6f;
    [SerializeField] private float popInDuration           = 0.3f;
    [SerializeField] private float beatWithoutNarration    = 0.25f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        if (leftButton     != null) { leftButton.onClick.RemoveAllListeners();     leftButton.onClick.AddListener(() => OnArrowTapped(DirectionArrow_MapDirections_BB2.Left)); }
        if (rightButton    != null) { rightButton.onClick.RemoveAllListeners();    rightButton.onClick.AddListener(() => OnArrowTapped(DirectionArrow_MapDirections_BB2.Right)); }
        if (straightButton != null) { straightButton.onClick.RemoveAllListeners(); straightButton.onClick.AddListener(() => OnArrowTapped(DirectionArrow_MapDirections_BB2.Straight)); }
        if (uturnButton    != null) { uturnButton.onClick.RemoveAllListeners();    uturnButton.onClick.AddListener(() => OnArrowTapped(DirectionArrow_MapDirections_BB2.UTurn)); }
        if (stopButton     != null) { stopButton.onClick.RemoveAllListeners();     stopButton.onClick.AddListener(() => OnArrowTapped(DirectionArrow_MapDirections_BB2.Stop)); }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[DirectionSteps_MapDirections_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        if (kidToken != null && startWaypoint != null)
            kidToken.position = startWaypoint.position;

        SetArrowsInteractable(false);
        stopButton?.gameObject.SetActive(false);

        StartCoroutine(IntroThenLoadStep(0));

        Debug.Log("[DirectionSteps_MapDirections_BB2] RestartGame — starting from step 0");
    }

    private IEnumerator IntroThenLoadStep(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadStepSequence(index));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Step sequence: spoken direction → arrows pop in (Stop only if needed)
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadStepSequence(int index)
    {
        SetArrowsInteractable(false);

        var data = steps[index];
        bool needsStop = data.correctArrow == DirectionArrow_MapDirections_BB2.Stop;
        stopButton?.gameObject.SetActive(needsStop);

        if (dialogueAudioSource != null && data.directionAudio != null)
        {
            dialogueAudioSource.clip = data.directionAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.directionAudio.length);
        }
        else
        {
            yield return new WaitForSeconds(beatWithoutNarration);
        }

        if (buttonPopSfx != null) AudioManager.Instance?.PlaySFX(buttonPopSfx);
        var routines = new List<Coroutine>();
        if (leftButton     != null) routines.Add(StartCoroutine(PopIn(leftButton.GetComponent<RectTransform>())));
        if (rightButton    != null) routines.Add(StartCoroutine(PopIn(rightButton.GetComponent<RectTransform>())));
        if (straightButton != null) routines.Add(StartCoroutine(PopIn(straightButton.GetComponent<RectTransform>())));
        if (uturnButton    != null) routines.Add(StartCoroutine(PopIn(uturnButton.GetComponent<RectTransform>())));
        if (needsStop && stopButton != null) routines.Add(StartCoroutine(PopIn(stopButton.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;

        SetArrowsInteractable(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnArrowTapped(DirectionArrow_MapDirections_BB2 tapped)
    {
        var data = steps[_currentIndex];
        if (tapped == data.correctArrow)
            StartCoroutine(HandleCorrectTap());
        else
            StartCoroutine(HandleWrongTap(tapped));
    }

    private IEnumerator HandleCorrectTap()
    {
        SetArrowsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        var data = steps[_currentIndex];

        if (kidToken != null && data.tokenWaypoint != null)
            yield return StartCoroutine(MoveToken(data.tokenWaypoint.position));

        VFXManager.Instance?.SpawnCorrectBurst(kidToken);

        if (dialogueAudioSource != null && data.directionAudio != null)
        {
            dialogueAudioSource.clip = data.directionAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.directionAudio.length);
        }

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < steps.Length)
            yield return StartCoroutine(LoadStepSequence(_currentIndex));
        else
            StartCoroutine(AllStepsComplete());
    }

    private IEnumerator HandleWrongTap(DirectionArrow_MapDirections_BB2 tapped)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

        Button wrongButton = tapped switch
        {
            DirectionArrow_MapDirections_BB2.Left     => leftButton,
            DirectionArrow_MapDirections_BB2.Right    => rightButton,
            DirectionArrow_MapDirections_BB2.Straight => straightButton,
            DirectionArrow_MapDirections_BB2.UTurn    => uturnButton,
            _                                          => stopButton
        };
        if (wrongButton != null)
            yield return StartCoroutine(WobbleButton(wrongButton.GetComponent<RectTransform>()));

        var data = steps[_currentIndex];
        if (dialogueAudioSource != null && data.directionAudio != null)
        {
            dialogueAudioSource.clip = data.directionAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.directionAudio.length);
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

    private IEnumerator MoveToken(Vector3 targetPosition)
    {
        Vector3 startPos = kidToken.position;
        float e = 0f;
        while (e < tokenMoveDuration)
        {
            e += Time.deltaTime;
            kidToken.position = Vector3.Lerp(startPos, targetPosition, Mathf.SmoothStep(0f, 1f, e / tokenMoveDuration));
            yield return null;
        }
        kidToken.position = targetPosition;
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

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllStepsComplete()
    {
        SetArrowsInteractable(false);
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

    private void SetArrowsInteractable(bool value)
    {
        if (leftButton     != null) leftButton.interactable     = value;
        if (rightButton    != null) rightButton.interactable    = value;
        if (straightButton != null) straightButton.interactable = value;
        if (uturnButton    != null) uturnButton.interactable    = value;
        if (stopButton     != null) stopButton.interactable     = value;
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
