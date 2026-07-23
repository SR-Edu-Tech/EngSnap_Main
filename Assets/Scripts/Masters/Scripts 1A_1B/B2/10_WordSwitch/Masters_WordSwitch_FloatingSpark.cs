using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Masters_WordSwitch_FloatingSpark : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    public string synonymText;
    public string homeBankKey;
    public float lifetimeRemaining;
    public Vector2 velocity;

    private bool isBeingDragged = false;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Masters_WordSwitch_Game_LessonOne gameManager;
    private CanvasGroup canvasGroup;

    public void Init(string syn, string bankKey, float life, float speed, Masters_WordSwitch_Game_LessonOne mgr) {
        synonymText = syn;
        homeBankKey = bankKey;
        lifetimeRemaining = life;
        gameManager = mgr;

        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Random diagonal launch angle
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized * speed;
        if (Mathf.Abs(velocity.x) < 30f) velocity.x = 40f * Mathf.Sign(velocity.x == 0 ? 1 : velocity.x);
        if (Mathf.Abs(velocity.y) < 30f) velocity.y = 40f * Mathf.Sign(velocity.y == 0 ? 1 : velocity.y);
    }

    private void Update() {
        if (isBeingDragged || gameManager == null || !gameManager.IsGameActive()) return;

        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining <= 0) {
            gameManager.OnSparkExpired(this);
            return;
        }

        // Dissolve blink warning in last 1.5 seconds
        if (lifetimeRemaining < 1.5f && canvasGroup != null) {
            canvasGroup.alpha = Mathf.PingPong(Time.time * 6f, 1f) * 0.5f + 0.5f;
        }

        if (rectTransform != null && rectTransform.parent is RectTransform parentRect) {
            rectTransform.anchoredPosition += velocity * Time.deltaTime;

            Vector2 pos = rectTransform.anchoredPosition;
            Rect rect = parentRect.rect;
            float halfW = rectTransform.rect.width * 0.5f;
            float halfH = rectTransform.rect.height * 0.5f;

            if (pos.x - halfW < rect.xMin) {
                pos.x = rect.xMin + halfW;
                velocity.x *= -1;
            } else if (pos.x + halfW > rect.xMax) {
                pos.x = rect.xMax - halfW;
                velocity.x *= -1;
            }

            if (pos.y - halfH < rect.yMin) {
                pos.y = rect.yMin + halfH;
                velocity.y *= -1;
            } else if (pos.y + halfH > rect.yMax) {
                pos.y = rect.yMax - halfH;
                velocity.y *= -1;
            }

            rectTransform.anchoredPosition = pos;
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {
        if (gameManager == null || !gameManager.IsGameActive()) return;
        isBeingDragged = true;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData) {
        if (!isBeingDragged || parentCanvas == null) return;
        rectTransform.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!isBeingDragged) return;
        isBeingDragged = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        if (gameManager != null) {
            gameManager.OnSparkDropped(this, eventData);
        }
    }
}
