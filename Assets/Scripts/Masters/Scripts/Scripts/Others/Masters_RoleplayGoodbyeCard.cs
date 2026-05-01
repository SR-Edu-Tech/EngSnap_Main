using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Masters_RoleplayGoodbyeCard : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {


    public event EventHandler OnSuccessfulDetection;


    [SerializeField]
    private string speechDetectionText;
    [SerializeField]
    private GameObject overFillGameObject;
    [SerializeField]
    private GameObject tickGameObject;
    [SerializeField]
    private Masters_HoldToTalkButton holdToTalkButton;


    private bool canLookForInput;


    private void OnEnable() {
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    private void OnDisable() {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
    }

    private void OnSpeechResult(string spokenText) {
        if (!canLookForInput) {
            return;
        }

        string spoken = spokenText.ToLower().Trim();
        Debug.Log($"Spoken: {spoken}");

        if (spoken == speechDetectionText) {
            // Correct
            canLookForInput = false;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            OnSuccessfulDetection?.Invoke(this, EventArgs.Empty);
            overFillGameObject.SetActive(true);
            tickGameObject.SetActive(true);
            holdToTalkButton.enabled = false;
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    public void OnPointerDown(PointerEventData eventData) {
        canLookForInput = true;
    }

    public void OnPointerUp(PointerEventData eventData) {
        canLookForInput = false;
    }


}
