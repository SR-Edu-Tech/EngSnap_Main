using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core Speaking controller for Unit 3: Beyond the Horizon (Book 2A).
/// Subclasses PolishedCommunication_Speaking_LessonOne.
/// SP01 — Say the Way — Ask & Give Directions Aloud.
/// Manages 6 speaking prompts (3 ASK tasks + 3 GIVE directions tasks).
/// </summary>
public class Masters_BeyondTheHorizon_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
        InitializeUnit3Speaking();
    }

#if UNITY_EDITOR
    private void Reset() {
        InitializeUnit3Speaking();
    }

    private void OnValidate() {
        InitializeUnit3Speaking();
    }
#endif

    private void InitializeUnit3Speaking() {
        // If already customized or initialized with Unit 3 prompts, skip
        if (speechToTextArray != null && speechToTextArray.Length > 0 && speechToTextArray[0] != null && 
            !string.IsNullOrEmpty(speechToTextArray[0].phraseCardText) && speechToTextArray[0].phraseCardText.Contains("restroom")) {
            return;
        }

        speechToTextArray = new SpeechToText[] {
            new SpeechToText {
                phraseCardText = "Excuse me, where is the restroom?",
                speechDetectionText = new string[] { 
                    "Excuse me, where is the restroom?", 
                    "Excuse me where is the restroom", 
                    "where is the restroom" 
                },
                statementAudioClip = LoadAudio("Excuse me where is the restroom")
            },
            new SpeechToText {
                phraseCardText = "Would you mind telling me the way to the Principal's office?",
                speechDetectionText = new string[] { 
                    "Would you mind telling me the way to the Principal's office?", 
                    "Would you mind telling me the way to the Principals office", 
                    "telling me the way to the Principals office" 
                },
                statementAudioClip = LoadAudio("Would you mind telling me the way to the Principals office")
            },
            new SpeechToText {
                phraseCardText = "Could you please tell me how I can get to the admin office?",
                speechDetectionText = new string[] { 
                    "Could you please tell me how I can get to the admin office?", 
                    "Could you please tell me how I can get to the admin office", 
                    "how I can get to the admin office" 
                },
                statementAudioClip = LoadAudio("Could you please tell me how I can get to the admin office")
            },
            new SpeechToText {
                phraseCardText = "Go straight and take a left from the junction. It is beside the school.",
                speechDetectionText = new string[] { 
                    "Go straight and take a left from the junction. It is beside the school.", 
                    "Go straight and take a left from the junction It is beside the school", 
                    "Go straight and take a left from the junction" 
                },
                statementAudioClip = LoadAudio("Go straight and take a left from the junction It is beside the school")
            },
            new SpeechToText {
                phraseCardText = "Go past the park. The clinic is opposite to the post office.",
                speechDetectionText = new string[] { 
                    "Go past the park. The clinic is opposite to the post office.", 
                    "Go past the park The clinic is opposite to the post office", 
                    "Go past the park" 
                },
                statementAudioClip = LoadAudio("Go past the park The clinic is opposite to the post office")
            },
            new SpeechToText {
                phraseCardText = "Walk along the corridor. The store is on your right.",
                speechDetectionText = new string[] { 
                    "Walk along the corridor. The store is on your right.", 
                    "Walk along the corridor The store is on your right", 
                    "Walk along the corridor" 
                },
                statementAudioClip = LoadAudio("Walk along the corridor The store is on your right")
            }
        };
    }

    private AudioClip LoadAudio(string clipName) {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/3_BeyondTheHorizon/Speaking/" + clipName + ".mp3");
#else
        return null;
#endif
    }
}
