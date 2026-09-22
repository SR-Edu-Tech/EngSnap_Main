using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    [System.Serializable]
    public class PrefixMachineRound
    {
        public string targetPrefix;
        public string rootWord;
        public string fullWord;
        public string meaningPrompt;
        public AudioClip wordAudio;
        public AudioClip prefixAudio;
        public AudioClip rootAudio;
    }

    public class U1_SA_GM02_PrefixMachine_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Activity Data (Optional)")]
        public U1_SA_ActivityDataSO_Masters_Phonics activityData;

        [Header("2. Narration Audio Clips (Voice A)")]
        public AudioClip introInstructionClip;
        public AudioClip successFeedbackClip;
        public AudioClip failHintClip;

        [Header("3. Scene Slots (Auto-Bound to your Hierarchy)")]
        public GameObject slotPrefixObj;
        public TextMeshProUGUI slotPrefixText;

        public GameObject slotRootObj;
        public TextMeshProUGUI slotRootText;

        public GameObject builtWordDisplayObj;
        public TextMeshProUGUI builtWordDisplayText;

        [Header("4. Scene Headers & Status (Auto-Bound)")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("5. Conveyor Tile Bank (Parent holding Tile_un, Tile_re, etc.)")]
        public Transform conveyorTileBank;

        private List<PrefixMachineRound> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        private Coroutine progressTweenCoroutine;

        private readonly string[] allPrefixes = new string[] { "un", "re", "dis", "pre", "mis", "im" };

        private void Awake()
        {
            AutoBindHierarchyElements();
            InitializeRounds();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            currentRoundIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            LoadRound(currentRoundIndex);

            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
            }
        }

        public void Initialize(U1_SA_ActivityDataSO_Masters_Phonics data)
        {
            activityData = data;
            currentRoundIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            LoadRound(currentRoundIndex);
        }

        /// <summary>
        /// Automatically binds to your existing GameObjects in the Hierarchy
        /// without overriding your Inspector font sizes or styles!
        /// </summary>
        private void AutoBindHierarchyElements()
        {
            // 1. Headers & Texts
            if (titleTMP == null)
            {
                var t = transform.Find("TitleText") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (promptTMP == null)
            {
                var t = transform.Find("Prompt_Text") ?? transform.Find("PromptText");
                if (t != null) promptTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (scoreTMP == null)
            {
                var t = transform.Find("ScoreText") ?? transform.Find("Score");
                if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            // 2. Machine Display Slots
            Transform machineDisplay = transform.Find("Machine_Display") ?? transform.Find("MachineDisplay");
            if (machineDisplay != null)
            {
                // Slot_Prefix
                if (slotPrefixObj == null)
                {
                    Transform sp = machineDisplay.Find("Slot_Prefix") ?? machineDisplay.Find("SlotPrefix");
                    if (sp != null) slotPrefixObj = sp.gameObject;
                }
                if (slotPrefixObj != null && slotPrefixText == null)
                {
                    slotPrefixText = slotPrefixObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                // Slot_Root
                if (slotRootObj == null)
                {
                    Transform sr = machineDisplay.Find("Slot_Root") ?? machineDisplay.Find("SlotRoot");
                    if (sr != null) slotRootObj = sr.gameObject;
                }
                if (slotRootObj != null && slotRootText == null)
                {
                    slotRootText = slotRootObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                // BuiltWord_Display
                if (builtWordDisplayObj == null)
                {
                    Transform bw = machineDisplay.Find("BuiltWord_Display") ?? machineDisplay.Find("Result_Slot") ?? machineDisplay.Find("Slot_Result");
                    if (bw != null) builtWordDisplayObj = bw.gameObject;
                }
                if (builtWordDisplayObj != null && builtWordDisplayText == null)
                {
                    builtWordDisplayText = builtWordDisplayObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                // Make background images transparent so the machine's glowing artwork shines through
                MakeSlotImageTransparent(slotPrefixObj);
                MakeSlotImageTransparent(slotRootObj);
                MakeSlotImageTransparent(builtWordDisplayObj);
            }

            // 3. TileBank Conveyor
            if (conveyorTileBank == null)
            {
                Transform c = transform.Find("TileBank_Conveyor") ?? transform.Find("TileBank");
                if (c != null) conveyorTileBank = c;
            }

            BindExistingConveyorButtons();
            StyleProgressBar();
        }

        private void MakeSlotImageTransparent(GameObject obj)
        {
            if (obj == null) return;
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(1f, 1f, 1f, 0f);
            }
        }

        private void BindExistingConveyorButtons()
        {
            if (conveyorTileBank == null) return;

            int index = 0;
            foreach (Transform child in conveyorTileBank)
            {
                if (child == null) continue;

                Button btn = child.GetComponent<Button>();
                if (btn == null) btn = child.gameObject.AddComponent<Button>();

                // Set prefix text to un, re, dis, pre, mis, im in order (preserves your font sizes!)
                string pText = (index < allPrefixes.Length) ? allPrefixes[index] : "un";
                index++;

                TextMeshProUGUI tmp = child.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.text = $"<b>{pText}</b>";
                }

                string capturedPrefix = pText;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(child, 1.15f));
                    OnPrefixTileTapped(capturedPrefix);
                });
            }
        }

        private void StyleProgressBar()
        {
            if (progressBar == null) return;

            Transform handle = progressBar.transform.Find("Handle Slide Area");
            if (handle != null) handle.gameObject.SetActive(false);

            Transform fill = progressBar.transform.Find("Fill Area/Fill");
            if (fill != null)
            {
                Image fillImg = fill.GetComponent<Image>();
                if (fillImg != null) fillImg.color = new Color(0.20f, 0.85f, 0.45f, 1f);
            }
        }

        private void InitializeRounds()
        {
            rounds = new List<PrefixMachineRound>
            {
                new PrefixMachineRound { targetPrefix = "un", rootWord = "known", fullWord = "unknown", meaningPrompt = "Make a word meaning: 'NOT KNOWN'" },
                new PrefixMachineRound { targetPrefix = "re", rootWord = "fill", fullWord = "refill", meaningPrompt = "Make a word meaning: 'FILL AGAIN'" },
                new PrefixMachineRound { targetPrefix = "dis", rootWord = "like", fullWord = "dislike", meaningPrompt = "Make a word meaning: 'NOT LIKE / OPPOSITE'" },
                new PrefixMachineRound { targetPrefix = "pre", rootWord = "heat", fullWord = "preheat", meaningPrompt = "Make a word meaning: 'HEAT BEFORE'" },
                new PrefixMachineRound { targetPrefix = "mis", rootWord = "place", fullWord = "misplace", meaningPrompt = "Make a word meaning: 'PLACE WRONGLY'" },
                new PrefixMachineRound { targetPrefix = "im", rootWord = "perfect", fullWord = "imperfect", meaningPrompt = "Make a word meaning: 'NOT PERFECT'" }
            };
        }

        public void LoadRound(int roundIndex)
        {
            if (rounds == null || rounds.Count == 0) InitializeRounds();

            if (roundIndex >= rounds.Count)
            {
                OnAllRoundsComplete();
                return;
            }

            isProcessingAnswer = false;
            currentRoundIndex = roundIndex;
            PrefixMachineRound currentRound = rounds[roundIndex];

            // 1. Prompts
            if (titleTMP != null) titleTMP.text = "<b>THE PREFIX MACHINE</b>";
            if (promptTMP != null) promptTMP.text = $"<b>{currentRound.meaningPrompt}</b>";

            // 2. Progress Slider
            UpdateProgressBar(roundIndex, rounds.Count);

            // 3. Update Existing Scene Slots (Preserves all Inspector font size settings)
            if (slotPrefixText != null)
            {
                slotPrefixText.text = "<b>[ Tap Prefix ]</b>";
                slotPrefixText.color = new Color(0.6f, 0.85f, 1f);
            }

            if (slotRootText != null)
            {
                slotRootText.text = $"<b>{currentRound.rootWord}</b>";
                slotRootText.color = Color.white;
            }

            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = "<b>?</b>";
                builtWordDisplayText.color = new Color(1f, 0.9f, 0.35f);
            }
        }

        private void UpdateProgressBar(int currentRound, int totalRounds)
        {
            if (progressBar != null)
            {
                progressBar.minValue = 0;
                progressBar.maxValue = totalRounds;
                
                if (progressTweenCoroutine != null) StopCoroutine(progressTweenCoroutine);
                progressTweenCoroutine = StartCoroutine(TweenSlider(currentRound));
            }
        }

        private IEnumerator TweenSlider(float targetVal)
        {
            float startVal = progressBar.value;
            float elapsed = 0f;
            float dur = 0.35f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                progressBar.value = Mathf.Lerp(startVal, targetVal, elapsed / dur);
                yield return null;
            }
            progressBar.value = targetVal;
        }

        private void OnPrefixTileTapped(string selectedPrefix)
        {
            if (isProcessingAnswer) return;

            PrefixMachineRound currentRound = rounds[currentRoundIndex];
            string cleanSelected = selectedPrefix.Trim().TrimEnd('-').ToLower();
            string cleanTarget = currentRound.targetPrefix.Trim().TrimEnd('-').ToLower();

            bool isCorrect = (cleanSelected == cleanTarget);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectAnswer(selectedPrefix, currentRound));
            }
            else
            {
                StartCoroutine(HandleIncorrectAnswer(selectedPrefix));
            }
        }

        private IEnumerator HandleCorrectAnswer(string prefix, PrefixMachineRound round)
        {
            isProcessingAnswer = true;

            // 1. Fill Prefix Slot Text
            if (slotPrefixText != null)
            {
                slotPrefixText.text = $"<b>{prefix}</b>";
                slotPrefixText.color = new Color(0.2f, 0.95f, 1f);
            }
            if (slotPrefixObj != null) StartCoroutine(PunchScale(slotPrefixObj.transform, 1.18f));

            // 2. Reveal Full Word in BuiltWord_Display
            yield return new WaitForSeconds(0.25f);
            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = $"<b>{round.fullWord}</b>";
                builtWordDisplayText.color = new Color(0.25f, 1f, 0.5f);
            }
            if (builtWordDisplayObj != null) StartCoroutine(PunchScale(builtWordDisplayObj.transform, 1.22f));

            // 3. Audio: Play Voice B Word Model
            if (U1_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
                if (round.wordAudio != null)
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(round.wordAudio);
            }

            // 4. Update Score
            currentScore += 100;
            UpdateScoreUI();

            yield return new WaitForSeconds(1.4f);

            // 5. Advance
            LoadRound(currentRoundIndex + 1);
        }

        private IEnumerator HandleIncorrectAnswer(string prefix)
        {
            isProcessingAnswer = true;

            if (slotPrefixText != null)
            {
                slotPrefixText.text = $"<b>{prefix}</b>";
                slotPrefixText.color = new Color(1f, 0.35f, 0.35f);
            }

            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && failHintClip != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(failHintClip);
            }

            if (slotPrefixObj != null)
            {
                Vector3 origPos = slotPrefixObj.transform.localPosition;
                for (int i = 0; i < 6; i++)
                {
                    slotPrefixObj.transform.localPosition = origPos + new Vector3((i % 2 == 0 ? 14 : -14), 0, 0);
                    yield return new WaitForSeconds(0.04f);
                }
                slotPrefixObj.transform.localPosition = origPos;
            }

            yield return new WaitForSeconds(0.4f);

            if (slotPrefixText != null)
            {
                slotPrefixText.text = "<b>[ Tap Prefix ]</b>";
                slotPrefixText.color = new Color(0.6f, 0.85f, 1f);
            }

            isProcessingAnswer = false;
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null)
            {
                scoreTMP.text = $"Score: <b>{currentScore}</b>";
            }
        }

        private void OnAllRoundsComplete()
        {
            Debug.Log("<color=green>Prefix Machine Completed! Advancing to Family Sort...</color>");

            if (promptTMP != null) promptTMP.text = "<b>Excellent! Machine Powered Up!</b>";

            if (U1_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U1_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }

        private IEnumerator PunchScale(Transform tr, float scaleMult)
        {
            if (tr == null) yield break;
            Vector3 orig = Vector3.one;
            tr.localScale = orig * scaleMult;
            yield return new WaitForSeconds(0.12f);
            if (tr != null) tr.localScale = orig;
        }
    }
}
