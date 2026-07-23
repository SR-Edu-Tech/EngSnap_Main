using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReadingTile : MonoBehaviour
{
    [Header("UI Components")]
    public Button button;
    public Image kidImage;
    public TextMeshProUGUI sentenceText;
    public Image backgroundImage;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(1f, 0.92f, 0.016f); // Yellow / Light up
    public Color clickedColor = new Color(0.7f, 1f, 0.7f); // Soft Green

    /// <summary>
    /// Initialize the tile with content from inspector data.
    /// </summary>
    public void SetData(string text, Sprite sprite)
    {
        if (sentenceText != null)
        {
            sentenceText.text = text;
        }

        if (kidImage != null)
        {
            kidImage.gameObject.SetActive(sprite != null);
            kidImage.sprite = sprite;
        }

        ResetVisuals();
    }

    /// <summary>
    /// Resets the background color to the normal color.
    /// </summary>
    public void ResetVisuals()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }

    /// <summary>
    /// Lights up or dims the tile.
    /// </summary>
    public void SetHighlight(bool active)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = active ? highlightColor : normalColor;
        }
    }

    /// <summary>
    /// Changes visual appearance when clicked/tapped.
    /// </summary>
    public void SetClickedVisual()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = clickedColor;
        }
    }
}
