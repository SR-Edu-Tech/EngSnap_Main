using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit2
{
    public class FirstSoundMatchController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit2";
        [SerializeField] private string topicName = "FirstSoundMatch";

        [Header("Data Reference")]
        [SerializeField] private FirstSoundMatchData matchData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Center Picture UI")]
        [SerializeField] private Image targetPictureImage;
        [SerializeField] private TMP_Text targetWordText;
        [SerializeField] private Button playWordButton;
        [SerializeField] private RectTransform pictureRectTransform;

        [Header("Edge Letter Choice Buttons (4 Options)")]
        [SerializeField] private Button[] letterChoiceButtons; // 4 buttons placed around edges
        [SerializeField] private TMP_Text[] letterChoiceTexts;
        [SerializeField] private RectTransform[] letterChoiceRects;

        [Header("Progress Ring UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
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
        [SerializeField] private AudioClip dragDropSfx;

        [Header("Button Feedback Colors")]
        [SerializeField] private Color correctColor = new Color(0.3f, 0.85f, 0.39f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.95f, 0.26f, 0.21f, 1f);

        private int currentRoundIndex = 0;
        private int totalRounds = 12;
        private int attemptCount = 0;
        private bool isTransitioning = false;
        private FirstSoundMatchItem currentItem;
        private char[] activeChoiceLetters = new char[4];
        private Coroutine hintPulseCoroutine;

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
            if (playWordButton != null) playWordButton.onClick.AddListener(ReplayWordAudio);
            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }

            for (int i = 0; i < letterChoiceButtons.Length; i++)
            {
                int index = i;
                if (letterChoiceButtons[i] != null)
                {
                    letterChoiceButtons[i].onClick.AddListener(() => OnLetterChoiceTapped(index));
                }
            }
        }

        public void StartActivity()
        {
            currentRoundIndex = 0;
            attemptCount = 0;
            isTransitioning = false;

            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);
            UpdateProgressUI(0f);

            StartCoroutine(StartIntroSequence());
        }

        private IEnumerator StartIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Listen to the word. Which letter does it START with?");

            if (matchData != null && matchData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(matchData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
            LoadRound(0);
        }

        private void LoadRound(int roundIndex)
        {
            if (matchData == null || matchData.matchItems == null || roundIndex >= matchData.matchItems.Length)
            {
                StartCoroutine(CompleteStopSequence());
                return;
            }

            currentRoundIndex = roundIndex;
            attemptCount = 0;
            if (momoHintObject != null) momoHintObject.SetActive(false);

            currentItem = matchData.matchItems[roundIndex];

            if (targetPictureImage != null && currentItem.pictureSprite != null)
                targetPictureImage.sprite = currentItem.pictureSprite;
            if (targetWordText != null)
                targetWordText.text = currentItem.wordName;

            SetDialogue($"Word: {currentItem.stretchedWordText}");
            if (currentItem.stretchedAudioClip != null)
            {
                PlayVoiceClipNonBlocking(currentItem.stretchedAudioClip);
            }

            SetupChoices(currentItem);
            UpdateProgressUI((float)currentRoundIndex / totalRounds);
        }

        private void StopHintPulse()
        {
            if (hintPulseCoroutine != null)
            {
                StopCoroutine(hintPulseCoroutine);
                hintPulseCoroutine = null;
            }
            if (letterChoiceRects != null)
            {
                for (int i = 0; i < letterChoiceRects.Length; i++)
                {
                    if (letterChoiceRects[i] != null)
                        letterChoiceRects[i].localScale = Vector3.one;
                }
            }
        }

        private void SetupChoices(FirstSoundMatchItem item)
        {
            StopHintPulse();
            List<char> choices = new List<char> { item.correctFirstLetter };

            if (item.distractorLetters != null)
            {
                foreach (char d in item.distractorLetters)
                {
                    if (!choices.Contains(d) && choices.Count < 4)
                        choices.Add(d);
                }
            }

            // Fill remaining if needed
            char fillChar = 'A';
            while (choices.Count < 4)
            {
                if (!choices.Contains(fillChar) && fillChar != item.correctFirstLetter)
                    choices.Add(fillChar);
                fillChar++;
            }

            // Shuffle
            for (int i = 0; i < choices.Count; i++)
            {
                char temp = choices[i];
                int r = Random.Range(i, choices.Count);
                choices[i] = choices[r];
                choices[r] = temp;
            }

            for (int i = 0; i < letterChoiceButtons.Length; i++)
            {
                if (i < choices.Count)
                {
                    activeChoiceLetters[i] = choices[i];
                    letterChoiceButtons[i].gameObject.SetActive(true);
                    if (letterChoiceTexts[i] != null)
                        letterChoiceTexts[i].text = choices[i].ToString();

                    Image btnImg = letterChoiceButtons[i].GetComponent<Image>();
                    if (btnImg != null) btnImg.color = Color.white;
                    letterChoiceButtons[i].transform.localScale = Vector3.one;
                }
            }
        }

        private bool IsAudioPlaying()
        {
            return voiceAudioSource != null && voiceAudioSource.isPlaying;
        }

        private void OnLetterChoiceTapped(int choiceIndex)
        {
            if (isTransitioning || IsAudioPlaying() || currentItem == null || choiceIndex >= activeChoiceLetters.Length) return;

            char selectedLetter = activeChoiceLetters[choiceIndex];
            bool isCorrect = (selectedLetter == currentItem.correctFirstLetter);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectMatch(choiceIndex, selectedLetter));
            }
            else
            {
                StartCoroutine(HandleWrongMatch(choiceIndex, selectedLetter));
            }
        }

        private IEnumerator HandleCorrectMatch(int choiceIndex, char letter)
        {
            isTransitioning = true;
            StopHintPulse();
            PlaySFX(correctChimeSfx);

            if (choiceIndex < letterChoiceButtons.Length && letterChoiceButtons[choiceIndex] != null)
            {
                Image btnImg = letterChoiceButtons[choiceIndex].GetComponent<Image>();
                if (btnImg != null) btnImg.color = correctColor;
            }

            if (letterChoiceRects != null && choiceIndex < letterChoiceRects.Length && letterChoiceRects[choiceIndex] != null)
            {
                TriggerWiggle(letterChoiceRects[choiceIndex]);
            }
            TriggerWiggleStarMeter();

            SetDialogue($"Yes! {currentItem.wordName.Substring(0, 1).ToUpper() + currentItem.wordName.Substring(1)} starts with /{char.ToLower(letter)}/. {letter}!");

            if (currentItem.successVoiceClip != null)
            {
                yield return PlayVoiceClip(currentItem.successVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            currentRoundIndex++;
            isTransitioning = false;

            if (currentRoundIndex < totalRounds && currentRoundIndex < matchData.matchItems.Length)
            {
                LoadRound(currentRoundIndex);
            }
            else
            {
                StartCoroutine(CompleteStopSequence());
            }
        }

        private IEnumerator HandleWrongMatch(int choiceIndex, char selectedLetter)
        {
            attemptCount++;
            PlaySFX(retryGentleSfx);

            Image btnImg = null;
            if (choiceIndex < letterChoiceButtons.Length && letterChoiceButtons[choiceIndex] != null)
            {
                btnImg = letterChoiceButtons[choiceIndex].GetComponent<Image>();
                if (btnImg != null) btnImg.color = wrongColor;
            }

            if (letterChoiceRects != null && choiceIndex < letterChoiceRects.Length && letterChoiceRects[choiceIndex] != null)
            {
                TriggerWiggle(letterChoiceRects[choiceIndex]);
            }

            int letterIndex = char.ToUpper(selectedLetter) - 'A';
            if (matchData != null && matchData.letterWrongTapVoiceClips != null &&
                letterIndex >= 0 && letterIndex < matchData.letterWrongTapVoiceClips.Length &&
                matchData.letterWrongTapVoiceClips[letterIndex] != null)
            {
                SetDialogue($"I say /{char.ToLower(selectedLetter)}/. Listen again: {currentItem.stretchedWordText}.");
                yield return PlayVoiceClip(matchData.letterWrongTapVoiceClips[letterIndex]);
            }
            else
            {
                SetDialogue($"I say /{char.ToLower(selectedLetter)}/. Listen again: {currentItem.stretchedWordText}.");
                if (currentItem.stretchedAudioClip != null)
                {
                    yield return PlayVoiceClip(currentItem.stretchedAudioClip);
                }
            }

            // Reset red indicator back to white after voice feedback completes
            if (btnImg != null)
            {
                btnImg.color = Color.white;
            }

            if (attemptCount >= 2)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                PopUpCorrectAnswer();
            }
        }

        private void PopUpCorrectAnswer()
        {
            StopHintPulse();
            for (int i = 0; i < activeChoiceLetters.Length; i++)
            {
                if (activeChoiceLetters[i] == currentItem.correctFirstLetter)
                {
                    if (i < letterChoiceRects.Length && letterChoiceRects[i] != null)
                    {
                        hintPulseCoroutine = StartCoroutine(LoopPulseScaleRect(letterChoiceRects[i], 1.25f, 0.8f));
                    }
                    break;
                }
            }
        }

        private IEnumerator LoopPulseScaleRect(RectTransform target, float maxScale, float cycleDuration)
        {
            if (target == null) yield break;
            Vector3 originalScale = Vector3.one;
            float halfCycle = cycleDuration / 2f;

            while (true)
            {
                // Scale up
                float elapsed = 0f;
                while (elapsed < halfCycle)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / halfCycle;
                    target.localScale = Vector3.Lerp(originalScale, originalScale * maxScale, t);
                    yield return null;
                }

                // Scale down back to normal
                elapsed = 0f;
                while (elapsed < halfCycle)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / halfCycle;
                    target.localScale = Vector3.Lerp(originalScale * maxScale, originalScale, t);
                    yield return null;
                }

                yield return new WaitForSeconds(0.05f);
            }
        }

        private IEnumerator CompleteStopSequence()
        {
            SetDialogue("You are matching sounds to letters. That is real reading!");
            if (matchData != null && matchData.closingVoiceClip != null)
            {
                yield return PlayVoiceClip(matchData.closingVoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            PlaySFX(starPopSfx);
            UpdateProgressUI(1f);
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

        private void ReplayWordAudio()
        {
            if (isTransitioning || IsAudioPlaying()) return;
            if (currentItem != null && currentItem.stretchedAudioClip != null)
            {
                PlayVoiceClipNonBlocking(currentItem.stretchedAudioClip);
            }
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
