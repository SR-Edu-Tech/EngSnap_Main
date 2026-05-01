using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ButtonPunchAnimator : MonoBehaviour {


    [SerializeField]
    private RectTransform rectTransform;
    

    private Button button;


    private void Awake() {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked() {
        rectTransform.DOKill(true);
        rectTransform.localScale = Vector3.one;

        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);
    }


}
