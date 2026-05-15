using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel1_WordBubbles_MagicWords_Reading
/// Controls the intro Word-Bubble screen:
///   • Spawns 5 bubbles one by one with sparkle FX and pop audio
///   • Each bubble plays its definition audio after popping in
///   • After all 5 are tapped, the NEXT button appears
///   • Tapped state is tracked per bubble
///
/// Attach to the root of Panel 1's canvas/panel.
/// </summary>
public class Panel1_WordBubbles_MagicWords_Reading : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════════
    //  Inspector References
    // ═════════════════════════════════════════════════════════════════════════

    [Header("── Word Data ───────────────────────")]
    [Tooltip("Ordered list of 5 MagicWordData assets (PLEASE → WELCOME)")]
    public MagicWordData_MagicWords_Reading[] magicWords; // length = 5

    [Header("── Bubble Prefab ───────────────────")]
    [Tooltip("Prefab with WordBubble_MagicWords_Reading component attached")]
    public GameObject wordBubblePrefab;

    [Tooltip("Parent RectTransform that bubbles are instantiated into")]
    public RectTransform bubbleContainer;

    [Header("── Bubble Positions ─────────────────")]
    [Tooltip("Anchored positions for each of the 5 bubbles in the container")]
    public Vector2[] bubblePositions; // length = 5

    [Header("── Sparkle FX ──────────────────────")]
    [Tooltip("Particle system prefab played at each bubble spawn position")]
    public GameObject sparkleFXPrefab;

    [Header("── NEXT Button ─────────────────────")]
    public Button nextButton;
    public GameObject nextButtonObject;

    [Header("── Audio ────────────────────────────")]
    [Tooltip("Short pop SFX played when each bubble appears")]
    public AudioClip sfxBubblePop;

    [Tooltip("Cheerful SFX when all 5 bubbles have been tapped")]
    public AudioClip sfxAllTapped;

    [Tooltip("SFX when student taps a bubble (bounce feedback)")]
    public AudioClip sfxBubbleTap;

    [Header("── Timing ───────────────────────────")]
    [Tooltip("Delay between each bubble popping in (seconds)")]
    [Range(0.5f, 3f)]
    public float bubbleSpawnInterval = 1.2f;

    [Tooltip("Delay between bubble pop and its audio playing")]
    [Range(0f, 1f)]
    public float audioAfterPopDelay = 0.3f;

    // ═════════════════════════════════════════════════════════════════════════
    //  Private State
    // ═════════════════════════════════════════════════════════════════════════

    private List<WordBubble_MagicWords_Reading> _spawnedBubbles =
        new List<WordBubble_MagicWords_Reading>();

    private int  _tappedCount    = 0;
    private bool _allSpawned     = false;
    private bool _introPlaying   = false;

    // Two dedicated sources so SFX (pop/tap) never block or cancel voiceovers
    private AudioSource _sfxSource;   // for pop, tap, fanfare – PlayOneShot stacking is fine here
    private AudioSource _voiceSource; // for definition voiceovers – only ONE plays at a time

    // ═════════════════════════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        // Get or create the SFX source (can reuse any existing AudioSource on this GO)
        _sfxSource = GetComponent<AudioSource>();
        if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

        // Voice source lives on a child so it is fully independent
        var voiceGO = new GameObject("VoiceSource_Panel1");
        voiceGO.transform.SetParent(transform);
        _voiceSource = voiceGO.AddComponent<AudioSource>();
        _voiceSource.playOnAwake = false;

        // Hide NEXT until all tapped
        if (nextButtonObject != null) nextButtonObject.SetActive(false);
        nextButton?.onClick.AddListener(OnNextClicked);
    }

    // Convenience: stop whatever voice is playing then play the new clip
    private void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;
        _voiceSource.Stop();
        _voiceSource.clip = clip;
        _voiceSource.Play();
    }

    void OnEnable()
    {
        // Reset and re-run intro each time panel becomes active
        ResetPanel();
        StartCoroutine(SpawnBubblesSequence());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Spawn Sequence
    // ═════════════════════════════════════════════════════════════════════════

    private void ResetPanel()
    {
        StopAllCoroutines();

        // Destroy previously spawned bubbles
        foreach (var b in _spawnedBubbles)
            if (b != null) Destroy(b.gameObject);
        _spawnedBubbles.Clear();

        _tappedCount  = 0;
        _allSpawned   = false;
        _introPlaying = false;

        if (nextButtonObject != null) nextButtonObject.SetActive(false);
    }

    private IEnumerator SpawnBubblesSequence()
    {
        _introPlaying = true;

        // Small initial pause so scene fully renders first
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < magicWords.Length; i++)
        {
            yield return SpawnBubble(i);
            yield return new WaitForSeconds(bubbleSpawnInterval);
        }

        _allSpawned   = true;
        _introPlaying = false;

        // Check if student already tapped everything (unlikely but safe)
        CheckAllTapped();
    }

    private IEnumerator SpawnBubble(int index)
    {
        var data = magicWords[index];
        Vector2 pos = (bubblePositions != null && index < bubblePositions.Length)
            ? bubblePositions[index]
            : new Vector2(0, 0);

        // ── Sparkle first ──────────────────────────────────────────────────
        if (sparkleFXPrefab != null)
        {
            var fx = Instantiate(sparkleFXPrefab, bubbleContainer);
            var fxRect = fx.GetComponent<RectTransform>();
            if (fxRect != null) fxRect.anchoredPosition = pos;
            Destroy(fx, 1.5f);
        }

        // ── Pop SFX (stacking is fine for short SFX) ──────────────────────
        if (sfxBubblePop != null)
            _sfxSource.PlayOneShot(sfxBubblePop);

        // ── Instantiate bubble ─────────────────────────────────────────────
        var go = Instantiate(wordBubblePrefab, bubbleContainer);
        var bubble = go.GetComponent<WordBubble_MagicWords_Reading>();
        var rectTr = go.GetComponent<RectTransform>();
        rectTr.anchoredPosition = pos;

        bubble.Initialise(data, index, OnBubbleTapped);
        bubble.PlayPopAnimation();
        _spawnedBubbles.Add(bubble);

        // ── Definition audio after pop settles ────────────────────────────
        // PlayVoice() stops any currently playing voice before starting the new one
        yield return new WaitForSeconds(audioAfterPopDelay);

        if (data.bubbleIntroAudio != null)
        {
            PlayVoice(data.bubbleIntroAudio);
            // Wait for the clip to finish, but bail early if the voice was
            // interrupted by a tap (source will have stopped or swapped clip)
            float elapsed = 0f;
            while (elapsed < data.bubbleIntroAudio.length && _voiceSource.isPlaying)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Tap Callbacks
    // ═════════════════════════════════════════════════════════════════════════

    private void OnBubbleTapped(int index, MagicWordData_MagicWords_Reading data)
    {
        // SFX: short tap feedback — PlayOneShot so it never blocks the voice source
        if (sfxBubbleTap != null)
            _sfxSource.PlayOneShot(sfxBubbleTap);

        // Voice: stop whatever is currently playing, then play this word's definition
        PlayVoice(data.bubbleIntroAudio);

        _tappedCount++;
        CheckAllTapped();
    }

    private void CheckAllTapped()
    {
        if (_tappedCount >= magicWords.Length && _allSpawned)
        {
            // Stop any lingering voiceover so the fanfare isn't buried under it
            _voiceSource.Stop();
            if (sfxAllTapped != null)
                _sfxSource.PlayOneShot(sfxAllTapped);

            StartCoroutine(ShowNextButtonDelayed(0.8f));
        }
    }

    private IEnumerator ShowNextButtonDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (nextButtonObject != null)
        {
            nextButtonObject.SetActive(true);
            // Animate the NEXT button in (bubble-scale punch)
            StartCoroutine(PunchScale(nextButtonObject.transform, 0.4f));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  NEXT Button
    // ═════════════════════════════════════════════════════════════════════════

    private void OnNextClicked()
    {
        GameManager_MagicWords_Reading.Instance?.GoToPanel2();
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Elastic punch-scale anim: overshoot then settle.</summary>
    private IEnumerator PunchScale(Transform t, float duration)
    {
        t.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            // Overshoot curve
            float s = p < 0.5f
                ? 4f * p * p * p
                : 1f + Mathf.Sin((p - 0.5f) * Mathf.PI * 3f) * 0.25f * (1f - p);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}