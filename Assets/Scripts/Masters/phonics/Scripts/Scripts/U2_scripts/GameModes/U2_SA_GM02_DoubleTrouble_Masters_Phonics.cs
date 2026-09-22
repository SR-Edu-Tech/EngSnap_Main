using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    [System.Serializable]
    public class DoubleTroubleRound
    {
        public string baseWord;
        public string targetEnding; // e.g. "-ed", "-ing", "-s"
        public SpellingRuleType correctRule; // AsItIs, DoubleIt, DropTheE
        public string correctSpelledWord;   // e.g. "hopping", "liked", "talked"
        public string ruleReason;           // "1 syllable, 1 vowel + 1 consonant"
        public AudioClip wordAudio;
    }

    public class U2_SA_GM02_DoubleTrouble_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Audio Clips")]
        public AudioClip introInstructionClip;
        public AudioClip correctFeedbackClip;
        public AudioClip wrongFeedbackClip;
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Machine Display Slots")]
        [SerializeField] private GameObject slotBaseObj;
        [SerializeField] private TextMeshProUGUI slotBaseText;

        [Header("4. Ending Choice Buttons (-s, -ed, -ing)")]
        [SerializeField] private Button btnEndingS;
        [SerializeField] private Button btnEndingED;
        [SerializeField] private Button btnEndingING;

        [Header("5. 3-Position Spelling Dial / Rule Buttons")]
        [SerializeField] private Button btnRuleAsItIs;   // ( ) AS IT IS
        [SerializeField] private Button btnRuleDoubleIt;  // ( ) DOUBLE IT
        [SerializeField] private Button btnRuleDropTheE;  // ( ) DROP THE E

        [Header("5b. Dial & Button Sprites (Active vs Inactive)")]
        [SerializeField] private Sprite spriteDialActive;    // U2_A3_Dial_Active (with glowing cyan arc)
        [SerializeField] private Sprite spriteDialInactive;  // U2_A3_Dial_Inactive (normal grey knob)
        [SerializeField] private Sprite spriteEndingActive;  // Active pill sprite
        [SerializeField] private Sprite spriteEndingInactive;// Inactive pill sprite

        [Header("6. Build Button & Result Display")]
        [SerializeField] private Button buildButton;
        [SerializeField] private GameObject resultDisplayObj;
        [SerializeField] private TextMeshProUGUI resultDisplayText;

        private List<DoubleTroubleRound> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;

        // Player choices
        private string selectedEnding = "-ing";
        private SpellingRuleType selectedRule = SpellingRuleType.AsItIs;

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

            // Ending buttons
            Transform conveyor = transform.Find("Conveyor_Tile_Bank") ?? transform.Find("Endings") ?? transform.Find("Endings_Bank") ?? transform;
            if (btnEndingS == null) btnEndingS = FindButton(conveyor, "btn_s", "tile_1", "s", "btn_endings");
            if (btnEndingED == null) btnEndingED = FindButton(conveyor, "btn_ed", "tile_2", "ed", "btn_endinged");
            if (btnEndingING == null) btnEndingING = FindButton(conveyor, "btn_ing", "tile_3", "ing", "btn_endinging");

            if (btnEndingS != null)
            {
                btnEndingS.onClick.RemoveAllListeners();
                btnEndingS.onClick.AddListener(() => SelectEnding("-s", btnEndingS));
            }
            if (btnEndingED != null)
            {
                btnEndingED.onClick.RemoveAllListeners();
                btnEndingED.onClick.AddListener(() => SelectEnding("-ed", btnEndingED));
            }
            if (btnEndingING != null)
            {
                btnEndingING.onClick.RemoveAllListeners();
                btnEndingING.onClick.AddListener(() => SelectEnding("-ing", btnEndingING));
            }

            // Dial rule buttons
            Transform dialBank = transform.Find("Dial_Bank") ?? transform.Find("Dial_Rule_Bank") ?? transform.Find("Rule_Dial") ?? transform;
            if (btnRuleAsItIs == null) btnRuleAsItIs = FindButton(dialBank, "asitis", "as_it_is", "rule_1", "btn_rule_asitis");
            if (btnRuleDoubleIt == null) btnRuleDoubleIt = FindButton(dialBank, "double", "double_it", "rule_2", "btn_rule_doubleit");
            if (btnRuleDropTheE == null) btnRuleDropTheE = FindButton(dialBank, "drop", "drop_the_e", "rule_3", "btn_rule_dropthee");

            if (btnRuleAsItIs != null)
            {
                btnRuleAsItIs.onClick.RemoveAllListeners();
                btnRuleAsItIs.onClick.AddListener(() => SelectRule(SpellingRuleType.AsItIs, btnRuleAsItIs));
            }
            if (btnRuleDoubleIt != null)
            {
                btnRuleDoubleIt.onClick.RemoveAllListeners();
                btnRuleDoubleIt.onClick.AddListener(() => SelectRule(SpellingRuleType.DoubleIt, btnRuleDoubleIt));
            }
            if (btnRuleDropTheE != null)
            {
                btnRuleDropTheE.onClick.RemoveAllListeners();
                btnRuleDropTheE.onClick.AddListener(() => SelectRule(SpellingRuleType.DropTheE, btnRuleDropTheE));
            }

            // Build button
            if (buildButton == null)
            {
                Transform b = transform.Find("Btn_Build") ?? transform.Find("BuildButton") ?? transform.Find("Build_Button");
                if (b != null) buildButton = b.GetComponent<Button>();
            }
            if (buildButton != null)
            {
                buildButton.onClick.RemoveAllListeners();
                buildButton.onClick.AddListener(OnBuildPressed);
            }

            // Result screen
            if (resultDisplayObj == null)
            {
                Transform r = transform.Find("Result_Screen") ?? transform.Find("ResultDisplay") ?? transform.Find("Result_Display");
                if (r != null) resultDisplayObj = r.gameObject;
            }
            if (resultDisplayObj != null && resultDisplayText == null)
                resultDisplayText = resultDisplayObj.GetComponentInChildren<TextMeshProUGUI>(true);
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
            // Curated rounds covering all 3 spelling rules
            rounds = new List<DoubleTroubleRound>
            {
                // 1. Double It (1 syllable, 1 short vowel, 1 consonant)
                new DoubleTroubleRound
                {
                    baseWord = "hop",
                    targetEnding = "-ing",
                    correctRule = SpellingRuleType.DoubleIt,
                    correctSpelledWord = "hopping",
                    ruleReason = "1 syllable, 1 vowel + 1 consonant -> Double it!"
                },
                new DoubleTroubleRound
                {
                    baseWord = "run",
                    targetEnding = "-ing",
                    correctRule = SpellingRuleType.DoubleIt,
                    correctSpelledWord = "running",
                    ruleReason = "Short vowel 'u' + single 'n' -> Double it!"
                },
                new DoubleTroubleRound
                {
                    baseWord = "stop",
                    targetEnding = "-ed",
                    correctRule = SpellingRuleType.DoubleIt,
                    correctSpelledWord = "stopped",
                    ruleReason = "1 short vowel 'o' + 'p' -> Double it!"
                },
                new DoubleTroubleRound
                {
                    baseWord = "skip",
                    targetEnding = "-ed",
                    correctRule = SpellingRuleType.DoubleIt,
                    correctSpelledWord = "skipped",
                    ruleReason = "1 short vowel 'i' + 'p' -> Double it!"
                },

                // 2. Drop the E (ends with silent 'e')
                new DoubleTroubleRound
                {
                    baseWord = "like",
                    targetEnding = "-ed",
                    correctRule = SpellingRuleType.DropTheE,
                    correctSpelledWord = "liked",
                    ruleReason = "Silent 'e' at the end -> Drop the E!"
                },
                new DoubleTroubleRound
                {
                    baseWord = "bake",
                    targetEnding = "-ing",
                    correctRule = SpellingRuleType.DropTheE,
                    correctSpelledWord = "baking",
                    ruleReason = "Silent 'e' at the end -> Drop the E!"
                },
                new DoubleTroubleRound
                {
                    baseWord = "smile",
                    targetEnding = "-ing",
                    correctRule = SpellingRuleType.DropTheE,
                    correctSpelledWord = "smiling",
                    ruleReason = "Silent 'e' at the end -> Drop the E!"
                },
                new DoubleTroubleRound
                {
                    baseWord = "ride",
                    targetEnding = "-ing",
                    correctRule = SpellingRuleType.DropTheE,
                    correctSpelledWord = "riding",
                    ruleReason = "Silent 'e' at the end -> Drop the E!"
                },

                // 3. As It Is (2 vowels or 2 consonants)
                new DoubleTroubleRound
                {
                    baseWord = "talk",
                    targetEnding = "-ed",
                    correctRule = SpellingRuleType.AsItIs,
                    correctSpelledWord = "talked",
                    ruleReason = "Ends in two consonants 'lk' -> Leave as it is!"
                },
                new DoubleTroubleRound
                {
                    baseWord = "play",
                    targetEnding = "-ed",
                    correctRule = SpellingRuleType.AsItIs,
                    correctSpelledWord = "played",
                    ruleReason = "Ends in vowel + y -> Leave as it is!"
                },
                new DoubleTroubleRound
                {
                    baseWord = "jump",
                    targetEnding = "-ing",
                    correctRule = SpellingRuleType.AsItIs,
                    correctSpelledWord = "jumping",
                    ruleReason = "Ends in two consonants 'mp' -> Leave as it is!"
                },
                new DoubleTroubleRound
                {
                    baseWord = "read",
                    targetEnding = "-ing",
                    correctRule = SpellingRuleType.AsItIs,
                    correctSpelledWord = "reading",
                    ruleReason = "Two vowels 'ea' together -> Leave as it is!"
                }
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
            DoubleTroubleRound round = rounds[currentRoundIndex];
            selectedEnding = round.targetEnding;
            selectedRule = SpellingRuleType.AsItIs;

            if (titleTMP != null) titleTMP.text = "<b>DOUBLE TROUBLE</b>";
            if (promptTMP != null) promptTMP.text = $"Add <b>{round.targetEnding}</b> to <b>\"{round.baseWord}\"</b>. Set the dial & press <b>BUILD</b>!";

            if (progressBar != null)
            {
                progressBar.value = (float)currentRoundIndex / rounds.Count;
            }

            if (slotBaseText != null)
            {
                slotBaseText.text = $"<b><color=#000000>{round.baseWord}</color></b>";
            }

            if (resultDisplayText != null)
            {
                resultDisplayText.text = "<b><color=#000000>?</color></b>";
            }

            HighlightSelectedEnding();
            HighlightSelectedRule();
        }

        public void SelectEnding(string ending, Button btn)
        {
            selectedEnding = ending;
            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayDialClick();
            HighlightSelectedEnding();
        }

        public void SelectRule(SpellingRuleType rule, Button btn)
        {
            selectedRule = rule;
            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayDialClick();
            HighlightSelectedRule();
        }

        private void HighlightSelectedEnding()
        {
            SetBtnHighlight(btnEndingS, selectedEnding == "-s", spriteEndingActive, spriteEndingInactive);
            SetBtnHighlight(btnEndingED, selectedEnding == "-ed", spriteEndingActive, spriteEndingInactive);
            SetBtnHighlight(btnEndingING, selectedEnding == "-ing", spriteEndingActive, spriteEndingInactive);
        }

        private void HighlightSelectedRule()
        {
            SetBtnHighlight(btnRuleAsItIs, selectedRule == SpellingRuleType.AsItIs, spriteDialActive, spriteDialInactive);
            SetBtnHighlight(btnRuleDoubleIt, selectedRule == SpellingRuleType.DoubleIt, spriteDialActive, spriteDialInactive);
            SetBtnHighlight(btnRuleDropTheE, selectedRule == SpellingRuleType.DropTheE, spriteDialActive, spriteDialInactive);
        }

        private void SetBtnHighlight(Button btn, bool isSelected, Sprite activeSprite = null, Sprite inactiveSprite = null)
        {
            if (btn == null) return;

            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                if (activeSprite != null && inactiveSprite != null)
                {
                    img.sprite = isSelected ? activeSprite : inactiveSprite;
                    img.color = Color.white;
                }
                else
                {
                    img.color = isSelected ? new Color(0.2f, 0.95f, 1f, 1f) : new Color(0.85f, 0.85f, 0.85f, 0.6f);
                }
            }

            // Tactile feedback: selected dial slightly swells to 1.1x scale
            btn.transform.localScale = isSelected ? Vector3.one * 1.1f : Vector3.one;
        }

        public void OnBuildPressed()
        {
            if (isProcessingAnswer || currentRoundIndex >= rounds.Count) return;

            DoubleTroubleRound currentRound = rounds[currentRoundIndex];
            bool endingMatches = (selectedEnding == currentRound.targetEnding);
            bool ruleMatches = (selectedRule == currentRound.correctRule);

            if (endingMatches && ruleMatches)
            {
                StartCoroutine(HandleCorrectBuild(currentRound));
            }
            else
            {
                StartCoroutine(HandleWrongBuild(currentRound));
            }
        }

        private IEnumerator HandleCorrectBuild(DoubleTroubleRound round)
        {
            isProcessingAnswer = true;

            if (resultDisplayText != null)
            {
                resultDisplayText.text = $"<b><color=#000000>{round.correctSpelledWord}</color></b>";
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<b><color=#FFFFFF>Correct! {round.ruleReason} -> {round.correctSpelledWord}</color></b>";
            }

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();

                AudioClip pronunciation = round.wordAudio ?? Resources.Load<AudioClip>($"U2_audio/{round.correctSpelledWord}") ?? Resources.Load<AudioClip>($"Audio/U2_audio/{round.correctSpelledWord}");
                if (pronunciation != null)
                {
                    U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(pronunciation);
                }
            }

            if (buildButton != null) StartCoroutine(PunchScale(buildButton.transform, 1.2f));

            currentScore += 100;
            UpdateScoreUI();

            // Give the child 1.4 seconds to see the completed word formula and hear pronunciation
            yield return new WaitForSeconds(1.4f);

            currentRoundIndex++;
            LoadCurrentRound();
        }

        private IEnumerator HandleWrongBuild(DoubleTroubleRound round)
        {
            isProcessingAnswer = true;

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }


            if (promptTMP != null) promptTMP.text = $"<color=#FF8A80>Check the rule: {round.ruleReason}</color>";

            yield return new WaitForSeconds(0.8f);

            if (resultDisplayText != null)
            {
                resultDisplayText.text = "<b>?</b>";
                resultDisplayText.color = new Color(1f, 0.9f, 0.35f);
            }

            isProcessingAnswer = false;
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
        }

        private void OnAllRoundsComplete()
        {
            Debug.Log("<color=green>Double Trouble (Activity 3) Completed! Advancing...</color>");

            if (promptTMP != null) promptTMP.text = "<b>Fantastic! Double Trouble Mastered!</b>";

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
