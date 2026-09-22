using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U4_SA_UnitChallenge_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Instruction")]
        [Tooltip("U04_VO_challenge_intro: 'Ten questions. Ears first.' (3s)")]
        public AudioClip challengeIntroClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioButton;

        [Header("3. Question & Options Container")]
        [SerializeField] private TextMeshProUGUI questionTextTMP;
        [SerializeField] private Transform optionsContainer;

        private List<ChallengeQuestionItem> questions;
        private int currentQuestionIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        private Sprite proceduralRoundedSprite;

        private void Awake()
        {
            AutoBindHierarchyElements();
            InitializeQuestions();
        }

        private void Start()
        {
            PlayIntroInstruction();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            currentQuestionIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            InitializeQuestions();
            LoadCurrentQuestion();

            PlayIntroInstruction();
        }

        public void PlayIntroInstruction()
        {
            StartCoroutine(DelayedPlayIntro());
        }

        private IEnumerator DelayedPlayIntro()
        {
            yield return new WaitForSeconds(0.15f);
            AudioClip clip = challengeIntroClip ?? Resources.Load<AudioClip>("U4_audio/Ten questions Ears first");
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null && clip != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
            }
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("Title BG/TitleText") ?? transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (promptTMP == null)
            {
                var t = transform.Find("prompt bg/Prompt_Text") ?? transform.Find("Prompt_Text") ?? transform.Find("PromptText") ?? transform.Find("InstructionText");
                if (t != null) promptTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (scoreTMP == null)
            {
                var t = transform.Find("ScoreText") ?? transform.Find("Score_Text") ?? transform.Find("Score");
                if (t != null) scoreTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("ProgressBar (1)") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            if (questionTextTMP == null)
            {
                Transform t = transform.Find("Question_Text") ?? transform.Find("QuestionText") ?? transform.Find("Prompt") ?? transform.Find("QuestionCard/QuestionText");
                if (t != null) questionTextTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (optionsContainer == null)
            {
                Transform t = transform.Find("Options_Container") ?? transform.Find("Options_Row") ?? transform.Find("AnswersContainer") ?? transform.Find("Options");
                if (t != null) optionsContainer = t;
            }

            if (replayAudioButton == null)
            {
                Transform r = transform.Find("ReplayButton") ?? transform.Find("Audio_Button") ?? transform.Find("Speaker_Button") ?? transform.Find("Replay_Button") ?? transform.Find("SpeakerButton");
                if (r == null)
                {
                    Button[] btns = GetComponentsInChildren<Button>(true);
                    foreach (var b in btns)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if (bName.Contains("replay") || bName.Contains("speaker") || bName.Contains("audio"))
                        {
                            replayAudioButton = b;
                            break;
                        }
                    }
                }
                else
                {
                    replayAudioButton = r.GetComponent<Button>();
                }
            }
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(replayAudioButton.transform, 1.15f));
                    PlayIntroInstruction();
                });
            }
        }

        private void InitializeQuestions()
        {
            questions = new List<ChallengeQuestionItem>
            {
                // Q1
                new ChallengeQuestionItem { prompt = "How many vowel sounds are in ONE syllable?", options = new[] { "One", "Two", "It depends" }, correctIndex = 0, explanation = "Every syllable has exactly ONE vowel sound!" },
                // Q2
                new ChallengeQuestionItem { prompt = "Which syllable type is \"bone\"?", options = new[] { "Magic 'e'", "Closed", "Vowel Team" }, correctIndex = 0, explanation = "\"bone\" follows the v c e pattern!" },
                // Q3
                new ChallengeQuestionItem { prompt = "Which beat is stronger in \"alarm\"?", options = new[] { "a", "LARM" }, correctIndex = 1, explanation = "\"LARM\" is the loud accented syllable!" },
                // Q4
                new ChallengeQuestionItem { prompt = "The schwa sound /e/ is...", options = new[] { "A lazy 'uh' sound", "A long 'ay' sound", "A silent letter" }, correctIndex = 0, explanation = "The schwa is the quick, lazy 'uh' sound in unaccented beats!" },
                // Q5
                new ChallengeQuestionItem { prompt = "Which letter makes the schwa in \"pencil\"?", options = new[] { "e", "n", "i" }, correctIndex = 2, explanation = "The 'i' goes lazy before final l: \"penc-ul\"!" },
                // Q6
                new ChallengeQuestionItem { prompt = "The vowel before a final L is usually...", options = new[] { "A schwa /e/", "A long vowel", "Silent" }, correctIndex = 0, explanation = "Clue 1: Vowels before final L weaken to schwa!" },
                // Q7
                new ChallengeQuestionItem { prompt = "Which beat is stronger in \"bacon\"?", options = new[] { "BA", "con" }, correctIndex = 0, explanation = "\"BA\" is the accented, louder beat!" },
                // Q8
                new ChallengeQuestionItem { prompt = "Which syllable type is \"cat\"?", options = new[] { "Open", "Closed", "r-Controlled" }, correctIndex = 1, explanation = "\"cat\" ends in a consonant that closes the vowel!" },
                // Q9
                new ChallengeQuestionItem { prompt = "Which letter is the schwa in \"zebra\"?", options = new[] { "e", "b", "a" }, correctIndex = 2, explanation = "Clue 2: The final 'a' sounds like \"uh\" (zeb-ruh)!" },
                // Q10
                new ChallengeQuestionItem { prompt = "Can a one-syllable word ever be divided?", options = new[] { "No, never", "Yes, always", "Only long ones" }, correctIndex = 0, explanation = "Rule 3: A one-syllable word is NEVER divided!" }
            };
        }

        private void LoadCurrentQuestion()
        {
            if (currentQuestionIndex >= questions.Count)
            {
                OnChallengeComplete();
                return;
            }

            isProcessingAnswer = false;
            ChallengeQuestionItem q = questions[currentQuestionIndex];

            if (titleTMP != null)
            {
                titleTMP.text = $"<b><color=#000000>UNIT CHALLENGE ({currentQuestionIndex + 1}/10)</color></b>";
                titleTMP.enableAutoSizing = true;
                titleTMP.fontSizeMin = 28;
                titleTMP.fontSizeMax = 44;
            }

            if (promptTMP != null)
            {
                promptTMP.text = "<color=#FFFFFF><b>Read carefully and tap the correct answer!</b></color>";
                promptTMP.enableAutoSizing = true;
                promptTMP.fontSizeMin = 22;
                promptTMP.fontSizeMax = 32;
            }

            if (progressBar != null)
            {
                progressBar.value = (float)currentQuestionIndex / questions.Count;
            }

            if (questionTextTMP != null)
            {
                questionTextTMP.text = $"<b><size=44><color=#000000>{q.prompt}</color></size></b>";
                questionTextTMP.alignment = TextAlignmentOptions.Center;
            }

            RenderOptions(q);
        }

        private void RenderOptions(ChallengeQuestionItem q)
        {
            if (optionsContainer == null) return;
            foreach (Transform c in optionsContainer) Destroy(c.gameObject);

            var hlg = optionsContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = optionsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            for (int i = 0; i < q.options.Length; i++)
            {
                int optIdx = i;
                string optText = q.options[i];

                GameObject btnObj = new GameObject($"Option_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(optionsContainer, false);
                RectTransform rt = btnObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(280, 110);

                Image img = btnObj.GetComponent<Image>();
                img.sprite = GetRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(btnObj.transform, false);
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(10, 8);
                textRT.offsetMax = new Vector2(-10, -8);

                var tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b><size=38><color=#000000>{optText}</color></size></b>";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                Button btn = btnObj.GetComponent<Button>();
                btn.onClick.AddListener(() => OnOptionSelected(optIdx, q, btnObj));
            }
        }

        private void OnOptionSelected(int chosenIndex, ChallengeQuestionItem q, GameObject btnObj)
        {
            if (isProcessingAnswer) return;
            isProcessingAnswer = true;

            if (chosenIndex == q.correctIndex)
            {
                StartCoroutine(HandleCorrect(q, btnObj));
            }
            else
            {
                StartCoroutine(HandleWrong(q, btnObj));
            }
        }

        private IEnumerator HandleCorrect(ChallengeQuestionItem q, GameObject btnObj)
        {
            currentScore += 100;
            UpdateScoreUI();

            if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U4_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(100);
            }

            if (btnObj != null)
            {
                var img = btnObj.GetComponent<Image>();
                if (img != null) img.color = new Color(0.72f, 1f, 0.72f);
                StartCoroutine(PunchScale(btnObj.transform, 1.15f));
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFFFFF><b>Correct! {q.explanation}</b></color>";
            }

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();
            }

            yield return new WaitForSeconds(1.3f);

            currentQuestionIndex++;
            LoadCurrentQuestion();
        }

        private IEnumerator HandleWrong(ChallengeQuestionItem q, GameObject btnObj)
        {
            if (btnObj != null)
            {
                var img = btnObj.GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 0.78f, 0.78f);
                StartCoroutine(ShakeOption(btnObj.transform));
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFD54F><b>Remember: {q.explanation}</b></color>";
            }

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            yield return new WaitForSeconds(1.5f);

            currentQuestionIndex++;
            LoadCurrentQuestion();
        }

        private IEnumerator ShakeOption(Transform target)
        {
            if (target == null) yield break;
            Vector3 original = target.localPosition;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.deltaTime;
                float xOffset = Mathf.Sin(elapsed * 45f) * 12f;
                target.localPosition = original + new Vector3(xOffset, 0, 0);
                yield return null;
            }
            if (target != null) target.localPosition = original;
        }

        private IEnumerator PunchScale(Transform target, float targetScale)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            float elapsed = 0f;
            float duration = 0.18f;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(original * targetScale, original, t);
                yield return null;
            }
            if (target != null) target.localScale = original;
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null)
            {
                scoreTMP.text = $"Score: <b>{currentScore}</b>";
                scoreTMP.fontSize = 32;
            }
        }

        private void OnChallengeComplete()
        {
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>Unit 4 Challenge Complete!</b></color>";
            if (progressBar != null) progressBar.value = 1f;

            if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U4_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }

        private Sprite GetRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 24;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius - 1)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist <= radius)
                    {
                        colors[y * size + x] = new Color(1f, 1f, 1f, radius - dist);
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
    }
}
