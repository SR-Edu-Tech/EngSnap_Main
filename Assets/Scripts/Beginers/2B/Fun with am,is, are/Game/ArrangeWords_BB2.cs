using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class SentenceArrangeData_BB2
{
    [Tooltip("The 4 words, IN CORRECT ORDER, e.g. 'I', 'am', 'reading', 'a book'")]
    public string[] words = new string[4];
    [Tooltip("Index (0-3) of the am/is/are chit within the words array above — that chit gets coloured and glows on completion")]
    public int beWordIndex;
    [Tooltip("VO of the full correct sentence, e.g. 'I am reading a book.'")]
    public AudioClip fullSentenceAudio;
    [Tooltip("Optional narrator VO played before the tray loads. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Arrange The Words — AmIsAre_BB2.
/// 4 jumbled word chits sit in a tray; 4 fixed slots form the sentence
/// track. Student drags each chit to the slot matching its position in
/// the sentence. A chit dropped on the wrong slot bounces back — no
/// penalty. Once all 4 slots are correctly filled: the full sentence
/// reads aloud, the am/is/are chit glows in its colour, chime plays, then
/// the next sentence loads. Fires OnFinished after all 5 sentences.
/// </summary>
public class ArrangeWords_BB2 : MonoBehaviour
{
    [Header("Sentences — 5, IN ORDER")]
    public SentenceArrangeData_BB2[] sentences = new SentenceArrangeData_BB2[5];

    [Header("Prefab")]
    public ArrangeChit_BB2 chitPrefab;

    [Header("Layout")]
    public Transform trayParent;
    [Tooltip("4 fixed slots, IN ORDER (index 0 = first word position)")]
    public ArrangeSlot_BB2[] slots = new ArrangeSlot_BB2[4];

    [Header("Be-Word Colors (am/is/are)")]
    [SerializeField] private Color amColor      = new Color(1f, 0.4f, 0.7f);
    [SerializeField] private Color isColor      = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color areColor     = new Color(0.4f, 0.85f, 0.4f);
    [SerializeField] private Color defaultColor = Color.black;

    [Header("UI")]
    public CanvasGroup mainCanvasGroup;
    public Button       nextButton;
    public AudioSource  dialogueAudioSource;
    [Tooltip("Generic hint VO for a chit dropped on the wrong slot")]
    public AudioClip    genericWrongHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Timing")]
    [SerializeField] private float placeAnimDuration       = 0.3f;
    [SerializeField] private float glowDuration              = 0.5f;
    [SerializeField] private float delayAfterComplete         = 1.0f;
    [SerializeField] private float beatWithoutNarration       = 0.25f;
    [SerializeField] private float delayBeforeNextButton      = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly List<ArrangeChit_BB2> _spawnedChits = new();
    private readonly Dictionary<int, ArrangeChit_BB2> _placedChits = new();
    private int _sentenceIndex = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i]?.Initialise(i, OnChitDroppedOnSlot);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[ArrangeWords_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _sentenceIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        StartCoroutine(IntroThenLoadSentence(0));

        Debug.Log("[ArrangeWords_BB2] RestartGame — starting from sentence 0");
    }

    private IEnumerator IntroThenLoadSentence(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadSentence(index));
    }

    private IEnumerator LoadSentence(int index)
    {
        ClearSpawnedChits();

        var data = sentences[index];

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

        var order = ShuffleIndices(data.words.Length);
        foreach (int wordIndex in order)
        {
            bool isBeWord = wordIndex == data.beWordIndex;
            var chit = Instantiate(chitPrefab, trayParent);
            chit.Initialise(data.words[wordIndex], wordIndex, isBeWord, Color.white);
            chit.SetTextColor(defaultColor);
            _spawnedChits.Add(chit);
        }

        // Let the tray's layout group (if any) finish arranging the chits
        // BEFORE we read/cache their positions.
        LayoutRebuilder.ForceRebuildLayoutImmediate(trayParent as RectTransform);

        foreach (var chit in _spawnedChits)
        {
            chit.CacheTrayPosition();
            // From here on, ignore any parent Layout Group (tray or slot)
            // so our own code fully controls this chit's position.
            chit.SetIgnoreLayout(true);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Drop handling
    // ════════════════════════════════════════════════════════════════════

    private void OnChitDroppedOnSlot(ArrangeChit_BB2 chit, ArrangeSlot_BB2 slot)
    {
        if (chit.CorrectSlotIndex != slot.slotIndex)
        {
            HandleWrongDrop();
            return;
        }

        StartCoroutine(PlaceChitInSlot(chit, slot));
    }

    private void HandleWrongDrop()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

        if (dialogueAudioSource != null && genericWrongHintClip != null)
        {
            dialogueAudioSource.clip = genericWrongHintClip;
            dialogueAudioSource.Play();
        }
        // chit.OnEndDrag() (fires right after this) handles snapping it back to the tray.
    }

    private IEnumerator PlaceChitInSlot(ArrangeChit_BB2 chit, ArrangeSlot_BB2 slot)
    {
        chit.MarkPlaced();
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        var rect = chit.GetComponent<RectTransform>();
        rect.SetParent(slot.transform, true);

        // Force centered anchors/pivot so anchoredPosition zero lands the
        // chit in the middle of the slot regardless of its original
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

        _placedChits[slot.slotIndex] = chit;

        if (_placedChits.Count >= sentences[_sentenceIndex].words.Length)
            StartCoroutine(SentenceComplete());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Sentence / game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator SentenceComplete()
    {
        var data = sentences[_sentenceIndex];

        if (_placedChits.TryGetValue(data.beWordIndex, out var beChit))
        {
            Color glowColor = ColorForWord(beChit.DisplayText);
            yield return StartCoroutine(GlowChit(beChit, glowColor));
        }

        if (dialogueAudioSource != null && data.fullSentenceAudio != null)
        {
            dialogueAudioSource.clip = data.fullSentenceAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.fullSentenceAudio.length);
        }

        yield return new WaitForSeconds(delayAfterComplete);

        _placedChits.Clear();
        _sentenceIndex++;
        if (_sentenceIndex < sentences.Length)
            yield return StartCoroutine(LoadSentence(_sentenceIndex));
        else
            StartCoroutine(AllSentencesComplete());
    }

    private IEnumerator GlowChit(ArrangeChit_BB2 chit, Color glowColor)
    {
        Color original = defaultColor;
        float e = 0f, half = glowDuration / 2f;
        while (e < half)
        {
            e += Time.deltaTime;
            chit.SetTextColor(Color.Lerp(original, glowColor, e / half));
            yield return null;
        }
        e = 0f;
        while (e < half)
        {
            e += Time.deltaTime;
            chit.SetTextColor(Color.Lerp(glowColor, original, e / half));
            yield return null;
        }
        chit.SetTextColor(glowColor); // settle on the word's colour rather than fading back to black
    }

    private Color ColorForWord(string word) => word.ToLower() switch
    {
        "am"  => amColor,
        "is"  => isColor,
        "are" => areColor,
        _     => defaultColor
    };

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

    private void ClearSpawnedChits()
    {
        foreach (var c in _spawnedChits)
            if (c != null) Destroy(c.gameObject);
        _spawnedChits.Clear();
        _placedChits.Clear();
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
