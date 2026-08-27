using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit5
{
    public class VowelTeamPair : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private Image handHoldLinkGraphic; // Visual overlay showing hand-hold link between vowels

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
            if (handHoldLinkGraphic != null)
            {
                handHoldLinkGraphic.gameObject.SetActive(true);
                StartCoroutine(LinkPulseAnimation());
            }
        }

        private IEnumerator LinkPulseAnimation()
        {
            float elapsed = 0f;
            float duration = 0.35f;
            Vector3 origScale = handHoldLinkGraphic.transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + Mathf.Sin(elapsed * 20f) * 0.2f;
                handHoldLinkGraphic.transform.localScale = origScale * scale;
                yield return null;
            }
            handHoldLinkGraphic.transform.localScale = origScale;
        }
    }
}
