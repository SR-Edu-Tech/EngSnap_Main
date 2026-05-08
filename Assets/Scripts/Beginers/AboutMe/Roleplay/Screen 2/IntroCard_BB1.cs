using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Self-Introduction Card — Phase 1 (Fill-in-Blanks)  [ENHANCED — Kindergarten Edition]
///
/// What's new vs the original:
///   • Typewriter effect — on Start() each sentence animates in one character at a time.
///   • Option buttons pop in one-by-one with a punch-scale + bounce tween (no DOTween needed).
///   • SFX hooks:
///       sfxOptionPop      — plays once per option button as it pops in
///       sfxButtonClick    — plays when a blank button or option is tapped
///       sfxKeyPress       — plays every key press (same clip as option pop, or assign separately)
///       sfxBackspace      — plays on ⌫
///       sfxSubmit         — plays when Done / Proceed is tapped
///   • Proceed button pops in with the same bounce when revealed.
///
/// ── Inspector wiring ─────────────────────────────────────────────────────────
///   (same as original — NEW fields marked with ★)
///
/// Blank Buttons  (Button — shows current selection in its TMP child)
///   nameBlankButton / ageBlankButton / cityBlankButton / likeBlankButton
///
/// Blank Labels
///   nameBlankLabel / ageBlankLabel / cityBlankLabel / likeBlankLabel
///
/// Option Container
///   optionsPanel / optionsContent / optionButtonPrefab
///
/// Keyboard row
///   keyboardPanel / keyboardInputDisplay / keyboardKeysParent / keyCapPrefab
///   keyboardDoneButton / keyboardBackspaceButton
///
/// Next Button
///   proceedButton
///
/// ★ Intro Sentence Labels (TMP)
///   introLine1 … introLine4  — assign 4 TMP labels for the animated intro sentences
///   introTypewriterSpeed     — seconds per character (default 0.04)
///   introLinePause           — extra pause between lines (default 0.3)
///
/// ★ SFX
///   sfxSource         — AudioSource to play clips on
///   sfxOptionPop      — AudioClip played when each option button pops in
///   sfxButtonClick    — AudioClip played when blank/option buttons are clicked
///   sfxKeyPress       — AudioClip for keyboard letter keys (defaults to sfxOptionPop)
///   sfxBackspace      — AudioClip for ⌫
///   sfxSubmit         — AudioClip for Done / Proceed
///
/// Preset Options
///   nameOptions / ageOptions / cityOptions / likeOptions
///
/// Optional Bridge
///   speechBridge
/// </summary>
public class IntroCard_BB1 : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("── Blank Buttons (show current selection) ──")]
    public Button nameBlankButton;
    public Button ageBlankButton;
    public Button cityBlankButton;
    public Button likeBlankButton;

    [Header("── Blank Labels ──────────────────────────────")]
    public TextMeshProUGUI nameBlankLabel;
    public TextMeshProUGUI ageBlankLabel;
    public TextMeshProUGUI cityBlankLabel;
    public TextMeshProUGUI likeBlankLabel;

    [Header("── Inline Options Panel ───────────────────────")]
    public GameObject optionsPanel;
    public Transform  optionsContent;
    public Button     optionButtonPrefab;

    [Header("── On-Canvas Keyboard Panel ───────────────────")]
    public GameObject       keyboardPanel;
    public TextMeshProUGUI  keyboardInputDisplay;
    public Transform        keyboardKeysParent;
    public Button           keyCapPrefab;
    public Button           keyboardDoneButton;
    public Button           keyboardBackspaceButton;

    [Header("── Next Button ─────────────────────────────────")]
    public Button proceedButton;

    [Header("★ Intro Typewriter Lines ────────────────────────")]
    [Tooltip("Assign 4 TMP labels that show the intro sentences one character at a time.")]
    public TextMeshProUGUI introLine1;
    public TextMeshProUGUI introLine2;
    public TextMeshProUGUI introLine3;
    public TextMeshProUGUI introLine4;

    [Tooltip("The full sentence for each intro line (shown via typewriter).")]
    public string introSentence1 = "Hi there! Let's learn about YOU! 🎉";
    public string introSentence2 = "Fill in the blanks below 👇";
    public string introSentence3 = "Tap a blank to choose your answer!";
    public string introSentence4 = "You can also type your own answer ✏️";

    [Tooltip("Seconds between each character in the typewriter effect.")]
    public float introTypewriterSpeed = 0.04f;
    [Tooltip("Extra pause (seconds) between lines.")]
    public float introLinePause = 0.3f;

    [Header("★ SFX ──────────────────────────────────────────")]
    public AudioSource sfxSource;
    [Tooltip("Played when each option button pops into view.")]
    public AudioClip sfxOptionPop;
    [Tooltip("Played when a blank button or option button is tapped.")]
    public AudioClip sfxButtonClick;
    [Tooltip("Played on every keyboard letter key press. Defaults to sfxOptionPop if null.")]
    public AudioClip sfxKeyPress;
    [Tooltip("Played when ⌫ is tapped.")]
    public AudioClip sfxBackspace;
    [Tooltip("Played when Done or Proceed is tapped.")]
    public AudioClip sfxSubmit;

    [Header("── Preset Options ──────────────────────────────")]
    public string[] nameOptions  = { "Joe", "Mary", "Sam", "Alex" };
    public string[] ageOptions   = { "four", "five", "six", "seven" };
    public string[] cityOptions  = { "Hyderabad", "Chennai", "Mumbai", "Delhi" };
    public string[] likeOptions  = { "chocolates", "playing", "reading", "dancing" };

    [Header("── Optional Bridge ─────────────────────────────")]
    public IntroCardToSpeech_BB1 speechBridge;

    // ── Runtime ────────────────────────────────────────────────────────────────

    private string _name = "", _age = "", _city = "", _like = "";

    private enum BlankField { None, Name, Age, City, Like }
    private BlankField _activeField = BlankField.None;

    private readonly List<Button> _spawnedOptions = new();
    private string _keyboardBuffer = "";

    private static readonly string[] KeyRows =
    {
        "QWERTYUIOP",
        "ASDFGHJKL",
        "ZXCVBNM"
    };

    private bool _started = false;

    // ── Unity Lifecycle ────────────────────────────────────────────────────────

    void Start()
    {
        _started = true;

        // Build keyboard while panel is active, then hide
        if (keyboardPanel != null) keyboardPanel.SetActive(true);

        if (keyboardKeysParent == null && keyboardPanel != null)
        {
            var keysGO = new GameObject("KeysParent",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            keysGO.transform.SetParent(keyboardPanel.transform, false);

            var rt  = keysGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var vlg = keysGO.GetComponent<VerticalLayoutGroup>();
            vlg.spacing                = 6;
            vlg.childAlignment         = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;
            vlg.padding                = new RectOffset(8, 8, 8, 8);

            var csf = keysGO.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            keyboardKeysParent = keysGO.transform;
        }

        if (keyboardKeysParent != null && keyboardKeysParent.childCount == 0)
            BuildKeyboard();

        HideOptions();
        HideKeyboard();

        if (proceedButton != null)
        {
            proceedButton.gameObject.SetActive(false);
            proceedButton.onClick.AddListener(OnProceedClicked);
            // Start at zero scale so it can pop in later
            proceedButton.transform.localScale = Vector3.zero;
        }

        // Wire blank buttons
        Wire(nameBlankButton, BlankField.Name);
        Wire(ageBlankButton,  BlankField.Age);
        Wire(cityBlankButton, BlankField.City);
        Wire(likeBlankButton, BlankField.Like);

        // Wire keyboard controls
        if (keyboardDoneButton      != null) keyboardDoneButton.onClick.AddListener(OnKeyboardDone);
        if (keyboardBackspaceButton != null) keyboardBackspaceButton.onClick.AddListener(OnKeyboardBackspace);

        // Reset blank labels
        SetLabel(nameBlankLabel, "");
        SetLabel(ageBlankLabel,  "");
        SetLabel(cityBlankLabel, "");
        SetLabel(likeBlankLabel, "");

        // Hide intro lines; we'll reveal them via typewriter
        SetIntroLineVisible(introLine1, false);
        SetIntroLineVisible(introLine2, false);
        SetIntroLineVisible(introLine3, false);
        SetIntroLineVisible(introLine4, false);

        // Start the typewriter sequence
        StartCoroutine(PlayIntroTypewriter());
    }

    void OnEnable()
    {
        if (!_started) return;

        _name           = "";
        _age            = "";
        _city           = "";
        _like           = "";
        _activeField    = BlankField.None;
        _keyboardBuffer = "";

        foreach (var b in _spawnedOptions)
            if (b != null) Destroy(b.gameObject);
        _spawnedOptions.Clear();

        HideOptions();
        HideKeyboard();

        SetLabel(nameBlankLabel, "");
        SetLabel(ageBlankLabel,  "");
        SetLabel(cityBlankLabel, "");
        SetLabel(likeBlankLabel, "");

        if (proceedButton != null)
            proceedButton.gameObject.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ★ INTRO TYPEWRITER
    // ═══════════════════════════════════════════════════════════════════════════

    void SetIntroLineVisible(TextMeshProUGUI lbl, bool visible)
    {
        if (lbl == null) return;
        lbl.gameObject.SetActive(visible);
        lbl.text = "";
    }

    IEnumerator PlayIntroTypewriter()
    {
        // Small initial delay so the panel finishes rendering
        yield return new WaitForSeconds(0.2f);

        var lines = new (TextMeshProUGUI lbl, string sentence)[]
        {
            (introLine1, introSentence1),
            (introLine2, introSentence2),
            (introLine3, introSentence3),
            (introLine4, introSentence4),
        };

        foreach (var (lbl, sentence) in lines)
        {
            if (lbl == null || string.IsNullOrEmpty(sentence)) continue;

            lbl.gameObject.SetActive(true);
            lbl.text = "";

            foreach (char ch in sentence)
            {
                lbl.text += ch;
                yield return new WaitForSeconds(introTypewriterSpeed);
            }

            yield return new WaitForSeconds(introLinePause);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ★ SFX HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    void PlayKeyPressSFX()   => PlaySFX(sfxKeyPress  != null ? sfxKeyPress  : sfxOptionPop);
    void PlayClickSFX()      => PlaySFX(sfxButtonClick);
    void PlayPopSFX()        => PlaySFX(sfxOptionPop);
    void PlayBackspaceSFX()  => PlaySFX(sfxBackspace);
    void PlaySubmitSFX()     => PlaySFX(sfxSubmit);

    // ═══════════════════════════════════════════════════════════════════════════
    // ★ PUNCH-SCALE POP TWEEN  (no external library needed)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Animates a RectTransform from scale 0 → overshoot → settle at 1.
    /// </summary>
    IEnumerator PopIn(Transform t, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        t.localScale = Vector3.zero;
        float duration   = 0.28f;
        float overshoot  = 1.18f;   // punch past 1 before settling
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / duration;

            // Simple spring curve: ease out expo then dip back
            float scale;
            if (p < 0.6f)
            {
                // expand to overshoot
                scale = Mathf.Lerp(0f, overshoot, p / 0.6f);
            }
            else
            {
                // settle from overshoot to 1
                scale = Mathf.Lerp(overshoot, 1f, (p - 0.6f) / 0.4f);
            }
            t.localScale = Vector3.one * scale;
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BLANK BUTTON WIRING
    // ═══════════════════════════════════════════════════════════════════════════

    void Wire(Button btn, BlankField field)
    {
        if (btn == null) return;
        btn.onClick.AddListener(() =>
        {
            PlayClickSFX();
            OnBlankTapped(field);
        });
    }

    void OnBlankTapped(BlankField field)
    {
        if (_activeField == field && optionsPanel != null && optionsPanel.activeSelf)
        {
            CloseAll();
            _activeField = BlankField.None;
            return;
        }

        _activeField = field;
        HideKeyboard();
        OpenOptions(field);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ★ INLINE OPTION BUTTONS WITH POP ANIMATION
    // ═══════════════════════════════════════════════════════════════════════════

    void OpenOptions(BlankField field)
    {
        // Destroy previous option buttons
        foreach (var b in _spawnedOptions)
            if (b != null) Destroy(b.gameObject);
        _spawnedOptions.Clear();

        string[] opts = field switch
        {
            BlankField.Name => nameOptions,
            BlankField.Age  => ageOptions,
            BlankField.City => cityOptions,
            _               => likeOptions
        };

        foreach (string opt in opts)
            SpawnOption(opt, isTypeIt: false);

        SpawnOption("✏  Type it", isTypeIt: true);

        if (optionsPanel != null) optionsPanel.SetActive(true);

        // Stagger pop-in for each button
        for (int i = 0; i < _spawnedOptions.Count; i++)
        {
            int idx = i;
            float delay = idx * 0.07f;  // 70 ms stagger
            StartCoroutine(PopInAndSFX(_spawnedOptions[idx].transform, delay));
        }
    }

    IEnumerator PopInAndSFX(Transform t, float delay)
    {
        // Start at zero before popping (avoid flash at full size)
        t.localScale = Vector3.zero;
        yield return StartCoroutine(PopIn(t, delay));
        PlayPopSFX();
    }

    void SpawnOption(string label, bool isTypeIt)
    {
        Button btn;

        if (optionButtonPrefab != null)
        {
            Transform parent = optionsContent != null ? optionsContent : optionsPanel?.transform;
            btn = Instantiate(optionButtonPrefab, parent);
        }
        else
        {
            Transform parent = optionsContent != null ? optionsContent : optionsPanel?.transform;

            var go = new GameObject("Opt_" + label,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 60);

            var img = go.GetComponent<Image>();
            img.color = isTypeIt
                ? new Color32(255, 220, 80, 255)
                : new Color32(255, 255, 255, 220);

            var txtGO = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(go.transform, false);
            var trt = txtGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            var tmp = txtGO.GetComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize  = 26;
            tmp.color     = Color.black;

            btn = go.GetComponent<Button>();
        }

        // Set label on prefab
        var tmpChild = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpChild != null) tmpChild.text = label;

        // Start hidden at zero scale; PopInAndSFX will animate it
        btn.transform.localScale = Vector3.zero;

        string captured = label;
        btn.onClick.AddListener(() =>
        {
            PlayClickSFX();
            if (isTypeIt)
            {
                HideOptions();
                OpenKeyboard();
            }
            else
            {
                SelectOption(captured);
            }
        });

        _spawnedOptions.Add(btn);
    }

    void HideOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    // ── Option Selection ───────────────────────────────────────────────────────

    void SelectOption(string value)
    {
        ApplySelection(value);
        CloseAll();
    }

    void ApplySelection(string value)
    {
        switch (_activeField)
        {
            case BlankField.Name: _name = value; SetLabel(nameBlankLabel, value); break;
            case BlankField.Age:  _age  = value; SetLabel(ageBlankLabel,  value); break;
            case BlankField.City: _city = value; SetLabel(cityBlankLabel, value); break;
            case BlankField.Like: _like = value; SetLabel(likeBlankLabel, value); break;
        }
        _activeField = BlankField.None;
        RefreshProceedButton();
    }

    void CloseAll()
    {
        HideOptions();
        HideKeyboard();
    }

    // ── On-Canvas Keyboard ─────────────────────────────────────────────────────

    void OpenKeyboard()
    {
        _keyboardBuffer = "";
        UpdateKeyboardDisplay();
        if (keyboardPanel != null) keyboardPanel.SetActive(true);
    }

    void HideKeyboard()
    {
        if (keyboardPanel != null) keyboardPanel.SetActive(false);
    }

    void OnKeyboardDone()
    {
        PlaySubmitSFX();
        string typed = _keyboardBuffer.Trim();
        if (!string.IsNullOrEmpty(typed))
            ApplySelection(typed);
        HideKeyboard();
    }

    void OnKeyboardBackspace()
    {
        PlayBackspaceSFX();
        if (_keyboardBuffer.Length > 0)
            _keyboardBuffer = _keyboardBuffer[..^1];
        UpdateKeyboardDisplay();
    }

    void OnKeyPress(string key)
    {
        PlayKeyPressSFX();
        _keyboardBuffer += key;
        UpdateKeyboardDisplay();
    }

    void UpdateKeyboardDisplay()
    {
        if (keyboardInputDisplay != null)
            keyboardInputDisplay.text = string.IsNullOrEmpty(_keyboardBuffer)
                ? "<color=#aaaaaa>Start typing…</color>"
                : _keyboardBuffer;
    }

    // ── Runtime Keyboard Builder ───────────────────────────────────────────────

    void BuildKeyboard()
    {
        if (keyboardKeysParent.GetComponent<VerticalLayoutGroup>() == null)
        {
            var vlg = keyboardKeysParent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = 6;
            vlg.childAlignment         = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;
            vlg.padding                = new RectOffset(8, 8, 8, 8);
        }

        foreach (string row in KeyRows)
        {
            var rowGO = new GameObject("Row_" + row[0],
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(keyboardKeysParent, false);

            var hlg = rowGO.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 6;
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            var csf = rowGO.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            foreach (char c in row)
            {
                string key = c.ToString();
                var keyBtn = CreateKey(key, rowGO.transform);
                keyBtn.onClick.AddListener(() => OnKeyPress(key));
            }
        }

        // Space bar row
        var spaceRowGO = new GameObject("Row_Space",
            typeof(RectTransform), typeof(HorizontalLayoutGroup));
        spaceRowGO.transform.SetParent(keyboardKeysParent, false);
        var spaceHLG = spaceRowGO.GetComponent<HorizontalLayoutGroup>();
        spaceHLG.childAlignment = TextAnchor.MiddleCenter;

        var spaceBtn = CreateKey("SPACE", spaceRowGO.transform, width: 220);
        spaceBtn.onClick.AddListener(() => OnKeyPress(" "));
    }

    Button CreateKey(string label, Transform parent, float width = 60, float height = 60)
    {
        var go = new GameObject("Key_" + label,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);

        var img = go.GetComponent<Image>();
        img.color = new Color32(230, 230, 230, 255);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = height;

        var txtGO = new GameObject("Label",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(go.transform, false);

        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        var tmp = txtGO.GetComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = label.Length > 2 ? 18 : 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.black;

        return go.GetComponent<Button>();
    }

    // ── Next / Proceed Button ──────────────────────────────────────────────────

    void RefreshProceedButton()
    {
        bool allFilled = !string.IsNullOrEmpty(_name)
                      && !string.IsNullOrEmpty(_age)
                      && !string.IsNullOrEmpty(_city)
                      && !string.IsNullOrEmpty(_like);

        if (proceedButton == null) return;

        if (allFilled && !proceedButton.gameObject.activeSelf)
        {
            proceedButton.gameObject.SetActive(true);
            // Pop it in with a celebratory bounce
            StartCoroutine(PopIn(proceedButton.transform));
            PlayPopSFX();
        }
        else if (!allFilled)
        {
            proceedButton.gameObject.SetActive(false);
        }
    }

    void OnProceedClicked()
    {
        PlaySubmitSFX();
        if (speechBridge != null)
            speechBridge.BuildAndLaunch(_name, _age, _city, _like);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public (string name, string age, string city, string like) GetFilledAnswers()
        => (_name, _age, _city, _like);

    // ── Helpers ────────────────────────────────────────────────────────────────

    void SetLabel(TextMeshProUGUI lbl, string value)
    {
        if (lbl == null) return;
        lbl.text  = string.IsNullOrEmpty(value) ? "________" : value;
        lbl.color = string.IsNullOrEmpty(value)
            ? new Color32(160, 160, 160, 255)
            : new Color32(30,  80, 200, 255);
    }
}