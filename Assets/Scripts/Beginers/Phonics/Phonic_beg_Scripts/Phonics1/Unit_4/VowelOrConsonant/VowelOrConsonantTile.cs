using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Unit4
{
    public class VowelOrConsonantTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text letterText;
        [SerializeField] private Image tileImage;

        private VowelOrConsonantData data;
        private VowelOrConsonantController controller;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private CanvasGroup canvasGroup;
        private Vector3 startPosition;
        private Vector2 landingPosition;
        private Transform startParent;
        private bool isDragging = false;
        private bool isDroppedCorrectly = false;
        private Coroutine activeAnimationCoroutine;

        public VowelOrConsonantData Data => data;
        public bool IsDroppedCorrectly => isDroppedCorrectly;
        public bool IsDragging => isDragging;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            parentCanvas = GetComponentInParent<Canvas>();

            LayoutElement layoutElem = GetComponent<LayoutElement>();
            if (layoutElem != null) layoutElem.ignoreLayout = true;
        }

        public void Setup(VowelOrConsonantData tileData, VowelOrConsonantController parentController)
        {
            data = tileData;
            controller = parentController;
            isDroppedCorrectly = false;
            isDragging = false;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

            startPosition = rectTransform.anchoredPosition;
            landingPosition = rectTransform.anchoredPosition;
            startParent = transform.parent;

            if (data != null)
            {
                if (letterText != null) letterText.text = data.letter.ToUpper();
                if (tileImage != null && data.letterSprite != null)
                {
                    tileImage.sprite = data.letterSprite;
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

        public void AnimateFallFromTop(Vector2 spawnPos, Vector2 targetLandingPos, float fallDuration = 0.8f)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            landingPosition = targetLandingPos;
            rectTransform.anchoredPosition = spawnPos;
            transform.localScale = Vector3.one;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            gameObject.SetActive(true);

            if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
            activeAnimationCoroutine = StartCoroutine(FallSequence(spawnPos, targetLandingPos, fallDuration));
        }

        private IEnumerator FallSequence(Vector2 startPos, Vector2 endPos, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (isDragging) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
                yield return null;
            }

            rectTransform.anchoredPosition = endPos;
            activeAnimationCoroutine = null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isDroppedCorrectly || (controller != null && controller.IsTransitioning)) return;

            if (activeAnimationCoroutine != null)
            {
                StopCoroutine(activeAnimationCoroutine);
                activeAnimationCoroutine = null;
            }

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
                    controller.CheckTileDrop(this, eventData);
                }
                else
                {
                    ReturnToLandingPosition();
                }
            }
        }

        public void OnDroppedOnBasket(VowelOrConsonantBasket basket)
        {
            if (controller != null)
            {
                controller.EvaluateTileDrop(this, basket);
            }
        }

        public void SetCorrectDrop(Vector3 targetWorldPos)
        {
            isDroppedCorrectly = true;
            if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
            activeAnimationCoroutine = StartCoroutine(DropInAnimation(targetWorldPos));
        }

        public void PlayWrongWobble()
        {
            if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
            activeAnimationCoroutine = StartCoroutine(WobbleAndReturnAnimation());
        }

        public void ReturnToStartPosition()
        {
            ReturnToLandingPosition();
        }

        public void ReturnToLandingPosition()
        {
            if (activeAnimationCoroutine != null) StopCoroutine(activeAnimationCoroutine);
            activeAnimationCoroutine = StartCoroutine(SmoothReturn(landingPosition));
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
            activeAnimationCoroutine = null;
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

            yield return StartCoroutine(SmoothReturn(landingPosition));
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
            activeAnimationCoroutine = null;
        }
    }
}
