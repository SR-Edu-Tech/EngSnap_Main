using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Listening screen for one specific unit.
/// Lines are instantiated dynamically from a shared LineItem_BB1 prefab — same as original.
/// Text and timings are set directly in the Inspector on this component (no ScriptableObjects).
///
/// WIRING (Inspector):
///   panel              → parent UnitPanelController_BB1
///   unitButton         → the UnitButton_BB1 that launched this screen
///   lineContainer      → empty RectTransform that holds spawned lines
///   linePrefab         → shared LineItem_BB1 prefab
///   audioSource        → AudioSource component
///   mainAudio          → FALLBACK AudioClip (used if a poem has no clip assigned)
///   backgroundImage    → Image component used as the screen background
///   poems              → list of PoemEntry (each has its own audio + background)
/// </summary>
public class ListeningScreen_BB1 : MonoBehaviour
{

    private string saveKey;
    // ── Callback ──────────────────────────────────────────────────────────
    [Header("Callback")]
    public UnitPanelController_BB1 panel;
    public UnitButton_BB1 unitButton;

    // ── Line Prefab Spawning ──────────────────────────────────────────────
    [Header("Line Prefab")]
    public Transform lineContainer;      // Empty RectTransform — lines spawn here
    public GameObject linePrefab;        // Shared LineItem_BB1 prefab

    // ── Content (set per screen in Inspector) ─────────────────────────────
    
    [System.Serializable]
    public class PoemEntry
    {
        [Header("Content")]
        public string badgeText;
        public string mainTitle;
        public List<LineEntry> lines = new List<LineEntry>();

        [Header("Per-Poem Audio")]
        [Tooltip("Audio clip for this poem. If null, the global mainAudio fallback is used.")]
        public AudioClip audioClip;

        [Header("Per-Poem Background")]
        [Tooltip("Sprite to show as the screen background for this poem. Leave null to keep the current background.")]
        public Sprite backgroundSprite;

        [Tooltip("Tint colour applied on top of the background image. Set alpha to 0 to leave colour unchanged.")]
        public Color backgroundTint = new Color(1f, 1f, 1f, 0f); // transparent = no tint
    }

    public List<PoemEntry> poems = new List<PoemEntry>();
    private int currentPoemIndex = 0;

    // For backward compatibility, if only lines are set, wrap as one poem
    public List<LineEntry> lines = new List<LineEntry>();

    [System.Serializable]
    public class LineEntry
    {
        public string text;
        public float startTime;   // leave 0 on both for auto-distribution
        public float endTime;
    }

    // ── Audio ─────────────────────────────────────────────────────────────
    [Header("Audio")]
    public AudioSource audioSource;
    [Tooltip("Fallback audio clip used when a PoemEntry has no audioClip assigned.")]
    public AudioClip mainAudio;

    // ── UI ────────────────────────────────────────────────────────────────
    [Header("UI")]
    public Button replayButton;
    public Button slowButton; // Button to play at 0.75x speed
    public Button nextButton;
    public bool enableReplay = true;

    [Header("Background")]
    [Tooltip("Image component that covers the screen background. Its sprite & colour are swapped per poem.")]
    public Image backgroundImage;

    // ── Runtime ───────────────────────────────────────────────────────────
    private List<UpdateLineItem_BB1> lineItems  = new List<UpdateLineItem_BB1>();
    private bool audioCompleted           = false;
    private bool interactionCompleted     = true;
    private Coroutine segmentCoroutine    = null;
    private float playbackSpeed           = 1.0f;
    private bool isSlowMode               = false;   // tracks whether slow mode is active

    // Resolved each Setup() — points to the active poem's clip (or fallback)
    private AudioClip activeAudio;

    // ── Unity Lifecycle ───────────────────────────────────────────────────

    void OnEnable()
    {
        Setup();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();
    }

    // ── Setup ─────────────────────────────────────────────────────────────

    void Setup()
    {
        audioCompleted       = false;
        interactionCompleted = true;
        isSlowMode           = false;   // always start in normal mode
        playbackSpeed        = 1.0f;
        
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        if (unitButton != null && !string.IsNullOrEmpty(unitButton.unitID))
        {
            saveKey = unitButton.unitID + "_poemIndex";
            currentPoemIndex = PlayerPrefs.GetInt(saveKey, 0);
        }

        if (poems != null && poems.Count > 0)
        {
            currentPoemIndex = Mathf.Clamp(currentPoemIndex, 0, poems.Count - 1);
            var poem = poems[currentPoemIndex];
            lines = poem.lines;

            // ── Resolve audio for this poem ──────────────────────────────
            activeAudio = (poem.audioClip != null) ? poem.audioClip : mainAudio;

            // ── Apply background for this poem ───────────────────────────
            ApplyPoemBackground(poem);
        }
        else
        {
            // Fallback: no poems array configured
            activeAudio = mainAudio;
        }

        CreateLines();
        SetupButtons();
        PlayAudio();
        ShowPoemBadgeAndTitle();
    }

