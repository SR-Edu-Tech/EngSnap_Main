using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit4
{
    public class SortingHouseCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private Image cardImage;

        private SortingWordCardItem cardData;
        private SortingHouseController controller;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private CanvasGroup canvasGroup;
        private Vector3 startAnchoredPosition;
        private bool isInitialPositionSaved = false;
        private bool isDragging = false;
        private bool isDroppedCorrectly = false;

        public SortingWordCardItem CardData => cardData;
        public bool IsDroppedCorrectly => isDroppedCorrectly;
        public RectTransform Rect => rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            parentCanvas = GetComponentInParent<Canvas>();

            if (!isInitialPositionSaved && rectTransform != null)
            {
                startAnchoredPosition = rectTransform.anchoredPosition;
                isInitialPositionSaved = true;
            }
        }

        public void SetupCard(SortingWordCardItem item, SortingHouseController mainController)
        {
            cardData = item;
            controller = mainController;
            isDroppedCorrectly = false;
            isDragging = false;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

            LayoutElement layoutElem = GetComponent<LayoutElement>();
            if (layoutElem != null) layoutElem.ignoreLayout = false;

            if (!isInitialPositionSaved && rectTransform != null)
            {
                startAnchoredPosition = rectTransform.anchoredPosition;
                isInitialPositionSaved = true;
            }

            if (isInitialPositionSaved && rectTransform != null)
            {
                rectTransform.anchoredPosition = startAnchoredPosition;
            }

            if (cardData != null)
            {
                if (wordText != null) wordText.text = cardData.wordName;
                if (cardImage != null && cardData.wordSprite != null)
                {
                    cardImage.sprite = cardData.wordSprite;
                    cardImage.gameObject.SetActive(true);
                }
                else if (cardImage != null)
                {
                    cardImage.gameObject.SetActive(false);
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

        public void ResetPosition()
        {
            if (rectTransform != null && isInitialPositionSaved)
            {
                rectTransform.anchoredPosition = startAnchoredPosition;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            isDroppedCorrectly = false;
            isDragging = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging || isDroppedCorrectly) return;
            if (controller != null && cardData != null)
            {
                controller.SpeakCardAudio(cardData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDroppedCorrectly || (controller != null && controller.IsTransitioning)) return;

            isDragging = true;
            if (rectTransform != null)
            {
                startAnchoredPosition = rectTransform.anchoredPosition;
                isInitialPositionSaved = true;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = 0.85f;
            }
            transform.SetAsLastSibling();

            if (controller != null && cardData != null)
            {
                controller.SpeakCardAudio(cardData);
            }
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

            if (!isDroppedCorrectly)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                    canvasGroup.alpha = 1f;
                }

                if (controller != null)
                {
                    controller.EvaluateCardDropFromDrag(this, eventData);
                }
                else
                {
                    ReturnToStartPosition();
                }
            }
        }

        public void OnDroppedOnLetterbox(SortingHouseLetterbox letterbox)
        {
            if (controller != null && letterbox != null)
            {
                controller.EvaluateCardDrop(this, letterbox.BoxIndex);
                letterbox.PlayDropBounceAnimation();
            }
        }

        public void SetCorrectDrop(Vector3 targetWorldPos)
        {
            isDroppedCorrectly = true;
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
            StopAllCoroutines();
            StartCoroutine(DropInAnimation(targetWorldPos));
        }

        public void PlayWrongWobble()
        {
            StartCoroutine(WobbleAndReturnAnimation());
        }

        public void ReturnToStartPosition()
        {
            StartCoroutine(SmoothReturn(startAnchoredPosition));
        }

        private IEnumerator DropInAnimation(Vector3 targetPos)
        {
            float elapsed = 0f;
            float duration = 0.25f;
            Vector3 startPos = transform.position;
            Vector3 startScale = transform.localScale;

            LayoutElement layoutElem = GetComponent<LayoutElement>();
            if (layoutElem != null) layoutElem.ignoreLayout = true;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
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
                float shake = Mathf.Sin(elapsed * 40f) * 14f;
                rectTransform.anchoredPosition = currPos + new Vector3(shake, 0, 0);
                yield return null;
            }

            yield return StartCoroutine(SmoothReturn(startAnchoredPosition));
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
