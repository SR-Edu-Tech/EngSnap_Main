using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CategorizeMultiple_S3A : MonoBehaviour
{
    [System.Serializable]
    public class PhraseItem
    {
        public Button phraseButton;
        public TMP_Text phraseText;

        public AudioClip audio;
        public AudioClip slowAudio;

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

        [Header("Phrase Cards")]
        public PhraseItem[] phrases;

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
            categoryBoard.anchoredPosition = boardStartPos;

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
                category.unexpandedButton.interactable = true;
            }

            if (category.expandedButton != null)
            {
                category.expandedButton.interactable = true;
            }

            foreach (var p in category.phrases)
            {
                p.played = false;

                p.phraseButton.transform.localScale = Vector3.one;
                p.phraseText.color = normalTextColor;

                Image img = p.phraseButton.GetComponent<Image>();

                if (img != null)
                    img.color = categoryNormalColor;
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
            CategoryData captured = category;

            category.unexpandedButton.onClick.RemoveAllListeners();
            category.unexpandedButton.onClick.AddListener(() =>
            {
                if (canInteract && !isSequencePlaying)
                    OpenCategory(captured);
            });

            category.expandedButton.onClick.RemoveAllListeners();
            category.expandedButton.onClick.AddListener(() =>
            {
                if (canInteract && !isSequencePlaying)
                    OpenCategory(captured);
            });
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

        yield return StartCoroutine(SlideInBoard());

        yield return ShowCategoryButtons();

        canInteract = true;
    }

    IEnumerator ShowCategoryButtons()
    {
        foreach (var category in categories)
        {
            StartCoroutine(PopUI(category.unexpandedButton.transform, categoryPopSpeed));

            if (popSfx)
                audioSource.PlayOneShot(popSfx);

            yield return new WaitForSeconds(staggerDelay);
        }
    }

    IEnumerator SlideInBoard()
    {
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * boardSlideSpeed;

            float ease = 1f - Mathf.Pow(1f - time, 3f);

            categoryBoard.anchoredPosition =
                Vector2.Lerp(boardStartPos, boardEndPos, ease);

            yield return null;
        }

        categoryBoard.anchoredPosition = boardEndPos;
    }

    void OpenCategory(CategoryData selected)
{
    foreach (var category in categories)
    {
        category.contentPanel.gameObject.SetActive(false);
    }

    // HIDE CATEGORY BOARD
    if (categoryBoard != null)
        categoryBoard.gameObject.SetActive(false);

    selected.contentPanel.gameObject.SetActive(true);

    if (sharedBottomBoard != null)
        sharedBottomBoard.SetActive(true);

    StartCoroutine(PlayPhraseSequence(selected));
}

    IEnumerator PlayPhraseSequence(CategoryData category)
    {
        isSequencePlaying = true;
        canInteract = false;

        foreach (var p in category.phrases)
        {
            yield return StartCoroutine(PlayRoutine(p));
            yield return new WaitForSeconds(0.2f);
        }

        category.isCompleted = true;

        isSequencePlaying = false;
        canInteract = true;

        CheckAllCategoriesVisited();
    }

    IEnumerator PlayRoutine(PhraseItem p)
    {
        p.phraseText.color = playingTextColor;

        pulseRoutine =
            StartCoroutine(PulseWhilePlaying(p.phraseButton.transform));

        AudioClip clipToPlay =
            (isSlowOn && p.slowAudio != null)
            ? p.slowAudio
            : p.audio;

        if (clipToPlay != null)
        {
            audioSource.clip = clipToPlay;
            audioSource.Play();

            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        p.played = true;

        ResetVisual(p);
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

    void ResetVisual(PhraseItem p)
    {
        p.phraseText.color = normalTextColor;

        Image img = p.phraseButton.GetComponent<Image>();

        if (img != null)
            img.color =
                p.played ? categoryVisitedColor : categoryNormalColor;

        p.phraseButton.transform.localScale = Vector3.one;
    }

    IEnumerator PopUI(Transform t, float speed)
    {
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * speed;

            float overshoot = 1.70158f;
            float c1 = overshoot + 1f;

            float ease =
                1f +
                c1 * Mathf.Pow(time - 1f, 3f) +
                overshoot * Mathf.Pow(time - 1f, 2f);

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