    /// <summary>
    /// Swaps the background Image sprite and tint for the current poem.
    /// </summary>
    void ApplyPoemBackground(PoemEntry poem)
    {
        if (backgroundImage == null) return;

        if (poem.backgroundSprite != null)
            backgroundImage.sprite = poem.backgroundSprite;

        // Only apply tint if the alpha channel is non-zero (i.e., something was set)
        if (poem.backgroundTint.a > 0f)
            backgroundImage.color = poem.backgroundTint;
    }

    void CreateLines()
    {
        // Destroy any previously spawned lines
        foreach (Transform child in lineContainer)
            Destroy(child.gameObject);

        lineItems.Clear();

        for (int i = 0; i < lines.Count; i++)
        {
            var entry = lines[i];
            GameObject obj = Instantiate(linePrefab, lineContainer);
            UpdateLineItem_BB1 item = obj.GetComponent<UpdateLineItem_BB1>();
            item.SetText(entry.text);
            lineItems.Add(item);

            // Add button component if not present
            Button btn = obj.GetComponent<Button>();
            if (btn == null)
                btn = obj.AddComponent<Button>();
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
                // Fix 2: Replay always plays at normal speed, clears slow mode
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
                // Fix 3: Toggle slow mode on/off
                isSlowMode    = !isSlowMode;
                playbackSpeed = isSlowMode ? 0.75f : 1.0f;
                UpdateSlowButtonVisual();
                PlayAudio();
            });
        }
    }

    /// <summary>
    /// Visually marks the slow button as active/inactive so the player knows the mode.
    /// Tint the button darker when active — swap for your own highlight logic if needed.
    /// </summary>
    void UpdateSlowButtonVisual()
    {
        if (slowButton == null) return;
        var colors = slowButton.colors;
        colors.normalColor = isSlowMode ? new Color(0.6f, 0.8f, 1f) : Color.white;
        slowButton.colors  = colors;
    }

    void OnNextClicked()
    {
        if (poems != null && poems.Count > 0 && currentPoemIndex < poems.Count - 1)
        {
            currentPoemIndex++;

            // SAVE PROGRESS HERE
            if (!string.IsNullOrEmpty(saveKey))
            {
                PlayerPrefs.SetInt(saveKey, currentPoemIndex);
                PlayerPrefs.Save();
            }

            Setup();
        }
        else
        {
            // Clear progress when completed
            if (!string.IsNullOrEmpty(saveKey))
            {
                PlayerPrefs.DeleteKey(saveKey);
            }

            if (panel != null && unitButton != null)
                panel.UnitFinished(unitButton);
            else
                gameObject.SetActive(false);
        }
    }

    // Animation stubs for badge/title (to be implemented)
    [Header("Badge/Title UI")]
    public RectTransform unitBadge;
    public TMPro.TextMeshProUGUI badgeText;
    public TMPro.TextMeshProUGUI mainTitle;

    void ShowPoemBadgeAndTitle()
    {
        if (poems != null && poems.Count > 0 && currentPoemIndex < poems.Count)
        {
            var poem = poems[currentPoemIndex];
            if (badgeText != null) badgeText.text = poem.badgeText;
            if (mainTitle != null) mainTitle.text = poem.mainTitle;
            // TODO: Animate badge drop and title reveal (see IntroManager_BB1)
        }
    }

    // ── Audio & Highlighting ──────────────────────────────────────────────

    void PlayAudio()
    {
        StopAllCoroutines();
        ResetHighlights();
        audioCompleted = false;

        // Use the per-poem resolved clip
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

        // Anchor to DSP time — never drifts, no 1-2 frame lag on mobile
        double playStartDsp = AudioSettings.dspTime;

        StartCoroutine(HighlightRoutine(playStartDsp));
        StartCoroutine(WaitForAudioEnd());
    }

    void OnLineItemClicked(int index)
    {
        if (segmentCoroutine != null)
            StopCoroutine(segmentCoroutine);
        segmentCoroutine = StartCoroutine(PlayAudioSegment(index));
    }

    IEnumerator PlayAudioSegment(int index)
    {
        if (audioSource == null || activeAudio == null || index < 0 || index >= lines.Count)
            yield break;

        var entry = lines[index];
        audioSource.Stop();
        audioSource.clip  = activeAudio;
        audioSource.time  = entry.startTime;
        audioSource.pitch = 1.0f;
        audioSource.Play();

        ApplyHighlight(index);

        while (audioSource.isPlaying && audioSource.time < entry.endTime)
        {
            yield return null;
        }
        audioSource.Stop();
        ResetHighlights();
        segmentCoroutine = null;
    }

    IEnumerator HighlightRoutine(double playStartDsp)
    {
        if (lineItems.Count == 0 || audioSource.clip == null)
            yield break;

        // Wait one frame — compressed clips (MP3/Vorbis) report length = 0
        // on the same frame they are assigned; prevents zero-duration windows.
        yield return null;

        List<Vector2> timingWindows = BuildTimingWindows();
        int  currentHighlight = -1;
        bool audioStarted     = false;

        // Capture the speed at the moment playback started so the routine stays
        // consistent even if playbackSpeed changes mid-flight (e.g. user taps slow again).
        float capturedSpeed = playbackSpeed;

        while (true)
        {
            // Fix 4: Wall-clock DSP elapsed × pitch = actual position in the audio timeline
            float elapsed = (float)(AudioSettings.dspTime - playStartDsp) * capturedSpeed;

            if (!audioStarted && elapsed > 0.01f) audioStarted = true;

            // Guard on audioStarted so we don't exit before isPlaying flips true
            if (audioStarted && !audioSource.isPlaying) break;

            int nextHighlight = -1;
            for (int i = 0; i < timingWindows.Count; i++)
            {
                Vector2 w      = timingWindows[i];
                bool    isLast = i == timingWindows.Count - 1;

                // Give the last line a small tail so it doesn't flash off one frame early
                if (elapsed >= w.x && (elapsed < w.y || (isLast && elapsed <= w.y + 0.1f)))
                {
                    nextHighlight = i;
                    break;
                }
            }

            if (nextHighlight != currentHighlight)
            {
                ApplyHighlight(nextHighlight);
                currentHighlight = nextHighlight;
            }

            yield return null;
        }

        ResetHighlights();
    }

    IEnumerator WaitForAudioEnd()
    {
        if (audioSource == null || audioSource.clip == null)
            yield break;

        // Wait until isPlaying flips true before polling — avoids false early exit
        // on slow devices that need 2+ frames to start audio
        yield return new WaitUntil(() => audioSource.isPlaying);

        while (audioSource.isPlaying)
            yield return null;

        // Fix 1: Slow plays once — after it ends, revert to normal mode automatically
        if (isSlowMode)
        {
            isSlowMode    = false;
            playbackSpeed = 1.0f;
            UpdateSlowButtonVisual();
        }

        audioCompleted = true;
        CheckCompletion();
    }

    // Call this from any tap/interaction in this screen if you need it
    public void MarkInteractionComplete()
    {
        interactionCompleted = true;
        CheckCompletion();
    }

    void CheckCompletion()
    {
        if (audioCompleted && interactionCompleted && nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    // ── Timing Windows ────────────────────────────────────────────────────

    List<Vector2> BuildTimingWindows()
    {
        var timings = new List<Vector2>(lines.Count);

        // Check if all entries have valid custom timings
        bool hasCustomTimings = true;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].endTime <= lines[i].startTime)
            {
                hasCustomTimings = false;
                break;
            }
        }

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

        // Auto-distribute by word count + punctuation weight
        float clipLength  = audioSource.clip.length;
        float totalWeight = 0f;
        var   weights     = new List<float>(lines.Count);

        for (int i = 0; i < lines.Count; i++)
        {
            string text       = lines[i].text ?? string.Empty;
            float  wordCount  = Mathf.Max(1, text.Split(' ').Length);
            float  punctBonus = CountOccurrences(text, '!') * 0.25f
                              + CountOccurrences(text, ',') * 0.15f
                              + CountOccurrences(text, '.') * 0.20f
                              + CountOccurrences(text, '?') * 0.25f;
            float  weight     = wordCount + punctBonus;
            weights.Add(weight);
            totalWeight      += weight;
        }

        float currentTime = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            float duration = clipLength * (weights[i] / Mathf.Max(totalWeight, 0.01f));
            float endTime  = i == weights.Count - 1
                           ? clipLength
                           : Mathf.Min(clipLength, currentTime + duration);
            timings.Add(new Vector2(currentTime, endTime));
            currentTime = endTime;
        }

        return timings;
    }

    // ── Highlight helpers ─────────────────────────────────────────────────

    void ApplyHighlight(int activeIndex)
    {
        for (int i = 0; i < lineItems.Count; i++)
            lineItems[i].Highlight(i == activeIndex);
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
        // Save current poem progress
        if (!string.IsNullOrEmpty(saveKey))
        {
            PlayerPrefs.SetInt(saveKey, currentPoemIndex);
            PlayerPrefs.Save();
        }

        // Stop audio & coroutines
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();

        // Go back to Unit Panel
        if (panel != null)
        {
            gameObject.SetActive(false);
            panel.gameObject.SetActive(true);
        }
    }
}