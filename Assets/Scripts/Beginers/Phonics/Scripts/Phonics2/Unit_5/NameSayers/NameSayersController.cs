using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit5
{
    public class NameSayersController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit5";
        [SerializeField] private string topicName = "NameSayers";

        [Header("Data Asset")]
        [SerializeField] private NameSayersData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;

        [Header("Phase 1: Boat Character Intro UI")]
        [SerializeField] private GameObject characterIntroPanel;
        [SerializeField] private Button[] vowelCharacterButtons = new Button[5]; // ā, ē, ī, ō, ū
        [SerializeField] private TMP_Text[] vowelCharacterTexts = new TMP_Text[5];
        [SerializeField] private Image[] pictureWordImages = new Image[4];
        [SerializeField] private TMP_Text[] pictureWordTexts = new TMP_Text[4];
        [SerializeField] private Button startContrastPhaseButton;

        [Header("Phase 2: Short vs Long Contrast UI")]
        [SerializeField] private GameObject contrastPhasePanel;
        [SerializeField] private TMP_Text contrastWordText;
        [SerializeField] private Image contrastWordImage;
        [SerializeField] private Button shortHatButton; // Curved Breve Hat
        [SerializeField] private Button longHatButton;  // Flat Macron Hat

        [Header("Phase 3: Hat Swap Drag UI")]
        [SerializeField] private GameObject hatSwapPhasePanel;
        [SerializeField] private TMP_Text hatSwapWordText;
        [SerializeField] private Image hatSwapWordImage;
        [SerializeField] private RectTransform hatTargetSlot;
        [SerializeField] private NameSayersHat breveHatUI;
        [SerializeField] private NameSayersHat macronHatUI;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;

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

        private int currentContrastIndex = 0;
        private int currentHatSwapIndex = 0;
        private bool isTransitioning = false;
        private Camera mainCamera;
        private Coroutine momoPulseCoroutine;

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
            StartActivity();
        }

        private void OnDisable()
        {
            DeactivateMascots();
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            for (int i = 0; i < vowelCharacterButtons.Length; i++)
            {
                int index = i;
                if (vowelCharacterButtons[i] != null)
                {
                    vowelCharacterButtons[i].onClick.AddListener(() => OnVowelCharacterClicked(index));
                }
            }

            if (startContrastPhaseButton != null)
            {
                startContrastPhaseButton.onClick.AddListener(StartContrastPhase);
            }

            if (shortHatButton != null)
            {
                shortHatButton.onClick.AddListener(() => OnContrastAnswer(false));
            }

            if (longHatButton != null)
            {
                longHatButton.onClick.AddListener(() => OnContrastAnswer(true));
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
            currentContrastIndex = 0;
            currentHatSwapIndex = 0;
            isTransitioning = false;

            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);

            ShowCharacterIntroPhase();
        }

        private void ShowCharacterIntroPhase()
        {
            if (characterIntroPanel != null) characterIntroPanel.SetActive(true);
            if (contrastPhasePanel != null) contrastPhasePanel.SetActive(false);
            if (hatSwapPhasePanel != null) hatSwapPhasePanel.SetActive(false);
            if (startContrastPhaseButton != null) startContrastPhaseButton.gameObject.SetActive(true);

            SetDialogue("Welcome to Long Vowel Lake! These five are the Name Sayers. They say their own names!");
            if (activityData != null && activityData.leoIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.leoIntroClip);
            }

            string[] vowelSymbols = new string[] { "ā", "ē", "ī", "ō", "ū" };
            for (int i = 0; i < vowelCharacterButtons.Length; i++)
            {
                if (vowelCharacterButtons[i] != null)
                {
                    vowelCharacterButtons[i].gameObject.SetActive(true);
                    if (vowelCharacterTexts[i] != null) vowelCharacterTexts[i].text = vowelSymbols[i];
                }
            }

            UpdateProgressUI(0.1f);
        }

        private void OnVowelCharacterClicked(int index)
        {
            if (isTransitioning || activityData == null || activityData.longVowels == null) return;
            if (index < 0 || index >= activityData.longVowels.Length) return;

            LongVowelItem item = activityData.longVowels[index];
            if (item == null) return;

            PlaySFX(activityData.starPopSfx);
            if (vowelCharacterButtons[index] != null)
            {
                TriggerWiggle(vowelCharacterButtons[index].GetComponent<RectTransform>());
            }

            SetDialogue($"My name is '{item.vowelName}'. {item.vowelSymbol} — {string.Join(", ", item.pictureWordNames)}!");
            if (item.nameVoiceClip != null)
            {
                PlayVoiceClipNonBlocking(item.nameVoiceClip);
            }

            for (int p = 0; p < 4; p++)
            {
                if (p < item.pictureWordNames.Length)
                {
                    if (pictureWordTexts != null && p < pictureWordTexts.Length && pictureWordTexts[p] != null)
                    {
                        pictureWordTexts[p].text = item.pictureWordNames[p];
                    }
                    if (pictureWordImages != null && p < pictureWordImages.Length && pictureWordImages[p] != null)
                    {
                        if (item.pictureWordSprites != null && p < item.pictureWordSprites.Length && item.pictureWordSprites[p] != null)
                        {
                            pictureWordImages[p].sprite = item.pictureWordSprites[p];
                            pictureWordImages[p].gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

        private void StartContrastPhase()
        {
            if (characterIntroPanel != null) characterIntroPanel.SetActive(false);
            if (contrastPhasePanel != null) contrastPhasePanel.SetActive(true);
            if (hatSwapPhasePanel != null) hatSwapPhasePanel.SetActive(false);

            SetDialogue("Remember the curvy hat? That was SHORT. This flat hat means LONG!");
            if (activityData != null && activityData.hatExplanationClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.hatExplanationClip);
            }

            LoadContrastRound(0);
        }

        private void LoadContrastRound(int index)
        {
            if (activityData == null || activityData.contrastPairs == null || index >= activityData.contrastPairs.Length)
            {
                StartHatSwapPhase();
                return;
            }

            currentContrastIndex = index;
            ShortLongContrastPair pair = activityData.contrastPairs[index];

            if (contrastWordText != null) contrastWordText.text = $"{pair.shortWord} / {pair.longWord}";
            if (contrastWordImage != null && pair.longWordSprite != null)
            {
                contrastWordImage.sprite = pair.longWordSprite;
                contrastWordImage.gameObject.SetActive(true);
            }

            SetDialogue($"Listen: {pair.shortWord} ... {pair.longWord}. Did the vowel say its SOUND, or its NAME?");
            if (pair.contrastPairClip != null)
            {
                PlayVoiceClipNonBlocking(pair.contrastPairClip);
            }

            UpdateProgressUI((0.2f + (index / 10f) * 0.4f));
        }

        private void OnContrastAnswer(bool tappedLongHat)
        {
            if (isTransitioning || activityData == null || currentContrastIndex >= activityData.contrastPairs.Length) return;

            ShortLongContrastPair pair = activityData.contrastPairs[currentContrastIndex];
            bool isCorrect = (tappedLongHat == pair.isLongCorrect);

            if (isCorrect)
            {
                StartCoroutine(HandleContrastCorrect(pair));
            }
            else
            {
                StartCoroutine(HandleContrastWrong(pair));
            }
        }

        private IEnumerator HandleContrastCorrect(ShortLongContrastPair pair)
        {
            isTransitioning = true;
            PlaySFX(activityData.correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue($"Yes! {pair.longWord.ToUpper()} — the vowel said its name. Flat hat!");
            yield return new WaitForSeconds(1.0f);

            currentContrastIndex++;
            isTransitioning = false;
            LoadContrastRound(currentContrastIndex);
        }

        private IEnumerator HandleContrastWrong(ShortLongContrastPair pair)
        {
            isTransitioning = true;
            PlaySFX(activityData.retryGentleSfx);

            SetDialogue($"Listen once more: {pair.shortWord} ... {pair.longWord}. {pair.longWord} says its name!");
            if (pair.contrastPairClip != null)
            {
                yield return PlayVoiceClip(pair.contrastPairClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            isTransitioning = false;
        }

        private void StartHatSwapPhase()
        {
            if (characterIntroPanel != null) characterIntroPanel.SetActive(false);
            if (contrastPhasePanel != null) contrastPhasePanel.SetActive(false);
            if (hatSwapPhasePanel != null) hatSwapPhasePanel.SetActive(true);

            if (breveHatUI != null) breveHatUI.SetupHat(this, false);
            if (macronHatUI != null) macronHatUI.SetupHat(this, true);

            LoadHatSwapRound(0);
        }

        private void LoadHatSwapRound(int index)
        {
            if (activityData == null || activityData.hatSwapRounds == null || index >= activityData.hatSwapRounds.Length)
            {
                CompleteStop1();
                return;
            }

            currentHatSwapIndex = index;
            HatSwapRound round = activityData.hatSwapRounds[index];

            if (hatSwapWordText != null) hatSwapWordText.text = round.wordText;
            if (hatSwapWordImage != null && round.wordSprite != null)
            {
                hatSwapWordImage.sprite = round.wordSprite;
                hatSwapWordImage.gameObject.SetActive(true);
            }

            if (breveHatUI != null) breveHatUI.ResetPosition();
            if (macronHatUI != null) macronHatUI.ResetPosition();

            SetDialogue($"Drag the right hat onto the vowel in '{round.wordText.ToUpper()}'!");
            if (round.wordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(round.wordAudioClip);
            }

            UpdateProgressUI((0.6f + (index / 5f) * 0.35f));
        }

        public void EvaluateHatDrop(NameSayersHat hat, PointerEventData eventData)
        {
            if (isTransitioning || hat == null || activityData == null || currentHatSwapIndex >= activityData.hatSwapRounds.Length) return;

            HatSwapRound round = activityData.hatSwapRounds[currentHatSwapIndex];
            bool targetHit = false;

            if (hatTargetSlot != null && eventData != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : mainCamera;
                targetHit = RectTransformUtility.RectangleContainsScreenPoint(hatTargetSlot, eventData.position, cam);
            }

            if (targetHit)
            {
                bool isCorrect = (hat.IsMacron == round.requiresMacron);
                if (isCorrect)
                {
                    StartCoroutine(HandleHatSwapCorrect(round));
                }
                else
                {
                    hat.ReturnToStartPosition();
                    StartCoroutine(HandleHatSwapWrong(round));
                }
            }
            else
            {
                hat.ReturnToStartPosition();
            }
        }

        private IEnumerator HandleHatSwapCorrect(HatSwapRound round)
        {
            isTransitioning = true;
            PlaySFX(activityData.correctChimeSfx);
            TriggerWiggleStarMeter();

            string hatName = round.requiresMacron ? "flat macron hat" : "curved breve hat";
            SetDialogue($"Perfect! '{round.wordText.ToUpper()}' gets the {hatName}!");
            yield return new WaitForSeconds(1.0f);

            currentHatSwapIndex++;
            isTransitioning = false;
            LoadHatSwapRound(currentHatSwapIndex);
        }

        private IEnumerator HandleHatSwapWrong(HatSwapRound round)
        {
            isTransitioning = true;
            PlaySFX(activityData.retryGentleSfx);

            string hatName = round.requiresMacron ? "flat hat (long sound)" : "curved hat (short sound)";
            SetDialogue($"Try again! '{round.wordText.ToUpper()}' needs the {hatName}!");
            yield return new WaitForSeconds(1.0f);

            isTransitioning = false;
        }

        private void CompleteStop1()
        {
            StartCoroutine(CompleteStop1Sequence());
        }

        private IEnumerator CompleteStop1Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData.correctChimeSfx);
            UpdateProgressUI(1.0f);

            SetDialogue("Short says the sound. Long says the name. You have both now!");
            if (activityData != null && activityData.leoClosingClip != null)
            {
                yield return PlayVoiceClip(activityData.leoClosingClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

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
