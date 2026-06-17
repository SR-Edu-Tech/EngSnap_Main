using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Beginners.ItsPlayTime.Reading
{
    /// <summary>
    /// Component representing a single tile in the playground gallery 3x3 grid.
    /// Must be in its own file of the same name to be attachable in Unity.
    /// </summary>
    public class PlaygroundTileView_ItsPlayTime_Reading : MonoBehaviour
    {
        [Header("UI References")]
        public Image backgroundImage;
        public Image illustrationImage;
        public TMP_Text labelText;
        public Button tapButton;

        [Header("Colors")]
        public Color normalColor = Color.white;
        public Color glowColor = new Color(1f, 0.9f, 0.2f, 1f); // soft kid-friendly yellow

        private Action _onTappedCallback;

        private void Awake()
        {
            if (tapButton != null)
            {
                tapButton.onClick.AddListener(OnTapped);
            }
        }

        public void Setup(PlaygroundTileData data, Action onTapped)
        {
            _onTappedCallback = onTapped;

            if (illustrationImage != null)
                illustrationImage.sprite = data.illustration;

            if (labelText != null)
                labelText.text = data.itemName;

            if (backgroundImage != null)
                backgroundImage.color = normalColor;
        }

        public void SetInteractable(bool value)
        {
            if (tapButton != null)
                tapButton.interactable = value;
        }

        public void StartGlow()
        {
            if (backgroundImage != null)
                backgroundImage.color = glowColor;
        }

        public void StopGlow()
        {
            if (backgroundImage != null)
                backgroundImage.color = normalColor;
        }

        public IEnumerator GentleBounce(float duration)
        {
            Vector3 originalScale = transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offset = 0.15f * Mathf.Sin((elapsed / duration) * Mathf.PI);
                transform.localScale = originalScale * (1f + offset);
                yield return null;
            }
            transform.localScale = originalScale;
        }

        private void OnTapped()
        {
            _onTappedCallback?.Invoke();
        }
    }
}