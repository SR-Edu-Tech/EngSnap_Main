using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Unit4
{
    public class FiveVowelsBalloon : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image balloonImage;
        [SerializeField] private TMP_Text letterText;
        [SerializeField] private GameObject tickMarkImage;
        [SerializeField] private GameObject glowHighlight;

        [Header("Floating Animation")]
        [SerializeField] private float floatSpeed = 1.5f;
        [SerializeField] private float floatAmount = 12f;
        [SerializeField] private float wobbleSpeed = 2f;
        [SerializeField] private float wobbleAngle = 4f;

        private FiveVowelsData data;
        private FiveVowelsController controller;
        private Button button;
        private RectTransform rectTransform;
        private Vector3 initialPosition;
        private bool isTapped = false;
        private bool isAnimating = true;

        public FiveVowelsData Data => data;
        public bool IsTapped => isTapped;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();

            if (rectTransform != null)
            {
                initialPosition = rectTransform.anchoredPosition;
            }
        }

        private void Start()
        {
            if (rectTransform != null)
            {
                initialPosition = rectTransform.anchoredPosition;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnBalloonClicked);
        }

        private void OnEnable()
        {
            isAnimating = true;
            StartCoroutine(FloatAnimation());
        }

        private void OnDisable()
        {
            isAnimating = false;
            StopAllCoroutines();
        }

        public void Setup(FiveVowelsData vowelData, FiveVowelsController parentController)
        {
            data = vowelData;
            controller = parentController;
            isTapped = false;

            if (data != null)
            {
                if (letterText != null) letterText.text = data.vowelLetter.ToUpper();

                if (balloonImage != null && data.balloonColor != Color.clear)
                {
                    balloonImage.color = data.balloonColor;
                }
            }

            if (tickMarkImage != null) tickMarkImage.SetActive(false);
            if (glowHighlight != null) glowHighlight.SetActive(false);
        }

        public void ResetBalloon()
        {
            isTapped = false;
            if (tickMarkImage != null) tickMarkImage.SetActive(false);
            if (glowHighlight != null) glowHighlight.SetActive(false);
            transform.localScale = Vector3.one;
            if (rectTransform != null && initialPosition != Vector3.zero)
            {
                rectTransform.anchoredPosition = initialPosition;
            }
        }

        private void OnBalloonClicked()
        {
            if (controller != null && controller.IsTransitioning) return;

            StartCoroutine(TapSequence());
        }

        private IEnumerator TapSequence()
        {
            // Pop & scale bounce effect
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.25f, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale * 1.25f, originalScale * 1.05f, t);
                yield return null;
            }

            isTapped = true;
            if (tickMarkImage != null) tickMarkImage.SetActive(true);
            if (glowHighlight != null) glowHighlight.SetActive(true);

            if (controller != null)
            {
                controller.OnVowelBalloonTapped(this);
            }
        }

        public void SetChantHighlight(bool active)
        {
            if (glowHighlight != null) glowHighlight.SetActive(active);
            if (active)
            {
                transform.localScale = Vector3.one * 1.15f;
            }
            else
            {
                transform.localScale = Vector3.one * 1.05f;
            }
        }

        private IEnumerator FloatAnimation()
        {
            // Stagger start time slightly based on sibling index
            float timeOffset = transform.GetSiblingIndex() * 0.7f;

            while (isAnimating)
            {
                if (rectTransform != null)
                {
                    float yOffset = Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatAmount;
                    float zRot = Mathf.Sin((Time.time + timeOffset) * wobbleSpeed) * wobbleAngle;

                    rectTransform.anchoredPosition = initialPosition + new Vector3(0, yOffset, 0);
                    rectTransform.localRotation = Quaternion.Euler(0, 0, zRot);
                }
                yield return null;
            }
        }
    }
}
