using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core controller for Unit 1: Boost Someone Up! - Writing Lesson One (W01).
/// Adapted from Book 2A PolishedCommunication Writing L1 to match blueprint standards.
/// </summary>
public class Masters_ConditionalClause_Writing_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class WritingQuestion {
        public string incomingMessageText; // The phrase with a blank
        public string[] acceptableExactMatches; // The missing word(s)
        public string hintText; // The word-bank rail
        public AudioClip correctAudio; // Added to play ARIA VO on correct
    }

    [Header("Writing Lesson Data")]
    [SerializeField] protected WritingQuestion[] questions;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private TextMeshProUGUI hintTMP; // Used as word-bank rail
    
    [Header("Settings & Visual Feedback")]
    [SerializeField] private float timeBetweenQuestions = 1.5f;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultInputFieldColor = Color.white;
    [SerializeField] private Image inputFieldBackground;

    private int currentQuestionIndex;
    private WritingQuestion currentQuestion;
    private bool isTransitioning = false;
    private List<string> remainingWordBank;

    protected override void Awake() {
        base.Awake();
        if (checkButton != null) {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        }
    }

    protected override void Start() {
        base.Start();
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        currentQuestionIndex = 0;
        isTransitioning = false;
        
        remainingWordBank = new List<string>();
        if (questions != null) {
            foreach (var q in questions) {
                if (q.acceptableExactMatches != null && q.acceptableExactMatches.Length > 0) {
                    remainingWordBank.Add(q.acceptableExactMatches[0]);
                }
            }
            // Shuffle word bank
            for (int i = 0; i < remainingWordBank.Count; i++) {
                string temp = remainingWordBank[i];
                int rand = Random.Range(i, remainingWordBank.Count);
                remainingWordBank[i] = remainingWordBank[rand];
                remainingWordBank[rand] = temp;
            }
        }

        StartCoroutine(InitializeLessonRoutine());
    }

    private IEnumerator InitializeLessonRoutine() {
        if (Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        } else {
            yield return new WaitForSeconds(1.0f);
        }
        LoadQuestion(0);
    }

    private void LoadQuestion(int index) {
        if (questions == null || index < 0 || index >= questions.Length) {
            OnAllQuestionsCompleted();
            return;
        }

        currentQuestionIndex = index;
        currentQuestion = questions[index];
        isTransitioning = false;

        if (promptTMP != null) promptTMP.text = currentQuestion.incomingMessageText;
        if (progressCountTMP != null) progressCountTMP.text = $"{index + 1}/{questions.Length}";
        if (hintTMP != null) {
            hintTMP.text = string.Join("   |   ", remainingWordBank);
            hintTMP.transform.parent.gameObject.SetActive(true); // Ensure hint container is visible
        }

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            if (inputFieldBackground != null) inputFieldBackground.color = defaultInputFieldColor;
            inputField.Select();
            inputField.ActivateInputField();
        }

        if (checkButton != null) checkButton.interactable = true;
    }

    private void OnCheckButtonClicked() {
        if (currentQuestion == null || inputField == null || isTransitioning) return;

        string userInput = inputField.text.Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        string normalizedInput = userInput.ToLowerInvariant();
        bool isCorrect = false;

        if (currentQuestion.acceptableExactMatches != null && currentQuestion.acceptableExactMatches.Length > 0) {
            foreach (string exact in currentQuestion.acceptableExactMatches) {
                if (!string.IsNullOrEmpty(exact) && normalizedInput == exact.ToLowerInvariant().Trim()) {
                    isCorrect = true;
                    break;
                }
            }
        }

        if (isCorrect) {
            isTransitioning = true;
            if (inputFieldBackground != null) inputFieldBackground.color = correctColor;
            inputField.interactable = false;
            checkButton.interactable = false;

            if (currentQuestion.acceptableExactMatches != null && currentQuestion.acceptableExactMatches.Length > 0) {
                remainingWordBank.Remove(currentQuestion.acceptableExactMatches[0]);
                if (hintTMP != null) {
                    hintTMP.text = string.Join("   |   ", remainingWordBank);
                }
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (currentQuestion.correctAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(currentQuestion.correctAudio);
                }
            }
            StartCoroutine(NextQuestionRoutine());
        } else {
            if (inputFieldBackground != null) inputFieldBackground.color = incorrectColor;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private IEnumerator NextQuestionRoutine() {
        if (currentQuestion.correctAudio != null && Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        } else {
            yield return new WaitForSeconds(timeBetweenQuestions);
        }
        LoadQuestion(currentQuestionIndex + 1);
    }

    private void OnAllQuestionsCompleted() {
        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            NextButtonAnimation();
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}

