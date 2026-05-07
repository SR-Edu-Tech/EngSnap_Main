using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToTalkButton_SeniorLev1A : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("listening");
        CrossPlatformSpeechManager_S1A.Instance?.StartListening();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("listening Done");

        CrossPlatformSpeechManager_S1A.Instance?.StopListening();
    }
}
