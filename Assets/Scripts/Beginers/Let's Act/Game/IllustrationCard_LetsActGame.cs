using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// IllustrationCard — right-side image card.
/// Acts as a DROP TARGET for drag lines from WordLabels.
/// Implements IDropHandler so Unity's EventSystem fires OnDrop
/// when a drag is released over this card.
/// </summary>
[RequireComponent(typeof(JuicyButton))]
public class IllustrationCard : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
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

    // Runtime
    public int  CorrectPairIndex { get; private set; }
    public bool IsMatched        { get; private set; }

    private Action<IllustrationCard> _onDropped;
    private JuicyButton _juicy;

    void Awake()
    {
        _juicy = GetComponent<JuicyButton>();
        if (matchIndicator   != null) matchIndicator.gameObject.SetActive(false);
        if (checkmarkOverlay != null) checkmarkOverlay.gameObject.SetActive(false);
    }

    // ── Setup ────────────────────────────────────────────────────────────────

    public void Initialise(int correctPairIndex, Sprite sprite, Action<IllustrationCard> onDropped)
    {
        CorrectPairIndex = correctPairIndex;
        if (illustrationImage != null) illustrationImage.sprite = sprite;
        _onDropped = onDropped;
        SetBg(idleColor);
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
        // Only highlight if something is being dragged
        if (eventData.pointerDrag != null)
            SetBg(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
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