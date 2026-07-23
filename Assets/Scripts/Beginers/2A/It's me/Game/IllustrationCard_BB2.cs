using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drop target for a matching illustration.
/// </summary>
public class IllustrationCard_BB2 : MonoBehaviour, IDropHandler
{
    [Header("References")]
    [SerializeField] private Image illustrationImage;
    [SerializeField] private Image backgroundImage;

    [Header("Colours")]
    [SerializeField] private Color idleColor = Color.white;
    [SerializeField] private Color matchedColor = new Color(0.6f, 1f, 0.65f, 1f);

    public int CorrectPairIndex { get; private set; }
    public bool IsMatched { get; private set; }

    private Action<IllustrationCard_BB2> _onDropped;
    private JuicyButton _juicy;

    private void Awake()
    {
        _juicy = GetComponent<JuicyButton>();

        if (backgroundImage != null)
            backgroundImage.color = idleColor;
    }

    public void Initialise(
        int correctPairIndex,
        Sprite sprite,
        Action<IllustrationCard_BB2> onDropped)
    {
        CorrectPairIndex = correctPairIndex;
        IsMatched = false;
        _onDropped = onDropped;

        if (illustrationImage != null)
            illustrationImage.sprite = sprite;

        if (backgroundImage != null)
            backgroundImage.color = idleColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (IsMatched)
            return;

        _onDropped?.Invoke(this);
    }

    public void SetMatched()
    {
        IsMatched = true;

        if (backgroundImage != null)
            backgroundImage.color = matchedColor;

        if (_juicy != null)
        {
            _juicy.PlayCorrectAnim();
            _juicy.SetDisabled(true);
        }
    }

    public void SetWrong()
    {
        if (_juicy != null)
            _juicy.PlayWrongAnim();

        if (backgroundImage != null)
            backgroundImage.color = idleColor;
    }
}