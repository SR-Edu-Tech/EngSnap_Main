using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One of the three fixed baskets (THINGS orange / SHOPS blue / CLOTHES green).
/// (Add Component → search "SortShoppingBasket_BB2")
/// </summary>
public class SortShoppingBasket_BB2 : MonoBehaviour, IDropHandler
{
    public ShoppingCategory_BB2 basketCategory;

    private System.Action<SortShoppingCard_BB2, SortShoppingBasket_BB2> _onCardDropped;

    public void Initialise(ShoppingCategory_BB2 category, System.Action<SortShoppingCard_BB2, SortShoppingBasket_BB2> onCardDropped)
    {
        basketCategory = category;
        _onCardDropped = onCardDropped;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var card = eventData.pointerDrag.GetComponent<SortShoppingCard_BB2>();
        if (card == null || card.IsPlaced) return;
        _onCardDropped?.Invoke(card, this);
    }
}
