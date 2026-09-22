using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Masters_WaysToExpressHappiness_Listening_LessonOne : Masters_BoostSomeoneUp_Listening_LessonOne {
    protected override void Awake() {
        base.Awake();
        ConfigureUnit2SortBins();
    }

    private void ConfigureUnit2SortBins() {
        Masters_UniversalSortBin[] bins = GetComponentsInChildren<Masters_UniversalSortBin>(true);
        Debug.Log("ConfigureUnit2SortBins found " + (bins != null ? bins.Length : 0) + " bins.");
        if (bins != null) {
            string[] labels = new string[] { "HAPPY", "EXCITED", "TIRED", "THIRSTY", "UNWELL", "SLEEP" };
            for (int i = 0; i < bins.Length; i++) {
                if (bins[i] != null) {
                    if (i < labels.Length) {
                        Debug.Log($"Activating bin {i} with label {labels[i]}");
                        bins[i].gameObject.SetActive(true);
                        bins[i].SetSortId(i);
                        SetBinLabelTextLocally(bins[i], labels[i]);
                        
                        Button binBtn = bins[i].GetButton();
                        if (binBtn != null) {
                            Masters_UniversalSortBin currentBin = bins[i];
                            binBtn.onClick.RemoveAllListeners();
                            binBtn.onClick.AddListener(() => OnSortBinClickedProxy(currentBin));
                        }

                        // The base script killed the slide animation for bins 3, 4, 5 by calling SetActive(false).
                        // We must restart it here so they don't get stuck at startPosition.
                        RestartSlideAnimation(bins[i]);
                    } else {
                        bins[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void RestartSlideAnimation(Masters_UniversalSortBin bin) {
        Masters_SlideAnimation slide = bin.GetComponent<Masters_SlideAnimation>();
        if (slide != null) {
            slide.StopAllCoroutines();
            var method = typeof(Masters_SlideAnimation).GetMethod("AnimationCoroutine", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null) {
                System.Collections.IEnumerator coroutine = (System.Collections.IEnumerator)method.Invoke(slide, null);
                if (coroutine != null) {
                    slide.StartCoroutine(coroutine);
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
