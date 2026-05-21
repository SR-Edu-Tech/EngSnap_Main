using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls Screen 2 — "MY STATIONERY".
/// 9 vocabulary cards in groups: [4] [4] [1].
///
/// KEY CHANGE: Cards are instantiated lazily per group.
/// When Next Group is pressed the current cards pop-out and are destroyed,
/// then the next group's cards are freshly instantiated and popped-in.
///
/// SCENE HIERARCHY — same as Screen 1:
///   Screen2Controller (this script)
///   ├─ PanelRoot (RectTransform + CanvasGroup)
///   ├─ SectionLabel (TMP_Text)
///   ├─ MascotBoy   (RectTransform)
///   ├─ MascotGirl  (RectTransform)
///   ├─ CardGrid    (GridLayoutGroup)
///   ├─ Btn_NextGroup (Button)
///   ├─ Btn_Next      (Button)
///   └─ Btn_Replay    (Button)
/// </summary>
public class Screen2Controller_MyClass_Reading : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────

    [Header("Data — 9 cards in order")]
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
[SerializeField] private GameObject screen3Object;

[Tooltip("If true, pressing Next finishes the unit instead of going to Screen 3")]
[SerializeField] private bool isLastScreen = false;  // ← ADD THIS
    // ── Runtime ────────────────────────────────────────────────────────────

    private List<VocabularyCardData_MyClass_Reading[]> _groupData = new();

    private VocabularyCardView_MyClass_Reading[] _activeCards;

    private int       _currentGroupIndex = 0;
    private bool      _autoPlayRunning   = false;
    private bool      _allDone           = false;
    private Coroutine _autoPlayCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        btnNextGroup.onClick.AddListener(OnNextGroupPressed);
        btnNext.onClick.AddListener(OnNextPressed);
        btnReplay.onClick.AddListener(OnReplayPressed);
    }

    private void OnEnable()
    {
        _currentGroupIndex = 0;
        _allDone           = false;
        _autoPlayRunning   = false;
        _activeCards       = null;

        HideButton(btnNext);
        HideButton(btnNextGroup);

        // Clear any leftover card objects from a previous activation
        for (int c = cardGrid.childCount - 1; c >= 0; c--)
            Destroy(cardGrid.GetChild(c).gameObject);

        SliceGroupData();
        StartCoroutine(ScreenEntrance());
    }

    // ── Slice raw data into groups ────────────────────────────────────────

    private void SliceGroupData()
    {
        _groupData.Clear();
        int total = allCards.Length;
        int idx = 0;
        while (idx < total)
        {
            int len = Mathf.Min(groupSize, total - idx);
            var slice = new VocabularyCardData_MyClass_Reading[len];
            for (int j = 0; j < len; j++)
                slice[j] = allCards[idx + j];
            _groupData.Add(slice);
            idx += len;
        }
    }

    // ── Screen Entrance ───────────────────────────────────────────────────

    private IEnumerator ScreenEntrance()
    {
        AudioManager_MyClass_Reading.Instance.PlayScreenEntry();
        yield return StartCoroutine(UIAnimator_MyClass_Reading.ScreenSlideIn(panelRoot, panelCG));
        yield return StartCoroutine(UIAnimator_MyClass_Reading.LabelDropIn(sectionLabel.rectTransform));

        StartCoroutine(UIAnimator_MyClass_Reading.MascotFloat(mascotBoy,  6f, 2.3f));
        StartCoroutine(UIAnimator_MyClass_Reading.MascotFloat(mascotGirl, 6f, 2.8f));

        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(ShowGroup(_currentGroupIndex));
    }

    // ── Show Group ────────────────────────────────────────────────────────

    private IEnumerator ShowGroup(int groupIndex)
    {
        // 1. Pop-out old cards and destroy
        if (_activeCards != null && _activeCards.Length > 0)
        {
            var oldTransforms = System.Array.ConvertAll(_activeCards, c => c.transform);
            yield return StartCoroutine(
                UIAnimator_MyClass_Reading.PopOutGroup(oldTransforms, cardStagger));

            foreach (var old in _activeCards)
                if (old != null) Destroy(old.gameObject);

            _activeCards = null;
        }

        // 2. Instantiate new group
        VocabularyCardData_MyClass_Reading[] data = _groupData[groupIndex];
        _activeCards = new VocabularyCardView_MyClass_Reading[data.Length];

        for (int j = 0; j < data.Length; j++)
        {
            var card = Instantiate(cardPrefab, cardGrid);
            card.Init(data[j], OnCardTapped);
            card.gameObject.SetActive(false);
            _activeCards[j] = card;
        }

        // 3. Staggered pop-in
        var transforms = System.Array.ConvertAll(_activeCards, c => c.transform);
        yield return StartCoroutine(UIAnimator_MyClass_Reading.PopInGroup(transforms, cardStagger));

        yield return new WaitForSeconds(0.2f);

        foreach (var card in _activeCards) card.SetInteractable(true);

        _autoPlayCoroutine = StartCoroutine(AutoPlayGroup(_activeCards));
    }

    // ── Auto-Play ─────────────────────────────────────────────────────────

    private IEnumerator AutoPlayGroup(VocabularyCardView_MyClass_Reading[] group)
    {
        _autoPlayRunning = true;
        SetButtonInteractable(btnNextGroup, false);
        SetButtonInteractable(btnReplay, false);

        for (int i = 0; i < group.Length; i++)
        {
            if (group[i] == null) continue;
            AudioClip clip = group[i].Audio;
            if (clip == null) continue;

            float len = clip.length;
            Coroutine glowCo = StartCoroutine(group[i].GlowForDuration(len));
            AudioManager_MyClass_Reading.Instance.PlayVoice(clip);

            yield return new WaitForSeconds(len + pauseBetweenWords);
            if (glowCo != null) StopCoroutine(glowCo);
        }

        _autoPlayRunning = false;

        bool isLast = _currentGroupIndex >= _groupData.Count - 1;
        if (isLast)
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

    // ── Tap Replay ────────────────────────────────────────────────────────

    private void OnCardTapped(VocabularyCardView_MyClass_Reading card)
    {
        if (_autoPlayRunning) return;
        StopAutoPlay();
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
        StartCoroutine(TransitionToScreen3());
    }

    private void OnReplayPressed()
    {
        AudioManager_MyClass_Reading.Instance.PlayButtonTap();
        StartCoroutine(UIAnimator_MyClass_Reading.ButtonPress(btnReplay.transform));
        StopAutoPlay();
        _autoPlayCoroutine = StartCoroutine(AutoPlayGroup(_activeCards));
    }

    // ── Navigation ────────────────────────────────────────────────────────

   private IEnumerator TransitionToScreen3()
{
    float t = 0f;
    while (t < 1f)
    {
        t += Time.deltaTime / 0.35f;
        panelCG.alpha = Mathf.Lerp(1f, 0f, UIAnimator_MyClass_Reading.EaseInOut(t));
        yield return null;
    }
    panelCG.alpha = 0f;

    yield return null;

    // ✅ If this is the last screen, finish the unit instead of going to Screen 3
    if (isLastScreen)
    {
        gameObject.SetActive(false);
        var manager = FindAnyObjectByType<ReadingManager_MyClass_Reading>();
        if (manager != null)
        {
            manager.UnitFinished();
            manager.gameObject.SetActive(false);
        }
    }
    else
    {
        if (screen3Object != null)
            screen3Object.SetActive(true);

        gameObject.SetActive(false);
    }
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