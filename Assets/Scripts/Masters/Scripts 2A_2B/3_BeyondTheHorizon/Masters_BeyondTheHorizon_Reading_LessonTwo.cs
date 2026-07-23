using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core Reading 2 controller for Unit 3: Beyond the Horizon (Book 2A).
/// Subclasses PolishedCommunication_Reading_LessonTwo to inherit full LineDrag matching & pagination.
/// R02 — Match Phrase <-> Kind (8 direction phrases on left, 3 categories on right: ASK, MOVEMENT, POSITION).
/// </summary>
public class Masters_BeyondTheHorizon_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
        InitializeUnit3Puzzles();
    }

#if UNITY_EDITOR
    private void Reset() {
        InitializeUnit3Puzzles();
    }

    private void OnValidate() {
        InitializeUnit3Puzzles();
    }
#endif

    private void InitializeUnit3Puzzles() {
        // If already set by inspector/injector, don't overwrite unless empty
        MatchPuzzle[] currentPuzzles = GetPuzzles();
        if (currentPuzzles != null && currentPuzzles.Length > 0 && currentPuzzles[0].rightPhrase.Contains("ASK")) return;

        string askKind = "ASK — request directions";
        string movKind = "MOVEMENT — how to travel";
        string posKind = "POSITION — where it sits";

        MatchPuzzle[] unit3Puzzles = new MatchPuzzle[] {
            new MatchPuzzle { leftPhrase = "Excuse me, where is the restroom?", rightPhrase = askKind },
            new MatchPuzzle { leftPhrase = "Could you please tell me how I can get to the admin office?", rightPhrase = askKind },
            new MatchPuzzle { leftPhrase = "Go straight...", rightPhrase = movKind },
            new MatchPuzzle { leftPhrase = "Turn left / right from the junction.", rightPhrase = movKind },
            new MatchPuzzle { leftPhrase = "Go past...", rightPhrase = movKind },
            new MatchPuzzle { leftPhrase = "It's opposite to...", rightPhrase = posKind },
            new MatchPuzzle { leftPhrase = "It is behind...", rightPhrase = posKind },
            new MatchPuzzle { leftPhrase = "The.... is on your right/ left.", rightPhrase = posKind }
        };

        SetPuzzles(unit3Puzzles);
    }

    public void SetPuzzles(MatchPuzzle[] data) {
        var field = typeof(Masters_PolishedCommunication_Reading_LessonTwo).GetField("puzzles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) {
            field.SetValue(this, data);
        }
    }

    public MatchPuzzle[] GetPuzzles() {
        var field = typeof(Masters_PolishedCommunication_Reading_LessonTwo).GetField("puzzles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) {
            return field.GetValue(this) as MatchPuzzle[];
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

        var nextSOField = typeof(Masters_PolishedCommunication_Reading_LessonTwo).GetField("nextLessonSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
