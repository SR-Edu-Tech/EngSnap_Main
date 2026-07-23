using UnityEngine;
using TMPro;
using System.Reflection;

/// <summary>
/// Speaking Lesson 1 for Unit 14 Real Life Interactions.
/// Displays conversation category in promptTMP and target speaking line in answerTMP without hint tapping.
/// </summary>
public class Masters_RealLifeInteractions_Speaking_LessonOne : Masters_SequenceYourThoughts_Speaking_LessonOne {

    private FieldInfo currentRoundField;
    private FieldInfo answerTMPField;
    private FieldInfo targetField;

    protected override void Awake() {
        currentRoundField = typeof(Masters_IsThereADifference_Speaking_LessonOne).GetField("currentRound", BindingFlags.NonPublic | BindingFlags.Instance);
        answerTMPField = typeof(Masters_IsThereADifference_Speaking_LessonOne).GetField("answerTMP", BindingFlags.NonPublic | BindingFlags.Instance);
        base.Awake();
    }

    private void Update() {
        if (currentRoundField != null && answerTMPField != null) {
            object roundObj = currentRoundField.GetValue(this);
            if (roundObj != null) {
                TextMeshProUGUI ansTMP = answerTMPField.GetValue(this) as TextMeshProUGUI;
                if (targetField == null) {
                    targetField = roundObj.GetType().GetField("targetSpokenSentence");
                }
                if (ansTMP != null && targetField != null) {
                    string targetText = targetField.GetValue(roundObj) as string;
                    if (!string.IsNullOrEmpty(targetText) && ansTMP.text != targetText) {
                        ansTMP.text = targetText;
                    }
                }
            }
        }
    }
}
