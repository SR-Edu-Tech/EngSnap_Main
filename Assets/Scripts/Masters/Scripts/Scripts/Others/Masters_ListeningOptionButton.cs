using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ListeningOptionButton : MonoBehaviour {

    [SerializeField]
    private bool canPopEffect = true;

    private RectTransform rectTransform;
    private Button button;


    private void Awake() {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        button.onClick.AddListener(OnButtonClicked);
    }

    public void ResetButton() {
        if (rectTransform != null) {
            rectTransform.DOKill(true);
            rectTransform.localScale = Vector3.one;
        }
    }

    private void OnButtonClicked() {
        if (canPopEffect) {
            rectTransform.DOKill(true);
            rectTransform.localScale = Vector3.one;

            rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);
        }
    }
}
