using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Unit 7: Question Tags - Roleplay Lesson Two (RP02: Free Scene — Just Checking).
/// Subclasses Unit 1's roleplay controller and manages the 3 free scene cards:
/// - Card A: A classmate before the test
/// - Card B: Asking a teacher
/// - Card C: At home with a cousin
/// Provides robust normalized validation so correct word combinations always pass,
/// plays sentence audio, manages hints, and advances to Quiz 1 upon completion.
/// </summary>
public class Masters_QuestionTags_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;
    }

    public void SetScenes(SceneData[] sceneData) {
        scenes = sceneData;
    }

    public SceneData[] GetScenes() {
        return scenes;
    }

    protected override void OnCheckButtonClicked() {
        if (!canClickCheck) return;

        string currentSentence = "";
        for (int i = 0; i < activeSlateWords.Count; i++) {
            if (activeSlateWords[i] != null) {
                currentSentence += activeSlateWords[i].GetButtonString() + " ";
            }
        }
        currentSentence = currentSentence.Trim();

        if (activeScene == null || activeScene.turns == null || currentTurnIndex >= activeScene.turns.Length) return;

        RoleplayTurn turn = activeScene.turns[currentTurnIndex];
        bool isCorrect = false;
        int matchedIndex = -1;

        if (turn.validSentences != null) {
            for (int i = 0; i < turn.validSentences.Length; i++) {
                string valid = turn.validSentences[i].Trim();
                if (currentSentence.Equals(valid, System.StringComparison.OrdinalIgnoreCase) ||
                    NormalizeText(currentSentence) == NormalizeText(valid)) {
                    isCorrect = true;
                    matchedIndex = i;
                    break;
                }
            }
        }

        if (isCorrect) {
            HandleCorrectAnswer(currentSentence, turn, matchedIndex);
        } else {
            HandleWrongAnswer(turn);
        }
    }

    private string NormalizeText(string s) {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace(",", "")
                .Replace(".", "")
                .Replace("?", "")
                .Replace("!", "")
                .Replace("'", "")
                .Replace("’", "")
                .Replace("\"", "")
                .Replace("“", "")
                .Replace("”", "")
                .Replace(" ", "")
                .Trim()
                .ToLowerInvariant();
    }
}
