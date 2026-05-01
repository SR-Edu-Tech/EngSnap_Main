using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Masters_HoldToTalkButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {


    [SerializeField]
    private TextMeshProUGUI statusTMP;


    public void OnPointerDown(PointerEventData eventData) {
        statusTMP.text = "Listening...";
        CrossPlatformSpeechManager.Instance?.StartListening();
    }

    public void OnPointerUp(PointerEventData eventData) {
        statusTMP.text = "Hold to talk";
        CrossPlatformSpeechManager.Instance?.StopListening();
    }


}
