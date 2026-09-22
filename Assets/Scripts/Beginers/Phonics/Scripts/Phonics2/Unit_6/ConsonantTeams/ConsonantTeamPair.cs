using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit6
{
    public class ConsonantTeamPair : MonoBehaviour
    {
        [Header("Letter Buttons")]
        [SerializeField] private Button[] letterButtons;
        [SerializeField] private TMP_Text[] letterTexts;
        [SerializeField] private Image[] letterBackgrounds;

        [Header("Hand-Hold Link Visual")]
        [SerializeField] private GameObject linkGlowObject;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color teamHighlightColor = new Color(1f, 0.85f, 0.3f, 1f); // Vibrant Gold
        [SerializeField] private Color normalLetterColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private ConsonantTeamsController controller;
        private TeamHuntWordItem currentHuntWord;

        public void SetupWord(TeamHuntWordItem item, ConsonantTeamsController parentController)
        {
            currentHuntWord = item;
            controller = parentController;

            if (linkGlowObject != null) linkGlowObject.SetActive(false);

            string word = item.fullWord;
            for (int i = 0; i < letterButtons.Length; i++)
            {
                if (i < word.Length && letterButtons[i] != null)
                {
                    letterButtons[i].gameObject.SetActive(true);
                    if (letterTexts != null && i < letterTexts.Length && letterTexts[i] != null)
                    {
                        letterTexts[i].text = word[i].ToString();
                    }
                    if (letterBackgrounds != null && i < letterBackgrounds.Length && letterBackgrounds[i] != null)
                    {
                        letterBackgrounds[i].color = defaultColor;
                    }

                    int charIndex = i;
                    letterButtons[i].onClick.RemoveAllListeners();
                    letterButtons[i].onClick.AddListener(() => OnLetterClicked(charIndex));
                }
                else if (letterButtons[i] != null)
                {
                    letterButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnLetterClicked(int index)
        {
            if (controller == null || controller.IsTransitioning || currentHuntWord == null) return;

            // Check if user clicked on one of the team letters
            bool isPartOfTeam = (index >= currentHuntWord.teamStartIndex && index < currentHuntWord.teamStartIndex + currentHuntWord.teamLetters.Length);

            if (isPartOfTeam)
            {
                controller.EvaluateTeamHuntTap(this, currentHuntWord);
            }
            else
            {
                // Wrong letter tapped
                if (letterBackgrounds != null && index < letterBackgrounds.Length && letterBackgrounds[index] != null)
                {
                    StartCoroutine(FlashLetterColor(letterBackgrounds[index], new Color(1f, 0.5f, 0.5f, 1f), 0.4f));
                }
            }
        }

        private readonly Color correctGreenColor = new Color(0.298f, 0.686f, 0.314f, 1f); // #4CAF50
        private readonly Color wrongRedColor = new Color(0.937f, 0.325f, 0.314f, 1f);     // #EF5350

        public void PlayHandHoldLinkAnimation()
        {
            if (currentHuntWord == null) return;

            if (linkGlowObject != null)
            {
                linkGlowObject.SetActive(true);
            }

            int start = currentHuntWord.teamStartIndex;
            int len = currentHuntWord.teamLetters.Length;

            for (int i = start; i < start + len; i++)
            {
                if (letterBackgrounds != null && i < letterBackgrounds.Length && letterBackgrounds[i] != null)
                {
                    letterBackgrounds[i].color = correctGreenColor;
                    StartCoroutine(PopScale(letterBackgrounds[i].transform, 1.25f, 0.35f));
                }
            }
        }

        private IEnumerator FlashLetterColor(Image img, Color flashColor, float duration)
        {
            if (img == null) yield break;
            Color orig = img.color;
            img.color = flashColor;
            RectTransform rt = img.GetComponent<RectTransform>();
            if (rt != null) StartCoroutine(ShakeAnimation(rt));

            yield return new WaitForSeconds(duration);
            img.color = orig;
        }

        private IEnumerator ShakeAnimation(RectTransform target)
        {
            if (target == null) yield break;
            Vector3 startPos = target.localPosition;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin(elapsed * 40f) * 10f * (1f - elapsed / duration);
                target.localPosition = startPos + new Vector3(offset, 0f, 0f);
                yield return null;
            }
            target.localPosition = startPos;
        }

        private IEnumerator PopScale(Transform tr, float scaleMult, float dur)
        {
            Vector3 orig = tr.localScale;
            Vector3 target = orig * scaleMult;
            float elapsed = 0f;
            float half = dur * 0.5f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                tr.localScale = Vector3.Lerp(orig, target, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                tr.localScale = Vector3.Lerp(target, orig, elapsed / half);
                yield return null;
            }

            tr.localScale = orig;
        }
    }
}
