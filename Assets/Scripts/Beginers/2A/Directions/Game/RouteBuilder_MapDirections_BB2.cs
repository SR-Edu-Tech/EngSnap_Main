using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum RouteCard_MapDirections_BB2 { GoStraight, TurnLeft, TurnRight, GoUp, Stop }

[System.Serializable]
public class TripData_MapDirections_BB2
{
    [Tooltip("Display name of the destination, e.g. 'the school' — used only for your own reference/notes")]
    public string placeName;
    [Tooltip("The exact sequence of cards that reaches this place, IN ORDER")]
    public RouteCard_MapDirections_BB2[] correctRoute;
    [Tooltip("Waypoints the friend token moves through on a CORRECT route — first entry is the start position, last is the destination")]
    public RectTransform[] pathWaypoints;
    [Tooltip("Spoken instruction for this trip, e.g. 'Guide your friend to the school.'")]
    public AudioClip introAudio;
    [Tooltip("VO played once the friend reaches the destination, e.g. cheering")]
    public AudioClip successAudio;
    [Tooltip("Optional — trip-specific wrong-route hint. Leave empty to use the generic one.")]
    public AudioClip wrongRouteHintClip;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Guide The Friend — MapDirections_BB2.
/// Student taps direction cards (Go straight / Turn left / Turn right /
/// Go up / Stop) IN ORDER to build a route, shown as a simple text strip.
/// Tapping GO! checks the built route against the trip's correct sequence.
/// Correct → friend token moves through pathWaypoints to the destination,
/// cheers, next trip loads. Wrong → friend stays put, a gentle hint plays,
/// the route strip clears automatically so the student can rebuild it.
/// No penalty, no move on a wrong route.
/// Fires OnFinished when Next is pressed after all 4 trips.
/// </summary>
public class RouteBuilder_MapDirections_BB2 : MonoBehaviour
{
    [Header("Trips — 4, IN ORDER")]
    public TripData_MapDirections_BB2[] trips = new TripData_MapDirections_BB2[4];

    [Header("UI — Friend Token")]
    public RectTransform friendToken;

    [Header("UI — Route Strip")]
    [Tooltip("Shows the route built so far, e.g. 'Go straight → Turn left'")]
    public TMP_Text routeStripText;

    [Header("UI — Direction Cards (fixed)")]
    public Button goStraightCard;
    public Button turnLeftCard;
    public Button turnRightCard;
    public Button goUpCard;
    public Button stopCard;
    public Button goButton;

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic hint VO for a wrong route, e.g. 'Hmm, try another way!'")]
    public AudioClip   genericWrongRouteHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Pop FX")]
    public AudioClip buttonPopSfx;
    [Tooltip("Short sound played each time a card is added to the route strip")]
    public AudioClip cardAddSfx;

    [Header("Timing")]
    [SerializeField] private float stepMoveDuration       = 0.5f;
    [SerializeField] private float delayAfterSuccess       = 0.9f;
    [SerializeField] private float delayBeforeNextButton   = 0.6f;
    [SerializeField] private float popInDuration            = 0.3f;
    [SerializeField] private float popOutDuration           = 0.2f;
    [SerializeField] private float beatWithoutNarration     = 0.25f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private static readonly Dictionary<RouteCard_MapDirections_BB2, string> CardText = new()
    {
        { RouteCard_MapDirections_BB2.GoStraight, "Go straight" },
        { RouteCard_MapDirections_BB2.TurnLeft,   "Turn left"   },
        { RouteCard_MapDirections_BB2.TurnRight,  "Turn right"  },
        { RouteCard_MapDirections_BB2.GoUp,       "Go up"       },
        { RouteCard_MapDirections_BB2.Stop,       "Stop"        },
    };

    private readonly List<RouteCard_MapDirections_BB2> _builtRoute = new();
    private int _currentTripIndex = 0;
    private int _lastRestartFrame = -1;
    private bool _isEvaluating = false;

    void Awake()
    {
        if (goStraightCard != null) { goStraightCard.onClick.RemoveAllListeners(); goStraightCard.onClick.AddListener(() => OnCardTapped(RouteCard_MapDirections_BB2.GoStraight)); }
        if (turnLeftCard   != null) { turnLeftCard.onClick.RemoveAllListeners();   turnLeftCard.onClick.AddListener(() => OnCardTapped(RouteCard_MapDirections_BB2.TurnLeft)); }
        if (turnRightCard  != null) { turnRightCard.onClick.RemoveAllListeners();  turnRightCard.onClick.AddListener(() => OnCardTapped(RouteCard_MapDirections_BB2.TurnRight)); }
        if (goUpCard       != null) { goUpCard.onClick.RemoveAllListeners();       goUpCard.onClick.AddListener(() => OnCardTapped(RouteCard_MapDirections_BB2.GoUp)); }
        if (stopCard       != null) { stopCard.onClick.RemoveAllListeners();       stopCard.onClick.AddListener(() => OnCardTapped(RouteCard_MapDirections_BB2.Stop)); }
        if (goButton       != null) { goButton.onClick.RemoveAllListeners();       goButton.onClick.AddListener(OnGoPressed); }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[RouteBuilder_MapDirections_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentTripIndex = 0;
        _isEvaluating = false;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        SetCardsInteractable(false);
        SetScaleZero(goStraightCard); SetScaleZero(turnLeftCard); SetScaleZero(turnRightCard);
        SetScaleZero(goUpCard); SetScaleZero(stopCard); SetScaleZero(goButton);

        StartCoroutine(IntroThenLoadTrip(0));

        Debug.Log("[RouteBuilder_MapDirections_BB2] RestartGame — starting from trip 0");
    }

    private IEnumerator IntroThenLoadTrip(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadTripSequence(index, isFirstLoad: true));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Trip sequence: place the friend at start → narrate → pop cards in
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadTripSequence(int index, bool isFirstLoad)
    {
        SetCardsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCards());

        ClearRoute();

        var trip = trips[index];
        if (friendToken != null && trip.pathWaypoints != null && trip.pathWaypoints.Length > 0 && trip.pathWaypoints[0] != null)
            friendToken.position = trip.pathWaypoints[0].position;

        if (dialogueAudioSource != null && trip.introAudio != null)
        {
            dialogueAudioSource.clip = trip.introAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(trip.introAudio.length);
        }
        else
        {
            yield return new WaitForSeconds(beatWithoutNarration);
        }

        if (buttonPopSfx != null) AudioManager.Instance?.PlaySFX(buttonPopSfx);
        var routines = new List<Coroutine>();
        foreach (var btn in new[] { goStraightCard, turnLeftCard, turnRightCard, goUpCard, stopCard, goButton })
            if (btn != null) routines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;

        SetCardsInteractable(true);
    }

