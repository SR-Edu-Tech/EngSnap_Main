using UnityEngine;

/// <summary>
/// Game Lesson One for Unit 5.
/// Inherits from Masters_BoostSomeoneUp_Game_LessonOne (Drag and Drop Sorter).
/// </summary>
public class Masters_Ask_Game_LessonOne : Masters_BoostSomeoneUp_Game_LessonOne {
    protected override void ConfigureBins() {
        if (sortBinArray == null || sortBinArray.Length == 0) return;
        
        string[] labels = { 
            "Ask a favour", "Ask about", "Ask for directions", "Ask permission",
            "Ask for an advice", "Ask a question", "Ask if / whether", "Ask for something"
        };

        // If we don't have enough bins in the array, spawn them!
        if (sortBinArray.Length < labels.Length) {
            Masters_3A_FallingSortBin templateBin = sortBinArray[0];
            Transform parentTransform = templateBin.transform.parent;
            
            Masters_3A_FallingSortBin[] newArray = new Masters_3A_FallingSortBin[labels.Length];
            for (int i = 0; i < sortBinArray.Length; i++) {
                newArray[i] = sortBinArray[i];
            }
            
            for (int i = sortBinArray.Length; i < labels.Length; i++) {
                Masters_3A_FallingSortBin newBin = Instantiate(templateBin, parentTransform);
                newBin.name = templateBin.name.Replace("1", (i + 1).ToString());
                newArray[i] = newBin;
            }
            sortBinArray = newArray;
        }

        for (int i = 0; i < sortBinArray.Length; i++) {
            if (sortBinArray[i] == null) continue;
            
            if (i < labels.Length) {
                sortBinArray[i].gameObject.SetActive(true);
                sortBinArray[i].ConfigureBin((Masters_3A_FallingSortCategory)i, labels[i]);
            } else {
                sortBinArray[i].gameObject.SetActive(false);
            }
        }
    }
}
