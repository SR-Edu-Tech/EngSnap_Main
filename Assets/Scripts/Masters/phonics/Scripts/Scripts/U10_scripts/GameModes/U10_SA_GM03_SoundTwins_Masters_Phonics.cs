using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U10_SA_GM03_SoundTwins_Masters_Phonics : MonoBehaviour
    {
        [Header("Grid & Card Container")]
        [SerializeField] private Transform cardGridContainer;
        [SerializeField] private GameObject cardPrefab;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI instructionText;

        [Header("State Tracking")]
        private List<U10MatchCardItem> activeCardItems = new List<U10MatchCardItem>();
        private List<GameObject> activeCardObjects = new List<GameObject>();
        private List<int> selectedIndices = new List<int>();

        private int currentRound = 1; // 1 = Round A (p.50), 2 = Round B (High Frequency)
        private int matchedGroupsCount = 0;
        private int totalGroupsInRound = 8;
        private int wrongTapsCount = 0;
        private bool isProcessingMatch = false;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;
            U10_UI_Utils.FormatHeaderTypography(root, "SOUND TWINS — HOMOPHONE MATCH", "Tap two words that sound the same");

            if (cardGridContainer == null)
            {
                cardGridContainer = root.Find("GridContainer") 
                                 ?? root.Find("CardsGrid") 
                                 ?? root.Find("Cards") 
                                 ?? root.Find("Content/Grid");
            }

            U10_UI_Utils.EnsureHUD(root, ref progressText, ref scoreText);

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            if (instructionText == null)
            {
                Transform it = root.Find("InstructionText") 
                            ?? root.Find("PromptText") 
                            ?? root.Find("Prompt");
                if (it != null) instructionText = it.GetComponent<TextMeshProUGUI>();
            }

            // Hide redundant RoundBadge and overlapping InstructionText
            Transform rb = root.Find("RoundBadge");
            if (rb != null) rb.gameObject.SetActive(false);

            Transform instr = root.Find("InstructionText");
            if (instr != null) instr.gameObject.SetActive(false);
        }

        public void StartActivity()
        {
            currentRound = 1;
            wrongTapsCount = 0;
            LoadRound(currentRound);
        }

        private void LoadRound(int round)
        {
            currentRound = round;
            matchedGroupsCount = 0;
            selectedIndices.Clear();
            isProcessingMatch = false;

            activeCardItems = (round == 1) ? U10_DataBank.GetSoundTwinsRoundA() : U10_DataBank.GetSoundTwinsRoundB();
            totalGroupsInRound = (round == 1) ? 8 : 7; // Round B has two 3-way sets and five 2-way sets

            // Shuffle display order
            List<int> shuffledIndices = new List<int>();
            for (int i = 0; i < activeCardItems.Count; i++) shuffledIndices.Add(i);
            ShuffleList(shuffledIndices);

            List<U10MatchCardItem> shuffledItems = new List<U10MatchCardItem>();
            foreach (int idx in shuffledIndices) shuffledItems.Add(activeCardItems[idx]);
            activeCardItems = shuffledItems;

            UpdateHUD();
            BuildCardsGrid();

            if (round == 1)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a1_intro");
            }
            else
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a1_rb");
            }
        }

