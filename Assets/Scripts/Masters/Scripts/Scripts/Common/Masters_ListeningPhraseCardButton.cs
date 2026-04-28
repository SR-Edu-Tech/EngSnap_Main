using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Masters_ListeningPhraseCardButton : MonoBehaviour {


    [SerializeField]
    private Image fillImage, borderImage;
    [SerializeField]
    private Color completedFillColor, completedBorderColor;
    [SerializeField]
    private bool canPopEffect = true;


    private RectTransform rectTransform;
    private Button button;
    private bool doOnce;


    private void Awake() {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked() {
        if (!doOnce) {
            doOnce = true;

            fillImage.color = completedFillColor;
            if (borderImage) {
                borderImage.color = completedBorderColor;
            }
        }

        if (canPopEffect) {
            rectTransform.DOKill(true);
            rectTransform.localScale = Vector3.one;

            rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);
        }
    }


}
