using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit10
{
    public class ICanSeeController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit10";
        [SerializeField] private string topicName = "ICanSee";

        [Header("Data Asset")]
        [SerializeField] private ICanSeeData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Dialogue / Subtitle UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: 3-Stacked-Lines Fluency Panel")]
        [SerializeField] private GameObject sentenceSetsPanel;
        [SerializeField] private TMP_Text setTitleTMP;
        [SerializeField] private GameObject[] lineContainers = new GameObject[3];
        [SerializeField] private TMP_Text[] lineTexts = new TMP_Text[3];
        [SerializeField] private Image[] lineIllustrations = new Image[3];
        [SerializeField] private Button[] lineSpeakerButtons = new Button[3];
        [SerializeField] private Image smoothMeterFillImage;
        [SerializeField] private Button nextSetButton;
        [SerializeField] private Button rereadSetButton;

        [Header("Phase 2: Make Your Own Sentence Panel")]
        [SerializeField] private GameObject makeYourOwnPanel;
        [SerializeField] private TMP_Text makeYourOwnPromptTMP;
        [SerializeField] private Image makeYourOwnDropSlotImage;
        [SerializeField] private TMP_Text makeYourOwnSentenceTMP;
        [SerializeField] private Button[] choicePictureButtons = new Button[4];
        [SerializeField] private Image[] choicePictureImages = new Image[4];
        [SerializeField] private TMP_Text[] choicePictureTexts = new TMP_Text[4];
        [SerializeField] private Button readMySentenceButton;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;

        [Header("Rewards & Navigation")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentSetIndex = 0;
        private int currentLineInSet = 0;
        private bool isRereadPass = false;
        private int makeYourOwnRound = 0;
        private bool isTransitioning = false;
        private bool hasInitialized = false;
        private float currentSmoothAmount = 0f;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
        }

        private void Start()
        {
            SetupButtonListeners();
            hasInitialized = true;
            StartActivity();
        }

        private void OnEnable()
        {
            if (hasInitialized)
            {
                StartActivity();
            }
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

        private void EnsureDataAssigned()
        {
            if (activityData == null)
            {
                activityData = Resources.Load<ICanSeeData>("Phonics2/Unit10/ICanSeeData_Unit10");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<ICanSeeData>();
                activityData.PopulateDefaultData();
            }
        }

        private void SetupButtonListeners()
        {
            for (int i = 0; i < lineSpeakerButtons.Length; i++)
            {
                int index = i;
                if (lineSpeakerButtons[i] != null)
                {
                    lineSpeakerButtons[i].onClick.RemoveAllListeners();
                    lineSpeakerButtons[i].onClick.AddListener(() => OnLineSpeakerClicked(index));
                }
            }

            if (nextSetButton != null)
            {
                nextSetButton.onClick.RemoveAllListeners();
                nextSetButton.onClick.AddListener(OnNextSetClicked);
            }

            if (rereadSetButton != null)
            {
                rereadSetButton.onClick.RemoveAllListeners();
                rereadSetButton.onClick.AddListener(OnRereadSetClicked);
            }

            for (int i = 0; i < choicePictureButtons.Length; i++)
            {
                int index = i;
                if (choicePictureButtons[i] != null)
                {
                    choicePictureButtons[i].onClick.RemoveAllListeners();
                    choicePictureButtons[i].onClick.AddListener(() => OnChoicePictureClicked(index));
                }
            }

            if (readMySentenceButton != null)
            {
                readMySentenceButton.onClick.RemoveAllListeners();
                readMySentenceButton.onClick.AddListener(OnReadMySentenceClicked);
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
        }

        public void ResetLevel()
        {
            StartActivity();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            currentSetIndex = 0;
            currentLineInSet = 0;
            isRereadPass = false;
            makeYourOwnRound = 0;
            currentSmoothAmount = 0.1f;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (nextSetButton != null) nextSetButton.gameObject.SetActive(false);
            if (rereadSetButton != null) rereadSetButton.gameObject.SetActive(false);
            if (makeYourOwnPanel != null) makeYourOwnPanel.SetActive(false);

            if (sentenceSetsPanel != null) sentenceSetsPanel.SetActive(true);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);

            StartCoroutine(RunActivityIntroRoutine());
        }

        private IEnumerator RunActivityIntroRoutine()
        {
            isTransitioning = true;
            UpdateSmoothMeter(0.15f);

            SetDialogue("Today we read lots of lines — and we read them SMOOTHLY.");
            if (activityData != null && activityData.openingIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.openingIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(1.8f);
            }

            isTransitioning = false;
            LoadSentenceSet(0);
        }

        private void LoadSentenceSet(int setIndex)
        {
            if (activityData == null || activityData.sentenceSets == null || setIndex >= activityData.sentenceSets.Length)
            {
                StartMakeYourOwnPhase();
                return;
            }

            currentSetIndex = setIndex;
            currentLineInSet = 0;
            var currentSet = activityData.sentenceSets[setIndex];

            if (setTitleTMP != null) setTitleTMP.text = currentSet.setTitle;
            if (nextSetButton != null) nextSetButton.gameObject.SetActive(false);
            if (rereadSetButton != null) rereadSetButton.gameObject.SetActive(false);

            for (int i = 0; i < lineContainers.Length; i++)
            {
                if (lineContainers[i] != null)
                {
                    bool hasLine = currentSet.lines != null && i < currentSet.lines.Length;
                    lineContainers[i].SetActive(hasLine);

                    if (hasLine)
                    {
                        var line = currentSet.lines[i];
                        if (lineTexts != null && i < lineTexts.Length && lineTexts[i] != null)
                        {
                            lineTexts[i].text = line.lineText;
                        }
                        if (lineIllustrations != null && i < lineIllustrations.Length && lineIllustrations[i] != null)
                        {
                            bool hasSprite = line.lineIllustration != null;
                            lineIllustrations[i].gameObject.SetActive(hasSprite);
                            if (hasSprite) lineIllustrations[i].sprite = line.lineIllustration;
                        }
                    }
                }
            }

            SetDialogue($"Set {setIndex + 1}: Read each line smoothly!");
            UpdateProgressUI((float)setIndex / 6f);
        }

        private void OnLineSpeakerClicked(int lineIndex)
        {
            if (activityData == null || currentSetIndex >= activityData.sentenceSets.Length) return;

            var set = activityData.sentenceSets[currentSetIndex];
            if (set.lines != null && lineIndex < set.lines.Length)
            {
                var line = set.lines[lineIndex];
                SetDialogue(line.lineText);

                if (line.lineAudioClip != null)
                {
                    PlayVoiceClipNonBlocking(line.lineAudioClip);
                }

                currentSmoothAmount = Mathf.Clamp01(currentSmoothAmount + 0.08f);
                UpdateSmoothMeter(currentSmoothAmount);
                PlaySFX(activityData != null ? activityData.wordTapSfx : null);

                if (lineIndex == 2 && !isRereadPass)
                {
                    StartCoroutine(HandleSetFinishedRoutine());
                }
            }
        }

        private IEnumerator HandleSetFinishedRoutine()
        {
            yield return new WaitForSeconds(1.0f);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (currentSetIndex == 0)
            {
                SetDialogue("\"I can see\" — you did not even have to sound that out, did you? You just READ it!");
                if (activityData != null && activityData.fluencyPraiseClip != null)
                {
                    yield return PlayVoiceClip(activityData.fluencyPraiseClip);
                }
            }

            if (rereadSetButton != null) rereadSetButton.gameObject.SetActive(true);
            if (nextSetButton != null) nextSetButton.gameObject.SetActive(true);
        }

        private void OnRereadSetClicked()
        {
            isRereadPass = true;
            if (rereadSetButton != null) rereadSetButton.gameObject.SetActive(false);
            SetDialogue("Let us read that set again. See if it feels easier this time.");
            if (activityData != null && activityData.rereadInvitationClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.rereadInvitationClip);
            }
        }

        private void OnNextSetClicked()
        {
            if (isTransitioning) return;
            isRereadPass = false;
            currentSetIndex++;
            LoadSentenceSet(currentSetIndex);
        }

        #region Phase 2: Make Your Own Sentence

        private void StartMakeYourOwnPhase()
        {
            if (sentenceSetsPanel != null) sentenceSetsPanel.SetActive(false);
            if (makeYourOwnPanel != null) makeYourOwnPanel.SetActive(true);

            makeYourOwnRound = 0;
            LoadMakeYourOwnRound(0);
        }

        private void LoadMakeYourOwnRound(int round)
        {
            makeYourOwnRound = round;
            if (makeYourOwnSentenceTMP != null) makeYourOwnSentenceTMP.text = "I can see a ____";
            if (readMySentenceButton != null) readMySentenceButton.gameObject.SetActive(false);

            if (activityData != null && activityData.makeYourOwnChoices != null)
            {
                for (int i = 0; i < choicePictureButtons.Length; i++)
                {
                    if (choicePictureButtons[i] != null)
                    {
                        bool active = i < activityData.makeYourOwnChoices.Length;
                        choicePictureButtons[i].gameObject.SetActive(active);

                        if (active)
                        {
                            var item = activityData.makeYourOwnChoices[i];
                            if (choicePictureTexts != null && i < choicePictureTexts.Length && choicePictureTexts[i] != null)
                            {
                                choicePictureTexts[i].text = item.word;
                            }
                            if (choicePictureImages != null && i < choicePictureImages.Length && choicePictureImages[i] != null && item.pictureSprite != null)
                            {
                                choicePictureImages[i].sprite = item.pictureSprite;
                            }
                        }
                    }
                }
            }

            SetDialogue("Now make your own. Pick a picture and read your sentence out loud!");
            if (round == 0 && activityData != null && activityData.makeYourOwnIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.makeYourOwnIntroClip);
            }

            UpdateProgressUI(0.66f + ((float)round / 2f) * 0.34f);
        }

        private void OnChoicePictureClicked(int index)
        {
            if (activityData == null || activityData.makeYourOwnChoices == null || index >= activityData.makeYourOwnChoices.Length) return;

            var chosen = activityData.makeYourOwnChoices[index];
            if (makeYourOwnSentenceTMP != null)
            {
                makeYourOwnSentenceTMP.text = $"I can see a <color=#2E7D32><b>{chosen.word}</b></color>.";
            }

            if (makeYourOwnDropSlotImage != null && chosen.pictureSprite != null)
            {
                makeYourOwnDropSlotImage.sprite = chosen.pictureSprite;
                makeYourOwnDropSlotImage.gameObject.SetActive(true);
            }

            PlaySFX(activityData != null ? activityData.wordTapSfx : null);
            SetDialogue($"I can see a {chosen.word}. Tap to read it aloud!");

            if (readMySentenceButton != null) readMySentenceButton.gameObject.SetActive(true);
        }

        private void OnReadMySentenceClicked()
        {
            StartCoroutine(RunReadMySentenceRoutine());
        }

        private IEnumerator RunReadMySentenceRoutine()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            SetDialogue("You wrote that sentence yourself — and then you read it!");
            if (activityData != null && activityData.makeYourOwnSuccessClip != null)
            {
                yield return PlayVoiceClip(activityData.makeYourOwnSuccessClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            if (makeYourOwnRound < 1)
            {
                isTransitioning = false;
                LoadMakeYourOwnRound(makeYourOwnRound + 1);
            }
            else
            {
                CompleteStop1();
            }
        }

        #endregion

        private void CompleteStop1()
        {
            StartCoroutine(CompleteStop1Sequence());
        }

        private IEnumerator CompleteStop1Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.setCompleteFanfareSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("Fluent and smooth! You are reading like a true reader!");
            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            yield return new WaitForSeconds(1.0f);
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
        }

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();
            DeactivateMascots();

            if (nextPanel != null) nextPanel.SetActive(true);
            else if (unitContentPanel != null) unitContentPanel.SetActive(true);

            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
                if (unitContentPanel != null) unitContentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }

        private void SetDialogue(string message)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, message, dialogueCanvasGroup);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (sfxAudioSource != null && clip != null)
            {
                sfxAudioSource.PlayOneShot(clip);
            }
        }

        private void PlayVoiceClipNonBlocking(AudioClip clip)
        {
            if (voiceAudioSource != null && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
            }
        }

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (voiceAudioSource != null && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }

        private void UpdateProgressUI(float fillAmount)
        {
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillAmount;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillAmount * 100)}%";
        }

        private void UpdateSmoothMeter(float fillAmount)
        {
            if (smoothMeterFillImage != null)
            {
                smoothMeterFillImage.fillAmount = fillAmount;
            }
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null) StartCoroutine(PopScaleRoutine(starMeterRect, 1.15f, 0.3f));
        }

        private IEnumerator PopScaleRoutine(Transform target, float maxScaleMul, float duration)
        {
            if (target == null) yield break;
            Vector3 startScale = target.localScale;
            Vector3 maxScale = startScale * maxScaleMul;
            float half = duration * 0.5f;

            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(startScale, maxScale, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(maxScale, startScale, elapsed / half);
                yield return null;
            }

            target.localScale = startScale;
        }
    }
}
