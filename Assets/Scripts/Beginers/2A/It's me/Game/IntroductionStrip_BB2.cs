using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Introduction Strip — BB2.
/// TOP:    an empty strip with 6 blank slots (assign 6 RectTransforms to
///         `slots`, left to right, already positioned in your scene).
/// CENTRE: a tray of 6 shuffled cards (spawned at runtime from `cards`).
///
/// Player taps cards in the correct intro order. `cards` array order IS the
/// correct order — index 0 must be tapped first, index 5 last.
/// Each correct tap: card animates into its slot + sentence reads aloud.
/// After all 6: short pause, full introduction plays back start to finish,
/// then the Next button appears.
///
/// Call RestartGame() every time this screen is (re)entered — it clears any
/// leftover cards from a previous playthrough and reshuffles fresh.
/// Fires OnFinished when Next is pressed — the GameManager decides what
/// happens next (open the unit panel), this script has no knowledge of it.
/// </summary>
public class IntroductionStrip_BB2 : MonoBehaviour
{
    [Header("Card Data — 6 cards, IN CORRECT INTRO ORDER (index 0 = tapped first)")]
    public IntroCardData_BB2[] cards = new IntroCardData_BB2[6];

    [Header("Prefab")]
    public IntroCard_BB2 cardPrefab;

    [Header("Layout")]
    [Tooltip("Parent that shuffled tray cards spawn into")]
    public Transform trayParent;
    [Tooltip("6 empty slot RectTransforms in the strip, LEFT TO RIGHT, already positioned in the scene")]
    public RectTransform[] slots;

    [Header("UI")]
    public CanvasGroup mainCanvasGroup;
    public Button       nextButton;
    [Tooltip("Plays each card's VO clip — both on placement and during the full-intro replay")]
    public AudioSource  dialogueAudioSource;

    [Header("Timing")]
    [SerializeField] private float placeAnimDuration   = 0.35f;
    [SerializeField] private float delayBeforeFullPlay = 0.5f;
    [SerializeField] private float delayBetweenLines   = 0.25f;

    /// Fired when the Next button is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly List<IntroCard_BB2> _spawnedCards = new();
    private int _nextExpectedIndex = 0;

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        StopAllCoroutines();
        ClearSpawnedCards();

        _nextExpectedIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        SpawnShuffledTray();

        Debug.Log("[IntroductionStrip_BB2] RestartGame — fresh tray spawned");
    }

    private void SpawnShuffledTray()
    {
        var order = ShuffleIndices(cards.Length);
        foreach (int idx in order)
        {
            var card = Instantiate(cardPrefab, trayParent);
            card.Initialise(idx, cards[idx], OnCardTapped);
            _spawnedCards.Add(card);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnCardTapped(IntroCard_BB2 card)
    {
        if (card.OrderIndex == _nextExpectedIndex)
            StartCoroutine(HandleCorrectTap(card));
        else
            StartCoroutine(HandleWrongTap(card));
    }

    private IEnumerator HandleCorrectTap(IntroCard_BB2 card)
    {
        card.MarkPlaced();
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        RectTransform slot = slots[_nextExpectedIndex];
        yield return StartCoroutine(MoveCardToSlot(card.GetComponent<RectTransform>(), slot));

        yield return StartCoroutine(PlayCardAudio(card.Data));

        _nextExpectedIndex++;

        if (_nextExpectedIndex >= cards.Length)
            StartCoroutine(PlayFullIntroduction());
    }

    private IEnumerator HandleWrongTap(IntroCard_BB2 card)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);
        VFXManager.Instance?.ScreenShake(6f, 0.15f);
        yield return StartCoroutine(ShakeCard(card.GetComponent<RectTransform>()));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

   private IEnumerator MoveCardToSlot(RectTransform cardRect, RectTransform slot)
{
    // Save world position
    Vector3 worldPos = cardRect.position;

    // Parent while keeping world position
    cardRect.SetParent(slot, true);

    // Restore world position
    cardRect.position = worldPos;

    // Force layout immediately
    Canvas.ForceUpdateCanvases();
    LayoutRebuilder.ForceRebuildLayoutImmediate(slot);

    Vector2 start = cardRect.anchoredPosition;

    float t = 0f;

    while (t < placeAnimDuration)
    {
        t += Time.deltaTime;

        float p = Mathf.SmoothStep(0f, 1f, t / placeAnimDuration);

        cardRect.anchoredPosition =
            Vector2.Lerp(start, Vector2.zero, p);

        cardRect.localScale =
            Vector3.Lerp(Vector3.one * 1.1f, Vector3.one, p);

        yield return null;
    }

    cardRect.anchoredPosition = Vector2.zero;
    cardRect.localScale = Vector3.one;
}

    private IEnumerator ShakeCard(RectTransform t)
    {
        Vector2 origin = t.anchoredPosition;
        float e = 0f, dur = 0.35f, mag = 18f;
        while (e < dur)
        {
            float x = Mathf.Sin(e * Mathf.PI * 12f) * mag * (1f - e / dur);
            t.anchoredPosition = origin + new Vector2(x, 0f);
            e += Time.deltaTime;
            yield return null;
        }
        t.anchoredPosition = origin;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Full playback once all 6 slots are filled
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator PlayFullIntroduction()
    {
        yield return new WaitForSeconds(delayBeforeFullPlay);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxRoundComplete);
        VFXManager.Instance?.SpawnConfetti();

        for (int i = 0; i < cards.Length; i++)
        {
            yield return StartCoroutine(PlayCardAudio(cards[i]));
            yield return new WaitForSeconds(delayBetweenLines);
        }

        nextButton?.gameObject.SetActive(true);
    }

    private IEnumerator PlayCardAudio(IntroCardData_BB2 data)
    {
        if (dialogueAudioSource != null && data.audioClip != null)
        {
            dialogueAudioSource.clip = data.audioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.audioClip.length);
        }
        else
        {
            // No audio assigned yet — fall back to a text-length-based pause
            // so the flow still feels readable during development.
            yield return new WaitForSeconds(Mathf.Max(0.6f, data.sentenceText.Length * 0.04f));
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
