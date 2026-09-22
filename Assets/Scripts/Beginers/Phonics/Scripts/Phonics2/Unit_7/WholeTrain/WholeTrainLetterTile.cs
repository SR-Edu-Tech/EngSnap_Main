using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit7
{
    [RequireComponent(typeof(RectTransform))]
    public class WholeTrainLetterTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
    {
        [Header("Tile Info")]
        [SerializeField] private int choiceIndex = 0;
        [SerializeField] private string letterText = "";

        [Header("Drag & Snap Settings")]
        [SerializeField] private float dragScaleMultiplier = 1.15f;
        [SerializeField] private float snapDistanceThreshold = 160f;
        [SerializeField] private float returnDuration = 0.25f;

        [Header("Vanish On Drop")]
        [Tooltip("When true, the tile vanishes in 0 seconds upon valid drop into the active carriage slot.")]
        [SerializeField] private bool vanishInstantlyOnDrop = true;

        private WholeTrainController controller;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private Vector2 startAnchoredPosition;
        private Vector3 originalScale;
        private int originalSiblingIndex;
        private bool isInitialized = false;
        private bool isDragging = false;
        private bool isReturning = false;
        private bool wasDragged = false;

        public int ChoiceIndex => choiceIndex;
        public string LetterText => letterText;
        public RectTransform Rect => rectTransform;

        private void Awake()
        {
            InitializeReferences();
        }

        private void Start()
        {
            SaveStartPosition();
        }

        private void InitializeReferences()
        {
            if (isInitialized) return;

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            rootCanvas = GetComponentInParent<Canvas>();
            if (controller == null) controller = GetComponentInParent<WholeTrainController>();

            originalScale = transform.localScale;
            isInitialized = true;
        }

        public void Setup(int index, string letter, WholeTrainController mainController)
        {
            InitializeReferences();
            choiceIndex = index;
            letterText = letter;
            controller = mainController;
            SaveStartPosition();
            ResetToHome(true);
        }

        public void SaveStartPosition()
        {
            if (rectTransform != null)
            {
                startAnchoredPosition = rectTransform.anchoredPosition;
                originalSiblingIndex = transform.GetSiblingIndex();
            }
        }

        public void ResetToHome(bool immediate = true)
        {
            StopAllCoroutines();
            isDragging = false;
            isReturning = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            transform.localScale = originalScale;

            if (immediate && rectTransform != null)
            {
                rectTransform.anchoredPosition = startAnchoredPosition;
                transform.SetSiblingIndex(originalSiblingIndex);
            }
            else if (gameObject.activeInHierarchy)
            {
                StartCoroutine(SmoothReturnRoutine(returnDuration));
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            wasDragged = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (controller != null && controller.IsTransitioning) return;
            if (isReturning) return;

            isDragging = true;
            wasDragged = true;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }

            transform.SetAsLastSibling();
            transform.localScale = originalScale * dragScaleMultiplier;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || rootCanvas == null || rectTransform == null) return;

            rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            transform.localScale = originalScale;

            if (controller == null)
            {
                ReturnToHomePosition();
                return;
            }

            RectTransform targetSlotRect = controller.ActiveCarriageSlotRect;
            if (targetSlotRect != null && IsOverlappingTarget(eventData.position, targetSlotRect))
            {
                controller.OnTileDropped(choiceIndex, this);
            }
            else
            {
                ReturnToHomePosition();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (wasDragged) return;
            if (controller != null && !controller.IsTransitioning)
            {
                controller.OnTileChoiceClicked(choiceIndex);
            }
        }

        public void ReturnToHomePosition()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(SmoothReturnRoutine(returnDuration));
            }
            else if (rectTransform != null)
            {
                rectTransform.anchoredPosition = startAnchoredPosition;
            }
        }

        public void OnDropSuccess()
        {
            StopAllCoroutines();
            isDragging = false;
            isReturning = false;

            if (vanishInstantlyOnDrop)
            {
                // Vanish in 0 seconds
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                }

                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = startAnchoredPosition;
                }

                transform.localScale = originalScale;
                transform.SetSiblingIndex(originalSiblingIndex);
            }
            else
            {
                StartCoroutine(SmoothReturnRoutine(returnDuration));
            }
        }

        public void OnDropFailed()
        {
            StartCoroutine(FailedDropRoutine());
        }

        private IEnumerator SmoothReturnRoutine(float duration)
        {
            isReturning = true;
            Vector2 currentPos = rectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                rectTransform.anchoredPosition = Vector2.Lerp(currentPos, startAnchoredPosition, t);
                yield return null;
            }

            rectTransform.anchoredPosition = startAnchoredPosition;
            transform.SetSiblingIndex(originalSiblingIndex);
            isReturning = false;
        }

        private IEnumerator FailedDropRoutine()
        {
            Vector2 currentPos = rectTransform.anchoredPosition;
            float elapsed = 0f;
            float shakeDuration = 0.3f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin(elapsed * 40f) * 15f * (1f - elapsed / shakeDuration);
                rectTransform.anchoredPosition = currentPos + new Vector2(offset, 0f);
                yield return null;
            }

            yield return StartCoroutine(SmoothReturnRoutine(returnDuration));
        }

        private bool IsOverlappingTarget(Vector2 screenPosition, RectTransform targetRect)
        {
            if (targetRect == null) return false;

            Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, screenPosition, cam))
            {
                return true;
            }

            float dist = Vector2.Distance(rectTransform.position, targetRect.position);
            float screenThreshold = snapDistanceThreshold * (rootCanvas != null ? rootCanvas.scaleFactor : 1f);
            return dist <= screenThreshold;
        }
    }
}
