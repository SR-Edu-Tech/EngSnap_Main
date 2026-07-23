using UnityEngine;
using UnityEngine.Serialization;
using TMPro;
using DG.Tweening;

public class Masters_FallingSortBin : MonoBehaviour {
    [Header("Unit & Category Selection")]
    [SerializeField] private Masters_FallingSortUnitName unitName;

    [FormerlySerializedAs("sortType")]
    [SerializeField] private Masters_Unit8_FallingSortCategory unit8Category;

    [SerializeField] private Masters_Unit9_FallingSortCategory unit9Category;

    [SerializeField] private Masters_Unit12_FallingSortCategory unit12Category;

    [SerializeField] private Masters_Unit13_FallingSortCategory unit13Category;

    [SerializeField] private Masters_Unit15_FallingSortCategory unit15Category;

    [SerializeField] private Masters_Unit2_FallingSortCategory unit2Category;

    [SerializeField] private Masters_Unit3_FallingSortCategory unit3Category;

    [SerializeField] private Masters_Unit4_FallingSortCategory unit4Category;

    [Header("Bin References")]
    [SerializeField] private RectTransform snapPointRectTransform;
    [SerializeField] private RectTransform dropThresholdRectTransform; // When Y goes below this, it's counted as entered
    [SerializeField] private TextMeshProUGUI categoryTMP;

    private void Awake() {
        UpdateCategoryText();
    }

    private void OnValidate() {
        UpdateCategoryText();
    }

