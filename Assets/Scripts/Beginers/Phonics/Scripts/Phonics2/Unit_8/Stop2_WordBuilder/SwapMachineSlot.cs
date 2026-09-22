using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit8
{
    /// <summary>
    /// Interactive revolving drum slot for the 3-slot Swap Machine.
    /// Supports cycling letters up/down with animated vertical rolling.
    /// </summary>
    public class SwapMachineSlot : MonoBehaviour
    {
        [Header("Slot UI Elements")]
        [SerializeField] private TMP_Text letterText;
        [SerializeField] private Button spinUpButton;
        [SerializeField] private Button spinDownButton;
        [SerializeField] private RectTransform drumRect;
        [SerializeField] private GameObject activeGlow;

        // Events
        public event Action<string> OnLetterChanged;

        private string[] availableLetters = new string[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "r", "s", "t", "u", "w" };
        private int currentIndex = 0;
        private bool isLocked = false;
        private bool isSpinning = false;

        public string CurrentLetter => (availableLetters != null && currentIndex >= 0 && currentIndex < availableLetters.Length) ? availableLetters[currentIndex] : "";

        private void Awake()
        {
            if (spinUpButton != null)
            {
                spinUpButton.onClick.RemoveAllListeners();
                spinUpButton.onClick.AddListener(() => Spin(1));
            }
            if (spinDownButton != null)
            {
                spinDownButton.onClick.RemoveAllListeners();
                spinDownButton.onClick.AddListener(() => Spin(-1));
            }
        }

        public void SetupSlot(string[] letters, string initialLetter, bool locked = false)
        {
            if (letters != null && letters.Length > 0)
            {
                availableLetters = letters;
            }

            int foundIdx = Array.FindIndex(availableLetters, l => string.Equals(l, initialLetter, StringComparison.OrdinalIgnoreCase));
            currentIndex = (foundIdx >= 0) ? foundIdx : 0;

            SetLocked(locked);
            UpdateVisuals();
        }

        public void SetLetterDirectly(string letter)
        {
            int foundIdx = Array.FindIndex(availableLetters, l => string.Equals(l, letter, StringComparison.OrdinalIgnoreCase));
            if (foundIdx >= 0)
            {
                currentIndex = foundIdx;
                UpdateVisuals();
            }
            else
            {
                if (letterText != null) letterText.text = letter;
            }
        }

        public void SetLocked(bool locked)
        {
            isLocked = locked;
            if (spinUpButton != null) spinUpButton.interactable = !locked;
            if (spinDownButton != null) spinDownButton.interactable = !locked;
            if (activeGlow != null) activeGlow.SetActive(!locked);
        }

        public void Spin(int direction)
        {
            if (isLocked || isSpinning || availableLetters == null || availableLetters.Length == 0) return;

            currentIndex = (currentIndex + direction + availableLetters.Length) % availableLetters.Length;
            StartCoroutine(AnimateSpinRoutine(direction));
        }

        private IEnumerator AnimateSpinRoutine(int direction)
        {
            isSpinning = true;
            if (drumRect != null)
            {
                float duration = 0.15f;
                float elapsed = 0f;
                Vector3 originalPos = drumRect.localPosition;
                float offset = direction * 25f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    drumRect.localPosition = originalPos + new Vector3(0f, Mathf.Sin(t * Mathf.PI) * offset, 0f);
                    yield return null;
                }
                drumRect.localPosition = originalPos;
            }

            UpdateVisuals();
            isSpinning = false;
            OnLetterChanged?.Invoke(CurrentLetter);
        }

        private void UpdateVisuals()
        {
            if (letterText != null && availableLetters != null && currentIndex >= 0 && currentIndex < availableLetters.Length)
            {
                letterText.text = availableLetters[currentIndex];
            }
        }
    }
}
