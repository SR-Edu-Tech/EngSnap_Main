using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit8
{
    /// <summary>
    /// Interactive slide bar mirroring the shoulder-slide physical blending action.
    /// Supports both standard UnityEngine.UI.Slider and custom Image/Handle RectTransform track.
    /// Tracks child's finger drag from left (shoulder: letter 0) to middle (elbow: letter 1)
    /// to right (wrist: letter 2), triggering sound activations and blend completion.
    /// </summary>
    public class BlendSlideBar : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Slider Track References")]
        [SerializeField] private RectTransform trackRect;
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private Image fillImage;
        [SerializeField] private Slider unitySlider;

        [Header("Letter Stop Positions (Normalized 0.0 to 1.0)")]
        [SerializeField] private float stop0Threshold = 0.15f; // First letter
        [SerializeField] private float stop1Threshold = 0.50f; // Middle letter
        [SerializeField] private float stop2Threshold = 0.85f; // Last letter
        [SerializeField] private float blendThreshold = 0.95f; // Rush-together blend trigger

        // Events
        public event Action<int> OnLetterStopReached; // 0, 1, 2
        public event Action OnBlendCompleted;
        public event Action OnSlideCancelled;

        private bool[] hasTriggeredStop = new bool[3];
        private bool isDragging = false;
        private bool isCompleted = false;
        private float currentProgress = 0f;
        private Vector2 handleStartPos;

        public bool IsDragging => isDragging;
        public float CurrentProgress => currentProgress;

        private void Awake()
        {
            if (trackRect == null) trackRect = GetComponent<RectTransform>();
            if (unitySlider == null) unitySlider = GetComponent<Slider>();
            if (unitySlider == null) unitySlider = GetComponentInChildren<Slider>();

            if (handleRect != null)
            {
                handleStartPos = handleRect.anchoredPosition;
            }
            else if (unitySlider != null && unitySlider.handleRect != null)
            {
                handleRect = unitySlider.handleRect;
                handleStartPos = handleRect.anchoredPosition;
            }

            if (fillImage == null && unitySlider != null && unitySlider.fillRect != null)
            {
                fillImage = unitySlider.fillRect.GetComponent<Image>();
            }

            ResetSlider();
        }

        private void OnEnable()
        {
            if (unitySlider != null)
            {
                unitySlider.onValueChanged.RemoveListener(OnUnitySliderValueChanged);
                unitySlider.onValueChanged.AddListener(OnUnitySliderValueChanged);
            }
        }

        private void OnDisable()
        {
            if (unitySlider != null)
            {
                unitySlider.onValueChanged.RemoveListener(OnUnitySliderValueChanged);
            }
        }

        public void ResetSlider()
        {
            isDragging = false;
            isCompleted = false;
            currentProgress = 0f;
            hasTriggeredStop[0] = false;
            hasTriggeredStop[1] = false;
            hasTriggeredStop[2] = false;

            SetInteractable(true);

            if (unitySlider != null)
            {
                unitySlider.value = unitySlider.minValue;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }

            if (trackRect != null && handleRect != null)
            {
                float width = trackRect.rect.width;
                float startX = -0.5f * width;
                handleRect.anchoredPosition = new Vector2(startX, handleRect.anchoredPosition.y);
            }
            else if (handleRect != null)
            {
                handleRect.anchoredPosition = handleStartPos;
            }
        }

        public void SetInteractable(bool interactable)
        {
            enabled = interactable;
            if (unitySlider != null) unitySlider.interactable = interactable;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isCompleted) return;
            isDragging = true;
            UpdateProgressFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || isCompleted) return;
            UpdateProgressFromPointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            if (!isCompleted)
            {
                OnSlideCancelled?.Invoke();
                ResetSlider();
            }
        }

        private void OnUnitySliderValueChanged(float val)
        {
            if (unitySlider == null) return;
            float range = unitySlider.maxValue - unitySlider.minValue;
            float norm = (range > 0.0001f) ? Mathf.Clamp01((val - unitySlider.minValue) / range) : 0f;
            ProcessNormalizedProgress(norm);
        }

        private void UpdateProgressFromPointer(PointerEventData eventData)
        {
            if (trackRect == null) return;

            Vector2 localPoint;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRect, eventData.position, eventData.pressEventCamera, out localPoint))
                return;

            float width = trackRect.rect.width;
            if (width <= 0f) return;

            float norm = Mathf.Clamp01((localPoint.x + (width * 0.5f)) / width);
            ProcessNormalizedProgress(norm);
        }

        private void ProcessNormalizedProgress(float norm)
        {
            currentProgress = norm;

            if (fillImage != null) fillImage.fillAmount = norm;

            if (unitySlider != null && Mathf.Abs(unitySlider.value - Mathf.Lerp(unitySlider.minValue, unitySlider.maxValue, norm)) > 0.01f)
            {
                unitySlider.value = Mathf.Lerp(unitySlider.minValue, unitySlider.maxValue, norm);
            }

            if (handleRect != null && trackRect != null)
            {
                float width = trackRect.rect.width;
                float targetX = (norm - 0.5f) * width;
                handleRect.anchoredPosition = new Vector2(targetX, handleRect.anchoredPosition.y);
            }

            // Check stops
            if (norm >= stop0Threshold && !hasTriggeredStop[0])
            {
                hasTriggeredStop[0] = true;
                OnLetterStopReached?.Invoke(0);
            }
            if (norm >= stop1Threshold && !hasTriggeredStop[1])
            {
                hasTriggeredStop[1] = true;
                OnLetterStopReached?.Invoke(1);
            }
            if (norm >= stop2Threshold && !hasTriggeredStop[2])
            {
                hasTriggeredStop[2] = true;
                OnLetterStopReached?.Invoke(2);
            }

            if (norm >= blendThreshold && !isCompleted)
            {
                isCompleted = true;
                isDragging = false;
                OnBlendCompleted?.Invoke();
            }
        }
    }
}
