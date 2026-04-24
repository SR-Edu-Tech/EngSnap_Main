using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class UpdateLineItem_BB1 : MonoBehaviour
{
        public Text  lineText;
    public Image background;

    public Color normalColor    = Color.white;
    public Color highlightColor = Color.yellow;

    public void SetText(string text)
    {
        lineText.text = text;
    }

    public void Highlight(bool state)
    {
        background.color = state ? highlightColor : normalColor;
    }
}
