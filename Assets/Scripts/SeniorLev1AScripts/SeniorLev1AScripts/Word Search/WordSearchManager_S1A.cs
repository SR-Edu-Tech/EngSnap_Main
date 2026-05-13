using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class WordSearchManager_S1A : MonoBehaviour
{
    [Header("Grid")]
    [Tooltip("0 = auto-size from word list.")]
    public int rows = 0;
    public int cols = 0;
    public Transform     gridParent;
    public WordSearchCell_S1A cellPrefab;

    [Header("Pill Overlay")]
    [Tooltip("Image prefab with capsule sprite. RaycastTarget = FALSE.")]
    public Image         pillPrefab;
    [Tooltip("Empty RectTransform same size/position as grid. Sibling of grid.")]
    public RectTransform pillParent;

    [Header("Colors")]
    public Color dragColor = new Color(1f, 0.85f, 0.25f, 0.55f);
    public List<Color> wordFoundColors = new List<Color>()
    {
        new Color(0.36f, 0.72f, 1.00f, 0.55f),
        new Color(0.95f, 0.50f, 0.35f, 0.55f),
        new Color(0.42f, 0.86f, 0.62f, 0.55f),
        new Color(0.85f, 0.48f, 0.90f, 0.55f),
        new Color(1.00f, 0.85f, 0.30f, 0.55f),
        new Color(0.40f, 0.88f, 0.88f, 0.55f),
        new Color(1.00f, 0.60f, 0.75f, 0.55f),
        new Color(1.00f, 0.65f, 0.20f, 0.55f),
    };
    private int colorIndex;

    [Header("UI")]
    public Text currentWordText;

    [Header("Word List UI")]
    public Transform  wordListParent;
    public WordItemUI_S1A wordItemPrefab;
    public List<string> words = new List<string>() { "PILOT","LAWYER","TEACHER","STUDENT","CLASS" };

    [Header("Topic")]
    [Tooltip("Optional Text that shows the topic title above the grid.")]
    public Text topicTitleText;

    [Header("Grid Lines")]
    public bool  showGridLines    = true;
    public Color gridLineColor    = new Color(0.75f, 0.75f, 0.75f, 1f);
    public float gridLineThickness = 1f;

    [Header("Audio")]
    public AudioSource audioSource;

    [Tooltip("Short tick played each time a new cell is added during dragging.")]
    public AudioClip dragTickClip;

    [Tooltip("Played when the player successfully finds a word.")]
    public AudioClip correctWordClip;

    [Tooltip("Played when the drag ends with no word match.")]
    public AudioClip wrongWordClip;

    [Tooltip("Played when all words are found (finish fanfare).")]
    public AudioClip levelCompleteClip;

    [Tooltip("Played for each star pop in the finish panel.")]
    public AudioClip starPopClip;

    [Tooltip("Short pop sound played for each cell during the intro wave animation.")]
    public AudioClip cellPopClip;

    [Range(0f, 1f)] public float sfxVolume = 1f;
    public bool enableSFX = true;

    [Header("Intro Wave Animation")]
    [Tooltip("Delay between each diagonal wave step in seconds. Lower = faster sweep.")]
    public float waveStepDelay    = 0.04f;
    [Tooltip("Duration of each cell's pop-in scale animation in seconds.")]
    public float cellPopDuration  = 0.22f;
    [Tooltip("Overshoot scale peak during pop-in (e.g. 1.25 = 25% overshoot).")]
    public float cellPopOvershoot = 1.25f;
    [Tooltip("If true, touch/drag input is blocked until the wave finishes.")]
    public bool  blockInputDuringIntro = true;

    // Gates player input until intro finishes
    private bool introComplete = false;

    [Header("Drag Feedback")]
    [Tooltip("Scale of letter cells while they are part of the active drag selection.")]
    public float dragCellScale   = 1.20f;
    [Tooltip("Speed of the scale transition.")]
    public float cellScaleSpeed  = 14f;

    [Header("Word Found Feedback")]
    [Tooltip("Peak scale the WordItemUI_S1A row hits when a word is found.")]
    public float wordPopScale    = 1.30f;
    [Tooltip("Duration of the pop-out bounce in seconds.")]
    public float wordPopDuration = 0.35f;

    [Header("Finish Panel")]
    [Tooltip("Panel shown when all words are found. Disabled by default in scene.")]
    public GameObject finishPanel;

    [Tooltip("Label on the finish panel (e.g. 'Well Done!').")]
    public Text finishTitleText;

    [Tooltip("Score label on the finish panel (e.g. '5 / 5').")]
    public Text finishScoreText;

    [Tooltip("Star images on the finish panel (3 recommended).")]
    public Image[] finishStars;
    public Sprite starFilledSprite;
    public Sprite starEmptySprite;

    [Tooltip("'Play Again' button on the finish panel.")]
    public Button playAgainButton;

    [Tooltip("Optional confetti particle system.")]
    public ParticleSystem confettiPrefab;

    [Header("Finish Panel Tuning")]
    public float panelPopScale    = 1.05f;
    public float panelPopDuration = 0.22f;
    public float starPopDuration  = 0.18f;
    public float starPopDelay     = 0.09f;

    [HideInInspector] public System.Action OnContinueClicked;

    private WordSearchCell_S1A[,] grid;
    private List<WordSearchCell_S1A> selection  = new List<WordSearchCell_S1A>();
    private Vector2Int selDir;
    private bool       isDragging;

    private Dictionary<string, WordItemUI_S1A> wordUIMap  = new Dictionary<string, WordItemUI_S1A>();
    private HashSet<string>                foundWords = new HashSet<string>();
    private Image dragPill;

    private HashSet<WordSearchCell_S1A> scaledCells = new HashSet<WordSearchCell_S1A>();

    private Camera uiCamera;

    private static readonly Vector2Int[] allDirs =
    {
        new Vector2Int( 0,  1), new Vector2Int( 0, -1),
        new Vector2Int( 1,  0), new Vector2Int(-1,  0),
        new Vector2Int( 1,  1), new Vector2Int(-1,  1),
        new Vector2Int( 1, -1), new Vector2Int(-1, -1),
    };

    void Start()
    {
        // Detect UI camera
        Canvas c = GetComponentInParent<Canvas>();
        if (c == null) c = FindObjectOfType<Canvas>();
        uiCamera = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
                   ? c.worldCamera : null;

        CreateWordListUI();
        BuildGrid();

        // Wire Play Again button
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgain);

        // Hide finish panel at start
        if (finishPanel != null)
            finishPanel.SetActive(false);

        // Launch the intro wave
        introComplete = false;
        PopupCoroutineRunner.Instance.StartCoroutine(PlayIntroWave());
    }

    // Re-run the intro wave when re-activated
    void OnEnable()
    {
        // Grid is null before Start has run — Start will launch the wave instead.
        if (grid == null) return;

        introComplete = false;
        PopupCoroutineRunner.Instance.StartCoroutine(PlayIntroWave());
    }

    // Load new topic
    public void LoadTopic(WordTopicData_S1A topic)
    {
        if (topic == null) return;

        words = new List<string>(topic.words);

        if (topicTitleText != null)
            topicTitleText.text = "Find the " + topic.topicName.ToLower() + ".";

        foundWords.Clear();
        colorIndex = 0;

        foreach (Transform t in wordListParent) Destroy(t.gameObject);
        wordUIMap.Clear();

        rows = 0; cols = 0;
        CreateWordListUI();
        BuildGrid();

        if (finishPanel != null) finishPanel.SetActive(false);

        // Re-run the intro wave for the new topic
        introComplete = false;
        PopupCoroutineRunner.Instance.StartCoroutine(PlayIntroWave());
    }

    // Update selection
    void Update()
    {
        // Animate cell scales every frame
        AnimateCellScales();

        if (!isDragging) return;

        Vector2 screenPos;
#if UNITY_EDITOR || UNITY_STANDALONE
        screenPos = Input.mousePosition;
#else
        if (Input.touchCount == 0) return;
        screenPos = Input.GetTouch(0).position;
#endif

        WordSearchCell_S1A hit = GetCellAtScreenPos(screenPos);
        if (hit != null) ContinueSelection(hit);
    }

    void CreateWordListUI()
    {
        foreach (string word in words)
        {
            WordItemUI_S1A item = Instantiate(wordItemPrefab, wordListParent);
            item.Init(word);
            wordUIMap[word] = item;
        }
    }

    void BuildGrid()
    {
        ClearAllPills();

        if (rows <= 0 || cols <= 0)
        {
            int longest = 0, total = 0;
            foreach (string w in words) { if (w.Length > longest) longest = w.Length; total += w.Length; }
            int sz = Mathf.Max(longest + 2, Mathf.CeilToInt(Mathf.Sqrt(total * 3f)));
            sz = Mathf.Clamp(sz, 1, 9);
            rows = cols = sz;
            Debug.Log($"[WordSearch] Auto grid: {rows}x{cols}");
        }
        else
        {
            rows = Mathf.Clamp(rows, 1, 9);
            cols = Mathf.Clamp(cols, 1, 9);
        }

        if (pillParent != null && pillParent.parent == gridParent)
        {
            pillParent.SetParent(gridParent.parent, true);
            Debug.LogWarning("[WordSearch] PillLayer was a child of gridParent — moved it to be a sibling.");
        }

        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
            Transform t = gridParent.GetChild(i);
            if (t.GetComponent<WordSearchCell_S1A>() != null) Destroy(t.gameObject);
        }

        char[,] cg = new char[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c2 = 0; c2 < cols; c2++)
                cg[r, c2] = ' ';

        List<string> sorted = new List<string>(words);
        sorted.Sort((a, b) => b.Length.CompareTo(a.Length));
        foreach (string w in sorted)
            if (!PlaceWord(w, cg))
                Debug.LogError($"[WordSearch] Could not place '{w}' — grid too small!");

        for (int r = 0; r < rows; r++)
            for (int c2 = 0; c2 < cols; c2++)
                if (cg[r, c2] == ' ')
                    cg[r, c2] = (char)('A' + Random.Range(0, 26));

        grid = new WordSearchCell_S1A[rows, cols];
        for (int r = 0; r < rows; r++)
            for (int c2 = 0; c2 < cols; c2++)
            {
                WordSearchCell_S1A cell = Instantiate(cellPrefab, gridParent);
                cell.Init(r, c2, cg[r, c2], this);
                grid[r, c2] = cell;

                // Hide the cell immediately — intro wave will reveal them
                cell.transform.localScale = Vector3.zero;

                if (showGridLines && cell.background != null)
                {
                    // Add outline for grid border lines
                    Outline outline = cell.background.GetComponent<Outline>();
                    if (outline == null) outline = cell.background.gameObject.AddComponent<Outline>();
                    outline.effectColor    = gridLineColor;
                    outline.effectDistance = new Vector2(gridLineThickness, -gridLineThickness);
                    outline.useGraphicAlpha = false;
                }
            }
    }

    // Place word with randomized directions to ensure fair distribution.
    bool PlaceWord(string word, char[,] cg)
    {
        // Shuffle directions for fair random selection
        Vector2Int[] shuffledDirs = (Vector2Int[])allDirs.Clone();
        for (int i = shuffledDirs.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int tmp = shuffledDirs[i];
            shuffledDirs[i] = shuffledDirs[j];
            shuffledDirs[j] = tmp;
        }

        // Build a shuffled list of all start positions
        List<Vector2Int> positions = new List<Vector2Int>(rows * cols);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                positions.Add(new Vector2Int(r, c));
        for (int i = positions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2Int tmp = positions[i];
            positions[i] = positions[j];
            positions[j] = tmp;
        }

        // Try every direction × every start position
        foreach (Vector2Int dir in shuffledDirs)
        {
            foreach (Vector2Int pos in positions)
            {
                int sr = pos.x, sc = pos.y;
                bool ok = true;
                for (int i = 0; i < word.Length && ok; i++)
                {
                    int nr = sr + dir.x * i, nc = sc + dir.y * i;
                    if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) { ok = false; break; }
                    // Allow placement if cell is empty OR already has the correct letter
                    // (crossword-style intersection with a previously placed word)
                    if (cg[nr, nc] != ' ' && cg[nr, nc] != word[i]) ok = false;
                }
                if (ok)
                {
                    for (int i = 0; i < word.Length; i++)
                        cg[sr + dir.x * i, sc + dir.y * i] = word[i];
                    return true;
                }
            }
        }
        return false;
    }

    // Find word cells
    List<WordSearchCell_S1A> FindWordCells(string word)
    {
        if (grid == null) return null;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                foreach (Vector2Int dir in allDirs)
                {
                    List<WordSearchCell_S1A> cells = new List<WordSearchCell_S1A>();
                    bool ok = true;
                    for (int i = 0; i < word.Length; i++)
                    {
                        int nr = r + dir.x * i, nc = c + dir.y * i;
                        if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) { ok = false; break; }
                        if (grid[nr, nc].letter != word[i]) { ok = false; break; }
                        cells.Add(grid[nr, nc]);
                    }
                    if (ok) return cells;
                }
        return null;
    }

    public void StartSelection(WordSearchCell_S1A cell)
    {
        if (blockInputDuringIntro && !introComplete) return;
        ClearDragPill();
        // Reset scale on any previously scaled cells
        ResetAllCellScales();
        selection.Clear();
        selDir     = Vector2Int.zero;
        isDragging = true;

        selection.Add(cell);
        SetCellDragScale(cell, true);
        dragPill = SpawnPill(selection, dragColor);
        UpdateCurrentWord();

        PlaySfx(dragTickClip);
    }

    public void ContinueSelection(WordSearchCell_S1A cell)
    {
        if (!isDragging || selection.Count == 0) return;

        // Shrink if dragging backward
        if (selection.Count > 1 && selection[selection.Count - 2] == cell)
        {
            WordSearchCell_S1A removed = selection[selection.Count - 1];
            selection.RemoveAt(selection.Count - 1);
            SetCellDragScale(removed, false);
            UpdateDragPill();
            UpdateCurrentWord();
            return;
        }

        if (selection[selection.Count - 1] == cell) return;
        if (selection.Contains(cell)) return;

        WordSearchCell_S1A first = selection[0];
        WordSearchCell_S1A last  = selection[selection.Count - 1];

        int dr = cell.row - last.row;
        int dc = cell.col - last.col;

        if (Mathf.Abs(dr) > 1 || Mathf.Abs(dc) > 1 || (dr == 0 && dc == 0)) return;

        Vector2Int newDir = new Vector2Int(dr, dc);
        if      (selDir == Vector2Int.zero) selDir = newDir;
        else if (newDir != selDir)          return;

        int steps = selection.Count;
        if (cell.row - first.row != selDir.x * steps) return;
        if (cell.col - first.col != selDir.y * steps) return;

        selection.Add(cell);
        SetCellDragScale(cell, true);
        UpdateDragPill();
        UpdateCurrentWord();

        // Tick sound for each new cell entered
        PlaySfx(dragTickClip);
    }

    public void EndSelection()
    {
        isDragging = false;
        ClearDragPill();

        // Reset drag scale on all selected cells
        foreach (var c in selection) SetCellDragScale(c, false);

        string formed   = GetSelectedWord();
        string reversed = ReverseString(formed);

        string matched = null;
        if      (words.Contains(formed)   && !foundWords.Contains(formed))   matched = formed;
        else if (words.Contains(reversed) && !foundWords.Contains(reversed)) matched = reversed;

        if (matched != null)
        {
            foundWords.Add(matched);

            Color pill = GetNextColor();
            SpawnPill(selection, pill);
            foreach (var c in selection) c.isLocked = true;

            if (wordUIMap.ContainsKey(matched))
            {
                wordUIMap[matched].MarkFound();
                // Pop-out animation on the word row
                PopupCoroutineRunner.Instance.StartCoroutine(
                    AnimateWordPop(wordUIMap[matched].transform));
            }

            PlaySfx(correctWordClip);

            // Check completion
            if (foundWords.Count >= words.Count)
            {
                PopupCoroutineRunner.Instance.StartCoroutine(ShowFinishPanelDelayed(0.6f));
            }
        }
        else
        {
            PlaySfx(wrongWordClip);
        }

        selection.Clear();
        selDir = Vector2Int.zero;
        if (currentWordText != null) currentWordText.text = "";
    }

    void SetCellDragScale(WordSearchCell_S1A cell, bool enlarged)
    {
        if (enlarged) scaledCells.Add(cell);
        else          scaledCells.Remove(cell);
    }

    void ResetAllCellScales()
    {
        scaledCells.Clear();
    }

    void AnimateCellScales()
    {
        // Animate cell scales
        if (!introComplete) return;
        if (grid == null) return;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (grid[r, c] == null) continue;
                Transform t = grid[r, c].transform;
                float target = scaledCells.Contains(grid[r, c]) ? dragCellScale : 1f;
                float current = t.localScale.x;
                float next = Mathf.Lerp(current, target, Time.deltaTime * cellScaleSpeed);
                t.localScale = Vector3.one * next;
            }
    }

    IEnumerator AnimateWordPop(Transform target)
    {
        if (target == null) yield break;

        float half = wordPopDuration * 0.5f;
        Vector3 originalScale = Vector3.one;

        // Scale up
        float e = 0f;
        while (e < half)
        {
            e += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(e / half);
            target.localScale = Vector3.LerpUnclamped(originalScale, Vector3.one * wordPopScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        // Scale back
        e = 0f;
        while (e < half)
        {
            e += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(e / half);
            target.localScale = Vector3.LerpUnclamped(Vector3.one * wordPopScale, originalScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        target.localScale = originalScale;
    }

    IEnumerator ShowFinishPanelDelayed(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ShowFinishPanel();
    }

    void ShowFinishPanel()
    {
        if (finishPanel == null)
        {
            Debug.LogWarning("[WordSearch] No finishPanel assigned — using legacy achievement popup.");
            return;
        }

        int total   = words.Count;
        int correct = foundWords.Count;
        float ratio = total > 0 ? (float)correct / total : 0f;
        int stars   = ratio >= 0.9f ? 3 : ratio >= 0.6f ? 2 : correct > 0 ? 1 : 0;

        if (finishTitleText != null) finishTitleText.text = "Well Done!";
        if (finishScoreText != null) finishScoreText.text = $"{correct} / {total}";

        HideAllFinishStars();

        if (finishStars != null)
            for (int i = 0; i < finishStars.Length; i++)
                if (finishStars[i] != null)
                    finishStars[i].sprite = (i < stars) ? starFilledSprite : starEmptySprite;

        if (playAgainButton != null)
            playAgainButton.gameObject.SetActive(false);

        finishPanel.SetActive(true);
        PlaySfx(levelCompleteClip);

        PopupCoroutineRunner.Instance.StartCoroutine(FinishPanelSequence(stars));
    }

    IEnumerator FinishPanelSequence(int starCount)
    {
        if (finishPanel == null) yield break;

        finishPanel.SetActive(true);
        RectTransform panelRt = finishPanel.GetComponent<RectTransform>();
        if (panelRt != null) panelRt.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        if (panelRt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);

        // Panel pop-in
        if (panelRt != null)
        {
            panelRt.localScale = Vector3.zero;
            float elapsed = 0f, dur = Mathf.Max(0.01f, panelPopDuration);
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float overshoot = Mathf.Lerp(0f, panelPopScale - 1f, Mathf.SmoothStep(0f, 1f, t));
                panelRt.localScale = Vector3.one * (1f + overshoot * (1f - Mathf.Pow(1 - t, 2f)));
                yield return null;
            }
            panelRt.localScale = Vector3.one;
        }

        yield return new WaitForSecondsRealtime(0.06f);

        // Star pop animations
        if (finishStars != null)
        {
            Canvas.ForceUpdateCanvases();
            if (panelRt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(panelRt);

            for (int i = 0; i < finishStars.Length; i++)
            {
                var star = finishStars[i];
                if (star == null) continue;

                SetStarAlpha(star, 1f);

                float durStar = Mathf.Max(0.01f, starPopDuration);
                float half    = durStar * 0.5f;
                float e       = 0f;

                while (e < half)
                {
                    e += Time.unscaledDeltaTime;
                    float tt = Mathf.Clamp01(e / half);
                    star.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one * 1.25f, Mathf.SmoothStep(0f, 1f, tt));
                    yield return null;
                }
                e = 0f;
                while (e < half)
                {
                    e += Time.unscaledDeltaTime;
                    float tt = Mathf.Clamp01(e / half);
                    star.transform.localScale = Vector3.LerpUnclamped(Vector3.one * 1.25f, Vector3.one, Mathf.SmoothStep(0f, 1f, tt));
                    yield return null;
                }
                star.transform.localScale = Vector3.one;

                PlaySfx(starPopClip);
                yield return new WaitForSecondsRealtime(starPopDelay);
            }
        }

        // Confetti on 3 stars
        if (starCount >= 3 && confettiPrefab != null)
        {
            Canvas canvas = finishPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var ps = Instantiate(confettiPrefab, canvas.transform);
                ps.transform.SetAsLastSibling();
                ps.Play();
                Destroy(ps.gameObject, 5f);
            }
        }

        if (playAgainButton != null)
            playAgainButton.gameObject.SetActive(true);
    }

    void HideAllFinishStars()
    {
        if (finishStars == null) return;
        foreach (var s in finishStars)
        {
            if (s == null) continue;
            s.gameObject.SetActive(true);
            s.transform.localScale = Vector3.zero;
            SetStarAlpha(s, 0f);
        }
    }

    public void OnPlayAgain()
    {
        foundWords.Clear();
        colorIndex = 0;

        // Rebuild word list UI
        foreach (Transform t in wordListParent) Destroy(t.gameObject);
        wordUIMap.Clear();

        rows = 0; cols = 0;
        CreateWordListUI();
        BuildGrid();

        if (finishPanel != null) finishPanel.SetActive(false);

        // Re-run the intro wave
        introComplete = false;
        PopupCoroutineRunner.Instance.StartCoroutine(PlayIntroWave());
    }

    // Play intro wave

    IEnumerator PlayIntroWave()
    {
        if (grid == null) { introComplete = true; yield break; }

        // Reset all cells to hidden so re-runs always start from zero scale
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] != null)
                    grid[r, c].transform.localScale = Vector3.zero;

        // Wait two frames — GridLayoutGroup needs at least one frame to settle
        // positions after Instantiate. Without this, world positions are (0,0).
        yield return null;
        yield return null;

        // Force layout one more time to be safe
        if (gridParent is RectTransform grt)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(grt);
        }

        int maxDiag = (rows - 1) + (cols - 1);

        for (int d = 0; d <= maxDiag; d++)
        {
            bool anyCellOnDiag = false;
            for (int r = 0; r < rows; r++)
            {
                int c = d - r;
                if (c < 0 || c >= cols) continue;
                if (grid[r, c] == null) continue;
                anyCellOnDiag = true;

                // Use the persistent runner so PopInCell survives panel toggles
                PopupCoroutineRunner.Instance.StartCoroutine(PopInCell(grid[r, c].transform));
            }

            if (anyCellOnDiag)
                PlaySfxPitched(cellPopClip, 1f + d * 0.015f);

            yield return new WaitForSeconds(waveStepDelay);
        }

        // Wait for last cells to finish popping
        yield return new WaitForSeconds(cellPopDuration);

        introComplete = true;
    }

    // Scales a single cell from zero → overshoot → 1 with a spring feel.
    IEnumerator PopInCell(Transform t)
    {
        if (t == null) yield break;

        float elapsed = 0f;
        float half    = cellPopDuration * 0.55f;
        float second  = cellPopDuration * 0.45f;

        // Phase 1: 0 → overshoot
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / half);
            t.localScale = Vector3.one * Mathf.LerpUnclamped(0f, cellPopOvershoot, EaseOutQuart(p));
            yield return null;
        }

        // Phase 2: overshoot → 1
        elapsed = 0f;
        while (elapsed < second)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / second);
            t.localScale = Vector3.one * Mathf.LerpUnclamped(cellPopOvershoot, 1f, EaseOutQuart(p));
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    // Smooth easing — fast start, deceleration
    static float EaseOutQuart(float t) { float f = 1f - t; return 1f - f * f * f * f; }

    // Plays a clip with a custom pitch (used for the rising wave pitch effect)
    void PlaySfxPitched(AudioClip clip, float pitch)
    {
        if (!enableSFX || clip == null) return;
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        float prevPitch   = audioSource.pitch;
        audioSource.pitch = Mathf.Clamp(pitch, 0.5f, 3f);
        audioSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume));
        audioSource.pitch = prevPitch;
    }

    WordSearchCell_S1A GetCellAtScreenPos(Vector2 screenPos)
    {
        if (grid == null) return null;

        float bestDist = float.MaxValue;
        WordSearchCell_S1A bestCell = null;

        int rMin = 0, rMax = rows - 1, cMin = 0, cMax = cols - 1;
        if (selection.Count > 0)
        {
            WordSearchCell_S1A last = selection[selection.Count - 1];
            rMin = Mathf.Max(0, last.row - 2);
            rMax = Mathf.Min(rows - 1, last.row + 2);
            cMin = Mathf.Max(0, last.col - 2);
            cMax = Mathf.Min(cols - 1, last.col + 2);
        }

        for (int r = rMin; r <= rMax; r++)
            for (int c = cMin; c <= cMax; c++)
            {
                WordSearchCell_S1A cell = grid[r, c];
                Vector2 center = GetCellScreenCenter(cell);
                float cellSize = GetCellScreenSize(cell);
                float dist = Vector2.Distance(screenPos, center);

                if (dist < cellSize * 0.55f && dist < bestDist)
                {
                    bestDist = dist;
                    bestCell = cell;
                }
            }
        return bestCell;
    }

    Vector2 GetCellScreenCenter(WordSearchCell_S1A cell)
    {
        Vector3[] corners = new Vector3[4];
        cell.GetComponent<RectTransform>().GetWorldCorners(corners);
        Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
        return RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
    }

    float GetCellScreenSize(WordSearchCell_S1A cell)
    {
        Vector3[] corners = new Vector3[4];
        cell.GetComponent<RectTransform>().GetWorldCorners(corners);
        Vector2 bl = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[0]);
        Vector2 br = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[3]);
        return Vector2.Distance(bl, br);
    }

    Image SpawnPill(List<WordSearchCell_S1A> cells, Color color)
    {
        if (cells == null || cells.Count == 0 || pillPrefab == null || pillParent == null)
            return null;

        WordSearchCell_S1A first = cells[0];
        WordSearchCell_S1A last  = cells[cells.Count - 1];

        Vector3 firstWorld = GetCellWorldCenter(first);
        Vector3 lastWorld  = GetCellWorldCenter(last);

        Vector2 firstLocal = WorldToLocal(firstWorld);
        Vector2 lastLocal  = WorldToLocal(lastWorld);

        Vector2 center   = (firstLocal + lastLocal) * 0.5f;
        float cellLocal  = WorldSizeToLocal(GetCellWorldSize(first));
        float dist       = Vector2.Distance(firstLocal, lastLocal);
        float pillWidth  = cells.Count == 1 ? cellLocal : dist + cellLocal;
        float pillHeight = cellLocal * 0.80f;

        float angle = cells.Count > 1
            ? Mathf.Atan2((lastLocal - firstLocal).y, (lastLocal - firstLocal).x) * Mathf.Rad2Deg
            : 0f;

        Image pill = Instantiate(pillPrefab, pillParent);
        pill.type = Image.Type.Sliced;
        RectTransform rt = pill.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one * 0.5f;
        rt.anchoredPosition = center;
        rt.sizeDelta        = new Vector2(pillWidth, pillHeight);
        rt.localEulerAngles = new Vector3(0, 0, angle);
        pill.color          = color;
        pill.raycastTarget  = false;
        return pill;
    }

    void UpdateDragPill() { ClearDragPill(); dragPill = SpawnPill(selection, dragColor); }

    void ClearDragPill()
    {
        if (dragPill != null) { Destroy(dragPill.gameObject); dragPill = null; }
    }

    void ClearAllPills()
    {
        dragPill = null;
        if (pillParent == null) return;
        for (int i = pillParent.childCount - 1; i >= 0; i--)
            Destroy(pillParent.GetChild(i).gameObject);
    }

    Vector3 GetCellWorldCenter(WordSearchCell_S1A cell)
    {
        Vector3[] c = new Vector3[4];
        cell.GetComponent<RectTransform>().GetWorldCorners(c);
        return (c[0] + c[2]) * 0.5f;
    }

    float GetCellWorldSize(WordSearchCell_S1A cell)
    {
        Vector3[] c = new Vector3[4];
        cell.GetComponent<RectTransform>().GetWorldCorners(c);
        float w = Vector2.Distance(c[0], c[3]);
        float h = Vector2.Distance(c[0], c[1]);
        return Mathf.Min(w, h);
    }

    float WorldSizeToLocal(float worldSize)
    {
        float s = pillParent.lossyScale.x;
        return s > 0.0001f ? worldSize / s : worldSize;
    }

    Vector2 WorldToLocal(Vector3 worldPt)
    {
        return pillParent.InverseTransformPoint(worldPt);
    }

    void UpdateCurrentWord()
    {
        if (currentWordText != null) currentWordText.text = GetSelectedWord();
    }

    string GetSelectedWord()
    {
        var sb = new StringBuilder();
        foreach (var c in selection) sb.Append(c.letter);
        return sb.ToString();
    }

    string ReverseString(string s)
    {
        char[] a = s.ToCharArray(); System.Array.Reverse(a); return new string(a);
    }

    Color GetNextColor()
    {
        if (wordFoundColors == null || wordFoundColors.Count == 0)
            return new Color(0.36f, 0.72f, 1f, 0.55f);
        Color c = wordFoundColors[colorIndex % wordFoundColors.Count];
        colorIndex++;
        return c;
    }

    void SetStarAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color; c.a = alpha; img.color = c;
    }

    void PlaySfx(AudioClip clip)
    {
        if (!enableSFX || clip == null) return;
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume));
    }

    //  Persistent coroutine runner
    public class PopupCoroutineRunner : MonoBehaviour
    {
        static PopupCoroutineRunner _inst;
        public static PopupCoroutineRunner Instance
        {
            get
            {
                if (_inst == null)
                {
                    var go = new GameObject("PopupCoroutineRunner");
                    DontDestroyOnLoad(go);
                    _inst = go.AddComponent<PopupCoroutineRunner>();
                }
                return _inst;
            }
        }
    }
}

