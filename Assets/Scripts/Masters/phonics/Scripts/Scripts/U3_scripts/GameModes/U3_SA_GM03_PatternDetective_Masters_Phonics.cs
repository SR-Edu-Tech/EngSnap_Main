using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U3_SA_GM03_PatternDetective_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Voice A Audio (Official Unit 3 Doc)")]
        [Tooltip("U03_VO_a5_intro: 'Every word is a pattern of consonants and vowels. Read it left to right and tap C or V for each letter.' (8s)")]
        public AudioClip introInstructionClip;

        [Tooltip("Celebration fanfare clip when activity 5 completes")]
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Center Word & Pattern Display")]
        [SerializeField] private GameObject wordCardObj;
        [SerializeField] private TextMeshProUGUI wordTextTMP;
        [SerializeField] private TextMeshProUGUI userPatternTMP;

        [Header("4. C and V Action Buttons")]
        [SerializeField] private Button btnConsonantC; // C Button
        [SerializeField] private Button btnVowelV;      // V Button
        [SerializeField] private Button btnDelete;      // Delete last input
        [SerializeField] private Button btnReplayAudio; // Replay word button

        private List<PatternItem> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        private string currentPatternInput = "";

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
            if (U3_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U3_SA_UnitFlowManager_Masters_Phonics.Instance.SetCurrentActivityIndex(5);
            }
            AutoBindHierarchyElements();
            currentRoundIndex = 0;
            currentScore = 0;
            currentPatternInput = "";
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
            else
            {
                PlayCurrentWordAudio();
            }
        }

        private IEnumerator DelayedPlayIntro()
        {
            yield return new WaitForSeconds(0.15f);
            if (U3_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
                yield return new WaitForSeconds(introInstructionClip.length + 0.3f);
                PlayCurrentWordAudio();
            }
        }

        public void PlayCurrentWordAudio()
        {
            if (currentRoundIndex >= rounds.Count) return;
            PatternItem item = rounds[currentRoundIndex];

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip clip = item.wordAudio
                              ?? Resources.Load<AudioClip>($"U3_audio/words_U3_MP/{item.word}")
                              ?? Resources.Load<AudioClip>($"Audio/U3_audio/words_U3_MP/{item.word}")
                              ?? Resources.Load<AudioClip>($"U3_audio/{item.word}")
                              ?? Resources.Load<AudioClip>($"Audio/U3_audio/{item.word}")
                              ?? Resources.Load<AudioClip>($"U3_audio/U03_WRD_{item.word}")
                              ?? Resources.Load<AudioClip>($"Audio/U3_audio/U03_WRD_{item.word}");
                if (clip != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
                }
            }
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("title img/TitleText") ?? transform.Find("title img") ?? transform.Find("Title BG") ?? transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (promptTMP == null)
            {
                var t = transform.Find("prompt bg/Prompt_Text") ?? transform.Find("prompt bg") ?? transform.Find("Prompt_Text") ?? transform.Find("PromptText") ?? transform.Find("InstructionText");
                if (t != null) promptTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (scoreTMP == null)
            {
                var t = transform.Find("ScoreText") ?? transform.Find("Score_Text") ?? transform.Find("Score");
                if (t != null) scoreTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("ProgressBar (1)") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            if (wordCardObj == null)
            {
                Transform t = transform.Find("Word_Card") ?? transform.Find("Center_Card") ?? transform.Find("WordDisplay") ?? transform.Find("WordCard");
                if (t != null) wordCardObj = t.gameObject;
            }
            if (wordCardObj != null && wordTextTMP == null)
            {
                var texts = wordCardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (texts.Length > 0) wordTextTMP = texts[0];
                if (texts.Length > 1 && userPatternTMP == null) userPatternTMP = texts[1];
            }

            if (userPatternTMP == null)
            {
                Transform t = transform.Find("UserPattern_Text") ?? transform.Find("PatternText") ?? transform.Find("PatternDisplay") ?? transform.Find("UserPattern");
                if (t != null) userPatternTMP = t.GetComponent<TextMeshProUGUI>();
            }

            // Search all child buttons recursively
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                string n = b.name.ToLower();
                if (btnConsonantC == null && (n.Contains("btn_c") || n.Contains("button_c") || n.Contains("consonant") || n == "c"))
                {
                    btnConsonantC = b;
                }
                else if (btnVowelV == null && (n.Contains("btn_v") || n.Contains("button_v") || n.Contains("vowel") || n == "v"))
                {
                    btnVowelV = b;
                }
                else if (btnDelete == null && (n.Contains("delete") || n.Contains("undo") || n.Contains("backspace") || n.Contains("btn_del")))
                {
                    btnDelete = b;
                }
                else if (btnReplayAudio == null && (n.Contains("replay") || n.Contains("audio") || n.Contains("speaker")))
                {
                    btnReplayAudio = b;
                }
            }

            if (btnConsonantC != null)
            {
                btnConsonantC.onClick.RemoveAllListeners();
                btnConsonantC.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(btnConsonantC.transform, 1.12f));
                    OnPatternInputTapped('C');
                });
            }

            if (btnVowelV != null)
            {
                btnVowelV.onClick.RemoveAllListeners();
                btnVowelV.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(btnVowelV.transform, 1.12f));
                    OnPatternInputTapped('V');
                });
            }

            if (btnDelete != null)
            {
                btnDelete.onClick.RemoveAllListeners();
                btnDelete.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(btnDelete.transform, 1.12f));
                    OnDeleteTapped();
                });
            }

            if (btnReplayAudio != null)
            {
                btnReplayAudio.onClick.RemoveAllListeners();
                btnReplayAudio.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(btnReplayAudio.transform, 1.15f));
                    PlayCurrentWordAudio();
                });
            }
        }

        private void InitializeRounds()
        {
            rounds = new List<PatternItem>
            {
                new PatternItem { word = "a", pattern = "V", typeLabel = "Type 1 (V)" },
                new PatternItem { word = "at", pattern = "VC", typeLabel = "Type 2 (VC)" },
                new PatternItem { word = "go", pattern = "CV", typeLabel = "Type 3 (CV)" },
                new PatternItem { word = "cat", pattern = "CVC", typeLabel = "Type 4 (CVC)" },
                new PatternItem { word = "but", pattern = "CVC", typeLabel = "Type 4 (CVC)" },
                new PatternItem { word = "end", pattern = "VCC", typeLabel = "Type 15 (VCC)" },
                new PatternItem { word = "and", pattern = "VCC", typeLabel = "Type 15 (VCC)" },
                new PatternItem { word = "belt", pattern = "CVCC", typeLabel = "Type 14 (CVCC)" },
                new PatternItem { word = "self", pattern = "CVCC", typeLabel = "Type 14 (CVCC)" },
                new PatternItem { word = "spin", pattern = "CCVC", typeLabel = "CCVC" },
                new PatternItem { word = "glad", pattern = "CCVC", typeLabel = "CCVC" },
                new PatternItem { word = "stand", pattern = "CCVCC", typeLabel = "CCVCC" }
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
            currentPatternInput = "";
            PatternItem item = rounds[currentRoundIndex];

            if (titleTMP != null) titleTMP.text = "<b>PATTERN DETECTIVE</b>";
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>Tap C (Consonant) or V (Vowel) for each letter in the word.</b></color>";

            if (progressBar != null)
            {
                progressBar.value = (float)currentRoundIndex / rounds.Count;
            }

            if (wordTextTMP != null)
            {
                // Format letters with generous tracking for C/V alignment
                string spacedWord = "";
                foreach (char ch in item.word)
                {
                    spacedWord += ch + "   ";
                }
                wordTextTMP.text = $"<b><color=#000000>{spacedWord.Trim()}</color></b>";
            }

            UpdatePatternDisplay();
        }

        public void OnPatternInputTapped(char cv)
        {
            if (isProcessingAnswer || currentRoundIndex >= rounds.Count) return;

            PatternItem item = rounds[currentRoundIndex];
            if (currentPatternInput.Length < item.word.Length)
            {
                currentPatternInput += cv;

                if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    if (cv == 'C') U3_SA_AudioManager_Masters_Phonics.Instance.PlayPatternC();
                    else U3_SA_AudioManager_Masters_Phonics.Instance.PlayPatternV();
                }

                UpdatePatternDisplay();

                // Auto-validate when length matches
                if (currentPatternInput.Length == item.word.Length)
                {
                    if (currentPatternInput == item.pattern)
                    {
                        StartCoroutine(HandleCorrectPattern(item));
                    }
                    else
                    {
                        StartCoroutine(HandleWrongPattern(item));
                    }
                }
            }
        }

        public void OnDeleteTapped()
        {
            if (isProcessingAnswer) return;

            if (currentPatternInput.Length > 0)
            {
                currentPatternInput = currentPatternInput.Substring(0, currentPatternInput.Length - 1);
                if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlaySplitUndo();
                }
                UpdatePatternDisplay();
            }
        }

        private void UpdatePatternDisplay()
        {
            if (userPatternTMP == null) return;

            PatternItem item = rounds[currentRoundIndex];
            string display = "";
            for (int i = 0; i < item.word.Length; i++)
            {
                if (i < currentPatternInput.Length)
                {
                    char c = currentPatternInput[i];
                    string col = (c == 'C') ? "#0288D1" : "#F57C00";
                    display += $"<color={col}><b>{c}</b></color>   ";
                }
                else
                {
                    display += "<color=#90A4AE>_   </color>";
                }
            }

            userPatternTMP.text = $"{display.Trim()}";
        }

        private IEnumerator HandleCorrectPattern(PatternItem item)
        {
            isProcessingAnswer = true;

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFFFFF><b>Correct! \"{item.word}\" has pattern {item.pattern} ({item.typeLabel})!</b></color>";
            }

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();

                AudioClip wordClip = item.wordAudio 
                                  ?? Resources.Load<AudioClip>($"U3_audio/words_U3_MP/{item.word}")
                                  ?? Resources.Load<AudioClip>($"Audio/U3_audio/words_U3_MP/{item.word}")
                                  ?? Resources.Load<AudioClip>($"U3_audio/{item.word}") 
                                  ?? Resources.Load<AudioClip>($"Audio/U3_audio/{item.word}");
                if (wordClip != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(wordClip);
                }
            }

            currentScore += 100;
            UpdateScoreUI();

            yield return new WaitForSeconds(1.4f);

            currentRoundIndex++;
            LoadCurrentRound();
        }

        private IEnumerator HandleWrongPattern(PatternItem item)
        {
            isProcessingAnswer = true;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFD54F><b>Check each letter carefully: \"{item.word}\" is {item.pattern}.</b></color>";
            }

            yield return new WaitForSeconds(1.6f);

            isProcessingAnswer = false;
            currentPatternInput = "";
            UpdatePatternDisplay();
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
        }

        private void OnAllRoundsComplete()
        {
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>Pattern Detective Solved! All 12 Patterns Clear!</b></color>";
            if (progressBar != null) progressBar.value = 1f;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null && celebrationClip != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(celebrationClip);
            }

            StartCoroutine(WaitAndShowCompletion());
        }

        private IEnumerator WaitAndShowCompletion()
        {
            float waitTime = (celebrationClip != null) ? Mathf.Min(celebrationClip.length, 2.5f) : 1.5f;
            yield return new WaitForSeconds(waitTime);

            if (U3_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U3_SA_UnitFlowManager_Masters_Phonics.Instance.ShowUnitCompletion();
            }
        }

        private IEnumerator PunchScale(Transform target, float targetScale)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(original * targetScale, original, t);
                yield return null;
            }
            if (target != null) target.localScale = original;
        }
    }
}
