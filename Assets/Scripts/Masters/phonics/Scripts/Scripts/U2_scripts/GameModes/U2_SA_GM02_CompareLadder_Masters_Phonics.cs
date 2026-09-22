using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public enum ComparisonCountType
    {
        TwoObjects,        // -er (taller, nicer)
        ThreeOrMoreObjects // -est (tallest, nicest)
    }

    [System.Serializable]
    public class CompareLadderRound
    {
        public string baseWord;
        public ComparisonCountType countType; // Two vs Three+
        public string targetEnding;          // "-er" vs "-est"
        public string fullWord;              // "taller" vs "tallest"
        public string promptText;            // "Two trees. Which is _____ ?"
        public AudioClip wordAudio;
    }

    public class U2_SA_GM02_CompareLadder_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Voice A Audio (Official Unit 2 Doc)")]
        [Tooltip("U02_VO_a4_intro: 'Comparing two things? Use e-r. Comparing three or more? Use e-s-t. Count what’s on the screen.' (9s)")]
        public AudioClip introInstructionClip;   // U02_VO_a4_intro

        [Tooltip("U02_VO_a4_open: 'Your turn. Pick any describing word and I’ll build both forms for you.' (6s)")]
        public AudioClip openExploreClip;         // U02_VO_a4_open

        [Tooltip("U02_VO_a4_unknown: 'I don’t know that one. Try another.' (3s)")]
        public AudioClip unknownTryAnotherClip;   // U02_VO_a4_unknown

        [Tooltip("Celebration fanfare clip when activity 4 completes")]
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Slot & Visual Objects Display")]
        [SerializeField] private GameObject slotRootObj;
        [SerializeField] private TextMeshProUGUI slotRootText;

        [SerializeField] private GameObject slotEndingObj;
        [SerializeField] private TextMeshProUGUI slotEndingText;

        [SerializeField] private GameObject builtWordDisplayObj;
        [SerializeField] private TextMeshProUGUI builtWordDisplayText;

        [Header("4. Ending Choice Buttons (-er vs -est)")]
        [SerializeField] private Button btnEndingER;
        [SerializeField] private Button btnEndingEST;

        private List<CompareLadderRound> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;

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
            InitializeRounds();
            LoadCurrentRound();

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
            }
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (promptTMP == null)
            {
                var t = transform.Find("PromptText") ?? transform.Find("Prompt_Text") ?? transform.Find("InstructionText");
                if (t != null) promptTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (scoreTMP == null)
            {
                var t = transform.Find("ScoreText") ?? transform.Find("Score_Text") ?? transform.Find("Score");
                if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("ProgressBar (1)") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            // Slots
            if (slotRootObj == null)
            {
                Transform t = transform.Find("Slot_Root") ?? transform.Find("Slot_Prefix") ?? transform.Find("RootSlot");
                if (t != null) slotRootObj = t.gameObject;
            }
            if (slotRootObj != null && slotRootText == null)
                slotRootText = slotRootObj.GetComponentInChildren<TextMeshProUGUI>(true);

            if (slotEndingObj == null)
            {
                Transform t = transform.Find("Slot_Suffix") ?? transform.Find("Slot_Ending");
                if (t != null) slotEndingObj = t.gameObject;
            }
            if (slotEndingObj != null && slotEndingText == null)
                slotEndingText = slotEndingObj.GetComponentInChildren<TextMeshProUGUI>(true);

            if (builtWordDisplayObj == null)
            {
                Transform t = transform.Find("BuiltWord_Display") ?? transform.Find("Result_Display") ?? transform.Find("Machine_Display");
                if (t != null) builtWordDisplayObj = t.gameObject;
            }
            if (builtWordDisplayObj != null && builtWordDisplayText == null)
                builtWordDisplayText = builtWordDisplayObj.GetComponentInChildren<TextMeshProUGUI>(true);

            // Choice Buttons
            Transform bank = transform.Find("Conveyor_Tile_Bank") ?? transform.Find("Tile_Bank") ?? transform;
            if (btnEndingER == null) btnEndingER = FindButton(bank, "btn_er", "tile_1", "er", "-er");
            if (btnEndingEST == null) btnEndingEST = FindButton(bank, "btn_est", "tile_2", "est", "-est");

            if (btnEndingER != null)
            {
                btnEndingER.onClick.RemoveAllListeners();
                btnEndingER.onClick.AddListener(() => OnEndingSelected("-er"));
                SetButtonText(btnEndingER, "<b>-er</b>\n<size=75%>(Comparing 2)</size>");
            }

            if (btnEndingEST != null)
            {
                btnEndingEST.onClick.RemoveAllListeners();
                btnEndingEST.onClick.AddListener(() => OnEndingSelected("-est"));
                SetButtonText(btnEndingEST, "<b>-est</b>\n<size=75%>(Comparing 3+)</size>");
            }
        }

        private Button FindButton(Transform parent, params string[] keywords)
        {
            Button[] all = parent.GetComponentsInChildren<Button>(true);
            foreach (var b in all)
            {
                string name = b.name.ToLower();
                foreach (var kw in keywords)
                {
                    if (name.Contains(kw)) return b;
                }
            }
            return null;
        }

        private void SetButtonText(Button btn, string text)
        {
            if (btn == null) return;
            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = text;
        }

        private void InitializeRounds()
        {
            // 10 curated items from Textbook Page 14
            rounds = new List<CompareLadderRound>
            {
                new CompareLadderRound { baseWord = "tall", countType = ComparisonCountType.TwoObjects, targetEnding = "-er", fullWord = "taller", promptText = "Comparing <b>2 people</b>: Joe is _____ than Jack." },
                new CompareLadderRound { baseWord = "clean", countType = ComparisonCountType.ThreeOrMoreObjects, targetEnding = "-est", fullWord = "cleanest", promptText = "Comparing <b>3 rooms</b>: This room is the _____ ." },
                new CompareLadderRound { baseWord = "nice", countType = ComparisonCountType.TwoObjects, targetEnding = "-er", fullWord = "nicer", promptText = "Comparing <b>2 parks</b>: This park is _____ ." },
                new CompareLadderRound { baseWord = "bright", countType = ComparisonCountType.ThreeOrMoreObjects, targetEnding = "-est", fullWord = "brightest", promptText = "Comparing <b>all the stars</b>: Which is the _____ star?" },
                new CompareLadderRound { baseWord = "wide", countType = ComparisonCountType.TwoObjects, targetEnding = "-er", fullWord = "wider", promptText = "Comparing <b>2 roads</b>: The highway is _____ ." },
                new CompareLadderRound { baseWord = "sharp", countType = ComparisonCountType.ThreeOrMoreObjects, targetEnding = "-est", fullWord = "sharpest", promptText = "Comparing <b>a box of pencils</b>: Find the _____ pencil." },
                new CompareLadderRound { baseWord = "near", countType = ComparisonCountType.TwoObjects, targetEnding = "-er", fullWord = "nearer", promptText = "Comparing <b>2 shops</b>: The bakery is _____ ." },
                new CompareLadderRound { baseWord = "great", countType = ComparisonCountType.ThreeOrMoreObjects, targetEnding = "-est", fullWord = "greatest", promptText = "Comparing <b>all players</b>: He was the _____ player." },
                new CompareLadderRound { baseWord = "strong", countType = ComparisonCountType.TwoObjects, targetEnding = "-er", fullWord = "stronger", promptText = "Comparing <b>2 lifters</b>: Leo is _____ ." },
                new CompareLadderRound { baseWord = "rich", countType = ComparisonCountType.ThreeOrMoreObjects, targetEnding = "-est", fullWord = "richest", promptText = "Comparing <b>the whole town</b>: Who is the _____ person?" }
            };
        }

        private void LoadCurrentRound()
        {
            if (currentRoundIndex >= rounds.Count)
            {
                OnAllRoundsComplete();
                return;
            }

            isProcessingAnswer = false;
            CompareLadderRound currentRound = rounds[currentRoundIndex];

            if (titleTMP != null) titleTMP.text = "<b>COMPARE LADDER</b>";
            if (promptTMP != null) promptTMP.text = $"<color=#FFFFFF><b>{currentRound.promptText}</b></color>";

            if (progressBar != null)
            {
                progressBar.value = (float)currentRoundIndex / rounds.Count;
            }

            if (slotRootText != null)
            {
                slotRootText.text = $"<b><color=#000000>{currentRound.baseWord}</color></b>";
            }

            if (slotEndingText != null)
            {
                slotEndingText.text = "<b><color=#000000>?</color></b>";
            }

            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = "<b><color=#000000>?</color></b>";
            }
        }

        public void OnEndingSelected(string ending)
        {
            if (isProcessingAnswer || currentRoundIndex >= rounds.Count) return;

            CompareLadderRound currentRound = rounds[currentRoundIndex];
            if (ending == currentRound.targetEnding)
            {
                StartCoroutine(HandleCorrectChoice(ending, currentRound));
            }
            else
            {
                StartCoroutine(HandleWrongChoice(ending, currentRound));
            }
        }

        private IEnumerator HandleCorrectChoice(string ending, CompareLadderRound round)
        {
            isProcessingAnswer = true;

            if (slotEndingText != null)
            {
                slotEndingText.text = $"<b><color=#000000>{ending}</color></b>";
            }

            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = $"<b><color=#000000>{round.fullWord}</color></b>";
            }

            string countDesc = (round.countType == ComparisonCountType.TwoObjects) ? "2 things -> -er" : "3+ things -> -est";
            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFFFFF><b>Correct! Comparing {countDesc}: {round.baseWord} + {ending} = {round.fullWord}</b></color>";
            }

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();

                AudioClip pronunciation = round.wordAudio ?? Resources.Load<AudioClip>($"U2_audio/{round.fullWord}") ?? Resources.Load<AudioClip>($"Audio/U2_audio/{round.fullWord}");
                if (pronunciation != null)
                {
                    U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(pronunciation);
                }
            }

            Button targetBtn = (ending == "-er") ? btnEndingER : btnEndingEST;
            if (targetBtn != null) StartCoroutine(PunchScale(targetBtn.transform, 1.25f));

            currentScore += 100;
            UpdateScoreUI();

            // Give the child 1.4 seconds to see the comparison formula and hear pronunciation
            yield return new WaitForSeconds(1.4f);

            currentRoundIndex++;
            LoadCurrentRound();
        }

        private IEnumerator HandleWrongChoice(string ending, CompareLadderRound round)
        {
            isProcessingAnswer = true;

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            string ruleCorrection = (round.countType == ComparisonCountType.TwoObjects) ? "We are comparing 2 things -> Use -er!" : "We are comparing 3 or more things -> Use -est!";
            if (promptTMP != null)
            {
                promptTMP.text = $"<size=120%><color=#AA1111><b>Watch the count! {ruleCorrection} ({round.fullWord})</b></color></size>";
            }

            Button targetBtn = (ending == "-er") ? btnEndingER : btnEndingEST;
            if (targetBtn != null) StartCoroutine(PunchScale(targetBtn.transform, 0.85f));

            yield return new WaitForSeconds(1.5f);

            isProcessingAnswer = false;
            LoadCurrentRound();
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
        }

        private void OnAllRoundsComplete()
        {
            Debug.Log("<color=green>Compare Ladder (Activity 4) Completed! Advancing...</color>");

            if (promptTMP != null) promptTMP.text = "<b>Fantastic! Compare Ladder Complete!</b>";

            if (progressBar != null) progressBar.value = 1f;

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null && celebrationClip != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(celebrationClip);
            }

            if (U2_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U2_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
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
