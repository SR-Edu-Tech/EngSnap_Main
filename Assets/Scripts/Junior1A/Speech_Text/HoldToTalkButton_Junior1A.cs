using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoldToTalkButton_Junior1A : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Listening...";
        CrossPlatformSpeechManager_junior.Instance?.StartListening();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "";
        CrossPlatformSpeechManager_junior.Instance?.StopListening();
    }
}
