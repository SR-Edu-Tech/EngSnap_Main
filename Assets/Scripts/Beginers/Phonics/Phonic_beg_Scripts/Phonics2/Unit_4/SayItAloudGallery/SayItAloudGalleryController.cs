using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit4
{
    public class SayItAloudGalleryController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit4";
        [SerializeField] private string topicName = "SayItAloudGallery";

        [Header("Data Reference")]
        [SerializeField] private SayItAloudGalleryData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Phase 1: Room Selector UI")]
        [SerializeField] private GameObject roomSelectorPanel;
        [SerializeField] private Button[] roomSelectButtons = new Button[5]; // ă, ĕ, ĭ, ŏ, ŭ
        [SerializeField] private TMP_Text[] roomSelectTexts = new TMP_Text[5];
        [SerializeField] private GameObject[] roomCompletedGlows = new GameObject[5]; // Glowing light indicators for completed rooms!

        [Header("Phase 2: Gallery Room UI")]
        [SerializeField] private GameObject galleryRoomPanel;
        [SerializeField] private GameObject pictureWallSection;
        [SerializeField] private GameObject wordWallRhymeSection;
        [SerializeField] private Button backToSelectorButton;
        [SerializeField] private Button nextToRhymesButton;
        [SerializeField] private Button nextToRoomSelectorButton;
        [SerializeField] private Button startEchoRoundButton;

        [Header("Picture Wall UI (8 Items)")]
        [SerializeField] private Button[] pictureWallButtons = new Button[8];
        [SerializeField] private Image[] pictureWallImages = new Image[8];
        [SerializeField] private TMP_Text[] pictureWallTexts = new TMP_Text[8];

        [Header("Word Wall Rhyme Families UI")]
        [SerializeField] private TMP_Text[] rhymeFamilyNameTexts;
        [SerializeField] private TMP_Text[] rhymeFamilyWordListTexts;
        [SerializeField] private Button[] rhymeFamilyRunButtons;

        [Header("Phase 3: Echo Round UI")]
        [SerializeField] private GameObject echoRoundPanel;
        [SerializeField] private TMP_Text echoPromptTMP;
        [SerializeField] private Image echoPromptImage;
        [SerializeField] private Button repeatSpeakButton;
        [SerializeField] private Button[] echoChoiceButtons; // Matching choice buttons for Echo Round
        [SerializeField] private TMP_Text[] echoChoiceTexts;
        [SerializeField] private Image[] echoChoiceImages;

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
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentRoomIndex = 0;
        private int echoRoundIndex = 0;
        private int totalEchoRounds = 5;
        private bool isEchoActive = false;
        private bool isTransitioning = false;
        private bool[] pictureStretchedState = new bool[8];
        private bool[] completedRooms = new bool[5];

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

        private void SetupButtonListeners()
        {
            if (backToSelectorButton != null)
            {
                backToSelectorButton.onClick.RemoveAllListeners();
                backToSelectorButton.onClick.AddListener(OnBackToSelectorClicked);
            }
            if (nextToRhymesButton != null)
            {
                nextToRhymesButton.onClick.RemoveAllListeners();
                nextToRhymesButton.onClick.AddListener(OnNextToRhymesClicked);
                nextToRhymesButton.gameObject.SetActive(true);
                nextToRhymesButton.interactable = true;
            }
            if (nextToRoomSelectorButton != null)
            {
                nextToRoomSelectorButton.onClick.RemoveAllListeners();
                nextToRoomSelectorButton.onClick.AddListener(OnNextToRoomSelectorClicked);
                nextToRoomSelectorButton.gameObject.SetActive(true);
                nextToRoomSelectorButton.interactable = true;
            }
            if (startEchoRoundButton != null)
            {
                startEchoRoundButton.onClick.RemoveAllListeners();
                startEchoRoundButton.onClick.AddListener(StartEchoRound);
            }
            if (repeatSpeakButton != null)
            {
                repeatSpeakButton.onClick.RemoveAllListeners();
                repeatSpeakButton.onClick.AddListener(OnEchoRepeatTapped);
            }

            if (echoChoiceButtons != null)
            {
                for (int i = 0; i < echoChoiceButtons.Length; i++)
                {
                    int index = i;
                    if (echoChoiceButtons[i] != null)
                    {
                        echoChoiceButtons[i].onClick.RemoveAllListeners();
                        echoChoiceButtons[i].onClick.AddListener(() => OnEchoChoiceSelected(index));
                    }
                }
            }

            for (int i = 0; i < roomSelectButtons.Length; i++)
            {
                int index = i;
                if (roomSelectButtons[i] != null)
                {
                    roomSelectButtons[i].onClick.RemoveAllListeners();
                    roomSelectButtons[i].onClick.AddListener(() => OpenGalleryRoom(index));
                }
            }

            for (int i = 0; i < pictureWallButtons.Length; i++)
            {
                int index = i;
                if (pictureWallButtons[i] != null)
                {
                    pictureWallButtons[i].onClick.RemoveAllListeners();
                    pictureWallButtons[i].onClick.AddListener(() => OnPictureWallTapped(index));
                }
            }

            if (rhymeFamilyRunButtons != null)
            {
                for (int i = 0; i < rhymeFamilyRunButtons.Length; i++)
                {
                    int index = i;
                    if (rhymeFamilyRunButtons[i] != null)
                    {
                        rhymeFamilyRunButtons[i].onClick.RemoveAllListeners();
                        rhymeFamilyRunButtons[i].onClick.AddListener(() => OnRhymeFamilyRunTapped(index));
                    }
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
            currentRoomIndex = 0;
            echoRoundIndex = 0;
            isEchoActive = false;
            isTransitioning = false;

            EnsureAudioSources();
            SetupButtonListeners();
            EnsureNavigationButtonsActive();
            ShowRoomSelector();
            UpdateProgressUI(0f);
            StartCoroutine(PlayIntroSequence());
        }

        public void EnsureNavigationButtonsActive()
        {
            if (nextToRhymesButton != null)
            {
                nextToRhymesButton.gameObject.SetActive(true);
                nextToRhymesButton.interactable = true;
            }
            if (nextToRoomSelectorButton != null)
            {
                nextToRoomSelectorButton.gameObject.SetActive(true);
                nextToRoomSelectorButton.interactable = true;
            }
        }

        private IEnumerator PlayIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Welcome to the Say It Aloud Gallery! Tap a room to explore its short vowel words.");

            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
        }

        public void OnNextToRhymesClicked()
        {
            StopAllAudio();
            isTransitioning = false;

            if (pictureWallSection != null) pictureWallSection.SetActive(false);
            if (wordWallRhymeSection != null)
            {
                wordWallRhymeSection.SetActive(true);
                wordWallRhymeSection.transform.SetAsLastSibling();

                CanvasGroup cg = wordWallRhymeSection.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }

                foreach (Transform child in wordWallRhymeSection.transform)
                {
                    if (child != null) child.gameObject.SetActive(true);
                }
            }

            EnsureNavigationButtonsActive();

            if (activityData != null && activityData.galleryRooms != null && currentRoomIndex >= 0 && currentRoomIndex < activityData.galleryRooms.Length)
            {
                SetupWordWall(activityData.galleryRooms[currentRoomIndex]);
            }

            SetDialogue("Listen to the Rhyme Word Families!");
        }

        public void OnNextToRoomSelectorClicked()
        {
            if (wordWallRhymeSection != null) wordWallRhymeSection.SetActive(false);
            if (pictureWallSection != null) pictureWallSection.SetActive(true);
            if (galleryRoomPanel != null) galleryRoomPanel.SetActive(true);

            if (currentRoomIndex >= 0 && currentRoomIndex < completedRooms.Length)
            {
                completedRooms[currentRoomIndex] = true;
            }
            ShowRoomSelector();
        }

        public void OnBackToSelectorClicked()
        {
            if (wordWallRhymeSection != null) wordWallRhymeSection.SetActive(false);
            if (pictureWallSection != null) pictureWallSection.SetActive(true);
            if (galleryRoomPanel != null) galleryRoomPanel.SetActive(true);

            if (currentRoomIndex >= 0 && currentRoomIndex < completedRooms.Length)
            {
                completedRooms[currentRoomIndex] = true;
            }
            ShowRoomSelector();
        }

        private void ShowRoomSelector()
        {
            isEchoActive = false;
            if (wordWallRhymeSection != null) wordWallRhymeSection.SetActive(false);
            if (pictureWallSection != null) pictureWallSection.SetActive(true);
            if (roomSelectorPanel != null) roomSelectorPanel.SetActive(true);
            if (galleryRoomPanel != null) galleryRoomPanel.SetActive(false);
            if (echoRoundPanel != null) echoRoundPanel.SetActive(false);

            SetupRoomSelectButtons();
        }

        private void SetupRoomSelectButtons()
        {
            string[] vowelLabels = new string[] { "ă", "ĕ", "ĭ", "ŏ", "ŭ" };
            int finishedCount = 0;

            for (int i = 0; i < roomSelectButtons.Length; i++)
            {
                if (roomSelectButtons[i] != null)
                {
                    roomSelectButtons[i].gameObject.SetActive(true);
                    roomSelectButtons[i].interactable = true;
                    if (roomSelectTexts != null && i < roomSelectTexts.Length && roomSelectTexts[i] != null)
                        roomSelectTexts[i].text = vowelLabels[i];
                    if (roomCompletedGlows != null && i < roomCompletedGlows.Length && roomCompletedGlows[i] != null)
                    {
                        roomCompletedGlows[i].SetActive(completedRooms[i]);
                    }
                }
                if (i < completedRooms.Length && completedRooms[i]) finishedCount++;
            }

            // Show StartEchoRoundButton ONLY after ALL roomSelectButtons are clicked!
            if (startEchoRoundButton != null)
            {
                bool allClicked = (roomSelectButtons.Length > 0 && finishedCount >= roomSelectButtons.Length);
                startEchoRoundButton.gameObject.SetActive(allClicked);
                if (allClicked)
                {
                    startEchoRoundButton.interactable = true;
                    SetDialogue("All rooms explored! Tap Echo Practice to play Leo's game! ⭐");
                }
            }
        }

        public void OpenGalleryRoom(int roomIdx)
        {
            if (activityData == null || activityData.galleryRooms == null || roomIdx >= activityData.galleryRooms.Length) return;

            currentRoomIndex = roomIdx;
            if (roomIdx >= 0 && roomIdx < completedRooms.Length)
            {
                completedRooms[roomIdx] = true;
            }
            isEchoActive = false;

            if (roomSelectButtons != null && roomIdx < roomSelectButtons.Length && roomSelectButtons[roomIdx] != null)
            {
                StartCoroutine(WiggleRect(roomSelectButtons[roomIdx].GetComponent<RectTransform>(), 0.35f, 8f));
            }

            if (roomCompletedGlows != null && roomIdx < roomCompletedGlows.Length && roomCompletedGlows[roomIdx] != null)
            {
                roomCompletedGlows[roomIdx].SetActive(true);
            }

            if (roomSelectorPanel != null) roomSelectorPanel.SetActive(false);
            if (galleryRoomPanel != null) galleryRoomPanel.SetActive(true);
            if (echoRoundPanel != null) echoRoundPanel.SetActive(false);

            EnsureNavigationButtonsActive();

            if (pictureWallSection != null)
            {
                pictureWallSection.SetActive(true);
                pictureWallSection.transform.SetAsLastSibling();
                CanvasGroup cg = pictureWallSection.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }
            if (wordWallRhymeSection != null) wordWallRhymeSection.SetActive(false);

            GalleryRoomData room = activityData.galleryRooms[roomIdx];

            SetDialogue($"Welcome to {room.roomTitle}! Tap any picture to hear its sound.");
            if (room.roomWelcomeClip != null) PlayVoiceClipNonBlocking(room.roomWelcomeClip);

            SetupPictureWall(room);
            SetupWordWall(room);
        }

        private void SetupPictureWall(GalleryRoomData room)
        {
            for (int i = 0; i < pictureWallButtons.Length; i++)
            {
                pictureStretchedState[i] = false;
                if (i < room.pictureWallItems.Length)
                {
                    pictureWallButtons[i].gameObject.SetActive(true);
                    GalleryPictureItem item = room.pictureWallItems[i];

                    if (pictureWallTexts[i] != null) pictureWallTexts[i].text = item.wordName;
                    if (pictureWallImages[i] != null && item.pictureSprite != null)
                        pictureWallImages[i].sprite = item.pictureSprite;
                }
                else
                {
                    pictureWallButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void SetupWordWall(GalleryRoomData room)
        {
            if (room == null || room.rhymeFamilies == null) return;

            if (rhymeFamilyNameTexts != null)
            {
                for (int i = 0; i < rhymeFamilyNameTexts.Length; i++)
                {
                    if (rhymeFamilyNameTexts[i] == null) continue;

                    if (i < room.rhymeFamilies.Length && room.rhymeFamilies[i] != null)
                    {
                        RhymeFamilyGroup fam = room.rhymeFamilies[i];
                        rhymeFamilyNameTexts[i].gameObject.SetActive(true);
                        rhymeFamilyNameTexts[i].text = fam.familyName;

                        if (rhymeFamilyWordListTexts != null && i < rhymeFamilyWordListTexts.Length && rhymeFamilyWordListTexts[i] != null)
                        {
                            rhymeFamilyWordListTexts[i].gameObject.SetActive(true);
                            rhymeFamilyWordListTexts[i].text = string.Join(" · ", fam.words);
                        }

                        if (rhymeFamilyRunButtons != null && i < rhymeFamilyRunButtons.Length && rhymeFamilyRunButtons[i] != null)
                        {
                            rhymeFamilyRunButtons[i].gameObject.SetActive(true);
                            rhymeFamilyRunButtons[i].interactable = true;
                        }
                    }
                    else
                    {
                        rhymeFamilyNameTexts[i].gameObject.SetActive(false);
                        if (rhymeFamilyWordListTexts != null && i < rhymeFamilyWordListTexts.Length && rhymeFamilyWordListTexts[i] != null)
                        {
                            rhymeFamilyWordListTexts[i].gameObject.SetActive(false);
                        }
                        if (rhymeFamilyRunButtons != null && i < rhymeFamilyRunButtons.Length && rhymeFamilyRunButtons[i] != null)
                        {
                            rhymeFamilyRunButtons[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void OnPictureWallTapped(int index)
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null) return;
            if (currentRoomIndex >= activityData.galleryRooms.Length) return;

            GalleryRoomData room = activityData.galleryRooms[currentRoomIndex];
            if (index >= room.pictureWallItems.Length) return;

            GalleryPictureItem item = room.pictureWallItems[index];
            PlaySFX(correctChimeSfx);

            bool isStretched = pictureStretchedState[index];
            pictureStretchedState[index] = !isStretched;

            if (isStretched && item.wordVowelStretchedClip != null)
            {
                SetDialogue($"Listen stretched: {item.wordName.ToUpper()}!");
                PlayVoiceClipNonBlocking(item.wordVowelStretchedClip);
            }
            else if (item.wordNormalClip != null)
            {
                SetDialogue($"Word: {item.wordName.ToUpper()}!");
                PlayVoiceClipNonBlocking(item.wordNormalClip);
            }
        }

        private void OnRhymeFamilyRunTapped(int familyIdx)
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null) return;
            if (currentRoomIndex >= activityData.galleryRooms.Length) return;

            GalleryRoomData room = activityData.galleryRooms[currentRoomIndex];
            if (familyIdx >= room.rhymeFamilies.Length) return;

            RhymeFamilyGroup fam = room.rhymeFamilies[familyIdx];
            SetDialogue($"Rhyme Family: {fam.familyName.ToUpper()} - {string.Join(" ", fam.words)}");

            if (fam.familyRunClip != null)
            {
                PlayVoiceClipNonBlocking(fam.familyRunClip);
            }
        }

        private void StartEchoRound()
        {
            isEchoActive = true;
            echoRoundIndex = 0;

            if (roomSelectorPanel != null) roomSelectorPanel.SetActive(false);
            if (galleryRoomPanel != null) galleryRoomPanel.SetActive(false);
            if (echoRoundPanel != null) echoRoundPanel.SetActive(true);

            SetDialogue("My turn, then your turn. Ready?");
            if (activityData != null && activityData.echoRoundIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.echoRoundIntroClip);
            }

            LoadEchoRound(0);
        }

        private int correctEchoChoiceIndex = 0;

        private void SetButtonColor(GameObject btnObj, Color c)
        {
            if (btnObj == null) return;
            Image img = btnObj.GetComponent<Image>();
            if (img != null) img.color = c;
        }

        private IEnumerator HighlightAndResetButtonColor(GameObject btnObj, Color activeColor, Color resetColor, float duration)
        {
            if (btnObj == null) yield break;
            SetButtonColor(btnObj, activeColor);
            yield return new WaitForSeconds(duration);
            SetButtonColor(btnObj, resetColor);
        }

        private void LoadEchoRound(int index)
        {
            if (activityData == null || currentRoomIndex >= activityData.galleryRooms.Length) return;
            GalleryRoomData room = activityData.galleryRooms[currentRoomIndex];

            if (index >= room.pictureWallItems.Length || index >= totalEchoRounds)
            {
                StartCoroutine(CompleteRoomSequence());
                return;
            }

            echoRoundIndex = index;
            GalleryPictureItem targetItem = room.pictureWallItems[index];

            if (echoPromptTMP != null) echoPromptTMP.text = targetItem.wordName;
            if (echoPromptImage != null && targetItem.pictureSprite != null)
                echoPromptImage.sprite = targetItem.pictureSprite;

            if (echoChoiceButtons != null && echoChoiceButtons.Length > 0)
            {
                List<GalleryPictureItem> choices = new List<GalleryPictureItem>();
                choices.Add(targetItem);
                for (int i = 0; i < room.pictureWallItems.Length; i++)
                {
                    if (choices.Count >= echoChoiceButtons.Length) break;
                    if (i != index && room.pictureWallItems[i] != null)
                    {
                        choices.Add(room.pictureWallItems[i]);
                    }
                }

                for (int i = choices.Count - 1; i > 0; i--)
                {
                    int r = Random.Range(0, i + 1);
                    var temp = choices[i];
                    choices[i] = choices[r];
                    choices[r] = temp;
                }

                correctEchoChoiceIndex = choices.IndexOf(targetItem);

                for (int i = 0; i < echoChoiceButtons.Length; i++)
                {
                    if (echoChoiceButtons[i] == null) continue;
                    if (i < choices.Count)
                    {
                        echoChoiceButtons[i].gameObject.SetActive(true);
                        SetButtonColor(echoChoiceButtons[i].gameObject, Color.white);
                        if (echoChoiceTexts != null && i < echoChoiceTexts.Length && echoChoiceTexts[i] != null)
                            echoChoiceTexts[i].text = choices[i].wordName;
                        if (echoChoiceImages != null && i < echoChoiceImages.Length && echoChoiceImages[i] != null && choices[i].pictureSprite != null)
                            echoChoiceImages[i].sprite = choices[i].pictureSprite;
                    }
                    else
                    {
                        echoChoiceButtons[i].gameObject.SetActive(false);
                    }
                }
            }

            SetDialogue($"Leo says: '{targetItem.wordName}'! Tap the matching picture and say it aloud!");
            if (targetItem.wordNormalClip != null)
            {
                PlayVoiceClipNonBlocking(targetItem.wordNormalClip);
            }

            UpdateProgressUI((float)(currentRoomIndex * 0.2f + (index + 1) * 0.04f));
        }

        private void OnEchoRepeatTapped()
        {
            if (isTransitioning || activityData == null) return;
            if (currentRoomIndex >= activityData.galleryRooms.Length) return;

            if (repeatSpeakButton != null)
            {
                StartCoroutine(WiggleRect(repeatSpeakButton.GetComponent<RectTransform>(), 0.35f, 8f));
            }

            GalleryRoomData room = activityData.galleryRooms[currentRoomIndex];
            if (echoRoundIndex < room.pictureWallItems.Length && room.pictureWallItems[echoRoundIndex] != null)
            {
                GalleryPictureItem targetItem = room.pictureWallItems[echoRoundIndex];
                SetDialogue($"Leo says: '{targetItem.wordName}'! Tap the matching picture!");
                if (targetItem.wordNormalClip != null)
                {
                    PlayVoiceClipNonBlocking(targetItem.wordNormalClip);
                }
            }
        }

        private void OnEchoChoiceSelected(int index)
        {
            if (isTransitioning || activityData == null) return;
            if (currentRoomIndex >= activityData.galleryRooms.Length) return;

            bool isCorrect = (index == correctEchoChoiceIndex);

            if (isCorrect)
            {
                if (echoChoiceButtons != null && index < echoChoiceButtons.Length && echoChoiceButtons[index] != null)
                {
                    SetButtonColor(echoChoiceButtons[index].gameObject, correctColor);
                    StartCoroutine(WiggleRect(echoChoiceButtons[index].GetComponent<RectTransform>(), 0.4f, 12f));
                }
                PlaySFX(correctChimeSfx);
                StartCoroutine(HandleEchoRepeatSuccess());
            }
            else
            {
                if (echoChoiceButtons != null && index < echoChoiceButtons.Length && echoChoiceButtons[index] != null)
                {
                    StartCoroutine(HighlightAndResetButtonColor(echoChoiceButtons[index].gameObject, wrongColor, Color.white, 0.6f));
                    StartCoroutine(WiggleRect(echoChoiceButtons[index].GetComponent<RectTransform>(), 0.4f, 10f));
                }
                PlaySFX(retryGentleSfx);
                SetDialogue("Try again! Listen to Leo's word!");
            }
        }

        private IEnumerator HandleEchoRepeatSuccess()
        {
            isTransitioning = true;
            TriggerWiggleStarMeter();

            SetDialogue("Nice loud voice! You said it just right.");
            if (activityData != null && activityData.echoPraiseClip != null)
            {
                yield return PlayVoiceClip(activityData.echoPraiseClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            echoRoundIndex++;
            isTransitioning = false;

            if (echoRoundIndex < totalEchoRounds)
            {
                LoadEchoRound(echoRoundIndex);
            }
            else
            {
                StartCoroutine(CompleteRoomSequence());
            }
        }

        private IEnumerator CompleteRoomSequence()
        {
            completedRooms[currentRoomIndex] = true;
            if (echoRoundPanel != null) echoRoundPanel.SetActive(false);

            int finishedCount = 0;
            for (int i = 0; i < completedRooms.Length; i++)
            {
                if (completedRooms[i]) finishedCount++;
            }

            float fill = (float)finishedCount / 5f;
            UpdateProgressUI(fill);

            if (finishedCount >= 5)
            {
                SetDialogue("All gallery rooms are glowing! Amazing work!");
                if (activityData != null && activityData.roomCompleteClip != null)
                {
                    yield return PlayVoiceClip(activityData.roomCompleteClip);
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
            }
            else
            {
                SetDialogue("This room is glowing now! Choose another room to explore.");
                yield return new WaitForSeconds(1.0f);
                ShowRoomSelector();
            }

            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
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
