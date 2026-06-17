using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Beginners.ItsPlayTime.Reading
{
    /// <summary>
    /// Component representing a single command vertical row.
    /// Must be in its own file of the same name to be attachable in Unity.
    /// </summary>
    public class CommandItemView_ItsPlayTime_Reading : MonoBehaviour
    {
        public Image backgroundImage;
        public TMP_Text labelText;
        public Button tapButton;

        public Color normalColor = new Color(0f, 0f, 0f, 0f);
        public Color glowColor = new Color(1f, 0.9f, 0.2f, 1f);

        private Action _onTappedCallback;

        private void Awake()
        {
            if (tapButton != null)
            {
                tapButton.onClick.AddListener(OnTapped);
            }
        }

        public void Setup(string text, Action onTapped)
        {
            _onTappedCallback = onTapped;

            if (labelText != null)
                labelText.text = text;

            if (backgroundImage != null)
                backgroundImage.color = normalColor;
        }

        public void SetHighlight(bool active)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = active ? glowColor : normalColor;
            }
        }

        public void SetInteractable(bool value)
        {
            if (tapButton != null)
                tapButton.interactable = value;
        }

        private void OnTapped()
        {
            _onTappedCallback?.Invoke();
        }
    }
}