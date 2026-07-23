using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_StartMiniQuizButtonAnimator : MonoBehaviour {


    [SerializeField]
    private RectTransform rectTransform;


    public void StartMiniQuizButtonAnimation() {
        rectTransform.transform.DOScale(Vector2.one * 0.75f, 0.5f).SetLoops(-1, LoopType.Yoyo);
    }

    public void ResetAnimation() {
        rectTransform.DOKill(true);
        rectTransform.transform.DOScale(Vector2.one, 0.5f);
    }

    
}
