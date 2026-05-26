using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
//  WritingScreen_PanelB_MLDL_Game
//
//  4 question rows, revealed ONE AT A TIME.
//  Each row:
//    • Slides in from the side with a bounce.
//    • Question audio auto-plays.
//    • Student taps YES tick or NO tick.
//    • Feedback audio plays.
//    • Next row appears.
//  Row 4 (boy character): if student taps YES → sprite swaps to happy/blushing.
//  After all 4 rows answered → Submit button appears.
//  Submit → UnitFinished.
// ─────────────────────────────────────────────────────────────────────────────
public class WritingScreen_PanelB_MLDL_Game : MonoBehaviour, IUnitCompletable
{
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton          unitButton;
    public void OnUnitStart(SharedUnitPanelController p, SharedUnitButton b) { panel = p; unitButton = b; }

    // ─────────────────────────────────────────────────────────────────────
    //  DATA
    // ─────────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class QuestionRow
    {
        [Header("Content")]
        public string    foodName;          // "tomatoes" — shown in summary
        public string    questionText;      // "Do you like tomatoes?"
        public Sprite    subjectSprite;     // image on left of row
        public Color     rowColor;          // alternating blue / pink

        [Header("Audio")]
        public AudioClip questionAudio;     // plays on row reveal
        public AudioClip yesAudio;          // feedback after YES tap
        public AudioClip noAudio;           // feedback after NO tap

        [Header("Row 4 — Special YES sprite (leave null for other rows)")]
        public Sprite    happySprite;       // swapped in when YES tapped (row 4)
    }

    [Header("─── Question Rows (4) ─────────────────────────────")]
    public List<QuestionRow> rows = new List<QuestionRow>();

    // ─────────────────────────────────────────────────────────────────────
    //  SCENE REFERENCES  — Row Prefab / Container
    // ─────────────────────────────────────────────────────────────────────
    [Header("─── Row Prefab & Container ──────────────────────────")]
    [Tooltip("Prefab must have: background Image, subject Image, question TMP, " +
             "YES Button+Image, NO Button+Image. See docs below.")]
    public GameObject    rowPrefab;
    public Transform     rowContainer;     // Vertical Layout Group

    [Header("─── Tick Sprites ───────────────────────────────────")]
    public Sprite        tickEmpty;        // empty checkbox sprite
    public Sprite        tickFilled;       // ticked checkbox sprite

    [Header("─── Audio ──────────────────────────────────────────")]
    public AudioSource   dialogueAudio;
    public AudioSource   sfxAudio;

    [Header("─── SFX ────────────────────────────────────────────")]
    public AudioClip     sfxRowAppear;     // whoosh when row slides in
    public AudioClip     sfxTickYes;       // satisfying tick SFX for YES
    public AudioClip     sfxTickNo;        // tick SFX for NO
    public AudioClip     sfxSpecialYes;    // extra magic SFX for row 4 YES
    public AudioClip     sfxAllDone;       // fanfare when all rows answered
    public AudioClip     sfxSubmit;        // submit button tap

    [Header("─── Submit Button ──────────────────────────────────")]
    public Button        submitButton;
    public RectTransform submitButtonRect;

    [Header("─── Summary Panel ───────────────────────────────────")]
    public GameObject summaryPanel;   // drag WritingScreen_Summary root here (reuse same summary)

    [Header("─── Animation ──────────────────────────────────────")]
    public float popDuration = 0.4f;
    public float slideOffset = 500f;   // rows slide in from this X offset

    // ─────────────────────────────────────────────────────────────────────
    //  RUNTIME
    // ─────────────────────────────────────────────────────────────────────
    private int          currentRowIndex = 0;
    private bool         waitingForTick  = false;
    private List<string> yesFoods        = new List<string>();
    private List<string> noFoods         = new List<string>();

    // References to spawned row components (cached per row for sprite swap)
    private struct SpawnedRow
    {
        public RectTransform root;
        public Image         subjectImage;
        public Image         yesTickImage;
        public Image         noTickImage;
        public Button        yesBtn;
        public Button        noBtn;
    }
    private List<SpawnedRow> spawnedRows = new List<SpawnedRow>();

    private AnimationCurve bounceCurve = new AnimationCurve(
        new Keyframe(0f,    0f,   0f,  6f),
        new Keyframe(0.65f, 1.1f, 0f,  0f),
        new Keyframe(1f,    1f,   0f,  0f));

