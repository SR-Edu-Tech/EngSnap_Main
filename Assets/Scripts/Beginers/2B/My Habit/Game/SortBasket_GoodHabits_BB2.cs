using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One of the two fixed baskets (HABIT green / QUALITY orange).
/// (Add Component → search "SortBasket_GoodHabits_BB2")
/// </summary>
public class SortBasket_GoodHabits_BB2 : MonoBehaviour, IDropHandler
{
    public HabitOrQuality_GoodHabits_BB2 basketCategory;

    private System.Action<SortCard_GoodHabits_BB2, SortBasket_GoodHabits_BB2> _onCardDropped;

    public void Initialise(HabitOrQuality_GoodHabits_BB2 category, System.Action<SortCard_GoodHabits_BB2, SortBasket_GoodHabits_BB2> onCardDropped)
    {
        basketCategory = category;
        _onCardDropped = onCardDropped;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var card = eventData.pointerDrag.GetComponent<SortCard_GoodHabits_BB2>();
        if (card == null || card.IsPlaced) return;
        _onCardDropped?.Invoke(card, this);
    }
}
