using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SCREEN 1 — Magic Word Quiz
/// Fixes applied:
///   1. Closure capture bug fixed (displaySlot captured correctly per iteration).
///   2. optionCorrectFX / optionWrongFX removed — was crashing when array size 0.
///   3. All animations are script-driven (no Animators, no ParticleSystem needed).
///   4. optionBackgrounds is optional — color feedback works via Button's Image component directly.
/// </summary>
public class MagicWordQuiz_MagicWords_BB1 : MonoBehaviour
{
    [Header("Data")]
    public MagicWordData_MagicWords_BB1 data;

    [Header("── Situation Card ──")]
    public Image           situationImage;
    public TextMeshProUGUI situationText;

    [Header("── Round Counter ──")]
    public TextMeshProUGUI roundText;    // "Round 1 / 5"
    public Slider          progressBar;

    [Header("── Option Buttons (exactly 3) ──")]
    public Button[]          optionButtons;      // 3 buttons
    public TextMeshProUGUI[] optionLabels;        // 3 TMP labels (children of buttons)
    // optionBackgrounds is optional — if left empty the script uses the Button's own Image
    public Image[]           optionBackgrounds;  // can leave empty in inspector

    [Header("── Colors ──")]
    public Color colorNormal  = new Color(1f,    1f,    1f,    1f);
    public Color colorCorrect = new Color(0.35f, 0.90f, 0.50f, 1f);
    public Color colorWrong   = new Color(0.95f, 0.30f, 0.30f, 1f);

    [Header("── Audio ──")]
    public AudioSource sfxSource;
    public AudioSource voSource;
    public AudioSource bgmSource;

    [Header("── Feedback Panel ──")]
    public GameObject      feedbackPanel;
    public TextMeshProUGUI feedbackText;

    [Header("── Next Screen ──")]
    public MagicWordConversation_MagicWords_BB1 conversationScreen;

    // ── Runtime ──────────────────────────────────────────────────────────
    private int   _currentRound     = 0;
    private int   _correctThisRound = -1;
    private bool  _inputLocked      = false;
    private int[] _shuffledIndices;

    // Image components resolved at runtime (Button's own Image if optionBackgrounds not set)
    private Image[] _resolvedBGs;

    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton          unitButton;

    // ── Unity ────────────────────────────────────────────────────────────
    void OnEnable() => StartCoroutine(StartQuiz());

