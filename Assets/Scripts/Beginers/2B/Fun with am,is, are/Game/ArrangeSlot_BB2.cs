using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One of the four fixed sentence-track slots, identified by position
/// (0 = first word, 3 = last word).
/// (Add Component → search "ArrangeSlot_BB2")
/// </summary>
public class ArrangeSlot_BB2 : MonoBehaviour, IDropHandler
{
    public int slotIndex;

    private System.Action<ArrangeChit_BB2, ArrangeSlot_BB2> _onChitDropped;

    public void Initialise(int index, System.Action<ArrangeChit_BB2, ArrangeSlot_BB2> onChitDropped)
    {
        slotIndex      = index;
        _onChitDropped = onChitDropped;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var chit = eventData.pointerDrag.GetComponent<ArrangeChit_BB2>();
        if (chit == null || chit.IsPlaced) return;
        _onChitDropped?.Invoke(chit, this);
    }
}
