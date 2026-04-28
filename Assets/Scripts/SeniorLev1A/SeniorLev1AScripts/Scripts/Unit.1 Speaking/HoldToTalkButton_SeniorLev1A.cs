using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToTalkButton_SeniorLev1A : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("listening");
        CrossPlatformSpeechManager.Instance?.StartListening();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("listening Done");

        CrossPlatformSpeechManager.Instance?.StopListening();
    }
}
