using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class BuildSlot_IsAre_BB2 : MonoBehaviour, IDropHandler
{
    public BuildCardCategory_IsAre_BB2 category;

    private System.Action<BuildCard_IsAre_BB2, BuildSlot_IsAre_BB2> _onCardDropped;

    public void Initialise(System.Action<BuildCard_IsAre_BB2, BuildSlot_IsAre_BB2> onCardDropped)
    {
        _onCardDropped = onCardDropped;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var card = eventData.pointerDrag.GetComponent<BuildCard_IsAre_BB2>();
        if (card == null || card.IsPlaced) return;
        _onCardDropped?.Invoke(card, this);
    }
}