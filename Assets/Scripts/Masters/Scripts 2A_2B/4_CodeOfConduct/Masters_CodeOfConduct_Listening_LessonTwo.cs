using UnityEngine;

/// <summary>
/// Core Listening 2 controller for Unit 4: Code of Conduct (Book 2A).
/// Inherits from `Masters_PolishedCommunication_Roleplay_LessonOne` to provide a two-character interaction:
/// Friend A speaks a `THANK YOU` line (`npcAudioClip`), student picks a `YOU'RE WELCOME` reply chip,
/// and Friend B speaks the natural reply (`correctOptionAudioClip`) in `studentCloud`.
/// </summary>
public class Masters_CodeOfConduct_Listening_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonOne {
    
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;
    }
}
