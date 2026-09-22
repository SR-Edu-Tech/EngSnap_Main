using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U6_SA_UnitChallenge_Masters_Phonics : MonoBehaviour
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

        [Header("Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Played when an option is correct")]
        public AudioClip correctSFX;
        [Tooltip("Played when an option is incorrect")]
        public AudioClip wrongSFX;
        [Tooltip("Played when an option is incorrect (alias for wrongSFX)")]
        public AudioClip incorrectSFX;

        [Header("Colors")]
        [SerializeField] private Color normalOptionColor = new Color(0.92f, 0.94f, 0.98f, 1f);
        [SerializeField] private Color correctOptionColor = new Color(0.18f, 0.8f, 0.44f, 1f);
        [SerializeField] private Color wrongOptionColor = new Color(0.92f, 0.3f, 0.3f, 1f);

        [Header("Questions Pool")]
        [SerializeField] private List<Unit6ChallengeQuestion> questions = new List<Unit6ChallengeQuestion>();

        // State
        private int currentQuestionIndex = 0;
        private int totalScore = 0;
        private int currentStreak = 0;
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
                BuildDefaultQuestions();
            }

            if (questionNumberText == null)
            {
                Transform qn = transform.Find("QuestionNumberText") 
                            ?? transform.Find("QuestionNumber")
                            ?? transform.Find("ProgressHUD/Progress_Text") 
                            ?? transform.Find("ProgressHUD/ProgressText") 
                            ?? transform.Find("Progress_Text") 
                            ?? transform.Find("ProgressText")
                            ?? transform.Find("ItemCounterText")
                            ?? transform.Find("Counter_Text");
                if (qn != null) questionNumberText = qn.GetComponent<TextMeshProUGUI>();

                if (questionNumberText == null)
                {
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var tmp in tmps)
                    {
                        string n = tmp.gameObject.name.ToLower();
                        if ((n.Contains("question") || n.Contains("progress") || n.Contains("counter") || n.Contains("count") || n.Contains("item")) &&
                            !n.Contains("score") && !n.Contains("streak") && !n.Contains("title") && !n.Contains("prompt") && !n.Contains("btn") && !n.Contains("button") && !n.Contains("bubble") && !n.Contains("feedback") && !n.Contains("option"))
                        {
                            questionNumberText = tmp;
                            break;
                        }
                    }
                }
            }

            if (promptText == null)
            {
                Transform pt = transform.Find("PromptText") ?? transform.Find("QuestionPrompt");
                if (pt != null) promptText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (replayAudioBtn == null)
            {
                Transform btn = transform.Find("ReplayAudioButton") ?? transform.Find("AudioButton") ?? transform.Find("ReplayBtn");
                if (btn != null) replayAudioBtn = btn.GetComponent<Button>();
            }

            if (optionsContainer == null)
            {
                optionsContainer = transform.Find("OptionsContainer") ?? transform.Find("Options");
            }

            if ((optionButtons == null || optionButtons.Length == 0 || optionButtons[0] == null) && optionsContainer != null)
            {
                List<Button> bList = new List<Button>();
                List<TextMeshProUGUI> tList = new List<TextMeshProUGUI>();
                for (int i = 0; i < 3; i++)
                {
                    Transform opt = optionsContainer.Find($"Option_{i}") ?? (i < optionsContainer.childCount ? optionsContainer.GetChild(i) : null);
                    if (opt != null)
                    {
                        var b = opt.GetComponent<Button>();
                        if (b != null)
                        {
                            bList.Add(b);
                            var t = opt.GetComponentInChildren<TextMeshProUGUI>();
                            if (t != null) tList.Add(t);
                        }
                    }
                }
                if (bList.Count > 0)
                {
                    optionButtons = bList.ToArray();
                    optionTexts = tList.ToArray();
                }
            }

            if (progressBar == null)
            {
                Transform pb = transform.Find("ProgressHUD/ProgressBar") ?? transform.Find("ProgressBar");
                if (pb != null) progressBar = pb.GetComponent<Slider>();
            }
            if (progressBar != null) U6_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);

            if (scoreText == null)
            {
                Transform sTxt = transform.Find("ProgressHUD/Score_Text") ?? transform.Find("Score_Text");
                if (sTxt != null) scoreText = sTxt.GetComponent<TextMeshProUGUI>();
            }

            if (streakText == null)
            {
                Transform stTxt = transform.Find("ProgressHUD/Streak_Text") ?? transform.Find("Streak_Text");
                if (stTxt != null) streakText = stTxt.GetComponent<TextMeshProUGUI>();
            }

            if (feedbackPanel == null)
            {
                Transform fp = transform.Find("FeedbackPanel");
                if (fp != null)
                {
                    feedbackPanel = fp.gameObject;
                    feedbackText = fp.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayQuestionAudio);
            }
        }

        public void StartChallenge()
        {
            StopAllCoroutines();
            currentQuestionIndex = 0;
            totalScore = 0;
            currentStreak = 0;
            isProcessing = false;

            UpdateHUD();
            LoadCurrentQuestion();
        }

        private void LoadCurrentQuestion()
        {
            if (currentQuestionIndex >= questions.Count)
            {
                FinishChallenge();
                return;
            }

            isProcessing = false;
            Unit6ChallengeQuestion q = questions[currentQuestionIndex];

            if (questionNumberText != null)
            {
                questionNumberText.text = $"<b>Question {currentQuestionIndex + 1} of {questions.Count}</b>";
                questionNumberText.fontSize = 24;
                questionNumberText.fontStyle = FontStyles.Bold;
                questionNumberText.color = Color.white;
            }

            if (promptText != null)
            {
                promptText.text = $"<b>{q.prompt}</b>";
                promptText.fontSize = 36;
                promptText.fontStyle = FontStyles.Bold;
                promptText.color = Color.white;
            }

            // Setup Options
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < q.options.Length)
                {
                    optionButtons[i].gameObject.SetActive(true);

                    Image btnImg = optionButtons[i].GetComponent<Image>();
                    if (btnImg != null)
                    {
                        if (btnImg.sprite == null)
                        {
                            btnImg.sprite = GetOrCreateRoundedSprite();
                            btnImg.type = Image.Type.Sliced;
                        }
                        btnImg.color = normalOptionColor;
                    }

                    if (optionTexts[i] != null)
                    {
                        optionTexts[i].text = q.options[i];
                        optionTexts[i].fontSize = 32;
                        optionTexts[i].fontStyle = FontStyles.Bold;
                        optionTexts[i].color = new Color(0.12f, 0.18f, 0.3f, 1f);
                    }

                    int choiceIdx = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(choiceIdx));
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }

            UpdateHUD();
            PlayQuestionAudio(q);
        }

        private void PlayQuestionAudio(Unit6ChallengeQuestion q)
        {
            if (q == null) return;
            if (q.customAudio != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(q.customAudio);
            }
            else if (!string.IsNullOrEmpty(q.audioClipName))
            {
                AudioClip clip = U6_SA_AudioManager_Masters_Phonics.ResolveAudio(q.audioClipName);
                if (clip != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlayVoicePrompt(q.audioClipName, q.prompt);
            }
        }

        public void ReplayQuestionAudio()
        {
            if (currentQuestionIndex < questions.Count)
            {
                PlayQuestionAudio(questions[currentQuestionIndex]);
                if (replayAudioBtn != null)
                    StartCoroutine(PunchScale(replayAudioBtn.transform, 1.15f, 0.15f));
            }
        }

        private void OnOptionSelected(int selectedIndex)
        {
            if (isProcessing) return;
            isProcessing = true;

            StartCoroutine(HandleOptionSelection(selectedIndex));
        }

        private IEnumerator HandleOptionSelection(int selectedIndex)
        {
            Unit6ChallengeQuestion q = questions[currentQuestionIndex];
            int correctIndex = q.correctIndex;
            string explanation = q.explanation;

            bool isCorrect = (selectedIndex == correctIndex);

            Image selImg = (selectedIndex < optionButtons.Length && optionButtons[selectedIndex] != null) ? optionButtons[selectedIndex].GetComponent<Image>() : null;
            Image corImg = (correctIndex < optionButtons.Length && optionButtons[correctIndex] != null) ? optionButtons[correctIndex].GetComponent<Image>() : null;

            if (isCorrect)
            {
                currentStreak++;
                int pts = 100 + (currentStreak > 1 ? 25 : 0);
                totalScore += pts;

                if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U6_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(pts);

                if (selImg != null) selImg.color = correctOptionColor;
                if (optionTexts[selectedIndex] != null) optionTexts[selectedIndex].color = Color.white;

                if (correctSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(correctSFX);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                StartCoroutine(PunchScale(optionButtons[selectedIndex].transform, 1.1f, 0.18f));
            }
            else
            {
                currentStreak = 0;
                if (selImg != null) selImg.color = wrongOptionColor;
                if (corImg != null) corImg.color = correctOptionColor;

                if (optionTexts[selectedIndex] != null) optionTexts[selectedIndex].color = Color.white;
                if (optionTexts[correctIndex] != null) optionTexts[correctIndex].color = Color.white;

                AudioClip wrongClip = wrongSFX != null ? wrongSFX : incorrectSFX;
                if (wrongClip != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(wrongClip);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");
            }

            UpdateHUD();

            if (promptText != null && !string.IsNullOrEmpty(explanation))
            {
                promptText.text = $"{(isCorrect ? "<color=#2E7D32><b>Correct!</b></color>" : "<color=#D32F2F><b>Not quite!</b></color>")} {explanation}";
            }

            yield return new WaitForSeconds(2.0f);

            currentQuestionIndex++;
            LoadCurrentQuestion();
        }

        private void UpdateHUD()
        {
            if (questionNumberText != null && questions.Count > 0)
            {
                questionNumberText.text = $"<b>Question {Mathf.Min(currentQuestionIndex + 1, questions.Count)} of {questions.Count}</b>";
                questionNumberText.color = Color.white;
            }

            if (progressBar != null && questions.Count > 0)
                progressBar.value = (float)currentQuestionIndex / questions.Count;

            if (scoreText != null)
                scoreText.text = $"Score: {totalScore}";

            if (streakText != null)
                streakText.text = currentStreak > 1 ? $"Streak: {currentStreak}x!" : "";
        }

        private void FinishChallenge()
        {
            if (progressBar != null) progressBar.value = 1f;
            U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("unit_complete");

            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(true);
                if (feedbackText != null)
                    feedbackText.text = $"Unit Challenge Complete!\nWand Bearer Badge Unlocked!\nTotal Unit Score: {totalScore}";
            }

            StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            yield return new WaitForSeconds(3.0f);
            if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U6_SA_UnitFlowManager_Masters_Phonics.Instance.CompleteCurrentActivity();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign All Items, Sprites & Audio")]
        public void EditorAutoAssignEverything()
        {
            AutoBindHierarchyElements();

            questions = U6_SA_DataTypes_Masters_Phonics.GetDefaultChallengeQuestions();

            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                if (q == null) continue;
                if (!string.IsNullOrEmpty(q.audioClipName))
                {
                    q.customAudio = FindAudioInEditor(q.audioClipName);
                }
            }

            if (correctSFX == null) correctSFX = FindAudioInEditor("correct");
            if (wrongSFX == null) wrongSFX = FindAudioInEditor("wrong");

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log($"<color=#10B981><b>[Unit Challenge] Auto-assigned {questions.Count} questions & Audio Clips!</b></color>");
        }

        private AudioClip FindAudioInEditor(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            string clean = word.ToLower().Trim();

            if (clean == "correct")
            {
                AudioClip c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3");
                if (c != null) return c;
            }
            else if (clean == "wrong" || clean == "incorrect")
            {
                AudioClip c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3");
                if (c != null) return c;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new string[] { "Assets/Audio/U6_audio", "Assets/Audio", "Assets/SFX", "Assets/Resources" });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename == clean || filename == $"u06_vo_{clean}" || filename == $"u06_wrd_{clean}" || filename == $"u06_sfx_{clean}")
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }

            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename.Contains(clean))
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            return null;
        }
#endif

        private IEnumerator PunchScale(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            target.localScale = original * scale;
            yield return new WaitForSeconds(duration);
            if (target != null) target.localScale = original;
        }

        private void BuildDefaultQuestions()
        {
            if (questions != null && questions.Count > 0) return;
            questions = U6_SA_DataTypes_Masters_Phonics.GetDefaultChallengeQuestions();
        }
    }
}
