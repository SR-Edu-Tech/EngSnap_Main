using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit5
{
    public class MagicEWand : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Visual Components")]
        [SerializeField] private Image wandImage;
        [SerializeField] private GameObject sparkleParticles;
        [SerializeField] private GameObject wandGlowObject;
        [SerializeField] private Button wandButton;

        [Header("Animation & Drag Tuning")]
        [SerializeField] private float idleFloatSpeed = 2.5f;
        [SerializeField] private float idleFloatAmount = 8f;
        [SerializeField] private float castFlightDuration = 0.45f;
        [SerializeField] private float returnFlightDuration = 0.35f;
        [SerializeField] private float dropSnapDistance = 140f;

        private MagicEController controller;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private CanvasGroup canvasGroup;
        private Vector3 homeWorldPosition;
        private Vector2 homeAnchoredPosition;
        private Vector3 initialScale;
        private bool isDragging = false;
        private bool isCasting = false;
        private Coroutine idleFloatCoroutine;

        public bool IsDragging => isDragging;
        public bool IsCasting => isCasting;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            parentCanvas = GetComponentInParent<Canvas>();

            initialScale = transform.localScale;
            if (rectTransform != null)
            {
                homeAnchoredPosition = rectTransform.anchoredPosition;
            }
            homeWorldPosition = transform.position;

            if (wandButton != null)
            {
                wandButton.onClick.AddListener(OnWandButtonClicked);
            }
        }

        private void OnEnable()
        {
            homeWorldPosition = transform.position;
            if (rectTransform != null) homeAnchoredPosition = rectTransform.anchoredPosition;
            StartIdleFloat();
        }

        private void OnDisable()
        {
            StopIdleFloat();
            ResetWandState();
        }

        public void SetupWand(MagicEController mainController)
        {
            controller = mainController;
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

            homeWorldPosition = transform.position;
            if (rectTransform != null) homeAnchoredPosition = rectTransform.anchoredPosition;

            if (sparkleParticles != null) sparkleParticles.SetActive(false);
            if (wandGlowObject != null) wandGlowObject.SetActive(true);

            ResetWandState();
            StartIdleFloat();
        }

        public void ResetWandState()
        {
            isDragging = false;
            isCasting = false;
            transform.position = homeWorldPosition;
            if (rectTransform != null) rectTransform.anchoredPosition = homeAnchoredPosition;
            transform.localScale = initialScale;
            transform.rotation = Quaternion.identity;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            if (sparkleParticles != null) sparkleParticles.SetActive(false);
        }

        #region Drag and Drop Handling

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isCasting || (controller != null && controller.IsTransitioning)) return;

            isDragging = true;
            StopIdleFloat();

            if (sparkleParticles != null) sparkleParticles.SetActive(true);
            if (wandGlowObject != null) wandGlowObject.SetActive(true);

            transform.localScale = initialScale * 1.15f;
            transform.SetAsLastSibling();

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            if (parentCanvas != null && rectTransform != null)
            {
                rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
            }
            else
            {
                transform.position = eventData.position;
            }

            // Notify controller for live proximity highlights on empty landing slot
            if (controller != null)
            {
                controller.OnWandDragUpdate(transform.position, dropSnapDistance);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            transform.localScale = initialScale;

            if (controller != null)
            {
                bool hitTarget = controller.EvaluateWandDrop(transform.position, dropSnapDistance);
                if (!hitTarget)
                {
                    ReturnToStartPosition();
                }
            }
            else
            {
                ReturnToStartPosition();
            }
        }

        #endregion

        #region Pointer / Click Handling

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging) return;
            TriggerWandCast();
        }

        private void OnWandButtonClicked()
        {
            if (isDragging) return;
            TriggerWandCast();
        }

        private void TriggerWandCast()
        {
            if (isCasting || isDragging) return;
            if (controller != null && !controller.IsTransitioning)
            {
                controller.CastMagicEWand();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isCasting || isDragging) return;
            transform.localScale = initialScale * 1.12f;
            if (wandGlowObject != null) wandGlowObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isCasting || isDragging) return;
            transform.localScale = initialScale;
        }

        #endregion

        #region Idle Float Animation

        private void StartIdleFloat()
        {
            StopIdleFloat();
            if (gameObject.activeInHierarchy && !isDragging && !isCasting)
            {
                idleFloatCoroutine = StartCoroutine(IdleFloatRoutine());
            }
        }

        private void StopIdleFloat()
        {
            if (idleFloatCoroutine != null)
            {
                StopCoroutine(idleFloatCoroutine);
                idleFloatCoroutine = null;
            }
        }

        private IEnumerator IdleFloatRoutine()
        {
            float time = 0f;
            while (!isCasting && !isDragging)
            {
                time += Time.deltaTime * idleFloatSpeed;
                float yOffset = Mathf.Sin(time) * idleFloatAmount;
                float angle = Mathf.Sin(time * 0.8f) * 4f;
                transform.position = homeWorldPosition + new Vector3(0, yOffset, 0);
                transform.rotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
        }

        #endregion

        #region Cast Flight & Return Animation

        public void ReturnToStartPosition()
        {
            StopAllCoroutines();
            StartCoroutine(SmoothReturn(homeAnchoredPosition));
        }

        private IEnumerator SmoothReturn(Vector2 targetAnchoredPos)
        {
            float elapsed = 0f;
            float duration = 0.28f;
            Vector2 startPos = rectTransform != null ? rectTransform.anchoredPosition : (Vector2)transform.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, smoothT);
                }
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI) * 8f);
                yield return null;
            }

            if (rectTransform != null) rectTransform.anchoredPosition = targetAnchoredPos;
            transform.rotation = Quaternion.identity;
            transform.localScale = initialScale;
            if (sparkleParticles != null) sparkleParticles.SetActive(false);

            StartIdleFloat();
        }

        public void PlayCastAnimation(Vector3 targetWordPos, Action onLandCallback = null)
        {
            StopIdleFloat();
            StartCoroutine(WandCastSequence(targetWordPos, onLandCallback));
        }

        private IEnumerator WandCastSequence(Vector3 targetPos, Action onLandCallback)
        {
            isCasting = true;
            Vector3 startPos = transform.position;
            float elapsed = 0f;

            if (sparkleParticles != null) sparkleParticles.SetActive(true);

            // Fly smoothly towards target position (end of the word) with arc
            while (elapsed < castFlightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / castFlightDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                float arc = Mathf.Sin(t * Mathf.PI) * 40f;
                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, smoothT);
                currentPos.y += arc;
                transform.position = currentPos;

                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 4f) * 15f);
                yield return null;
            }

            transform.position = targetPos;
            transform.rotation = Quaternion.identity;

            // Trigger silent e landing callback
            onLandCallback?.Invoke();

            yield return new WaitForSeconds(0.35f);

            // Return smoothly to home position
            elapsed = 0f;
            Vector3 returnStartPos = transform.position;
            while (elapsed < returnFlightDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / returnFlightDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(returnStartPos, homeWorldPosition, smoothT);
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 2f) * 10f);
                yield return null;
            }

            transform.position = homeWorldPosition;
            if (rectTransform != null) rectTransform.anchoredPosition = homeAnchoredPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = initialScale;

            if (sparkleParticles != null) sparkleParticles.SetActive(false);
            isCasting = false;

            StartIdleFloat();
        }

        #endregion
    }
}


