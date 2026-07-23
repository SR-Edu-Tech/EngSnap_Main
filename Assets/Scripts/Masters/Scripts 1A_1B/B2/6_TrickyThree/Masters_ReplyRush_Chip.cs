using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

public class Masters_ReplyRush_Chip : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI answerText;
    [SerializeField] private Button chipButton;
    [SerializeField] private RectTransform rectTransform;
    
    private Masters_TrickyThree_Game_LessonOne gameManager;
    private bool isCorrectAnswer;
    private RectTransform boundaryArea;
    
    private Vector2 velocity;
    private bool isMoving = false;

    private void Awake() {
        if (chipButton == null) chipButton = GetComponent<Button>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        
        chipButton.onClick.AddListener(OnChipClicked);
    }

    public void Initialize(string text, bool isCorrect, RectTransform boundary, float speed, Masters_TrickyThree_Game_LessonOne manager) {
        if (answerText != null) answerText.text = text;
        
        isCorrectAnswer = isCorrect;
        boundaryArea = boundary;
        gameManager = manager;

        // Pick a random direction
        float randomAngle = Random.Range(0f, 360f);
        velocity = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)).normalized * speed;
        
        isMoving = true;
    }

    private void Update() {
        if (!isMoving || boundaryArea == null) return;

        // Move
        rectTransform.anchoredPosition += velocity * Time.deltaTime;

        // Check Bounds and Bounce
        Vector2 pos = rectTransform.anchoredPosition;
        float halfWidth = rectTransform.rect.width / 2f;
        float halfHeight = rectTransform.rect.height / 2f;

        // Assuming boundaryArea pivot is (0.5, 0.5) and chip pivot is (0.5, 0.5)
        float minX = boundaryArea.rect.xMin + halfWidth;
        float maxX = boundaryArea.rect.xMax - halfWidth;
        float minY = boundaryArea.rect.yMin + halfHeight;
        float maxY = boundaryArea.rect.yMax - halfHeight;

        bool bounced = false;

        if (pos.x <= minX) {
            pos.x = minX;
            velocity.x = Mathf.Abs(velocity.x); // Force right
            bounced = true;
        } else if (pos.x >= maxX) {
            pos.x = maxX;
            velocity.x = -Mathf.Abs(velocity.x); // Force left
            bounced = true;
        }

        if (pos.y <= minY) {
            pos.y = minY;
            velocity.y = Mathf.Abs(velocity.y); // Force up
            bounced = true;
        } else if (pos.y >= maxY) {
            pos.y = maxY;
            velocity.y = -Mathf.Abs(velocity.y); // Force down
            bounced = true;
        }

        if (bounced) {
            rectTransform.anchoredPosition = pos;
        }
    }

    private void OnChipClicked() {
        if (!isMoving) return;
        gameManager.HandleChipClicked(this, isCorrectAnswer);
    }

    public void ExplodeAndDestroy() {
        isMoving = false;
        chipButton.interactable = false;
        
        // Simple POP animation before destroying
        rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 10, 1f).OnComplete(() => {
            Destroy(gameObject);
        });
    }

    public void DestroySilently() {
        Destroy(gameObject);
    }
}
