using UnityEngine;

/// <summary>
/// Core Game G01 controller for Book 3A Unit 10: Stop Apologizing.
/// Inherits from Masters_BoostSomeoneUp_Game_LessonOne.
/// </summary>
public class Masters_StopApologizingStartThanking_Game_LessonOne : Masters_BoostSomeoneUp_Game_LessonOne {
    protected override void ConfigureBins() {
        if (sortBinArray == null) return;
        
        Masters_3A_FallingSortCategory[] categories = {
            Masters_3A_FallingSortCategory.Reframe,
            Masters_3A_FallingSortCategory.Praise,
            Masters_3A_FallingSortCategory.Complain,
            Masters_3A_FallingSortCategory.Respond
        };
        string[] labels = { "Reframe", "Praise", "Complain", "Respond" };

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
