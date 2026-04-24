using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Represents a single answer option button in the quiz.
///
/// WIRING (Inspector / prefab):
///   buttonComponent  → the Button component on this GameObject
///   optionLabel      → TextMeshProUGUI that shows the option text (e.g. "Good morning!")
///   optionImage      → Image used for optional icon (hidden if no sprite)
///   background       → Image used as the button background for colour feedback
///   checkmark        → GameObject shown on correct selection (hidden by default)
///   crossMark        → GameObject shown on wrong selection (hidden by default)
///   revealHighlight  → Image shown in green when this is the correct answer after a wrong pick
///
/// Colours:
///   normalColor      → default button colour
///   correctColor     → green glow on correct pick
///   wrongColor       → red on wrong pick
///   revealColor      → soft green to reveal correct answer after a wrong pick
///   lockedColor      → greyed out while audio is playing / locked
/// </summary>
public class QuizOptionButton_BB1 : MonoBehaviour
{
    [Header("UI References")]
    public Button           buttonComponent;
    public TextMeshProUGUI  optionLabel;
    public Image            optionImage;    // optional icon
    public Image            background;
    public GameObject       checkmark;      // tick icon
    public GameObject       crossMark;      // X icon
    public Image            revealHighlight; // green border shown on correct after wrong pick

    [Header("Colours")]
    public Color normalColor  = new Color(0.95f, 0.95f, 1.00f, 1f);
    public Color correctColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    public Color wrongColor   = new Color(0.90f, 0.25f, 0.25f, 1f);
    public Color revealColor  = new Color(0.30f, 0.85f, 0.40f, 0.45f);
    public Color lockedColor  = new Color(0.75f, 0.75f, 0.80f, 1f);

    [Header("Animation")]
    public float shakeDuration   = 0.45f;
    public float shakeMagnitude  = 14f;
    public float glowPulsePeriod = 0.6f;

    // Callback set by QuizQuestionUI_BB1
    private Action<int> onClickCallback;
    private int         optionIndex;
    private bool        isLocked = false;

    // ── Initialise ────────────────────────────────────────────────────────

    public void Initialise(int index, string text, Sprite icon, Action<int> onClick)
    {
        optionIndex      = index;
        onClickCallback  = onClick;

        if (optionLabel != null) optionLabel.text = text;

        if (optionImage != null)
        {
            if (icon != null)
            {
                optionImage.sprite  = icon;
                optionImage.enabled = true;
            }
            else
            {
                optionImage.enabled = false;
            }
        }

        ResetVisuals();

        buttonComponent.onClick.RemoveAllListeners();
        buttonComponent.onClick.AddListener(OnButtonClicked);
    }

    // ── Lock / Unlock (while audio plays or answer shown) ─────────────────

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        buttonComponent.interactable = !locked;
        if (background != null)
            background.color = locked ? lockedColor : normalColor;
    }

    // ── Feedback states ───────────────────────────────────────────────────

    /// <summary>Show correct-answer glow + checkmark. Locks the button.</summary>
    public void ShowCorrect()
    {
        SetLocked(true);
        StopAllCoroutines();
        if (background    != null) background.color    = correctColor;
        if (checkmark     != null) checkmark.SetActive(true);
        if (crossMark     != null) crossMark.SetActive(false);
        if (revealHighlight != null) revealHighlight.enabled = false;
        StartCoroutine(GlowPulse());
    }

    /// <summary>Show wrong-answer shake + red. Locks the button.</summary>
    public void ShowWrong()
    {
        SetLocked(true);
        StopAllCoroutines();
        if (background  != null) background.color  = wrongColor;
        if (checkmark   != null) checkmark.SetActive(false);
        if (crossMark   != null) crossMark.SetActive(true);
        if (revealHighlight != null) revealHighlight.enabled = false;
        StartCoroutine(ShakeRoutine());
    }

    /// <summary>Highlight this button as the correct answer (after a wrong pick by the player).</summary>
    public void ShowRevealCorrect()
    {
        SetLocked(true);
        StopAllCoroutines();
        if (background  != null) background.color  = revealColor;
        if (checkmark   != null) checkmark.SetActive(true);
        if (crossMark   != null) crossMark.SetActive(false);
        if (revealHighlight != null) revealHighlight.enabled = true;
    }

    /// <summary>Reset to default appearance.</summary>
    public void ResetVisuals()
    {
        StopAllCoroutines();
        isLocked = false;
        buttonComponent.interactable = true;
        if (background      != null) background.color       = normalColor;
        if (checkmark       != null) checkmark.SetActive(false);
        if (crossMark       != null) crossMark.SetActive(false);
        if (revealHighlight != null) revealHighlight.enabled = false;
    }

    // ── Internal ──────────────────────────────────────────────────────────

    void OnButtonClicked()
    {
        if (isLocked) return;
        onClickCallback?.Invoke(optionIndex);
    }

    IEnumerator ShakeRoutine()
    {
        Vector3 origin = transform.localPosition;
        float   t      = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float offset = Mathf.Sin(t * 60f) * shakeMagnitude * (1f - t / shakeDuration);
            transform.localPosition = origin + new Vector3(offset, 0f, 0f);
            yield return null;
        }
        transform.localPosition = origin;
    }

    IEnumerator GlowPulse()
    {
        // Pulse the alpha of the background between full and 80% three times
        if (background == null) yield break;
        Color baseColor = background.color;
        for (int i = 0; i < 3; i++)
        {
            float t = 0f;
            while (t < glowPulsePeriod)
            {
                t += Time.deltaTime;
                float alpha        = Mathf.Lerp(1f, 0.7f, Mathf.PingPong(t / glowPulsePeriod * 2f, 1f));
                background.color   = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
        }
        background.color = baseColor;
    }
}
