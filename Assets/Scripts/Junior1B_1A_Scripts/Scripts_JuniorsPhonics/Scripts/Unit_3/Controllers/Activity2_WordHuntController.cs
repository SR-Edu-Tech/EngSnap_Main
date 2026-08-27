using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

    public class Activity2_WordHuntController : MonoBehaviour
    {
        [Header("Grid UI")]
        public Transform gridContainer;
        public GameObject letterTilePrefab;
        public TextMeshProUGUI targetWordText;
        public AudioSource audioSource;
        public AudioClip chimeSFX;
        public AudioClip tryAgainSFX;

        private WordHuntGridData currentGrid;
        private int currentTargetIndex = 0;
        private List<GameObject> spawnedTiles = new List<GameObject>();
        private List<int> selectedIndices = new List<int>();
        private int wrongAttempts = 0;

        public System.Action OnActivityComplete;

        public void Setup(Unit3LevelData levelData)
        {
            currentGrid = levelData.huntGrid;
            currentTargetIndex = 0;
            wrongAttempts = 0;
            BuildGrid();
            PromptNextWord();
        }

        private void BuildGrid()
        {
            foreach (Transform child in gridContainer) Destroy(child.gameObject);
            spawnedTiles.Clear();

            if (currentGrid == null)
            {
                Debug.LogWarning("Activity2_WordHuntController: currentGrid (huntGrid) is null!");
                return;
            }

            GridLayoutGroup layout = gridContainer.GetComponent<GridLayoutGroup>();
            if (layout != null)
            {
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = Mathf.Max(1, currentGrid.columns);
            }

            for (int r = 0; r < currentGrid.rows; r++)
            {
                for (int c = 0; c < currentGrid.columns; c++)
                {
                    int index = r * currentGrid.columns + c;
                    GameObject tile = Instantiate(letterTilePrefab, gridContainer);
                    tile.transform.localScale = Vector3.one;
                    tile.transform.localPosition = Vector3.zero;

                    // Disable drag-and-drop so Activity 2 Word Hunt tiles are strictly TAP-ONLY
                    SpellPictureDragTile dragComp = tile.GetComponent<SpellPictureDragTile>();
                    if (dragComp != null) Destroy(dragComp);

                    char letter = currentGrid.GetLetterAt(r, c);

                    TextMeshProUGUI txt = tile.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null)
                    {
                        txt.text = letter.ToString().ToUpper();
                        txt.fontSize = 44f;
                        txt.fontStyle = FontStyles.Bold;
                        txt.alignment = TextAlignmentOptions.Center;
                    }

                    Image img = GetTileImage(tile);
                    if (img != null)
                    {
                        img.color = Color.white;
                    }

                    U3_WordHuntTilePointer ptr = tile.GetComponent<U3_WordHuntTilePointer>();
                    if (ptr == null) ptr = tile.AddComponent<U3_WordHuntTilePointer>();
                    ptr.tileIndex = index;
                    ptr.controller = this;

                    Button btn = tile.GetComponent<Button>();
                    if (btn == null) btn = tile.GetComponentInChildren<Button>();
                    if (btn != null)
                    {
                        int capturedIndex = index;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnGridTileClicked(capturedIndex));
                    }

                    spawnedTiles.Add(tile);
                }
            }
        }

        private Image GetTileImage(GameObject tile)
        {
            if (tile == null) return null;
            Image img = tile.GetComponent<Image>();
            if (img == null) img = tile.GetComponentInChildren<Image>(true);
            return img;
        }

        private void PromptNextWord()
        {
            selectedIndices.Clear();
            wrongAttempts = 0;
            isPointerDragging = false;
            if (currentTargetIndex < currentGrid.targetWords.Count)
            {
                targetWordText.text = "Find: " + currentGrid.targetWords[currentTargetIndex].ToUpper();
            }
        }

        private bool isPointerDragging = false;

        public void OnTilePointerDown(int index)
        {
            isPointerDragging = true;
            if (!selectedIndices.Contains(index) && selectedIndices.Count < 3)
            {
                OnGridTileClicked(index);
            }
        }

        public void OnTilePointerEnter(int index)
        {
            if (isPointerDragging && !selectedIndices.Contains(index) && selectedIndices.Count < 3)
            {
                OnGridTileClicked(index);
            }
        }

        public void OnTilePointerUp(int index)
        {
            isPointerDragging = false;
            if (selectedIndices.Count == 3)
            {
                CheckSelectedWord();
            }
        }

        public void OnGridTileClicked(int index)
        {
            if (index < 0 || index >= spawnedTiles.Count) return;

            // Prevent selecting disabled/already-matched tiles
            Button tileBtn = spawnedTiles[index].GetComponent<Button>();
            if (tileBtn != null && !tileBtn.interactable) return;

            // Toggle unselect if already selected
            if (selectedIndices.Contains(index))
            {
                selectedIndices.Remove(index);
                Image unselectImg = GetTileImage(spawnedTiles[index]);
                if (unselectImg != null) unselectImg.color = Color.white;
                spawnedTiles[index].transform.localScale = Vector3.one;
                return;
            }

            // Select tile with bold highlight & scale pop
            selectedIndices.Add(index);
            Image selectImg = GetTileImage(spawnedTiles[index]);
            if (selectImg != null) selectImg.color = new Color(1f, 0.85f, 0.1f, 1f); // Vibrant Gold
            spawnedTiles[index].transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            if (selectedIndices.Count == 3) // CVC words are 3 letters
            {
                CheckSelectedWord();
            }
        }

        private void CheckSelectedWord()
        {
            if (currentGrid == null || currentTargetIndex >= currentGrid.targetWords.Count) return;

            string formed = "";
            foreach (int idx in selectedIndices)
            {
                int r = idx / currentGrid.columns;
                int c = idx % currentGrid.columns;
                formed += currentGrid.GetLetterAt(r, c);
            }

            string target = currentGrid.targetWords[currentTargetIndex].ToUpper();

            if (formed == target)
            {
                // Correct selection -> Lock in with vibrant green & normal scale
                if (audioSource != null && chimeSFX != null) audioSource.PlayOneShot(chimeSFX);
                foreach (int idx in selectedIndices)
                {
                    Image img = GetTileImage(spawnedTiles[idx]);
                    if (img != null) img.color = new Color(0.2f, 0.85f, 0.3f, 1f); // Emerald Green
                    spawnedTiles[idx].transform.localScale = Vector3.one;
                    Button btn = spawnedTiles[idx].GetComponent<Button>();
                    if (btn != null) btn.interactable = false;
                }

                selectedIndices.Clear();
                currentTargetIndex++;

                if (currentTargetIndex < currentGrid.targetWords.Count)
                {
                    Invoke(nameof(PromptNextWord), 1.0f);
                }
                else
                {
                    // Activity 2 Complete -> Notify Unit3Manager to advance to Activity 3 / Reward
                    Invoke(nameof(CompleteActivity), 1.2f);
                }
            }
            else
            {
                // Wrong selection -> gentle reset to white & scale 1
                if (audioSource != null && tryAgainSFX != null) audioSource.PlayOneShot(tryAgainSFX);
                wrongAttempts++;
                foreach (int idx in selectedIndices)
                {
                    Image img = GetTileImage(spawnedTiles[idx]);
                    if (img != null) img.color = Color.white;
                    spawnedTiles[idx].transform.localScale = Vector3.one;
                }
                selectedIndices.Clear();

                if (wrongAttempts >= 2)
                {
                    TriggerSoftHint();
                }
            }
        }

        private void CompleteActivity()
        {
            OnActivityComplete?.Invoke();
        }

        private void TriggerSoftHint()
        {
            // Soft hint: gently pulse or tint the grid tile corresponding to the target word's first letter
            Debug.Log("Hint: Look closely at the target word's first sound!");
        }
    }

    public class U3_WordHuntTilePointer : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        public int tileIndex;
        public Activity2_WordHuntController controller;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (controller != null) controller.OnTilePointerDown(tileIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (controller != null) controller.OnTilePointerEnter(tileIndex);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (controller != null) controller.OnTilePointerUp(tileIndex);
        }
    }