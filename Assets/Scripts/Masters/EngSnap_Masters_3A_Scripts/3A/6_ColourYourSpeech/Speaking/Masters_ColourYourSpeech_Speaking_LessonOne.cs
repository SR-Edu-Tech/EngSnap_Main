using UnityEngine;

/// <summary>
/// Core controller for Unit 6: Colour Your Speech - Speaking Lesson One (SP01).
/// Inherits directly from the canonical Book 3A Speaking controller (Masters_BoostSomeoneUp_Speaking_LessonOne).
/// Manages speech recognition via Levenshtein distance (> 0.75), accuracy slider feedback,
/// phrase card carousel lifecycle, and Unit 6 Hub progression integration.
/// </summary>
public class Masters_ColourYourSpeech_Speaking_LessonOne : Masters_BoostSomeoneUp_Speaking_LessonOne
{
    protected override void Awake()
    {
        base.Awake();
        topic = Masters_Topic.Speaking;
    }

    protected override void OnNextButtonClicked()
    {
        // Mark Unit 6 SP01 Complete in Hub progress
        M3A_U6_HubProgress.MarkSP01Complete();

        base.OnNextButtonClicked();
    }
}
