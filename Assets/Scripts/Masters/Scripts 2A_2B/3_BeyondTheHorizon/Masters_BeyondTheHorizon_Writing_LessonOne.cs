using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core Writing 1 controller for Unit 3: Beyond the Horizon (Book 2A).
/// Subclasses PolishedCommunication_Writing_LessonOne to inherit fill-in-the-blank text evaluation & hint system.
/// W01 — Complete the Direction Phrase (across 10 verbatim rounds with rich alternative synonyms).
/// </summary>
public class Masters_BeyondTheHorizon_Writing_LessonOne : Masters_PolishedCommunication_Writing_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;
        InitializeUnit3Questions();
    }

#if UNITY_EDITOR
    private void Reset() {
        InitializeUnit3Questions();
    }

    private void OnValidate() {
        InitializeUnit3Questions();
    }
#endif

    private void InitializeUnit3Questions() {
        WritingQuestion[] currentQuestions = GetQuestions();
        if (currentQuestions != null && currentQuestions.Length > 0 && currentQuestions[0].acceptableExactMatches != null && currentQuestions[0].acceptableExactMatches.Length > 2) return;

        WritingQuestion[] unit3Questions = new WritingQuestion[] {
            new WritingQuestion {
                incomingMessageText = "Excuse me, where is the ________? [ask]",
                requiredKeywords = new string[] { "restroom" },
                acceptableExactMatches = new string[] { "restroom", "washroom", "bathroom", "toilet", "lavatory", "office", "library", "clinic", "exit" },
                hintText = "Type 'restroom' (or 'washroom', 'bathroom') to complete the phrase."
            },
            new WritingQuestion {
                incomingMessageText = "Would you mind ________ me the way to the Principal's office? [ask]",
                requiredKeywords = new string[] { "telling" },
                acceptableExactMatches = new string[] { "telling", "showing", "guiding", "directing" },
                hintText = "Type 'telling' (or 'showing', 'guiding') to complete the phrase."
            },
            new WritingQuestion {
                incomingMessageText = "Could you please tell me how I can ________ to the admin office? [ask]",
                requiredKeywords = new string[] { "get" },
                acceptableExactMatches = new string[] { "get", "reach", "go", "walk", "travel" },
                hintText = "Type 'get' (or 'reach', 'go') to complete the phrase."
            },
            new WritingQuestion {
                incomingMessageText = "On which ________ is the dining hall? [ask]",
                requiredKeywords = new string[] { "floor" },
                acceptableExactMatches = new string[] { "floor", "level", "story", "storey", "side" },
                hintText = "Type 'floor' (or 'level') to complete the phrase."
            },
            new WritingQuestion {
                incomingMessageText = "Go ________... [movement]",
                requiredKeywords = new string[] { "straight" },
                acceptableExactMatches = new string[] { "straight", "forward", "ahead", "past", "along" },
                hintText = "Type 'straight' (or 'forward', 'ahead') to complete the phrase."
            },
            new WritingQuestion {
                incomingMessageText = "Turn left / right from the ________. [movement]",
                requiredKeywords = new string[] { "junction" },
                acceptableExactMatches = new string[] { "junction", "intersection", "corner", "crossing", "signal", "turn" },
                hintText = "Type 'junction' (or 'corner', 'intersection') to complete the phrase."
            },
            new WritingQuestion {
                incomingMessageText = "Walk / Go along the ________. [movement]",
                requiredKeywords = new string[] { "corridor" },
                acceptableExactMatches = new string[] { "corridor", "road", "hallway", "path", "street", "passage", "aisle" },
                hintText = "Type 'corridor' (or 'road', 'hallway') to complete the phrase."
            },
            new WritingQuestion {
                incomingMessageText = "It's ________ to... [position]",
                requiredKeywords = new string[] { "opposite" },
                acceptableExactMatches = new string[] { "opposite", "next", "close", "near", "adjacent" },
                hintText = "Type 'opposite' (or 'next', 'close') to complete the phrase."
            },
            new WritingQuestion {
                incomingMessageText = "It is ________... [position]",
                requiredKeywords = new string[] { "beside" },
                acceptableExactMatches = new string[] { "beside", "next", "near", "opposite", "behind", "nearby" },
                hintText = "Type 'beside' (or 'next', 'near') to complete the phrase."
            },
            new WritingQuestion {
                incomingMessageText = "It's in ________... and...... [position]",
                requiredKeywords = new string[] { "between" },
                acceptableExactMatches = new string[] { "between", "middle" },
                hintText = "Type 'between' (or 'middle') to complete the phrase."
            }
        };

        SetQuestions(unit3Questions);
    }

    public void SetQuestions(WritingQuestion[] data) {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonOne).GetField("questions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) {
            field.SetValue(this, data);
        }
    }

    public WritingQuestion[] GetQuestions() {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonOne).GetField("questions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) {
            return field.GetValue(this) as WritingQuestion[];
        }
        return null;
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.LogWarning($"Topic not set for {this.name}!");
            return;
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        var nextSOField = typeof(Masters_PolishedCommunication_Writing_LessonOne).GetField("nextLessonSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Masters_LessonSO nextLessonSO = nextSOField != null ? nextSOField.GetValue(this) as Masters_LessonSO : null;

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
