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
        if (button != null) {
            button.onClick.AddListener(OnButtonClicked);
        } else {
            Debug.LogWarning($"Button component missing on {gameObject.name}");
        }
    }

    private void OnEnable() {
        if (speakerGameObject) {
            speakerGameObject.SetActive(false);
        }
    }

    private void OnButtonClicked() {
        if (speakerGameObject) {
            speakerGameObject.SetActive(true);
        }

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        rectTransform.DOKill(true);
        rectTransform.localScale = Vector3.one;

        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);
        Masters_AudioManager.Instance.PlayVoiceOver(audioClip);
        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
            if (speakerGameObject) {
                speakerGameObject.SetActive(false);
            }
        }));
    }


}
