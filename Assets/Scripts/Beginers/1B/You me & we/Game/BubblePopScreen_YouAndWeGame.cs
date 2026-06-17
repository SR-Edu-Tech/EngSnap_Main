using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  BubblePopScreen_YouAndWeGame  —  Screen 2
///  Rising bubbles, each with an "I am ___" word.
///  Target prompt at top. Child taps the matching bubble.
/// ════════════════════════════════════════════════════════════════════
///
///  SCENE HIERARCHY  (Screen2_BubblePop GO — add this script here)
///  ─────────────────────────────────────────────────────────────────
///  Screen2_BubblePop        [this script]
///    ├─ BubbleSpawnArea      RectTransform  (full screen, bubbles born at bottom)
///    ├─ TargetPromptBG       Image (colourful banner at top)
///    │    └─ TargetText      TMP_Text  "Pop: I am HAPPY"
///    ├─ RobinSpeech          TMP_Text  (feedback / encouragement)
///    ├─ SparkleParticleRoot  RectTransform  (sparkle pool parent)
///    ├─ NextButton           Button         (disabled until all popped)
///    └─ SfxSource            AudioSource
///
///  BUBBLE PREFAB  (Bubble_Prefab):
///    Bubble_Prefab    [RectTransform] [CanvasGroup] [Image] [Button]
///                     [BubbleItem_YouAndWeGame]
///      └─ WordText    TMP_Text
///
///  Inspector:
///    bubblePrefab      → Bubble_Prefab
///    sparklePrefab     → a small sparkle/star prefab (Image, no script needed)
///    spawnArea         → BubbleSpawnArea RT
///    targetText        → TargetText TMP_Text
///    robinSpeech       → RobinSpeech TMP_Text
///    nextButton        → NextButton
///    sfxSource         → SfxSource
///    popClip           → bubble pop sound
///    wrongClip         → soft wobble/buzz
///    allDoneClip       → celebration fanfare
///    words             → array of affirmation words (strong, smart, kind…)
///    bubblesPerWord    → how many decoy bubbles alongside target (2-3)
/// </summary>
public class BubblePopScreen_YouAndWeGame : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject bubblePrefab;
    public GameObject sparklePrefab;

    [Header("Areas")]
    public RectTransform spawnArea;
    public RectTransform sparkleRoot;

    [Header("UI")]
    public TMP_Text targetText;
    public TMP_Text robinSpeech;
    public Button   nextButton;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip   popClip;
    public AudioClip   wrongClip;
    public AudioClip   allDoneClip;

    [Header("Data")]
    [Tooltip("strong, smart, kind, happy, brave, helpful, lucky, the best")]
    public string[] words = { "strong","smart","kind","happy","brave","helpful","lucky","the best" };

    [Header("Config")]
    [Tooltip("Extra decoy bubbles alongside the target each round")]
    public int   decoysPerRound  = 2;
    public float riseSpeed       = 55f;   // px/s
    public float bubbleSpawnInterval = 0.6f;

    // ── Private ──────────────────────────────────────────────────────
    private YouAndWeGameController_YouAndWeGame _controller;
    private List<BubbleItem_YouAndWeGame>       _activeBubbles = new List<BubbleItem_YouAndWeGame>();
    private List<string>                        _remainingWords;
    private string                              _currentTarget;
    private bool                                _roundActive   = false;
    private int                                 _totalRounds;
    private int                                 _roundsDone;

    // Lane system — divides spawn area into equal columns so bubbles never overlap
    private List<float> _laneXPositions = new List<float>();

    // ── Public API ───────────────────────────────────────────────────
    public void Initialise(YouAndWeGameController_YouAndWeGame controller)
    {
        _controller = controller;
        ResetScreen();
        StartCoroutine(RunGame());
    }

    void ResetScreen()
    {
        ClearBubbles();
        _remainingWords = new List<string>(words);
        ShuffleList(_remainingWords);
        _totalRounds = _remainingWords.Count;
        _roundsDone  = 0;

        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        SetTarget("Get ready…");
        SetRobinSpeech("Pop the right bubble! 🫧");
    }

    // ── Main game loop ────────────────────────────────────────────────
    IEnumerator RunGame()
    {
        yield return new WaitForSeconds(1f);

        while (_remainingWords.Count > 0)
        {
            _currentTarget = _remainingWords[0];
            _remainingWords.RemoveAt(0);

            yield return StartCoroutine(RunRound(_currentTarget));
        }

        // All done!
        PlayClip(allDoneClip);
        SetTarget("🌟 Amazing! 🌟");
        SetRobinSpeech("You know them all! You are the BEST!");
        yield return new WaitForSeconds(0.8f);
        if (nextButton != null) nextButton.interactable = true;
    }

    IEnumerator RunRound(string targetWord)
    {
        _roundActive = true;
        ClearBubbles();

        // Show target prompt
        SetTarget($"Pop: I am {targetWord.ToUpper()}");
        SetRobinSpeech($"Find… I am {targetWord}!");

        // Build pool: target + decoys (no repeats)
        List<string> pool = new List<string> { targetWord };
        List<string> decoyPool = new List<string>(words);
        decoyPool.Remove(targetWord);
        ShuffleList(decoyPool);
        for (int i = 0; i < decoysPerRound && i < decoyPool.Count; i++)
            pool.Add(decoyPool[i]);
        ShuffleList(pool);

        // Pre-compute one lane per bubble so none share the same column
        BuildLanes(pool.Count);
        ShuffleList(_laneXPositions);   // randomise lane assignment each round

        // Spawn bubbles staggered — each gets a unique lane X and a staggered Y start
        // so they enter the screen at different heights (no vertical pile-up either)
        float areaH   = spawnArea.rect.height;
        float yStep   = Mathf.Min(80f, areaH / (pool.Count + 1));   // vertical gap between entry points
        for (int i = 0; i < pool.Count; i++)
        {
            float laneX  = _laneXPositions[i];
            float startY = -(areaH * 0.5f + 60f) - i * yStep;      // earlier bubbles start lower → spread on screen
            SpawnBubble(pool[i], pool[i] == targetWord, laneX, startY);
            yield return new WaitForSeconds(bubbleSpawnInterval);
        }

        // Wait until target popped
        while (_roundActive)
            yield return null;

        _roundsDone++;
        yield return new WaitForSeconds(0.4f);
    }

    // Divide the spawn area width into count equal lanes and store centre X of each
    void BuildLanes(int count)
    {
        _laneXPositions.Clear();
        if (spawnArea == null || count <= 0) return;

        float areaW    = spawnArea.rect.width;
        float margin   = 60f;                          // keep bubbles away from edges
        float usable   = areaW - margin * 2f;
        float laneW    = usable / count;

        for (int i = 0; i < count; i++)
        {
            float centreX = -areaW * 0.5f + margin + laneW * i + laneW * 0.5f;
            _laneXPositions.Add(centreX);
        }
    }

    // ── Bubble spawning ───────────────────────────────────────────────
    // laneX and startY are now always supplied by RunRound via the lane system.
    void SpawnBubble(string word, bool isTarget, float laneX, float startY)
    {
        if (bubblePrefab == null) return;

        GameObject go = Instantiate(bubblePrefab, spawnArea);
        var bubble    = go.GetComponent<BubbleItem_YouAndWeGame>();
        if (bubble == null) bubble = go.AddComponent<BubbleItem_YouAndWeGame>();

        bubble.Initialise(word, isTarget, laneX, startY, riseSpeed, this, spawnArea);
        _activeBubbles.Add(bubble);
    }

    // ── Called by BubbleItem on tap ───────────────────────────────────
    public void OnBubbleTapped(BubbleItem_YouAndWeGame bubble, bool isTarget)
    {
        if (!_roundActive) return;

        if (isTarget)
        {
            // ✅ Correct pop
            PlayClip(popClip);
            bubble.PopAndDestroy();
            _activeBubbles.Remove(bubble);
            SpawnSparkles(bubble.transform.position);
            SetRobinSpeech($"I am {_currentTarget}! ⭐");
            StartCoroutine(PunchText(targetText));
            _roundActive = false;

            // Dismiss decoys gently
            StartCoroutine(DismissRemainingBubbles());
        }
        else
        {
            // ❌ Wrong — wobble
            PlayClip(wrongClip);
            bubble.Wobble();
            SetRobinSpeech("Almost! Try again! 😊");
        }
    }

    IEnumerator DismissRemainingBubbles()
    {
        yield return new WaitForSeconds(0.3f);
        foreach (var b in new List<BubbleItem_YouAndWeGame>(_activeBubbles))
        {
            if (b != null) b.FadeOutAndDestroy();
        }
        _activeBubbles.Clear();
    }

    // ── Sparkles ─────────────────────────────────────────────────────
    void SpawnSparkles(Vector3 worldPos)
    {
        if (sparklePrefab == null || sparkleRoot == null) return;
        for (int i = 0; i < 8; i++)
            StartCoroutine(SingleSparkle(worldPos, i));
    }

    IEnumerator SingleSparkle(Vector3 worldPos, int index)
    {
        yield return new WaitForSeconds(index * 0.04f);

        GameObject go = Instantiate(sparklePrefab, sparkleRoot);
        RectTransform rt = go.GetComponent<RectTransform>();
        CanvasGroup cg   = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        // Convert world → local in sparkleRoot
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            sparkleRoot,
            RectTransformUtility.WorldToScreenPoint(null, worldPos),
            null, out localPos);
        rt.anchoredPosition = localPos;

        float angle  = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist   = Random.Range(40f, 100f);
        Vector2 dir  = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        float t = 0f, dur = 0.55f;
        Vector2 startPos = localPos;
        while (t < dur)
        {
            if (go == null) yield break;
            t += Time.deltaTime;
            float p = t / dur;
            rt.anchoredPosition = startPos + dir * dist * p;
            cg.alpha            = 1f - p;
            float s             = Mathf.Lerp(1.2f, 0.2f, p);
            rt.localScale       = Vector3.one * s;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ── Helpers ───────────────────────────────────────────────────────
    void ClearBubbles()
    {
        foreach (var b in _activeBubbles)
            if (b != null) Destroy(b.gameObject);
        _activeBubbles.Clear();
    }

    void SetTarget(string text)  { if (targetText  != null) targetText.text  = text; }
    void SetRobinSpeech(string t){ if (robinSpeech != null) robinSpeech.text = t; }

    IEnumerator PunchText(TMP_Text txt)
    {
        if (txt == null) yield break;
        Vector3 orig = txt.transform.localScale;
        txt.transform.localScale = orig * 1.25f;
        yield return new WaitForSeconds(0.1f);
        txt.transform.localScale = orig;
    }

    void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    void OnNextClicked() => _controller?.OnGameComplete();

    static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}