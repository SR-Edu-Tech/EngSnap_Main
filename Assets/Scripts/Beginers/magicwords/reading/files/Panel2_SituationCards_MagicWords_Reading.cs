using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel2_SituationCards_MagicWords_Reading
/// Controls the Situation Card reader screen (Panel 2).
///
/// Sequence:
///   • Reveals cards one at a time with a slide+bounce animation
///   • Each card auto-plays its situation voiceover
///   • Student can tap the illustration to replay the situation audio
///   • Student can tap the magic word text to play just the word audio
///   • After the last card's audio completes, NEXT + REPLAY buttons appear
///   • NEXT → GameManager.GoToNextUnit()
///   • REPLAY → GameManager.ReplayFromStart()
///
/// Attach to the root of Panel 2's canvas/panel.
/// </summary>
public class Panel2_SituationCards_MagicWords_Reading : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════════════════

    [Header("── Word Data ─────────────────────────")]
    [Tooltip("Same 5 MagicWordData assets used in Panel 1 (must match order)")]
    public MagicWordData_MagicWords_Reading[] magicWords;

    [Header("── Card Prefab ──────────────────────")]
    [Tooltip("Prefab with SituationCard_MagicWords_Reading attached")]
    public GameObject situationCardPrefab;

    [Tooltip("Parent RectTransform cards are instantiated into (ScrollRect content or plain panel)")]
    public RectTransform cardContainer;

    [Header("── Layout ───────────────────────────")]
    [Tooltip("Horizontal spacing between cards (pixels)")]
    public float cardSpacingX = 40f;

    [Tooltip("Width of each card (pixels)")]
    public float cardWidth = 340f;

    [Header("── Buttons ──────────────────────────")]
    public Button   nextButton;
    public Button   replayButton;
    public GameObject bottomButtonsObject;

    [Header("── Audio ────────────────────────────")]
    [Tooltip("Played when a card slides in")]
    public AudioClip sfxCardSlide;

    [Tooltip("Played when student taps illustration or word")]
    public AudioClip sfxCardTap;

    [Tooltip("Fanfare when all 5 cards have been presented")]
    public AudioClip sfxAllCardsDone;

    [Header("── Timing ───────────────────────────")]
    [Tooltip("Seconds between last card's audio ending and next card appearing")]
    [Range(0.3f, 2f)]
    public float cardToCardPause = 0.9f;

    // ═══════════════════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════════════════

    private List<SituationCard_MagicWords_Reading> _cards =
        new List<SituationCard_MagicWords_Reading>();

    // Separate sources: SFX can stack freely; voice is exclusive (Stop before Play)
    private AudioSource _sfxSource;
    private AudioSource _voiceSource;

    private int  _currentCardIndex  = 0;
    private bool _allCardsShown     = false;   // true only after the full loop completes
    private bool _autoSequenceRunning = false;

    // ═══════════════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════════════════════════

    void Awake()
    {
        // SFX source – reuse any existing AudioSource on this GameObject
        _sfxSource = GetComponent<AudioSource>();
        if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

        // Voice source – dedicated child so Stop() never touches SFX
        var voiceGO = new GameObject("VoiceSource_Panel2");
        voiceGO.transform.SetParent(transform);
        _voiceSource = voiceGO.AddComponent<AudioSource>();
        _voiceSource.playOnAwake = false;

        if (bottomButtonsObject != null) bottomButtonsObject.SetActive(false);

        nextButton?.onClick.AddListener(OnNextClicked);
        replayButton?.onClick.AddListener(OnReplayClicked);
    }

    // Stop the current voiceover and play a new one
    private void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;
        _voiceSource.Stop();
        _voiceSource.clip = clip;
        _voiceSource.Play();
    }

    void OnEnable()
    {
        ResetPanel();
        StartCoroutine(RunCardSequence());
    }

    // ═══════════════════════════════════════════════════════════════
    //  Panel Reset
    // ═══════════════════════════════════════════════════════════════

    private void ResetPanel()
    {
        StopAllCoroutines();
        _autoSequenceRunning = false;
        _allCardsShown       = false;
        _currentCardIndex    = 0;

        _voiceSource?.Stop();

        foreach (var c in _cards)
            if (c != null) Destroy(c.gameObject);
        _cards.Clear();

        if (bottomButtonsObject != null) bottomButtonsObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Sequence
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator RunCardSequence()
    {
        _autoSequenceRunning = true;
        _allCardsShown       = false;

        yield return new WaitForSeconds(0.4f); // let panel render

        for (int i = 0; i < magicWords.Length; i++)
        {
            _currentCardIndex = i;
            yield return SpawnAndPlayCard(i);
        }

        // ── Every card has now been shown and its audio played ────────────
        _allCardsShown       = true;
        _autoSequenceRunning = false;

        if (sfxAllCardsDone != null)
            _sfxSource.PlayOneShot(sfxAllCardsDone);

        yield return new WaitForSeconds(0.8f);
        ShowBottomButtons();
    }

    private IEnumerator SpawnAndPlayCard(int index)
    {
        var data = magicWords[index];

        // ── Slide SFX (short, stacking fine) ─────────────────────
        if (sfxCardSlide != null)
            _sfxSource.PlayOneShot(sfxCardSlide);

        // ── Instantiate ───────────────────────────────────────────
        var go   = Instantiate(situationCardPrefab, cardContainer);
        var card = go.GetComponent<SituationCard_MagicWords_Reading>();
        var rect = go.GetComponent<RectTransform>();

        rect.anchoredPosition = new Vector2(
            index * (cardWidth + cardSpacingX), 0f);

        card.Initialise(data, OnIllustrationTapped, OnWordTextTapped);

        // ── Slide-in animation – must fully complete before audio ─
        yield return card.PlaySlideIn();
        _cards.Add(card);

        // ── Auto-play situation voiceover ─────────────────────────
        yield return new WaitForSeconds(data.cardAutoPlayDelay);

        if (data.situationAudio != null)
        {
            PlayVoice(data.situationAudio);

            // Wait for the clip to finish naturally.
            // If a tap interrupts it (_voiceSource.Stop() was called),
            // isPlaying becomes false and we move on immediately —
            // the sequence is NOT blocked waiting out a dead timer.
            float elapsed = 0f;
            while (elapsed < data.situationAudio.length && _voiceSource.isPlaying)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(cardToCardPause);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Tap Callbacks (from SituationCard)
    // ═══════════════════════════════════════════════════════════════

    private void OnIllustrationTapped(MagicWordData_MagicWords_Reading data)
    {
        // Tap SFX plays freely alongside voice
        if (sfxCardTap != null) _sfxSource.PlayOneShot(sfxCardTap);

        // Replay situation audio – stops whatever is currently playing first.
        // Note: if the auto-sequence is still running, interrupting the voice
        // here causes the coroutine's isPlaying-loop to exit early, which is
        // intentional — the student's tap is more important than the sequence timer.
        PlayVoice(data.situationAudio);
    }

    private void OnWordTextTapped(MagicWordData_MagicWords_Reading data)
    {
        if (sfxCardTap != null) _sfxSource.PlayOneShot(sfxCardTap);

        // Play word-only clip; fall back to full situation audio if not set
        PlayVoice(data.wordOnlyAudio != null ? data.wordOnlyAudio : data.situationAudio);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Buttons
    // ═══════════════════════════════════════════════════════════════

    private void ShowBottomButtons()
    {
        if (bottomButtonsObject == null) return;
        bottomButtonsObject.SetActive(true);
        StartCoroutine(PunchScale(bottomButtonsObject.transform, 0.45f));
    }

    private void OnNextClicked()
    {
        GameManager_MagicWords_Reading.Instance?.GoToNextUnit();
    }

    private void OnReplayClicked()
    {
        GameManager_MagicWords_Reading.Instance?.ReplayFromStart();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════

    private IEnumerator PunchScale(Transform t, float dur)
    {
        t.localScale = Vector3.zero;
        float e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float p = Mathf.Clamp01(e / dur);
            float s = ElasticOut(p);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private static float ElasticOut(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        const float p = 0.3f;
        return Mathf.Pow(2f, -10f * t)
             * Mathf.Sin((t - p / 4f) * (2f * Mathf.PI) / p) + 1f;
    }
}