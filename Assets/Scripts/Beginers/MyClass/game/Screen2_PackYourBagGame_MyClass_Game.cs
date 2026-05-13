using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SCREEN 2 — PACK YOUR BAG DRAG GAME
///
/// ── SCENE HIERARCHY ──────────────────────────────────────────────
///  Canvas  (Screen Space – Overlay, 1080×1920)
///   ├── InstructionLabel          TextMeshProUGUI
///   ├── ItemCardArea              RectTransform + HorizontalLayoutGroup
///   │      (cards are spawned here at runtime)
///   ├── BagImage                  Image  — the school bag art
///   │    └── BagDropZone          Image (alpha 0, RaycastTarget OFF)
///   │                             ← this is the INVISIBLE drop target
///   ├── CelebrationPanel          GameObject (hidden at start)
///   │    └── CelebrationText      TextMeshProUGUI
///   ├── DoneButton                Button (shown only at end of Round 3)
///   └── two AudioSource GameObjects — sfxSource, voiceSource
///
/// ── DRAGGABLE ITEM PREFAB ────────────────────────────────────────
///  DraggableItemPrefab
///   ├── Image  (your item art — Preserve Aspect ON)
///   ├── CanvasGroup
///   ├── DraggableItem_MyClass_Game
///   └── NameLabel  (TextMeshProUGUI, optional)
///
/// ── ROUND DATA ───────────────────────────────────────────────────
///  Fill round1Items and round2Items in the Inspector.
///  Each ItemData needs: name, sprite, isSchoolItem flag, voiceName clip.
///  Round 3 is generated automatically from whatever was packed in R1+R2.
/// </summary>
public class Screen2_PackYourBagGame_MyClass_Game : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────

    [Header("── PANEL NAVIGATION ──")]
    [Tooltip("Assign the GameFlowManager_MyClass_Game in the scene")]
    public GameFlowManager_MyClass_Game flowManager;

    [Header("── UI REFERENCES ──")]
    public TextMeshProUGUI instructionLabel;
    public RectTransform   itemCardArea;       // HorizontalLayoutGroup parent
    public RectTransform   bagDropZone;        // invisible RectTransform over bag opening
    public Image           bagImage;           // the visible bag sprite
    public Sprite          bagEmptySprite;
    public Sprite          bagFullSprite;

    [Header("── DRAGGABLE ITEM PREFAB ──")]
    [Tooltip("Prefab with DraggableItem_MyClass_Game + Image + CanvasGroup")]
    public GameObject draggableItemPrefab;

    [Header("── CELEBRATION ──")]
    public GameObject      celebrationPanel;
    public TextMeshProUGUI celebrationText;
    public GameObject      doneButton;         // shown at end of Round 3

    [Header("── AUDIO ──")]
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Space]
    public AudioClip voice_PackYourBag;
    public AudioClip voice_GreatChoice;
    public AudioClip voice_WrongItem;
    public AudioClip voice_WhatIsInYourBag;

    [Space]
    public AudioClip sfx_Pop;
    public AudioClip sfx_ItemSlide;
    public AudioClip sfx_Celebration;
    public AudioClip sfx_WrongBounce;
    public AudioClip sfx_ItemReveal;

    // ─────────────────────────────────────────────────────────────
    //  ITEM DATA  (fill in Inspector)
    // ─────────────────────────────────────────────────────────────

    [System.Serializable]
    public struct ItemData
    {
        public string   name;
        public Sprite   sprite;
        public bool     isSchoolItem;   // true = correct, false = decoy
        public AudioClip voiceName;     // played in Round 3 naming reveal
    }

    [Header("── ROUND 1 ITEMS (set 6: 3 correct, 3 decoy) ──")]
    public ItemData[] round1Items;   // pencil✓ eraser✓ notebook✓  toycar✗ apple✗ ball✗

    [Header("── ROUND 2 ITEMS (set 6: 3 correct, 3 decoy) ──")]
    public ItemData[] round2Items;   // textbook✓ ruler✓ colourpencil✓  umbrella✗ spoon✗ pillow✗

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────

    private int  currentRound   = 1;
    private int  correctPacked  = 0;
    private int  correctNeeded  = 0;

    // BUG FIX: prevents RoundCompleteFlow from being started twice when two
    // correct items land in the bag at the same moment.
    private bool roundCompleting = false;

    private List<DraggableItem_MyClass_Game> activeCards   = new List<DraggableItem_MyClass_Game>();
    private List<ItemData>                   packedItems   = new List<ItemData>(); // accumulates R1+R2

    // Round 3 — track which items the child has already tapped so we can
    // show the Done button once all have been heard at least once.
    private HashSet<string> namedItemsHeard = new HashSet<string>();

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        // First activation handled by OnEnable below.
    }

    void OnEnable()
    {
        // Fires every time the panel is SetActive(true).
        // On the very first enable (before GameFlowManager calls ResetAndStart),
        // we just ensure the UI starts in a clean hidden state.
        // On all subsequent enables, GameFlowManager has already called
        // ResetAndStart() right after SetActive so nothing extra is needed here.
    }

    /// <summary>
    /// Fully resets all state and restarts from Round 1.
    /// Called by GameFlowManager.GoToScreen2() AFTER SetActive(true),
    /// so the panel is always active when StartCoroutine fires.
    /// </summary>
    public void ResetAndStart()
    {
        StopAllCoroutines();

        // Reset state
        currentRound    = 1;
        correctPacked   = 0;
        correctNeeded   = 0;
        roundCompleting = false;
        packedItems.Clear();
        namedItemsHeard.Clear();

        // Reset UI
        ClearCards();
        celebrationPanel.SetActive(false);
        doneButton.SetActive(false);
        bagImage.sprite = bagEmptySprite;

        // Panel is active (GameFlowManager guarantees it), so this is safe.
        StartCoroutine(BeginRound(1));
    }

    // ─────────────────────────────────────────────────────────────
    //  ROUND MANAGEMENT
    // ─────────────────────────────────────────────────────────────

    IEnumerator BeginRound(int round)
    {
        currentRound   = round;
        correctPacked  = 0;
        roundCompleting = false;    // BUG FIX: reset guard at the start of every round
        ClearCards();

        if (round == 1 || round == 2)
        {
            ItemData[] items = (round == 1) ? round1Items : round2Items;

            correctNeeded = 0;
            foreach (var item in items)
                if (item.isSchoolItem) correctNeeded++;

            instructionLabel.text = round == 1
                ? "Pack your bag!\nDrag the SCHOOL items into your bag!"
                : "Keep going!\nDrag the right items!";

            StartCoroutine(LabelPop(instructionLabel.transform));
            PlayVoice(voice_PackYourBag);

            yield return new WaitForSeconds(0.5f);
            yield return SpawnCards(items);
        }
        else if (round == 3)
        {
            yield return Round3NamingFlow();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  CARD SPAWNING
    // ─────────────────────────────────────────────────────────────

    IEnumerator SpawnCards(ItemData[] items)
    {
        // Shuffle so the order is different every playthrough
        List<ItemData> shuffled = new List<ItemData>(items);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j   = UnityEngine.Random.Range(0, i + 1);
            var tmp = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = tmp;
        }

        foreach (var data in shuffled)
        {
            // BUG FIX: Instantiate with NO parent so the HorizontalLayoutGroup
            // never sees a blank/default card occupying a slot.
            // We move it into itemCardArea only after Setup() has run.
            // This also means it doesn't matter whether draggableItemPrefab is a
            // true Project asset or a scene object — either way the original stays
            // where it is and only the fully-configured clone enters the layout.
            GameObject go = Instantiate(draggableItemPrefab);
            go.SetActive(false);   // keep invisible while we configure it

            var card = go.GetComponent<DraggableItem_MyClass_Game>();
            card.Setup(data.name, data.sprite, data.isSchoolItem, bagDropZone);

            ItemData capturedData = data;
            DraggableItem_MyClass_Game capturedCard = card;
            card.OnDroppedInBag      = () => OnItemDroppedInBag(capturedCard, capturedData);
            card.OnDroppedOutsideBag = () => OnItemDroppedOutside(capturedCard);

            // NOW move into the layout and activate — one clean entry, no flash
            go.transform.SetParent(itemCardArea, false);
            go.SetActive(true);

            activeCards.Add(card);
            PlaySFX(sfx_Pop);

            yield return StartCoroutine(card.SpawnAnimation());
            card.RecordHome();

            yield return new WaitForSeconds(0.07f);
        }
    }

    void ClearCards()
    {
        foreach (var c in activeCards)
            if (c != null) Destroy(c.gameObject);
        activeCards.Clear();
    }

    // ─────────────────────────────────────────────────────────────
    //  DRAG EVENT HANDLERS
    // ─────────────────────────────────────────────────────────────

    void OnItemDroppedInBag(DraggableItem_MyClass_Game card, ItemData data)
    {
        if (data.isSchoolItem)
        {
            // ── CORRECT ──
            PlaySFX(sfx_ItemSlide);
            PlayVoice(voice_GreatChoice);

            packedItems.Add(data);
            correctPacked++;

            // Slide the card into the bag, then destroy it and check round end
            StartCoroutine(card.SlideIntoBagAnimation(bagDropZone, () =>
            {
                activeCards.Remove(card);
                Destroy(card.gameObject);
                CheckRoundComplete();
            }));
        }
        else
        {
            // ── WRONG ──
            PlaySFX(sfx_WrongBounce);
            PlayVoice(voice_WrongItem);
            StartCoroutine(card.BounceBackAnimation());
        }
    }

    void OnItemDroppedOutside(DraggableItem_MyClass_Game card)
    {
        StartCoroutine(card.SnapBackAnimation());
    }

    // BUG FIX: roundCompleting flag prevents this being called twice when two correct
    // items land simultaneously, which would start RoundCompleteFlow twice.
    void CheckRoundComplete()
    {
        if (roundCompleting) return;
        if (correctPacked >= correctNeeded)
        {
            roundCompleting = true;
            StartCoroutine(RoundCompleteFlow());
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  ROUND COMPLETE FLOW
    // ─────────────────────────────────────────────────────────────

    IEnumerator RoundCompleteFlow()
    {
        yield return new WaitForSeconds(0.3f);

        PlaySFX(sfx_Celebration);
        bagImage.sprite = bagFullSprite;
        StartCoroutine(BagZipAnimation());

        // Show celebration panel
        yield return new WaitForSeconds(0.5f);
        celebrationPanel.SetActive(true);
        celebrationText.text = currentRound < 2
            ? "Great job! 🎉\nKeep packing!"
            : "Bag is packed! 🎒⭐";
        StartCoroutine(LabelPop(celebrationText.transform));

        yield return new WaitForSeconds(1.8f);
        celebrationPanel.SetActive(false);
        bagImage.sprite = bagEmptySprite;

        if (currentRound == 1)
            StartCoroutine(BeginRound(2));
        else if (currentRound == 2)
            StartCoroutine(BeginRound(3));
    }

    // ─────────────────────────────────────────────────────────────
    //  ROUND 3 — NAMING BONUS
    // ─────────────────────────────────────────────────────────────

    IEnumerator Round3NamingFlow()
    {
        namedItemsHeard.Clear();

        instructionLabel.text = "What is in your bag? 🎒\nTap each item to hear its name!";
        StartCoroutine(LabelPop(instructionLabel.transform));
        PlayVoice(voice_WhatIsInYourBag);

        yield return new WaitForSeconds(1.2f);

        // Show every packed item as a tappable naming card
        foreach (var data in packedItems)
        {
            // BUG FIX: same pattern as SpawnCards — instantiate outside layout,
            // configure fully, then reparent so no blank slot ever appears.
            GameObject go = Instantiate(draggableItemPrefab);
            go.SetActive(false);

            var card = go.GetComponent<DraggableItem_MyClass_Game>();
            card.Setup(data.name, data.sprite, true, null); // null bag → not draggable

            string capturedName = data.name;

            card.SetNamingMode(data.voiceName, sfxSource, voiceSource, sfx_Pop);
            StartCoroutine(WatchForNamingTap(card, capturedName));

            go.transform.SetParent(itemCardArea, false);
            go.SetActive(true);

            activeCards.Add(card);

            PlaySFX(sfx_ItemReveal);
            yield return StartCoroutine(card.SpawnAnimation());
            card.RecordHome();

            yield return new WaitForSeconds(0.15f);
        }

        // Done button will appear once all items have been tapped (see WatchForNamingTap)
    }

    /// <summary>
    /// Watches the voice source each frame. When the clip changes to this card's voice,
    /// we know the child tapped it. Once all packed items are heard, show Done button.
    /// Simple polling — no need for extra events in DraggableItem.
    /// </summary>
    IEnumerator WatchForNamingTap(DraggableItem_MyClass_Game card, string itemName)
    {
        AudioClip targetClip = null;
        // Find the clip for this item from packedItems
        foreach (var d in packedItems)
            if (d.name == itemName) { targetClip = d.voiceName; break; }

        if (targetClip == null) yield break;

        bool heard = false;
        while (!heard)
        {
            // The voice source will be playing this clip if the child tapped the card
            if (voiceSource.clip == targetClip && voiceSource.isPlaying)
            {
                namedItemsHeard.Add(itemName);
                heard = true;
                CheckAllNamed();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    void CheckAllNamed()
    {
        if (namedItemsHeard.Count >= packedItems.Count)
        {
            StartCoroutine(ShowDoneButton());
        }
    }

    IEnumerator ShowDoneButton()
    {
        yield return new WaitForSeconds(0.5f);
        doneButton.SetActive(true);
        StartCoroutine(PopIn(doneButton.transform));
    }

    /// <summary>
    /// Called by the Done button's onClick event in the Inspector.
    /// Delegates to GameFlowManager — no scene loading needed.
    /// </summary>
    public void OnDoneButtonPressed()
    {
        PlaySFX(sfx_Pop);
        if (flowManager != null)
            flowManager.GoToUnitPanel();
        else
            Debug.LogError("[Screen2] flowManager is not assigned on " + gameObject.name +
                           ". Assign GameFlowManager_MyClass_Game in the Inspector.");
    }

    // ─────────────────────────────────────────────────────────────
    //  BAG ZIP ANIMATION
    // ─────────────────────────────────────────────────────────────

    IEnumerator BagZipAnimation()
    {
        Vector3 orig   = bagImage.transform.localScale;
        Vector3 squish = new Vector3(orig.x * 1.15f, orig.y * 0.85f, 1f);
        float   dur    = 0.25f;
        float   e      = 0f;

        while (e < dur) { e += Time.deltaTime; bagImage.transform.localScale = Vector3.Lerp(orig, squish, e / dur); yield return null; }
        e = 0f; dur = 0.2f;
        while (e < dur) { e += Time.deltaTime; bagImage.transform.localScale = Vector3.Lerp(squish, orig, e / dur); yield return null; }

        bagImage.transform.localScale = orig;
    }

    // ─────────────────────────────────────────────────────────────
    //  GENERIC ANIMATION HELPERS
    // ─────────────────────────────────────────────────────────────

    IEnumerator LabelPop(Transform t)
    {
        Vector3 orig = t.localScale;
        float   e    = 0f;
        float   dur  = 0.3f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float s = 1f + 0.28f * Mathf.Sin((e / dur) * Mathf.PI);
            t.localScale = orig * s;
            yield return null;
        }
        t.localScale = orig;
    }

    IEnumerator PopIn(Transform t)
    {
        t.localScale = Vector3.zero;
        float dur = 0.5f, e = 0f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float p = e / dur;
            float s = p < 0.7f
                ? Mathf.Lerp(0f, 1.15f, p / 0.7f)
                : Mathf.Lerp(1.15f, 1f, (p - 0.7f) / 0.3f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ─────────────────────────────────────────────────────────────
    //  AUDIO HELPERS
    // ─────────────────────────────────────────────────────────────

    void PlaySFX(AudioClip clip)
    {
        if (clip && sfxSource) sfxSource.PlayOneShot(clip);
    }

    void PlayVoice(AudioClip clip)
    {
        if (!clip || !voiceSource) return;
        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
    }
}