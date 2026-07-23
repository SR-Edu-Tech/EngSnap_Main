using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class IntroCard_BB2 : MonoBehaviour
{
    [Header("UI Refs")]
    public Image    illustrationImage;
    public TMP_Text sentenceText;
    public Button   tapButton;

    public int              OrderIndex { get; private set; }
    public IntroCardData_BB2 Data      { get; private set; }
    public bool              IsPlaced  { get; private set; }

    private System.Action<IntroCard_BB2> _onTapped;

    public void Initialise(int orderIndex, IntroCardData_BB2 data, System.Action<IntroCard_BB2> onTapped)
    {
        OrderIndex = orderIndex;
        Data       = data;
        _onTapped  = onTapped;
        IsPlaced   = false;

        if (sentenceText != null)      sentenceText.text        = data.sentenceText;
        if (illustrationImage != null) illustrationImage.sprite = data.illustrationSprite;

        if (tapButton != null)
        {
            tapButton.interactable = true;
            tapButton.onClick.RemoveAllListeners();
            tapButton.onClick.AddListener(HandleTap);
        }
    }

    private void HandleTap()
    {
        if (IsPlaced) return;
        _onTapped?.Invoke(this);
    }

    public void SetInteractable(bool value)
    {
        if (tapButton != null) tapButton.interactable = value;
    }

    public void MarkPlaced()
    {
        IsPlaced = true;
        SetInteractable(false);
    }
}