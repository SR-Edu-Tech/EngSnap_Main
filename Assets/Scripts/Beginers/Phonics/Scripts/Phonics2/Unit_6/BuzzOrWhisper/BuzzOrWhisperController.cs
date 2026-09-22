using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit6
{
    public class BuzzOrWhisperController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit6";
        [SerializeField] private string topicName = "BuzzOrWhisper";

        [Header("Data Asset")]
        [SerializeField] private BuzzOrWhisperData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Physical Tests UI (Throat Ripple & Tissue Puff)")]
        [SerializeField] private GameObject physicalTestsPanel;
        [SerializeField] private GameObject throatRippleVisual; // Glowing soundwave ripple on mascot's throat
        [SerializeField] private Button throatTestZButton;
        [SerializeField] private Button throatTestSButton;
        [SerializeField] private RectTransform hangingTissueRect; // Tissue UI that flaps/flies
        [SerializeField] private Button puffTestPButton;
        [SerializeField] private Button puffTestBButton;
        [SerializeField] private Button startSortingButton;

        [Header("Phase 2: Tunnel Sorting UI (10 Rounds)")]
        [SerializeField] private GameObject sortingPanel;
        [SerializeField] private TMP_Text currentSoundSymbolTMP;
        [SerializeField] private Image currentMouthImage;
        [SerializeField] private Button replaySoundButton;
        [SerializeField] private Button buzzTunnelButton;
        [SerializeField] private Button whisperTunnelButton;
        [SerializeField] private GameObject buzzTunnelGlow;
        [SerializeField] private GameObject whisperTunnelBreeze;

        [Header("Phase 3: Free Play Soundboard UI")]
        [SerializeField] private GameObject freePlayPanel;
        [SerializeField] private Button[] freePlayButtons;
        [SerializeField] private TMP_Text[] freePlayTexts;
        [SerializeField] private Image[] freePlayMouthImages;
        [SerializeField] private Button finishActivityButton;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentSortingIndex = 0;
        private int failAttempts = 0;
        private bool isTransitioning = false;
        private Coroutine tissueFlapCoroutine;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
        }

        private void Start()
        {
            SetupButtonListeners();
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
                activityData = Resources.Load<BuzzOrWhisperData>("Phonics2/Unit6/BuzzOrWhisperData_Unit6");
            }
        }

        private void StopAllAudio()
        {
            if (voiceAudioSource != null && voiceAudioSource.isPlaying) voiceAudioSource.Stop();
            if (sfxAudioSource != null && sfxAudioSource.isPlaying) sfxAudioSource.Stop();
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            if (throatTestZButton != null) throatTestZButton.onClick.AddListener(OnThroatTestZTapped);
            if (throatTestSButton != null) throatTestSButton.onClick.AddListener(OnThroatTestSTapped);
            if (puffTestPButton != null) puffTestPButton.onClick.AddListener(OnPuffTestPTapped);
            if (puffTestBButton != null) puffTestBButton.onClick.AddListener(OnPuffTestBTapped);
            if (startSortingButton != null) startSortingButton.onClick.AddListener(StartSortingPhase);

            if (replaySoundButton != null) replaySoundButton.onClick.AddListener(ReplayCurrentSound);
            if (buzzTunnelButton != null) buzzTunnelButton.onClick.AddListener(() => OnTunnelChosen(true));
            if (whisperTunnelButton != null) whisperTunnelButton.onClick.AddListener(() => OnTunnelChosen(false));

            if (freePlayButtons != null)
            {
                for (int i = 0; i < freePlayButtons.Length; i++)
                {
                    int index = i;
                    if (freePlayButtons[i] != null)
                    {
                        freePlayButtons[i].onClick.AddListener(() => OnFreePlaySoundTapped(index));
                    }
                }
            }

            if (finishActivityButton != null) finishActivityButton.onClick.AddListener(CompleteStop1);

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
            currentSortingIndex = 0;
            failAttempts = 0;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (throatRippleVisual != null) throatRippleVisual.SetActive(false);

            ShowPhysicalTestsPhase();
        }

        #region Phase 1: Physical Tests (Throat & Puff)

        private void ShowPhysicalTestsPhase()
        {
            if (physicalTestsPanel != null) physicalTestsPanel.SetActive(true);
            if (sortingPanel != null) sortingPanel.SetActive(false);
            if (freePlayPanel != null) freePlayPanel.SetActive(false);

            SetDialogue("Welcome to the Buzz and Whisper Cave! Some sounds buzz… and some are just air.");
            if (activityData != null && activityData.leoWelcomeClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.leoWelcomeClip);
            }

            UpdateProgressUI(0.05f);
        }

        private void OnThroatTestZTapped()
        {
            if (isTransitioning) return;
            StartCoroutine(ThroatTestZRoutine());
        }

        private IEnumerator ThroatTestZRoutine()
        {
            isTransitioning = true;
            if (throatRippleVisual != null) throatRippleVisual.SetActive(true);
            PlaySFX(activityData != null ? activityData.buzzHumSfx : null);

            SetDialogue("Put your hand on your throat. Say zzzzzz. Feel that buzz?");
            if (activityData != null && activityData.throatTestPromptClip != null)
            {
                yield return PlayVoiceClip(activityData.throatTestPromptClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
        }

        private void OnThroatTestSTapped()
        {
            if (isTransitioning) return;
            StartCoroutine(ThroatTestSRoutine());
        }

        private IEnumerator ThroatTestSRoutine()
        {
            isTransitioning = true;
            if (throatRippleVisual != null) throatRippleVisual.SetActive(false);
            PlaySFX(activityData != null ? activityData.whisperBreezeSfx : null);

            SetDialogue("Now say sssssss. No buzz! Just air. Same mouth — different sound!");
            if (activityData != null && activityData.throatTestContrastClip != null)
            {
                yield return PlayVoiceClip(activityData.throatTestContrastClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
        }

        private void OnPuffTestPTapped()
        {
            if (isTransitioning) return;
            AnimateTissuePuff(true); // Big puff!
            SetDialogue("Watch the tissue. /p/ — big puff!");
            PlaySFX(activityData != null ? activityData.tissueFlySfx : null);
        }

        private void OnPuffTestBTapped()
        {
            if (isTransitioning) return;
            AnimateTissuePuff(false); // Little puff!
            SetDialogue("Watch the tissue. /b/ — little puff!");
        }

        private void AnimateTissuePuff(bool isBigPuff)
        {
            if (hangingTissueRect == null) return;
            if (tissueFlapCoroutine != null) StopCoroutine(tissueFlapCoroutine);
            tissueFlapCoroutine = StartCoroutine(TissueFlapAnimation(isBigPuff));
        }

        private IEnumerator TissueFlapAnimation(bool isBigPuff)
        {
            float duration = isBigPuff ? 0.8f : 0.4f;
            float maxAngle = isBigPuff ? -55f : -15f;
            float elapsed = 0f;
            Quaternion startRot = Quaternion.identity;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float angle = Mathf.Sin(t * Mathf.PI * 3f) * maxAngle * (1f - t);
                hangingTissueRect.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            hangingTissueRect.localRotation = startRot;
        }

        #endregion

        #region Phase 2: Tunnel Sorting (10 Rounds)

        private void StartSortingPhase()
        {
            if (physicalTestsPanel != null) physicalTestsPanel.SetActive(false);
            if (sortingPanel != null) sortingPanel.SetActive(true);
            if (freePlayPanel != null) freePlayPanel.SetActive(false);

            currentSortingIndex = 0;
            LoadSortingRound(0);
        }

        private void LoadSortingRound(int index)
        {
            if (activityData == null || activityData.sortingSounds == null || index >= activityData.sortingSounds.Length)
            {
                StartFreePlayPhase();
                return;
            }

            currentSortingIndex = index;
            failAttempts = 0;
            isTransitioning = false;
            if (momoHintObject != null) momoHintObject.SetActive(false);

            BuzzSoundItem item = activityData.sortingSounds[index];

            if (currentSoundSymbolTMP != null)
            {
                currentSoundSymbolTMP.text = item.soundSymbol;
                StartCoroutine(PopScaleRoutine(currentSoundSymbolTMP.transform, 1.25f, 0.3f));
            }

            if (currentMouthImage != null)
            {
                if (item.mouthSprite != null)
                {
                    currentMouthImage.sprite = item.mouthSprite;
                    currentMouthImage.gameObject.SetActive(true);
                }
                else
                {
                    currentMouthImage.gameObject.SetActive(false);
                }
            }

            SetDialogue($"Listen: {item.soundSymbol}. Buzz tunnel, or whisper tunnel? Send it down!");
            if (item.soundClip != null)
            {
                PlayVoiceClipNonBlocking(item.soundClip);
            }

            UpdateProgressUI(0.15f + (index / 10f) * 0.65f);
        }

        private void ReplayCurrentSound()
        {
            if (activityData == null || currentSortingIndex >= activityData.sortingSounds.Length) return;
            BuzzSoundItem item = activityData.sortingSounds[currentSortingIndex];
            if (item != null && item.soundClip != null)
            {
                PlayVoiceClipNonBlocking(item.soundClip);
            }
        }

        private readonly Color correctGreenColor = new Color(0.298f, 0.686f, 0.314f, 1f); // #4CAF50
        private readonly Color wrongRedColor = new Color(0.937f, 0.325f, 0.314f, 1f);     // #EF5350

        private void OnTunnelChosen(bool chosenBuzz)
        {
            if (isTransitioning || activityData == null || currentSortingIndex >= activityData.sortingSounds.Length) return;

            BuzzSoundItem item = activityData.sortingSounds[currentSortingIndex];
            bool isCorrect = (chosenBuzz == item.isBuzzer);
            Button chosenButton = chosenBuzz ? buzzTunnelButton : whisperTunnelButton;

            if (chosenButton != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenButton, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                StartCoroutine(HandleSortingCorrect(item, chosenBuzz));
            }
            else
            {
                StartCoroutine(HandleSortingWrong(item));
            }
        }

        private IEnumerator HandleSortingCorrect(BuzzSoundItem item, bool isBuzz)
        {
            isTransitioning = true;
            PlaySFX(activityData != null && activityData.correctChimeSfx != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (isBuzz && buzzTunnelGlow != null) StartCoroutine(PopScaleRoutine(buzzTunnelGlow.transform, 1.3f, 0.4f));
            if (!isBuzz && whisperTunnelBreeze != null) StartCoroutine(PopScaleRoutine(whisperTunnelBreeze.transform, 1.3f, 0.4f));

            if (item.isBuzzer)
            {
                SetDialogue($"Yes! {item.soundSymbol} buzzes. Hand on your throat — {item.letterName.ToLower()}{item.letterName.ToLower()}{item.letterName.ToLower()}!");
            }
            else
            {
                SetDialogue($"Yes! {item.soundSymbol} is a whisperer. Just air — {item.soundSymbol} {item.soundSymbol}!");
            }

            if (item.voiceSuccessClip != null)
            {
                yield return PlayVoiceClip(item.voiceSuccessClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            currentSortingIndex++;
            isTransitioning = false;
            LoadSortingRound(currentSortingIndex);
        }

        private IEnumerator HandleSortingWrong(BuzzSoundItem item)
        {
            isTransitioning = true;
            failAttempts++;
            PlaySFX(activityData != null && activityData.retryGentleSfx != null ? activityData.retryGentleSfx : null);

            if (failAttempts >= 2 && momoHintObject != null)
            {
                momoHintObject.SetActive(true);
            }

            SetDialogue("Hand on your throat! Buzz, or no buzz?");
            if (activityData != null && activityData.momoHintClip != null)
            {
                yield return PlayVoiceClip(activityData.momoHintClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
        }

        private IEnumerator FlashButtonFeedback(Button btn, bool isCorrect, float duration = 0.5f)
        {
            if (btn == null) yield break;
            Image img = btn.GetComponent<Image>();
            Color originalColor = (img != null) ? img.color : Color.white;
            RectTransform rt = btn.GetComponent<RectTransform>();

            if (img != null)
            {
                img.color = isCorrect ? correctGreenColor : wrongRedColor;
            }

            if (rt != null)
            {
                if (isCorrect)
                {
                    StartCoroutine(WiggleAnimation(rt));
                }
                else
                {
                    StartCoroutine(ShakeAnimation(rt));
                }
            }

            yield return new WaitForSeconds(duration);

            if (img != null)
            {
                img.color = originalColor;
            }
        }

        private IEnumerator ShakeAnimation(RectTransform target)
        {
            if (target == null) yield break;
            Vector3 startPos = target.localPosition;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin(elapsed * 40f) * 12f * (1f - elapsed / duration);
                target.localPosition = startPos + new Vector3(offset, 0f, 0f);
                yield return null;
            }
            target.localPosition = startPos;
        }

        #endregion

        #region Phase 3: Free Play Soundboard

        private void StartFreePlayPhase()
        {
            if (sortingPanel != null) sortingPanel.SetActive(false);
            if (freePlayPanel != null) freePlayPanel.SetActive(true);

            SetDialogue("Great sorting! Now tap any sound to feel the throat buzz!");
            if (activityData != null && activityData.freePlayInstructionClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.freePlayInstructionClip);
            }

            BuzzSoundItem[] sounds = (activityData != null && activityData.freePlaySounds != null && activityData.freePlaySounds.Length > 0)
                ? activityData.freePlaySounds
                : activityData.sortingSounds;

            if (freePlayButtons != null)
            {
                for (int i = 0; i < freePlayButtons.Length; i++)
                {
                    if (i < sounds.Length && freePlayButtons[i] != null)
                    {
                        freePlayButtons[i].gameObject.SetActive(true);
                        if (freePlayTexts != null && i < freePlayTexts.Length && freePlayTexts[i] != null)
                        {
                            freePlayTexts[i].text = sounds[i].soundSymbol;
                        }
                        if (freePlayMouthImages != null && i < freePlayMouthImages.Length && freePlayMouthImages[i] != null)
                        {
                            if (sounds[i].mouthSprite != null)
                            {
                                freePlayMouthImages[i].sprite = sounds[i].mouthSprite;
                                freePlayMouthImages[i].gameObject.SetActive(true);
                            }
                            else
                            {
                                freePlayMouthImages[i].gameObject.SetActive(false);
                            }
                        }
                    }
                    else if (freePlayButtons[i] != null)
                    {
                        freePlayButtons[i].gameObject.SetActive(false);
                    }
                }
            }

            UpdateProgressUI(0.9f);
        }

        private void OnFreePlaySoundTapped(int index)
        {
            if (isTransitioning || activityData == null) return;
            BuzzSoundItem[] sounds = (activityData.freePlaySounds != null && activityData.freePlaySounds.Length > 0)
                ? activityData.freePlaySounds
                : activityData.sortingSounds;

            if (index < 0 || index >= sounds.Length) return;
            BuzzSoundItem item = sounds[index];

            PlaySFX(activityData.starPopSfx);
            if (item.soundClip != null) PlayVoiceClipNonBlocking(item.soundClip);

            if (freePlayButtons != null && index < freePlayButtons.Length && freePlayButtons[index] != null)
            {
                StartCoroutine(FlashButtonFeedback(freePlayButtons[index], true, 0.4f));
            }

            if (item.isBuzzer)
            {
                if (throatRippleVisual != null)
                {
                    throatRippleVisual.SetActive(true);
                    StartCoroutine(PopScaleRoutine(throatRippleVisual.transform, 1.4f, 0.4f));
                }
                SetDialogue($"{item.soundSymbol} BUZZES! Feel your throat!");
            }
            else
            {
                if (throatRippleVisual != null) throatRippleVisual.SetActive(false);
                SetDialogue($"{item.soundSymbol} is a whisperer. Just air!");
            }
        }

        #endregion

        public void CompleteStop1()
        {
            StartCoroutine(CompleteStop1Sequence());
        }

        private IEnumerator CompleteStop1Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null && activityData.correctChimeSfx != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("You can feel which sounds buzz and which whisper! Awesome job!");

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.gameObject.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            yield return new WaitForSeconds(1.2f);
            isTransitioning = false;
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
            if (starMeterRect != null) StartCoroutine(WiggleAnimation(starMeterRect));
        }

        private IEnumerator WiggleAnimation(RectTransform target)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            Vector3 startScale = target.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + Mathf.Sin(elapsed * 25f) * 0.12f;
                target.localScale = startScale * scale;
                yield return null;
            }
            target.localScale = startScale;
        }

        private IEnumerator PopScaleRoutine(Transform target, float targetScale, float duration)
        {
            if (target == null) yield break;
            Vector3 startScale = target.localScale;
            Vector3 maxScale = startScale * targetScale;
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                target.localScale = Vector3.Lerp(startScale, maxScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                target.localScale = Vector3.Lerp(maxScale, startScale, t);
                yield return null;
            }

            target.localScale = startScale;
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
                unitContentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }
    }
}
