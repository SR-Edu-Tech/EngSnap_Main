using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit10
{
    public class DogInTheWellController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit10";
        [SerializeField] private string topicName = "TheDogInTheWell";

        [Header("Data Asset")]
        [SerializeField] private DogInTheWellData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Dialogue / Subtitle UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1 & 2: Well Story Panel UI")]
        [SerializeField] private GameObject storyPanel;
        [SerializeField] private Image wellSceneImage;
        [SerializeField] private TMP_Text currentLineTMP;
        [SerializeField] private Button[] wordButtonsInLine = new Button[12];
        [SerializeField] private TMP_Text[] wordTextsInLine = new TMP_Text[12];
        [SerializeField] private Image[] wordHighlightsInLine = new Image[12];
        [SerializeField] private Button readLineHelpButton;
        [SerializeField] private Button nextLineTickButton;

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

        private int currentLineIndex = 0;
        private bool isTransitioning = false;
        private bool hasInitialized = false;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
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
                activityData = Resources.Load<DogInTheWellData>("Phonics2/Unit10/DogInTheWellData_Unit10");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<DogInTheWellData>();
                activityData.PopulateDefaultData();
            }
        }

        private void SetupButtonListeners()
        {
            if (readLineHelpButton != null)
            {
                readLineHelpButton.onClick.RemoveAllListeners();
                readLineHelpButton.onClick.AddListener(OnReadLineHelpClicked);
            }

            if (nextLineTickButton != null)
            {
                nextLineTickButton.onClick.RemoveAllListeners();
                nextLineTickButton.onClick.AddListener(OnNextLineTickClicked);
            }

            for (int i = 0; i < wordButtonsInLine.Length; i++)
            {
                int index = i;
                if (wordButtonsInLine[i] != null)
                {
                    wordButtonsInLine[i].onClick.RemoveAllListeners();
                    wordButtonsInLine[i].onClick.AddListener(() => OnWordInLineClicked(index));
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

        public void ResetLevel()
        {
            StartActivity();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            currentLineIndex = 0;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);

            if (storyPanel != null) storyPanel.SetActive(true);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);

            StartCoroutine(RunActivityIntroRoutine());
        }

        private IEnumerator RunActivityIntroRoutine()
        {
            isTransitioning = true;
            SetDialogue("This is the longest story yet. You know every word in it. Take your time — I will be quiet.");
            if (activityData != null && activityData.openingIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.openingIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            isTransitioning = false;
            LoadStoryLine(0);
        }

        private void LoadStoryLine(int index)
        {
            if (activityData == null || activityData.storyLines == null || index >= activityData.storyLines.Length)
            {
                StartCoroutine(RunFluencySecondPassRoutine());
                return;
            }

            currentLineIndex = index;
            var lineItem = activityData.storyLines[index];

            if (currentLineTMP != null) currentLineTMP.text = lineItem.lineText;

            // Update scene illustration beat
            if (wellSceneImage != null && activityData.storySceneBeatSprites != null && lineItem.animationBeatIndex < activityData.storySceneBeatSprites.Length)
            {
                var sprite = activityData.storySceneBeatSprites[lineItem.animationBeatIndex];
                if (sprite != null)
                {
                    wellSceneImage.sprite = sprite;
                    wellSceneImage.gameObject.SetActive(true);
                }
            }

            for (int i = 0; i < wordButtonsInLine.Length; i++)
            {
                if (wordButtonsInLine[i] != null)
                {
                    bool active = lineItem.wordTokens != null && i < lineItem.wordTokens.Length;
                    wordButtonsInLine[i].gameObject.SetActive(active);

                    if (active && wordTextsInLine != null && i < wordTextsInLine.Length && wordTextsInLine[i] != null)
                    {
                        wordTextsInLine[i].text = lineItem.wordTokens[i];
                    }
                    if (wordHighlightsInLine != null && i < wordHighlightsInLine.Length && wordHighlightsInLine[i] != null)
                    {
                        wordHighlightsInLine[i].gameObject.SetActive(false);
                    }
                }
            }

            SetDialogue(lineItem.lineText);
            UpdateProgressUI((float)index / (activityData.storyLines.Length * 2f));
        }

        private void OnWordInLineClicked(int wordIndex)
        {
            if (activityData == null || activityData.storyLines == null || currentLineIndex >= activityData.storyLines.Length) return;

            var lineItem = activityData.storyLines[currentLineIndex];
            if (lineItem.wordTokens != null && wordIndex >= 0 && wordIndex < lineItem.wordTokens.Length)
            {
                string word = lineItem.wordTokens[wordIndex];
                SetDialogue(word);

                if (wordHighlightsInLine != null && wordIndex < wordHighlightsInLine.Length && wordHighlightsInLine[wordIndex] != null)
                {
                    wordHighlightsInLine[wordIndex].gameObject.SetActive(true);
                }

                if (wordButtonsInLine != null && wordIndex < wordButtonsInLine.Length && wordButtonsInLine[wordIndex] != null)
                {
                    StartCoroutine(PopScaleRoutine(wordButtonsInLine[wordIndex].transform, 1.2f, 0.2f));
                }
            }
        }

        private void OnReadLineHelpClicked()
        {
            if (activityData == null || activityData.storyLines == null || currentLineIndex >= activityData.storyLines.Length) return;

            var lineItem = activityData.storyLines[currentLineIndex];
            SetDialogue(lineItem.lineText);

            if (lineItem.lineAudioClip != null)
            {
                PlayVoiceClipNonBlocking(lineItem.lineAudioClip);
            }
        }

        private void OnNextLineTickClicked()
        {
            if (isTransitioning) return;

            PlaySFX(activityData != null ? activityData.lineCompleteTickSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            currentLineIndex++;
            LoadStoryLine(currentLineIndex);
        }

        private IEnumerator RunFluencySecondPassRoutine()
        {
            isTransitioning = true;
            SetDialogue("You read that whole story line by line! Now let us read it smoothly together!");

            if (activityData != null && activityData.storyLines != null)
            {
                for (int l = 0; l < activityData.storyLines.Length; l++)
                {
                    LoadStoryLine(l);
                    var line = activityData.storyLines[l];
                    if (line.lineAudioClip != null) PlayVoiceClipNonBlocking(line.lineAudioClip);

                    if (line.wordTokens != null)
                    {
                        for (int w = 0; w < line.wordTokens.Length; w++)
                        {
                            if (wordHighlightsInLine != null && w < wordHighlightsInLine.Length && wordHighlightsInLine[w] != null)
                                wordHighlightsInLine[w].gameObject.SetActive(true);

                            yield return new WaitForSeconds(0.45f);

                            if (wordHighlightsInLine != null && w < wordHighlightsInLine.Length && wordHighlightsInLine[w] != null)
                                wordHighlightsInLine[w].gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        yield return new WaitForSeconds(1.2f);
                    }
                }
            }

            PlaySFX(activityData != null ? activityData.rescueCheerSfx : null);
            SetDialogue("The dog is out! You read the whole story!");
            if (activityData != null && activityData.endingCelebrationClip != null)
            {
                yield return PlayVoiceClip(activityData.endingCelebrationClip);
            }

            isTransitioning = false;
            CompleteStop3();
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

            SetDialogue("A whole story conquered! Next up: The Graduation Questions!");
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
