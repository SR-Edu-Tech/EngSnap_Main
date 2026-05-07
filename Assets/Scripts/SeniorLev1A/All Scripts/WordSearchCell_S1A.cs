using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// WordSearchCell_S1A — purely a letter holder + touch start/end detector.
// Drag detection (including diagonal) is handled entirely by WordSearchManager_S1A
// using screen position polling, NOT OnPointerEnter. This is because
// OnPointerEnter only fires when the pointer passes through a cell's center,
// which causes diagonal drags to skip cells entirely.
public class WordSearchCell_S1A : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector] public int  row;
    [HideInInspector] public int  col;
    [HideInInspector] public char letter;
    [HideInInspector] public bool isLocked;

    public Image background;   // white cell background
    public Text  letterText;


    private WordSearchManager_S1A manager;

    public void Init(int r, int c, char l, WordSearchManager_S1A m)
    {
        row     = r;
        col     = c;
        letter  = l;
        manager = m;
        letterText.text = l.ToString();


    }

    public void OnPointerDown(PointerEventData e) => manager.StartSelection(this);
    public void OnPointerUp(PointerEventData e)   => manager.EndSelection();
}