using UnityEngine;

public class Masters_Invitations_Writing_LessonTwo : Masters_BoostSomeoneUp_Writing_LessonTwo
{
    protected override void LoadPrompt(int index) {
        base.LoadPrompt(index);
        
        if (studentInputField != null) {
            studentInputField.readOnly = false;
        }
    }
}
