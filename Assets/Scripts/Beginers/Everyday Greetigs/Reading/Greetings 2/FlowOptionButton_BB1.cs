using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;


public class FlowOptionButton_BB1 : MonoBehaviour
{
    [Header("UI (auto-found if not assigned)")]
    public Image    background;
    public Image    emojiImage;   // sprite-based emoji instead of text
    public TMP_Text labelText;
    public Button   button;

    [Header("Content (set in Inspector)")]
    public Sprite emojiSprite;   // drag your emoji sprite here
    public string label = "Option";

    private Action onTapped;

    void Awake()
    {
        if (button     == null) button     = GetComponent<Button>();
        if (background == null) background = GetComponent<Image>();
        if (emojiImage != null && emojiSprite != null) emojiImage.sprite = emojiSprite;
        if (labelText  != null) labelText.text = label;
    }

    public void Initialize(Color defaultColor, Action callback)
    {
        onTapped = callback;
        SetColor(defaultColor);
        SetInteractable(true);
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onTapped?.Invoke());
    }

    public void SetColor(Color c)       { if (background != null) background.color = c; }
    public void SetInteractable(bool v) { if (button     != null) button.interactable = v; }

    /// <summary>
    /// Sets the local scale of this button's RectTransform directly.
    /// Called by PostReadingFlow_BB1's PulseScale coroutine every frame.
    /// </summary>
    public void SetScale(Vector3 scale)
    {
        transform.localScale = scale;
    }
}