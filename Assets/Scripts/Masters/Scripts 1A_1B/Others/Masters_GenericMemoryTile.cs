using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A completely generic memory tile that doesn't rely on Book 2A's class names.
/// Handles tile states (Open, Closed, Matched) and communicates clicks via an interface.
/// </summary>
public interface IGenericMemoryGameController {
    bool CanSelectTile();
    void TileSelected(Masters_GenericMemoryTile selectedTile);
}

public class Masters_GenericMemoryTile : MonoBehaviour {
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image bgImage;
    [SerializeField] private Button tileButton;

    [Header("State Colors")]
    [SerializeField] private Color closedColor = Color.gray;
    [SerializeField] private Color openColor = Color.white;
    [SerializeField] private Color matchedColor = Color.green;

    private string matchId;
    private IGenericMemoryGameController gameController;
    private bool isMatched = false;
    private bool isOpen = false;
    public bool alwaysShowContent { get; private set; } = false;

    public void Setup(string id, string displayText, IGenericMemoryGameController controller, bool alwaysShow = false) {
        matchId = id;
        gameController = controller;
        alwaysShowContent = alwaysShow;
        
        if (contentText != null) contentText.text = displayText;
        
        if (tileButton == null) tileButton = GetComponent<Button>();
        if (tileButton != null) {
            tileButton.onClick.RemoveAllListeners();
            tileButton.onClick.AddListener(OnTileClicked);
            tileButton.interactable = true;
        }
        
        isMatched = false;
        CloseTile();
    }

    private void OnTileClicked() {
        if (isMatched || (isOpen && !alwaysShowContent) || gameController == null || !gameController.CanSelectTile()) return;
        
        OpenTile();
        gameController.TileSelected(this);
    }

    public void OpenTile() {
        isOpen = true;
        if (bgImage != null) bgImage.color = openColor;
        if (contentText != null) contentText.gameObject.SetActive(true);
    }

    public void CloseTile() {
        isOpen = false;
        if (bgImage != null) bgImage.color = closedColor;
        if (contentText != null) contentText.gameObject.SetActive(alwaysShowContent || isOpen);
    }

    public void SetMatched() {
        isMatched = true;
        if (tileButton != null) tileButton.interactable = false;
        if (bgImage != null) bgImage.color = matchedColor;
    }

    public string GetMatchId() {
        return matchId;
    }

    public string GetDisplayText() {
        if (contentText != null) return contentText.text;
        return "";
    }
}
