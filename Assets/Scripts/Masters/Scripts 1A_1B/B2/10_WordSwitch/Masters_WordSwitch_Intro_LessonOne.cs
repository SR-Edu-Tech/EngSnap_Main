using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Intro Lesson 1 for Unit 10 Word Switch.
/// Supports sequential pop-up cards AND/OR smooth auto-scrolling ScrollRect UX.
/// </summary>
public class Masters_WordSwitch_Intro_LessonOne : Masters_Lesson {

    private const string END_LEVEL = "EndLevel";

    [Header("Word Switch Intro Settings")]
    [SerializeField] private float timeToShowNextButton = 5f;
    [SerializeField] private AudioClip introVoiceOver;

    [Header("Sequential Object Activation (Optional)")]
    [SerializeField] protected GameObject[] objectsToActivateInSequence;
    [SerializeField] protected float initialActivationDelay = 1f;
    [SerializeField] protected float delayBetweenObjects = 0.5f;
    [SerializeField] protected float popUpDuration = 0.4f;

    protected System.Collections.Generic.Dictionary<GameObject, Vector3> originalScales = new System.Collections.Generic.Dictionary<GameObject, Vector3>();

    [Header("Auto Scroll View UX (Optional)")]
    [SerializeField] private ScrollRect autoScrollRect;
    [SerializeField] private RectTransform scrollContent; // Can explicitly assign Content container here
    [SerializeField] private float autoScrollDistance = 1200f; // Exact distance in pixels to scroll upward
    [SerializeField] private float autoScrollDelay = 1.5f;
    [SerializeField] private float autoScrollDuration = 12f;
    [SerializeField] private bool loopInfinite = true;
    [SerializeField] private bool loopYoyo = false; // If true drifts back and forth; if false restarts like a ticker

    protected override void Awake() {
        base.Awake();

        float autoEndLevelTime = 25f;
        Invoke(END_LEVEL, autoEndLevelTime);

        StartCoroutine(NextButtonAnimationCoroutine());

        if (objectsToActivateInSequence != null) {
            foreach (GameObject obj in objectsToActivateInSequence) {
                if (obj != null) {
                    if (!originalScales.ContainsKey(obj)) {
                        originalScales[obj] = obj.transform.localScale;
                    }
                    obj.transform.localScale = Vector3.zero;
                    obj.SetActive(false);
                }
            }
        }

        if (objectsToActivateInSequence != null && objectsToActivateInSequence.Length > 0) {
            StartCoroutine(ActivateObjectsSequentially());
        }

        if (autoScrollRect != null || scrollContent != null) {
            StartCoroutine(AutoScrollCoroutine());
        }

        if (introVoiceOver != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introVoiceOver);
        }
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    protected override void OnNextButtonClicked() {
        EndLevel();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    private void EndLevel() {
        if (topic != Masters_Topic.None) {
            Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        }
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }

    private IEnumerator NextButtonAnimationCoroutine() {
        yield return new WaitForSeconds(timeToShowNextButton);
        NextButtonAnimation();
    }

    protected virtual IEnumerator ActivateObjectsSequentially() {
        yield return new WaitForSeconds(initialActivationDelay);
        for (int i = 0; i < objectsToActivateInSequence.Length; i++) {
            GameObject obj = objectsToActivateInSequence[i];
            if (obj != null) {
                obj.SetActive(true);
                Vector3 targetScale = originalScales.ContainsKey(obj) ? originalScales[obj] : Vector3.one;
                obj.transform.DOScale(targetScale, popUpDuration).SetEase(Ease.OutBack);
            }
            yield return new WaitForSeconds(delayBetweenObjects);
        }
    }

    private IEnumerator AutoScrollCoroutine() {
        yield return new WaitForSeconds(autoScrollDelay);

        // Resolve target content RectTransform
        RectTransform target = scrollContent != null ? scrollContent : (autoScrollRect != null ? autoScrollRect.content : null);

        if (target != null) {
            Vector2 startPos = target.anchoredPosition;
            Vector2 endPos = new Vector2(startPos.x, startPos.y + autoScrollDistance);
            
            var tween = target.DOAnchorPos(endPos, autoScrollDuration).SetEase(Ease.Linear);
            if (loopInfinite) {
                LoopType loopMode = loopYoyo ? LoopType.Yoyo : LoopType.Restart;
                tween.SetLoops(-1, loopMode);
            }
        } else if (autoScrollRect != null) {
            // Fallback to ScrollRect normalized position
            while (true) {
                float elapsed = 0f;
                while (elapsed < autoScrollDuration) {
                    elapsed += Time.deltaTime;
                    float t = elapsed / autoScrollDuration;
                    autoScrollRect.verticalNormalizedPosition = loopYoyo ? Mathf.PingPong(t * 2f, 1f) : Mathf.Lerp(1f, 0f, t);
                    yield return null;
                }
                if (!loopInfinite) break;
            }
        }
    }
}
