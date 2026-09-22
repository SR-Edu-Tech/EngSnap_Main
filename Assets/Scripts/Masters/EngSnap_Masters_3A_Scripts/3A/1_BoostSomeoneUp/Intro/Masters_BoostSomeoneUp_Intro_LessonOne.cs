using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core Intro controller for Book 3A Unit 1: Boost Someone Up!
/// Exact copy of Book 2 PolishedCommunication_Intro_LessonOne with updated class/field names.
/// Manages sequential presentation of concept cards/street graphics, narrator audio,
/// and dynamic runtime duplication/auto-scrolling of master reference sentences.
/// </summary>
public class Masters_BoostSomeoneUp_Intro_LessonOne : Masters_Lesson {

    private const string END_LEVEL = "EndLevel";

    [Header("Intro Presentation Settings")]
    [SerializeField] private float timeToShowNextButton = 5f;
    [SerializeField] private GameObject[] objectsToActivateInSequence;
    [SerializeField] private float initialActivationDelay = 1f;
    [SerializeField] private float delayBetweenObjects = 0.5f;
    [SerializeField] private float popUpDuration = 0.4f;

    [Header("Floating Animation")]
    [SerializeField] private float floatHeight = 15f;
    [SerializeField] private float floatDuration = 1.5f;

    [Header("Master Reference Bank Sentences")]
    [SerializeField] protected string[] formalSentences = new string[] {
        "You are fantastic!",
        "That was a nice try.",
        "We are a team.",
        "You have the best smile.",
        "Give it your best shot.",
        "We encourage each other.",
        "Could you tell me a little more?",
        "We are all important."
    };

    [SerializeField] protected string[] informalSentences = new string[] {
        "You've almost got it.",
        "Your perspective is refreshing.",
        "You deserve a hug right now.",
        "I like your style.",
        "You have a great sense of humour.",
        "You're strong.",
        "We work hard.",
        "Wow! That is really interesting."
    };

    [Header("Auto-Scroll Settings")]
    [SerializeField] private float autoScrollDelay = 2.0f;
    [SerializeField] private float autoScrollDuration = 12.0f;

    protected override void Awake() {
        base.Awake();

        // Safety timeout
        Invoke(END_LEVEL, 20f);

        StartCoroutine(NextButtonAnimationCoroutine());
        
        if (objectsToActivateInSequence != null) {
            foreach (GameObject obj in objectsToActivateInSequence) {
                if (obj != null) {
                    obj.transform.localScale = Vector3.zero;
                    obj.SetActive(false);
                }
            }
        }

        if (objectsToActivateInSequence != null && objectsToActivateInSequence.Length > 0) {
            StartCoroutine(ActivateObjectsSequentially());
        }
    }

    protected override void Start() {
        base.Start();
        
        // Populate Formal and Informal auto-scrolling reference banks at runtime if present
        if (transform.Find("Formal") != null) PopulateScrollView("Formal", formalSentences);
        if (transform.Find("Informal") != null) PopulateScrollView("Informal", informalSentences);
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    protected override void OnNextButtonClicked() {
        EndLevel();
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    private void EndLevel() {
        if (topic == Masters_Topic.None) {
            Debug.LogWarning($"Topic not set for {this.name}!");
            return;
        }
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private IEnumerator NextButtonAnimationCoroutine() {
        yield return new WaitForSeconds(timeToShowNextButton);
        NextButtonAnimation();
    }

    private IEnumerator ActivateObjectsSequentially() {
        yield return new WaitForSeconds(initialActivationDelay);
        
        if (objectsToActivateInSequence != null) {
            foreach (GameObject obj in objectsToActivateInSequence) {
                if (obj != null) {
                    obj.SetActive(true);
                    obj.transform.DOScale(Vector3.one, popUpDuration).SetEase(Ease.OutBack).OnComplete(() => {
                        if (obj != null) {
                            float startY = obj.transform.localPosition.y;
                            obj.transform.DOLocalMoveY(startY + floatHeight, floatDuration)
                                .SetEase(Ease.InOutSine)
                                .SetLoops(-1, LoopType.Yoyo);
                        }
                    });
                }
                yield return new WaitForSeconds(delayBetweenObjects);
            }
        }
    }

    private void PopulateScrollView(string scrollRectName, string[] sentences) {
        Transform scrollRectTrans = transform.Find(scrollRectName);
        if (scrollRectTrans == null) {
            scrollRectTrans = FindChildRecursive(transform, scrollRectName);
        }

        if (scrollRectTrans == null) {
            Debug.LogWarning($"Could not find ScrollView named '{scrollRectName}' in Intro prefab.");
            return;
        }

        ScrollRect scrollRect = scrollRectTrans.GetComponent<ScrollRect>();

        Transform contentTrans = null;
        if (scrollRect != null && scrollRect.content != null) {
            contentTrans = scrollRect.content;
        } else {
            contentTrans = FindChildRecursive(scrollRectTrans, "Content");
        }

        if (contentTrans == null) {
            Debug.LogWarning($"Could not find 'Content' transform under '{scrollRectName}'.");
            return;
        }

        // Ensure Content has a ContentSizeFitter so it expands vertically to fit all duplicated items
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
        templateObj.SetActive(false); // Hide the template item

        // Duplicate for each sentence in the reference bank
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
            StartCoroutine(AutoScrollCoroutine(scrollRect));
        }
    }

    private new Transform FindChildRecursive(Transform parent, string targetName) {
        foreach (Transform child in parent) {
            if (child.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase)) {
                return child;
            }
            Transform found = FindChildRecursive(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private IEnumerator AutoScrollCoroutine(ScrollRect scrollRect) {
        while (scrollRect != null && !scrollRect.gameObject.activeInHierarchy) {
            yield return null;
        }

        yield return new WaitForSeconds(autoScrollDelay);
        
        if (scrollRect == null || scrollRect.content == null) yield break;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);

        scrollRect.velocity = Vector2.zero;
        scrollRect.inertia = false;

        while (true) {
            float elapsed = 0f;
            scrollRect.verticalNormalizedPosition = 1f;
            
            while (elapsed < autoScrollDuration) {
                if (scrollRect == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / autoScrollDuration);
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