    public void UpdateCategoryText() {
        if (categoryTMP == null) {
            categoryTMP = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (categoryTMP != null) {
            string formattedText = "";
            if (unitName == Masters_FallingSortUnitName.Unit8_ChattingBees) {
                formattedText = unit8Category.ToString();
            } else if (unitName == Masters_FallingSortUnitName.Unit1_PolishedCommunication) {
                formattedText = (unit8Category == Masters_Unit8_FallingSortCategory.Ask) ? "FORMAL" : "INFORMAL";
            } else if (unitName == Masters_FallingSortUnitName.Unit9_SmartAlternatives) {
                switch (unit9Category) {
                    case Masters_Unit9_FallingSortCategory.Congratulations: formattedText = "Congratulations"; break;
                    case Masters_Unit9_FallingSortCategory.IAgree: formattedText = "I agree"; break;
                    case Masters_Unit9_FallingSortCategory.ItsEasy: formattedText = "It's easy!"; break;
                    case Masters_Unit9_FallingSortCategory.ILikeIt: formattedText = "I like it"; break;
                    case Masters_Unit9_FallingSortCategory.ForExample: formattedText = "For example"; break;
                    case Masters_Unit9_FallingSortCategory.IThink: formattedText = "I think"; break;
                }
            } else if (unitName == Masters_FallingSortUnitName.Unit12_SequenceYourThoughts) {
                formattedText = unit12Category.ToString();
            } else if (unitName == Masters_FallingSortUnitName.Unit13_ConnectorsOfTimeAndPlace) {
                formattedText = unit13Category.ToString().ToUpperInvariant();
            } else if (unitName == Masters_FallingSortUnitName.Unit15_PresentationPointers) {
                switch (unit15Category) {
                    case Masters_Unit15_FallingSortCategory.GettingAttention: formattedText = "Getting Attention"; break;
                    case Masters_Unit15_FallingSortCategory.Introduction: formattedText = "Introduction"; break;
                    case Masters_Unit15_FallingSortCategory.Presentation: formattedText = "Presentation"; break;
                    case Masters_Unit15_FallingSortCategory.ConclusionGratitude: formattedText = "Conclusion/Gratitude"; break;
                }
            } else if (unitName == Masters_FallingSortUnitName.Unit2_ClearConfusion) {
                switch (unit2Category) {
                    case Masters_Unit2_FallingSortCategory.AskToRepeat: formattedText = "ASK TO REPEAT"; break;
                    case Masters_Unit2_FallingSortCategory.ExplainAgain: formattedText = "EXPLAIN AGAIN"; break;
                    case Masters_Unit2_FallingSortCategory.ReasonAndAsk: formattedText = "REASON + ASK"; break;
                    case Masters_Unit2_FallingSortCategory.AskPermission: formattedText = "ASK PERMISSION"; break;
                    case Masters_Unit2_FallingSortCategory.SignalPolitely: formattedText = "SIGNAL POLITELY"; break;
                }
            } else if (unitName == Masters_FallingSortUnitName.Unit3_BeyondTheHorizon) {
                switch (unit3Category) {
                    case Masters_Unit3_FallingSortCategory.Ask: formattedText = "ASK"; break;
                    case Masters_Unit3_FallingSortCategory.Movement: formattedText = "MOVEMENT"; break;
                    case Masters_Unit3_FallingSortCategory.Position: formattedText = "POSITION"; break;
                }
            } else if (unitName == Masters_FallingSortUnitName.Unit4_CodeOfConduct) {
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

    public void SetUnit8Category(Masters_Unit8_FallingSortCategory cat) {
        unit8Category = cat;
        UpdateCategoryText();
    }

    public bool MatchesUnit8(Masters_Unit8_FallingSortCategory cat) {
        return (unitName == Masters_FallingSortUnitName.Unit8_ChattingBees || unitName == Masters_FallingSortUnitName.Unit1_PolishedCommunication) && unit8Category == cat;
    }

    public bool MatchesUnit9(Masters_Unit9_FallingSortCategory cat) {
        return unitName == Masters_FallingSortUnitName.Unit9_SmartAlternatives && unit9Category == cat;
    }

    public bool MatchesUnit12(Masters_Unit12_FallingSortCategory cat) {
        return unitName == Masters_FallingSortUnitName.Unit12_SequenceYourThoughts && unit12Category == cat;
    }

    public bool MatchesUnit13(Masters_Unit13_FallingSortCategory cat) {
        return unitName == Masters_FallingSortUnitName.Unit13_ConnectorsOfTimeAndPlace && unit13Category == cat;
    }

    public bool MatchesUnit15(Masters_Unit15_FallingSortCategory cat) {
        return unitName == Masters_FallingSortUnitName.Unit15_PresentationPointers && unit15Category == cat;
    }

    public void SetUnit2Category(Masters_Unit2_FallingSortCategory cat) {
        unitName = Masters_FallingSortUnitName.Unit2_ClearConfusion;
        unit2Category = cat;
        UpdateCategoryText();
    }

    public bool MatchesUnit2(Masters_Unit2_FallingSortCategory cat) {
        return unitName == Masters_FallingSortUnitName.Unit2_ClearConfusion && unit2Category == cat;
    }

    public void SetUnit3Category(Masters_Unit3_FallingSortCategory cat) {
        unitName = Masters_FallingSortUnitName.Unit3_BeyondTheHorizon;
        unit3Category = cat;
        UpdateCategoryText();
    }

    public bool MatchesUnit3(Masters_Unit3_FallingSortCategory cat) {
        return unitName == Masters_FallingSortUnitName.Unit3_BeyondTheHorizon && unit3Category == cat;
    }

    public void SetUnit4Category(Masters_Unit4_FallingSortCategory cat) {
        unitName = Masters_FallingSortUnitName.Unit4_CodeOfConduct;
        unit4Category = cat;
        UpdateCategoryText();
    }

    public bool MatchesUnit4(Masters_Unit4_FallingSortCategory cat) {
        return unitName == Masters_FallingSortUnitName.Unit4_CodeOfConduct && unit4Category == cat;
    }

    public RectTransform GetSnapPoint() {
        return snapPointRectTransform;
    }

    public float GetDropThresholdY() {
        if (dropThresholdRectTransform != null) {
            return dropThresholdRectTransform.position.y;
        }
        return GetComponent<RectTransform>().position.y; // Fallback to bin's own Y
    }

    public void AnimateCatch(bool isCorrect) {
        transform.DOKill(true);
        if (isCorrect) {
            transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 10, 1f);
        } else {
            transform.DOShakePosition(0.3f, new Vector3(15f, 0, 0), 20);
        }
    }
}