    // ─────────────────────────────────────────────────────────────────────
    void OnEnable()
    {
        ResetAll();
        Setup();
    }
    void OnDisable() { StopAllCoroutines(); SafeStop(dialogueAudio); }

    void ResetAll()
    {
        // Ensure summary is hidden so it starts fresh on replay
        if (summaryPanel != null) summaryPanel.SetActive(false);
    }

    void Setup()
    {
        currentRowIndex = 0;
        waitingForTick  = false;
        yesFoods        = new List<string>();
        noFoods         = new List<string>();
        spawnedRows.Clear();

        // Clear any leftover rows
        foreach (Transform child in rowContainer) Destroy(child.gameObject);

        if (submitButton != null)
        {
            submitButton.gameObject.SetActive(false);
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmit);
        }

        StartCoroutine(RevealNextRow());
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ROW REVEAL LOOP
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator RevealNextRow()
    {
        if (currentRowIndex >= rows.Count) yield break;
        var data = rows[currentRowIndex];

        // ── Spawn row from prefab ─────────────────────────────────────
        var go  = Instantiate(rowPrefab, rowContainer);
        var sr  = ParseRowPrefab(go, data);
        spawnedRows.Add(sr);

        // Start off-screen to the right (alternating: odd rows come from left)
        bool fromLeft = (currentRowIndex % 2 == 0);
        SetAnchoredX(sr.root, fromLeft ? -slideOffset : slideOffset);
        SetScale(sr.root, 0f);

        // Disable tick buttons until audio finishes
        SetRowInteractable(sr, false);

        // ── Slide in ──────────────────────────────────────────────────
        PlaySFX(sfxRowAppear);
        yield return StartCoroutine(SlideAndPop(sr.root,
            fromLeft ? -slideOffset : slideOffset, 0f, popDuration));

        yield return new WaitForSeconds(0.2f);

        // ── Question audio ────────────────────────────────────────────
        yield return StartCoroutine(PlayDialogue(data.questionAudio));

        yield return new WaitForSeconds(0.1f);

        // ── Enable tick buttons ───────────────────────────────────────
        SetRowInteractable(sr, true);

        // Wire buttons — capture index for closure
        int rowIdx  = currentRowIndex;
        var srCopy  = sr;
        var dataCopy = data;
        sr.yesBtn.onClick.AddListener(() => OnTick(rowIdx, true,  srCopy, dataCopy));
        sr.noBtn.onClick.AddListener(()  => OnTick(rowIdx, false, srCopy, dataCopy));

        // Wait for student tap
        waitingForTick = true;
        yield return new WaitWhile(() => waitingForTick);
    }

    void OnTick(int rowIdx, bool isYes, SpawnedRow sr, QuestionRow data)
    {
        if (!waitingForTick) return;
        waitingForTick = false;
        StartCoroutine(HandleTick(rowIdx, isYes, sr, data));
    }

    IEnumerator HandleTick(int rowIdx, bool isYes, SpawnedRow sr, QuestionRow data)
    {
        // Disable both buttons immediately
        SetRowInteractable(sr, false);

        // Fill the correct tick sprite
        if (isYes)
        {
            yesFoods.Add(string.IsNullOrEmpty(data.foodName) ? data.questionText : data.foodName);
            if (sr.yesTickImage != null) sr.yesTickImage.sprite = tickFilled;
            PlaySFX(sfxTickYes);
            StartCoroutine(TickBounce(sr.yesTickImage));

            // Row 4 special: swap sprite + extra SFX
            if (data.happySprite != null)
            {
                yield return new WaitForSeconds(0.15f);
                PlaySFX(sfxSpecialYes);
                yield return StartCoroutine(SpriteSwapBounce(sr.subjectImage, data.happySprite));
            }
        }
        else
        {
            noFoods.Add(string.IsNullOrEmpty(data.foodName) ? data.questionText : data.foodName);
            if (sr.noTickImage != null) sr.noTickImage.sprite = tickFilled;
            PlaySFX(sfxTickNo);
            StartCoroutine(TickBounce(sr.noTickImage));
        }

        yield return new WaitForSeconds(0.25f);

        // Feedback audio
        AudioClip feedback = isYes ? data.yesAudio : data.noAudio;
        yield return StartCoroutine(PlayDialogue(feedback));

        yield return new WaitForSeconds(0.2f);

        currentRowIndex++;

        // ── Hide answered row, then show next ────────────────────────────
        if (currentRowIndex < rows.Count)
        {
            // Slide the answered row out before spawning the next
            yield return StartCoroutine(SlideOut(sr.root));
            sr.root.gameObject.SetActive(false);

            yield return StartCoroutine(RevealNextRow());
        }
        else
        {
            // Slide last row out too, then wrap up
            yield return StartCoroutine(SlideOut(sr.root));
            sr.root.gameObject.SetActive(false);

            // All rows done
            PlaySFX(sfxAllDone);
            yield return new WaitForSeconds(0.4f);
            OpenSummary();
        }
    }

