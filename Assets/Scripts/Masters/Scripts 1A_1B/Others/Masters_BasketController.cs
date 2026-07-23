using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Masters_BasketController : MonoBehaviour, IDragHandler {

    [SerializeField]
    private float minX = -400f; // Adjust based on your Canvas size
    [SerializeField]
    private float maxX = 400f;

    private RectTransform rectTransform;
    private Masters_OfferingAHelpingHand_Game_LessonOne gameManager;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
    }

    public void Setup(Masters_OfferingAHelpingHand_Game_LessonOne manager) {
        gameManager = manager;
    }

    public void OnDrag(PointerEventData eventData) {
        if (rectTransform == null || gameManager == null || !gameManager.IsGameActive()) {
            return;
        }

        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)rectTransform.parent, 
            eventData.position, 
            eventData.pressEventCamera, 
            out localPointerPosition)) {
            
            float clampedX = Mathf.Clamp(localPointerPosition.x, minX, maxX);
            rectTransform.anchoredPosition = new Vector2(clampedX, rectTransform.anchoredPosition.y);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (gameManager == null || !gameManager.IsGameActive()) return;

        if (collision.TryGetComponent(out Masters_FallingItem fallingItem)) {
            gameManager.HandleItemCaught(fallingItem.GetIsCorrect());
            Destroy(fallingItem.gameObject);
        }
    }
}
