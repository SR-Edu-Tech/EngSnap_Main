using UnityEngine;

/// <summary>
/// Subclasses Masters_PolishedCommunication_Reading_LessonTwo (Book 2A Polished Communication Reading 2 base).
/// Implements full interactive line-drag matching, level pagination (2 sets of 5 pairs), visual feedback, and audio narration.
/// Content: R02 Match — Statement and Tag (10 verbatim pairs).
/// </summary>
public class Masters_QuestionTags_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

    protected override void Awake() {
        topic = Masters_Topic.Reading;
        base.Awake();
    }
}
