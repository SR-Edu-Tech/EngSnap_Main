using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// ChitSortController_do  —  Screen 2 "Everyday I Do"
/// ─────────────────────────────────────────────────────────────────────────
/// Spawns 8 draggable chits from ONE prefab. Two bins: DO / DON'T.
/// Drag uses IDragHandler. DOTween handles snap-back and settle.
///
/// HIERARCHY:
///   Screen2_ChitSort
///     ├─ Tray                  ← HorizontalLayoutGroup — chits spawn here
///     ├─ DoBin                 ← Image (green bin)  — assign to doBin
///     ├─ DontBin               ← Image (red bin)    — assign to dontBin
///     ├─ DoGlow                ← Image (alpha 0, flashes on correct DO drop)
///     ├─ DontGlow              ← Image (alpha 0, flashes on correct DON'T drop)
///     ├─ SortedCounterText     ← TMP_Text
///     └─ NextButton            ← Button
///
/// CHIT PREFAB (one prefab):
///   ChitPrefab                 ← Image (bg) + CanvasGroup  [no script needed on prefab]
///     ├─ ChitImage             ← Image
///     └─ ChitLabel             ← TMP_Text
/// </summary>
public class ChitSortController_do : MonoBehaviour
{
    // ── Chit data ────────────────────────────────────────────────────────

    [System.Serializable]
    public class ChitData
    {
        public Sprite    chitSprite;
        public string    actionWord;
        public bool      isDO;
        public AudioClip correctAudioClip;   // "I read!" / "I don't shout!"
    }

    // ── Inspector ────────────────────────────────────────────────────────

    [Header("Prefab — ONE prefab for all 8 chits")]
    [SerializeField] private GameObject chitPrefab;

    [Header("Chit Data (8 entries)")]
    [SerializeField] private ChitData[] chitDataArray = new ChitData[8];

    [Header("Scene Refs")]
    [SerializeField] private RectTransform tray;
    [SerializeField] private RectTransform doBin;
    [SerializeField] private RectTransform dontBin;
    [SerializeField] private Image         doGlow;
    [SerializeField] private Image         dontGlow;
    [SerializeField] private TMP_Text      sortedCounterText;
    [SerializeField] private Button        nextButton;

    [Header("Audio")]
    [SerializeField] private AudioSource voSource;     // VO / speech clips
    [SerializeField] private AudioSource sfxSource;    // SFX (correct sparkle, wrong thud)
    [SerializeField] private AudioClip   introClip;
    [SerializeField] private AudioClip   wrongClip;
    [SerializeField] private AudioClip   allSortedClip;
    [SerializeField] private AudioClip   sfxCorrect;
    [SerializeField] private AudioClip   sfxWrong;

    // ── Runtime ──────────────────────────────────────────────────────────

    private GameManager_EverydayIDo_do _manager;
    private int  _sortedCount;
    private bool _inputLocked;

    // Per-chit runtime state stored in a small inner class
    private class ChitState
    {
        public RectTransform rect;
        public CanvasGroup   cg;
        public bool          isDO;
        public bool          sorted;
        public Vector2       homePos;         // anchored pos in tray
        public string        actionWord;
        public AudioClip     correctClip;
        public Image         chitImageComp;   // the child Image showing the sprite
    }
    private List<ChitState> _chits = new();
    private Canvas _rootCanvas;

    // Which chit is currently being dragged
    private ChitState _dragging;

    // ════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════

    public void StartGame(GameManager_EverydayIDo_do manager)
    {
        _manager     = manager;
        _sortedCount = 0;
        _inputLocked = false;
        _rootCanvas  = GetComponentInParent<Canvas>();

        StopAllCoroutines();
        SpawnChits();
        InitUI();
        StartCoroutine(IntroThenEnable());
    }

    public void ResetGame()
    {
        StopAllCoroutines();
        _sortedCount = 0;
        _inputLocked = false;
        if (voSource  != null) voSource.Stop();
        if (sfxSource != null) sfxSource.Stop();
        DestroyChits();
        ResetGlow(doGlow);
        ResetGlow(dontGlow);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Spawn
    // ════════════════════════════════════════════════════════════════════

    private void SpawnChits()
    {
        DestroyChits();
        if (chitPrefab == null) { Debug.LogError("[ChitSort] chitPrefab not assigned!"); return; }

        // Shuffle order
        var indices = new List<int>();
        for (int i = 0; i < chitDataArray.Length; i++) indices.Add(i);
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        foreach (int idx in indices)
        {
            var data = chitDataArray[idx];
            var go   = Instantiate(chitPrefab, tray);
            go.name  = $"Chit_{data.actionWord}";

            var rt   = go.GetComponent<RectTransform>();
            var cg   = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();

            // Find child Image for illustration
            Image chitImg = null;
            foreach (Transform child in go.transform)
            {
                var img = child.GetComponent<Image>();
                if (img != null) { chitImg = img; break; }
            }
            if (chitImg != null) chitImg.sprite = data.chitSprite;

            // Find child TMP_Text for label
            var label = go.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null) label.text = data.actionWord;

            var state = new ChitState
            {
                rect        = rt,
                cg          = cg,
                isDO        = data.isDO,
                sorted      = false,
                actionWord  = data.actionWord,
                correctClip = data.correctAudioClip,
                chitImageComp = chitImg,
            };
            _chits.Add(state);

            // Wire drag events via EventTrigger
            WireDragEvents(go, state);

            go.transform.localScale = Vector3.zero; // starts hidden
        }

        StartCoroutine(CacheHomesNextFrame());
    }

    private IEnumerator CacheHomesNextFrame()
    {
        yield return null; // let HorizontalLayoutGroup settle
        foreach (var c in _chits)
            c.homePos = c.rect.anchoredPosition;
    }

