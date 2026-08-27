using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToTalkButton_SeniorLev3A : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        var gameplay = FindObjectOfType<SpeakingGameplay_S3A>();
        if (gameplay != null)
        {
            //gameplay.OnMicToggleClicked();
        }
    }
}