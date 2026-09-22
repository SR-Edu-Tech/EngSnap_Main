using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit5
{
    public class MagicEWordCard : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI Components")]
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private Image wordImage;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Button cardButton;
        [SerializeField] private GameObject highlightGlow;
        [SerializeField] private GameObject sparkleParticles;

        [Header("Vowel Color Palette")]
        [SerializeField] private string vowelAColor = "#FF7043"; // Coral orange for long a
        [SerializeField] private string vowelIColor = "#29B6F6"; // Sky blue for long i
        [SerializeField] private string vowelUColor = "#AB47BC"; // Purple for long u
        [SerializeField] private string silentEColor = "#FFD54F"; // Gold for silent e

        private string rawWord = "";
        private AudioClip audioClip;
        private Action<MagicEWordCard> onClickCallback;
        private bool isInteractable = true;
        private Vector3 initialScale = Vector3.one;

        private void Awake()
        {
            initialScale = transform.localScale;
            if (cardButton != null)
            {
                cardButton.onClick.AddListener(OnCardButtonClicked);
            }
        }

        public void SetupCard(string word, Sprite sprite, AudioClip clip, Action<MagicEWordCard> onClick = null)
        {
            rawWord = word;
            audioClip = clip;
            onClickCallback = onClick;
            isInteractable = true;

            if (wordText != null)
            {
                string vColor = GetColorForVowel(word);
                wordText.text = MagicEData.FormatMagicEWord(word, vColor, silentEColor);
            }

            if (wordImage != null)
            {
                if (sprite != null)
                {
                    wordImage.sprite = sprite;
                    wordImage.gameObject.SetActive(true);
                }
                else
                {
                    wordImage.gameObject.SetActive(false);
                }
            }

            if (highlightGlow != null) highlightGlow.SetActive(false);
            if (sparkleParticles != null) sparkleParticles.SetActive(false);
        }

        public string RawWord => rawWord;
        public AudioClip AudioClip => audioClip;

        public void SetInteractable(bool active)
        {
            isInteractable = active;
            if (cardButton != null) cardButton.interactable = active;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable) return;
            TriggerCardAction();
        }

        private void OnCardButtonClicked()
        {
            if (!isInteractable) return;
            TriggerCardAction();
        }

        public void TriggerCardAction()
        {
            PlayBounceAnimation();
            if (sparkleParticles != null)
            {
                sparkleParticles.SetActive(false);
                sparkleParticles.SetActive(true);
            }
            onClickCallback?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isInteractable) return;
            transform.localScale = initialScale * 1.05f;
            if (highlightGlow != null) highlightGlow.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = initialScale;
            if (highlightGlow != null) highlightGlow.SetActive(false);
        }

        public void PlayBounceAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(BounceRoutine());
        }

        private IEnumerator BounceRoutine()
        {
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scaleMultiplier = 1f + Mathf.Sin(progress * Mathf.PI) * 0.18f;
                transform.localScale = initialScale * scaleMultiplier;
                yield return null;
            }

            transform.localScale = initialScale;
        }

        private string GetColorForVowel(string word)
        {
            if (string.IsNullOrEmpty(word)) return vowelAColor;
            string lower = word.ToLower();
            if (lower.Contains("i")) return vowelIColor;
            if (lower.Contains("u")) return vowelUColor;
            return vowelAColor;
        }
    }
}
