using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Unit4
{
    public class VowelOrConsonantBasket : MonoBehaviour, IDropHandler
    {
        public enum BasketType { Vowel, Consonant }

        [Header("Basket Settings")]
        [SerializeField] private BasketType basketType = BasketType.Vowel;
        [SerializeField] private TMP_Text basketLabel;
        [SerializeField] private GameObject hintStrip;

        private RectTransform rectTransform;

        public BasketType Type => basketType;
        public RectTransform Rect => rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                VowelOrConsonantTile tile = eventData.pointerDrag.GetComponent<VowelOrConsonantTile>();
                if (tile != null)
                {
                    tile.OnDroppedOnBasket(this);
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

        public bool ContainsPosition(Vector2 screenPoint, Camera cam)
        {
            if (rectTransform == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, cam);
        }
    }
}
