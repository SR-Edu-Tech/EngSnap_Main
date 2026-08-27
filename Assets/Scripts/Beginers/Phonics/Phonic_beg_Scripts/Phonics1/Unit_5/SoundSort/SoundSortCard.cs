using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Unit5
{
    public class SoundSortCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private Image cardImage;

        private SoundSortData data;
        private SoundSortController controller;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private CanvasGroup canvasGroup;
        private Vector3 startPosition;
        private Vector2 initialAnchoredPosition;
        private bool isInitialPositionSaved = false;
        private bool isDragging = false;
        private bool isDroppedCorrectly = false;

        public SoundSortData Data => data;
        public bool IsDroppedCorrectly => isDroppedCorrectly;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            parentCanvas = GetComponentInParent<Canvas>();

            LayoutElement layoutElem = GetComponent<LayoutElement>();
            if (layoutElem != null) layoutElem.ignoreLayout = true;

            if (!isInitialPositionSaved && rectTransform != null)
            {
                initialAnchoredPosition = rectTransform.anchoredPosition;
                isInitialPositionSaved = true;
            }
        }

        public void Setup(SoundSortData cardData, SoundSortController parentController)
        {
            data = cardData;
            controller = parentController;
            isDroppedCorrectly = false;
            isDragging = false;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

            if (!isInitialPositionSaved && rectTransform != null)
            {
                initialAnchoredPosition = rectTransform.anchoredPosition;
                isInitialPositionSaved = true;
            }

            if (isInitialPositionSaved && rectTransform != null)
            {
                rectTransform.anchoredPosition = initialAnchoredPosition;
            }

            startPosition = rectTransform.anchoredPosition;

            if (data != null)
            {
                if (wordText != null) wordText.text = data.word;
                if (cardImage != null && data.wordSprite != null)
                {
                    cardImage.sprite = data.wordSprite;
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging || isDroppedCorrectly) return;
            if (controller != null && data != null)
            {
                controller.SpeakCardWord(data);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDroppedCorrectly || (controller != null && controller.IsTransitioning)) return;

            isDragging = true;
            startPosition = rectTransform.anchoredPosition;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0.85f;
            }
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || isDroppedCorrectly) return;

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

            if (!isDroppedCorrectly)
            {
                if (controller != null)
                {
                    controller.CheckCardDrop(this, eventData);
                }
                else
                {
                    ReturnToStartPosition();
                }
            }
        }

        public void OnDroppedOnBucket(SoundSortBucket bucket)
        {
            if (controller != null)
            {
                controller.EvaluateCardDrop(this, bucket);
            }
        }

        public void SetCorrectDrop(Vector3 targetWorldPos)
        {
            isDroppedCorrectly = true;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
            StartCoroutine(DropInAnimation(targetWorldPos));
        }

        public void PlayWrongWobble()
        {
            StartCoroutine(WobbleAndReturnAnimation());
        }

        public void ReturnToStartPosition()
        {
            StartCoroutine(SmoothReturn(startPosition));
        }

        private IEnumerator DropInAnimation(Vector3 targetPos)
        {
            float elapsed = 0f;
            float duration = 0.25f;
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        private IEnumerator WobbleAndReturnAnimation()
        {
            float elapsed = 0f;
            float duration = 0.3f;
            Vector3 currPos = rectTransform.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 40f) * 12f;
                rectTransform.anchoredPosition = currPos + new Vector3(shake, 0, 0);
                yield return null;
            }

            yield return StartCoroutine(SmoothReturn(startPosition));
        }

        private IEnumerator SmoothReturn(Vector3 targetAnchoredPos)
        {
            float elapsed = 0f;
            float duration = 0.2f;
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
