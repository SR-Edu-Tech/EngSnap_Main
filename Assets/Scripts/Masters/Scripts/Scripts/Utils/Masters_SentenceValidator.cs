using System.Collections.Generic;
using UnityEngine;

public static class Masters_SentenceValidator {
    
    // Max allowed unknown words (Level 2 strictness)
    private const int MAX_UNKNOWN_WORDS = 2;
    private static HashSet<string> validWordsSet;
    private static bool isDictionaryLoaded = false;

    private static void EnsureDictionaryLoaded() {
        if (isDictionaryLoaded) return;

        validWordsSet = new HashSet<string>();
        TextAsset wordsAsset = Resources.Load<TextAsset>("words");
        if (wordsAsset != null) {
            string[] lines = wordsAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines) {
                validWordsSet.Add(line.Trim().ToLower());
            }
            Debug.Log($"Masters_SentenceValidator: Loaded {validWordsSet.Count} words into Dictionary.");
        } else {
            Debug.LogError("Masters_SentenceValidator: Failed to load words.txt from Resources! Ensure words.txt is placed in Assets/Resources/");
        }
        isDictionaryLoaded = true;
    }

    private static bool IsValidWord(string word) {
        if (string.IsNullOrEmpty(word)) return false;
        
        // Strip basic punctuation attached to the word
        string cleanWord = word.Trim().TrimEnd('.', ',', '!', '?', ';', ':').TrimStart('\'', '\"', '(').TrimEnd(')', '\'', '\"').ToLower();
        
        if (string.IsNullOrEmpty(cleanWord)) return true; // Just punctuation is ignored

        return validWordsSet.Contains(cleanWord);
    }

    public static bool Validate(string input, string[] validKeywords, out string feedback) {
        EnsureDictionaryLoaded();
        feedback = "";
        
        if (string.IsNullOrWhiteSpace(input)) {
            feedback = "Input cannot be empty.";
            return false;
        }

        string lowerInput = input.ToLower().Trim();

        // Level 1: Keyword Check
        bool hasKeyword = false;
        if (validKeywords != null && validKeywords.Length > 0) {
            foreach (string kw in validKeywords) {
                if (lowerInput.Contains(kw.ToLower())) {
                    hasKeyword = true;
                    break;
                }
            }
            if (!hasKeyword) {
                feedback = "Sentence must contain at least one of the required keywords.";
                return false;
            }
        }

        // Level 2: Dictionary / Spell Check
        if (validWordsSet != null && validWordsSet.Count > 0) {
            string[] words = input.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            int unknownCount = 0;
            List<string> unknownWords = new List<string>();

            foreach (string word in words) {
                if (!IsValidWord(word)) {
                    // Ignore numbers
                    if (!float.TryParse(word, out _)) {
                        unknownCount++;
                        unknownWords.Add(word);
                    }
                }
            }

            if (unknownCount > MAX_UNKNOWN_WORDS) {
                feedback = $"Too many unknown words. Please check your spelling.";
                return false;
            }
        }

        return true;
    }
}
