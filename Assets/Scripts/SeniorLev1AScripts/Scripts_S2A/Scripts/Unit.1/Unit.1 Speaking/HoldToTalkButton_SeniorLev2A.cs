using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToTalkButton_SeniorLev2A : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var gameplay = FindObjectOfType<SpeakingGameplay_S2A>();
        if (gameplay != null)
        {
            //gameplay.OnMicToggleClicked();
        }
    }
}