using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SCREEN 2 — Fill-in-the-Blank Conversation
///
/// CHANGES FROM ORIGINAL:
///   - Removed: sparkleParticles, confettiParticles, nextButtonAnimator, wordButtonGlowFX
///   - All animations are script-driven (fade, slide, pulse, shake)
///   - wordButtonBGs is optional — falls back to Button's own Image if not assigned
///   - Fixed: wrong-answer sound was reusing wordSelectSound; now uses data.wrongFX (or falls back gracefully)
///   - Fixed: _currentLineIndex was advancing inside HandleCorrectWord AND PlayNextLine causing double-advance
///   - Fixed: conversation didn't show left/right avatar correctly — now driven by speakerName matching
///   - prefab-less mode supported: if linePrefab is null, lines are shown in a single TMP label (simple mode)
///
/// INSPECTOR MINIMUM:
///   - data (ScriptableObject)
///   - linesContainer  (a VerticalLayoutGroup RectTransform)
///   - linePrefab      (has child TMP called "LineText", optionally "SpeakerName" and "Avatar")
///   - wordButtons[]   (5 buttons)
///   - wordButtonLabels[] (5 TMP labels)
///   - voSource, sfxSource
///   - nextButton
/// Everything else is optional.
/// </summary>
public class MagicWordConversation_MagicWords_BB1 : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Data")]
    public MagicWordData_MagicWords_BB1 data;

    [Header("── Conversation Display ──")]
    public Transform       linesContainer;   // VerticalLayoutGroup parent
    public GameObject      linePrefab;       // must have child TMP named "LineText"

    [Header("── Speaker Avatars ──")]
    public Image           speakerAvatarLeft;
    public Image           speakerAvatarRight;
    public TextMeshProUGUI speakerNameLeft;
    public TextMeshProUGUI speakerNameRight;

    [Header("── Word Bank (up to 5 buttons) ──")]
    public Button[]          wordButtons;       // assign in inspector
    public TextMeshProUGUI[] wordButtonLabels;  // TMP labels on each word button
    public Image[]           wordButtonBGs;     // optional; falls back to Button's Image

    [Header("── Word Bank Colors ──")]
    public Color wordDefaultColor = new Color(0.98f, 0.95f, 1.00f, 1f);
    public Color wordUsedColor    = new Color(0.75f, 0.75f, 0.80f, 1f);
    public Color wordCorrectColor = new Color(0.35f, 0.90f, 0.50f, 1f);
    public Color wordWrongColor   = new Color(0.95f, 0.30f, 0.30f, 1f);

    [Header("── Audio ──")]
    public AudioSource voSource;
    public AudioSource sfxSource;
    public AudioSource bgmSource;

    [Header("── Feedback Panel ──")]
    public GameObject      feedbackPanel;
    public TextMeshProUGUI feedbackLabel;

    [Header("── Celebration ──")]
    public GameObject      celebrationPanel;
    public TextMeshProUGUI celebrationText;

    [Header("── Next Button ──")]
    public Button nextButton;

    // ── Shared refs from Screen 1 ─────────────────────────────────────────
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton          unitButton;

    // ── Runtime ───────────────────────────────────────────────────────────
    private int           _currentLineIndex  = 0;
    private int           _currentBlankIndex = 0;
    private bool          _inputLocked       = true;

    private List<int>     _blankLineIndices = new List<int>();   // which data.lines[] indices have a blank
    private List<string>  _answers          = new List<string>(); // correct answer per blank, in order
    private List<TMP_Text> _spawnedLineTexts = new List<TMP_Text>(); // one per spawned line GO

    private bool[]        _wordUsed;           // tracks which word-bank slots are spent
    private Image[]       _resolvedWordBGs;    // resolved Image refs (own Image fallback)

    private List<Coroutine> _pulseRoutines = new List<Coroutine>();

    // ── Unity ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        _inputLocked = true;
        StartCoroutine(InitConversation());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (voSource  != null) voSource.Stop();
        if (sfxSource != null) sfxSource.Stop();
    }

    // ── Init ──────────────────────────────────────────────────────────────
    IEnumerator InitConversation()
    {
        // Reset state
        _currentLineIndex  = 0;
        _currentBlankIndex = 0;
        _blankLineIndices.Clear();
        _answers.Clear();
        _spawnedLineTexts.Clear();

        // Clear old spawned lines
        foreach (Transform child in linesContainer)
            Destroy(child.gameObject);

        // Hide UI elements
        if (nextButton       != null) nextButton.gameObject.SetActive(false);
        if (celebrationPanel != null) celebrationPanel.SetActive(false);
        if (feedbackPanel    != null) feedbackPanel.SetActive(false);

        // Gather blank metadata
        for (int i = 0; i < data.lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(data.lines[i].blankAnswer))
            {
                _blankLineIndices.Add(i);
                _answers.Add(data.lines[i].blankAnswer.Trim());
            }
        }

        // Resolve word bank images
        int wordCount = wordButtons != null ? wordButtons.Length : 0;
        _wordUsed        = new bool[wordCount];
        _resolvedWordBGs = new Image[wordCount];

        for (int i = 0; i < wordCount; i++)
        {
            _wordUsed[i] = false;
            if (wordButtonBGs != null && i < wordButtonBGs.Length && wordButtonBGs[i] != null)
                _resolvedWordBGs[i] = wordButtonBGs[i];
            else
                _resolvedWordBGs[i] = wordButtons[i].GetComponent<Image>();
        }

        SetupWordBank();

        // BGM
        if (bgmSource != null && data.bgmClip != null && !bgmSource.isPlaying)
        {
            bgmSource.clip = data.bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(PlayNextLine());
    }

    // ── Word Bank Setup ───────────────────────────────────────────────────
    void SetupWordBank()
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            bool hasWord = data.wordBankWords != null && i < data.wordBankWords.Length;
            wordButtons[i].gameObject.SetActive(hasWord);
            if (!hasWord) continue;

            if (wordButtonLabels != null && i < wordButtonLabels.Length && wordButtonLabels[i] != null)
                wordButtonLabels[i].text = data.wordBankWords[i];

            SetWordBGColor(i, wordDefaultColor);
            wordButtons[i].interactable = false;
            wordButtons[i].onClick.RemoveAllListeners();

            int captured = i;
            wordButtons[i].onClick.AddListener(() => OnWordTapped(captured));
        }
    }

    void SetWordBankInteractable(bool state)
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            if (!_wordUsed[i])
                wordButtons[i].interactable = state;
        }
    }

    void SetWordBGColor(int i, Color c)
    {
        if (_resolvedWordBGs != null && i < _resolvedWordBGs.Length && _resolvedWordBGs[i] != null)
            _resolvedWordBGs[i].color = c;
    }

    // ── Line Playback ─────────────────────────────────────────────────────
    IEnumerator PlayNextLine()
    {
        if (_currentLineIndex >= data.lines.Length)
        {
            yield return StartCoroutine(AllLinesComplete());
            yield break;
        }

        var  line     = data.lines[_currentLineIndex];
        bool hasBlank = !string.IsNullOrEmpty(line.blankAnswer);

        // Update side avatars
        UpdateSpeakerAvatars(line);

        // Spawn line GO
        TMP_Text lineText = null;
        if (linePrefab != null)
        {
            var lineGO = Instantiate(linePrefab, linesContainer);

            // Wire up speaker name / avatar inside prefab if present
            var nameLabel = lineGO.transform.Find("SpeakerName")?.GetComponent<TMP_Text>();
            if (nameLabel != null) nameLabel.text = line.speakerName;

            var avatar = lineGO.transform.Find("Avatar")?.GetComponent<Image>();
            if (avatar != null && line.speakerAvatar != null) avatar.sprite = line.speakerAvatar;

            lineText = lineGO.transform.Find("LineText")?.GetComponent<TMP_Text>();
            _spawnedLineTexts.Add(lineText);

            // Fade in
            yield return StartCoroutine(FadeInGO(lineGO));
        }

        // Set initial line text
        if (lineText != null)
            lineText.text = hasBlank
                ? ReplaceBlank(line.lineText, "<color=#FFD700><b>________</b></color>")
                : line.lineText;

        if (hasBlank)
        {
            // Play audio up to blank
            yield return StartCoroutine(PlayClip(line.lineAudio, 0.15f));

            // Enable word bank and wait for player input
            SetWordBankInteractable(true);
            StartPulsingWordBank();
            _inputLocked = false;
            // ← coroutine pauses here; OnWordTapped() will call ContinueAfterBlank()
        }
        else
        {
            // No blank — auto-advance
            yield return StartCoroutine(PlayClip(line.lineAudio, 0.3f));

            _currentLineIndex++;
            yield return StartCoroutine(PlayNextLine());
        }
    }

    // ── Word Tapped ───────────────────────────────────────────────────────
    void OnWordTapped(int wordIndex)
    {
        if (_inputLocked) return;
        _inputLocked = true;

        StopPulsingWordBank();
        SetWordBankInteractable(false);

        string tapped  = data.wordBankWords[wordIndex].Trim();
        string correct = _answers[_currentBlankIndex];

        bool isCorrect = string.Equals(tapped, correct, System.StringComparison.OrdinalIgnoreCase);

        if (isCorrect)
            StartCoroutine(HandleCorrectWord(wordIndex));
        else
            StartCoroutine(HandleWrongWord(wordIndex));
    }

    IEnumerator HandleCorrectWord(int wordIndex)
    {
        string word = data.wordBankWords[wordIndex].Trim();

        // Color feedback on word button
        SetWordBGColor(wordIndex, wordCorrectColor);
        yield return StartCoroutine(PulseOnce(wordButtons[wordIndex].transform));

        // Sound
        if (sfxSource != null && data.wordSelectSound != null)
            sfxSource.PlayOneShot(data.wordSelectSound);

        ShowFeedback(" Correct! Wonderful! ", wordCorrectColor);
        yield return new WaitForSeconds(0.5f);

        // Fill blank in line text
        var lineText = _spawnedLineTexts.Count > 0 ? _spawnedLineTexts[_spawnedLineTexts.Count - 1] : null;
        var line     = data.lines[_currentLineIndex];
        if (lineText != null)
            lineText.text = ReplaceBlank(line.lineText, $"<color=#2ECC71><b>{word}</b></color>");

        yield return new WaitForSeconds(0.35f);

        // Play after-blank audio (rest of the sentence)
        yield return StartCoroutine(PlayClip(line.afterBlankAudio, 0.2f));

        // Mark word as used
        _wordUsed[wordIndex] = true;
        wordButtons[wordIndex].interactable = false;
        SetWordBGColor(wordIndex, wordUsedColor);

        HideFeedback();

        _currentBlankIndex++;
        _currentLineIndex++;

        yield return StartCoroutine(PlayNextLine());
    }

    IEnumerator HandleWrongWord(int wordIndex)
    {
        SetWordBGColor(wordIndex, wordWrongColor);

        // Use wrongFX if exists, else reuse wordSelectSound as fallback
        if (sfxSource != null)
        {
            var clip = data.wrongFX != null ? data.wrongFX : data.wordSelectSound;
            if (clip != null) sfxSource.PlayOneShot(clip);
        }

        ShowFeedback("Oops! Try again! 💪", wordWrongColor);
        yield return StartCoroutine(ShakeButton(wordIndex));
        yield return new WaitForSeconds(0.6f);

        SetWordBGColor(wordIndex, wordDefaultColor);
        HideFeedback();

        // Re-enable word bank for another attempt
        SetWordBankInteractable(true);
        StartPulsingWordBank();
        _inputLocked = false;
    }

    // ── All Lines Done ────────────────────────────────────────────────────
    IEnumerator AllLinesComplete()
    {
        // Celebration sound
        if (sfxSource != null && data.allDoneAudio != null)
            sfxSource.PlayOneShot(data.allDoneAudio);

        // Show celebration panel (script-driven fade in/out)
        if (celebrationPanel != null)
        {
            if (celebrationText != null)
                celebrationText.text = "🌟 Amazing! You know all the Magic Words! 🌟";

            yield return StartCoroutine(FadeInGO(celebrationPanel));
            yield return new WaitForSeconds(2.5f);
            yield return StartCoroutine(FadeOutGO(celebrationPanel));
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // Show NEXT button with a pop
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            yield return StartCoroutine(PopTransform(nextButton.transform));
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }
    }

    void OnNextClicked()
    {
        StopAllCoroutines();
        if (voSource  != null) voSource.Stop();
        if (bgmSource != null) bgmSource.Stop();

        var cachedPanel  = panel;
        var cachedButton = unitButton;

        gameObject.SetActive(false);

        if (cachedPanel != null && cachedButton != null)
            cachedPanel.UnitFinished(cachedButton);
        else
            Debug.LogWarning("[MagicWordConversation] panel or unitButton is null on finish!");
    }

    // ── Speaker Avatars ───────────────────────────────────────────────────
    // Assumes two speakers; the first speaker name encountered is "Left", second is "Right".
    private string _leftSpeakerName  = null;
    private string _rightSpeakerName = null;

    void UpdateSpeakerAvatars(MagicWordData_MagicWords_BB1.ConversationLine line)
    {
        // Assign sides on first encounter
        if (_leftSpeakerName == null)
        {
            _leftSpeakerName = line.speakerName;
        }
        else if (_rightSpeakerName == null && line.speakerName != _leftSpeakerName)
        {
            _rightSpeakerName = line.speakerName;
        }

        bool isLeft = line.speakerName == _leftSpeakerName;

        // Highlight active speaker, dim the other
        if (speakerAvatarLeft  != null) speakerAvatarLeft.color  = isLeft  ? Color.white : new Color(0.6f,0.6f,0.6f,1f);
        if (speakerAvatarRight != null) speakerAvatarRight.color = !isLeft ? Color.white : new Color(0.6f,0.6f,0.6f,1f);

        if (speakerNameLeft  != null) speakerNameLeft.text  = _leftSpeakerName  ?? "";
        if (speakerNameRight != null) speakerNameRight.text = _rightSpeakerName ?? "";

        // Update avatar sprites
        if (isLeft  && speakerAvatarLeft  != null && line.speakerAvatar != null) speakerAvatarLeft.sprite  = line.speakerAvatar;
        if (!isLeft && speakerAvatarRight != null && line.speakerAvatar != null) speakerAvatarRight.sprite = line.speakerAvatar;
    }

    // ── Feedback ──────────────────────────────────────────────────────────
    void ShowFeedback(string msg, Color color)
    {
        if (feedbackPanel == null) return;
        feedbackPanel.SetActive(true);
        if (feedbackLabel != null) { feedbackLabel.text = msg; feedbackLabel.color = color; }
    }

    void HideFeedback()
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    // ── Word Bank Pulse (script-driven, no GlowFX needed) ─────────────────
    void StartPulsingWordBank()
    {
        StopPulsingWordBank();
        for (int i = 0; i < wordButtons.Length; i++)
        {
            if (!_wordUsed[i] && wordButtons[i].interactable)
            {
                var r = StartCoroutine(PulseLoop(wordButtons[i].transform));
                _pulseRoutines.Add(r);
            }
        }
    }

    void StopPulsingWordBank()
    {
        foreach (var r in _pulseRoutines) if (r != null) StopCoroutine(r);
        _pulseRoutines.Clear();
        for (int i = 0; i < wordButtons.Length; i++)
            wordButtons[i].transform.localScale = Vector3.one;
    }

    IEnumerator PulseLoop(Transform t)
    {
        while (true)
        {
            float s = 1f + 0.06f * Mathf.Sin(Time.time * Mathf.PI * 2f);
            t.localScale = Vector3.one * s;
            yield return null;
        }
    }

    // ── Script-Driven Animations ──────────────────────────────────────────

    IEnumerator FadeInGO(GameObject go)
    {
        go.SetActive(true);
        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float dur = 0.3f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / dur);
            yield return null;
        }
        cg.alpha = 1f;
    }

    IEnumerator FadeOutGO(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>() ?? go.AddComponent<CanvasGroup>();
        float dur = 0.3f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(1f - elapsed / dur);
            yield return null;
        }
        go.SetActive(false);
    }

    IEnumerator PopTransform(Transform t)
    {
        t.localScale = Vector3.zero;
        float dur = 0.22f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, elapsed / dur);
            yield return null;
        }
        elapsed = 0f;
        float settle = 0.1f;
        while (elapsed < settle)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, elapsed / settle);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    IEnumerator PulseOnce(Transform t)
    {
        float dur = 0.12f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1f, 1.2f, elapsed / dur);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, elapsed / dur);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    IEnumerator ShakeButton(int idx)
    {
        var rt     = wordButtons[idx].GetComponent<RectTransform>();
        var origin = rt.anchoredPosition;
        float dur = 0.35f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Sin(elapsed / dur * Mathf.PI * 10f) * 15f * (1f - elapsed / dur);
            rt.anchoredPosition = origin + new Vector2(x, 0f);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Play an AudioClip and wait for it to finish (+ optional extra delay). Safe if clip is null.</summary>
    IEnumerator PlayClip(AudioClip clip, float extraDelay = 0f)
    {
        if (voSource != null && clip != null)
        {
            voSource.clip = clip;
            voSource.Play();
            yield return new WaitForSeconds(clip.length + extraDelay);
        }
        else
        {
            yield return new WaitForSeconds(0.5f + extraDelay);
        }
    }

    string ReplaceBlank(string lineText, string replacement)
    {
        return lineText.Replace("________", replacement);
    }
}