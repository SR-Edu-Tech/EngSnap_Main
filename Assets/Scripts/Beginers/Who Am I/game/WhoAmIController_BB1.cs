using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// One clue for a riddle round. First clue shows automatically;
/// extra clues are revealed one at a time via the Hear More button.
/// </summary>
[System.Serializable]
public class WhoAmIClue
{
    [TextArea] public string text;
    public AudioClip audio;
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// One riddle round — clues, answer index into the shared items array,
/// and which 3 items appear as answer options (by index).
/// </summary>
[System.Serializable]
public class WhoAmIRound
{
    [Tooltip("Clues in order. First shown automatically; rest revealed by Hear More.")]
    public WhoAmIClue[] clues;

    [Tooltip("Index into whoAmIItems that is the correct answer.")]
    public int correctItemIndex;

    [Tooltip("Indices into whoAmIItems for the 3 answer cards. Order is randomised at runtime.")]
    public int[] optionItemIndices = new int[3];
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// One item — used as answer cards (Screen 1) and drag cards (Screen 2).
/// </summary>
[System.Serializable]
public class WhoAmIItem
{
    public string label;
    public Sprite illustration;
    public AudioClip audio;
}

// ─────────────────────────────────────────────────────────────────────────────
/// <summary>
/// WhoAmIController_BB1
///
/// SCREEN 1 — Riddle Quiz (4 rounds)
///   • Clue text displayed; student taps one of 3 image cards at the bottom.
///   • "Hear More" reveals the next clue one at a time.
///   • Correct tap → green flash + mascot happy + next round.
///   • Wrong tap   → red shake + mascot sad + try again (card stays enabled).
///   • After all 4 rounds correct → Next button → Screen 2.
///
/// SCREEN 2 — Like / Don't Like Drag (8 cards)
///   • A card falls from top one at a time.
///   • Student drags it to LEFT (Like) or RIGHT (Don't Like) zone.
///   • No wrong answer — both zones are valid.
///   • Mascot sprite swaps on drop; audio feedback plays.
///   • After 8 cards → Summary screen → Next → unit panel.
///
/// INSPECTOR SETUP:
///   whoAmIItems  : 4 items (Mom, Tree, Car, Giraffe) — shared by both screens.
///   rounds       : 4 rounds, each referencing item indices + clues.
///   likeItems    : 8 items for Screen 2 (4 colours + 4 fruits).
///
/// PREFABS:
///   answerCardPrefab  : Image (illustration) + Button. No text label needed.
///   dragCardPrefab    : Image (illustration) + TMP_Text label + CanvasGroup.
///                       Must have DragCard_WhoAmI component on root.
///
/// HIERARCHY (suggested):
///   WhoAmIRoot  ← this script + AudioSource(x2)
///   ├── Screen1Root
///   │   ├── CluePanel
///   │   │   ├── ClueText          ← clueText
///   │   │   └── HearMoreButton    ← hearMoreButton
///   │   ├── AnswerRow             ← answerRow  (HorizontalLayoutGroup)
///   │   ├── RoundLabel            ← roundLabel ("Round 1 of 4")
///   │   ├── MascotImage           ← mascotImage1
///   │   ├── FeedbackText          ← feedbackText1
///   │   └── NextButton1           ← nextButton1  (hidden until all rounds done)
///   └── Screen2Root
///       ├── LikeZone              ← likeZone    (left, green)
///       ├── DislikeZone           ← dislikeZone (right, red)
///       ├── CardSpawnPoint        ← cardSpawnPoint (top centre)
///       ├── RoundLabel2           ← roundLabel2 ("Round 1 of 8")
///       ├── MascotImage2          ← mascotImage2
///       ├── FeedbackText2         ← feedbackText2
///       └── SummaryPanel          ← summaryPanel
///           ├── SummaryText       ← summaryText
///           └── NextButton2       ← nextButton2
/// </summary>
public class WhoAmIController_BB1 : MonoBehaviour, IUnitCompletable
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
    //  INSPECTOR — SHARED DATA
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Shared Item Library ─────────────────────────")]
    [Tooltip("Items used as answer cards (Screen 1) and drag cards (Screen 2). Index matches round references.")]
    public WhoAmIItem[] whoAmIItems;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — SCREEN 1
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Screen 1 — Riddle Quiz ──────────────────────")]
    public GameObject screen1Root;
    public WhoAmIRound[]  rounds;

