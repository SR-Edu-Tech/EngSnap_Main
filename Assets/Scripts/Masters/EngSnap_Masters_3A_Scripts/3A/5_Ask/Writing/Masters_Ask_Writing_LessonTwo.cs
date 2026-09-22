using UnityEngine;

/// <summary>
/// Writing Lesson Two for Unit 5.
/// Inherits from Masters_BoostSomeoneUp_Writing_LessonTwo.
/// </summary>
public class Masters_Ask_Writing_LessonTwo : Masters_BoostSomeoneUp_Writing_LessonTwo {
    protected override void LoadPrompt(int index) {
        base.LoadPrompt(index);
        if (studentInputField != null) {
            studentInputField.readOnly = false;
        }
    }

    protected override bool ValidateInput(string userInput, Masters_BoostSomeoneUp_Writing_LessonTwo.WritingPrompt currentPrompt, out string failReason) {
        failReason = "";
        
        // Require at least 4 words to ensure they wrote a phrase/sentence (starter chips are usually 2-3 words)
        if (userInput.Split(new char[] {' ', '.', '?', '!'}, System.StringSplitOptions.RemoveEmptyEntries).Length < 4) {
            return false;
        }

        // For Unit 5 Ask, we want to accept the input if it contains AT LEAST ONE of the valid keywords.
        // (Unlike the base class which demands ALL keywords).
        bool hasKeyword = false;
        foreach (var keyword in currentPrompt.validKeywords) {
            if (!string.IsNullOrEmpty(keyword) && userInput.Contains(keyword.ToLowerInvariant().Trim())) {
                hasKeyword = true;
                break;
            }
        }
        
        return hasKeyword;
    }
}
