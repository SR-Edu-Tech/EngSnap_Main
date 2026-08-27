using UnityEngine;
using System;

public class Masters_WaysToExpressHappiness_Writing_LessonTwo : Masters_BoostSomeoneUp_Writing_LessonTwo {
    
    protected override void LoadPrompt(int index) {
        base.LoadPrompt(index);
        if (studentInputField != null) {
            studentInputField.readOnly = false;
        }
    }

    protected override bool ValidateInput(string userInput, WritingPrompt currentPrompt, out string failReason) {
        failReason = "";
        
        string[] words = userInput.Split(new char[] { ' ', '.', ',', '!', '?', '\n', '\r', '\t', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 3) {
            failReason = "Please write a more complete sentence (at least 3 words).";
            return false;
        }

        // Anti-gibberish checks
        foreach (string w in words) {
            // Check for flood of repeating characters (e.g., bbbbb or xxxx)
            for (int i = 0; i < w.Length - 2; i++) {
                if (char.IsLetter(w[i]) && w[i] == w[i + 1] && w[i] == w[i + 2]) {
                    failReason = $"Please write natural English words (avoid repeating characters like '{w}').";
                    return false;
                }
            }

            // Check if longer words contain at least one vowel/y or digits/common abbreviations
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

        // Must contain one of the starter chips
        bool hasStarter = false;
        int starterWordCount = 0;
        if (currentPrompt.starterChipsText != null && currentPrompt.starterChipsText.Length > 0) {
            foreach (var chip in currentPrompt.starterChipsText) {
                if (!string.IsNullOrEmpty(chip) && userInput.Contains(chip.ToLowerInvariant().Trim())) {
                    hasStarter = true;
                    string[] chipWords = chip.Split(new char[] { ' ', '.', ',', '!', '?', '\n', '\r', '\t', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
                    starterWordCount = chipWords.Length;
                    break;
                }
            }
        } else {
            hasStarter = true; // No chips provided
        }

        if (!hasStarter) {
            failReason = "Please use one of the starter phrases provided in the buttons.";
            return false;
        }

        if (words.Length <= starterWordCount) {
            failReason = "Please complete the sentence by adding more words after the starter phrase.";
            return false;
        }

        return true;
    }
}
