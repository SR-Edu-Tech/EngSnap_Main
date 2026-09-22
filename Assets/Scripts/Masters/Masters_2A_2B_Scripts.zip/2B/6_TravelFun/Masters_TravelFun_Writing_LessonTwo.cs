using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



public class Masters_TravelFun_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
public class SimpleTravelSentenceItem {
    public string sentenceWithBlank;
    public string correctPhrase;
    public string fullCompletedSentence;
    public string[] options;
    public AudioClip sentenceAudio;
    public AudioClip slowSentenceAudio;
}

    [Header("5 Kid-Friendly Simple Travel Sentence Items")]
    [SerializeField]
    private SimpleTravelSentenceItem[] questions;

    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;

    [Header("Sentence Card")]
    [SerializeField]
    private TextMeshProUGUI instructionTMP;
    [SerializeField]
    private TextMeshProUGUI sentenceTMP;
    [SerializeField]
    private TextMeshProUGUI feedbackTMP;

    [Header("4 Large Option Buttons (RoundedPillCombined)")]
    [SerializeField]
    private Button[] optionButtons;
    private Image[] optionImages;
    private TextMeshProUGUI[] optionTexts;

    [Header("Action & Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Results Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Colors & Styling")]
    [SerializeField]
    private Color defaultButtonColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    [SerializeField]
    private Color correctButtonColor = new Color(0.14f, 0.65f, 0.28f, 1f);
    [SerializeField]
    private Color wrongButtonColor = new Color(0.80f, 0.20f, 0.20f, 1f);

    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNextButtonClicked);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        AutoBindReferences();

        StartCoroutine(StartWithIntroRoutine());
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "Sentence tmt", "Rule", "PhraseCardsGrid", "KeepButton", "FixButton",
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "Subtitle", "TargetIdiomBadge", "SpeakerAPrompt", "InputArea", "WordBankRail"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void AutoBindReferences() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) headerTMP.text = "WRITING BRANCH (Travel Fun)";

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) titleTMP.text = "W02 Write the Travel Diary";

        if (progressTMP == null) {
            Transform pTrans = transform.Find("HeaderContainer/Progress") ?? transform.Find("ExpressionCountTMP");
            if (pTrans != null) progressTMP = pTrans.GetComponent<TextMeshProUGUI>();
        }

        Transform cardTr = transform.Find("SentenceCard");
        if (cardTr != null) {
            if (instructionTMP == null) {
                Transform inTr = cardTr.Find("InstructionText");
                if (inTr != null) instructionTMP = inTr.GetComponent<TextMeshProUGUI>();
            }
            if (sentenceTMP == null) {
                Transform sTr = cardTr.Find("SentenceText");
                if (sTr != null) sentenceTMP = sTr.GetComponent<TextMeshProUGUI>();
            }
            if (feedbackTMP == null) {
                Transform fbTr = cardTr.Find("FeedbackText");
                if (fbTr != null) feedbackTMP = fbTr.GetComponent<TextMeshProUGUI>();
            }
        }

        Transform optsTr = transform.Find("OptionsContainer");
        if (optsTr != null) {
            List<Button> btnList = new List<Button>();
            for (int i = 0; i < optsTr.childCount; i++) {
                Button b = optsTr.GetChild(i).GetComponent<Button>();
                if (b != null) btnList.Add(b);
            }
            if (btnList.Count > 0) optionButtons = btnList.ToArray();
        }

        if (optionButtons != null && optionButtons.Length > 0) {
            optionImages = new Image[optionButtons.Length];
            optionTexts = new TextMeshProUGUI[optionButtons.Length];

            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int idx = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                    optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(idx));
                }
            }
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("audio"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }
    }

    private IEnumerator StartWithIntroRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 2.5f;
            yield return new WaitForSeconds(delay);
        } else {
            yield return new WaitForSeconds(0.3f);
        }

        StartLesson();
    }

    private void StartLesson() {
        if (questions == null || questions.Length == 0) return;

        currentQuestionIndex = 0;
        score = 0;
        isProcessingInput = false;

        LoadQuestion(0);
    }

    private void LoadQuestion(int qIndex) {
        if (qIndex < 0 || qIndex >= questions.Length) {
            ShowResults();
            return;
        }

        currentQuestionIndex = qIndex;
        isProcessingInput = false;

        var q = questions[qIndex];

        // 1. Update Progress
        if (progressTMP != null) {
            progressTMP.text = $"{qIndex + 1}/{questions.Length}";
        }

        // 2. Set Sentence Card
        if (instructionTMP != null) {
            instructionTMP.text = "Complete the sentence:";
        }

        if (sentenceTMP != null) {
            sentenceTMP.text = q.sentenceWithBlank;
            sentenceTMP.transform.DOPunchScale(new Vector3(0.04f, 0.04f, 0f), 0.25f, 4, 0.5f);
        }

        if (feedbackTMP != null) {
            feedbackTMP.text = "";
        }

        // 3. Setup Option Buttons
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] == null) continue;

                bool hasOpt = (q.options != null && i < q.options.Length);
                optionButtons[i].gameObject.SetActive(hasOpt);
                optionButtons[i].interactable = hasOpt;
                optionButtons[i].transform.localScale = Vector3.one;

                if (optionImages != null && optionImages[i] != null) {
                    optionImages[i].color = defaultButtonColor;
                }

                if (hasOpt && optionTexts != null && optionTexts[i] != null) {
                    optionTexts[i].text = q.options[i];
                }
            }
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }
    }

    private void OnOptionButtonClicked(int optionIndex) {
        if (isProcessingInput || questions == null || currentQuestionIndex >= questions.Length) return;

        var q = questions[currentQuestionIndex];
        if (q.options == null || optionIndex < 0 || optionIndex >= q.options.Length) return;

        string chosen = q.options[optionIndex].Trim().ToLower();
        string correct = q.correctPhrase.Trim().ToLower();

        bool isCorrect = (chosen == correct);

        StartCoroutine(ProcessChoiceRoutine(isCorrect, optionIndex, q));
    }

    private IEnumerator ProcessChoiceRoutine(bool isCorrect, int optionIndex, SimpleTravelSentenceItem q) {
        isProcessingInput = true;

        if (isCorrect) {
            // Correct Choice!
            score++;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Green highlight & punch scale
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctButtonColor;
            }
            if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 6, 0.5f);
            }

            // Update sentence with bold turquoise correct phrase
            if (sentenceTMP != null) {
                sentenceTMP.text = q.sentenceWithBlank.Replace("______", $"<b><color=#40E0D0>{q.correctPhrase}</color></b>");
            }

            // Positive feedback text
            if (feedbackTMP != null) {
                feedbackTMP.text = "<b><color=#40E0D0>Great job! 🌟</color></b>";
                feedbackTMP.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.25f, 5, 0.5f);
            }

            // Disable all option buttons
            if (optionButtons != null) {
                foreach (var btn in optionButtons) {
                    if (btn != null) btn.interactable = false;
                }
            }

            // Play voiceover clip of full sentence
            if (q.sentenceAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(q.sentenceAudio);
                float delay = q.sentenceAudio.length > 0 ? q.sentenceAudio.length : 2.5f;
                yield return new WaitForSeconds(delay + 0.3f);
            } else {
                yield return new WaitForSeconds(1.5f);
            }

            // Automatically advance to next question
            isProcessingInput = false;
            LoadQuestion(currentQuestionIndex + 1);
        } else {
            // Wrong Choice!
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            // Red flash & shake
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = wrongButtonColor;
            }
            if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOShakePosition(0.35f, new Vector3(10f, 0f, 0f), 10, 90f);
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "<color=#FF7A7A>Try again! 😊</color>";
            }

            yield return new WaitForSeconds(0.45f);

            // Reset option button color so child can try another option
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = defaultButtonColor;
            }

            isProcessingInput = false;
        }
    }

    private void HandleNextButtonClicked() {
        if (isProcessingInput) return;
        LoadQuestion(currentQuestionIndex + 1);
    }

    private void OnReplayAudioClicked() {
        if (isProcessingInput || questions == null || currentQuestionIndex >= questions.Length) return;

        var q = questions[currentQuestionIndex];
        if (q.sentenceAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(q.sentenceAudio);
        } else if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    private void ShowResults() {
        isProcessingInput = true;

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        int total = (questions != null) ? questions.Length : 5;

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Score: {score} / {total}";
        }

        bool passed = score >= 4;

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ?
                "“Outstanding! You completed all the travel sentences!”" :
                "“Good effort! Try again to complete the sentences!”";
            resultStatusTMP.color = passed ? new Color(0.2f, 0.9f, 0.4f) : new Color(1f, 0.6f, 0.2f);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(false);
            }
        } else {
            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(true);
                retryBtn.interactable = true;
            }
            if (nextButton != null) {
                nextButton.gameObject.SetActive(false);
            }
        }
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
        StartLesson();
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

