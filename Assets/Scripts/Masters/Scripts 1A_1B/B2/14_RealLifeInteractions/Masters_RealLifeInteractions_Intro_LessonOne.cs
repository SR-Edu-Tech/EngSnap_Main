using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

[System.Serializable]
public class Masters_SceneScrollViewData {
    public string sceneName;
    public ScrollRect scrollRect;
    public RectTransform scrollContent;
    public float scrollDistance = 1200f;
    public float scrollDuration = 14f;
    public float scrollDelay = 1.5f;
    public bool loopYoyo = true;
}

/// <summary>
/// Intro Lesson 1 for Unit 14 Real Life Interactions.
/// Inherits from WordSwitch Intro template and adds multi-scene simultaneous auto-scrolling
/// for all 3 scene scrollviews (Ordering Food / Restaurant, School Time / Home, At the Clinic).
/// </summary>
public class Masters_RealLifeInteractions_Intro_LessonOne : Masters_WordSwitch_Intro_LessonOne {

    [Header("Multi-Scene Auto Scroll Settings")]
    public Masters_SceneScrollViewData[] sceneScrollViews;

    protected override void Awake() {
        // Disable base autoScrollRect via reflection before base.Awake() runs so base doesn't double-animate
        var baseScrollProp = typeof(Masters_WordSwitch_Intro_LessonOne).GetField("autoScrollRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (baseScrollProp != null) {
            baseScrollProp.SetValue(this, null);
        }
        var baseContentProp = typeof(Masters_WordSwitch_Intro_LessonOne).GetField("scrollContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (baseContentProp != null) {
            baseContentProp.SetValue(this, null);
        }

        base.Awake();

        // Start multi-scene auto scrolling
        if (sceneScrollViews != null && sceneScrollViews.Length > 0) {
            foreach (var s in sceneScrollViews) {
                if (s != null && (s.scrollRect != null || s.scrollContent != null)) {
                    StartCoroutine(RunCustomAutoScroll(s));
                }
            }
        } else {
            // Fallback: auto-detect all ScrollRects in children
            ScrollRect[] allScrolls = GetComponentsInChildren<ScrollRect>(true);
            foreach (var sr in allScrolls) {
                if (sr != null) {
                    Masters_SceneScrollViewData autoData = new Masters_SceneScrollViewData {
                        sceneName = sr.gameObject.name,
                        scrollRect = sr,
                        scrollContent = sr.content,
                        scrollDistance = 1200f,
                        scrollDuration = 14f,
                        scrollDelay = 1.5f,
                        loopYoyo = true
                    };
                    StartCoroutine(RunCustomAutoScroll(autoData));
                }
            }
        }
    }

    private IEnumerator RunCustomAutoScroll(Masters_SceneScrollViewData s) {
        yield return new WaitForSeconds(s.scrollDelay);

        RectTransform target = s.scrollContent != null ? s.scrollContent : (s.scrollRect != null ? s.scrollRect.content : null);

        if (target != null && s.scrollDistance > 0) {
            Vector2 startPos = target.anchoredPosition;
            Vector2 endPos = new Vector2(startPos.x, startPos.y + s.scrollDistance);
            
            var tween = target.DOAnchorPos(endPos, s.scrollDuration).SetEase(Ease.Linear);
            tween.SetLoops(-1, s.loopYoyo ? LoopType.Yoyo : LoopType.Restart);
        } else if (s.scrollRect != null) {
            while (true) {
                float elapsed = 0f;
                while (elapsed < s.scrollDuration) {
                    elapsed += Time.deltaTime;
                    float t = elapsed / s.scrollDuration;
                    s.scrollRect.verticalNormalizedPosition = s.loopYoyo ? Mathf.PingPong(t * 2f, 1f) : Mathf.Lerp(1f, 0f, t);
                    yield return null;
                }
            }
        }
    }
}
