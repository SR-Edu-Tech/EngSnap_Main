using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls individual memory tiles in the Chatting Bees Game Lesson 2 (Match Pairs).
/// Handles tile states (Open, Closed, Matched) and communicates clicks back to the main game controller.
/// </summary>
public class Masters_ChattingBees_MemoryTile : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The text component displaying the phrase/word.")]
    [SerializeField] private TextMeshProUGUI contentText;
    
    [Tooltip("The background image of the tile, which changes color based on state.")]
    [SerializeField] private Image bgImage;
    
    [Tooltip("The button component capturing user clicks.")]
    [SerializeField] private Button tileButton;

    [Header("State Colors")]
    [SerializeField] private Color closedColor = Color.gray;
    [SerializeField] private Color openColor = Color.white;
    [SerializeField] private Color matchedColor = Color.green;

    // Internal state tracking
    private string matchId;
    private Masters_ChattingBees_Game_LessonTwo gameController;
    private bool isMatched = false;
    private bool isOpen = false;
    public bool isResponseTile { get; private set; }

    public void SetIsResponseTile(bool isResponse) {
        isResponseTile = isResponse;
    }


    /// <summary>
    /// Initializes the tile with its match identifier, display text, and a reference to the main controller.
    /// </summary>
    public void Setup(string id, string displayText, Masters_ChattingBees_Game_LessonTwo controller)
    {
        matchId = id;
        gameController = controller;
        
        if (contentText != null) contentText.text = displayText;
        
        // Ensure button is linked and clean up old listeners
        if (tileButton == null) tileButton = GetComponent<Button>();
        if (tileButton != null)
        {
            tileButton.onClick.RemoveAllListeners();
            tileButton.onClick.AddListener(OnTileClicked);
            tileButton.interactable = true;
        }
        
        isMatched = false;
        CloseTile();
    }

    /// <summary>
    /// Triggered when the user taps this specific tile.
    /// Defers to the game controller to check if selection is allowed right now.
    /// </summary>
    private void OnTileClicked()
    {
        if (isMatched || isOpen || !gameController.CanSelectTile()) return;
        
        OpenTile();
        gameController.TileSelected(this);
    }

    /// <summary>
    /// Visually reveals the tile's content.
    /// </summary>
    public void OpenTile()
    {
        isOpen = true;
        if (bgImage != null) bgImage.color = openColor;
        if (contentText != null) contentText.gameObject.SetActive(true);
    }

    /// <summary>
    /// Visually hides the tile's content (flips it back over).
    /// </summary>
    public void CloseTile()
    {
        isOpen = false;
        if (bgImage != null) bgImage.color = closedColor;
        if (contentText != null) contentText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Locks the tile in the open, "matched" state permanently.
    /// </summary>
    public void SetMatched()
    {
        isMatched = true;
        if (tileButton != null) tileButton.interactable = false;
        if (bgImage != null) bgImage.color = matchedColor;
    }

    /// <summary>
    /// Returns the hidden identifier used to check if two tiles form a pair.
    /// </summary>
    public string GetMatchId()
    {
        return matchId;
    }
}
