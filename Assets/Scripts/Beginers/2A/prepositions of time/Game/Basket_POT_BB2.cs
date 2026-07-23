using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// One of the three fixed baskets (IN / ON / AT).
/// (Add Component → search "Basket_POT_BB2")
/// </summary>
public class Basket_POT_BB2 : MonoBehaviour, IDropHandler
{
    public PotWord_POT_BB2 basketWord;

    private System.Action<Chit_POT_BB2, Basket_POT_BB2> _onChitDropped;

    public void Initialise(PotWord_POT_BB2 word, System.Action<Chit_POT_BB2, Basket_POT_BB2> onChitDropped)
    {
        basketWord     = word;
        _onChitDropped = onChitDropped;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var chit = eventData.pointerDrag.GetComponent<Chit_POT_BB2>();
        if (chit == null || chit.IsPlaced) return;
        _onChitDropped?.Invoke(chit, this);
    }
}
