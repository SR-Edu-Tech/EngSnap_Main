using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit8
{
    /// <summary>
    /// Interactive Sight Word Pocket UI on the companion character.
    /// Holds collected non-decodable sight word cards. Tapping any card plays its audio.
    /// Supports animated card entry when newly introduced.
    /// </summary>
    public class SightWordPocketUI : MonoBehaviour
    {
        [Header("Pocket UI Elements")]
        [SerializeField] private Button pocketOpenButton;
        [SerializeField] private GameObject cardsContainer;
        [SerializeField] private Button[] cardButtons = new Button[12];
        [SerializeField] private TMP_Text[] cardTexts = new TMP_Text[12];
        [SerializeField] private Image[] cardImages = new Image[12];

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip cardPopSfx;

        // Events
        public event Action<string> OnSightWordTapped;
        public event Action<int, string> OnCardIndexTapped;

        private List<SightWordCard> collectedWords = new List<SightWordCard>();

        public int CollectedWordCount => collectedWords.Count;
        public List<SightWordCard> CollectedWords => collectedWords;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (pocketOpenButton != null)
            {
                pocketOpenButton.onClick.RemoveAllListeners();
                pocketOpenButton.onClick.AddListener(TogglePocket);
            }

            SetupCardButtonListeners();
        }

        private void SetupCardButtonListeners()
        {
            for (int i = 0; i < cardButtons.Length; i++)
            {
                int index = i;
                if (cardButtons[i] != null)
                {
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardClicked(index));
                }
            }
        }

        public void ClearPocket()
        {
            collectedWords.Clear();
            UpdateVisuals();
        }

        public void AddWord(SightWordCard card)
        {
            if (card == null) return;
            if (!collectedWords.Exists(c => string.Equals(c.word, card.word, StringComparison.OrdinalIgnoreCase)))
            {
                collectedWords.Add(card);
                UpdateVisuals();
            }
        }

        public void AddWords(IEnumerable<SightWordCard> cards)
        {
            if (cards == null) return;
            foreach (var c in cards)
            {
                AddWord(c);
            }
        }

        public void TogglePocket()
        {
            if (cardsContainer != null)
            {
                bool active = !cardsContainer.activeSelf;
                cardsContainer.SetActive(active);
            }
        }

        public void SetPocketOpen(bool open)
        {
            if (cardsContainer != null)
            {
                cardsContainer.SetActive(open);
            }
        }

        public IEnumerator AnimateCardIntoPocket(SightWordCard card, Transform spawnOrigin = null)
        {
            AddWord(card);
            if (audioSource != null && cardPopSfx != null)
            {
                audioSource.PlayOneShot(cardPopSfx);
            }

            int idx = collectedWords.FindIndex(c => string.Equals(c.word, card.word, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0 && idx < cardButtons.Length && cardButtons[idx] != null)
            {
                Transform cardTransform = cardButtons[idx].transform;
                Vector3 originalScale = cardTransform.localScale;
                cardTransform.localScale = Vector3.zero;

                float elapsed = 0f;
                float duration = 0.35f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    cardTransform.localScale = Vector3.LerpUnclamped(Vector3.zero, originalScale, Mathf.Sin(t * Mathf.PI * 0.5f));
                    yield return null;
                }
                cardTransform.localScale = originalScale;
            }
        }

        private void OnCardClicked(int index)
        {
            if (index >= 0 && index < collectedWords.Count)
            {
                SightWordCard card = collectedWords[index];
                if (audioSource != null && card.wordAudioClip != null)
                {
                    audioSource.Stop();
                    audioSource.clip = card.wordAudioClip;
                    audioSource.Play();
                }
                else if (audioSource != null && cardPopSfx != null)
                {
                    audioSource.PlayOneShot(cardPopSfx);
                }

                if (cardButtons[index] != null)
                {
                    StartCoroutine(PopScaleRoutine(cardButtons[index].transform, 1.2f, 0.2f));
                }

                OnSightWordTapped?.Invoke(card.word);
                OnCardIndexTapped?.Invoke(index, card.word);
            }
        }

        public void HighlightCard(int index, bool highlight)
        {
            if (index >= 0 && index < cardButtons.Length && cardButtons[index] != null)
            {
                if (highlight)
                {
                    StartCoroutine(PopScaleRoutine(cardButtons[index].transform, 1.15f, 0.25f));
                }
            }
        }

        public void SetCardInteractable(int index, bool interactable)
        {
            if (index >= 0 && index < cardButtons.Length && cardButtons[index] != null)
            {
                cardButtons[index].interactable = interactable;
            }
        }

        public void SetAllCardsInteractable(bool interactable)
        {
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] != null)
                {
                    cardButtons[i].interactable = interactable;
                }
            }
        }

        private void UpdateVisuals()
        {
            for (int i = 0; i < cardButtons.Length; i++)
            {
                if (cardButtons[i] != null)
                {
                    bool active = i < collectedWords.Count;
                    cardButtons[i].gameObject.SetActive(active);

                    if (active && cardTexts != null && i < cardTexts.Length && cardTexts[i] != null)
                    {
                        cardTexts[i].text = collectedWords[i].word;
                    }
                }
            }
        }

        private IEnumerator PopScaleRoutine(Transform target, float maxScaleMul, float duration)
        {
            if (target == null) yield break;
            Vector3 startScale = target.localScale;
            Vector3 maxScale = startScale * maxScaleMul;
            float half = duration * 0.5f;

            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(startScale, maxScale, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(maxScale, startScale, elapsed / half);
                yield return null;
            }

            target.localScale = startScale;
        }
    }
}
