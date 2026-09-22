using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit10
{
    public class EllFamilyController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit10";
        [SerializeField] private string topicName = "TheEllFamily";

        [Header("Data Asset")]
        [SerializeField] private EllFamilyData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Dialogue / Subtitle UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Swap Machine UI")]
        [SerializeField] private GameObject swapMachinePanel;
        [SerializeField] private TMP_Text onsetLetterTMP;
        [SerializeField] private TMP_Text doubleLLetterTMP;
        [SerializeField] private TMP_Text blendedWordTMP;
        [SerializeField] private Image wordIllustrationImage;
        [SerializeField] private Button blendLeverButton;
        [SerializeField] private Button nextWordButton;

        [Header("Phase 2: Meet Dell & Story Words UI")]
        [SerializeField] private GameObject meetDellPanel;
        [SerializeField] private TMP_Text dellCharacterTMP;
        [SerializeField] private Image dellCharacterImage;
        [SerializeField] private Button dellPronounceButton;
        [SerializeField] private Button nextToWallButton;

        [Header("Phase 3: Family Read Wall")]
        [SerializeField] private GameObject familyWallPanel;
        [SerializeField] private Button[] wallWordButtons = new Button[6];
        [SerializeField] private TMP_Text[] wallWordTexts = new TMP_Text[6];
        [SerializeField] private Image[] wallWordHighlights = new Image[6];
        [SerializeField] private Button finishFamilyWallButton;

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

        private int currentWordIndex = 0;
        private int wallTappedCount = 0;
        private bool isTransitioning = false;
        private bool hasInitialized = false;

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
                activityData = Resources.Load<EllFamilyData>("Phonics2/Unit10/EllFamilyData_Unit10");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<EllFamilyData>();
                activityData.PopulateDefaultData();
            }
        }

        private void SetupButtonListeners()
        {
            if (blendLeverButton != null)
            {
                blendLeverButton.onClick.RemoveAllListeners();
                blendLeverButton.onClick.AddListener(OnBlendLeverClicked);
            }

            if (nextWordButton != null)
            {
                nextWordButton.onClick.RemoveAllListeners();
                nextWordButton.onClick.AddListener(OnNextWordClicked);
            }

            if (dellPronounceButton != null)
            {
                dellPronounceButton.onClick.RemoveAllListeners();
                dellPronounceButton.onClick.AddListener(OnDellPronounceClicked);
            }

            if (nextToWallButton != null)
            {
                nextToWallButton.onClick.RemoveAllListeners();
                nextToWallButton.onClick.AddListener(StartFamilyWallPhase);
            }

            for (int i = 0; i < wallWordButtons.Length; i++)
            {
                int index = i;
                if (wallWordButtons[i] != null)
                {
                    wallWordButtons[i].onClick.RemoveAllListeners();
                    wallWordButtons[i].onClick.AddListener(() => OnWallWordClicked(index));
                }
            }

            if (finishFamilyWallButton != null)
            {
                finishFamilyWallButton.onClick.RemoveAllListeners();
                finishFamilyWallButton.onClick.AddListener(CompleteStop2);
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
            currentWordIndex = 0;
            wallTappedCount = 0;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (nextWordButton != null) nextWordButton.gameObject.SetActive(false);
            if (meetDellPanel != null) meetDellPanel.SetActive(false);
            if (familyWallPanel != null) familyWallPanel.SetActive(false);
            if (finishFamilyWallButton != null) finishFamilyWallButton.gameObject.SetActive(false);

            if (swapMachinePanel != null) swapMachinePanel.SetActive(true);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);

            StartCoroutine(RunActivityIntroRoutine());
        }

        private IEnumerator RunActivityIntroRoutine()
        {
            isTransitioning = true;
            SetDialogue("Remember s-c-oo-p, when two letters held hands? Look — two l's, doing exactly the same thing!");
            if (activityData != null && activityData.openingCallbackClip != null)
            {
                yield return PlayVoiceClip(activityData.openingCallbackClip);
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            isTransitioning = false;
            LoadSwapWord(0);
        }

        private void LoadSwapWord(int index)
        {
            if (activityData == null || activityData.ellWords == null || index >= activityData.ellWords.Length)
            {
                StartMeetDellPhase();
                return;
            }

            currentWordIndex = index;
            var item = activityData.ellWords[index];

            if (onsetLetterTMP != null) onsetLetterTMP.text = item.onset;
            if (doubleLLetterTMP != null) doubleLLetterTMP.text = item.rime;
            if (blendedWordTMP != null) blendedWordTMP.text = $"{item.onset} + {item.rime}";
            if (nextWordButton != null) nextWordButton.gameObject.SetActive(false);

            if (wordIllustrationImage != null)
            {
                bool hasSprite = item.wordPicture != null;
                wordIllustrationImage.gameObject.SetActive(hasSprite);
                if (hasSprite) wordIllustrationImage.sprite = item.wordPicture;
            }

            SetDialogue($"Blend {item.onset} with {item.rime}!");
            UpdateProgressUI((float)index / 10f);
        }

        private void OnBlendLeverClicked()
        {
            if (isTransitioning) return;
            StartCoroutine(RunBlendRoutine());
        }

        private IEnumerator RunBlendRoutine()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.tileSnapSfx : null);

            var item = activityData.ellWords[currentWordIndex];
            if (blendedWordTMP != null) blendedWordTMP.text = $"<b>{item.fullWord}</b>";

            PlaySFX(activityData != null ? activityData.wordBlendSfx : null);
            SetDialogue(item.fullWord);

            if (item.fullWordAudioClip != null)
            {
                yield return PlayVoiceClip(item.fullWordAudioClip);
            }
            else if (currentWordIndex == 0 && activityData != null && activityData.blendDemonstrationClip != null)
            {
                yield return PlayVoiceClip(activityData.blendDemonstrationClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (nextWordButton != null) nextWordButton.gameObject.SetActive(true);
            isTransitioning = false;
        }

        private void OnNextWordClicked()
        {
            if (isTransitioning) return;
            currentWordIndex++;
            LoadSwapWord(currentWordIndex);
        }

        #region Phase 2: Meet Dell

        private void StartMeetDellPhase()
        {
            if (swapMachinePanel != null) swapMachinePanel.SetActive(false);
            if (meetDellPanel != null) meetDellPanel.SetActive(true);

            if (dellCharacterTMP != null) dellCharacterTMP.text = "Dell";
            SetDialogue("This is Dell. It is somebody's name, so it wears a big D — but it reads just like bell.");

            if (activityData != null && activityData.meetDellClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.meetDellClip);
            }

            UpdateProgressUI(0.65f);
        }

        private void OnDellPronounceClicked()
        {
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            SetDialogue("Dell! Capital D-e-l-l!");
        }

        #endregion

        #region Phase 3: Family Read Wall

        private void StartFamilyWallPhase()
        {
            if (meetDellPanel != null) meetDellPanel.SetActive(false);
            if (familyWallPanel != null) familyWallPanel.SetActive(true);

            wallTappedCount = 0;
            if (finishFamilyWallButton != null) finishFamilyWallButton.gameObject.SetActive(false);

            if (activityData != null && activityData.ellWords != null)
            {
                for (int i = 0; i < wallWordButtons.Length; i++)
                {
                    if (wallWordButtons[i] != null)
                    {
                        bool active = i < activityData.ellWords.Length;
                        wallWordButtons[i].gameObject.SetActive(active);

                        if (active)
                        {
                            var item = activityData.ellWords[i];
                            if (wallWordTexts != null && i < wallWordTexts.Length && wallWordTexts[i] != null)
                            {
                                wallWordTexts[i].text = item.fullWord;
                            }
                            if (wallWordHighlights != null && i < wallWordHighlights.Length && wallWordHighlights[i] != null)
                            {
                                wallWordHighlights[i].gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }

            SetDialogue("Read them all to me: well, bell, fell, tell, yell, sell.");
            if (activityData != null && activityData.familyReadPromptClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.familyReadPromptClip);
            }

            UpdateProgressUI(0.85f);
        }

        private void OnWallWordClicked(int index)
        {
            if (activityData == null || activityData.ellWords == null || index >= activityData.ellWords.Length) return;

            var item = activityData.ellWords[index];
            SetDialogue(item.fullWord);

            if (item.fullWordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(item.fullWordAudioClip);
            }

            if (wallWordHighlights != null && index < wallWordHighlights.Length && wallWordHighlights[index] != null)
            {
                wallWordHighlights[index].gameObject.SetActive(true);
            }

            PlaySFX(activityData != null ? activityData.tileSnapSfx : null);
            wallTappedCount++;

            if (wallTappedCount >= activityData.ellWords.Length)
            {
                if (finishFamilyWallButton != null) finishFamilyWallButton.gameObject.SetActive(true);
                SetDialogue("Now you know every word in the story. Let us go and read it!");
                if (activityData != null && activityData.storyBridgeClip != null)
                {
                    PlayVoiceClipNonBlocking(activityData.storyBridgeClip);
                }
            }
        }

        #endregion

        private void CompleteStop2()
        {
            StartCoroutine(CompleteStop2Sequence());
        }

        private IEnumerator CompleteStop2Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("The -ell family conquered! Ready for The Dog in the Well!");
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
