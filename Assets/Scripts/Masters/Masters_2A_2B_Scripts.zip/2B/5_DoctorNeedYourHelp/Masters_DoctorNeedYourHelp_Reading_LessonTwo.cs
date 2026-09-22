using UnityEngine;

namespace EngSnap.Masters.Unit5 {

    /// <summary>
    /// Core Reading 2 controller for Unit 5: Doctor Need Your Help (Book 2B).
    /// R02 Match — Symptom and Body Part: 10 verbatim symptom sentences on left, body parts on right.
    /// Pairs symptom sentences to body parts across 2 sets (5 pairs each).
    /// Inherits full line-drag and node matching architecture from `Masters_ClearConfusion_Reading_LessonTwo`.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Reading_LessonTwo : Masters_ClearConfusion_Reading_LessonTwo {

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;
        }

        public new void InitializePuzzlesIfEmpty() {
            // Can be initialized with 10 symptom-to-body-part pairs
            base.InitializePuzzlesIfEmpty();
        }
    }
}
