using UnityEngine;

/// <summary>
/// Core Reading 2 controller for Unit 4: Code of Conduct (Book 2A).
/// R02 Match — Expression <-> Family: Line-drag matching puzzle across 2 pages (4 items each).
/// Inherits pagination and line matching mechanics from `Masters_ClearConfusion_Reading_LessonTwo`.
/// </summary>
public class Masters_CodeOfConduct_Reading_LessonTwo : Masters_ClearConfusion_Reading_LessonTwo {
    
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
    }
}
