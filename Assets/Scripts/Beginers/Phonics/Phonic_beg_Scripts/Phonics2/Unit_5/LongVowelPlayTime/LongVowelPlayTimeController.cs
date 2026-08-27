using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit5
{
    public class LongVowelPlayTimeController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit5";
        [SerializeField] private string topicName = "LongVowelPlayTime";

        [Header("Data Asset")]
        [SerializeField] private LongVowelPlayTimeData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;

        [Header("Phase 1: Gap-Fill Worksheet UI")]
        [SerializeField] private GameObject worksheetPanel;
        [SerializeField] private TMP_Text worksheetWordText;
        [SerializeField] private Image worksheetWordImage;
        [SerializeField] private RectTransform gapDropSlot;
        [SerializeField] private LongVowelPlayTimeTile[] tileUIs = new LongVowelPlayTimeTile[3];
        [SerializeField] private Button startStarRoundButton; // INACTIVE initially - activates ONLY after Phase 1 worksheet complete!

        [Header("Phase 2: Tara Star Round UI")]
        [SerializeField] private GameObject starRoundPanel;
        [SerializeField] private TMP_Text starPromptTMP;
        [SerializeField] private Image starPromptImage;
        [SerializeField] private Button[] starChoiceButtons = new Button[3];
        [SerializeField] private TMP_Text[] starChoiceTexts = new TMP_Text[3];
        [SerializeField] private Image[] starChoiceImages = new Image[3];

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject taraMascotObject;
        [SerializeField] private GameObject momoHintObject;

        [Header("Rewards & Progression UI")]
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

        private int worksheetIndex = 0;
        private int starChallengeIndex = 0;
        private bool isStarRoundActive = false;
        private bool isTransitioning = false;
        private Camera mainCamera;
        private StarRoundUnit5Challenge currentStarChallenge;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (voiceAudioSource == null) voiceAudioSource = gameObject.AddComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            SetupButtonListeners();
            StartActivity();
        }

        private void OnEnable()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
            StartActivity();
        }

        private void OnDisable()
        {
            DeactivateMascots();
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            if (startStarRoundButton != null)
            {
                startStarRoundButton.onClick.AddListener(StartStarRound);
            }

            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                int index = i;
                if (starChoiceButtons[i] != null)
                {
                    starChoiceButtons[i].onClick.AddListener(() => OnStarChoiceSelected(index));
                }
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

        public void StartActivity()
        {
            worksheetIndex = 0;
            starChallengeIndex = 0;
            isStarRoundActive = false;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);

            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);

            if (worksheetPanel != null) worksheetPanel.SetActive(true);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);

            SetDialogue("Look at the picture and listen. Which letters are missing?");
            if (activityData != null && activityData.leoIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.leoIntroClip);
            }

            LoadWorksheetItem(0);
        }

        private void LoadWorksheetItem(int index)
        {
            if (activityData == null || activityData.worksheetItems == null || index >= activityData.worksheetItems.Length)
            {
                CompleteWorksheetPhase();
                return;
            }

            worksheetIndex = index;
            PlayTimeWorksheetItem item = activityData.worksheetItems[index];

            if (worksheetWordText != null) worksheetWordText.text = item.wordWithGap;
            if (worksheetWordImage != null && item.wordSprite != null)
            {
                worksheetWordImage.sprite = item.wordSprite;
                worksheetWordImage.gameObject.SetActive(true);
            }

            for (int t = 0; t < tileUIs.Length; t++)
            {
                if (t < item.tileOptions.Length && tileUIs[t] != null)
                {
                    tileUIs[t].SetupTile(item.tileOptions[t], this);
                    tileUIs[t].gameObject.SetActive(true);
                }
                else if (tileUIs[t] != null)
                {
                    tileUIs[t].gameObject.SetActive(false);
                }
            }

            SetDialogue($"Listen: {item.fullWordText.ToUpper()}. Which team makes that sound here?");
            if (item.missingSoundClip != null)
            {
                PlayVoiceClipNonBlocking(item.missingSoundClip);
            }

            UpdateProgressUI((index / 9f) * 0.5f);
        }

        public void EvaluateTileDrop(LongVowelPlayTimeTile tile, PointerEventData eventData)
        {
            if (isTransitioning || tile == null || activityData == null || worksheetIndex >= activityData.worksheetItems.Length) return;

            PlayTimeWorksheetItem item = activityData.worksheetItems[worksheetIndex];
            bool targetHit = false;

            if (gapDropSlot != null && eventData != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : mainCamera;
                targetHit = RectTransformUtility.RectangleContainsScreenPoint(gapDropSlot, eventData.position, cam);
            }

            if (targetHit)
            {
                bool isCorrect = (tile.TileSpelling == item.correctSpellingTile);
                if (isCorrect)
                {
                    StartCoroutine(HandleWorksheetCorrect(item));
                }
                else
                {
                    tile.ReturnToStartPosition();
                    StartCoroutine(HandleWorksheetWrong(tile.TileSpelling, item));
                }
            }
            else
            {
                tile.ReturnToStartPosition();
            }
        }

        private IEnumerator HandleWorksheetCorrect(PlayTimeWorksheetItem item)
        {
            isTransitioning = true;
            PlaySFX(activityData.correctChimeSfx);
            TriggerWiggleStarMeter();

            if (worksheetWordText != null) worksheetWordText.text = item.fullWordText;

            SetDialogue($"Yes! {item.fullWordText.ToUpper()}! Now trace it and say it — {item.fullWordText}!");
            if (item.wordAudioClip != null)
            {
                yield return PlayVoiceClip(item.wordAudioClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            worksheetIndex++;
            isTransitioning = false;
            LoadWorksheetItem(worksheetIndex);
        }

        private IEnumerator HandleWorksheetWrong(string chosenSpelling, PlayTimeWorksheetItem item)
        {
            isTransitioning = true;
            PlaySFX(activityData.retryGentleSfx);

            SetDialogue($"Good guess! Both '{chosenSpelling}' and '{item.correctSpellingTile}' say the sound — for this word we use '{item.correctSpellingTile}'!");
            yield return new WaitForSeconds(1.2f);

            isTransitioning = false;
        }

        private void CompleteWorksheetPhase()
        {
            if (worksheetWordText != null) worksheetWordText.text = "COMPLETE!";
            if (startStarRoundButton != null)
            {
                startStarRoundButton.gameObject.SetActive(true);
            }

            SetDialogue("Great job completing all words! Tap 'Start Star Round' to continue with Tara! ⭐");
        }

        private void StartStarRound()
        {
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);
            if (worksheetPanel != null) worksheetPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(true);

            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(true);

            isStarRoundActive = true;
            starChallengeIndex = 0;

            SetDialogue("Tara says: My turn! Six quick challenges. Ready? Roar!");
            if (activityData != null && activityData.taraOpenerClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.taraOpenerClip);
            }

            LoadStarChallenge(0);
        }

        private void LoadStarChallenge(int index)
        {
            if (activityData == null || activityData.starChallenges == null || index >= activityData.starChallenges.Length)
            {
                StartCoroutine(CompleteStarRoundSequence());
                return;
            }

            starChallengeIndex = index;
            currentStarChallenge = activityData.starChallenges[index];

            if (starPromptTMP != null) starPromptTMP.text = currentStarChallenge.questionPrompt;
            if (starPromptImage != null && currentStarChallenge.promptSprite != null)
            {
                starPromptImage.sprite = currentStarChallenge.promptSprite;
                starPromptImage.gameObject.SetActive(true);
            }
            else if (starPromptImage != null)
            {
                starPromptImage.gameObject.SetActive(false);
            }

            for (int c = 0; c < starChoiceButtons.Length; c++)
            {
                if (c < currentStarChallenge.choices.Length && starChoiceButtons[c] != null)
                {
                    starChoiceButtons[c].gameObject.SetActive(true);
                    if (starChoiceTexts != null && c < starChoiceTexts.Length && starChoiceTexts[c] != null)
                    {
                        starChoiceTexts[c].text = currentStarChallenge.choices[c];
                    }
                    if (starChoiceImages != null && c < starChoiceImages.Length && starChoiceImages[c] != null)
                    {
                        if (currentStarChallenge.choiceSprites != null && c < currentStarChallenge.choiceSprites.Length && currentStarChallenge.choiceSprites[c] != null)
                        {
                            starChoiceImages[c].sprite = currentStarChallenge.choiceSprites[c];
                            starChoiceImages[c].gameObject.SetActive(true);
                        }
                        else
                        {
                            starChoiceImages[c].gameObject.SetActive(false);
                        }
                    }
                }
                else if (starChoiceButtons[c] != null)
                {
                    starChoiceButtons[c].gameObject.SetActive(false);
                }
            }

            SetDialogue(currentStarChallenge.questionPrompt);
            if (currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }

            UpdateProgressUI(0.5f + (index / 6f) * 0.5f);
        }

        private void OnStarChoiceSelected(int choiceIndex)
        {
            if (isTransitioning || currentStarChallenge == null) return;

            bool isCorrect = (choiceIndex == currentStarChallenge.correctChoiceIndex);
            if (isCorrect)
            {
                StartCoroutine(HandleStarChoiceCorrect());
            }
            else
            {
                StartCoroutine(HandleStarChoiceWrong());
            }
        }

        private IEnumerator HandleStarChoiceCorrect()
        {
            isTransitioning = true;
            PlaySFX(activityData.correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue("Roar! You got it right!");
            yield return new WaitForSeconds(0.8f);

            starChallengeIndex++;
            isTransitioning = false;
            LoadStarChallenge(starChallengeIndex);
        }

        private IEnumerator HandleStarChoiceWrong()
        {
            isTransitioning = true;
            PlaySFX(activityData.retryGentleSfx);

            SetDialogue("Give it another try! Listen carefully to Tara!");
            if (currentStarChallenge != null && currentStarChallenge.promptClip != null)
            {
                yield return PlayVoiceClip(currentStarChallenge.promptClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            isTransitioning = false;
        }

        private IEnumerator CompleteStarRoundSequence()
        {
            isTransitioning = true;
            PlaySFX(activityData.correctChimeSfx);
            UpdateProgressUI(1.0f);

            SetDialogue("Short vowels, long vowels, magic e and teams. You are a LONG VOWEL HERO!");
            if (activityData != null && activityData.badgeVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.badgeVoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);

            yield return new WaitForSeconds(1.5f);

            SetDialogue("Unit Six is open! Next time we find out which sounds BUZZ and which ones whisper!");
            if (activityData != null && activityData.unit6UnlockVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.unit6UnlockVoiceClip);
            }

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.gameObject.SetActive(true);
            isTransitioning = false;
        }

        private void SetDialogue(string message)
        {
            if (dialogueText != null) dialogueText.text = message;
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
            if (starMeterRect != null) TriggerWiggle(starMeterRect);
        }

        private void TriggerWiggle(RectTransform target)
        {
            if (target != null) StartCoroutine(WiggleAnimation(target));
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
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }
    }
}
