using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit5
{
    public class NameSayersHat : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private bool isMacron = true; // true = flat hat (macron/long), false = curved hat (breve/short)

        private NameSayersController controller;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private CanvasGroup canvasGroup;
        private Vector3 startAnchoredPosition;
        private bool isDragging = false;

        public bool IsMacron => isMacron;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            parentCanvas = GetComponentInParent<Canvas>();

            if (rectTransform != null)
            {
                startAnchoredPosition = rectTransform.anchoredPosition;
            }
        }

        public void SetupHat(NameSayersController mainController, bool macron)
        {
            controller = mainController;
            isMacron = macron;
            isDragging = false;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

            ResetPosition();
        }

        public void ResetPosition()
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = startAnchoredPosition;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (controller != null && controller.IsTransitioning) return;

            isDragging = true;
            if (rectTransform != null)
            {
                startAnchoredPosition = rectTransform.anchoredPosition;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0.85f;
            }
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            if (parentCanvas != null && rectTransform != null)
            {
                rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            if (controller != null)
            {
                controller.EvaluateHatDrop(this, eventData);
            }
            else
            {
                ReturnToStartPosition();
            }
        }

        public void ReturnToStartPosition()
        {
            StartCoroutine(SmoothReturn(startAnchoredPosition));
        }

        private IEnumerator SmoothReturn(Vector3 targetAnchoredPos)
        {
            float elapsed = 0f;
            float duration = 0.25f;
            Vector3 start = rectTransform.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rectTransform.anchoredPosition = Vector3.Lerp(start, targetAnchoredPos, elapsed / duration);
                yield return null;
            }

            rectTransform.anchoredPosition = targetAnchoredPos;
        }
    }
}
