using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One of the three fixed houses (HE blue / SHE pink / IT green).
/// (Add Component → search "SortPronounHouse_BB2")
/// </summary>
public class SortPronounHouse_BB2 : MonoBehaviour, IDropHandler
{
    public PronounWord_Pronouns_BB2 houseCategory;

    private System.Action<SortPronounCard_BB2, SortPronounHouse_BB2> _onCardDropped;

    public void Initialise(PronounWord_Pronouns_BB2 category, System.Action<SortPronounCard_BB2, SortPronounHouse_BB2> onCardDropped)
    {
        houseCategory  = category;
        _onCardDropped = onCardDropped;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var card = eventData.pointerDrag.GetComponent<SortPronounCard_BB2>();
        if (card == null || card.IsPlaced) return;
        _onCardDropped?.Invoke(card, this);
    }
}
