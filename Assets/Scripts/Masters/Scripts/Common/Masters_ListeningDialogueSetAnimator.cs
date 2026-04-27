using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Masters_ListeningDialogueSetAnimator : MonoBehaviour {


    [SerializeField]
    private RectTransform[] dialogueRectTransformArray;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private float timeBetweenEachAnimation;


    private void OnEnable() {
        StartCoroutine(StartAnimation());
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    private IEnumerator StartAnimation() {
        foreach(RectTransform dialogueRectTransform in dialogueRectTransformArray) {
            dialogueRectTransform.localScale = Vector3.zero;
        }

        for (int i = 0; i < dialogueRectTransformArray.Length; i++) { 
            yield return new WaitForSeconds(timeBetweenEachAnimation);
            dialogueRectTransformArray[i].DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }
    }


}
