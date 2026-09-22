using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit2
{
    public class TwoFamiliesController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit2";
        [SerializeField] private string topicName = "TwoFamilies";

        [Header("Data Reference")]
        [SerializeField] private TwoFamiliesData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Phase 1: Mouth Demo UI")]
        [SerializeField] private GameObject mouthDemoPanel;
        [SerializeField] private Image mouthCloseUpImage;
        [SerializeField] private Button nextToSortingButton;

        [Header("Phase 2: Sorting Game UI")]
        [SerializeField] private GameObject sortingGamePanel;
        [SerializeField] private Image currentLetterImage;
        [SerializeField] private TMP_Text currentLetterText;
        [SerializeField] private Button vowelHousesButton; // Gold houses
        [SerializeField] private Button consonantStreetButton; // Consonant street
        [SerializeField] private RectTransform vowelHousesRect;
        [SerializeField] private RectTransform consonantStreetRect;

        [Header("Phase 3: Free Play Vowel Choir UI")]
        [SerializeField] private GameObject freePlayPanel;
        [SerializeField] private Button[] vowelSingersButtons; // 5 Vowels: A, E, I, O, U

        [Header("Progress & Rewards")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private GameObject starMeterGameObject;
        [SerializeField] private RectTransform starMeterRect;
        [Header("Rewards & Progression")]
        [Tooltip("Confetti particle system to play on completion.")]
        [SerializeField] private GameObject confettiParticles;

        [Tooltip("The sticker reward popup screen.")]
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;

        [Tooltip("The button to continue to the next activity.")]
        [SerializeField] private GameObject continueButton;

        [Tooltip("The next panel or activity to show when Continue is clicked.")]
        [SerializeField] private GameObject nextPanel;

        [Tooltip("The current panel to hide when Continue is clicked. (Assign this GameObject or its parent panel)")]
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        [Header("Mascots")]
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip starPopSfx;
        [SerializeField] private AudioClip houseCheerSfx;

        private int currentRoundIndex = 0;
        private int totalRounds = 10;
        private int attemptCount = 0;
        private bool isTransitioning = false;
        private Coroutine wiggleCoroutine;
        private Coroutine freePlayWobbleCoroutine;

        public string UnitID => unitID;
        public string TopicName => topicName;

        private void Awake()
        {
            EnsureAudioSources();
            SetupButtonListeners();
        }

        private void Start()
        {
            StartActivity();
        }

        private void OnEnable()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            StartActivity();
        }

        private void OnDisable()
        {
            StopAllAudio();
            StopAllCoroutines();
            DeactivateMascots();
        }

        private void StopAllAudio()
        {
            if (voiceAudioSource != null && voiceAudioSource.isPlaying) voiceAudioSource.Stop();
            if (sfxAudioSource != null && sfxAudioSource.isPlaying) sfxAudioSource.Stop();
        }

        private void EnsureAudioSources()
        {
            if (voiceAudioSource == null) voiceAudioSource = gameObject.AddComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.spatialBlend = 0f;
            sfxAudioSource.spatialBlend = 0f;
        }

        private void SetupButtonListeners()
        {
            if (vowelHousesButton != null)
                vowelHousesButton.onClick.AddListener(() => OnChoiceSelected(true));
            if (consonantStreetButton != null)
                consonantStreetButton.onClick.AddListener(() => OnChoiceSelected(false));
            if (nextToSortingButton != null)
                nextToSortingButton.onClick.AddListener(StartSortingPhase);
            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }

            if (vowelSingersButtons != null)
            {
                for (int i = 0; i < vowelSingersButtons.Length; i++)
                {
                    int index = i;
                    vowelSingersButtons[i].onClick.AddListener(() => OnVowelSingerTapped(index));
                }
            }
        }

        public void StartActivity()
        {
            currentRoundIndex = 0;
            attemptCount = 0;
            isTransitioning = false;

            if (mouthDemoPanel != null) mouthDemoPanel.SetActive(true);
            if (nextToSortingButton != null) nextToSortingButton.gameObject.SetActive(false);
            if (sortingGamePanel != null) sortingGamePanel.SetActive(false);
            if (freePlayPanel != null) freePlayPanel.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);

            UpdateProgressUI(0);
            StartCoroutine(PlayMouthDemoSequence());
        }

        private IEnumerator PlayMouthDemoSequence()
        {
            if (nextToSortingButton != null) nextToSortingButton.gameObject.SetActive(false);

            SetDialogue("Welcome to Alphabet Town! Twenty-six letters live here — but in two different places.");
            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }

            SetDialogue("Open your mouth and sing with me… aaaaa. Nothing gets in the way! That is a vowel.");
            if (mouthCloseUpImage != null && activityData != null)
                mouthCloseUpImage.sprite = activityData.openVowelMouthSprite;
            if (activityData != null && activityData.vowelDemoVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.vowelDemoVoiceClip);
            }

            SetDialogue("Now say /b/. Feel it? Your lips shut! And /t/ — your tongue taps. Those are consonants.");
            if (mouthCloseUpImage != null && activityData != null)
                mouthCloseUpImage.sprite = activityData.lipsTogetherMouthSprite;
            if (activityData != null && activityData.consonantDemoVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.consonantDemoVoiceClip);
            }

            // Show Next to Sorting button ONLY after ALL mouth demo UI audio is completely finished!
            if (nextToSortingButton != null) nextToSortingButton.gameObject.SetActive(true);
        }

        private bool IsAudioPlaying()
        {
            return voiceAudioSource != null && voiceAudioSource.isPlaying;
        }

        private void StartSortingPhase()
        {
            if (isTransitioning || IsAudioPlaying()) return;
            if (mouthDemoPanel != null) mouthDemoPanel.SetActive(false);
            if (sortingGamePanel != null) sortingGamePanel.SetActive(true);
            LoadRound(0);
        }

        private void LoadRound(int roundIndex)
        {
            if (activityData == null || activityData.sortingLetters == null || roundIndex >= activityData.sortingLetters.Length)
            {
                StartFreePlayPhase();
                return;
            }

            currentRoundIndex = roundIndex;
            attemptCount = 0;
            if (momoHintObject != null) momoHintObject.SetActive(false);

            SortingLetterItem item = activityData.sortingLetters[roundIndex];
            if (currentLetterText != null) currentLetterText.text = item.letterChar.ToString();
            if (currentLetterImage != null && item.letterSprite != null) currentLetterImage.sprite = item.letterSprite;

            SetDialogue($"Hello! I am {item.letterChar}. Where do I live?");
            if (item.letterQuestionClip != null)
            {
                PlayVoiceClipNonBlocking(item.letterQuestionClip);
            }

            UpdateProgressUI((float)currentRoundIndex / totalRounds);
        }

        public void OnChoiceSelected(bool selectedVowel)
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null || currentRoundIndex >= activityData.sortingLetters.Length) return;

            SortingLetterItem item = activityData.sortingLetters[currentRoundIndex];
            bool isCorrect = (selectedVowel == item.isVowel);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectChoice(item, selectedVowel));
            }
            else
            {
                StartCoroutine(HandleWrongChoice(selectedVowel));
            }
        }

        private IEnumerator HandleCorrectChoice(SortingLetterItem item, bool isVowel)
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            PlaySFX(houseCheerSfx);

            RectTransform targetRect = isVowel ? vowelHousesRect : consonantStreetRect;
            if (targetRect != null) TriggerWiggle(targetRect);

            TriggerWiggleStarMeter();

            SetDialogue(item.successVoiceClip != null ? item.successVoiceClip.name : (isVowel ? $"Yes! {item.letterChar} is a vowel!" : $"Yes! {item.letterChar} is a consonant!"));
            if (item.successVoiceClip != null)
            {
                yield return PlayVoiceClip(item.successVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            currentRoundIndex++;
            isTransitioning = false;

            if (currentRoundIndex < totalRounds && currentRoundIndex < activityData.sortingLetters.Length)
            {
                LoadRound(currentRoundIndex);
            }
            else
            {
                StartFreePlayPhase();
            }
        }

        private IEnumerator HandleWrongChoice(bool selectedVowel)
        {
            attemptCount++;
            PlaySFX(retryGentleSfx);

            RectTransform targetRect = selectedVowel ? vowelHousesRect : consonantStreetRect;
            if (targetRect != null) TriggerWiggle(targetRect);

            if (attemptCount >= 2)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                SetDialogue("Try singing it. If it sings with an open mouth, it is a vowel!");
                if (activityData != null && activityData.momoHintVoiceClip != null)
                {
                    yield return PlayVoiceClip(activityData.momoHintVoiceClip);
                }
            }
            else
            {
                SetDialogue("Listen again. Does your mouth stay wide open, or do your lips/tongue touch?");
                yield return new WaitForSeconds(0.8f);
            }
        }

        private void StartFreePlayPhase()
        {
            if (sortingGamePanel != null) sortingGamePanel.SetActive(false);
            if (freePlayPanel != null) freePlayPanel.SetActive(true);

            UpdateProgressUI(1f);
            SetDialogue("Five singers, twenty-one helpers. Tap any vowel to hear them sing!");
            if (activityData != null && activityData.sungVowelsVoiceClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.sungVowelsVoiceClip);
            }

            if (freePlayWobbleCoroutine != null) StopCoroutine(freePlayWobbleCoroutine);
            freePlayWobbleCoroutine = StartCoroutine(ContinuousFreePlayWobbleCoroutine());

            StartCoroutine(HandleCompletionSequence());
        }

        private IEnumerator ContinuousFreePlayWobbleCoroutine()
        {
            float elapsed = 0f;
            while (freePlayPanel != null && freePlayPanel.activeSelf)
            {
                elapsed += Time.deltaTime;
                if (vowelSingersButtons != null)
                {
                    for (int i = 0; i < vowelSingersButtons.Length; i++)
                    {
                        if (vowelSingersButtons[i] != null)
                        {
                            RectTransform rt = vowelSingersButtons[i].GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                float offset = i * 0.5f;
                                float rotAngle = Mathf.Sin((elapsed * 3.5f) + offset) * 6.0f;
                                float scalePulse = 1.0f + (Mathf.Sin((elapsed * 4.5f) + offset) * 0.06f);
                                rt.localRotation = Quaternion.Euler(0f, 0f, rotAngle);
                                rt.localScale = new Vector3(scalePulse, scalePulse, 1f);
                            }
                        }
                    }
                }
                yield return null;
            }
        }

        private void OnVowelSingerTapped(int index)
        {
            if (isTransitioning || IsAudioPlaying()) return;
            string[] vowelSounds = { "aaa", "eee", "iii", "ooo", "uuu" };
            if (index >= 0 && index < vowelSounds.Length)
            {
                SetDialogue($"Vowel Singer: {vowelSounds[index]}!");
                PlaySFX(correctChimeSfx);
                if (vowelSingersButtons != null && index < vowelSingersButtons.Length)
                {
                    TriggerWiggle(vowelSingersButtons[index].GetComponent<RectTransform>());
                }
            }
        }

        private IEnumerator HandleCompletionSequence()
        {
            SetDialogue("Five singers, twenty-one helpers. Now let's meet every letter properly!");
            if (activityData != null && activityData.bridgeToStop2VoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.bridgeToStop2VoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            PlaySFX(starPopSfx);
            yield return new WaitForSeconds(0.5f);

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.SetActive(true);
            isTransitioning = false;
        }

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();
            DeactivateMascots();

            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
            }
            else if (unitContentPanel != null)
            {
                unitContentPanel.SetActive(true);
            }

            if (currentPanel != null) 
             {
                 currentPanel.SetActive(false);
                unitContentPanel.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        private void TriggerWiggle(RectTransform target)
        {
            if (target == null) return;
            StartCoroutine(WiggleRect(target, 0.4f, 12f));
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null)
            {
                StartCoroutine(WiggleRect(starMeterRect, 0.5f, 15f));
            }
        }

        private IEnumerator WiggleRect(RectTransform target, float duration, float angle)
        {
            Quaternion originalRot = target.localRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;
                float currentAngle = Mathf.Sin(percent * Mathf.PI * 8f) * angle * (1f - percent);
                target.localRotation = originalRot * Quaternion.Euler(0f, 0f, currentAngle);
                yield return null;
            }

            target.localRotation = originalRot;
        }

        private void ReplayCurrentAudio()
        {
            if (activityData != null && currentRoundIndex < activityData.sortingLetters.Length)
            {
                var item = activityData.sortingLetters[currentRoundIndex];
                if (item.letterQuestionClip != null) PlayVoiceClipNonBlocking(item.letterQuestionClip);
            }
        }

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) yield break;
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length + 0.1f);
        }

        private void PlayVoiceClipNonBlocking(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) return;
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxAudioSource == null) return;
            sfxAudioSource.PlayOneShot(clip);
        }

        private void SetDialogue(string msg)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, msg, dialogueCanvasGroup);
        }

        private void UpdateProgressUI(float fillPercent)
        {
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillPercent;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillPercent * 100)}%";
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        public void GoToPreviousPanel()
        {
            DeactivateMascots();
            if (currentPanel != null) currentPanel.SetActive(false);
            if (unitContentPanel != null) unitContentPanel.SetActive(true);
        }
    }
}
