using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit4
{
    public class VowelSwapMachineController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit4";
        [SerializeField] private string topicName = "VowelSwapMachine";

        [Header("Data Reference")]
        [SerializeField] private VowelSwapMachineData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Phase 1 & 2: Vowel Swap Machine UI")]
        [SerializeField] private GameObject swapMachinePanel;
        [SerializeField] private TMP_Text prefixSlotText;
        [SerializeField] private TMP_Text middleVowelSlotText;
        [SerializeField] private TMP_Text suffixSlotText;
        [SerializeField] private Button[] vowelDialButtons = new Button[5]; // a, e, i, o, u
        [SerializeField] private Button vowelUpButton;
        [SerializeField] private Button vowelDownButton;
        [SerializeField] private Button prevSetButton;
        [SerializeField] private Button nextSetButton;
        [SerializeField] private GameObject realWordCardObject;
        [SerializeField] private Image realWordImage;
        [SerializeField] private TMP_Text realWordText;
        [SerializeField] private GameObject sillyMonsterPopUpObject;
        [SerializeField] private Image sillyMonsterImage;
        [SerializeField] private Button startQuizButton;

        [Header("Phase 3: Real Word Quiz UI")]
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private TMP_Text quizPromptTMP;
        [SerializeField] private Button[] wordChoiceButtons = new Button[5];
        [SerializeField] private TMP_Text[] wordChoiceTexts = new TMP_Text[5];

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

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip starPopSfx;
        [SerializeField] private AudioClip dialClickSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentSetIndex = 0;
        private int currentVowelIndex = 0;
        private int quizRoundIndex = 0;
        private int totalQuizRounds = 4;
        private bool isQuizActive = false;
        private bool isTransitioning = false;
        private RealWordQuizRound currentQuizRound;
        private int failAttempts = 0;
        private Coroutine momoPulseCoroutine;

        private HashSet<int> visitedVowelsInCurrentSet = new HashSet<int>();
        private HashSet<int> completedSwapSets = new HashSet<int>();
        private Coroutine autoAdvanceCoroutine;

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
            if (startQuizButton != null) startQuizButton.onClick.AddListener(StartQuizRound);
            if (prevSetButton != null) prevSetButton.onClick.AddListener(PrevSwapSet);
            if (nextSetButton != null) nextSetButton.onClick.AddListener(NextSwapSet);
            if (vowelUpButton != null) vowelUpButton.onClick.AddListener(OnVowelUpClicked);
            if (vowelDownButton != null) vowelDownButton.onClick.AddListener(OnVowelDownClicked);

            if (vowelDialButtons != null)
            {
                for (int i = 0; i < vowelDialButtons.Length; i++)
                {
                    int index = i;
                    if (vowelDialButtons[i] != null)
                        vowelDialButtons[i].onClick.AddListener(() => OnVowelDialTapped(index));
                }
            }

            for (int i = 0; i < wordChoiceButtons.Length; i++)
            {
                int index = i;
                if (wordChoiceButtons[i] != null)
                    wordChoiceButtons[i].onClick.AddListener(() => OnWordChoiceSelected(index));
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
            currentSetIndex = 0;
            currentVowelIndex = 0;
            quizRoundIndex = 0;
            isQuizActive = false;
            isTransitioning = false;
            completedSwapSets.Clear();
            visitedVowelsInCurrentSet.Clear();

            if (swapMachinePanel != null) swapMachinePanel.SetActive(true);
            if (quizPanel != null) quizPanel.SetActive(false);
            if (sillyMonsterPopUpObject != null) sillyMonsterPopUpObject.SetActive(false);
            if (realWordCardObject != null) realWordCardObject.SetActive(true);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);

            LoadSwapSet(0);
            UpdateProgressUI(0f);
            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("This is the Vowel Swap Machine. The outside letters stay… but the middle one can change!");

            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            if (startQuizButton != null) startQuizButton.gameObject.SetActive(true);
            isTransitioning = false;
        }

        private void LoadSwapSet(int setIdx)
        {
            if (activityData == null || activityData.swapSets == null || setIdx >= activityData.swapSets.Length) return;

            currentSetIndex = setIdx;
            currentVowelIndex = 0;
            visitedVowelsInCurrentSet.Clear();
            visitedVowelsInCurrentSet.Add(0);

            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            SwapMachineSet set = activityData.swapSets[setIdx];
            if (prefixSlotText != null) prefixSlotText.text = set.prefixLetter;
            if (suffixSlotText != null) suffixSlotText.text = set.suffixLetter;

            UpdateMachineVowelDisplay();
        }

        private void PrevSwapSet()
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null) return;
            PlaySFX(dialClickSfx);
            int prevIdx = (currentSetIndex - 1 + activityData.swapSets.Length) % activityData.swapSets.Length;
            LoadSwapSet(prevIdx);
        }

        private void NextSwapSet()
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null) return;
            PlaySFX(dialClickSfx);
            int nextIdx = (currentSetIndex + 1) % activityData.swapSets.Length;
            LoadSwapSet(nextIdx);
        }

        public void OnVowelUpClicked()
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null) return;
            int prevVowel = (currentVowelIndex - 1 + 5) % 5;
            OnVowelDialTapped(prevVowel);
        }

        public void OnVowelDownClicked()
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null) return;
            int nextVowel = (currentVowelIndex + 1) % 5;
            OnVowelDialTapped(nextVowel);
        }

        public void OnVowelDialTapped(int vowelIdx)
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null) return;

            currentVowelIndex = vowelIdx;
            visitedVowelsInCurrentSet.Add(vowelIdx);
            PlaySFX(dialClickSfx);
            UpdateMachineVowelDisplay();

            CheckAutoAdvance();
        }

        private void CheckAutoAdvance()
        {
            if (visitedVowelsInCurrentSet.Count >= 5 && !completedSwapSets.Contains(currentSetIndex))
            {
                completedSwapSets.Add(currentSetIndex);
                float totalSets = (activityData != null && activityData.swapSets != null && activityData.swapSets.Length > 0) ? activityData.swapSets.Length : 6f;
                UpdateProgressUI((float)completedSwapSets.Count / totalSets);

                if (autoAdvanceCoroutine != null) StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
            }
        }

        private IEnumerator AutoAdvanceRoutine()
        {
            yield return new WaitForSeconds(1.8f);

            if (activityData != null && activityData.swapSets != null && completedSwapSets.Count >= activityData.swapSets.Length)
            {
                SetDialogue("Great job! You explored all the Swap Machine words! Tap the Quiz button to test your skills!");
                if (startQuizButton != null) startQuizButton.gameObject.SetActive(true);
            }
            else if (activityData != null && activityData.swapSets != null && currentSetIndex < activityData.swapSets.Length - 1)
            {
                NextSwapSet();
            }
        }

        private void UpdateMachineVowelDisplay()
        {
            if (activityData == null || currentSetIndex >= activityData.swapSets.Length) return;

            SwapMachineSet currentSet = activityData.swapSets[currentSetIndex];
            if (currentVowelIndex >= currentSet.vowelOptions.Length) return;

            SwapWordOption opt = currentSet.vowelOptions[currentVowelIndex];
            if (middleVowelSlotText != null) middleVowelSlotText.text = opt.vowelChar.ToString();

            if (opt.isRealWord)
            {
                if (sillyMonsterPopUpObject != null) sillyMonsterPopUpObject.SetActive(false);
                if (realWordCardObject != null) realWordCardObject.SetActive(true);

                if (realWordText != null) realWordText.text = opt.fullWord;
                if (realWordImage != null && opt.pictureSprite != null) realWordImage.sprite = opt.pictureSprite;

                SetDialogue($"Real Word: {opt.fullWord.ToUpper()}!");
                if (opt.wordAudioClip != null) PlayVoiceClipNonBlocking(opt.wordAudioClip);
            }
            else
            {
                if (realWordCardObject != null) realWordCardObject.SetActive(false);
                if (sillyMonsterPopUpObject != null) sillyMonsterPopUpObject.SetActive(true);

                if (activityData.sillyMonsterSprites != null && activityData.sillyMonsterSprites.Length > 0)
                {
                    int monsterIdx = Random.Range(0, activityData.sillyMonsterSprites.Length);
                    if (sillyMonsterImage != null && activityData.sillyMonsterSprites[monsterIdx] != null)
                        sillyMonsterImage.sprite = activityData.sillyMonsterSprites[monsterIdx];
                }

                SetDialogue("Bppppt! That is not a word — it is just a silly sound!");
                PlaySFX(activityData != null ? activityData.nonsenseMonsterRaspberrySfx : retryGentleSfx);
                if (activityData != null && activityData.nonsenseMonsterVoiceClip != null)
                    PlayVoiceClipNonBlocking(activityData.nonsenseMonsterVoiceClip);
            }
        }

        private void StartQuizRound()
        {
            isQuizActive = true;
            quizRoundIndex = 0;

            if (swapMachinePanel != null) swapMachinePanel.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(true);

            SetDialogue("Which of these are REAL words? Tap them!");
            LoadQuizRound(0);
        }

        private void LoadQuizRound(int index)
        {
            if (activityData == null || activityData.realWordRounds == null || index >= activityData.realWordRounds.Length)
            {
                StartCoroutine(CompleteQuizSequence());
                return;
            }

            quizRoundIndex = index;
            failAttempts = 0;
            StopMomoPulse();
            if (momoHintObject != null) momoHintObject.SetActive(false);

            currentQuizRound = activityData.realWordRounds[index];
            if (quizPromptTMP != null) quizPromptTMP.text = currentQuizRound.promptText;

            SetDialogue($"Find the real words for pattern '{currentQuizRound.patternLabel}'!");
            SetupQuizChoiceButtons();
            UpdateProgressUI((float)index / totalQuizRounds);
        }

        private void SetupQuizChoiceButtons()
        {
            for (int i = 0; i < wordChoiceButtons.Length; i++)
            {
                if (i < currentQuizRound.wordOptions.Length)
                {
                    wordChoiceButtons[i].gameObject.SetActive(true);
                    wordChoiceButtons[i].transform.localScale = Vector3.one;

                    Image btnImg = wordChoiceButtons[i].GetComponent<Image>();
                    if (btnImg != null) btnImg.color = Color.white;

                    if (wordChoiceTexts[i] != null) wordChoiceTexts[i].text = currentQuizRound.wordOptions[i];
                }
                else
                {
                    wordChoiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnWordChoiceSelected(int optionIndex)
        {
            if (isTransitioning || IsAudioPlaying() || currentQuizRound == null) return;

            bool isReal = (optionIndex >= 0 && optionIndex < currentQuizRound.isRealWord.Length) ? currentQuizRound.isRealWord[optionIndex] : false;
            Button tappedBtn = (optionIndex >= 0 && optionIndex < wordChoiceButtons.Length) ? wordChoiceButtons[optionIndex] : null;

            if (tappedBtn != null)
            {
                TriggerWiggle(tappedBtn.GetComponent<RectTransform>());
                Image btnImg = tappedBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isReal ? correctColor : wrongColor;
                    if (!isReal) StartCoroutine(ResetButtonColor(btnImg, 0.8f));
                }
            }

            if (isReal)
            {
                StopMomoPulse();
                StartCoroutine(HandleChoiceCorrect(optionIndex));
            }
            else
            {
                failAttempts++;
                if (failAttempts >= 2 && momoHintObject != null)
                {
                    momoHintObject.SetActive(true);
                    SetDialogue("Momo says: Tap the glowing real word!");
                    TriggerMomoRealWordPulse();
                }
                StartCoroutine(HandleChoiceWrong());
            }
        }

        private IEnumerator ResetButtonColor(Image targetImage, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (targetImage != null) targetImage.color = Color.white;
        }

        private IEnumerator HandleChoiceCorrect(int index)
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue($"Yes! '{currentQuizRound.wordOptions[index].ToUpper()}' is a real word!");
            yield return new WaitForSeconds(0.8f);

            quizRoundIndex++;
            isTransitioning = false;

            if (quizRoundIndex < totalQuizRounds && quizRoundIndex < activityData.realWordRounds.Length)
            {
                LoadQuizRound(quizRoundIndex);
            }
            else
            {
                StartCoroutine(CompleteQuizSequence());
            }
        }

        private IEnumerator HandleChoiceWrong()
        {
            PlaySFX(activityData != null ? activityData.nonsenseMonsterRaspberrySfx : retryGentleSfx);
            SetDialogue("Bppppt! That is just a silly sound. Try again!");
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator CompleteQuizSequence()
        {
            if (quizPanel != null) quizPanel.SetActive(false);
            UpdateProgressUI(1f);

            SetDialogue("You changed the middle and changed the word. That is what vowels do!");
            if (activityData != null && activityData.swapSuccessClosingClip != null)
            {
                yield return PlayVoiceClip(activityData.swapSuccessClosingClip);
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
            StopMomoPulse();
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private void TriggerMomoRealWordPulse()
        {
            StopMomoPulse();
            if (currentQuizRound == null || currentQuizRound.isRealWord == null) return;

            for (int i = 0; i < currentQuizRound.isRealWord.Length; i++)
            {
                if (currentQuizRound.isRealWord[i] && i < wordChoiceButtons.Length && wordChoiceButtons[i] != null)
                {
                    RectTransform rect = wordChoiceButtons[i].GetComponent<RectTransform>();
                    momoPulseCoroutine = StartCoroutine(PulseCorrectAnswerLoop(rect));
                    break;
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

            if (wordChoiceButtons != null)
            {
                for (int i = 0; i < wordChoiceButtons.Length; i++)
                {
                    if (wordChoiceButtons[i] != null)
                        wordChoiceButtons[i].transform.localScale = Vector3.one;
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
