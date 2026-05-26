using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ListeningScreen_BB1 : MonoBehaviour, IUnitCompletable
{
    private string saveKey;

    // ── IUnitCompletable — auto-set at runtime ────────────────────────────
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton unitButton;

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        panel      = sharedPanel;
        unitButton = sharedButton;
    }

    // ── Line Prefab Spawning ──────────────────────────────────────────────
    [Header("Line Prefab")]
    public Transform  lineContainer;
    public GameObject linePrefab;

    [System.Serializable]
    public class PoemEntry
    {
        [Header("Content")]
        public string badgeText;
        public string mainTitle;
        public List<LineEntry> lines = new List<LineEntry>();

        [Header("Per-Poem Audio")]
        public AudioClip audioClip;

        [Header("Per-Poem Background")]
        public Sprite backgroundSprite;
        public Color  backgroundTint = new Color(1f, 1f, 1f, 0f);
    }

    public List<PoemEntry> poems = new List<PoemEntry>();
    private int currentPoemIndex = 0;

    public List<LineEntry> lines = new List<LineEntry>();

    [System.Serializable]
    public class LineEntry
    {
        public string text;
        public float  startTime;
        public float  endTime;

        // ── Per-Line Background (optional) ───────────────────────────────
        // Leave lineBackgroundSprite = null and lineBackgroundTint.a = 0
        // to fall back to the poem-level background. Existing setups are
        // unaffected because these fields default to null / alpha-0.
        [Header("Per-Line Background (optional)")]
        public Sprite lineBackgroundSprite;
        public Color  lineBackgroundTint = new Color(1f, 1f, 1f, 0f);
    }

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   mainAudio;

    [Header("UI")]
    public Button replayButton;
    public Button slowButton;
    public Button nextButton;
    public bool   enableReplay = true;

    [Header("Background")]
    public Image backgroundImage;

    [Header("Badge/Title UI")]
    public RectTransform              unitBadge;
    public TMPro.TextMeshProUGUI      badgeText;
    public TMPro.TextMeshProUGUI      mainTitle;

    // ── Runtime ───────────────────────────────────────────────────────────
    private List<UpdateLineItem_BB1> lineItems            = new List<UpdateLineItem_BB1>();
    private bool                     audioCompleted       = false;
    private bool                     interactionCompleted = true;
    private Coroutine                segmentCoroutine     = null;
    private float                    playbackSpeed        = 1.0f;
    private bool                     isSlowMode           = false;
    private AudioClip                activeAudio;

    // Cached poem-level background so we can restore it when no line bg is set
    private Sprite activePoemSprite;
    private Color  activePoemTint;

    void OnEnable()  => Setup();
    void OnDisable() { StopAllCoroutines(); if (audioSource != null) audioSource.Stop(); }

    void Setup()
    {
        audioCompleted       = false;
        interactionCompleted = true;
        isSlowMode           = false;
        playbackSpeed        = 1.0f;

        if (nextButton != null) nextButton.gameObject.SetActive(false);

        // Build save key from unitButton if available
        if (unitButton != null)
        {
            saveKey = $"{unitButton.unitType}_poemIndex";
            currentPoemIndex = PlayerPrefs.GetInt(saveKey, 0);
        }

        if (poems != null && poems.Count > 0)
        {
            currentPoemIndex = Mathf.Clamp(currentPoemIndex, 0, poems.Count - 1);
            var poem = poems[currentPoemIndex];
            lines       = poem.lines;
            activeAudio = (poem.audioClip != null) ? poem.audioClip : mainAudio;
            ApplyPoemBackground(poem);
        }
        else
        {
            activeAudio = mainAudio;
        }

        CreateLines();
        SetupButtons();
        PlayAudio();
        ShowPoemBadgeAndTitle();
    }

    void ApplyPoemBackground(PoemEntry poem)
    {
        if (backgroundImage == null) return;

        // Cache the poem-level values so per-line logic can fall back to them
        activePoemSprite = poem.backgroundSprite;
        activePoemTint   = poem.backgroundTint;

        if (poem.backgroundSprite != null) backgroundImage.sprite = poem.backgroundSprite;
        if (poem.backgroundTint.a > 0f)   backgroundImage.color  = poem.backgroundTint;
    }

    // ── Per-Line Background ───────────────────────────────────────────────
    // Called whenever the highlighted line changes (index == -1 means no line
    // is active → restore the poem-level background).
    void ApplyLineBackground(int lineIndex)
    {
        if (backgroundImage == null) return;

        if (lineIndex >= 0 && lineIndex < lines.Count)
        {
            var entry = lines[lineIndex];

            // Use the line's sprite if one is assigned; otherwise keep current
            if (entry.lineBackgroundSprite != null)
                backgroundImage.sprite = entry.lineBackgroundSprite;
            else if (activePoemSprite != null)
                backgroundImage.sprite = activePoemSprite;

            // Use the line's tint if it has visible alpha; otherwise fall back
            if (entry.lineBackgroundTint.a > 0f)
                backgroundImage.color = entry.lineBackgroundTint;
            else if (activePoemTint.a > 0f)
                backgroundImage.color = activePoemTint;
        }
        else
        {
            // No active line — restore poem-level background
            if (activePoemSprite != null) backgroundImage.sprite = activePoemSprite;
            if (activePoemTint.a > 0f)   backgroundImage.color  = activePoemTint;
        }
    }

    void CreateLines()
    {
        foreach (Transform child in lineContainer) Destroy(child.gameObject);
        lineItems.Clear();

        for (int i = 0; i < lines.Count; i++)
        {
            GameObject obj  = Instantiate(linePrefab, lineContainer);
            var        item = obj.GetComponent<UpdateLineItem_BB1>();
            item.SetText(lines[i].text);
            lineItems.Add(item);

            Button btn = obj.GetComponent<Button>() ?? obj.AddComponent<Button>();
            int idx = i;
            btn.onClick.AddListener(() => OnLineItemClicked(idx));
        }
    }

    void SetupButtons()
    {
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(enableReplay);
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() =>
            {
                isSlowMode    = false;
                playbackSpeed = 1.0f;
                UpdateSlowButtonVisual();
                PlayAudio();
            });
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (slowButton != null)
        {
            slowButton.gameObject.SetActive(true);
            slowButton.onClick.RemoveAllListeners();
            slowButton.onClick.AddListener(() =>
            {
                isSlowMode    = !isSlowMode;
                playbackSpeed = isSlowMode ? 0.75f : 1.0f;
                UpdateSlowButtonVisual();
                PlayAudio();
            });
        }
    }

    void UpdateSlowButtonVisual()
    {
        if (slowButton == null) return;
        var colors         = slowButton.colors;
        colors.normalColor = isSlowMode ? new Color(0.6f, 0.8f, 1f) : Color.white;
        slowButton.colors  = colors;
    }

    void OnNextClicked()
    {
        if (poems != null && poems.Count > 0 && currentPoemIndex < poems.Count - 1)
        {
            currentPoemIndex++;
            if (!string.IsNullOrEmpty(saveKey)) { PlayerPrefs.SetInt(saveKey, currentPoemIndex); PlayerPrefs.Save(); }
            Setup();
        }
        else
        {
            if (!string.IsNullOrEmpty(saveKey)) { PlayerPrefs.DeleteKey(saveKey); PlayerPrefs.Save(); }

            if (panel != null && unitButton != null)
                panel.UnitFinished(unitButton);
            else
                gameObject.SetActive(false);
        }
    }

    void ShowPoemBadgeAndTitle()
    {
        if (poems != null && poems.Count > 0 && currentPoemIndex < poems.Count)
        {
            var poem = poems[currentPoemIndex];
            if (badgeText != null) badgeText.text = poem.badgeText;
            if (mainTitle != null) mainTitle.text  = poem.mainTitle;
        }
    }

    void PlayAudio()
    {
        StopAllCoroutines();
        ResetHighlights();
        audioCompleted = false;

        if (audioSource == null || activeAudio == null)
        {
            audioCompleted = true;
            CheckCompletion();
            return;
        }

        audioSource.Stop();
        audioSource.clip  = activeAudio;
        audioSource.pitch = playbackSpeed;
        audioSource.Play();

        double playStartDsp = AudioSettings.dspTime;
        StartCoroutine(HighlightRoutine(playStartDsp));
        StartCoroutine(WaitForAudioEnd());
    }

    void OnLineItemClicked(int index)
    {
        if (segmentCoroutine != null) StopCoroutine(segmentCoroutine);
        segmentCoroutine = StartCoroutine(PlayAudioSegment(index));
    }

    IEnumerator PlayAudioSegment(int index)
    {
        if (audioSource == null || activeAudio == null || index < 0 || index >= lines.Count) yield break;
        var entry = lines[index];
        audioSource.Stop();
        audioSource.clip  = activeAudio;
        audioSource.time  = entry.startTime;
        audioSource.pitch = 1.0f;
        audioSource.Play();
        ApplyHighlight(index);           // also swaps background for this line
        while (audioSource.isPlaying && audioSource.time < entry.endTime) yield return null;
        audioSource.Stop();
        ResetHighlights();               // restores poem-level background
        segmentCoroutine = null;
    }

    IEnumerator HighlightRoutine(double playStartDsp)
    {
        if (lineItems.Count == 0 || audioSource.clip == null) yield break;
        yield return null;

        List<Vector2> timingWindows    = BuildTimingWindows();
        int           currentHighlight = -1;
        bool          audioStarted    = false;
        float         capturedSpeed   = playbackSpeed;

        while (true)
        {
            float elapsed = (float)(AudioSettings.dspTime - playStartDsp) * capturedSpeed;
            if (!audioStarted && elapsed > 0.01f) audioStarted = true;
            if (audioStarted && !audioSource.isPlaying) break;

            int nextHighlight = -1;
            for (int i = 0; i < timingWindows.Count; i++)
            {
                Vector2 w      = timingWindows[i];
                bool    isLast = i == timingWindows.Count - 1;
                if (elapsed >= w.x && (elapsed < w.y || (isLast && elapsed <= w.y + 0.1f)))
                    { nextHighlight = i; break; }
            }

            if (nextHighlight != currentHighlight)
            {
                ApplyHighlight(nextHighlight);   // text highlight + background swap
                currentHighlight = nextHighlight;
            }
            yield return null;
        }
        ResetHighlights();
    }

    IEnumerator WaitForAudioEnd()
    {
        if (audioSource == null || audioSource.clip == null) yield break;
        yield return new WaitUntil(() => audioSource.isPlaying);
        while (audioSource.isPlaying) yield return null;

        if (isSlowMode) { isSlowMode = false; playbackSpeed = 1.0f; UpdateSlowButtonVisual(); }
        audioCompleted = true;
        CheckCompletion();
    }

    public void MarkInteractionComplete() { interactionCompleted = true; CheckCompletion(); }

    void CheckCompletion()
    {
        if (audioCompleted && interactionCompleted && nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    List<Vector2> BuildTimingWindows()
    {
        var  timings          = new List<Vector2>(lines.Count);
        bool hasCustomTimings = true;
        for (int i = 0; i < lines.Count; i++)
            if (lines[i].endTime <= lines[i].startTime) { hasCustomTimings = false; break; }

        if (hasCustomTimings)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                float clipLen = audioSource.clip.length;
                float start   = Mathf.Clamp(lines[i].startTime, 0f, clipLen);
                float end     = Mathf.Clamp(lines[i].endTime,   start, clipLen);
                timings.Add(new Vector2(start, end));
            }
            return timings;
        }

        float clipLength = audioSource.clip.length, totalWeight = 0f;
        var weights = new List<float>(lines.Count);
        for (int i = 0; i < lines.Count; i++)
        {
            string text       = lines[i].text ?? string.Empty;
            float  wordCount  = Mathf.Max(1, text.Split(' ').Length);
            float  punctBonus = CountOccurrences(text, '!') * 0.25f + CountOccurrences(text, ',') * 0.15f
                              + CountOccurrences(text, '.') * 0.20f + CountOccurrences(text, '?') * 0.25f;
            float w = wordCount + punctBonus;
            weights.Add(w); totalWeight += w;
        }

        float currentTime = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            float duration = clipLength * (weights[i] / Mathf.Max(totalWeight, 0.01f));
            float endTime  = i == weights.Count - 1 ? clipLength : Mathf.Min(clipLength, currentTime + duration);
            timings.Add(new Vector2(currentTime, endTime));
            currentTime = endTime;
        }
        return timings;
    }

    // ApplyHighlight now also drives the background swap
    void ApplyHighlight(int activeIndex)
    {
        for (int i = 0; i < lineItems.Count; i++) lineItems[i].Highlight(i == activeIndex);
        ApplyLineBackground(activeIndex);
    }

    void ResetHighlights() => ApplyHighlight(-1);

    int CountOccurrences(string text, char target)
    {
        int count = 0;
        foreach (char c in text) if (c == target) count++;
        return count;
    }

    public void OnBackClicked()
    {
        if (!string.IsNullOrEmpty(saveKey)) { PlayerPrefs.SetInt(saveKey, currentPoemIndex); PlayerPrefs.Save(); }
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();
        gameObject.SetActive(false);
        if (panel != null) panel.gameObject.SetActive(true);
    }
} 