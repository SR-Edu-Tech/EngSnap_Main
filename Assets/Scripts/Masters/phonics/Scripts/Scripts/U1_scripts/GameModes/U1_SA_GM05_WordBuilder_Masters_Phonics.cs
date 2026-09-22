using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    [System.Serializable]
    public class WordLabRound
    {
        public string targetPrefix; // e.g. "dis"
        public string rootWord;     // e.g. "agree"
        public string targetSuffix; // e.g. "able"
        public string fullWord;     // e.g. "disagreeable"
        public string meaningPrompt;// e.g. "Make a word meaning: 'NOT AGREEABLE / UNPLEASANT'"
        public AudioClip wordAudio;
    }

    public class U1_SA_GM05_WordBuilder_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Activity Data (Optional)")]
        public U1_SA_ActivityDataSO_Masters_Phonics currentActivityData;

        [Header("2. Narration Audio Clips (Voice A)")]
        public AudioClip introInstructionClip;
        public AudioClip correctFeedbackClip;
        public AudioClip failHintClip;
        public AudioClip celebrationClip;

        [Header("3. Scene Slots (Auto-Bound to Hierarchy)")]
        public GameObject slotPrefixObj;
        public TextMeshProUGUI slotPrefixText;

        public GameObject slotRootObj;
        public TextMeshProUGUI slotRootText;

        public GameObject slotSuffixObj;
        public TextMeshProUGUI slotSuffixText;

        public GameObject builtWordDisplayObj;
        public TextMeshProUGUI builtWordDisplayText;

        public Button buildButton;

        [Header("4. Scene Headers & Status (Auto-Bound to Hierarchy)")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI wordsFoundTMP;
        [SerializeField] private Slider progressBar;

        [Header("5. Side Shelf Racks (Auto-Bound to Hierarchy)")]
        public Transform prefixShelfLeft;
        public Transform suffixShelfRight;
        public Transform conveyorTileBank;

        private AudioSource localAudioSource;
        private List<WordLabRound> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        private Coroutine progressTweenCoroutine;
        private Coroutine idleHintCoroutine;

        // Current round player selections
        private string selectedPrefix = "";
        private string selectedSuffix = "";

        // Track shelf tile transforms for hints
        private List<Transform> prefixTileTransforms = new List<Transform>();
        private List<string> prefixTileKeys = new List<string>();

        private List<Transform> suffixTileTransforms = new List<Transform>();
        private List<string> suffixTileKeys = new List<string>();

        private void Awake()
        {
            localAudioSource = GetComponent<AudioSource>();
            if (localAudioSource == null) localAudioSource = gameObject.AddComponent<AudioSource>();

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

            PlayAudioClip(introInstructionClip, true);
        }

        public void Initialize(U1_SA_ActivityDataSO_Masters_Phonics data)
        {
            currentActivityData = data;
            currentRoundIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            InitializeRounds();
            LoadRound(currentRoundIndex);
        }

        private void AutoBindHierarchyElements()
        {
            // 1. Headers & Status
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

            if (wordsFoundTMP == null)
            {
                var t = transform.Find("WordsFound_Text") ?? transform.Find("FoundWords_Log");
                if (t != null) wordsFoundTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("ProgressBar (1)") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            // 2. Machine Display Slots
            Transform machineDisplay = transform.Find("Machine_Display") ?? transform.Find("MachineDisplay");
            if (machineDisplay != null)
            {
                // Slot_Prefix
                if (slotPrefixObj == null)
                {
                    Transform sp = machineDisplay.Find("Slot_Prefix") ?? machineDisplay.Find("SlotPrefix") ?? machineDisplay.Find("PREFIX");
                    if (sp != null) slotPrefixObj = sp.gameObject;
                }
                if (slotPrefixObj != null && slotPrefixText == null)
                {
                    slotPrefixText = slotPrefixObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                // Slot_Root / BASE
                if (slotRootObj == null)
                {
                    Transform sr = machineDisplay.Find("Slot_Root") ?? machineDisplay.Find("SlotRoot") ?? machineDisplay.Find("BASE") ?? machineDisplay.Find("Slot_Base");
                    if (sr != null) slotRootObj = sr.gameObject;
                }
                if (slotRootObj != null && slotRootText == null)
                {
                    slotRootText = slotRootObj.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                // Slot_Suffix
                if (slotSuffixObj == null)
                {
                    Transform ss = machineDisplay.Find("Slot_Suffix") ?? machineDisplay.Find("SlotSuffix") ?? machineDisplay.Find("SUFFIX");
                    if (ss != null) slotSuffixObj = ss.gameObject;
                }
                if (slotSuffixObj != null && slotSuffixText == null)
                {
                    slotSuffixText = slotSuffixObj.GetComponentInChildren<TextMeshProUGUI>(true);
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
            }

            // 3. BUILD Button search (Search everywhere under panel)
            if (buildButton == null)
            {
                Button[] allButtons = GetComponentsInChildren<Button>(true);
                foreach (var btn in allButtons)
                {
                    if (btn == null) continue;
                    string bName = btn.name.ToLower();
                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    string bTxt = (btnText != null) ? btnText.text.ToLower() : "";

                    if (bName.Contains("build") || bTxt.Contains("build"))
                    {
                        buildButton = btn;
                        break;
                    }
                }
            }

            if (buildButton != null)
            {
                buildButton.onClick.RemoveAllListeners();
                buildButton.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(buildButton.transform, 1.15f));
                    OnBuildButtonClicked();
                });
            }

            // 4. Side Shelf Racks
            if (prefixShelfLeft == null)
            {
                prefixShelfLeft = transform.Find("PrefixShelf_Left") ?? transform.Find("TileBank_Prefix") ?? transform.Find("Prefix_Bank");
            }

            if (suffixShelfRight == null)
            {
                suffixShelfRight = transform.Find("SuffixShelf_Right") ?? transform.Find("TileBank_Suffix") ?? transform.Find("Suffix_Bank");
            }

            if (conveyorTileBank == null)
            {
                conveyorTileBank = transform.Find("TileBank_Conveyor") ?? transform.Find("TileBank");
            }

            BindSideRacks();
            StyleProgressBar();
        }

        private void BindSideRacks()
        {
            // 1. Left Prefix Shelf - Extract key by inspecting child's name and text
            if (prefixShelfLeft != null)
            {
                prefixTileTransforms.Clear();
                prefixTileKeys.Clear();

                foreach (Transform child in prefixShelfLeft)
                {
                    if (child == null) continue;

                    Button btn = child.GetComponent<Button>();
                    if (btn == null) btn = child.gameObject.AddComponent<Button>();

                    TextMeshProUGUI tmp = child.GetComponentInChildren<TextMeshProUGUI>(true);
                    string raw = child.name.ToLower();
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                    {
                        raw += " " + tmp.text.ToLower();
                    }

                    string pKey = "un";
                    if (raw.Contains("dis")) pKey = "dis";
                    else if (raw.Contains("mis")) pKey = "mis";
                    else if (raw.Contains("re")) pKey = "re";
                    else if (raw.Contains("pre")) pKey = "pre";
                    else if (raw.Contains("im")) pKey = "im";
                    else if (raw.Contains("in")) pKey = "in";
                    else if (raw.Contains("un")) pKey = "un";

                    prefixTileTransforms.Add(child);
                    prefixTileKeys.Add(pKey);

                    if (tmp != null)
                    {
                        tmp.text = $"<b>{pKey}-</b>";
                    }

                    string captured = pKey;
                    Transform capturedTr = child;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        StartCoroutine(PunchScale(capturedTr, 1.12f));
                        OnPrefixSelected(captured);
                    });
                }
            }

            // 2. Right Suffix Shelf - Extract key by inspecting child's name and text
            if (suffixShelfRight != null)
            {
                suffixTileTransforms.Clear();
                suffixTileKeys.Clear();

                foreach (Transform child in suffixShelfRight)
                {
                    if (child == null) continue;

                    Button btn = child.GetComponent<Button>();
                    if (btn == null) btn = child.gameObject.AddComponent<Button>();

                    TextMeshProUGUI tmp = child.GetComponentInChildren<TextMeshProUGUI>(true);
                    string raw = child.name.ToLower();
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text))
                    {
                        raw += " " + tmp.text.ToLower();
                    }

                    string sKey = "able";
                    if (raw.Contains("ment")) sKey = "ment";
                    else if (raw.Contains("ful")) sKey = "ful";
                    else if (raw.Contains("ance")) sKey = "ance";
                    else if (raw.Contains("ness")) sKey = "ness";
                    else if (raw.Contains("less")) sKey = "less";
                    else if (raw.Contains("dom")) sKey = "dom";
                    else if (raw.Contains("ship")) sKey = "ship";
                    else if (raw.Contains("able")) sKey = "able";

                    suffixTileTransforms.Add(child);
                    suffixTileKeys.Add(sKey);

                    if (tmp != null)
                    {
                        tmp.text = $"<b>-{sKey}</b>";
                    }

                    string captured = sKey;
                    Transform capturedTr = child;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        StartCoroutine(PunchScale(capturedTr, 1.12f));
                        OnSuffixSelected(captured);
                    });
                }
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
            rounds = new List<WordLabRound>
            {
                new WordLabRound { targetPrefix = "dis", rootWord = "agree", targetSuffix = "able", fullWord = "disagreeable", meaningPrompt = "Make a word meaning: 'NOT AGREEABLE / UNPLEASANT'" },
                new WordLabRound { targetPrefix = "un", rootWord = "break", targetSuffix = "able", fullWord = "unbreakable", meaningPrompt = "Make a word meaning: 'NOT ABLE TO BE BROKEN'" },
                new WordLabRound { targetPrefix = "re", rootWord = "place", targetSuffix = "ment", fullWord = "replacement", meaningPrompt = "Make a word meaning: 'THE ACT OF REPLACING'" },
                new WordLabRound { targetPrefix = "un", rootWord = "help", targetSuffix = "ful", fullWord = "unhelpful", meaningPrompt = "Make a word meaning: 'NOT HELPFUL'" },
                new WordLabRound { targetPrefix = "mis", rootWord = "place", targetSuffix = "ment", fullWord = "misplacement", meaningPrompt = "Make a word meaning: 'PLACING IN WRONG SPOT'" },
                new WordLabRound { targetPrefix = "dis", rootWord = "appear", targetSuffix = "ance", fullWord = "disappearance", meaningPrompt = "Make a word meaning: 'THE STATE OF DISAPPEARING'" }
            };

            // Link wordAudio from activity data or Resources
            if (currentActivityData != null && currentActivityData.targetWords != null)
            {
                foreach (var r in rounds)
                {
                    var found = currentActivityData.targetWords.Find(w => w != null && w.fullWord.ToLower() == r.fullWord.ToLower());
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
            selectedPrefix = "";
            selectedSuffix = "";
            WordLabRound currentRound = rounds[roundIndex];

            Debug.Log($"<color=cyan>[WordLab] Loading Round {roundIndex + 1}/{rounds.Count}: Target = '{currentRound.targetPrefix}' + '{currentRound.rootWord}' + '{currentRound.targetSuffix}' -> '{currentRound.fullWord}'</color>");

            // 1. Prompts
            if (titleTMP != null) titleTMP.text = "<b>WORD LAB</b>";
            if (promptTMP != null) promptTMP.text = $"<b>{currentRound.meaningPrompt}</b>";

            if (wordsFoundTMP != null)
            {
                wordsFoundTMP.text = $"Words found: <b>{roundIndex}/{rounds.Count}</b>";
            }

            // 2. Progress Slider
            UpdateProgressBar(roundIndex, rounds.Count);

            // 3. Reset Machine Slots
            if (slotPrefixText != null)
            {
                slotPrefixText.text = "<b>[ Prefix ]</b>";
                slotPrefixText.color = new Color(0.6f, 0.85f, 1f);
            }

            if (slotRootText != null)
            {
                slotRootText.text = $"<b>{currentRound.rootWord}</b>";
                slotRootText.color = Color.white;
            }

            if (slotSuffixText != null)
            {
                slotSuffixText.text = "<b>[ Suffix ]</b>";
                slotSuffixText.color = new Color(1f, 0.6f, 0.85f);
            }

            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = "<b>?</b>";
                builtWordDisplayText.color = new Color(1f, 0.9f, 0.35f);
            }

            // 4. Start Subtle Idle Hint timer
            if (idleHintCoroutine != null) StopCoroutine(idleHintCoroutine);
            idleHintCoroutine = StartCoroutine(IdleHintSequence(currentRound));
        }

        private IEnumerator IdleHintSequence(WordLabRound round)
        {
            yield return new WaitForSeconds(4.0f);

            // Subtle pulse on the correct prefix shelf tile
            for (int i = 0; i < prefixTileKeys.Count; i++)
            {
                if (prefixTileKeys[i].ToLower() == round.targetPrefix.ToLower() && i < prefixTileTransforms.Count)
                {
                    StartCoroutine(SubtleGlowPulse(prefixTileTransforms[i]));
                    break;
                }
            }

            // Subtle pulse on the correct suffix shelf tile
            for (int i = 0; i < suffixTileKeys.Count; i++)
            {
                if (suffixTileKeys[i].ToLower() == round.targetSuffix.ToLower() && i < suffixTileTransforms.Count)
                {
                    StartCoroutine(SubtleGlowPulse(suffixTileTransforms[i]));
                    break;
                }
            }
        }

        private IEnumerator SubtleGlowPulse(Transform tr)
        {
            if (tr == null) yield break;
            Vector3 original = tr.localScale;
            for (int p = 0; p < 2; p++)
            {
                tr.localScale = original * 1.10f;
                yield return new WaitForSeconds(0.18f);
                tr.localScale = original;
                yield return new WaitForSeconds(0.18f);
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

        public void OnPrefixSelected(string prefix)
        {
            if (isProcessingAnswer) return;
            selectedPrefix = prefix.Trim().TrimEnd('-').ToLower();

            Debug.Log($"[WordLab] Prefix Selected: '{selectedPrefix}'");

            if (slotPrefixText != null)
            {
                slotPrefixText.text = $"<b>{prefix}-</b>";
                slotPrefixText.color = new Color(0.2f, 0.95f, 1f);
            }
            if (slotPrefixObj != null) StartCoroutine(PunchScale(slotPrefixObj.transform, 1.15f));

            if (!string.IsNullOrEmpty(selectedPrefix) && !string.IsNullOrEmpty(selectedSuffix))
            {
                if (buildButton != null) StartCoroutine(PunchScale(buildButton.transform, 1.1f));
            }
        }

        public void OnSuffixSelected(string suffix)
        {
            if (isProcessingAnswer) return;
            selectedSuffix = suffix.Trim().TrimStart('-').ToLower();

            Debug.Log($"[WordLab] Suffix Selected: '{selectedSuffix}'");

            if (slotSuffixText != null)
            {
                slotSuffixText.text = $"<b>-{suffix}</b>";
                slotSuffixText.color = new Color(1f, 0.35f, 0.95f);
            }
            if (slotSuffixObj != null) StartCoroutine(PunchScale(slotSuffixObj.transform, 1.15f));

            if (!string.IsNullOrEmpty(selectedPrefix) && !string.IsNullOrEmpty(selectedSuffix))
            {
                if (buildButton != null) StartCoroutine(PunchScale(buildButton.transform, 1.1f));
            }
        }

        public void OnBuildButtonClicked()
        {
            if (isProcessingAnswer) return;

            if (string.IsNullOrEmpty(selectedPrefix) || string.IsNullOrEmpty(selectedSuffix))
            {
                Debug.LogWarning("[WordLab] Cannot build: Prefix or Suffix not chosen yet.");
                if (promptTMP != null) promptTMP.text = "<b>Select both a Prefix on the left and a Suffix on the right!</b>";
                return;
            }

            WordLabRound currentRound = rounds[currentRoundIndex];
            string pClean = selectedPrefix.Trim().TrimEnd('-').ToLower();
            string sClean = selectedSuffix.Trim().TrimStart('-').ToLower();
            string targetPClean = currentRound.targetPrefix.Trim().TrimEnd('-').ToLower();
            string targetSClean = currentRound.targetSuffix.Trim().TrimStart('-').ToLower();

            Debug.Log($"[WordLab] Checking Build: Player='{pClean}' + '{currentRound.rootWord}' + '{sClean}' vs Target='{targetPClean}' + '{currentRound.rootWord}' + '{targetSClean}'");

            bool isCorrect = (pClean == targetPClean && sClean == targetSClean);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectAssembly(currentRound));
            }
            else
            {
                StartCoroutine(HandleIncorrectAssembly(currentRound));
            }
        }

        private IEnumerator HandleCorrectAssembly(WordLabRound round)
        {
            isProcessingAnswer = true;
            if (idleHintCoroutine != null) StopCoroutine(idleHintCoroutine);

            Debug.Log($"<color=green>[WordLab] CORRECT! Built: '{round.fullWord}'</color>");

            // 1. Highlight all 3 machine slots in neon green
            if (slotPrefixText != null)
            {
                slotPrefixText.text = $"<b>{round.targetPrefix}-</b>";
                slotPrefixText.color = new Color(0.25f, 1f, 0.5f);
            }
            if (slotRootText != null)
            {
                slotRootText.color = new Color(0.25f, 1f, 0.5f);
            }
            if (slotSuffixText != null)
            {
                slotSuffixText.text = $"<b>-{round.targetSuffix}</b>";
                slotSuffixText.color = new Color(0.25f, 1f, 0.5f);
            }

            // 2. Reveal in BuiltWord_Display
            if (builtWordDisplayText != null)
            {
                builtWordDisplayText.text = $"<b>{round.fullWord}</b>";
                builtWordDisplayText.color = new Color(0.25f, 1f, 0.5f);
            }
            if (builtWordDisplayObj != null) StartCoroutine(PunchScale(builtWordDisplayObj.transform, 1.25f));

            // 3. Audio: Play clean Voice B pronunciation (or Voice A correct praise)
            if (round.wordAudio != null)
            {
                PlayAudioClip(round.wordAudio, false);
            }
            else if (correctFeedbackClip != null)
            {
                PlayAudioClip(correctFeedbackClip, true);
            }

            // 4. Score
            currentScore += 100;
            UpdateScoreUI();

            yield return new WaitForSeconds(1.5f);

            // 5. Advance
            LoadRound(currentRoundIndex + 1);
        }

        private IEnumerator HandleIncorrectAssembly(WordLabRound round)
        {
            isProcessingAnswer = true;

            Debug.LogWarning($"<color=red>[WordLab] INCORRECT attempt: '{selectedPrefix}' + '{round.rootWord}' + '{selectedSuffix}'</color>");

            if (failHintClip != null)
            {
                PlayAudioClip(failHintClip, true);
            }

            // Subtle Hint on prompt
            if (promptTMP != null)
            {
                promptTMP.text = $"<b>Try: {round.targetPrefix}- + {round.rootWord} + -{round.targetSuffix}!</b>";
            }

            // Shake machine slots
            if (slotPrefixObj != null && slotSuffixObj != null)
            {
                Vector3 origP = slotPrefixObj.transform.localPosition;
                Vector3 origS = slotSuffixObj.transform.localPosition;
                for (int i = 0; i < 6; i++)
                {
                    float off = (i % 2 == 0 ? 12 : -12);
                    slotPrefixObj.transform.localPosition = origP + new Vector3(off, 0, 0);
                    slotSuffixObj.transform.localPosition = origS + new Vector3(off, 0, 0);
                    yield return new WaitForSeconds(0.04f);
                }
                slotPrefixObj.transform.localPosition = origP;
                slotSuffixObj.transform.localPosition = origS;
            }

            yield return new WaitForSeconds(0.5f);

            // Reset slots
            selectedPrefix = "";
            selectedSuffix = "";

            if (slotPrefixText != null)
            {
                slotPrefixText.text = "<b>[ Prefix ]</b>";
                slotPrefixText.color = new Color(0.6f, 0.85f, 1f);
            }

            if (slotSuffixText != null)
            {
                slotSuffixText.text = "<b>[ Suffix ]</b>";
                slotSuffixText.color = new Color(1f, 0.6f, 0.85f);
            }

            isProcessingAnswer = false;
        }

        private void PlayAudioClip(AudioClip clip, bool isVoiceA)
        {
            if (clip == null) return;

            if (U1_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                if (isVoiceA)
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
                else
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            }
            else if (localAudioSource != null)
            {
                localAudioSource.Stop();
                localAudioSource.PlayOneShot(clip);
            }
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
            Debug.Log("<color=green>[WordLab] Word Lab Completed! Advancing to Unit Completion...</color>");

            if (promptTMP != null) promptTMP.text = "<b>Unit One Mastered! Excellent Work!</b>";

            PlayAudioClip(celebrationClip, true);

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
