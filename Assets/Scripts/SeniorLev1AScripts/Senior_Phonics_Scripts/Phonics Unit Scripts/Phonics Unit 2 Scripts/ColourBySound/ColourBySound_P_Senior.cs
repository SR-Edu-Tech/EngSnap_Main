using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public enum VowelCategory
{
    ShortA,
    ShortE,
    ShortI,
    ShortO,
    ShortU,
    LongA,
    LongE,
    LongI,
    LongO,
    LongU
}

[System.Serializable]
public class GridCell
{
    [Tooltip("The word text displayed in the cell.")]
    public string word;

    [Tooltip("The correct vowel sound category for this word.")]
    public VowelCategory correctCategory;

    [Tooltip("The pronunciation audio clip of the word.")]
    public AudioClip wordAudio;

    [Header("UI Assignments")]
    [Tooltip("The button component of the cell.")]
    public Button button;

    [Tooltip("The background image of the cell to fill with color.")]
    public Image cellImage;

    [Tooltip("The word text field inside the cell.")]
    public TextMeshProUGUI wordText;

    [HideInInspector]
    public bool isCompleted = false;
    
    [HideInInspector]
    public Color originalColor;
}

[System.Serializable]
public class PaletteButton
{
    [Tooltip("Vowel category this button represents.")]
    public VowelCategory vowelCategory;

    [Tooltip("The button component in the palette.")]
    public Button button;

    [Tooltip("The color to fill the cell with when this category is selected.")]
    public Color buttonColor = Color.white;

    [Tooltip("The text label component of the palette button.")]
    public TextMeshProUGUI buttonText;

    [Tooltip("The image component of the palette button.")]
    public Image buttonImage;
}

