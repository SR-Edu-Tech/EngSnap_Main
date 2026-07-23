using UnityEngine;

/// <summary>
/// Writing Lesson 2 for Unit 15 Presentation Pointers.
/// Subclasses IsThereADifference_Writing_LessonTwo for 5-step speech outline construction
/// with multi-word phrase keyword matching.
/// </summary>
public class Masters_PresentationPointers_Writing_LessonTwo : Masters_IsThereADifference_Writing_LessonTwo {

    protected override bool ValidateSentence(string rawInput, RuleStep step, out string failReason) {
        failReason = "";

        if (string.IsNullOrWhiteSpace(rawInput)) {
            failReason = "Please enter your sentence.";
            return false;
        }

        if (Masters_SentenceValidator.IsProfanity(rawInput)) {
            failReason = "Inappropriate or vulgar language is not permitted!";
            return false;
        }

        if (!Masters_SentenceValidator.Validate(rawInput, step.keywordOptions, out string dictFeedback)) {
            failReason = dictFeedback;
            return false;
        }

        string[] words = rawInput.Split(new char[] { ' ', '.', ',', '!', '?', '\n', '\r', '\t', ';', ':' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 2) {
            failReason = "Please write a complete phrase or sentence.";
            return false;
        }

        // Check for required keyword presence supporting multi-word phrases (e.g. "Good morning", "Ladies and Gentlemen")
        bool hasKeyword = false;
        if (step.keywordOptions != null && step.keywordOptions.Length > 0) {
            foreach (string kw in step.keywordOptions) {
                if (string.IsNullOrEmpty(kw)) continue;
                if (rawInput.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) {
                    hasKeyword = true;
                    break;
                }
            }
        } else {
            hasKeyword = true;
        }

        if (!hasKeyword) {
            failReason = $"Your sentence must include one of the target phrases: <b>{string.Join(" / ", step.keywordOptions)}</b>.\n\nHint: {step.hintMessage}";
            return false;
        }

        return true;
    }
}
