using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Masters_HangmanLetterOption : MonoBehaviour {
    
    [SerializeField] private TextMeshProUGUI letterTextTMP;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Color disabledColor = Color.gray;
    
    private Button button;
    private char myLetter;
    private Masters_AbbreviationsAndAcronyms_Game_LessonTwo gameManager;
    private Color originalColor;

    private void Awake() {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
        if (buttonImage != null) {
            originalColor = buttonImage.color;
        }
    }

    public void Setup(char letter, Masters_AbbreviationsAndAcronyms_Game_LessonTwo manager) {
        myLetter = letter;
        gameManager = manager;
        
        if (letterTextTMP != null) {
            letterTextTMP.text = letter.ToString().ToUpper();
        }
        
        // Reset state
        button.interactable = true;
        if (buttonImage != null) {
            buttonImage.color = originalColor;
        }
    }

    private void OnClicked() {
        if (gameManager == null) return;
        
        // Disable after clicking to prevent multiple guesses of the same wrong letter
        button.interactable = false;
        if (buttonImage != null) {
            buttonImage.color = disabledColor;
        }
        
        gameManager.OnLetterOptionClicked(this, myLetter);
    }
}
