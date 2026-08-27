using UnityEngine;
using UnityEngine.EventSystems;

public class SC_BasketDropZone_Phonics_Junior : MonoBehaviour, IDropHandler
{
    [SerializeField] private bool isVowelBasket;
    [SerializeField] private SC_VowelQuizManager_Phonics_Junior quizManager;

    private void Awake()
    {
        EnsureInit();
    }

    private void EnsureInit()
    {
        if (quizManager == null) quizManager = GetComponentInParent<SC_VowelQuizManager_Phonics_Junior>();
        if (quizManager == null) quizManager = FindFirstObjectByType<SC_VowelQuizManager_Phonics_Junior>(FindObjectsInactive.Include);
    }

    public void OnDrop(PointerEventData eventData)
    {
        EnsureInit();

        SC_DraggableLetter_Phonics_Juniors draggable = null;

        if (eventData != null && eventData.pointerDrag != null)
        {
            draggable = eventData.pointerDrag.GetComponent<SC_DraggableLetter_Phonics_Juniors>();
        }

        if (draggable == null)
        {
            draggable = FindFirstObjectByType<SC_DraggableLetter_Phonics_Juniors>();
        }

        if (draggable != null)
        {
            draggable.SnapTo(transform);

            if (quizManager != null)
            {
                if (isVowelBasket)
                    quizManager.SelectVowel();
                else
                    quizManager.SelectConsonant();
            }
        }
    }
}