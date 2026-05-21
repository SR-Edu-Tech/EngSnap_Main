using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// One entry for a vocabulary card — same pattern as ActionWordEntry.
/// Assign all fields per card in the Inspector — no index mismatch possible.
/// </summary>
[System.Serializable]
public class ReviewWordEntry
{
    [Tooltip("Word shown on the card banner, e.g. 'Sing'")]
    public string word;

    [Tooltip("Illustration sprite on the top half of the card")]
    public Sprite cardIllustration;

    [Tooltip("Mascot sprite swapped to when this word is active")]
    public Sprite mascotSprite;

    [Tooltip("Audio clip played when this card is shown or tapped")]
    public AudioClip wordAudio;
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// AllActionsReviewController — Screen 3 of the Let's Act unit.
/// Implements IUnitCompletable so SharedUnitPanelController can open/close it.
///
/// FLOW:
///   Part A — 3 new word cards appear one by one (Sing / Walk / Dance).
///            Mascot sprite swaps per card. Tap any revealed card to replay.
///   Part B — Carousel of all 15 action cards appears below Part A.
///            Student swipes/scrolls and taps any card to hear it.
///            After tapping 5+ unique cards → NEXT button activates.
///   Done   — UnitFinished() called → returns to unit panel.
///
/// HIERARCHY EXAMPLE:
///   AllActionsReviewRoot          ← add AllActionsReviewController here
///     ├── PartA_Root              ← drag to partARoot
///     │     ├── CardsGrid         ← drag to partACardsGrid   (HorizontalLayoutGroup)
///     │     ├── MascotImage       ← drag to mascotImage
///     │     ├── WordToastText     ← drag to wordToastText (TMP)
///     │     └── SnailButton       ← drag to snailButton
///     ├── PartB_Root              ← drag to partBRoot (starts hidden)
///     │     ├── PartBLabel        ← drag to partBLabel (TMP)
///     │     └── ScrollRect        ← drag to carouselScrollRect
///     │           └── Viewport
///     │                 └── Content ← drag to carouselContent
///     ├── ReplayButton            ← drag to replayButton
///     ├── NextButton              ← drag to nextButton (starts hidden)
///     └── CompletionScreen        ← drag to completionScreen
///           └── DoneButton        ← drag to doneButton
///
/// INSPECTOR WIRING:
///   partACards  → 3 entries  (Sing, Walk, Dance)
///   allCards    → 15 entries (Read … Dance in full order)
///   Each entry  = word + cardIllustration + mascotSprite + wordAudio
///
///   mascotImage         → the Image component for the mascot
///   mascotIdleSprite    → default idle sprite
///   cardPrefab          → prefab with AllActionsCard on root
///   carouselCardPrefab  → same prefab (or a different size prefab) for carousel
///   tapCountToUnlockNext → default 5 — how many unique carousel cards tapped
///                          before NEXT button appears
///
///   SFX: sfx_cardPop · sfx_tap · sfx_partBReveal · sfx_allDone
///        sfx_nextScreen · sfx_completion
/// </summary>
public class AllActionsReviewController_LetsAct_ReadingBB1 : MonoBehaviour, IUnitCompletable
{
    // ── IUnitCompletable ──────────────────────────────────────────────────
    private SharedUnitPanelController _panel;
    private SharedUnitButton          _unitButton;

