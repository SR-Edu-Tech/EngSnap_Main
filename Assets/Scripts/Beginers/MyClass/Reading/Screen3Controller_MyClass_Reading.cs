using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls Screen 3 — "CLASSROOM LANGUAGE".
/// 7 phrase cards revealed ONE AT A TIME as audio plays automatically.
/// Tap any visible card to replay that phrase.
/// After all 7 phrases play → NEXT button appears.
///
/// SCENE HIERARCHY:
///   Screen3Controller (this script)
///   ├─ PanelRoot (RectTransform + CanvasGroup)     ← slide-in root
///   ├─ SectionLabel (TMP_Text)                     ← "CLASSROOM LANGUAGE"
///   ├─ MascotBoy   (RectTransform)                 ← left, speaks phrases
///   ├─ MascotGirl  (RectTransform)                 ← right, nods as teacher
///   ├─ CardList    (VerticalLayoutGroup)            ← cards stacked vertically
///   ├─ Btn_Next      (Button)   ← hidden until all 7 phrases done
///   └─ Btn_Replay    (Button)
/// </summary>
public class Screen3Controller_MyClass_Reading : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Data — 7 phrase cards in order")]
    [SerializeField] private PhraseCardData_MyClass_Reading[] allPhrases;  // assign 7 SO assets

    [Header("Prefab")]
    [SerializeField] private PhraseCardView_MyClass_Reading cardPrefab;

    [Header("Scene References")]
    [SerializeField] private RectTransform  panelRoot;
    [SerializeField] private CanvasGroup    panelCG;
    [SerializeField] private TMP_Text       sectionLabel;
    [SerializeField] private Transform      cardList;       // VerticalLayoutGroup parent
    [SerializeField] private Transform      mascotBoy;
    [SerializeField] private Transform      mascotGirl;

    [Header("Buttons")]
    [SerializeField] private Button         btnNext;
    [SerializeField] private Button         btnReplay;

    [Header("Settings")]
    [SerializeField] private float pauseBetweenPhrases = 0.5f;  // silence gap between cards

    [Header("Mascot Nod Settings")]
    [SerializeField] private float nodAngle    = 15f;   // degrees girl mascot tilts
    [SerializeField] private float nodDuration = 0.3f;

    [Header("Screen Navigation")]
    [SerializeField] private GameObject nextScreenObject;   // activate to go to next screen

    // ── Runtime ────────────────────────────────────────────────────────────

    private List<PhraseCardView_MyClass_Reading> _cards = new List<PhraseCardView_MyClass_Reading>();
    private int       _currentIndex     = 0;   // next card to reveal
    private bool      _autoPlayRunning  = false;
    private bool      _allDone          = false;
    private Coroutine _autoPlayCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        btnNext.onClick.AddListener(OnNextPressed);
        btnReplay.onClick.AddListener(OnReplayPressed);
    }

    private void OnEnable()
    {
        // Reset state each time screen activates
        _currentIndex    = 0;
        _allDone         = false;
        _autoPlayRunning = false;

        HideButton(btnNext);

        BuildCards();
        StartCoroutine(ScreenEntrance());
    }

    // ── Build Cards ───────────────────────────────────────────────────────

    private void BuildCards()
    {
        // Destroy any previously created cards
        foreach (Transform child in cardList) Destroy(child.gameObject);
        _cards.Clear();

        foreach (var data in allPhrases)
        {
            PhraseCardView_MyClass_Reading card = Instantiate(cardPrefab, cardList);
            card.Init(data, OnCardTapped);
            card.gameObject.SetActive(false);   // hidden until revealed
            _cards.Add(card);
        }
    }

    // ── Screen Entrance ───────────────────────────────────────────────────

    private IEnumerator ScreenEntrance()
    {
        AudioManager_MyClass_Reading.Instance.PlayScreenEntry();

        yield return StartCoroutine(UIAnimator_MyClass_Reading.ScreenSlideIn(panelRoot, panelCG));
        yield return StartCoroutine(UIAnimator_MyClass_Reading.LabelDropIn(sectionLabel.rectTransform));

        // Mascots float
        StartCoroutine(UIAnimator_MyClass_Reading.MascotFloat(mascotBoy,  6f, 2.3f));
        StartCoroutine(UIAnimator_MyClass_Reading.MascotFloat(mascotGirl, 6f, 2.8f));

        yield return new WaitForSeconds(0.4f);

        // Begin revealing cards one by one
        _autoPlayCoroutine = StartCoroutine(AutoRevealSequence());
    }

    // ── Auto-Reveal Sequence ──────────────────────────────────────────────

    /// <summary>
    /// Reveals each phrase card one at a time, plays its audio with glow,
    /// triggers girl mascot nod, then pauses before the next card.
    /// </summary>
    private IEnumerator AutoRevealSequence()
    {
        _autoPlayRunning = true;
        SetButtonInteractable(btnReplay, false);

        for (int i = _currentIndex; i < _cards.Count; i++)
        {
            _currentIndex = i;
            PhraseCardView_MyClass_Reading card = _cards[i];

            // 1. Reveal card (slide-in animation)
            card.gameObject.SetActive(true);
            yield return StartCoroutine(card.RevealIn());

            // Enable tap on this card immediately after it appears
            card.SetInteractable(true);

            AudioClip clip = card.Audio;
            if (clip == null)
            {
                yield return new WaitForSeconds(pauseBetweenPhrases);
                continue;
            }

            float clipLength = clip.length;

            // 2. Play glow + audio simultaneously
            Coroutine glowCo = StartCoroutine(card.GlowForDuration(clipLength));
            AudioManager_MyClass_Reading.Instance.PlayVoice(clip);

            // 3. Boy mascot bounce (speaking gesture)
            StartCoroutine(UIAnimator_MyClass_Reading.TapBounce(mascotBoy, 0.35f));

            // 4. Girl mascot nods slightly after a short delay (teacher response)
            StartCoroutine(GirlNod());

            // 5. Wait for audio to finish
            yield return new WaitForSeconds(clipLength);
            StopCoroutine(glowCo);

            // 6. Pause before next card
            yield return new WaitForSeconds(pauseBetweenPhrases);
        }

        // All cards revealed and played
        _autoPlayRunning = false;
        _allDone = true;

        AudioManager_MyClass_Reading.Instance.PlaySuccessChime();
        ShowButton(btnNext);
        yield return StartCoroutine(ButtonPopIn(btnNext.transform));

        SetButtonInteractable(btnReplay, true);
    }

    // ── Girl Mascot Nod ───────────────────────────────────────────────────

    /// <summary>Girl mascot tilts forward then returns — simulates a teacher nod.</summary>
    private IEnumerator GirlNod()
    {
        // Small delay so the nod feels like a response to the phrase
        yield return new WaitForSeconds(0.25f);

        Quaternion origin = mascotGirl.localRotation;
        Quaternion tilt   = Quaternion.Euler(0f, 0f, -nodAngle);

        float half = nodDuration * 0.5f;
        float t = 0f;

        // Tilt forward
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            mascotGirl.localRotation = Quaternion.Lerp(origin, tilt, UIAnimator_MyClass_Reading.EaseInOut(t));
            yield return null;
        }

        t = 0f;
        // Return
        while (t < 1f)
        {
            t += Time.deltaTime / half;
            mascotGirl.localRotation = Quaternion.Lerp(tilt, origin, UIAnimator_MyClass_Reading.EaseOutBack(t));
            yield return null;
        }

        mascotGirl.localRotation = origin;
    }

    // ── Card Tap (replay any visible card) ────────────────────────────────

    private void OnCardTapped(PhraseCardView_MyClass_Reading card)
    {
        if (_autoPlayRunning) return;   // ignore taps during auto sequence

        StopAutoPlay();
        AudioManager_MyClass_Reading.Instance.StopVoice();
        _autoPlayCoroutine = StartCoroutine(ReplayCard(card));
    }

    private IEnumerator ReplayCard(PhraseCardView_MyClass_Reading card)
    {
        _autoPlayRunning = true;
        SetButtonInteractable(btnReplay, false);
        if (!_allDone) SetButtonInteractable(btnNext, false);

        AudioClip clip = card.Audio;
        float len = clip != null ? clip.length : 0f;

        if (clip != null)
        {
            Coroutine glowCo = StartCoroutine(card.GlowForDuration(len));
            AudioManager_MyClass_Reading.Instance.PlayVoice(clip);

            // Boy speaks, girl nods
            StartCoroutine(UIAnimator_MyClass_Reading.TapBounce(mascotBoy, 0.35f));
            StartCoroutine(GirlNod());

            yield return new WaitForSeconds(len);
            StopCoroutine(glowCo);
        }

        _autoPlayRunning = false;
        SetButtonInteractable(btnReplay, true);
        if (_allDone) SetButtonInteractable(btnNext, true);
    }

    // ── Replay Button — replays all phrases from the start ────────────────

    private void OnReplayPressed()
    {
        if (_autoPlayRunning) return;

        AudioManager_MyClass_Reading.Instance.PlayButtonTap();
        StartCoroutine(UIAnimator_MyClass_Reading.ButtonPress(btnReplay.transform));

        // Reset all cards — hide them, rebuild, then run sequence again
        StopAutoPlay();
        _allDone = false;
        _currentIndex = 0;
        HideButton(btnNext);

        // Hide all existing cards then re-reveal from card 1
        foreach (var c in _cards)
        {
            c.SetInteractable(false);
            c.gameObject.SetActive(false);
        }

        _autoPlayCoroutine = StartCoroutine(AutoRevealSequence());
    }

    // ── Next Button ───────────────────────────────────────────────────────

    private void OnNextPressed()
    {
        if (_autoPlayRunning) return;

        AudioManager_MyClass_Reading.Instance.PlayNextScreen();
        StartCoroutine(UIAnimator_MyClass_Reading.ButtonPress(btnNext.transform));
        StartCoroutine(TransitionToNextScreen());
    }
