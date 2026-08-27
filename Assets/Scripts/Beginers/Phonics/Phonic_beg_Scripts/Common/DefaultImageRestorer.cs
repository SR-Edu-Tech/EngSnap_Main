using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Common
{
    /// <summary>
    /// Component attached to a UI GameObject with an Image component.
    /// Caches the initial default sprite at Awake and ensures that:
    /// 1. When a valid custom asset image (non-null sprite) is assigned, it displays that image.
    /// 2. When no asset image is available (sprite set to null or disabled), it automatically reverts to and keeps the default image.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class DefaultImageRestorer : MonoBehaviour
    {
        [Header("Target Image Component")]
        [Tooltip("The Image component to manage. If unassigned, automatically grabs from this GameObject.")]
        [SerializeField] private Image targetImage;

        [Header("Default Sprite")]
        [Tooltip("The default sprite to fall back to when no asset image is available. If unassigned, automatically captures the initial sprite from Target Image at Awake.")]
        [SerializeField] private Sprite defaultSprite;

        [Header("Auto-Protection")]
        [Tooltip("If true, automatically monitors targetImage.sprite in LateUpdate and restores defaultSprite whenever sprite becomes null or disabled.")]
        [SerializeField] private bool autoRestoreIfNull = true;

        private Sprite cachedInitialDefaultSprite;
        private Sprite lastProcessedSprite;
        private bool isInitialized = false;

        public Sprite DefaultSprite
        {
            get => defaultSprite != null ? defaultSprite : cachedInitialDefaultSprite;
            set
            {
                defaultSprite = value;
                cachedInitialDefaultSprite = value;
            }
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        private void Start()
        {
            InitializeIfNeeded();
        }

        private void OnEnable()
        {
            InitializeIfNeeded();
            CheckAndUpdateSprite();
        }

        private void LateUpdate()
        {
            if (autoRestoreIfNull)
            {
                CheckAndUpdateSprite();
            }
        }

        private void InitializeIfNeeded()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            if (!isInitialized)
            {
                if (defaultSprite != null)
                {
                    cachedInitialDefaultSprite = defaultSprite;
                }
                else if (targetImage != null && targetImage.sprite != null)
                {
                    cachedInitialDefaultSprite = targetImage.sprite;
                    defaultSprite = targetImage.sprite;
                }

                if (targetImage != null)
                {
                    lastProcessedSprite = targetImage.sprite;
                }

                isInitialized = true;
            }
        }

        /// <summary>
        /// Monitored every LateUpdate. Allows custom non-null sprites to be displayed,
        /// but automatically restores cached default sprite whenever sprite is set to null or image is disabled.
        /// </summary>
        private void CheckAndUpdateSprite()
        {
            if (targetImage == null) return;

            Sprite effectiveDefault = defaultSprite != null ? defaultSprite : cachedInitialDefaultSprite;
            if (effectiveDefault == null) return;

            Sprite current = targetImage.sprite;

            if (current == null || !targetImage.enabled)
            {
                // No image assigned or image disabled -> Keep and display default image!
                targetImage.sprite = effectiveDefault;
                targetImage.enabled = true;
                lastProcessedSprite = effectiveDefault;
            }
            else
            {
                // A valid sprite is present -> Keep and display current sprite!
                lastProcessedSprite = current;
            }
        }

        /// <summary>
        /// Explicit API method to set a new sprite.
        /// Pass non-null sprite to display custom image; pass null to restore default.
        /// </summary>
        public void SetImage(Sprite newSprite)
        {
            InitializeIfNeeded();

            if (targetImage == null) return;

            Sprite effectiveDefault = defaultSprite != null ? defaultSprite : cachedInitialDefaultSprite;

            if (newSprite != null)
            {
                targetImage.sprite = newSprite;
                targetImage.enabled = true;
                lastProcessedSprite = newSprite;
            }
            else if (effectiveDefault != null)
            {
                targetImage.sprite = effectiveDefault;
                targetImage.enabled = true;
                lastProcessedSprite = effectiveDefault;
            }
        }

        /// <summary>
        /// Restores the default sprite explicitly.
        /// </summary>
        public void RestoreDefault()
        {
            InitializeIfNeeded();

            Sprite effectiveDefault = defaultSprite != null ? defaultSprite : cachedInitialDefaultSprite;
            if (targetImage != null && effectiveDefault != null)
            {
                targetImage.sprite = effectiveDefault;
                targetImage.enabled = true;
                lastProcessedSprite = effectiveDefault;
            }
        }
    }
}
