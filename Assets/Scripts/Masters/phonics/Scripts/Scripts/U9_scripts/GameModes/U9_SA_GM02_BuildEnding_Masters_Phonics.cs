using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U9_SA_GM02_BuildEnding_Masters_Phonics : MonoBehaviour
    {
        [Header("Base Display Slot")]
        [SerializeField] private TextMeshProUGUI baseChunkText;
        [SerializeField] private TextMeshProUGUI endingSlotText;
        [SerializeField] private TextMeshProUGUI resultWordText;

        [Header("8 Ending Buttons (ble, dle, gle, tle, cle, fle, ple, zle)")]
        [SerializeField] private Button bleBtn;
        [SerializeField] private Button dleBtn;
        [SerializeField] private Button gleBtn;
        [SerializeField] private Button tleBtn;
        [SerializeField] private Button cleBtn;
        [SerializeField] private Button fleBtn;
        [SerializeField] private Button pleBtn;
        [SerializeField] private Button zleBtn;
        public Button[] allEndingButtons;

        [Header("Doubling Switch")]
        [SerializeField] private TextMeshProUGUI toggleLabelText; // "Double the Letter?"
        [SerializeField] private Button doubleToggleBtn;
        [SerializeField] private Image doubleToggleKnobImg;
        [SerializeField] private TextMeshProUGUI doubleToggleStatusText; // "NO" or "YES"
        [SerializeField] private Sprite switchOnSprite;
        [SerializeField] private Sprite switchOffSprite;
        [SerializeField] private Color toggleOnColor = new Color(0.12f, 0.75f, 0.38f);
        [SerializeField] private Color toggleOffColor = new Color(0.4f, 0.45f, 0.55f);

        [Header("Action Buttons")]
        [SerializeField] private Button buildActionBtn;
        [SerializeField] private Button replayAudioBtn;

        [Header("Instruction / Prompt")]
        [SerializeField] private TextMeshProUGUI promptInstructionText;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        private List<BuildEndingItem> items = new List<BuildEndingItem>();
        private int currentItemIndex = 0;
        private string selectedEnding = "";
        private bool isDoublingEnabled = false;
        private int firstAttemptSuccesses = 0;
        private bool hasFailedCurrentItem = false;
        private bool isProcessing = false;
        private bool hasPlayedRoundBNotice = false;

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
            U9_UI_Utils.FormatHeaderTypography(root, "ACTIVITY 3: BUILD THE ENDING", "Doubling & -le Suffixes");

            if (promptInstructionText == null)
            {
                Transform pt = root.Find("InstructionText") ?? root.Find("PromptText") ?? root.Find("Prompt") ?? root.Find("Header/PromptText");
                if (pt != null) promptInstructionText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (promptInstructionText != null)
            {
                promptInstructionText.text = "<b>Choose the -le ending and toggle the Doubling Switch if needed!</b>";
                promptInstructionText.fontSize = 24;
                promptInstructionText.alignment = TextAlignmentOptions.Center;
                promptInstructionText.color = new Color(0.85f, 0.92f, 1f);
            }

            if (baseChunkText == null)
            {
                Transform b = root.Find("BaseSlot/BaseText") ?? root.Find("BaseText") ?? root.Find("BaseSlot");
                if (b != null) baseChunkText = b.GetComponent<TextMeshProUGUI>() ?? b.GetComponentInChildren<TextMeshProUGUI>(true);
                Transform bs = root.Find("BaseSlot");
                if (bs != null && bs.GetComponent<Image>() != null) U9_UI_Utils.ApplyRoundedCardStyle(bs.GetComponent<Image>(), new Color(0.9f, 0.95f, 1f));
            }

            if (endingSlotText == null)
            {
                Transform e = root.Find("EndingSlot/EndingText") ?? root.Find("EndingText") ?? root.Find("EndingSlot");
                if (e != null) endingSlotText = e.GetComponent<TextMeshProUGUI>() ?? e.GetComponentInChildren<TextMeshProUGUI>(true);
                Transform es = root.Find("EndingSlot");
                if (es != null && es.GetComponent<Image>() != null) U9_UI_Utils.ApplyRoundedCardStyle(es.GetComponent<Image>(), new Color(0.9f, 0.95f, 1f));
            }

            if (resultWordText == null)
            {
                Transform r = root.Find("ResultText") ?? root.Find("WordPreviewText") ?? root.Find("FinalWordText");
                if (r != null) resultWordText = r.GetComponent<TextMeshProUGUI>();
            }

            // Bind Endings Tray
            Transform tray = root.Find("EndingsTray") ?? root.Find("EndingButtons") ?? root;
            if (bleBtn == null) bleBtn = FindButton(tray, "Btn_ble", "ble", "bleBtn");
            if (dleBtn == null) dleBtn = FindButton(tray, "Btn_dle", "dle", "dleBtn");
            if (gleBtn == null) gleBtn = FindButton(tray, "Btn_gle", "gle", "gleBtn");
            if (tleBtn == null) tleBtn = FindButton(tray, "Btn_tle", "tle", "tleBtn");
            if (cleBtn == null) cleBtn = FindButton(tray, "Btn_cle", "cle", "cleBtn");
            if (fleBtn == null) fleBtn = FindButton(tray, "Btn_fle", "fle", "fleBtn");
            if (pleBtn == null) pleBtn = FindButton(tray, "Btn_ple", "ple", "pleBtn");
            if (zleBtn == null) zleBtn = FindButton(tray, "Btn_zle", "zle", "zleBtn");

            allEndingButtons = new Button[] { bleBtn, dleBtn, gleBtn, tleBtn, cleBtn, fleBtn, pleBtn, zleBtn };
            foreach (var btn in allEndingButtons)
            {
                if (btn != null && btn.GetComponent<Image>() != null)
                {
                    U9_UI_Utils.ApplyRoundedButtonStyle(btn.GetComponent<Image>(), new Color(0.15f, 0.2f, 0.3f));
                }
            }

            // Bind Doubling Switch
            if (doubleToggleBtn == null)
            {
                doubleToggleBtn = FindButton(root, "DoublingSwitch", "DoubleToggleBtn", "ToggleBtn", "DoublingBtn");
            }

            if (toggleLabelText == null)
            {
                Transform tl = root.Find("DoublingLabel") ?? root.Find("ToggleLabel") ?? root.Find("DoublingSwitch_Label") ?? root.Find("DoublingSwitch/Label");
                if (tl != null) toggleLabelText = tl.GetComponent<TextMeshProUGUI>();
            }

            if (toggleLabelText != null)
            {
                toggleLabelText.text = "<b>Double the Letter?</b>";
                toggleLabelText.fontSize = 20;
                toggleLabelText.alignment = TextAlignmentOptions.Center;
                toggleLabelText.color = new Color(0.85f, 0.92f, 1f);
            }

            EnsureSwitchSprites();

            if (doubleToggleBtn != null)
            {
                if (doubleToggleKnobImg == null) doubleToggleKnobImg = doubleToggleBtn.GetComponent<Image>();
                if (doubleToggleKnobImg != null)
                {
                    if (switchOffSprite != null)
                    {
                        doubleToggleKnobImg.sprite = switchOffSprite;
                        doubleToggleKnobImg.color = Color.white;
                        doubleToggleKnobImg.preserveAspect = true;
                    }
                    else
                    {
                        U9_UI_Utils.ApplyRoundedButtonStyle(doubleToggleKnobImg, new Color(0.4f, 0.45f, 0.55f));
                    }
                }
                if (doubleToggleStatusText == null) doubleToggleStatusText = doubleToggleBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (buildActionBtn == null)
            {
                buildActionBtn = FindButton(root, "BuildBtn", "Build_Btn", "ActionBtn", "Btn_Build");
            }
            if (buildActionBtn != null && buildActionBtn.GetComponent<Image>() != null)
            {
                U9_UI_Utils.ApplyRoundedButtonStyle(buildActionBtn.GetComponent<Image>(), new Color(0.12f, 0.75f, 0.38f));
            }

            if (replayAudioBtn == null)
            {
                replayAudioBtn = FindButton(root, "ReplayBtn", "Speaker_Button", "AudioBtn");
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

            AttachListeners();
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"Content/{name}") 
                           ?? root.Find($"EndingsTray/{name}") 
                           ?? root.Find($"HUD/{name}") 
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

        private void AttachListeners()
        {
            AttachEndingButton(bleBtn, "ble");
            AttachEndingButton(dleBtn, "dle");
            AttachEndingButton(gleBtn, "gle");
            AttachEndingButton(tleBtn, "tle");
            AttachEndingButton(cleBtn, "cle");
            AttachEndingButton(fleBtn, "fle");
            AttachEndingButton(pleBtn, "ple");
            AttachEndingButton(zleBtn, "zle");

            if (doubleToggleBtn != null)
            {
                doubleToggleBtn.onClick.RemoveAllListeners();
                doubleToggleBtn.onClick.AddListener(OnToggleDoubling);
            }

            if (buildActionBtn != null)
            {
                buildActionBtn.onClick.RemoveAllListeners();
                buildActionBtn.onClick.AddListener(OnBuildActionTapped);
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentBaseAudio);
            }
        }

        private void AttachEndingButton(Button btn, string ending)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (isProcessing) return;
                selectedEnding = ending;
                HighlightSelectedButton(btn);
                if (endingSlotText != null)
                {
                    endingSlotText.text = $"<b><color=#8B5CF6>{ending}</color></b>";
                }
                UpdateResultPreview();
                U9_SA_AudioManager_Masters_Phonics.Instance?.PlayEndingChunk(ending);
                UpdatePromptGuidance();
            });
        }

        private void HighlightSelectedButton(Button selectedBtn)
        {
            foreach (var b in allEndingButtons)
            {
                if (b == null) continue;
                var img = b.GetComponent<Image>();
                var txt = b.GetComponentInChildren<TextMeshProUGUI>(true);
                if (b == selectedBtn)
                {
                    if (img != null) img.color = new Color(0.18f, 0.55f, 0.95f); // Bright active cyan/blue
                    if (txt != null) txt.color = Color.white;
                }
                else
                {
                    if (img != null) img.color = new Color(0.15f, 0.2f, 0.3f); // Normal dark slate
                    if (txt != null) txt.color = new Color(0.9f, 0.95f, 1f);
                }
            }
        }

        public void StartActivity()
        {
            items = U9_DataBank.GetActivity3Pool();
            currentItemIndex = 0;
            firstAttemptSuccesses = 0;
            hasPlayedRoundBNotice = false;
            isProcessing = false;

            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_a3_intro", () =>
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
            selectedEnding = "";
            isDoublingEnabled = false;
            isProcessing = false;

            BuildEndingItem item = items[currentItemIndex];

            // 1. Smart 3-4 Candidate Selection (Prevents 8-button cognitive overload for kids)
            SetupCandidateButtons(item.endingChunk);

            // 2. Reset Button visuals
            HighlightSelectedButton(null);

            // 3. Setup Doubling UI & HUD
            UpdateDoublingUI();
            UpdateHUD();

            // 4. Setup Base Chunk Display
            if (baseChunkText != null)
            {
                string vowelHint = item.isRoundBOpen 
                    ? "\n<size=50%><color=#0284C7><b>[Long Vowel — Single Letter]</b></color></size>" 
                    : (item.shouldDoubleLastLetter 
                        ? "\n<size=50%><color=#D97706><b>[Short Vowel — Double the Letter!]</b></color></size>" 
                        : "\n<size=50%><color=#D97706><b>[Short Vowel — Keep Single]</b></color></size>");
                baseChunkText.text = $"<b>{item.baseChunk}</b>{vowelHint}";
                baseChunkText.color = new Color(0.08f, 0.12f, 0.25f);
            }

            if (endingSlotText != null)
            {
                endingSlotText.text = "<b><color=#94A3B8>[ ? ]</color></b>";
            }

            // 5. Update Prompt and Preview
            UpdateResultPreview();
            UpdatePromptGuidance();

            // 6. Play Base Audio
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayBaseChunk(item.baseChunk);

            // 7. Check Round B transition line
            if (item.isRoundBOpen && !hasPlayedRoundBNotice)
            {
                hasPlayedRoundBNotice = true;
                U9_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U09_VO_a3_rb");
            }
        }

        private void SetupCandidateButtons(string correctEnding)
        {
            string[] allEndings = new string[] { "ble", "dle", "gle", "tle", "cle", "fle", "ple", "zle" };
            Button[] btnMap = new Button[] { bleBtn, dleBtn, gleBtn, tleBtn, cleBtn, fleBtn, pleBtn, zleBtn };

            // Pick 3 candidate endings: 1 correct + 2 distractors (or 4 total)
            List<int> distractorIndices = new List<int>();
            for (int i = 0; i < allEndings.Length; i++)
            {
                if (!allEndings[i].Equals(correctEnding, StringComparison.OrdinalIgnoreCase))
                {
                    distractorIndices.Add(i);
                }
            }

            // Shuffle distractors
            for (int i = 0; i < distractorIndices.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, distractorIndices.Count);
                int temp = distractorIndices[i];
                distractorIndices[i] = distractorIndices[r];
                distractorIndices[r] = temp;
            }

            // Active set includes correctEnding + top 2-3 distractors
            HashSet<string> activeEndings = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { correctEnding };
            int countToInclude = Mathf.Min(3, distractorIndices.Count); // total 4 choices
            for (int i = 0; i < countToInclude; i++)
            {
                activeEndings.Add(allEndings[distractorIndices[i]]);
            }

            // Activate candidates and deactivate non-candidates
            for (int i = 0; i < allEndings.Length; i++)
            {
                Button btn = btnMap[i];
                if (btn != null)
                {
                    bool isActive = activeEndings.Contains(allEndings[i]);
                    btn.gameObject.SetActive(isActive);
                }
            }
        }

        private void UpdatePromptGuidance()
        {
            if (promptInstructionText == null) return;

            BuildEndingItem item = items[currentItemIndex];
            if (string.IsNullOrEmpty(selectedEnding))
            {
                if (item.isRoundBOpen)
                {
                    promptInstructionText.text = "<b>Step 1:</b> Tap the matching <b>-le ending</b> for this open-vowel base!";
                }
                else
                {
                    promptInstructionText.text = "<b>Step 1:</b> Tap the <b>-le ending</b> to attach to the short-vowel base!";
                }
            }
            else
            {
                if (!item.isRoundBOpen)
                {
                    promptInstructionText.text = "<b>Step 2:</b> Does the short vowel need a <b>Doubled Letter</b>? Toggle switch, then tap <b>BUILD</b>!";
                }
                else
                {
                    promptInstructionText.text = "<b>Step 2:</b> Long vowel stays single! Tap <b>BUILD</b> to test your word.";
                }
            }
        }

        private void EnsureSwitchSprites()
        {
#if UNITY_EDITOR
            if (switchOnSprite == null || switchOffSprite == null)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/unit9_MP/sprite U9 MP.png");
                if (assets != null)
                {
                    foreach (var a in assets)
                    {
                        if (a is Sprite s)
                        {
                            if (s.name == "U09_UI_Doubling_Switch_On" || s.name.IndexOf("Switch_On", StringComparison.OrdinalIgnoreCase) >= 0)
                                switchOnSprite = s;
                            if (s.name == "U09_UI_Doubling_Switch_Off" || s.name.IndexOf("Switch_Off", StringComparison.OrdinalIgnoreCase) >= 0)
                                switchOffSprite = s;
                        }
                    }
                }
            }
