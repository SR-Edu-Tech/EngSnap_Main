using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Common.ShortVowels
{
    [System.Serializable]
    public class StoryLineItem
    {
        [Tooltip("Line GameObject in the scene (e.g. Line1_Panel, Line2_Panel).")]
        public GameObject lineObject;

        [Tooltip("Full line text / subtitle for this story line.")]
        [TextArea(1, 3)]
        public string lineText;

        [Tooltip("Full line audio clip to play automatically when line appears.")]
        public AudioClip lineAudioClip;


    }

    public enum SlideDirection
    {
        FromRight = 0,
        FromLeft = 1,
        FromBottom = 2,
        FromTop = 3
    }

    public class ShortVowelStoryController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [Tooltip("Target Unit ID e.g. 'Unit6', 'Unit7', 'Unit8', 'Unit9', 'Unit10'.")]
        [SerializeField] private string unitID = "Unit6";

        [Tooltip("Topic key name for progress tracking e.g. 'ReadAndPlay'.")]
        [SerializeField] private string topicName = "ReadAndPlay";

        [Header("Mascot & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Story Display Container & Visuals")]
        [SerializeField] private GameObject storyPanelContainer;

        [Tooltip("Optional central UI Image component for displaying the story line illustration picture.")]
        [SerializeField] private Image storyLineIllustrationImage;

        [Header("Story Lines & Audio Clips")]
        [Tooltip("Array of story lines (Line GameObject + Line Audio Clip). Each activates sequentially.")]
        public StoryLineItem[] storyLines;

        [Tooltip("If true, previous story line GameObjects remain visible on screen instead of disappearing.")]
        public bool keepPreviousLinesVisible = true;

        [Header("Procedural UI Slide Animation")]
        [Tooltip("If true, automatically slides line GameObjects smoothly on screen without requiring manual Animator controllers.")]
        public bool useSlideAnimation = true;
        public SlideDirection slideDirection = SlideDirection.FromRight;
        public float slideDistance = 600f;
        public float slideDuration = 0.4f;

        [Tooltip("Optional Animator trigger name to play if useSlideAnimation is false e.g. 'Play' or 'Show'.")]
        public string lineAnimTriggerName = "Play";

        [Header("Story Auto-Advance Settings")]
        [Tooltip("If true, automatically advances to the next story line after line audio completes.")]
        public bool autoAdvanceLines = true;
        public float autoAdvanceExtraDelay = 0.8f;

        [Header("Recap Star Round Data (Used ONLY for Recap Questions)")]
        [Tooltip("ScriptableObject containing recap questions for the Star Round.")]
        [SerializeField] private ShortVowelStoryData recapStoryData;

        [Header("Recap Navigation & Transition Button")]
        [Tooltip("Button that appears after all story lines complete. Kid taps this button to open the Recap Round.")]
        [SerializeField] private Button startRecapButton;

        [Header("Recap Star Round UI")]
        [SerializeField] private GameObject recapPanelContainer;
        [SerializeField] private TMP_Text recapCategoryHeaderText;
        [SerializeField] private TMP_Text recapQuestionText;
        [SerializeField] private Image recapPromptImage;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Image starMeterFillImage;
        [SerializeField] private TMP_Text starMeterCountText;
        [SerializeField] private GameObject starBadgeObject;

        [Header("Voice Script & SFX Audio Clips")]
        [SerializeField] private AudioClip introClip;               // "Read the story! Tap any line to hear."
        [SerializeField] private AudioClip recapIntroClip;          // "Quick recap round! Answer to earn your Star Badge!"
        [SerializeField] private AudioClip completionPraiseClip;  // "You are a Short Vowel Star! Next Unit is open!"
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip wrongWobbleSfx;
        [SerializeField] private AudioClip starJingleSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;
       

        private int currentLineIndex = 0;
        private int currentRecapIndex = 0;
        private int correctRecapAnswers = 0;
        private bool isInRecapMode = false;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;
        private Coroutine autoAdvanceCoroutine;
        private Coroutine slideInCoroutine;

        private Dictionary<RectTransform, Vector2> initialPositions = new Dictionary<RectTransform, Vector2>();

        private Sprite defaultStoryLineIllustrationSprite;
        private Sprite defaultRecapPromptSprite;

        public bool IsTransitioning => isTransitioning;
        public string UnitID => unitID;

        private void Awake()
        {
            EnsureAudioSources();
            CacheInitialPositions();
            CacheDefaultStorySprites();
        }

        private void CacheDefaultStorySprites()
        {
            if (defaultStoryLineIllustrationSprite == null && storyLineIllustrationImage != null && storyLineIllustrationImage.sprite != null)
            {
                defaultStoryLineIllustrationSprite = storyLineIllustrationImage.sprite;
            }
            if (defaultRecapPromptSprite == null && recapPromptImage != null && recapPromptImage.sprite != null)
            {
                defaultRecapPromptSprite = recapPromptImage.sprite;
            }
        }

        private void SetStoryIllustration(Sprite sprite)
        {
            if (storyLineIllustrationImage == null) return;

            CacheDefaultStorySprites();
            storyLineIllustrationImage.gameObject.SetActive(true);
            storyLineIllustrationImage.enabled = true;

            EngSnap.Common.DefaultImageRestorer restorer = storyLineIllustrationImage.GetComponent<EngSnap.Common.DefaultImageRestorer>();
            if (restorer != null)
            {
                restorer.SetImage(sprite);
            }
            else
            {
                if (sprite != null)
                {
                    storyLineIllustrationImage.sprite = sprite;
                }
                else if (defaultStoryLineIllustrationSprite != null)
                {
                    storyLineIllustrationImage.sprite = defaultStoryLineIllustrationSprite;
                }
            }
        }

        private void SetRecapPromptImage(Sprite sprite)
        {
            if (recapPromptImage == null) return;

            CacheDefaultStorySprites();
            recapPromptImage.gameObject.SetActive(true);
            recapPromptImage.enabled = true;

            EngSnap.Common.DefaultImageRestorer restorer = recapPromptImage.GetComponent<EngSnap.Common.DefaultImageRestorer>();
            if (restorer != null)
            {
                restorer.SetImage(sprite);
            }
            else
            {
                if (sprite != null)
                {
                    recapPromptImage.sprite = sprite;
                }
                else if (defaultRecapPromptSprite != null)
                {
                    recapPromptImage.sprite = defaultRecapPromptSprite;
                }
            }
        }

        private void CacheInitialPositions()
        {
            if (storyLines != null)
            {
                foreach (var item in storyLines)
                {
                    if (item != null && item.lineObject != null)
                    {
                        GetOrCacheInitialPosition(item.lineObject.GetComponent<RectTransform>());
                    }
                }
            }
        }

        private Vector2 GetOrCacheInitialPosition(RectTransform rect)
        {
            if (rect == null) return Vector2.zero;
            if (!initialPositions.ContainsKey(rect))
            {
                initialPositions[rect] = rect.anchoredPosition;
            }
            return initialPositions[rect];
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
            CacheInitialPositions();

            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }

            if (startRecapButton != null)
            {
                startRecapButton.onClick.RemoveAllListeners();
                startRecapButton.onClick.AddListener(OnStartRecapButtonClicked);
            }

            ResetLevel();
        }

        private void OnEnable()
        {
            CacheInitialPositions();
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
            currentLineIndex = 0;
            currentRecapIndex = 0;
            correctRecapAnswers = 0;
            isInRecapMode = false;
            isTransitioning = false;
            isActivityCompleted = false;

            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            if (slideInCoroutine != null)
            {
                StopCoroutine(slideInCoroutine);
                slideInCoroutine = null;
            }

            SetAllLinesInteractable(true);

            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (starBadgeObject != null) starBadgeObject.SetActive(false);
            if (startRecapButton != null) startRecapButton.gameObject.SetActive(false);

            if (storyPanelContainer != null) storyPanelContainer.SetActive(true);
            if (recapPanelContainer != null) recapPanelContainer.SetActive(false);

            SetStoryIllustration(null);

            DeactivateAllLineObjects();
            SetSubtitles("Read along! Tap any word button to hear sound.");
        }

        private void DeactivateAllLineObjects()
        {
            if (storyLines != null)
            {
                foreach (var item in storyLines)
                {
                    if (item != null && item.lineObject != null) item.lineObject.SetActive(false);
                }
            }
        }

        private int GetTotalLineCount()
        {
            return storyLines != null ? storyLines.Length : 0;
        }

        private GameObject GetLineGameObject(int index)
        {
            if (storyLines != null && index >= 0 && index < storyLines.Length && storyLines[index] != null)
            {
                return storyLines[index].lineObject;
            }
            return null;
        }

        private string GetLineText(int index)
        {
            if (storyLines != null && index >= 0 && index < storyLines.Length && storyLines[index] != null)
            {
                return storyLines[index].lineText;
            }
            return null;
        }

        private AudioClip GetLineAudioClip(int index)
        {
            if (storyLines != null && index >= 0 && index < storyLines.Length && storyLines[index] != null)
            {
                return storyLines[index].lineAudioClip;
            }
            return null;
        }

        private Sprite GetLineSprite(int index)
        {
            return null;
        }

        public void UpdateStoryLineIllustration(int index)
        {
            Sprite sprite = GetLineSprite(index);
            SetStoryIllustration(sprite);
        }

        private void LoadStoryLine(int lineIdx)
        {
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            int totalLines = GetTotalLineCount();
            if (lineIdx >= totalLines)
            {
                ShowRecapTransitionUI();
                return;
            }

            // Update dialogue / subtitles & illustration for this line
            string lText = GetLineText(lineIdx);
            if (!string.IsNullOrEmpty(lText))
            {
                SetSubtitles(lText);
            }

            UpdateStoryLineIllustration(lineIdx);

            GameObject targetLineObj = null;

            // Manage line GameObjects
            for (int i = 0; i < totalLines; i++)
            {
                GameObject lineObj = GetLineGameObject(i);
                if (lineObj == null) continue;

                if (keepPreviousLinesVisible)
                {
                    if (i <= lineIdx)
                    {
                        lineObj.SetActive(true);
                        if (i == lineIdx)
                        {
                            targetLineObj = lineObj;
                        }
                    }
                    else
                    {
                        lineObj.SetActive(false);
                    }
                }
                else
                {
                    if (i == lineIdx)
                    {
                        lineObj.SetActive(true);
                        targetLineObj = lineObj;
                    }
                    else
                    {
                        lineObj.SetActive(false);
                    }
                }
            }

            AudioClip clip = GetLineAudioClip(lineIdx);
            float voiceLen = (clip != null) ? clip.length : 0f;

            if (targetLineObj != null)
            {
                if (useSlideAnimation)
                {
                    PlaySlideInAnimation(targetLineObj, voiceLen);
                }
                else
                {
                    Animator anim = targetLineObj.GetComponent<Animator>();
                    if (anim != null && !string.IsNullOrEmpty(lineAnimTriggerName))
                    {
                        anim.SetTrigger(lineAnimTriggerName);
                    }
                    if (slideInCoroutine != null) StopCoroutine(slideInCoroutine);
                    slideInCoroutine = StartCoroutine(TemporaryDisableLinesRoutine(Mathf.Max(0.4f, voiceLen)));
                }
            }

            // Play line audio and schedule auto-advance
            float duration = 2.0f;
            if (clip != null)
            {
                PlayVoice(clip);
                duration = clip.length + autoAdvanceExtraDelay;
            }

            if (autoAdvanceLines)
            {
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceLineRoutine(duration));
            }
        }

        private void PlaySlideInAnimation(GameObject targetObj, float extraWaitTime = 0f)
        {
            if (targetObj == null) return;
            RectTransform rect = targetObj.GetComponent<RectTransform>();
            if (rect == null) return;

            CanvasGroup canvasGroup = targetObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = targetObj.AddComponent<CanvasGroup>();

            if (slideInCoroutine != null)
            {
                StopCoroutine(slideInCoroutine);
                slideInCoroutine = null;
            }

            slideInCoroutine = StartCoroutine(SlideInCoroutine(rect, canvasGroup, extraWaitTime));
        }

        private IEnumerator SlideInCoroutine(RectTransform rect, CanvasGroup canvasGroup, float extraWaitTime)
        {
            isTransitioning = true;
            SetAllLinesInteractable(false);

            Vector2 targetPos = GetOrCacheInitialPosition(rect);
            Vector2 startOffset = Vector2.zero;

            switch (slideDirection)
            {
                case SlideDirection.FromRight: startOffset = new Vector2(slideDistance, 0); break;
                case SlideDirection.FromLeft: startOffset = new Vector2(-slideDistance, 0); break;
                case SlideDirection.FromBottom: startOffset = new Vector2(0, -slideDistance); break;
                case SlideDirection.FromTop: startOffset = new Vector2(0, slideDistance); break;
            }

            Vector2 startPos = targetPos + startOffset;
            rect.anchoredPosition = startPos;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);

                yield return null;
            }

            rect.anchoredPosition = targetPos;
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            float remainingVoiceTime = extraWaitTime - slideDuration;
            if (remainingVoiceTime > 0f)
            {
                yield return new WaitForSeconds(remainingVoiceTime);
            }

            SetAllLinesInteractable(true);
            isTransitioning = false;
            slideInCoroutine = null;
        }

        private IEnumerator TemporaryDisableLinesRoutine(float duration)
        {
            isTransitioning = true;
            SetAllLinesInteractable(false);
            yield return new WaitForSeconds(duration);
            SetAllLinesInteractable(true);
            isTransitioning = false;
            slideInCoroutine = null;
        }

        private void SetAllLinesInteractable(bool interactable)
        {
            if (storyPanelContainer != null)
            {
                CanvasGroup mainCg = storyPanelContainer.GetComponent<CanvasGroup>();
                if (mainCg == null && !interactable)
                {
                    mainCg = storyPanelContainer.AddComponent<CanvasGroup>();
                }
                if (mainCg != null)
                {
                    mainCg.interactable = interactable;
                    mainCg.blocksRaycasts = interactable;
                }

                Button[] containerButtons = storyPanelContainer.GetComponentsInChildren<Button>(true);
                if (containerButtons != null)
                {
                    foreach (var btn in containerButtons)
                    {
                        if (btn != null) btn.interactable = interactable;
                    }
                }
            }

            if (storyLines != null)
            {
                foreach (var item in storyLines)
                {
                    if (item != null && item.lineObject != null)
                    {
                        CanvasGroup cg = item.lineObject.GetComponent<CanvasGroup>();
                        if (cg == null && !interactable)
                        {
                            cg = item.lineObject.AddComponent<CanvasGroup>();
                        }
                        if (cg != null)
                        {
                            cg.interactable = interactable;
                            cg.blocksRaycasts = interactable;
                        }

                        Button[] lineButtons = item.lineObject.GetComponentsInChildren<Button>(true);
                        if (lineButtons != null)
                        {
                            foreach (var btn in lineButtons)
                            {
                                if (btn != null) btn.interactable = interactable;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Public method to play audio when a kid taps any word button in the scene.
        /// Automatically plays audio and triggers a playful button wiggle animation!
        /// </summary>
        public void PlayAudio(AudioClip clip)
        {
            if (clip == null || isTransitioning) return;
            if (!isInRecapMode)
            {
                UpdateStoryLineIllustration(currentLineIndex);
            }
            PlayVoice(clip);

            // Wiggle whichever button was tapped in Unity EventSystem
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject != null)
            {
                PlayWiggleAnimation(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject);
            }

            if (autoAdvanceLines && autoAdvanceCoroutine != null && !isInRecapMode)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceLineRoutine(clip.length + 1.2f));
            }
        }

        public void PlayWordAudio(AudioClip clip)
        {
            PlayAudio(clip);
        }

        public void PlayWordAudio(AudioClip clip, GameObject buttonObj)
        {
            if (isTransitioning) return;
            if (buttonObj != null) PlayWiggleAnimation(buttonObj);
            PlayAudio(clip);
        }

        public void PlayWordSound(AudioClip clip)
        {
            PlayAudio(clip);
        }

        /// <summary>
        /// Public OnClick method to display a specific Sprite in the story line illustration image section.
        /// Drag this method directly into Unity UI Button OnClick event in the Inspector and select a Sprite!
        /// </summary>
        public void ShowImage(Sprite sprite)
        {
            SetStoryIllustration(sprite);
        }

        /// <summary>
        /// Public OnClick method to display current story line's illustration picture.
        /// Drag this method into Unity UI Button OnClick event (No parameters required).
        /// </summary>
        public void OnShowLineImageClicked()
        {
            UpdateStoryLineIllustration(currentLineIndex);
        }

        /// <summary>
        /// Alias OnClick method for showing the current story line picture.
        /// </summary>
        public void ShowLineImage()
        {
            OnShowLineImageClicked();
        }

        public void PlayWiggleAnimation(GameObject obj)
        {
            if (obj == null || !obj.activeInHierarchy) return;
            StartCoroutine(WiggleCoroutine(obj.transform));
        }

        private IEnumerator WiggleCoroutine(Transform tr)
        {
            Vector3 originalScale = tr.localScale;
            Quaternion originalRot = tr.localRotation;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Tactile scale pulse up to 1.15x
                float scaleFactor = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
                tr.localScale = originalScale * scaleFactor;

                // Playful wiggle angle (-8° to +8°)
                float rotAngle = Mathf.Sin(t * Mathf.PI * 3f) * 8f * (1f - t);
                tr.localRotation = originalRot * Quaternion.Euler(0, 0, rotAngle);

                yield return null;
            }

            tr.localScale = originalScale;
            tr.localRotation = originalRot;
        }

        public void PlayCorrectBounceAnimation(GameObject obj)
        {
            if (obj == null || !obj.activeInHierarchy) return;
            StartCoroutine(CorrectBounceCoroutine(obj.transform));
        }

        private IEnumerator CorrectBounceCoroutine(Transform tr)
        {
            Vector3 originalScale = tr.localScale;
            float duration = 0.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Happy bounce scale pulse up to 1.25x
                float scaleFactor = 1f + Mathf.Sin(t * Mathf.PI) * 0.25f;
                tr.localScale = originalScale * scaleFactor;

                yield return null;
            }

            tr.localScale = originalScale;
        }

        public void PlayWrongWobbleAnimation(GameObject obj)
        {
            if (obj == null || !obj.activeInHierarchy) return;
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                StartCoroutine(WrongWobbleCoroutine(rect));
            }
            else
            {
                StartCoroutine(WiggleCoroutine(obj.transform));
            }
        }

        private IEnumerator WrongWobbleCoroutine(RectTransform rect)
        {
            Vector2 originalPos = rect.anchoredPosition;
            float duration = 0.45f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Dampened rapid horizontal shake/wobble
                float offset = Mathf.Sin(t * Mathf.PI * 8f) * 18f * (1f - t);
                rect.anchoredPosition = originalPos + new Vector2(offset, 0f);

                yield return null;
            }

            rect.anchoredPosition = originalPos;
        }

        private IEnumerator AutoAdvanceLineRoutine(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            if (!isInRecapMode && !isTransitioning)
            {
                OnNextLineClicked();
            }
        }

        public void OnNextLineClicked()
        {
            if (isTransitioning || isInRecapMode) return;

            currentLineIndex++;
            if (currentLineIndex >= GetTotalLineCount())
            {
                ShowRecapTransitionUI();
            }
            else
            {
                LoadStoryLine(currentLineIndex);
            }
        }

        private void ShowRecapTransitionUI()
        {
            if (startRecapButton != null)
            {
                startRecapButton.gameObject.SetActive(true);
            }
            else
            {
                OnStartRecapButtonClicked();
            }
            SetSubtitles("Great reading! Tap Next to start the Recap Round!");
        }

        public void OnStartRecapButtonClicked()
        {
            if (startRecapButton != null) startRecapButton.gameObject.SetActive(false);
            if (storyPanelContainer != null) storyPanelContainer.SetActive(false);
            if (recapPanelContainer != null) recapPanelContainer.SetActive(true);

            StartRecapMode();
        }

        public void OnPrevLineClicked()
        {
            if (isTransitioning || isInRecapMode) return;
            currentLineIndex = Mathf.Max(0, currentLineIndex - 1);
            LoadStoryLine(currentLineIndex);
        }

        public void OnPlayLineAudioClicked()
        {
            if (isTransitioning || isInRecapMode) return;
            UpdateStoryLineIllustration(currentLineIndex);
            AudioClip clip = GetLineAudioClip(currentLineIndex);
            if (clip != null)
            {
                PlayVoice(clip);
            }
        }

        private void StartRecapMode()
        {
            isInRecapMode = true;
            currentRecapIndex = 0;
            correctRecapAnswers = 0;

            if (storyPanelContainer != null) storyPanelContainer.SetActive(false);
            if (recapPanelContainer != null) recapPanelContainer.SetActive(true);

            StartCoroutine(RecapIntroSequence());
        }

        private IEnumerator RecapIntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Quick recap round! Answer to earn your Star Badge!");

            if (recapIntroClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = recapIntroClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(recapIntroClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            isTransitioning = false;
            LoadRecapQuestion(currentRecapIndex);
        }

        private void LoadRecapQuestion(int qIdx)
        {
            if (recapStoryData == null || recapStoryData.recapQuestions == null || recapStoryData.recapQuestions.Length == 0)
            {
                StartCoroutine(CompletionSequence());
                return;
            }

            UpdateStarMeterUI();

            if (qIdx < recapStoryData.recapQuestions.Length)
            {
                RecapQuestionData q = recapStoryData.recapQuestions[qIdx];
                if (q != null)
                {
                    if (recapQuestionText != null) recapQuestionText.text = q.questionText;
                    SetRecapPromptImage(q.promptSprite);

                    switch (q.questionType)
                    {
                        case RecapQuestionType.TapPictureForWord:
                            if (recapCategoryHeaderText != null) recapCategoryHeaderText.text = "Tap the Picture!";
                            SetSubtitles("Tap the picture for the spoken word!");
                            break;
                        case RecapQuestionType.FillMissingVowel:
                            if (recapCategoryHeaderText != null) recapCategoryHeaderText.text = "Fill the Missing Sound!";
                            SetSubtitles("Fill in the missing vowel sound!");
                            break;
                        case RecapQuestionType.PickRhymingWord:
                            if (recapCategoryHeaderText != null) recapCategoryHeaderText.text = "Rhyme Time!";
                            SetSubtitles("Pick the word that rhymes!");
                            break;
                    }

                    if (q.questionAudioClip != null) PlayVoice(q.questionAudioClip);

                    SetupChoiceButtons(q);
                }
            }
            else
            {
                StartCoroutine(CompletionSequence());
            }
        }

        private void SetupChoiceButtons(RecapQuestionData q)
        {
            if (choiceButtons == null) return;

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] == null) continue;

                bool hasWord = (q.choiceWords != null && i < q.choiceWords.Length && !string.IsNullOrEmpty(q.choiceWords[i]));
                bool hasSprite = (q.choiceSprites != null && i < q.choiceSprites.Length && q.choiceSprites[i] != null);

                if (hasWord || hasSprite)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    int btnIdx = i;
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(btnIdx));

                    TMP_Text txt = choiceButtons[i].GetComponentInChildren<TMP_Text>(true);
                    if (txt != null)
                    {
                        txt.text = hasWord ? q.choiceWords[i] : "";
                        txt.gameObject.SetActive(hasWord);
                    }

                    Image img = choiceButtons[i].GetComponentInChildren<Image>(true);
                    if (img != null && img.gameObject != choiceButtons[i].gameObject)
                    {
                        img.gameObject.SetActive(true);
                        img.enabled = true;
                        if (hasSprite)
                        {
                            img.sprite = q.choiceSprites[i];
                        }
                        else
                        {
                            EngSnap.Common.DefaultImageRestorer restorer = img.GetComponent<EngSnap.Common.DefaultImageRestorer>();
                            if (restorer != null)
                            {
                                restorer.SetImage(null);
                            }
                        }
                    }
                }
                else
                {
                    choiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnChoiceSelected(int choiceIdx)
        {
            if (isTransitioning || !isInRecapMode) return;
            if (recapStoryData == null || recapStoryData.recapQuestions == null || currentRecapIndex >= recapStoryData.recapQuestions.Length) return;

            RecapQuestionData q = recapStoryData.recapQuestions[currentRecapIndex];
            bool isCorrect = (q != null && choiceIdx == q.correctChoiceIndex);

            GameObject tappedButton = (choiceButtons != null && choiceIdx >= 0 && choiceIdx < choiceButtons.Length && choiceButtons[choiceIdx] != null)
                ? choiceButtons[choiceIdx].gameObject
                : null;

            if (isCorrect)
            {
                StartCoroutine(CorrectRecapSequence(tappedButton));
            }
            else
            {
                StartCoroutine(WrongRecapSequence(tappedButton));
            }
        }

        private IEnumerator CorrectRecapSequence(GameObject choiceButtonObj)
        {
            isTransitioning = true;

            if (choiceButtonObj != null)
            {
                PlayCorrectBounceAnimation(choiceButtonObj);
            }

            if (correctChimeSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(correctChimeSfx);
            }

            correctRecapAnswers++;
            UpdateStarMeterUI();
            SetSubtitles("Correct! Great reading!");

            yield return new WaitForSeconds(0.85f);

            currentRecapIndex++;
            isTransitioning = false;
            LoadRecapQuestion(currentRecapIndex);
        }

        private IEnumerator WrongRecapSequence(GameObject choiceButtonObj)
        {
            isTransitioning = true;

            if (choiceButtonObj != null)
            {
                PlayWrongWobbleAnimation(choiceButtonObj);
            }

            if (wrongWobbleSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(wrongWobbleSfx);
            }

            SetSubtitles("Try again! Listen carefully.");
            yield return new WaitForSeconds(0.7f);
            isTransitioning = false;
        }

        private void UpdateStarMeterUI()
        {
            int total = (recapStoryData != null && recapStoryData.recapQuestions != null) ? recapStoryData.recapQuestions.Length : 1;

            if (starMeterCountText != null)
            {
                starMeterCountText.text = $"{correctRecapAnswers} / {total}";
            }

            if (starMeterFillImage != null && total > 0)
            {
                starMeterFillImage.fillAmount = (float)correctRecapAnswers / total;
            }
        }

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Read along! Tap any word button to hear sound.");

            if (introClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = introClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(introClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }

            isTransitioning = false;
            LoadStoryLine(0);
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            if (starBadgeObject != null) starBadgeObject.SetActive(true);
            if (starJingleSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(starJingleSfx);
            }

            string vowel = (recapStoryData != null && !string.IsNullOrEmpty(recapStoryData.targetVowel)) ? recapStoryData.targetVowel.ToUpper() : "Vowel";
            SetSubtitles($"You are a Short {vowel} Star! Next Unit is open!");

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

        private void PlayVoice(AudioClip clip)
        {
            if (clip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
            }
        }

        private void SetSubtitles(string text)
        {
            EngSnap.Common.DialogueBoxAutoHider.SetDialogue(dialogueText, text, null);
        }
    }
}
