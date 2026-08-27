using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class BubbleOptionData_GoodHabits_BB2
{
    [Tooltip("Optional label, e.g. 'waking up early'")]
    public string labelText;
    [Tooltip("Picture for this bubble")]
    public Sprite bubbleSprite;
    [Tooltip("True if this is the GOOD habit — the one the student should tap")]
    public bool isGoodHabit;
}

[System.Serializable]
public class RoundData_GoodHabits_BB2
{
    [Tooltip("Exactly 3 options for this round — order they appear in the 3 fixed bubble slots")]
    public BubbleOptionData_GoodHabits_BB2[] bubbles = new BubbleOptionData_GoodHabits_BB2[3];
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Catch The Good Habit — GoodHabits_BB2.
/// 3 FIXED bubble slots show a new set of pictures each round (some good
/// habits, some not). Student taps the good one — it flies into the green
/// basket with a chime + star burst, then the next round loads. Tapping a
/// not-good bubble is never penalised: it wobbles gently, a hint plays,
/// and the round continues (retry within the same round, no reset).
/// Fires OnFinished when Next is pressed after all 8 rounds.
/// </summary>
public class GoodHabitPop_BB2 : MonoBehaviour
{
    [Header("Rounds — 8, IN ORDER")]
    public RoundData_GoodHabits_BB2[] rounds = new RoundData_GoodHabits_BB2[8];

    [Header("UI — Bubble Slots (fixed, 3)")]
    public Button[]   bubbleButtons = new Button[3];
    public Image[]    bubbleImages  = new Image[3];
    public TMP_Text[] bubbleLabels  = new TMP_Text[3];

    [Header("UI — Basket")]
    [Tooltip("Where a correctly-tapped bubble flies to")]
    public RectTransform basketTarget;

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'Not that one — pick the good habit!'")]
    public AudioClip   wrongTapHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Pop FX")]
    public AudioClip bubblesPopSfx;
    [Tooltip("Chime played when the correct bubble lands in the basket")]
    public AudioClip catchChimeSfx;

    [Header("Timing")]
    [SerializeField] private float flyToBasketDuration    = 0.4f;
    [SerializeField] private float delayAfterCorrect       = 0.7f;
    [SerializeField] private float delayBeforeNextButton   = 0.6f;
    [SerializeField] private float popInDuration            = 0.35f;
    [SerializeField] private float popOutDuration           = 0.2f;
    [SerializeField] private float beatWithoutNarration     = 0.25f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;
    private readonly Vector2[] _originalAnchoredPositions = new Vector2[3];

    void Awake()
    {
        for (int i = 0; i < bubbleButtons.Length; i++)
        {
            int capturedIndex = i;
            if (bubbleButtons[i] != null)
            {
                bubbleButtons[i].onClick.RemoveAllListeners();
                bubbleButtons[i].onClick.AddListener(() => OnBubbleTapped(capturedIndex));
                _originalAnchoredPositions[i] = bubbleButtons[i].GetComponent<RectTransform>().anchoredPosition;
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
            Debug.LogWarning("[GoodHabitPop_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        foreach (var btn in bubbleButtons)
            SetScaleZero(btn != null ? btn.GetComponent<RectTransform>() : null);
        SetBubblesInteractable(false);

        StartCoroutine(IntroThenLoadRound(0));

        Debug.Log("[GoodHabitPop_BB2] RestartGame — starting from round 0");
    }

    private IEnumerator IntroThenLoadRound(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadRoundSequence(index, isFirstLoad: true));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Round sequence: pop out old bubbles (if any) → refill data → pop in
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadRoundSequence(int index, bool isFirstLoad)
    {
        SetBubblesInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutBubbles());

        var data = rounds[index];
        for (int i = 0; i < bubbleButtons.Length && i < data.bubbles.Length; i++)
        {
            var option = data.bubbles[i];
            if (bubbleImages != null && i < bubbleImages.Length && bubbleImages[i] != null)
                bubbleImages[i].sprite = option.bubbleSprite;
            if (bubbleLabels != null && i < bubbleLabels.Length && bubbleLabels[i] != null)
                bubbleLabels[i].text = option.labelText;

            var rect = bubbleButtons[i] != null ? bubbleButtons[i].GetComponent<RectTransform>() : null;
            if (rect != null) ResetBubblePosition(rect, i);
        }

        yield return new WaitForSeconds(beatWithoutNarration);

        if (bubblesPopSfx != null) AudioManager.Instance?.PlaySFX(bubblesPopSfx);
        var routines = new List<Coroutine>();
        foreach (var btn in bubbleButtons)
            if (btn != null) routines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;

        SetBubblesInteractable(true);
    }

    private IEnumerator PopOutBubbles()
    {
        var routines = new List<Coroutine>();
        foreach (var btn in bubbleButtons)
            if (btn != null) routines.Add(StartCoroutine(PopOut(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnBubbleTapped(int index)
    {
        var data = rounds[_currentIndex];
        if (index >= data.bubbles.Length) return;

        if (data.bubbles[index].isGoodHabit)
            StartCoroutine(HandleCorrectTap(index));
        else
            StartCoroutine(HandleWrongTap(index));
    }

    private IEnumerator HandleCorrectTap(int index)
    {
        SetBubblesInteractable(false);
        AudioManager.Instance?.PlaySFX(catchChimeSfx != null ? catchChimeSfx : AudioManager.Instance.sfxCorrect);

        var rect = bubbleButtons[index] != null ? bubbleButtons[index].GetComponent<RectTransform>() : null;
        if (rect != null && basketTarget != null)
            yield return StartCoroutine(FlyToBasket(rect));

        if (basketTarget != null) VFXManager.Instance?.SpawnCorrectBurst(basketTarget);

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < rounds.Length)
            StartCoroutine(LoadRoundSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllRoundsComplete());
    }

    private IEnumerator HandleWrongTap(int index)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

        var rect = bubbleButtons[index] != null ? bubbleButtons[index].GetComponent<RectTransform>() : null;
        if (rect != null)
            yield return StartCoroutine(WobbleButton(rect));

        if (dialogueAudioSource != null && wrongTapHintClip != null)
        {
            dialogueAudioSource.clip = wrongTapHintClip;
            dialogueAudioSource.Play();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator FlyToBasket(RectTransform bubbleRect)
    {
        Vector3 startPos = bubbleRect.position;
        Vector3 endPos   = basketTarget.position;
        float e = 0f;
        while (e < flyToBasketDuration)
        {
            e += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, e / flyToBasketDuration);
            bubbleRect.position   = Vector3.Lerp(startPos, endPos, t);
            bubbleRect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }
        bubbleRect.localScale = Vector3.zero;
    }

    private IEnumerator WobbleButton(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 originalScale = t.localScale;
        float e = 0f, dur = 0.3f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float wobble = Mathf.Sin(e * Mathf.PI * 8f) * 0.08f * (1f - e / dur);
            t.localScale = originalScale * (1f + wobble);
            yield return null;
        }
        t.localScale = originalScale;
    }

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

    /// Restores this slot's original scene position (undoing last round's
    /// FlyToBasket move if this slot happened to be the correct bubble)
    /// and resets scale to zero ready for the next PopIn.
    private void ResetBubblePosition(RectTransform rect, int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < _originalAnchoredPositions.Length)
            rect.anchoredPosition = _originalAnchoredPositions[slotIndex];
        rect.localScale = Vector3.zero;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllRoundsComplete()
    {
        SetBubblesInteractable(false);
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

    private void SetBubblesInteractable(bool value)
    {
        foreach (var btn in bubbleButtons)
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
