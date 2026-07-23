using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core Quiz controller for Unit 3: Beyond the Horizon (Book 2A).
/// Subclasses PolishedCommunication_Quiz_LessonOne.
/// Q01 — Town Hall Quiz — Beyond the Horizon.
/// Manages 12 mixed-format questions (MCQ, Fill, Match, Map, Order, Odd one out, T/F)
/// with correctOptionIndex evenly distributed across buttons A, B, C, D (0, 1, 2, 3).
/// </summary>
public class Masters_BeyondTheHorizon_Quiz_LessonOne : Masters_PolishedCommunication_Quiz_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;
        InitializeUnit3Quiz();
    }

#if UNITY_EDITOR
    private void Reset() {
        InitializeUnit3Quiz();
    }

    private void OnValidate() {
        InitializeUnit3Quiz();
    }
#endif

    private void InitializeUnit3Quiz() {
        // If already populated with Unit 3 questions, skip
        if (quizArray != null && quizArray.Length > 0 && quizArray[0] != null && 
            !string.IsNullOrEmpty(quizArray[0].question) && quizArray[0].question.Contains("restroom")) {
            return;
        }

        quizArray = new Quiz[] {
            // Q1: MCQ (Correct index 0 -> ASK)
            new Quiz {
                question = "What kind is 'Excuse me, where is the restroom?'",
                questionAudioClip = LoadAudio("What kind of phrase is Excuse me where is the restroom"),
                options = new string[] { "ASK", "MOVEMENT", "POSITION", "GREETING" },
                correctOptionIndex = 0
            },
            // Q2: MCQ (Correct index 1 -> MOVEMENT)
            new Quiz {
                question = "What kind is 'Turn left / right from the junction.'",
                questionAudioClip = LoadAudio("What kind of phrase is Turn left or right from the junction"),
                options = new string[] { "ASK", "MOVEMENT", "POSITION", "EXCLAMATION" },
                correctOptionIndex = 1
            },
            // Q3: MCQ (Correct index 2 -> POSITION)
            new Quiz {
                question = "What kind is 'It's opposite to...'",
                questionAudioClip = LoadAudio("What kind of phrase is Its opposite to"),
                options = new string[] { "ASK", "MOVEMENT", "POSITION", "QUESTION" },
                correctOptionIndex = 2
            },
            // Q4: Fill (Correct index 3 -> telling)
            new Quiz {
                question = "Would you mind _______ me the way to the Principal's office?",
                questionAudioClip = LoadAudio("Would you mind telling me the way to the Principals office"),
                options = new string[] { "taking", "walking", "turning", "telling" },
                correctOptionIndex = 3
            },
            // Q5: Fill (Correct index 0 -> junction)
            new Quiz {
                question = "Turn left / right from the _______.",
                questionAudioClip = LoadAudio("Turn left or right from the junction"),
                options = new string[] { "junction", "restroom", "position", "question" },
                correctOptionIndex = 0
            },
            // Q6: Match (Correct index 1 -> POSITION)
            new Quiz {
                question = "Match: 'The.... is on your right/ left.'",
                questionAudioClip = LoadAudio("Match the phrase The destination is on your right or left"),
                options = new string[] { "ASK", "POSITION", "MOVEMENT", "GREETING" },
                correctOptionIndex = 1
            },
            // Q7: Map (Correct index 2 -> Go straight...)
            new Quiz {
                question = "On the map the destination is straight ahead. Best phrase?",
                questionAudioClip = LoadAudio("On the map the destination is straight ahead What is the best phrase"),
                options = new string[] { "Turn left...", "It is behind...", "Go straight...", "Where is the..." },
                correctOptionIndex = 2
            },
            // Q8: Map (Correct index 3 -> It's opposite to...)
            new Quiz {
                question = "The clinic faces the post office across the road. Best phrase?",
                questionAudioClip = LoadAudio("The clinic faces the post office across the road What is the best phrase"),
                options = new string[] { "Go straight...", "Walk along...", "On which floor...", "It's opposite to..." },
                correctOptionIndex = 3
            },
            // Q9: Order (Correct index 0 -> straight, turn, beside)
            new Quiz {
                question = "Put in route order: ___ 'It is beside...' · ___ 'Go straight...' · ___ 'Turn left from the junction.'",
                questionAudioClip = LoadAudio("Put the direction phrases in route order"),
                options = new string[] { "straight, turn, beside", "beside, straight, turn", "turn, beside, straight", "straight, beside, turn" },
                correctOptionIndex = 0
            },
            // Q10: Odd one out (Correct index 1 -> Go past...)
            new Quiz {
                question = "Which is NOT a way to ask? 'Where is the stationery store?' · 'How do I get to the hall?' · 'Go past...' · 'Can you tell me the directions?'",
                questionAudioClip = LoadAudio("Which of these is NOT a way to ask for directions"),
                options = new string[] { "Where is the store?", "Go past...", "How do I get there?", "Can you tell me?" },
                correctOptionIndex = 1
            },
            // Q11: MCQ (Correct index 2 -> It is behind...)
            new Quiz {
                question = "Which tells you WHERE a place sits (not how to move)?",
                questionAudioClip = LoadAudio("Which phrase tells you where a place sits"),
                options = new string[] { "Go straight...", "Turn left...", "It is behind...", "Walk along..." },
                correctOptionIndex = 2
            },
            // Q12: T/F (Correct index 1 -> True)
            new Quiz {
                question = "True or False: 'Could you please tell me how I can get to the admin office?' is a polite way to ask.",
                questionAudioClip = LoadAudio("True or False Could you please tell me how I can get to the admin office is a polite way to ask"),
                options = new string[] { "False", "True" },
                correctOptionIndex = 1
            }
        };
    }

    private AudioClip LoadAudio(string clipName) {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/3_BeyondTheHorizon/Quiz/" + clipName + ".mp3");
#else
        return null;
#endif
    }
}