    private void DestroyChits()
    {
        foreach (var c in _chits) if (c.rect != null) Destroy(c.rect.gameObject);
        _chits.Clear();
        _dragging = null;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Wire drag via EventTrigger (no MonoBehaviour needed on prefab)
    // ════════════════════════════════════════════════════════════════════

    private void WireDragEvents(GameObject go, ChitState state)
    {
        var et = go.AddComponent<EventTrigger>();

        AddTrigger(et, EventTriggerType.BeginDrag, _ => OnBeginDrag(state));
        AddTrigger(et, EventTriggerType.Drag,      e => OnDrag(state, e));
        AddTrigger(et, EventTriggerType.EndDrag,   _ => OnEndDrag(state));
    }

    private static void AddTrigger(EventTrigger et, EventTriggerType type,
                                   UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        et.triggers.Add(entry);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Drag handlers
    // ════════════════════════════════════════════════════════════════════

    private void OnBeginDrag(ChitState state)
    {
        if (state.sorted || _inputLocked) return;
        _dragging = state;

        // Lift to root canvas so it renders above bins
        state.rect.SetParent(_rootCanvas.transform, true);
        state.rect.SetAsLastSibling();
        state.cg.blocksRaycasts = false;

        state.rect.DOKill();
        state.rect.transform.DOScale(1.12f, 0.1f).SetEase(Ease.OutBack);
    }

    private void OnDrag(ChitState state, BaseEventData data)
    {
        if (state.sorted || _dragging != state) return;
        var ped = (PointerEventData)data;
        state.rect.anchoredPosition += ped.delta / _rootCanvas.scaleFactor;
    }

    private void OnEndDrag(ChitState state)
    {
        if (_dragging != state) return;
        _dragging = null;

        state.cg.blocksRaycasts = true;

        // Re-parent back to tray so snap-back anchor works
        state.rect.SetParent(tray, true);

        // Check which bin the chit is over
        RectTransform hitBin = GetBinUnder(state.rect);

        if (hitBin == null)
        {
            SnapBack(state);
            return;
        }

        bool droppedOnDo = (hitBin == doBin);
        bool correct     = (state.isDO == droppedOnDo);

        if (correct) StartCoroutine(CorrectSort(state, hitBin));
        else         StartCoroutine(WrongSort(state));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Overlap check
    // ════════════════════════════════════════════════════════════════════

    private RectTransform GetBinUnder(RectTransform chit)
    {
        Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, chit.position);

        if (RectTransformUtility.RectangleContainsScreenPoint(doBin,   screenPos, cam)) return doBin;
        if (RectTransformUtility.RectangleContainsScreenPoint(dontBin, screenPos, cam)) return dontBin;
        return null;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Correct / Wrong
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator CorrectSort(ChitState state, RectTransform bin)
    {
        state.sorted = true;
        PlaySFX(sfxCorrect);

        // Settle chit into bin centre
        state.rect.SetParent(bin, true);
        state.rect.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.OutBack);
        state.rect.transform.DOScale(0.8f, 0.3f);

        // Glow the bin
        Image glow = (bin == doBin) ? doGlow : dontGlow;
        FlashGlow(glow);

        yield return new WaitForSeconds(0.25f);
        PlayVO(state.correctClip);

        _sortedCount++;
        UpdateCounter();

        if (_sortedCount >= chitDataArray.Length)
            StartCoroutine(AllSortedSequence());
    }

    private IEnumerator WrongSort(ChitState state)
    {
        PlaySFX(sfxWrong);
        SnapBack(state);
        yield return new WaitForSeconds(0.35f);
        PlayVO(wrongClip);
    }

    private void SnapBack(ChitState state)
    {
        state.rect.DOKill();
        state.rect.DOAnchorPos(state.homePos, 0.4f).SetEase(Ease.OutBack);
        state.rect.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        state.rect.transform.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 10, 0f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  All sorted
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllSortedSequence()
    {
        yield return new WaitForSeconds(0.4f);
        FlashGlow(doGlow);
        FlashGlow(dontGlow);
        PlayVO(allSortedClip);
        if (allSortedClip != null) yield return new WaitForSeconds(allSortedClip.length);
        if (nextButton != null) nextButton.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Glow helper
    // ════════════════════════════════════════════════════════════════════

    private void FlashGlow(Image glow)
    {
        if (glow == null) return;
        glow.DOKill();
        glow.color = new Color(1f, 1f, 0.4f, 0.7f);
        glow.DOFade(0f, 0.6f).SetEase(Ease.OutQuad);
    }

    private void ResetGlow(Image glow)
    {
        if (glow == null) return;
        glow.DOKill();
        var c = glow.color; c.a = 0f;
        glow.color = c;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Init / intro
    // ════════════════════════════════════════════════════════════════════

    private void InitUI()
    {
        UpdateCounter();
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => _manager?.OnScreen2Complete());
        }
    }

    private IEnumerator IntroThenEnable()
    {
        // Stagger chit pop-in
        for (int i = 0; i < _chits.Count; i++)
            _chits[i].rect.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(i * 0.08f);

        yield return new WaitForSeconds(_chits.Count * 0.08f + 0.4f);
        PlayVO(introClip);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Audio / counter
    // ════════════════════════════════════════════════════════════════════

    private void UpdateCounter()
    {
        if (sortedCounterText != null)
            sortedCounterText.text = $"Sorted: {_sortedCount} / {chitDataArray.Length}";
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip);
    }

    private void PlayVO(AudioClip clip)
    {
        if (voSource == null || clip == null) return;
        voSource.Stop(); voSource.clip = clip; voSource.Play();
    }
}