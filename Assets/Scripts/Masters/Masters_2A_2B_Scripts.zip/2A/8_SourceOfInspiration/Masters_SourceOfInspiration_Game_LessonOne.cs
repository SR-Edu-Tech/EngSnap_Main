using UnityEngine;

/// <summary>
/// Subclass for Unit 8 (Source of Inspiration) Game/Challenge Lesson One:
/// CH01 — The Tongue Twister Challenge (Recording Booth).
/// Inherits 100% of core Recording Booth UI state machine, live microphone recording,
/// waveform visualizer, countdown flow, timer, playback, and LevelManager progression.
/// </summary>
public class Masters_SourceOfInspiration_Game_LessonOne : RecordingBoothController {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Game;
    }
}
