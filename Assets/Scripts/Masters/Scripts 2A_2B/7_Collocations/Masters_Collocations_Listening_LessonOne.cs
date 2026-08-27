using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum CollocationHub {
    GET = 0,
    CATCH = 1,
    IDEA = 2,
    SAVE = 3
}

/// <summary>
/// Subclass for Unit 7 (Collocations) Listening Lesson One: Hear It - Which Hub Does It Belong To?
/// Core gameplay: 12 balanced rounds across GET, CATCH, IDEA, and SAVE hubs.
/// Audio-to-hub recognition with snap animation on correct answer and repel/retry on wrong answer.
/// Pass threshold: 9 out of 12 correct.
/// </summary>
public class Masters_Collocations_Listening_LessonOne : Masters_PolishedCommunication_Listening_LessonOne {

    [System.Serializable]
    public class CollocationQuestionData {
        public string partnerText;
        public CollocationHub correctHub;
        public string fullCollocationText;
        public AudioClip partnerAudio;
        public AudioClip fullCollocationAudio;
    }

    [Header("Unit 7 Collocations Listening L1 Data")]
    [SerializeField] private CollocationQuestionData[] collocationQuestions;
    [SerializeField] private Button[] hubButtons; // Index 0: GET, Index 1: CATCH, Index 2: IDEA, Index 3: SAVE
    [SerializeField] private TextMeshProUGUI spokenWordTMP;
    [SerializeField] private TextMeshProUGUI collocationProgressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTMP;
    [SerializeField] private Button retryButton;

    [Header("Audio & SFX")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip snapSFX;
    [SerializeField] private AudioClip repelSFX;

    [Header("Rules")]
    [SerializeField] private int passScore = 6;

    protected override void Awake() {
        base.Awake();
        narratorSpeech = null;
        AutoFindUIReferences();
    }

    protected override void Start() {
        narratorSpeech = null;
        topic = Masters_Topic.Listening;
        UpdateTitleAndUIComponents();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        ConfigureHubButtons();

        currentQuestionIndex = 0;
        correctScore = 0;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        StartCoroutine(InitializeCollocationLessonRoutine());
    }

    private IEnumerator InitializeCollocationLessonRoutine() {
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
            yield return new WaitForSeconds(ariaIntroAudio.length + 0.2f);
        } else {
            yield return new WaitForSeconds(0.3f);
        }

        LoadCollocationQuestion(0);
    }

    private void AutoFindUIReferences() {
        if (spokenWordTMP == null) {
            Transform bubble = transform.Find("SpeechBubble") ?? transform.Find("SoundBench/SpeechBubble");
            if (bubble != null) spokenWordTMP = bubble.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (collocationProgressTMP == null) {
            Transform prog = transform.Find("ProgressIndicator") ?? transform.Find("ProgressText");
            if (prog != null) collocationProgressTMP = prog.GetComponent<TextMeshProUGUI>();
        }

        if (scoreTMP == null) {
            Transform sc = transform.Find("ScoreIndicator") ?? transform.Find("ScoreText");
            if (sc != null) scoreTMP = sc.GetComponent<TextMeshProUGUI>();
        }

        if (hubButtons == null || hubButtons.Length < 4) {
            Transform hubContainer = transform.Find("HubButtons") ?? transform.Find("Hubs");
            if (hubContainer != null) {
                Button[] btns = hubContainer.GetComponentsInChildren<Button>(true);
                if (btns.Length >= 4) {
                    hubButtons = new Button[4];
                    for (int i = 0; i < 4 && i < btns.Length; i++) {
                        hubButtons[i] = btns[i];
                    }
                }
            }
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text;
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Occasion") || textVal.Contains("Polished") || textVal.Contains("L01")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "L01 Hear It – Which Hub Does It Belong To?";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("LISTENING") || textVal.Contains("COMMUNICATION")) {
                tmp.text = "LISTENING BRANCH (Sound Bench)";
            }
        }
    }

