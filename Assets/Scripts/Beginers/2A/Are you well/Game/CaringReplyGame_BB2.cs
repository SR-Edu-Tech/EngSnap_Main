using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class ReplyCardData_CaringReply_BB2
{
    [Tooltip("The caring reply text shown on this card, e.g. 'Get some rest. I hope you get better soon.'")]
    [TextArea] public string replyText;
    [Tooltip("VO read aloud when this reply is chosen")]
    public AudioClip replyAudio;
}

[System.Serializable]
public class FriendData_CaringReply_BB2
{
    [Tooltip("How the friend feels, e.g. 'I have a cold.'")]
    public string feelingText;
    [Tooltip("Friend's picture before being comforted")]
    public Sprite friendSprite;
    [Tooltip("Optional — friend's picture once comforted, e.g. smiling. Leave empty to keep the same sprite.")]
    public Sprite comfortedSprite;
    [Tooltip("Optional narrator VO of the friend's line. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
    [Tooltip("Check this for bigger ailments (toothache, stomachache) where the DOCTOR reply is the kindest choice. If checked and the student picks a different reply, a gentle nudge plays — but it still counts, no penalty.")]
    public bool needsDoctorNudge;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Caring Reply — CaringReply_BB2.
/// A friend appears and says how they feel. Three FIXED reply cards (same
/// three caring sentences every round) let the student pick a kind reply.
/// EVERY reply is accepted — there is no wrong answer. For the two bigger
/// ailments (toothache, stomachache) flagged via needsDoctorNudge, picking
/// a reply other than the doctor card (index 2) plays one gentle nudge
/// line first, then still proceeds normally — no penalty, no retry forced.
/// Fires OnFinished when Next is pressed after all 6 friends are helped.
/// </summary>
public class CaringReplyGame_BB2 : MonoBehaviour
{
    [Header("Friends — 6, IN ORDER")]
    public FriendData_CaringReply_BB2[] friends = new FriendData_CaringReply_BB2[6];

    [Header("Reply Cards — 3 FIXED cards, reused every round")]
    [Tooltip("Index 2 MUST be the 'Let's see the doctor...' reply — the doctor nudge check relies on this order.")]
    public ReplyCardData_CaringReply_BB2[] replyCards = new ReplyCardData_CaringReply_BB2[3];
    [Tooltip("The 3 tappable card Buttons in the scene, same order as replyCards above")]
    public Button[]   replyCardButtons  = new Button[3];
    [Tooltip("Text component on each card Button, same order")]
    public TMP_Text[] replyCardTexts    = new TMP_Text[3];

    private const int DoctorReplyIndex = 2;

    [Header("UI — Friend")]
    public TMP_Text friendFeelingText;
    public Image     friendImage;

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button       nextButton;
    public AudioSource  dialogueAudioSource;
    [Tooltip("Gentle nudge VO for the doctor ailments, e.g. \"That's kind! For this, a doctor can help best.\"")]
    public AudioClip    doctorNudgeClip;

    [Header("Narration — plays once each")]
    [Tooltip("Plays ONCE at the very start — e.g. 'Your friend feels unwell. What will you kindly say?'")]
    public AudioClip introAudioClip;
    [Tooltip("Plays ONCE after the 6th friend — e.g. 'You are a caring friend!'")]
    public AudioClip outroAudioClip;

    [Header("Pop FX")]
    public AudioClip contentPopSfx;
    public AudioClip cardsPopSfx;

    [Header("Timing")]
    [SerializeField] private float popInDuration                 = 0.35f;
    [SerializeField] private float popOutDuration                = 0.2f;
    [SerializeField] private float beatWithoutNarration          = 0.25f;
    [SerializeField] private float delayBetweenContentAndCards   = 0.15f;
    [SerializeField] private float delayAfterComfort             = 0.9f;
    [SerializeField] private float delayBeforeNextButton         = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        for (int i = 0; i < replyCardButtons.Length; i++)
        {
            int capturedIndex = i;
            if (replyCardButtons[i] != null)
            {
                replyCardButtons[i].onClick.RemoveAllListeners();
                replyCardButtons[i].onClick.AddListener(() => OnReplyTapped(capturedIndex));
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[CaringReplyGame_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        SetScaleZero(friendFeelingText != null ? friendFeelingText.rectTransform : null);
        SetScaleZero(friendImage != null ? friendImage.rectTransform : null);
        foreach (var btn in replyCardButtons)
            SetScaleZero(btn != null ? btn.GetComponent<RectTransform>() : null);
        SetCardsInteractable(false);

        // Fixed card texts only need setting once — they never change.
        for (int i = 0; i < replyCardTexts.Length && i < replyCards.Length; i++)
            if (replyCardTexts[i] != null) replyCardTexts[i].text = replyCards[i].replyText;

        StartCoroutine(IntroThenLoadFriend(0));

        Debug.Log("[CaringReplyGame_BB2] RestartGame — starting from friend 0");
    }

    private IEnumerator IntroThenLoadFriend(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadFriendSequence(index, isFirstLoad: true));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Friend reveal sequence: narrator → pop content → pop cards
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadFriendSequence(int index, bool isFirstLoad)
    {
        SetCardsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCurrent());

        var data = friends[index];
        if (friendFeelingText != null) friendFeelingText.text  = data.feelingText;
        if (friendImage       != null) friendImage.sprite      = data.friendSprite;

        if (dialogueAudioSource != null && data.promptAudio != null)
        {
            dialogueAudioSource.clip = data.promptAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.promptAudio.length);
        }
        else
        {
            yield return new WaitForSeconds(beatWithoutNarration);
        }

        if (contentPopSfx != null) AudioManager.Instance?.PlaySFX(contentPopSfx);
        var contentRoutines = new List<Coroutine>();
        if (friendFeelingText != null) contentRoutines.Add(StartCoroutine(PopIn(friendFeelingText.rectTransform)));
        if (friendImage       != null) contentRoutines.Add(StartCoroutine(PopIn(friendImage.rectTransform)));
        foreach (var r in contentRoutines) yield return r;

        yield return new WaitForSeconds(delayBetweenContentAndCards);

        if (cardsPopSfx != null) AudioManager.Instance?.PlaySFX(cardsPopSfx);
        var cardRoutines = new List<Coroutine>();
        foreach (var btn in replyCardButtons)
            if (btn != null) cardRoutines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in cardRoutines) yield return r;

        SetCardsInteractable(true);
    }

    private IEnumerator PopOutCurrent()
    {
        var routines = new List<Coroutine>();
        if (friendFeelingText != null) routines.Add(StartCoroutine(PopOut(friendFeelingText.rectTransform)));
        if (friendImage       != null) routines.Add(StartCoroutine(PopOut(friendImage.rectTransform)));
        foreach (var btn in replyCardButtons)
            if (btn != null) routines.Add(StartCoroutine(PopOut(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling — every reply is accepted, no wrong answers
    // ════════════════════════════════════════════════════════════════════

    private void OnReplyTapped(int replyIndex)
    {
        StartCoroutine(HandleReplyChosen(replyIndex));
    }

    private IEnumerator HandleReplyChosen(int replyIndex)
    {
        SetCardsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        var friend = friends[_currentIndex];

        // Gentle nudge for the bigger ailments if a non-doctor reply was picked.
        // Still counts — this is guidance, not a penalty or a required retry.
        if (friend.needsDoctorNudge && replyIndex != DoctorReplyIndex)
        {
            if (dialogueAudioSource != null && doctorNudgeClip != null)
            {
                dialogueAudioSource.clip = doctorNudgeClip;
                dialogueAudioSource.Play();
                yield return new WaitForSeconds(doctorNudgeClip.length);
            }
        }

        // Friend feels comforted.
        if (friendImage != null && friend.comfortedSprite != null)
            friendImage.sprite = friend.comfortedSprite;

        VFXManager.Instance?.SpawnCorrectBurst(friendImage != null ? friendImage.rectTransform : transform as RectTransform);

        var reply = replyCards[replyIndex];
        if (dialogueAudioSource != null && reply.replyAudio != null)
        {
            dialogueAudioSource.clip = reply.replyAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(reply.replyAudio.length);
        }

        yield return new WaitForSeconds(delayAfterComfort);

        _currentIndex++;
        if (_currentIndex < friends.Length)
            StartCoroutine(LoadFriendSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllFriendsHelped());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator PopIn(RectTransform t)
    {
        if (t == null) yield break;
        t.localScale = Vector3.zero;
        float e = 0f;
        while (e < popInDuration)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, EaseOutBack(e / popInDuration));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private IEnumerator PopOut(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 start = t.localScale;
        float e = 0f;
        while (e < popOutDuration)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.Lerp(start, Vector3.zero, e / popOutDuration);
            yield return null;
        }
        t.localScale = Vector3.zero;
    }

    private static void SetScaleZero(RectTransform t)
    {
        if (t != null) t.localScale = Vector3.zero;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllFriendsHelped()
    {
        SetCardsInteractable(false);
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

    private void SetCardsInteractable(bool value)
    {
        foreach (var btn in replyCardButtons)
            if (btn != null) btn.interactable = value;
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
