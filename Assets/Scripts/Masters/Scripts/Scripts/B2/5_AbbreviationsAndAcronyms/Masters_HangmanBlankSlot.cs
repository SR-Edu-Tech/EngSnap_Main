using TMPro;
using UnityEngine;

public class Masters_HangmanBlankSlot : MonoBehaviour {
    
    [SerializeField] private TextMeshProUGUI slotTextTMP;
    [SerializeField] private string blankCharacter = "_"; // What it shows when empty
    
    private char targetLetter;
    public bool isRevealed { get; private set; }

    public void Setup(char target) {
        targetLetter = target;
        isRevealed = false;
        
        if (slotTextTMP != null) {
            slotTextTMP.text = blankCharacter;
        }
    }

    public char GetTargetLetter() {
        return targetLetter;
    }

    public void Reveal() {
        isRevealed = true;
        if (slotTextTMP != null) {
            slotTextTMP.text = targetLetter.ToString().ToUpper();
        }
    }
}
