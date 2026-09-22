using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U9_SA_GM03s_OneOrTwo_Masters_Phonics : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Card & Drag Transform")]
        [SerializeField] private RectTransform cardRectTransform;
        [SerializeField] private TextMeshProUGUI cardWordText;
        [SerializeField] private Image cardBackgroundImage;

        [Header("Drop Bins / Chutes (Left: Long, Right: Short)")]
        [SerializeField] private RectTransform longBinRect;
        [SerializeField] private RectTransform shortBinRect;
        [SerializeField] private Button longBinBtn;   // Tap fallback
        [SerializeField] private Button shortBinBtn;  // Tap fallback

        [Header("Rule Feedback Animations")]
        [SerializeField] private GameObject openGateGraphicGO;
        [SerializeField] private GameObject closedDoorGraphicGO;
        [SerializeField] private TextMeshProUGUI splitExplanationText;

        [Header("Instruction / Prompt")]
        [SerializeField] private TextMeshProUGUI promptInstructionText;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        private Canvas parentCanvas;
        private RectTransform canvasRect;
        private Vector2 cardOriginPos;
        private List<OneOrTwoItem> items = new List<OneOrTwoItem>();
        private int currentItemIndex = 0;
        private int firstAttemptSuccesses = 0;
        private bool hasFailedCurrentItem = false;
        private bool isProcessing = false;
        private Coroutine activeMotionRoutine = null;

        private Vector2 dragStartPointerLocalPos;
        private Vector2 dragStartCardAnchoredPos;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            UpdateHUD();
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;
            U9_UI_Utils.FormatHeaderTypography(root, "ACTIVITY 2: ONE OR TWO?", "Open vs Closed Syllables");
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null)
                canvasRect = parentCanvas.GetComponent<RectTransform>();

            if (promptInstructionText == null)
            {
                Transform pt = root.Find("InstructionText") ?? root.Find("PromptText") ?? root.Find("Prompt") ?? root.Find("Header/PromptText");
                if (pt != null) promptInstructionText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (promptInstructionText != null)
            {
                promptInstructionText.text = "<b>Drag the word to the Long (Open) or Short (Closed) bin!</b>";
                promptInstructionText.fontSize = 24;
                promptInstructionText.alignment = TextAlignmentOptions.Center;
                promptInstructionText.color = new Color(0.85f, 0.92f, 1f);
            }

            if (cardRectTransform == null)
            {
                Transform c = root.Find("CenterCard") ?? root.Find("WordCard") ?? root.Find("Card");
                if (c != null) cardRectTransform = c.GetComponent<RectTransform>();
            }

            if (cardRectTransform != null)
            {
                cardOriginPos = cardRectTransform.anchoredPosition;
                if (cardWordText == null) cardWordText = cardRectTransform.GetComponentInChildren<TextMeshProUGUI>(true);
                if (cardBackgroundImage == null) cardBackgroundImage = cardRectTransform.GetComponent<Image>();
                if (cardBackgroundImage != null) U9_UI_Utils.ApplyRoundedCardStyle(cardBackgroundImage, new Color(0.96f, 0.98f, 1f));

                // Attach direct drag helper to the card itself for responsive touch capture
                var dragHelper = cardRectTransform.GetComponent<U9_OneOrTwo_CardDragHelper>() 
                              ?? cardRectTransform.gameObject.AddComponent<U9_OneOrTwo_CardDragHelper>();
                dragHelper.Init(this);
            }

            if (longBinRect == null)
            {
                Transform lb = root.Find("LongBin") ?? root.Find("OpenBin") ?? root.Find("Bin_Long");
                if (lb != null) longBinRect = lb.GetComponent<RectTransform>();
            }

            if (shortBinRect == null)
            {
                Transform sb = root.Find("ShortBin") ?? root.Find("ClosedBin") ?? root.Find("Bin_Short");
                if (sb != null) shortBinRect = sb.GetComponent<RectTransform>();
            }

            if (longBinRect != null)
            {
                var img = longBinRect.GetComponent<Image>();
                if (img != null) U9_UI_Utils.ApplyRoundedCardStyle(img, new Color(0.1f, 0.55f, 0.9f));
            }

            if (shortBinRect != null)
            {
                var img = shortBinRect.GetComponent<Image>();
                if (img != null) U9_UI_Utils.ApplyRoundedCardStyle(img, new Color(0.85f, 0.3f, 0.25f));
            }

            if (longBinBtn == null && longBinRect != null) longBinBtn = longBinRect.GetComponent<Button>();
            if (shortBinBtn == null && shortBinRect != null) shortBinBtn = shortBinRect.GetComponent<Button>();

            if (openGateGraphicGO == null)
            {
                Transform g = root.Find("GateGraphic") ?? root.Find("OpenGate") ?? root.Find("Gate");
                if (g != null) openGateGraphicGO = g.gameObject;
            }

            if (closedDoorGraphicGO == null)
            {
                Transform d = root.Find("DoorGraphic") ?? root.Find("ClosedDoor") ?? root.Find("Door");
                if (d != null) closedDoorGraphicGO = d.gameObject;
            }

            if (splitExplanationText == null)
            {
                Transform t = root.Find("ExplanationText") ?? root.Find("SplitText") ?? root.Find("FeedbackText");
                if (t != null) splitExplanationText = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressText == null)
            {
                Transform pt = root.Find("ProgressHUD/Progress_Text")
                            ?? root.Find("ProgressHUD/ProgressText")
                            ?? root.Find("ProgressText")
                            ?? root.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            if (scoreText == null)
            {
                Transform st = root.Find("ProgressHUD/Score_Text")
                            ?? root.Find("ProgressHUD/ScoreText")
                            ?? root.Find("ScoreText")
                            ?? root.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }

            AttachButtonListeners();
            UpdateHUD();
        }

        private void AttachButtonListeners()
        {
            if (longBinBtn != null)
            {
                longBinBtn.onClick.RemoveAllListeners();
                longBinBtn.onClick.AddListener(() => OnBinSelected(VowelLength.LongOpen));
            }

            if (shortBinBtn != null)
            {
                shortBinBtn.onClick.RemoveAllListeners();
                shortBinBtn.onClick.AddListener(() => OnBinSelected(VowelLength.ShortClosed));
            }
        }

        public void StartActivity()
        {
            items = U9_DataBank.GetActivity2Pool();
            // Shuffle
            for (int i = 0; i < items.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, items.Count);
                var temp = items[i];
                items[i] = items[r];
                items[r] = temp;
            }

            if (items.Count > 16) items = items.GetRange(0, 16);

            currentItemIndex = 0;
            firstAttemptSuccesses = 0;
            isProcessing = false;

            UpdateHUD();

            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_a2_intro", () =>
                {
                    if (this != null && gameObject.activeInHierarchy)
                    {
                        LoadCurrentItem();
                    }
                });
            }
            else
            {
                LoadCurrentItem();
            }
        }

        private void LoadCurrentItem()
        {
            if (!gameObject.activeInHierarchy) return;

            if (currentItemIndex >= items.Count)
            {
                CompleteActivity();
                return;
            }

            hasFailedCurrentItem = false;
            isProcessing = false;

            if (activeMotionRoutine != null) StopCoroutine(activeMotionRoutine);

            if (cardRectTransform != null)
            {
                cardRectTransform.anchoredPosition = cardOriginPos;
                cardRectTransform.localRotation = Quaternion.identity;
                cardRectTransform.localScale = Vector3.one;
            }

            ResetBinVisuals();

            if (openGateGraphicGO != null) openGateGraphicGO.SetActive(false);
            if (closedDoorGraphicGO != null) closedDoorGraphicGO.SetActive(false);
            if (splitExplanationText != null) splitExplanationText.text = "";

            UpdateHUD();

            OneOrTwoItem item = items[currentItemIndex];
            if (cardWordText != null)
            {
                cardWordText.text = $"<b>{item.fullWord}</b>";
                cardWordText.color = new Color(0.06f, 0.09f, 0.16f); // Deep navy black
            }

            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWord(item.fullWord);
        }

        // =====================================================================
        // Ultra-Smooth Drag & Drop Handling
        // =====================================================================

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isProcessing || cardRectTransform == null) return;
            if (activeMotionRoutine != null) StopCoroutine(activeMotionRoutine);

            Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? parentCanvas.worldCamera : null;
            RectTransform container = canvasRect != null ? canvasRect : transform as RectTransform;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(container, eventData.position, cam, out Vector2 localPointerPos))
            {
                dragStartPointerLocalPos = localPointerPos;
                dragStartCardAnchoredPos = cardRectTransform.anchoredPosition;
            }

            // Lift card: scale up slightly, straight rotation
            cardRectTransform.localScale = Vector3.one * 1.06f;
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayClick();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isProcessing || cardRectTransform == null) return;

            Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? parentCanvas.worldCamera : null;
            RectTransform container = canvasRect != null ? canvasRect : transform as RectTransform;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(container, eventData.position, cam, out Vector2 currentLocalPointerPos))
            {
                Vector2 delta = currentLocalPointerPos - dragStartPointerLocalPos;
                Vector2 newCardPos = dragStartCardAnchoredPos + delta;
                cardRectTransform.anchoredPosition = newCardPos;

                // Subtle smooth tilt while dragging
                float tiltAngle = Mathf.Clamp((newCardPos.x - cardOriginPos.x) * -0.04f, -12f, 12f);
                cardRectTransform.localRotation = Quaternion.Euler(0, 0, tiltAngle);

                // Live hover highlighting on Bins
                UpdateBinHoverHighlights(newCardPos.x);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isProcessing || cardRectTransform == null) return;

            Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? parentCanvas.worldCamera : null;

            bool inLong = longBinRect != null && RectTransformUtility.RectangleContainsScreenPoint(longBinRect, eventData.position, cam);
            bool inShort = shortBinRect != null && RectTransformUtility.RectangleContainsScreenPoint(shortBinRect, eventData.position, cam);

            float deltaX = cardRectTransform.anchoredPosition.x - cardOriginPos.x;
            if (!inLong && !inShort)
            {
                if (deltaX < -140f) inLong = true;
                else if (deltaX > 140f) inShort = true;
            }

            ResetBinVisuals();

            if (inLong)
            {
                if (longBinRect != null)
                    activeMotionRoutine = StartCoroutine(CoGlideToTarget(longBinRect.anchoredPosition, () => OnBinSelected(VowelLength.LongOpen)));
                else
                    OnBinSelected(VowelLength.LongOpen);
            }
            else if (inShort)
            {
                if (shortBinRect != null)
                    activeMotionRoutine = StartCoroutine(CoGlideToTarget(shortBinRect.anchoredPosition, () => OnBinSelected(VowelLength.ShortClosed)));
                else
                    OnBinSelected(VowelLength.ShortClosed);
            }
            else
            {
                // Smooth return spring to center
                activeMotionRoutine = StartCoroutine(CoSmoothReturn());
            }
        }

        private void UpdateBinHoverHighlights(float currentCardX)
        {
            if (longBinRect != null)
            {
                bool hoverLong = currentCardX < -120f;
                longBinRect.localScale = Vector3.Lerp(longBinRect.localScale, hoverLong ? Vector3.one * 1.08f : Vector3.one, Time.deltaTime * 15f);
            }

            if (shortBinRect != null)
            {
                bool hoverShort = currentCardX > 120f;
                shortBinRect.localScale = Vector3.Lerp(shortBinRect.localScale, hoverShort ? Vector3.one * 1.08f : Vector3.one, Time.deltaTime * 15f);
            }
        }

        private void ResetBinVisuals()
        {
            if (longBinRect != null) longBinRect.localScale = Vector3.one;
            if (shortBinRect != null) shortBinRect.localScale = Vector3.one;
        }

        private IEnumerator CoGlideToTarget(Vector2 targetPos, Action onComplete)
        {
            isProcessing = true;
            float elapsed = 0f;
            float duration = 0.18f;
            Vector2 startPos = cardRectTransform.anchoredPosition;
            Vector3 startScale = cardRectTransform.localScale;
            Quaternion startRot = cardRectTransform.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                if (cardRectTransform != null)
                {
                    cardRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                    cardRectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.9f, t);
                    cardRectTransform.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, t);
                }
                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator CoSmoothReturn()
        {
            float elapsed = 0f;
            float duration = 0.22f;
            Vector2 startPos = cardRectTransform.anchoredPosition;
            Vector3 startScale = cardRectTransform.localScale;
            Quaternion startRot = cardRectTransform.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                if (cardRectTransform != null)
                {
                    cardRectTransform.anchoredPosition = Vector2.Lerp(startPos, cardOriginPos, t);
                    cardRectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
                    cardRectTransform.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, t);
                }
                yield return null;
            }

            if (cardRectTransform != null)
            {
                cardRectTransform.anchoredPosition = cardOriginPos;
                cardRectTransform.localScale = Vector3.one;
                cardRectTransform.localRotation = Quaternion.identity;
            }
        }

        private void OnBinSelected(VowelLength selectedChoice)
        {
            isProcessing = true;

            OneOrTwoItem item = items[currentItemIndex];
            bool isCorrect = (selectedChoice == item.vowelLength);

            if (isCorrect)
            {
                StartCoroutine(CoHandleCorrect(item));
            }
            else
            {
                StartCoroutine(CoHandleWrong(item));
            }
        }

        private IEnumerator CoHandleCorrect(OneOrTwoItem item)
        {
            if (!hasFailedCurrentItem)
            {
                firstAttemptSuccesses++;
                U9_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(100);
            }

            UpdateHUD();

            if (item.vowelLength == VowelLength.LongOpen)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance?.PlayGateOpen();
                if (openGateGraphicGO != null) openGateGraphicGO.SetActive(true);
                if (splitExplanationText != null)
                {
                    splitExplanationText.text = $"<b>{item.splitWord} -> 1 consonant before -le (Open Gate = Long Vowel)</b>";
                }
            }
            else
            {
                U9_SA_AudioManager_Masters_Phonics.Instance?.PlayDoorSlam();
                if (closedDoorGraphicGO != null) closedDoorGraphicGO.SetActive(true);
                if (splitExplanationText != null)
                {
                    splitExplanationText.text = $"<b>{item.splitWord} -> 2 consonants before -le (Closed Door = Short Vowel)</b>";
                }
            }

            // Word audio
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlaySyllables(item.splitWord);

            yield return new WaitForSeconds(1.4f);

            currentItemIndex++;
            LoadCurrentItem();
        }

        private IEnumerator CoHandleWrong(OneOrTwoItem item)
        {
            hasFailedCurrentItem = true;
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
            U9_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(item.fullWord);

            // Smooth return to origin
            yield return StartCoroutine(CoSmoothReturn());

            // Remediation line
            string remediationVO = item.vowelLength == VowelLength.LongOpen ? "U09_VO_a2_gate" : "U09_VO_a2_door";
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(remediationVO);

            yield return new WaitForSeconds(1.5f);
            isProcessing = false;
        }

        private void UpdateHUD()
        {
            if (progressText != null)
            {
                progressText.text = $"<b>Item {currentItemIndex + 1} of {items.Count}</b>";
            }

            if (progressBar != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
                progressBar.value = items.Count > 0 ? (float)(currentItemIndex + 1) / items.Count : 0f;
            }

            if (scoreText != null && U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                scoreText.text = $"<b>Score: {U9_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore()}</b>";
            }
        }

        private void CompleteActivity()
        {
            int earnedStars = 1;
            if (firstAttemptSuccesses >= 14) earnedStars = 3;
            else if (firstAttemptSuccesses >= 11) earnedStars = 2;

            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.ShowActivityCompletionDialog(
                    transform, 2, earnedStars, firstAttemptSuccesses * 100, () =>
                    {
                        U9_SA_UnitFlowManager_Masters_Phonics.Instance.OpenActivity3();
                    });
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 9 One Or Two] Auto-Assigned Hierarchy Elements & Bins!</b></color>");
        }
#endif
    }

    /// <summary>
    /// Helper component dynamically attached to the Card object to forward pointer and drag events smoothly.
    /// </summary>
    public class U9_OneOrTwo_CardDragHelper : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private U9_SA_GM03s_OneOrTwo_Masters_Phonics parentGameMode;

        public void Init(U9_SA_GM03s_OneOrTwo_Masters_Phonics gm)
        {
            parentGameMode = gm;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (parentGameMode != null) parentGameMode.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (parentGameMode != null) parentGameMode.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (parentGameMode != null) parentGameMode.OnEndDrag(eventData);
        }
    }
}
