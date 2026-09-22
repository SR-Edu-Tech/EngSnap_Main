// Placeholder: the original RecordingBoothController was missing from the project.
// Replace with the real implementation (mic recording, waveform, countdown, playback).
public class RecordingBoothController : Masters_Lesson {
    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