private IEnumerator TransitionToNextScreen()
{
    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / 0.35f;
        panelCG.alpha = Mathf.Lerp(1f, 0f, UIAnimator_MyClass_Reading.EaseInOut(t));
        yield return null;
    }
    panelCG.alpha = 0f;

    if (nextScreenObject != null)
        nextScreenObject.SetActive(true);

    yield return null;

    gameObject.SetActive(false);

    FindAnyObjectByType<ReadingManager_MyClass_Reading>().gameObject.SetActive(false);
}

    // ── Helpers ───────────────────────────────────────────────────────────

    private void StopAutoPlay()
    {
        if (_autoPlayCoroutine != null) StopCoroutine(_autoPlayCoroutine);
        _autoPlayRunning = false;
    }

    private void ShowButton(Button btn)
    {
        btn.gameObject.SetActive(true);
        SetButtonInteractable(btn, true);
    }

    private void HideButton(Button btn)
    {
        btn.gameObject.SetActive(false);
    }

    private void SetButtonInteractable(Button btn, bool value)
    {
        btn.interactable = value;
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg) cg.alpha = value ? 1f : 0.45f;
    }

    private IEnumerator ButtonPopIn(Transform btn)
    {
        AudioManager_MyClass_Reading.Instance.PlayButtonTap();
        yield return StartCoroutine(UIAnimator_MyClass_Reading.PopIn(btn, 0.3f));
        StartCoroutine(UIAnimator_MyClass_Reading.ButtonIdlePulse(btn));
    }
}
