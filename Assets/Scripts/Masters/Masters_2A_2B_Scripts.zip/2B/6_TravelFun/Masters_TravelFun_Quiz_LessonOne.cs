using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



public class Masters_TravelFun_Quiz_LessonOne : Masters_Lesson {

[System.Serializable]
public class TravelFun_QuizQuestionItem {
    public string questionText;
    public string correctOption;
    public string[] distractorOptions;
    public AudioClip questionAudio;
}

    [Header("12 Quiz Questions Data")]
    [SerializeField]
    private TravelFun_QuizQuestionItem[] questions = new TravelFun_QuizQuestionItem[] {
        // Q1: MCQ
        new TravelFun_QuizQuestionItem {
            questionText = "What does \"touch down\" mean?",
            correctOption = "Land (Planes)",
            distractorOptions = new string[] { "Leave the ground & fly", "Start a journey", "Delay when travelling" }
        },
        // Q2: MCQ
        new TravelFun_QuizQuestionItem {
            questionText = "What does \"stop over\" mean?",
            correctOption = "Stay somewhere for a short time during a long journey",
            distractorOptions = new string[] { "Enter a bus or train", "Pay the bill and leave hotel", "Say goodbye at station" }
        },
        // Q3: MATCH
        new TravelFun_QuizQuestionItem {
            questionText = "Hold up",
            correctOption = "Delay when travelling",
            distractorOptions = new string[] { "Arrive and register at a hotel", "Board the airplane early", "Leave a bus or train" }
        },
        // Q4: MATCH
        new TravelFun_QuizQuestionItem {
            questionText = "See off",
            correctOption = "Go to the airport or station to say goodbye to someone",
            distractorOptions = new string[] { "Enter a train", "Leave the hotel", "Delay the trip" }
        },
        // Q5: PAIR (2 Options)
        new TravelFun_QuizQuestionItem {
            questionText = "You climb aboard the bus — get on or get off?",
            correctOption = "GET ON",
            distractorOptions = new string[] { "GET OFF" }
        },
        // Q6: PAIR (2 Options)
        new TravelFun_QuizQuestionItem {
            questionText = "You pay and leave the hotel — check in or check out?",
            correctOption = "CHECK OUT",
            distractorOptions = new string[] { "CHECK IN" }
        },
        // Q7: FILL
        new TravelFun_QuizQuestionItem {
            questionText = "The train _____ late.",
            correctOption = "got in",
            distractorOptions = new string[] { "set off", "picked up", "touched down" }
        },
        // Q8: FILL
        new TravelFun_QuizQuestionItem {
            questionText = "Next weekend we're hoping to _____ to the sea side.",
            correctOption = "get away",
            distractorOptions = new string[] { "check out", "hold up", "see off" }
        },
        // Q9: PICTURE
        new TravelFun_QuizQuestionItem {
            questionText = "A plane lifting off the runway",
            correctOption = "take off",
            distractorOptions = new string[] { "touch down", "get on", "check in" }
        },
        // Q10: ORDER
        new TravelFun_QuizQuestionItem {
            questionText = "Which comes first on a journey:\ncheck out → set off → check in",
            correctOption = "set off → check in → check out",
            distractorOptions = new string[] { "check out → check in → set off", "check in → set off → check out", "set off → check out → check in" }
        },
        // Q11: ODD ONE OUT
        new TravelFun_QuizQuestionItem {
            questionText = "Which phrase does NOT belong to this unit?",
            correctOption = "Look up",
            distractorOptions = new string[] { "Hurry up", "Pick up", "Set off" }
        },
        // Q12: TRUE / FALSE (2 Options)
        new TravelFun_QuizQuestionItem {
            questionText = "\"Get in\" means to arrive, when we talk about a train or plane.",
            correctOption = "TRUE",
            distractorOptions = new string[] { "FALSE" }
        }
    };

    [Header("UI Header & Stats")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI questionTextTMP;

    [Header("Answer Option Buttons")]
    [SerializeField] private Button[] optionButtons = new Button[0]; // 4 buttons

    [Header("Audio Controls")]
    [SerializeField] private Button replayAudioBtn;

    [Header("Results & Retry Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;

    [Header("Feedback Banner")]
    [SerializeField] private TextMeshProUGUI feedbackTMP;

    [Header("Colors & Styling")]
    [SerializeField] private Color defaultOptionColor = new Color(0.12f, 0.42f, 0.68f, 1f);
    [SerializeField] private Color correctColor = new Color(0.15f, 0.72f, 0.35f, 1f);
    [SerializeField] private Color wrongColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentRoundHasRetried = false;

    private const int PASS_MARK = 9;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;

        AutoBindReferences();

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartQuiz);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNextButtonClicked);
            nextButton.gameObject.SetActive(false);
        }