private void BuildCardsGrid()
{
    if (cardGridContainer == null) return;

    activeCardObjects.Clear();
    int childCount = cardGridContainer.childCount;

    // Reuse pre-existing Editor cards if present
    if (childCount >= activeCardItems.Count)
    {
        for (int i = 0; i < childCount; i++)
        {
            GameObject cardGo = cardGridContainer.GetChild(i).gameObject;

            if (i < activeCardItems.Count)
            {
                cardGo.SetActive(true);
                U10MatchCardItem item = activeCardItems[i];

                // Update text without destroying user font/styling
                var txt = cardGo.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    txt.text = $"<b>{item.word}</b>";
                    txt.color = new Color(0.08f, 0.15f, 0.28f);
                }

                // Ensure button listener is wired
                int index = i;
                Button btn = cardGo.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnCardTapped(index));
                }

                // Reset card visual state
                var img = cardGo.GetComponent<Image>();
                if (img != null) img.color = Color.white;

                // Reset outline if present
                Transform outline = cardGo.transform.Find("SelectedOutline");
                if (outline != null) outline.gameObject.SetActive(false);

                activeCardObjects.Add(cardGo);
            }
            else
            {
                // Hide any extra cards not needed for this round
                cardGo.SetActive(false);
            }
        }
    }
    else
    {
        // Fallback only if no pre-built cards exist in the hierarchy
        foreach (Transform child in cardGridContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < activeCardItems.Count; i++)
        {
            U10MatchCardItem item = activeCardItems[i];
            GameObject cardObj = CreateCardButton(cardGridContainer, item.word, i);
            activeCardObjects.Add(cardObj);
        }
    }
}

        private GameObject CreateCardButton(Transform parent, string word, int index)
        {
            GameObject cardGo = new GameObject($"Card_{index}_{word}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGo.transform.SetParent(parent, false);
            RectTransform rt = cardGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(175, 76);
            U10_UI_Utils.ApplyRoundedButtonStyle(cardGo.GetComponent<Image>(), new Color(0.96f, 0.98f, 1f));

            GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(cardGo.transform, false);
            txtGO.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = $"<b>{word}</b>";
            txt.fontSize = 28;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.08f, 0.15f, 0.28f);

            Button btn = cardGo.GetComponent<Button>();
            btn.onClick.AddListener(() => OnCardTapped(index));

            return cardGo;
        }

        private void OnCardTapped(int index)
        {
            if (isProcessingMatch || index < 0 || index >= activeCardItems.Count) return;
            if (selectedIndices.Contains(index)) return;

            U10MatchCardItem tappedItem = activeCardItems[index];
            GameObject tappedObj = activeCardObjects[index];

            // 1. Play Word Audio
            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayWord(tappedItem.word);
            StartCoroutine(CoPunchTransform(tappedObj.transform));

            // 2. If a selection is already started, validate group compatibility immediately
            //    (player tapped a card from a DIFFERENT group → wrong, flash red and reset)
            if (selectedIndices.Count > 0)
            {
                int firstGroupId = activeCardItems[selectedIndices[0]].matchGroupId;
                if (tappedItem.matchGroupId != firstGroupId)
                {
                    // Flash tapped card red along with already-selected cards
                    SetCardVisual(tappedObj, new Color(0.85f, 0.25f, 0.25f), false);
                    foreach (int sel in selectedIndices)
                        SetCardVisual(activeCardObjects[sel], new Color(0.85f, 0.25f, 0.25f), false);

                    wrongTapsCount++;
                    U10_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
                    U10_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(tappedItem.word, currentRound == 2);

                    // Snapshot the selection before clearing, so coroutine can reset card visuals
                    List<int> snapshot = new List<int>(selectedIndices);
                    selectedIndices.Clear();
                    isProcessingMatch = true;
                    StartCoroutine(CoResetCardsAfterDelay(snapshot, tappedObj));
                    return;
                }
            }

            // 3. Highlight Selection (cyan)
            selectedIndices.Add(index);
            SetCardVisual(tappedObj, new Color(0.12f, 0.55f, 0.85f), true);

            // 4. Determine required group size from the FIRST selected card (not current)
            int requiredCount = activeCardItems[selectedIndices[0]].groupSize;

            // In Round B, prompt when 2 of a 3-way set are selected
            if (requiredCount == 3 && selectedIndices.Count == 2)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a1_three");
            }

            if (selectedIndices.Count == requiredCount)
            {
                StartCoroutine(CoValidateSelectedGroup());
            }
        }

        private IEnumerator CoResetCardsAfterDelay(List<int> previousSelection, GameObject wrongCard)
        {
            yield return new WaitForSeconds(0.6f);

            // Reset previously selected cards
            foreach (int idx in previousSelection)
            {
                if (idx >= 0 && idx < activeCardObjects.Count)
                {
                    SetCardVisual(activeCardObjects[idx], Color.white, false);
                    var txt = activeCardObjects[idx].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.color = new Color(0.08f, 0.15f, 0.28f);
                }
            }

            // Reset wrongly tapped card
            if (wrongCard != null)
            {
                SetCardVisual(wrongCard, Color.white, false);
                var txt = wrongCard.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null) txt.color = new Color(0.08f, 0.15f, 0.28f);
            }

            selectedIndices.Clear();
            isProcessingMatch = false;
        }


        private IEnumerator CoValidateSelectedGroup()
        {
            isProcessingMatch = true;

            // Check if all selected indices share the same matchGroupId
            int targetGroupId = activeCardItems[selectedIndices[0]].matchGroupId;
            bool isMatch = true;
            foreach (int idx in selectedIndices)
            {
                if (activeCardItems[idx].matchGroupId != targetGroupId)
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
            {
                // Correct Match
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayTwinMatch();
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(100);
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.IncrementWordsPaired(selectedIndices.Count);
                matchedGroupsCount++;
                UpdateHUD();

                foreach (int idx in selectedIndices)
                {
                    GameObject go = activeCardObjects[idx];
                    SetCardVisual(go, new Color(0.18f, 0.75f, 0.42f), false); // Green rounded card, outline OFF!
                    var txt = go.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.color = Color.white;
                }

                yield return new WaitForSeconds(0.4f);

                // Lift & Disable Matched Cards
                foreach (int idx in selectedIndices)
                {
                    GameObject go = activeCardObjects[idx];
                    var btn = go.GetComponent<Button>();
                    if (btn != null) btn.interactable = false;
                    var img = go.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.18f, 0.75f, 0.42f, 0.7f); // Soft rounded green
                    var txt = go.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.color = Color.white;
                }

                if (matchedGroupsCount >= totalGroupsInRound)
                {
                    yield return new WaitForSeconds(0.6f);
                    OnRoundCompleted();
                }
            }
            else
            {
                // Wrong Match
                wrongTapsCount++;
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(activeCardItems[selectedIndices[0]].word, currentRound == 2);

                foreach (int idx in selectedIndices)
                {
                    GameObject go = activeCardObjects[idx];
                    SetCardVisual(go, new Color(0.92f, 0.3f, 0.3f), false); // Soft red, outline off
                    var txt = go.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.color = Color.white;
                }

                yield return new WaitForSeconds(0.6f);

                // Reset Visuals
                foreach (int idx in selectedIndices)
                {
                    GameObject go = activeCardObjects[idx];
                    SetCardVisual(go, Color.white, false);
                    var txt = go.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.color = new Color(0.08f, 0.15f, 0.28f);
                }
            }

            selectedIndices.Clear();
            isProcessingMatch = false;
        }

        private void SetCardVisual(GameObject cardObj, Color color, bool selected)
        {
            if (cardObj == null) return;
            var img = cardObj.GetComponent<Image>();
            if (img != null) img.color = color;

            Transform outline = cardObj.transform.Find("SelectedOutline");
            if (outline != null)
            {
                var oImg = outline.GetComponent<Image>();
                if (oImg != null && img != null)
                {
                    // Copy card's rounded sprite so the outline itself is rounded!
                    if (oImg.sprite != img.sprite)
                    {
                        oImg.sprite = img.sprite;
                        oImg.type = Image.Type.Sliced;
                    }
                    oImg.color = new Color(0.12f, 0.55f, 0.85f, 0.9f);
                }
                outline.gameObject.SetActive(selected);
            }
        }

        private void OnRoundCompleted()
        {
            if (currentRound == 1)
            {
                // Advance to Round B
                LoadRound(2);
            }
            else
            {
                // Activity Complete! Calculate Stars
                int stars = 3;
                if (wrongTapsCount > 6) stars = 1;
                else if (wrongTapsCount > 3) stars = 2;

                int points = stars * 150;
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.ShowActivityCompletionDialog(stars, points);
            }
        }

        private void UpdateHUD()
        {
            string progressStr = $"<b>Round {currentRound} · Pairs: {matchedGroupsCount}/{totalGroupsInRound}</b>";
            if (progressText != null)
            {
                progressText.text = progressStr;
            }

            var allTMP = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var pt in allTMP)
            {
                if (pt != null && (pt.name.Equals("ProgressText", StringComparison.OrdinalIgnoreCase) || 
                                   pt.name.Equals("Progress_Text", StringComparison.OrdinalIgnoreCase)))
                {
                    pt.text = progressStr;
                }
            }

            if (progressBar != null)
            {
                progressBar.value = (float)matchedGroupsCount / totalGroupsInRound;
            }

            if (scoreText != null)
            {
                int sc = U10_SA_UnitFlowManager_Masters_Phonics.Instance != null 
                    ? U10_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore() 
                    : 0;
                scoreText.text = $"<b>Score: {sc}</b>";
            }
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        private IEnumerator CoPunchTransform(Transform target)
        {
            if (target == null) yield break;
            Vector3 orig = Vector3.one;
            target.localScale = orig * 1.15f;
            yield return new WaitForSeconds(0.12f);
            if (target != null) target.localScale = orig;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 10 Sound Twins] Auto-Assigned Hierarchy & Grid!</b></color>");
        }
#endif
    }
}
