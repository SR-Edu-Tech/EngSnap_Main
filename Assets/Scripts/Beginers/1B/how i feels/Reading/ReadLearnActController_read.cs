using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ReadLearnActController_read  —  Screen 2
/// ─────────────────────────────────────────────────────────────────────────
/// Spawns 10 sentence cards at runtime from ONE prefab. No manual card setup.
///
/// HIERARCHY (you build this, cards are spawned by script):
///   Screen2_ReadLearnAct
///     ├─ SentenceList          ← Vertical Layout Group — assign to 'sentenceList'
///     ├─ FeelingKidImage       ← Image (sprite swaps per sentence)
///     ├─ ActCounterText        ← TMP_Text "Acted: 0 / 10"
///     ├─ NextButton            ← Button
///     └─ ReplayButton          ← Button
///
/// SENTENCE CARD PREFAB (one prefab, assign to 'cardPrefab'):
///   SentenceCardPrefab         ← SentenceCard_read + Button + Image (bg)
///     └─ SentenceText          ← TMP_Text
/// </summary>
public class ReadLearnActController_read : MonoBehaviour
{
    [System.Serializable]
    public class SentenceData
    {
        public string    sentence;
        public Sprite    feelingKidSprite;
        public AudioClip audioClip;
    }

    [Header("Prefab — ONE card prefab used for all 10")]
    [SerializeField] private SentenceCard_read cardPrefab;

    [Header("Sentence Data (10 entries in order)")]
    [SerializeField] private SentenceData[] sentenceData = new SentenceData[10];

    [Header("Scene Refs")]
    [SerializeField] private Transform  sentenceList;
    [SerializeField] private Image      feelingKidImage;
    [SerializeField] private TMP_Text   actCounterText;
    [SerializeField] private Button     nextButton;
    [SerializeField] private Button     replayButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   introClip;
    [SerializeField] private AudioClip   actingModeClip;
    [SerializeField] private AudioClip   allActedClip;

    [Header("Timing")]
    [SerializeField] private float autoPlayInterval = 0.4f;

    // ── Runtime ──────────────────────────────────────────────────────────
    private GameManager_Reading_read _manager;
    private List<SentenceCard_read>  _cards    = new();
    private HashSet<int>             _actedSet = new();
    private bool                     _actingPhase;

    // ════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════

    public void StartScreen(GameManager_Reading_read manager)
    {
        _manager     = manager;
        _actedSet.Clear();
        _actingPhase = false;

        StopAllCoroutines();
        SpawnCards();
        InitUI();
        StartCoroutine(AutoPlayPhase());
    }

    public void ResetScreen()
    {
        StopAllCoroutines();
        _actedSet.Clear();
        _actingPhase = false;
        if (audioSource != null) audioSource.Stop();
        DestroyCards();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Spawn / destroy
    // ════════════════════════════════════════════════════════════════════

    private void SpawnCards()
    {
        DestroyCards();

        if (cardPrefab == null)
        {
            Debug.LogError("[ReadLearnAct] cardPrefab not assigned!");
            return;
        }

        for (int i = 0; i < sentenceData.Length; i++)
        {
            var card = Instantiate(cardPrefab, sentenceList);
            card.name = $"Card_{i}_{sentenceData[i].sentence}";
            _cards.Add(card);
        }
    }

    private void DestroyCards()
    {
        foreach (var c in _cards)
            if (c != null) Destroy(c.gameObject);
        _cards.Clear();
    }

    // ════════════════════════════════════════════════════════════════════
    //  InitUI
    // ════════════════════════════════════════════════════════════════════

    private void InitUI()
    {
        UpdateCounter();

        if (nextButton   != null) { nextButton.gameObject.SetActive(false);  nextButton.onClick.RemoveAllListeners();  nextButton.onClick.AddListener(OnNextPressed); }
        if (replayButton != null) { replayButton.gameObject.SetActive(false); replayButton.onClick.RemoveAllListeners(); replayButton.onClick.AddListener(OnReplayPressed); }

        if (feelingKidImage != null && sentenceData.Length > 0 && sentenceData[0].feelingKidSprite != null)
            feelingKidImage.sprite = sentenceData[0].feelingKidSprite;

        for (int i = 0; i < _cards.Count; i++)
        {
            _cards[i].Initialise(
                index:    i,
                sentence: sentenceData[i].sentence,
                locked:   true,
                onTapped: OnCardTapped
            );
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Phase 1 — Auto-play
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AutoPlayPhase()
    {
        PlayVO(introClip);
        if (introClip != null) yield return new WaitForSeconds(introClip.length + 0.4f);

        for (int i = 0; i < _cards.Count; i++)
        {
            var data = sentenceData[i];

            _cards[i].SetHighlight(true);
            SwapKid(data.feelingKidSprite);
            PlayVO(data.audioClip);

            float clipLen = data.audioClip != null ? data.audioClip.length : 0.8f;
            yield return new WaitForSeconds(clipLen + autoPlayInterval);

            _cards[i].SetHighlight(false);
        }

        if (sentenceData.Length > 0) SwapKid(sentenceData[0].feelingKidSprite);

        yield return StartCoroutine(ActivateActingPhase());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Phase 2 — Acting mode
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator ActivateActingPhase()
    {
        PlayVO(actingModeClip);
        if (actingModeClip != null) yield return new WaitForSeconds(actingModeClip.length + 0.3f);

        _actingPhase = true;

        for (int i = 0; i < _cards.Count; i++)
        {
            _cards[i].SetLocked(false);
            StartCoroutine(_cards[i].PopIn(i * 0.06f));
        }

        if (replayButton != null) replayButton.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Card tapped
    // ════════════════════════════════════════════════════════════════════

    private void OnCardTapped(int index)
    {
        if (!_actingPhase) return;

        var data = sentenceData[index];
        SwapKid(data.feelingKidSprite);
        PlayVO(data.audioClip);
        _cards[index].PlayTapAnim();

        _actedSet.Add(index);
        UpdateCounter();

        if (_actedSet.Count >= sentenceData.Length)
            StartCoroutine(AllActedSequence());
    }

    private IEnumerator AllActedSequence()
    {
        _actingPhase = false;
        yield return new WaitForSeconds(0.5f);
        PlayVO(allActedClip);
        if (allActedClip != null) yield return new WaitForSeconds(allActedClip.length);
        if (nextButton != null) nextButton.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private void SwapKid(Sprite sprite)
    {
        if (feelingKidImage == null || sprite == null) return;
        feelingKidImage.sprite = sprite;
        StartCoroutine(KidPopIn());
    }

    private IEnumerator KidPopIn()
    {
        Transform t = feelingKidImage.transform;
        float e = 0f, dur = 0.25f;
        while (e < dur)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(e / dur));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private void UpdateCounter()
    {
        if (actCounterText != null)
            actCounterText.text = $"Acted: {_actedSet.Count} / {sentenceData.Length}";
    }

    private void OnNextPressed() => _manager?.OnScreen2Complete();

    private void OnReplayPressed()
    {
        _actedSet.Clear();
        _actingPhase = false;
        if (nextButton   != null) nextButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        DestroyCards();
        SpawnCards();
        InitUI();
        StartCoroutine(AutoPlayPhase());
    }

    private void PlayVO(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}