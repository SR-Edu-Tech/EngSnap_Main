using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U8_SA_GM03s_ErRule_Masters_Phonics : MonoBehaviour
    {
        [Header("Item Pool (16 items: 8 End/Quiet, 8 Middle/Stressed)")]
        public List<ErRuleItem> items;
        private int currentIndex = 0;
        private int score = 0;
        private int currentStreak = 0;
        private int firstAttemptCorrectCount = 0;
        private bool isProcessing = false;
        private bool isFirstAttemptOnItem = true;

        [Header("UI Bins / Pods")]
        [SerializeField] private RectTransform endQuietPod; // Left (er)
        [SerializeField] private RectTransform midStressedPod; // Right (ir/ur)
        [SerializeField] private Button endQuietBtn;
        [SerializeField] private Button midStressedBtn;

        [Header("Rule Banner")]
        [SerializeField] private TextMeshProUGUI ruleBannerText;

        [Header("Swipe Card")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private TextMeshProUGUI cardWordText;
        [SerializeField] private TextMeshProUGUI cardSubText;
        [SerializeField] private CanvasGroup cardCanvasGroup;
        private Vector2 cardInitialAnchoredPos;
        private Vector3 cardInitialLocalPos;
        private Vector3 dragOffset;
        private Coroutine snapBackCoroutine;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioBtn;

        [Header("SFX")]
        public AudioClip correctSFX;
        public AudioClip wrongSFX;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            if (items == null || items.Count == 0)
                items = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity4Items();

            Transform root = transform;

            if (endQuietPod == null)
            {
                Transform t = root.Find("Pods/EndQuietPod") ?? root.Find("LeftPod") ?? root.Find("EndPod");
                if (t != null) endQuietPod = t.GetComponent<RectTransform>();
            }
            if (midStressedPod == null)
            {
                Transform t = root.Find("Pods/MidStressedPod") ?? root.Find("RightPod") ?? root.Find("MiddlePod");
                if (t != null) midStressedPod = t.GetComponent<RectTransform>();
            }

            if (endQuietPod != null && endQuietBtn == null) endQuietBtn = endQuietPod.GetComponentInChildren<Button>(true);
            if (midStressedPod != null && midStressedBtn == null) midStressedBtn = midStressedPod.GetComponentInChildren<Button>(true);

            if (ruleBannerText == null)
            {
                Transform rb = root.Find("RuleBanner/Text") ?? root.Find("RuleBannerText") ?? root.Find("RuleBanner");
                if (rb != null) ruleBannerText = rb.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (cardContainer == null)
            {
                Transform c = root.Find("CardContainer") ?? root.Find("SwipeCard") ?? root.Find("Card");
                if (c != null) cardContainer = c.GetComponent<RectTransform>();
            }

            if (cardContainer != null)
            {
                if (cardCanvasGroup == null)
                {
                    cardCanvasGroup = cardContainer.GetComponent<CanvasGroup>();
                    if (cardCanvasGroup == null) cardCanvasGroup = cardContainer.gameObject.AddComponent<CanvasGroup>();
                }
                if (cardWordText == null)
                {
                    Transform wt = cardContainer.Find("WordText") ?? cardContainer.Find("Text");
                    if (wt != null) cardWordText = wt.GetComponent<TextMeshProUGUI>();
                    else cardWordText = cardContainer.GetComponentInChildren<TextMeshProUGUI>();
                }
                if (cardSubText == null)
                {
                    Transform st = cardContainer.Find("SubText") ?? cardContainer.Find("PhoneticText");
                    if (st != null) cardSubText = st.GetComponent<TextMeshProUGUI>();
                }

                if (Application.isPlaying)
                {
                    var proxy = cardContainer.GetComponent<U8_ErRule_CardDragProxy>();
                    if (proxy == null) proxy = cardContainer.gameObject.AddComponent<U8_ErRule_CardDragProxy>();
                    proxy.owner = this;
                }
            }

            if (replayAudioBtn == null)
            {
                Transform r = root.Find("ReplayAudioBtn") 
                           ?? root.Find("ReplayButton") 
                           ?? root.Find("ReplayAudioButton")
                           ?? root.Find("HeaderRibbon/ReplayAudioBtn")
                           ?? root.Find("Speaker_Button");
                if (r != null) replayAudioBtn = r.GetComponent<Button>();
            }

            if (progressText == null)
            {
                Transform pt = root.Find("ProgressHUD/ProgressText") 
                            ?? root.Find("ProgressHUD/Progress_Text") 
                            ?? root.Find("ProgressText")
                            ?? root.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }
            if (scoreText == null)
            {
                Transform st = root.Find("ScoreHUD/ScoreText") 
                            ?? root.Find("ScoreHUD/Score_Text") 
                            ?? root.Find("ScoreText")
                            ?? root.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }
            if (streakText == null)
            {
                Transform stt = root.Find("ScoreHUD/StreakText") ?? root.Find("HUD/StreakText") ?? root.Find("StreakText");
                if (stt != null) streakText = stt.GetComponent<TextMeshProUGUI>();
            }
            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            AttachListeners();
        }

        private void AttachListeners()
        {
            if (endQuietBtn != null)
            {
                endQuietBtn.onClick.RemoveAllListeners();
                endQuietBtn.onClick.AddListener(() => OnChoiceSelected(ErRuleCategory.End_Quiet));
            }
            if (midStressedBtn != null)
            {
                midStressedBtn.onClick.RemoveAllListeners();
                midStressedBtn.onClick.AddListener(() => OnChoiceSelected(ErRuleCategory.Middle_Stressed));
            }
            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentWordAudio);
            }
        }

        public void StartActivity()
        {
            currentIndex = 0;
            score = 0;
            currentStreak = 0;
            firstAttemptCorrectCount = 0;
            isProcessing = false;

            if (ruleBannerText != null)
                ruleBannerText.text = "<b>Rule:</b> The quiet <color=#00E5FF>/ər/</color> at the <b>END</b> of a word = <color=#FFD700>\"er\"</color>";

            if (cardContainer != null)
            {
                cardInitialAnchoredPos = cardContainer.anchoredPosition;
                cardInitialLocalPos = cardContainer.localPosition;
            }

            AudioClip introClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_a4_intro");
            if (introClip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(introClip);

            LoadCurrentCard();
        }

        private void LoadCurrentCard()
        {
            if (currentIndex >= items.Count)
            {
                StartCoroutine(HandleActivityCompleted());
                return;
            }

            var item = items[currentIndex];
            isFirstAttemptOnItem = true;
            isProcessing = false;

            if (cardContainer != null)
            {
                if (snapBackCoroutine != null)
                {
                    StopCoroutine(snapBackCoroutine);
                    snapBackCoroutine = null;
                }
                cardContainer.anchoredPosition = cardInitialAnchoredPos;
                cardContainer.localPosition = cardInitialLocalPos;
                cardContainer.localRotation = Quaternion.identity;
                cardContainer.localScale = Vector3.one;
                if (cardCanvasGroup != null)
                {
                    cardCanvasGroup.alpha = 1f;
                    cardCanvasGroup.blocksRaycasts = true;
                }
            }

            if (cardWordText != null)
            {
                cardWordText.text = $"<b>{item.word}</b>";
            }
            if (cardSubText != null)
            {
                cardSubText.text = "Is the r-sound at the END and quiet?";
            }

            UpdateHUD();
            ReplayCurrentWordAudio();
        }

        public void ReplayCurrentWordAudio()
        {
            if (currentIndex < items.Count)
            {
                var item = items[currentIndex];
                PlayWordAudio(item.word, item.wordAudio);
            }
        }

        private void PlayWordAudio(string word, AudioClip overrideClip)
        {
            if (overrideClip != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(overrideClip);
                return;
            }
            AudioClip clip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio($"U08_WRD_{word}") ??
                             U8_SA_AudioManager_Masters_Phonics.ResolveAudio(word);
            if (clip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(clip);
        }

        public void OnChoiceSelected(ErRuleCategory chosenCategory)
        {
            if (isProcessing || currentIndex >= items.Count) return;
            isProcessing = true;

            var item = items[currentIndex];
            bool isCorrect = (chosenCategory == item.ruleCategory);
            bool isLeft = (chosenCategory == ErRuleCategory.End_Quiet);

            if (isCorrect)
            {
                if (isFirstAttemptOnItem) firstAttemptCorrectCount++;
                currentStreak++;
                int streakMultiplier = Mathf.Min(currentStreak, 4);
                score += (15 * streakMultiplier);
                UpdateHUD();

                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordsRead(1);

                StartCoroutine(HandleSwipeSuccess(isLeft, item));
            }
            else
            {
                isFirstAttemptOnItem = false;
                currentStreak = 0;
                UpdateHUD();
                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordMistake(item.word);

                StartCoroutine(HandleWrongFeedback());
            }
        }

        private IEnumerator HandleSwipeSuccess(bool isLeft, ErRuleItem item)
        {
            PlaySFX(correctSFX, true);

            if (cardContainer != null)
            {
                Vector3 startPos = cardContainer.position;
                Vector3 offscreen = startPos + (isLeft ? Vector3.left * 1200f : Vector3.right * 1200f);
                float elapsed = 0f;
                float dur = 0.35f;

                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / dur;
                    cardContainer.position = Vector3.Lerp(startPos, offscreen, t);
                    cardContainer.localRotation = Quaternion.Euler(0, 0, isLeft ? t * 25f : -t * 25f);
                    if (cardCanvasGroup != null) cardCanvasGroup.alpha = 1f - t;
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.2f);
            currentIndex++;
            LoadCurrentCard();
        }

        private IEnumerator HandleWrongFeedback()
        {
            PlaySFX(wrongSFX, false);

            if (cardContainer != null)
            {
                float elapsed = 0f;
                while (elapsed < 0.4f)
                {
                    elapsed += Time.deltaTime;
                    float xOffset = Mathf.Sin(elapsed * 45f) * 18f;
                    cardContainer.anchoredPosition = new Vector2(cardInitialAnchoredPos.x + xOffset, cardInitialAnchoredPos.y);
                    yield return null;
                }
                cardContainer.anchoredPosition = cardInitialAnchoredPos;
                cardContainer.localPosition = cardInitialLocalPos;
                cardContainer.localRotation = Quaternion.identity;
            }

            yield return new WaitForSeconds(0.3f);
            isProcessing = false;
        }

        // =====================================================================
        // Drag / Flick Gesture Detection
        // =====================================================================

        private Vector2 dragStartPos;

        public void OnCardBeginDrag(PointerEventData eventData)
        {
            if (isProcessing || cardContainer == null) return;
            if (snapBackCoroutine != null)
            {
                StopCoroutine(snapBackCoroutine);
                snapBackCoroutine = null;
            }

            if (cardCanvasGroup != null) cardCanvasGroup.blocksRaycasts = false;
            dragStartPos = eventData.position;

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(cardContainer, eventData.position, cam, out Vector3 worldPoint))
            {
                dragOffset = cardContainer.position - worldPoint;
            }
            else
            {
                dragOffset = Vector3.zero;
            }
        }

        public void OnBeginDrag(PointerEventData eventData) => OnCardBeginDrag(eventData);

        public void OnCardDrag(PointerEventData eventData)
        {
            if (isProcessing || cardContainer == null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(cardContainer, eventData.position, cam, out Vector3 worldPoint))
            {
                cardContainer.position = worldPoint + dragOffset;
            }

            float deltaX = eventData.position.x - dragStartPos.x;
            float rotation = Mathf.Clamp(-deltaX * 0.05f, -20f, 20f);
            cardContainer.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        public void OnDrag(PointerEventData eventData) => OnCardDrag(eventData);

        public void OnCardDragEnd(PointerEventData eventData)
        {
            if (cardCanvasGroup != null) cardCanvasGroup.blocksRaycasts = true;
            if (isProcessing || cardContainer == null) return;

            float deltaX = eventData.position.x - dragStartPos.x;
            float threshold = Screen.width * 0.15f;

            if (deltaX < -threshold)
            {
                OnChoiceSelected(ErRuleCategory.End_Quiet); // Swipe Left
            }
            else if (deltaX > threshold)
            {
                OnChoiceSelected(ErRuleCategory.Middle_Stressed); // Swipe Right
            }
            else
            {
                StartSnapBack();
            }
        }

        public void OnEndDrag(PointerEventData eventData) => OnCardDragEnd(eventData);

        public void StartSnapBack()
        {
            if (snapBackCoroutine != null) StopCoroutine(snapBackCoroutine);
            snapBackCoroutine = StartCoroutine(SnapBackRoutine());
        }

        private IEnumerator SnapBackRoutine()
        {
            if (cardContainer == null) yield break;

            Vector3 startPos = cardContainer.position;
            Quaternion startRot = cardContainer.localRotation;
            Vector3 targetPos = (cardContainer.parent != null) 
                ? cardContainer.parent.TransformPoint(cardInitialLocalPos) 
                : cardContainer.position;

            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cardContainer.position = Vector3.Lerp(startPos, targetPos, t);
                cardContainer.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, t);
                yield return null;
            }

            cardContainer.localPosition = cardInitialLocalPos;
            cardContainer.anchoredPosition = cardInitialAnchoredPos;
            cardContainer.localRotation = Quaternion.identity;
            snapBackCoroutine = null;
        }

        private void PlaySFX(AudioClip clip, bool isCorrect)
        {
            if (clip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX(clip);
            else
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayAnswerFeedbackSFX(isCorrect);
        }

        private void UpdateHUD()
        {
            if (progressBar != null)
            {
                progressBar.maxValue = items.Count;
                progressBar.value = currentIndex + 1;
                U8_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
            }

            if (progressText != null)
                progressText.text = $"<b>Item {currentIndex + 1} of {items.Count}</b>";

            if (scoreText != null)
                scoreText.text = $"Score: <b>{score}</b>";

            if (streakText != null)
                streakText.text = currentStreak > 1 ? $"<b>Streak: {currentStreak}x</b>" : "";
        }

        private IEnumerator HandleActivityCompleted()
        {
            isProcessing = true;
            if (progressBar != null)
            {
                progressBar.value = items.Count;
            }

            int earnedStars = 1;
            if (firstAttemptCorrectCount >= 15) earnedStars = 3;
            else if (firstAttemptCorrectCount >= 13) earnedStars = 2;

            if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(4, earnedStars);
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(score);
            }

            // Outro Voiceline explaining exceptions (her, term, germ, clerk)
            AudioClip outroClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_a4_outro");
            if (outroClip != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(outroClip);
                yield return new WaitForSeconds(outroClip.length + 0.5f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            U8_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform,
                "Activity 4 — The -er Rule",
                earnedStars,
                score,
                () => {
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity5();
                }
            );
        }
    }
}
