using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Common.ShortVowels
{
    public class ShortVowelBuildController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [Tooltip("Target Unit ID e.g. 'Unit6', 'Unit7', 'Unit8', 'Unit9', 'Unit10'.")]
        [SerializeField] private string unitID = "Unit6";

        [Tooltip("Topic key name for progress tracking e.g. 'BuildAWord'.")]
        [SerializeField] private string topicName = "BuildAWord";

        [Header("Mascot & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Sound Boxes UI (c - a - t)")]
        [SerializeField] private TMP_Text box1Text;
        [SerializeField] private TMP_Text box2Text;
        [SerializeField] private TMP_Text box3Text;
        [SerializeField] private Button box1Button;
        [SerializeField] private Button box2Button;
        [SerializeField] private Button box3Button;
        [SerializeField] private RectTransform box1Rect;
        [SerializeField] private RectTransform box2Rect;
        [SerializeField] private RectTransform box3Rect;

        [Header("Button Indicator Glow Objects")]
        [Tooltip("Optional glow/highlight GameObject active when Box 1 is the next expected tap.")]
        [SerializeField] private GameObject box1GlowObject;

        [Tooltip("Optional glow/highlight GameObject active when Box 2 is the next expected tap.")]
        [SerializeField] private GameObject box2GlowObject;

        [Tooltip("Optional glow/highlight GameObject active when Box 3 is the next expected tap.")]
        [SerializeField] private GameObject box3GlowObject;

        [Tooltip("Optional glow/highlight GameObject active when Blend Button is ready.")]
        [SerializeField] private GameObject blendButtonGlowObject;

        [Tooltip("Optional glow/highlight GameObject active when Swap Letter Button is ready.")]
        [SerializeField] private GameObject swapButtonGlowObject;

        [Header("Blend Controls & Display")]
        [SerializeField] private Button blendButton;
        [SerializeField] private TMP_Text resultWordText;
        [SerializeField] private Image resultPictureImage;

        [Header("Swappable Letter Buttons")]
        [SerializeField] private Button swapLetterButton;
        [SerializeField] private TMP_Text swapLetterButtonText;

        [Header("Build Data Sets")]
        [SerializeField] private ShortVowelBuildData[] buildRoundData;

        [Header("Voice Script & SFX Audio Clips")]
        [SerializeField] private AudioClip introClip;             // "Let's build words! Tap each sound, then blend!"
        [SerializeField] private AudioClip completionPraiseClip;  // "Awesome job building words!"
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip slideBlendSfx;
        [SerializeField] private AudioClip tileSwapSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;
        

        private int currentRoundIndex = 0;
        private int currentSwapStepIndex = -1; // -1 means initial word
        private bool isBlended = false;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;

        private string activeSound1 = "";
        private string activeSound2 = "";
        private string activeSound3 = "";
        private AudioClip activeSound1Clip;
        private AudioClip activeSound2Clip;
        private AudioClip activeSound3Clip;

        private Coroutine activeIndicatorWiggleCoroutine;
        private Sprite defaultResultPictureSprite;

        public bool IsTransitioning => isTransitioning;
        public string UnitID => unitID;

        private void Awake()
        {
            EnsureAudioSources();
            CacheDefaultPictureSprite();
        }

        private void CacheDefaultPictureSprite()
        {
            if (defaultResultPictureSprite == null && resultPictureImage != null && resultPictureImage.sprite != null)
            {
                defaultResultPictureSprite = resultPictureImage.sprite;
            }
        }

        private void SetResultPicture(Sprite wordSprite)
        {
            if (resultPictureImage == null) return;

            CacheDefaultPictureSprite();
            resultPictureImage.gameObject.SetActive(true);
            resultPictureImage.enabled = true;

            EngSnap.Common.DefaultImageRestorer restorer = resultPictureImage.GetComponent<EngSnap.Common.DefaultImageRestorer>();
            if (restorer != null)
            {
                restorer.SetImage(wordSprite);
            }
            else
            {
                if (wordSprite != null)
                {
                    resultPictureImage.sprite = wordSprite;
                }
                else if (defaultResultPictureSprite != null)
                {
                    resultPictureImage.sprite = defaultResultPictureSprite;
                }
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

            if (box1Button != null) box1Button.onClick.AddListener(OnBox1Clicked);
            if (box2Button != null) box2Button.onClick.AddListener(OnBox2Clicked);
            if (box3Button != null) box3Button.onClick.AddListener(OnBox3Clicked);
            if (blendButton != null) blendButton.onClick.AddListener(OnBlendButtonClicked);
            if (swapLetterButton != null) swapLetterButton.onClick.AddListener(OnSwapLetterClicked);

            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }

            ResetLevel();
        }

        private void OnEnable()
        {
            ResetLevel();
            StartCoroutine(StartIntroOnNextFrame());
        }

        private IEnumerator StartIntroOnNextFrame()
        {
            yield return null;
            if (gameObject.activeInHierarchy && !isActivityCompleted)
            {
                yield return StartCoroutine(IntroSequence());
            }
        }

        public void ResetLevel()
        {
            currentRoundIndex = 0;
            currentSwapStepIndex = -1;
            isBlended = false;
            isTransitioning = false;
            isActivityCompleted = false;

            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);

            ClearAllGlowsAndWiggles();
            LoadRound(currentRoundIndex);
            IndicateStep(0);
            SetSubtitles("Let's build words! Tap each sound, then blend!");
        }

        private void LoadRound(int roundIdx)
        {
            if (buildRoundData == null || buildRoundData.Length == 0) return;

            if (roundIdx < buildRoundData.Length)
            {
                ShortVowelBuildData data = buildRoundData[roundIdx];
                if (data != null)
                {
                    activeSound1 = data.sound1;
                    activeSound2 = data.sound2;
                    activeSound3 = data.sound3;
                    activeSound1Clip = data.sound1Clip;
                    activeSound2Clip = data.sound2Clip;
                    activeSound3Clip = data.sound3Clip;

                    if (box1Text != null) box1Text.text = activeSound1;
                    if (box2Text != null) box2Text.text = activeSound2;
                    if (box3Text != null) box3Text.text = activeSound3;

                    if (resultWordText != null) resultWordText.text = data.initialWord;
                    SetResultPicture(data.initialWordSprite);

                    UpdateSwapButtonUI(data);
                }
            }
            isBlended = false;
        }

        private void UpdateSwapButtonUI(ShortVowelBuildData data)
        {
            if (swapLetterButton == null) return;

            if (data != null && data.swapSteps != null && currentSwapStepIndex + 1 < data.swapSteps.Length)
            {
                swapLetterButton.gameObject.SetActive(true);
                LetterSwapStep nextStep = data.swapSteps[currentSwapStepIndex + 1];
                if (swapLetterButtonText != null)
                {
                    swapLetterButtonText.text = $"Swap to '{nextStep.newLetter.ToUpper()}'";
                }
            }
            else
            {
                swapLetterButton.gameObject.SetActive(false);
            }
        }

        public void OnBox1Clicked()
        {
            if (isTransitioning) return;
            if (activeSound1Clip != null) PlayVoice(activeSound1Clip);
            SetSubtitles($"Sound: '{activeSound1}'");
            IndicateStep(1); // Indicate Box 2 next
        }

        public void OnBox2Clicked()
        {
            if (isTransitioning) return;
            if (activeSound2Clip != null) PlayVoice(activeSound2Clip);
            SetSubtitles($"Middle vowel sound: '{activeSound2}'");
            IndicateStep(2); // Indicate Box 3 next
        }

        public void OnBox3Clicked()
        {
            if (isTransitioning) return;
            if (activeSound3Clip != null) PlayVoice(activeSound3Clip);
            SetSubtitles($"Sound: '{activeSound3}'");
            IndicateStep(3); // Indicate Blend button next
        }

        public void OnBlendButtonClicked()
        {
            if (isTransitioning || isBlended) return;
            StartCoroutine(BlendSequence());
        }

        private IEnumerator BlendSequence()
        {
            isTransitioning = true;
            ClearAllGlowsAndWiggles();

            if (slideBlendSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(slideBlendSfx);
            }

            // Run synchronized 3-box wiggle animation
            yield return StartCoroutine(WiggleThreeBoxesSequence(0.45f));

            ShortVowelBuildData data = GetCurrentBuildData();
            AudioClip wordClip = null;
            Sprite wordSprite = null;
            string blendedWord = "";

            if (data != null)
            {
                if (currentSwapStepIndex >= 0 && data.swapSteps != null && currentSwapStepIndex < data.swapSteps.Length)
                {
                    LetterSwapStep step = data.swapSteps[currentSwapStepIndex];
                    blendedWord = step.resultingWord;
                    wordClip = step.blendedWordClip;
                    wordSprite = step.wordSprite;
                }
                else
                {
                    blendedWord = data.initialWord;
                    wordClip = data.initialBlendedClip;
                    wordSprite = data.initialWordSprite;
                }
            }

            SetResultPicture(wordSprite);

            if (resultWordText != null) resultWordText.text = blendedWord;

            SetSubtitles($"Blend! '{blendedWord}'!");

            if (wordClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = wordClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(wordClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            isBlended = true;
            isTransitioning = false;

            if (data != null && data.swapSteps != null && currentSwapStepIndex + 1 < data.swapSteps.Length)
            {
                IndicateStep(4); // Indicate Swap Letter Button next
            }
            else if (data != null && (data.swapSteps == null || currentSwapStepIndex + 1 >= data.swapSteps.Length))
            {
                currentRoundIndex++;
                if (currentRoundIndex >= buildRoundData.Length && !isActivityCompleted)
                {
                    StartCoroutine(CompletionSequence());
                }
                else
                {
                    currentSwapStepIndex = -1;
                    LoadRound(currentRoundIndex);
                    IndicateStep(0); // Start next round indicating Box 1
                }
            }
        }

        public void OnSwapLetterClicked()
        {
            if (isTransitioning) return;

            ShortVowelBuildData data = GetCurrentBuildData();
            if (data == null || data.swapSteps == null) return;

            if (currentSwapStepIndex + 1 < data.swapSteps.Length)
            {
                currentSwapStepIndex++;
                StartCoroutine(PerformLetterSwapSequence(data.swapSteps[currentSwapStepIndex]));
            }
        }

        private IEnumerator PerformLetterSwapSequence(LetterSwapStep step)
        {
            isTransitioning = true;
            ClearAllGlowsAndWiggles();

            if (tileSwapSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(tileSwapSfx);
            }

            SetResultPicture(null);

            if (step.swapPosition == 0)
            {
                activeSound1 = step.newLetter;
                if (step.newLetterSoundClip != null) activeSound1Clip = step.newLetterSoundClip;
                if (box1Text != null) box1Text.text = activeSound1;
            }
            else if (step.swapPosition == 1)
            {
                activeSound2 = step.newLetter;
                if (step.newLetterSoundClip != null) activeSound2Clip = step.newLetterSoundClip;
                if (box2Text != null) box2Text.text = activeSound2;
            }
            else if (step.swapPosition == 2)
            {
                activeSound3 = step.newLetter;
                if (step.newLetterSoundClip != null) activeSound3Clip = step.newLetterSoundClip;
                if (box3Text != null) box3Text.text = activeSound3;
            }

            if (step.reinforcementVoiceClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = step.reinforcementVoiceClip;
                voiceAudioSource.Play();
                SetSubtitles($"Now blend the new word!");
                yield return new WaitForSeconds(step.reinforcementVoiceClip.length + 0.2f);
            }
            else
            {
                SetSubtitles($"Changed to '{step.newLetter}'! Tap blend!");
                yield return new WaitForSeconds(0.5f);
            }

            isBlended = false;
            isTransitioning = false;
            UpdateSwapButtonUI(GetCurrentBuildData());

            IndicateStep(3); // Indicate Blend button next after letter swap!
        }

        private void IndicateStep(int stepIndex)
        {
            ClearAllGlowsAndWiggles();

            Transform targetTransform = null;
            GameObject glowObj = null;

            switch (stepIndex)
            {
                case 0:
                    targetTransform = box1Rect;
                    glowObj = box1GlowObject;
                    break;
                case 1:
                    targetTransform = box2Rect;
                    glowObj = box2GlowObject;
                    break;
                case 2:
                    targetTransform = box3Rect;
                    glowObj = box3GlowObject;
                    break;
                case 3:
                    if (blendButton != null) targetTransform = blendButton.transform;
                    glowObj = blendButtonGlowObject;
                    break;
                case 4:
                    if (swapLetterButton != null) targetTransform = swapLetterButton.transform;
                    glowObj = swapButtonGlowObject;
                    break;
            }

            if (glowObj != null) glowObj.SetActive(true);
            if (targetTransform != null)
            {
                activeIndicatorWiggleCoroutine = StartCoroutine(SingleWiggleCoroutine(targetTransform));
            }
        }

        private void ClearAllGlowsAndWiggles()
        {
            if (activeIndicatorWiggleCoroutine != null)
            {
                StopCoroutine(activeIndicatorWiggleCoroutine);
                activeIndicatorWiggleCoroutine = null;
            }

            if (box1GlowObject != null) box1GlowObject.SetActive(false);
            if (box2GlowObject != null) box2GlowObject.SetActive(false);
            if (box3GlowObject != null) box3GlowObject.SetActive(false);
            if (blendButtonGlowObject != null) blendButtonGlowObject.SetActive(false);
            if (swapButtonGlowObject != null) swapButtonGlowObject.SetActive(false);

            ResetTransformRotationsAndScales();
        }

        private void ResetTransformRotationsAndScales()
        {
            if (box1Rect != null) { box1Rect.localRotation = Quaternion.identity; box1Rect.localScale = Vector3.one; }
            if (box2Rect != null) { box2Rect.localRotation = Quaternion.identity; box2Rect.localScale = Vector3.one; }
            if (box3Rect != null) { box3Rect.localRotation = Quaternion.identity; box3Rect.localScale = Vector3.one; }
            if (blendButton != null) { blendButton.transform.localRotation = Quaternion.identity; blendButton.transform.localScale = Vector3.one; }
            if (swapLetterButton != null) { swapLetterButton.transform.localRotation = Quaternion.identity; swapLetterButton.transform.localScale = Vector3.one; }
        }

        private IEnumerator SingleWiggleCoroutine(Transform targetTransform, float duration = 0.45f)
        {
            if (targetTransform == null) yield break;

            Vector3 origScale = Vector3.one;
            Quaternion origRot = Quaternion.identity;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;

                float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.25f;
                targetTransform.localScale = origScale * scaleFactor;

                float rotZ = Mathf.Sin(percent * Mathf.PI * 2f) * 10f;
                targetTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

                yield return null;
            }

            targetTransform.localScale = origScale;
            targetTransform.localRotation = origRot;
            activeIndicatorWiggleCoroutine = null;
        }

        private IEnumerator WiggleThreeBoxesSequence(float duration)
        {
            float elapsed = 0f;
            Vector3 origScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;

                float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.25f;
                float rotZ = Mathf.Sin(percent * Mathf.PI * 2f) * 10f;

                if (box1Rect != null)
                {
                    box1Rect.localScale = origScale * scaleFactor;
                    box1Rect.localRotation = Quaternion.Euler(0f, 0f, rotZ);
                }
                if (box2Rect != null)
                {
                    box2Rect.localScale = origScale * scaleFactor;
                    box2Rect.localRotation = Quaternion.Euler(0f, 0f, -rotZ);
                }
                if (box3Rect != null)
                {
                    box3Rect.localScale = origScale * scaleFactor;
                    box3Rect.localRotation = Quaternion.Euler(0f, 0f, rotZ);
                }

                yield return null;
            }

            ResetTransformRotationsAndScales();
        }

        private ShortVowelBuildData GetCurrentBuildData()
        {
            if (buildRoundData != null && currentRoundIndex < buildRoundData.Length)
            {
                return buildRoundData[currentRoundIndex];
            }
            return null;
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

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Let's build words! Tap each sound, then blend!");

            if (introClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = introClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(introClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;
            ClearAllGlowsAndWiggles();

            if (correctChimeSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(correctChimeSfx);
            }

            SetSubtitles("Great job building short vowel words!");

            if (completionPraiseClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = completionPraiseClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(completionPraiseClip.length + 0.3f);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);

            TopicProgressUI.MarkTopicComplete(unitID, topicName);

            isTransitioning = false;
        }

        public void GoToNextPanel()
        {
            if (isActivityCompleted)
            {
                TopicProgressUI.MarkTopicComplete(gameObject);
            }

            ResetLevel();

            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
                if (unitContentPanel != null && nextPanel != unitContentPanel && !nextPanel.transform.IsChildOf(unitContentPanel.transform))
                {
                    unitContentPanel.SetActive(false);
                }
            }
            else if (unitContentPanel != null)
            {
                unitContentPanel.SetActive(true);
            }

            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }

        private void SetSubtitles(string text)
        {
            EngSnap.Common.DialogueBoxAutoHider.SetDialogue(dialogueText, text, null);
        }
    }
}
