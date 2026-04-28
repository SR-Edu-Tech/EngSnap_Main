using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_BackButton : MonoBehaviour {


    [SerializeField]
    private RectTransform parentTransform;
    [SerializeField]
    private float animationTime = 0.5f;


    private Button button;


    private void Awake() {
        button = this.GetComponent<Button>();
        button.onClick.AddListener(OnBackButtonClicked);
    }

    private void OnEnable() {
        StartingAnimation();
    }

    private void StartingAnimation() {
        parentTransform.localScale = Vector3.zero;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        parentTransform.DOScale(Vector3.one, animationTime).SetEase(Ease.OutExpo);
    }

    private void OnBackButtonClicked() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectNegative);
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnBackButtonClicked();
    }


}
