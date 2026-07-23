using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Masters_ToggleToTalkButton : MonoBehaviour, IPointerClickHandler {


    [SerializeField]
    private TextMeshProUGUI statusTMP;
    [SerializeField]
    private Image buttonImage;
    [SerializeField]
    private Sprite micSprite;
    [SerializeField]
    private Sprite waveFormSprite;


    private bool isClicked;


    public void OnPointerClick(PointerEventData eventData) {
        if (isClicked) {
            isClicked = false;
            statusTMP.text = "Click to talk";
            if (buttonImage != null && micSprite != null) buttonImage.sprite = micSprite;
            CrossPlatformSpeechManager.Instance?.StopListening();
        } else {
            isClicked = true;
            statusTMP.text = "Listening...";
            if (buttonImage != null && waveFormSprite != null) buttonImage.sprite = waveFormSprite;
            CrossPlatformSpeechManager.Instance?.StartListening();
        }

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    public void ResetButton() {
        if (isClicked) {
            isClicked = false;
            statusTMP.text = "Click to talk";
            if (buttonImage != null && micSprite != null) buttonImage.sprite = micSprite;
            CrossPlatformSpeechManager.Instance?.StopListening();
        }
    }

}