    public void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button)
    {
        _panel      = panel;
        _unitButton = button;
        StartGame();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — PART A
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Part A — New Words ─────────────────────────────")]
    public GameObject partARoot;
    public Transform  partACardsGrid;       // HorizontalLayoutGroup parent
    public Image      mascotImage;
    public Sprite     mascotIdleSprite;
    public TMP_Text   wordToastText;
    public Button     snailButton;

    [Space]
    [Tooltip("3 new word cards for Part A. Each entry = word + illustration + mascot sprite + audio.")]
    public ReviewWordEntry[] partACards = new ReviewWordEntry[3];

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — PART B CAROUSEL
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Part B — All 15 Actions Carousel ────────────────")]
    public GameObject  partBRoot;           // starts hidden; shown after Part A
    public TMP_Text    partBLabel;          // "All the actions you know!"
    public ScrollRect  carouselScrollRect;  // the ScrollRect on the carousel
    public Transform   carouselContent;    // Content child of the ScrollRect

    [Space]
    [Tooltip("All 15 action word entries in display order.")]
    public ReviewWordEntry[] allCards = new ReviewWordEntry[15];

    [Tooltip("How many unique carousel cards the student must tap before NEXT appears.")]
    public int tapCountToUnlockNext = 5;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — BUTTONS / COMPLETION
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Buttons & Completion ──────────────────────────────")]
    public Button     replayButton;
    public Button     nextButton;
    public GameObject completionScreen;
    public Button     doneButton;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — PREFABS
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Card Prefabs ─────────────────────────────────────")]
    [Tooltip("Prefab with AllActionsCard_LetsAct_ReadingBB1 on root — used for Part A cards")]
    public AllActionsCard_LetsAct_ReadingBB1 partACardPrefab;

    [Tooltip("Prefab for carousel cards (can be same prefab or a smaller variant)")]
    public AllActionsCard_LetsAct_ReadingBB1 carouselCardPrefab;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — AUDIO (celebration / reveal SFX)
    // ═════════════════════════════════════════════════════════════════════
    [Header("── SFX ─────────────────────────────────────────────")]
    public AudioClip sfx_cardPop;
    public AudioClip sfx_tap;
    public AudioClip sfx_partBReveal;   // plays when carousel appears
    public AudioClip sfx_allDone;       // plays after Part A complete
    public AudioClip sfx_nextScreen;
    public AudioClip sfx_completion;
    public AudioClip sfx_carouselUnlock; // plays when NEXT unlocks after 5 taps

    [Header("── Celebration Audio ───────────────────────────────")]
    [Tooltip("'You know all 15 actions! Swipe and tap any!' voice line")]
    public AudioClip audio_youKnowAll15;

    // ═════════════════════════════════════════════════════════════════════
    //  RUNTIME STATE
    // ═════════════════════════════════════════════════════════════════════
    private AudioSource _audio;

    private AllActionsCard_LetsAct_ReadingBB1[] _partASpawned;
    private AllActionsCard_LetsAct_ReadingBB1[] _carouselSpawned;

    private int  _partARevealed;
    private bool _slowMode;
    private bool _isPlaying;

    private bool[] _carouselTapped;     // tracks which carousel cards have been tapped
    private int    _uniqueCarouselTaps; // count of unique cards tapped
    private bool   _nextUnlocked;

    private float BaseDelay      => _slowMode ? 1.2f : 0.7f;
    private float CardPopDelay   => _slowMode ? 0.5f : 0.3f;
    private float MascotHoldTime => _slowMode ? 1.4f : 0.9f;

    // ═════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════
    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();

        partARoot.SetActive(false);
        partBRoot.SetActive(false);
        if (completionScreen) completionScreen.SetActive(false);

        nextButton.gameObject.SetActive(false);

        replayButton.onClick.AddListener(OnReplay);
        snailButton.onClick.AddListener(OnToggleSlow);
        nextButton.onClick.AddListener(OnNext);
        if (doneButton) doneButton.onClick.AddListener(OnDone);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GAME START
    // ═════════════════════════════════════════════════════════════════════
    void StartGame()
    {
        _slowMode          = false;
        _isPlaying         = false;
        _partARevealed     = 0;
        _uniqueCarouselTaps = 0;
        _nextUnlocked      = false;

        partARoot.SetActive(true);
        partBRoot.SetActive(false);
        if (completionScreen) completionScreen.SetActive(false);
        nextButton.gameObject.SetActive(false);

        HideToast();
        SetMascotIdle();

        BuildPartACards();
        BuildCarousel();

        if (_partASpawned == null)
        {
            Debug.LogError("[AllActionsReviewController] Part A cards failed to build.");
            return;
        }

        StartCoroutine(AutoPlayPartA());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BUILD — PART A CARDS
    // ═════════════════════════════════════════════════════════════════════
    void BuildPartACards()
    {
        if (!ValidatePrefabAndGrid(partACardPrefab, partACardsGrid, "Part A")) return;
        if (partACards == null || partACards.Length == 0)
        {
            Debug.LogError("[AllActionsReviewController] partACards is empty. Fill it in the Inspector.");
            return;
        }

        foreach (Transform child in partACardsGrid) Destroy(child.gameObject);

        int count = partACards.Length;
        _partASpawned = new AllActionsCard_LetsAct_ReadingBB1[count];

        for (int i = 0; i < count; i++)
        {
            ReviewWordEntry entry = partACards[i];
            AllActionsCard_LetsAct_ReadingBB1 card = Instantiate(partACardPrefab, partACardsGrid);

            card.SetWord(entry.word);
            card.SetSprite(entry.cardIllustration);
            card.SetHidden(true);

            Button btn = EnsureButton(card);
            int idx = i;
            btn.onClick.AddListener(() => OnPartACardTapped(idx));

            _partASpawned[i] = card;
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BUILD — CAROUSEL (all 15 cards)
    // ═════════════════════════════════════════════════════════════════════
    void BuildCarousel()
    {
        if (!ValidatePrefabAndGrid(carouselCardPrefab, carouselContent, "Carousel")) return;
        if (allCards == null || allCards.Length == 0)
        {
            Debug.LogError("[AllActionsReviewController] allCards is empty. Fill it in the Inspector.");
            return;
        }

        foreach (Transform child in carouselContent) Destroy(child.gameObject);

        int count = allCards.Length;
        _carouselTapped  = new bool[count];
        _carouselSpawned = new AllActionsCard_LetsAct_ReadingBB1[count];

        for (int i = 0; i < count; i++)
        {
            ReviewWordEntry entry = allCards[i];
            AllActionsCard_LetsAct_ReadingBB1 card = Instantiate(carouselCardPrefab, carouselContent);

            card.SetWord(entry.word);
            card.SetSprite(entry.cardIllustration);
            card.SetHidden(true); // hidden until RevealPartB() pops them in one by one

            Button btn = EnsureButton(card);
            int idx = i;
            btn.onClick.AddListener(() => OnCarouselCardTapped(idx));

            _carouselSpawned[i] = card;
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  AUTO-PLAY PART A
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator AutoPlayPartA()
    {
        _isPlaying = true;

        if (_partASpawned == null || partACards == null)
        {
            Debug.LogError("[AllActionsReviewController] AutoPlayPartA: data missing.");
            _isPlaying = false;
            yield break;
        }

        int count = Mathf.Min(_partASpawned.Length, partACards.Length);

        for (int i = 0; i < count; i++)
        {
            if (_partASpawned[i] == null)
            {
                Debug.LogError($"[AllActionsReviewController] Part A card {i} is null.");
                _isPlaying = false;
                yield break;
            }

            // Pause before each card (after first)
            if (i > 0) yield return new WaitForSeconds(BaseDelay * 0.4f);

            // Pop card in
            _partASpawned[i].SetHidden(false);
            _partASpawned[i].PlayPopAnim();
            PlaySFX(sfx_cardPop);
            yield return new WaitForSeconds(CardPopDelay);

            _partARevealed = i + 1;
            yield return new WaitForSeconds(0.15f);

            // Mascot swap
            SetMascotSprite(partACards[i].mascotSprite);

            // Toast + word audio
            ShowToast(partACards[i].word);
            AudioClip clip = partACards[i].wordAudio;
            if (clip != null)
            {
                _audio.PlayOneShot(clip);
                yield return new WaitForSeconds(clip.length + 0.15f);
            }
            else
            {
                yield return new WaitForSeconds(MascotHoldTime);
            }

            HideToast();
            SetMascotIdle();
            yield return new WaitForSeconds(0.2f);
        }

        // Part A done — celebrate then reveal Part B
        PlaySFX(sfx_allDone);
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(RevealPartB());

        _isPlaying = false;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  REVEAL PART B CAROUSEL
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator RevealPartB()
    {
        partBRoot.SetActive(true);
        PlaySFX(sfx_partBReveal);

        // Animate Part B label scale pop
        if (partBLabel != null)
        {
            partBLabel.transform.localScale = Vector3.zero;
            yield return StartCoroutine(ScalePop(partBLabel.transform, 0.4f));
        }

        yield return new WaitForSeconds(0.3f);

        // Play celebration voice
        if (audio_youKnowAll15 != null)
        {
            _audio.PlayOneShot(audio_youKnowAll15);
            yield return new WaitForSeconds(audio_youKnowAll15.length + 0.2f);
        }

        // Animate carousel cards popping in one by one (fast)
        if (_carouselSpawned != null)
        {
            foreach (var card in _carouselSpawned)
            {
                if (card == null) continue;
                card.SetHidden(false);
                card.PlayPopAnim();
                PlaySFX(sfx_cardPop);
                yield return new WaitForSeconds(0.08f); // fast cascade
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  PART A — CARD TAPPED
    // ═════════════════════════════════════════════════════════════════════
    void OnPartACardTapped(int idx)
    {
        if (_isPlaying) return;
        if (idx >= _partARevealed) return;
        if (_partASpawned == null || idx >= _partASpawned.Length) return;
        if (_partASpawned[idx] == null) return;

        _partASpawned[idx].PlayTapAnim();
        PlaySFX(sfx_tap);
        ShowToast(partACards[idx].word);
        SetMascotSprite(partACards[idx].mascotSprite);

        AudioClip clip = partACards[idx].wordAudio;
        if (clip != null) _audio.PlayOneShot(clip);

        StartCoroutine(ClearTapFeedback());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CAROUSEL — CARD TAPPED
    // ═════════════════════════════════════════════════════════════════════
    void OnCarouselCardTapped(int idx)
    {
        if (_carouselSpawned == null || idx >= _carouselSpawned.Length) return;
        if (_carouselSpawned[idx] == null) return;

        _carouselSpawned[idx].PlayTapAnim();
        PlaySFX(sfx_tap);
        ShowToast(allCards[idx].word);
        SetMascotSprite(allCards[idx].mascotSprite);

        AudioClip clip = allCards[idx].wordAudio;
        if (clip != null) _audio.PlayOneShot(clip);

        // Track unique taps for NEXT unlock
        if (!_carouselTapped[idx])
        {
            _carouselTapped[idx] = true;
            _uniqueCarouselTaps++;

            if (!_nextUnlocked && _uniqueCarouselTaps >= tapCountToUnlockNext)
            {
                _nextUnlocked = true;
                StartCoroutine(UnlockNextButton());
            }
        }

        StartCoroutine(ClearTapFeedback());
    }

    IEnumerator ClearTapFeedback()
    {
        yield return new WaitForSeconds(1.4f);
        HideToast();
        SetMascotIdle();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  UNLOCK NEXT BUTTON
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator UnlockNextButton()
    {
        PlaySFX(sfx_carouselUnlock);
        nextButton.gameObject.SetActive(true);
        nextButton.transform.localScale = Vector3.zero;
        yield return StartCoroutine(ScalePop(nextButton.transform, 0.45f));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BUTTON HANDLERS
    // ═════════════════════════════════════════════════════════════════════
    void OnNext()
    {
        PlaySFX(sfx_nextScreen);
        ShowCompletion();
    }

    void OnReplay()
    {
        if (_isPlaying) return;
        StopAllCoroutines();
        _isPlaying = false;
        _nextUnlocked = false;
        _uniqueCarouselTaps = 0;
        nextButton.gameObject.SetActive(false);
        partBRoot.SetActive(false);
        HideToast();
        SetMascotIdle();
        BuildPartACards();
        BuildCarousel();
        if (_partASpawned != null) StartCoroutine(AutoPlayPartA());
    }

    void OnToggleSlow()
    {
        _slowMode = !_slowMode;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  COMPLETION
    // ═════════════════════════════════════════════════════════════════════
    void ShowCompletion()
    {
        partARoot.SetActive(false);
        partBRoot.SetActive(false);
        if (completionScreen) completionScreen.SetActive(true);
        PlaySFX(sfx_completion);
    }

    public void OnDone()
    {
        if (completionScreen) completionScreen.SetActive(false);
        _panel?.UnitFinished(_unitButton);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MASCOT HELPERS
    // ═════════════════════════════════════════════════════════════════════
    void SetMascotSprite(Sprite sprite)
    {
        if (mascotImage == null || sprite == null) return;
        mascotImage.sprite = sprite;
    }

    void SetMascotIdle()
    {
        if (mascotImage == null || mascotIdleSprite == null) return;
        mascotImage.sprite = mascotIdleSprite;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TOAST HELPERS
    // ═════════════════════════════════════════════════════════════════════
    void ShowToast(string word)
    {
        if (wordToastText == null) return;
        wordToastText.text = word + "!";
        wordToastText.gameObject.SetActive(true);
    }

    void HideToast()
    {
        if (wordToastText != null) wordToastText.gameObject.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  SCALE POP ANIMATION (no Animator — pure coroutine)
    //  0 → 1.15 → 1.0
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator ScalePop(Transform t, float duration)
    {
        float half = duration * 0.7f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(0f, 1.15f, elapsed / half);
            t.localScale = Vector3.one * s;
            yield return null;
        }

        elapsed = 0f;
        float second = duration * 0.3f;
        while (elapsed < second)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(1.15f, 1f, elapsed / second);
            t.localScale = Vector3.one * s;
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  AUDIO
    // ═════════════════════════════════════════════════════════════════════
    void PlaySFX(AudioClip clip)
    {
        if (clip != null && _audio != null)
            _audio.PlayOneShot(clip);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Gets or adds a Button on the card root — safe even if Awake hasn't run.</summary>
    Button EnsureButton(AllActionsCard_LetsAct_ReadingBB1 card)
    {
        Button btn = card.button;
        if (btn == null)
        {
            btn = card.GetComponent<Button>();
            if (btn == null) btn = card.gameObject.AddComponent<Button>();
            card.button = btn;
        }
        return btn;
    }

    bool ValidatePrefabAndGrid(AllActionsCard_LetsAct_ReadingBB1 prefab, Transform grid, string label)
    {
        if (prefab == null)
        {
            Debug.LogError($"[AllActionsReviewController] {label} card prefab is not assigned in Inspector.");
            return false;
        }
        if (grid == null)
        {
            Debug.LogError($"[AllActionsReviewController] {label} grid/content transform is not assigned in Inspector.");
            return false;
        }
        return true;
    }
}