using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class QAPairData_BB2
{
    [Tooltip("The question, e.g. 'Who is your best friend?'")]
    public string questionText;
    [Tooltip("The matching answer, e.g. 'My best friend is Harry.'")]
    public string answerText;
    [Tooltip("This pair's colour tint — used on the question card and the glow on a correct match")]
    public Color pairColor = Color.white;
    [Tooltip("VO of the question, played first on a correct match")]
    public AudioClip questionAudio;
    [Tooltip("VO of the answer, played after the question on a correct match")]
    public AudioClip answerAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Q &amp; A Match — QuestionWords_BB2.
/// 6 FIXED question cards sit in a left column (placed directly in the
/// scene, not spawned). A tray of 6 shuffled answer cards sits on the
/// right. Student drags each answer onto the question it belongs to.
/// Correct: pair snaps together, glows in the question's colour, question
/// then answer read aloud, chime plays. Wrong: answer bounces back to the
/// tray — no penalty. Fires OnFinished when all 6 pairs are matched.
/// </summary>
public class MatchQA_BB2 : MonoBehaviour
{
    [Header("Pairs — 6, matched 1:1 with the 6 QuestionCardSlot_BB2 below by index")]
    public QAPairData_BB2[] pairs = new QAPairData_BB2[6];

    [Header("Fixed Question Cards (6, left column, placed in scene — same order as pairs above)")]
    public QuestionCardSlot_BB2[] questionSlots = new QuestionCardSlot_BB2[6];

    [Header("Prefab")]
    public AnswerCard_BB2 answerCardPrefab;

    [Header("Layout")]
    public Transform trayParent;

    [Header("UI")]
    public CanvasGroup mainCanvasGroup;
    public Button       nextButton;
    public AudioSource  dialogueAudioSource;
    [Tooltip("Generic hint VO for a wrong match, e.g. 'Hmm — which question?'")]
    public AudioClip    genericWrongHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Timing")]
    [SerializeField] private float placeAnimDuration = 0.3f;
    [SerializeField] private float glowDuration       = 0.5f;
    [SerializeField] private float delayBeforeNextButton = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly List<AnswerCard_BB2> _spawnedCards = new();
    private int _matchedCount = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        for (int i = 0; i < questionSlots.Length && i < pairs.Length; i++)
        {
            int capturedIndex = i;
            questionSlots[i]?.Initialise(i, pairs[i].questionText, pairs[i].pairColor, OnAnswerDroppedOnQuestion);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[MatchQA_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _matchedCount = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        // Re-apply question text/colour in case data changed since Awake.
        for (int i = 0; i < questionSlots.Length && i < pairs.Length; i++)
            questionSlots[i]?.Initialise(i, pairs[i].questionText, pairs[i].pairColor, OnAnswerDroppedOnQuestion);

        StartCoroutine(IntroThenSpawnTray());

        Debug.Log("[MatchQA_BB2] RestartGame — starting fresh tray");
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

        var order = ShuffleIndices(pairs.Length);
        foreach (int idx in order)
        {
            var data = pairs[idx];
            var card = Instantiate(answerCardPrefab, trayParent);
            card.Initialise(idx, data.answerText, data.answerAudio, Color.white);
            _spawnedCards.Add(card);
        }

        // Let the tray's layout group (if any) finish arranging the cards
        // BEFORE we read/cache their positions.
        LayoutRebuilder.ForceRebuildLayoutImmediate(trayParent as RectTransform);

        foreach (var card in _spawnedCards)
        {
            card.CacheTrayPosition();
            // From here on, ignore any parent Layout Group (tray or slot)
            // so our own code fully controls this card's position.
            card.SetIgnoreLayout(true);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Drop handling
    // ════════════════════════════════════════════════════════════════════

    private void OnAnswerDroppedOnQuestion(AnswerCard_BB2 card, QuestionCardSlot_BB2 slot)
    {
        if (card.PairIndex != slot.PairIndex)
        {
            HandleWrongDrop();
            return;
        }

        StartCoroutine(PlaceCardOnQuestion(card, slot));
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

    private IEnumerator PlaceCardOnQuestion(AnswerCard_BB2 card, QuestionCardSlot_BB2 slot)
    {
        card.MarkPlaced();
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        var rect = card.GetComponent<RectTransform>();
        rect.SetParent(slot.transform, true);

        // Force centered anchors/pivot so anchoredPosition zero lands the
        // answer neatly against its question card regardless of its
        // original tray-layout anchors.
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

        // Glow the question card in its pair colour.
        var data = pairs[slot.PairIndex];
        if (slot.background != null)
            yield return StartCoroutine(GlowImage(slot.background, slot.TintColor, data.pairColor));

        if (dialogueAudioSource != null && data.questionAudio != null)
        {
            dialogueAudioSource.clip = data.questionAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.questionAudio.length);
        }
        if (dialogueAudioSource != null && data.answerAudio != null)
        {
            dialogueAudioSource.clip = data.answerAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.answerAudio.length);
        }

        _spawnedCards.Remove(card);
        Destroy(card.gameObject);

        _matchedCount++;
        if (_matchedCount >= pairs.Length)
            StartCoroutine(AllPairsMatched());
    }

    private IEnumerator GlowImage(Image img, Color baseColor, Color glowColor)
    {
        Color brighter = Color.Lerp(glowColor, Color.white, 0.5f);
        float e = 0f, half = glowDuration / 2f;
        while (e < half)
        {
            e += Time.deltaTime;
            img.color = Color.Lerp(baseColor, brighter, e / half);
            yield return null;
        }
        e = 0f;
        while (e < half)
        {
            e += Time.deltaTime;
            img.color = Color.Lerp(brighter, baseColor, e / half);
            yield return null;
        }
        img.color = baseColor;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllPairsMatched()
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
