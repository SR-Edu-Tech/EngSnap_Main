using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_UniversalSortPhraseCard : MonoBehaviour {

    [Tooltip("The ID used to match with the sort bin.")]
    [SerializeField]
    private int sortId;
    [SerializeField]
    private TextMeshProUGUI expressionTMP;

    private Button button;

    private void Awake() {
        button = GetComponent<Button>();
    }

    public Button GetButton() {
        return button;
    }

    public void SetSortIdAndExpression(int sortId, string expression) {
        this.sortId = sortId;
        expressionTMP.text = expression;
    }

    public int GetSortId() {
        return sortId;
    }
}