    private void ConfigureHubButtons() {
        if (hubButtons == null || hubButtons.Length == 0) return;

        string[] defaultLabels = new string[] { "GET", "CATCH", "IDEA", "SAVE" };
        Color[] hubColors = new Color[] {
            new Color(0.9f, 0.32f, 0.32f, 1f), // GET - Coral Red
            new Color(0.2f, 0.72f, 0.45f, 1f), // CATCH - Emerald Green
            new Color(0.28f, 0.55f, 0.9f, 1f), // IDEA - Sky Blue
            new Color(0.95f, 0.65f, 0.2f, 1f)  // SAVE - Warm Orange
        };

        for (int i = 0; i < hubButtons.Length; i++) {
            if (hubButtons[i] == null) continue;

            hubButtons[i].gameObject.SetActive(true);
            Image btnImg = hubButtons[i].GetComponent<Image>();
            if (btnImg != null) {
                btnImg.preserveAspect = false;
                btnImg.color = hubColors[i % hubColors.Length];
            }

            TMP_Text tmp = hubButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) {
                tmp.gameObject.SetActive(true);
                tmp.text = defaultLabels[i % defaultLabels.Length];
                tmp.color = Color.white;
            }

            int hubIdx = i;
            hubButtons[i].onClick.RemoveAllListeners();
            hubButtons[i].onClick.AddListener(() => OnHubSelected(hubIdx));
        }
    }

    private void LoadCollocationQuestion(int index) {
        if (collocationQuestions == null || index >= collocationQuestions.Length) {
            EvaluateFinalScore();
            return;
        }

        currentQuestionIndex = index;
        isAnswering = false;

        CollocationQuestionData q = collocationQuestions[currentQuestionIndex];
        if (q == null) return;

        if (spokenWordTMP != null) {
            spokenWordTMP.text = q.partnerText;
            spokenWordTMP.transform.DOKill();
            spokenWordTMP.transform.localScale = Vector3.one;
            spokenWordTMP.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
        }

        if (collocationProgressTMP != null) {
            collocationProgressTMP.text = $"Question {currentQuestionIndex + 1}/{collocationQuestions.Length}";
        }

        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {correctScore}";
        }

        if (q.partnerAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(q.partnerAudio);
        }
    }

    private void OnHubSelected(int hubIndex) {
        if (isAnswering || collocationQuestions == null || currentQuestionIndex >= collocationQuestions.Length) return;

        CollocationQuestionData q = collocationQuestions[currentQuestionIndex];
        if (q == null) return;

        bool isCorrect = ((int)q.correctHub == hubIndex);

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

            // Magnetic Visual Snap Animation
            if (hubIndex < hubButtons.Length && hubButtons[hubIndex] != null) {
                hubButtons[hubIndex].transform.DOKill(true);
                hubButtons[hubIndex].transform.localScale = Vector3.one;
            }

            // Update speech bubble to show full formed collocation
            if (spokenWordTMP != null) {
                spokenWordTMP.text = q.fullCollocationText;
                spokenWordTMP.transform.DOKill(true);
                spokenWordTMP.transform.DOPunchScale(Vector3.one * 0.2f, 0.35f);
            }

            // Play complete collocation voice
            if (q.fullCollocationAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(q.fullCollocationAudio);
            }

            StartCoroutine(NextCollocationQuestionRoutine());
        } else {
            // Magnetic Repel Sound & Visual Reject
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (hubIndex < hubButtons.Length && hubButtons[hubIndex] != null) {
                hubButtons[hubIndex].transform.DOKill(true);
                hubButtons[hubIndex].transform.DOShakePosition(0.45f, new Vector3(14f, 0f, 0f), 15, 90f);
            }

            // Keep round active - student can try another hub
        }
    }

    private IEnumerator NextCollocationQuestionRoutine() {
        yield return new WaitForSeconds(1.6f);
        LoadCollocationQuestion(currentQuestionIndex + 1);
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
                resultTMP.text = $"EXCELLENT! Score: {correctScore}/{collocationQuestions.Length}\nYou matched the hubs!";
            } else {
                resultTMP.text = $"TRY AGAIN! Score: {correctScore}/{collocationQuestions.Length}\nYou need at least {passScore}/12 to pass.";
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
        LoadCollocationQuestion(0);
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Listening;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}