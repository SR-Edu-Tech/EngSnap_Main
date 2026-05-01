using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Masters_SlideAnimation : MonoBehaviour {


    [SerializeField]
    private Vector3 startPosition, endPosition;
    [SerializeField]
    private float animationSpeed = 0.5f, timeBetweenEachAnimation = 0.25f;
    [SerializeField]
    private int order;


    private RectTransform rectTransform;


    private void Awake() {
        rectTransform = GetComponent<RectTransform>();
        StartCoroutine(AnimationCoroutine());
    }

    private IEnumerator AnimationCoroutine() {
        rectTransform.anchoredPosition = startPosition;
        yield return new WaitForSeconds(timeBetweenEachAnimation * order);
        rectTransform.DOAnchorPos(endPosition, animationSpeed).SetEase(Ease.OutExpo);
    }


}
