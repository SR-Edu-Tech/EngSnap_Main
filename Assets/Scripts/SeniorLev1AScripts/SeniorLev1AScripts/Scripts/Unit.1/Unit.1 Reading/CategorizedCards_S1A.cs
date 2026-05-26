using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategorizedCards_S1A : MonoBehaviour
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

        [HideInInspector]
        public bool openedBefore;
        [HideInInspector]
        public bool isCompleted;
    }

    [Header("UI Title")]
    public TMP_Text titleText;

    [Header("Title Pop")]
    public float popDuration = 1.75f;
    public float popAmplitude = 0.75f;
    public float popFrequency = 4f;
    public float popStagger = 0.05f;

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

    [Header("Toggles (Optional)")]
    public Button slowButton;
    public Button repeatButton;
    public Image slowBG;
    public Image repeatBG;
    public Color activeToggleColor = Color.green;

    private bool isSlowOn = false;
    private bool isRepeatOn = false;
    private bool useQuestionVisitedLogic;

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

    public Color playingTextColor =
        new Color(1f, 0.8f, 0.25f);

    private Coroutine currentAudioRoutine;
    private Coroutine pulseRoutine;
    private QuestionItem currentPlayingQuestion;

    private bool canInteract;

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
        currentPlayingQuestion = null;

        if (titleText != null)
        {
            CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();
            if (titleCG == null) titleCG = titleText.gameObject.AddComponent<CanvasGroup>();
            titleCG.alpha = 0f;
            titleText.ForceMeshUpdate();
        }

        if (categoryBoard != null)
        {
            categoryBoard.anchoredPosition = boardStartPos;
        }

        useQuestionVisitedLogic = (slowButton != null || repeatButton != null);

        if (sharedBottomBoard != null)
            sharedBottomBoard.SetActive(false);

        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }

        isSlowOn = false;
        isRepeatOn = false;
        if (slowBG != null) slowBG.color = categoryNormalColor;
        if (repeatBG != null) repeatBG.color = categoryNormalColor;

        foreach (var category in categories)
        {
            category.contentPanel.gameObject.SetActive(false);
            category.openedBefore = false;
            category.isCompleted = false;

            if (category.unexpandedButton != null)
            {
                category.unexpandedButton.transform.localScale = Vector3.zero;
                Image img = category.unexpandedButton.GetComponent<Image>();
                if (img != null) img.color = categoryNormalColor;
            }

            if (category.expandedButton != null)
            {
                category.expandedButton.transform.localScale = Vector3.one;
                Image img = category.expandedButton.GetComponent<Image>();
                if (img != null) img.color = categoryNormalColor;
            }

            foreach (var q in category.questions)
            {
                q.originalScale = Vector3.one;
                q.played = false;
                q.questionButton.transform.localScale = Vector3.zero;
                q.questionText.color = normalTextColor;
                Image qImg = q.questionButton.GetComponent<Image>();
                if (qImg != null) qImg.color = categoryNormalColor;
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
                    if (canInteract) OpenCategory(capturedCategory);
                });
            }

            if (category.expandedButton != null)
            {
                category.expandedButton.onClick.RemoveAllListeners();
                category.expandedButton.onClick.AddListener(() =>
                {
                    if (canInteract) OpenCategory(capturedCategory);
                });
            }

            foreach (var q in category.questions)
            {
                QuestionItem capturedQuestion = q;

                q.questionButton.onClick.RemoveAllListeners();

                q.questionButton.onClick.AddListener(() =>
                {
                    if (canInteract)
                        PlayQuestionAudio(capturedQuestion);
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

        if (titleText != null)
        {
            StartCoroutine(TitleAnim());
            yield return new WaitForSeconds(0.8f);
        }

        if (categoryBoard != null)
        {
            yield return StartCoroutine(SlideInBoard());
        }

        if (introAudio)
        {
            yield return new WaitWhile(() => audioSource.isPlaying);
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
            
            // Ease-out cubic
            float ease = 1f - Mathf.Pow(1f - clamped, 3f);
            
            categoryBoard.anchoredPosition = Vector2.Lerp(boardStartPos, boardEndPos, ease);
            yield return null;
        }
        categoryBoard.anchoredPosition = boardEndPos;
    }

    void OpenCategory(CategoryData selectedCategory)
    {
        foreach (var category in categories)
        {
            category.contentPanel.gameObject.SetActive(false);
            
            Color targetColor = category.isCompleted ? categoryVisitedColor : categoryNormalColor;
            Color targetTextColor = category.isCompleted ? categoryVisitedTextColor : normalTextColor;

            if (category.unexpandedButton != null)
            {
                Image img = category.unexpandedButton.GetComponent<Image>();
                if (img != null) img.color = targetColor;
                
                TMP_Text txt = category.unexpandedButton.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.color = targetTextColor;
            }

            if (category.expandedButton != null)
            {
                Image img = category.expandedButton.GetComponent<Image>();
                if (img != null) img.color = targetColor;

                TMP_Text txt = category.expandedButton.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.color = targetTextColor;
            }
        }

        if (sharedBottomBoard != null)
            sharedBottomBoard.SetActive(true);

        selectedCategory.contentPanel.gameObject.SetActive(true);

        if (selectedCategory.unexpandedButton != null)
        {
            Image img = selectedCategory.unexpandedButton.GetComponent<Image>();
            if (img != null) img.color = categorySelectedColor;
            
            TMP_Text txt = selectedCategory.unexpandedButton.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.color = normalTextColor;
        }

        if (selectedCategory.expandedButton != null)
        {
            Image img = selectedCategory.expandedButton.GetComponent<Image>();
            if (img != null) img.color = categorySelectedColor;
            
            TMP_Text txt = selectedCategory.expandedButton.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.color = normalTextColor;
        }

        if (!selectedCategory.openedBefore)
        {
            selectedCategory.openedBefore = true;
            StartCoroutine(AnimateQuestions(selectedCategory));
            
            if (!useQuestionVisitedLogic)
            {
                selectedCategory.isCompleted = true;
                CheckAllCategoriesVisited();
            }
        }
        else
        {
            foreach (var q in selectedCategory.questions)
            {
                q.questionButton.transform.localScale = Vector3.one;
            }
        }
    }

    void CheckAllCategoriesVisited()
    {
        foreach (var cat in categories)
        {
            if (!cat.isCompleted) return;
        }

        if (nextButton != null && !nextButton.activeSelf)
        {
            nextButton.SetActive(true);
        }
    }

    IEnumerator AnimateQuestions(CategoryData category)
    {
        canInteract = false;

        foreach (var q in category.questions)
        {
            StartCoroutine(PopUI(q.questionButton.transform, questionPopSpeed));

            if (popSfx)
                audioSource.PlayOneShot(popSfx);

            yield return new WaitForSeconds(staggerDelay);
        }

        canInteract = true;
    }

    void ToggleSlow()
    {
        if (!canInteract) return;
        isSlowOn = !isSlowOn;
        if (slowBG != null) slowBG.color = isSlowOn ? activeToggleColor : categoryNormalColor;
    }

    void ToggleRepeat()
    {
        if (!canInteract) return;
        isRepeatOn = !isRepeatOn;
        if (repeatBG != null) repeatBG.color = isRepeatOn ? activeToggleColor : categoryNormalColor;
        
        if (audioSource != null)
            audioSource.loop = isRepeatOn;
    }

    void PlayQuestionAudio(QuestionItem q)
    {
        if (currentAudioRoutine != null)
            StopCoroutine(currentAudioRoutine);

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        if (audioSource.isPlaying)
            audioSource.Stop();

        if (currentPlayingQuestion != null)
            ResetVisual(currentPlayingQuestion);

        currentPlayingQuestion = q;
        currentAudioRoutine = StartCoroutine(PlayRoutine(q));
    }

    IEnumerator PlayRoutine(QuestionItem q)
    {
        q.questionText.color = playingTextColor;

        pulseRoutine = StartCoroutine(PulseWhilePlaying(q.questionButton.transform));

        AudioClip clipToPlay = (isSlowOn && q.slowAudio != null) ? q.slowAudio : q.audio;

        if (clipToPlay != null)
        {
            audioSource.clip = clipToPlay;
            audioSource.loop = isRepeatOn;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        q.played = true;

        if (useQuestionVisitedLogic)
            CheckCategoryCompletionFromQuestions();

        ResetVisual(q);
        currentPlayingQuestion = null;
    }

    void CheckCategoryCompletionFromQuestions()
    {
        foreach (var category in categories)
        {
            if (category.isCompleted) continue;

            if (category.contentPanel.gameObject.activeInHierarchy)
            {
                bool allPlayed = true;
                foreach (var q in category.questions)
                {
                    if (!q.played)
                    {
                        allPlayed = false;
                        break;
                    }
                }

                if (allPlayed)
                {
                    category.isCompleted = true;
                    // Visually update the unexpanded and expanded buttons right now if we are looking at it
                    if (category.unexpandedButton != null)
                    {
                        Image img = category.unexpandedButton.GetComponent<Image>();
                        if (img != null) img.color = categoryVisitedColor;
                        TMP_Text txt = category.unexpandedButton.GetComponentInChildren<TMP_Text>();
                        if (txt != null) txt.color = categoryVisitedTextColor;
                    }
                    if (category.expandedButton != null)
                    {
                        Image img = category.expandedButton.GetComponent<Image>();
                        if (img != null) img.color = categoryVisitedColor;
                        TMP_Text txt = category.expandedButton.GetComponentInChildren<TMP_Text>();
                        if (txt != null) txt.color = categoryVisitedTextColor;
                    }

                    CheckAllCategoriesVisited();
                }
            }
        }
    }

    void ResetVisual(QuestionItem q)
    {
        Color txtColor = normalTextColor;
        Color bgColor = categoryNormalColor;

        if (useQuestionVisitedLogic && q.played)
        {
            txtColor = categoryVisitedTextColor;
            bgColor = categoryVisitedColor;
        }

        q.questionText.color = txtColor;
        
        Image img = q.questionButton.GetComponent<Image>();
        if (img != null) img.color = bgColor;

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
            float ease = 1f + c1 * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

            t.localScale = Vector3.one * ease;
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    IEnumerator TitleAnim()
    {
        yield return new WaitForEndOfFrame();
        yield return null;

        titleText.ForceMeshUpdate();
        yield return null;
        titleText.ForceMeshUpdate();

        TMP_TextInfo textInfo = titleText.textInfo;
        int charCount = textInfo.characterCount;

        if (charCount == 0) yield break;

        titleText.maxVisibleCharacters = charCount;
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();
        bool revealed = false;
        float elapsed = 0f;

        float expectedTime = (charCount * popStagger) + Mathf.Max(0.5f, 1f / popFrequency);
        float totalDuration = Mathf.Max(popDuration, expectedTime);

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            textInfo = titleText.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int matIndex = charInfo.materialReferenceIndex;
                int vertIndex = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[matIndex].vertices;
                Vector3 charMid = (vertices[vertIndex] + vertices[vertIndex + 2]) / 2f;

                float letterDelay = i * popStagger;
                float localTime = elapsed - letterDelay;

                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterDur = Mathf.Max(0.1f, 1f / popFrequency);
                    float t = Mathf.Clamp01(localTime / letterDur);

                    float overshoot = 1.70158f * (1f + popAmplitude);
                    float c3 = overshoot + 1f;

                    scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIndex].vertices[vertIndex + v];
                    Vector3 offset = orig - charMid;
                    vertices[vertIndex + v] = charMid + offset * scale;
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                titleText.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }

            yield return null;

            if (!revealed && titleCG != null)
            {
                titleCG.alpha = 1f;
                revealed = true;
            }
        }

        textInfo = titleText.textInfo;
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
            titleText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    IEnumerator PulseWhilePlaying(Transform t)
    {
        while (audioSource.isPlaying)
        {
            float scale =
                1f + Mathf.Sin(Time.time * 8f) * 0.05f;

            t.localScale =
                Vector3.one * scale;

            yield return null;
        }

        t.localScale = Vector3.one;
    }
}