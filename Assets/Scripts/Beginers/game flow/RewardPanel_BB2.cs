using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  RewardPanel_BB2  —  ONE shared panel, works for every topic.
/// ════════════════════════════════════════════════════════════════════
///
///  SCENE HIERARCHY (build once, disable GO at start):
///  ─────────────────────────────────────────────────
///  RewardPanel_BB2       [this script]  [CanvasGroup]
///    ├─ Overlay           Image (dark semi-transparent full-screen bg)
///    └─ Card              Image (white rounded card)
///          ├─ TitleText         TMP_Text  ← champion title (set per topic)
///          ├─ SubtitleText      TMP_Text  ← "UNIT COMPLETE"
///          ├─ StarsContainer    RectTransform  ← HorizontalLayoutGroup here
///          ├─ WordsLabel        TMP_Text  ← "Words You Learned:"
///          └─ WordsText         TMP_Text  ← words (set per topic)
///    ├─ ReplayButton      Button
///    └─ NextButton        Button
///
///  STAR PREFAB — keep it simple:
///  ──────────────────────────────
///  StarPrefab    [RectTransform]  [CanvasGroup on THIS root]
///    └─ StarImage    Image  (your starfish sprite, ~120×120 px)
///
///  No TMP_Text needed inside the prefab — label is created in code.
///  Do NOT set scale=0 on the prefab. Leave it at 1,1,1.
///
///  StarsContainer HorizontalLayoutGroup settings:
///    Child Alignment      : Middle Center
///    Spacing              : 10
///    Child Force Expand   : Width ✗  Height ✗
///    Child Control Size   : Width ✗  Height ✗
///
///  INSPECTOR WIRING:
///    titleText        TMP_Text  (TitleText)
///    subtitleText     TMP_Text  (SubtitleText)   optional
///    starsContainer   RectTransform  (StarsContainer)
///    starPrefab       your star prefab
///    wordsText        TMP_Text  (WordsText)
///    replayButton     Button
///    nextButton       Button
///    panelCanvasGroup CanvasGroup on root GO
///    popSound         AudioClip  (short pop)
///    fanfareSound     AudioClip  (celebration)
///    audioSource      AudioSource on this GO or child
/// </summary>
public class RewardPanel_BB2 : MonoBehaviour
{
    [Header("UI – Text")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text wordsText;

    [Header("UI – Stars")]
    public RectTransform starsContainer;
    public GameObject    starPrefab;

    [Header("UI – Buttons")]
    public Button replayButton;
    public Button nextButton;

    [Header("Panel Fade")]
    public CanvasGroup panelCanvasGroup;

    [Header("SFX")]
    public AudioClip   popSound;
    public AudioClip   fanfareSound;
    public AudioSource audioSource;

    [Header("Timing")]
    public float fadeInDuration  = 0.35f;
    public float starDelay       = 0.22f;
    public float starPopDuration = 0.40f;
    public float starOvershoot   = 1.35f;

    [Header("Label Style")]
    public TMP_FontAsset labelFont;
    public float         labelFontSize = 20f;
    public Color         labelColor    = Color.white;

    // ── Private ──────────────────────────────────────────────────────
    private SharedUnitPanelController _controller;
    private readonly List<GameObject> _stars = new List<GameObject>();

    // ════════════════════════════════════════════════════════════════
    //  PUBLIC API
    // ════════════════════════════════════════════════════════════════
 
    public void Show(TopicData_BB2 topicData, SharedUnitPanelController controller)
    {
        _controller = controller;
        StopAllCoroutines();
        DestroyAllStars();

        gameObject.SetActive(true);

        // ── Dynamic text (changes per topic) ─────────────────────────
        if (titleText != null)
            titleText.text = (topicData != null && !string.IsNullOrEmpty(topicData.championTitle))
                ? topicData.championTitle
                : (topicData != null ? $"{topicData.topicID} Champion" : "Champion!");

        if (subtitleText != null)
            subtitleText.text = "UNIT COMPLETE";

        if (wordsText != null)
            wordsText.text = (topicData != null
                              && topicData.learnedWords != null
                              && topicData.learnedWords.Length > 0)
                ? string.Join("  ·  ", topicData.learnedWords)
                : string.Empty;

        // ── Buttons ───────────────────────────────────────────────────
        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(OnReplay);
        }
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNext);
        }

        // ── Spawn ALL stars immediately so Layout Group positions them ─
        // They start invisible (CanvasGroup alpha=0) — pop animation fades them in.
        // This is the key fix: layout reads RectTransform size (correct at scale 1),
        // not CanvasGroup alpha, so positions are correct from frame 1.
        var entries = (topicData != null && topicData.unitEntries != null)
            ? topicData.unitEntries
            : new TopicUnitEntry[0];

        foreach (var entry in entries)
        {
            string name = !string.IsNullOrEmpty(entry.unitDisplayName)
                ? entry.unitDisplayName
                : entry.unitType.ToString();
            SpawnStar(name);   // spawns hidden, layout-positioned correctly
        }

        StartCoroutine(RunAnimation(entries.Length));
    }

    public void Hide()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════
    //  STAR SPAWNING
    //  Spawned at scale 1 so HorizontalLayoutGroup measures them correctly.
    //  CanvasGroup alpha=0 hides them until their pop coroutine runs.
    // ════════════════════════════════════════════════════════════════

    void SpawnStar(string unitName)
    {
        GameObject go = Instantiate(starPrefab, starsContainer);
        _stars.Add(go);

        // Scale stays at 1 — layout needs it. We hide via CanvasGroup.
        go.transform.localScale = Vector3.one;

        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Label — use existing TMP if prefab has one, else create it in code
        TMP_Text existingLabel = go.GetComponentInChildren<TMP_Text>(true);
        if (existingLabel != null)
        {
            existingLabel.text = unitName;
        }
        else
        {
            GameObject labelGO  = new GameObject("UnitLabel", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);

            TMP_Text tmp        = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text            = unitName;
            tmp.fontSize        = labelFontSize;
            tmp.color           = labelColor;
            tmp.alignment       = TextAlignmentOptions.Center;
            if (labelFont != null) tmp.font = labelFont;

            RectTransform rt    = labelGO.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.sizeDelta        = new Vector2(0f, 36f);
            rt.anchoredPosition = new Vector2(0f, -4f);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  ANIMATION — fade panel in, then pop each star in sequence
    // ════════════════════════════════════════════════════════════════

    IEnumerator RunAnimation(int count)
    {
        // Fade panel in
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            float e = 0f;
            while (e < fadeInDuration)
            {
                e += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(e / fadeInDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;
        }

        PlayClip(fanfareSound);
        yield return new WaitForSeconds(0.2f);

        // Pop stars one by one using their already-correct positions
        for (int i = 0; i < count && i < _stars.Count; i++)
        {
            StartCoroutine(PopStar(_stars[i]));
            PlayClip(popSound);
            yield return new WaitForSeconds(starDelay);
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  POP: scale down → punch up past 1 → settle at 1, fade in alpha
    //  Stars are already at scale 1 in layout. We animate a "squish then pop":
    //  start visually small (scale=0.01 so it's invisible), punch to overshoot, settle.
    // ════════════════════════════════════════════════════════════════

    IEnumerator PopStar(GameObject star)
    {
        if (star == null) yield break;
        CanvasGroup cg = star.GetComponent<CanvasGroup>();

        // Compress to near-zero instantly (visually hidden, layout unaffected)
        star.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // Phase 1: scale from ~0 → overshoot, fade in alpha
        float phase1 = starPopDuration * 0.6f;
        float t = 0f;
        while (t < phase1)
        {
            if (star == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / phase1);
            float s = EaseOutBack(p, starOvershoot);
            star.transform.localScale = Vector3.one * s;
            if (cg != null) cg.alpha  = Mathf.Clamp01(p * 3f);
            yield return null;
        }

        // Phase 2: overshoot → settle at 1
        float peakScale = EaseOutBack(1f, starOvershoot);
        float phase2    = starPopDuration * 0.4f;
        t = 0f;
        while (t < phase2)
        {
            if (star == null) yield break;
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / phase2);
            float s = Mathf.Lerp(peakScale, 1f, EaseOutCubic(p));
            star.transform.localScale = Vector3.one * s;
            yield return null;
        }

        if (star == null) yield break;
        star.transform.localScale = Vector3.one;
        if (cg != null) cg.alpha  = 1f;

        StartCoroutine(IdleBounce(star));
    }

    IEnumerator IdleBounce(GameObject star)
    {
        if (star == null) yield break;
        yield return new WaitForSeconds(Random.Range(0f, 0.5f));

        float speed  = Random.Range(1.5f, 2.5f);
        float amount = Random.Range(0.04f, 0.08f);
        float t      = Random.Range(0f, Mathf.PI * 2f);

        while (star != null && star.activeInHierarchy)
        {
            t += Time.deltaTime * speed;
            star.transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI * 2f) * amount);
            yield return null;
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  EASING
    // ════════════════════════════════════════════════════════════════

    static float EaseOutBack(float t, float o = 1.70158f)
    {
        t -= 1f;
        return t * t * ((o + 1f) * t + o) + 1f;
    }

    static float EaseOutCubic(float t)
    {
        t -= 1f;
        return t * t * t + 1f;
    }

    // ════════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════════

    void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    void DestroyAllStars()
    {
        foreach (var s in _stars)
            if (s != null) Destroy(s);
        _stars.Clear();

        // Belt-and-braces: clear any orphaned children
        if (starsContainer != null)
            foreach (Transform child in starsContainer)
                Destroy(child.gameObject);
    }

    void OnReplay() => _controller?.OnRewardReplay();
    void OnNext()   => _controller?.OnRewardNext();
}