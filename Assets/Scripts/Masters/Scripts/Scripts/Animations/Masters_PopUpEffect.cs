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
    [SerializeField]
    private bool canPlayAutomatically = true;


    private void OnEnable() {
        parentRectTransform.localScale = Vector3.zero;
        if (canPlayAutomatically) {
            StartCoroutine(StartingAnimationCoroutine());
        }
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    private IEnumerator StartingAnimationCoroutine() {
        yield return new WaitForSeconds(timeBetweenEachAnimation * order);
        if (canPlaySound) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }
        parentRectTransform.DOScale(Vector3.one, popUpAnimationTime).SetEase(Ease.OutExpo);
    }

    public void Pop() {
        StartCoroutine(StartingAnimationCoroutine());
    }


}
