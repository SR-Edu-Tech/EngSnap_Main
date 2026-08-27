using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Subclass for Unit 7 (Collocations) Reading Lesson One: R01 Complete the Collocation (in-context).
/// Core gameplay: 12 balanced rounds across GET, CATCH, SAVE, and IDEA hubs.
/// In-context sentence gap completion with cross-hub distractors shuffled dynamically.
/// Correct selection snaps chip into gap, reveals completed sentence, and plays ARIA full sentence voiceover.
/// Pass threshold: 9 out of 12 correct.
/// </summary>
public class Masters_Collocations_Reading_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class ReadingR01RoundData {
        public string sentenceWithGap;
        public string correctCompletion;
        public CollocationHub hubId;
        public string[] distractors;
        public string fullSentenceReveal;
        public AudioClip revealAudio;
    }

    [Header("Unit 7 Collocations Reading R01 Data")]
    [SerializeField] private ReadingR01RoundData[] rounds;
    [SerializeField] private TextMeshProUGUI sentenceTMP;
    [SerializeField] private Button[] optionChips; // 4 Option Chips
    [SerializeField] private TextMeshProUGUI collocationR01ProgressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTMP;
    [SerializeField] private Button retryButton;

    [Header("Audio & SFX")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip snapSFX;
    [SerializeField] private AudioClip repelSFX;

    [Header("Rules")]
    [SerializeField] private int passScore = 6;

    private int currentQuestionIndex = 0;
    private int correctScore = 0;
    private bool isAnswering = false;

    protected override void Awake() {
        base.Awake();
        AutoFindUIReferences();
    }

    protected override void Start() {
        topic = Masters_Topic.Reading;
        UpdateTitleAndUIComponents();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        currentQuestionIndex = 0;
        correctScore = 0;
        StartCoroutine(InitializeCollocationR01Routine());
    }

    private void AutoFindUIReferences() {
        if (sentenceTMP == null) {
            Transform card = transform.Find("SentenceCard") ?? transform.Find("ReadingBench/SentenceCard");
            if (card != null) sentenceTMP = card.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (collocationR01ProgressTMP == null) {
            Transform prog = transform.Find("ProgressIndicator") ?? transform.Find("ProgressText");
            if (prog != null) collocationR01ProgressTMP = prog.GetComponent<TextMeshProUGUI>();
        }

        if (scoreTMP == null) {
            Transform sc = transform.Find("ScoreIndicator") ?? transform.Find("ScoreText");
            if (sc != null) scoreTMP = sc.GetComponent<TextMeshProUGUI>();
        }

        if (optionChips == null || optionChips.Length < 4) {
            Transform chipsContainer = transform.Find("OptionChips") ?? transform.Find("Chips");
            if (chipsContainer != null) {
                Button[] btns = chipsContainer.GetComponentsInChildren<Button>(true);
                if (btns.Length >= 4) {
                    optionChips = new Button[4];
                    for (int i = 0; i < 4 && i < btns.Length; i++) {
                        optionChips[i] = btns[i];
                    }
                }
            }
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text;
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Occasion") || textVal.Contains("Polished") || textVal.Contains("R01") || textVal.Contains("Complete")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "R01 Complete the Collocation (in-context)";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("LISTENING") || textVal.Contains("READING")) {
                tmp.text = "READING BRANCH (Reading Bench)";
            }
        }
    }

    private IEnumerator InitializeCollocationR01Routine() {
        AudioClip clip = ariaIntroAudio ?? narratorSpeech;
        if (clip == null) {
#if UNITY_EDITOR
            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Reading/R01/Which word snaps into this gap.mp3");
            if (clip == null) {
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Reading/Which word snaps together with this word to make a real collocation.mp3");
            }
#endif
        }

        if (clip != null) {
            ariaIntroAudio = clip;
            narratorSpeech = clip;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }

            AudioSource localSource = GetComponent<AudioSource>();
            if (localSource == null) localSource = gameObject.AddComponent<AudioSource>();
            localSource.Stop();
            localSource.clip = clip;
            localSource.volume = 1.0f;
            localSource.spatialBlend = 0f;
            localSource.Play();

            yield return new WaitForSeconds(clip.length + 0.2f);
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        LoadRound(0);
    }

    private void LoadRound(int index) {
        if (rounds == null || index >= rounds.Length) {
            EvaluateFinalScore();
            return;
        }

        currentQuestionIndex = index;
        isAnswering = false;

        ReadingR01RoundData r = rounds[currentQuestionIndex];
        if (r == null) return;

        if (sentenceTMP != null) {
            sentenceTMP.text = r.sentenceWithGap;
            sentenceTMP.transform.DOKill();
            sentenceTMP.transform.localScale = Vector3.one;
            sentenceTMP.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
        }

        if (collocationR01ProgressTMP != null) {
            collocationR01ProgressTMP.text = $"Question {currentQuestionIndex + 1}/{rounds.Length}";
        }

        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {correctScore}";
        }

        // Prepare and shuffle option chips (1 correct + 3 distractors)
        List<string> options = new List<string>();
        if (!string.IsNullOrEmpty(r.correctCompletion)) options.Add(r.correctCompletion);
        if (r.distractors != null) {
            foreach (var d in r.distractors) {
                if (!string.IsNullOrEmpty(d) && !options.Contains(d)) options.Add(d);
            }
        }

        // Shuffle options deterministically based on round index
        System.Random rng = new System.Random(currentQuestionIndex + 77);
        int n = options.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            string value = options[k];
            options[k] = options[n];
            options[n] = value;
        }

        if (optionChips != null && optionChips.Length > 0) {
            Color[] chipColors = new Color[] {
                new Color(0.9f, 0.32f, 0.32f, 1f), // GET Red
                new Color(0.2f, 0.72f, 0.45f, 1f), // CATCH Green
                new Color(0.28f, 0.55f, 0.9f, 1f), // IDEA Blue
                new Color(0.95f, 0.65f, 0.2f, 1f)  // SAVE Orange
            };

            for (int i = 0; i < optionChips.Length; i++) {
                if (optionChips[i] == null) continue;

                bool hasText = (i < options.Count);
                optionChips[i].gameObject.SetActive(hasText);

                if (hasText) {
                    string optionText = options[i];

                    Image btnImg = optionChips[i].GetComponent<Image>();
                    if (btnImg != null) {
                        btnImg.color = chipColors[i % chipColors.Length];
                    }

                    TMP_Text tmp = optionChips[i].GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null) {
                        tmp.gameObject.SetActive(true);
                        tmp.text = optionText;
                        tmp.color = Color.white;
                    }

                    optionChips[i].onClick.RemoveAllListeners();
                    optionChips[i].onClick.AddListener(() => OnOptionSelected(optionText, i));
                }
            }
        }
    }

    private void OnOptionSelected(string selectedOption, int chipIndex) {
        if (isAnswering || rounds == null || currentQuestionIndex >= rounds.Length) return;

        ReadingR01RoundData r = rounds[currentQuestionIndex];
        if (r == null) return;

        bool isCorrect = (selectedOption == r.correctCompletion);

        if (isCorrect) {
            isAnswering = true;
            correctScore++;

            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {correctScore}";
            }

            // Magnetic Snap Sound
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            }

            // Visual Snap Punch on Chip
            if (chipIndex < optionChips.Length && optionChips[chipIndex] != null) {
                optionChips[chipIndex].transform.DOKill(true);
                optionChips[chipIndex].transform.DOPunchScale(Vector3.one * 0.22f, 0.35f, 10, 0.8f);
            }

            // Update Sentence to Full Reveal
            if (sentenceTMP != null && !string.IsNullOrEmpty(r.fullSentenceReveal)) {
                sentenceTMP.text = r.fullSentenceReveal;
                sentenceTMP.transform.DOKill(true);
                sentenceTMP.transform.DOPunchScale(Vector3.one * 0.18f, 0.35f);
            }

            // Play ARIA full sentence voiceover
            if (r.revealAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(r.revealAudio);
            }

            float delay = (r.revealAudio != null) ? r.revealAudio.length + 0.5f : 2.0f;
            StartCoroutine(NextRoundRoutine(delay));
        } else {
            // Magnetic Repel Sound & Visual Shake
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (chipIndex < optionChips.Length && optionChips[chipIndex] != null) {
                optionChips[chipIndex].transform.DOKill(true);
                optionChips[chipIndex].transform.DOShakePosition(0.45f, new Vector3(14f, 0f, 0f), 15, 90f);
            }

            // Keep round active - student can retry
        }
    }

    private IEnumerator NextRoundRoutine(float delay) {
        yield return new WaitForSeconds(delay);
        LoadRound(currentQuestionIndex + 1);
    }

    private void EvaluateFinalScore() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (correctScore >= passScore);

        if (resultTMP != null) {
            if (passed) {
                resultTMP.text = $"EXCELLENT! Score: {correctScore}/{rounds.Length}\nYou completed the collocations!";
            } else {
                resultTMP.text = $"TRY AGAIN! Score: {correctScore}/{rounds.Length}\nYou need at least {passScore}/12 to pass.";
            }
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
                NextButtonAnimation();
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (retryButton != null) {
                retryButton.gameObject.SetActive(true);
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(RestartLesson);
            }
        }
    }

    public void RestartLesson() {
        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }
        currentQuestionIndex = 0;
        correctScore = 0;
        LoadRound(0);
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Reading;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}