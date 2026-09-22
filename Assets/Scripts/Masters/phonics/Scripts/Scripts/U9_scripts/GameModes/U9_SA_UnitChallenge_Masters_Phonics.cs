using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U9_SA_UnitChallenge_Masters_Phonics : MonoBehaviour
    {
        [Header("Question Header & Prompt")]
        [SerializeField] private TextMeshProUGUI questionNumberText;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI targetWordDisplay;
        [SerializeField] private Button replayAudioBtn;

        [Header("Option Buttons (Up to 4)")]
        [SerializeField] private Button option1Btn;
        [SerializeField] private Button option2Btn;
        [SerializeField] private Button option3Btn;
        [SerializeField] private Button option4Btn;
        public Button[] optionButtons;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        private List<U9ChallengeQuestion> questions = new List<U9ChallengeQuestion>();
        private int currentQuestionIndex = 0;
        private int correctAnswersCount = 0;
        private bool isProcessing = false;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            UpdateHUD();
            StartChallenge();
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;
            U9_UI_Utils.FormatHeaderTypography(root, "UNIT CHALLENGE — CONSONANT + LE", "Mastery Evaluation");

            if (questionNumberText == null)
            {
                Transform qn = root.Find("QuestionNumberText") ?? root.Find("QNumber") ?? root.Find("ProgressHUD/QText");
                if (qn != null) questionNumberText = qn.GetComponent<TextMeshProUGUI>();
            }

            if (promptText == null)
            {
                Transform p = root.Find("PromptText") ?? root.Find("QuestionPrompt") ?? root.Find("Prompt");
                if (p != null) promptText = p.GetComponent<TextMeshProUGUI>();
            }

            if (targetWordDisplay == null)
            {
                Transform tw = root.Find("TargetWordText") ?? root.Find("WordDisplay") ?? root.Find("WordBox");
                if (tw != null) targetWordDisplay = tw.GetComponent<TextMeshProUGUI>();
            }

            if (replayAudioBtn == null)
            {
                replayAudioBtn = FindButton(root, "ReplayBtn", "Speaker_Button", "AudioBtn", "ReplayAudioBtn", "Audio_Button", "SpeakerBtn", "Replay_Button", "ListenBtn", "PlayAudioBtn", "Speaker", "Audio_Btn", "Btn_Speaker");
            }

            // Bind Options
            Transform optRoot = root.Find("OptionsContainer") ?? root.Find("Options") ?? root;
            if (option1Btn == null) option1Btn = FindButton(optRoot, "Option_1", "Option1", "Btn_Opt1");
            if (option2Btn == null) option2Btn = FindButton(optRoot, "Option_2", "Option2", "Btn_Opt2");
            if (option3Btn == null) option3Btn = FindButton(optRoot, "Option_3", "Option3", "Btn_Opt3");
            if (option4Btn == null) option4Btn = FindButton(optRoot, "Option_4", "Option4", "Btn_Opt4");

            optionButtons = new Button[] { option1Btn, option2Btn, option3Btn, option4Btn };

            if (progressText == null)
            {
                Transform pt = root.Find("ProgressHUD/Progress_Text")
                            ?? root.Find("ProgressHUD/ProgressText")
                            ?? root.Find("ProgressText")
                            ?? root.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            if (scoreText == null)
            {
                Transform st = root.Find("ProgressHUD/Score_Text")
                            ?? root.Find("ScoreText")
                            ?? root.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentQuestionAudio);
            }
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"OptionsContainer/{name}") 
                           ?? root.Find($"Options/{name}") 
                           ?? root.Find($"Content/{name}") 
                           ?? root.Find($"HUD/{name}") 
                           ?? FindDeepChild(root, name);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>();
                    if (b != null) return b;
                }
            }
            return null;
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        public void StartChallenge()
        {
            questions = U9_DataBank.GetChallengeQuestions();
            currentQuestionIndex = 0;
            correctAnswersCount = 0;
            isProcessing = false;

            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_challenge_intro", () =>
                {
                    if (this != null && gameObject.activeInHierarchy)
                    {
                        LoadCurrentQuestion();
                    }
                });
            }
            else
            {
                LoadCurrentQuestion();
            }
        }

        private void LoadCurrentQuestion()
        {
            if (!gameObject.activeInHierarchy) return;

            if (currentQuestionIndex >= questions.Count)
            {
                CompleteChallenge();
                return;
            }

            isProcessing = false;
            UpdateHUD();

            U9ChallengeQuestion q = questions[currentQuestionIndex];
            if (questionNumberText != null) questionNumberText.text = $"<b>Question {currentQuestionIndex + 1} of {questions.Count}</b>";
            if (promptText != null) promptText.text = $"<b>{q.promptText}</b>";
            if (targetWordDisplay != null) targetWordDisplay.text = string.IsNullOrEmpty(q.targetWord) ? "" : $"<b>{q.targetWord}</b>";

            // Bind Options
            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button btn = optionButtons[i];
                if (btn == null) continue;

                if (i < q.options.Length)
                {
                    btn.gameObject.SetActive(true);
                    var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null)
                    {
                        txt.text = $"<b>{q.options[i]}</b>";
                        txt.color = Color.white; // Strictly white text only
                    }

                    var img = btn.GetComponent<Image>();
                    if (img != null) U9_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.15f, 0.22f, 0.35f, 0.95f)); // Modern slate navy

                    int optionIndex = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnOptionSelected(optionIndex, btn));
                }
                else
                {
                    btn.gameObject.SetActive(false);
                }
            }

            // Auto-play Question Audio
            PlayQuestionAudio(q);
        }

        public void PlayQuestionAudio(U9ChallengeQuestion q)
        {
            if (q == null || U9_SA_AudioManager_Masters_Phonics.Instance == null) return;

            switch (q.questionId)
            {
                case 1:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_card1");
                    break;
                case 2:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord("sprinkle");
                    break;
                case 3:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_card2");
                    break;
                case 4:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord("bugle");
                    break;
                case 5:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord("bottle");
                    break;
                case 6:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord("wiggle");
                    break;
                case 7:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlaySentence("The gentle eagle landed.");
                    break;
                case 8:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_card1");
                    break;
                case 9:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord("candle");
                    break;
                case 10:
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord("ogle");
                    break;
                default:
                    if (!string.IsNullOrEmpty(q.targetWord))
                        U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord(q.targetWord);
                    break;
            }
        }

        private void ReplayCurrentQuestionAudio()
        {
            if (currentQuestionIndex < questions.Count)
            {
                if (replayAudioBtn != null)
                {
                    StartCoroutine(CoPunchButton(replayAudioBtn.transform));
                }
                PlayQuestionAudio(questions[currentQuestionIndex]);
            }
        }

        private IEnumerator CoPunchButton(Transform target)
        {
            if (target == null) yield break;
            Vector3 orig = Vector3.one;
            target.localScale = orig * 1.15f;
            yield return new WaitForSeconds(0.12f);
            if (target != null) target.localScale = orig;
        }

        private void OnOptionSelected(int selectedIndex, Button btn)
        {
            if (isProcessing) return;
            isProcessing = true;

            U9ChallengeQuestion q = questions[currentQuestionIndex];
            bool isCorrect = (selectedIndex == q.correctOptionIndex);

            if (isCorrect)
            {
                correctAnswersCount++;
                U9_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(150);
                U9_SA_AudioManager_Masters_Phonics.Instance?.PlayCorrect();

                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) U9_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.12f, 0.75f, 0.38f));
                    var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.color = Color.white;
                }
            }
            else
            {
                U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
                U9_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(q.promptText);

                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) U9_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.85f, 0.25f, 0.25f));
                    var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.color = Color.white;
                }
            }

            UpdateHUD();

            StartCoroutine(CoAdvanceQuestion());
        }

        private IEnumerator CoAdvanceQuestion()
        {
            yield return new WaitForSeconds(1.2f);
            currentQuestionIndex++;
            LoadCurrentQuestion();
        }


        private void UpdateHUD()
        {
            if (progressText != null)
            {
                progressText.text = $"<b>Question {currentQuestionIndex + 1} of {questions.Count}</b>";
            }

            if (progressBar != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
                progressBar.value = (float)(currentQuestionIndex + 1) / questions.Count;
            }

            if (scoreText != null && U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                scoreText.text = $"<b>Score: {U9_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore()}</b>";
            }
        }

        private void CompleteChallenge()
        {
            bool passed = correctAnswersCount >= 7;

            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                // Advance directly to the Grand Seven Types Map Finale (7/7)
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.OpenSevenTypesMap();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 9 Challenge] Auto-Assigned Questions, Prompts & Options!</b></color>");
        }
#endif
    }
}
