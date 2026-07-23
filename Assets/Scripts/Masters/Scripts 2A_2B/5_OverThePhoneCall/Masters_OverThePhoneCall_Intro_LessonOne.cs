using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core Intro controller for Unit 5: Over the Phone Call (Book 2A).
/// Inherits presentation and animation from Book 2A reference base (`Masters_PolishedCommunication_Intro_LessonOne`),
/// while overriding the auto-scrolling viewports (`Formal` and `Informal`) with Unit 5 Master Verbatim phone phrases.
/// </summary>
public class Masters_OverThePhoneCall_Intro_LessonOne : Masters_PolishedCommunication_Intro_LessonOne {

    [Header("Unit 5 Master Verbatim Phone Phrases")]
    [SerializeField] private string[] unit5FormalSentences = new string[] {
        "Good morning! This is Rosy Speaking.",
        "May I ask who's calling, please?",
        "Could you hold on a moment, please?",
        "Thank you for calling.",
        "Could you put me through to Jane?",
        "I'm calling on behalf of Mr. Sharma.",
        "Could you please speak a little louder?",
        "I'm afraid the line is quite bad.",
        "Would you like to leave a message?",
        "Thank you for your assistance. Goodbye!"
    };

    [SerializeField] private string[] unit5InformalSentences = new string[] {
        "Hi! It's Vinay here.",
        "Is Anitha there?",
        "Who is this?",
        "Just a minute.",
        "Sorry, I didn't catch that.",
        "Hang on a sec!",
        "Can you speak up? It's noisy here.",
        "You're breaking up!",
        "I'll call you right back.",
        "Catch you later, bye!"
    };

    [Header("Auto-Scroll Settings")]
    [SerializeField] private float scrollDelay = 2.0f;
    [SerializeField] private float scrollDuration = 12.0f;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Intro;
    }

    protected override void Start() {
        base.Start();

        // Override scrollviews with Unit 5 verbatim phone phrases
        if (transform.Find("Formal") != null) PopulateScrollViewUnit5("Formal", unit5FormalSentences);
        else if (transform.Find("ScrollView_Formal") != null) PopulateScrollViewUnit5("ScrollView_Formal", unit5FormalSentences);

        if (transform.Find("Informal") != null) PopulateScrollViewUnit5("Informal", unit5InformalSentences);
        else if (transform.Find("ScrollView_Informal") != null) PopulateScrollViewUnit5("ScrollView_Informal", unit5InformalSentences);
    }

    private void PopulateScrollViewUnit5(string scrollRectName, string[] sentences) {
        Transform scrollRectTrans = transform.Find(scrollRectName);
        if (scrollRectTrans == null) {
            scrollRectTrans = FindChildRecursiveUnit5(transform, scrollRectName);
        }

        if (scrollRectTrans == null) {
            Debug.LogWarning($"Could not find ScrollView named '{scrollRectName}' in Unit 5 Intro prefab.");
            return;
        }

        ScrollRect scrollRect = scrollRectTrans.GetComponent<ScrollRect>();

        Transform contentTrans = null;
        if (scrollRect != null && scrollRect.content != null) {
            contentTrans = scrollRect.content;
        } else {
            contentTrans = FindChildRecursiveUnit5(scrollRectTrans, "Content");
        }

        if (contentTrans == null) {
            Debug.LogWarning($"Could not find 'Content' transform under '{scrollRectName}'.");
            return;
        }

        ContentSizeFitter fitter = contentTrans.GetComponent<ContentSizeFitter>();
        if (fitter == null) {
            fitter = contentTrans.gameObject.AddComponent<ContentSizeFitter>();
        }
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        Transform templateTrans = null;
        if (contentTrans.childCount > 0) {
            templateTrans = contentTrans.GetChild(0);
        }

        if (templateTrans == null) {
            Debug.LogWarning($"No template UI item found inside '{scrollRectName}/Viewport/Content'.");
            return;
        }

        GameObject templateObj = templateTrans.gameObject;
        templateObj.SetActive(false);

        // Remove any old items instantiated by base.Start() so we only display Unit 5 phrases
        for (int i = contentTrans.childCount - 1; i >= 1; i--) {
            DestroyImmediate(contentTrans.GetChild(i).gameObject);
        }

        // Populate exact Unit 5 Master Verbatim sentences
        foreach (string sentence in sentences) {
            GameObject newItem = Instantiate(templateObj, contentTrans, false);
            newItem.name = $"Item_{sentence}";
            newItem.transform.localScale = Vector3.one;
            newItem.transform.localPosition = Vector3.zero;
            newItem.transform.localRotation = Quaternion.identity;
            newItem.SetActive(true);

            TMP_Text tmpText = newItem.GetComponent<TMP_Text>();
            if (tmpText == null) tmpText = newItem.GetComponentInChildren<TMP_Text>();

            if (tmpText != null) {
                tmpText.text = sentence;
            } else {
                Text legacyText = newItem.GetComponent<Text>();
                if (legacyText == null) legacyText = newItem.GetComponentInChildren<Text>();
                if (legacyText != null) legacyText.text = sentence;
            }
        }

        if (scrollRect != null) {
            StartCoroutine(AutoScrollUnit5Coroutine(scrollRect));
        }
    }

    private Transform FindChildRecursiveUnit5(Transform parent, string targetName) {
        foreach (Transform child in parent) {
            if (child.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase)) {
                return child;
            }
            Transform found = FindChildRecursiveUnit5(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private IEnumerator AutoScrollUnit5Coroutine(ScrollRect scrollRect) {
        while (scrollRect != null && !scrollRect.gameObject.activeInHierarchy) {
            yield return null;
        }

        yield return new WaitForSeconds(scrollDelay);

        if (scrollRect == null || scrollRect.content == null) yield break;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

        scrollRect.velocity = Vector2.zero;
        scrollRect.inertia = false;

        while (true) {
            float elapsed = 0f;
            scrollRect.verticalNormalizedPosition = 1f;

            while (elapsed < scrollDuration) {
                if (scrollRect == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / scrollDuration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(1f, 0f, easedT);
                yield return null;
            }

            scrollRect.verticalNormalizedPosition = 0f;
            yield return new WaitForSeconds(3f);

            elapsed = 0f;
            while (elapsed < 3f) {
                if (scrollRect == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 3f);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                scrollRect.verticalNormalizedPosition = Mathf.Lerp(0f, 1f, easedT);
                yield return null;
            }

            scrollRect.verticalNormalizedPosition = 1f;
            yield return new WaitForSeconds(2f);
        }
    }
}