    void OpenSummary()
    {
        var summary = summaryPanel != null
                      ? summaryPanel.GetComponent<WritingScreen_Summary_MLDL_Game>()
                      : null;

        if (summary != null)
        {
            var capturedPanel      = panel;
            var capturedUnitButton = unitButton;

            // Activate FIRST so coroutines inside Initialise can run
            summaryPanel.SetActive(true);
            gameObject.SetActive(false);

            summary.Initialise(yesFoods, noFoods, () =>
            {
                // Next tapped → unit complete
                if (capturedPanel != null && capturedUnitButton != null)
                    capturedPanel.UnitFinished(capturedUnitButton);
            });
        }
        else
        {
            // Fallback: no summary wired up — just show submit button
            ShowSubmit();
        }
    }

    void ShowSubmit()
    {
        if (submitButton == null) return;
        SetScale(submitButtonRect, 0f);
        submitButton.gameObject.SetActive(true);
        StartCoroutine(ScalePop(submitButtonRect, popDuration));
    }

    void OnSubmit()
    {
        PlaySFX(sfxSubmit);
        if (panel != null && unitButton != null) panel.UnitFinished(unitButton);
        else gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PREFAB PARSING
    //  Expected prefab child naming (case-insensitive contains):
    //    "background" or "bg"  → row background Image (colour set here)
    //    "subject"             → food/character Image
    //    "questiontext"        → TMP label
    //    "yestick" or "yes"    → YES tick Image  (child of YES button or the button itself)
    //    "notick"  or "no"     → NO tick Image
    //    "yesbtn"              → YES Button component
    //    "nobtn"               → NO Button component
    //
    //  Simplest prefab layout that works:
    //    RowRoot (RectTransform)
    //      ├── Background (Image)          ← named "bg" or "background"
    //      ├── SubjectImage (Image)        ← named "subject"
    //      ├── QuestionText (TMP)          ← named "questiontext"
    //      ├── YesButton (Button+Image)    ← named "yesbtn"; child Image named "yestick"
    //      └── NoButton  (Button+Image)    ← named "nobtn";  child Image named "notick"
    // ─────────────────────────────────────────────────────────────────────
    SpawnedRow ParseRowPrefab(GameObject go, QuestionRow data)
    {
        var sr = new SpawnedRow();
        sr.root = go.GetComponent<RectTransform>();

        // ── Buttons first (needed by tick-image fallback below) ──────────
        foreach (var btn in go.GetComponentsInChildren<Button>(true))
        {
            string n = btn.gameObject.name.ToLower();
            if      (n.Contains("yes")) sr.yesBtn = btn;
            else if (n.Contains("no"))  sr.noBtn  = btn;
        }

        // ── Images ───────────────────────────────────────────────────────
        foreach (var img in go.GetComponentsInChildren<Image>(true))
        {
            string n = img.gameObject.name.ToLower();
            if (n.Contains("background") || n.Contains("bg"))
                img.color = data.rowColor;
            else if (n.Contains("subject"))
            { sr.subjectImage = img; img.sprite = data.subjectSprite; }
            // Match tick images STRICTLY first (before generic "yes"/"no" check)
            // so we don't accidentally grab the Button's own Image component.
            else if (n.Contains("yestick") || (n.Contains("yes") && n.Contains("tick")))
            { sr.yesTickImage = img; img.sprite = tickEmpty; }
            else if (n.Contains("notick")  || (n.Contains("no")  && n.Contains("tick")))
            { sr.noTickImage  = img; img.sprite = tickEmpty; }
        }

        // If tick images are still null it means the child Image is NOT named with
        // "tick" — fall back to finding the first Image *child* inside each button.
        if (sr.yesTickImage == null && sr.yesBtn != null)
        {
            var img = sr.yesBtn.GetComponentInChildren<Image>(true);
            if (img != null && img.gameObject != sr.yesBtn.gameObject)
            { sr.yesTickImage = img; img.sprite = tickEmpty; }
        }
        if (sr.noTickImage == null && sr.noBtn != null)
        {
            var img = sr.noBtn.GetComponentInChildren<Image>(true);
            if (img != null && img.gameObject != sr.noBtn.gameObject)
            { sr.noTickImage = img; img.sprite = tickEmpty; }
        }

        foreach (var tmp in go.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string n = tmp.gameObject.name.ToLower();
            if (n.Contains("question") || n.Contains("text"))
                tmp.text = data.questionText;
        }

        return sr;
    }

    void SetRowInteractable(SpawnedRow sr, bool v)
    {
        if (sr.yesBtn != null) sr.yesBtn.interactable = v;
        if (sr.noBtn  != null) sr.noBtn.interactable  = v;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ANIMATIONS
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator SlideAndPop(RectTransform rt, float fromX, float toX, float dur)
    {
        if (rt == null) yield break;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float s = bounceCurve.Evaluate(t);
            rt.localScale    = new Vector3(s, s, 1f);
            SetAnchoredX(rt, Mathf.Lerp(fromX, toX, bounceCurve.Evaluate(t)));
            yield return null;
        }
        rt.localScale = Vector3.one;
        SetAnchoredX(rt, toX);
    }

    IEnumerator SlideOut(RectTransform rt)
    {
        if (rt == null) yield break;
        float dur = 0.25f, elapsed = 0f;
        Vector2 startPos = rt.anchoredPosition;
        // Slide out to the opposite side from where it came in
        bool slideRight = startPos.x >= 0f;
        float targetX   = slideRight ? slideOffset : -slideOffset;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float ease = t * t;
            rt.anchoredPosition = new Vector2(Mathf.Lerp(startPos.x, targetX, ease), startPos.y);
            rt.localScale       = new Vector3(Mathf.Lerp(1f, 0f, t), Mathf.Lerp(1f, 0f, t), 1f);
            yield return null;
        }
        rt.localScale = Vector3.zero;
    }

