using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Masters_BreathEffect : MonoBehaviour {


    [SerializeField]
    private RectTransform rectTransform;


    private void Awake() {
        rectTransform.DOScale(Vector2.one * 0.75f, 0.5f).SetLoops(-1, LoopType.Yoyo);
    }


}
