using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit5
{
    public class VowelTeamPair : MonoBehaviour, IPointerClickHandler
    {
        [Header("Text Displays")]
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private TMP_Text teamHighlightText; // Displays "ee", "ea", "oa", "ai" etc.

        [Header("Optional Graphic Overlay")]
        [SerializeField] private Image handHoldLinkGraphic; // Optional visual graphic overlay

        [Header("Highlight Styling")]
        [SerializeField] private Color teamHighlightColor = new Color(1f, 0.82f, 0.1f); // Vibrant Gold

        private VowelTeamsController controller;
        private VowelTeamSpottingWord currentSpottingWord;
        private bool isLinked = false;

        public bool IsLinked => isLinked;

        public void SetupWord(VowelTeamSpottingWord spottingWord, VowelTeamsController mainController)
        {
            currentSpottingWord = spottingWord;
            controller = mainController;
            isLinked = false;

            if (wordText != null && spottingWord != null)
            {
                wordText.text = spottingWord.wordText;
            }
            if (teamHighlightText != null)
            {
                if (spottingWord != null && !string.IsNullOrEmpty(spottingWord.correctTeamLetters))
                {
                    teamHighlightText.text = spottingWord.correctTeamLetters;
                }
                teamHighlightText.gameObject.SetActive(false);
            }
            if (handHoldLinkGraphic != null)
            {
                handHoldLinkGraphic.gameObject.SetActive(false);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (controller != null && !controller.IsTransitioning && !isLinked)
            {
                controller.EvaluateTeamSpottingTap(this, currentSpottingWord);
            }
        }

        public void PlayHandHoldLinkAnimation()
        {
            isLinked = true;

            // Highlight the vowel team within the main word text
            if (wordText != null && currentSpottingWord != null)
            {
                string word = currentSpottingWord.wordText;
                string team = currentSpottingWord.correctTeamLetters;
                int start = currentSpottingWord.teamStartIndex;
                int len = currentSpottingWord.teamLength > 0 ? currentSpottingWord.teamLength : (team != null ? team.Length : 2);

                if (start >= 0 && start + len <= word.Length)
                {
                    string colorHex = ColorUtility.ToHtmlStringRGB(teamHighlightColor);
                    string highlighted = word.Substring(0, start) + $"<color=#{colorHex}><b>{word.Substring(start, len)}</b></color>" + word.Substring(start + len);
                    wordText.text = highlighted;
                }
            }

            // Show and animate the vowel team text badge (ee, ea, oa, etc.)
            if (teamHighlightText != null)
            {
                if (currentSpottingWord != null && !string.IsNullOrEmpty(currentSpottingWord.correctTeamLetters))
                {
                    teamHighlightText.text = currentSpottingWord.correctTeamLetters;
                }
                teamHighlightText.gameObject.SetActive(true);
                StartCoroutine(PulseAnimation(teamHighlightText.transform));
            }

            if (handHoldLinkGraphic != null)
            {
                handHoldLinkGraphic.gameObject.SetActive(true);
                StartCoroutine(PulseAnimation(handHoldLinkGraphic.transform));
            }
        }

        private IEnumerator PulseAnimation(Transform target)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            float duration = 0.35f;
            Vector3 origScale = target.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + Mathf.Sin(elapsed * 20f) * 0.25f;
                target.localScale = origScale * scale;
                yield return null;
            }
            target.localScale = origScale;
        }
    }
}
