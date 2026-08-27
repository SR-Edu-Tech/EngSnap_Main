using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class SortCardData_GoodHabits_BB2
{
    [Tooltip("The card's sentence, e.g. 'I wake up early' or 'I will be kind'")]
    public string cardText;
    [Tooltip("Correct basket for this card")]
    public HabitOrQuality_GoodHabits_BB2 category;
    [Tooltip("VO read aloud once sorted correctly")]
    public AudioClip cardAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Sort It Out — GoodHabits_BB2.
/// Player drags each of the 8 cards into the green HABIT basket (a daily
/// "I do..." action) or the orange QUALITY basket (an "I will..." promise).
/// Wrong drops bounce gently back to the tray — no penalty. Correctly
/// sorted cards play their audio, then are destroyed (their job is done).
/// Fires OnFinished when Next is pressed after all 8 cards are sorted.
/// </summary>
public class SortHabitsOrQualities_BB2 : MonoBehaviour
{
    [Header("Cards — 8 total")]
    public SortCardData_GoodHabits_BB2[] cards = new SortCardData_GoodHabits_BB2[8];

    [Header("Prefab")]
    public SortCard_GoodHabits_BB2 cardPrefab;

    [Header("Layout")]
    public Transform trayParent;
    public SortBasket_GoodHabits_BB2 habitBasket;
    public SortBasket_GoodHabits_BB2 qualityBasket;

    [Header("UI")]
    public CanvasGroup mainCanvasGroup;
    public Button       nextButton;
    public AudioSource  dialogueAudioSource;
    [Tooltip("Generic hint VO for a wrong basket drop, e.g. 'Hmm — habit or promise?'")]
    public AudioClip    genericWrongHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Card Colors")]
    [SerializeField] private Color habitCardColor   = new Color(0.4f, 0.85f, 0.4f);
    [SerializeField] private Color qualityCardColor = new Color(1f, 0.65f, 0.3f);

    [Header("Timing")]
    [SerializeField] private float placeAnimDuration = 0.3f;
    [SerializeField] private float delayBeforeNextButton = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly List<SortCard_GoodHabits_BB2> _spawnedCards = new();
    private int _sortedCount = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        habitBasket?.Initialise(HabitOrQuality_GoodHabits_BB2.Habit, OnCardDroppedOnBasket);
        qualityBasket?.Initialise(HabitOrQuality_GoodHabits_BB2.Quality, OnCardDroppedOnBasket);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[SortHabitsOrQualities_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _sortedCount = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        StartCoroutine(IntroThenSpawnTray());

        Debug.Log("[SortHabitsOrQualities_BB2] RestartGame — starting fresh tray");
    }

    private IEnumerator IntroThenSpawnTray()
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        SpawnTray();
    }

    private void SpawnTray()
    {
        ClearSpawnedCards();

        var order = ShuffleIndices(cards.Length);
        foreach (int idx in order)
        {
            var data  = cards[idx];
            var color = data.category == HabitOrQuality_GoodHabits_BB2.Habit ? habitCardColor : qualityCardColor;

            var card = Instantiate(cardPrefab, trayParent);
            card.Initialise(data.cardText, data.category, data.cardAudio, color);
            _spawnedCards.Add(card);
        }

        // Let the tray's layout group (if any) finish arranging the cards
        // BEFORE we read/cache their positions.
        LayoutRebuilder.ForceRebuildLayoutImmediate(trayParent as RectTransform);

        foreach (var card in _spawnedCards)
        {
            card.CacheTrayPosition();
            // From here on, ignore any parent Layout Group (tray or basket)
            // so our own code fully controls this card's position.
            card.SetIgnoreLayout(true);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Drop handling
    // ════════════════════════════════════════════════════════════════════

    private void OnCardDroppedOnBasket(SortCard_GoodHabits_BB2 card, SortBasket_GoodHabits_BB2 basket)
    {
        if (card.Category != basket.basketCategory)
        {
            HandleWrongDrop();
            return;
        }

        StartCoroutine(PlaceCardInBasket(card, basket));
    }

    private void HandleWrongDrop()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

        if (dialogueAudioSource != null && genericWrongHintClip != null)
        {
            dialogueAudioSource.clip = genericWrongHintClip;
            dialogueAudioSource.Play();
        }
        // card.OnEndDrag() (fires right after this) handles snapping it back to the tray.
    }

    private IEnumerator PlaceCardInBasket(SortCard_GoodHabits_BB2 card, SortBasket_GoodHabits_BB2 basket)
    {
        card.MarkPlaced();
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        var rect = card.GetComponent<RectTransform>();
        rect.SetParent(basket.transform, true);

        // Force centered anchors/pivot so anchoredPosition zero lands the
        // card in the middle of the basket regardless of its original
        // tray-layout anchors.
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);

        Vector2 startPos = rect.anchoredPosition;
        float e = 0f;
        while (e < placeAnimDuration)
        {
            e += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, e / placeAnimDuration);
            yield return null;
        }
        rect.anchoredPosition = Vector2.zero;

        VFXManager.Instance?.SpawnCorrectBurst(rect);

        if (dialogueAudioSource != null && card.CardAudio != null)
        {
            dialogueAudioSource.clip = card.CardAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(card.CardAudio.length);
        }

        _spawnedCards.Remove(card);
        Destroy(card.gameObject);

        _sortedCount++;
        if (_sortedCount >= cards.Length)
            StartCoroutine(AllCardsSorted());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllCardsSorted()
    {
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

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private void ClearSpawnedCards()
    {
        foreach (var c in _spawnedCards)
            if (c != null) Destroy(c.gameObject);
        _spawnedCards.Clear();
    }

    private static List<int> ShuffleIndices(int count)
    {
        var list = new List<int>();
        for (int i = 0; i < count; i++) list.Add(i);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
