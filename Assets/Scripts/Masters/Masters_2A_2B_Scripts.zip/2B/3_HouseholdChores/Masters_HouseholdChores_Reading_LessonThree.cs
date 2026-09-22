using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// Unit 3: Household Chores — Reading Lesson Three (R03 Who Does What? — Helping My Mom).
/// 8 in-context dialogue jobs to assign to the family member who promised to do it:
/// 3 Character Buttons: NEHA (0), MOM (1), SUHO (2).
/// Features:
/// 1. Introduction voiceover plays first with controls locked.
/// 2. Once introduction completes, the game automatically unlocks and allows play.
/// 3. Student reads each chore sentence and taps the character who said/promised it.
/// 4. Correct -> glowing highlight + ARIA/character dialogue reads aloud; Wrong -> shake + retry.
/// Pass threshold: >= 6 / 8 jobs assigned correctly.
/// </summary>
public class Masters_HouseholdChores_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
public class HouseholdChores_ReadingR03Job {
    public string jobText;
    public string speakerName;
    public int targetSpeakerIndex; // 0 = NEHA, 1 = MOM, 2 = SUHO
    public AudioClip jobAudio;
}

    [Header("8 Configurable Job Items (GDD p.16)")]
    [SerializeField] public HouseholdChores_ReadingR03Job[] jobs;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI jobCardTMP;

    [Header("3 Character Target Buttons (NEHA, MOM, SUHO)")]
    [SerializeField] private Button[] characterButtons = new Button[3];
    [SerializeField] private TextMeshProUGUI[] characterTexts = new TextMeshProUGUI[3];
    [SerializeField] private Image[] characterImages = new Image[3];

    [Header("Audio Controls")]
    [SerializeField] private Button replayAudioBtn;

    [Header("Results Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button continueBtn;

    [Header("Colors & Animation")]
    [SerializeField] private Color normalBtnColor = new Color(0.18f, 0.24f, 0.36f, 1f);
    [SerializeField] private Color correctColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color wrongColor = new Color(0.9f, 0.25f, 0.2f, 1f);

    [Header("Pass Threshold")]
    [SerializeField] private int passThreshold = 6;

    private int currentJobIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool isIntroPhase = true;
    private bool currentJobRetried = false;
    private Coroutine introRoutine;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;

        PurgeLegacyChildren();
        AutoBindReferences();
        WireButtons();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Reading;

        PurgeLegacyChildren();
        AutoBindReferences();
        EnsureHeaderAndTitle();

        if (jobs == null || jobs.Length == 0) {
            PopulateDefaultJobs();
        }

        // Lock controls during introduction
        isIntroPhase = true;
        isProcessingInput = true;
        SetControlsInteractable(false);

        introRoutine = StartCoroutine(BeginLessonRoutine());
    }

    private IEnumerator BeginLessonRoutine() {
        if (progressTMP != null) progressTMP.text = "Introduction";
        if (jobCardTMP != null) {
            jobCardTMP.text = "\"Read each job from the conversation and tap the family member who promised to do it: Neha, Mom, or Suho!\"";
        }

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        StartLesson();
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP != null) headerTMP.text = "HOUSEHOLD CHORES 🧹";
        if (titleTMP != null) {
            titleTMP.text = "R03 Who Does What? — Helping My Mom";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f);
        }
        if (subtitleTMP != null) subtitleTMP.text = "Tap the family member who promised to do this chore!";
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)",
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(lTrans.gameObject);
                else DestroyImmediate(lTrans.gameObject);
            }
        }
    }

    private void SetControlsInteractable(bool interactable) {
        for (int i = 0; i < characterButtons.Length; i++) {
            if (characterButtons[i] != null) {
                characterButtons[i].interactable = interactable;
            }
        }

        if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;
    }

    private void AutoBindReferences() {
        if (headerTMP == null) {
            Transform t = transform.Find("UnitHeading") ?? transform.Find("HeaderTMP") ?? transform.Find("CommonHUD/HeaderTMP") ?? transform.Find("CommonHUD/UnitHeading");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("TitleTMP") ?? transform.Find("CommonHUD/TitleTMP") ?? transform.Find("CommonHUD/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP == null) {
            Transform t = transform.Find("LessonSubtitle") ?? transform.Find("SubtitleTMP") ?? transform.Find("CommonHUD/SubtitleTMP");
            if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP == null) {
            Transform t = transform.Find("ProgressTMP") ?? transform.Find("ExpressionCountTMP") ?? transform.Find("CommonHUD/ProgressTMP");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (scoreTMP == null) {
            Transform t = transform.Find("ScoreTMP") ?? transform.Find("CommonHUD/ScoreTMP");
            if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
        }

        // 3 Character target buttons
        List<Button> foundBtns = new List<Button>();
        string[] candidateNames = new string[] {
            "CharacterButton_0", "CharacterButton_1", "CharacterButton_2",
            "OptionButton", "OptionButton (1)", "OptionButton (2)",
            "PhraseCard_1", "PhraseCard_2", "PhraseCard_3"
        };
        foreach (var name in candidateNames) {
            Transform t = transform.Find(name) ?? transform.Find($"OptionButtonsContainer/{name}") ?? transform.Find($"PhraseCardsGrid/{name}") ?? transform.Find($"Controls/{name}");
            if (t != null) {
                Button b = t.GetComponent<Button>();
                if (b != null && !foundBtns.Contains(b)) {
                    foundBtns.Add(b);
                }
            }
        }

        if (foundBtns.Count < 3) {
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (bName.Contains("back") || bName.Contains("next") || bName.Contains("replay") || bName.Contains("retry") || bName.Contains("continue") || bName.Contains("start")) continue;
                if (!foundBtns.Contains(b)) {
                    foundBtns.Add(b);
                    if (foundBtns.Count == 3) break;
                }
            }
        }

        string[] defaultNames = new string[] { "NEHA", "MOM", "SUHO" };
        for (int i = 0; i < 3 && i < foundBtns.Count; i++) {
            if (characterButtons[i] == null) characterButtons[i] = foundBtns[i];
            if (characterButtons[i] != null) {
                if (characterTexts[i] == null) characterTexts[i] = characterButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (characterImages[i] == null) characterImages[i] = characterButtons[i].GetComponent<Image>();
                if (characterTexts[i] != null) {
                    characterTexts[i].text = defaultNames[i];
                }
            }
        }

        if (jobCardTMP == null) {
            Transform t = transform.Find("SituationCard/Text (TMP)") ?? transform.Find("SpeechBubble/Text (TMP)") ?? transform.Find("PromptCard/Text (TMP)") ?? transform.Find("PromptTMP") ?? transform.Find("WindowSignContainer/Text (TMP)");
            if (t != null) jobCardTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (jobCardTMP == null) {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps) {
                    string tName = tmp.name.ToLower();
                    bool isCharText = false;
                    for (int j = 0; j < 3; j++) {
                        if (characterTexts[j] == tmp) { isCharText = true; break; }
                    }
                    if ((tName.Contains("situation") || tName.Contains("prompt") || tName.Contains("transcript") || tName.Contains("card")) && !isCharText) {
                        jobCardTMP = tmp;
                        break;
                    }
                }
            }
        }

        if (replayAudioBtn == null) {
            Transform t = transform.Find("ReplayBtn") ?? transform.Find("ReplayAudioBtn") ?? transform.Find("SpeakerIcon") ?? transform.Find("CommonHUD/ReplayBtn") ?? transform.Find("SituationCard/SpeakerIcon");
            if (t != null) replayAudioBtn = t.GetComponent<Button>();
        }

        if (resultPanel == null) {
            Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsContainer");
            if (t != null) {
                resultPanel = t.gameObject;
                resultTitleTMP = t.Find("ResultTitle")?.GetComponent<TextMeshProUGUI>() ?? t.Find("TitleTMP")?.GetComponent<TextMeshProUGUI>();
                resultScoreTMP = t.Find("ResultScore")?.GetComponent<TextMeshProUGUI>() ?? t.Find("ScoreTMP")?.GetComponent<TextMeshProUGUI>();
                resultStatusTMP = t.Find("ResultStatus")?.GetComponent<TextMeshProUGUI>() ?? t.Find("StatusTMP")?.GetComponent<TextMeshProUGUI>();
                retryBtn = t.Find("RetryButton")?.GetComponent<Button>() ?? t.Find("RetryBtn")?.GetComponent<Button>();
                continueBtn = t.Find("ReturnHubButton")?.GetComponent<Button>() ?? t.Find("ContinueBtn")?.GetComponent<Button>() ?? t.Find("NextButton")?.GetComponent<Button>();
            }
        }
    }

    private void WireButtons() {
        for (int i = 0; i < 3; i++) {
            int idx = i;
            if (characterButtons[i] != null) {
                characterButtons[i].onClick.RemoveAllListeners();
                characterButtons[i].onClick.AddListener(() => OnCharacterSelected(idx));
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartLesson);
        }

        if (continueBtn != null) {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(OnContinueClicked);
        }
    }

    public void StartLesson() {
        isIntroPhase = false;
        currentJobIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        SetControlsInteractable(true);
        LoadJob(0);
    }

    public void RestartLesson() {
        StartLesson();
    }

    public void LoadJob(int jobIndex) {
        if (jobs == null || jobs.Length == 0) return;
        currentJobIndex = Mathf.Clamp(jobIndex, 0, jobs.Length - 1);
        currentJobRetried = false;
        isProcessingInput = false;
        SetControlsInteractable(true);

        HouseholdChores_ReadingR03Job job = jobs[currentJobIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Job {currentJobIndex + 1} of {jobs.Length}";
        }
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {score}/{jobs.Length}";
        }

        if (jobCardTMP != null) {
            jobCardTMP.text = $"\"{job.jobText}\"";
        }

        string[] names = new string[] { "NEHA", "MOM", "SUHO" };
        for (int i = 0; i < 3; i++) {
            if (characterButtons[i] == null) continue;
            characterButtons[i].gameObject.SetActive(true);
            characterButtons[i].interactable = true;

            if (characterTexts[i] != null) {
                characterTexts[i].text = names[i];
            }

            if (characterImages[i] != null) {
                characterImages[i].color = normalBtnColor;
            }

            characterButtons[i].transform.DOKill();
            characterButtons[i].transform.localScale = Vector3.one * 0.95f;
            characterButtons[i].transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        PlayJobAudio(job);
    }

    private void PlayJobAudio(HouseholdChores_ReadingR03Job job) {
        if (job.jobAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(job.jobAudio);
        }
    }

    public void ReplayCurrentAudio() {
        if (isIntroPhase) {
            if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            }
            return;
        }

        if (jobs != null && currentJobIndex >= 0 && currentJobIndex < jobs.Length) {
            PlayJobAudio(jobs[currentJobIndex]);
            if (replayAudioBtn != null) replayAudioBtn.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f);
        }
    }

    public void OnCharacterSelected(int charIndex) {
        if (isProcessingInput || isIntroPhase || jobs == null || currentJobIndex >= jobs.Length) return;

        HouseholdChores_ReadingR03Job job = jobs[currentJobIndex];
        bool isCorrect = (charIndex == job.targetSpeakerIndex);

        if (isCorrect) {
            isProcessingInput = true;
            if (!currentJobRetried) {
                score++;
            }

            if (characterImages[charIndex] != null) {
                characterImages[charIndex].color = correctColor;
            }
            if (characterButtons[charIndex] != null) {
                characterButtons[charIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (jobCardTMP != null) {
                jobCardTMP.text = $"<color=#55FF88><b>[{job.speakerName}]</b>: \"{job.jobText}\"</color>";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(HandleCorrectJobRoutine(job));
        } else {
            currentJobRetried = true;
            if (characterImages[charIndex] != null) {
                characterImages[charIndex].DOColor(wrongColor, 0.2f).OnComplete(() => {
                    if (characterImages[charIndex] != null) characterImages[charIndex].DOColor(normalBtnColor, 0.3f);
                });
            }
            if (characterButtons[charIndex] != null) {
                characterButtons[charIndex].transform.DOShakePosition(0.35f, 10f, 15);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private IEnumerator HandleCorrectJobRoutine(HouseholdChores_ReadingR03Job job) {
        if (job.jobAudio != null && Masters_AudioManager.Instance != null) {
            yield return new WaitForSeconds(0.2f);
            Masters_AudioManager.Instance.PlayVoiceOver(job.jobAudio);
            yield return new WaitForSeconds(job.jobAudio.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        if (currentJobIndex + 1 < jobs.Length) {
            LoadJob(currentJobIndex + 1);
        } else {
            EndLesson();
        }
    }

    private void EndLesson() {
        bool passed = (score >= passThreshold);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultTitleTMP != null) resultTitleTMP.text = passed ? "Great Job! 🎉" : "Keep Practicing!";
            if (resultScoreTMP != null) resultScoreTMP.text = $"Score: {score} / {jobs.Length}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "You assigned all the household chores to the right family members!" : "Try again to get at least 6 correct!";
                resultStatusTMP.color = passed ? Color.green : Color.yellow;
            }
            if (retryBtn != null) retryBtn.gameObject.SetActive(!passed);
            if (continueBtn != null) continueBtn.gameObject.SetActive(passed);
        } else {
            OnContinueClicked();
        }
    }

    public void OnContinueClicked() {
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Writing);
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Reading);
        } else {
            base.OnNextButtonClicked();
        }
    }

    protected override void OnNextButtonClicked() {
        if (isIntroPhase) {
            if (introRoutine != null) StopCoroutine(introRoutine);
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.StopVoiceOver();
            StartLesson();
            return;
        }

        base.OnNextButtonClicked();
    }

    public void PopulateDefaultJobs() {
        string aDir = "Assets/Audio/2B/3_HouseholdChores/Reading/";

        jobs = new HouseholdChores_ReadingR03Job[] {
            // 1. Suho
            new HouseholdChores_ReadingR03Job {
                jobText = "I will wash the car and do a bit of gardening, too.",
                speakerName = "Suho",
                targetSpeakerIndex = 2, // Suho
#if UNITY_EDITOR
                jobAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r03_q01.mp3")
#endif
            },
            // 2. Neha
            new HouseholdChores_ReadingR03Job {
                jobText = "I will separate the plastic from the wet garbage and also remove cobwebs.",
                speakerName = "Neha",
                targetSpeakerIndex = 0, // Neha
#if UNITY_EDITOR
                jobAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r03_q02.mp3")
#endif
            },
            // 3. Mom
            new HouseholdChores_ReadingR03Job {
                jobText = "I will put all the heavy clothes into the washing machine.",
                speakerName = "Mom",
                targetSpeakerIndex = 1, // Mom
#if UNITY_EDITOR
                jobAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r03_q03.mp3")
#endif
            },
            // 4. Mom (asks Neha) -> Speaker: Mom
            new HouseholdChores_ReadingR03Job {
                jobText = "Get all the dirty clothes and woollens.",
                speakerName = "Mom",
                targetSpeakerIndex = 1, // Mom
#if UNITY_EDITOR
                jobAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r03_q04.mp3")
#endif
            },
            // 5. Mom (asks Suho) -> Speaker: Mom
            new HouseholdChores_ReadingR03Job {
                jobText = "Clean your closet and dust your room.",
                speakerName = "Mom",
                targetSpeakerIndex = 1, // Mom
#if UNITY_EDITOR
                jobAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r03_q05.mp3")
#endif
            },
            // 6. Mom
            new HouseholdChores_ReadingR03Job {
                jobText = "I'm off to the kitchen. It needs a very thorough cleaning.",
                speakerName = "Mom",
                targetSpeakerIndex = 1, // Mom
#if UNITY_EDITOR
                jobAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r03_q06.mp3")
#endif
            },
            // 7. Neha
            new HouseholdChores_ReadingR03Job {
                jobText = "Mom, I will help you in mopping the floor.",
                speakerName = "Neha",
                targetSpeakerIndex = 0, // Neha
#if UNITY_EDITOR
                jobAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r03_q07.mp3")
#endif
            },
            // 8. Neha
            new HouseholdChores_ReadingR03Job {
                jobText = "Today I can help you with the cleaning of our house.",
                speakerName = "Neha",
                targetSpeakerIndex = 0, // Neha
#if UNITY_EDITOR
                jobAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r03_q08.mp3")
#endif
            }
        };
    }
}
