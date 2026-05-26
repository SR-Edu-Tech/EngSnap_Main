using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToTalkButton_SeniorLev1A : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var gameplay = FindObjectOfType<SpeakingGameplay_S1A>();
        if (gameplay != null)
        {
            gameplay.OnMicToggleClicked();
        }
    }
}