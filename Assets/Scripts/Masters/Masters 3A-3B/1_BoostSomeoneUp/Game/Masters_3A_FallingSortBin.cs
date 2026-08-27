using UnityEngine;
using TMPro;
using DG.Tweening;

public class Masters_3A_FallingSortBin : MonoBehaviour {
    [SerializeField] private Masters_3A_FallingSortCategory category;

    [Header("Bin References")]
    [SerializeField] private RectTransform snapPointRectTransform;
    [SerializeField] private RectTransform dropThresholdRectTransform;
    [SerializeField] private TextMeshProUGUI categoryTMP;

    public void ConfigureBin(Masters_3A_FallingSortCategory cat, string textLabel) {
        category = cat;
        if (categoryTMP == null) categoryTMP = GetComponentInChildren<TextMeshProUGUI>(true);
        if (categoryTMP != null) {
            categoryTMP.text = textLabel;
        }
    }

    public bool MatchesCategory(Masters_3A_FallingSortCategory cat) {
        return category == cat;
    }

    public Masters_3A_FallingSortCategory GetCategory() {
        return category;
    }

    public void AnimateCatch(bool isCorrect) {
        transform.DOPunchScale(Vector3.one * (isCorrect ? 0.2f : 0.1f), 0.3f);
    }

    public RectTransform GetSnapPoint() {
        return snapPointRectTransform;
    }

    public float GetDropThresholdY() {
        if (dropThresholdRectTransform != null) {
            return dropThresholdRectTransform.position.y;
        }
        return GetComponent<RectTransform>().position.y;
    }
}
