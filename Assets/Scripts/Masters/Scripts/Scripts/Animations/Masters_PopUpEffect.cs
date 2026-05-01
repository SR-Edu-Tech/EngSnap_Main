using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Masters_PopUpEffect : MonoBehaviour {


    [SerializeField]
    private RectTransform parentRectTransform;
    [SerializeField]
    private float popUpAnimationTime = 0.5f, timeBetweenEachAnimation = 0.25f;
    [SerializeField]
    private int order;
    [SerializeField]
    private bool canPlaySound;


    private void OnEnable() {
        StartCoroutine(StartingAnimationCoroutine());
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    private IEnumerator StartingAnimationCoroutine() {
        parentRectTransform.localScale = Vector3.zero;
        yield return new WaitForSeconds(timeBetweenEachAnimation * order);
        if (canPlaySound) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }
        parentRectTransform.DOScale(Vector3.one, popUpAnimationTime).SetEase(Ease.OutExpo);
    }


}
