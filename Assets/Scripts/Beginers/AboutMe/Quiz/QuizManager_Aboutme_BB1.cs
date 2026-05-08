using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// QuizManager_Aboutme_BB1  — Kindergarten Edition
///
/// FEATURES:
///  • Question text bounces in letter-by-letter
///  • Option buttons pop in one-by-one with a wobbly overshoot spring
///  • Correct answer → scale-up bounce + green flash
///  • Wrong answer  → red shake + correct revealed
///  • Audio button pulses BIG while audio plays
///  • Progress dots (Q1–Q5) always visible, tappable on completion panel
///  • Completion panel: filled/empty stars per question, Replay + Next buttons
///  • Replay restarts from Q1; tapping a dot replays only that question then returns
///
/// SCENE HIERARCHY (suggested):
///   QuizManager (this script + 2x AudioSource)
///   Canvas
///     ├── QuizPanel
///     │     ├── QuestionText (TMP)
///     │     ├── ImageContainer → QuestionImage (Image)
///     │     ├── ReplayAudioButton → ReplayIcon (Image)
///     │     ├── ProgressDotsParent   ← 5 dot Buttons spawned at runtime
///     │     └── OptionsParent        ← 3 option Buttons (A B C)
///     └── CompletionPanel (hidden at start)
///           ├── StarsParent          ← 5 star Images spawned at runtime
///           ├── ReplayQuizButton
///           └── NextButton
/// </summary>
public class QuizManager_Aboutme_BB1 : MonoBehaviour
{
    // ══════════════════════════════════════════════
    // DATA
    // ══════════════════════════════════════════════

    public enum QuestionType { FillAnswer, ListenAndPick, MoodMatch, PoemLine, IntroduceYourself }

    [System.Serializable]
    public class QuizQuestion
    {
        public QuestionType  type;
        [TextArea]
        public string        questionText;
        public AudioClip     questionAudio;
        public Sprite        questionImage;      // MoodMatch only
        public string[]      optionLabels;       // exactly 3
        public int           correctIndex;       // 0-based
        public AudioClip     wrongExplainAudio;
    }

    // ══════════════════════════════════════════════
    // INSPECTOR
    // ══════════════════════════════════════════════

    [Header("── Quiz Data ──")]
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    [Header("── Quiz Panel ──")]
    public GameObject      quizPanel;
    public TextMeshProUGUI questionText;
    public GameObject      imageContainer;
    public Image           questionImage;

    [Header("── Option Buttons (3) ──")]
    public Button[]            optionButtons;   // exactly 3, pre-placed in scene
    public TextMeshProUGUI[]   optionTexts;
    public Image[]             optionBGs;

    [Header("── Replay Audio Button ──")]
    public Button  replayAudioButton;
    public Image   replayAudioIcon;
    public Sprite  iconPlaying;
    public Sprite  iconIdle;

    [Header("── Progress Dots ──")]
    public Transform  progressDotsParent;   // horizontal layout group
    public Sprite     dotNormal;            // grey circle
    public Sprite     dotActive;            // coloured circle
    public Sprite     dotDone;             // white tick circle

    [Header("── Completion Panel ──")]
    public GameObject  completionPanel;
    public Transform   starsParent;             // horizontal layout group — 5 stars
    public Sprite      starFilled;
    public Sprite      starEmpty;
    public Transform   questionButtonsParent;   // layout group — 5 big Q buttons spawned at runtime
    public Sprite      qButtonNormal;           // round/rect sprite for idle state  (can be null → solid color)
    public Sprite      qButtonCorrect;          // optional tinted sprite when that Q was correct
    public Sprite      qButtonWrong;            // optional tinted sprite when that Q was wrong
    public Button      replayQuizButton;        // restarts from Q1
    public Button      nextButton;              // unitpanel / next scene
    public GameObject  unitPanel;

    [Header("── Audio ──")]
    public AudioSource  questionAudioSource;
    public AudioSource  sfxSource;
    public AudioClip    correctSFX;
    public AudioClip    wrongSFX;
    public AudioClip    popSFX;             // button pop-in sound
    public AudioClip    completeSFX;        // played on completion panel

