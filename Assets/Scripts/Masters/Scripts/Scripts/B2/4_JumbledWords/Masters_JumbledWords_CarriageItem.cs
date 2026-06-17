using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class Masters_JumbledWords_CarriageItem : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI wordText;
    [SerializeField] private Button carriageButton;
    [SerializeField] private RectTransform rectTransform;
    
    private string currentWord;
    private Masters_JumbledWords_Game_LessonOne gameManager;
    private float driftSpeed;
    private bool isDrifting = false;
    private RectTransform spawnArea;

    private void Awake() {
        if (carriageButton == null) carriageButton = GetComponent<Button>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        
        carriageButton.onClick.AddListener(OnCarriageClicked);
    }

    public void Initialize(string word, float speed, RectTransform area, Masters_JumbledWords_Game_LessonOne manager) {
        currentWord = word;
        if (wordText != null) wordText.text = word;
        driftSpeed = speed;
        spawnArea = area;
        gameManager = manager;
        isDrifting = true;
    }

    private void Update() {
        if (!isDrifting || spawnArea == null) return;

        // Drift logic (drifting left across the screen)
        rectTransform.anchoredPosition += Vector2.left * driftSpeed * Time.deltaTime;

        // If it goes off the left edge, loop back to the right edge
        if (rectTransform.anchoredPosition.x < spawnArea.rect.xMin - rectTransform.rect.width) {
            float yPos = Random.Range(spawnArea.rect.yMin, spawnArea.rect.yMax);
            rectTransform.anchoredPosition = new Vector2(spawnArea.rect.xMax + rectTransform.rect.width, yPos);
        }
    }

    private void OnCarriageClicked() {
        if (!isDrifting) return;
        gameManager.HandleCarriageClicked(this);
    }

    public string GetWord() {
        return currentWord;
    }

    public void BounceAndReject() {
        // DoTween visual bounce effect
        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 10, 1f);
    }

    public void CoupleAndStop() {
        isDrifting = false;
        carriageButton.interactable = false;
        // The game manager will handle displaying the coupled word, so we can just destroy this item
        Destroy(gameObject);
    }
}
