using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class SituationData_MagicWords_BB2
{
    [Tooltip("The situation shown/read out, e.g. 'Your friend gives you a chocolate. What will you say?'")]
    [TextArea] public string situationText;
    [Tooltip("Correct magic word balloon for this situation")]
    public MagicWord_MagicWords_BB2 correctWord;
    [Tooltip("Optional illustration for the situation card")]
    public Sprite situationImage;
    [Tooltip("Optional narrator VO reading the situation. Leave empty to use a short pause instead.")]
    public AudioClip situationAudio;
    [Tooltip("VO of the correct response, e.g. 'Thank you!' or 'You are welcome!'")]
    public AudioClip correctResponseAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Pop The Balloon — MagicWords_BB2.
/// A situation card plays, 5 fixed balloons (Please / Sorry / Thank you /
/// Excuse me / Welcome) float on screen. Student pops the balloon with the
/// right magic word. Correct → sparkle burst + response VO + balloons
/// refill for the next situation. Wrong → balloon wobbles and re-inflates,
/// a gentle hint plays, no penalty, student tries again.
/// Fires OnFinished when Next is pressed after all 8 situations.
/// </summary>
public class BalloonPop_MagicWords_BB2 : MonoBehaviour
{
    [Header("Situations — 8, IN ORDER")]
    public SituationData_MagicWords_BB2[] situations = new SituationData_MagicWords_BB2[8];

    [Header("UI — Situation Card")]
    public TMP_Text situationText;
    public Image    situationImage;

    [Header("UI — Balloons (fixed, 5)")]
    public Balloon_MagicWords_BB2 pleaseBalloon;
    public Balloon_MagicWords_BB2 sorryBalloon;
    public Balloon_MagicWords_BB2 thankYouBalloon;
    public Balloon_MagicWords_BB2 excuseMeBalloon;
    public Balloon_MagicWords_BB2 welcomeBalloon;

    [Header("Balloon Colors")]
    [SerializeField] private Color pleaseColor   = new Color(1f, 0.7f, 0.3f);
    [SerializeField] private Color sorryColor    = new Color(0.6f, 0.4f, 0.9f);
    [SerializeField] private Color thankYouColor = new Color(0.3f, 0.8f, 0.4f);
    [SerializeField] private Color excuseMeColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color welcomeColor  = new Color(1f, 0.4f, 0.7f);

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong pop, e.g. 'Hmm, which magic word?'")]
    public AudioClip   wrongPopHintClip;

    [Header("Narration — plays once each")]
    [Tooltip("Plays ONCE at the very start — e.g. 'Pop the right balloon! Which magic word will you use?'")]
    public AudioClip introAudioClip;
    [Tooltip("Plays ONCE after the last situation — e.g. 'You always know the magic word!'")]
    public AudioClip outroAudioClip;

    [Header("Timing")]
    [SerializeField] private float beatWithoutNarration    = 0.25f;
    [SerializeField] private float delayAfterCorrect        = 0.9f;
    [SerializeField] private float balloonPopInDuration     = 0.35f;
    [SerializeField] private float delayBeforeNextButton    = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;
    private Dictionary<MagicWord_MagicWords_BB2, Balloon_MagicWords_BB2> _balloonsByWord;

    void Awake()
    {
        _balloonsByWord = new Dictionary<MagicWord_MagicWords_BB2, Balloon_MagicWords_BB2>
        {
            { MagicWord_MagicWords_BB2.Please,   pleaseBalloon },
            { MagicWord_MagicWords_BB2.Sorry,    sorryBalloon },
            { MagicWord_MagicWords_BB2.ThankYou, thankYouBalloon },
            { MagicWord_MagicWords_BB2.ExcuseMe, excuseMeBalloon },
            { MagicWord_MagicWords_BB2.Welcome,  welcomeBalloon },
        };

        InitBalloon(pleaseBalloon,   MagicWord_MagicWords_BB2.Please,   "Please",    pleaseColor);
        InitBalloon(sorryBalloon,    MagicWord_MagicWords_BB2.Sorry,    "Sorry",     sorryColor);
        InitBalloon(thankYouBalloon, MagicWord_MagicWords_BB2.ThankYou, "Thank you", thankYouColor);
        InitBalloon(excuseMeBalloon, MagicWord_MagicWords_BB2.ExcuseMe, "Excuse me", excuseMeColor);
        InitBalloon(welcomeBalloon,  MagicWord_MagicWords_BB2.Welcome,  "Welcome",   welcomeColor);
    }

    private void InitBalloon(Balloon_MagicWords_BB2 balloon, MagicWord_MagicWords_BB2 word, string text, Color color)
    {
        if (balloon == null) return;
        balloon.Initialise(word, text, color, OnBalloonTapped);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[BalloonPop_MagicWords_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        foreach (var b in _balloonsByWord.Values)
        {
            b?.SetScaleZero();
            b?.SetInteractable(false);
        }

        StartCoroutine(IntroThenLoadSituation(0));

        Debug.Log("[BalloonPop_MagicWords_BB2] RestartGame — starting from situation 0");
    }

    private IEnumerator IntroThenLoadSituation(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadSituationSequence(index));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Situation sequence: show card + narration → balloons pop in
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadSituationSequence(int index)
    {
        var data = situations[index];

        if (situationText  != null) situationText.text = data.situationText;
        if (situationImage != null) situationImage.sprite = data.situationImage;

        if (dialogueAudioSource != null && data.situationAudio != null)
        {
            dialogueAudioSource.clip = data.situationAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.situationAudio.length);
        }
        else
        {
            yield return new WaitForSeconds(beatWithoutNarration);
        }

        var routines = new List<Coroutine>();
        foreach (var b in _balloonsByWord.Values)
            if (b != null) routines.Add(StartCoroutine(b.PopIn(balloonPopInDuration)));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnBalloonTapped(Balloon_MagicWords_BB2 balloon)
    {
        var data = situations[_currentIndex];
        if (balloon.Word == data.correctWord)
            StartCoroutine(HandleCorrectPop(balloon));
        else
            StartCoroutine(HandleWrongPop(balloon));
    }

    private IEnumerator HandleCorrectPop(Balloon_MagicWords_BB2 balloon)
    {
        foreach (var b in _balloonsByWord.Values) b?.SetInteractable(false);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);
        yield return StartCoroutine(balloon.PlayPop());
        VFXManager.Instance?.SpawnCorrectBurst(balloon.GetComponent<RectTransform>());

        var data = situations[_currentIndex];
        if (dialogueAudioSource != null && data.correctResponseAudio != null)
        {
            dialogueAudioSource.clip = data.correctResponseAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.correctResponseAudio.length);
        }

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < situations.Length)
            yield return StartCoroutine(LoadSituationSequence(_currentIndex));
        else
            StartCoroutine(AllSituationsComplete());
    }

    private IEnumerator HandleWrongPop(Balloon_MagicWords_BB2 balloon)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);
        yield return StartCoroutine(balloon.PlayWrongWobble());

        if (dialogueAudioSource != null && wrongPopHintClip != null)
        {
            dialogueAudioSource.clip = wrongPopHintClip;
            dialogueAudioSource.Play();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllSituationsComplete()
    {
        foreach (var b in _balloonsByWord.Values) b?.SetInteractable(false);

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
    //  Next button — wire this to the Button's OnClick() in the Inspector
    // ════════════════════════════════════════════════════════════════════

    public void OnNextButtonPressed()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
        OnFinished?.Invoke();
    }
}
