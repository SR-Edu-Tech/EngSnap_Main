using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U10_SA_UnitChallenge_Masters_Phonics : MonoBehaviour
    {
        [Header("Question Header & Prompt")]
        [SerializeField] private TextMeshProUGUI questionNumberText;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private TextMeshProUGUI targetWordDisplay;
        [SerializeField] private Button replayAudioBtn;

        [Header("Option Buttons (Up to 3)")]
        [SerializeField] private Button option1Btn;
        [SerializeField] private Button option2Btn;
        [SerializeField] private Button option3Btn;
        public Button[] optionButtons;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        private List<U10ChallengeQuestion> questions = new List<U10ChallengeQuestion>();
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

            if (questionNumberText == null)
            {
                Transform qn = FindDeepChild(root, "QuestionNumberText") ?? FindDeepChild(root, "QNumber") ?? root.Find("ProgressHUD/QText");
                if (qn != null) questionNumberText = qn.GetComponent<TextMeshProUGUI>();
            }

            if (promptText == null)
            {
                Transform p = FindDeepChild(root, "PromptText") ?? FindDeepChild(root, "QuestionPrompt") ?? FindDeepChild(root, "Prompt");
                if (p != null) promptText = p.GetComponent<TextMeshProUGUI>();
            }

            if (targetWordDisplay == null)
            {
                Transform tw = FindDeepChild(root, "TargetWordText") ?? FindDeepChild(root, "WordDisplay") ?? FindDeepChild(root, "WordBox");
                if (tw != null) targetWordDisplay = tw.GetComponent<TextMeshProUGUI>();
            }

            if (replayAudioBtn == null)
            {
                replayAudioBtn = FindButton(root, "ReplayAudioBtn", "ReplayBtn", "Speaker_Button", "AudioBtn", "Audio_Button", "SpeakerBtn");
            }

            // Bind Options
            if (option1Btn == null) option1Btn = FindButton(root, "Option_1", "Option1", "Btn_Opt1");
            if (option2Btn == null) option2Btn = FindButton(root, "Option_2", "Option2", "Btn_Opt2");
            if (option3Btn == null) option3Btn = FindButton(root, "Option_3", "Option3", "Btn_Opt3");

            optionButtons = new Button[] { option1Btn, option2Btn, option3Btn };

            U10_UI_Utils.EnsureHUD(root, ref progressText, ref scoreText);

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
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
            questions = U10_DataBank.GetChallengeQuestions();
            currentQuestionIndex = 0;
            correctAnswersCount = 0;
            isProcessing = false;

            if (U10_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U10_VO_challenge_intro", () =>
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

            U10ChallengeQuestion q = questions[currentQuestionIndex];
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
                        txt.color = Color.white;
                    }

                    var img = btn.GetComponent<Image>();
                    if (img != null) U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.15f, 0.22f, 0.35f, 0.95f));

                    int optionIndex = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnOptionSelected(optionIndex, btn));
                }
                else
                {
                    btn.gameObject.SetActive(false);
                }
            }

            PlayQuestionAudio(q);
        }

        public void PlayQuestionAudio(U10ChallengeQuestion q)
        {
            if (q == null || U10_SA_AudioManager_Masters_Phonics.Instance == null) return;

            if (!string.IsNullOrEmpty(q.audioKey))
            {
                if (q.audioKey.StartsWith("U10_VO_") || q.audioKey.StartsWith("VO_"))
                {
                    U10_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(q.audioKey);
                }
                else
                {
                    U10_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(q.audioKey);
                }
            }
            else if (!string.IsNullOrEmpty(q.targetWord))
            {
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayWord(q.targetWord);
            }
        }

        private void ReplayCurrentQuestionAudio()
        {
            if (currentQuestionIndex < questions.Count)
            {
                if (replayAudioBtn != null)
                {
                    StartCoroutine(CoPunchTransform(replayAudioBtn.transform));
                }
                PlayQuestionAudio(questions[currentQuestionIndex]);
            }
        }

        private void OnOptionSelected(int selectedIndex, Button btn)
        {
            if (isProcessing) return;
            isProcessing = true;

            U10ChallengeQuestion q = questions[currentQuestionIndex];
            bool isCorrect = (selectedIndex == q.correctOptionIndex);

            if (isCorrect)
            {
                correctAnswersCount++;
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(150);
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayCorrect();

                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.12f, 0.75f, 0.38f));
                }
            }
            else
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(q.promptText);

                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.85f, 0.25f, 0.25f));
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
            string progressStr = $"<b>Question {currentQuestionIndex + 1} of {questions.Count}</b>";
            if (progressText != null)
            {
                progressText.text = progressStr;
            }

            if (questionNumberText != null)
            {
                questionNumberText.text = progressStr;
            }

            var allTMP = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var pt in allTMP)
            {
                if (pt != null && (pt.name.Equals("ProgressText", StringComparison.OrdinalIgnoreCase) || 
                                   pt.name.Equals("Progress_Text", StringComparison.OrdinalIgnoreCase) ||
                                   pt.name.Equals("QuestionNumberText", StringComparison.OrdinalIgnoreCase)))
                {
                    pt.text = progressStr;
                }
            }

            if (progressBar != null)
            {
                progressBar.value = (float)(currentQuestionIndex + 1) / questions.Count;
            }

            if (scoreText != null)
            {
                int sc = U10_SA_UnitFlowManager_Masters_Phonics.Instance != null 
                    ? U10_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore() 
                    : 0;
                scoreText.text = $"<b>Score: {sc}</b>";
            }
        }

        private void CompleteChallenge()
        {
            if (U10_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                // Advance to Unit 10 Completion Panel
                U10_SA_UnitFlowManager_Masters_Phonics.Instance.OpenCompletionPanel();
            }
        }

        private IEnumerator CoPunchTransform(Transform target)
        {
            if (target == null) yield break;
            Vector3 orig = Vector3.one;
            target.localScale = orig * 1.15f;
            yield return new WaitForSeconds(0.12f);
            if (target != null) target.localScale = orig;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 10 Challenge] Auto-Assigned Questions, Prompts & Options!</b></color>");
        }
#endif
    }
}
