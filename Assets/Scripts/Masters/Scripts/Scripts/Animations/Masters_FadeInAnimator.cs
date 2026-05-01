using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Masters_FadeInAnimator : MonoBehaviour {


    [SerializeField]
    private CanvasGroup fillCanvasGroup;
    [SerializeField]
    private CanvasGroup borderCanvasGroup;
    [SerializeField]
    private float animationSpeed;


    private void OnEnable() {
        fillCanvasGroup.alpha = 0f;
        borderCanvasGroup.alpha = 0f;
        FadeIn();
    }

    private void FadeIn() {
        fillCanvasGroup.DOFade(1f, animationSpeed);
        borderCanvasGroup.DOFade(1f, animationSpeed);
    }


}