        if (resultPanel != null) resultPanel.SetActive(false);
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Quiz;
        AutoBindReferences();
        EnsureNextAndBackButtonWired();
        StartCoroutine(StartWithIntroRoutine());
    }

    private void AutoBindReferences() {
        if (headerTMP == null) {
            Transform h = transform.Find("HeaderContainer/Branch") ?? transform.Find("HeaderContainer/Title") ?? transform.Find("Header");
            if (h != null) headerTMP = h.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) headerTMP.text = "QUIZ BRANCH (Departure Gate Quiz)";

        if (titleTMP == null) {
            Transform t = transform.Find("HeaderContainer/Title") ?? transform.Find("LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "Q01 Departure Gate Quiz — Travel Fun";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f);
        }

        if (progressTMP == null) {
            Transform p = transform.Find("TopStatsContainer/Progress") ?? transform.Find("Progress");
            if (p != null) progressTMP = p.GetComponent<TextMeshProUGUI>();
        }

        if (scoreTMP == null) {
            Transform s = transform.Find("TopStatsContainer/Score") ?? transform.Find("Score");
            if (s != null) scoreTMP = s.GetComponent<TextMeshProUGUI>();
        }

        if (questionTextTMP == null) {
            Transform q = transform.Find("QuestionContainer/QuestionText") ?? transform.Find("QuestionText");
            if (q != null) questionTextTMP = q.GetComponent<TextMeshProUGUI>();
        }

        if (replayAudioBtn == null) {
            Transform r = transform.Find("QuestionContainer/RepeatAudioButton") ?? transform.Find("RepeatAudioButton");
            if (r != null) replayAudioBtn = r.GetComponent<Button>();
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(PlayCurrentQuestionAudio);
        }

        if (feedbackTMP == null) {
            Transform fb = transform.Find("FeedbackBanner");
            if (fb != null) feedbackTMP = fb.GetComponent<TextMeshProUGUI>();
        }

        // Bind Option Buttons
        Transform grid = transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
        if (grid != null) {
            List<Button> bList = new List<Button>();
            for (int i = 0; i < grid.childCount; i++) {
                Button btn = grid.GetChild(i).GetComponent<Button>();
                if (btn != null) {
                    bList.Add(btn);
                    // Ensure child text does NOT block raycasts
                    TextMeshProUGUI childTxt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (childTxt != null) {
                        childTxt.raycastTarget = false;
                    }
                }
            }
            optionButtons = bList.ToArray();
        }

        // Result Panel
        if (resultPanel == null) {
            Transform res = transform.Find("ResultPanel");
            if (res != null) resultPanel = res.gameObject;
        }

        if (resultPanel != null) {
            if (resultTitleTMP == null) {
                Transform t = resultPanel.transform.Find("Title") ?? resultPanel.transform.Find("ResultTitle");
                if (t != null) resultTitleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (resultScoreTMP == null) {
                Transform s = resultPanel.transform.Find("Score") ?? resultPanel.transform.Find("ResultScore");
                if (s != null) resultScoreTMP = s.GetComponent<TextMeshProUGUI>();
            }
            if (resultStatusTMP == null) {
                Transform st = resultPanel.transform.Find("Status") ?? resultPanel.transform.Find("ResultStatus");
                if (st != null) resultStatusTMP = st.GetComponent<TextMeshProUGUI>();
            }
            if (retryBtn == null) {
                Transform rb = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                if (rb != null) retryBtn = rb.GetComponent<Button>();
            }
        }
    }

    private IEnumerator StartWithIntroRoutine() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 3.0f;
            yield return new WaitForSeconds(delay);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        RestartQuiz();
    }

    public void RestartQuiz() {
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;
        currentRoundHasRetried = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        ShowQuestion(0);
    }

    private void ShowQuestion(int index) {
        if (questions == null || questions.Length == 0) return;
        if (index < 0 || index >= questions.Length) {
            EndQuiz();
            return;
        }

        currentRoundIndex = index;
        isProcessingInput = false;
        currentRoundHasRetried = false;

        var q = questions[index];

        // Update Progress & Score
        if (progressTMP != null) progressTMP.text = $"Question {index + 1} / {questions.Length}";
        if (scoreTMP != null) scoreTMP.text = $"Score: {score} / {questions.Length}";

        // Update Question Text
        if (questionTextTMP != null) questionTextTMP.text = q.questionText;

        // Hide Feedback
        if (feedbackTMP != null) {
            feedbackTMP.text = "";
            feedbackTMP.gameObject.SetActive(false);
        }

        // Build Options List (Correct + Distractors)
        List<string> currentOptions = new List<string>();
        currentOptions.Add(q.correctOption);
        if (q.distractorOptions != null) {
            foreach (var d in q.distractorOptions) {
                if (!string.IsNullOrEmpty(d)) currentOptions.Add(d);
            }
        }

        // Keep True/False and Pair in natural order, shuffle MCQs
        bool isTwoOption = (currentOptions.Count == 2);
        if (!isTwoOption) {
            // Fisher-Yates Shuffle
            for (int i = 0; i < currentOptions.Count; i++) {
                int rnd = Random.Range(i, currentOptions.Count);
                string temp = currentOptions[i];
                currentOptions[i] = currentOptions[rnd];
                currentOptions[rnd] = temp;
            }
        }

        // Populate Buttons
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] == null) continue;

            int btnIdx = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(btnIdx));

            if (i < currentOptions.Count) {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;
                optionButtons[i].transform.localScale = Vector3.one;

                Image img = optionButtons[i].GetComponent<Image>();
                if (img != null) img.color = defaultOptionColor;

                TextMeshProUGUI txt = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null) {
                    txt.text = currentOptions[i];
                    txt.color = Color.white;
                    txt.raycastTarget = false;
                }
            } else {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        PlayCurrentQuestionAudio();
    }

    private void PlayCurrentQuestionAudio() {
        if (currentRoundIndex >= 0 && currentRoundIndex < questions.Length) {
            AudioClip clip = questions[currentRoundIndex].questionAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }
    }

    private void OnOptionButtonClicked(int buttonIndex) {
        if (isProcessingInput || buttonIndex < 0 || buttonIndex >= optionButtons.Length) return;
        if (currentRoundIndex < 0 || currentRoundIndex >= questions.Length) return;

        var q = questions[currentRoundIndex];
        TextMeshProUGUI clickedTxt = optionButtons[buttonIndex].GetComponentInChildren<TextMeshProUGUI>(true);
        if (clickedTxt == null) return;

        bool isCorrect = clickedTxt.text.Trim().Equals(q.correctOption.Trim(), System.StringComparison.OrdinalIgnoreCase);

        StartCoroutine(HandleOptionAnswer(buttonIndex, isCorrect, q));
    }

    private IEnumerator HandleOptionAnswer(int buttonIndex, bool isCorrect, TravelFun_QuizQuestionItem q) {
        isProcessingInput = true;

        // Lock buttons
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] != null) optionButtons[i].interactable = false;
        }

        Image btnImg = optionButtons[buttonIndex].GetComponent<Image>();

        if (isCorrect) {
            if (!currentRoundHasRetried) {
                score++;
                if (scoreTMP != null) scoreTMP.text = $"Score: {score} / {questions.Length}";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (btnImg != null) btnImg.color = correctColor;
            optionButtons[buttonIndex].transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 5, 0.5f);

            if (feedbackTMP != null) {
                feedbackTMP.gameObject.SetActive(true);
                feedbackTMP.text = "<color=#40FF70>CORRECT! 🌟</color>";
            }

            yield return new WaitForSeconds(1.5f);
            ShowQuestion(currentRoundIndex + 1);
        } else {
            // Incorrect
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (btnImg != null) btnImg.color = wrongColor;
            optionButtons[buttonIndex].transform.DOShakePosition(0.3f, new Vector3(8f, 0f, 0f), 10, 90f);

            if (!currentRoundHasRetried) {
                // Friendly Retry Allowed
                currentRoundHasRetried = true;

                if (feedbackTMP != null) {
                    feedbackTMP.gameObject.SetActive(true);
                    feedbackTMP.text = "<color=#FFCC00>Try Again!</color>";
                }

                yield return new WaitForSeconds(1.0f);

                // Reset button colors and re-enable for 1 retry
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null && optionButtons[i].gameObject.activeSelf) {
                        Image img = optionButtons[i].GetComponent<Image>();
                        if (img != null) img.color = defaultOptionColor;
                        optionButtons[i].interactable = true;
                    }
                }
                isProcessingInput = false;
            } else {
                // Second error -> reveal correct answer and move on
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] == null) continue;
                    TextMeshProUGUI txt = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null && txt.text.Trim().Equals(q.correctOption.Trim(), System.StringComparison.OrdinalIgnoreCase)) {
                        Image img = optionButtons[i].GetComponent<Image>();
                        if (img != null) img.color = correctColor;
                    }
                }

                if (feedbackTMP != null) {
                    feedbackTMP.gameObject.SetActive(true);
                    feedbackTMP.text = $"<color=#FF5555>Correct Answer:</color> <b>{q.correctOption}</b>";
                }

                yield return new WaitForSeconds(2.0f);
                ShowQuestion(currentRoundIndex + 1);
            }
        }
    }

    private void EndQuiz() {
        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        bool passed = (score >= PASS_MARK);

        if (resultTitleTMP != null) {
            resultTitleTMP.text = passed ? "QUIZ COMPLETE! 🌟" : "QUIZ INCOMPLETE";
            resultTitleTMP.color = passed ? new Color(0.2f, 0.95f, 0.4f) : new Color(1f, 0.45f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Your Score: {score} / {questions.Length}";
            resultScoreTMP.color = passed ? new Color(1f, 0.9f, 0.2f) : Color.white;
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ?
                "“Great Job! You passed!”" :
                "“Good Try! You need 9 / 12 to pass.”";
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
            nextButton.interactable = passed;
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
            retryBtn.interactable = !passed;
        }
    }

    private void HandleNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        topic = Masters_Topic.Quiz;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