    [Header("── Colors ──")]
    public Color normalColor  = new Color(1f,    1f,    1f,    1f);
    public Color correctColor = new Color(0.22f, 0.85f, 0.40f, 1f);
    public Color wrongColor   = new Color(0.92f, 0.25f, 0.25f, 1f);
    public Color dotActiveColor = new Color(1f, 0.85f, 0.2f, 1f);

    // ══════════════════════════════════════════════
    // PRIVATE STATE
    // ══════════════════════════════════════════════

    private int          _currentIndex   = 0;
    private bool         _answering      = false;
    private bool         _reviewMode     = false;   // true when replaying single Q from completion
    private bool[]       _results;                  // true = correct, false = wrong (per question)
    private Coroutine    _pulseCoroutine = null;

    // Runtime-spawned UI references
    private Image[]  _starImages;
    private Button[] _dotButtons;
    private Image[]  _dotImages;
    private Button[] _qButtons;     // big question buttons on completion panel
    private Image[]  _qButtonBGs;

    // ══════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════

    void Start()
    {
        BuildProgressDots();
        BuildStars();
        BuildQuestionButtons();

        replayAudioButton.onClick.AddListener(OnReplayAudioPressed);
        replayQuizButton.onClick.AddListener(OnReplayQuizPressed);
        nextButton.onClick.AddListener(OnNextPressed);

        ResetQuiz();
    }

    // Called every time the GameObject/panel is re-enabled (e.g. returning from unitPanel)
    void OnEnable()
    {
        // _results is null on very first enable (Start hasn't run yet), so guard
        if (_results != null)
            ResetQuiz();
    }

    /// <summary>
    /// Fully resets all state and restarts from Q1.
    /// Safe to call from Start, OnEnable, or the Replay button.
    /// </summary>
    void ResetQuiz()
    {
        _currentIndex = 0;
        _reviewMode   = false;
        _results      = new bool[questions.Count];

        // Stop any playing audio
        if (questionAudioSource != null) questionAudioSource.Stop();
        if (_pulseCoroutine != null) { StopCoroutine(_pulseCoroutine); _pulseCoroutine = null; }
        if (replayAudioButton != null)
        {
            replayAudioButton.transform.localScale = Vector3.one;
            replayAudioButton.interactable = true;
            if (replayAudioIcon != null && iconIdle != null)
                replayAudioIcon.sprite = iconIdle;
        }

        // Panels
        completionPanel.SetActive(false);
        quizPanel.SetActive(true);

        // Hide option buttons
        foreach (var btn in optionButtons)
        {
            btn.gameObject.SetActive(false);
            btn.interactable = true;
            btn.transform.localScale = Vector3.zero;
        }
        for (int i = 0; i < optionBGs.Length; i++)
            optionBGs[i].color = normalColor;

        // Reset stars
        if (_starImages != null)
            foreach (var s in _starImages)
                s.transform.localScale = Vector3.zero;

        // Reset dots
        if (_dotButtons != null)
        {
            for (int i = 0; i < _dotButtons.Length; i++)
            {
                _dotButtons[i].interactable = false;
                _dotImages[i].sprite = dotNormal;
                _dotImages[i].color  = Color.white;
            }
        }

        // Reset Q buttons
        if (_qButtons != null)
        {
            for (int i = 0; i < _qButtons.Length; i++)
            {
                _qButtons[i].interactable = false;
                _qButtons[i].transform.localScale = Vector3.zero;
            }
        }

        // Reset question text scale
        if (questionText != null)
            questionText.transform.localScale = Vector3.one;

        LoadQuestion(0);
    }

    // ══════════════════════════════════════════════
    // BUILD DYNAMIC UI
    // ══════════════════════════════════════════════

