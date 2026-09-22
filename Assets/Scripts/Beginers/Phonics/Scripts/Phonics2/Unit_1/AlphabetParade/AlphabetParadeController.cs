using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit1
{
    public class AlphabetParadeController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit1";
        [SerializeField] private string topicName = "AlphabetParade";

        [Header("ScriptableObject Data")]
        [SerializeField] private AlphabetParadeData paradeData;

        [Header("UI & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Parade Grid / Letter Buttons (26 Letters A-Z)")]
        [SerializeField] private GameObject paradeGridPanel;
        [SerializeField] private Button[] letterGridButtons;
        [SerializeField] private TMP_Text[] letterGridTexts; // 26 TMP_Text labels for A-Z
        [SerializeField] private GameObject[] vowelGlowObjects; // A, E, I, O, U glow highlights

        [Header("Letter Card Popup UI")]
        [SerializeField] private GameObject letterCardPopup;
        [SerializeField] private Image cardLetterImage;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text cardSoundText;
        [SerializeField] private TMP_Text cardWordText;
        [SerializeField] private Image cardWordImage;
        [SerializeField] private Button cardPlayAudioButton;
        [SerializeField] private Button cardCloseButton;

        [Header("Quiz UI")]
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private Button[] quizChoiceButtons;
        [SerializeField] private Image[] quizChoiceImages;
        [SerializeField] private TMP_Text[] quizChoiceTexts;

        [Header("Progress Ring UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;

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

        [Header("Mascots & Hints")]
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip floatMarchSfx;

        private int currentQuizIndex = 0;
        private int totalQuizRounds = 5;
        private int attemptsCount = 0;
        private bool isQuizActive = false;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;

        private AlphabetCardItem currentQuizCard;
        private int correctQuizChoiceIndex = 0;

        public string UnitID => unitID;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
        }

        private void EnsureDataAssigned()
        {
            if (paradeData == null)
            {
                paradeData = Resources.Load<AlphabetParadeData>("Phonics2/Unit1/AlphabetParadeData");
            }
        }

        private void EnsureAudioSources()
        {
            if (sfxAudioSource == null)
            {
                sfxAudioSource = GetComponent<AudioSource>();
                if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }
            sfxAudioSource.spatialBlend = 0f;
            sfxAudioSource.volume = 1f;

            if (voiceAudioSource == null) voiceAudioSource = sfxAudioSource;
            else voiceAudioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
            SetupButtonListeners();
            ResetLevel();
        }

        private void OnEnable()
        {
            EnsureDataAssigned();
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            ResetLevel();
            StartCoroutine(StartIntroSequence());
        }

        private void OnDisable()
        {
            DeactivateMascots();
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            if (letterGridButtons != null)
            {
                for (int i = 0; i < letterGridButtons.Length; i++)
                {
                    int lIdx = i;
                    if (letterGridButtons[i] != null)
                    {
                        letterGridButtons[i].onClick.RemoveAllListeners();
                        letterGridButtons[i].onClick.AddListener(() => OnGridLetterTapped(lIdx));
                    }
                }
            }

            if (quizChoiceButtons != null)
            {
                for (int i = 0; i < quizChoiceButtons.Length; i++)
                {
                    int qIdx = i;
                    if (quizChoiceButtons[i] != null)
                    {
                        quizChoiceButtons[i].onClick.RemoveAllListeners();
                        quizChoiceButtons[i].onClick.AddListener(() => OnQuizChoiceSelected(qIdx));
                    }
                }
            }

            if (cardCloseButton != null)
            {
                cardCloseButton.onClick.RemoveAllListeners();
                cardCloseButton.onClick.AddListener(CloseLetterCard);
            }

            if (cardPlayAudioButton != null)
            {
                cardPlayAudioButton.onClick.RemoveAllListeners();
                cardPlayAudioButton.onClick.AddListener(ReplayCardAudio);
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
            currentQuizIndex = 0;
            attemptsCount = 0;
            isQuizActive = false;
            isTransitioning = false;
            isActivityCompleted = false;

            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (letterCardPopup != null) letterCardPopup.SetActive(false);

            SetVowelsGlowActive(false);
            HideAllPanels();
            if (paradeGridPanel != null) paradeGridPanel.SetActive(true);
            SetupGridLabels();
            UpdateProgressMeter();
        }

        private void SetupGridLabels()
        {
            EnsureDataAssigned();

            if (letterGridButtons != null)
            {
                for (int i = 0; i < letterGridButtons.Length; i++)
                {
                    if (letterGridButtons[i] != null)
                    {
                        letterGridButtons[i].gameObject.SetActive(true);
                        letterGridButtons[i].interactable = true;
                    }
                }
            }

            if (paradeData == null || paradeData.alphabetCards == null) return;

            for (int i = 0; i < paradeData.alphabetCards.Length; i++)
            {
                if (letterGridTexts != null && i < letterGridTexts.Length && letterGridTexts[i] != null && paradeData.alphabetCards[i] != null)
                {
                    char upper = paradeData.alphabetCards[i].letterChar;
                    char lower = char.ToLower(upper);
                    letterGridTexts[i].text = $"{upper}{lower}"; // e.g. "Aa", "Bb", "Cc"
                }
            }
        }

        private void HideAllPanels()
        {
            if (paradeGridPanel != null) paradeGridPanel.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(false);
        }

        private bool IsAudioPlaying()
        {
            return isTransitioning || (voiceAudioSource != null && voiceAudioSource.isPlaying);
        }

        private void SetGridButtonsInteractable(bool interactable)
        {
            if (letterGridButtons == null) return;
            foreach (var btn in letterGridButtons)
            {
                if (btn != null) btn.interactable = interactable;
            }
        }

        private IEnumerator StartIntroSequence()
        {
            isTransitioning = true;
            SetGridButtonsInteractable(false);
            if (paradeGridPanel != null) paradeGridPanel.SetActive(true);

            SetSubtitles("Here comes the Alphabet Parade! Twenty-six letters, all in a line.");

            if (paradeData != null && paradeData.introVoiceClip != null)
            {
                PlayVoice(paradeData.introVoiceClip);
                yield return new WaitForSeconds(paradeData.introVoiceClip.length + 0.2f);
            }

            // Play Alphabet Song
            if (paradeData != null && paradeData.alphabetSongClip != null)
            {
                PlayVoice(paradeData.alphabetSongClip);
                yield return new WaitForSeconds(paradeData.alphabetSongClip.length + 0.3f);
            }

            SetSubtitles("Every letter has TWO things: a name, and a sound. Listen.");
            if (paradeData != null && paradeData.keyTeachingVoiceClip != null)
            {
                PlayVoice(paradeData.keyTeachingVoiceClip);
                yield return new WaitForSeconds(paradeData.keyTeachingVoiceClip.length + 0.3f);
            }

            SetSubtitles("Tap any letter you like!");
            if (paradeData != null && paradeData.freeExploreInstructionClip != null)
            {
                PlayVoice(paradeData.freeExploreInstructionClip);
                yield return new WaitForSeconds(paradeData.freeExploreInstructionClip.length + 0.1f);
            }

            SetGridButtonsInteractable(true);
            isTransitioning = false;
        }

        private void OnGridLetterTapped(int letterIndex)
        {
            if (IsAudioPlaying() || isQuizActive) return;

            if (letterGridButtons != null && letterIndex >= 0 && letterIndex < letterGridButtons.Length && letterGridButtons[letterIndex] != null)
            {
                PlayWiggleAnimation(letterGridButtons[letterIndex].transform);
                PlayBounceAnimation(letterGridButtons[letterIndex].transform);
            }

            if (paradeData == null || paradeData.alphabetCards == null || letterIndex >= paradeData.alphabetCards.Length) return;

            AlphabetCardItem item = paradeData.alphabetCards[letterIndex];
            if (item != null)
            {
                OpenLetterCard(item);
            }
        }

        private void OpenLetterCard(AlphabetCardItem item)
        {
            if (item == null) return;
            currentQuizCard = item;

            if (letterCardPopup != null) letterCardPopup.SetActive(true);

            if (cardLetterImage != null && item.letterSprite != null) cardLetterImage.sprite = item.letterSprite;
            if (cardNameText != null) cardNameText.text = $"Name: {item.letterName}";
            if (cardSoundText != null) cardSoundText.text = $"Sound: {item.letterSound}";
            if (cardWordText != null) cardWordText.text = item.pictureWord;
            if (cardWordImage != null && item.pictureWordSprite != null) cardWordImage.sprite = item.pictureWordSprite;

            SetSubtitles($"This is {item.letterChar}. Its name is \"{item.letterName}\". Its sound is {item.letterSound}. {item.pictureWord}!");

            if (item.cardAudioClip != null)
            {
                StartCoroutine(PlayCardAudioSequence(item.cardAudioClip));
            }
        }

        private IEnumerator PlayCardAudioSequence(AudioClip clip)
        {
            isTransitioning = true;
            if (clip != null)
            {
                PlayVoice(clip);
                yield return new WaitForSeconds(clip.length + 0.2f);
            }
            isTransitioning = false;
        }

        private void ReplayCardAudio()
        {
            if (IsAudioPlaying()) return;

            if (currentQuizCard != null && currentQuizCard.cardAudioClip != null)
            {
                StartCoroutine(PlayCardAudioSequence(currentQuizCard.cardAudioClip));
            }
        }

        private void CloseLetterCard()
        {
            if (IsAudioPlaying()) return;
            if (letterCardPopup != null) letterCardPopup.SetActive(false);
        }

        public void StartQuizPhase()
        {
            if (IsAudioPlaying()) return;
            isQuizActive = true;
            CloseLetterCard();
            LoadQuizRound(0);
        }

        private void LoadQuizRound(int quizIdx)
        {
            currentQuizIndex = quizIdx;
            attemptsCount = 0;
            StopHintPulseAnimation();
            if (momoHintObject != null) momoHintObject.SetActive(false);

            UpdateProgressMeter();

            if (quizIdx >= totalQuizRounds)
            {
                StartCoroutine(VowelPeekSequence());
                return;
            }

            HideAllPanels();
            if (quizPanel != null) quizPanel.SetActive(true);

            int targetCardIdx = 0;
            if (paradeData != null && paradeData.quizLetterIndices != null && quizIdx < paradeData.quizLetterIndices.Length)
            {
                targetCardIdx = paradeData.quizLetterIndices[quizIdx];
            }

            if (paradeData != null && paradeData.alphabetCards != null && targetCardIdx < paradeData.alphabetCards.Length)
            {
                currentQuizCard = paradeData.alphabetCards[targetCardIdx];
            }

            if (currentQuizCard == null) return;

            // Pick 2 distractors
            List<AlphabetCardItem> pool = new List<AlphabetCardItem>(paradeData.alphabetCards);
            pool.Remove(currentQuizCard);
            AlphabetCardItem d1 = pool[Random.Range(0, pool.Count)];
            pool.Remove(d1);
            AlphabetCardItem d2 = pool[Random.Range(0, pool.Count)];

            List<AlphabetCardItem> choices = new List<AlphabetCardItem> { currentQuizCard, d1, d2 };
            ShuffleList(choices);

            correctQuizChoiceIndex = choices.IndexOf(currentQuizCard);

            for (int i = 0; i < quizChoiceButtons.Length && i < choices.Count; i++)
            {
                if (quizChoiceButtons[i] != null)
                {
                    quizChoiceButtons[i].gameObject.SetActive(true);
                    quizChoiceButtons[i].interactable = true;
                    if (quizChoiceImages != null && i < quizChoiceImages.Length && quizChoiceImages[i] != null)
                    {
                        quizChoiceImages[i].sprite = choices[i].letterSprite;
                    }
                    if (quizChoiceTexts != null && i < quizChoiceTexts.Length && quizChoiceTexts[i] != null)
                    {
                        quizChoiceTexts[i].text = choices[i].letterChar.ToString();
                    }
                }
            }

            SetSubtitles($"Which letter says {currentQuizCard.letterSound}? Tap it.");
            StartCoroutine(PlayQuizPromptSequence());
        }

        private IEnumerator PlayQuizPromptSequence()
        {
            isTransitioning = true;
            SetQuizChoicesInteractable(false);

            if (paradeData != null && paradeData.quizInstructionClip != null)
            {
                PlayVoice(paradeData.quizInstructionClip);
                yield return new WaitForSeconds(paradeData.quizInstructionClip.length + 0.15f);
            }

            AudioClip soundClip = (currentQuizCard != null && currentQuizCard.soundOnlyClip != null)
                ? currentQuizCard.soundOnlyClip
                : (currentQuizCard != null ? currentQuizCard.cardAudioClip : null);

            if (soundClip != null)
            {
                PlayVoice(soundClip);
                yield return new WaitForSeconds(soundClip.length + 0.3f);
            }

            SetQuizChoicesInteractable(true);
            isTransitioning = false;
        }

        private void OnQuizChoiceSelected(int index)
        {
            if (IsAudioPlaying() || isActivityCompleted) return;

            attemptsCount++;
            bool isCorrect = (index == correctQuizChoiceIndex);
            GameObject tappedObj = (index >= 0 && index < quizChoiceButtons.Length && quizChoiceButtons[index] != null) ? quizChoiceButtons[index].gameObject : null;

            if (isCorrect)
            {
                StartCoroutine(CorrectQuizSequence(tappedObj));
            }
            else
            {
                StartCoroutine(RetryQuizSequence(tappedObj));
            }
        }

        private IEnumerator CorrectQuizSequence(GameObject tappedObj)
        {
            isTransitioning = true;

            if (correctChimeSfx != null) PlaySfx(correctChimeSfx);
            if (tappedObj != null) PlayBounceAnimation(tappedObj.transform);

            char ch = currentQuizCard != null ? currentQuizCard.letterChar : 'A';
            string sound = currentQuizCard != null ? currentQuizCard.letterSound : "/a/";
            SetSubtitles($"Yes! {ch} says {sound}!");

            yield return new WaitForSeconds(0.8f);

            isTransitioning = false;
            LoadQuizRound(currentQuizIndex + 1);
        }

        private IEnumerator RetryQuizSequence(GameObject tappedObj)
        {
            isTransitioning = true;

            if (retryGentleSfx != null) PlaySfx(retryGentleSfx);
            if (tappedObj != null) PlayWiggleAnimation(tappedObj.transform);

            if (attemptsCount >= 3)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                SetSubtitles("Psst! Tap this one!");

                GameObject correctObj = GetCurrentCorrectButton();
                if (correctObj != null)
                {
                    StartHintPulseAnimation(correctObj.transform);
                }

                yield return new WaitForSeconds(1.0f);
            }
            else
            {
                string soundStr = currentQuizCard != null ? currentQuizCard.letterSound : "";
                SetSubtitles($"Listen once more! Which letter says {soundStr}?");
                AudioClip soundClip = (currentQuizCard != null && currentQuizCard.soundOnlyClip != null)
                    ? currentQuizCard.soundOnlyClip
                    : (currentQuizCard != null ? currentQuizCard.cardAudioClip : null);

                if (soundClip != null)
                {
                    PlayVoice(soundClip);
                    yield return new WaitForSeconds(soundClip.length + 0.3f);
                }
                else
                {
                    yield return new WaitForSeconds(0.6f);
                }
            }

            isTransitioning = false;
        }

        private IEnumerator VowelPeekSequence()
        {
            isTransitioning = true;

            HideAllPanels();
            if (paradeGridPanel != null) paradeGridPanel.SetActive(true);
            SetVowelsGlowActive(true);
            WobbleVowelButtons();

            SetSubtitles("Look — these five letters are glowing. A, E, I, O, U. They are special. We will meet them next time!");

            if (paradeData != null && paradeData.vowelPeekVoiceClip != null)
            {
                PlayVoice(paradeData.vowelPeekVoiceClip);
                yield return new WaitForSeconds(paradeData.vowelPeekVoiceClip.length + 0.3f);
            }

            SetSubtitles("Names help us spell. Sounds help us READ. We will use the sounds.");
            if (paradeData != null && paradeData.spellReadVoiceClip != null)
            {
                PlayVoice(paradeData.spellReadVoiceClip);
                yield return new WaitForSeconds(paradeData.spellReadVoiceClip.length + 0.3f);
            }

            StartCoroutine(CompletionSequence());
        }

        private void WobbleVowelButtons()
        {
            int[] vowelIndices = new int[] { 0, 4, 8, 14, 20 };
            foreach (int idx in vowelIndices)
            {
                if (letterGridButtons != null && idx < letterGridButtons.Length && letterGridButtons[idx] != null)
                {
                    PlayWiggleAnimation(letterGridButtons[idx].transform);
                    PlayBounceAnimation(letterGridButtons[idx].transform);
                }
                if (vowelGlowObjects != null && idx < vowelGlowObjects.Length && vowelGlowObjects[idx] != null)
                {
                    PlayWiggleAnimation(vowelGlowObjects[idx].transform);
                    PlayBounceAnimation(vowelGlowObjects[idx].transform);
                }
            }
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            SetSubtitles("Great job in the Alphabet Parade!");

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);

            TopicProgressUI.MarkTopicComplete(unitID, topicName);

            isTransitioning = false;
            yield break;
        }

        #region Navigation & Helpers
        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();

            if (isActivityCompleted)
            {
                TopicProgressUI.MarkTopicComplete(gameObject);
            }

            DeactivateMascots();
            ResetLevel();

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

        private void SetVowelsGlowActive(bool active)
        {
            if (vowelGlowObjects == null) return;
            foreach (var glow in vowelGlowObjects)
            {
                if (glow != null) glow.SetActive(active);
            }
        }

        private void SetQuizChoicesInteractable(bool interactable)
        {
            if (quizChoiceButtons == null) return;
            foreach (var btn in quizChoiceButtons)
            {
                if (btn != null) btn.interactable = interactable;
            }
        }

        private void UpdateProgressMeter()
        {
            if (progressRingFillImage != null)
            {
                progressRingFillImage.fillAmount = (float)currentQuizIndex / totalQuizRounds;
            }
            if (progressText != null)
            {
                progressText.text = $"{currentQuizIndex} / {totalQuizRounds}";
            }
        }

        private void PlayVoice(AudioClip clip)
        {
            if (clip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
            }
        }

        private void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(clip, volume);
            }
        }

        private void SetSubtitles(string text)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int rnd = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[rnd];
                list[rnd] = temp;
            }
        }

        private void PlayBounceAnimation(Transform tr)
        {
            if (tr == null) return;
            StartCoroutine(BounceCoroutine(tr));
        }

        private IEnumerator BounceCoroutine(Transform tr)
        {
            Vector3 orig = tr.localScale;
            float dur = 0.35f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
                tr.localScale = orig * scale;
                yield return null;
            }
            tr.localScale = orig;
        }

        private void PlayWiggleAnimation(Transform tr)
        {
            if (tr == null) return;
            StartCoroutine(WiggleCoroutine(tr));
        }

        private IEnumerator WiggleCoroutine(Transform tr)
        {
            Vector3 origScale = tr.localScale;
            Quaternion origRot = tr.localRotation;
            float dur = 0.3f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float rot = Mathf.Sin(t * Mathf.PI * 3f) * 6f * (1f - t);
                tr.localRotation = origRot * Quaternion.Euler(0, 0, rot);
                yield return null;
            }
            tr.localScale = origScale;
            tr.localRotation = origRot;
        }

        private Coroutine hintPulseCoroutine;
        private Transform hintPulsingTransform;
        private Vector3 hintOriginalScale = Vector3.one;

        private void StartHintPulseAnimation(Transform tr)
        {
            StopHintPulseAnimation();
            if (tr == null || !tr.gameObject.activeInHierarchy) return;
            hintPulsingTransform = tr;
            hintOriginalScale = tr.localScale;
            hintPulseCoroutine = StartCoroutine(HintPulseCoroutine(tr, hintOriginalScale));
        }

        private void StopHintPulseAnimation()
        {
            if (hintPulseCoroutine != null)
            {
                StopCoroutine(hintPulseCoroutine);
                hintPulseCoroutine = null;
            }
            if (hintPulsingTransform != null)
            {
                hintPulsingTransform.localScale = hintOriginalScale;
                hintPulsingTransform = null;
            }
        }

        private IEnumerator HintPulseCoroutine(Transform tr, Vector3 baseScale)
        {
            float pulseSpeed = 4.0f;
            float pulseAmount = 0.22f; // Smooth pop up and down scale pulse between 1.0x and 1.22x

            while (tr != null && tr.gameObject.activeInHierarchy && momoHintObject != null && momoHintObject.activeInHierarchy)
            {
                float sine = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
                tr.localScale = baseScale * (1f + sine * pulseAmount);
                yield return null;
            }

            if (tr != null) tr.localScale = baseScale;
            hintPulsingTransform = null;
        }

        private GameObject GetCurrentCorrectButton()
        {
            if (quizChoiceButtons != null && correctQuizChoiceIndex >= 0 && correctQuizChoiceIndex < quizChoiceButtons.Length)
            {
                return quizChoiceButtons[correctQuizChoiceIndex] != null ? quizChoiceButtons[correctQuizChoiceIndex].gameObject : null;
            }
            return null;
        }
        #endregion
    }
}
