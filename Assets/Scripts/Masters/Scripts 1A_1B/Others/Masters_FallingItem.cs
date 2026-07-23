using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Masters_FallingItem : MonoBehaviour {

    [SerializeField]
    private TextMeshProUGUI answerTMP;
    
    [SerializeField]
    private float fallSpeed = 200f; // Speed for UI Canvas elements

    private bool isCorrect;
    private Masters_OfferingAHelpingHand_Game_LessonOne gameManager;
    private RectTransform rectTransform;

    public void Initialize(string answerText, bool correct, Masters_OfferingAHelpingHand_Game_LessonOne manager) {
        answerTMP.text = answerText;
        isCorrect = correct;
        gameManager = manager;
        rectTransform = GetComponent<RectTransform>();
    }

    public bool GetIsCorrect() {
        return isCorrect;
    }

    private void Update() {
        // Move downwards. Assuming this is in a UI Canvas.
        if (rectTransform != null) {
            rectTransform.anchoredPosition += Vector2.down * fallSpeed * Time.deltaTime;

            // Destroy if it falls too far below the screen to prevent memory leak
            if (rectTransform.anchoredPosition.y < -2000f) {
                Destroy(gameObject);
            }
        }
    }
}
