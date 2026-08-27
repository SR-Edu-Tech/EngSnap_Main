using System;
using UnityEngine;
using TMPro;

namespace EngSnap.Common
{
    /// <summary>
    /// Separate component and utility class that monitors and controls dialogue box activation.
    /// If dialogue text is empty or whitespace, deactivates the dialogue box area.
    /// When dialogue text is used again with non-empty content, reactivates the dialogue box area.
    /// Works across Phonics 1 and Phonics 2.
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueBoxAutoHider : MonoBehaviour
    {
        [Header("Target UI Components")]
        [Tooltip("The TMP_Text component displaying dialogue text.")]
        [SerializeField] private TMP_Text dialogueText;

        [Tooltip("Optional CanvasGroup for fading/showing/hiding dialogue box.")]
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Tooltip("Optional root dialogue box area GameObject to activate/deactivate.")]
        [SerializeField] private GameObject dialogueBoxArea;

        private string lastText = null;

        private void Awake()
        {
            if (dialogueText == null)
            {
                dialogueText = GetComponent<TMP_Text>();
                if (dialogueText == null)
                {
                    dialogueText = GetComponentInChildren<TMP_Text>();
                }
            }

            if (dialogueCanvasGroup == null)
            {
                dialogueCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (dialogueBoxArea == null && dialogueCanvasGroup != null)
            {
                dialogueBoxArea = dialogueCanvasGroup.gameObject;
            }
        }

        private void Start()
        {
            CheckAndUpdateState();
        }

        private void LateUpdate()
        {
            if (dialogueText != null && dialogueText.text != lastText)
            {
                CheckAndUpdateState();
            }
        }

        /// <summary>
        /// Manually force a check and update of the dialogue box active state based on dialogueText.
        /// </summary>
        public void CheckAndUpdateState()
        {
            string currentText = dialogueText != null ? dialogueText.text : string.Empty;
            lastText = currentText;
            ApplyState(dialogueText, currentText, dialogueCanvasGroup, dialogueBoxArea);
        }

        /// <summary>
        /// Static helper method to set dialogue text and automatically update dialogue box active state.
        /// If message is empty/whitespace, deactivates dialogue box area. If non-empty, reactivates it.
        /// </summary>
        public static void SetDialogue(TMP_Text textComponent, string message, CanvasGroup canvasGroup = null, GameObject dialogueBoxArea = null)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(message);
            string formattedText = isEmpty ? string.Empty : message;

            if (textComponent != null)
            {
                textComponent.text = formattedText;
            }

            ApplyState(textComponent, formattedText, canvasGroup, dialogueBoxArea);
        }

        /// <summary>
        /// Helper method to apply active/inactive state to canvasGroup and dialogueBoxArea based on text emptiness.
        /// </summary>
        public static void ApplyState(TMP_Text textComponent, string text, CanvasGroup canvasGroup = null, GameObject dialogueBoxArea = null)
        {
            bool isEmpty = string.IsNullOrWhiteSpace(text);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = isEmpty ? 0f : 1f;
                canvasGroup.interactable = !isEmpty;
                canvasGroup.blocksRaycasts = !isEmpty;

                if (canvasGroup.gameObject != null)
                {
                    canvasGroup.gameObject.SetActive(!isEmpty);
                }
            }

            if (dialogueBoxArea != null)
            {
                dialogueBoxArea.SetActive(!isEmpty);
            }
            else if (canvasGroup == null && textComponent != null && textComponent.transform.parent != null)
            {
                // Deactivate parent container if no explicit canvasGroup or dialogueBoxArea is assigned
                textComponent.transform.parent.gameObject.SetActive(!isEmpty);
            }
        }
    }
}
