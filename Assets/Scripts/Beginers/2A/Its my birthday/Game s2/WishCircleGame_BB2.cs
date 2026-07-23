using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// One friend in the birthday wish circle (matches the book's page-12 art).
/// </summary>
[System.Serializable]
public class FriendData_BB2
{
    [Tooltip("Friend illustration.")]
    public Sprite friendSprite;

    [Tooltip("Optional — for designer reference only, not shown in-game.")]
    public string friendName;
}

/// <summary>
/// One birthday wish phrase card, e.g. "Happy birthday!"
/// </summary>
[System.Serializable]
public class WishData_BB2
{
    [Tooltip("Wish text shown on the card.")]
    [TextArea] public string wishText;

    [Tooltip("VO clip read aloud when this wish is given to a friend.")]
    public AudioClip wishAudioClip;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2 (BB3)
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Wish Circle — BB3.
/// CENTRE: 7 friends pre-placed in a circle (friendSlots), matching the
///         book's page-12 illustration.
/// A single draggable gift box (giftBox) is dragged friend to friend.
/// BOTTOM: a tray of 7 unique wish cards, spawned from wishesData —
///         each usable once.
///
/// Flow: drag gift box onto a friend → that friend becomes "active" and
/// the tray highlights → tap a wish → wish is assigned to the active
/// friend (VO plays, card greys out) → gift box is free to drag again.
/// Tapping an already-used wish just wobbles it + plays a nudge line, no
/// penalty, no state change.
///
/// Call RestartGame() every time this screen is (re)entered.
/// Fires OnFinished when Next is pressed — GameManager_BB2 decides what
/// happens next.
/// </summary>
public class WishCircleGame_BB2 : MonoBehaviour
{
    [Header("Data — 7 friends, in circle display order")]
    public FriendData_BB2[] friendsData = new FriendData_BB2[7];

    [Header("Data — 7 unique wishes")]
    public WishData_BB2[] wishesData = new WishData_BB2[7];

    [Header("Friend Slots — 7 pre-placed in the scene, circle layout")]
    public FriendSlot_BB2[] friendSlots = new FriendSlot_BB2[7];

    [Header("Gift Box")]
    public GiftBox_BB2 giftBox;
    [Tooltip("Home/start position for the gift box, e.g. centre of the circle")]
    public RectTransform giftBoxHomeAnchor;

    [Header("Wish Tray")]
    public WishCard_BB2 wishCardPrefab;
    public Transform wishTrayParent;

    [Header("UI")]
    public CanvasGroup mainCanvasGroup;
    public Button nextButton;
    [Tooltip("Plays each wish card's VO clip")]
    public AudioSource dialogueAudioSource;

    [Header("Narrator VO (optional)")]
    public AudioClip introVO;
    public AudioClip tryDifferentWishVO;
    public AudioClip allWishedVO;

    [Header("Timing")]
    [SerializeField] private float delayBeforeOutro = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly List<WishCard_BB2> _wishCards = new();
    private FriendSlot_BB2 _activeFriend;
    private int _wishedCount;

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        StopAllCoroutines();
        ClearTray();

        _activeFriend = null;
        _wishedCount = 0;

        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        SetupFriends();
        SpawnWishTray();

        if (giftBox != null)
            giftBox.ResetToHome(giftBoxHomeAnchor);

       if (introVO != null)
{
    Debug.Log($"[WishCircle] AudioManager.Instance = {AudioManager.Instance}, playing {introVO.name}");
    AudioManager.Instance?.PlayVO(introVO);
}
else
{
    Debug.Log("[WishCircle] introVO is null!");
}

        Debug.Log("[WishCircleGame_BB2] RestartGame — fresh circle");
    }

    private void SetupFriends()
    {
        for (int i = 0; i < friendSlots.Length; i++)
        {
            if (friendSlots[i] == null) continue;
            var data = i < friendsData.Length ? friendsData[i] : null;
            friendSlots[i].Initialise(i, data, OnFriendDropped);
        }
    }

    private void SpawnWishTray()
    {
        for (int i = 0; i < wishesData.Length; i++)
        {
            var card = Instantiate(wishCardPrefab, wishTrayParent);
            card.Initialise(i, wishesData[i], OnWishTapped);
            _wishCards.Add(card);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Gift box dropped on a friend
    // ════════════════════════════════════════════════════════════════════

    private void OnFriendDropped(FriendSlot_BB2 friend)
    {
        if (friend.IsWished) return;

        if (_activeFriend != null)
            _activeFriend.SetActiveHighlight(false);

        _activeFriend = friend;
        _activeFriend.SetActiveHighlight(true);

        if (giftBox != null)
            giftBox.MarkDocked(friend.DockAnchor);

        SetTrayHighlighted(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Wish tapped
    // ════════════════════════════════════════════════════════════════════

    private void OnWishTapped(WishCard_BB2 card)
    {
        if (_activeFriend == null)
        {
            // Gift box hasn't been dropped on a friend yet — gentle nudge.
            card.PlayWobble();
            return;
        }

        if (card.IsUsed)
        {
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);
            card.PlayWobble();
            if (tryDifferentWishVO != null)
                AudioManager.Instance?.PlayVO(tryDifferentWishVO);
            return;
        }

        StartCoroutine(AssignWish(card));
    }

    private IEnumerator AssignWish(WishCard_BB2 card)
    {
        card.SetUsed();
        _activeFriend.SetWished();
        SetTrayHighlighted(false);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);
        VFXManager.Instance?.SpawnCorrectBurst(_activeFriend.GetComponent<RectTransform>());

        yield return StartCoroutine(PlayWishAudio(card.Data));

        _activeFriend = null;
        _wishedCount++;

        if (_wishedCount >= friendSlots.Length)
            StartCoroutine(CompleteSequence());
    }

    private IEnumerator PlayWishAudio(WishData_BB2 data)
    {
        if (dialogueAudioSource != null && data.wishAudioClip != null)
        {
            dialogueAudioSource.clip = data.wishAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.wishAudioClip.length);
        }
        else
        {
            // No audio assigned yet — fall back to a text-length-based pause.
            yield return new WaitForSeconds(Mathf.Max(0.6f, data.wishText.Length * 0.04f));
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  All 7 friends wished
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator CompleteSequence()
    {
        yield return new WaitForSeconds(delayBeforeOutro);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxRoundComplete);
        VFXManager.Instance?.SpawnConfetti();

        if (allWishedVO != null)
        {
            AudioManager.Instance?.PlayVO(allWishedVO);
            yield return new WaitForSeconds(allWishedVO.length);
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

    private void SetTrayHighlighted(bool on)
    {
        foreach (var c in _wishCards)
            if (c != null) c.SetHighlighted(on);
    }

    private void ClearTray()
    {
        foreach (var c in _wishCards)
            if (c != null) Destroy(c.gameObject);
        _wishCards.Clear();
    }
}
