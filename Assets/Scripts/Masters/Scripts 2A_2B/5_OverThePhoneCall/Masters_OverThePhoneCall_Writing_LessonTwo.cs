using UnityEngine;
using TMPro;

/// <summary>
/// Core Writing Lesson Two controller for Unit 5: Over The Phone Call (Book 2A).
/// W02 Write the Phone Message: 3 call scenarios on a message pad + word-bank rail + keyword validation.
/// Inherits exact multi-line input handling, rail chips, and visual validators from `Masters_CodeOfConduct_Writing_LessonTwo`.
/// </summary>
public class Masters_OverThePhoneCall_Writing_LessonTwo : Masters_CodeOfConduct_Writing_LessonTwo {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;
    }

    protected override void Start() {
        base.Start();
        foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true)) {
            if (t != null && (t.text.Contains("Etiquette Validators") || t.text.Contains("Validation Checks"))) {
                t.text = "<b>Phone Message Validators</b>";
            }
        }
    }
}
