using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;
using EngSnap.Phonics2.Unit8;

namespace EngSnap.Phonics2.Unit9
{
    public class SentenceStreetController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit9";
        [SerializeField] private string topicName = "SentenceStreet";

        [Header("Data Asset")]
        [SerializeField] private SentenceStreetData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Dialogue / Subtitle UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Sight Word Pocket UI")]
        [SerializeField] private SightWordPocketUI sightWordPocketUI;
        [SerializeField] private GameObject pocketIntroPanel;
        [SerializeField] private TMP_Text introCardTMP;
        [SerializeField] private Button introCardButton;

        [Header("Sentence Strip UI")]
        [SerializeField] private SentenceStripUI sentenceStripUI;
        [SerializeField] private Button readWholeButton;
        [SerializeField] private Button nextSentenceButton;

        [Header("Optional Slide Bar for Decodable Words")]
        [SerializeField] private GameObject decodableSlideBarContainer;
        [SerializeField] private BlendSlideBar decodableSlideBar;
        [SerializeField] private TMP_Text[] decodableLetterTexts = new TMP_Text[3];

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

        private int currentSentenceIndex = 0;
        private int totalSentences = 6;
        private int tappedWordCount = 0;
        private bool isTransitioning = false;
        private bool hasInitialized = false;
        private bool hasCompletedPocketIntro = false;
        private int currentPocketTargetIndex = 0;
        private bool isWaitingForPocketTaps = false;
        private SentenceItem currentSentence;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();

            if (sentenceStripUI != null)
            {
                sentenceStripUI.OnWordTapped += HandleSentenceWordTapped;
            }

            if (sightWordPocketUI != null)
            {
                sightWordPocketUI.OnCardIndexTapped += HandlePocketCardTapped;
            }
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

