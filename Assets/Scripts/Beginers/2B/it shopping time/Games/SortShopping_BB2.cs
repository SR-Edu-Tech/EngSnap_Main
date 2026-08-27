using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class SortCardData_Shopping_BB2
{
    [Tooltip("The card's word, e.g. 'wallet' or 'bakery' or 'shirt'")]
    public string cardText;
    [Tooltip("Correct basket for this card")]
    public ShoppingCategory_BB2 category;
    [Tooltip("VO read aloud once sorted correctly, e.g. 'Wallet!'")]
    public AudioClip cardAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Fill The Trolley — Shopping_BB2.
/// Player drags each of the 9 cards into the orange THINGS basket, blue
/// SHOPS basket, or green CLOTHES basket. Wrong drops bounce gently back
/// to the tray — no penalty. Correctly sorted cards play their audio,
/// then are destroyed. Fires OnFinished after all 9 cards are sorted.
/// </summary>
public class SortShopping_BB2 : MonoBehaviour
{
    [Header("Cards — 9 total")]
    public SortCardData_Shopping_BB2[] cards = new SortCardData_Shopping_BB2[9];

    [Header("Prefab")]
    public SortShoppingCard_BB2 cardPrefab;

    [Header("Layout")]
    public Transform trayParent;
    public SortShoppingBasket_BB2 thingsBasket;
    public SortShoppingBasket_BB2 shopsBasket;
    public SortShoppingBasket_BB2 clothesBasket;

    [Header("UI")]
    public CanvasGroup mainCanvasGroup;
    public Button       nextButton;
    public AudioSource  dialogueAudioSource;
    [Tooltip("Generic hint VO for a wrong basket drop, e.g. 'Hmm — thing, shop or clothes?'")]
    public AudioClip    genericWrongHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Card Colors")]
    [SerializeField] private Color thingsCardColor  = new Color(1f, 0.65f, 0.3f);
    [SerializeField] private Color shopsCardColor   = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color clothesCardColor = new Color(0.4f, 0.85f, 0.4f);

    [Header("Timing")]
    [SerializeField] private float placeAnimDuration = 0.3f;
    [SerializeField] private float delayBeforeNextButton = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly List<SortShoppingCard_BB2> _spawnedCards = new();
    private int _sortedCount = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        thingsBasket?.Initialise(ShoppingCategory_BB2.Things, OnCardDroppedOnBasket);
        shopsBasket?.Initialise(ShoppingCategory_BB2.Shops, OnCardDroppedOnBasket);
        clothesBasket?.Initialise(ShoppingCategory_BB2.Clothes, OnCardDroppedOnBasket);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[SortShopping_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _sortedCount = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        StartCoroutine(IntroThenSpawnTray());

        Debug.Log("[SortShopping_BB2] RestartGame — starting fresh tray");
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
            var color = data.category switch
            {
                ShoppingCategory_BB2.Things  => thingsCardColor,
                ShoppingCategory_BB2.Shops   => shopsCardColor,
                _                              => clothesCardColor
            };

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

    private void OnCardDroppedOnBasket(SortShoppingCard_BB2 card, SortShoppingBasket_BB2 basket)
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

    private IEnumerator PlaceCardInBasket(SortShoppingCard_BB2 card, SortShoppingBasket_BB2 basket)
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
