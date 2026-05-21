using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// One entry per vocabulary card — assign everything here in the Inspector.
/// Word label, illustration sprite, mascot sprite, and audio clip are all
/// grouped together so there is zero chance of an index mismatch.
/// </summary>
[System.Serializable]
public class ActionWordEntry
{
    [Tooltip("Word shown on the card banner, e.g. 'Read'")]
    public string word;

    [Tooltip("Illustration image shown on the top half of the card")]
    public Sprite cardIllustration;

    [Tooltip("Mascot sprite to swap to when this word plays or is tapped")]
    public Sprite mascotSprite;

    [Tooltip("Audio clip that plays when this card is shown or tapped")]
    public AudioClip wordAudio;
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// ActionWordsController — controls the Action Words vocabulary unit.
/// Implements IUnitCompletable so SharedUnitPanelController can open/close it.
///
/// INSPECTOR SETUP:
///
///  Screen 1 / Screen 2 sections:
///    - Drag the root GameObjects, grid transforms, mascot Image, toast TMP_Text,
///      and the 3 buttons (Next / Replay / Snail).
///    - mascotIdleSprite1/2 → default sprite shown when no word is active.
///    - screen1Cards / screen2Cards → 6 entries each.
///      Each entry has: word · cardIllustration · mascotSprite · wordAudio
///      Everything for one card lives in one row — no cross-array mismatch.
///
///  Completion:
///    - completionScreen → shown after Screen 2 finishes.
///    - doneButton → calls UnitFinished, returns to unit panel.
///
///  Card Prefab:
///    - cardPrefab → prefab with ActionWordCard_LetsAct_ListeningBB1 on root.
///
///  SFX:
///    - sfx_cardPop / sfx_tap / sfx_allDone / sfx_nextScreen / sfx_completion
/// </summary>
public class ActionWordsController_LetsAct_ListeningBB1 : MonoBehaviour, IUnitCompletable
{
    // ── IUnitCompletable ──────────────────────────────────────────────────
    private SharedUnitPanelController _panel;
    private SharedUnitButton          _button;

    public void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button)
    {
        _panel  = panel;
        _button = button;
        StartGame();
    }

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — SCREEN 1
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Screen 1 ──────────────────────────────────────")]
    public GameObject screen1Root;
    public Transform  cardsGrid1;
    public Image      mascotImage1;
    public Sprite     mascotIdleSprite1;
    public TMP_Text   wordToastText1;
    public Button     nextButton1;
    public Button     replayButton1;
    public Button     snailButton1;

    [Space]
    [Tooltip("6 card entries for Screen 1. word + cardIllustration + mascotSprite + wordAudio per card.")]
    public ActionWordEntry[] screen1Cards = new ActionWordEntry[6];

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — SCREEN 2
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Screen 2 ──────────────────────────────────────")]
    public GameObject screen2Root;
    public Transform  cardsGrid2;
    public Image      mascotImage2;
    public Sprite     mascotIdleSprite2;
    public TMP_Text   wordToastText2;
    public Button     nextButton2;
    public Button     replayButton2;
    public Button     snailButton2;

    [Space]
    [Tooltip("6 card entries for Screen 2. word + cardIllustration + mascotSprite + wordAudio per card.")]
    public ActionWordEntry[] screen2Cards = new ActionWordEntry[6];

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — COMPLETION
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Completion ────────────────────────────────────")]
    public GameObject completionScreen;
    public Button     doneButton;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — CARD PREFAB
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Card Prefab ──────────────────────────────────")]
    [Tooltip("Prefab with ActionWordCard_LetsAct_ListeningBB1 on root GameObject")]
    public ActionWordCard_LetsAct_ListeningBB1 cardPrefab;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — SFX
    // ═════════════════════════════════════════════════════════════════════
    [Header("── SFX ──────────────────────────────────────────")]
    public AudioClip sfx_cardPop;
    public AudioClip sfx_tap;
    public AudioClip sfx_allDone;
    public AudioClip sfx_nextScreen;
    public AudioClip sfx_completion;

    // ═════════════════════════════════════════════════════════════════════
    //  RUNTIME STATE
    // ═════════════════════════════════════════════════════════════════════
    private AudioSource _audio;

    private ActionWordCard_LetsAct_ListeningBB1[] _spawnedCards1;
    private ActionWordCard_LetsAct_ListeningBB1[] _spawnedCards2;

    private int  _revealedCount1;
    private int  _revealedCount2;
    private bool _slowMode;
    private bool _isPlaying;

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

        screen1Root.SetActive(false);
        screen2Root.SetActive(false);
        if (completionScreen) completionScreen.SetActive(false);

        nextButton1.onClick.AddListener(OnNextScreen1);
        replayButton1.onClick.AddListener(OnReplayScreen1);
        snailButton1.onClick.AddListener(OnToggleSlow);

        nextButton2.onClick.AddListener(OnNextScreen2);
        replayButton2.onClick.AddListener(OnReplayScreen2);
        snailButton2.onClick.AddListener(OnToggleSlow);

