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

    private static readonly HashSet<string> BannedWords = new HashSet<string> {
        "fuck", "fucking", "fucked", "fucker", "shit", "shitty", "bullshit", "bitch", "bitches", "bitchy",
        "ass", "asshole", "asses", "dick", "dicks", "pussy", "pussies", "cock", "cocks", "cunt", "cunts",
        "bastard", "bastards", "whore", "whores", "slut", "sluts", "damn", "damned", "crap", "crappy",
        "nigger", "nigga", "faggot", "retard", "retarded", "chink", "spic", "kike", "wop", "gook",
        "wetback", "tranny", "sex", "sexy", "porn", "porno", "pornography", "nude", "naked", "boob",
        "boobs", "tits", "titty", "penis", "vagina", "dildo", "cum", "jizz", "orgasm", "masturbate",
        "rape", "rapist", "molest", "pedophile", "incest", "suicide"
    };

    public static bool IsProfanity(string text) {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string lower = text.ToLowerInvariant().Trim();
        string[] tokens = lower.Split(new char[] { ' ', '.', ',', '!', '?', ';', ':', '-', '_', '/', '(', ')' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string token in tokens) {
            if (BannedWords.Contains(token)) return true;
        }
        return false;
    }

    public static bool IsValidSingleWord(string word) {
        EnsureDictionaryLoaded();
        return IsValidWord(word);
    }

    private static bool IsValidWord(string word) {
        if (string.IsNullOrEmpty(word)) return false;
        
        // Strip basic punctuation attached to the word
        string cleanWord = word.Trim().TrimEnd('.', ',', '!', '?', ';', ':').TrimStart('\'', '\"', '(').TrimEnd(')', '\'', '\"').ToLower();
        
        if (string.IsNullOrEmpty(cleanWord)) return true; // Just punctuation is ignored

        if (IsProfanity(cleanWord)) return false;

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

        if (IsProfanity(lowerInput)) {
            feedback = "Inappropriate language is not allowed.";
            return false;
        }

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
