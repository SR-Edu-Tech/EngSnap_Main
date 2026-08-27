using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit4
{
    public class SortingHouseLetterbox : MonoBehaviour, IDropHandler
    {
        [Header("Letterbox Configuration")]
        [SerializeField] private int boxIndex = 0; // 0:ă, 1:ĕ, 2:ĭ, 3:ŏ, 4:ŭ, 5:Not today!
        [SerializeField] private string vowelLabel = "ă";
        [SerializeField] private TMP_Text boxLabelText;
        [SerializeField] private Image boxBackgroundImage;
        [SerializeField] private Image houseImage;
        [SerializeField] private Color boxVowelColor = Color.white;

        private RectTransform rectTransform;

        public int BoxIndex => boxIndex;
        public string VowelLabel => vowelLabel;
        public RectTransform Rect => rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            UpdateLabel();
        }

        public void SetupLetterbox(int index, string label, Color color)
        {
            boxIndex = index;
            vowelLabel = label;
            boxVowelColor = color;
            if (boxBackgroundImage != null) boxBackgroundImage.color = color;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (boxLabelText != null)
            {
                boxLabelText.text = vowelLabel;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                SortingHouseCard card = eventData.pointerDrag.GetComponent<SortingHouseCard>();
                if (card != null)
                {
                    card.OnDroppedOnLetterbox(this);
                }
            }
        }

        public void PlayDropBounceAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(BounceSequence());
        }

        private IEnumerator BounceSequence()
        {
            Vector3 origScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(origScale, origScale * 1.15f, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(origScale * 1.15f, origScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = origScale;
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera camera)
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null) return false;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : camera;
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, cam);
        }
    }
}
