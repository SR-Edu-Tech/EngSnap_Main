using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_OfferingAHelpingHand_MemoryTile : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Image bgImage;
    [SerializeField] private Color closedColor = Color.gray;
    [SerializeField] private Color openColor = Color.white;
    [SerializeField] private Color matchedColor = Color.green;
    [SerializeField] private Button tileButton;

    private string matchId;
    private Masters_OfferingAHelpingHand_Game_LessonTwo gameController;
    private bool isMatched = false;
    private bool isOpen = false;

    public void Setup(string id, string displayText, Masters_OfferingAHelpingHand_Game_LessonTwo controller)
    {
        matchId = id;
        gameController = controller;
        if (contentText != null) contentText.text = displayText;
        
        if (tileButton == null) tileButton = GetComponent<Button>();
        if (tileButton != null)
        {
            tileButton.onClick.RemoveAllListeners();
            tileButton.onClick.AddListener(OnTileClicked);
        }
        
        isMatched = false;
        if (tileButton != null) tileButton.interactable = true;
        
        CloseTile();
    }

    private void OnTileClicked()
    {
        if (isMatched || isOpen || !gameController.CanSelectTile()) return;
        
        OpenTile();
        gameController.TileSelected(this);
    }

    public void OpenTile()
    {
        isOpen = true;
        if (bgImage != null) bgImage.color = openColor;
        if (contentText != null) contentText.gameObject.SetActive(true);
    }

    public void CloseTile()
    {
        isOpen = false;
        if (bgImage != null) bgImage.color = closedColor;
        if (contentText != null) contentText.gameObject.SetActive(false);
    }

    public void SetMatched()
    {
        isMatched = true;
        if (tileButton != null) tileButton.interactable = false;
        if (bgImage != null) bgImage.color = matchedColor;
    }

    public string GetMatchId()
    {
        return matchId;
    }
}
