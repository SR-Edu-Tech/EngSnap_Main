using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U5_SA_UnitChallenge_Masters_Phonics : MonoBehaviour
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
        [SerializeField] private List<Unit5ChallengeQuestion> questions = new List<Unit5ChallengeQuestion>();

        // State
        private int currentQuestionIndex = 0;
        private int totalScore = 0;
        private int currentStreak = 0;
        private bool isProcessing = false;

        private void Awake()
        {
            if (replayAudioBtn != null)
                replayAudioBtn.onClick.AddListener(ReplayQuestionAudio);

            BuildDefaultQuestions();
        }

        private void OnEnable()
        {
            StartChallenge();
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

        private void LoadCurrentQuestion()
        {
            if (currentQuestionIndex >= questions.Count)
            {
                FinishChallenge();
                return;
            }

            isProcessing = false;
            Unit5ChallengeQuestion q = questions[currentQuestionIndex];

            if (questionNumberText != null)
            {
                questionNumberText.text = $"Question {currentQuestionIndex + 1} of {questions.Count}";
                questionNumberText.fontSize = 24;
                questionNumberText.fontStyle = FontStyles.Bold;
            }

            if (promptText != null)
            {
                promptText.text = q.prompt;
                promptText.fontSize = 36;
                promptText.fontStyle = FontStyles.Bold;
                promptText.color = new Color(0.1f, 0.14f, 0.25f, 1f);
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

        private void PlayQuestionAudio(Unit5ChallengeQuestion q)
        {
            if (q.customAudio != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance?.PlayVoicePrompt(q.audioClipName, q.prompt);
            }
            else if (!string.IsNullOrEmpty(q.audioClipName))
            {
                AudioClip clip = Resources.Load<AudioClip>($"U5_audio/{q.audioClipName}");
                if (clip != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayVoicePrompt(q.audioClipName, q.prompt);
            }
        }

        public void ReplayQuestionAudio()
        {
            if (currentQuestionIndex < questions.Count)
            {
                PlayQuestionAudio(questions[currentQuestionIndex]);
                StartCoroutine(PunchScale(replayAudioBtn.transform, 1.15f, 0.15f));
            }
        }

        private void OnOptionSelected(int selectedIndex)
        {
            if (isProcessing) return;
            isProcessing = true;

            Unit5ChallengeQuestion q = questions[currentQuestionIndex];
            bool isCorrect = (selectedIndex == q.correctIndex);

            StartCoroutine(AnswerFeedbackRoutine(selectedIndex, q.correctIndex, isCorrect, q.explanation));
        }

        private IEnumerator AnswerFeedbackRoutine(int selectedIndex, int correctIndex, bool isCorrect, string explanation)
        {
            Image selImg = optionButtons[selectedIndex].GetComponent<Image>();
            Image corImg = optionButtons[correctIndex].GetComponent<Image>();

            if (isCorrect)
            {
                currentStreak++;
                int points = 40 + (currentStreak >= 3 ? 15 : 0);
                totalScore += points;

                if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U5_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(points);

                if (selImg != null) selImg.color = correctOptionColor;
                if (optionTexts[selectedIndex] != null) optionTexts[selectedIndex].color = Color.white;

                if (correctSFX != null && U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(correctSFX);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

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
                if (wrongClip != null && U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(wrongClip);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");
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
            U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("unit_complete");

            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(true);
                if (feedbackText != null)
                    feedbackText.text = $"Unit Challenge Complete!\nDoorkeeper Badge Unlocked!\nTotal Unit Score: {totalScore}";
            }

            StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            yield return new WaitForSeconds(3.0f);
            if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U5_SA_UnitFlowManager_Masters_Phonics.Instance.CompleteCurrentActivity();
            }
        }

        private IEnumerator PunchScale(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            target.localScale = original * scale;
            yield return new WaitForSeconds(duration);
            target.localScale = original;
        }

        private void BuildDefaultQuestions()
        {
            if (questions != null && questions.Count > 0) return;

            questions = new List<Unit5ChallengeQuestion>()
            {
                new Unit5ChallengeQuestion
                {
                    prompt = "In the word <b>hi</b>, is the syllable Open or Closed?",
                    questionType = UnitChallengeType.TextMCQ,
                    options = new string[] { "Open (No consonant closes the door)", "Closed (Door is shut)" },
                    correctIndex = 0,
                    explanation = "No consonant closes the door, so 'i' is free to say its name 'eye'!"
                },
                new Unit5ChallengeQuestion
                {
                    prompt = "What kind of vowel sound does the word <b>got</b> make?",
                    questionType = UnitChallengeType.AudioVowelSound,
                    options = new string[] { "Short 'o' (like in pot)", "Long 'o' (like in go)" },
                    correctIndex = 0,
                    explanation = "The letter 't' closes the door, making 'o' a short vowel sound!"
                },
                new Unit5ChallengeQuestion
                {
                    prompt = "In the word <b>club</b>, is the syllable Open or Closed?",
                    questionType = UnitChallengeType.DoorSwipe,
                    options = new string[] { "Closed (Door closed by 'b')", "Open (Door open)" },
                    correctIndex = 0,
                    explanation = "'b' traps the vowel 'u', making it a short sound!"
                },
                new Unit5ChallengeQuestion
                {
                    prompt = "In <b>ba-con</b>, why does the first syllable have a LONG 'a' sound?",
                    questionType = UnitChallengeType.TextMCQ,
                    options = new string[] { "The first syllable 'ba' is open (no consonant follows 'a')", "Because it has 5 letters", "Because 'con' makes it loud" },
                    correctIndex = 0,
                    explanation = "The first door 'ba' is open, so 'a' says its name 'ay'!"
                },
                new Unit5ChallengeQuestion
                {
                    prompt = "Which of these words has a <b>CLOSED</b> first syllable?",
                    questionType = UnitChallengeType.FirstDoorDrag,
                    options = new string[] { "nap-kin", "ba-by", "ti-ger" },
                    correctIndex = 0,
                    explanation = "'nap' ends in consonant 'p', closing the door!"
                },
                new Unit5ChallengeQuestion
                {
                    prompt = "In <b>me</b>, does the vowel make a short or long sound?",
                    questionType = UnitChallengeType.AudioVowelSound,
                    options = new string[] { "Long 'e' (ee)", "Short 'e' (eh)" },
                    correctIndex = 0,
                    explanation = "'me' is an open syllable, so 'e' says its name 'ee'!"
                },
                new Unit5ChallengeQuestion
                {
                    prompt = "Is the word <b>said</b> a regular closed syllable or a rule-breaker sight word?",
                    questionType = UnitChallengeType.TextMCQ,
                    options = new string[] { "Rule-breaker sight word (sounds like 'sed')", "Regular open syllable" },
                    correctIndex = 0,
                    explanation = "'said' sounds like 'sed' - it is an irregular rule-breaker!"
                },
                new Unit5ChallengeQuestion
                {
                    prompt = "Divide the word <b>equator</b> correctly into syllables:",
                    questionType = UnitChallengeType.ChunkBuilder,
                    options = new string[] { "e - qua - tor", "eq - ua - tor", "equ - a - tor" },
                    correctIndex = 0,
                    explanation = "'e-qua-tor' starts with open syllable 'e' ('ee')!"
                },
                new Unit5ChallengeQuestion
                {
                    prompt = "In <b>spi-der</b>, is the first syllable open or closed?",
                    questionType = UnitChallengeType.FirstDoorDrag,
                    options = new string[] { "Open (Long 'i')", "Closed (Short 'i')" },
                    correctIndex = 0,
                    explanation = "'spi' ends in vowel 'i' with no consonant closing it!"
                },
                new Unit5ChallengeQuestion
                {
                    prompt = "<b>Transfer Challenge:</b> Look at the new word <b>tu-lip</b>. What sound does 'tu' make?",
                    questionType = UnitChallengeType.TransferQuestion,
                    options = new string[] { "Long 'u' ('you') because 'tu' is an open syllable", "Short 'u' (like 'uh') because 'lip' is closed" },
                    correctIndex = 0,
                    explanation = "The first door 'tu' is open, so 'u' makes the long 'you' sound!"
                }
            };
        }
    }
}
