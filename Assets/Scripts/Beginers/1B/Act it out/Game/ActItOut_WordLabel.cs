using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Beginners.ActItOut
{
    [RequireComponent(typeof(JuicyButton))]
    public class ActItOut_WordLabel : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("Refs")]
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Image    backgroundImage;

        [Header("Visual States")]
        [SerializeField] private Color idleColor     = new Color(1f,  0.97f, 0.85f, 1f);
        [SerializeField] private Color draggingColor = new Color(1f,  0.85f, 0.35f, 1f);
        [SerializeField] private Color matchedColor  = new Color(0.6f, 1f,  0.65f, 1f);

        [Header("Pulsing Animation")]
        [Tooltip("Optional child transform containing visuals to pulse. Prevents conflict with JuicyButton scale animations.")]
        public Transform pulseContainer;
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.04f;

        public int  PairIndex  { get; private set; }
        public bool IsMatched  { get; private set; }
        public bool IsDragging { get; private set; }

        private Action<ActItOut_WordLabel>                    _onDragBegin;
        private Action<ActItOut_WordLabel, Vector2>           _onDragging;
        private Action<ActItOut_WordLabel, PointerEventData>  _onDragEnd;
        private JuicyButton _juicy;
        private Vector3 _originalScale;

        private float _pulseTimer;

        void Awake()
        {
            _juicy = GetComponent<JuicyButton>();
            _originalScale = pulseContainer != null ? pulseContainer.localScale : transform.localScale;
            // Generate a random offset so all cards don't pulse in exact synchronization
            _pulseTimer = UnityEngine.Random.Range(0f, 100f);
        }

        public void Initialise(int pairIndex, string word, AudioClip wordClip,
                               Action<ActItOut_WordLabel> onDragBegin,
                               Action<ActItOut_WordLabel, Vector2> onDragging,
                               Action<ActItOut_WordLabel, PointerEventData> onDragEnd)
        {
            PairIndex      = pairIndex;
            // Display sentence text as is or uppercase (we can uppercase or keep original case.
            // MatchingGameController uses toUpper for word but these are sentence cards: 'I am eating.')
            // Let's keep original sentence capitalization but clean it if needed.
            labelText.text = word; 
            _onDragBegin   = onDragBegin;
            _onDragging    = onDragging;
            _onDragEnd     = onDragEnd;
            SetBg(idleColor);
        }

        void Update()
        {
            if (!IsMatched && !IsDragging)
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
                    // Fallback to transform scale only if no container is assigned
                    transform.localScale = targetScale;
                }
            }
            else
            {
                // Reset scale when matched or dragging to avoid stuck scale
                if (pulseContainer != null && pulseContainer.localScale != _originalScale)
                {
                    pulseContainer.localScale = _originalScale;
                }
            }
        }

        // IPointerDownHandler — needed so EventSystem tracks this object for drag
        public void OnPointerDown(PointerEventData eventData) { }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsMatched) return;
            IsDragging = true;
            SetBg(draggingColor);
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
            _onDragBegin?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            _onDragging?.Invoke(this, eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging) return;
            IsDragging = false;
            if (!IsMatched) SetBg(idleColor);
            _onDragEnd?.Invoke(this, eventData);
        }

        public void SetMatched()
        {
            IsMatched = true;
            SetBg(matchedColor);
            _juicy.PlayCorrectAnim();
            _juicy.SetDisabled(true);

            // Restore scale of pulseContainer immediately
            if (pulseContainer != null)
            {
                pulseContainer.localScale = _originalScale;
            }
        }

        public void SetWrong()
        {
            _juicy.PlayWrongAnim();
            SetBg(idleColor);
        }

        private void SetBg(Color c)
        {
            if (backgroundImage != null) backgroundImage.color = c;
        }
    }
}
