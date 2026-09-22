using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U3_SA_GM04_ChinTap_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Voice A Audio (Official Unit 3 Doc)")]
        [Tooltip("U03_VO_a1_intro: 'Listen to the word, then tap the pad once for every beat you hear. Replay it as many times as you like.' (8s)")]
        public AudioClip introInstructionClip;

        [Tooltip("U03_VO_a1_hint: 'Say it out loud with your hand under your chin. Count the drops.' (5s)")]
        public AudioClip hintChinMethodClip;

        [Tooltip("Celebration fanfare clip when activity 1 completes")]
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Center Word Display")]
        [SerializeField] private GameObject wordCardObj;
        [SerializeField] private TextMeshProUGUI wordTextTMP;
        [SerializeField] private Button replayWordAudioButton;

        [Header("4. Beat Pad Interactive Component")]
        [SerializeField] private Button beatPadButton;
        [SerializeField] private Transform beatDotsContainer;
        [SerializeField] private TextMeshProUGUI beatCountTMP;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button lockInButton;

        [Header("5. Dot Sprites (Active vs Inactive)")]
        public Sprite dotActiveSprite;
        public Sprite dotInactiveSprite;

        private List<SyllableWordItem> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private int currentTapCount = 0;
        private bool isProcessingAnswer = false;
        private List<Image> spawnedDots = new List<Image>();

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
            currentTapCount = 0;
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
            if (U3_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
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

            // 1. Center Word Card & Word Text
            if (wordCardObj == null)
            {
                Transform t = transform.Find("Word_Card") ?? transform.Find("Center_Card") ?? transform.Find("WordDisplay");
                if (t != null) wordCardObj = t.gameObject;
            }
            if (wordCardObj != null && wordTextTMP == null)
            {
                wordTextTMP = wordCardObj.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // 2. Replay Audio Button
            if (replayWordAudioButton == null)
            {
                Transform t = transform.Find("ReplayButton") ?? transform.Find("AudioButton") ?? transform.Find("Speaker_Button");
                if (t == null && wordCardObj != null)
                {
                    t = wordCardObj.transform.Find("ReplayButton") ?? wordCardObj.transform.Find("AudioButton") ?? wordCardObj.transform.Find("Speaker_Button");
                }
                if (t != null) replayWordAudioButton = t.GetComponent<Button>();
            }
            if (replayWordAudioButton != null)
            {
                replayWordAudioButton.onClick.RemoveAllListeners();
                replayWordAudioButton.onClick.AddListener(PlayCurrentWordAudio);
            }

            // 3. Beat Dots Container
            if (beatDotsContainer == null)
            {
                Transform t = transform.Find("Beat_Dots_Container") ?? transform.Find("Dots_Row") ?? transform.Find("Dots");
                if (t == null && wordCardObj != null)
                {
                    t = wordCardObj.transform.Find("Beat_Dots_Container") ?? wordCardObj.transform.Find("Dots_Row") ?? wordCardObj.transform.Find("Dots");
                }
                if (t != null) beatDotsContainer = t;
            }

            // 4. Beat Count Text
            if (beatCountTMP == null)
            {
                Transform t = transform.Find("BeatCount_Text") ?? transform.Find("BeatsText") ?? transform.Find("CountText");
                if (t == null && wordCardObj != null)
                {
                    t = wordCardObj.transform.Find("BeatCount_Text") ?? wordCardObj.transform.Find("BeatsText") ?? wordCardObj.transform.Find("CountText");
                }
                if (t != null) beatCountTMP = t.GetComponent<TextMeshProUGUI>();
            }

            // 5. Large Interactive Beat Pad
            if (beatPadButton == null)
            {
                Transform t = transform.Find("Beat_Pad") ?? transform.Find("BeatPad_Button") ?? transform.Find("Btn_TapHere") ?? transform.Find("Beat Pad");
                if (t != null) beatPadButton = t.GetComponent<Button>();
            }
            if (beatPadButton != null)
            {
                beatPadButton.onClick.RemoveAllListeners();
                beatPadButton.onClick.AddListener(OnBeatPadTapped);
            }

            // 6. Action Buttons (CLEAR & LOCK IN)
            if (clearButton == null)
            {
                Transform t = transform.Find("Btn_Clear") ?? transform.Find("ClearButton") ?? transform.Find("ActionButtons/Btn_Clear");
                if (t == null && wordCardObj != null)
                {
                    t = wordCardObj.transform.Find("Btn_Clear") ?? wordCardObj.transform.Find("ClearButton") ?? wordCardObj.transform.Find("ActionButtons/Btn_Clear");
                }
                if (t != null) clearButton = t.GetComponent<Button>();
            }
            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(OnClearTapped);
            }

            if (lockInButton == null)
            {
                Transform t = transform.Find("Btn_LockIn") ?? transform.Find("LockInButton") ?? transform.Find("Submit_Button") ?? transform.Find("ActionButtons/Btn_LockIn");
                if (t == null && wordCardObj != null)
                {
                    t = wordCardObj.transform.Find("Btn_LockIn") ?? wordCardObj.transform.Find("LockInButton") ?? wordCardObj.transform.Find("Submit_Button") ?? wordCardObj.transform.Find("ActionButtons/Btn_LockIn");
                }
                if (t != null) lockInButton = t.GetComponent<Button>();
            }
            if (lockInButton != null)
            {
                lockInButton.onClick.RemoveAllListeners();
                lockInButton.onClick.AddListener(OnLockInTapped);
            }
        }

        private void InitializeRounds()
        {
            rounds = new List<SyllableWordItem>
            {
                // Round A: 1-2 Syllables
                new SyllableWordItem { word = "pen", syllableCount = 1, hyphenatedSplit = "pen", chunks = new[] { "pen" }, category = SyllableCategory.Monosyllabic },
                new SyllableWordItem { word = "match", syllableCount = 1, hyphenatedSplit = "match", chunks = new[] { "match" }, category = SyllableCategory.Monosyllabic },
                new SyllableWordItem { word = "pencil", syllableCount = 2, hyphenatedSplit = "pen-cil", chunks = new[] { "pen", "cil" }, category = SyllableCategory.Disyllabic },
                new SyllableWordItem { word = "rabbit", syllableCount = 2, hyphenatedSplit = "rab-bit", chunks = new[] { "rab", "bit" }, category = SyllableCategory.Disyllabic },

                // Round B: 2-3 Syllables
                new SyllableWordItem { word = "candle", syllableCount = 2, hyphenatedSplit = "can-dle", chunks = new[] { "can", "dle" }, category = SyllableCategory.Disyllabic },
                new SyllableWordItem { word = "laptop", syllableCount = 2, hyphenatedSplit = "lap-top", chunks = new[] { "lap", "top" }, category = SyllableCategory.Disyllabic },
                new SyllableWordItem { word = "carnival", syllableCount = 3, hyphenatedSplit = "car-ni-val", chunks = new[] { "car", "ni", "val" }, category = SyllableCategory.Trisyllabic },
                new SyllableWordItem { word = "carpenter", syllableCount = 3, hyphenatedSplit = "car-pen-ter", chunks = new[] { "car", "pen", "ter" }, category = SyllableCategory.Trisyllabic },

                // Round C: 3-4 Syllables
                new SyllableWordItem { word = "harmony", syllableCount = 3, hyphenatedSplit = "har-mo-ny", chunks = new[] { "har", "mo", "ny" }, category = SyllableCategory.Trisyllabic },
                new SyllableWordItem { word = "pharmacy", syllableCount = 3, hyphenatedSplit = "phar-ma-cy", chunks = new[] { "phar", "ma", "cy" }, category = SyllableCategory.Trisyllabic },
                new SyllableWordItem { word = "material", syllableCount = 4, hyphenatedSplit = "ma-te-ri-al", chunks = new[] { "ma", "te", "ri", "al" }, category = SyllableCategory.Polysyllabic },
                new SyllableWordItem { word = "convenient", syllableCount = 4, hyphenatedSplit = "con-ve-ni-ent", chunks = new[] { "con", "ve", "ni", "ent" }, category = SyllableCategory.Polysyllabic },

                // Round D: Mixed 1-5 Syllables
                new SyllableWordItem { word = "quack", syllableCount = 1, hyphenatedSplit = "quack", chunks = new[] { "quack" }, category = SyllableCategory.Monosyllabic },
                new SyllableWordItem { word = "butter", syllableCount = 2, hyphenatedSplit = "but-ter", chunks = new[] { "but", "ter" }, category = SyllableCategory.Disyllabic },
                new SyllableWordItem { word = "calculator", syllableCount = 4, hyphenatedSplit = "cal-cu-la-tor", chunks = new[] { "cal", "cu", "la", "tor" }, category = SyllableCategory.Polysyllabic },
                new SyllableWordItem { word = "hippopotamus", syllableCount = 5, hyphenatedSplit = "hip-po-pot-a-mus", chunks = new[] { "hip", "po", "pot", "a", "mus" }, category = SyllableCategory.Polysyllabic }
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
            currentTapCount = 0;
            UpdateDotsDisplay();

            SyllableWordItem item = rounds[currentRoundIndex];

            if (titleTMP != null) titleTMP.text = "<b>CHIN TAP</b>";
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>Listen & tap the pad once for each beat you hear!</b></color>";

            if (progressBar != null)
            {
                progressBar.value = (float)currentRoundIndex / rounds.Count;
            }

            if (wordTextTMP != null)
            {
                wordTextTMP.text = $"<b><color=#000000>{item.word}</color></b>";
            }

            PlayCurrentWordAudio();
        }

        public void PlayCurrentWordAudio()
        {
            if (currentRoundIndex >= rounds.Count) return;
            SyllableWordItem item = rounds[currentRoundIndex];

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

        public void OnBeatPadTapped()
        {
            if (isProcessingAnswer) return;

            if (currentTapCount < 6)
            {
                currentTapCount++;
                if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayBeat();
                }

                if (beatPadButton != null)
                {
                    StartCoroutine(PunchScale(beatPadButton.transform, 1.12f));
                }

                UpdateDotsDisplay();
            }
        }

        public void OnClearTapped()
        {
            if (isProcessingAnswer) return;
            currentTapCount = 0;
            UpdateDotsDisplay();
            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
            }
        }

        private void UpdateDotsDisplay()
        {
            if (beatCountTMP != null)
            {
                beatCountTMP.text = (currentTapCount == 0) ? "<color=#556677>0 beats</color>" : $"<b><color=#000000>{currentTapCount} beat{(currentTapCount > 1 ? "s" : "")}</color></b>";
            }

            EnsureDotsContainer();
            for (int i = 0; i < spawnedDots.Count; i++)
            {
                if (spawnedDots[i] != null)
                {
                    bool isActive = (i < currentTapCount);
                    if (dotActiveSprite != null && dotInactiveSprite != null)
                    {
                        spawnedDots[i].sprite = isActive ? dotActiveSprite : dotInactiveSprite;
                        spawnedDots[i].color = Color.white;
                    }
                    else
                    {
                        spawnedDots[i].color = isActive ? new Color(1f, 0.75f, 0.1f) : new Color(0.7f, 0.75f, 0.8f, 0.4f);
                    }
                }
            }
        }

        private void EnsureDotsContainer()
        {
            if (beatDotsContainer == null) return;

            if (spawnedDots.Count == 0)
            {
                spawnedDots.Clear();
                for (int i = 0; i < 5; i++)
                {
                    GameObject dotObj = new GameObject($"Dot_{i + 1}", typeof(RectTransform), typeof(Image));
                    dotObj.transform.SetParent(beatDotsContainer, false);
                    RectTransform rt = dotObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(36, 36);
                    Image img = dotObj.GetComponent<Image>();
                    spawnedDots.Add(img);
                }
            }
        }

        public void OnLockInTapped()
        {
            if (isProcessingAnswer || currentRoundIndex >= rounds.Count) return;
            if (currentTapCount == 0) return;

            SyllableWordItem item = rounds[currentRoundIndex];
            bool isCorrect = (currentTapCount == item.syllableCount);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectAnswer(item));
            }
            else
            {
                StartCoroutine(HandleWrongAnswer(item));
            }
        }

        private IEnumerator HandleCorrectAnswer(SyllableWordItem item)
        {
            isProcessingAnswer = true;

            if (wordTextTMP != null)
            {
                wordTextTMP.text = $"<b><color=#007722>{item.hyphenatedSplit}</color></b>";
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFFFFF><b>Correct! \"{item.word}\" has {item.syllableCount} beat{(item.syllableCount > 1 ? "s" : "")} ({item.category})!</b></color>";
            }

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();

                AudioClip sepClip = item.separatedAudio ?? Resources.Load<AudioClip>($"U3_audio/sep_{item.word}") ?? Resources.Load<AudioClip>($"Audio/U3_audio/sep_{item.word}");
                if (sepClip != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(sepClip);
                }
            }

            currentScore += 100;
            UpdateScoreUI();

            yield return new WaitForSeconds(1.5f);

            currentRoundIndex++;
            LoadCurrentRound();
        }

        private IEnumerator HandleWrongAnswer(SyllableWordItem item)
        {
            isProcessingAnswer = true;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFD54F><b>Listen again! \"{item.word}\" has {item.syllableCount} beat{(item.syllableCount > 1 ? "s" : "")} ({item.hyphenatedSplit}).</b></color>";
            }

            yield return new WaitForSeconds(1.8f);

            isProcessingAnswer = false;
            currentTapCount = 0;
            UpdateDotsDisplay();
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
        }

        private void OnAllRoundsComplete()
        {
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>Great Beat Tapping! Chin Tap Mastered!</b></color>";
            if (progressBar != null) progressBar.value = 1f;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null && celebrationClip != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(celebrationClip);
            }

            if (U3_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U3_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }

        private IEnumerator PunchScale(Transform tr, float scaleMult)
        {
            Vector3 orig = Vector3.one;
            tr.localScale = orig * scaleMult;
            yield return new WaitForSeconds(0.12f);
            tr.localScale = orig;
        }
    }
}
