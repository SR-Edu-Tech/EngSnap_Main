using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Subclass for Unit 6: Groove On - Speaking Lesson One (SP01: Say It with Cheer - Wish Aloud).
/// Inherits 100% of the speech recognition, coroutines, phrase card spawning, and UI animations
/// directly from the gold-standard base class `Masters_PolishedCommunication_Speaking_LessonOne`.
/// </summary>
public class Masters_GrooveOn_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
        InitializeUnit6Prompts();
    }

    private void InitializeUnit6Prompts() {
        if (speechToTextArray != null && speechToTextArray.Length > 0 && speechToTextArray[0] != null &&
            !string.IsNullOrEmpty(speechToTextArray[0].phraseCardText) && speechToTextArray[0].phraseCardText.Contains("birthday")) {
            return;
        }

        string audioDir = "Assets/Audio/2A/6_GrooveOn/Speaking/";
#if UNITY_EDITOR
        speechToTextArray = new SpeechToText[] {
            new SpeechToText {
                phraseCardText = "<b>1. Wish your friend a happy birthday today:</b>\nSay: \"<color=#FFD700>Wish you a very happy birthday! Have fun!</color>\"",
                speechDetectionText = new string[] {
                    "Wish you a very happy birthday Have fun",
                    "Wish you a very happy birthday",
                    "happy birthday Have fun",
                    "happy birthday",
                    "Wish you a happy birthday"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Wish you a very happy birthday Have fun.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>2. You missed your friend's birthday yesterday:</b>\nSay: \"<color=#FFD700>Belated birthday wishes! Hope you had a blast!</color>\"",
                speechDetectionText = new string[] {
                    "Belated birthday wishes Hope you had a blast",
                    "Belated birthday wishes",
                    "Hope you had a blast",
                    "Belated happy birthday"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Belated birthday wishes Hope you had a blast.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>3. Ask about the party details:</b>\nSay: \"<color=#FFD700>Where's the party? What about the theme?</color>\"",
                speechDetectionText = new string[] {
                    "Wheres the party What about the theme",
                    "Where is the party What about the theme",
                    "Where is the party",
                    "Wheres the party",
                    "What about the theme"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Wheres the party What about the theme.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>4. Greet your friend on Diwali:</b>\nSay: \"<color=#FFD700>Wish you a Happy Diwali! Joy and prosperity this Diwali!</color>\"",
                speechDetectionText = new string[] {
                    "Wish you a Happy Diwali Joy and prosperity this Diwali",
                    "Wish you a Happy Diwali",
                    "Happy Diwali Joy and prosperity",
                    "Happy Diwali"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Wish you a Happy Diwali Joy and prosperity this Diwali.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>5. Greet your friend on any festival:</b>\nSay: \"<color=#FFD700>Happy Eid! Have a wonderful celebration with your family!</color>\"",
                speechDetectionText = new string[] {
                    "Happy Eid Have a wonderful celebration with your family",
                    "Happy Eid Have a wonderful celebration",
                    "Happy Eid",
                    "Have a wonderful celebration"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Happy Eid Have a wonderful celebration with your family.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>6. Say what your family does before a festival:</b>\nSay: \"<color=#FFD700>We clean the house and decorate the house.</color>\"",
                speechDetectionText = new string[] {
                    "We clean the house and decorate the house",
                    "We clean the house and decorate it",
                    "We clean the house",
                    "decorate the house"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "We clean the house and decorate the house.mp3")
            }
        };
#endif
    }
}