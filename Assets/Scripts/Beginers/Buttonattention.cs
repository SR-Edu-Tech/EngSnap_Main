using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;
public class Buttonattention : MonoBehaviour
{
    // Start is called before the first frame update
  void OnEnable()
{
    transform.localScale = Vector3.one;

    transform.DOScale(Vector3.one * 0.75f, 0.5f)
             .SetLoops(-1, LoopType.Yoyo)
             .SetEase(Ease.InOutSine);
}

void OnDisable()
{
    transform.DOKill();
}
}
