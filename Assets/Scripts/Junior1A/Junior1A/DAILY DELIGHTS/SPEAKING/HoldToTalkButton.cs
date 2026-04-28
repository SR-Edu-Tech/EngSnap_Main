using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToTalkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
       // transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Listening...";
        CrossPlatformSpeechManager.Instance?.StartListening();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
       // transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
        CrossPlatformSpeechManager.Instance?.StopListening();
    }
}
