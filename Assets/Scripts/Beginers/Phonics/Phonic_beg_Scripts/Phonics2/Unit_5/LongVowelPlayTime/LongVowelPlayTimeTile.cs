using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit5
{
    public class LongVowelPlayTimeTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TMP_Text tileText;

        private LongVowelPlayTimeController controller;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private CanvasGroup canvasGroup;
        private Vector3 startAnchoredPosition;
        private bool isDragging = false;
        private string tileSpelling = "ee";

        public string TileSpelling => tileSpelling;

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

        public void SetupTile(string spelling, LongVowelPlayTimeController mainController)
        {
            tileSpelling = spelling;
            controller = mainController;
            isDragging = false;

            if (tileText != null) tileText.text = spelling;
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
                controller.EvaluateTileDrop(this, eventData);
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