        if (doneButton) doneButton.onClick.AddListener(OnDone);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  GAME START
    // ═════════════════════════════════════════════════════════════════════
    void StartGame()
    {
        _slowMode  = false;
        _isPlaying = false;

        screen1Root.SetActive(true);
        screen2Root.SetActive(false);
        if (completionScreen) completionScreen.SetActive(false);

        nextButton1.gameObject.SetActive(false);
        nextButton2.gameObject.SetActive(false);

        HideToast(1);
        HideToast(2);
        SetMascotIdle(1);
        SetMascotIdle(2);

        BuildCards(1);
        BuildCards(2);

        if (_spawnedCards1 == null || _spawnedCards2 == null)
        {
            Debug.LogError("[ActionWordsController] Cards failed to build. Check all Inspector fields are assigned.");
            return;
        }

        StartCoroutine(AutoPlayScreen(1));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BUILD CARDS
    //  Each card gets its own data from its ActionWordEntry — word and
    //  illustration come from the same row so they can never mismatch.
    // ═════════════════════════════════════════════════════════════════════
    void BuildCards(int screen)
    {
        Transform         grid    = screen == 1 ? cardsGrid1   : cardsGrid2;
        ActionWordEntry[] entries = screen == 1 ? screen1Cards : screen2Cards;

        if (cardPrefab == null)
        {
            Debug.LogError("[ActionWordsController] cardPrefab is not assigned in Inspector.");
            return;
        }
        if (grid == null)
        {
            Debug.LogError($"[ActionWordsController] cardsGrid{screen} is not assigned in Inspector.");
            return;
        }
        if (entries == null || entries.Length == 0)
        {
            Debug.LogError($"[ActionWordsController] screen{screen}Cards array is empty. Fill it in the Inspector.");
            return;
        }

        // Clear previously spawned cards
        foreach (Transform child in grid)
            Destroy(child.gameObject);

        int count = entries.Length;
        var arr   = new ActionWordCard_LetsAct_ListeningBB1[count];

        for (int i = 0; i < count; i++)
        {
            ActionWordEntry entry = entries[i];
            ActionWordCard_LetsAct_ListeningBB1 card = Instantiate(cardPrefab, grid);

            // ── Apply this entry's data directly to its own card ──────────
            // word and illustration come from the same Inspector row,
            // so they are guaranteed to match.
            card.SetWord(entry.word);
            card.SetSprite(entry.cardIllustration);
            card.SetHidden(true);

            // ── Wire button ───────────────────────────────────────────────
            // Awake may not have run if the parent GO is inactive (Screen2
            // is off at startup), so we get/add Button here directly.
            Button btn = card.button;
            if (btn == null)
            {
                btn = card.GetComponent<Button>();
                if (btn == null) btn = card.gameObject.AddComponent<Button>();
                card.button = btn;
            }

            int capturedIdx = i;
            int capturedScr = screen;
            btn.onClick.AddListener(() => OnCardTapped(capturedIdx, capturedScr));

            arr[i] = card;
        }

        if (screen == 1) { _spawnedCards1 = arr; _revealedCount1 = 0; }
        else             { _spawnedCards2 = arr; _revealedCount2 = 0; }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  AUTO-PLAY COROUTINE
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator AutoPlayScreen(int screen)
    {
        _isPlaying = true;

        ActionWordCard_LetsAct_ListeningBB1[] cards   = screen == 1 ? _spawnedCards1 : _spawnedCards2;
        ActionWordEntry[]                     entries = screen == 1 ? screen1Cards    : screen2Cards;

        if (cards == null || entries == null)
        {
            Debug.LogError("[ActionWordsController] AutoPlayScreen: data is null.");
            _isPlaying = false;
            yield break;
        }

        int count = Mathf.Min(cards.Length, entries.Length);

        for (int i = 0; i < count; i++)
        {
            if (cards[i] == null)
            {
                Debug.LogError($"[ActionWordsController] Card {i} is null.");
                _isPlaying = false;
                yield break;
            }

            // Brief pause between pairs (every 2 cards)
            if (i > 0 && i % 2 == 0)
                yield return new WaitForSeconds(BaseDelay * 0.5f);

            // Reveal card with pop animation
            cards[i].SetHidden(false);
            cards[i].PlayPopAnim();
            PlaySFX(sfx_cardPop);
            yield return new WaitForSeconds(CardPopDelay);

            if (screen == 1) _revealedCount1 = i + 1;
            else             _revealedCount2 = i + 1;

            yield return new WaitForSeconds(0.15f);

            // Swap mascot to this card's sprite
            SetMascotSprite(screen, entries[i].mascotSprite);

            // Show word toast
            ShowToast(screen, entries[i].word);

            // Play word audio
            AudioClip clip = entries[i].wordAudio;
            if (clip != null)
            {
                _audio.PlayOneShot(clip);
                yield return new WaitForSeconds(clip.length + 0.15f);
            }
            else
            {
                yield return new WaitForSeconds(MascotHoldTime);
            }

            HideToast(screen);
            SetMascotIdle(screen);
            yield return new WaitForSeconds(0.2f);
        }

        PlaySFX(sfx_allDone);

        if (screen == 1) nextButton1.gameObject.SetActive(true);
        else             nextButton2.gameObject.SetActive(true);

        _isPlaying = false;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  CARD TAPPED
    // ═════════════════════════════════════════════════════════════════════
    void OnCardTapped(int idx, int screen)
    {
        if (_isPlaying) return;

        int revealed = screen == 1 ? _revealedCount1 : _revealedCount2;
        if (idx >= revealed) return;

        ActionWordCard_LetsAct_ListeningBB1[] cards   = screen == 1 ? _spawnedCards1 : _spawnedCards2;
        ActionWordEntry[]                     entries = screen == 1 ? screen1Cards    : screen2Cards;

        if (cards == null || idx >= cards.Length || cards[idx] == null) return;

        cards[idx].PlayTapAnim();
        PlaySFX(sfx_tap);
        ShowToast(screen, entries[idx].word);
        SetMascotSprite(screen, entries[idx].mascotSprite);

        AudioClip clip = entries[idx].wordAudio;
        if (clip != null) _audio.PlayOneShot(clip);

        StartCoroutine(ClearTapAfterDelay(screen));
    }

    IEnumerator ClearTapAfterDelay(int screen)
    {
        yield return new WaitForSeconds(1.4f);
        HideToast(screen);
        SetMascotIdle(screen);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  MASCOT HELPERS
    // ═════════════════════════════════════════════════════════════════════
    void SetMascotSprite(int screen, Sprite sprite)
    {
        Image img = screen == 1 ? mascotImage1 : mascotImage2;
        if (img == null || sprite == null) return;
        img.sprite = sprite;
    }

    void SetMascotIdle(int screen)
    {
        Image  img    = screen == 1 ? mascotImage1      : mascotImage2;
        Sprite idle   = screen == 1 ? mascotIdleSprite1 : mascotIdleSprite2;
        if (img == null || idle == null) return;
        img.sprite = idle;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TOAST HELPERS
    // ═════════════════════════════════════════════════════════════════════
    void ShowToast(int screen, string word)
    {
        TMP_Text t = screen == 1 ? wordToastText1 : wordToastText2;
        if (t == null) return;
        t.text = word + "!";
        t.gameObject.SetActive(true);
    }

    void HideToast(int screen)
    {
        TMP_Text t = screen == 1 ? wordToastText1 : wordToastText2;
        if (t != null) t.gameObject.SetActive(false);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  BUTTON HANDLERS
    // ═════════════════════════════════════════════════════════════════════
    void OnNextScreen1()
    {
        if (_isPlaying) return;
        PlaySFX(sfx_nextScreen);
        screen1Root.SetActive(false);
        screen2Root.SetActive(true);

        if (_spawnedCards2 == null) BuildCards(2);
        if (_spawnedCards2 == null)
        {
            Debug.LogError("[ActionWordsController] Screen 2 cards failed to build.");
            return;
        }

        StartCoroutine(AutoPlayScreen(2));
    }

    void OnNextScreen2()
    {
        if (_isPlaying) return;
        ShowCompletion();
    }

    void OnReplayScreen1()
    {
        if (_isPlaying) return;
        StopAllCoroutines();
        _isPlaying = false;
        nextButton1.gameObject.SetActive(false);
        HideToast(1);
        SetMascotIdle(1);
        BuildCards(1);
        if (_spawnedCards1 != null) StartCoroutine(AutoPlayScreen(1));
    }

    void OnReplayScreen2()
    {
        if (_isPlaying) return;
        StopAllCoroutines();
        _isPlaying = false;
        nextButton2.gameObject.SetActive(false);
        HideToast(2);
        SetMascotIdle(2);
        BuildCards(2);
        if (_spawnedCards2 != null) StartCoroutine(AutoPlayScreen(2));
    }

void OnToggleSlow()
{
    _slowMode = !_slowMode;

    if (_audio != null)
    {
        _audio.pitch = _slowMode ? 0.75f : 1f;
    }

    Color c = _slowMode ? Color.green : Color.white;

    if (snailButton1 != null)
        snailButton1.image.color = c;

    if (snailButton2 != null)
        snailButton2.image.color = c;

    Debug.Log("Slow Mode: " + (_slowMode ? "ON" : "OFF"));
}
    // ═════════════════════════════════════════════════════════════════════
    //  COMPLETION
    // ═════════════════════════════════════════════════════════════════════
    void ShowCompletion()
    {
        screen2Root.SetActive(false);
        if (completionScreen) completionScreen.SetActive(true);
        PlaySFX(sfx_completion);
    }

    public void OnDone()
    {
        if (completionScreen) completionScreen.SetActive(false);
        _panel?.UnitFinished(_button);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  AUDIO
    // ═════════════════════════════════════════════════════════════════════
    void PlaySFX(AudioClip clip)
    {
        if (clip != null && _audio != null)
            _audio.PlayOneShot(clip);
    }
}