using UnityEngine;
using TMPro;
using DG.Tweening;

public class Masters_JumbledWords_BubbleItem : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI sentenceText;
    [SerializeField] private RectTransform rectTransform;
    
    private string sentence;
    private bool isJumbled;
    private float driftSpeed;
    private Masters_JumbledWords_Game_LessonTwo gameManager;
    private RectTransform spawnArea;
    private bool isActive = false;

    private void Awake() {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(string text, bool jumbled, float speed, RectTransform area, Masters_JumbledWords_Game_LessonTwo manager) {
        sentence = text;
        isJumbled = jumbled;
        if (sentenceText != null) sentenceText.text = text;
        driftSpeed = speed;
        spawnArea = area;
        gameManager = manager;
        isActive = true;
    }

    private void Update() {
        if (!isActive || spawnArea == null) return;

        // Drift upwards
        rectTransform.anchoredPosition += Vector2.up * driftSpeed * Time.deltaTime;

        // Check if it reached the top of the spawn area
        if (rectTransform.anchoredPosition.y > spawnArea.rect.yMax + rectTransform.rect.height) {
            isActive = false;
            gameManager.HandleBubbleEscaped(this);
            Destroy(gameObject);
        }
    }

    public bool IsJumbled() {
        return isJumbled;
    }

    public void Pop() {
        isActive = false;
        // DoTween scale down and then destroy
        rectTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
            Destroy(gameObject);
        });
    }

    public void Buzz() {
        isActive = false; // Stop drifting so it doesn't look like it's still in play
        
        // Shake it to indicate error, then shrink and destroy
        rectTransform.DOPunchRotation(new Vector3(0, 0, 15f), 0.3f, 10, 1f).OnComplete(() => {
            rectTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
                Destroy(gameObject);
            });
        });
    }
}
