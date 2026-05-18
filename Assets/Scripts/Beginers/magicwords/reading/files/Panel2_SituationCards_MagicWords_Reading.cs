using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel2_SituationCards_MagicWords_Reading
/// Controls the Situation Card reader screen (Panel 2).
///
/// CHANGE FROM ORIGINAL:
///   - Replaced GameManager_MagicWords_Reading.Instance?.GoToNextUnit() / ReplayFromStart()
///     with a direct [SerializeField] reference.
///     Drag the GameManager GO into the 'gameManager' field in the Inspector.
/// </summary>
public class Panel2_SituationCards_MagicWords_Reading : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  Inspector
    // ═══════════════════════════════════════════════════════════════

    [Header("── Game Manager ─────────────────────")]
    [Tooltip("Drag the GameManager_MagicWords_Reading GO here")]
    public GameManager_MagicWords_Reading gameManager;

    [Header("── Word Data ─────────────────────────")]
    public MagicWordData_MagicWords_Reading[] magicWords;

    [Header("── Card Prefab ──────────────────────")]
    public GameObject    situationCardPrefab;
    public RectTransform cardContainer;

    [Header("── Layout ───────────────────────────")]
    public float cardSpacingX = 40f;
    public float cardWidth    = 340f;

    [Header("── Buttons ──────────────────────────")]
    public Button     nextButton;
    public Button     replayButton;
    public GameObject bottomButtonsObject;

    [Header("── Audio ────────────────────────────")]
    public AudioClip sfxCardSlide;
    public AudioClip sfxCardTap;
    public AudioClip sfxAllCardsDone;

    [Header("── Timing ───────────────────────────")]
    [Range(0.3f, 2f)]
    public float cardToCardPause = 0.9f;

    // ═══════════════════════════════════════════════════════════════
    //  Private
    // ═══════════════════════════════════════════════════════════════

    private List<SituationCard_MagicWords_Reading> _cards =
        new List<SituationCard_MagicWords_Reading>();

    private AudioSource _sfxSource;
    private AudioSource _voiceSource;

    private int  _currentCardIndex    = 0;
    private bool _allCardsShown       = false;
    private bool _autoSequenceRunning = false;

    // ═══════════════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ═══════════════════════════════════════════════════════════════

    void Awake()
    {
        _sfxSource = GetComponent<AudioSource>();
        if (_sfxSource == null) _sfxSource = gameObject.AddComponent<AudioSource>();

        var voiceGO = new GameObject("VoiceSource_Panel2");
        voiceGO.transform.SetParent(transform);
        _voiceSource = voiceGO.AddComponent<AudioSource>();
        _voiceSource.playOnAwake = false;

        if (bottomButtonsObject != null) bottomButtonsObject.SetActive(false);

        nextButton?.onClick.AddListener(OnNextClicked);
        replayButton?.onClick.AddListener(OnReplayClicked);
    }

    private void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;
        _voiceSource.Stop();
        _voiceSource.clip = clip;
        _voiceSource.Play();
    }

    // OnEnable fires every time GameManager sets this panel active
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

        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < magicWords.Length; i++)
        {
            _currentCardIndex = i;
            yield return SpawnAndPlayCard(i);
        }

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

        if (sfxCardSlide != null)
            _sfxSource.PlayOneShot(sfxCardSlide);

        var go   = Instantiate(situationCardPrefab, cardContainer);
        var card = go.GetComponent<SituationCard_MagicWords_Reading>();
        var rect = go.GetComponent<RectTransform>();

        rect.anchoredPosition = new Vector2(index * (cardWidth + cardSpacingX), 0f);

        card.Initialise(data, OnIllustrationTapped, OnWordTextTapped);

        yield return card.PlaySlideIn();
        _cards.Add(card);

        yield return new WaitForSeconds(data.cardAutoPlayDelay);

        if (data.situationAudio != null)
        {
            PlayVoice(data.situationAudio);

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
    //  Tap Callbacks
    // ═══════════════════════════════════════════════════════════════

    private void OnIllustrationTapped(MagicWordData_MagicWords_Reading data)
    {
        if (sfxCardTap != null) _sfxSource.PlayOneShot(sfxCardTap);
        PlayVoice(data.situationAudio);
    }

    private void OnWordTextTapped(MagicWordData_MagicWords_Reading data)
    {
        if (sfxCardTap != null) _sfxSource.PlayOneShot(sfxCardTap);
        PlayVoice(data.wordOnlyAudio != null ? data.wordOnlyAudio : data.situationAudio);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Buttons — use direct reference, NOT static Instance
    // ═══════════════════════════════════════════════════════════════

    private void ShowBottomButtons()
    {
        if (bottomButtonsObject == null) return;
        bottomButtonsObject.SetActive(true);
        StartCoroutine(PunchScale(bottomButtonsObject.transform, 0.45f));
    }

    private void OnNextClicked()
    {
        if (gameManager != null)
            gameManager.GoToNextUnit();
        else
            Debug.LogWarning("[Panel2_SituationCards] gameManager reference not assigned in Inspector!");
    }

    private void OnReplayClicked()
    {
        if (gameManager != null)
            gameManager.ReplayFromStart();
        else
            Debug.LogWarning("[Panel2_SituationCards] gameManager reference not assigned in Inspector!");
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