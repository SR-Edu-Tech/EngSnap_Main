using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit6
{
    public class SoundTwinsController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit6";
        [SerializeField] private string topicName = "SoundTwins";

        [Header("Data Asset")]
        [SerializeField] private SoundTwinsData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Meet the Twins UI")]
        [SerializeField] private GameObject meetTwinsPanel;
        [SerializeField] private TMP_Text pairTitleTMP;
        [SerializeField] private Image sharedMouthImage;
        [SerializeField] private TMP_Text mouthDescTMP;
        [SerializeField] private Button buzzerTwinButton;
        [SerializeField] private TMP_Text buzzerTwinText;
        [SerializeField] private GameObject buzzerTwinGlow;
        [SerializeField] private Button whispererTwinButton;
        [SerializeField] private TMP_Text whispererTwinText;
        [SerializeField] private Button nextTwinButton;
        [SerializeField] private Button startMinimalPairsButton;

        [Header("Phase 2: Minimal Pairs Quiz UI (8 Rounds)")]
        [SerializeField] private GameObject minimalPairsPanel;
        [SerializeField] private TMP_Text minimalPromptTMP;
        [SerializeField] private Button replaySpokenWordButton;
        [SerializeField] private Button choiceButtonA;
        [SerializeField] private Image choiceImageA;
        [SerializeField] private TMP_Text choiceTextA;
        [SerializeField] private Button choiceButtonB;
        [SerializeField] private Image choiceImageB;
        [SerializeField] private TMP_Text choiceTextB;

        [Header("Phase 3: Twin Trouble Gap UI (4 Rounds)")]
        [SerializeField] private GameObject twinTroublePanel;
        [SerializeField] private Image twinTroubleWordImage;
        [SerializeField] private TMP_Text twinTroubleGapTMP;
        [SerializeField] private Button optionButton1;
        [SerializeField] private TMP_Text optionText1;
        [SerializeField] private Button optionButton2;
        [SerializeField] private TMP_Text optionText2;

        [Header("Phase 4: V vs W Comparison UI")]
        [SerializeField] private GameObject vVsWPanel;
        [SerializeField] private Image mouthVImage;
        [SerializeField] private Image mouthWImage;
        [SerializeField] private Button buttonCardV;
        [SerializeField] private Image cardVImage;
        [SerializeField] private TMP_Text cardVText;
        [SerializeField] private Button buttonCardW;
        [SerializeField] private Image cardWImage;
        [SerializeField] private TMP_Text cardWText;
        [SerializeField] private Button finishStop2Button;

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

        private int currentTwinPairIndex = 0;
        private int currentMinimalPairIndex = 0;
        private int currentTwinTroubleIndex = 0;
        private int currentVvsWIndex = 0;
        private bool isTransitioning = false;

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
                activityData = Resources.Load<SoundTwinsData>("Phonics2/Unit6/SoundTwinsData_Unit6");
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
            if (buzzerTwinButton != null) buzzerTwinButton.onClick.AddListener(OnBuzzerTwinTapped);
            if (whispererTwinButton != null) whispererTwinButton.onClick.AddListener(OnWhispererTwinTapped);
            if (nextTwinButton != null) nextTwinButton.onClick.AddListener(OnNextTwinPairTapped);
            if (startMinimalPairsButton != null) startMinimalPairsButton.onClick.AddListener(StartMinimalPairsPhase);

            if (replaySpokenWordButton != null) replaySpokenWordButton.onClick.AddListener(ReplayMinimalPairWord);
            if (choiceButtonA != null) choiceButtonA.onClick.AddListener(() => OnMinimalPairChoiceSelected(0));
            if (choiceButtonB != null) choiceButtonB.onClick.AddListener(() => OnMinimalPairChoiceSelected(1));

            if (optionButton1 != null) optionButton1.onClick.AddListener(() => OnTwinTroubleOptionSelected(0));
            if (optionButton2 != null) optionButton2.onClick.AddListener(() => OnTwinTroubleOptionSelected(1));

            if (buttonCardV != null) buttonCardV.onClick.AddListener(() => OnVvsWCardSelected(0));
            if (buttonCardW != null) buttonCardW.onClick.AddListener(() => OnVvsWCardSelected(1));
            if (finishStop2Button != null) finishStop2Button.onClick.AddListener(CompleteStop2);

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
            currentTwinPairIndex = 0;
            currentMinimalPairIndex = 0;
            currentTwinTroubleIndex = 0;
            currentVvsWIndex = 0;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);

            ShowMeetTwinsPhase();
        }

        #region Phase 1: Meet the 6 Twin Pairs

        private void ShowMeetTwinsPhase()
        {
            if (meetTwinsPanel != null) meetTwinsPanel.SetActive(true);
            if (minimalPairsPanel != null) minimalPairsPanel.SetActive(false);
            if (twinTroublePanel != null) twinTroublePanel.SetActive(false);
            if (vVsWPanel != null) vVsWPanel.SetActive(false);
            if (startMinimalPairsButton != null) startMinimalPairsButton.gameObject.SetActive(false);

            SetDialogue("These two are twins. Same lips, same tongue — but listen! One buzzes, one whispers.");
            if (activityData != null && activityData.leoIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.leoIntroClip);
            }

            LoadTwinPair(0);
        }

        private void LoadTwinPair(int index)
        {
            if (activityData == null || activityData.twinPairs == null || index >= activityData.twinPairs.Length)
            {
                if (startMinimalPairsButton != null) startMinimalPairsButton.gameObject.SetActive(true);
                return;
            }

            currentTwinPairIndex = index;
            SoundTwinPairItem pair = activityData.twinPairs[index];

            if (pairTitleTMP != null) pairTitleTMP.text = $"Twin Pair: {pair.pairName}";
            if (mouthDescTMP != null) mouthDescTMP.text = pair.mouthDescription;

            if (buzzerTwinText != null) buzzerTwinText.text = pair.buzzerLetter;
            if (whispererTwinText != null) whispererTwinText.text = pair.whispererLetter;

            if (buzzerTwinGlow != null) buzzerTwinGlow.SetActive(true);

            if (sharedMouthImage != null && pair.sharedMouthSprite != null)
            {
                sharedMouthImage.sprite = pair.sharedMouthSprite;
                sharedMouthImage.gameObject.SetActive(true);
            }

            if (nextTwinButton != null)
            {
                nextTwinButton.gameObject.SetActive(index < activityData.twinPairs.Length - 1);
            }
            if (startMinimalPairsButton != null)
            {
                startMinimalPairsButton.gameObject.SetActive(index >= activityData.twinPairs.Length - 1);
            }

            UpdateProgressUI(0.05f + (index / 6f) * 0.25f);
        }

        private void OnBuzzerTwinTapped()
        {
            if (isTransitioning || activityData == null || currentTwinPairIndex >= activityData.twinPairs.Length) return;
            SoundTwinPairItem pair = activityData.twinPairs[currentTwinPairIndex];

            PlaySFX(activityData.starPopSfx);
            if (pair.buzzerSoundClip != null) PlayVoiceClipNonBlocking(pair.buzzerSoundClip);
            SetDialogue($"Buzzer: /{pair.buzzerLetter}/ BUZZES! Feel the throat vibration!");
            TriggerWiggleStarMeter();
        }

        private void OnWhispererTwinTapped()
        {
            if (isTransitioning || activityData == null || currentTwinPairIndex >= activityData.twinPairs.Length) return;
            SoundTwinPairItem pair = activityData.twinPairs[currentTwinPairIndex];

            PlaySFX(activityData.starPopSfx);
            if (pair.whispererSoundClip != null) PlayVoiceClipNonBlocking(pair.whispererSoundClip);
            SetDialogue($"Whisperer: /{pair.whispererLetter}/ whispers! Same mouth, no buzz!");
        }

        private void OnNextTwinPairTapped()
        {
            currentTwinPairIndex++;
            LoadTwinPair(currentTwinPairIndex);
        }

        #endregion

        #region Phase 2: Minimal Pairs Quiz (8 Rounds)

        private void StartMinimalPairsPhase()
        {
            if (meetTwinsPanel != null) meetTwinsPanel.SetActive(false);
            if (minimalPairsPanel != null) minimalPairsPanel.SetActive(true);
            if (twinTroublePanel != null) twinTroublePanel.SetActive(false);
            if (vVsWPanel != null) vVsWPanel.SetActive(false);

            currentMinimalPairIndex = 0;
            LoadMinimalPairRound(0);
        }

        private void LoadMinimalPairRound(int index)
        {
            if (activityData == null || activityData.minimalPairs == null || index >= activityData.minimalPairs.Length)
            {
                StartTwinTroublePhase();
                return;
            }

            currentMinimalPairIndex = index;
            isTransitioning = false;
            MinimalPairQuizItem item = activityData.minimalPairs[index];

            if (minimalPromptTMP != null) minimalPromptTMP.text = $"Listen carefully… Which picture is it?";
            SetDialogue($"Listen carefully… Which picture is it?");

            if (choiceTextA != null) choiceTextA.text = item.choiceWordA;
            if (choiceImageA != null && item.choiceSpriteA != null)
            {
                choiceImageA.sprite = item.choiceSpriteA;
                choiceImageA.gameObject.SetActive(true);
            }

            if (choiceTextB != null) choiceTextB.text = item.choiceWordB;
            if (choiceImageB != null && item.choiceSpriteB != null)
            {
                choiceImageB.sprite = item.choiceSpriteB;
                choiceImageB.gameObject.SetActive(true);
            }

            if (item.targetWordAudio != null)
            {
                PlayVoiceClipNonBlocking(item.targetWordAudio);
            }

            UpdateProgressUI(0.3f + (index / 8f) * 0.35f);
        }

        private void ReplayMinimalPairWord()
        {
            if (activityData == null || currentMinimalPairIndex >= activityData.minimalPairs.Length) return;
            MinimalPairQuizItem item = activityData.minimalPairs[currentMinimalPairIndex];
            if (item != null && item.targetWordAudio != null)
            {
                PlayVoiceClipNonBlocking(item.targetWordAudio);
            }
        }

        private readonly Color correctGreenColor = new Color(0.298f, 0.686f, 0.314f, 1f); // #4CAF50
        private readonly Color wrongRedColor = new Color(0.937f, 0.325f, 0.314f, 1f);     // #EF5350

        private void OnMinimalPairChoiceSelected(int choiceIndex)
        {
            if (isTransitioning || activityData == null || currentMinimalPairIndex >= activityData.minimalPairs.Length) return;

            MinimalPairQuizItem item = activityData.minimalPairs[currentMinimalPairIndex];
            bool isCorrect = (choiceIndex == item.correctChoiceIndex);
            Button chosenBtn = (choiceIndex == 0) ? choiceButtonA : choiceButtonB;

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                StartCoroutine(HandleMinimalPairCorrect(item));
            }
            else
            {
                StartCoroutine(HandleMinimalPairWrong(item));
            }
        }

        private IEnumerator HandleMinimalPairCorrect(MinimalPairQuizItem item)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            string contrastWord = item.correctChoiceIndex == 0 ? item.choiceWordB : item.choiceWordA;
            SetDialogue($"Yes! {item.targetWord.ToUpper()}! A {(item.targetIsWhisperer ? "whisper" : "buzz")} at the start. {contrastWord.ToUpper()} would {(item.targetIsWhisperer ? "BUZZ" : "whisper")}!");

            if (item.successFeedbackClip != null)
            {
                yield return PlayVoiceClip(item.successFeedbackClip);
            }
            else
            {
                yield return new WaitForSeconds(1.1f);
            }

            currentMinimalPairIndex++;
            isTransitioning = false;
            LoadMinimalPairRound(currentMinimalPairIndex);
        }

        private IEnumerator HandleMinimalPairWrong(MinimalPairQuizItem item)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

            SetDialogue($"Listen again: {item.choiceWordA} … {item.choiceWordB} …");
            if (activityData != null && activityData.momoRetryClip != null)
            {
                yield return PlayVoiceClip(activityData.momoRetryClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
        }

        #endregion

        #region Phase 3: Twin Trouble Gap (4 Rounds)

        private void StartTwinTroublePhase()
        {
            if (minimalPairsPanel != null) minimalPairsPanel.SetActive(false);
            if (twinTroublePanel != null) twinTroublePanel.SetActive(true);
            if (vVsWPanel != null) vVsWPanel.SetActive(false);

            currentTwinTroubleIndex = 0;
            LoadTwinTroubleRound(0);
        }

        private void LoadTwinTroubleRound(int index)
        {
            if (activityData == null || activityData.twinTroubleItems == null || index >= activityData.twinTroubleItems.Length)
            {
                StartVvsWPhase();
                return;
            }

            currentTwinTroubleIndex = index;
            isTransitioning = false;
            TwinTroubleGapItem item = activityData.twinTroubleItems[index];

            if (twinTroubleGapTMP != null) twinTroubleGapTMP.text = item.displayGapWord;
            if (twinTroubleWordImage != null && item.wordSprite != null)
            {
                twinTroubleWordImage.sprite = item.wordSprite;
                twinTroubleWordImage.gameObject.SetActive(true);
            }

            if (optionText1 != null) optionText1.text = item.correctLetter;
            if (optionText2 != null) optionText2.text = item.distractorLetter;

            SetDialogue($"Which twin completes the word '{item.fullWord.ToUpper()}'? Hand on your throat!");
            if (item.wordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(item.wordAudioClip);
            }

            UpdateProgressUI(0.65f + (index / 4f) * 0.2f);
        }

        private void OnTwinTroubleOptionSelected(int optionIndex)
        {
            if (isTransitioning || activityData == null || currentTwinTroubleIndex >= activityData.twinTroubleItems.Length) return;

            TwinTroubleGapItem item = activityData.twinTroubleItems[currentTwinTroubleIndex];
            bool isCorrect = (optionIndex == 0); // Option 1 is correct in our structure
            Button chosenBtn = (optionIndex == 0) ? optionButton1 : optionButton2;

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                StartCoroutine(HandleTwinTroubleCorrect(item));
            }
            else
            {
                StartCoroutine(HandleTwinTroubleWrong(item));
            }
        }

        private IEnumerator HandleTwinTroubleCorrect(TwinTroubleGapItem item)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (twinTroubleGapTMP != null)
            {
                twinTroubleGapTMP.text = $"<color=#FFD54F><b>{item.correctLetter}</b></color>{item.fullWord.Substring(item.correctLetter.Length)}";
            }

            SetDialogue($"Spot on! {item.fullWord.ToUpper()} starts with {item.correctLetter}!");
            yield return new WaitForSeconds(0.9f);

            currentTwinTroubleIndex++;
            isTransitioning = false;
            LoadTwinTroubleRound(currentTwinTroubleIndex);
        }

        private IEnumerator HandleTwinTroubleWrong(TwinTroubleGapItem item)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

            SetDialogue($"Feel your throat: does {item.fullWord.ToUpper()} buzz or whisper?");
            yield return new WaitForSeconds(1.0f);

            isTransitioning = false;
        }

        #endregion

        #region Phase 4: V vs W Comparison

        private void StartVvsWPhase()
        {
            if (twinTroublePanel != null) twinTroublePanel.SetActive(false);
            if (vVsWPanel != null) vVsWPanel.SetActive(true);

            currentVvsWIndex = 0;

            SetDialogue("Now two that are NOT twins, but often get mixed up. /v/ — teeth on your lip. /w/ — round lips, like a kiss!");
            if (activityData != null && activityData.vVsWIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.vVsWIntroClip);
            }

            LoadVvsWRound(0);
        }

        private void LoadVvsWRound(int index)
        {
            if (activityData == null || activityData.vVsWItems == null || index >= activityData.vVsWItems.Length)
            {
                if (finishStop2Button != null) finishStop2Button.gameObject.SetActive(true);
                return;
            }

            currentVvsWIndex = index;
            isTransitioning = false;
            VvsWItem item = activityData.vVsWItems[index];

            if (cardVText != null) cardVText.text = item.wordV;
            if (cardVImage != null && item.spriteV != null)
            {
                cardVImage.sprite = item.spriteV;
                cardVImage.gameObject.SetActive(true);
            }

            if (cardWText != null) cardWText.text = item.wordW;
            if (cardWImage != null && item.spriteW != null)
            {
                cardWImage.sprite = item.spriteW;
                cardWImage.gameObject.SetActive(true);
            }

            UpdateProgressUI(0.85f + (index / 4f) * 0.12f);
        }

        private void OnVvsWCardSelected(int cardIndex)
        {
            if (isTransitioning || activityData == null || currentVvsWIndex >= activityData.vVsWItems.Length) return;

            VvsWItem item = activityData.vVsWItems[currentVvsWIndex];
            bool isCorrect = (cardIndex == item.targetIndex);
            Button chosenBtn = (cardIndex == 0) ? buttonCardV : buttonCardW;

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
                TriggerWiggleStarMeter();
                SetDialogue(cardIndex == 0 ? $"{item.wordV.ToUpper()}: Teeth on your lip!" : $"{item.wordW.ToUpper()}: Round lips like a kiss!");
                currentVvsWIndex++;
                LoadVvsWRound(currentVvsWIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue("Look at the mouth! Teeth on lip for /v/, round lips for /w/!");
            }
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

        public void CompleteStop2()
        {
            StartCoroutine(CompleteStop2Sequence());
        }

        private IEnumerator CompleteStop2Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("You mastered the Sound Twins and the V/W mouth test! Super work!");

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