        private void OnDestroy()
        {
            if (sentenceStripUI != null)
            {
                sentenceStripUI.OnWordTapped -= HandleSentenceWordTapped;
            }

            if (sightWordPocketUI != null)
            {
                sightWordPocketUI.OnCardIndexTapped -= HandlePocketCardTapped;
            }
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
                activityData = Resources.Load<SentenceStreetData>("Phonics2/Unit9/SentenceStreetData_Unit9");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<SentenceStreetData>();
                activityData.PopulateDefaultData();
            }
        }

        private void SetupButtonListeners()
        {
            if (readWholeButton != null)
            {
                readWholeButton.onClick.RemoveAllListeners();
                readWholeButton.onClick.AddListener(OnReadWholeClicked);
            }

            if (nextSentenceButton != null)
            {
                nextSentenceButton.onClick.RemoveAllListeners();
                nextSentenceButton.onClick.AddListener(OnNextSentenceClicked);
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
            currentSentenceIndex = 0;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (nextSentenceButton != null) nextSentenceButton.gameObject.SetActive(false);
            if (readWholeButton != null) readWholeButton.gameObject.SetActive(false);
            if (decodableSlideBarContainer != null) decodableSlideBarContainer.SetActive(false);

            if (leoMascotObject != null) leoMascotObject.SetActive(true);

            // Hide Sentence Strip initially until pocket words are tapped left-to-right
            if (sentenceStripUI != null)
            {
                sentenceStripUI.SetVisible(hasCompletedPocketIntro);
            }

            if (!hasCompletedPocketIntro)
            {
                StartCoroutine(RunPocketIntroSequence());
            }
            else
            {
                LoadSentence(0);
            }
        }

        private IEnumerator RunPocketIntroSequence()
        {
            isTransitioning = true;
            if (sentenceStripUI != null) sentenceStripUI.SetVisible(false);
            if (pocketIntroPanel != null) pocketIntroPanel.SetActive(true);
            if (sightWordPocketUI != null)
            {
                sightWordPocketUI.ClearPocket();
                sightWordPocketUI.SetPocketOpen(true);
            }

            SetDialogue("Six more pocket words today. These ones you just know — no sounding out!");
            if (activityData != null && activityData.pocketIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.pocketIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            // 1. Introduce 6 sight words into pocket
            if (activityData != null && activityData.sightWords != null)
            {
                for (int i = 0; i < activityData.sightWords.Length; i++)
                {
                    var card = activityData.sightWords[i];
                    if (introCardTMP != null) introCardTMP.text = card.word;

                    SetDialogue($"'{card.word}' — pop it into your pocket!");

                    if (card.introScriptClip != null)
                    {
                        yield return PlayVoiceClip(card.introScriptClip);
                    }
                    else if (card.wordAudioClip != null)
                    {
                        yield return PlayVoiceClip(card.wordAudioClip);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.8f);
                    }

                    if (sightWordPocketUI != null)
                    {
                        yield return StartCoroutine(sightWordPocketUI.AnimateCardIntoPocket(card));
                    }
                }
            }

            if (pocketIntroPanel != null) pocketIntroPanel.SetActive(false);

            // 2. Guided Left-to-Right Pocket Tap Practice
            SetDialogue("Now tap each word in your pocket from left to right!");
            isWaitingForPocketTaps = true;
            currentPocketTargetIndex = 0;

            int totalPocketWords = activityData != null && activityData.sightWords != null ? activityData.sightWords.Length : 6;
            while (currentPocketTargetIndex < totalPocketWords)
            {
                if (sightWordPocketUI != null)
                {
                    sightWordPocketUI.HighlightCard(currentPocketTargetIndex, true);
                }
                yield return new WaitForSeconds(0.4f);
            }

            isWaitingForPocketTaps = false;
            hasCompletedPocketIntro = true;

            SetDialogue("Great! Your pocket is ready.");
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            yield return new WaitForSeconds(0.8f);

            // 3. Reveal the Sentence Strip!
            if (sentenceStripUI != null)
            {
                sentenceStripUI.SetVisible(true);
            }

            SetDialogue("Here comes your first sentence. Tap each word and read it!");
            if (activityData != null && activityData.sentenceIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.sentenceIntroClip);
            }

            isTransitioning = false;
            LoadSentence(0);
        }

        private void HandlePocketCardTapped(int index, string word)
        {
            if (isWaitingForPocketTaps)
            {
                if (index == currentPocketTargetIndex)
                {
                    currentPocketTargetIndex++;
                    PlaySFX(activityData != null ? activityData.wordTapSfx : null);
                }
                else if (index > currentPocketTargetIndex)
                {
                    currentPocketTargetIndex = Mathf.Max(currentPocketTargetIndex, index + 1);
                }
            }
        }

        private void LoadSentence(int index)
        {
            if (activityData == null || activityData.sentences == null || index >= activityData.sentences.Length)
            {
                CompleteStop3();
                return;
            }

            StartCoroutine(LoadSentenceRoutine(index));
        }

        private IEnumerator LoadSentenceRoutine(int index)
        {
            isTransitioning = true;
            currentSentenceIndex = index;
            currentSentence = activityData.sentences[index];
            tappedWordCount = 0;

            if (nextSentenceButton != null) nextSentenceButton.gameObject.SetActive(false);
            if (readWholeButton != null) readWholeButton.gameObject.SetActive(false);
            if (decodableSlideBarContainer != null) decodableSlideBarContainer.SetActive(false);

            if (sentenceStripUI != null)
            {
                sentenceStripUI.SetVisible(true);
                sentenceStripUI.DisplaySentence(currentSentence, false, true);
            }

            SetDialogue($"Sentence {index + 1}: Tap each word from left to right to read it!");
            UpdateProgressUI((float)index / totalSentences);

            isTransitioning = false;
            yield return null;
        }

        private void HandleSentenceWordTapped(int index, SentenceWordToken token)
        {
            if (isTransitioning || token == null) return;

            PlaySFX(activityData != null ? activityData.wordTapSfx : null);
            SetDialogue(token.wordText);

            if (token.wordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(token.wordAudioClip);
            }

            // Reveal and highlight the next word from left to right
            if (sentenceStripUI != null && currentSentence != null && currentSentence.tokens != null)
            {
                if (index + 1 < currentSentence.tokens.Length)
                {
                    sentenceStripUI.RevealWord(index + 1);
                }
            }

            tappedWordCount++;

            // If tapped through all words in the sentence, enable Read Whole pass!
            if (currentSentence != null && currentSentence.tokens != null && tappedWordCount >= currentSentence.tokens.Length)
            {
                if (readWholeButton != null) readWholeButton.gameObject.SetActive(true);
                SetDialogue("Now let us read it all together, nice and smooth! Tap Read Whole.");
                if (activityData != null && activityData.readWholeInvitationClip != null)
                {
                    PlayVoiceClipNonBlocking(activityData.readWholeInvitationClip);
                }
            }
        }

        private void OnReadWholeClicked()
        {
            if (isTransitioning || currentSentence == null) return;
            StartCoroutine(RunReadWholeRoutine());
        }

        private IEnumerator RunReadWholeRoutine()
        {
            isTransitioning = true;
            if (readWholeButton != null) readWholeButton.gameObject.SetActive(false);

            // Reveal noun illustrations above nouns
            if (sentenceStripUI != null)
            {
                sentenceStripUI.RevealAllNounPictures();
            }

            SetDialogue(currentSentence.fullSentence);

            // Play whole sentence audio while animating left-to-right highlight
            if (currentSentence.wholeSentenceAudio != null)
            {
                PlayVoiceClipNonBlocking(currentSentence.wholeSentenceAudio);
            }

            if (sentenceStripUI != null)
            {
                yield return StartCoroutine(sentenceStripUI.AnimateReadAlongHighlight(0.55f));
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            PlaySFX(activityData != null ? activityData.sentenceCompleteSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            SetDialogue("You read a SENTENCE. A whole sentence, by yourself!");
            if (activityData != null && activityData.sentenceSuccessClip != null)
            {
                yield return PlayVoiceClip(activityData.sentenceSuccessClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            if (nextSentenceButton != null)
            {
                nextSentenceButton.gameObject.SetActive(true);
            }
            else
            {
                OnNextSentenceClicked();
            }

            isTransitioning = false;
        }

        private void OnNextSentenceClicked()
        {
            if (isTransitioning) return;
            currentSentenceIndex++;
            LoadSentence(currentSentenceIndex);
        }

        private void CompleteStop3()
        {
            StartCoroutine(CompleteStop3Sequence());
        }

        private IEnumerator CompleteStop3Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("Six whole sentences conquered! Get ready for Story Time!");
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
