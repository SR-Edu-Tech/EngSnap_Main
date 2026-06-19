using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_UniversalLineDragMatch : MonoBehaviour {

    public enum Column { Left, Right }

    [SerializeField]
    private Column columnPosition;
    [SerializeField]
    private Transform lineRendererPointTransform;
    [Tooltip("The string to match with the other side. They must match exactly.")]
    [SerializeField]
    private string correctMatch;
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private Image fillImage, borderImage;
    [SerializeField]
    private Button button;
    [SerializeField]
    private TextMeshProUGUI displayTextTMP;

    private bool isSolved;

    public void Initialize(Column column, string text, string matchText) {
        this.columnPosition = column;
        if (displayTextTMP != null) displayTextTMP.text = text;
        this.correctMatch = matchText;
    }

    public Column GetColumnPosition() {
        return columnPosition;
    }

    public Transform GetLineRendererPointTransform() {
        if (!isSolved) return lineRendererPointTransform;
        return null;
    }

    public LineRenderer GetLineRenderer() {
        if (!isSolved) return lineRenderer;
        return null;
    }

    public string GetCorrectMatch() {
        if (!isSolved) return correctMatch;
        return "";
    }

    public void Solved() {
        isSolved = true;
        if (button != null) button.interactable = false;
    }

    public bool GetIsSolved() {
        return isSolved;
    }
}
