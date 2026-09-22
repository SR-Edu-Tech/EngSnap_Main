using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Core Listening 1 controller for Book 3A Unit 9: Let's Talk About Future Plans (Hear It — Plan, Wish or When?).
/// Inherits from Masters_BoostSomeoneUp_Listening_LessonOne.
/// Configures 3 bins: PLAN (0), WISH (1), WHEN (2).
/// </summary>
public class Masters_LetsTalkAboutFuturePlans_Listening_LessonOne : Masters_BoostSomeoneUp_Listening_LessonOne {

    protected override void Awake() {
        topic = Masters_Topic.Listening;
        base.Awake();
        ConfigureUnit9SortBins();
    }

    private void ConfigureUnit9SortBins() {
        Masters_UniversalSortBin[] bins = GetComponentsInChildren<Masters_UniversalSortBin>(true);
        if (bins != null) {
            string[] labels = new string[] { "PLAN", "WISH", "WHEN" };
            for (int i = 0; i < bins.Length; i++) {
                if (bins[i] != null) {
                    if (i < labels.Length) {
                        bins[i].gameObject.SetActive(true);
                        bins[i].SetSortId(i);
                        SetBinLabelTextLocally(bins[i], labels[i]);

                        Button binBtn = bins[i].GetButton();
                        if (binBtn != null) {
                            Masters_UniversalSortBin currentBin = bins[i];
                            binBtn.onClick.RemoveAllListeners();
                            binBtn.onClick.AddListener(() => OnSortBinClickedProxy(currentBin));
                        }
                    } else {
                        bins[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void OnSortBinClickedProxy(Masters_UniversalSortBin currentBin) {
        var method = typeof(Masters_BoostSomeoneUp_Listening_LessonOne).GetMethod("OnSortBinClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null) {
            method.Invoke(this, new object[] { currentBin });
        } else {
            Debug.LogError("Could not find OnSortBinClicked via reflection!");
        }
    }

    private void SetBinLabelTextLocally(Masters_UniversalSortBin bin, string text) {
        if (bin == null) return;
        TMP_Text tmp = bin.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) {
            tmp.text = text;
        } else {
            Text legacy = bin.GetComponentInChildren<Text>(true);
            if (legacy != null) legacy.text = text;
        }
    }
}
