using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Unit4
{
    public class CatchTheVowelTile : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text letterText;
        [SerializeField] private Image tileImage;
        [SerializeField] private GameObject starParticleFx;
        [SerializeField] private GameObject tickMarkImage;
        [SerializeField] private GameObject glowOutlineImage;

        private CatchTheVowelData.LetterTileItem dataItem;
        private CatchTheVowelController controller;
        private Button button;
        private RectTransform rectTransform;
        private bool isCaught = false;

        public CatchTheVowelData.LetterTileItem DataItem => dataItem;
        public bool IsCaught => isCaught;
        public bool IsVowel => (dataItem != null) && dataItem.isVowel;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();
        }

        private void Start()
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnTileClicked);
        }

        public void Setup(CatchTheVowelData.LetterTileItem item, CatchTheVowelController parentController)
        {
            dataItem = item;
            controller = parentController;
            isCaught = false;

            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

            if (dataItem != null)
            {
                if (letterText != null) letterText.text = dataItem.letter.ToUpper();
                if (rectTransform != null && dataItem.localPosition != Vector2.zero)
                {
                    rectTransform.anchoredPosition = dataItem.localPosition;
                }
            }

            if (starParticleFx != null) starParticleFx.SetActive(false);
            if (tickMarkImage != null) tickMarkImage.SetActive(false);
            if (glowOutlineImage != null) glowOutlineImage.SetActive(false);

            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        public void ResetTile()
        {
            isCaught = false;
            if (starParticleFx != null) starParticleFx.SetActive(false);
            if (tickMarkImage != null) tickMarkImage.SetActive(false);
            if (glowOutlineImage != null) glowOutlineImage.SetActive(false);
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        private void OnTileClicked()
        {
            if (controller != null && controller.IsTransitioning) return;

            if (IsVowel)
            {
                if (!isCaught)
                {
                    StartCoroutine(VowelPopSequence());
                }
            }
            else
            {
                StartCoroutine(ConsonantWobbleSequence());
            }
        }

        private IEnumerator VowelPopSequence()
        {
            isCaught = true;

            if (starParticleFx != null) starParticleFx.SetActive(true);
            if (tickMarkImage != null) tickMarkImage.SetActive(true);
            if (glowOutlineImage != null) glowOutlineImage.SetActive(true);

            // Pop scale & glow animation
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(originalScale, originalScale * 1.35f, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(originalScale * 1.35f, originalScale * 1.15f, elapsed / duration);
                yield return null;
            }

            if (controller != null)
            {
                controller.OnVowelCaught(this);
            }
        }

        private IEnumerator ConsonantWobbleSequence()
        {
            if (rectTransform == null) yield break;

            Vector3 origPos = rectTransform.anchoredPosition;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 45f) * 8f;
                rectTransform.anchoredPosition = origPos + new Vector3(shake, 0, 0);
                yield return null;
            }

            rectTransform.anchoredPosition = origPos;

            if (controller != null)
            {
                controller.OnConsonantTapped(this);
            }
        }
    }
}