    // ── Entry Point ───────────────────────────────────────────────────────
    public IEnumerator StartQuiz()
    {
        _currentRound = 0;

        // Resolve background image references once
        _resolvedBGs = new Image[optionButtons.Length];
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionBackgrounds != null && i < optionBackgrounds.Length && optionBackgrounds[i] != null)
                _resolvedBGs[i] = optionBackgrounds[i];
            else
                _resolvedBGs[i] = optionButtons[i].GetComponent<Image>(); // fallback to button's own image
        }

        if (bgmSource != null && data.bgmClip != null)
        {
            bgmSource.clip = data.bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        yield return StartCoroutine(LoadRound(_currentRound));
    }

    // ── Load Round ────────────────────────────────────────────────────────
    IEnumerator LoadRound(int roundIndex)
    {
        _inputLocked = true;

        var round = data.rounds[roundIndex];

        // Round counter & progress
        if (roundText   != null) roundText.text      = $"Round {roundIndex + 1} / {data.rounds.Length}";
        if (progressBar != null) progressBar.value   = (float)roundIndex / data.rounds.Length;

        // Situation
        if (situationImage != null) situationImage.sprite = round.situationImage;
        if (situationText  != null) situationText.text    = round.situationText;

        // Script-driven situation card pop-in
        if (situationImage != null)
            yield return StartCoroutine(PopTransform(situationImage.transform));

        // Build shuffled options
        string[] options = { round.optionA, round.optionB, round.optionC };
        _shuffledIndices  = ShuffleIndices(3);
        _correctThisRound = -1;

        for (int i = 0; i < 3; i++)
        {
            int dataSlot = _shuffledIndices[i];

            // Set label
            if (optionLabels != null && i < optionLabels.Length && optionLabels[i] != null)
                optionLabels[i].text = options[dataSlot];

            // Reset color
            if (_resolvedBGs[i] != null) _resolvedBGs[i].color = colorNormal;

            // Disable while loading
            optionButtons[i].interactable = false;
            optionButtons[i].onClick.RemoveAllListeners();

            // ── FIX: capture loop variable in a local copy ──
            int capturedSlot = i;
            optionButtons[i].onClick.AddListener(() => OnOptionTapped(capturedSlot));

            // Track which display slot is correct
            if (dataSlot == round.correctIndex)
                _correctThisRound = i;
        }

        // Script-driven stagger pop-in for option buttons
        yield return StartCoroutine(PopCardsIn());

        // Play question audio
        if (voSource != null && round.questionAudio != null)
        {
            voSource.clip = round.questionAudio;
            voSource.Play();
            yield return new WaitForSeconds(round.questionAudio.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        // Enable input
        foreach (var btn in optionButtons) btn.interactable = true;
        _inputLocked = false;
    }

    // ── Option Tapped ─────────────────────────────────────────────────────
    void OnOptionTapped(int displaySlot)
    {
        if (_inputLocked) return;
        _inputLocked = true;
        foreach (var btn in optionButtons) btn.interactable = false;

        bool isCorrect = (displaySlot == _correctThisRound);
        StartCoroutine(HandleAnswer(displaySlot, isCorrect));
    }

    IEnumerator HandleAnswer(int tappedSlot, bool isCorrect)
    {
        var round = data.rounds[_currentRound];

        if (isCorrect)
        {
            // Flash correct color, pulse scale
            SetBGColor(tappedSlot, colorCorrect);
            StartCoroutine(PulseScale(optionButtons[tappedSlot].transform));

            if (sfxSource != null && data.correctFX != null)
                sfxSource.PlayOneShot(data.correctFX);

            ShowFeedback(" Correct! Great job! ", colorCorrect);

            if (voSource != null && round.correctAudio != null)
            {
                voSource.clip = round.correctAudio;
                voSource.Play();
                yield return new WaitForSeconds(round.correctAudio.length);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            HideFeedback();
            yield return new WaitForSeconds(0.3f);

            _currentRound++;
            if (_currentRound >= data.rounds.Length)
                yield return StartCoroutine(QuizComplete());
            else
                yield return StartCoroutine(LoadRound(_currentRound));
        }
        else
        {
            // Flash wrong color, shake
            SetBGColor(tappedSlot, colorWrong);

            if (sfxSource != null && data.wrongFX != null)
                sfxSource.PlayOneShot(data.wrongFX);

            ShowFeedback("Oops! Try again! 💪", colorWrong);

            yield return StartCoroutine(ShakeCard(tappedSlot));

            if (voSource != null && round.wrongAudio != null)
            {
                voSource.clip = round.wrongAudio;
                voSource.Play();
                yield return new WaitForSeconds(round.wrongAudio.length);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            // Reset tapped card back to normal
            SetBGColor(tappedSlot, colorNormal);
            HideFeedback();

            // Re-enable all buttons
            foreach (var btn in optionButtons) btn.interactable = true;
            _inputLocked = false;
        }
    }

    IEnumerator QuizComplete()
    {
        if (progressBar != null) progressBar.value = 1f;
        yield return new WaitForSeconds(0.5f);

        gameObject.SetActive(false);

        if (conversationScreen != null)
        {
            conversationScreen.panel      = panel;
            conversationScreen.unitButton = unitButton;
            conversationScreen.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[MagicWordQuiz] conversationScreen not assigned!");
        }
    }

    // ── Feedback ──────────────────────────────────────────────────────────
    void ShowFeedback(string msg, Color color)
    {
        if (feedbackPanel == null) return;
        feedbackPanel.SetActive(true);
        if (feedbackText != null) { feedbackText.text = msg; feedbackText.color = color; }
    }

    void HideFeedback()
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    // ── Script-Driven Animations ──────────────────────────────────────────

    /// <summary>Staggered pop-in for all 3 option buttons.</summary>
    IEnumerator PopCardsIn()
    {
        foreach (var btn in optionButtons)
            btn.transform.localScale = Vector3.zero;

        foreach (var btn in optionButtons)
        {
            yield return StartCoroutine(PopTransform(btn.transform));
            yield return new WaitForSeconds(0.06f);
        }
    }

    /// <summary>Scale from 0 → 1.12 → 1 (spring feel).</summary>
    IEnumerator PopTransform(Transform t)
    {
        float dur = 0.2f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(0f, 1.12f, elapsed / dur);
            yield return null;
        }
        elapsed = 0f;
        float settle = 0.1f;
        while (elapsed < settle)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, elapsed / settle);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    /// <summary>Quick upward pulse on correct answer.</summary>
    IEnumerator PulseScale(Transform t)
    {
        float dur = 0.15f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(1f, 1.18f, elapsed / dur);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Lerp(1.18f, 1f, elapsed / dur);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    /// <summary>Horizontal shake on wrong answer.</summary>
    IEnumerator ShakeCard(int slot)
    {
        var rt     = optionButtons[slot].GetComponent<RectTransform>();
        var origin = rt.anchoredPosition;
        float dur = 0.4f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float x = Mathf.Sin(elapsed / dur * Mathf.PI * 10f) * 18f * (1f - elapsed / dur);
            rt.anchoredPosition = origin + new Vector2(x, 0f);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    void SetBGColor(int slot, Color color)
    {
        if (_resolvedBGs != null && slot < _resolvedBGs.Length && _resolvedBGs[slot] != null)
            _resolvedBGs[slot].color = color;
    }

    int[] ShuffleIndices(int count)
    {
        int[] arr = new int[count];
        for (int i = 0; i < count; i++) arr[i] = i;
        for (int i = count - 1; i > 0; i--)
        {
            int j   = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }
}