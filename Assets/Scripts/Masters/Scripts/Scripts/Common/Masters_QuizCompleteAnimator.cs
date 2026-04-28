using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Masters_QuizCompleteAnimator : MonoBehaviour {


    [SerializeField]
    private RectTransform backgroundRectTransform;
    [SerializeField]
    private RectTransform[] starRectTransformArray;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private float timeBetweenAnimation;


    private void OnEnable() {
        backgroundRectTransform.localScale = Vector3.zero;
        foreach (RectTransform starRectTransform in starRectTransformArray) {
            starRectTransform.localScale = Vector3.zero;
        }

        backgroundRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
            StartCoroutine(StarAnimation());
        });
    }

    private IEnumerator StarAnimation() {
        foreach (RectTransform starRectTransform in starRectTransformArray) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            starRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
            yield return new WaitForSeconds(timeBetweenAnimation);
        }
    }


}
