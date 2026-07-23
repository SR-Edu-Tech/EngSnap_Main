using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_JumbledWords_Writing_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class WritingQuestion {
        [TextArea]
        public string jumbledSentence;
        
        [Tooltip("List all acceptable grammatically correct variations of the sentence. (Punctuation and case are ignored in comparison)")]
        public string[] acceptableCorrectSentences;

        [Tooltip("The required ending punctuation, e.g., . or ?")]
        public string requiredPunctuation = ".";
    }

    [SerializeField]
    private WritingQuestion[] questions;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI jumbledSentenceTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Animation & Feedback")]
    [SerializeField] private float timeBetweenQuestions = 1.5f;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultInputFieldColor = Color.white;
    [SerializeField] private Image inputFieldBackground;

    private int currentQuestionIndex = 0;
    private bool canCheck = false;

    protected override void Awake() {
        base.Awake();
        checkButton.onClick.AddListener(OnCheckButtonClicked);
    }

    protected override void Start() {
        base.Start();
        LoadQuestion(0);
    }

    private void LoadQuestion(int index) {
        if (index >= questions.Length) {
            // Lesson over
            inputField.gameObject.SetActive(false);
            jumbledSentenceTMP.gameObject.SetActive(false);
            checkButton.gameObject.SetActive(false);
            
            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        currentQuestionIndex = index;
        progressCountTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";

        WritingQuestion question = questions[currentQuestionIndex];
        
        jumbledSentenceTMP.text = question.jumbledSentence;
        inputField.text = "";
        
        if (inputFieldBackground != null) {
            inputFieldBackground.color = defaultInputFieldColor;
        }

        canCheck = true;
        checkButton.interactable = true;
    }

    private void OnCheckButtonClicked() {
        if (!canCheck) return;

        string input = inputField.text.Trim();
        if (string.IsNullOrEmpty(input)) {
            // Give a little shake if empty
            inputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            return;
        }

        WritingQuestion currentQ = questions[currentQuestionIndex];

        // 1. Check ending punctuation
        if (!input.EndsWith(currentQ.requiredPunctuation)) {
            WrongAnswer();
            return;
        }

        // 2. Validate words multiset
        List<string> inputWords = ExtractWords(input);
        List<string> jumbledWords = ExtractWords(currentQ.jumbledSentence);

        if (inputWords.Count != jumbledWords.Count) {
            WrongAnswer();
            return;
        }

        List<string> sortedInput = new List<string>(inputWords);
        List<string> sortedJumbled = new List<string>(jumbledWords);
        sortedInput.Sort();
        sortedJumbled.Sort();

        for (int i = 0; i < sortedInput.Count; i++) {
            if (sortedInput[i] != sortedJumbled[i]) {
                WrongAnswer();
                return;
            }
        }

        // 3. Check against acceptable grammatically sound orders
        bool isGrammaticallySound = false;
        string normalizedInput = string.Join(" ", inputWords);

        if (currentQ.acceptableCorrectSentences != null && currentQ.acceptableCorrectSentences.Length > 0) {
            foreach (string correctVariation in currentQ.acceptableCorrectSentences) {
                if (string.IsNullOrEmpty(correctVariation)) continue;
                
                string normalizedCorrect = string.Join(" ", ExtractWords(correctVariation));
                if (normalizedInput == normalizedCorrect) {
                    isGrammaticallySound = true;
                    break;
                }
            }
        } else {
            // If the designer hasn't added any acceptable variations, we fallback to just the multiset check.
            isGrammaticallySound = true; 
        }

        if (!isGrammaticallySound) {
            WrongAnswer();
            return;
        }

        CorrectAnswer();
    }

    private List<string> ExtractWords(string sentence) {
        // Remove common terminal/separating punctuation, keep internal hyphens or apostrophes
        string clean = sentence.Replace(",", "").Replace(".", "").Replace("?", "").Replace("!", "").Replace(";", "").Replace(":", "").ToLower();
        return clean.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).OrderBy(w => w).ToList();
    }

    private void CorrectAnswer() {
        canCheck = false;
        checkButton.interactable = false;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

        if (inputFieldBackground != null) {
            inputFieldBackground.color = correctColor;
        }

        Invoke(nameof(NextQuestion), timeBetweenQuestions);
    }

    private void WrongAnswer() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        
        if (inputFieldBackground != null) {
            inputFieldBackground.DOKill();
            inputFieldBackground.DOColor(incorrectColor, 0.2f).OnComplete(() => {
                inputFieldBackground.DOColor(defaultInputFieldColor, 0.3f);
            });
        }
        
        inputField.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);
    }

    private void NextQuestion() {
        LoadQuestion(currentQuestionIndex + 1);
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
