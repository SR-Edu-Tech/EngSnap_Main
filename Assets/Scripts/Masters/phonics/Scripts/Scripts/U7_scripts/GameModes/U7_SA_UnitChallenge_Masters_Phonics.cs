using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U7_SA_UnitChallenge_Masters_Phonics : MonoBehaviour
    {
        [Header("Question UI")]
        [SerializeField] private TextMeshProUGUI questionNumberText;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private Button replayAudioBtn;

        [Header("Options UI")]
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TextMeshProUGUI[] optionTexts;

        [Header("HUD & Progress")]
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("Badge & Completion Modal")]
        [SerializeField] private GameObject completionModal;
        [SerializeField] private TextMeshProUGUI badgeTitleText;
        [SerializeField] private TextMeshProUGUI badgeDescriptionText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private Image badgeIconImage;
        [SerializeField] private Button finishUnitButton;

        [Header("Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Played when an option is correct")]
        public AudioClip correctSFX;
        [Tooltip("Played when an option is incorrect")]
        public AudioClip wrongSFX;
        [Tooltip("Celebration fanfare")]
        public AudioClip unitCompletedFanfare;

        [Header("Option Colors & Feedback")]
        [Tooltip("Default background color of answer options")]
        public Color normalOptionColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        [Tooltip("Color when correct answer option is chosen")]
        public Color correctOptionColor = new Color(0.18f, 0.8f, 0.44f, 1f);
        [Tooltip("Color when wrong answer option is chosen")]
        public Color wrongOptionColor = new Color(0.92f, 0.3f, 0.3f, 1f);

        [Header("Questions Pool")]
        public List<Unit7ChallengeQuestion> questions = new List<Unit7ChallengeQuestion>();

        // State
        private int currentQuestionIndex = 0;
        private int totalScore = 0;
        private int currentStreak = 0;
        private int correctCount = 0;
        private bool isProcessing = false;

        private static Sprite proceduralRoundedSprite = null;

        public static Sprite GetOrCreateRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 28;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius - 1.5f)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            Vector4 border = new Vector4(radius, radius, radius, radius);
            proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return proceduralRoundedSprite;
        }

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
            {
                questions = U7_SA_DataTypes_Masters_Phonics.GetDefaultChallengeQuestions();
            }

            if (questionNumberText == null)
            {
                Transform qn = transform.Find("ProgressHUD/ProgressText")
                            ?? transform.Find("ProgressHUD/Progress_Text")
                            ?? transform.Find("HUD/QuestionNumberText") 
                            ?? transform.Find("QuestionNumberText")
                            ?? transform.Find("ProgressText")
                            ?? transform.Find("Progress_Text")
                            ?? transform.Find("HUD/ProgressText");
                if (qn != null) questionNumberText = qn.GetComponent<TextMeshProUGUI>();
            }
            if (promptText == null)
            {
                Transform pt = transform.Find("QuestionBox/PromptText") ?? transform.Find("PromptText");
                if (pt != null) promptText = pt.GetComponent<TextMeshProUGUI>();
            }
            if (replayAudioBtn == null)
            {
                Transform rb = transform.Find("QuestionBox/ReplayAudioBtn") 
                            ?? transform.Find("ReplayAudioBtn")
                            ?? transform.Find("ReplayButton")
                            ?? transform.Find("ReplayAudioButton")
                            ?? transform.Find("Header_Container/ReplayBtn")
                            ?? transform.Find("HeaderRibbon/ReplayAudioBtn")
                            ?? transform.Find("QuestionBox/Speaker_Button")
                            ?? transform.Find("Speaker_Button") 
                            ?? transform.Find("SpeakerButton");
                if (rb != null) replayAudioBtn = rb.GetComponent<Button>();
                if (replayAudioBtn == null)
                {
                    var allBtns = GetComponentsInChildren<Button>(true);
                    foreach (var b in allBtns)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if (bName.Contains("speaker") || bName.Contains("replay") || bName.Contains("audio"))
                        {
                            replayAudioBtn = b;
                            break;
                        }
                    }
                }
            }
            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentQuestionAudio);
            }

            if (optionsContainer == null)
            {
                Transform oc = transform.Find("OptionsContainer") ?? transform.Find("Options");
                if (oc != null) optionsContainer = oc;
            }

            if (optionsContainer != null)
            {
                var btns = optionsContainer.GetComponentsInChildren<Button>(true);
                if (btns != null && btns.Length > 0)
                {
                    optionButtons = btns;
                    optionTexts = new TextMeshProUGUI[btns.Length];
                    for (int i = 0; i < btns.Length; i++)
                    {
                        optionTexts[i] = btns[i].GetComponentInChildren<TextMeshProUGUI>(true);
                        int capturedIndex = i;
                        btns[i].onClick.RemoveAllListeners();
                        btns[i].onClick.AddListener(() => OnOptionClicked(capturedIndex));
                    }
                }
            }

            if (progressBar == null)
            {
                Transform pb = transform.Find("ProgressHUD/ProgressBar") 
                            ?? transform.Find("ProgressHUD/Progress_Bar")
                            ?? transform.Find("HUD/ProgressBar") 
                            ?? transform.Find("ProgressBar")
                            ?? transform.Find("Progress_Bar");
                if (pb != null) progressBar = pb.GetComponent<Slider>();
                if (progressBar == null) progressBar = GetComponentInChildren<Slider>(true);
            }
            if (scoreText == null)
            {
                Transform st = transform.Find("ScoreHUD/ScoreText")
                            ?? transform.Find("ScoreHUD/Score_Text")
                            ?? transform.Find("Score_HUD/Score_Text") 
                            ?? transform.Find("Score_HUD/ScoreText")
                            ?? transform.Find("HUD_Container/Score_Text")
                            ?? transform.Find("HUD_Container/ScoreText")
                            ?? transform.Find("ProgressHUD/Score_Text")
                            ?? transform.Find("HUD/ScoreText") 
                            ?? transform.Find("ScoreText")
                            ?? transform.Find("Score_Text")
                            ?? transform.Find("HUD/Score_Text");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }
            if (streakText == null)
            {
                Transform stt = transform.Find("ScoreHUD/StreakText")
                            ?? transform.Find("ScoreHUD/Streak_Text")
                            ?? transform.Find("Score_HUD/Streak_Text") 
                            ?? transform.Find("Score_HUD/StreakText")
                            ?? transform.Find("HUD_Container/Streak_Text")
                            ?? transform.Find("HUD/StreakText") 
                            ?? transform.Find("StreakText")
                            ?? transform.Find("Streak_Text")
                            ?? transform.Find("HUD/Streak_Text");
                if (stt != null) streakText = stt.GetComponent<TextMeshProUGUI>();
            }

            if (feedbackPanel == null)
            {
                Transform fp = transform.Find("FeedbackPanel");
                if (fp != null) feedbackPanel = fp.gameObject;
            }
            if (feedbackPanel != null)
            {
                feedbackText = feedbackPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (completionModal == null)
            {
                Transform cm = transform.Find("CompletionModal") ?? transform.Find("BadgeModal");
                if (cm != null) completionModal = cm.gameObject;
            }
            if (completionModal != null)
            {
                badgeTitleText = completionModal.transform.Find("BadgeTitle")?.GetComponent<TextMeshProUGUI>();
                badgeDescriptionText = completionModal.transform.Find("BadgeDesc")?.GetComponent<TextMeshProUGUI>();
                finalScoreText = completionModal.transform.Find("FinalScore")?.GetComponent<TextMeshProUGUI>();
                badgeIconImage = completionModal.transform.Find("BadgeIcon")?.GetComponent<Image>();
                finishUnitButton = completionModal.GetComponentInChildren<Button>(true);

                if (finishUnitButton != null)
                {
                    finishUnitButton.onClick.RemoveAllListeners();
                    finishUnitButton.onClick.AddListener(OnFinishUnitTapped);
                }
            }
        }

        public void StartChallenge()
        {
            currentQuestionIndex = 0;
            totalScore = 0;
            currentStreak = 0;
            correctCount = 0;
            isProcessing = false;

            if (feedbackPanel != null) feedbackPanel.SetActive(false);
            if (completionModal != null) completionModal.SetActive(false);

            UpdateHUD();
            LoadQuestion();

            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip intro = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_VO_challenge_intro") ??
                                  U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Unit 7 Challenge. Show what you know about vowel teams and diphthongs!");
                if (intro != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(intro);
            }
        }

        private void LoadQuestion()
        {
            if (currentQuestionIndex >= questions.Count)
            {
                ShowCompletionBadge();
                return;
            }

            var q = questions[currentQuestionIndex];

            if (questionNumberText != null)
                questionNumberText.text = $"<b>Question {currentQuestionIndex + 1} of {questions.Count}</b>";

            if (promptText != null)
            {
                promptText.text = $"<b>{q.prompt}</b>";
                promptText.color = new Color(0.1f, 0.15f, 0.22f, 1f);
            }

            if (feedbackPanel != null) feedbackPanel.SetActive(false);

            // Populate Options
            Sprite rounded = GetOrCreateRoundedSprite();
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < q.options.Length)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].interactable = true;

                    Image btnImg = optionButtons[i].GetComponent<Image>();
                    if (btnImg != null)
                    {
                        btnImg.sprite = rounded;
                        btnImg.type = Image.Type.Sliced;
                        btnImg.color = normalOptionColor;
                    }

                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null)
                    {
                        optionTexts[i].text = $"<b>{q.options[i]}</b>";
                        optionTexts[i].color = new Color(0.1f, 0.15f, 0.22f, 1f);
                    }
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            UpdateHUD();
            ReplayCurrentQuestionAudio();
            isProcessing = false;
        }

        public void ReplayCurrentQuestionAudio()
        {
            if (currentQuestionIndex >= questions.Count) return;
            var q = questions[currentQuestionIndex];

            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip clip = q.customAudio ??
                                 U7_SA_AudioManager_Masters_Phonics.ResolveAudio(q.audioClipName) ??
                                 U7_SA_AudioManager_Masters_Phonics.ResolveAudio(q.prompt);
                if (clip != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
            }
        }

        private void OnOptionClicked(int selectedIndex)
        {
            if (isProcessing || currentQuestionIndex >= questions.Count) return;

            isProcessing = true;
            var q = questions[currentQuestionIndex];
            bool isCorrect = (selectedIndex == q.correctIndex);

            // Highlight buttons
            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].interactable = false;
                Image img = optionButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    if (i == q.correctIndex)
                        img.color = correctOptionColor;
                    else if (i == selectedIndex && !isCorrect)
                        img.color = wrongOptionColor;
                }
            }

            if (isCorrect)
            {
                correctCount++;
                currentStreak++;
                int points = 20 + (currentStreak > 2 ? 10 : 0);
                totalScore += points;

                PlaySFX(correctSFX, true);

                if (feedbackPanel != null)
                {
                    feedbackPanel.SetActive(true);
                    if (feedbackText != null)
                        feedbackText.text = $"<b><color=#10B981>Correct!</color></b> {q.explanation}";
                }
            }
            else
            {
                currentStreak = 0;
                PlaySFX(wrongSFX, false);

                if (feedbackPanel != null)
                {
                    feedbackPanel.SetActive(true);
                    if (feedbackText != null)
                        feedbackText.text = $"<b><color=#EF4444>Not quite.</color></b> {q.explanation}";
                }
            }

            UpdateHUD();
            StartCoroutine(AdvanceToNextQuestionDelayed(isCorrect ? 2.0f : 3.0f));
        }

        private IEnumerator AdvanceToNextQuestionDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            currentQuestionIndex++;
            LoadQuestion();
        }

        private void ShowCompletionBadge()
        {
            int earnedStars = (correctCount >= 10) ? 3 : (correctCount >= 7) ? 2 : 1;

            if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(8, earnedStars);
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(totalScore);
            }

            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                if (unitCompletedFanfare != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(unitCompletedFanfare);

                AudioClip victoryClip = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_VO_unit_complete") ??
                                        U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Congratulations! You earned the Sound Glider badge and completed Unit 7!");
                if (victoryClip != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(victoryClip);
            }

            // Show activity completion modal with "Next" button directing directly to Unit 7 Completion Panel
            U7_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform, 
                "Unit 7 Challenge", 
                earnedStars, 
                totalScore, 
                () => {
                    if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    {
                        U7_SA_UnitFlowManager_Masters_Phonics.Instance.OpenCompletion();
                    }
                }
            );
        }

        private void OnFinishUnitTapped()
        {
            if (completionModal != null) completionModal.SetActive(false);

            if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(totalScore);
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.OpenCompletion();
            }
        }

        private void PlaySFX(AudioClip clip, bool isCorrect)
        {
            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                if (clip != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(clip);
                else
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayAnswerFeedbackSFX(isCorrect);
            }
        }

        private void UpdateHUD()
        {
            if (questionNumberText != null && questions.Count > 0)
            {
                questionNumberText.text = $"Item <b>{Mathf.Min(currentQuestionIndex + 1, questions.Count)}</b> of <b>{questions.Count}</b>";
            }

            if (progressBar != null)
            {
                progressBar.maxValue = questions.Count;
                progressBar.value = currentQuestionIndex;
                U7_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
            }

            if (scoreText != null)
                scoreText.text = $"Score: <b>{totalScore}</b>";

            if (streakText != null)
                streakText.text = currentStreak > 1 ? $"<b>{currentStreak}x STREAK!</b>" : "";
        }
    }
}
