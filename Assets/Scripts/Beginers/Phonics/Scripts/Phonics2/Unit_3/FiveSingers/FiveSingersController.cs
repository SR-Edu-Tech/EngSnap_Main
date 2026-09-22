using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit3
{
    public class FiveSingersController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit3";
        [SerializeField] private string topicName = "FiveSingers";

        [Header("Data Reference")]
        [SerializeField] private FiveSingersData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Phase 1: Stage & Throat Buzz UI")]
        [SerializeField] private GameObject stagePanel;
        [SerializeField] private Button[] stageVowelButtons = new Button[5]; // A, E, I, O, U
        [SerializeField] private Button feelBuzzButton;
        [SerializeField] private Button whisperConsonantButton;
        [SerializeField] private Button playVowelSongButton; // A-E-I-O-U Song Section Button
        [SerializeField] private GameObject feelBuzzGlowHighlight;
        [SerializeField] private GameObject whisperConsonantGlowHighlight;

        [Header("Phase 2 & 3: Word Tiles & Picture UI")]
        [SerializeField] private GameObject wordPanel;
        [SerializeField] private Image wordPictureImage;
        [SerializeField] private TMP_Text wordDisplayTMP;
        [SerializeField] private Button[] letterTileButtons;
        [SerializeField] private TMP_Text[] letterTileTexts;
        [SerializeField] private Image[] letterTileHighlights;
        [SerializeField] private GameObject missingVowelContainer;
        [SerializeField] private Button missingVowelChoiceButton;
        [SerializeField] private TMP_Text missingVowelChoiceText;

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
        [SerializeField] private AudioClip throatBuzzSfx;
        [SerializeField] private AudioClip wordSnapSfx;

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

        [Tooltip("The current panel to hide when Continue is clicked.")]
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentRoundIndex = 0;
        private int totalFindRounds = 6;
        private int totalBrokenRounds = 3;
        private bool isBrokenPhase = false;
        private bool isTransitioning = false;
        private WordSingerItem currentWordItem;
        private Coroutine vowelWobbleCoroutine;

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
            StartActivity();
        }

        private void OnDisable()
        {
            StopVowelWobbleAnimation();
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
            if (feelBuzzButton != null)
                feelBuzzButton.onClick.AddListener(OnFeelBuzzClicked);
            if (whisperConsonantButton != null)
                whisperConsonantButton.onClick.AddListener(OnWhisperConsonantClicked);
            if (playVowelSongButton != null)
                playVowelSongButton.onClick.AddListener(OnPlayVowelSongClicked);

            if (stageVowelButtons != null)
            {
                for (int i = 0; i < stageVowelButtons.Length; i++)
                {
                    int index = i;
                    if (stageVowelButtons[i] != null)
                        stageVowelButtons[i].onClick.AddListener(() => OnStageVowelTapped(index));
                }
            }

            if (letterTileButtons != null)
            {
                for (int i = 0; i < letterTileButtons.Length; i++)
                {
                    int index = i;
                    if (letterTileButtons[i] != null)
                        letterTileButtons[i].onClick.AddListener(() => OnLetterTileTapped(index));
                }
            }

            if (missingVowelChoiceButton != null)
            {
                missingVowelChoiceButton.onClick.AddListener(OnMissingVowelChoiceTapped);
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
            currentRoundIndex = 0;
            isBrokenPhase = false;
            isTransitioning = false;

            if (stagePanel != null) stagePanel.SetActive(true);
            if (wordPanel != null) wordPanel.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);
            if (feelBuzzGlowHighlight != null) feelBuzzGlowHighlight.SetActive(false);
            if (whisperConsonantGlowHighlight != null) whisperConsonantGlowHighlight.SetActive(false);

            StartVowelWobbleAnimation();
            UpdateProgressUI(0f);
            StartCoroutine(PlayIntroSequence());
        }

        private void StartVowelWobbleAnimation()
        {
            if (vowelWobbleCoroutine != null) StopCoroutine(vowelWobbleCoroutine);
            vowelWobbleCoroutine = StartCoroutine(ContinuousVowelWobbleCoroutine());
        }

        private void StopVowelWobbleAnimation()
        {
            if (vowelWobbleCoroutine != null)
            {
                StopCoroutine(vowelWobbleCoroutine);
                vowelWobbleCoroutine = null;
            }
            if (stageVowelButtons != null)
            {
                for (int i = 0; i < stageVowelButtons.Length; i++)
                {
                    if (stageVowelButtons[i] != null)
                    {
                        RectTransform rt = stageVowelButtons[i].GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.localRotation = Quaternion.identity;
                            rt.localScale = Vector3.one;
                        }
                    }
                }
            }
        }

        private IEnumerator ContinuousVowelWobbleCoroutine()
        {
            float elapsed = 0f;
            while (stagePanel != null && stagePanel.activeSelf)
            {
                elapsed += Time.deltaTime;
                if (stageVowelButtons != null)
                {
                    for (int i = 0; i < stageVowelButtons.Length; i++)
                    {
                        if (stageVowelButtons[i] != null)
                        {
                            RectTransform rt = stageVowelButtons[i].GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                float offset = i * 0.4f;
                                float rotAngle = Mathf.Sin((elapsed * 3.5f) + offset) * 5.0f;
                                float scalePulse = 1.0f + (Mathf.Sin((elapsed * 4.5f) + offset) * 0.05f);
                                rt.localRotation = Quaternion.Euler(0f, 0f, rotAngle);
                                rt.localScale = new Vector3(scalePulse, scalePulse, 1f);
                            }
                        }
                    }
                }
                yield return null;
            }
        }

        private IEnumerator PlayIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Welcome to Vowel Valley! Five letters live here, and all five of them can SING.");

            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            SetDialogue("Put your hand on your throat and sing with me — aaaaa. Feel the buzz?");
            if (feelBuzzButton != null) feelBuzzButton.gameObject.SetActive(true);
            if (whisperConsonantButton != null) whisperConsonantButton.gameObject.SetActive(true);
            if (playVowelSongButton != null) playVowelSongButton.gameObject.SetActive(true);

            if (activityData != null && activityData.feelTheBuzzClip != null)
            {
                yield return PlayVoiceClip(activityData.feelTheBuzzClip);
            }

            isTransitioning = false;
        }

        private void OnStageVowelTapped(int index)
        {
            if (isTransitioning || IsAudioPlaying()) return;
            StartCoroutine(PlayStageVowelSequence(index));
        }

        private IEnumerator PlayStageVowelSequence(int index)
        {
            isTransitioning = true;
            string[] vowels = { "A", "E", "I", "O", "U" };
            string vowelName = (index >= 0 && index < vowels.Length) ? vowels[index] : "Vowel";
            SetDialogue($"The {vowelName} is singing! Feel the throat buzz!");

            PlaySFX(correctChimeSfx);

            if (activityData != null && activityData.vowelSingers != null && index < activityData.vowelSingers.Length && activityData.vowelSingers[index].sungVowelClip != null)
            {
                yield return PlayVoiceClip(activityData.vowelSingers[index].sungVowelClip);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }
            isTransitioning = false;
        }

        private void OnFeelBuzzClicked()
        {
            if (isTransitioning || IsAudioPlaying()) return;

            if (feelBuzzGlowHighlight != null) feelBuzzGlowHighlight.SetActive(true);
            if (whisperConsonantGlowHighlight != null) whisperConsonantGlowHighlight.SetActive(false);

            if (feelBuzzButton != null) StartCoroutine(AnimateButtonTapPop(feelBuzzButton.transform));

            StartCoroutine(PlayFeelBuzzSequence());
        }

        private IEnumerator PlayFeelBuzzSequence()
        {
            isTransitioning = true;
            PlaySFX(throatBuzzSfx != null ? throatBuzzSfx : correctChimeSfx);
            SetDialogue("Aaaaa! Feel the vibration in your throat! All vowels are voiced!");

            if (activityData != null && activityData.feelTheBuzzClip != null)
            {
                yield return PlayVoiceClip(activityData.feelTheBuzzClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
            isTransitioning = false;
        }

        private void OnWhisperConsonantClicked()
        {
            if (isTransitioning || IsAudioPlaying()) return;

            if (whisperConsonantGlowHighlight != null) whisperConsonantGlowHighlight.SetActive(true);
            if (feelBuzzGlowHighlight != null) feelBuzzGlowHighlight.SetActive(false);

            if (whisperConsonantButton != null) StartCoroutine(AnimateButtonTapPop(whisperConsonantButton.transform));

            StartCoroutine(PlayWhisperConsonantSequence());
        }

        private IEnumerator AnimateButtonTapPop(Transform targetTransform)
        {
            if (targetTransform == null) yield break;
            Vector3 originalScale = targetTransform.localScale;
            targetTransform.localScale = originalScale * 1.15f;
            yield return new WaitForSeconds(0.15f);
            targetTransform.localScale = originalScale;
        }

        private IEnumerator PlayWhisperConsonantSequence()
        {
            isTransitioning = true;
            SetDialogue("Now whisper /t/. No buzz! Vowels always turn the buzzer on.");

            if (activityData != null && activityData.whisperConsonantClip != null)
            {
                yield return PlayVoiceClip(activityData.whisperConsonantClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
            StartCoroutine(TransitionToFindSingerPhase());
        }

        private void OnPlayVowelSongClicked()
        {
            if (isTransitioning || IsAudioPlaying()) return;
            StartCoroutine(PlayVowelSongSequence());
        }

        private IEnumerator PlayVowelSongSequence()
        {
            isTransitioning = true;
            SetDialogue("Sing with me! A - E - I - O - U!");
            PlaySFX(correctChimeSfx);

            if (activityData != null && activityData.vowelSongAudioClip != null)
            {
                yield return PlayVoiceClip(activityData.vowelSongAudioClip);
            }
            else
            {
                yield return new WaitForSeconds(2.5f);
            }

            isTransitioning = false;
        }

        private IEnumerator TransitionToFindSingerPhase()
        {
            yield return new WaitForSeconds(0.5f);
            StopVowelWobbleAnimation();
            if (stagePanel != null) stagePanel.SetActive(false);
            if (wordPanel != null) wordPanel.SetActive(true);
            isBrokenPhase = false;
            LoadFindSingerRound(0);
        }

        private void LoadFindSingerRound(int index)
        {
            if (activityData == null || activityData.findSingerItems == null || index >= activityData.findSingerItems.Length)
            {
                StartBrokenWordPhase();
                return;
            }

            currentRoundIndex = index;
            currentWordItem = activityData.findSingerItems[index];

            if (wordPictureImage != null && currentWordItem.pictureSprite != null)
                wordPictureImage.sprite = currentWordItem.pictureSprite;

            if (wordDisplayTMP != null) wordDisplayTMP.text = currentWordItem.fullWord;

            SetupLetterTiles(currentWordItem.fullWord);
            if (missingVowelContainer != null) missingVowelContainer.SetActive(false);

            SetDialogue($"Find the singer in '{currentWordItem.fullWord}'. Tap the vowel!");
            if (activityData != null && activityData.findTheSingerPromptClip != null && index == 0)
            {
                PlayVoiceClipNonBlocking(activityData.findTheSingerPromptClip);
            }

            UpdateProgressUI((float)index / 9f);
        }

        private void SetupLetterTiles(string word)
        {
            if (letterTileButtons == null) return;

            for (int i = 0; i < letterTileButtons.Length; i++)
            {
                if (i < word.Length)
                {
                    letterTileButtons[i].gameObject.SetActive(true);
                    if (letterTileTexts[i] != null) letterTileTexts[i].text = word[i].ToString();
                    if (letterTileHighlights[i] != null) letterTileHighlights[i].gameObject.SetActive(false);
                }
                else
                {
                    letterTileButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnLetterTileTapped(int tileIndex)
        {
            if (isTransitioning || IsAudioPlaying() || currentWordItem == null) return;

            string word = currentWordItem.fullWord;
            if (tileIndex < 0 || tileIndex >= word.Length) return;

            char tappedChar = char.ToLowerInvariant(word[tileIndex]);
            bool isVowel = (tappedChar == 'a' || tappedChar == 'e' || tappedChar == 'i' || tappedChar == 'o' || tappedChar == 'u');

            if (isVowel)
            {
                StartCoroutine(HandleCorrectVowelTileTapped(tileIndex));
            }
            else
            {
                StartCoroutine(HandleWrongConsonantTileTapped(tileIndex));
            }
        }

        private IEnumerator HandleCorrectVowelTileTapped(int tileIndex)
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            PlaySFX(wordSnapSfx);

            if (letterTileHighlights != null && tileIndex < letterTileHighlights.Length && letterTileHighlights[tileIndex] != null)
            {
                letterTileHighlights[tileIndex].gameObject.SetActive(true);
            }

            char vChar = char.ToUpperInvariant(currentWordItem.fullWord[tileIndex]);
            SetDialogue($"Yes! The {vChar} is singing in the middle of '{currentWordItem.fullWord}'.");

            if (currentWordItem.wordAudioClip != null)
            {
                yield return PlayVoiceClip(currentWordItem.wordAudioClip);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            currentRoundIndex++;
            isTransitioning = false;

            if (currentRoundIndex < totalFindRounds && currentRoundIndex < activityData.findSingerItems.Length)
            {
                LoadFindSingerRound(currentRoundIndex);
            }
            else
            {
                StartBrokenWordPhase();
            }
        }

        private IEnumerator HandleWrongConsonantTileTapped(int tileIndex)
        {
            PlaySFX(retryGentleSfx);
            SetDialogue("That's a consonant! Lips or tongue touch. Find the vowel that sings!");
            yield return new WaitForSeconds(0.6f);
        }

        private void StartBrokenWordPhase()
        {
            isBrokenPhase = true;
            currentRoundIndex = 0;

            SetDialogue("Uh-oh — I took the vowel away! Can you read it?");
            if (activityData != null && activityData.wordBrokeDemoClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.wordBrokeDemoClip);
            }

            LoadBrokenWordRound(0);
        }

        private void LoadBrokenWordRound(int index)
        {
            if (activityData == null || activityData.brokenWordItems == null || index >= activityData.brokenWordItems.Length)
            {
                StartCoroutine(CompleteStopSequence());
                return;
            }

            currentRoundIndex = index;
            currentWordItem = activityData.brokenWordItems[index];

            if (wordPictureImage != null && currentWordItem.pictureSprite != null)
                wordPictureImage.sprite = currentWordItem.pictureSprite;

            if (wordDisplayTMP != null) wordDisplayTMP.text = currentWordItem.gapWord;

            SetupLetterTiles(currentWordItem.gapWord);

            if (missingVowelContainer != null) missingVowelContainer.SetActive(true);
            if (missingVowelChoiceText != null) missingVowelChoiceText.text = currentWordItem.vowelChar.ToString().ToUpper();

            SetDialogue("Put the singer back! Every word needs a vowel.");
            UpdateProgressUI((6f + index) / 9f);
        }

        private void OnMissingVowelChoiceTapped()
        {
            if (isTransitioning || IsAudioPlaying() || currentWordItem == null) return;
            StartCoroutine(HandlePutSingerBackCorrect());
        }

        private IEnumerator HandlePutSingerBackCorrect()
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            PlaySFX(wordSnapSfx);

            if (wordDisplayTMP != null) wordDisplayTMP.text = currentWordItem.fullWord;
            if (missingVowelContainer != null) missingVowelContainer.SetActive(false);

            SetDialogue($" {currentWordItem.vowelChar} . {currentWordItem.fullWord.ToUpper()}! Every word needs a vowel!");

            if (currentWordItem.wordAudioClip != null)
            {
                yield return PlayVoiceClip(currentWordItem.wordAudioClip);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            currentRoundIndex++;
            isTransitioning = false;

            if (currentRoundIndex < totalBrokenRounds && currentRoundIndex < activityData.brokenWordItems.Length)
            {
                LoadBrokenWordRound(currentRoundIndex);
            }
            else
            {
                StartCoroutine(CompleteStopSequence());
            }
        }

        private IEnumerator CompleteStopSequence()
        {
            if (wordPanel != null) wordPanel.SetActive(false);
            UpdateProgressUI(1f);

            SetDialogue("Every word needs a singer! You found them all!");
            if (activityData != null && activityData.closingVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.closingVoiceClip);
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
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
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

            TopicProgressUI.RefreshAllTicks();
        }

        public void GoToPreviousPanel()
        {
            DeactivateMascots();
            if (currentPanel != null) currentPanel.SetActive(false);
            if (unitContentPanel != null) unitContentPanel.SetActive(true);
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
