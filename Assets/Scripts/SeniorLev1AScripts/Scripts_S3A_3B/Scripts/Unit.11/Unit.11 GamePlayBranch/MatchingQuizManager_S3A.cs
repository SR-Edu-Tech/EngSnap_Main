using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchingQuizManager_S3A : MonoBehaviour
{
    [System.Serializable]
    public class MatchItem
    {
        [Header("DRAGGABLE OPTION")]
        public RectTransform optionObject;

        [Header("TARGET QUESTION")] 
        public RectTransform targetQuestion;

        [HideInInspector]
        public Vector2 startPosition;

        [HideInInspector]
        public bool completed;

        [HideInInspector]
        public Image image;

        [HideInInspector]
        public CanvasGroup canvasGroup;
    }

    [Header("MATCH ITEMS")]
    public MatchItem[] matchItems;

    [Header("UI")]
    public TMP_Text titleText;

    public RectTransform optionsBG;

    public Button nextButton;

    [Header("AUDIO")]
    public AudioSource audioSource;

    [SerializeField] AudioClip introClip;

    public AudioClip popClip;

    public AudioClip correctAudio;

    public AudioClip wrongAudio;

    public AudioClip completionAudio;

    [Header("COLORS")]
    public Color normalColor = Color.white;

    public Color correctColor = Color.green;

    public Color wrongColor = Color.red;

    [Header("ANIMATION")]
    public float dragScale = 1.08f;

    private MatchItem currentDragging;

    private int completedCount = 0;

    private Vector2 optionsBGOriginalPos;

    void Start()
    {

        if (audioSource != null &&
            introClip != null)
        {
            audioSource.clip =
                introClip;

            audioSource.Play();
        }

    // YOUR OTHER START LOGIC

        nextButton.gameObject.SetActive(false);

        optionsBGOriginalPos =
            optionsBG.anchoredPosition;

        // START BELOW ORIGINAL POSITION
        optionsBG.anchoredPosition =
            optionsBGOriginalPos +
            new Vector2(0, -350f);

        // TITLE POP
        titleText.transform.localScale =
            Vector3.zero;

        LeanTween.scale(
            titleText.gameObject,
            Vector3.one,
            0.4f)
            .setEaseOutBack();

        // OPTIONS BG SLIDE
        LeanTween.move(
            optionsBG,
            optionsBGOriginalPos,
            0.5f)
            .setEaseOutBack();

        // SETUP ITEMS
        for (int i = 0;
            i < matchItems.Length;
            i++)
        {
            matchItems[i].startPosition =
                matchItems[i]
                .optionObject
                .anchoredPosition;

            matchItems[i].image =
                matchItems[i]
                .optionObject
                .GetComponent<Image>();

            matchItems[i].canvasGroup =
                matchItems[i]
                .optionObject
                .GetComponent<CanvasGroup>();

            matchItems[i].completed =
                false;

            if (matchItems[i].image != null)
            {
                matchItems[i].image.color =
                    normalColor;
            }
        }
    }

    public void StartDrag(
    RectTransform draggedObject)
{
    for (int i = 0;
        i < matchItems.Length;
        i++)
    {
        if (matchItems[i].optionObject ==
            draggedObject)
        {
            if (matchItems[i].completed)
                return;

            currentDragging =
                matchItems[i];

            // DISABLE SCROLL WHILE DRAGGING
            ScrollRect scrollRect =
                draggedObject
                .GetComponentInParent<ScrollRect>();

            if (scrollRect != null)
            {
                scrollRect.enabled = false;
            }

            LeanTween.scale(
                draggedObject.gameObject,
                Vector3.one * dragScale,
                0.15f)
                .setEaseOutBack();

            break;
        }
    }
}

    public void EndDrag(
    RectTransform draggedObject)
{
    if (currentDragging == null)
        return;

    bool matched = false;

    // ENABLE SCROLL AGAIN
    ScrollRect scrollRect =
        draggedObject
        .GetComponentInParent<ScrollRect>();

    if (scrollRect != null)
    {
        scrollRect.enabled = true;
    }

    // CHECK TARGET QUESTION
    if (RectTransformUtility
        .RectangleContainsScreenPoint(
            currentDragging.targetQuestion,
            draggedObject.position))
    {
        matched = true;

        CorrectMatch(currentDragging);
    }

    // WRONG
    if (!matched)
    {
        WrongMatch(currentDragging);
    }

    // RESET SCALE
    LeanTween.scale(
        draggedObject.gameObject,
        Vector3.one,
        0.15f);

    currentDragging = null;
}

    void CorrectMatch(
        MatchItem item)
    {
        item.completed = true;

        completedCount++;

        // RETURN TO ORIGINAL POSITION
        LeanTween.move(
            item.optionObject,
            item.startPosition,
            0.25f)
            .setEaseOutBack();

        // GREEN
        if (item.image != null)
        {
            item.image.color =
                correctColor;
        }

        // DISABLE DRAG
        if (item.canvasGroup != null)
        {
            item.canvasGroup.blocksRaycasts =
                false;
        }

        // AUDIO
        if (correctAudio != null)
        {
            audioSource.PlayOneShot(
                correctAudio);
        }

        // COMPLETE
        if (completedCount >=
            matchItems.Length)
        {
            StartCoroutine(
                CompleteRoutine());
        }
    }

    void WrongMatch(
        MatchItem item)
    {
        // RED
        if (item.image != null)
        {
            item.image.color =
                wrongColor;
        }

        // AUDIO
        if (wrongAudio != null)
        {
            audioSource.PlayOneShot(
                wrongAudio);
        }

        // RETURN TO ORIGINAL POSITION
        LeanTween.move(
            item.optionObject,
            item.startPosition,
            0.25f)
            .setEaseOutBack();

        StartCoroutine(
            ResetColor(item));
    }

    IEnumerator ResetColor(
        MatchItem item)
    {
        yield return new WaitForSeconds(
            0.3f);

        if (!item.completed &&
            item.image != null)
        {
            item.image.color =
                normalColor;
        }
    }

    IEnumerator CompleteRoutine()
    {
        yield return new WaitForSeconds(
            0.5f);

        if (completionAudio != null)
        {
            audioSource.PlayOneShot(
                completionAudio);
        }

        nextButton.gameObject.SetActive(true);

        nextButton.transform.localScale =
            Vector3.zero;

        LeanTween.scale(
            nextButton.gameObject,
            Vector3.one,
            0.35f)
            .setEaseOutBack();
    }
}