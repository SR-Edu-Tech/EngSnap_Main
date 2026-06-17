using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Beginners.ActItOut
{
    [RequireComponent(typeof(JuicyButton))]
    public class ActItOut_IllustrationCard : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Refs")]
        [SerializeField] private Image illustrationImage;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image matchIndicator;      // glowing ring — hide by default
        [SerializeField] private Image checkmarkOverlay;    // shown on correct match

        [Header("Colors")]
        [SerializeField] private Color idleColor    = Color.white;
        [SerializeField] private Color hoverColor   = new Color(0.85f, 0.95f, 1f,  1f);
        [SerializeField] private Color matchedColor = new Color(0.65f, 1f,   0.68f,1f);
        [SerializeField] private Color wrongColor   = new Color(1f,    0.62f, 0.62f,1f);

        [Header("Pulsing Animation")]
        [Tooltip("Optional child transform containing visuals to pulse. Prevents conflict with JuicyButton scale animations.")]
        public Transform pulseContainer;
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.04f;

        // Runtime
        public int  CorrectPairIndex { get; private set; }
        public bool IsMatched        { get; private set; }

        private Action<ActItOut_IllustrationCard> _onDropped;
        private JuicyButton _juicy;
        private Vector3 _originalScale;

        private float _pulseTimer;
        private bool _isHovered;

        void Awake()
        {
            _juicy = GetComponent<JuicyButton>();
            if (matchIndicator   != null) matchIndicator.gameObject.SetActive(false);
            if (checkmarkOverlay != null) checkmarkOverlay.gameObject.SetActive(false);
            _originalScale = pulseContainer != null ? pulseContainer.localScale : transform.localScale;
            // Generate a random offset so cards pulse out of sync
            _pulseTimer = UnityEngine.Random.Range(0f, 100f);
        }

        // ── Setup ────────────────────────────────────────────────────────────────

        public void Initialise(int correctPairIndex, Sprite sprite, Action<ActItOut_IllustrationCard> onDropped)
        {
            CorrectPairIndex = correctPairIndex;
            if (illustrationImage != null) illustrationImage.sprite = sprite;
            _onDropped = onDropped;
            SetBg(idleColor);
        }

        void Update()
        {
            if (!IsMatched && !_isHovered)
            {
                _pulseTimer += Time.deltaTime;
                float scaleOffset = Mathf.Sin(_pulseTimer * pulseSpeed) * pulseAmount;
                Vector3 targetScale = _originalScale * (1f + scaleOffset);
                if (pulseContainer != null)
                {
                    pulseContainer.localScale = targetScale;
                }
                else
                {
                    transform.localScale = targetScale;
                }
            }
            else
            {
                if (pulseContainer != null && pulseContainer.localScale != _originalScale)
                {
                    pulseContainer.localScale = _originalScale;
                }
            }
        }

        // ── Drop target ──────────────────────────────────────────────────────────

        public void OnDrop(PointerEventData eventData)
        {
            if (IsMatched) return;
            _onDropped?.Invoke(this);
        }

        // Highlight card while a drag hovers over it
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsMatched) return;
            _isHovered = true;
            // Only highlight if something is being dragged
            if (eventData.pointerDrag != null)
                SetBg(hoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            if (!IsMatched) SetBg(idleColor);
        }

        // ── State setters ────────────────────────────────────────────────────────

        public void SetMatched()
        {
            IsMatched = true;
            SetBg(matchedColor);
            _juicy.PlayCorrectAnim();
            _juicy.SetDisabled(true);

            if (matchIndicator   != null) matchIndicator.gameObject.SetActive(true);
            if (checkmarkOverlay != null) checkmarkOverlay.gameObject.SetActive(true);

            if (pulseContainer != null)
            {
                pulseContainer.localScale = _originalScale;
            }

            VFXManager.Instance?.SpawnCorrectBurst(GetComponent<RectTransform>());
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxLineSnap);
        }

        public void SetWrong()
        {
            _juicy.PlayWrongAnim();
            SetBg(wrongColor);
            Invoke(nameof(ResetToIdle), 0.6f);
            VFXManager.Instance?.SpawnWrongPuff(GetComponent<RectTransform>());
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void ResetToIdle() => SetBg(idleColor);

        private void SetBg(Color c)
        {
            if (cardBackground != null) cardBackground.color = c;
        }
    }
}
