using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SpellPictureDragTile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Vector3 startWorldPosition;
    private Transform startParent;
    private Canvas mainCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    public TextMeshProUGUI letterText;

    public bool isDropped = false; // Flag to prevent premature return to tray

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        letterText = GetComponentInChildren<TextMeshProUGUI>();
        mainCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDropped = false; // Reset flag at start of drag
        startWorldPosition = transform.position;
        startParent = transform.parent;
        if (mainCanvas == null) mainCanvas = GetComponentInParent<Canvas>();

        // Bring tile to top Canvas keeping world position so it doesn't jump
        if (mainCanvas != null)
        {
            transform.SetParent(mainCanvas.transform, true);
            transform.SetAsLastSibling();
        }

        if (canvasGroup != null) canvasGroup.blocksRaycasts = false; // Allow raycasts to pass to DropBox
    }

    public void OnDrag(PointerEventData eventData)
    {
        Canvas canvas = mainCanvas != null ? mainCanvas : GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            rectTransform,
            eventData.position,
            cam,
            out Vector3 worldPoint))
        {
            if (!float.IsNaN(worldPoint.x) && !float.IsNaN(worldPoint.y) && !float.IsNaN(worldPoint.z) &&
                !float.IsInfinity(worldPoint.x) && !float.IsInfinity(worldPoint.y) && !float.IsInfinity(worldPoint.z))
            {
                transform.position = worldPoint;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        if (!isDropped)
        {
            Canvas canvas = mainCanvas != null ? mainCanvas : GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : eventData.pressEventCamera;

            // 1. Check direct DropBox collision / proximity
            SpellPictureDropBox[] boxes = FindObjectsByType<SpellPictureDropBox>(FindObjectsSortMode.None);
            SpellPictureDropBox bestBox = null;
            float minDistance = float.MaxValue;

            foreach (var box in boxes)
            {
                RectTransform boxRect = box.GetComponent<RectTransform>();
                if (boxRect != null)
                {
                    // Check if cursor/finger is inside box or close to it
                    if (RectTransformUtility.RectangleContainsScreenPoint(boxRect, eventData.position, cam))
                    {
                        bestBox = box;
                        break;
                    }

                    // Calculate distance to box center
                    Vector3 boxWorldPos = boxRect.position;
                    float dist = Vector3.Distance(transform.position, boxWorldPos);
                    if (dist < minDistance && dist < 250f) // Within 250 units distance fallback
                    {
                        minDistance = dist;
                        bestBox = box;
                    }
                }
            }

            if (bestBox != null)
            {
                bestBox.OnDropTile(this);
                return;
            }

            // Return to tray if released away from drop area
            ReturnToTray();
        }
    }

    public void ReturnToTray()
    {
        isDropped = false;
        gameObject.SetActive(true);
        transform.SetParent(startParent, true);
        transform.position = startWorldPosition;
        transform.localScale = Vector3.one;

        Image img = GetComponent<Image>();
        if (img == null) img = GetComponentInChildren<Image>(true);
        if (img != null) img.color = Color.white;

        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
    }

    public string GetLetter()
    {
        if (letterText == null) letterText = GetComponentInChildren<TextMeshProUGUI>();
        return letterText != null ? letterText.text : "";
    }
}