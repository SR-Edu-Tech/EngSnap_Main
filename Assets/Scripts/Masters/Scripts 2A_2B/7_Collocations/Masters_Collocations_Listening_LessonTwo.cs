using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Subclass for Unit 7 (Collocations) Listening Lesson Two: Hear the Whole Collocation – True or False?
/// Core gameplay: 10 rounds (5 TRUE, 5 FALSE cross-hub mismatches).
/// Student listens to pair and judges whether it is a real collocation.
/// For FALSE items, ARIA explains the correct hub (e.g., "Ready belongs to GET.") and formed collocation is shown.
/// Pass threshold: 8 out of 10 correct.
/// </summary>
public class Masters_Collocations_Listening_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class CollocationTrueFalseRoundData {
        public string pairText;
        public bool isTrue;
        public string partnerKey;
        public CollocationHub correctHub;
        public string fullCorrectCollocation;
        public AudioClip pairAudio;
        public AudioClip correctionAudio;
    }

    [Header("Unit 7 Collocations Listening L02 Data")]
    [SerializeField] private CollocationTrueFalseRoundData[] rounds;
    [SerializeField] private Button trueButton;
    [SerializeField] private Button falseButton;
    [SerializeField] private TextMeshProUGUI pairTextTMP;
    [SerializeField] private TextMeshProUGUI collocationL02ProgressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTMP;
    [SerializeField] private Button retryButton;

    [Header("Audio & SFX")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip snapSFX;
    [SerializeField] private AudioClip repelSFX;

    [Header("Rules")]
    [SerializeField] private int passScore = 5;

    private int currentQuestionIndex = 0;
    private int correctScore = 0;
    private bool isAnswering = false;

    protected override void Awake() {
        base.Awake();
        AutoFindUIReferences();
    }

    protected override void Start() {
        topic = Masters_Topic.Listening;
        UpdateTitleAndUIComponents();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        ConfigureButtons();

        currentQuestionIndex = 0;
        correctScore = 0;
        StartCoroutine(InitializeCollocationL02Routine());
    }

    private void AutoFindUIReferences() {
        if (pairTextTMP == null) {
            Transform bubble = transform.Find("SpeechBubble") ?? transform.Find("SoundBench/SpeechBubble");
            if (bubble != null) pairTextTMP = bubble.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (collocationL02ProgressTMP == null) {
            Transform prog = transform.Find("ProgressIndicator") ?? transform.Find("ProgressText");
            if (prog != null) collocationL02ProgressTMP = prog.GetComponent<TextMeshProUGUI>();
        }

        if (scoreTMP == null) {
            Transform sc = transform.Find("ScoreIndicator") ?? transform.Find("ScoreText");
            if (sc != null) scoreTMP = sc.GetComponent<TextMeshProUGUI>();
        }

        if (trueButton == null) {
            Transform tBtn = transform.Find("TrueButton") ?? transform.Find("Buttons/TrueButton");
            if (tBtn != null) trueButton = tBtn.GetComponent<Button>();
        }

        if (falseButton == null) {
            Transform fBtn = transform.Find("FalseButton") ?? transform.Find("Buttons/FalseButton");
            if (fBtn != null) falseButton = fBtn.GetComponent<Button>();
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text;
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Occasion") || textVal.Contains("Polished") || textVal.Contains("L01") || textVal.Contains("L02")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "L02 Hear the Whole Collocation – True or False?";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("LISTENING") || textVal.Contains("COMMUNICATION")) {
                tmp.text = "LISTENING BRANCH (Sound Bench)";
            }
        }
    }

    private void ConfigureButtons() {
        if (trueButton != null) {
            trueButton.gameObject.SetActive(true);
            TMP_Text tmp = trueButton.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) {
                tmp.gameObject.SetActive(true);
                tmp.text = "TRUE";
                tmp.color = Color.white;
            }
            trueButton.onClick.RemoveAllListeners();
            trueButton.onClick.AddListener(() => OnAnswerSelected(true));
        }

        if (falseButton != null) {
            falseButton.gameObject.SetActive(true);
            TMP_Text tmp = falseButton.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) {
                tmp.gameObject.SetActive(true);
                tmp.text = "FALSE";
                tmp.color = Color.white;
            }
            falseButton.onClick.RemoveAllListeners();
            falseButton.onClick.AddListener(() => OnAnswerSelected(false));
        }
    }

    private IEnumerator InitializeCollocationL02Routine() {
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        LoadRound(0);
    }

    private void LoadRound(int index) {
        if (rounds == null || index >= rounds.Length) {
            EvaluateFinalScore();
            return;
        }

        currentQuestionIndex = index;
        isAnswering = false;

        CollocationTrueFalseRoundData r = rounds[currentQuestionIndex];
        if (r == null) return;

        if (pairTextTMP != null) {
            pairTextTMP.text = r.pairText;
            pairTextTMP.transform.DOKill();
            pairTextTMP.transform.localScale = Vector3.one;
            pairTextTMP.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
        }

        if (collocationL02ProgressTMP != null) {
            collocationL02ProgressTMP.text = $"Question {currentQuestionIndex + 1}/{rounds.Length}";
        }

        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {correctScore}";
        }

        if (r.pairAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(r.pairAudio);
        }
    }

    private void OnAnswerSelected(bool userChoice) {
        if (isAnswering || rounds == null || currentQuestionIndex >= rounds.Length) return;

        CollocationTrueFalseRoundData r = rounds[currentQuestionIndex];
        if (r == null) return;

        bool isCorrect = (userChoice == r.isTrue);

        if (isCorrect) {
            isAnswering = true;
            correctScore++;

            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {correctScore}";
            }

            // Magnetic Snap Sound
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            }

            // Visual Snap Punch
            Button chosenBtn = userChoice ? trueButton : falseButton;
            if (chosenBtn != null) {
                chosenBtn.transform.DOKill(true);
                chosenBtn.transform.localScale = Vector3.one;
            }

            if (!r.isTrue) {
                // FALSE Item Correctly Identified: ARIA explains the correct hub pairing!
                if (pairTextTMP != null && !string.IsNullOrEmpty(r.fullCorrectCollocation)) {
                    pairTextTMP.text = r.fullCorrectCollocation;
                    pairTextTMP.transform.DOKill(true);
                    pairTextTMP.transform.DOPunchScale(Vector3.one * 0.18f, 0.3f);
                }

                if (r.correctionAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(r.correctionAudio);
                }

                StartCoroutine(NextRoundRoutine(r.correctionAudio != null ? r.correctionAudio.length + 0.5f : 2.2f));
            } else {
                // TRUE Item Correctly Identified
                StartCoroutine(NextRoundRoutine(1.4f));
            }
        } else {
            // Magnetic Repel Sound & Visual Reject
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            Button chosenBtn = userChoice ? trueButton : falseButton;
            if (chosenBtn != null) {
                chosenBtn.transform.DOKill(true);
                chosenBtn.transform.DOShakePosition(0.45f, new Vector3(14f, 0f, 0f), 15, 90f);
            }

            // Keep round active - student can retry
        }
    }

    private IEnumerator NextRoundRoutine(float delay) {
        yield return new WaitForSeconds(delay);
        LoadRound(currentQuestionIndex + 1);
    }

    private void EvaluateFinalScore() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (correctScore >= passScore);

        if (resultTMP != null) {
            if (passed) {
                resultTMP.text = $"MAGNIFICENT! Score: {correctScore}/{rounds.Length}\nListening Branch Complete!";
            } else {
                resultTMP.text = $"KEEP TRYING! Score: {correctScore}/{rounds.Length}\nYou need at least {passScore}/10 to pass.";
            }
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
                NextButtonAnimation();
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (retryButton != null) {
                retryButton.gameObject.SetActive(true);
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(RestartLesson);
            }
        }
    }

    public void RestartLesson() {
        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }
        currentQuestionIndex = 0;
        correctScore = 0;
        LoadRound(0);
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Listening;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}