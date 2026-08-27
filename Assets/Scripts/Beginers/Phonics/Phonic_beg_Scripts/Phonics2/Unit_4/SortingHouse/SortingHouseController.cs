using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit4
{
    public class SortingHouseController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit4";
        [SerializeField] private string topicName = "SortingHouse";

        [Header("Data Reference")]
        [SerializeField] private SortingHouseData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Phase 1: Sorting UI (Drag & Drop)")]
        [SerializeField] private GameObject sortingPanel;
        [SerializeField] private SortingHouseCard activeSortingCardUI;
        [SerializeField] private SortingHouseLetterbox[] letterboxComponents = new SortingHouseLetterbox[3]; // 3 Letterbox Slots
        [SerializeField] private Button[] letterboxButtons = new Button[3]; // Fallback tap buttons
        [SerializeField] private TMP_Text[] letterboxTexts = new TMP_Text[3];
        [SerializeField] private Button startStarRoundButton;

        [Header("Phase 2: Tara Star Round UI")]
        [SerializeField] private GameObject starRoundPanel;
        [SerializeField] private TMP_Text starPromptTMP;
        [SerializeField] private Image starPromptImage;
        [SerializeField] private Button[] starChoiceButtons = new Button[3];
        [SerializeField] private TMP_Text[] starChoiceTexts = new TMP_Text[3];
        [SerializeField] private Image[] starChoiceImages = new Image[3];

        [Header("Phase 2 Round 5: Quick Sort Drag UI")]
        [SerializeField] private GameObject quickSortContainer;
        [SerializeField] private SortingHouseCard[] quickSortCardUIs = new SortingHouseCard[3];
        [SerializeField] private SortingHouseLetterbox[] quickSortLetterboxComponents = new SortingHouseLetterbox[3];
        [SerializeField] private Button[] quickSortLetterboxButtons = new Button[3];
        [SerializeField] private TMP_Text[] quickSortLetterboxTexts = new TMP_Text[3];

        [Header("Phase 2 Round 6: Vowel Song Recap UI")]
        [SerializeField] private GameObject vowelSongContainer;
        [SerializeField] private Button[] vowelSongButtons = new Button[5]; // ă, ĕ, ĭ, ŏ, ŭ
        [SerializeField] private TMP_Text[] vowelSongTexts = new TMP_Text[5];

        [Header("Button Feedback Colors")]
        [SerializeField] private Color correctColor = new Color(0.3f, 0.69f, 0.31f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.96f, 0.26f, 0.21f, 1f);

        [Header("Progress Ring UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;

        [Header("Mascots")]
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;
        [SerializeField] private GameObject taraMascotObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip starPopSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private Image badgeDisplayImage;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int sortingCardIndex = 0;
        private int starChallengeIndex = 0;
        private int totalStarChallenges = 6;
        private bool isStarRoundActive = false;
        private bool isTransitioning = false;
        private SortingWordCardItem currentSortingCard;
        private StarRoundUnit4Challenge currentStarChallenge;
        private int failAttempts = 0;
        private Coroutine momoPulseCoroutine;
        private Camera mainCamera;

        private int quickSortRemaining = 0;

        public string UnitID => unitID;
        public string TopicName => topicName;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            mainCamera = Camera.main;
            EnsureAudioSources();
            SetupButtonListeners();
            SetupLetterboxComponents();
        }

        private void Start()
        {
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

        private void SetupLetterboxComponents()
        {
            string[] labels = new string[] { "ă", "ĕ", "ĭ", "ŏ", "ŭ", "Not today!" };
            Color[] colors = new Color[]
            {
                new Color(0.95f, 0.35f, 0.35f), // ă Red/Pink
                new Color(0.35f, 0.75f, 0.95f), // ĕ Blue
                new Color(0.95f, 0.85f, 0.35f), // ĭ Yellow
                new Color(0.45f, 0.85f, 0.45f), // ŏ Green
                new Color(0.85f, 0.45f, 0.85f), // ŭ Purple
                new Color(0.65f, 0.65f, 0.65f)  // Not today Grey
            };

            for (int i = 0; i < letterboxComponents.Length; i++)
            {
                if (letterboxComponents[i] != null)
                {
                    letterboxComponents[i].SetupLetterbox(i, labels[i], colors[i]);
                }
            }
        }

        private void SetupButtonListeners()
        {
            if (startStarRoundButton != null) startStarRoundButton.onClick.AddListener(StartStarRound);

            for (int i = 0; i < letterboxButtons.Length; i++)
            {
                int index = i;
                if (letterboxButtons[i] != null)
                    letterboxButtons[i].onClick.AddListener(() => OnLetterboxSelected(index));
            }

            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                int index = i;
                if (starChoiceButtons[i] != null)
                    starChoiceButtons[i].onClick.AddListener(() => OnStarChoiceSelected(index));
            }

            for (int i = 0; i < quickSortLetterboxButtons.Length; i++)
            {
                int index = i;
                if (quickSortLetterboxButtons[i] != null)
                    quickSortLetterboxButtons[i].onClick.AddListener(() => OnQuickSortLetterboxSelected(index));
            }

            for (int i = 0; i < vowelSongButtons.Length; i++)
            {
                int index = i;
                if (vowelSongButtons[i] != null)
                    vowelSongButtons[i].onClick.AddListener(() => OnVowelSongButtonClicked(index));
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
            sortingCardIndex = 0;
            starChallengeIndex = 0;
            isStarRoundActive = false;
            isTransitioning = false;

            if (sortingPanel != null) sortingPanel.SetActive(true);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
            if (quickSortContainer != null) quickSortContainer.SetActive(false);
            if (vowelSongContainer != null) vowelSongContainer.SetActive(false);

            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);

            SetupLetterboxes();
            UpdateProgressUI(0f);
            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Post each word into the right letterbox. Listen to the MIDDLE sound!");

            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            LoadSortingCard(0);
            isTransitioning = false;
        }

        private readonly string[] allBoxLabels = new string[] { "ă", "ĕ", "ĭ", "ŏ", "ŭ", "Not today!" };
        private readonly Color[] allBoxColors = new Color[]
        {
            new Color(0.95f, 0.35f, 0.35f, 1f), // ă Red
            new Color(0.35f, 0.75f, 0.95f, 1f), // ĕ Blue
            new Color(0.95f, 0.85f, 0.35f, 1f), // ĭ Yellow
            new Color(0.45f, 0.85f, 0.45f, 1f), // ŏ Green
            new Color(0.85f, 0.45f, 0.85f, 1f), // ŭ Purple
            new Color(0.65f, 0.65f, 0.65f, 1f)  // Not today Grey
        };
        private List<int> currentActiveBoxIndices = new List<int>();

        private void SetupLetterboxes()
        {
            for (int s = 0; s < letterboxButtons.Length; s++)
            {
                if (letterboxButtons[s] != null)
                {
                    letterboxButtons[s].gameObject.SetActive(true);
                }
            }
        }

        private void DisplayThreeActiveLetterboxes(int correctBoxIndex)
        {
            currentActiveBoxIndices.Clear();
            currentActiveBoxIndices.Add(correctBoxIndex);

            List<int> candidateIndices = new List<int>();
            for (int i = 0; i < 6; i++)
            {
                if (i != correctBoxIndex) candidateIndices.Add(i);
            }

            int distractor1 = candidateIndices[(sortingCardIndex) % candidateIndices.Count];
            candidateIndices.Remove(distractor1);
            int distractor2 = candidateIndices[(sortingCardIndex * 3 + 1) % candidateIndices.Count];

            currentActiveBoxIndices.Add(distractor1);
            currentActiveBoxIndices.Add(distractor2);
            currentActiveBoxIndices.Sort(); // Order ă, ĕ, ĭ, ŏ, ŭ, Not today!

            // Configure the 3 physical letterboxes in the scene (slots 0, 1, 2)
            for (int s = 0; s < letterboxComponents.Length && s < 3; s++)
            {
                int targetBoxIdx = currentActiveBoxIndices[s];
                string label = allBoxLabels[targetBoxIdx];
                Color color = allBoxColors[targetBoxIdx];

                if (letterboxComponents[s] != null)
                {
                    letterboxComponents[s].SetupLetterbox(targetBoxIdx, label, color);
                    letterboxComponents[s].gameObject.SetActive(true);
                }

                if (letterboxButtons != null && s < letterboxButtons.Length && letterboxButtons[s] != null)
                {
                    letterboxButtons[s].gameObject.SetActive(true);
                }

                if (letterboxTexts != null && s < letterboxTexts.Length && letterboxTexts[s] != null)
                {
                    letterboxTexts[s].text = label;
                }
            }
        }

        private void LoadSortingCard(int index)
        {
            if (activityData == null || activityData.sortingCards == null || index >= activityData.sortingCards.Length)
            {
                if (activeSortingCardUI != null) activeSortingCardUI.gameObject.SetActive(false);
                if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(true);
                SetDialogue("Great job sorting all words! Tap 'Start Star Round' to continue with Tara! ⭐");
                return;
            }

            sortingCardIndex = index;
            failAttempts = 0;
            StopMomoPulse();
            if (momoHintObject != null) momoHintObject.SetActive(false);

            currentSortingCard = activityData.sortingCards[index];

            // Display only 3 active letterboxes at a time for this card
            DisplayThreeActiveLetterboxes(currentSortingCard.correctBoxIndex);

            if (activeSortingCardUI != null)
            {
                activeSortingCardUI.SetupCard(currentSortingCard, this);
            }

            SetDialogue($"Word card: '{currentSortingCard.wordName.ToUpper()}'! Drag it to the right letterbox!");
            SpeakCardAudio(currentSortingCard);

            UpdateProgressUI((float)index / 25f);
        }

        public void SpeakCardAudio(SortingWordCardItem item)
        {
            if (item == null) return;
            if (item.wordMiddleStretchedClip != null)
            {
                PlayVoiceClipNonBlocking(item.wordMiddleStretchedClip);
            }
            else if (item.wordNormalClip != null)
            {
                PlayVoiceClipNonBlocking(item.wordNormalClip);
            }
        }

        public void EvaluateCardDropFromDrag(SortingHouseCard card, PointerEventData eventData)
        {
            if (card == null || eventData == null) return;

            SortingHouseLetterbox targetBox = null;

            if (isStarRoundActive && currentStarChallenge != null && currentStarChallenge.challengeType == StarChallengeType.QuickSortDrag)
            {
                for (int i = 0; i < quickSortLetterboxComponents.Length; i++)
                {
                    if (quickSortLetterboxComponents[i] != null && quickSortLetterboxComponents[i].gameObject.activeInHierarchy && quickSortLetterboxComponents[i].ContainsScreenPoint(eventData.position, mainCamera))
                    {
                        targetBox = quickSortLetterboxComponents[i];
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < letterboxComponents.Length; i++)
                {
                    if (letterboxComponents[i] != null && letterboxComponents[i].gameObject.activeInHierarchy && letterboxComponents[i].ContainsScreenPoint(eventData.position, mainCamera))
                    {
                        targetBox = letterboxComponents[i];
                        break;
                    }
                }
            }

            if (targetBox != null)
            {
                card.OnDroppedOnLetterbox(targetBox);
            }
            else
            {
                card.ReturnToStartPosition();
            }
        }

        private void OnLetterboxSelected(int slotIndex)
        {
            if (isTransitioning || IsAudioPlaying() || currentSortingCard == null) return;
            if (slotIndex >= 0 && slotIndex < letterboxComponents.Length && letterboxComponents[slotIndex] != null)
            {
                int targetBoxIndex = letterboxComponents[slotIndex].BoxIndex;
                EvaluateCardDrop(activeSortingCardUI, targetBoxIndex);
            }
        }

        private void OnQuickSortLetterboxSelected(int slotIndex)
        {
            if (isTransitioning || IsAudioPlaying()) return;
            if (slotIndex >= 0 && slotIndex < quickSortLetterboxComponents.Length && quickSortLetterboxComponents[slotIndex] != null)
            {
                int targetBoxIndex = quickSortLetterboxComponents[slotIndex].BoxIndex;
                SortingHouseCard activeQuickCard = GetActiveUnsortedQuickCard();
                if (activeQuickCard != null)
                {
                    EvaluateCardDrop(activeQuickCard, targetBoxIndex);
                }
            }
        }

        private SortingHouseCard GetActiveUnsortedQuickCard()
        {
            if (quickSortCardUIs == null) return null;
            for (int i = 0; i < quickSortCardUIs.Length; i++)
            {
                if (quickSortCardUIs[i] != null && quickSortCardUIs[i].gameObject.activeInHierarchy && !quickSortCardUIs[i].IsDroppedCorrectly)
                {
                    return quickSortCardUIs[i];
                }
            }
            return null;
        }

        public void EvaluateCardDrop(SortingHouseCard cardUI, int boxIndex)
        {
            if (isTransitioning || cardUI == null || cardUI.CardData == null) return;

            bool isCorrect = (boxIndex == cardUI.CardData.correctBoxIndex);

            if (isCorrect)
            {
                StopMomoPulse();
                SortingHouseLetterbox targetSlot = null;
                SortingHouseLetterbox[] activeBoxes = (isStarRoundActive && currentStarChallenge != null && currentStarChallenge.challengeType == StarChallengeType.QuickSortDrag)
                    ? quickSortLetterboxComponents
                    : letterboxComponents;

                if (activeBoxes != null)
                {
                    for (int s = 0; s < activeBoxes.Length; s++)
                    {
                        if (activeBoxes[s] != null && activeBoxes[s].BoxIndex == boxIndex)
                        {
                            targetSlot = activeBoxes[s];
                            break;
                        }
                    }
                }

                if (targetSlot != null)
                {
                    cardUI.SetCorrectDrop(targetSlot.transform.position);
                }

                if (isStarRoundActive && currentStarChallenge != null && currentStarChallenge.challengeType == StarChallengeType.QuickSortDrag)
                {
                    StartCoroutine(HandleQuickSortCardCorrect());
                }
                else
                {
                    StartCoroutine(HandleSortingCorrect(boxIndex));
                }
            }
            else
            {
                failAttempts++;
                if (cardUI != null) cardUI.PlayWrongWobble();

                if (failAttempts >= 2 && momoHintObject != null)
                {
                    momoHintObject.SetActive(true);
                    SetDialogue("Momo says: Post it into the glowing letterbox!");
                    TriggerMomoSortingPulse();
                }
                StartCoroutine(HandleSortingWrong());
            }
        }

        private IEnumerator HandleQuickSortCardCorrect()
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            quickSortRemaining--;
            yield return new WaitForSeconds(0.5f);

            isTransitioning = false;

            if (quickSortRemaining <= 0)
            {
                if (quickSortContainer != null) quickSortContainer.SetActive(false);
                if (sortingPanel != null) sortingPanel.SetActive(false);
                StartCoroutine(HandleStarChoiceCorrect());
            }
        }

        private IEnumerator HandleSortingCorrect(int boxIndex)
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            if (currentSortingCard.isDistractor)
            {
                SetDialogue($"Careful! '{currentSortingCard.wordName.ToUpper()}' does not have any of our five short sounds. Great job putting it in 'Not today'!");
                if (activityData != null && activityData.distractorWarningClip != null)
                {
                    yield return PlayVoiceClip(activityData.distractorWarningClip);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }
            else
            {
                SetDialogue($"Yes! '{currentSortingCard.wordName.ToUpper()}' goes to the right letterbox!");
                yield return new WaitForSeconds(0.8f);
            }

            sortingCardIndex++;
            isTransitioning = false;

            int totalCardsCount = (activityData != null && activityData.sortingCards != null) ? activityData.sortingCards.Length : 25;
            if (sortingCardIndex < totalCardsCount)
            {
                LoadSortingCard(sortingCardIndex);
            }
            else
            {
                if (activeSortingCardUI != null) activeSortingCardUI.gameObject.SetActive(false);
                if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(true);
                SetDialogue("Great job sorting all words! Tap 'Start Star Round' to continue with Tara! ⭐");
            }
        }

        private IEnumerator HandleSortingWrong()
        {
            PlaySFX(retryGentleSfx);
            SpeakCardAudio(currentSortingCard);
            SetDialogue($"Listen again — '{currentSortingCard.wordName}'. Which sound is that?");
            yield return new WaitForSeconds(0.8f);
        }

        private void StartStarRound()
        {
            isStarRoundActive = true;
            starChallengeIndex = 0;

            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);
            if (sortingPanel != null) sortingPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(true);

            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(true);

            SetDialogue("My turn! Six quick challenges. Ready? Roar!");
            if (activityData != null && activityData.taraStarRoundOpenerClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.taraStarRoundOpenerClip);
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
            failAttempts = 0;
            StopMomoPulse();
            if (momoHintObject != null) momoHintObject.SetActive(false);

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

            SetDialogue(currentStarChallenge.questionPrompt);
            if (currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }

            // Handle round types
            if (currentStarChallenge.challengeType == StarChallengeType.QuickSortDrag)
            {
                SetupQuickSortRound();
            }
            else if (currentStarChallenge.challengeType == StarChallengeType.VowelSongRecap)
            {
                SetupVowelSongRound();
            }
            else
            {
                if (quickSortContainer != null) quickSortContainer.SetActive(false);
                if (vowelSongContainer != null) vowelSongContainer.SetActive(false);
                SetupStarChoices();
            }

            UpdateProgressUI((25f + index) / 31f);
        }

        private void SetupStarChoices()
        {
            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                if (i < currentStarChallenge.choices.Length)
                {
                    starChoiceButtons[i].gameObject.SetActive(true);
                    starChoiceButtons[i].transform.localScale = Vector3.one;

                    Image btnImg = starChoiceButtons[i].GetComponent<Image>();
                    if (btnImg != null) btnImg.color = Color.white;

                    if (starChoiceTexts[i] != null) starChoiceTexts[i].text = currentStarChallenge.choices[i];

                    if (starChoiceImages[i] != null && currentStarChallenge.choiceSprites != null && i < currentStarChallenge.choiceSprites.Length && currentStarChallenge.choiceSprites[i] != null)
                    {
                        starChoiceImages[i].sprite = currentStarChallenge.choiceSprites[i];
                        starChoiceImages[i].gameObject.SetActive(true);
                    }
                    else if (starChoiceImages[i] != null)
                    {
                        starChoiceImages[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    starChoiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void SetupQuickSortRound()
        {
            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                if (starChoiceButtons[i] != null) starChoiceButtons[i].gameObject.SetActive(false);
            }

            if (sortingPanel != null) sortingPanel.SetActive(false);
            if (quickSortContainer != null) quickSortContainer.SetActive(true);
            if (vowelSongContainer != null) vowelSongContainer.SetActive(false);

            // Configure the separate QuickSort Letterbox UI components (ă = 0, ĕ = 1, ĭ = 2)
            if (quickSortLetterboxComponents != null && quickSortLetterboxComponents.Length >= 3)
            {
                for (int s = 0; s < 3; s++)
                {
                    if (quickSortLetterboxComponents[s] != null)
                    {
                        quickSortLetterboxComponents[s].SetupLetterbox(s, allBoxLabels[s], allBoxColors[s]);
                        quickSortLetterboxComponents[s].gameObject.SetActive(true);
                    }
                    if (quickSortLetterboxTexts != null && s < quickSortLetterboxTexts.Length && quickSortLetterboxTexts[s] != null)
                    {
                        quickSortLetterboxTexts[s].text = allBoxLabels[s];
                    }
                }
            }

            if (currentStarChallenge.quickDragCards != null && currentStarChallenge.quickDragCards.Length > 0)
            {
                quickSortRemaining = currentStarChallenge.quickDragCards.Length;
                for (int i = 0; i < quickSortCardUIs.Length; i++)
                {
                    if (i < currentStarChallenge.quickDragCards.Length && quickSortCardUIs[i] != null)
                    {
                        quickSortCardUIs[i].SetupCard(currentStarChallenge.quickDragCards[i], this);
                    }
                    else if (quickSortCardUIs[i] != null)
                    {
                        quickSortCardUIs[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private HashSet<int> tappedVowelsInSongRound = new HashSet<int>();

        private void SetupVowelSongRound()
        {
            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                if (starChoiceButtons[i] != null) starChoiceButtons[i].gameObject.SetActive(false);
            }

            if (quickSortContainer != null) quickSortContainer.SetActive(false);
            if (vowelSongContainer != null) vowelSongContainer.SetActive(true);

            tappedVowelsInSongRound.Clear();

            string[] vowels = new string[] { "ă", "ĕ", "ĭ", "ŏ", "ŭ" };
            for (int i = 0; i < vowelSongButtons.Length; i++)
            {
                if (vowelSongButtons[i] != null)
                {
                    vowelSongButtons[i].gameObject.SetActive(true);
                    vowelSongButtons[i].transform.localScale = Vector3.one;

                    Image btnImg = vowelSongButtons[i].GetComponent<Image>();
                    if (btnImg != null) btnImg.color = Color.white;

                    if (vowelSongTexts[i] != null) vowelSongTexts[i].text = vowels[i];
                }
            }

            SetDialogue("Tap all 5 short vowels to sing along with Tara! ă, ĕ, ĭ, ŏ, ŭ!");
            if (activityData != null && activityData.vowelSongAudioClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.vowelSongAudioClip);
            }
        }

        private void OnVowelSongButtonClicked(int index)
        {
            if (isTransitioning) return;

            PlaySFX(starPopSfx);
            if (index >= 0 && index < vowelSongButtons.Length && vowelSongButtons[index] != null)
            {
                TriggerWiggle(vowelSongButtons[index].GetComponent<RectTransform>());
                Image btnImg = vowelSongButtons[index].GetComponent<Image>();
                if (btnImg != null) btnImg.color = correctColor;
            }

            tappedVowelsInSongRound.Add(index);

            string[] vowelNames = new string[] { "Short A ă!", "Short E ĕ!", "Short I ĭ!", "Short O ŏ!", "Short U ŭ!" };
            if (index >= 0 && index < vowelNames.Length)
            {
                SetDialogue(vowelNames[index]);
            }

            if (tappedVowelsInSongRound.Count >= 5)
            {
                StartCoroutine(FinishVowelSongSequence());
            }
        }

        private IEnumerator FinishVowelSongSequence()
        {
            isTransitioning = true;
            yield return new WaitForSeconds(0.8f);
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue("Awesome! You sang all five short vowels with Tara!");
            yield return new WaitForSeconds(1.0f);

            isTransitioning = false;
            StartCoroutine(CompleteStarRoundSequence());
        }

        private void OnStarChoiceSelected(int choiceIndex)
        {
            if (isTransitioning || IsAudioPlaying() || currentStarChallenge == null) return;

            bool isCorrect = (choiceIndex == currentStarChallenge.correctChoiceIndex);
            Button tappedBtn = (choiceIndex >= 0 && choiceIndex < starChoiceButtons.Length) ? starChoiceButtons[choiceIndex] : null;

            if (tappedBtn != null)
            {
                TriggerWiggle(tappedBtn.GetComponent<RectTransform>());
                Image btnImg = tappedBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isCorrect ? correctColor : wrongColor;
                    if (!isCorrect) StartCoroutine(ResetButtonColor(btnImg, 0.8f));
                }
            }

            if (isCorrect)
            {
                StopMomoPulse();
                StartCoroutine(HandleStarChoiceCorrect());
            }
            else
            {
                failAttempts++;
                if (failAttempts >= 2 && momoHintObject != null)
                {
                    momoHintObject.SetActive(true);
                    SetDialogue("Momo says: Tap the glowing answer!");
                    TriggerMomoStarChoicePulse();
                }
                StartCoroutine(HandleStarChoiceWrong());
            }
        }

        private IEnumerator ResetButtonColor(Image targetImage, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (targetImage != null) targetImage.color = Color.white;
        }

        private IEnumerator HandleStarChoiceCorrect()
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue("Roar! You got it right!");
            yield return new WaitForSeconds(0.8f);

            starChallengeIndex++;
            isTransitioning = false;

            if (starChallengeIndex < totalStarChallenges && starChallengeIndex < activityData.starChallenges.Length)
            {
                LoadStarChallenge(starChallengeIndex);
            }
            else
            {
                StartCoroutine(CompleteStarRoundSequence());
            }
        }

        private IEnumerator HandleStarChoiceWrong()
        {
            PlaySFX(retryGentleSfx);
            SetDialogue("Almost! Try again.");
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator CompleteStarRoundSequence()
        {
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
            UpdateProgressUI(1f);

            SetDialogue("You can hear every short vowel. You are a SHORT VOWEL STAR!");
            if (activityData != null && activityData.shortVowelStarBadgeVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.shortVowelStarBadgeVoiceClip);
            }

            if (activityData != null && activityData.shortVowelStarBadgeSprite != null && badgeDisplayImage != null)
            {
                badgeDisplayImage.sprite = activityData.shortVowelStarBadgeSprite;
                badgeDisplayImage.gameObject.SetActive(true);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            PlaySFX(starPopSfx);
            yield return new WaitForSeconds(0.5f);

            if (activityData != null && activityData.unit5UnlockVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.unit5UnlockVoiceClip);
            }

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.SetActive(true);
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            StopMomoPulse();
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
        }

        private void TriggerMomoSortingPulse()
        {
            StopMomoPulse();
            SortingHouseLetterbox[] activeBoxes = (isStarRoundActive && currentStarChallenge != null && currentStarChallenge.challengeType == StarChallengeType.QuickSortDrag)
                ? quickSortLetterboxComponents
                : letterboxComponents;

            if (activeBoxes != null)
            {
                for (int s = 0; s < activeBoxes.Length; s++)
                {
                    if (activeBoxes[s] != null && activeBoxes[s].gameObject.activeInHierarchy && activeBoxes[s].BoxIndex == currentSortingCard.correctBoxIndex)
                    {
                        momoPulseCoroutine = StartCoroutine(PulseCorrectAnswerLoop(activeBoxes[s].GetComponent<RectTransform>()));
                        break;
                    }
                }
            }
        }

        private void TriggerMomoStarChoicePulse()
        {
            StopMomoPulse();
            if (currentStarChallenge != null && currentStarChallenge.correctChoiceIndex >= 0 && currentStarChallenge.correctChoiceIndex < starChoiceButtons.Length)
            {
                Button correctBtn = starChoiceButtons[currentStarChallenge.correctChoiceIndex];
                if (correctBtn != null)
                {
                    momoPulseCoroutine = StartCoroutine(PulseCorrectAnswerLoop(correctBtn.GetComponent<RectTransform>()));
                }
            }
        }

        private void StopMomoPulse()
        {
            if (momoPulseCoroutine != null)
            {
                StopCoroutine(momoPulseCoroutine);
                momoPulseCoroutine = null;
            }

            if (letterboxButtons != null)
            {
                for (int i = 0; i < letterboxButtons.Length; i++)
                {
                    if (letterboxButtons[i] != null) letterboxButtons[i].transform.localScale = Vector3.one;
                }
            }

            if (starChoiceButtons != null)
            {
                for (int i = 0; i < starChoiceButtons.Length; i++)
                {
                    if (starChoiceButtons[i] != null) starChoiceButtons[i].transform.localScale = Vector3.one;
                }
            }
        }

        private IEnumerator PulseCorrectAnswerLoop(RectTransform targetRect)
        {
            if (targetRect == null) yield break;
            Vector3 baseScale = Vector3.one;
            Vector3 maxScale = Vector3.one * 1.15f;
            float pulseSpeed = 4f;

            while (true)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                targetRect.localScale = Vector3.Lerp(baseScale, maxScale, t);
                yield return null;
            }
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
