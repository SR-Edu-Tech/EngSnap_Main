using UnityEngine;
using UnityEngine.EventSystems;

namespace MastersPhonics
{
    public class U8_FiveFamilies_CardDragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public U8_SA_GM03_FiveFamilies_Masters_Phonics owner;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner != null) owner.OnCardBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner != null) owner.OnCardDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null) owner.OnCardDragEnd(eventData);
        }
    }
}
