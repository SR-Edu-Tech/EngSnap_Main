using UnityEngine;
using System.Collections.Generic;

public class Masters_WaysToExpressHappiness_Game_LessonOne : Masters_BoostSomeoneUp_Game_LessonOne {
    
    protected override void ConfigureBins() {
        if (sortBinArray == null || sortBinArray.Length == 0) return;
        
        Masters_3A_FallingSortCategory[] categories = {
            Masters_3A_FallingSortCategory.Happy,
            Masters_3A_FallingSortCategory.Excited,
            Masters_3A_FallingSortCategory.Tired,
            Masters_3A_FallingSortCategory.Thirsty,
            Masters_3A_FallingSortCategory.Unwell,
            Masters_3A_FallingSortCategory.Sleep
        };
        string[] labels = { "HAPPY", "EXCITED", "TIRED", "THIRSTY", "UNWELL", "SLEEP" };

        // Ensure we have enough bins (Unit 1 only had 3)
        if (sortBinArray.Length < categories.Length) {
            Masters_3A_FallingSortBin[] newArray = new Masters_3A_FallingSortBin[categories.Length];
            for (int i = 0; i < sortBinArray.Length; i++) {
                newArray[i] = sortBinArray[i];
            }
            // Duplicate the last bin to fill the array
            Masters_3A_FallingSortBin templateBin = sortBinArray[sortBinArray.Length - 1];
            for (int i = sortBinArray.Length; i < categories.Length; i++) {
                newArray[i] = Instantiate(templateBin, templateBin.transform.parent);
            }
            sortBinArray = newArray;
        }

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
