using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class SpellPictureDropBox : MonoBehaviour, IDropHandler
{
    public TextMeshProUGUI boxText;

    private void Awake()
    {
        boxText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null) return;

        SpellPictureDragTile dragTile = eventData.pointerDrag.GetComponent<SpellPictureDragTile>();
        if (dragTile != null)
        {
            OnDropTile(dragTile);
        }
    }

    public void OnDropTile(SpellPictureDragTile dragTile)
    {
        if (dragTile == null) return;

        dragTile.isDropped = true; // Mark as successfully dropped!

        // Snap tile inside the drop box and render on top of the box image
        dragTile.transform.SetParent(transform, true);
        dragTile.transform.localPosition = Vector3.zero;
        dragTile.transform.localScale = Vector3.one;
        dragTile.transform.SetAsLastSibling(); // Guaranteed to render on top of drop box and green board!
        dragTile.gameObject.SetActive(true);

        if (boxText == null) boxText = GetComponentInChildren<TextMeshProUGUI>();
        if (boxText != null)
        {
            boxText.text = dragTile.GetLetter();
        }

        // Check spelling completion
        Activity3_SpellPictureController controller = FindFirstObjectByType<Activity3_SpellPictureController>();
        if (controller != null)
        {
            controller.CheckSpelling();
        }
    }
}