public class ColourBySound_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Grid Setup (5x4 Grid)")]
    [Tooltip("The 20 cells in the 5x4 grid. Order should match row-by-row layout.")]
    public GridCell[] gridCells = new GridCell[20];

    [Header("Palette Setup (10 Buttons)")]
    [Tooltip("The 10 color buttons representing short and long vowels.")]
    public PaletteButton[] paletteButtons = new PaletteButton[10];

    [Header("Editor Setup Links (For Auto-Assign Menu)")]
    [Tooltip("The Grid_Panel GameObject containing the 20 word cells as its children.")]
    public RectTransform gridPanel;

    [Tooltip("The 'Long' Palette Panel containing the 5 long vowel buttons.")]
    public RectTransform longPalettePanel;

    [Tooltip("The 'Short' Palette Panel containing the 5 short vowel buttons.")]
    public RectTransform shortPalettePanel;

    [Header("Audio Settings")]
    [Tooltip("Audio source for playing the word pronunciation and mascot dialogue.")]
    public AudioSource mascotAudioSource;

    [Tooltip("Audio source for playing correct/wrong/win sound effects.")]
    public AudioSource sfxAudioSource;

    [Tooltip("Introduction voice clip played on start.")]
    public AudioClip introClip;

    [Tooltip("Sound effect played when a correct color is chosen.")]
    public AudioClip correctChime;

    [Tooltip("Sound effect played when a wrong color is chosen (optional).")]
    public AudioClip wrongSFX;

    [Tooltip("Sound effect played when the entire grid is colored.")]
    public AudioClip winSFX;

    [Header("UI Components & Reveal")]
    [Tooltip("Optional text title of the activity.")]
    public TextMeshProUGUI titleText;

    [Tooltip("Button to proceed after game completion.")]
    public GameObject nextButton;

    [Tooltip("Optional picture overlay to fade in upon completion (representing the hidden picture reveal).")]
    public CanvasGroup hiddenPictureOverlay;

    [Header("Events")]
    [Tooltip("Unity Event triggered upon completion.")]
    public UnityEvent onComplete;

    // Runtime state
    private GridCell selectedCell = null;
    private bool canPlay = false;

    private void OnEnable()
    {
        ResetGame();
        StartCoroutine(IntroSequence());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
    }

    private void ResetGame()
    {
        selectedCell = null;
        canPlay = false;

        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }

        if (hiddenPictureOverlay != null)
        {
            hiddenPictureOverlay.alpha = 0f;
            hiddenPictureOverlay.gameObject.SetActive(false);
        }

        // Initialize grid cells
        foreach (var cell in gridCells)
        {
            if (cell == null) continue;

            // Runtime fallback to resolve components dynamically if Inspector layout was stale/unassigned
            if (cell.button != null)
            {
                if (cell.cellImage == null)
                {
                    cell.cellImage = cell.button.GetComponent<Image>();
                }
                if (cell.wordText == null)
                {
                    cell.wordText = cell.button.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }

            cell.isCompleted = false;
            if (cell.cellImage != null)
            {
                // Cache original color if not already cached
                if (cell.originalColor == Color.clear || cell.originalColor.a == 0f)
                {
                    cell.originalColor = cell.cellImage.color;
                }
                cell.cellImage.color = cell.originalColor;
            }

            if (cell.wordText != null)
            {
                cell.wordText.text = cell.word;
                cell.wordText.color = new Color(cell.wordText.color.r, cell.wordText.color.g, cell.wordText.color.b, 1f);
            }

            if (cell.button != null)
            {
                cell.button.enabled = true;
                cell.button.transition = Selectable.Transition.ColorTint; // Restore normal button tinting on reset
                cell.button.interactable = true;
                cell.button.transform.localScale = Vector3.one;

                cell.button.onClick.RemoveAllListeners();
                cell.button.onClick.AddListener(() => OnCellClicked(cell));
            }
        }

        // Initialize palette buttons
        foreach (var palette in paletteButtons)
        {
            if (palette == null || palette.button == null) continue;

            // Runtime fallback to resolve components dynamically if Inspector layout was stale/unassigned
            if (palette.buttonImage == null)
            {
                palette.buttonImage = palette.button.GetComponent<Image>();
            }
            if (palette.buttonText == null)
            {
                palette.buttonText = palette.button.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Prevent button component from overriding color palette colors
            palette.button.transition = Selectable.Transition.None;

            if (palette.buttonImage != null)
            {
                palette.buttonImage.color = palette.buttonColor;
            }
            if (palette.buttonText != null)
            {
                palette.buttonText.text = GetVowelLabel(palette.vowelCategory);
            }

            palette.button.onClick.RemoveAllListeners();
            palette.button.onClick.AddListener(() => OnPaletteClicked(palette));
        }
    }

    private IEnumerator IntroSequence()
    {
        // Animate title and grid in (optional visual polish using LeanTween)
        if (titleText != null)
        {
            titleText.transform.localScale = Vector3.zero;
            LeanTween.scale(titleText.gameObject, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBack);
        }

        // Stagger pop in of grid cells
        for (int i = 0; i < gridCells.Length; i++)
        {
            var cell = gridCells[i];
            if (cell != null && cell.button != null)
            {
                cell.button.transform.localScale = Vector3.zero;
                LeanTween.scale(cell.button.gameObject, Vector3.one, 0.3f)
                    .setEase(LeanTweenType.easeOutBack)
                    .setDelay(i * 0.03f);
            }
        }

        // Play Intro Audio
        if (mascotAudioSource != null && introClip != null)
        {
            mascotAudioSource.clip = introClip;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(introClip.length + 0.2f);
        }

        canPlay = true;
    }

    private void OnCellClicked(GridCell clickedCell)
    {
        Debug.Log($"[ColourBySound] OnCellClicked: '{clickedCell.word}' (Correct: {clickedCell.correctCategory}), canPlay={canPlay}, isCompleted={clickedCell.isCompleted}");
        if (!canPlay || clickedCell.isCompleted) return;

        // Visual selection update
        if (selectedCell != null && selectedCell != clickedCell)
        {
            // Reset previous selected cell scale
            LeanTween.cancel(selectedCell.button.gameObject);
            LeanTween.scale(selectedCell.button.gameObject, Vector3.one, 0.15f);
        }

        selectedCell = clickedCell;
        Debug.Log($"[ColourBySound] Cell '{clickedCell.word}' is now the selected cell.");

        // Pulse and hold selected cell scale
        LeanTween.cancel(selectedCell.button.gameObject);
        LeanTween.scale(selectedCell.button.gameObject, Vector3.one * 1.12f, 0.2f).setEase(LeanTweenType.easeOutBack);

        // Play word audio
        if (mascotAudioSource != null && clickedCell.wordAudio != null)
        {
            mascotAudioSource.clip = clickedCell.wordAudio;
            mascotAudioSource.Play();
        }
    }

    private void OnPaletteClicked(PaletteButton palette)
    {
        Debug.Log($"[ColourBySound] OnPaletteClicked: Category clicked={palette.vowelCategory}, selectedCell='{(selectedCell != null ? selectedCell.word : "null")}', canPlay={canPlay}");
        if (!canPlay || selectedCell == null) return;

        if (palette.vowelCategory == selectedCell.correctCategory)
        {
            Debug.Log($"[ColourBySound] CORRECT MATCH! Word: '{selectedCell.word}' matched category '{palette.vowelCategory}'");
            // Correct coloring match!
            selectedCell.isCompleted = true;

            Button cellButton = selectedCell.button;
            if (cellButton != null)
            {
                // Cancel any selection tweens first before starting new animations
                LeanTween.cancel(cellButton.gameObject);
                cellButton.transition = Selectable.Transition.None; // Prevent button component from overriding filled color
                cellButton.enabled = false; // Disable button component to prevent tint overrides
            }

            // Fill cell image color
            if (selectedCell.cellImage != null)
            {
                Image targetImage = selectedCell.cellImage; // Capture reference locally to avoid null reference after clearing selectedCell
                Color fromColor = targetImage.color;
                Color toColor = palette.buttonColor;
                Debug.Log($"[ColourBySound] Fading cellImage color from {fromColor} to {toColor}");
                LeanTween.value(targetImage.gameObject, fromColor, toColor, 0.25f)
                    .setOnUpdate((Color c) => { 
                        if (targetImage != null)
                        {
                            targetImage.color = c; 
                        }
                    });
            }
            else
            {
                Debug.LogWarning($"[ColourBySound] cellImage for '{selectedCell.word}' is NULL! Cannot change color.");
            }

            // Play correct chime
            if (sfxAudioSource != null && correctChime != null)
            {
                sfxAudioSource.PlayOneShot(correctChime);
            }

            // Reset scale with a pop effect
            if (cellButton != null)
            {
                LeanTween.scale(cellButton.gameObject, Vector3.one * 1.25f, 0.15f)
                    .setLoopPingPong(1)
                    .setOnComplete(() => {
                        if (cellButton != null)
                        {
                            cellButton.transform.localScale = Vector3.one;
                        }
                    });
            }

            selectedCell = null;

            // Check if all cells are complete
            if (CheckCompletion())
            {
                StartCoroutine(CompleteSequence());
            }
        }
        else
        {
            Debug.Log($"[ColourBySound] INCORRECT MATCH! Word '{selectedCell.word}' has correct category '{selectedCell.correctCategory}', but you clicked '{palette.vowelCategory}'");
            // Incorrect match!
            // Shake cell
            if (selectedCell.button != null)
            {
                StartCoroutine(ShakeCell(selectedCell));
            }

            // Play wrong SFX
            if (sfxAudioSource != null && wrongSFX != null)
            {
                sfxAudioSource.PlayOneShot(wrongSFX);
            }

            // Repeat word clip via Mascot
            if (mascotAudioSource != null && selectedCell.wordAudio != null)
            {
                mascotAudioSource.clip = selectedCell.wordAudio;
                mascotAudioSource.Play();
            }
        }
    }

    private IEnumerator ShakeCell(GridCell cell)
    {
        Transform target = cell.button.transform;
        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;
        float duration = 0.25f;
        float magnitude = 7f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            target.localPosition = originalPos + new Vector3(x, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPos;
    }

    private bool CheckCompletion()
    {
        foreach (var cell in gridCells)
        {
            if (cell != null && !cell.isCompleted)
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerator CompleteSequence()
    {
        canPlay = false;
        if (unitCompleteAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        // Play Win SFX
        if (sfxAudioSource != null && winSFX != null)
        {
            sfxAudioSource.PlayOneShot(winSFX);
        }

        yield return new WaitForSeconds(0.4f);

        // 1. Fade out all text labels in the grid to fully reveal the colored hidden picture blocks
        foreach (var cell in gridCells)
        {
            if (cell != null && cell.wordText != null)
            {
                TextMeshProUGUI txt = cell.wordText;
                LeanTween.value(txt.gameObject, 1f, 0f, 0.8f)
                    .setOnUpdate((float a) => {
                        txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, a);
                    });
            }
        }

        yield return new WaitForSeconds(0.8f);

        // 2. Pulse the completed mosaic cells as a unified celebration
        for (int i = 0; i < gridCells.Length; i++)
        {
            var cell = gridCells[i];
            if (cell != null && cell.button != null)
            {
                LeanTween.scale(cell.button.gameObject, Vector3.one * 1.15f, 0.25f)
                    .setLoopPingPong(1)
                    .setDelay(i * 0.02f);
            }
        }

        yield return new WaitForSeconds(0.8f);

        // 3. Fade in a custom illustration overlay if provided
        if (hiddenPictureOverlay != null)
        {
            hiddenPictureOverlay.gameObject.SetActive(true);
            hiddenPictureOverlay.alpha = 0f;
            LeanTween.value(hiddenPictureOverlay.gameObject, 0f, 1f, 0.6f)
                .setOnUpdate((float val) => { hiddenPictureOverlay.alpha = val; });

            yield return new WaitForSeconds(0.6f);
        }

        // 4. Trigger Unity completion events and show next level button
        onComplete?.Invoke();

        if (nextButton != null)
        {
            nextButton.SetActive(true);
            nextButton.transform.localScale = Vector3.zero;
            LeanTween.scale(nextButton, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
        }
    }

    #region Context Menu Utilities
    [ContextMenu("Auto Fill Grid Words and Categories")]
    public void AutoFillGridData()
    {
        string[] words = new string[20] {
            "snow", "aim", "use", "make", "coat",
            "five", "drive", "we", "kite", "sky",
            "bean", "bed", "jet", "web", "deer",
            "sock", "hat", "hop", "apple", "mom"
        };
        
        VowelCategory[] categories = new VowelCategory[20] {
            VowelCategory.LongO, VowelCategory.LongA, VowelCategory.LongU, VowelCategory.LongA, VowelCategory.LongO,
            VowelCategory.LongI, VowelCategory.LongI, VowelCategory.LongE, VowelCategory.LongI, VowelCategory.LongI,
            VowelCategory.LongE, VowelCategory.ShortE, VowelCategory.ShortE, VowelCategory.ShortE, VowelCategory.LongE,
            VowelCategory.ShortO, VowelCategory.ShortA, VowelCategory.ShortO, VowelCategory.ShortA, VowelCategory.ShortO
        };

        if (gridCells == null || gridCells.Length != 20)
        {
            gridCells = new GridCell[20];
            for (int i = 0; i < 20; i++)
            {
                gridCells[i] = new GridCell();
            }
        }

        for (int i = 0; i < 20; i++)
        {
            if (gridCells[i] == null) gridCells[i] = new GridCell();
            gridCells[i].word = words[i];
            gridCells[i].correctCategory = categories[i];
            if (gridCells[i].wordText != null)
            {
                gridCells[i].wordText.text = words[i];
            }
        }
        Debug.Log("Successfully auto-filled 20 words and categories into grid cells in a symmetric landscape layout.");
    }

    [ContextMenu("Auto Setup Palette Colors")]
    public void AutoSetupPaletteColors()
    {
        if (paletteButtons == null || paletteButtons.Length != 10)
        {
            paletteButtons = new PaletteButton[10];
            for (int i = 0; i < 10; i++)
            {
                paletteButtons[i] = new PaletteButton();
            }
        }

        // Set categories and standard colors
        paletteButtons[0].vowelCategory = VowelCategory.ShortA;
        paletteButtons[0].buttonColor = new Color(0.1f, 0.45f, 0.1f); // Dark Green

        paletteButtons[1].vowelCategory = VowelCategory.ShortE;
        paletteButtons[1].buttonColor = new Color(0.5f, 0.85f, 0.5f); // Light Green

        paletteButtons[2].vowelCategory = VowelCategory.ShortI;
        paletteButtons[2].buttonColor = new Color(0.55f, 0.27f, 0.07f); // Brown

        paletteButtons[3].vowelCategory = VowelCategory.ShortO;
        paletteButtons[3].buttonColor = new Color(0.1f, 0.1f, 0.55f); // Dark Blue

        paletteButtons[4].vowelCategory = VowelCategory.ShortU;
        paletteButtons[4].buttonColor = new Color(0.53f, 0.81f, 0.98f); // Light Blue

        paletteButtons[5].vowelCategory = VowelCategory.LongA;
        paletteButtons[5].buttonColor = new Color(0.85f, 0.1f, 0.1f); // Red

        paletteButtons[6].vowelCategory = VowelCategory.LongE;
        paletteButtons[6].buttonColor = new Color(0.95f, 0.95f, 0.1f); // Yellow

        paletteButtons[7].vowelCategory = VowelCategory.LongI;
        paletteButtons[7].buttonColor = new Color(1f, 0.45f, 0.65f); // Pink

        paletteButtons[8].vowelCategory = VowelCategory.LongO;
        paletteButtons[8].buttonColor = new Color(0.55f, 0.15f, 0.55f); // Purple

        paletteButtons[9].vowelCategory = VowelCategory.LongU;
        paletteButtons[9].buttonColor = new Color(1f, 0.55f, 0f); // Orange

        // Update labels and colors in the Editor
        for (int i = 0; i < paletteButtons.Length; i++)
        {
            var p = paletteButtons[i];
            if (p.buttonText != null)
            {
                p.buttonText.text = GetVowelLabel(p.vowelCategory);
            }
            if (p.buttonImage != null)
            {
                p.buttonImage.color = p.buttonColor;
            }
        }

        Debug.Log("Successfully setup palette category mappings and colors.");
    }

    private string GetVowelLabel(VowelCategory category)
    {
        switch (category)
        {
            case VowelCategory.ShortA: return "short a";
            case VowelCategory.ShortE: return "short e";
            case VowelCategory.ShortI: return "short i";
            case VowelCategory.ShortO: return "short o";
            case VowelCategory.ShortU: return "short u";
            case VowelCategory.LongA: return "long a";
            case VowelCategory.LongE: return "long e";
            case VowelCategory.LongI: return "long i";
            case VowelCategory.LongO: return "long o";
            case VowelCategory.LongU: return "long u";
            default: return "";
        }
    }

    [ContextMenu("Auto Assign UI References")]
    public void AutoAssignUIReferences()
    {
        // 1. Assign grid cells from gridPanel children
        if (gridPanel != null)
        {
            int childCount = gridPanel.childCount;
            if (childCount < 20)
            {
                Debug.LogWarning($"[ColourBySound] gridPanel only has {childCount} children, but we need 20 cells!");
            }

            if (gridCells == null || gridCells.Length != 20)
            {
                gridCells = new GridCell[20];
            }

            for (int i = 0; i < 20; i++)
            {
                if (gridCells[i] == null) gridCells[i] = new GridCell();

                if (i < childCount)
                {
                    Transform cellChild = gridPanel.GetChild(i);
                    
                    // Search recursively in children to find the Button component (e.g. on the child named "Button")
                    Button cellBtn = cellChild.GetComponentInChildren<Button>(true);
                    gridCells[i].button = cellBtn;

                    if (cellBtn != null)
                    {
                        // Get the Image component attached to the Button itself
                        gridCells[i].cellImage = cellBtn.GetComponent<Image>();
                    }
                    else
                    {
                        // Fallback to searching children for the Image component
                        gridCells[i].cellImage = cellChild.GetComponentInChildren<Image>(true);
                    }

                    // Get TextMeshProUGUI in children
                    gridCells[i].wordText = cellChild.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
            Debug.Log("Successfully assigned Button, Image, and TextMeshProUGUI references for the 20 grid cells from children of gridPanel.");
        }
        else
        {
            Debug.LogError("[ColourBySound] gridPanel reference is null! Please assign it in the Inspector.");
        }

        // 2. Assign palette buttons from longPalettePanel and shortPalettePanel
        if (paletteButtons == null || paletteButtons.Length != 10)
        {
            AutoSetupPaletteColors(); // initialize if empty
        }

        // We assume Long has 5 buttons in order: LongA, LongE, LongI, LongO, LongU
        // We assume Short has 5 buttons in order: ShortA, ShortE, ShortI, ShortO, ShortU
        AssignPaletteFromPanel(shortPalettePanel, new VowelCategory[] {
            VowelCategory.ShortA, VowelCategory.ShortE, VowelCategory.ShortI, VowelCategory.ShortO, VowelCategory.ShortU
        });

        AssignPaletteFromPanel(longPalettePanel, new VowelCategory[] {
            VowelCategory.LongA, VowelCategory.LongE, VowelCategory.LongI, VowelCategory.LongO, VowelCategory.LongU
        });

        // Set palette texts and colors immediately in the editor
        for (int j = 0; j < paletteButtons.Length; j++)
        {
            var p = paletteButtons[j];
            if (p.buttonText != null)
            {
                p.buttonText.text = GetVowelLabel(p.vowelCategory);
            }
            if (p.buttonImage != null)
            {
                p.buttonImage.color = p.buttonColor;
            }
        }
    }

    private void AssignPaletteFromPanel(RectTransform panel, VowelCategory[] categories)
    {
        if (panel == null)
        {
            Debug.LogWarning("[ColourBySound] Palette panel reference is null! Skipping auto-assignment for this panel.");
            return;
        }

        int count = Mathf.Min(panel.childCount, categories.Length);
        for (int i = 0; i < count; i++)
        {
            Transform buttonChild = panel.GetChild(i);
            Button btn = buttonChild.GetComponentInChildren<Button>(true);
            VowelCategory cat = categories[i];

            // Find matching category in paletteButtons array and assign button
            for (int j = 0; j < paletteButtons.Length; j++)
            {
                if (paletteButtons[j].vowelCategory == cat)
                {
                    paletteButtons[j].button = btn;
                    if (btn != null)
                    {
                        paletteButtons[j].buttonImage = btn.GetComponent<Image>();
                        paletteButtons[j].buttonText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                    else
                    {
                        paletteButtons[j].buttonImage = buttonChild.GetComponentInChildren<Image>(true);
                        paletteButtons[j].buttonText = buttonChild.GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                    break;
                }
            }
        }
        Debug.Log($"Successfully assigned button references for {panel.name} panel.");
    }
    #endregion
}
