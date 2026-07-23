using UnityEngine;
using TMPro;
using System.Reflection;

/// <summary>
/// Writing Lesson 1 for Unit 14 Real Life Interactions.
/// Implements Word Bank + Slate sentence construction with dynamic Cue Line display.
/// </summary>
public class Masters_RealLifeInteractions_Writing_LessonOne : Masters_OfferingAHelpingHand_Writing_LessonOne {

    [Header("Cue Line UI")]
    [SerializeField] public TextMeshProUGUI cueLineTMP;
    [SerializeField] public string[] cueLines;

    private FieldInfo puzzleIndexField;
    private int lastDisplayedIndex = -1;

    protected override void Awake() {
        puzzleIndexField = typeof(Masters_OfferingAHelpingHand_Writing_LessonOne).GetField("arrangeWordsPuzzleIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        base.Awake();
    }

    protected override void Start() {
        base.Start();
        EnsureCueLineTMP();
        UpdateCueLine();
    }

    private void EnsureCueLineTMP() {
        if (cueLineTMP == null) {
            Transform qObj = transform.Find("Question ") ?? transform.Find("Question");
            if (qObj == null) {
                foreach (Transform t in GetComponentsInChildren<Transform>(true)) {
                    if (t.name.Trim().Equals("Question", System.StringComparison.OrdinalIgnoreCase)) {
                        qObj = t;
                        break;
                    }
                }
            }
            if (qObj != null) {
                cueLineTMP = qObj.GetComponent<TextMeshProUGUI>() ?? qObj.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    private void Update() {
        UpdateCueLine();
    }

    private void UpdateCueLine() {
        if (puzzleIndexField != null && cueLines != null && cueLines.Length > 0) {
            EnsureCueLineTMP();
            if (cueLineTMP != null) {
                int rawIndex = (int)puzzleIndexField.GetValue(this);
                // When puzzle 0 loads, arrangeWordsPuzzleIndex is incremented to 1
                int currentIndex = Mathf.Clamp(rawIndex - 1, 0, cueLines.Length - 1);
                if (currentIndex != lastDisplayedIndex) {
                    lastDisplayedIndex = currentIndex;
                    cueLineTMP.text = cueLines[currentIndex];
                }
            }
        }
    }
}
