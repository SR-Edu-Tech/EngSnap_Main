using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Masters_ToggleToTalkButton : MonoBehaviour, IPointerClickHandler {


    [SerializeField]
    private TextMeshProUGUI statusTMP;


    private bool isClicked;


    public void OnPointerClick(PointerEventData eventData) {
        if (isClicked) {
            isClicked = false;
            statusTMP.text = "Click to talk";
            CrossPlatformSpeechManager.Instance?.StopListening();
        } else {
            isClicked = true;
            statusTMP.text = "Listening...";
            CrossPlatformSpeechManager.Instance?.StartListening();
        }

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }


}