    public TMP_Text   clueText;
    public Button     hearMoreButton;
    public Transform  answerRow;              // parent for 3 answer card buttons
    public TMP_Text   roundLabel;
    public Image      mascotImage1;
    public Sprite     mascotIdle1;
    public Sprite     mascotHappy1;
    public Sprite     mascotSad1;
    public TMP_Text   feedbackText1;
    public Button     nextButton1;

    [Tooltip("Prefab: root has Button + Image (illustration). No text needed.")]
    public GameObject answerCardPrefab;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — SCREEN 2
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Screen 2 — Like / Don't Like ────────────────")]
    public GameObject screen2Root;

    [Tooltip("8 items for Screen 2 (4 colour cards + 4 fruit cards), in order.")]
    public WhoAmIItem[] likeItems;

    public RectTransform likeZone;
    public RectTransform dislikeZone;
    public Transform     cardSpawnPoint;
    public TMP_Text      roundLabel2;
    public Image         mascotImage2;
    public Sprite        mascotIdle2;
    public Sprite        mascotThumbsUp;
    public Sprite        mascotThumbsDown;
    public TMP_Text      feedbackText2;

    public GameObject summaryPanel;
    public TMP_Text   summaryText;
    public Button     nextButton2;

    [Tooltip("Prefab: root has CanvasGroup + Image + TMP_Text + DragCard_WhoAmI component.")]
    public GameObject dragCardPrefab;

    // ═════════════════════════════════════════════════════════════════════
    //  INSPECTOR — AUDIO
    // ═════════════════════════════════════════════════════════════════════
    [Header("── Audio ────────────────────────────────────────")]
    public AudioSource voiceAudio;
    public AudioSource sfxAudio;
    public AudioClip   sfx_correct;
    public AudioClip   sfx_wrong;
    public AudioClip   sfx_cardDrop;
    public AudioClip   sfx_complete;
    public AudioClip   audio_youLikeIt;      // "You like it! Great!"
    public AudioClip   audio_youDontLike;    // "You don't like [item]! That is okay!"
    public AudioClip   summaryAudio;

    // ═════════════════════════════════════════════════════════════════════
    //  RUNTIME — SCREEN 1
    // ═════════════════════════════════════════════════════════════════════
    private int  _roundIndex;
    private int  _clueIndex;
    private bool _waitingForAnswer;
    private List<GameObject> _answerCards = new List<GameObject>();

    // ═════════════════════════════════════════════════════════════════════
    //  RUNTIME — SCREEN 2
    // ═════════════════════════════════════════════════════════════════════
    private int          _likeCardIndex;
    private List<string> _likedItems    = new List<string>();
    private List<string> _dislikedItems = new List<string>();
    private bool         _dragBusy;
    private GameObject   _activeCard;

