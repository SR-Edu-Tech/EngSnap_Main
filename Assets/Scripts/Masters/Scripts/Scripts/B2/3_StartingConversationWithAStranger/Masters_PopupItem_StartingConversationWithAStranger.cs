using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_PopupItem_StartingConversationWithAStranger : MonoBehaviour {

    [SerializeField]
    private TextMeshProUGUI answerTMP;
    
    [SerializeField]
    private Button itemButton;

    private bool isCorrect;
    private Masters_StartingConversationWithAStranger_Game_LessonOne gameManager;
    private RectTransform rectTransform;
    
    private float lifetime;

    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        if (itemButton != null) {
            itemButton.onClick.AddListener(OnItemClicked);
        } else {
            // fallback if Button component is on same object
            Button btn = GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OnItemClicked);
        }
    }

    public void Initialize(string answerText, bool correct, float itemLifetime, Masters_StartingConversationWithAStranger_Game_LessonOne manager) {
        if (answerTMP != null) answerTMP.text = answerText;
        isCorrect = correct;
        lifetime = itemLifetime;
        gameManager = manager;
        
        // Pop up animation using DOTween
        rectTransform.localScale = Vector3.zero;
        rectTransform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }

    private void Update() {
        if (gameManager == null || !gameManager.IsGameActive()) return;

        lifetime -= Time.deltaTime;
        if (lifetime <= 0) {
            // Lifetime expired
            if (isCorrect) {
                gameManager.HandleCorrectItemMissed();
            }
            
            // Pop out animation then destroy
            rectTransform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                Destroy(gameObject);
            });
            // Stop updating
            gameManager = null;
        }
    }

    private void OnItemClicked() {
        if (gameManager == null || !gameManager.IsGameActive()) return;

        gameManager.HandleItemClicked(isCorrect);
        
        // Pop out animation then destroy
        rectTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
            Destroy(gameObject);
        });
        
        // Disable further interaction
        gameManager = null;
        if (itemButton != null) itemButton.interactable = false;
    }
}

