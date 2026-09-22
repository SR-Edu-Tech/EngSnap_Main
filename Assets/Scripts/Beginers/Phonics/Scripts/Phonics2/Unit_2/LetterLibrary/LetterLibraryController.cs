using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit2
{
    public class LetterLibraryController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit2";
        [SerializeField] private string topicName = "LetterLibrary";

        [Header("Data Reference")]
        [SerializeField] private LetterLibraryData libraryData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Shelves & Library View")]
        [SerializeField] private GameObject libraryRoomPanel;
        [SerializeField] private Button[] shelfButtons; // 4 Shelves
        [SerializeField] private Image[] shelfGlowImages;
        [SerializeField] private Transform[] shelfLetterContainers; // 4 Containers holding letter item buttons
        [SerializeField] private Button letterButtonPrefab;

        [Header("Letter Card Modal UI")]
        [SerializeField] private GameObject letterCardModal;
        [SerializeField] private TMP_Text cardLetterText;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardSoundText;
        [SerializeField] private TMP_Text cardWordText;
        [SerializeField] private Image cardLetterImage;
        [SerializeField] private Image cardWordImage;
        [SerializeField] private Image cardMouthImage;
        [SerializeField] private Button cardPlaySoundButton;
        [SerializeField] private Button cardCloseButton;

        [Header("Shelf Quiz UI (5-Question Check)")]
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private TMP_Text quizQuestionText;
        [SerializeField] private Button[] quizChoiceButtons; // 3 Letter choice buttons
        [SerializeField] private TMP_Text[] quizChoiceTexts;
        [SerializeField] private Image[] quizChoiceImages;

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
        [SerializeField] private AudioClip cardFlipSfx;

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

        private int activeShelfIndex = 0;
        private int quizRoundIndex = 0;
        private int attemptCount = 0;
        private bool isQuizActive = false;
        private bool isTransitioning = false;
        private bool isShelfLockedIn = false;
        private HashSet<char> exploredLettersInActiveShelf = new HashSet<char>();
        private Vector2[] originalContainerPositions;
        private LibraryLetterCard currentCard;
        private LibraryLetterCard currentQuizTarget;

        public string UnitID => unitID;
        public string TopicName => topicName;

        private void Awake()
        {
            EnsureAudioSources();
            SetupButtonListeners();
            StoreOriginalContainerPositions();
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

        private void StoreOriginalContainerPositions()
        {
            if (shelfLetterContainers != null)
            {
                originalContainerPositions = new Vector2[shelfLetterContainers.Length];
                for (int i = 0; i < shelfLetterContainers.Length; i++)
                {
                    if (shelfLetterContainers[i] != null)
                    {
                        RectTransform rt = shelfLetterContainers[i].GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            originalContainerPositions[i] = rt.anchoredPosition;
                        }
                    }
                }
            }
        }

        private void SetupButtonListeners()
        {
            if (shelfButtons != null)
            {
                for (int i = 0; i < shelfButtons.Length; i++)
                {
                    int shelfIndex = i;
                    shelfButtons[i].onClick.AddListener(() => OpenShelf(shelfIndex));
                }
            }

            if (cardCloseButton != null) cardCloseButton.onClick.AddListener(CloseLetterCard);
            if (cardPlaySoundButton != null) cardPlaySoundButton.onClick.AddListener(PlayCurrentCardAudio);
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
            isShelfLockedIn = false;
            exploredLettersInActiveShelf.Clear();

            if (libraryRoomPanel != null) libraryRoomPanel.SetActive(true);
            if (letterCardModal != null) letterCardModal.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);

            BuildShelfLetterButtons();

            // Hide all shelf letter containers on start (only show when a shelf button is tapped)
            if (shelfLetterContainers != null)
            {
                for (int i = 0; i < shelfLetterContainers.Length; i++)
                {
                    if (shelfLetterContainers[i] != null)
                    {
                        shelfLetterContainers[i].gameObject.SetActive(false);
                    }
                }
            }

            RefreshShelfGlows();
            UpdateProgressUI(0f);

            SetDialogue("This is the Letter Library. Every letter has a card. Tap one!");
            if (libraryData != null && libraryData.libraryIntroClip != null)
            {
                PlayVoiceClipNonBlocking(libraryData.libraryIntroClip);
            }
        }

        private void BuildShelfLetterButtons()
        {
            if (libraryData == null || libraryData.shelves == null || shelfLetterContainers == null) return;

            for (int s = 0; s < libraryData.shelves.Length && s < shelfLetterContainers.Length; s++)
            {
                Transform container = shelfLetterContainers[s];
                if (container == null) continue;

                LibraryShelfGroup shelf = libraryData.shelves[s];
                if (shelf == null || shelf.shelfLetters == null) continue;

                // Check if buttons are pre-placed in hierarchy under container
                Button[] existingButtons = container.GetComponentsInChildren<Button>(true);

                for (int i = 0; i < shelf.shelfLetters.Length; i++)
                {
                    LibraryLetterCard card = shelf.shelfLetters[i];
                    Button btn = null;

                    if (existingButtons != null && i < existingButtons.Length)
                    {
                        btn = existingButtons[i];
                    }
                    else if (letterButtonPrefab != null)
                    {
                        btn = Instantiate(letterButtonPrefab, container);
                    }

                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OpenLetterCard(card));

                        // Set text or image if components are present on button
                        TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
                        if (btnText != null) btnText.text = card.letterChar.ToString();

                        Image btnImg = btn.GetComponent<Image>();
                        if (btnImg != null && card.letterSprite != null) btnImg.sprite = card.letterSprite;
                    }
                }
            }
        }

        private void RefreshShelfGlows()
        {
            if (shelfGlowImages == null) return;
            for (int i = 0; i < shelfGlowImages.Length; i++)
            {
                bool isCompleted = PlayerPrefs.GetInt($"{unitID}_{topicName}_Shelf_{i}", 0) == 1;
                shelfGlowImages[i].gameObject.SetActive(isCompleted);
            }
        }

        private bool IsAudioPlaying()
        {
            return voiceAudioSource != null && voiceAudioSource.isPlaying;
        }

        public void OpenShelf(int shelfIndex)
        {
            if (isTransitioning || IsAudioPlaying() || libraryData == null || shelfIndex >= libraryData.shelves.Length) return;

            // Block opening another shelf if player is currently exploring/taking quiz for a locked shelf
            if (isShelfLockedIn && activeShelfIndex != shelfIndex)
            {
                SetDialogue("Finish exploring this shelf and complete its quiz first!");
                return;
            }

            activeShelfIndex = shelfIndex;
            isShelfLockedIn = true;
            LibraryShelfGroup shelf = libraryData.shelves[shelfIndex];

            // Slide in corresponding shelf container from side
            if (shelfLetterContainers != null && shelfIndex < shelfLetterContainers.Length && shelfLetterContainers[shelfIndex] != null)
            {
                for (int i = 0; i < shelfLetterContainers.Length; i++)
                {
                    if (shelfLetterContainers[i] != null && i != shelfIndex)
                    {
                        shelfLetterContainers[i].gameObject.SetActive(false);
                    }
                }
                StartCoroutine(SlideContainerIn(shelfIndex));
            }

            SetDialogue($"Welcome to {shelf.shelfName}! Tap each letter to listen to its sound.");
            PlaySFX(cardFlipSfx);
        }

        private IEnumerator SlideContainerIn(int shelfIndex)
        {
            if (shelfLetterContainers == null || shelfIndex >= shelfLetterContainers.Length || shelfLetterContainers[shelfIndex] == null) yield break;

            Transform container = shelfLetterContainers[shelfIndex];
            container.gameObject.SetActive(true);
            RectTransform rt = container.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 targetPos = (originalContainerPositions != null && shelfIndex < originalContainerPositions.Length) 
                    ? originalContainerPositions[shelfIndex] 
                    : rt.anchoredPosition;

                float offset = (Screen.width > 0) ? Screen.width : 1000f;
                Vector2 startPos = targetPos + new Vector2(offset, 0f);

                float elapsed = 0f;
                float duration = 0.35f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    rt.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / duration);
                    yield return null;
                }
                rt.anchoredPosition = targetPos;
            }
        }

        private IEnumerator SlideContainerOut(int shelfIndex)
        {
            if (shelfLetterContainers == null || shelfIndex >= shelfLetterContainers.Length || shelfLetterContainers[shelfIndex] == null) yield break;

            Transform container = shelfLetterContainers[shelfIndex];
            RectTransform rt = container.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector2 startPos = rt.anchoredPosition;
                float offset = (Screen.width > 0) ? Screen.width : 1000f;
                Vector2 endPos = startPos - new Vector2(offset, 0f);

                float elapsed = 0f;
                float duration = 0.3f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    rt.anchoredPosition = Vector2.Lerp(startPos, endPos, elapsed / duration);
                    yield return null;
                }
            }
            container.gameObject.SetActive(false);
        }

        public void OpenLetterCard(LibraryLetterCard card)
        {
            if (card == null) return;
            currentCard = card;

            // Track explored letter for active shelf
            exploredLettersInActiveShelf.Add(card.letterChar);

            if (letterCardModal != null) letterCardModal.SetActive(true);
            PlaySFX(cardFlipSfx);

            if (cardLetterText != null) cardLetterText.text = $"{card.letterChar}{card.letterChar.ToString().ToLower()}";
            if (cardNameText != null) cardNameText.text = card.letterName;
            if (cardSoundText != null) cardSoundText.text = card.letterSound;
            if (cardWordText != null) cardWordText.text = card.pictureWord;

            if (cardLetterImage != null && card.letterSprite != null) cardLetterImage.sprite = card.letterSprite;
            if (cardWordImage != null && card.pictureWordSprite != null) cardWordImage.sprite = card.pictureWordSprite;
            if (cardMouthImage != null && card.mouthCloseUpSprite != null) cardMouthImage.sprite = card.mouthCloseUpSprite;

            SetDialogue($"This is {card.letterChar}. Its name is \"{card.letterName}\". Its sound is {card.letterSound} … {card.pictureWord}!");

            if (card.cardAudioClip != null)
            {
                PlayVoiceClipNonBlocking(card.cardAudioClip);
            }
        }

        public void CloseLetterCard()
        {
            if (voiceAudioSource != null && voiceAudioSource.isPlaying)
            {
                voiceAudioSource.Stop();
            }

            if (letterCardModal != null) letterCardModal.SetActive(false);

            if (libraryData == null || activeShelfIndex >= libraryData.shelves.Length) return;
            LibraryShelfGroup shelf = libraryData.shelves[activeShelfIndex];

            // Check if all letters on this shelf have been explored/tapped at least once
            if (shelf.shelfLetters != null && exploredLettersInActiveShelf.Count >= shelf.shelfLetters.Length)
            {
                if (!isQuizActive)
                {
                    SetDialogue("All letters on this shelf explored! Now let's test your sounds with a quick quiz.");
                    StartCoroutine(LaunchQuizWithDelay(1.0f));
                }
            }
            else if (shelf.shelfLetters != null)
            {
                int remaining = shelf.shelfLetters.Length - exploredLettersInActiveShelf.Count;
                SetDialogue($"Explored {exploredLettersInActiveShelf.Count} of {shelf.shelfLetters.Length} letters. Tap {remaining} more letter(s) to unlock the shelf quiz!");
            }
        }

        private IEnumerator LaunchQuizWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartShelfQuiz(activeShelfIndex);
        }

        private void StartShelfQuiz(int shelfIndex)
        {
            if (libraryData == null || shelfIndex >= libraryData.shelves.Length) return;

            isQuizActive = true;
            quizRoundIndex = 0;

            // Deactivate all other panels so ONLY quizPanel is active on screen!
            if (libraryRoomPanel != null) libraryRoomPanel.SetActive(false);
            if (letterCardModal != null) letterCardModal.SetActive(false);

            if (shelfLetterContainers != null)
            {
                for (int i = 0; i < shelfLetterContainers.Length; i++)
                {
                    if (shelfLetterContainers[i] != null)
                    {
                        shelfLetterContainers[i].gameObject.SetActive(false);
                    }
                }
            }

            if (quizPanel != null) quizPanel.SetActive(true);

            LoadQuizRound(quizRoundIndex);
        }

        private void LoadQuizRound(int roundIndex)
        {
            if (libraryData == null || activeShelfIndex >= libraryData.shelves.Length) return;

            LibraryShelfGroup shelf = libraryData.shelves[activeShelfIndex];
            if (shelf.shelfLetters == null || shelf.shelfLetters.Length == 0 || roundIndex >= shelf.quizTargetIndices.Length)
            {
                CompleteShelf(activeShelfIndex);
                return;
            }

            attemptCount = 0;
            if (momoHintObject != null) momoHintObject.SetActive(false);

            int targetIndex = shelf.quizTargetIndices[roundIndex];
            currentQuizTarget = shelf.shelfLetters[Mathf.Clamp(targetIndex, 0, shelf.shelfLetters.Length - 1)];

            SetDialogue($"Which letter says {currentQuizTarget.letterSound}?");
            if (currentQuizTarget.soundOnlyClip != null)
            {
                PlayVoiceClipNonBlocking(currentQuizTarget.soundOnlyClip);
            }

            SetupQuizChoices(shelf, currentQuizTarget);
            UpdateProgressUI((float)(activeShelfIndex * 5 + roundIndex) / 20f);
        }

        private void SetupQuizChoices(LibraryShelfGroup shelf, LibraryLetterCard target)
        {
            List<LibraryLetterCard> choices = new List<LibraryLetterCard> { target };

            // Pick 2 random distractors from shelf
            foreach (var card in shelf.shelfLetters)
            {
                if (card != target && choices.Count < 3)
                {
                    choices.Add(card);
                }
            }

            // Shuffle
            for (int i = 0; i < choices.Count; i++)
            {
                var temp = choices[i];
                int r = Random.Range(i, choices.Count);
                choices[i] = choices[r];
                choices[r] = temp;
            }

            for (int i = 0; i < quizChoiceButtons.Length; i++)
            {
                if (i < choices.Count)
                {
                    quizChoiceButtons[i].gameObject.SetActive(true);
                    LibraryLetterCard chosenCard = choices[i];
                    Button btn = quizChoiceButtons[i];

                    // Reset button image color to normal white
                    Image btnImg = btn.GetComponent<Image>();
                    if (btnImg != null) btnImg.color = Color.white;

                    if (quizChoiceTexts[i] != null) quizChoiceTexts[i].text = chosenCard.letterChar.ToString();
                    if (quizChoiceImages[i] != null && chosenCard.letterSprite != null) quizChoiceImages[i].sprite = chosenCard.letterSprite;

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnQuizChoiceSelected(chosenCard, btn));
                }
                else
                {
                    quizChoiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnQuizChoiceSelected(LibraryLetterCard chosenCard, Button tappedButton)
        {
            if (isTransitioning || IsAudioPlaying() || currentQuizTarget == null) return;

            bool isCorrect = (chosenCard.letterChar == currentQuizTarget.letterChar);

            // Trigger wiggle animation and color highlight on tapped button
            if (tappedButton != null)
            {
                RectTransform rt = tappedButton.GetComponent<RectTransform>();
                if (rt != null) TriggerWiggle(rt);

                Image btnImg = tappedButton.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isCorrect ? new Color(0.3f, 0.85f, 0.3f) : new Color(0.95f, 0.3f, 0.3f);
                }
            }

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectQuizChoice(chosenCard, tappedButton));
            }
            else
            {
                StartCoroutine(HandleWrongQuizChoice(chosenCard, tappedButton));
            }
        }

        private IEnumerator HandleCorrectQuizChoice(LibraryLetterCard chosenCard, Button tappedButton)
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue($"Yes! {chosenCard.letterChar} says {chosenCard.letterSound} — like a {chosenCard.pictureWord}: {chosenCard.letterSound} {chosenCard.pictureWord}!");

            if (chosenCard.cardAudioClip != null)
            {
                yield return PlayVoiceClip(chosenCard.cardAudioClip);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            quizRoundIndex++;
            isTransitioning = false;

            LibraryShelfGroup shelf = libraryData.shelves[activeShelfIndex];
            if (quizRoundIndex < shelf.quizTargetIndices.Length)
            {
                LoadQuizRound(quizRoundIndex);
            }
            else
            {
                CompleteShelf(activeShelfIndex);
            }
        }

        private IEnumerator HandleWrongQuizChoice(LibraryLetterCard chosenCard, Button tappedButton)
        {
            attemptCount++;
            PlaySFX(retryGentleSfx);

            if (attemptCount >= 2)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                SetDialogue($"This one! Say it with me — {currentQuizTarget.letterSound}!");
                if (libraryData != null && libraryData.momoQuizHintClip != null)
                {
                    yield return PlayVoiceClip(libraryData.momoQuizHintClip);
                }
            }
            else
            {
                SetDialogue($"Listen again. {currentQuizTarget.letterSound} … {currentQuizTarget.letterSound}. Which one is it?");
                if (currentQuizTarget.soundOnlyClip != null)
                {
                    yield return PlayVoiceClip(currentQuizTarget.soundOnlyClip);
                }
            }

            // Reset wrong button color back to normal white for retry
            if (tappedButton != null)
            {
                Image btnImg = tappedButton.GetComponent<Image>();
                if (btnImg != null) btnImg.color = Color.white;
            }
        }

        private void CompleteShelf(int shelfIndex)
        {
            PlayerPrefs.SetInt($"{unitID}_{topicName}_Shelf_{shelfIndex}", 1);
            PlayerPrefs.Save();
            RefreshShelfGlows();

            StartCoroutine(HandleShelfCompletionSequence());
        }

        private IEnumerator HandleShelfCompletionSequence()
        {
            if (quizPanel != null) quizPanel.SetActive(false);
            isQuizActive = false;
            isShelfLockedIn = false;
            exploredLettersInActiveShelf.Clear();

            // Reactivate main library room view
            if (libraryRoomPanel != null) libraryRoomPanel.SetActive(true);

            // Keep container active on shelf and play pop-up bounce celebrate animation
            if (shelfLetterContainers != null && activeShelfIndex < shelfLetterContainers.Length && shelfLetterContainers[activeShelfIndex] != null)
            {
                shelfLetterContainers[activeShelfIndex].gameObject.SetActive(true);
                StartCoroutine(PopContainerAnimation(shelfLetterContainers[activeShelfIndex]));
            }

            PlaySFX(starPopSfx);
            SetDialogue("Shelf complete! Your shelf is glowing. Tap another shelf to continue!");
            if (libraryData != null && libraryData.shelfCompleteVoiceClip != null)
            {
                yield return PlayVoiceClip(libraryData.shelfCompleteVoiceClip);
            }

            if (AreAllShelvesComplete())
            {
                StartCoroutine(CompleteStopSequence());
            }
        }

        private IEnumerator PopContainerAnimation(Transform container)
        {
            if (container == null) yield break;

            container.gameObject.SetActive(true);
            RectTransform rt = container.GetComponent<RectTransform>();
            if (rt != null)
            {
                Vector3 originalScale = rt.localScale;
                float elapsed = 0f;
                float duration = 0.45f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float scaleMultiplier = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
                    rt.localScale = originalScale * scaleMultiplier;
                    yield return null;
                }
                rt.localScale = originalScale;
            }
        }

        private bool AreAllShelvesComplete()
        {
            if (libraryData == null || libraryData.shelves == null) return false;
            for (int i = 0; i < libraryData.shelves.Length; i++)
            {
                if (PlayerPrefs.GetInt($"{unitID}_{topicName}_Shelf_{i}", 0) == 0)
                {
                    return false;
                }
            }
            return true;
        }

        private IEnumerator CompleteStopSequence()
        {
            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            PlaySFX(starPopSfx);
            UpdateProgressUI(1f);
            yield return new WaitForSeconds(0.5f);

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.SetActive(true);
            isTransitioning = false;
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

            if (currentPanel != null)
            {
                 currentPanel.SetActive(false);
                unitContentPanel.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        private void PlayCurrentCardAudio()
        {
            if (currentCard != null && currentCard.cardAudioClip != null)
            {
                PlayVoiceClipNonBlocking(currentCard.cardAudioClip);
            }
        }

        private void ReplayCurrentAudio()
        {
            if (isQuizActive && currentQuizTarget != null && currentQuizTarget.soundOnlyClip != null)
            {
                PlayVoiceClipNonBlocking(currentQuizTarget.soundOnlyClip);
            }
            else if (currentCard != null && currentCard.cardAudioClip != null)
            {
                PlayVoiceClipNonBlocking(currentCard.cardAudioClip);
            }
        }

        private void TriggerWiggle(RectTransform target)
        {
            if (target != null)
            {
                StartCoroutine(WiggleRect(target, 0.4f, 12f));
            }
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null)
            {
                StartCoroutine(WiggleRect(starMeterRect, 0.5f, 15f));
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

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) yield break;
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length + 0.1f);
        }

        private void PlayVoiceClipNonBlocking(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) return;
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxAudioSource == null) return;
            sfxAudioSource.PlayOneShot(clip);
        }

        private void SetDialogue(string msg)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, msg, dialogueCanvasGroup);
        }

        private void UpdateProgressUI(float fillPercent)
        {
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillPercent;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillPercent * 100)}%";
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        public void GoToPreviousPanel()
        {
            DeactivateMascots();
            if (currentPanel != null) currentPanel.SetActive(false);
            if (unitContentPanel != null) unitContentPanel.SetActive(true);
        }
    }
}
