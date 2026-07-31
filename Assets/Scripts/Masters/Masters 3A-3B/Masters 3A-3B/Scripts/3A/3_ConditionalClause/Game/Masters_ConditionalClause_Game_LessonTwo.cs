using UnityEngine;

public class Masters_ConditionalClause_Game_LessonTwo : Masters_BoostSomeoneUp_Game_LessonOne {
    protected override void ConfigureBins() {
        if (sortBinArray == null) return;
        
        // Define the 2 required categories
        Masters_3A_FallingSortCategory[] categories = {
            Masters_3A_FallingSortCategory.PresentSimple,
            Masters_3A_FallingSortCategory.Will
        };
        string[] labels = { "Present Simple", "Will" };

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
