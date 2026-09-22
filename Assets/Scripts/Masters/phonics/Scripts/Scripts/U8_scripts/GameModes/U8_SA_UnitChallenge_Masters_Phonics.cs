using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U8_SA_UnitChallenge_Masters_Phonics : MonoBehaviour
    {
        [Header("Challenge Questions (10 items)")]
        public List<U8ChallengeQuestion> questions;
        private int currentQuestionIndex = 0;
        private int totalScore = 0;
        private int correctCount = 0;
        private int currentStreak = 0;
        private bool isProcessing = false;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI questionNumberText;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI ruleTipText;
        [SerializeField] private Button replayAudioBtn;

        [Header("Option Buttons (Up to 3 options)")]
        [SerializeField] private List<Button> optionButtons;
        [SerializeField] private List<TextMeshProUGUI> optionTexts;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private Slider progressBar;

        [Header("Visual Feedback Colors")]
        public Color defaultBtnColor = new Color(0.12f, 0.16f, 0.24f, 1f);
        public Color correctBtnColor = new Color(0.12f, 0.72f, 0.35f, 1f);
        public Color wrongBtnColor = new Color(0.92f, 0.25f, 0.25f, 1f);

        [Header("SFX")]
        public AudioClip correctSFX;
        public AudioClip wrongSFX;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartChallenge();
        }

        public void AutoBindHierarchyElements()
        {
            if (questions == null || questions.Count == 0)
                questions = U8_SA_DataTypes_Masters_Phonics.GetDefaultChallengeQuestions();

            Transform root = transform;

            if (questionNumberText == null)
            {
                Transform qn = root.Find("QuestionBox/QuestionNumberText") 
                            ?? root.Find("QuestionNumberText") 
                            ?? root.Find("ProgressHUD/ProgressText");
                if (qn != null) questionNumberText = qn.GetComponent<TextMeshProUGUI>();
            }

            if (promptText == null)
            {
                Transform pt = root.Find("QuestionBox/PromptText") ?? root.Find("PromptText");
                if (pt != null) promptText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (ruleTipText == null)
            {
                Transform rt = root.Find("QuestionBox/RuleTipText") ?? root.Find("TipText");
                if (rt != null) ruleTipText = rt.GetComponent<TextMeshProUGUI>();
            }

            if (replayAudioBtn == null)
            {
                Transform rb = root.Find("QuestionBox/ReplayAudioBtn") 
                            ?? root.Find("ReplayAudioBtn") 
                            ?? root.Find("ReplayButton") 
                            ?? root.Find("HeaderRibbon/ReplayAudioBtn")
                            ?? root.Find("Speaker_Button");
                if (rb != null) replayAudioBtn = rb.GetComponent<Button>();
            }

            if (optionButtons == null || optionButtons.Count == 0)
            {
                optionButtons = new List<Button>();
                optionTexts = new List<TextMeshProUGUI>();

                Transform optContainer = root.Find("OptionsContainer") ?? root.Find("Options");
                if (optContainer != null)
                {
                    var btns = optContainer.GetComponentsInChildren<Button>(true);
                    foreach (var b in btns)
                    {
                        if (b != replayAudioBtn)
                        {
                            optionButtons.Add(b);
                            optionTexts.Add(b.GetComponentInChildren<TextMeshProUGUI>(true));
                        }
                    }
                }
            }

            if (progressText == null)
            {
                Transform pt = root.Find("ProgressHUD/ProgressText") 
                            ?? root.Find("ProgressHUD/Progress_Text") 
                            ?? root.Find("ProgressText")
                            ?? root.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }
            if (scoreText == null)
            {
                Transform st = root.Find("ScoreHUD/ScoreText") ?? root.Find("ScoreHUD/Score_Text") ?? root.Find("HUD/ScoreText") ?? root.Find("ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }
            if (streakText == null)
            {
                Transform stt = root.Find("ScoreHUD/StreakText") ?? root.Find("ScoreHUD/Streak_Text") ?? root.Find("HUD/StreakText") ?? root.Find("StreakText");
                if (stt != null) streakText = stt.GetComponent<TextMeshProUGUI>();
            }
            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            AttachListeners();
        }

        private void AttachListeners()
        {
            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentQuestionAudio);
            }

            for (int i = 0; i < optionButtons.Count; i++)
            {
                int capturedIdx = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionTapped(capturedIdx));
            }
        }

        public void StartChallenge()
        {
            currentQuestionIndex = 0;
            totalScore = 0;
            correctCount = 0;
            currentStreak = 0;
            isProcessing = false;

            AudioClip introClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_challenge_intro");
            if (introClip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(introClip);

            LoadCurrentQuestion();
        }

        private void LoadCurrentQuestion()
        {
            if (currentQuestionIndex >= questions.Count)
            {
                StartCoroutine(HandleChallengeCompleted());
                return;
            }

            var q = questions[currentQuestionIndex];
            isProcessing = false;

            ResetButtonVisuals();

            if (questionNumberText != null)
                questionNumberText.text = $"<b>Question {currentQuestionIndex + 1} of {questions.Count}</b>";

            if (promptText != null)
                promptText.text = $"<b>{q.promptText}</b>";

            if (ruleTipText != null)
                ruleTipText.text = "";

            // Populate options
            for (int i = 0; i < optionButtons.Count; i++)
            {
                if (i < q.options.Count)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    if (i < optionTexts.Count && optionTexts[i] != null)
                    {
                        optionTexts[i].text = $"<b>{q.options[i]}</b>";
                    }
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            UpdateHUD();
            ReplayCurrentQuestionAudio();
        }

        public void ReplayCurrentQuestionAudio()
        {
            if (currentQuestionIndex < questions.Count)
            {
                var q = questions[currentQuestionIndex];
                if (q.promptAudio != null)
                {
                    U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(q.promptAudio);
                }
                else if (!string.IsNullOrEmpty(q.targetWord))
                {
                    AudioClip clip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio($"U08_WRD_{q.targetWord}") ??
                                     U8_SA_AudioManager_Masters_Phonics.ResolveAudio(q.targetWord);
                    if (clip != null)
                        U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(clip);
                }
            }
        }

        private void OnOptionTapped(int index)
        {
            if (isProcessing || currentQuestionIndex >= questions.Count) return;
            isProcessing = true;

            var q = questions[currentQuestionIndex];
            bool isCorrect = (index == q.correctIndex);

            Button selectedBtn = (index < optionButtons.Count) ? optionButtons[index] : null;

            if (isCorrect)
            {
                correctCount++;
                currentStreak++;
                int streakMultiplier = Mathf.Min(currentStreak, 4);
                totalScore += (25 * streakMultiplier);
                UpdateHUD();

                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordsRead(1);

                StartCoroutine(HandleCorrectAnswer(selectedBtn));
            }
            else
            {
                currentStreak = 0;
                UpdateHUD();
                if (!string.IsNullOrEmpty(q.targetWord) && U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordMistake(q.targetWord);
                }

                StartCoroutine(HandleWrongAnswer(selectedBtn, q.correctIndex, q.ruleTip));
            }
        }

        private IEnumerator HandleCorrectAnswer(Button btn)
        {
            if (btn != null)
            {
                Image img = btn.GetComponent<Image>();
                if (img != null) img.color = correctBtnColor;
            }

            PlaySFX(correctSFX, true);
            yield return new WaitForSeconds(0.7f);

            currentQuestionIndex++;
            LoadCurrentQuestion();
        }

        private IEnumerator HandleWrongAnswer(Button wrongBtn, int correctIdx, string ruleTip)
        {
            if (wrongBtn != null)
            {
                Image img = wrongBtn.GetComponent<Image>();
                if (img != null) img.color = wrongBtnColor;
            }

            if (correctIdx >= 0 && correctIdx < optionButtons.Count && optionButtons[correctIdx] != null)
            {
                Image cImg = optionButtons[correctIdx].GetComponent<Image>();
                if (cImg != null) cImg.color = correctBtnColor;
            }

            if (ruleTipText != null && !string.IsNullOrEmpty(ruleTip))
            {
                ruleTipText.text = $"<color=#FFD700><b>Rule:</b> {ruleTip}</color>";
            }

            PlaySFX(wrongSFX, false);
            yield return new WaitForSeconds(1.5f);

            currentQuestionIndex++;
            LoadCurrentQuestion();
        }

        private void ResetButtonVisuals()
        {
            foreach (var b in optionButtons)
            {
                if (b != null)
                {
                    Image img = b.GetComponent<Image>();
                    if (img != null) img.color = defaultBtnColor;
                }
            }
        }

        private void PlaySFX(AudioClip clip, bool isCorrect)
        {
            if (clip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX(clip);
            else
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayAnswerFeedbackSFX(isCorrect);
        }

        private void UpdateHUD()
        {
            if (progressBar != null)
            {
                progressBar.maxValue = questions.Count;
                progressBar.value = currentQuestionIndex + 1;
                U8_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
            }

            if (progressText != null)
                progressText.text = $"<b>Question {currentQuestionIndex + 1} of {questions.Count}</b>";

            if (scoreText != null)
                scoreText.text = $"Score: <b>{totalScore}</b>";

            if (streakText != null)
                streakText.text = currentStreak > 1 ? $"<b>Streak: {currentStreak}x</b>" : "";
        }

        private IEnumerator HandleChallengeCompleted()
        {
            isProcessing = true;
            if (progressBar != null)
            {
                progressBar.value = questions.Count;
            }

            int earnedStars = 1;
            if (correctCount >= 9) earnedStars = 3;
            else if (correctCount >= 7) earnedStars = 2;

            if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(6, earnedStars);
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(totalScore);
            }

            yield return new WaitForSeconds(0.8f);

            U8_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform,
                "Unit 8 Challenge",
                earnedStars,
                totalScore,
                () => {
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenMapUpdate();
                }
            );
        }
    }
}
