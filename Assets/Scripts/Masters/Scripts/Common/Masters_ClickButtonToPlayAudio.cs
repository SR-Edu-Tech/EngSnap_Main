using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ClickButtonToPlayAudio : MonoBehaviour {


    [SerializeField]
    private AudioClip audioClip;
    [SerializeField]
    private GameObject speakerGameObject;


    private Button button;
    private RectTransform rectTransform;


    private void Awake() {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked() {
        speakerGameObject.SetActive(true);

        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);
        Masters_AudioManager.Instance.PlayVoiceOver(audioClip);
        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
            speakerGameObject.SetActive(false);
        }));
    }


}
