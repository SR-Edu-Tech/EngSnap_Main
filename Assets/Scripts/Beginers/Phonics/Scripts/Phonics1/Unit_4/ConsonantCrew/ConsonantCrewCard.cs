using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Unit4
{
    public class ConsonantCrewCard : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text letterText;

        [Header("Dimming & Wiggle Settings")]
        [SerializeField] private float dimmedAlpha = 0.55f;
        [SerializeField] private float wiggleDuration = 0.45f;

        private ConsonantCrewData data;
        private ConsonantCrewController controller;
        private Button button;
        private CanvasGroup canvasGroup;
        private Vector3 initialScale;
        private Quaternion initialRotation;
        private Coroutine wiggleCoroutine;
        private bool isVisited = false;

        public ConsonantCrewData Data => data;
        public bool IsVisited => isVisited;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            initialScale = transform.localScale;
            initialRotation = transform.localRotation;
        }

        private void Start()
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnCardClicked);
        }

        public void Setup(ConsonantCrewData consonantData, ConsonantCrewController parentController, bool isExplored = false)
        {
            data = consonantData;
            controller = parentController;
            isVisited = isExplored;

            if (data != null)
            {
                if (letterText != null) letterText.text = data.letter.ToUpper();
            }

            SetDimmed(isVisited);
        }

        public void SetDimmed(bool dimmed)
        {
            isVisited = dimmed;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = dimmed ? dimmedAlpha : 1.0f;
            }
        }

        public void ResetCard()
        {
            isVisited = false;
            SetDimmed(false);
            transform.localScale = (initialScale != Vector3.zero) ? initialScale : Vector3.one;
            transform.localRotation = initialRotation;
        }

        private void OnCardClicked()
        {
            if (controller != null && controller.IsTransitioning) return;

            isVisited = true;
            SetDimmed(true);
            PlayWiggle();

            if (controller != null)
            {
                controller.OnConsonantCardTapped(this);
            }
        }

        public void PlayWiggle()
        {
            if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = StartCoroutine(WiggleCoroutine());
        }

        private IEnumerator WiggleCoroutine()
        {
            if (initialScale == Vector3.zero) initialScale = transform.localScale;

            float elapsed = 0f;

            while (elapsed < wiggleDuration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / wiggleDuration;

                // Scale: pop up then return (MeetPhonics math)
                float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.25f;
                transform.localScale = initialScale * scaleFactor;

                // Rotation: tilt left then right then back (MeetPhonics math)
                float rotZ = Mathf.Sin(percent * Mathf.PI * 2f) * 10f;
                transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

                yield return null;
            }

            transform.localScale = initialScale;
            transform.localRotation = initialRotation;
            wiggleCoroutine = null;
        }
    }
}
