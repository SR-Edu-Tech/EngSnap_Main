using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategorizedCards_S3A : MonoBehaviour
{
    [System.Serializable]
    public class QuestionItem
    {
        public Button questionButton;
        public TMP_Text questionText;

        public AudioClip audio;
        public AudioClip slowAudio;

        [HideInInspector] public Vector3 originalScale;
        [HideInInspector] public bool played;
    }

    [System.Serializable]
    public class CategoryData
    {
        public string categoryName;

        [Header("Category Buttons")]
        public Button unexpandedButton;
        public Button expandedButton;

        [Header("Expanded Content")]
        public RectTransform contentPanel;

        [Header("Questions")]
        public QuestionItem[] questions;

        [HideInInspector] public bool isCompleted;
    }

    [Header("UI Title")]
    public TMP_Text titleText;

    [Header("Board Slide")]
    public RectTransform categoryBoard;
    public Vector2 boardStartPos = new Vector2(-1500f, 0f);
    public Vector2 boardEndPos = Vector2.zero;
    public float boardSlideSpeed = 4f;

    [Header("Category Colors")]
    public Color categoryNormalColor = Color.white;
    public Color categorySelectedColor = new Color(0.8f, 1f, 0.8f);
    public Color categoryVisitedColor = Color.gray;
    public Color categoryVisitedTextColor = Color.white;

    [Header("Shared UI")]
    public GameObject sharedBottomBoard;
    public GameObject nextButton;

    [Header("Controls")]
    public Button slowButton;
    public Button repeatButton;
    public Image slowBG;
    public Image repeatBG;
    public Color activeToggleColor = Color.green;

    [Header("Categories")]
    public CategoryData[] categories;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip introAudio;
    public AudioClip popSfx;

    [Header("Animation")]
    public float categoryPopSpeed = 4f;
    public float questionPopSpeed = 2f;
    public float staggerDelay = 0.3f;

    [Header("Colors")]
    public Color normalTextColor = Color.black;
    public Color playingTextColor = new Color(1f, 0.8f, 0.25f);

    private bool isSlowOn = false;
    private bool isRepeatOn = false;

    private Coroutine pulseRoutine;

    private bool canInteract;
    private bool isSequencePlaying;

    void OnEnable()
    {
        ResetUI();

        SetupButtons();

        StartCoroutine(IntroFlow());
    }

    void OnDisable()
    {
        StopAllCoroutines();

        if (audioSource)
            audioSource.Stop();
    }

    void ResetUI()
    {
        canInteract = false;
        isSequencePlaying = false;

        if (categoryBoard != null)
        {
            categoryBoard.anchoredPosition = boardStartPos;
        }

        if (sharedBottomBoard != null)
            sharedBottomBoard.SetActive(false);

        if (nextButton != null)
            nextButton.SetActive(false);

        isSlowOn = false;
        isRepeatOn = false;

        if (slowBG != null)
            slowBG.color = categoryNormalColor;

        if (repeatBG != null)
            repeatBG.color = categoryNormalColor;

        foreach (var category in categories)
        {
            category.contentPanel.gameObject.SetActive(false);
            category.isCompleted = false;

            if (category.unexpandedButton != null)
            {
                category.unexpandedButton.transform.localScale = Vector3.zero;

                Image img = category.unexpandedButton.GetComponent<Image>();

                if (img != null)
                    img.color = categoryNormalColor;

                category.unexpandedButton.interactable = true;
            }

            if (category.expandedButton != null)
            {
                category.expandedButton.transform.localScale = Vector3.one;

                Image img = category.expandedButton.GetComponent<Image>();

                if (img != null)
                    img.color = categoryNormalColor;

                category.expandedButton.interactable = true;
            }

            foreach (var q in category.questions)
            {
                q.originalScale = Vector3.one;
                q.played = false;

                q.questionButton.transform.localScale = Vector3.one;

                q.questionText.color = normalTextColor;

                Image qImg = q.questionButton.GetComponent<Image>();

                if (qImg != null)
                    qImg.color = categoryNormalColor;
            }
        }
    }

    void SetupButtons()
    {
        if (slowButton != null)
        {
            slowButton.onClick.RemoveAllListeners();
            slowButton.onClick.AddListener(ToggleSlow);
        }

        if (repeatButton != null)
        {
            repeatButton.onClick.RemoveAllListeners();
            repeatButton.onClick.AddListener(ToggleRepeat);
        }

        foreach (var category in categories)
        {
            CategoryData capturedCategory = category;

            if (category.unexpandedButton != null)
            {
                category.unexpandedButton.onClick.RemoveAllListeners();

                category.unexpandedButton.onClick.AddListener(() =>
                {
                    if (canInteract && !isSequencePlaying)
                        OpenCategory(capturedCategory);
                });
            }

            if (category.expandedButton != null)
            {
                category.expandedButton.onClick.RemoveAllListeners();

                category.expandedButton.onClick.AddListener(() =>
                {
                    if (canInteract && !isSequencePlaying)
                        OpenCategory(capturedCategory);
                });
            }
        }
    }

    IEnumerator IntroFlow()
    {
        if (introAudio)
        {
            audioSource.clip = introAudio;
            audioSource.Play();
        }

        yield return new WaitForSeconds(0.5f);

        if (categoryBoard != null)
        {
            yield return StartCoroutine(SlideInBoard());
        }

        yield return ShowCategoryButtons();

        canInteract = true;
    }

    IEnumerator ShowCategoryButtons()
    {
        foreach (var category in categories)
        {
            if (category.unexpandedButton != null)
            {
                StartCoroutine(PopUI(category.unexpandedButton.transform, categoryPopSpeed));

                if (popSfx)
                    audioSource.PlayOneShot(popSfx);

                yield return new WaitForSeconds(staggerDelay);
            }
        }
    }

    IEnumerator SlideInBoard()
    {
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * boardSlideSpeed;

            float clamped = Mathf.Clamp01(time);

            float ease = 1f - Mathf.Pow(1f - clamped, 3f);

            categoryBoard.anchoredPosition =
                Vector2.Lerp(boardStartPos, boardEndPos, ease);

            yield return null;
        }

        categoryBoard.anchoredPosition = boardEndPos;
    }

    void OpenCategory(CategoryData selectedCategory)
    {
        if (isSequencePlaying)
            return;

        foreach (var category in categories)
        {
            category.contentPanel.gameObject.SetActive(false);

            Color targetColor =
                category.isCompleted ? categoryVisitedColor : categoryNormalColor;

            if (category.unexpandedButton != null)
            {
                Image img = category.unexpandedButton.GetComponent<Image>();

                if (img != null)
                    img.color = targetColor;
            }

            if (category.expandedButton != null)
            {
                Image img = category.expandedButton.GetComponent<Image>();

                if (img != null)
                    img.color = targetColor;
            }
        }

        selectedCategory.contentPanel.gameObject.SetActive(true);

        if (sharedBottomBoard != null)
            sharedBottomBoard.SetActive(true);

        if (selectedCategory.unexpandedButton != null)
        {
            Image img = selectedCategory.unexpandedButton.GetComponent<Image>();

            if (img != null)
                img.color = categorySelectedColor;
        }

        if (selectedCategory.expandedButton != null)
        {
            Image img = selectedCategory.expandedButton.GetComponent<Image>();

            if (img != null)
                img.color = categorySelectedColor;
        }

        StartCoroutine(PlayQuestionSequence(selectedCategory));
    }

    IEnumerator PlayQuestionSequence(CategoryData category)
    {
        isSequencePlaying = true;
        canInteract = false;

        foreach (var cat in categories)
        {
            if (cat.unexpandedButton != null)
                cat.unexpandedButton.interactable = false;

            if (cat.expandedButton != null)
                cat.expandedButton.interactable = false;
        }

        foreach (var q in category.questions)
        {
            yield return StartCoroutine(PlayRoutine(q));

            yield return new WaitForSeconds(0.2f);
        }

        category.isCompleted = true;

        if (category.unexpandedButton != null)
            category.unexpandedButton.interactable = false;

        if (category.expandedButton != null)
            category.expandedButton.interactable = false;

        foreach (var cat in categories)
        {
            if (!cat.isCompleted)
            {
                if (cat.unexpandedButton != null)
                    cat.unexpandedButton.interactable = true;

                if (cat.expandedButton != null)
                    cat.expandedButton.interactable = true;
            }
        }

        isSequencePlaying = false;
        canInteract = true;

        CheckAllCategoriesVisited();
    }

    IEnumerator PlayRoutine(QuestionItem q)
    {
        q.questionText.color = playingTextColor;

        pulseRoutine =
            StartCoroutine(PulseWhilePlaying(q.questionButton.transform));

        AudioClip clipToPlay =
            (isSlowOn && q.slowAudio != null) ? q.slowAudio : q.audio;

        if (clipToPlay != null)
        {
            audioSource.clip = clipToPlay;
            audioSource.loop = false;
            audioSource.Play();

            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        q.played = true;

        ResetVisual(q);
    }

    void CheckAllCategoriesVisited()
    {
        foreach (var cat in categories)
        {
            if (!cat.isCompleted)
                return;
        }

        if (nextButton != null)
            nextButton.SetActive(true);

        // Replay allowed after both finished
        foreach (var cat in categories)
        {
            if (cat.unexpandedButton != null)
                cat.unexpandedButton.interactable = true;

            if (cat.expandedButton != null)
                cat.expandedButton.interactable = true;
        }
    }

    void ToggleSlow()
    {
        if (!canInteract)
            return;

        isSlowOn = !isSlowOn;

        if (slowBG != null)
            slowBG.color =
                isSlowOn ? activeToggleColor : categoryNormalColor;
    }

    void ToggleRepeat()
    {
        if (!canInteract)
            return;

        isRepeatOn = !isRepeatOn;

        if (repeatBG != null)
            repeatBG.color =
                isRepeatOn ? activeToggleColor : categoryNormalColor;
    }

    void ResetVisual(QuestionItem q)
    {
        q.questionText.color = normalTextColor;

        Image img = q.questionButton.GetComponent<Image>();

        if (img != null)
            img.color =
                q.played ? categoryVisitedColor : categoryNormalColor;

        q.questionButton.transform.localScale = Vector3.one;
    }

    IEnumerator PopUI(Transform t, float speed)
    {
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * speed;

            float clamped = Mathf.Clamp01(time);

            float overshoot = 1.70158f;
            float c1 = overshoot + 1f;

            float ease =
                1f +
                c1 * Mathf.Pow(clamped - 1f, 3f) +
                overshoot * Mathf.Pow(clamped - 1f, 2f);

            t.localScale = Vector3.one * ease;

            yield return null;
        }

        t.localScale = Vector3.one;
    }

    IEnumerator PulseWhilePlaying(Transform t)
    {
        while (audioSource.isPlaying)
        {
            float scale =
                1f + Mathf.Sin(Time.time * 8f) * 0.05f;

            t.localScale = Vector3.one * scale;

            yield return null;
        }

        t.localScale = Vector3.one;
    }
}