using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Unit5
{
    public class SoundSortBucket : MonoBehaviour, IDropHandler
    {
        [Header("Bucket Config")]
        [Tooltip("The vowel letter for this bucket e.g. a, e, i, o, u.")]
        [SerializeField] private string vowelKey = "a";
        [SerializeField] private TMP_Text bucketLabelText;

        private RectTransform rectTransform;

        public string VowelKey => vowelKey.ToLowerInvariant();
        public RectTransform Rect => rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            UpdateLabel();
        }

        public void SetVowelKey(string key)
        {
            vowelKey = key;
            UpdateLabel();
        }

        private void UpdateLabel()
        {
            if (bucketLabelText != null && !string.IsNullOrEmpty(vowelKey))
            {
                bucketLabelText.text = vowelKey.ToUpper();
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                SoundSortCard card = eventData.pointerDrag.GetComponent<SoundSortCard>();
                if (card != null)
                {
                    card.OnDroppedOnBucket(this);
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