    private IEnumerator PopOutCards()
    {
        var routines = new List<Coroutine>();
        foreach (var btn in new[] { goStraightCard, turnLeftCard, turnRightCard, goUpCard, stopCard, goButton })
            if (btn != null) routines.Add(StartCoroutine(PopOut(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Building the route
    // ════════════════════════════════════════════════════════════════════

    private void OnCardTapped(RouteCard_MapDirections_BB2 card)
    {
        if (_isEvaluating) return;

        _builtRoute.Add(card);
        AudioManager.Instance?.PlaySFX(cardAddSfx != null ? cardAddSfx : AudioManager.Instance.sfxButtonTap);
        RefreshRouteStripText();
    }

    private void RefreshRouteStripText()
    {
        if (routeStripText == null) return;
        routeStripText.text = _builtRoute.Count == 0
            ? ""
            : string.Join(" → ", _builtRoute.Select(c => CardText[c]));
    }

    private void ClearRoute()
    {
        _builtRoute.Clear();
        RefreshRouteStripText();
    }

    // ════════════════════════════════════════════════════════════════════
    //  GO! — evaluate the built route
    // ════════════════════════════════════════════════════════════════════

    private void OnGoPressed()
    {
        if (_isEvaluating) return;
        if (_builtRoute.Count == 0) return;

        StartCoroutine(EvaluateRoute());
    }

    private IEnumerator EvaluateRoute()
    {
        _isEvaluating = true;
        SetCardsInteractable(false);

        var trip = trips[_currentTripIndex];
        bool correct = _builtRoute.SequenceEqual(trip.correctRoute);

        if (correct)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

            if (friendToken != null && trip.pathWaypoints != null)
            {
                for (int i = 1; i < trip.pathWaypoints.Length; i++)
                {
                    if (trip.pathWaypoints[i] == null) continue;
                    yield return StartCoroutine(MoveToken(trip.pathWaypoints[i].position));
                }
            }

            VFXManager.Instance?.SpawnCorrectBurst(friendToken);

            if (dialogueAudioSource != null && trip.successAudio != null)
            {
                dialogueAudioSource.clip = trip.successAudio;
                dialogueAudioSource.Play();
                yield return new WaitForSeconds(trip.successAudio.length);
            }

            yield return new WaitForSeconds(delayAfterSuccess);

            _currentTripIndex++;
            _isEvaluating = false;

            if (_currentTripIndex < trips.Length)
                yield return StartCoroutine(LoadTripSequence(_currentTripIndex, isFirstLoad: false));
            else
                StartCoroutine(AllTripsComplete());
        }
        else
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

            AudioClip hint = trip.wrongRouteHintClip != null ? trip.wrongRouteHintClip : genericWrongRouteHintClip;
            if (dialogueAudioSource != null && hint != null)
            {
                dialogueAudioSource.clip = hint;
                dialogueAudioSource.Play();
                yield return new WaitForSeconds(hint.length);
            }

            ClearRoute();
            _isEvaluating = false;
            SetCardsInteractable(true);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator MoveToken(Vector3 targetPosition)
    {
        Vector3 startPos = friendToken.position;
        float e = 0f;
        while (e < stepMoveDuration)
        {
            e += Time.deltaTime;
            friendToken.position = Vector3.Lerp(startPos, targetPosition, Mathf.SmoothStep(0f, 1f, e / stepMoveDuration));
            yield return null;
        }
        friendToken.position = targetPosition;
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

    private static void SetScaleZero(Button b)
    {
        if (b != null) b.GetComponent<RectTransform>().localScale = Vector3.zero;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllTripsComplete()
    {
        SetCardsInteractable(false);
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

    private void SetCardsInteractable(bool value)
    {
        if (goStraightCard != null) goStraightCard.interactable = value;
        if (turnLeftCard   != null) turnLeftCard.interactable   = value;
        if (turnRightCard  != null) turnRightCard.interactable  = value;
        if (goUpCard       != null) goUpCard.interactable       = value;
        if (stopCard       != null) stopCard.interactable       = value;
        if (goButton       != null) goButton.interactable       = value;
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
