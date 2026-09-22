using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit7
{
    [RequireComponent(typeof(RectTransform))]
    public class MiddleCarriageVowelHouseTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
    {
        [Header("Vowel Info")]
        [SerializeField] private int vowelIndex = 0; // 0=a, 1=e, 2=i, 3=o, 4=u
        [SerializeField] private string vowelLetter = "a";

        [Header("Drag & Snap Settings")]
        [SerializeField] private float dragScaleMultiplier = 1.15f;
        [SerializeField] private float snapDistanceThreshold = 160f;
        [SerializeField] private float returnDuration = 0.25f;

        [Header("Vanish On Drop Settings")]
        [Tooltip("When true, the house tile vanishes immediately (0 seconds) upon dropping into the middle carriage slot.")]
        [SerializeField] private bool vanishInstantlyOnDrop = true;

        private MiddleCarriageController controller;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private Transform originalParent;
        private int originalSiblingIndex;
        private Vector2 startAnchoredPosition;
        private Vector3 originalScale;
        private bool isInitialized = false;
        private bool isDragging = false;
        private bool isReturning = false;
        private bool wasDragged = false;

        public int VowelIndex => vowelIndex;
        public string VowelLetter => vowelLetter;
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
            if (controller == null) controller = GetComponentInParent<MiddleCarriageController>();

            originalScale = transform.localScale;
            isInitialized = true;
        }

        public void Setup(int index, string letter, MiddleCarriageController mainController)
        {
            InitializeReferences();
            vowelIndex = index;
            vowelLetter = letter;
            controller = mainController;
            SaveStartPosition();
        }

        public void SaveStartPosition()
        {
            if (rectTransform != null)
            {
                startAnchoredPosition = rectTransform.anchoredPosition;
                originalSiblingIndex = transform.GetSiblingIndex();
                originalParent = transform.parent;
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
            else
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

            RectTransform slotRect = controller.MiddleCarriageSlotRect;
            if (slotRect != null && IsOverlappingTarget(eventData.position, slotRect))
            {
                controller.OnVowelHouseDropped(vowelIndex, this);
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
                controller.OnVowelHouseSelected(vowelIndex);
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
                // Vanish in 0 seconds (instantly disappear on drop into carriage slot)
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
            // Shake and return smoothly to home
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

            // Check 1: Screen point inside target rect bounds
            Camera cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rootCanvas.worldCamera : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(targetRect, screenPosition, cam))
            {
                return true;
            }

            // Check 2: World space distance threshold
            float dist = Vector2.Distance(rectTransform.position, targetRect.position);
            float screenThreshold = snapDistanceThreshold * (rootCanvas != null ? rootCanvas.scaleFactor : 1f);
            return dist <= screenThreshold;
        }
    }
}
