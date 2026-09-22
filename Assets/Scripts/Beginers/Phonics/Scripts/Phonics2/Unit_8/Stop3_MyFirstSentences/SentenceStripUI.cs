using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit8
{
    /// <summary>
    /// Interactive Sentence Strip component.
    /// Displays sentence word-by-word with tap states, noun picture badges,
    /// and left-to-right flowing read-along highlight animations.
    /// </summary>
    public class SentenceStripUI : MonoBehaviour
    {
        [Header("Word Slot Elements (Up to 10 Words)")]
        [SerializeField] private GameObject[] wordContainers = new GameObject[10];
        [SerializeField] private Button[] wordButtons = new Button[10];
        [SerializeField] private TMP_Text[] wordTexts = new TMP_Text[10];
        [SerializeField] private Image[] wordHighlightGlows = new Image[10];
        [SerializeField] private Image[] nounPictureImages = new Image[10];

        // Events
        public event Action<int, SentenceWordToken> OnWordTapped;

        private SentenceItem currentSentence;
        private int activeWordCount = 0;

        private void Awake()
        {
            SetupButtonListeners();
        }

        private void SetupButtonListeners()
        {
            for (int i = 0; i < wordButtons.Length; i++)
            {
                int index = i;
                if (wordButtons[i] != null)
                {
                    wordButtons[i].onClick.RemoveAllListeners();
                    wordButtons[i].onClick.AddListener(() => OnWordClicked(index));
                }
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void DisplaySentence(SentenceItem item, bool showNounPictures = false, bool progressiveReveal = true)
        {
            currentSentence = item;
            if (item == null || item.tokens == null)
            {
                ClearSentence();
                return;
            }

            activeWordCount = Mathf.Min(item.tokens.Length, wordContainers.Length);

            for (int i = 0; i < wordContainers.Length; i++)
            {
                if (wordContainers[i] != null)
                {
                    bool inSentence = i < activeWordCount;
                    bool showInitially = inSentence && (!progressiveReveal || i == 0);
                    wordContainers[i].SetActive(showInitially);

                    if (inSentence)
                    {
                        var token = item.tokens[i];
                        if (wordTexts != null && i < wordTexts.Length && wordTexts[i] != null)
                        {
                            wordTexts[i].text = token.wordText;
                        }

                        if (wordHighlightGlows != null && i < wordHighlightGlows.Length && wordHighlightGlows[i] != null)
                        {
                            wordHighlightGlows[i].gameObject.SetActive(i == 0 && progressiveReveal);
                        }

                        if (wordButtons != null && i < wordButtons.Length && wordButtons[i] != null)
                        {
                            wordButtons[i].interactable = !progressiveReveal || i == 0;
                        }

                        if (nounPictureImages != null && i < nounPictureImages.Length && nounPictureImages[i] != null)
                        {
                            bool hasNoun = showNounPictures && token.isNounWithPicture && token.nounSprite != null;
                            nounPictureImages[i].gameObject.SetActive(hasNoun);
                            if (hasNoun) nounPictureImages[i].sprite = token.nounSprite;
                        }
                    }
                }
            }
        }

        public void RevealWord(int index)
        {
            if (index >= 0 && index < activeWordCount)
            {
                if (wordContainers != null && index < wordContainers.Length && wordContainers[index] != null)
                {
                    wordContainers[index].SetActive(true);
                    StartCoroutine(PopScaleRoutine(wordContainers[index].transform, 1.15f, 0.25f));
                }

                if (wordButtons != null && index < wordButtons.Length && wordButtons[index] != null)
                {
                    wordButtons[index].interactable = true;
                }

                SetWordHighlight(index, true);
            }
        }

        public void RevealAllNounPictures()
        {
            if (currentSentence == null || currentSentence.tokens == null) return;
            for (int i = 0; i < activeWordCount; i++)
            {
                var token = currentSentence.tokens[i];
                if (token.isNounWithPicture && nounPictureImages != null && i < nounPictureImages.Length && nounPictureImages[i] != null)
                {
                    nounPictureImages[i].gameObject.SetActive(true);
                    if (token.nounSprite != null) nounPictureImages[i].sprite = token.nounSprite;
                    StartCoroutine(PopScaleRoutine(nounPictureImages[i].transform, 1.25f, 0.3f));
                }
            }
        }

        public void SetWordHighlight(int index, bool highlight)
        {
            if (wordHighlightGlows != null && index >= 0 && index < wordHighlightGlows.Length && wordHighlightGlows[index] != null)
            {
                wordHighlightGlows[index].gameObject.SetActive(highlight);
            }
        }

        public void ClearHighlights()
        {
            if (wordHighlightGlows == null) return;
            for (int i = 0; i < wordHighlightGlows.Length; i++)
            {
                if (wordHighlightGlows[i] != null)
                {
                    wordHighlightGlows[i].gameObject.SetActive(false);
                }
            }
        }

        public IEnumerator AnimateReadAlongHighlight(float wordDuration = 0.5f)
        {
            ClearHighlights();
            for (int i = 0; i < activeWordCount; i++)
            {
                SetWordHighlight(i, true);
                if (wordButtons != null && i < wordButtons.Length && wordButtons[i] != null)
                {
                    StartCoroutine(PopScaleRoutine(wordButtons[i].transform, 1.15f, wordDuration));
                }
                yield return new WaitForSeconds(wordDuration);
                SetWordHighlight(i, false);
            }
        }

        public void ClearSentence()
        {
            activeWordCount = 0;
            currentSentence = null;
            for (int i = 0; i < wordContainers.Length; i++)
            {
                if (wordContainers[i] != null) wordContainers[i].SetActive(false);
            }
        }

        private void OnWordClicked(int index)
        {
            if (currentSentence == null || currentSentence.tokens == null || index < 0 || index >= currentSentence.tokens.Length)
                return;

            SetWordHighlight(index, true);
            if (wordButtons != null && index < wordButtons.Length && wordButtons[index] != null)
            {
                StartCoroutine(PopScaleRoutine(wordButtons[index].transform, 1.2f, 0.2f));
            }

            OnWordTapped?.Invoke(index, currentSentence.tokens[index]);
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
