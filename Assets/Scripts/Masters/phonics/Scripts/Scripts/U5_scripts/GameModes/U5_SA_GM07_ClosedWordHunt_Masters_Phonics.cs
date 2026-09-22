using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U5_SA_GM07_ClosedWordHunt_Masters_Phonics : MonoBehaviour
    {
        [Header("Grid Layout & Config")]
        [SerializeField] private int gridRows = 8;
        [SerializeField] private int gridCols = 8;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private GameObject cellPrefab;

        [Header("Word Bank UI")]
        [SerializeField] private Transform wordBankContainer;
        [SerializeField] private GameObject wordBankItemPrefab;

        [Header("HUD & Progress")]
        [SerializeField] private TextMeshProUGUI foundCountText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI mascotCommentaryText;
        [SerializeField] private GameObject mascotBubble;
        [SerializeField] private Button hintButton;
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Played when a correct word is found in the grid")]
        public AudioClip correctSFX;
        [Tooltip("Played when an invalid word is selected")]
        public AudioClip wrongSFX;
        [Tooltip("SFX played when tapping/dragging letters")]
        public AudioClip gridSelectSFX;
        [Tooltip("SFX played when a word is completed/highlighted")]
        public AudioClip gridFoundSFX;
        [Tooltip("SFX played for closed door sound")]
        public AudioClip doorSlamSFX;

        [Header("Colors & Sprites")]
        [SerializeField] private Color defaultCellColor = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] private Color selectedCellColor = new Color(1f, 0.92f, 0.45f, 1f);
        [SerializeField] private Color[] foundHighlightColors = new Color[]
        {
            new Color(0.38f, 0.85f, 0.65f, 1f), // Mint Green
            new Color(0.45f, 0.75f, 0.98f, 1f), // Sky Blue
            new Color(1f, 0.65f, 0.45f, 1f),    // Coral Sunset
            new Color(0.85f, 0.6f, 0.95f, 1f),  // Lavender
            new Color(0.55f, 0.65f, 1f, 1f),    // Periwinkle
            new Color(1f, 0.85f, 0.35f, 1f),    // Golden Yellow
            new Color(0.4f, 0.88f, 0.82f, 1f),  // Aqua Teal
            new Color(0.98f, 0.6f, 0.75f, 1f),  // Rose
            new Color(0.75f, 0.82f, 0.98f, 1f), // Soft Blue
            new Color(0.9f, 0.75f, 0.98f, 1f)   // Soft Orchid
        };

        // Grid internal representation
        private char[,] charGrid;
        private GridCellView[,] cellViews;
        private List<ClosedWordSearchItem> targetWords = new List<ClosedWordSearchItem>();
        private Dictionary<string, WordBankItemView> wordBankViews = new Dictionary<string, WordBankItemView>();
        
        // Touch Drag Selection State
        private bool isPointerDown = false;
        private GridCellView startCell = null;
        private GridCellView currentCell = null;
        private List<GridCellView> selectedPath = new List<GridCellView>();

        private int foundWordsCount = 0;
        private int totalScore = 0;

        private static Sprite proceduralRoundedSprite = null;

        public static Sprite GetOrCreateRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius - 1.5f)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist <= radius + 1.5f)
                    {
                        float alpha = Mathf.Clamp01(1f - (dist - (radius - 1.5f)) / 3f);
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return proceduralRoundedSprite;
        }

        private void Awake()
        {
            AutoBindHierarchyElements();
            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(GiveHint);
                hintButton.onClick.AddListener(GiveHint);
            }

            BuildDefaultTargetWords();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            if (hintButton != null)
            {
                hintButton.onClick.RemoveListener(GiveHint);
                hintButton.onClick.AddListener(GiveHint);
            }
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            if (gridContainer == null)
            {
                Transform t = transform.Find("LetterGridContainer") 
                           ?? transform.Find("GridContainer") 
                           ?? transform.Find("Grid")
                           ?? transform.Find("LetterGrid");
                if (t != null) gridContainer = t.GetComponent<RectTransform>();
            }

            if (gridContainer != null && gridLayout == null)
            {
                gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
            }

            if (gridContainer == null)
            {
                GameObject gridObj = new GameObject("LetterGridContainer", typeof(RectTransform), typeof(GridLayoutGroup));
                gridObj.transform.SetParent(transform, false);
                gridContainer = gridObj.GetComponent<RectTransform>();
                gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
                gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
                gridContainer.pivot = new Vector2(0.5f, 0.5f);
                gridContainer.anchoredPosition = new Vector2(-280f, -30f);
                gridContainer.sizeDelta = new Vector2(720f, 720f);
                gridLayout = gridObj.GetComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(80f, 80f);
                gridLayout.spacing = new Vector2(8f, 8f);
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = gridCols;
            }
            else if (gridLayout != null)
            {
                gridLayout.cellSize = new Vector2(80f, 80f);
                gridLayout.spacing = new Vector2(8f, 8f);
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = gridCols;
            }

            if (wordBankContainer == null)
            {
                Transform t = transform.Find("WordBankContainer") 
                           ?? transform.Find("WordBank") 
                           ?? transform.Find("Bank")
                           ?? transform.Find("WordsContainer");
                if (t != null) wordBankContainer = t;
                else
                {
                    GameObject bankObj = new GameObject("WordBankContainer", typeof(RectTransform), typeof(GridLayoutGroup));
                    bankObj.transform.SetParent(transform, false);
                    wordBankContainer = bankObj.transform;
                    RectTransform bankRt = bankObj.GetComponent<RectTransform>();
                    bankRt.anchorMin = new Vector2(0.5f, 0.5f);
                    bankRt.anchorMax = new Vector2(0.5f, 0.5f);
                    bankRt.pivot = new Vector2(0.5f, 0.5f);
                    bankRt.anchoredPosition = new Vector2(460f, -40f);
                    bankRt.sizeDelta = new Vector2(440f, 580f);
                    var bankGlg = bankObj.GetComponent<GridLayoutGroup>();
                    bankGlg.cellSize = new Vector2(205f, 62f);
                    bankGlg.spacing = new Vector2(16f, 16f);
                    bankGlg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    bankGlg.constraintCount = 2;
                }
            }
            else
            {
                var bankGlg = wordBankContainer.GetComponent<GridLayoutGroup>();
                if (bankGlg != null)
                {
                    bankGlg.cellSize = new Vector2(205f, 62f);
                    bankGlg.spacing = new Vector2(16f, 16f);
                }
            }

            Transform headerT = transform.Find("TitleText") ?? transform.Find("Header");
            if (headerT == null)
            {
                GameObject hObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                hObj.transform.SetParent(transform, false);
                RectTransform hRt = hObj.GetComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0.5f, 0.5f);
                hRt.anchorMax = new Vector2(0.5f, 0.5f);
                hRt.pivot = new Vector2(0.5f, 0.5f);
                hRt.anchoredPosition = new Vector2(0f, 440f);
                hRt.sizeDelta = new Vector2(1200f, 70f);
                TextMeshProUGUI tmp = hObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "<b>ACTIVITY 4: CLOSED-WORD HUNT</b>";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 38;
                tmp.color = new Color(0.12f, 0.2f, 0.35f, 1f);
            }

            if (progressBar == null) progressBar = GetComponentInChildren<Slider>(true);
            if (foundCountText == null)
            {
                Transform t = transform.Find("ProgressText") ?? transform.Find("FoundCountText") ?? transform.Find("Progress_Text");
                if (t != null) foundCountText = t.GetComponent<TextMeshProUGUI>();
            }
            if (scoreText == null)
            {
                Transform t = transform.Find("ScoreText") ?? transform.Find("Score_Text") ?? transform.Find("HUD/ScoreText");
                if (t != null) scoreText = t.GetComponent<TextMeshProUGUI>();
            }
            if (hintButton == null)
            {
                Transform t = transform.Find("HintButton") ?? transform.Find("Btn_Hint") ?? transform.Find("Hint_Button");
                if (t != null) hintButton = t.GetComponent<Button>();
            }
            if (mascotBubble == null)
            {
                Transform t = transform.Find("MascotBubble") ?? transform.Find("Mascot_Bubble") ?? transform.Find("CommentaryBubble");
                if (t != null) mascotBubble = t.gameObject;
            }
            if (mascotCommentaryText == null)
            {
                if (mascotBubble != null) mascotCommentaryText = mascotBubble.GetComponentInChildren<TextMeshProUGUI>(true);
                if (mascotCommentaryText == null)
                {
                    Transform t = transform.Find("MascotText") ?? transform.Find("CommentaryText");
                    if (t != null) mascotCommentaryText = t.GetComponent<TextMeshProUGUI>();
                }
            }
            if (feedbackPanel == null)
            {
                Transform t = transform.Find("FeedbackPanel") ?? transform.Find("Feedback_Panel");
                if (t != null)
                {
                    feedbackPanel = t.gameObject;
                    feedbackText = feedbackPanel.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            foundWordsCount = 0;
            totalScore = 0;
            isPointerDown = false;
            startCell = null;
            currentCell = null;
            selectedPath.Clear();

            foreach (var item in targetWords)
                item.isFound = false;

            GenerateGrid();
            PopulateWordBankUI();
            UpdateHUD();

            ShowMascotCommentary("Find all 10 closed-syllable words in the letter grid!");
        }

        private void BuildDefaultTargetWords()
        {
            if (targetWords != null && targetWords.Count > 0) return;

            targetWords = new List<ClosedWordSearchItem>()
            {
                new ClosedWordSearchItem { word = "BED", isIrregularSightWord = false },
                new ClosedWordSearchItem { word = "BIG", isIrregularSightWord = false },
                new ClosedWordSearchItem { word = "CAB", isIrregularSightWord = false },
                new ClosedWordSearchItem { word = "CAT", isIrregularSightWord = false },
                new ClosedWordSearchItem { word = "CLUB", isIrregularSightWord = false },
                new ClosedWordSearchItem { word = "FOX", isIrregularSightWord = false },
                new ClosedWordSearchItem { word = "FROG", isIrregularSightWord = false },
                new ClosedWordSearchItem { word = "GOT", isIrregularSightWord = false },
                new ClosedWordSearchItem { word = "HIP", isIrregularSightWord = false },
                new ClosedWordSearchItem { word = "POT", isIrregularSightWord = false }
            };
        }

        private void GenerateGrid()
        {
            charGrid = new char[gridRows, gridCols];

            // 1. Clear grid
            for (int r = 0; r < gridRows; r++)
                for (int c = 0; c < gridCols; c++)
                    charGrid[r, c] = '\0';

            // 2. Place target words
            int[] dRow = { 0, 1, 1, -1 };
            int[] dCol = { 1, 0, 1, 1 };

            foreach (var item in targetWords)
            {
                string w = item.word.ToUpper();
                bool placed = false;
                int attempts = 0;

                while (!placed && attempts < 100)
                {
                    attempts++;
                    int dir = Random.Range(0, 3); // horizontal, vertical, diagonal
                    int dr = dRow[dir];
                    int dc = dCol[dir];

                    int startR = Random.Range(0, gridRows);
                    int startC = Random.Range(0, gridCols);

                    int endR = startR + dr * (w.Length - 1);
                    int endC = startC + dc * (w.Length - 1);

                    if (endR >= 0 && endR < gridRows && endC >= 0 && endC < gridCols)
                    {
                        bool canPlace = true;
                        for (int i = 0; i < w.Length; i++)
                        {
                            int r = startR + dr * i;
                            int c = startC + dc * i;
                            if (charGrid[r, c] != '\0' && charGrid[r, c] != w[i])
                            {
                                canPlace = false;
                                break;
                            }
                        }

                        if (canPlace)
                        {
                            for (int i = 0; i < w.Length; i++)
                            {
                                int r = startR + dr * i;
                                int c = startC + dc * i;
                                charGrid[r, c] = w[i];
                            }
                            placed = true;
                        }
                    }
                }
            }

            // 3. Fill remaining letters with random consonants and vowels
            string fillerLetters = "ABCDEFGHIKLMNOPRSTUVXYZ";
            for (int r = 0; r < gridRows; r++)
            {
                for (int c = 0; c < gridCols; c++)
                {
                    if (charGrid[r, c] == '\0')
                    {
                        charGrid[r, c] = fillerLetters[Random.Range(0, fillerLetters.Length)];
                    }
                }
            }

            // 4. Instantiate or update UI cells
            InstantiateGridUI();
        }

        private void InstantiateGridUI()
        {
            if (gridContainer == null) return;

            // Clear old cells
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }

            cellViews = new GridCellView[gridRows, gridCols];

            for (int r = 0; r < gridRows; r++)
            {
                for (int c = 0; c < gridCols; c++)
                {
                    GameObject obj = null;
                    if (cellPrefab != null)
                        obj = Instantiate(cellPrefab, gridContainer);
                    else
                        obj = CreateDefaultCellGameObject(r, c);

                    GridCellView view = obj.GetComponent<GridCellView>();
                    if (view == null) view = obj.AddComponent<GridCellView>();

                    view.Init(r, c, charGrid[r, c], defaultCellColor, this);
                    cellViews[r, c] = view;
                }
            }
        }

        private GameObject CreateDefaultCellGameObject(int r, int c)
        {
            GameObject cellObj = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image));
            cellObj.transform.SetParent(gridContainer, false);
            
            Image img = cellObj.GetComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = defaultCellColor;

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(cellObj.transform, false);
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 34;
            tmp.color = new Color(0.12f, 0.16f, 0.25f, 1f);
            tmp.fontStyle = FontStyles.Bold;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            return cellObj;
        }

        private void PopulateWordBankUI()
        {
            if (wordBankContainer == null) return;

            foreach (Transform child in wordBankContainer)
                Destroy(child.gameObject);

            wordBankViews.Clear();

            foreach (var item in targetWords)
            {
                GameObject obj = null;
                if (wordBankItemPrefab != null)
                    obj = Instantiate(wordBankItemPrefab, wordBankContainer);
                else
                    obj = CreateDefaultWordBankItem(item.word);

                WordBankItemView view = obj.GetComponent<WordBankItemView>();
                if (view == null) view = obj.AddComponent<WordBankItemView>();

                view.Init(item.word, () => PlayWordSound(item.word));
                wordBankViews[item.word.ToUpper()] = view;
            }
        }

        private GameObject CreateDefaultWordBankItem(string word)
        {
            GameObject itemObj = new GameObject($"Word_{word}", typeof(RectTransform), typeof(Image), typeof(Button));
            itemObj.transform.SetParent(wordBankContainer, false);

            Image img = itemObj.GetComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.95f, 0.97f, 1f, 1f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(itemObj.transform, false);
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = word;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            tmp.color = new Color(0.18f, 0.24f, 0.36f, 1f);
            tmp.fontStyle = FontStyles.Bold;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            return itemObj;
        }

        // -------------------------------------------------------------
        // Cell Pointer / Touch Handling
        // -------------------------------------------------------------
        public void OnCellPointerDown(GridCellView cell)
        {
            isPointerDown = true;
            startCell = cell;
            currentCell = cell;
            HighlightSelectionPath();
        }

        public void OnCellPointerEnter(GridCellView cell)
        {
            if (!isPointerDown) return;
            currentCell = cell;
            HighlightSelectionPath();
        }

        public void OnCellPointerUp(GridCellView cell)
        {
            if (!isPointerDown) return;
            isPointerDown = false;
            currentCell = cell;
            EvaluateSelectedPath();
        }

        private void HighlightSelectionPath()
        {
            // Clear current preview path
            foreach (var c in selectedPath)
            {
                if (!c.IsPermanentlyFound)
                    c.SetColor(defaultCellColor);
            }
            selectedPath.Clear();

            if (startCell == null || currentCell == null) return;

            int dr = currentCell.Row - startCell.Row;
            int dc = currentCell.Col - startCell.Col;

            int stepR = (dr == 0) ? 0 : (dr > 0 ? 1 : -1);
            int stepC = (dc == 0) ? 0 : (dc > 0 ? 1 : -1);

            // Verify if straight line (horizontal, vertical, or 45-deg diagonal)
            bool isStraightLine = (dr == 0 || dc == 0 || Mathf.Abs(dr) == Mathf.Abs(dc));
            if (!isStraightLine)
            {
                selectedPath.Add(startCell);
                if (!startCell.IsPermanentlyFound)
                    startCell.SetColor(selectedCellColor);
                return;
            }

            int length = Mathf.Max(Mathf.Abs(dr), Mathf.Abs(dc)) + 1;
            for (int i = 0; i < length; i++)
            {
                int r = startCell.Row + stepR * i;
                int c = startCell.Col + stepC * i;
                if (r >= 0 && r < gridRows && c >= 0 && c < gridCols)
                {
                    GridCellView cellView = cellViews[r, c];
                    selectedPath.Add(cellView);
                    if (!cellView.IsPermanentlyFound)
                        cellView.SetColor(selectedCellColor);
                }
            }
        }

        private void EvaluateSelectedPath()
        {
            if (selectedPath.Count < 2)
            {
                ClearSelectionVisuals();
                return;
            }

            // Build forward and backward strings
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var c in selectedPath) sb.Append(c.Letter);
            string selectedWord = sb.ToString().ToUpper();

            // Reverse string check
            char[] revArr = selectedWord.ToCharArray();
            System.Array.Reverse(revArr);
            string reverseWord = new string(revArr);

            ClosedWordSearchItem matchedItem = targetWords.Find(w => !w.isFound && (w.word.ToUpper() == selectedWord || w.word.ToUpper() == reverseWord));

            if (matchedItem != null)
            {
                // Word found!
                matchedItem.isFound = true;
                foundWordsCount++;
                totalScore += 30;

                if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U5_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(30);

                Color highlightCol = foundHighlightColors[(foundWordsCount - 1) % foundHighlightColors.Length];
                foreach (var c in selectedPath)
                {
                    c.MarkFound(highlightCol);
                }

                if (wordBankViews.ContainsKey(matchedItem.word.ToUpper()))
                {
                    wordBankViews[matchedItem.word.ToUpper()].MarkFound(highlightCol);
                }

                // Audio & SFX
                if (doorSlamSFX != null && U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(doorSlamSFX);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayDoorSlamSFX();

                if (gridFoundSFX != null && U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(gridFoundSFX);
                else if (correctSFX != null && U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(correctSFX);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                PlayWordSound(matchedItem.word);

                ShowMascotCommentary($"Found <b>{matchedItem.word}</b>! Consonant closes the door — short vowel!");

                UpdateHUD();

                if (foundWordsCount >= targetWords.Count)
                {
                    FinishActivity();
                }
            }
            else
            {
                // Wrong / Invalid selection SFX
                if (wrongSFX != null && U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(wrongSFX);

                // Check if user found irregular sight words 'said' or 'was'
                if (selectedWord == "SAID" || reverseWord == "SAID" || selectedWord == "WAS" || reverseWord == "WAS")
                {
                    ShowMascotCommentary("That one's a rule-breaker! You just have to remember it!");
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wobble");
                }
                ClearSelectionVisuals();
            }

            selectedPath.Clear();
            startCell = null;
            currentCell = null;
        }

        private void ClearSelectionVisuals()
        {
            foreach (var c in selectedPath)
            {
                if (!c.IsPermanentlyFound)
                    c.SetColor(defaultCellColor);
            }
        }

        private void PlayWordSound(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            U5_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(word);
        }

        private void ShowMascotCommentary(string msg)
        {
            if (mascotCommentaryText != null)
                mascotCommentaryText.text = msg;
            if (mascotBubble != null)
            {
                mascotBubble.SetActive(true);
                StartCoroutine(PunchScale(mascotBubble.transform, 1.08f, 0.15f));
            }
        }

        public void GiveHint()
        {
            ClosedWordSearchItem unfound = targetWords.Find(w => !w.isFound);
            if (unfound != null)
            {
                ShowMascotCommentary($"Look for <b>{unfound.word}</b> (starts with '{unfound.word[0]}')!");
                PlayWordSound(unfound.word);
            }
        }

        private void UpdateHUD()
        {
            if (foundCountText != null)
                foundCountText.text = $"{foundWordsCount} / {targetWords.Count}";

            if (progressBar != null)
                progressBar.value = (float)foundWordsCount / targetWords.Count;

            if (scoreText != null)
                scoreText.text = $"Score: {totalScore}";
        }

        private void FinishActivity()
        {
            if (progressBar != null) progressBar.value = 1f;
            U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("activity_complete");

            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(true);
                if (feedbackText != null)
                    feedbackText.text = $"Activity 4 Complete!\nAll 10 Closed-Syllable Words Found!\nScore: {totalScore}";
            }

            StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            yield return new WaitForSeconds(2.5f);
            if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U5_SA_UnitFlowManager_Masters_Phonics.Instance.CompleteCurrentActivity();
            }
        }

        private IEnumerator PunchScale(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            target.localScale = original * scale;
            yield return new WaitForSeconds(duration);
            target.localScale = original;
        }
    }

    // -------------------------------------------------------------
    // Helper Grid Cell Component
    // -------------------------------------------------------------
    public class GridCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        public int Row { get; private set; }
        public int Col { get; private set; }
        public char Letter { get; private set; }
        public bool IsPermanentlyFound { get; private set; }

        private Image bgImage;
        private TextMeshProUGUI letterText;
        private U5_SA_GM07_ClosedWordHunt_Masters_Phonics parentGame;

        public void Init(int r, int c, char letter, Color defaultCol, U5_SA_GM07_ClosedWordHunt_Masters_Phonics game)
        {
            Row = r;
            Col = c;
            Letter = letter;
            parentGame = game;
            IsPermanentlyFound = false;

            bgImage = GetComponent<Image>();
            if (bgImage != null && bgImage.sprite == null)
            {
                bgImage.sprite = U5_SA_GM07_ClosedWordHunt_Masters_Phonics.GetOrCreateRoundedSprite();
                bgImage.type = Image.Type.Sliced;
            }

            letterText = GetComponentInChildren<TextMeshProUGUI>();
            if (letterText != null)
            {
                letterText.text = letter.ToString();
                letterText.fontSize = 34;
                letterText.fontStyle = FontStyles.Bold;
                letterText.color = new Color(0.12f, 0.16f, 0.25f, 1f);
            }

            SetColor(defaultCol);
        }

        public void SetColor(Color col)
        {
            if (bgImage != null)
                bgImage.color = col;
        }

        public void MarkFound(Color highlightCol)
        {
            IsPermanentlyFound = true;
            SetColor(highlightCol);
            if (letterText != null)
            {
                letterText.color = Color.white;
                letterText.fontStyle = FontStyles.Bold;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            parentGame?.OnCellPointerDown(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            parentGame?.OnCellPointerEnter(this);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            parentGame?.OnCellPointerUp(this);
        }
    }

    // -------------------------------------------------------------
    // Helper Word Bank Item Component
    // -------------------------------------------------------------
    public class WordBankItemView : MonoBehaviour
    {
        private TextMeshProUGUI wordText;
        private Image bgImage;
        private string originalWord = "";

        public void Init(string word, System.Action onClick)
        {
            originalWord = word;
            wordText = GetComponentInChildren<TextMeshProUGUI>();
            bgImage = GetComponent<Image>();

            if (bgImage != null && bgImage.sprite == null)
            {
                bgImage.sprite = U5_SA_GM07_ClosedWordHunt_Masters_Phonics.GetOrCreateRoundedSprite();
                bgImage.type = Image.Type.Sliced;
            }

            if (wordText != null)
            {
                wordText.text = word;
                wordText.fontSize = 24;
                wordText.fontStyle = FontStyles.Bold;
                wordText.color = new Color(0.18f, 0.24f, 0.36f, 1f);
            }

            Button btn = GetComponent<Button>();
            if (btn != null && onClick != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onClick());
            }
        }

        public void MarkFound(Color col)
        {
            if (bgImage != null)
                bgImage.color = col;

            if (wordText != null)
            {
                wordText.text = originalWord;
                wordText.fontStyle = FontStyles.Bold;
                wordText.color = Color.white;
            }
        }
    }
}
