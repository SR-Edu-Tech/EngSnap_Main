using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToTalkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
       // transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Listening...";
        CrossPlatformSpeechManager_BB1.Instance?.StartListening();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
       // transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
        CrossPlatformSpeechManager_BB1.Instance?.StopListening();
    }
}
