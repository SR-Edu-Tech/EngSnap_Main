using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Unit5
{
    public class WhichSoundController : MonoBehaviour
    {
        [Header("Mascot & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Word Display UI")]
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private Image wordPictureImage;
        [SerializeField] private Button playWordAudioButton;

        [Header("Choice Buttons")]
        [SerializeField] private Button shortChoiceButton;
        [SerializeField] private Button longChoiceButton;

        [Header("Star Meter & Badge")]
        [SerializeField] private Image starMeterFillImage;
        [SerializeField] private TMP_Text starMeterCountText;
        [SerializeField] private GameObject vowelSoundStarBadge;

        [Header("Word Data Sets")]
        [SerializeField] private WhichSoundData[] roundWordData;

        [Header("Voice Script Audio Clips")]
        [SerializeField] private AudioClip introClip;               // "Is it a short sound or a long sound? Listen and choose!"
        [SerializeField] private AudioClip tryAgainClip;          // "Listen again! Is it short or long?"
        [SerializeField] private AudioClip victoryUnlockClip;       // "You are a Vowel Sound Star! Unit 6 is open!"
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip wrongWobbleSfx;
        [SerializeField] private AudioClip starJingleSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentWordIndex = 0;
        private int correctAnswersCount = 0;
        private int totalRounds = 5;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            if (sfxAudioSource == null)
            {
                sfxAudioSource = GetComponent<AudioSource>();
                if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }
            sfxAudioSource.spatialBlend = 0f;
            sfxAudioSource.volume = 1f;

            if (voiceAudioSource == null) voiceAudioSource = sfxAudioSource;
            else voiceAudioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            EnsureAudioSources();

            if (shortChoiceButton != null)
            {
                shortChoiceButton.onClick.RemoveAllListeners();
                shortChoiceButton.onClick.AddListener(OnShortChoiceClicked);
            }

            if (longChoiceButton != null)
            {
                longChoiceButton.onClick.RemoveAllListeners();
                longChoiceButton.onClick.AddListener(OnLongChoiceClicked);
            }

            if (playWordAudioButton != null)
            {
                playWordAudioButton.onClick.RemoveAllListeners();
                playWordAudioButton.onClick.AddListener(ReplayWordAudio);
            }

            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }

            ResetLevel();
        }

        private void OnEnable()
        {
            ResetLevel();
            StartCoroutine(StartIntroOnNextFrame());
        }

        private IEnumerator StartIntroOnNextFrame()
        {
            yield return null;
            if (gameObject.activeInHierarchy && !isActivityCompleted)
            {
                yield return StartCoroutine(IntroSequence());
            }
        }

        public void ResetLevel()
        {
            currentWordIndex = 0;
            correctAnswersCount = 0;
            isTransitioning = false;
            isActivityCompleted = false;

            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (vowelSoundStarBadge != null) vowelSoundStarBadge.SetActive(false);

            totalRounds = (roundWordData != null && roundWordData.Length > 0) ? roundWordData.Length : 5;
            UpdateStarMeterUI();

            LoadNextWord();
            SetSubtitles("Is it a short sound or a long sound? Listen and choose!");
        }

        private void LoadNextWord()
        {
            if (roundWordData == null || roundWordData.Length == 0) return;

            if (currentWordIndex < roundWordData.Length)
            {
                WhichSoundData data = roundWordData[currentWordIndex];
                if (data != null)
                {
                    if (wordText != null) wordText.text = data.word;
                    if (wordPictureImage != null && data.wordSprite != null) wordPictureImage.sprite = data.wordSprite;
                    if (data.wordAudioClip != null) PlayAudio(data.wordAudioClip);
                }
            }
        }

        public void ReplayWordAudio()
        {
            if (isTransitioning) return;
            if (roundWordData != null && currentWordIndex < roundWordData.Length)
            {
                WhichSoundData data = roundWordData[currentWordIndex];
                if (data != null && data.wordAudioClip != null)
                {
                    PlayAudio(data.wordAudioClip);
                }
            }
        }

        private void PlayAudio(AudioClip clip)
        {
            if (clip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
            }
        }

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Is it a short sound or a long sound? Listen and choose!");

            if (introClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = introClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(introClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
        }

        public void OnShortChoiceClicked()
        {
            if (isTransitioning) return;
            EvaluateChoice(false);
        }

        public void OnLongChoiceClicked()
        {
            if (isTransitioning) return;
            EvaluateChoice(true);
        }

        private void EvaluateChoice(bool selectedLong)
        {
            if (roundWordData == null || currentWordIndex >= roundWordData.Length) return;

            WhichSoundData data = roundWordData[currentWordIndex];
            bool isCorrect = (data != null) && (data.isLongVowel == selectedLong);

            if (isCorrect)
            {
                StartCoroutine(CorrectChoiceSequence(data));
            }
            else
            {
                StartCoroutine(WrongChoiceSequence());
            }
        }

        private IEnumerator CorrectChoiceSequence(WhichSoundData data)
        {
            isTransitioning = true;

            if (correctChimeSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(correctChimeSfx);
            }

            correctAnswersCount++;
            UpdateStarMeterUI();

            string word = (data != null) ? data.word : "word";
            string vowel = (data != null) ? data.targetVowel.ToLower() : "a";
            string soundType = (data != null && data.isLongVowel) ? "long" : "short";

            SetSubtitles($"Yes! '{word}' has the {soundType} {vowel} sound!");

            if (data != null && data.praiseAudioClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = data.praiseAudioClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(data.praiseAudioClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            currentWordIndex++;
            if (currentWordIndex >= totalRounds && !isActivityCompleted)
            {
                yield return StartCoroutine(CompletionSequence());
            }
            else
            {
                LoadNextWord();
                isTransitioning = false;
            }
        }

        private IEnumerator WrongChoiceSequence()
        {
            isTransitioning = true;

            if (wrongWobbleSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(wrongWobbleSfx);
            }

            SetSubtitles("Listen again! Is it short or long?");

            if (tryAgainClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = tryAgainClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(tryAgainClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            isTransitioning = false;
        }

        private void UpdateStarMeterUI()
        {
            if (starMeterCountText != null)
            {
                starMeterCountText.text = $"{correctAnswersCount} / {totalRounds}";
            }

            if (starMeterFillImage != null && totalRounds > 0)
            {
                starMeterFillImage.fillAmount = (float)correctAnswersCount / totalRounds;
            }
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            if (vowelSoundStarBadge != null) vowelSoundStarBadge.SetActive(true);
            if (starJingleSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(starJingleSfx);
            }

            SetSubtitles("You are a Vowel Sound Star! Unit 6 is open!");

            if (victoryUnlockClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = victoryUnlockClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(victoryUnlockClip.length + 0.3f);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);
            TopicProgressUI.MarkTopicComplete("Unit5", "WhichSound");

            isTransitioning = false;
        }

        public void GoToNextPanel()
        {
            if (isActivityCompleted)
            {
                TopicProgressUI.MarkTopicComplete(gameObject);
            }

            ResetLevel();

            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
                if (unitContentPanel != null && nextPanel != unitContentPanel && !nextPanel.transform.IsChildOf(unitContentPanel.transform))
                {
                    unitContentPanel.SetActive(false);
                }
            }
            else if (unitContentPanel != null)
            {
                unitContentPanel.SetActive(true);
            }

            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }

        private void SetSubtitles(string text)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
        }
    }
}