    IEnumerator ScalePop(RectTransform rt, float dur)
    {
        if (rt == null) yield break;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = bounceCurve.Evaluate(Mathf.Clamp01(elapsed / dur));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator TickBounce(Image tickImg)
    {
        if (tickImg == null) yield break;
        var rt = tickImg.GetComponent<RectTransform>();
        if (rt == null) yield break;
        AnimationCurve c = new AnimationCurve(
            new Keyframe(0f,   1f,   0f, 8f),
            new Keyframe(0.4f, 1.3f, 0f, 0f),
            new Keyframe(1f,   1f,   0f, 0f));
        float dur = 0.35f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = c.Evaluate(Mathf.Clamp01(elapsed / dur));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator SpriteSwapBounce(Image img, Sprite newSprite)
    {
        if (img == null) yield break;
        var rt = img.GetComponent<RectTransform>();

        // Squish
        float dur = 0.1f, t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0.75f, t / dur);
            if (rt != null) rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        img.sprite = newSprite;

        // Pop back
        yield return StartCoroutine(ScalePop(rt, popDuration));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator PlayDialogue(AudioClip clip)
    {
        if (dialogueAudio == null || clip == null) yield break;
        dialogueAudio.Stop();
        dialogueAudio.clip = clip;
        dialogueAudio.Play();
        yield return new WaitUntil(() => dialogueAudio.isPlaying);
        while (dialogueAudio.isPlaying) yield return null;
    }

    void PlaySFX(AudioClip clip) { if (sfxAudio != null && clip != null) sfxAudio.PlayOneShot(clip); }
    void SetScale(RectTransform rt, float s) { if (rt != null) rt.localScale = new Vector3(s, s, 1f); }
    void SetAnchoredX(RectTransform rt, float x) { if (rt != null) rt.anchoredPosition = new Vector2(x, rt.anchoredPosition.y); }
    void SafeStop(AudioSource a) { if (a != null) a.Stop(); }

    public void OnBackClicked()
    {
        StopAllCoroutines();
        SafeStop(dialogueAudio);
        gameObject.SetActive(false);
        if (panel != null) panel.gameObject.SetActive(true);
    }
}