    void BuildProgressDots()
    {
        // Remove any existing children
        foreach (Transform c in progressDotsParent) Destroy(c.gameObject);

        _dotButtons = new Button[questions.Count];
        _dotImages  = new Image[questions.Count];

        for (int i = 0; i < questions.Count; i++)
        {
            int captured = i;

            // Create dot button
            GameObject dot = new GameObject($"Dot_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            dot.transform.SetParent(progressDotsParent, false);

            RectTransform rt = dot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60f, 60f);

            Image img = dot.GetComponent<Image>();
            img.sprite = dotNormal;
            img.color  = Color.white;

            // Label "1"–"5"
            GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(dot.transform, false);
            RectTransform lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            TextMeshProUGUI lbl = labelGO.GetComponent<TextMeshProUGUI>();
            lbl.text      = (i + 1).ToString();
            lbl.fontSize  = 22;
            lbl.fontStyle = FontStyles.Bold;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = Color.white;

            Button btn = dot.GetComponent<Button>();
            btn.interactable = false;   // enabled only on completion panel
            btn.onClick.AddListener(() => OnDotPressed(captured));

            _dotButtons[i] = btn;
            _dotImages[i]  = img;
        }
    }

    void BuildStars()
    {
        foreach (Transform c in starsParent) Destroy(c.gameObject);
        _starImages = new Image[questions.Count];

        for (int i = 0; i < questions.Count; i++)
        {
            GameObject star = new GameObject($"Star_{i + 1}", typeof(RectTransform), typeof(Image));
            star.transform.SetParent(starsParent, false);

            RectTransform rt = star.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80f, 80f);

            Image img = star.GetComponent<Image>();
            img.sprite = starEmpty;
            img.color  = Color.white;
            img.transform.localScale = Vector3.zero;   // hidden until completion

            _starImages[i] = img;
        }
    }

    void BuildQuestionButtons()
    {
        if (questionButtonsParent == null) return;
        foreach (Transform c in questionButtonsParent) Destroy(c.gameObject);

        _qButtons   = new Button[questions.Count];
        _qButtonBGs = new Image[questions.Count];

        // Bright kid-friendly colours per button
        Color[] palette = new Color[]
        {
            new Color(1.00f, 0.60f, 0.20f), // orange
            new Color(0.30f, 0.75f, 0.95f), // sky blue
            new Color(0.65f, 0.40f, 0.90f), // purple
            new Color(0.25f, 0.80f, 0.50f), // green
            new Color(0.95f, 0.35f, 0.55f), // pink
        };

        for (int i = 0; i < questions.Count; i++)
        {
            int captured = i;

            // ── Container button ──
            GameObject go = new GameObject($"QBtn_{i + 1}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(questionButtonsParent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110f, 110f);

            Image bg = go.GetComponent<Image>();
            bg.sprite = qButtonNormal;
            bg.color  = palette[i % palette.Length];
            bg.type   = Image.Type.Sliced;   // safe even if sprite is null

            Button btn = go.GetComponent<Button>();
            btn.interactable = false;         // enabled only when completion panel shows
            btn.onClick.AddListener(() => OnQuestionButtonPressed(captured));

            // ── Big number label ──
            GameObject numGO = new GameObject("Number",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            numGO.transform.SetParent(go.transform, false);
            RectTransform nrt = numGO.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0.25f);
            nrt.anchorMax = new Vector2(1f, 1.0f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;
            TextMeshProUGUI numTMP = numGO.GetComponent<TextMeshProUGUI>();
            numTMP.text      = (i + 1).ToString();
            numTMP.fontSize  = 42;
            numTMP.fontStyle = FontStyles.Bold;
            numTMP.alignment = TextAlignmentOptions.Center;
            numTMP.color     = Color.black;

            // ── Small "Q" label below the number ──
            GameObject lblGO = new GameObject("Label",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGO.transform.SetParent(go.transform, false);
            RectTransform lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0.35f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            TextMeshProUGUI lblTMP = lblGO.GetComponent<TextMeshProUGUI>();
            lblTMP.text      = "Q";
            lblTMP.fontSize  = 18;
            lblTMP.fontStyle = FontStyles.Bold;
            lblTMP.alignment = TextAlignmentOptions.Center;
            lblTMP.color     = new Color(0f, 0f, 0f, 0.6f);

            _qButtons[i]   = btn;
            _qButtonBGs[i] = bg;
        }
    }

    // ══════════════════════════════════════════════
    // QUESTION LOADING
    // ══════════════════════════════════════════════

    void LoadQuestion(int index)
    {
        if (index >= questions.Count) return;

        QuizQuestion q = questions[index];
        _answering = false;

        // Hide option buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(false);
            optionButtons[i].transform.localScale = Vector3.zero;
            SetButtonColor(i, normalColor);
            optionTexts[i].text = q.optionLabels[i];
        }

        // Image
        bool showImage = (q.type == QuestionType.MoodMatch && q.questionImage != null);
        imageContainer.SetActive(showImage);
        if (showImage) questionImage.sprite = q.questionImage;

        // Refresh dots (active dot highlighted)
        RefreshDots();

        // Animate question text in, then play audio, then pop buttons
        StartCoroutine(QuestionEntranceSequence(q));
    }

    IEnumerator QuestionEntranceSequence(QuizQuestion q)
    {
        // 1. Bounce-in the question text
        yield return StartCoroutine(BounceInText(questionText));

        // 2. Play audio
        float audioLen = 0f;
        if (q.questionAudio != null)
        {
            questionAudioSource.clip = q.questionAudio;
            questionAudioSource.Play();
            audioLen = q.questionAudio.length;
            StartReplayPulse(audioLen);
        }

        // 3. Pop buttons one by one (staggered, no need to wait for audio to finish)
        yield return StartCoroutine(PopInButtonsSequential(0.25f));
    }

    // ══════════════════════════════════════════════
    // ANIMATIONS — TEXT
    // ══════════════════════════════════════════════

    IEnumerator BounceInText(TextMeshProUGUI tmp)
    {
        tmp.transform.localScale = Vector3.zero;
        float elapsed  = 0f;
        float duration = 0.35f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Overshoot spring
            float s = 1f + 0.3f * Mathf.Sin(t * Mathf.PI) * (1f - t);
            tmp.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0f, s, Mathf.SmoothStep(0f, 1f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        tmp.transform.localScale = Vector3.one;
    }

    // ══════════════════════════════════════════════
    // ANIMATIONS — BUTTON POP-IN (one by one)
    // ══════════════════════════════════════════════

    IEnumerator PopInButtonsSequential(float delayBetween)
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(true);
            if (popSFX != null) sfxSource.PlayOneShot(popSFX);
            StartCoroutine(SpringPopIn(optionButtons[i].transform));
            yield return new WaitForSeconds(delayBetween);
        }

        // Wire callbacks
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int captured = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(captured));
            optionButtons[i].interactable = true;
        }

        _answering = true;
    }

    IEnumerator SpringPopIn(Transform t)
    {
        t.localScale = Vector3.zero;
        float elapsed  = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            float p = elapsed / duration;
            // Elastic overshoot
            float s = p < 0.6f
                ? Mathf.SmoothStep(0f, 1.25f, p / 0.6f)
                : Mathf.Lerp(1.25f, 1f, (p - 0.6f) / 0.4f);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    // ══════════════════════════════════════════════
    // ANSWER HANDLING
    // ══════════════════════════════════════════════

    void OnOptionSelected(int selectedIndex)
    {
        if (!_answering) return;
        _answering = false;

        foreach (var btn in optionButtons) btn.interactable = false;

        QuizQuestion q = questions[_currentIndex];
        bool isCorrect = (selectedIndex == q.correctIndex);

        // Record result only in normal quiz flow
        if (!_reviewMode)
            _results[_currentIndex] = isCorrect;

        if (isCorrect)
        {
            SetButtonColor(selectedIndex, correctColor);
            sfxSource.PlayOneShot(correctSFX);
            StartCoroutine(BounceCorrectButton(optionButtons[selectedIndex].transform));
            StartCoroutine(ProceedAfterDelay(1.2f));
        }
        else
        {
            SetButtonColor(selectedIndex, wrongColor);
            sfxSource.PlayOneShot(wrongSFX);
            StartCoroutine(ShakeButton(optionButtons[selectedIndex].transform));
            SetButtonColor(q.correctIndex, correctColor);

            if (q.wrongExplainAudio != null)
            {
                questionAudioSource.Stop();
                questionAudioSource.clip = q.wrongExplainAudio;
                questionAudioSource.Play();
            }

            StartCoroutine(ProceedAfterDelay(2f));
        }
    }

    // Scale up with a celebratory bounce
    IEnumerator BounceCorrectButton(Transform t)
    {
        float elapsed  = 0f;
        float duration = 0.5f;
        while (elapsed < duration)
        {
            float p = elapsed / duration;
            float s = 1f + 0.4f * Mathf.Sin(p * Mathf.PI);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    IEnumerator ProceedAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_reviewMode)
        {
            // Return to completion panel
            _reviewMode = false;
            ShowCompletionPanel();
            yield break;
        }

        _currentIndex++;

        if (_currentIndex >= questions.Count)
        {
            ShowCompletionPanel();
        }
        else
        {
            foreach (var btn in optionButtons) btn.interactable = true;
            LoadQuestion(_currentIndex);
        }
    }

    // ══════════════════════════════════════════════
    // PROGRESS DOTS
    // ══════════════════════════════════════════════

    void RefreshDots()
    {
        for (int i = 0; i < _dotImages.Length; i++)
        {
            if (i == _currentIndex)
            {
                _dotImages[i].sprite = dotActive;
                _dotImages[i].color  = dotActiveColor;
            }
            else
            {
                _dotImages[i].sprite = dotNormal;
                _dotImages[i].color  = Color.white;
            }
        }
    }

    // ══════════════════════════════════════════════
    // COMPLETION PANEL
    // ══════════════════════════════════════════════

    void ShowCompletionPanel()
    {
        quizPanel.SetActive(false);
        completionPanel.SetActive(true);

        // Enable dot buttons so kids can tap individual questions
        for (int i = 0; i < _dotButtons.Length; i++)
        {
            _dotButtons[i].interactable = true;
            _dotImages[i].sprite = _results[i] ? dotDone : dotNormal;
            _dotImages[i].color  = _results[i] ? correctColor : wrongColor;
        }

        // Enable + update big Q buttons
        if (_qButtons != null)
        {
            for (int i = 0; i < _qButtons.Length; i++)
            {
                _qButtons[i].interactable = true;

                // Tint button green/red based on result
                if (_results[i])
                {
                    _qButtonBGs[i].color = qButtonCorrect != null
                        ? Color.white
                        : new Color(0.22f, 0.80f, 0.45f);
                    if (qButtonCorrect != null) _qButtonBGs[i].sprite = qButtonCorrect;
                }
                else
                {
                    // Keep original palette colour but darken slightly for wrong
                    _qButtonBGs[i].color = new Color(
                        _qButtonBGs[i].color.r * 0.75f,
                        _qButtonBGs[i].color.g * 0.75f,
                        _qButtonBGs[i].color.b * 0.75f, 1f);
                    if (qButtonWrong != null) _qButtonBGs[i].sprite = qButtonWrong;
                }


            }

            // Staggered pop-in for Q buttons
            StartCoroutine(PopInQuestionButtons());
        }

        if (completeSFX != null) sfxSource.PlayOneShot(completeSFX);

        StartCoroutine(AnimateStars());
    }

    IEnumerator PopInQuestionButtons()
    {
        for (int i = 0; i < _qButtons.Length; i++)
        {
            _qButtons[i].transform.localScale = Vector3.zero;
        }

        yield return new WaitForSeconds(0.3f); // let stars start first

        for (int i = 0; i < _qButtons.Length; i++)
        {
            if (popSFX != null) sfxSource.PlayOneShot(popSFX);
            StartCoroutine(SpringPopIn(_qButtons[i].transform));
            yield return new WaitForSeconds(0.18f);
        }
    }

    IEnumerator AnimateStars()
    {
        for (int i = 0; i < _starImages.Length; i++)
        {
            _starImages[i].sprite = _results[i] ? starFilled : starEmpty;
            yield return StartCoroutine(StarPopIn(_starImages[i].transform, _results[i]));
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator StarPopIn(Transform t, bool filled)
    {
        t.localScale = Vector3.zero;
        float elapsed  = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            float p = elapsed / duration;
            float s = p < 0.65f
                ? Mathf.SmoothStep(0f, 1.35f, p / 0.65f)
                : Mathf.Lerp(1.35f, 1f, (p - 0.65f) / 0.35f);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;

        // Extra wiggle for filled (correct) stars
        if (filled) StartCoroutine(WiggleStar(t));
    }

    IEnumerator WiggleStar(Transform t)
    {
        float elapsed  = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            float angle = Mathf.Sin(elapsed * Mathf.PI * 6f) * 18f * (1f - elapsed / duration);
            t.localRotation = Quaternion.Euler(0f, 0f, angle);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localRotation = Quaternion.identity;
    }

    // ══════════════════════════════════════════════
    // COMPLETION PANEL BUTTONS
    // ══════════════════════════════════════════════

    void OnReplayQuizPressed()
    {
        ResetQuiz();
    }

    void OnNextPressed()
    {
        unitPanel.SetActive(true);
    }

    // Big Q button on completion panel tapped
    void OnQuestionButtonPressed(int index)
    {
        _currentIndex = index;
        _reviewMode   = true;

        completionPanel.SetActive(false);
        quizPanel.SetActive(true);

        // Disable all nav while reviewing
        foreach (var d in _dotButtons) d.interactable = false;
        if (_qButtons != null)
            foreach (var q in _qButtons) q.interactable = false;

        // Animate the tapped button before switching
        StartCoroutine(TapBounceAndLoad(index));
    }

    IEnumerator TapBounceAndLoad(int index)
    {
        // Quick bounce on the tapped button
        if (_qButtons != null && index < _qButtons.Length)
        {
            Transform t = _qButtons[index].transform;
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                float s = 1f + 0.3f * Mathf.Sin((elapsed / 0.25f) * Mathf.PI);
                t.localScale = Vector3.one * s;
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        LoadQuestion(index);
    }

    // Dot tapped on completion panel → replay only that question, then return
    void OnDotPressed(int index)
    {
        _currentIndex = index;
        _reviewMode   = true;

        completionPanel.SetActive(false);
        quizPanel.SetActive(true);

        // Reset dot interactivity while in review
        foreach (var d in _dotButtons) d.interactable = false;
        if (_qButtons != null)
            foreach (var q in _qButtons) q.interactable = false;

        LoadQuestion(index);
    }

    // ══════════════════════════════════════════════
    // REPLAY AUDIO BUTTON
    // ══════════════════════════════════════════════

    public void OnReplayAudioPressed()
    {
        if (_currentIndex >= questions.Count) return;
        QuizQuestion q = questions[_currentIndex];
        if (q.questionAudio == null) return;

        questionAudioSource.Stop();
        questionAudioSource.clip = q.questionAudio;
        questionAudioSource.Play();
        StartReplayPulse(q.questionAudio.length);
    }

    void StartReplayPulse(float duration)
    {
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(PulseReplayButton(duration));
    }

    IEnumerator PulseReplayButton(float duration)
    {
        if (replayAudioButton == null) yield break;

        if (replayAudioIcon != null && iconPlaying != null)
            replayAudioIcon.sprite = iconPlaying;

        replayAudioButton.interactable = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Big rhythmic pulse: 1 → 1.35 → 1 at ~1 Hz
            float pulse = 1f + 0.35f * Mathf.Abs(Mathf.Sin(elapsed * Mathf.PI * 1.5f));
            replayAudioButton.transform.localScale = Vector3.one * pulse;
            elapsed += Time.deltaTime;
            yield return null;
        }

        replayAudioButton.transform.localScale = Vector3.one;
        replayAudioButton.interactable = true;

        if (replayAudioIcon != null && iconIdle != null)
            replayAudioIcon.sprite = iconIdle;
    }

    // ══════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════

    void SetButtonColor(int index, Color color)
    {
        optionBGs[index].color = color;
    }

    IEnumerator ShakeButton(Transform t)
    {
        Vector3 origin    = t.localPosition;
        float   elapsed   = 0f;
        float   duration  = 0.45f;
        float   magnitude = 18f;

        while (elapsed < duration)
        {
            float x = Mathf.Sin(elapsed * Mathf.PI * 12f) * magnitude * (1f - elapsed / duration);
            t.localPosition = origin + new Vector3(x, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localPosition = origin;
    }
}