#endif
        }

        private void OnToggleDoubling()
        {
            if (isProcessing) return;
            isDoublingEnabled = !isDoublingEnabled;
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayDialClick();
            UpdateDoublingUI();
            UpdateResultPreview();
            UpdatePromptGuidance();
        }

        private void UpdateDoublingUI()
        {
            EnsureSwitchSprites();

            if (doubleToggleKnobImg != null)
            {
                if (switchOnSprite != null && switchOffSprite != null)
                {
                    doubleToggleKnobImg.sprite = isDoublingEnabled ? switchOnSprite : switchOffSprite;
                    doubleToggleKnobImg.color = Color.white;
                    doubleToggleKnobImg.preserveAspect = true;
                }
                else
                {
                    doubleToggleKnobImg.color = isDoublingEnabled ? toggleOnColor : toggleOffColor;
                }
            }

            if (doubleToggleStatusText != null)
            {
                doubleToggleStatusText.gameObject.SetActive(true);
                // If it was meant as a dynamic status text
                if (doubleToggleStatusText.text.IndexOf("DOUBLE", StringComparison.OrdinalIgnoreCase) >= 0 || doubleToggleStatusText.text.IndexOf("2x", StringComparison.OrdinalIgnoreCase) >= 0 || doubleToggleStatusText.text.IndexOf("1x", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    doubleToggleStatusText.text = isDoublingEnabled ? "<b>DOUBLE: ON (2x)</b>" : "<b>DOUBLE: OFF (1x)</b>";
                }
            }
        }

        private void UpdateResultPreview()
        {
            if (resultWordText == null) return;

            BuildEndingItem item = items[currentItemIndex];
            string basePart = item.baseChunk;
            char lastChar = basePart.Length > 0 ? basePart[basePart.Length - 1] : ' ';

            if (string.IsNullOrEmpty(selectedEnding))
            {
                resultWordText.text = $"<b><color=#0284C7>{basePart}</color> + <color=#94A3B8>[ ? ]</color> = <color=#64748B>{basePart}...</color></b>";
            }
            else
            {
                if (isDoublingEnabled && basePart.Length > 0)
                {
                    string fullPreview = basePart + lastChar + selectedEnding;
                    resultWordText.text = $"<b><color=#0284C7>{basePart}</color> + <color=#F59E0B>{lastChar}</color> + <color=#8B5CF6>{selectedEnding}</color> = <color=#0F172A>{fullPreview}</color></b>";
                }
                else
                {
                    string fullPreview = basePart + selectedEnding;
                    resultWordText.text = $"<b><color=#0284C7>{basePart}</color> + <color=#8B5CF6>{selectedEnding}</color> = <color=#0F172A>{fullPreview}</color></b>";
                }
            }
        }

        public void OnBuildActionTapped()
        {
            if (isProcessing || string.IsNullOrEmpty(selectedEnding)) return;
            isProcessing = true;

            BuildEndingItem item = items[currentItemIndex];
            bool endingCorrect = string.Equals(selectedEnding, item.endingChunk, StringComparison.OrdinalIgnoreCase);
            bool doublingCorrect = (isDoublingEnabled == item.shouldDoubleLastLetter);
            bool isCorrect = endingCorrect && doublingCorrect;

            if (isCorrect)
            {
                StartCoroutine(CoHandleCorrect(item));
            }
            else
            {
                StartCoroutine(CoHandleWrong(item, endingCorrect, doublingCorrect));
            }
        }

        private IEnumerator CoHandleCorrect(BuildEndingItem item)
        {
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayCorrect();

            if (!hasFailedCurrentItem)
            {
                firstAttemptSuccesses++;
                U9_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(100);
            }

            UpdateHUD();

            if (resultWordText != null)
            {
                resultWordText.text = $"<b><color=#10B981>✓ {item.finalWord.ToUpper()}</color></b>";
            }

            if (endingSlotText != null)
            {
                endingSlotText.text = $"<b><color=#10B981>{item.endingChunk}</color></b>";
            }

            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWord(item.finalWord);

            yield return new WaitForSeconds(1.5f);

            currentItemIndex++;
            LoadCurrentItem();
        }

        private IEnumerator CoHandleWrong(BuildEndingItem item, bool endingCorrect, bool doublingCorrect)
        {
            hasFailedCurrentItem = true;
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
            U9_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(item.finalWord);

            if (resultWordText != null)
            {
                if (!doublingCorrect)
                {
                    string hint = item.shouldDoubleLastLetter ? "(Need to Double!)" : "(Keep Single!)";
                    resultWordText.text = $"<b><color=#EF4444>Check Doubling Switch! {hint}</color></b>";
                }
                else
                {
                    resultWordText.text = $"<b><color=#EF4444>Try another -le ending!</color></b>";
                }
            }

            // Audio remediation
            if (!doublingCorrect)
            {
                if (item.shouldDoubleLastLetter)
                {
                    U9_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U09_VO_a2_door");
                }
                else
                {
                    U9_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U09_VO_a2_gate");
                }
            }

            yield return new WaitForSeconds(1.3f);
            UpdateResultPreview();
            isProcessing = false;
        }

        private void ReplayCurrentBaseAudio()
        {
            if (currentItemIndex < items.Count && U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayBaseChunk(items[currentItemIndex].baseChunk);
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

        private void CompleteActivity()
        {
            int earnedStars = 1;
            if (firstAttemptSuccesses >= 11) earnedStars = 3;
            else if (firstAttemptSuccesses >= 9) earnedStars = 2;

            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.ShowActivityCompletionDialog(
                    transform, 3, earnedStars, firstAttemptSuccesses * 100, () =>
                    {
                        U9_SA_UnitFlowManager_Masters_Phonics.Instance.OpenActivity4();
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
            Debug.Log("<color=#10B981><b>[Unit 9 Build Ending] Auto-Assigned Slots, Switch, Tray & Action Buttons!</b></color>");
        }
#endif
    }
}
