using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit8 {


    /// <summary>
    /// Quiz Panel controller for Unit 8 Source of Inspiration: Q01 The Sound Check Quiz.
    /// Manages 8 mixed questions (Audio MCQ, MCQ, Fill/Tap, Order, True/False).
    /// Pass mark is 6 of 8. Features big audio play button, color feedback, wrong answer hint box, and audio readbacks.
    /// </summary>
    public class Masters_SourceOfInspiration_Quiz_LessonOne : Masters_Lesson {

public class SOIQuizQuestion {
        public int id;
        public string format;
        public string questionText;
        public string[] options;
        public int correctOptionIndex;
        public string explanationText;
        public AudioClip audioClip;
    }
    
        [Header("UI References")]
        public TextMeshProUGUI formatHeaderTMP;
        public TextMeshProUGUI questionTextTMP;
        public TextMeshProUGUI progressTMP;
        public TextMeshProUGUI explanationTMP;

        public Button playAudioBtn;
        public Button[] optionButtons;
        public Image[] optionImages;

        [Header("Result Panel References")]
        public GameObject quizResultSubPanel;
        public TextMeshProUGUI quizResultScoreTMP;
        public TextMeshProUGUI quizResultStatusTMP;

        [Header("Audio Clips")]
        public AudioClip q01IntroAudio;
        public AudioClip voWitchAudio;
        public AudioClip voWishAudio;
        public AudioClip q01PassAudio;
        public AudioClip q01FailAudio;

        [Header("Colors")]
        public Color normalBtnColor = new Color(0.15f, 0.22f, 0.35f, 1f);
        public Color correctColor = new Color(0.2f, 0.8f, 0.26f, 1f);
        public Color wrongColor = new Color(0.95f, 0.25f, 0.2f, 1f);

        private List<SOIQuizQuestion> questions;
        private int currentQIdx = 0;
        private int score = 0;
        private SOIQuizQuestion currentQ;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Quiz;
            CacheOptionImages();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Quiz;
            StartQuiz();
        }

        private void CacheOptionImages() {
            if (optionButtons != null) {
                optionImages = new Image[optionButtons.Length];
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null) {
                        optionImages[i] = optionButtons[i].GetComponent<Image>();
                    }
                }
            }
        }



        private void OnEnable() {
            StartQuiz();
        }

        public void StartQuiz() {
            RestartQuiz();
        }

        public void RestartQuiz() {
            if (quizResultSubPanel != null) quizResultSubPanel.SetActive(false);
            currentQIdx = 0;
            score = 0;

            if (q01IntroAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(q01IntroAudio);
            }

            InitQuestions();
            LoadQuestion(currentQIdx);
        }

        private void InitQuestions() {
            questions = new List<SOIQuizQuestion>();

            // Q1 (Audio MCQ)
            questions.Add(new SOIQuizQuestion {
                id = 1, format = "Audio MCQ",
                questionText = "ARIA plays a audio clip below. Was it 'wish' or 'witch'?",
                options = new string[] { "wish", "witch" },
                correctOptionIndex = 1,
                explanationText = "The sound ended with a sharp 'ch' sound -> 'witch'!",
                audioClip = voWitchAudio
            });

            // Q2 (Audio MCQ)
            questions.Add(new SOIQuizQuestion {
                id = 2, format = "Audio MCQ",
                questionText = "ARIA plays a audio clip below. Was it 'wish' or 'witch'?",
                options = new string[] { "wish", "witch" },
                correctOptionIndex = 0,
                explanationText = "The sound ended with a soft 'sh' sound -> 'wish'!",
                audioClip = voWishAudio
            });

            // Q3 (MCQ)
            questions.Add(new SOIQuizQuestion {
                id = 3, format = "MCQ",
                questionText = "Which word appears only ONCE in the whole twister?",
                options = new string[] { "wish", "witch", "you" },
                correctOptionIndex = 1,
                explanationText = "'witch' appears only once in 'the witch wishes'!"
            });

            // Q4 (Fill / Tap)
            questions.Add(new SOIQuizQuestion {
                id = 4, format = "Fill / Tap",
                questionText = "Finish the line: 'I wish to wish the wish you wish to _______'",
                options = new string[] { "witch", "wish", "won't" },
                correctOptionIndex = 1,
                explanationText = "The line completes with 'wish' -> 'wish you wish to wish'."
            });

            // Q5 (MCQ)
            questions.Add(new SOIQuizQuestion {
                id = 5, format = "MCQ",
                questionText = "The tricky part of this twister is that two words sound almost the same. Which two?",
                options = new string[] { "wish & witch", "wish & won't", "you & wish" },
                correctOptionIndex = 0,
                explanationText = "'wish' (/wɪʃ/) and 'witch' (/wɪtʃ/) differ by only one final sound!"
            });

            // Q6 (Order)
            questions.Add(new SOIQuizQuestion {
                id = 6, format = "Order",
                questionText = "Put the three lines in order:\nLine 1: 'I wish to wish...'\nLine 2: 'but if you wish...'\nLine 3: 'I won't wish...'",
                options = new string[] { "Line 1 -> Line 2 -> Line 3", "Line 2 -> Line 1 -> Line 3", "Line 3 -> Line 2 -> Line 1" },
                correctOptionIndex = 0,
                explanationText = "The twister order is Line 1 -> Line 2 -> Line 3!"
            });

            // Q7 (True/False)
            questions.Add(new SOIQuizQuestion {
                id = 7, format = "True/False",
                questionText = "True or False: A tongue twister is meant to be hard to say fast.",
                options = new string[] { "True", "False" },
                correctOptionIndex = 0,
                explanationText = "True! Tongue twisters challenge clear fast pronunciation."
            });

            // Q8 (MCQ)
            questions.Add(new SOIQuizQuestion {
                id = 8, format = "MCQ",
                questionText = "What's the best way to get good at a tongue twister?",
                options = new string[] { "Start slow, then speed up", "Start as fast as you can", "Say it silently" },
                correctOptionIndex = 0,
                explanationText = "Practice rule: Start slow for accuracy, then build up speed!"
            });
        }

        private void LoadQuestion(int index) {
            if (questions == null || index >= questions.Count) {
                EndQuiz();
                return;
            }

            currentQ = questions[index];
            if (progressTMP != null) progressTMP.text = $"Question {index + 1}/8";
            if (formatHeaderTMP != null) formatHeaderTMP.text = $"FORMAT: {currentQ.format.ToUpper()}";
            if (questionTextTMP != null) questionTextTMP.text = currentQ.questionText;
            if (explanationTMP != null) explanationTMP.text = "";

            ResetOptionVisuals();
            SetOptionsInteractable(true);

            // Show big play audio button if question has audio
            bool hasAudio = (currentQ.audioClip != null);
            if (playAudioBtn != null) {
                playAudioBtn.gameObject.SetActive(hasAudio);
            }
            if (hasAudio) {
                PlayQuestionAudio();
            }

            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] == null) continue;

                    if (currentQ.options != null && i < currentQ.options.Length) {
                        optionButtons[i].gameObject.SetActive(true);
                        string optText = currentQ.options[i];
                        TextMeshProUGUI tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = optText;

                        int optionIndex = i;
                        optionButtons[i].onClick.RemoveAllListeners();
                        optionButtons[i].onClick.AddListener(() => OnOptionSelected(optionIndex));
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void PlayQuestionAudio() {
            if (currentQ != null && currentQ.audioClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentQ.audioClip);
            }
        }

        private void OnPlayAudioClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            PlayQuestionAudio();
        }

        private void ResetOptionVisuals() {
            if (optionImages != null) {
                foreach (var img in optionImages) {
                    if (img != null) img.color = normalBtnColor;
                }
            }
        }

        private void SetOptionsInteractable(bool state) {
            if (optionButtons != null) {
                foreach (var btn in optionButtons) {
                    if (btn != null) btn.interactable = state;
                }
            }
        }

        private void OnOptionSelected(int index) {
            if (currentQ == null) return;

            bool isCorrect = (index == currentQ.correctOptionIndex);
            if (optionButtons != null && index < optionButtons.Length && optionButtons[index] != null) {
                Image img = optionButtons[index].GetComponent<Image>();
                if (img != null) img.color = isCorrect ? correctColor : wrongColor;
            }

            if (isCorrect) {
                score++;
                if (explanationTMP != null) explanationTMP.text = $"CORRECT! {currentQ.explanationText}";
                if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

                SetOptionsInteractable(false);
                StartCoroutine(AdvanceQuestionRoutine());
            } else {
                if (explanationTMP != null) explanationTMP.text = $"INCORRECT! {currentQ.explanationText}";
                if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);

                SetOptionsInteractable(false);
                StartCoroutine(WrongAnswerDelayRoutine());
            }
        }

        private IEnumerator WrongAnswerDelayRoutine() {
            yield return new WaitForSeconds(1.5f);
            ResetOptionVisuals();
            SetOptionsInteractable(true);
        }

        private IEnumerator AdvanceQuestionRoutine() {
            yield return new WaitForSeconds(1.5f);
            currentQIdx++;
            LoadQuestion(currentQIdx);
        }

        private void EndQuiz() {
            bool passed = (score >= 6);
            if (quizResultSubPanel != null) {
                quizResultSubPanel.SetActive(true);
                if (quizResultScoreTMP != null) quizResultScoreTMP.text = $"Final Score: {score}/8";
                if (quizResultStatusTMP != null) {
                    quizResultStatusTMP.text = passed ? "CONGRATULATIONS! SOUND CHECK PASSED!" : "SCORED LESS THAN 6/8. TAP RETRY TO TRY AGAIN!";
                    quizResultStatusTMP.color = passed ? Color.green : Color.red;
                }
            }

            if (passed && nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Quiz;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
