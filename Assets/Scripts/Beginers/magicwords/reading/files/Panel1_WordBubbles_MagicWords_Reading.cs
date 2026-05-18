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
/// CHANGE FROM ORIGINAL:
///   - Replaced GameManager_MagicWords_Reading.Instance?.GoToPanel2()
///     with a direct [SerializeField] reference to avoid singleton coupling.
///     Drag the GameManager GO into the 'gameManager' field in the Inspector.
/// </summary>
public class Panel1_WordBubbles_MagicWords_Reading : MonoBehaviour
{
    // ═════════════════════════════════════════════════════════════════════════
    //  Inspector References
    // ═════════════════════════════════════════════════════════════════════════

    [Header("── Game Manager ────────────────────")]
    [Tooltip("Drag the GameManager_MagicWords_Reading GO here")]
    public GameManager_MagicWords_Reading gameManager;

    [Header("── Word Data ───────────────────────")]
    [Tooltip("Ordered list of 5 MagicWordData assets (PLEASE → WELCOME)")]
    public MagicWordData_MagicWords_Reading[] magicWords;

    [Header("── Bubble Prefab ───────────────────")]
    [Tooltip("Prefab with WordBubble_MagicWords_Reading component attached")]
    public GameObject wordBubblePrefab;

    [Tooltip("Parent RectTransform that bubbles are instantiated into")]
    public RectTransform bubbleContainer;

    [Header("── Bubble Positions ─────────────────")]
    [Tooltip("Anchored positions for each of the 5 bubbles in the container")]
    public Vector2[] bubblePositions;

    [Header("── Sparkle FX ──────────────────────")]
    [Tooltip("Particle system prefab played at each bubble spawn position")]
    public GameObject sparkleFXPrefab;

    [Header("── NEXT Button ─────────────────────")]
    public Button     nextButton;
    public GameObject nextButtonObject;

    [Header("── Audio ────────────────────────────")]
    public AudioClip sfxBubblePop;
    public AudioClip sfxAllTapped;
    public AudioClip sfxBubbleTap;

    [Header("── Timing ───────────────────────────")]
    [Range(0.5f, 3f)]
    public float bubbleSpawnInterval = 1.2f;
    [Range(0f, 1f)]
    public float audioAfterPopDelay = 0.3f;

    // ═════════════════════════════════════════════════════════════════════════
    //  Private State
    // ═════════════════════════════════════════════════════════════════════════

    private List<WordBubble_MagicWords_Reading> _spawnedBubbles =
        new List<WordBubble_MagicWords_Reading>();

    private int  _tappedCount  = 0;
    private bool _allSpawned   = false;
    private bool _introPlaying = false;

    private AudioSource _sfxSource;
    private AudioSource _voiceSource;

    // ═════════════════════════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _sfxSource = GetComponent<AudioSource>();
        if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

        var voiceGO = new GameObject("VoiceSource_Panel1");
        voiceGO.transform.SetParent(transform);
        _voiceSource = voiceGO.AddComponent<AudioSource>();
        _voiceSource.playOnAwake = false;

        if (nextButtonObject != null) nextButtonObject.SetActive(false);
        nextButton?.onClick.AddListener(OnNextClicked);
    }

    private void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;
        _voiceSource.Stop();
        _voiceSource.clip = clip;
        _voiceSource.Play();
    }

    // OnEnable fires every time this panel is shown (SetActive(true))
    // GameManager calls SetActive(true) → this resets and restarts automatically
    void OnEnable()
    {
        ResetPanel();
        StartCoroutine(SpawnBubblesSequence());
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Spawn Sequence
    // ═════════════════════════════════════════════════════════════════════════

    private void ResetPanel()
    {
        StopAllCoroutines();

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

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < magicWords.Length; i++)
        {
            yield return SpawnBubble(i);
            yield return new WaitForSeconds(bubbleSpawnInterval);
        }

        _allSpawned   = true;
        _introPlaying = false;

        CheckAllTapped();
    }

    private IEnumerator SpawnBubble(int index)
    {
        var data = magicWords[index];
        Vector2 pos = (bubblePositions != null && index < bubblePositions.Length)
            ? bubblePositions[index]
            : Vector2.zero;

        if (sparkleFXPrefab != null)
        {
            var fx = Instantiate(sparkleFXPrefab, bubbleContainer);
            var fxRect = fx.GetComponent<RectTransform>();
            if (fxRect != null) fxRect.anchoredPosition = pos;
            Destroy(fx, 1.5f);
        }

        if (sfxBubblePop != null)
            _sfxSource.PlayOneShot(sfxBubblePop);

        var go     = Instantiate(wordBubblePrefab, bubbleContainer);
        var bubble = go.GetComponent<WordBubble_MagicWords_Reading>();
        var rectTr = go.GetComponent<RectTransform>();
        rectTr.anchoredPosition = pos;

        bubble.Initialise(data, index, OnBubbleTapped);
        bubble.PlayPopAnimation();
        _spawnedBubbles.Add(bubble);

        yield return new WaitForSeconds(audioAfterPopDelay);

        if (data.bubbleIntroAudio != null)
        {
            PlayVoice(data.bubbleIntroAudio);
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
        if (sfxBubbleTap != null)
            _sfxSource.PlayOneShot(sfxBubbleTap);

        PlayVoice(data.bubbleIntroAudio);

        _tappedCount++;
        CheckAllTapped();
    }

    private void CheckAllTapped()
    {
        if (_tappedCount >= magicWords.Length && _allSpawned)
        {
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
            StartCoroutine(PunchScale(nextButtonObject.transform, 0.4f));
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  NEXT Button — uses direct reference, NOT static Instance
    // ═════════════════════════════════════════════════════════════════════════

    private void OnNextClicked()
    {
        if (gameManager != null)
            gameManager.GoToPanel2();
        else
            Debug.LogWarning("[Panel1_WordBubbles] gameManager reference not assigned in Inspector!");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═════════════════════════════════════════════════════════════════════════

    private IEnumerator PunchScale(Transform t, float duration)
    {
        t.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;
            float s = p < 0.5f
                ? 4f * p * p * p
                : 1f + Mathf.Sin((p - 0.5f) * Mathf.PI * 3f) * 0.25f * (1f - p);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}