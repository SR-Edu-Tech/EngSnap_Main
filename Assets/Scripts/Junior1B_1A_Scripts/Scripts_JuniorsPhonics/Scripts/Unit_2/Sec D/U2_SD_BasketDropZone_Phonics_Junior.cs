
    using UnityEngine;
    using UnityEngine.EventSystems;

    public class U2_SD_BasketDropZone_Phonics_Junior : MonoBehaviour, IDropHandler
    {
        [Header("Basket Type")]
        [Tooltip("Check TRUE for Short Vowel Basket (breve ˘), FALSE for Long Vowel Basket (macron ¯)")]
        [SerializeField] private bool isShortBasket = true;
        [SerializeField] private U2_SD_ShortOrLongManager_Phonics_Junior manager;

        public void OnDrop(PointerEventData eventData)
        {
            if (manager == null) manager = FindFirstObjectByType<U2_SD_ShortOrLongManager_Phonics_Junior>();

            if (manager != null)
            {
                manager.OnCardDroppedOnBasket(isShortBasket);
            }
        }

        // Allows tapping the basket as an alternative
        public void OnBasketClicked()
        {
            if (manager == null) manager = FindFirstObjectByType<U2_SD_ShortOrLongManager_Phonics_Junior>();

            if (manager != null)
            {
                manager.OnCardDroppedOnBasket(isShortBasket);
            }
        }
    }