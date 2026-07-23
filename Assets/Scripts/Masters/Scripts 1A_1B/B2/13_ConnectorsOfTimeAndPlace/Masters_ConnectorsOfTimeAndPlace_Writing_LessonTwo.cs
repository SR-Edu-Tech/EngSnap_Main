using System;
using UnityEngine;

/// <summary>
/// Writing Lesson 2 for Unit 13 Connectors of Time and Place.
/// Implements step-by-step sentence construction checking for TIME connectors, PLACE connectors, and BOTH.
/// Supports multi-word connector matching (e.g. 'in the meantime', 'next to', 'on the other side').
/// </summary>
public class Masters_ConnectorsOfTimeAndPlace_Writing_LessonTwo : Masters_IsThereADifference_Writing_LessonTwo {

    private static readonly string[] timeConnectors = new string[] {
        "meanwhile", "finally", "at last", "immediately", "thereafter", "at that time",
        "subsequently", "eventually", "currently", "presently", "in the meantime", "in the past"
    };

    private static readonly string[] placeConnectors = new string[] {
        "there", "here", "beyond", "nearby", "next to", "at that point", "opposite to",
        "adjacent to", "on the other side", "in the front", "in the back"
    };

    protected override bool ValidateSentence(string rawInput, RuleStep step, out string failReason) {
        failReason = "";

        if (!Masters_SentenceValidator.Validate(rawInput, step.keywordOptions, out string dictFeedback)) {
            failReason = dictFeedback;
            return false;
        }

        string[] words = rawInput.Split(new char[] { ' ', '.', ',', '!', '?', '\n', '\r', '\t', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 4) {
            failReason = "Please write a complete sentence (at least 4 words).";
            return false;
        }

        // Anti-gibberish checks
        foreach (string w in words) {
            for (int i = 0; i < w.Length - 2; i++) {
                if (char.IsLetter(w[i]) && w[i] == w[i + 1] && w[i] == w[i + 2]) {
                    failReason = $"Please write natural English words (avoid repeating characters like '{w}').";
                    return false;
                }
            }

            if (w.Length >= 3) {
                bool hasVowelOrDigit = false;
                foreach (char c in w.ToLowerInvariant()) {
                    if ("aeiouy0123456789".Contains(c)) {
                        hasVowelOrDigit = true;
                        break;
                    }
                }
                if (!hasVowelOrDigit) {
                    failReason = $"The word '{w}' does not appear to be a valid English word.";
                    return false;
                }
            }
        }

        // Check for forbidden keyword
        if (!string.IsNullOrEmpty(step.forbiddenKeyword)) {
            if (rawInput.IndexOf(step.forbiddenKeyword, StringComparison.OrdinalIgnoreCase) >= 0) {
                failReason = $"Remember the rule: please avoid using '{step.forbiddenKeyword}'.";
                return false;
            }
        }

        // Check connectors based on step title or keywords
        bool hasTime = false;
        string matchedTime = "";
        foreach (string tc in timeConnectors) {
            if (ContainsPhrase(rawInput, tc)) {
                hasTime = true;
                matchedTime = tc;
                break;
            }
        }

        bool hasPlace = false;
        string matchedPlace = "";
        foreach (string pc in placeConnectors) {
            if (ContainsPhrase(rawInput, pc)) {
                hasPlace = true;
                matchedPlace = pc;
                break;
            }
        }

        if (step.ruleTitle != null && step.ruleTitle.ToUpperInvariant().Contains("BOTH")) {
            if (!hasTime || !hasPlace) {
                failReason = $"Your sentence must contain both a TIME connector and a PLACE connector.\n\nHint: {step.hintMessage}";
                return false;
            }
        } else if (step.ruleTitle != null && step.ruleTitle.ToUpperInvariant().Contains("PLACE")) {
            if (!hasPlace) {
                failReason = $"Your sentence must include one of the PLACE connectors (e.g. nearby, next to, opposite to).\n\nHint: {step.hintMessage}";
                return false;
            }
        } else if (step.ruleTitle != null && step.ruleTitle.ToUpperInvariant().Contains("TIME")) {
            if (!hasTime) {
                failReason = $"Your sentence must include one of the TIME connectors (e.g. meanwhile, finally, immediately).\n\nHint: {step.hintMessage}";
                return false;
            }
        } else {
            // Check general step keywordOptions
            bool foundAny = false;
            if (step.keywordOptions != null) {
                foreach (string kw in step.keywordOptions) {
                    if (ContainsPhrase(rawInput, kw)) {
                        foundAny = true;
                        break;
                    }
                }
            }
            if (!foundAny) {
                failReason = $"Your sentence must include a required connector word.\n\nHint: {step.hintMessage}";
                return false;
            }
        }

        return true;
    }

    private bool ContainsPhrase(string text, string phrase) {
        int idx = text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;

        // Check word boundary before
        if (idx > 0 && char.IsLetterOrDigit(text[idx - 1])) return false;

        // Check word boundary after
        int endIdx = idx + phrase.Length;
        if (endIdx < text.Length && char.IsLetterOrDigit(text[endIdx])) return false;

        return true;
    }
}
