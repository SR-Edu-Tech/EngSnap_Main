using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum BuildCardCategory_IsAre_BB2 { Subject, Verb, Word }

[System.Serializable]
public class SentenceBuildData_IsAre_BB2
{
    [Tooltip("Picture cue shown before the sentence is built, e.g. one boy")]
    public Sprite pictureCue;
    [Tooltip("Optional — swapped in once the sentence is complete, e.g. boy helping a friend")]
    public Sprite actionSprite;

    [Tooltip("Correct subject word, e.g. 'He'")]
    public string subjectWord;
    [Tooltip("Correct verb — type 'is' or 'are' (case-insensitive)")]
    public string verbWord;
    [Tooltip("Correct describing word, e.g. 'kind'")]
    public string describingWord;

    [Tooltip("Extra WRONG subject cards to mix into the tray, e.g. 'She', 'They'")]
    public string[] distractorSubjects;
    [Tooltip("Extra WRONG describing-word cards to mix into the tray")]
    public string[] distractorDescribingWords;

    [Tooltip("VO of the full sentence once built, e.g. 'He is kind!'")]
    public AudioClip fullSentenceAudio;
    [Tooltip("Optional — specific hint VO for a wrong IS/ARE drop, e.g. 'One friend takes IS!'")]
    public AudioClip verbHintClip;
}

// ─────────────────────────────────────────────────────────────────────────
//  DRAGGABLE CARD — attach to your build-card prefab
//  (Add Component → search "BuildCard_IsAre_BB2")
//  Assumes a Screen Space - Overlay Canvas.
// ─────────────────────────────────────────────────────────────────────────


// ─────────────────────────────────────────────────────────────────────────
//  SLOT — attach to each of the 3 fixed slots in your build strip
//  (Add Component → search "BuildSlot_IsAre_BB2")
// ─────────────────────────────────────────────────────────────────────────


// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2 (this file's primary/matching-name class)
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Build The Sentence — BB2 (IS/ARE).
/// Player looks at a picture cue, then drags a SUBJECT card, an IS/ARE card,
/// and a describing-word card into three fixed slots to build the sentence.
/// Wrong drops bounce gently back to the tray — no penalty.
/// Fires OnFinished when Next is pressed after all sentences are built.
/// </summary>
public class BuildTheSentence_IsAre_BB2 : MonoBehaviour
{
    [Header("Sentences — 6, IN ORDER")]
    public SentenceBuildData_IsAre_BB2[] sentences = new SentenceBuildData_IsAre_BB2[6];

    [Header("Prefab")]
    public BuildCard_IsAre_BB2 cardPrefab;

    [Header("Layout")]
    public Transform trayParent;
    public BuildSlot_IsAre_BB2 subjectSlot;
    public BuildSlot_IsAre_BB2 verbSlot;
    public BuildSlot_IsAre_BB2 wordSlot;

    [Header("UI")]
    public Image        pictureCueImage;
    public CanvasGroup  mainCanvasGroup;
    public Button        nextButton;
    public AudioSource   dialogueAudioSource;
    [Tooltip("Generic hint VO for a wrong subject/word drop")]
    public AudioClip     genericWrongHintClip;

    [Header("Narration — plays once each")]
    [Tooltip("Plays ONCE at the very start of the screen, before the first sentence loads — e.g. 'Let's build a sentence! Look at the picture...'")]
    public AudioClip introAudioClip;
    [Tooltip("Plays ONCE after the last sentence is built, before the Next button appears — e.g. 'You built every sentence!'")]
    public AudioClip outroAudioClip;

    [Header("Card Colors")]
    [SerializeField] private Color subjectCardColor = Color.white;
    [SerializeField] private Color isCardColor       = new Color(1f, 0.4f, 0.7f);
    [SerializeField] private Color areCardColor      = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color wordCardColor     = Color.white;

    [Header("Timing")]
    [SerializeField] private float placeAnimDuration     = 0.3f;
    [SerializeField] private float delayAfterComplete     = 1.2f;
    [SerializeField] private float delayBeforeNextButton  = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly List<BuildCard_IsAre_BB2> _spawnedCards = new();
    private int  _sentenceIndex = 0;
    private bool _subjectFilled, _verbFilled, _wordFilled;
    private int  _lastRestartFrame = -1;

    void Awake()
    {
        subjectSlot?.Initialise(OnCardDroppedOnSlot);
        verbSlot?.Initialise(OnCardDroppedOnSlot);
        wordSlot?.Initialise(OnCardDroppedOnSlot);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        // Guards against RestartGame() being fired twice in the same click
        // (e.g. a duplicate OnClick() listener) — without this, the second
        // call's StopAllCoroutines() kills the intro audio mid-playback and
        // restarts it, which can make it sound like it never played at all.
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[BuildTheSentence_IsAre_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _sentenceIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;
        StartCoroutine(IntroThenLoadFirstSentence());
        Debug.Log("[BuildTheSentence_IsAre_BB2] RestartGame — starting from sentence 0");
    }

    private IEnumerator IntroThenLoadFirstSentence()
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        LoadSentence(0);
    }

