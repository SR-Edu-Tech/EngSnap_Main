using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Subclass for Unit 7: Collocations - Speaking Lesson One (SP01: Say the Pair — Use It in a Sentence).
/// Inherits 100% of the speech recognition, coroutines, phrase card spawning, and UI animations
/// directly from the gold-standard base class `Masters_PolishedCommunication_Speaking_LessonOne`.
/// </summary>
public class Masters_Collocations_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
        InitializeUnit7Prompts();
    }

    private void InitializeUnit7Prompts() {
        if (speechToTextArray != null && speechToTextArray.Length > 0 && speechToTextArray[0] != null &&
            !string.IsNullOrEmpty(speechToTextArray[0].phraseCardText) && speechToTextArray[0].phraseCardText.Contains("ready")) {
            return;
        }

        string audioDir = "Assets/Audio/2A/7_Collocations/Speaking/";
#if UNITY_EDITOR
        speechToTextArray = new SpeechToText[] {
            new SpeechToText {
                phraseCardText = "<b>1. Pair: GET READY</b>\nSay: \"<color=#FFD700>I get ready for school every morning.</color>\"",
                speechDetectionText = new string[] {
                    "I get ready for school every morning",
                    "get ready for school every morning",
                    "I get ready for school",
                    "get ready",
                    "get ready for school"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "I get ready for school every morning.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>2. Pair: GET TOGETHER</b>\nSay: \"<color=#FFD700>We get together on Sundays for dinner.</color>\"",
                speechDetectionText = new string[] {
                    "We get together on Sundays for dinner",
                    "get together on Sundays for dinner",
                    "We get together on Sundays",
                    "get together",
                    "get together for dinner"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "We get together on Sundays for dinner.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>3. Pair: CATCH A COLD</b>\nSay: \"<color=#FFD700>Wear a jacket so you don't catch a cold.</color>\"",
                speechDetectionText = new string[] {
                    "Wear a jacket so you dont catch a cold",
                    "Wear a jacket so you don't catch a cold",
                    "you dont catch a cold",
                    "catch a cold",
                    "Wear a jacket"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Wear a jacket so you dont catch a cold.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>4. Pair: CATCH A BUS</b>\nSay: \"<color=#FFD700>Hurry up or we won't catch a bus.</color>\"",
                speechDetectionText = new string[] {
                    "Hurry up or we wont catch a bus",
                    "Hurry up or we won't catch a bus",
                    "we wont catch a bus",
                    "catch a bus",
                    "Hurry up"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Hurry up or we wont catch a bus.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>5. Pair: SAVE TIME</b>\nSay: \"<color=#FFD700>Taking the shortcut will save time.</color>\"",
                speechDetectionText = new string[] {
                    "Taking the shortcut will save time",
                    "the shortcut will save time",
                    "Taking the shortcut",
                    "save time"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Taking the shortcut will save time.mp3")
            },
            new SpeechToText {
                phraseCardText = "<b>6. Pair: BRIGHT IDEA</b>\nSay: \"<color=#FFD700>She had a bright idea for the science project.</color>\"",
                speechDetectionText = new string[] {
                    "She had a bright idea for the science project",
                    "a bright idea for the science project",
                    "She had a bright idea",
                    "bright idea"
                },
                statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "She had a bright idea for the science project.mp3")
            }
        };
#endif
    }
}