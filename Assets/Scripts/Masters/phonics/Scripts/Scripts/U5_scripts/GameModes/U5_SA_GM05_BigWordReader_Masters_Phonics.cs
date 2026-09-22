using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U5_SA_GM05_BigWordReader_Masters_Phonics : MonoBehaviour
    {
        [Header("UI - Word Assembly Slots")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject slotPrefab;

        [Header("UI - Syllable Bank Tiles")]
        [SerializeField] private Transform bankContainer;
        [SerializeField] private GameObject tilePrefab;

        [Header("UI - Word Showcase & Analysis")]
        [SerializeField] private GameObject showcasePanel;
        [SerializeField] private TextMeshProUGUI fullWordText;
        [SerializeField] private TextMeshProUGUI syllableBreakdownText;
        [SerializeField] private TextMeshProUGUI doorAnalysisText;
        [SerializeField] private Image doorTypeIcon;

        [Header("UI - HUD & Controls")]
        [SerializeField] private TextMeshProUGUI roundTitleText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Button replayWholeWordBtn;
        [SerializeField] private Button clearBtn;
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Sprites")]
        [SerializeField] private Sprite openDoorSprite;
        [SerializeField] private Sprite closedDoorSprite;
        [SerializeField] private Sprite defaultTileSprite;
        [SerializeField] private Sprite placedTileSprite;
        [SerializeField] private Sprite correctTileSprite;

        [Header("Colors")]
        [SerializeField] private Color openColor = new Color(0.2f, 0.7f, 1f, 1f);
        [SerializeField] private Color closedColor = new Color(1f, 0.55f, 0.2f, 1f);

        [Header("Items Pool")]
        [SerializeField] private List<BigWordChunkItem> allWords = new List<BigWordChunkItem>();

        // State
        private int currentWordIndex = 0;
        private int currentRound = 1;
        private int totalScore = 0;
        private bool isProcessing = false;

        private List<BigWordChunkItem> sessionWords = new List<BigWordChunkItem>();
        private List<SyllableTileView> bankTiles = new List<SyllableTileView>();
        private List<SyllableSlotView> targetSlots = new List<SyllableSlotView>();

        private void Awake()
        {
            if (replayWholeWordBtn != null)
                replayWholeWordBtn.onClick.AddListener(ReplayWholeWordAudio);

            if (clearBtn != null)
                clearBtn.onClick.AddListener(ClearAllPlacedTiles);

            BuildDefaultWordsPool();
        }

        private void OnEnable()
        {
            StartActivity();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            currentWordIndex = 0;
            currentRound = 1;
            totalScore = 0;
            isProcessing = false;

            sessionWords = new List<BigWordChunkItem>(allWords);
            UpdateHUD();
            LoadCurrentWord();
        }

        private void LoadCurrentWord()
        {
            if (currentWordIndex >= sessionWords.Count)
            {
                FinishActivity();
                return;
            }

            isProcessing = false;
            BigWordChunkItem currentItem = sessionWords[currentWordIndex];

            // Round tracking (Items 0-5: Round 1 (2-syllables), Items 6-11: Round 2 (3-syllables))
            currentRound = (currentWordIndex < 6) ? 1 : 2;

            if (roundTitleText != null)
                roundTitleText.text = (currentRound == 1) ? "Round 1: 2-Syllable Words" : "Round 2: 3-Syllable Big Words";

            if (showcasePanel != null)
                showcasePanel.SetActive(false);

            BuildSlotsAndTiles(currentItem);
            UpdateHUD();

            // Play word prompt
            PlayWholeWordAudio();
        }

        private void BuildSlotsAndTiles(BigWordChunkItem item)
        {
            // Clear previous slots
            if (slotContainer != null)
            {
                foreach (Transform child in slotContainer)
                    Destroy(child.gameObject);
            }
            targetSlots.Clear();

            // Clear previous bank tiles
            if (bankContainer != null)
            {
                foreach (Transform child in bankContainer)
                    Destroy(child.gameObject);
            }
            bankTiles.Clear();

            // Create target slots
            for (int i = 0; i < item.chunks.Length; i++)
            {
                GameObject slotObj = null;
                if (slotPrefab != null)
                    slotObj = Instantiate(slotPrefab, slotContainer);
                else
                    slotObj = CreateDefaultSlotObject(i);

                SyllableSlotView slotView = slotObj.GetComponent<SyllableSlotView>();
                if (slotView == null) slotView = slotObj.AddComponent<SyllableSlotView>();

                slotView.Init(i, this);
                targetSlots.Add(slotView);
            }

            // Create scrambled bank tiles
            List<string> scrambledChunks = new List<string>(item.chunks);
            ShuffleList(scrambledChunks);
            // Ensure not identical to target order if length > 1
            if (scrambledChunks.Count > 1 && IsInSameOrder(scrambledChunks, item.chunks))
            {
                string temp = scrambledChunks[0];
                scrambledChunks[0] = scrambledChunks[1];
                scrambledChunks[1] = temp;
            }

            for (int i = 0; i < scrambledChunks.Count; i++)
            {
                GameObject tileObj = null;
                if (tilePrefab != null)
                    tileObj = Instantiate(tilePrefab, bankContainer);
                else
                    tileObj = CreateDefaultTileObject(scrambledChunks[i]);

                SyllableTileView tileView = tileObj.GetComponent<SyllableTileView>();
                if (tileView == null) tileView = tileObj.AddComponent<SyllableTileView>();

                tileView.Init(scrambledChunks[i], this);
                bankTiles.Add(tileView);
            }
        }

        private bool IsInSameOrder(List<string> a, string[] b)
        {
            if (a.Count != b.Length) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

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
                    else if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
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

            Vector4 border = new Vector4(radius, radius, radius, radius);
            proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return proceduralRoundedSprite;
        }

        private GameObject CreateDefaultSlotObject(int index)
        {
            GameObject slotObj = new GameObject($"Slot_{index}", typeof(RectTransform), typeof(Image));
            slotObj.transform.SetParent(slotContainer, false);

            Image img = slotObj.GetComponent<Image>();
            img.sprite = placedTileSprite != null ? placedTileSprite : GetOrCreateRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.85f, 0.89f, 0.96f, 1f);

            RectTransform rt = slotObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 110);

            return slotObj;
        }

        private GameObject CreateDefaultTileObject(string chunk)
        {
            GameObject tileObj = new GameObject($"Tile_{chunk}", typeof(RectTransform), typeof(Image), typeof(Button));
            tileObj.transform.SetParent(bankContainer, false);

            Image img = tileObj.GetComponent<Image>();
            img.sprite = defaultTileSprite != null ? defaultTileSprite : GetOrCreateRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.24f, 0.48f, 0.92f, 1f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(tileObj.transform, false);
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = chunk;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 44;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            RectTransform rt = tileObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(175, 105);

            return tileObj;
        }

        // -------------------------------------------------------------
        // Tile Interactions
        // -------------------------------------------------------------
        public void OnTileClicked(SyllableTileView tile)
        {
            if (isProcessing) return;

            // Play chunk audio
            PlayChunkAudio(tile.ChunkText);

            if (tile.PlacedSlot == null)
            {
                // Move from bank to first free slot
                SyllableSlotView freeSlot = targetSlots.Find(s => s.OccupyingTile == null);
                if (freeSlot != null)
                {
                    freeSlot.SetTile(tile);
                    tile.PlacedSlot = freeSlot;
                    tile.transform.SetParent(freeSlot.transform);
                    tile.transform.localPosition = Vector3.zero;

                    CheckAssemblyCompletion();
                }
            }
            else
            {
                // Return from slot to bank
                tile.PlacedSlot.ClearTile();
                tile.PlacedSlot = null;
                tile.transform.SetParent(bankContainer);
                tile.transform.localPosition = Vector3.zero;
            }
        }

        public void ClearAllPlacedTiles()
        {
            if (isProcessing) return;

            foreach (var tile in bankTiles)
            {
                if (tile.PlacedSlot != null)
                {
                    tile.PlacedSlot.ClearTile();
                    tile.PlacedSlot = null;
                    tile.transform.SetParent(bankContainer);
                    tile.transform.localPosition = Vector3.zero;
                }
            }
        }

        private void CheckAssemblyCompletion()
        {
            // Verify if all slots filled
            foreach (var slot in targetSlots)
            {
                if (slot.OccupyingTile == null) return;
            }

            // All slots filled -> evaluate
            StartCoroutine(EvaluateAssemblyRoutine());
        }

        private IEnumerator EvaluateAssemblyRoutine()
        {
            isProcessing = true;
            BigWordChunkItem currentItem = sessionWords[currentWordIndex];

            bool isCorrect = true;
            for (int i = 0; i < currentItem.chunks.Length; i++)
            {
                if (targetSlots[i].OccupyingTile.ChunkText != currentItem.chunks[i])
                {
                    isCorrect = false;
                    break;
                }
            }

            if (isCorrect)
            {
                totalScore += 30;
                if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U5_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(30);

                U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                // Highlight tiles green
                foreach (var slot in targetSlots)
                {
                    if (slot.OccupyingTile != null)
                        slot.OccupyingTile.SetHighlightColor(new Color(0.18f, 0.8f, 0.44f, 1f));
                }

                // Show showcase panel with door analysis
                if (showcasePanel != null)
                {
                    showcasePanel.SetActive(true);
                    if (fullWordText != null) fullWordText.text = currentItem.word.ToUpper();
                    if (syllableBreakdownText != null) syllableBreakdownText.text = string.Join(" - ", currentItem.chunks);

                    if (doorAnalysisText != null)
                    {
                        string doorStr = (currentItem.firstDoorType == SyllableDoorType.Open) ? "Open Syllable (Long Sound)" : "Closed Syllable (Short Sound)";
                        doorAnalysisText.text = $"First syllable <b>{currentItem.chunks[0]}</b> is <color=#{ColorUtility.ToHtmlStringRGB(currentItem.firstDoorType == SyllableDoorType.Open ? openColor : closedColor)}>{doorStr}</color>!";
                    }

                    if (doorTypeIcon != null)
                    {
                        doorTypeIcon.sprite = (currentItem.firstDoorType == SyllableDoorType.Open) ? openDoorSprite : closedDoorSprite;
                        doorTypeIcon.gameObject.SetActive(true);
                    }
                }

                // Play whole word audio
                PlayWholeWordAudio();

                yield return new WaitForSeconds(2.0f);

                currentWordIndex++;
                LoadCurrentWord();
            }
            else
            {
                // Play spoken audio of the wrong assembled syllables sequence
                for (int i = 0; i < targetSlots.Count; i++)
                {
                    if (targetSlots[i].OccupyingTile != null)
                    {
                        PlayChunkAudio(targetSlots[i].OccupyingTile.ChunkText);
                        yield return new WaitForSeconds(0.4f);
                    }
                }

                U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");

                // Shake slots
                Vector3 origPos = slotContainer.localPosition;
                for (int i = 0; i < 3; i++)
                {
                    slotContainer.localPosition = origPos + new Vector3(-15f, 0, 0);
                    yield return new WaitForSeconds(0.04f);
                    slotContainer.localPosition = origPos + new Vector3(15f, 0, 0);
                    yield return new WaitForSeconds(0.04f);
                }
                slotContainer.localPosition = origPos;

                yield return new WaitForSeconds(0.4f);
                ClearAllPlacedTiles();
                isProcessing = false;
            }
        }

        private void PlayChunkAudio(string chunk)
        {
            if (string.IsNullOrEmpty(chunk)) return;
            U5_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(chunk);
        }

        public void ReplayWholeWordAudio()
        {
            if (currentWordIndex < sessionWords.Count)
            {
                PlayWholeWordAudio();
                if (replayWholeWordBtn != null)
                    StartCoroutine(PunchScale(replayWholeWordBtn.transform, 1.15f, 0.15f));
            }
        }

        private void PlayWholeWordAudio()
        {
            if (currentWordIndex >= sessionWords.Count) return;

            BigWordChunkItem item = sessionWords[currentWordIndex];
            if (item.wholeWordAudio != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(item.wholeWordAudio);
            }
            else
            {
                U5_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(item.word);
            }
        }

        private void UpdateHUD()
        {
            if (progressText != null)
                progressText.text = $"{Mathf.Min(currentWordIndex + 1, sessionWords.Count)} / {sessionWords.Count}";

            if (progressBar != null && sessionWords.Count > 0)
                progressBar.value = (float)currentWordIndex / sessionWords.Count;

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
                    feedbackText.text = $"Activity 5 Complete!\nBig Word Reader Mastered!\nScore: {totalScore}";
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

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        private void BuildDefaultWordsPool()
        {
            if (allWords != null && allWords.Count > 0) return;

            allWords = new List<BigWordChunkItem>()
            {
                // Round 1 (2-syllables)
                new BigWordChunkItem { word = "bacon", chunks = new string[] { "ba", "con" }, roundIndex = 1, firstDoorType = SyllableDoorType.Open },
                new BigWordChunkItem { word = "basket", chunks = new string[] { "bas", "ket" }, roundIndex = 1, firstDoorType = SyllableDoorType.Closed },
                new BigWordChunkItem { word = "hotel", chunks = new string[] { "ho", "tel" }, roundIndex = 1, firstDoorType = SyllableDoorType.Open },
                new BigWordChunkItem { word = "napkin", chunks = new string[] { "nap", "kin" }, roundIndex = 1, firstDoorType = SyllableDoorType.Closed },
                new BigWordChunkItem { word = "silent", chunks = new string[] { "si", "lent" }, roundIndex = 1, firstDoorType = SyllableDoorType.Open },
                new BigWordChunkItem { word = "velvet", chunks = new string[] { "vel", "vet" }, roundIndex = 1, firstDoorType = SyllableDoorType.Closed },

                // Round 2 (3-syllables)
                new BigWordChunkItem { word = "dinosaur", chunks = new string[] { "di", "no", "saur" }, roundIndex = 2, firstDoorType = SyllableDoorType.Open },
                new BigWordChunkItem { word = "crocodile", chunks = new string[] { "croc", "o", "dile" }, roundIndex = 2, firstDoorType = SyllableDoorType.Closed },
                new BigWordChunkItem { word = "elephant", chunks = new string[] { "el", "e", "phant" }, roundIndex = 2, firstDoorType = SyllableDoorType.Closed },
                new BigWordChunkItem { word = "octopus", chunks = new string[] { "oc", "to", "pus" }, roundIndex = 2, firstDoorType = SyllableDoorType.Closed },
                new BigWordChunkItem { word = "equator", chunks = new string[] { "e", "qua", "tor" }, roundIndex = 2, firstDoorType = SyllableDoorType.Open },
                new BigWordChunkItem { word = "volcano", chunks = new string[] { "vol", "ca", "no" }, roundIndex = 2, firstDoorType = SyllableDoorType.Closed }
            };
        }
    }

    // -------------------------------------------------------------
    // Helper Syllable Slot & Tile Components
    // -------------------------------------------------------------
    public class SyllableSlotView : MonoBehaviour
    {
        public int SlotIndex { get; private set; }
        public SyllableTileView OccupyingTile { get; private set; }
        private U5_SA_GM05_BigWordReader_Masters_Phonics parentGame;
        private Image bgImage;

        public void Init(int index, U5_SA_GM05_BigWordReader_Masters_Phonics game)
        {
            SlotIndex = index;
            parentGame = game;
            OccupyingTile = null;

            bgImage = GetComponent<Image>();
            if (bgImage != null && bgImage.sprite == null)
            {
                bgImage.sprite = U5_SA_GM05_BigWordReader_Masters_Phonics.GetOrCreateRoundedSprite();
                bgImage.type = Image.Type.Sliced;
                bgImage.color = new Color(0.85f, 0.89f, 0.96f, 1f);
            }
        }

        public void SetTile(SyllableTileView tile)
        {
            OccupyingTile = tile;
        }

        public void ClearTile()
        {
            OccupyingTile = null;
        }
    }

    public class SyllableTileView : MonoBehaviour
    {
        public string ChunkText { get; private set; }
        public SyllableSlotView PlacedSlot { get; set; }

        private TextMeshProUGUI chunkTMP;
        private Image bgImage;
        private U5_SA_GM05_BigWordReader_Masters_Phonics parentGame;

        public void Init(string chunk, U5_SA_GM05_BigWordReader_Masters_Phonics game)
        {
            ChunkText = chunk;
            parentGame = game;
            PlacedSlot = null;

            chunkTMP = GetComponentInChildren<TextMeshProUGUI>();
            bgImage = GetComponent<Image>();

            if (bgImage != null && bgImage.sprite == null)
            {
                bgImage.sprite = U5_SA_GM05_BigWordReader_Masters_Phonics.GetOrCreateRoundedSprite();
                bgImage.type = Image.Type.Sliced;
                bgImage.color = new Color(0.24f, 0.48f, 0.92f, 1f);
            }

            if (chunkTMP != null)
            {
                chunkTMP.text = chunk;
                chunkTMP.fontSize = 44;
                chunkTMP.fontStyle = FontStyles.Bold;
                chunkTMP.color = Color.white;
            }

            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnClick);
            }
        }

        public void SetHighlightColor(Color col)
        {
            if (bgImage != null)
                bgImage.color = col;
        }

        private void OnClick()
        {
            parentGame?.OnTileClicked(this);
        }
    }
}