    private void LoadSentence(int index)
    {
        ClearSpawnedCards();
        _subjectFilled = _verbFilled = _wordFilled = false;

        var data = sentences[index];
        if (pictureCueImage != null) pictureCueImage.sprite = data.pictureCue;

        var trayItems = new List<(string text, BuildCardCategory_IsAre_BB2 category, Color color)>();

        trayItems.Add((data.subjectWord, BuildCardCategory_IsAre_BB2.Subject, subjectCardColor));
        if (data.distractorSubjects != null)
            foreach (var s in data.distractorSubjects)
                trayItems.Add((s, BuildCardCategory_IsAre_BB2.Subject, subjectCardColor));

        trayItems.Add(("is",  BuildCardCategory_IsAre_BB2.Verb, isCardColor));
        trayItems.Add(("are", BuildCardCategory_IsAre_BB2.Verb, areCardColor));

        trayItems.Add((data.describingWord, BuildCardCategory_IsAre_BB2.Word, wordCardColor));
        if (data.distractorDescribingWords != null)
            foreach (var w in data.distractorDescribingWords)
                trayItems.Add((w, BuildCardCategory_IsAre_BB2.Word, wordCardColor));

        Shuffle(trayItems);

        foreach (var item in trayItems)
        {
            var card = Instantiate(cardPrefab, trayParent);
            card.Initialise(item.text, item.category, item.color);
            _spawnedCards.Add(card);
        }

        // Let the tray's Horizontal/Grid Layout Group finish arranging the
        // freshly-spawned cards BEFORE we read/cache their positions.
        LayoutRebuilder.ForceRebuildLayoutImmediate(trayParent as RectTransform);

        foreach (var card in _spawnedCards)
        {
            card.CacheTrayPosition();
            // From here on, this card is invisible to any Layout Group it's
            // parented under (tray or slot) — our own code fully controls
            // its position for dragging, snap-back, and slot placement.
            card.SetIgnoreLayout(true);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Drop handling
    // ════════════════════════════════════════════════════════════════════

    private void OnCardDroppedOnSlot(BuildCard_IsAre_BB2 card, BuildSlot_IsAre_BB2 slot)
    {
        var data = sentences[_sentenceIndex];

        if (card.Category != slot.category)
        {
            HandleWrongDrop(slot.category);
            return;
        }

        bool correct = false;
        switch (slot.category)
        {
            case BuildCardCategory_IsAre_BB2.Subject:
                correct = string.Equals(card.DisplayText, data.subjectWord, System.StringComparison.OrdinalIgnoreCase);
                break;
            case BuildCardCategory_IsAre_BB2.Verb:
                correct = string.Equals(card.DisplayText, data.verbWord, System.StringComparison.OrdinalIgnoreCase);
                break;
            case BuildCardCategory_IsAre_BB2.Word:
                correct = string.Equals(card.DisplayText, data.describingWord, System.StringComparison.OrdinalIgnoreCase);
                break;
        }

        if (!correct)
        {
            HandleWrongDrop(slot.category);
            return;
        }

        StartCoroutine(PlaceCardInSlot(card, slot));
    }

    private void HandleWrongDrop(BuildCardCategory_IsAre_BB2 attemptedCategory)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

        var data = sentences[_sentenceIndex];
        AudioClip hint = (attemptedCategory == BuildCardCategory_IsAre_BB2.Verb && data.verbHintClip != null)
            ? data.verbHintClip
            : genericWrongHintClip;

        if (dialogueAudioSource != null && hint != null)
        {
            dialogueAudioSource.clip = hint;
            dialogueAudioSource.Play();
        }
        // card.OnEndDrag() (fires right after this) handles snapping it back to the tray.
    }

    private IEnumerator PlaceCardInSlot(BuildCard_IsAre_BB2 card, BuildSlot_IsAre_BB2 slot)
    {
        card.MarkPlaced();
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        var rect = card.GetComponent<RectTransform>();
        rect.SetParent(slot.transform, true);

        // Force centered anchors/pivot — the card prefab's original anchors
        // (often top-left, inherited from tray layout) would otherwise make
        // anchoredPosition zero land in a corner of the slot instead of its
        // center.
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

        switch (slot.category)
        {
            case BuildCardCategory_IsAre_BB2.Subject: _subjectFilled = true; break;
            case BuildCardCategory_IsAre_BB2.Verb:    _verbFilled    = true; break;
            case BuildCardCategory_IsAre_BB2.Word:    _wordFilled    = true; break;
        }

        if (_subjectFilled && _verbFilled && _wordFilled)
            StartCoroutine(SentenceComplete());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Sentence / game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator SentenceComplete()
    {
        var data = sentences[_sentenceIndex];

        VFXManager.Instance?.SpawnConfetti();

        if (pictureCueImage != null && data.actionSprite != null)
            pictureCueImage.sprite = data.actionSprite;

        if (dialogueAudioSource != null && data.fullSentenceAudio != null)
        {
            dialogueAudioSource.clip = data.fullSentenceAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.fullSentenceAudio.length);
        }

        yield return new WaitForSeconds(delayAfterComplete);

        _sentenceIndex++;
        if (_sentenceIndex < sentences.Length)
            LoadSentence(_sentenceIndex);
        else
            StartCoroutine(AllSentencesComplete());
    }

    private IEnumerator AllSentencesComplete()
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

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}