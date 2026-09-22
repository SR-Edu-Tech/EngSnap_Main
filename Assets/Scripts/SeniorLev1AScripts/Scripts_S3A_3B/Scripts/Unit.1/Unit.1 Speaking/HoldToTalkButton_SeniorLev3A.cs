using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToTalkButton_SeniorLev3A : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        SpeakingGameplay_S3A gameplay = FindObjectOfType<SpeakingGameplay_S3A>();
        if (gameplay != null)
        {
            gameplay.OnMicButtonClicked();
        }
        else
        {
            Debug.LogError("[HoldToTalkButton] SpeakingGameplay_S3A reference missing in scene!");
        }
    }
}