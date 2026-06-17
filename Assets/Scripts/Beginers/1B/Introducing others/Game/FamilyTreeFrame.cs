using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  FamilyTreeFrame
///  Represents a single empty frame on the family tree (e.g., Father, Mother).
///  Handles hint glow, drop zone detection, and snap-feedback animations.
/// ════════════════════════════════════════════════════════════════════
///
///  STRUCTURE:
///  Frame GameObject       [this script] [RectTransform]
///    ├─ GlowOverlay         Image      (bright glow border, normally hidden)
///    ├─ Label               TMP_Text   ("Father", "Mother", etc.)
///    └─ DropZone            RectTransform (detection area, can be self or child)
/// </summary>
public class FamilyTreeFrame : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique identifier matching the correct relative (e.g., father, mother, sister)")]
    public string relativeId;

    [Header("Visual Wiring")]
    [Tooltip("The border or overlay image that glows to highlight this frame")]
    public Image frameGlowImage;
    [Tooltip("The text label showing the relation name (e.g., 'Father')")]
    public TMP_Text labelText;
    [Tooltip("The area used for drag-drop overlap check. If null, uses this RectTransform")]
    public RectTransform dropZone;
    [Tooltip("The parent transform where the snapped portrait card will live. If null, uses dropZone")]
    public Transform snapParent;

    [Header("Animation Settings")]
    public float glowPulseDuration = 0.5f;
    public float scalePunchAmount = 0.15f;
    public float scalePunchDuration = 0.35f;

    // State properties
    public bool IsFilled { get; set; } = false;
    public RectTransform RectTransform { get; private set; }

    private Tween _glowTween;
    private Tween _scaleTween;

    void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        
        if (dropZone == null)
            dropZone = RectTransform;

        if (snapParent == null)
            snapParent = dropZone;

        ResetFrame();
    }

    /// <summary>
    /// Resets the frame to its initial empty state, clearing visual glow and occupation status.
    /// </summary>
    public void ResetFrame()
    {
        IsFilled = false;
        
        // Kill existing tweens
        KillTweens();

        // Clear glow opacity
        if (frameGlowImage != null)
        {
            Color c = frameGlowImage.color;
            c.a = 0f;
            frameGlowImage.color = c;
        }

        // Reset scale
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Plays a pulsating glow sequence (fade in & out) to act as a hint on wrong drops.
    /// </summary>
    public void PlayHintGlow()
    {
        if (frameGlowImage == null) return;

        // Kill any ongoing glow tween
        _glowTween?.Kill();

        // Pulsate the glow alpha: 0 -> 1 -> 0 -> 1 -> 0
        frameGlowImage.color = new Color(frameGlowImage.color.r, frameGlowImage.color.g, frameGlowImage.color.b, 0f);
        
        _glowTween = frameGlowImage.DOFade(0.85f, glowPulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(4, LoopType.Yoyo) // Fade up/down twice
            .OnComplete(() => {
                frameGlowImage.DOFade(0f, 0.25f);
            });
    }

    /// <summary>
    /// Plays a celebratory juice animation (scale punch/bounce) when the correct portrait is snapped.
    /// </summary>
    public void PlayCorrectAnimation()
    {
        _scaleTween?.Kill();
        transform.localScale = Vector3.one;

        // Punch scale up slightly to feel extremely clicky/juicy
        _scaleTween = transform.DOPunchScale(Vector3.one * scalePunchAmount, scalePunchDuration, 5, 0.5f);
        
        // Quick flash of glow to green (if custom color) or full opacity, then fade
        if (frameGlowImage != null)
        {
            _glowTween?.Kill();
            frameGlowImage.color = new Color(frameGlowImage.color.r, frameGlowImage.color.g, frameGlowImage.color.b, 1f);
            _glowTween = frameGlowImage.DOFade(0f, 0.6f).SetDelay(0.1f);
        }
    }

    private void KillTweens()
    {
        _glowTween?.Kill();
        _scaleTween?.Kill();
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