    // ═════════════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════
    void Awake()
    {
        if (voiceAudio == null) voiceAudio = gameObject.AddComponent<AudioSource>();
        if (sfxAudio   == null) sfxAudio   = gameObject.AddComponent<AudioSource>();

        if (hearMoreButton) hearMoreButton.onClick.AddListener(OnHearMore);
        if (nextButton1)    nextButton1.onClick.AddListener(OnNextToScreen2);
        if (nextButton2)    nextButton2.onClick.AddListener(OnDone);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  START
    // ═════════════════════════════════════════════════════════════════════
    void StartGame()
    {
        screen1Root.SetActive(true);
        screen2Root.SetActive(false);
        if (summaryPanel) summaryPanel.SetActive(false);
        if (nextButton1)  nextButton1.gameObject.SetActive(false);

        _roundIndex    = 0;
        _likeCardIndex = 0;
        _likedItems.Clear();
        _dislikedItems.Clear();

        SetMascot(mascotImage1, mascotIdle1);
        ShowFeedback(feedbackText1, "");
        LoadRound(_roundIndex);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  SCREEN 1 — ROUNDS
    // ═════════════════════════════════════════════════════════════════════
    void LoadRound(int idx)
    {
        _waitingForAnswer = false;
        _clueIndex        = 0;

        WhoAmIRound round = rounds[idx];

        // Round label
        if (roundLabel) roundLabel.text = $"Round {idx + 1} of {rounds.Length}";

        // First clue
        ShowClue(round, 0);

        // Hear More button — only show if there are extra clues
        if (hearMoreButton)
            hearMoreButton.gameObject.SetActive(round.clues.Length > 1);

        // Mascot idle
        SetMascot(mascotImage1, mascotIdle1);
        ShowFeedback(feedbackText1, "");

        // Build answer cards with randomised positions
        BuildAnswerCards(round);

        _waitingForAnswer = true;
    }

    void ShowClue(WhoAmIRound round, int clueIdx)
    {
        if (clueText) clueText.text = round.clues[clueIdx].text;
        AudioClip clip = round.clues[clueIdx].audio;
        if (clip) voiceAudio.PlayOneShot(clip);
    }

    void OnHearMore()
    {
        WhoAmIRound round = rounds[_roundIndex];
        int next = _clueIndex + 1;
        if (next >= round.clues.Length) return;

        _clueIndex = next;
        ShowClue(round, _clueIndex);

        // Hide button when last clue reached
        if (_clueIndex >= round.clues.Length - 1)
            hearMoreButton.gameObject.SetActive(false);
    }

    void BuildAnswerCards(WhoAmIRound round)
    {
        // Destroy old cards
        foreach (var c in _answerCards)
            if (c) Destroy(c);
        _answerCards.Clear();

        // Shuffle option order
        int[] order = (int[])round.optionItemIndices.Clone();
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j   = Random.Range(0, i + 1);
            int tmp = order[i]; order[i] = order[j]; order[j] = tmp;
        }

        foreach (int itemIdx in order)
        {
            WhoAmIItem item = whoAmIItems[itemIdx];
            GameObject go   = Instantiate(answerCardPrefab, answerRow);

            // Assign illustration
            Image img = go.GetComponentInChildren<Image>();
            if (img) img.sprite = item.illustration;

            // Wire button
            Button btn      = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();

            bool isCorrect = (itemIdx == round.correctItemIndex);
            btn.onClick.AddListener(() => OnAnswerTapped(isCorrect, item));

            _answerCards.Add(go);
        }
    }

    void OnAnswerTapped(bool correct, WhoAmIItem item)
    {
        if (!_waitingForAnswer) return;
        _waitingForAnswer = false;

        if (correct)
        {
            sfxAudio.PlayOneShot(sfx_correct);
            if (item.audio) voiceAudio.PlayOneShot(item.audio);
            SetMascot(mascotImage1, mascotHappy1);
            ShowFeedback(feedbackText1, $"{item.label}! Correct!");
            StartCoroutine(AdvanceRound());
        }
        else
        {
            sfxAudio.PlayOneShot(sfx_wrong);
            SetMascot(mascotImage1, mascotSad1);
            ShowFeedback(feedbackText1, "Try again!");
            StartCoroutine(ResetAfterWrong());
        }
    }

    IEnumerator ResetAfterWrong()
    {
        yield return new WaitForSeconds(1.2f);
        SetMascot(mascotImage1, mascotIdle1);
        ShowFeedback(feedbackText1, "");
        _waitingForAnswer = true;
    }

    IEnumerator AdvanceRound()
    {
        yield return new WaitForSeconds(1.4f);

        _roundIndex++;
        if (_roundIndex < rounds.Length)
        {
            SetMascot(mascotImage1, mascotIdle1);
            ShowFeedback(feedbackText1, "");
            LoadRound(_roundIndex);
        }
        else
        {
            // All rounds done
            SetMascot(mascotImage1, mascotHappy1);
            ShowFeedback(feedbackText1, "Amazing! All done!");
            sfxAudio.PlayOneShot(sfx_complete);
            yield return new WaitForSeconds(0.8f);
            if (nextButton1) nextButton1.gameObject.SetActive(true);
        }
    }

    // ═════════════════════════════════════════════════════════════════════
    //  TRANSITION TO SCREEN 2
    // ═════════════════════════════════════════════════════════════════════
    void OnNextToScreen2()
    {
        screen1Root.SetActive(false);
        screen2Root.SetActive(true);
        if (summaryPanel) summaryPanel.SetActive(false);
        _likeCardIndex = 0;
        _likedItems.Clear();
        _dislikedItems.Clear();
        SetMascot(mascotImage2, mascotIdle2);
        ShowFeedback(feedbackText2, "");
        StartCoroutine(SpawnNextDragCard());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  SCREEN 2 — DRAG CARDS
    // ═════════════════════════════════════════════════════════════════════
    IEnumerator SpawnNextDragCard()
    {
        if (_likeCardIndex >= likeItems.Length)
        {
            ShowSummary();
            yield break;
        }

        _dragBusy = true;

        WhoAmIItem item = likeItems[_likeCardIndex];

        if (roundLabel2)
            roundLabel2.text = $"Round {_likeCardIndex + 1} of {likeItems.Length}";

        // Instantiate drag card
        GameObject go = Instantiate(dragCardPrefab, cardSpawnPoint);
        go.transform.position = cardSpawnPoint.position;
        _activeCard = go;

        // Set illustration + label
        Image img = go.GetComponentInChildren<Image>();
        if (img) img.sprite = item.illustration;
        TMP_Text lbl = go.GetComponentInChildren<TMP_Text>();
        if (lbl) lbl.text = item.label;

        // Play item audio
        if (item.audio) voiceAudio.PlayOneShot(item.audio);

        // Wire drag handler
        DragCard_WhoAmI drag = go.GetComponent<DragCard_WhoAmI>();
        if (drag == null) drag = go.AddComponent<DragCard_WhoAmI>();
        drag.Init(likeZone, dislikeZone, OnCardDropped);

        _dragBusy = false;
        yield return null;
    }

    void OnCardDropped(GameObject card, bool liked)
    {
        if (_dragBusy) return;
        _dragBusy = true;

        WhoAmIItem item = likeItems[_likeCardIndex];
        sfxAudio.PlayOneShot(sfx_cardDrop);

        if (liked)
        {
            _likedItems.Add(item.label);
            SetMascot(mascotImage2, mascotThumbsUp);
            ShowFeedback(feedbackText2, $"You like {item.label}! Great!");
            voiceAudio.PlayOneShot(audio_youLikeIt);
        }
        else
        {
            _dislikedItems.Add(item.label);
            SetMascot(mascotImage2, mascotThumbsDown);
            ShowFeedback(feedbackText2, $"You don't like {item.label}! That is okay!");
            voiceAudio.PlayOneShot(audio_youDontLike);
        }

        _likeCardIndex++;
        StartCoroutine(AdvanceDragCard());
    }

    IEnumerator AdvanceDragCard()
    {
        // Wait until the like/dislike voice line finishes before moving on
        yield return new WaitWhile(() => voiceAudio.isPlaying);
        // Small gap after voice ends before next card appears
        yield return new WaitForSeconds(0.4f);

        SetMascot(mascotImage2, mascotIdle2);
        ShowFeedback(feedbackText2, "");
        // Card destroys itself at end of its own FadeOut — no Destroy needed here
        _activeCard = null;
        _dragBusy   = false;
        StartCoroutine(SpawnNextDragCard());
    }

    // ═════════════════════════════════════════════════════════════════════
    //  SUMMARY
    // ═════════════════════════════════════════════════════════════════════
    void ShowSummary()
    {
        if (summaryPanel) summaryPanel.SetActive(true);
        sfxAudio.PlayOneShot(sfx_complete);
        if (summaryAudio) voiceAudio.PlayOneShot(summaryAudio);

        string liked    = _likedItems.Count    > 0 ? string.Join(", ", _likedItems)    : "nothing";
        string disliked = _dislikedItems.Count > 0 ? string.Join(", ", _dislikedItems) : "nothing";

        if (summaryText)
            summaryText.text = $"You like: {liked}.\nYou don't like: {disliked}.";
    }

    // ═════════════════════════════════════════════════════════════════════
    //  COMPLETION
    // ═════════════════════════════════════════════════════════════════════
    public void OnDone()
    {
        if (summaryPanel) summaryPanel.SetActive(false);
        screen2Root.SetActive(false);
        _panel?.UnitFinished(_unitButton);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═════════════════════════════════════════════════════════════════════
    void SetMascot(Image img, Sprite sprite)
    {
        if (img && sprite) img.sprite = sprite;
    }

    void ShowFeedback(TMP_Text t, string msg)
    {
        if (t == null) return;
        t.text = msg;
        t.gameObject.SetActive(!string.IsNullOrEmpty(msg));
    }
}