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

        [Header("Animation & Wiggle Tuning")]
        [SerializeField] private float idleWiggleSpeed = 3.5f;
        [SerializeField] private float idleWiggleAngle = 8f;
        [SerializeField] private float castFlightDuration = 0.45f;
        [SerializeField] private float returnFlightDuration = 0.35f;
        [SerializeField] private float dropSnapDistance = 140f;

        private MagicEController controller;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private CanvasGroup canvasGroup;
        private Vector3 homeWorldPosition;
        private Vector2 homeAnchoredPosition;
        private Vector3 initialScale = Vector3.one;
        private bool isDragging = false;
        private bool isCasting = false;
        private Coroutine idleWiggleCoroutine;

        public bool IsDragging => isDragging;
        public bool IsCasting => isCasting;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (wandImage == null) wandImage = GetComponent<Image>();
            canvasGroup = GetComponent<CanvasGroup>();
            parentCanvas = GetComponentInParent<Canvas>();

            if (transform.localScale != Vector3.zero)
            {
                initialScale = transform.localScale;
            }
            else
            {
                initialScale = Vector3.one;
            }

            if (rectTransform != null && rectTransform.anchoredPosition != Vector2.zero)
            {
                homeAnchoredPosition = rectTransform.anchoredPosition;
            }
            if (transform.position != Vector3.zero)
            {
                homeWorldPosition = transform.position;
            }

            if (wandButton != null)
            {
                wandButton.onClick.AddListener(OnWandButtonClicked);
            }
        }

        private void OnEnable()
        {
            if (rectTransform != null && rectTransform.anchoredPosition != Vector2.zero)
            {
                homeAnchoredPosition = rectTransform.anchoredPosition;
            }
            if (transform.position != Vector3.zero)
            {
                homeWorldPosition = transform.position;
            }
            if (transform.localScale != Vector3.zero)
            {
                initialScale = transform.localScale;
            }
            else
            {
                initialScale = Vector3.one;
                transform.localScale = Vector3.one;
            }

            EnsureVisible();
            StartIdleWiggle();
        }

        private void OnDisable()
        {
            StopIdleWiggle();
        }

        public void SetupWand(MagicEController mainController)
        {
            controller = mainController;
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (wandImage == null) wandImage = GetComponent<Image>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

            if (rectTransform != null && rectTransform.anchoredPosition != Vector2.zero)
            {
                homeAnchoredPosition = rectTransform.anchoredPosition;
            }
            if (transform.position != Vector3.zero)
            {
                homeWorldPosition = transform.position;
            }
            if (transform.localScale != Vector3.zero)
            {
                initialScale = transform.localScale;
            }
            else
            {
                initialScale = Vector3.one;
            }

            EnsureVisible();
            ResetWandState();
            StartIdleWiggle();
        }

        public void EnsureVisible()
        {
            gameObject.SetActive(true);
            if (transform.localScale == Vector3.zero)
            {
                transform.localScale = initialScale != Vector3.zero ? initialScale : Vector3.one;
            }

            if (wandImage == null) wandImage = GetComponent<Image>();
            if (wandImage != null)
            {
                wandImage.enabled = true;
                wandImage.gameObject.SetActive(true);
                Color c = wandImage.color;
                c.a = 1f;
                wandImage.color = c;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void ResetWandState()
        {
            isDragging = false;
            isCasting = false;

            if (rectTransform != null && homeAnchoredPosition != Vector2.zero)
            {
                rectTransform.anchoredPosition = homeAnchoredPosition;
            }
            else if (homeWorldPosition != Vector3.zero)
            {
                transform.position = homeWorldPosition;
            }

            transform.localScale = (initialScale == Vector3.zero) ? Vector3.one : initialScale;
            transform.localRotation = Quaternion.identity;

            EnsureVisible();
            if (sparkleParticles != null) sparkleParticles.SetActive(false);
            if (wandGlowObject != null) wandGlowObject.SetActive(true);
        }

        #region Drag and Drop Handling

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isCasting || (controller != null && controller.IsTransitioning)) return;

            isDragging = true;
            StopIdleWiggle();

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
            PlayWiggle(0.3f, 12f);
            TriggerWandCast();
        }

        private void OnWandButtonClicked()
        {
            if (isDragging) return;
            PlayWiggle(0.3f, 12f);
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

        #region Idle Wiggle Animation (No Positional Movement)

        public void StartIdleWiggle()
        {
            StopIdleWiggle();
            if (gameObject.activeInHierarchy && !isDragging && !isCasting)
            {
                idleWiggleCoroutine = StartCoroutine(IdleWiggleRoutine());
            }
        }

        public void StopIdleWiggle()
        {
            if (idleWiggleCoroutine != null)
            {
                StopCoroutine(idleWiggleCoroutine);
                idleWiggleCoroutine = null;
            }
        }

        // Backwards compatibility aliases
        public void StartIdleFloat() => StartIdleWiggle();
        public void StopIdleFloat() => StopIdleWiggle();

        private IEnumerator IdleWiggleRoutine()
        {
            float time = 0f;
            while (!isCasting && !isDragging)
            {
                time += Time.deltaTime * idleWiggleSpeed;
                float angle = Mathf.Sin(time) * idleWiggleAngle;

                // Pure rotational wiggle: zero positional movement or displacement
                transform.localRotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
        }

        public void PlayWiggle(float duration = 0.35f, float intensity = 12f)
        {
            if (isDragging || isCasting) return;
            StopIdleWiggle();
            StartCoroutine(SingleWiggleRoutine(duration, intensity));
        }

        private IEnumerator SingleWiggleRoutine(float duration, float intensity)
        {
            float elapsed = 0f;
            while (elapsed < duration && !isDragging && !isCasting)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float decay = 1f - t;
                float angle = Mathf.Sin(elapsed * 28f) * intensity * decay;

                transform.localRotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }

            transform.localRotation = Quaternion.identity;
            StartIdleWiggle();
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

                if (rectTransform != null && targetAnchoredPos != Vector2.zero)
                {
                    rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, smoothT);
                }
                else if (homeWorldPosition != Vector3.zero)
                {
                    transform.position = Vector3.Lerp(transform.position, homeWorldPosition, smoothT);
                }

                transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 2f) * idleWiggleAngle);
                yield return null;
            }

            if (rectTransform != null && targetAnchoredPos != Vector2.zero)
            {
                rectTransform.anchoredPosition = targetAnchoredPos;
            }
            else if (homeWorldPosition != Vector3.zero)
            {
                transform.position = homeWorldPosition;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = (initialScale == Vector3.zero) ? Vector3.one : initialScale;
            EnsureVisible();
            if (sparkleParticles != null) sparkleParticles.SetActive(false);

            StartIdleWiggle();
        }

        public void PlayCastAnimation(Vector3 targetWordPos, Action onLandCallback = null)
        {
            StopIdleWiggle();
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

                transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 4f) * 15f);
                yield return null;
            }

            transform.position = targetPos;
            transform.localRotation = Quaternion.identity;

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

                if (homeWorldPosition != Vector3.zero)
                {
                    transform.position = Vector3.Lerp(returnStartPos, homeWorldPosition, smoothT);
                }
                transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * Mathf.PI * 2f) * 10f);
                yield return null;
            }

            if (rectTransform != null && homeAnchoredPosition != Vector2.zero)
            {
                rectTransform.anchoredPosition = homeAnchoredPosition;
            }
            else if (homeWorldPosition != Vector3.zero)
            {
                transform.position = homeWorldPosition;
            }

            transform.localRotation = Quaternion.identity;
            transform.localScale = (initialScale == Vector3.zero) ? Vector3.one : initialScale;
            EnsureVisible();

            if (sparkleParticles != null) sparkleParticles.SetActive(false);
            isCasting = false;

            StartIdleWiggle();
        }

        #endregion
    }
}



