using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U9_SA_GM02s_TurtleRule_Masters_Phonics : MonoBehaviour
    {
        [Header("Letter Tiles Container")]
        [SerializeField] private Transform tileContainer;
        [SerializeField] private GameObject letterTilePrefab;
        [SerializeField] private GameObject gapTargetPrefab;
        [SerializeField] private GameObject dividerBarPrefab;

        [Header("Buttons & Action UI")]
        [SerializeField] private Button splitActionBtn;
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Button hintCountbackBtn;

        [Header("Instruction / Prompt")]
        [SerializeField] private TextMeshProUGUI promptInstructionText;

        [Header("Countback 3-2-1 Overlay")]
        [SerializeField] private GameObject countbackBadgeGO;
        [SerializeField] private TextMeshProUGUI countbackStepText; // "3", "2", "1"

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Item Pool & State")]
        private List<TurtleSplitItem> items = new List<TurtleSplitItem>();
        private int currentItemIndex = 0;
        private int selectedGapIndex = -1;
        private int firstAttemptSuccesses = 0;
        private bool hasFailedCurrentItem = false;
        private bool isProcessing = false;

        private List<GameObject> activeTileGOs = new List<GameObject>();
        private List<GameObject> letterTileGOs = new List<GameObject>();
        private List<GameObject> gapTileGOs = new List<GameObject>();
        private List<Button> activeGapButtons = new List<Button>();
        private GameObject activeDividerBar = null;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            UpdateHUD();
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;
            U9_UI_Utils.FormatHeaderTypography(root, "ACTIVITY 1: THE TURTLE RULE", "Count Back Three & Split");

            if (promptInstructionText == null)
            {
                Transform pt = root.Find("InstructionText") ?? root.Find("PromptText") ?? root.Find("Prompt") ?? root.Find("Header/PromptText");
                if (pt != null) promptInstructionText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (promptInstructionText != null)
            {
                promptInstructionText.text = "<b>Count back 3 letters, tap the gap, and press SPLIT!</b>";
                promptInstructionText.fontSize = 24;
                promptInstructionText.alignment = TextAlignmentOptions.Center;
                promptInstructionText.color = new Color(0.85f, 0.92f, 1f);
            }

            if (tileContainer == null)
            {
                tileContainer = root.Find("WordContainer")
                             ?? root.Find("TileContainer")
                             ?? root.Find("LetterTiles")
                             ?? root.Find("Content/TileContainer");
            }

            if (tileContainer != null)
            {
                var hlg = tileContainer.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null)
                {
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.spacing = 10;
                    hlg.childControlWidth = false;
                    hlg.childControlHeight = false;
                }
                var rt = tileContainer.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(1200, 180);
                    rt.anchoredPosition = new Vector2(0, 10);
                }
            }

            if (splitActionBtn == null)
            {
                splitActionBtn = FindButton(root, "SplitBtn", "Split_Btn", "ActionBtn", "Btn_Split");
            }

            if (replayAudioBtn == null)
            {
                replayAudioBtn = FindButton(root, "ReplayBtn", "Speaker_Button", "AudioBtn", "ReplayAudioBtn");
            }

            if (hintCountbackBtn == null)
            {
                hintCountbackBtn = FindButton(root, "HintBtn", "CountbackBtn", "Btn_Hint");
            }

            if (countbackBadgeGO == null)
            {
                Transform cb = root.Find("CountbackBadge") ?? root.Find("Countback_Overlay");
                if (cb != null)
                {
                    countbackBadgeGO = cb.gameObject;
                    countbackStepText = cb.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }

            if (progressText == null)
            {
                Transform pt = root.Find("ProgressHUD/Progress_Text")
                            ?? root.Find("ProgressHUD/ProgressText")
                            ?? root.Find("ProgressText")
                            ?? root.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            if (scoreText == null)
            {
                Transform st = root.Find("ProgressHUD/Score_Text")
                            ?? root.Find("ScoreText")
                            ?? root.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }

            AttachButtonListeners();
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"Content/{name}") 
                           ?? root.Find($"HUD/{name}") 
                           ?? root.Find($"ProgressHUD/{name}") 
                           ?? FindDeepChild(root, name);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>();
                    if (b != null) return b;
                }
            }
            return null;
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void AttachButtonListeners()
        {
            if (splitActionBtn != null)
            {
                splitActionBtn.onClick.RemoveAllListeners();
                splitActionBtn.onClick.AddListener(OnSplitActionTapped);
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentWordAudio);
            }

            if (hintCountbackBtn != null)
            {
                hintCountbackBtn.onClick.RemoveAllListeners();
                hintCountbackBtn.onClick.AddListener(() => StartCoroutine(CoPlayCountbackAnimation()));
            }
        }

        public void StartActivity()
        {
            items = U9_DataBank.GetActivity1SplitPool();
            // Shuffle
            for (int i = 0; i < items.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, items.Count);
                var temp = items[i];
                items[i] = items[r];
                items[r] = temp;
            }

            // Limit to 16 items
            if (items.Count > 16) items = items.GetRange(0, 16);

            currentItemIndex = 0;
            firstAttemptSuccesses = 0;
            isProcessing = false;

            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_a1_intro", () =>
                {
                    if (this != null && gameObject.activeInHierarchy)
                    {
                        LoadCurrentItem();
                    }
                });
            }
            else
            {
                LoadCurrentItem();
            }
        }

        private void LoadCurrentItem()
        {
            if (!gameObject.activeInHierarchy) return;

            if (currentItemIndex >= items.Count)
            {
                CompleteActivity();
                return;
            }

            hasFailedCurrentItem = false;
            selectedGapIndex = -1;
            isProcessing = false;

            UpdateHUD();
            BuildLetterTiles(items[currentItemIndex]);

            // Audio on load
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWord(items[currentItemIndex].fullWord);

            // Tutorial animation on first 3 items
            if (currentItemIndex < 3 && gameObject.activeInHierarchy)
            {
                StartCoroutine(CoPlayCountbackAnimation());
            }
        }

        private void BuildLetterTiles(TurtleSplitItem item)
        {
            ClearTiles();
            if (tileContainer == null) return;

            string word = item.fullWord;
            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];

                // Letter Tile (Bigger & Bold)
                GameObject tile = CreateLetterTile(c.ToString().ToUpper(), i);
                activeTileGOs.Add(tile);
                letterTileGOs.Add(tile);

                // Gap between letters (only if not the last letter)
                if (i < word.Length - 1)
                {
                    int gapIdx = i + 1;
                    GameObject gap = CreateGapTarget(gapIdx);
                    activeTileGOs.Add(gap);
                    gapTileGOs.Add(gap);
                }
            }
        }

        private GameObject CreateLetterTile(string letter, int index)
        {
            GameObject tileGO = new GameObject($"Tile_{index}_{letter}", typeof(RectTransform), typeof(Image), typeof(Outline));
            tileGO.transform.SetParent(tileContainer, false);

            RectTransform rt = tileGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(105, 135);

            Image img = tileGO.GetComponent<Image>();
            U9_UI_Utils.ApplyRoundedCardStyle(img, new Color(0.98f, 0.99f, 1f));

            Outline outline = tileGO.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0.75f, 0.85f, 0.95f, 0.8f);
                outline.effectDistance = new Vector2(2, -2);
            }

            GameObject textGO = new GameObject("LetterText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(tileGO.transform, false);
            RectTransform txtRt = textGO.GetComponent<RectTransform>();
            txtRt.sizeDelta = rt.sizeDelta;

            TextMeshProUGUI txt = textGO.GetComponent<TextMeshProUGUI>();
            txt.text = $"<b>{letter}</b>";
            txt.fontSize = 72;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.06f, 0.09f, 0.16f); // Deep navy black

            return tileGO;
        }

        private GameObject CreateGapTarget(int gapIndex)
        {
            GameObject gapGO = new GameObject($"Gap_{gapIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            gapGO.transform.SetParent(tileContainer, false);

            RectTransform rt = gapGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(38, 135);

            Image img = gapGO.GetComponent<Image>();
            // Subtle interactive slit indicator
            U9_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.2f, 0.5f, 0.8f, 0.18f));

            // Visible center dashed slit line
            GameObject slitGO = new GameObject("SlitIndicator", typeof(RectTransform), typeof(Image));
            slitGO.transform.SetParent(gapGO.transform, false);
            RectTransform srt = slitGO.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(4, 90);
            Image sImg = slitGO.GetComponent<Image>();
            sImg.color = new Color(0.35f, 0.6f, 0.85f, 0.4f);

            Button btn = gapGO.GetComponent<Button>();
            int capturedIdx = gapIndex;
            btn.onClick.AddListener(() => OnGapSelected(capturedIdx, gapGO.transform));
            activeGapButtons.Add(btn);

            return gapGO;
        }

        private void OnGapSelected(int gapIndex, Transform gapTransform)
        {
            if (isProcessing) return;

            selectedGapIndex = gapIndex;
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayClick();

            // Reset all gap target visuals
            foreach (var g in gapTileGOs)
            {
                if (g != null)
                {
                    var img = g.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.2f, 0.5f, 0.8f, 0.18f);
                }
            }

            // Highlight selected gap
            if (gapTransform != null)
            {
                var curImg = gapTransform.GetComponent<Image>();
                if (curImg != null) curImg.color = new Color(0.14f, 0.65f, 1f, 0.35f);
            }

            // Place or move divider bar
            if (activeDividerBar == null)
            {
                activeDividerBar = new GameObject("DividerBar", typeof(RectTransform), typeof(Image));
                RectTransform rt = activeDividerBar.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(14, 150);
                Image img = activeDividerBar.GetComponent<Image>();
                U9_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.02f, 0.52f, 0.95f)); // Vivid Electric Blue
            }

            activeDividerBar.transform.SetParent(gapTransform, false);
            activeDividerBar.transform.localPosition = Vector3.zero;
            activeDividerBar.SetActive(true);
        }

        public void OnSplitActionTapped()
        {
            if (isProcessing || selectedGapIndex == -1) return;
            isProcessing = true;

            TurtleSplitItem item = items[currentItemIndex];
            bool isCorrect = (selectedGapIndex == item.correctGapIndex);

            if (isCorrect)
            {
                StartCoroutine(CoHandleCorrectSplit(item));
            }
            else
            {
                StartCoroutine(CoHandleWrongSplit(item));
            }
        }

        private IEnumerator CoHandleCorrectSplit(TurtleSplitItem item)
        {
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlaySplitClick();

            if (!hasFailedCurrentItem)
            {
                firstAttemptSuccesses++;
                U9_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(100);
            }

            UpdateHUD();

            // Syllable coloration
            int splitPoint = item.correctGapIndex;
            for (int i = 0; i < letterTileGOs.Count; i++)
            {
                if (letterTileGOs[i] != null)
                {
                    Image img = letterTileGOs[i].GetComponent<Image>();
                    if (img != null)
                    {
                        // First syllable = Soft Sky Blue, Second syllable = Soft Emerald Green
                        img.color = (i < splitPoint) ? new Color(0.73f, 0.90f, 1f) : new Color(0.74f, 0.95f, 0.78f);
                    }
                }
            }

            // Expand the split gap so the split is clearly visible and separates the syllables
            if (selectedGapIndex > 0 && selectedGapIndex <= gapTileGOs.Count)
            {
                GameObject chosenGap = gapTileGOs[selectedGapIndex - 1];
                if (chosenGap != null)
                {
                    RectTransform grt = chosenGap.GetComponent<RectTransform>();
                    if (grt != null) grt.sizeDelta = new Vector2(75, 135);
                    if (activeDividerBar != null)
                    {
                        Image dImg = activeDividerBar.GetComponent<Image>();
                        if (dImg != null) dImg.color = new Color(0.12f, 0.75f, 0.38f); // Emerald Success Divider
                    }
                }
            }

            // Word chunk audio playback
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlaySyllables(item.splitWord);
            }

            yield return new WaitForSeconds(1.4f);

            currentItemIndex++;
            LoadCurrentItem();
        }

        private IEnumerator CoHandleWrongSplit(TurtleSplitItem item)
        {
            hasFailedCurrentItem = true;
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
            U9_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(item.fullWord);

            // Flash divider bar red
            if (activeDividerBar != null)
            {
                Image dImg = activeDividerBar.GetComponent<Image>();
                if (dImg != null) dImg.color = new Color(0.92f, 0.25f, 0.25f);
            }

            yield return new WaitForSeconds(0.5f);

            // Replay countback animation as remediation
            yield return StartCoroutine(CoPlayCountbackAnimation());

            isProcessing = false;
        }

        private IEnumerator CoPlayCountbackAnimation()
        {
            TurtleSplitItem item = items[currentItemIndex];
            string word = item.fullWord;
            int len = word.Length;

            // Highlight 3 letters from the end: len-1 (1st back), len-2 (2nd back), len-3 (3rd back)
            for (int step = 1; step <= 3; step++)
            {
                int letterIdx = len - step;
                if (letterIdx >= 0 && letterIdx < letterTileGOs.Count)
                {
                    GameObject tile = letterTileGOs[letterIdx];
                    if (tile != null)
                    {
                        Image img = tile.GetComponent<Image>();
                        if (img != null) img.color = new Color(1f, 0.88f, 0.35f); // Golden amber pulse
                        U9_SA_AudioManager_Masters_Phonics.Instance?.PlayCountbackTick();
                    }
                }
                yield return new WaitForSeconds(0.45f);
            }

            // Highlight the correct gap target
            int correctGapIdx = item.correctGapIndex;
            if (correctGapIdx > 0 && correctGapIdx <= gapTileGOs.Count)
            {
                GameObject targetGap = gapTileGOs[correctGapIdx - 1];
                if (targetGap != null)
                {
                    Image gImg = targetGap.GetComponent<Image>();
                    if (gImg != null) gImg.color = new Color(0.14f, 0.65f, 1f, 0.6f);
                }
            }
        }

        private void ReplayCurrentWordAudio()
        {
            if (currentItemIndex < items.Count && U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord(items[currentItemIndex].fullWord);
            }
        }

        private void UpdateHUD()
        {
            if (progressText != null)
            {
                progressText.text = $"<b>Item {currentItemIndex + 1} of {items.Count}</b>";
            }

            if (progressBar != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
                progressBar.value = (float)(currentItemIndex + 1) / items.Count;
            }

            if (scoreText != null && U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                scoreText.text = $"<b>Score: {U9_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore()}</b>";
            }
        }

        private void ClearTiles()
        {
            foreach (var go in activeTileGOs)
            {
                if (go != null) Destroy(go);
            }
            activeTileGOs.Clear();
            letterTileGOs.Clear();
            gapTileGOs.Clear();
            activeGapButtons.Clear();
            if (activeDividerBar != null)
            {
                Destroy(activeDividerBar);
                activeDividerBar = null;
            }
        }

        private void CompleteActivity()
        {
            int earnedStars = 1;
            if (firstAttemptSuccesses >= 14) earnedStars = 3;
            else if (firstAttemptSuccesses >= 11) earnedStars = 2;

            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.ShowActivityCompletionDialog(
                    transform, 1, earnedStars, firstAttemptSuccesses * 100, () =>
                    {
                        U9_SA_UnitFlowManager_Masters_Phonics.Instance.OpenActivity2();
                    });
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 9 Turtle Rule] Auto-Assigned Hierarchy Elements & Buttons!</b></color>");
        }
#endif
    }
}
