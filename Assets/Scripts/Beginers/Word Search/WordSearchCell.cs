using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// WordSearchCell — purely a letter holder + touch start/end detector.
// Drag detection (including diagonal) is handled entirely by WordSearchManager
// using screen position polling, NOT OnPointerEnter. This is because
// OnPointerEnter only fires when the pointer passes through a cell's center,
// which causes diagonal drags to skip cells entirely.
public class WordSearchCell : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector] public int  row;
    [HideInInspector] public int  col;
    [HideInInspector] public char letter;
    [HideInInspector] public bool isLocked;

    public Image background;   // white cell background
    public Text  letterText;

    [Tooltip("Background color of each cell. Default white to match your prefab.")]
    public Color cellBackgroundColor = Color.white;

    private WordSearchManager manager;

    public void Init(int r, int c, char l, WordSearchManager m)
    {
        row     = r;
        col     = c;
        letter  = l;
        manager = m;
        letterText.text = l.ToString();

        // Set the cell background to the configured color (white by default).
        // The Outline component for grid lines requires the Image to be active.
        // Pill overlays render on top and handle all highlight/selection colors.
        if (background != null)
            background.color = cellBackgroundColor;
    }

    public void OnPointerDown(PointerEventData e) => manager.StartSelection(this);
    public void OnPointerUp(PointerEventData e)   => manager.EndSelection();
}