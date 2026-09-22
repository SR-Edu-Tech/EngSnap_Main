using UnityEngine;

namespace EngSnap.Masters.Unit5 {

    /// <summary>
    /// Core Speaking 1 controller for Unit 5: Doctor Need Your Help (Book 2B).
    /// SP01 Say How You Feel — 6 Speaking Prompts.
    /// Inherits 100% of speech recognition, phrase card spawning, Levenshtein similarity, and UI flow
    /// directly from the gold-standard base class `Masters_PolishedCommunication_Speaking_LessonOne`.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Speaking;
        }
    }
}
