using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    [System.Serializable]
    public class YToIRoundItem
    {
        public string baseWord;
        public string endingToAdd;   // e.g. "-ed", "-ing", "-es", "-s"
        public string targetAnswer;  // e.g. "cried", "crying", "stories", "boys"
        public string rulePrompt;    // "Consonant + y -> changes to i" / "y stays before -ing"
        public AudioClip wordAudio;
    }

    public class U2_SA_GM05_YToILab_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Voice A Audio (Official Unit 2 Doc)")]
        [Tooltip("U02_VO_a5_intro: 'Words ending in y are fussy. Sometimes the y changes. Sometimes it stays. Let’s find out when.' (9s)")]
        public AudioClip introInstructionClip;   // U02_VO_a5_intro

        [Tooltip("U02_VO_a5_interstitial: 'Before e-d, the y changes to i. Before i-n-g, the y stays. Cried… but crying.' (9s)")]
        public AudioClip interstitialClip;        // U02_VO_a5_interstitial

        [Tooltip("U02_VO_a5_roundc: 'Now plurals. But watch out — if there’s a vowel just before the y, nothing changes at all.' (8s)")]
        public AudioClip roundCPluralsClip;       // U02_VO_a5_roundc

        [Tooltip("Celebration fanfare clip when activity 5 completes")]
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Machine Slots")]
        [SerializeField] private GameObject slotBaseObj;
        [SerializeField] private TextMeshProUGUI slotBaseText;

        [SerializeField] private GameObject slotEndingObj;
        [SerializeField] private TextMeshProUGUI slotEndingText;

        [SerializeField] private GameObject builtWordDisplayObj;
        [SerializeField] private TextMeshProUGUI builtWordDisplayText;

        [Header("4. Letter Tile Options / Spelling Choice Buttons")]
        [SerializeField] private Button btnOptionChangeY; // e.g. "i + ed" / "i + es"
        [SerializeField] private Button btnOptionKeepY;   // e.g. "y + ing" / "y + s"

        private List<YToIRoundItem> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;

        private void Awake()
        {
            AutoBindHierarchyElements();
            InitializeRounds();
        }

        private void Start()
        {
            PlayIntroInstruction();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            currentRoundIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            InitializeRounds();
            LoadCurrentRound();

            PlayIntroInstruction();
        }

        private void PlayIntroInstruction()
        {
            if (introInstructionClip != null)
            {
                StartCoroutine(DelayedPlayIntro());
            }
        }

        private IEnumerator DelayedPlayIntro()
        {
            yield return new WaitForSeconds(0.15f);
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
            if (slotBaseObj == null)
            {
                Transform t = transform.Find("Slot_Root") ?? transform.Find("Slot_Base") ?? transform.Find("Slot_Prefix");
                if (t != null) slotBaseObj = t.gameObject;
            }
            if (slotBaseObj != null && slotBaseText == null)
                slotBaseText = slotBaseObj.GetComponentInChildren<TextMeshProUGUI>(true);

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

            // Option Buttons (from Tile_Bank, Conveyor, or hierarchy)
            Transform bank = transform.Find("Tile_Bank") ?? transform.Find("Conveyor_Tile_Bank") ?? transform.Find("PrefixShelf_Left") ?? transform.Find("SuffixShelf_Right") ?? transform;
            if (btnOptionChangeY == null) btnOptionChangeY = FindButton(bank, "btn_optionchangey", "optionchange", "btn_opt1", "tile_1", "left", "change", "btn_change");
            if (btnOptionKeepY == null) btnOptionKeepY = FindButton(bank, "btn_optionkeepy", "optionkeep", "btn_opt2", "tile_2", "right", "keep", "btn_keep");

            if (btnOptionChangeY != null)
            {
                btnOptionChangeY.onClick.RemoveAllListeners();
                btnOptionChangeY.onClick.AddListener(() => OnOptionSelected(true));
            }

            if (btnOptionKeepY != null)
            {
                btnOptionKeepY.onClick.RemoveAllListeners();
                btnOptionKeepY.onClick.AddListener(() => OnOptionSelected(false));
            }
        }

        private Button FindButton(Transform parent, params string[] keywords)
        {
            Button[] buttons = parent.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string name = b.name.ToLower();
                foreach (var kw in keywords)
                {
                    if (name.Contains(kw)) return b;
                }
            }
            return null;
        }

        private void InitializeRounds()
        {
            rounds = new List<YToIRoundItem>
            {
                // Phase 1: Consonant + y + -ed (cried, copied, tried) -> change y to i
                new YToIRoundItem { baseWord = "cry", endingToAdd = "-ed", targetAnswer = "cried", rulePrompt = "Consonant + y: change y to i before -ed! (cry -> cried)" },
                new YToIRoundItem { baseWord = "copy", endingToAdd = "-ed", targetAnswer = "copied", rulePrompt = "Consonant + y: change y to i before -ed! (copy -> copied)" },
                new YToIRoundItem { baseWord = "try", endingToAdd = "-ed", targetAnswer = "tried", rulePrompt = "Consonant + y: change y to i before -ed! (try -> tried)" },

                // Phase 2: Consonant + y + -ing (crying, copying, trying) -> Y STAYS!
                new YToIRoundItem { baseWord = "cry", endingToAdd = "-ing", targetAnswer = "crying", rulePrompt = "Keep the y before -ing to avoid double ii! (cry -> crying)" },
                new YToIRoundItem { baseWord = "copy", endingToAdd = "-ing", targetAnswer = "copying", rulePrompt = "Keep the y before -ing! (copy -> copying)" },
                new YToIRoundItem { baseWord = "try", endingToAdd = "-ing", targetAnswer = "trying", rulePrompt = "Keep the y before -ing! (try -> trying)" },

                // Phase 3: Plurals (baby -> babies, story -> stories) -> change y to i
                new YToIRoundItem { baseWord = "baby", endingToAdd = "-es", targetAnswer = "babies", rulePrompt = "Consonant + y: change y to i and add -es! (baby -> babies)" },
                new YToIRoundItem { baseWord = "story", endingToAdd = "-es", targetAnswer = "stories", rulePrompt = "Consonant + y: change y to i and add -es! (story -> stories)" },

                // Phase 4: Vowel + y (play -> played, boy -> boys) -> Y STAYS!
                new YToIRoundItem { baseWord = "play", endingToAdd = "-ed", targetAnswer = "played", rulePrompt = "Vowel before y ('ay'): y stays as it is! (play -> played)" },
                new YToIRoundItem { baseWord = "boy", endingToAdd = "-s", targetAnswer = "boys", rulePrompt = "Vowel before y ('oy'): y stays as it is! (boy -> boys)" },
                new YToIRoundItem { baseWord = "stay", endingToAdd = "-ing", targetAnswer = "staying", rulePrompt = "Vowel before y: y stays! (stay -> staying)" }
            };

            // Shuffle
            for (int i = 0; i < rounds.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, rounds.Count);
                var temp = rounds[i];
                rounds[i] = rounds[rnd];
                rounds[rnd] = temp;
            }
        }

        private void LoadCurrentRound()
        {
            if (currentRoundIndex >= rounds.Count)
            {
                OnAllRoundsComplete();
                return;
            }

            isProcessingAnswer = false;
            YToIRoundItem round = rounds[currentRoundIndex];

            if (titleTMP != null) titleTMP.text = "<b>Y-TO-I LAB</b>";
            if (promptTMP != null) promptTMP.text = $"Add <b>{round.endingToAdd}</b> to <b>\"{round.baseWord}\"</b>. What happens to the Y?";

            if (progressBar != null)
            {
                progressBar.value = (float)currentRoundIndex / rounds.Count;
            }

            if (slotBaseText != null)
            {
                slotBaseText.text = $"<b><color=#000000>{round.baseWord}</color></b>";
            }

            if (slotEndingText != null)
            {
                slotEndingText.text = $"<b><color=#000000>{round.endingToAdd}</color></b>";
            }

            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = "<b><color=#000000>?</color></b>";
            }

            // Configure option buttons dynamically based on ending
            if (round.endingToAdd == "-ed")
            {
                SetButtonLabel(btnOptionChangeY, "<b>-ied</b>\n<size=75%>(Change y to i)</size>");
                SetButtonLabel(btnOptionKeepY, "<b>-yed</b>\n<size=75%>(Keep y)</size>");
            }
            else if (round.endingToAdd == "-ing")
            {
                SetButtonLabel(btnOptionChangeY, "<b>-iing</b>\n<size=75%>(Change y to i)</size>");
                SetButtonLabel(btnOptionKeepY, "<b>-ying</b>\n<size=75%>(Keep y)</size>");
            }
            else // Plurals (-s / -ies)
            {
                SetButtonLabel(btnOptionChangeY, "<b>-ies</b>\n<size=75%>(Change y to i)</size>");
                SetButtonLabel(btnOptionKeepY, "<b>-ys / -s</b>\n<size=75%>(Just add s)</size>");
            }
        }

        private void SetButtonLabel(Button btn, string text)
        {
            if (btn == null) return;
            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = text;
        }

        public void OnOptionSelected(bool choseChangeY)
        {
            if (isProcessingAnswer || currentRoundIndex >= rounds.Count) return;

            YToIRoundItem round = rounds[currentRoundIndex];

            // Determine if the correct answer changed Y or kept Y
            bool shouldChangeY = round.targetAnswer.Contains("ie") || (round.targetAnswer.EndsWith("ied") && !round.baseWord.EndsWith("i"));
            if (round.targetAnswer.EndsWith("ying") || round.targetAnswer.EndsWith("ys") || round.targetAnswer.EndsWith("ays") || round.targetAnswer.EndsWith("eys"))
                shouldChangeY = false;

            bool isCorrect = (choseChangeY == shouldChangeY);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectChoice(round));
            }
            else
            {
                StartCoroutine(HandleWrongChoice(round));
            }
        }

        private IEnumerator HandleCorrectChoice(YToIRoundItem round)
        {
            isProcessingAnswer = true;

            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = $"<b><color=#000000>{round.targetAnswer}</color></b>";
            }
            if (builtWordDisplayObj != null) StartCoroutine(PunchScale(builtWordDisplayObj.transform, 1.25f));

            if (promptTMP != null) promptTMP.text = $"<b><color=#FFFFFF>Correct! {round.rulePrompt}</color></b>";

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();

                AudioClip pronunciation = round.wordAudio ?? Resources.Load<AudioClip>($"U2_audio/{round.targetAnswer}") ?? Resources.Load<AudioClip>($"Audio/U2_audio/{round.targetAnswer}");
                if (pronunciation != null)
                {
                    U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(pronunciation);
                }
            }

            currentScore += 100;
            UpdateScoreUI();

            // Give the child 1.4 seconds to see the completed word and hear pronunciation
            yield return new WaitForSeconds(1.4f);

            currentRoundIndex++;
            LoadCurrentRound();
        }

        private IEnumerator HandleWrongChoice(YToIRoundItem round)
        {
            isProcessingAnswer = true;

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = "<size=80%><b><color=#AA1111>Try Again!</color></b></size>";
            }

            if (promptTMP != null) promptTMP.text = $"<b><color=#FFD54F>Rule reminder: {round.rulePrompt}</color></b>";

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
            Debug.Log("<color=green>Y-to-I Lab (Activity 5) Completed! Advancing to Unit Complete...</color>");

            if (promptTMP != null) promptTMP.text = "<b>Unit Two Mastered! Superb Job!</b>";

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
