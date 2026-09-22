using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    [System.Serializable]
    public class SuffixPartyRound
    {
        public string rootWord;
        public string targetSuffix;
        public string fullWord;
        public string meaningPrompt;
        public AudioClip wordAudio;
    }

    public class U1_SA_GM04_SuffixParty_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Activity Data (Optional)")]
        public U1_SA_ActivityDataSO_Masters_Phonics activityData;

        [Header("2. Narration Audio Clips (Voice A)")]
        public AudioClip introInstructionClip;
        public AudioClip correctFeedbackClip;
        public AudioClip failHintClip;
        public AudioClip celebrationClip;

        [Header("3. Scene Slots (Auto-Bound to Hierarchy)")]
        public GameObject slotRootObj;
        public TextMeshProUGUI slotRootText;

        public GameObject slotSuffixObj;
        public TextMeshProUGUI slotSuffixText;

        public GameObject builtWordDisplayObj;
        public TextMeshProUGUI builtWordDisplayText;

        [Header("4. Scene Headers & Status (Auto-Bound to Hierarchy)")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("5. Conveyor Tile Bank (Parent holding Suffix Tiles)")]
        public Transform conveyorTileBank;

        private List<SuffixPartyRound> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        private Coroutine progressTweenCoroutine;

        private readonly string[] allSuffixes = new string[] { "ful", "less", "ness", "ment", "dom", "ship" };

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
            InitializeRounds();
            LoadRound(currentRoundIndex);
        }

        private void AutoBindHierarchyElements()
        {
            // 1. Headers & Texts
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

            // 2. Machine Display Slots (Root on Left, Suffix in Center, BuiltWord on Right)
            Transform machineDisplay = transform.Find("Machine_Display") ?? transform.Find("MachineDisplay");
            if (machineDisplay != null)
            {
                // Slot_Root (Left Window)
                if (slotRootObj == null)
                {
                    Transform sr = machineDisplay.Find("Slot_Root") ?? machineDisplay.Find("SlotRoot") ?? machineDisplay.Find("Slot_Prefix");
                    if (sr != null) slotRootObj = sr.gameObject;
                }
                if (slotRootObj != null && slotRootText == null)
                {
                    slotRootText = slotRootObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                // Slot_Suffix (Center Window - Drop Target)
                if (slotSuffixObj == null)
                {
                    Transform ss = machineDisplay.Find("Slot_Suffix") ?? machineDisplay.Find("SlotSuffix") ?? machineDisplay.Find("Slot_Root_Center");
                    if (ss != null) slotSuffixObj = ss.gameObject;
                }
                if (slotSuffixObj != null && slotSuffixText == null)
                {
                    slotSuffixText = slotSuffixObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                // BuiltWord_Display (Right Window)
                if (builtWordDisplayObj == null)
                {
                    Transform bw = machineDisplay.Find("BuiltWord_Display") ?? machineDisplay.Find("Result_Slot") ?? machineDisplay.Find("Slot_Result");
                    if (bw != null) builtWordDisplayObj = bw.gameObject;
                }
                if (builtWordDisplayObj != null && builtWordDisplayText == null)
                {
                    builtWordDisplayText = builtWordDisplayObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                MakeSlotImageTransparent(slotRootObj);
                MakeSlotImageTransparent(slotSuffixObj);
                MakeSlotImageTransparent(builtWordDisplayObj);
            }

            // 3. TileBank Conveyor
            if (conveyorTileBank == null)
            {
                Transform c = transform.Find("TileBank_Conveyor") ?? transform.Find("TileBank");
                if (c != null) conveyorTileBank = c;
            }

            BindConveyorButtons();
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

        private void BindConveyorButtons()
        {
            if (conveyorTileBank == null) return;

            int index = 0;
            foreach (Transform child in conveyorTileBank)
            {
                if (child == null) continue;

                Button btn = child.GetComponent<Button>();
                if (btn == null) btn = child.gameObject.AddComponent<Button>();

                string sText = (index < allSuffixes.Length) ? allSuffixes[index] : "ful";
                index++;

                TextMeshProUGUI tmp = child.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    tmp.text = $"<b>-{sText}</b>";
                }

                string capturedSuffix = sText;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(child, 1.15f));
                    OnSuffixTileTapped(capturedSuffix);
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
            rounds = new List<SuffixPartyRound>
            {
                new SuffixPartyRound { rootWord = "care", targetSuffix = "ful", fullWord = "careful", meaningPrompt = "Make a word meaning: 'FULL OF CARE'" },
                new SuffixPartyRound { rootWord = "help", targetSuffix = "less", fullWord = "helpless", meaningPrompt = "Make a word meaning: 'WITHOUT HELP'" },
                new SuffixPartyRound { rootWord = "kind", targetSuffix = "ness", fullWord = "kindness", meaningPrompt = "Make a word meaning: 'STATE OF BEING KIND'" },
                new SuffixPartyRound { rootWord = "agree", targetSuffix = "ment", fullWord = "agreement", meaningPrompt = "Make a word meaning: 'ACT OF AGREEING'" },
                new SuffixPartyRound { rootWord = "free", targetSuffix = "dom", fullWord = "freedom", meaningPrompt = "Make a word meaning: 'STATE OF BEING FREE'" },
                new SuffixPartyRound { rootWord = "friend", targetSuffix = "ship", fullWord = "friendship", meaningPrompt = "Make a word meaning: 'STATE OF BEING FRIENDS'" }
            };

            // Auto-link word audio from activity data if available
            if (activityData != null && activityData.targetWords != null)
            {
                foreach (var r in rounds)
                {
                    var found = activityData.targetWords.Find(w => w != null && w.fullWord.ToLower() == r.fullWord.ToLower());
                    if (found != null)
                    {
                        r.wordAudio = found.fullWordAudioClip;
                    }
                }
            }
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
            SuffixPartyRound currentRound = rounds[roundIndex];

            // 1. Prompts
            if (titleTMP != null) titleTMP.text = "<b>SUFFIX PARTY</b>";
            if (promptTMP != null) promptTMP.text = $"<b>{currentRound.meaningPrompt}</b>";

            // 2. Progress Slider
            UpdateProgressBar(roundIndex, rounds.Count);

            // 3. Update Existing Scene Slots
            if (slotRootText != null)
            {
                slotRootText.text = $"<b>{currentRound.rootWord}</b>";
                slotRootText.color = Color.white;
            }

            if (slotSuffixText != null)
            {
                slotSuffixText.text = "<b>[ Tap Suffix ]</b>";
                slotSuffixText.color = new Color(0.6f, 0.85f, 1f);
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

        private void OnSuffixTileTapped(string selectedSuffix)
        {
            if (isProcessingAnswer) return;

            SuffixPartyRound currentRound = rounds[currentRoundIndex];
            string cleanSelected = selectedSuffix.Trim().TrimStart('-').ToLower();
            string cleanTarget = currentRound.targetSuffix.Trim().TrimStart('-').ToLower();

            bool isCorrect = (cleanSelected == cleanTarget);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectAnswer(selectedSuffix, currentRound));
            }
            else
            {
                StartCoroutine(HandleIncorrectAnswer(selectedSuffix));
            }
        }

        private IEnumerator HandleCorrectAnswer(string suffix, SuffixPartyRound round)
        {
            isProcessingAnswer = true;

            // 1. Fill Suffix Slot Text
            if (slotSuffixText != null)
            {
                slotSuffixText.text = $"<b>-{suffix}</b>";
                slotSuffixText.color = new Color(0.2f, 0.95f, 1f);
            }
            if (slotSuffixObj != null) StartCoroutine(PunchScale(slotSuffixObj.transform, 1.18f));

            // 2. Reveal Full Word in BuiltWord_Display
            yield return new WaitForSeconds(0.25f);
            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = $"<b>{round.fullWord}</b>";
                builtWordDisplayText.color = new Color(0.25f, 1f, 0.5f);
            }
            if (builtWordDisplayObj != null) StartCoroutine(PunchScale(builtWordDisplayObj.transform, 1.22f));

            // 3. Audio: Play Voice B Word Model (or Correct Feedback)
            if (U1_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
                if (round.wordAudio != null)
                {
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(round.wordAudio);
                }
                else if (correctFeedbackClip != null)
                {
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(correctFeedbackClip);
                }
            }

            // 4. Update Score
            currentScore += 100;
            UpdateScoreUI();

            yield return new WaitForSeconds(1.4f);

            // 5. Advance
            LoadRound(currentRoundIndex + 1);
        }

        private IEnumerator HandleIncorrectAnswer(string suffix)
        {
            isProcessingAnswer = true;

            if (slotSuffixText != null)
            {
                slotSuffixText.text = $"<b>-{suffix}</b>";
                slotSuffixText.color = new Color(1f, 0.35f, 0.35f);
            }

            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && failHintClip != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(failHintClip);
            }

            if (slotSuffixObj != null)
            {
                Vector3 origPos = slotSuffixObj.transform.localPosition;
                for (int i = 0; i < 6; i++)
                {
                    slotSuffixObj.transform.localPosition = origPos + new Vector3((i % 2 == 0 ? 14 : -14), 0, 0);
                    yield return new WaitForSeconds(0.04f);
                }
                slotSuffixObj.transform.localPosition = origPos;
            }

            yield return new WaitForSeconds(0.4f);

            if (slotSuffixText != null)
            {
                slotSuffixText.text = "<b>[ Tap Suffix ]</b>";
                slotSuffixText.color = new Color(0.6f, 0.85f, 1f);
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
            Debug.Log("<color=green>Suffix Party Completed! Advancing to Word Lab...</color>");

            if (promptTMP != null) promptTMP.text = "<b>Fantastic! Suffix Party Complete!</b>";

            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && celebrationClip != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(celebrationClip);
            }

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
