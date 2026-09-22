using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit3
{
    public class TwoVoicesController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit3";
        [SerializeField] private string topicName = "TwoVoices";

        [Header("Data Reference")]
        [SerializeField] private TwoVoicesData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Phase 1: Interactive Toy & Switch UI")]
        [SerializeField] private GameObject toyPanel;
        [SerializeField] private Button[] vowelSelectButtons; // A, E, I, O, U
        [SerializeField] private Image currentVowelCharacterImage;
        [SerializeField] private Image currentPictureImage;
        [SerializeField] private Toggle voiceSwitchToggle; // Breve (Short) vs Macron (Long)
        [SerializeField] private Button switchBreveButton;
        [SerializeField] private Button switchMacronButton;
        [SerializeField] private Button startQuizButton;

        [Header("Phase 2 & 3: Quiz & Mark It UI")]
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private Image quizPictureImage;
        [SerializeField] private TMP_Text quizWordDisplayTMP;
        [SerializeField] private Button nameChoiceButton;   // Macron / Name / Long
        [SerializeField] private Button soundChoiceButton;  // Breve / Sound / Short
        [SerializeField] private RectTransform nameChoiceRect;
        [SerializeField] private RectTransform soundChoiceRect;
        [SerializeField] private Button applyBreveMarkButton;
        [SerializeField] private Button applyMacronMarkButton;

        [Header("Feedback Colors")]
        [SerializeField] private Color correctColor = new Color(0.3f, 0.69f, 0.31f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.96f, 0.26f, 0.21f, 1f);

        [Header("Progress Ring UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;

        [Header("Mascots")]
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip starPopSfx;
        [SerializeField] private AudioClip switchFlipSfx;

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

        [Tooltip("The current panel to hide when Continue is clicked.")]
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int selectedVowelIndex = 0;
        private bool isMacronLongSelected = false;
        private int currentQuizIndex = 0;
        private int totalWhichVoiceRounds = 8;
        private int totalMarkItRounds = 4;
        private bool isMarkItPhase = false;
        private bool isTransitioning = false;
        private WhichVoiceItem currentQuizItem;
        private int failAttempts = 0;

        public string UnitID => unitID;
        public string TopicName => topicName;

        private Dictionary<Button, Color> defaultButtonColors = new Dictionary<Button, Color>();

        private void Awake()
        {
            EnsureAudioSources();
            SetupButtonListeners();
            CacheDefaultButtonColors();
        }

        private void CacheDefaultButtonColors()
        {
            CacheColor(nameChoiceButton);
            CacheColor(soundChoiceButton);
            CacheColor(applyBreveMarkButton);
            CacheColor(applyMacronMarkButton);
        }

        private void CacheColor(Button btn)
        {
            if (btn != null)
            {
                Image img = btn.GetComponent<Image>();
                if (img != null && !defaultButtonColors.ContainsKey(btn))
                {
                    defaultButtonColors[btn] = img.color;
                }
            }
        }

        private void ResetButtonColors()
        {
            ResetSingleButtonColor(nameChoiceButton);
            ResetSingleButtonColor(soundChoiceButton);
            ResetSingleButtonColor(applyBreveMarkButton);
            ResetSingleButtonColor(applyMacronMarkButton);
        }

        private void ResetSingleButtonColor(Button btn)
        {
            if (btn != null)
            {
                Image img = btn.GetComponent<Image>();
                if (img != null)
                {
                    if (defaultButtonColors.TryGetValue(btn, out Color origColor))
                    {
                        img.color = origColor;
                    }
                    else
                    {
                        img.color = Color.white;
                    }
                }
            }
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
            if (switchBreveButton != null) switchBreveButton.onClick.AddListener(() => SetToySwitchState(false));
            if (switchMacronButton != null) switchMacronButton.onClick.AddListener(() => SetToySwitchState(true));
            if (startQuizButton != null) startQuizButton.onClick.AddListener(StartQuizPhase);

            if (vowelSelectButtons != null)
            {
                for (int i = 0; i < vowelSelectButtons.Length; i++)
                {
                    int index = i;
                    if (vowelSelectButtons[i] != null)
                        vowelSelectButtons[i].onClick.AddListener(() => SelectVowelToy(index));
                }
            }

            if (nameChoiceButton != null) nameChoiceButton.onClick.AddListener(() => OnQuizChoiceSelected(true));
            if (soundChoiceButton != null) soundChoiceButton.onClick.AddListener(() => OnQuizChoiceSelected(false));

            if (applyBreveMarkButton != null) applyBreveMarkButton.onClick.AddListener(() => OnMarkChoiceSelected(false));
            if (applyMacronMarkButton != null) applyMacronMarkButton.onClick.AddListener(() => OnMarkChoiceSelected(true));

            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }
        }

        public void StartActivity()
        {
            selectedVowelIndex = 0;
            isMacronLongSelected = false;
            currentQuizIndex = 0;
            isMarkItPhase = false;
            isTransitioning = false;
            failAttempts = 0;

            if (toyPanel != null) toyPanel.SetActive(true);
            if (quizPanel != null) quizPanel.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);

            UpdateProgressUI(0f);
            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Every singer here has TWO voices. Watch this switch!");

            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            SetDialogue("Long vowels say their NAME. Short vowels say their SOUND.");
            if (activityData != null && activityData.ruleExplanationClip != null)
            {
                yield return PlayVoiceClip(activityData.ruleExplanationClip);
            }

            SelectVowelToy(0);
            if (startQuizButton != null) startQuizButton.gameObject.SetActive(true);
            isTransitioning = false;
        }

        private void SelectVowelToy(int index)
        {
            if (activityData == null || activityData.vowelToyItems == null || index < 0 || index >= activityData.vowelToyItems.Length) return;

            selectedVowelIndex = index;
            UpdateToyVisualsAndAudio();
        }

        private void SetToySwitchState(bool isLongMacron)
        {
            isMacronLongSelected = isLongMacron;
            PlaySFX(switchFlipSfx != null ? switchFlipSfx : correctChimeSfx);
            UpdateToyVisualsAndAudio();
        }

        private void UpdateToyVisualsAndAudio()
        {
            if (activityData == null || selectedVowelIndex >= activityData.vowelToyItems.Length) return;

            VowelVoiceToyItem item = activityData.vowelToyItems[selectedVowelIndex];

            if (currentVowelCharacterImage != null)
                currentVowelCharacterImage.sprite = isMacronLongSelected ? item.vowelLongSprite : item.vowelShortSprite;

            if (currentPictureImage != null)
                currentPictureImage.sprite = isMacronLongSelected ? item.longPictureSprite : item.shortPictureSprite;

            AudioClip clipToPlay = isMacronLongSelected ? item.nameVoiceClip : item.soundVoiceClip;
            string voiceType = isMacronLongSelected ? "NAME (Long ¯)" : "SOUND (Short ˘)";
            SetDialogue($"{item.vowelLetter} in {voiceType} voice!");

            if (clipToPlay != null)
            {
                PlayVoiceClipNonBlocking(clipToPlay);
            }
        }

        private void StartQuizPhase()
        {
            if (isTransitioning || IsAudioPlaying()) return;

            if (toyPanel != null) toyPanel.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(true);

            SetDialogue("Listen to each word. Did the vowel say its NAME, or its SOUND?");
            if (activityData != null && activityData.quizInstructionClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.quizInstructionClip);
            }

            LoadQuizRound(0);
        }

        private void LoadQuizRound(int index)
        {
            if (activityData == null || activityData.quizItems == null || index >= activityData.quizItems.Length)
            {
                StartMarkItPhase();
                return;
            }

            ResetButtonColors();
            currentQuizIndex = index;
            failAttempts = 0;
            if (momoHintObject != null) momoHintObject.SetActive(false);

            currentQuizItem = activityData.quizItems[index];

            if (quizPictureImage != null && currentQuizItem.pictureSprite != null)
                quizPictureImage.sprite = currentQuizItem.pictureSprite;

            if (quizWordDisplayTMP != null) quizWordDisplayTMP.text = currentQuizItem.wordName;

            if (applyBreveMarkButton != null) applyBreveMarkButton.gameObject.SetActive(false);
            if (applyMacronMarkButton != null) applyMacronMarkButton.gameObject.SetActive(false);
            if (nameChoiceButton != null) nameChoiceButton.gameObject.SetActive(true);
            if (soundChoiceButton != null) soundChoiceButton.gameObject.SetActive(true);

            SetDialogue($"Listen: '{currentQuizItem.wordName}'. Did the vowel say its NAME or its SOUND?");
            if (currentQuizItem.wordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(currentQuizItem.wordAudioClip);
            }

            UpdateProgressUI((float)index / 12f);
        }

        private void OnQuizChoiceSelected(bool selectedLongName)
        {
            if (isTransitioning || IsAudioPlaying() || currentQuizItem == null) return;

            bool isCorrect = (selectedLongName == currentQuizItem.isLongName);
            Button chosenBtn = selectedLongName ? nameChoiceButton : soundChoiceButton;

            if (chosenBtn != null)
            {
                TriggerWiggle(chosenBtn.GetComponent<RectTransform>());
                Image btnImg = chosenBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isCorrect ? correctColor : wrongColor;
                }
            }

            if (isCorrect)
            {
                StartCoroutine(HandleQuizCorrect());
            }
            else
            {
                failAttempts++;
                if (failAttempts >= 2 && momoHintObject != null)
                {
                    momoHintObject.SetActive(true);
                    SetDialogue("Momo says: The curvy mark (˘) means short sound! The straight mark (¯) means long name!");
                    if (activityData != null && activityData.momoMarkingHintClip != null)
                    {
                        PlayVoiceClipNonBlocking(activityData.momoMarkingHintClip);
                    }
                }
                StartCoroutine(HandleQuizWrong(chosenBtn));
            }
        }

        private IEnumerator HandleQuizCorrect()
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            string voiceTypeStr = currentQuizItem.isLongName ? "NAME! That is a long vowel!" : "SOUND! That is a short vowel!";
            SetDialogue($"Yes! In '{currentQuizItem.wordName}', the vowel said its {voiceTypeStr}");

            if (currentQuizItem.explanationClip != null)
            {
                yield return PlayVoiceClip(currentQuizItem.explanationClip);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            currentQuizIndex++;
            isTransitioning = false;

            if (currentQuizIndex < totalWhichVoiceRounds && currentQuizIndex < activityData.quizItems.Length)
            {
                LoadQuizRound(currentQuizIndex);
            }
            else
            {
                StartMarkItPhase();
            }
        }

        private IEnumerator HandleQuizWrong(Button chosenBtn = null)
        {
            PlaySFX(retryGentleSfx);
            SetDialogue("Listen once more… Can you hear the difference? Try again!");
            yield return new WaitForSeconds(0.6f);
            if (chosenBtn != null)
            {
                ResetSingleButtonColor(chosenBtn);
            }
        }

        private void StartMarkItPhase()
        {
            isMarkItPhase = true;
            currentQuizIndex = 0;

            SetDialogue("Mark it! Put the curved breve (˘) for short sound or straight macron (¯) for long name!");
            LoadMarkItRound(0);
        }

        private void LoadMarkItRound(int index)
        {
            if (activityData == null || activityData.markItems == null || index >= activityData.markItems.Length)
            {
                StartCoroutine(CompleteStopSequence());
                return;
            }

            ResetButtonColors();
            currentQuizIndex = index;
            failAttempts = 0;
            currentQuizItem = activityData.markItems[index];

            if (quizPictureImage != null && currentQuizItem.pictureSprite != null)
                quizPictureImage.sprite = currentQuizItem.pictureSprite;

            if (quizWordDisplayTMP != null) quizWordDisplayTMP.text = currentQuizItem.wordName;

            if (nameChoiceButton != null) nameChoiceButton.gameObject.SetActive(false);
            if (soundChoiceButton != null) soundChoiceButton.gameObject.SetActive(false);
            if (applyBreveMarkButton != null) applyBreveMarkButton.gameObject.SetActive(true);
            if (applyMacronMarkButton != null) applyMacronMarkButton.gameObject.SetActive(true);

            SetDialogue($"Tap the correct mark for '{currentQuizItem.wordName}'!");
            UpdateProgressUI((8f + index) / 12f);
        }

        private void OnMarkChoiceSelected(bool selectedMacron)
        {
            if (isTransitioning || IsAudioPlaying() || currentQuizItem == null) return;

            bool isCorrect = (selectedMacron == currentQuizItem.isLongName);
            Button chosenBtn = selectedMacron ? applyMacronMarkButton : applyBreveMarkButton;

            if (chosenBtn != null)
            {
                TriggerWiggle(chosenBtn.GetComponent<RectTransform>());
                Image btnImg = chosenBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isCorrect ? correctColor : wrongColor;
                }
            }

            if (isCorrect)
            {
                StartCoroutine(HandleMarkCorrect(selectedMacron));
            }
            else
            {
                StartCoroutine(HandleQuizWrong(chosenBtn));
            }
        }

        private IEnumerator HandleMarkCorrect(bool isMacron)
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            string markName = isMacron ? "macron (¯) for long name!" : "breve (˘) for short sound!";
            SetDialogue($"Great mark! You placed the {markName}");

            yield return new WaitForSeconds(0.8f);

            currentQuizIndex++;
            isTransitioning = false;

            if (currentQuizIndex < totalMarkItRounds && currentQuizIndex < activityData.markItems.Length)
            {
                LoadMarkItRound(currentQuizIndex);
            }
            else
            {
                StartCoroutine(CompleteStopSequence());
            }
        }

        private IEnumerator CompleteStopSequence()
        {
            if (quizPanel != null) quizPanel.SetActive(false);
            UpdateProgressUI(1f);

            SetDialogue("You can tell short and long vowels apart! Fantastic!");
            if (activityData != null && activityData.closingVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.closingVoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            PlaySFX(starPopSfx);
            yield return new WaitForSeconds(0.5f);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.SetActive(true);
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
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

            if (currentPanel != null)  {
                 currentPanel.SetActive(false);
                unitContentPanel.SetActive(false);
            }
            else gameObject.SetActive(false);

            TopicProgressUI.RefreshAllTicks();
        }

        public void GoToPreviousPanel()
        {
            DeactivateMascots();
            if (currentPanel != null) currentPanel.SetActive(false);
            if (unitContentPanel != null) unitContentPanel.SetActive(true);
        }

        private void TriggerWiggle(RectTransform target)
        {
            if (target == null) return;
            StartCoroutine(WiggleRect(target, 0.35f, 10f));
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null)
            {
                StartCoroutine(WiggleRect(starMeterRect, 0.45f, 12f));
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

        private bool IsAudioPlaying()
        {
            return voiceAudioSource != null && voiceAudioSource.isPlaying;
        }

        private void SetDialogue(string msg)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, msg, dialogueCanvasGroup);
        }

        private void UpdateProgressUI(float fillPercent)
        {
            fillPercent = Mathf.Clamp01(fillPercent);
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillPercent;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillPercent * 100)}%";
        }

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) yield break;
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length + 0.1f);
        }

        private void PlayVoiceClipNonBlocking(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) return;
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxAudioSource == null) return;
            sfxAudioSource.PlayOneShot(clip);
        }
    }
}
