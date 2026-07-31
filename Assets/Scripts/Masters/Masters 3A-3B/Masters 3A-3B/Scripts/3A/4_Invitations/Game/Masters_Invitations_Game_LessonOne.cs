using UnityEngine;
using System.Collections.Generic;

public class Masters_Invitations_Game_LessonOne : Masters_BoostSomeoneUp_Game_LessonOne {
    
    protected override void ConfigureBins() {
        if (sortBinArray == null || sortBinArray.Length == 0) return;
        
        Masters_3A_FallingSortCategory[] categories = {
            Masters_3A_FallingSortCategory.Invite,
            Masters_3A_FallingSortCategory.Accept,
            Masters_3A_FallingSortCategory.Refuse
        };
        string[] labels = { "INVITE", "ACCEPT", "REFUSE" };

        for (int i = 0; i < sortBinArray.Length; i++) {
            if (sortBinArray[i] == null) continue;
            
            if (i < categories.Length) {
                sortBinArray[i].gameObject.SetActive(true);
                sortBinArray[i].ConfigureBin(categories[i], labels[i]);
            } else {
                sortBinArray[i].gameObject.SetActive(false);
            }
        }
    }
}
