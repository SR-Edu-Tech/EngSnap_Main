using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls Screen 1 — "IN MY CLASSROOM".
/// 14 vocabulary cards shown in groups of 4 with auto-play and tap-to-replay.
///
/// KEY CHANGE: Cards are instantiated lazily per group.
/// When Next Group is pressed the current cards pop-out and are destroyed,
/// then the next group's cards are freshly instantiated and popped-in.
/// This prevents hidden cards from occupying GridLayout space.
///
/// SCENE HIERARCHY:
///   Screen1Controller (this script)
///   ├─ PanelRoot (RectTransform + CanvasGroup)
///   ├─ SectionLabel (TMP_Text)
///   ├─ MascotBoy   (RectTransform)
///   ├─ MascotGirl  (RectTransform)
///   ├─ CardGrid    (GridLayoutGroup)
///   ├─ Btn_NextGroup (Button)
///   ├─ Btn_Next      (Button)
///   └─ Btn_Replay    (Button)
/// </summary>
public class Screen1Controller_MyClass_Reading : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Data — 14 cards in order")]
    [SerializeField] private VocabularyCardData_MyClass_Reading[] allCards;

    [Header("Prefab")]
    [SerializeField] private VocabularyCardView_MyClass_Reading cardPrefab;

    [Header("Scene References")]
    [SerializeField] private RectTransform  panelRoot;
    [SerializeField] private CanvasGroup    panelCG;
    [SerializeField] private TMP_Text       sectionLabel;
    [SerializeField] private Transform      cardGrid;
    [SerializeField] private Transform      mascotBoy;
    [SerializeField] private Transform      mascotGirl;

    [Header("Buttons")]
    [SerializeField] private Button         btnNextGroup;
    [SerializeField] private Button         btnNext;
    [SerializeField] private Button         btnReplay;

    [Header("Settings")]
    [SerializeField] private int   groupSize         = 4;
    [SerializeField] private float cardStagger        = 0.09f;
    [SerializeField] private float pauseBetweenWords  = 0.35f;

    [Header("Screen Navigation")]
    [SerializeField] private GameObject screen2Object;

    // ── Runtime ────────────────────────────────────────────────────────────

    // Raw data only — no pre-instantiated cards
    private List<VocabularyCardData_MyClass_Reading[]> _groupData = new();

    // The card views currently alive in the grid
    private VocabularyCardView_MyClass_Reading[] _activeCards;

    private int       _currentGroupIndex = 0;
    private bool      _autoPlayRunning   = false;
    private bool      _allDone           = false;
    private Coroutine _autoPlayCoroutine;
    private Coroutine _boyFloatCoroutine;
    private Coroutine _girlFloatCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────────

    private bool _initialized = false;

    private void Awake()
    {
        btnNextGroup.onClick.AddListener(OnNextGroupPressed);
        btnNext.onClick.AddListener(OnNextPressed);
        btnReplay.onClick.AddListener(OnReplayPressed);
    }

    /// <summary>
    /// Start runs once, guaranteed after Awake on all children is complete.
    /// Handles the very first activation safely.
    /// </summary>
    private void Start()
    {
        _initialized = true;
        ResetAndBegin();
    }

    /// <summary>
    /// OnEnable fires on every re-activation (returning from Unit Panel).
    /// Skips the first enable because Start() handles that — avoids the
    /// blank-card bug where OnEnable fires before child Awakes complete.
    /// </summary>
    private void OnEnable()
    {
        if (!_initialized) return;  // first run handled by Start()
        ResetAndBegin();
    }

    private void ResetAndBegin()
    {
        StopAllCoroutines();

        _currentGroupIndex  = 0;
        _allDone            = false;
        _autoPlayRunning    = false;
        _activeCards        = null;
        _boyFloatCoroutine  = null;
        _girlFloatCoroutine = null;

        // Destroy any card GameObjects left over from a previous run
        for (int i = cardGrid.childCount - 1; i >= 0; i--)
            Destroy(cardGrid.GetChild(i).gameObject);

        HideButton(btnNext);
        HideButton(btnNextGroup);

        // Reset panel so the entrance animation plays cleanly every time
        if (panelCG != null) panelCG.alpha = 1f;

        SliceGroupData();
        StartCoroutine(ScreenEntrance());
    }

    // ── Slice raw data into groups (no Instantiate here) ──────────────────

    private void SliceGroupData()
    {
        _groupData.Clear();
        int total = allCards.Length;
        int i = 0;
        while (i < total)
        {
            int len = Mathf.Min(groupSize, total - i);
            var slice = new VocabularyCardData_MyClass_Reading[len];
            for (int j = 0; j < len; j++)
                slice[j] = allCards[i + j];
            _groupData.Add(slice);
            i += len;
        }
    }

    // ── Screen Entrance ───────────────────────────────────────────────────

    private IEnumerator ScreenEntrance()
    {
        AudioManager_MyClass_Reading.Instance.PlayScreenEntry();

        yield return StartCoroutine(UIAnimator_MyClass_Reading.ScreenSlideIn(panelRoot, panelCG));
        yield return StartCoroutine(UIAnimator_MyClass_Reading.LabelDropIn(sectionLabel.rectTransform));

        _boyFloatCoroutine  = StartCoroutine(UIAnimator_MyClass_Reading.MascotFloat(mascotBoy,  6f, 2.3f));
        _girlFloatCoroutine = StartCoroutine(UIAnimator_MyClass_Reading.MascotFloat(mascotGirl, 6f, 2.8f));

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(ShowGroup(_currentGroupIndex));
    }

    // ── Show Group ────────────────────────────────────────────────────────
    // Pop-out old cards → destroy → instantiate new → pop-in

    private IEnumerator ShowGroup(int groupIndex)
    {
        // 1. Animate old cards out and destroy them so the grid is empty
        if (_activeCards != null && _activeCards.Length > 0)
        {
            var oldTransforms = System.Array.ConvertAll(_activeCards, c => c.transform);
            yield return StartCoroutine(
                UIAnimator_MyClass_Reading.PopOutGroup(oldTransforms, cardStagger));

            foreach (var old in _activeCards)
                if (old != null) Destroy(old.gameObject);

            _activeCards = null;
        }

        // 2. Instantiate this group's cards (hidden at scale 0)
        VocabularyCardData_MyClass_Reading[] data = _groupData[groupIndex];
        _activeCards = new VocabularyCardView_MyClass_Reading[data.Length];

        for (int j = 0; j < data.Length; j++)
        {
            var card = Instantiate(cardPrefab, cardGrid);
            card.Init(data[j], OnCardTapped);
            card.gameObject.SetActive(false);   // PopInGroup will activate them
            _activeCards[j] = card;
        }

        // 3. Staggered pop-in
        var transforms = System.Array.ConvertAll(_activeCards, c => c.transform);
        yield return StartCoroutine(UIAnimator_MyClass_Reading.PopInGroup(transforms, cardStagger));

        yield return new WaitForSeconds(0.2f);

        foreach (var card in _activeCards) card.SetInteractable(true);

        _autoPlayCoroutine = StartCoroutine(AutoPlayGroup(_activeCards));
    }

    // ── Auto-Play Sequence ────────────────────────────────────────────────

    private IEnumerator AutoPlayGroup(VocabularyCardView_MyClass_Reading[] group)
    {
        _autoPlayRunning = true;
        SetButtonInteractable(btnNextGroup, false);
        SetButtonInteractable(btnReplay, false);

        for (int i = 0; i < group.Length; i++)
        {
            if (group[i] == null) continue;    // guard: destroyed during transition
            AudioClip clip = group[i].Audio;
            if (clip == null) continue;

            float clipLength = clip.length;
            Coroutine glowCo = StartCoroutine(group[i].GlowForDuration(clipLength));
            AudioManager_MyClass_Reading.Instance.PlayVoice(clip);

            yield return new WaitForSeconds(clipLength + pauseBetweenWords);
            if (glowCo != null) StopCoroutine(glowCo);
        }

        _autoPlayRunning = false;

        bool isLastGroup = _currentGroupIndex >= _groupData.Count - 1;

        if (isLastGroup)
        {
            _allDone = true;
            AudioManager_MyClass_Reading.Instance.PlaySuccessChime();
            ShowButton(btnNext);
            yield return StartCoroutine(ButtonPopIn(btnNext.transform));
        }
        else
        {
            ShowButton(btnNextGroup);
            yield return StartCoroutine(ButtonPopIn(btnNextGroup.transform));
        }

        SetButtonInteractable(btnReplay, true);
    }

    // ── Tap-to-Replay ─────────────────────────────────────────────────────

    private void OnCardTapped(VocabularyCardView_MyClass_Reading card)
    {
        if (_autoPlayRunning) return;
        StopAutoPlay();
        AudioManager_MyClass_Reading.Instance.StopVoice();
        _autoPlayCoroutine = StartCoroutine(ReplayCard(card));
    }

    private IEnumerator ReplayCard(VocabularyCardView_MyClass_Reading card)
    {
        _autoPlayRunning = true;
        SetButtonInteractable(btnNextGroup, false);
        SetButtonInteractable(btnReplay, false);

        float len = card.Audio != null ? card.Audio.length : 0f;
        Coroutine glowCo = StartCoroutine(card.GlowForDuration(len));
        AudioManager_MyClass_Reading.Instance.PlayVoice(card.Audio);

        yield return new WaitForSeconds(len);
        StopCoroutine(glowCo);

        _autoPlayRunning = false;
        SetButtonInteractable(btnNextGroup, !_allDone);
        SetButtonInteractable(btnReplay, true);
    }

    // ── Button Handlers ───────────────────────────────────────────────────

    private void OnNextGroupPressed()
    {
        if (_autoPlayRunning) return;
        AudioManager_MyClass_Reading.Instance.PlayNextGroup();
        StartCoroutine(UIAnimator_MyClass_Reading.ButtonPress(btnNextGroup.transform));
        HideButton(btnNextGroup);

        _currentGroupIndex++;
        StartCoroutine(ShowGroup(_currentGroupIndex));
    }

    private void OnNextPressed()
    {
        if (_autoPlayRunning) return;
        AudioManager_MyClass_Reading.Instance.PlayNextScreen();
        StartCoroutine(UIAnimator_MyClass_Reading.ButtonPress(btnNext.transform));
        StartCoroutine(TransitionToScreen2());
    }

    private void OnReplayPressed()
    {
        AudioManager_MyClass_Reading.Instance.PlayButtonTap();
        StartCoroutine(UIAnimator_MyClass_Reading.ButtonPress(btnReplay.transform));
        StopAutoPlay();
        // Re-run auto-play on cards already in the grid (no re-instantiation)
        _autoPlayCoroutine = StartCoroutine(AutoPlayGroup(_activeCards));
    }

    // ── Navigation ────────────────────────────────────────────────────────

    private IEnumerator TransitionToScreen2()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            panelCG.alpha = Mathf.Lerp(1f, 0f, UIAnimator_MyClass_Reading.EaseInOut(t));
            yield return null;
        }
        panelCG.alpha = 0f;

        if (screen2Object != null)
            screen2Object.SetActive(true);

        yield return null;
        gameObject.SetActive(false);
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

    private void HideButton(Button btn) => btn.gameObject.SetActive(false);

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