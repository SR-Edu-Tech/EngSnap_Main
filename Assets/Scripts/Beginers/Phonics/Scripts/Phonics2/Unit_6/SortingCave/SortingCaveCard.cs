using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit6
{
    public class SortingCaveCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI Visuals")]
        [SerializeField] private TMP_Text soundSymbolTMP;
        [SerializeField] private TMP_Text wordNameTMP;
        [SerializeField] private Image wordImage;
        [SerializeField] private CanvasGroup canvasGroup;

        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private Vector2 initialCenterPosition;
        private bool hasInitializedCenter = false;
        private SortingCaveController controller;
        private ConsonantSoundCardItem cardData;
        private bool isDragging = false;
        private bool isLocked = false;
        private Coroutine returnCoroutine;
        private Coroutine appearCoroutine;
        private Coroutine absorbCoroutine;

        public ConsonantSoundCardItem CardData => cardData;
        public bool IsLocked => isLocked;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            parentCanvas = GetComponentInParent<Canvas>();

            if (!hasInitializedCenter && rectTransform != null)
            {
                initialCenterPosition = rectTransform.anchoredPosition;
                hasInitializedCenter = true;
            }
        }

        public void SetupCard(ConsonantSoundCardItem data, SortingCaveController parentController)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

            if (!hasInitializedCenter && rectTransform != null)
            {
                initialCenterPosition = rectTransform.anchoredPosition;
                hasInitializedCenter = true;
            }

            cardData = data;
            controller = parentController;
            isLocked = false;
            isDragging = false;

            StopAllActiveCardCoroutines();

            // Reset anchored position strictly back to initial center position
            rectTransform.anchoredPosition = initialCenterPosition;

            if (soundSymbolTMP != null) soundSymbolTMP.text = data.soundSymbol;
            if (wordNameTMP != null) wordNameTMP.text = data.wordName;

            if (wordImage != null && data.wordSprite != null)
            {
                wordImage.sprite = data.wordSprite;
                wordImage.gameObject.SetActive(true);
            }
            else if (wordImage != null)
            {
                wordImage.gameObject.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            gameObject.SetActive(true);
            appearCoroutine = StartCoroutine(AppearPopRoutine());
        }

        private void StopAllActiveCardCoroutines()
        {
            if (returnCoroutine != null) { StopCoroutine(returnCoroutine); returnCoroutine = null; }
            if (appearCoroutine != null) { StopCoroutine(appearCoroutine); appearCoroutine = null; }
            if (absorbCoroutine != null) { StopCoroutine(absorbCoroutine); absorbCoroutine = null; }
        }

        private IEnumerator AppearPopRoutine()
        {
            transform.localScale = Vector3.one * 0.3f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = t * t * (3f - 2f * t);

                transform.localScale = Vector3.Lerp(Vector3.one * 0.3f, Vector3.one, smoothT);
                if (canvasGroup != null) canvasGroup.alpha = smoothT;
                yield return null;
            }

            transform.localScale = Vector3.one;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            appearCoroutine = null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isLocked || controller == null || controller.IsTransitioning) return;

            StopAllActiveCardCoroutines();

            isDragging = true;
            transform.SetAsLastSibling();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.9f;
                canvasGroup.blocksRaycasts = false;
            }

            transform.localScale = Vector3.one * 1.08f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || isLocked || parentCanvas == null) return;
            rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging || isLocked) return;
            isDragging = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            transform.localScale = Vector3.one;

            if (controller != null)
            {
                controller.EvaluateCardDrop(this, eventData);
            }
            else
            {
                ReturnToStartPosition();
            }
        }

        public void ReturnToStartPosition()
        {
            StopAllActiveCardCoroutines();
            returnCoroutine = StartCoroutine(SmoothReturn());
        }

        private IEnumerator SmoothReturn()
        {
            float elapsed = 0f;
            float duration = 0.35f;
            Vector2 currentPos = rectTransform.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = t * t * (3f - 2f * t);
                rectTransform.anchoredPosition = Vector2.Lerp(currentPos, initialCenterPosition, smoothT);
                yield return null;
            }

            rectTransform.anchoredPosition = initialCenterPosition;
            returnCoroutine = null;
        }

        public void AnimateAbsorbIntoCrystal(RectTransform targetSlot, Action onComplete)
        {
            StopAllActiveCardCoroutines();
            isLocked = true;
            absorbCoroutine = StartCoroutine(AbsorbIntoBinRoutine(targetSlot, onComplete));
        }

        private IEnumerator AbsorbIntoBinRoutine(RectTransform targetSlot, Action onComplete)
        {
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 targetPos = targetSlot != null ? targetSlot.anchoredPosition : startPos;

            // If target slot is under a different parent, calculate parent local position accurately
            if (targetSlot != null && targetSlot.parent != rectTransform.parent && parentCanvas != null)
            {
                Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, targetSlot.position);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, screenPoint, cam, out Vector2 localPoint))
                {
                    targetPos = localPoint;
                }
            }

            float elapsed = 0f;
            float duration = 0.4f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = t * t * (3f - 2f * t);

                rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                // Shrink and fade as it enters the crystal bin
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.15f, smoothT);
                if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);

            absorbCoroutine = null;
            onComplete?.Invoke();
        }

        public void HideCard()
        {
            StopAllActiveCardCoroutines();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
        }
    }
}
