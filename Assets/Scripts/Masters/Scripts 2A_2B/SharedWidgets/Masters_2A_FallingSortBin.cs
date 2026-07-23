using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class Masters_2A_FallingSortBin : MonoBehaviour {
    [Header("Unit & Category Selection")]
    [SerializeField] private Masters_2A_FallingSortUnitName unitName = Masters_2A_FallingSortUnitName.Unit1_PolishedCommunication;

    [SerializeField] private Masters_Unit1_FallingSortCategory unit1Category;
    [SerializeField] private Masters_Unit4_FallingSortCategory unit4Category;

    [Header("Bin References")]
    [SerializeField] private RectTransform snapPointRectTransform;
    [SerializeField] private RectTransform dropThresholdRectTransform;
    [SerializeField] private TextMeshProUGUI categoryTMP;

    private void Awake() {
        UpdateCategoryText();
    }

    private void OnValidate() {
        UpdateCategoryText();
    }

    public void ConfigureBin(Masters_Unit1_FallingSortCategory category, string textLabel) {
        unit1Category = category;
        if (categoryTMP == null) categoryTMP = GetComponentInChildren<TextMeshProUGUI>(true);
        if (categoryTMP != null) {
            categoryTMP.text = textLabel;
        } else {
            Text legacyText = GetComponentInChildren<Text>(true);
            if (legacyText != null) legacyText.text = textLabel;
        }
    }

    public void UpdateCategoryText() {
        if (categoryTMP == null) {
            categoryTMP = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (categoryTMP != null) {
            string formattedText = "";
            if (unitName == Masters_2A_FallingSortUnitName.Unit1_PolishedCommunication) {
                formattedText = unit1Category.ToString();
            } else if (unitName == Masters_2A_FallingSortUnitName.Unit4_CodeOfConduct) {
                switch (unit4Category) {
                    case Masters_Unit4_FallingSortCategory.ThankYou: formattedText = "THANK YOU"; break;
                    case Masters_Unit4_FallingSortCategory.YoureWelcome: formattedText = "YOU'RE WELCOME"; break;
                    case Masters_Unit4_FallingSortCategory.SayingSorry: formattedText = "SAYING SORRY"; break;
                    case Masters_Unit4_FallingSortCategory.GoodJob: formattedText = "GOOD JOB"; break;
                    case Masters_Unit4_FallingSortCategory.Beautiful: formattedText = "BEAUTIFUL"; break;
                }
            }
            categoryTMP.text = formattedText;
        }
    }

    public bool MatchesUnit1(Masters_Unit1_FallingSortCategory cat) {
        return unitName == Masters_2A_FallingSortUnitName.Unit1_PolishedCommunication && unit1Category == cat;
    }

    public void SetUnit4Category(Masters_Unit4_FallingSortCategory cat) {
        unitName = Masters_2A_FallingSortUnitName.Unit4_CodeOfConduct;
        unit4Category = cat;
        UpdateCategoryText();
    }

    public bool MatchesUnit4(Masters_Unit4_FallingSortCategory cat) {
        return unitName == Masters_2A_FallingSortUnitName.Unit4_CodeOfConduct && unit4Category == cat;
    }

    public Masters_Unit1_FallingSortCategory GetCategory() {
        return unit1Category;
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
