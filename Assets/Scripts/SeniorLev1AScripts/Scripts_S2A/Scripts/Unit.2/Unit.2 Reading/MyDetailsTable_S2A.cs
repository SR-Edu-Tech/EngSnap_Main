using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MyDetailsTable_S2A : MonoBehaviour
{
    [System.Serializable]
    public class DialoguePair
    {
        [Header("BUTTONS")]
        public Button greetingButton;
        public Button responseButton;

        [Header("BACKGROUNDS")]
        public Image greetingBg;
        public Image responseBg;

        [Header("CONTAINER")]
        public RectTransform container;

        [Header("TEXT")]
        public TMP_Text greetingText;
        public TMP_Text responseText;

        [Header("AUDIO")]
        public AudioClip greetingAudio;
        public AudioClip responseAudio;

        [Header("SPEAKER ICONS")]
        public GameObject greetingSpeakerIcon;
        public GameObject responseSpeakerIcon;

        [HideInInspector] public Vector3 originalScale;
        [HideInInspector] public bool visited;
        [HideInInspector] public bool hasBouncedIn;
    }

    [Header("UI")]
    public RectTransform title;
    public RectTransform board;
    public RectTransform greetingsHeader;
    public RectTransform responsesHeader;
    public GameObject nextButton;

    [Header("Scroll")]
    public ScrollRect scrollRect;
    public RectTransform content;

    [Header("Pairs")]
    public DialoguePair[] pairs;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    public AudioClip promptGreeting; // "for this greeting"
    public AudioClip promptResponse; // "this should be response"

    public AudioClip popSfx;

    [Header("Colors")]

    // NEW:
    // When ON, each TMP text keeps the color it had in the scene
    // and uses that color for both Normal and Highlight.
    public bool useOriginalTextColor;

    public Color normalColor = Color.black;
    public Color highlightColor = new Color(1f, 0.85f, 0.3f);
    public Color visitedColor = Color.gray;
    public Color visitedHighlightColor = new Color(1f, 0.95f, 0.5f);
    public Color normalButtonColor = Color.white;
    public Color visitedButtonColor = new Color(0.85f, 0.85f, 0.85f);

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float delayBetweenPairs = 0.4f;
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    private Coroutine currentRoutine;
    private bool isAutoPlaying = true;
    private Vector2 boardOriginalPos;
    private bool isInitialized = false;

    // Stores the ORIGINAL colors already assigned to the TMP texts
    // in the Unity scene.
    //
    // This is private, so nothing appears in the Inspector.
    private Dictionary<DialoguePair, Color> originalGreetingColors =
        new Dictionary<DialoguePair, Color>();

    private Dictionary<DialoguePair, Color> originalResponseColors =
        new Dictionary<DialoguePair, Color>();

    void OnEnable()
    {
        ResetUIState();
        SetupButtons();
        StartCoroutine(MainFlow());
    }

    void OnDisable()
    {
        StopAllCoroutines();

        if (audioSource)
            audioSource.Stop();
    }

    void ResetUIState()
    {
        if (!isInitialized)
        {
            boardOriginalPos = board.anchoredPosition;
            isInitialized = true;
        }

        board.anchoredPosition = new Vector2(0, -1200);

        HideTextSafely(title.GetComponent<TMP_Text>());
        HideTextSafely(greetingsHeader.GetComponent<TMP_Text>());
        HideTextSafely(responsesHeader.GetComponent<TMP_Text>());

        if (nextButton)
            nextButton.SetActive(false);

        foreach (var p in pairs)
        {
            // -------------------------------------------------
            // CAPTURE THE ORIGINAL SCENE COLORS
            // -------------------------------------------------
            //
            // This happens BEFORE the script changes the color.
            // So if you manually made a text blue/green/etc.
            // in the Unity Inspector, that exact color is saved.
            //
            if (p.greetingText != null &&
                !originalGreetingColors.ContainsKey(p))
            {
                originalGreetingColors.Add(
                    p,
                    p.greetingText.color
                );
            }

            if (p.responseText != null &&
                !originalResponseColors.ContainsKey(p))
            {
                originalResponseColors.Add(
                    p,
                    p.responseText.color
                );
            }

            p.originalScale = Vector3.one;
            p.container.localScale = Vector3.one;

            p.greetingButton.transform.localScale = Vector3.zero;
            p.responseButton.transform.localScale = Vector3.zero;

            SetBgColor(p.greetingBg, normalButtonColor);
            SetBgColor(p.responseBg, normalButtonColor);

            p.visited = false;
            p.hasBouncedIn = false;

            // -------------------------------------------------
            // NORMAL TEXT COLOR
            // -------------------------------------------------
            //
            // ON  -> use the original color from the scene
            // OFF -> use your normalColor setting
            //
            if (useOriginalTextColor)
            {
                p.greetingText.color =
                    GetOriginalGreetingColor(p);

                p.responseText.color =
                    GetOriginalResponseColor(p);
            }
            else
            {
                p.greetingText.color = normalColor;
                p.responseText.color = normalColor;
            }

            HideTextSafely(p.greetingText);
            HideTextSafely(p.responseText);

            if (p.greetingSpeakerIcon)
                p.greetingSpeakerIcon.SetActive(false);

            if (p.responseSpeakerIcon)
                p.responseSpeakerIcon.SetActive(false);
        }

        scrollRect.verticalNormalizedPosition = 1f;
    }

    // ---------------------------------------------------------
    // GET ORIGINAL GREETING COLOR
    // ---------------------------------------------------------

    Color GetOriginalGreetingColor(DialoguePair pair)
    {
        if (originalGreetingColors.ContainsKey(pair))
            return originalGreetingColors[pair];

        return normalColor;
    }

    // ---------------------------------------------------------
    // GET ORIGINAL RESPONSE COLOR
    // ---------------------------------------------------------

    Color GetOriginalResponseColor(DialoguePair pair)
    {
        if (originalResponseColors.ContainsKey(pair))
            return originalResponseColors[pair];

        return normalColor;
    }

    void HideTextSafely(TMP_Text txt)
    {
        if (txt != null)
        {
            CanvasGroup cg = txt.GetComponent<CanvasGroup>();

            if (cg == null)
                cg = txt.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
        }
    }

    void SetBgColor(Image bg, Color c)
    {
        if (bg != null)
            bg.color = c;
    }

    void SetupButtons()
    {
        foreach (var pair in pairs)
        {
            DialoguePair captured = pair;

            pair.greetingButton.onClick.RemoveAllListeners();
            pair.responseButton.onClick.RemoveAllListeners();

            pair.greetingButton.onClick.AddListener(
                () => OnPairClicked(captured)
            );

            pair.responseButton.onClick.AddListener(
                () => OnPairClicked(captured)
            );
        }
    }

    IEnumerator MainFlow()
    {
        isAutoPlaying = true;

        if (introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        StartCoroutine(TitleAnim(title.GetComponent<TMP_Text>()));

        yield return new WaitForSeconds(0.2f);

        yield return SlideUp(board, 1200f);

        StartCoroutine(
            TitleAnim(greetingsHeader.GetComponent<TMP_Text>())
        );

        StartCoroutine(
            TitleAnim(responsesHeader.GetComponent<TMP_Text>())
        );

        yield return new WaitForSeconds(0.4f);

        if (introClip)
            yield return new WaitWhile(
                () => audioSource.isPlaying
            );

        for (int i = 0; i < pairs.Length; i++)
        {
            yield return ScrollTo(pairs[i].container);

            if (popSfx)
                audioSource.PlayOneShot(popSfx);

            yield return BounceIn(
                pairs[i].greetingButton.transform
            );

            if (popSfx)
                audioSource.PlayOneShot(popSfx);

            yield return BounceIn(
                pairs[i].responseButton.transform
            );

            pairs[i].hasBouncedIn = true;

            yield return StartCoroutine(
                PlayPairSequence(pairs[i], true)
            );

            yield return new WaitForSeconds(
                delayBetweenPairs
            );
        }

        isAutoPlaying = false;
    }

    void OnPairClicked(DialoguePair pair)
    {
        if (isAutoPlaying)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (audioSource.isPlaying)
            audioSource.Stop();

        // Mark visited immediately on tap
        if (!pair.visited)
        {
            pair.visited = true;

            ApplyVisitedColor(pair);

            CheckAllPairsVisited();
        }

        ResetAllPairs();

        currentRoutine = StartCoroutine(
            PlayPairSequence(pair, false)
        );
    }

    IEnumerator PlayPairSequence(
        DialoguePair pair,
        bool isAutoPlay
    )
    {
        yield return ScrollTo(pair.container);

        // -----------------------------------------------------
        // GREETING PART
        // -----------------------------------------------------

        HighlightGreeting(pair, true);

        if (!pair.visited || isAutoPlay)
        {
            StartCoroutine(
                PopTextPerChar(pair.greetingText)
            );
        }

        if (promptGreeting)
        {
            audioSource.clip = promptGreeting;
            audioSource.Play();

            yield return new WaitWhile(
                () => audioSource.isPlaying
            );
        }

        if (pair.greetingAudio)
        {
            Coroutine scaleCo = StartCoroutine(
                ScaleCard(
                    pair.greetingButton.transform,
                    1.05f
                )
            );

            audioSource.clip = pair.greetingAudio;
            audioSource.Play();

            yield return new WaitWhile(
                () => audioSource.isPlaying
            );

            if (scaleCo != null)
                StopCoroutine(scaleCo);

            StartCoroutine(
                ScaleCard(
                    pair.greetingButton.transform,
                    1.0f
                )
            );
        }

        HighlightGreeting(pair, false);

        // -----------------------------------------------------
        // RESPONSE PART
        // -----------------------------------------------------

        HighlightResponse(pair, true);

        if (!pair.visited || isAutoPlay)
        {
            StartCoroutine(
                PopTextPerChar(pair.responseText)
            );
        }

        if (promptResponse)
        {
            audioSource.clip = promptResponse;
            audioSource.Play();

            yield return new WaitWhile(
                () => audioSource.isPlaying
            );
        }

        if (pair.responseAudio)
        {
            Coroutine scaleCo = StartCoroutine(
                ScaleCard(
                    pair.responseButton.transform,
                    1.05f
                )
            );

            audioSource.clip = pair.responseAudio;
            audioSource.Play();

            yield return new WaitWhile(
                () => audioSource.isPlaying
            );

            if (scaleCo != null)
                StopCoroutine(scaleCo);

            StartCoroutine(
                ScaleCard(
                    pair.responseButton.transform,
                    1.0f
                )
            );
        }

        HighlightResponse(pair, false);

        // Check completion
        if (!isAutoPlay)
        {
            CheckAllPairsVisited();
        }
    }

    // =========================================================
    // GREETING HIGHLIGHT
    // =========================================================

    void HighlightGreeting(DialoguePair pair, bool state)
    {
        Color targetColor;

        if (state)
        {
            // ---------------------------------------------
            // HIGHLIGHT
            // ---------------------------------------------
            //
            // Visited behavior stays exactly the same.
            //
            // If not visited:
            // ON  -> original scene color
            // OFF -> highlightColor
            //
            targetColor = pair.visited
                ? visitedHighlightColor
                : (
                    useOriginalTextColor
                        ? GetOriginalGreetingColor(pair)
                        : highlightColor
                  );
        }
        else
        {
            // ---------------------------------------------
            // NORMAL
            // ---------------------------------------------
            //
            // Visited behavior stays exactly the same.
            //
            // If not visited:
            // ON  -> original scene color
            // OFF -> normalColor
            //
            targetColor = pair.visited
                ? visitedColor
                : (
                    useOriginalTextColor
                        ? GetOriginalGreetingColor(pair)
                        : normalColor
                  );
        }

        pair.greetingText.color = targetColor;

        if (pair.greetingSpeakerIcon)
        {
            pair.greetingSpeakerIcon.SetActive(state);

            if (state)
            {
                Image iconImg =
                    pair.greetingSpeakerIcon.GetComponent<Image>();

                if (iconImg != null)
                    iconImg.color = targetColor;
            }
        }
    }

    // =========================================================
    // RESPONSE HIGHLIGHT
    // =========================================================

    void HighlightResponse(DialoguePair pair, bool state)
    {
        Color targetColor;

        if (state)
        {
            // ---------------------------------------------
            // HIGHLIGHT
            // ---------------------------------------------
            //
            // Visited behavior stays exactly the same.
            //
            targetColor = pair.visited
                ? visitedHighlightColor
                : (
                    useOriginalTextColor
                        ? GetOriginalResponseColor(pair)
                        : highlightColor
                  );
        }
        else
        {
            // ---------------------------------------------
            // NORMAL
            // ---------------------------------------------
            //
            // Visited behavior stays exactly the same.
            //
            targetColor = pair.visited
                ? visitedColor
                : (
                    useOriginalTextColor
                        ? GetOriginalResponseColor(pair)
                        : normalColor
                  );
        }

        pair.responseText.color = targetColor;

        if (pair.responseSpeakerIcon)
        {
            pair.responseSpeakerIcon.SetActive(state);

            if (state)
            {
                Image iconImg =
                    pair.responseSpeakerIcon.GetComponent<Image>();

                if (iconImg != null)
                    iconImg.color = targetColor;
            }
        }
    }

    // =========================================================
    // VISITED COLOR
    // =========================================================

    void ApplyVisitedColor(DialoguePair pair)
    {
        // IMPORTANT:
        // These colors are NOT affected by the new bool.

        pair.greetingText.color = visitedColor;
        pair.responseText.color = visitedColor;

        SetBgColor(
            pair.greetingBg,
            visitedButtonColor
        );

        SetBgColor(
            pair.responseBg,
            visitedButtonColor
        );
    }

    // =========================================================
    // RESET ALL PAIRS
    // =========================================================

    void ResetAllPairs()
    {
        foreach (var p in pairs)
        {
            if (!p.visited)
            {
                // -----------------------------------------
                // NORMAL COLOR
                // -----------------------------------------
                //
                // ON  -> original scene color
                // OFF -> normalColor
                //
                if (useOriginalTextColor)
                {
                    p.greetingText.color =
                        GetOriginalGreetingColor(p);

                    p.responseText.color =
                        GetOriginalResponseColor(p);
                }
                else
                {
                    p.greetingText.color = normalColor;
                    p.responseText.color = normalColor;
                }

                SetBgColor(
                    p.greetingBg,
                    normalButtonColor
                );

                SetBgColor(
                    p.responseBg,
                    normalButtonColor
                );
            }
            else
            {
                // Visited colors remain completely unchanged
                ApplyVisitedColor(p);
            }

            if (p.greetingSpeakerIcon)
                p.greetingSpeakerIcon.SetActive(false);

            if (p.responseSpeakerIcon)
                p.responseSpeakerIcon.SetActive(false);

            if (p.hasBouncedIn)
            {
                p.greetingButton.transform.localScale =
                    Vector3.one;

                p.responseButton.transform.localScale =
                    Vector3.one;
            }
        }
    }

    // =========================================================
    // CHECK ALL PAIRS
    // =========================================================

    void CheckAllPairsVisited()
    {
        foreach (var p in pairs)
        {
            if (!p.visited)
                return;
        }

        if (nextButton)
            nextButton.SetActive(true);
    }

    // =========================================================
    // BOUNCE ANIMATION
    // =========================================================

    IEnumerator BounceIn(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;

            float clamped = Mathf.Clamp01(t);

            float overshoot = 1.70158f;

            float c1 = overshoot + 1f;

            float ease =
                1f
                + c1 * Mathf.Pow(clamped - 1f, 3f)
                + overshoot * Mathf.Pow(clamped - 1f, 2f);

            target.localScale =
                Vector3.one * ease;

            yield return null;
        }

        target.localScale = Vector3.one;
    }

    // =========================================================
    // SCALE CARD
    // =========================================================

    IEnumerator ScaleCard(
        Transform target,
        float targetScale
    )
    {
        Vector3 startScale = target.localScale;

        Vector3 endScale =
            Vector3.one * targetScale;

        float time = 0;

        float duration = 0.15f;

        while (time < 1)
        {
            time += Time.deltaTime / duration;

            target.localScale =
                Vector3.Lerp(
                    startScale,
                    endScale,
                    time
                );

            yield return null;
        }

        target.localScale = endScale;
    }

    // =========================================================
    // SLIDE UP
    // =========================================================

    IEnumerator SlideUp(
        RectTransform t,
        float speed
    )
    {
        Vector2 start =
            new Vector2(0, -1200);

        Vector2 end =
            boardOriginalPos;

        float time = 0;

        while (time < 1)
        {
            time +=
                Time.deltaTime *
                (speed / 1000f);

            float clamped =
                Mathf.Clamp01(time);

            float overshoot = 1.2f;

            float c1 =
                overshoot + 1f;

            float ease =
                1f
                + c1 * Mathf.Pow(clamped - 1f, 3f)
                + overshoot * Mathf.Pow(clamped - 1f, 2f);

            t.anchoredPosition =
                Vector2.LerpUnclamped(
                    start,
                    end,
                    ease
                );

            yield return null;
        }

        t.anchoredPosition = end;
    }

    // =========================================================
    // SCROLL TO
    // =========================================================

    IEnumerator ScrollTo(RectTransform target)
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        float contentHeight =
            content.rect.height;

        float viewportHeight =
            scrollRect.viewport.rect.height;

        if (contentHeight <= viewportHeight)
        {
            yield break;
        }

        float targetY =
            Mathf.Abs(
                target.anchoredPosition.y
            ) - 50f;

        targetY =
            Mathf.Max(0f, targetY);

        if (
            pairs.Length > 0 &&
            target == pairs[0].container
        )
        {
            targetY = 0f;
        }

        float normalized =
            1 - Mathf.Clamp01(
                targetY /
                (contentHeight - viewportHeight)
            );

        float start =
            scrollRect.verticalNormalizedPosition;

        float time = 0;

        while (time < 1)
        {
            time += Time.deltaTime * 3f;

            scrollRect.verticalNormalizedPosition =
                Mathf.Lerp(
                    start,
                    normalized,
                    time
                );

            yield return null;
        }

        scrollRect.verticalNormalizedPosition =
            normalized;
    }

    // =========================================================
    // TITLE ANIMATION
    // =========================================================

    IEnumerator TitleAnim(TMP_Text txt)
    {
        if (txt == null)
            yield break;

        CanvasGroup titleCG =
            txt.GetComponent<CanvasGroup>();

        if (titleCG == null)
            titleCG =
                txt.gameObject.AddComponent<CanvasGroup>();

        titleCG.alpha = 0f;

        yield return new WaitForEndOfFrame();
        yield return null;

        txt.ForceMeshUpdate();

        yield return null;

        txt.ForceMeshUpdate();

        string originalText = txt.text;

        TMP_TextInfo textInfo =
            txt.textInfo;

        int charCount =
            textInfo.characterCount;

        if (charCount == 0)
            yield break;

        txt.maxVisibleCharacters =
            charCount;

        TMP_MeshInfo[] cachedMeshInfo =
            textInfo.CopyMeshInfoVertexData();

        bool revealed = false;

        float elapsed = 0f;

        float expectedTime =
            (charCount * titlePopStagger)
            + Mathf.Max(
                0.5f,
                1f / titlePopFrequency
            );

        float totalDuration =
            Mathf.Max(
                titlePopDuration,
                expectedTime
            );

        while (elapsed < totalDuration)
        {
            if (txt.text != originalText)
                break;

            elapsed += Time.deltaTime;

            textInfo = txt.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo =
                    textInfo.characterInfo[i];

                if (!charInfo.isVisible)
                    continue;

                int matIndex =
                    charInfo.materialReferenceIndex;

                int vertIndex =
                    charInfo.vertexIndex;

                Vector3[] vertices =
                    textInfo.meshInfo[matIndex].vertices;

                Vector3 charMid =
                    (
                        vertices[vertIndex]
                        + vertices[vertIndex + 2]
                    ) / 2f;

                float letterDelay =
                    i * titlePopStagger;

                float localTime =
                    elapsed - letterDelay;

                float scale = 0f;

                if (localTime > 0f)
                {
                    float letterDur =
                        Mathf.Max(
                            0.1f,
                            1f / titlePopFrequency
                        );

                    float t =
                        Mathf.Clamp01(
                            localTime / letterDur
                        );

                    float overshoot =
                        1.70158f *
                        (1f + titlePopAmplitude);

                    float c3 =
                        overshoot + 1f;

                    scale =
                        1f
                        + c3 *
                          Mathf.Pow(
                              t - 1f,
                              3f
                          )
                        + overshoot *
                          Mathf.Pow(
                              t - 1f,
                              2f
                          );
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig =
                        cachedMeshInfo[
                            matIndex
                        ].vertices[
                            vertIndex + v
                        ];

                    Vector3 offset =
                        orig - charMid;

                    vertices[
                        vertIndex + v
                    ] =
                        charMid
                        + offset * scale;
                }
            }

            for (
                int m = 0;
                m < textInfo.meshInfo.Length;
                m++
            )
            {
                textInfo.meshInfo[m].mesh.vertices =
                    textInfo.meshInfo[m].vertices;

                txt.UpdateGeometry(
                    textInfo.meshInfo[m].mesh,
                    m
                );
            }

            yield return null;

            if (!revealed && titleCG != null)
            {
                titleCG.alpha = 1f;
                revealed = true;
            }
        }

        if (txt.text == originalText)
        {
            textInfo = txt.textInfo;

            for (
                int i = 0;
                i < textInfo.meshInfo.Length;
                i++
            )
            {
                textInfo.meshInfo[i].mesh.vertices =
                    cachedMeshInfo[i].vertices;

                txt.UpdateGeometry(
                    textInfo.meshInfo[i].mesh,
                    i
                );
            }
        }

        txt.maxVisibleCharacters = 99999;
    }

    // =========================================================
    // POP TEXT PER CHARACTER
    // =========================================================

    IEnumerator PopTextPerChar(
        TMP_Text tmp,
        float popDur = 1.2f,
        float charStagger = 0.04f,
        float popAmp = 0.6f,
        float popFreq = 4f
    )
    {
        if (tmp == null)
            yield break;

        tmp.ForceMeshUpdate();

        yield return null;

        tmp.ForceMeshUpdate();

        string originalText = tmp.text;

        TMP_TextInfo textInfo =
            tmp.textInfo;

        int charCount =
            textInfo.characterCount;

        if (charCount == 0)
            yield break;

        tmp.maxVisibleCharacters =
            charCount;

        TMP_MeshInfo[] cachedMeshInfo =
            textInfo.CopyMeshInfoVertexData();

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo =
                textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int matIdx =
                charInfo.materialReferenceIndex;

            int vertIdx =
                charInfo.vertexIndex;

            Vector3[] vertices =
                textInfo.meshInfo[matIdx].vertices;

            Vector3 charMid =
                (
                    vertices[vertIdx]
                    + vertices[vertIdx + 2]
                ) / 2f;

            for (int v = 0; v < 4; v++)
            {
                vertices[vertIdx + v] =
                    charMid;
            }
        }

        for (
            int m = 0;
            m < textInfo.meshInfo.Length;
            m++
        )
        {
            textInfo.meshInfo[m].mesh.vertices =
                textInfo.meshInfo[m].vertices;

            tmp.UpdateGeometry(
                textInfo.meshInfo[m].mesh,
                m
            );
        }

        yield return null;

        CanvasGroup cg =
            tmp.GetComponent<CanvasGroup>();

        if (cg != null)
            cg.alpha = 1f;

        float expectedTime =
            (charCount * charStagger)
            + Mathf.Max(
                0.5f,
                1f / popFreq
            );

        float totalDuration =
            Mathf.Max(
                popDur,
                expectedTime
            );

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            if (tmp.text != originalText)
                break;

            elapsed += Time.deltaTime;

            textInfo = tmp.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo =
                    textInfo.characterInfo[i];

                if (!charInfo.isVisible)
                    continue;

                int matIdx =
                    charInfo.materialReferenceIndex;

                int vertIdx =
                    charInfo.vertexIndex;

                Vector3[] vertices =
                    textInfo.meshInfo[matIdx].vertices;

                Vector3 charMid =
                    (
                        vertices[vertIdx]
                        + vertices[vertIdx + 2]
                    ) / 2f;

                float delay =
                    i * charStagger;

                float localTime =
                    elapsed - delay;

                float scale = 0f;

                if (localTime > 0f)
                {
                    float letterDur =
                        Mathf.Max(
                            0.1f,
                            1f / popFreq
                        );

                    float lt =
                        Mathf.Clamp01(
                            localTime / letterDur
                        );

                    float overshoot =
                        1.70158f *
                        (1f + popAmp);

                    float c3 =
                        overshoot + 1f;

                    scale =
                        1f
                        + c3 *
                          Mathf.Pow(
                              lt - 1f,
                              3f
                          )
                        + overshoot *
                          Mathf.Pow(
                              lt - 1f,
                              2f
                          );
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig =
                        cachedMeshInfo[
                            matIdx
                        ].vertices[
                            vertIdx + v
                        ];

                    Vector3 offset =
                        orig - charMid;

                    vertices[
                        vertIdx + v
                    ] =
                        charMid
                        + offset * scale;
                }
            }

            for (
                int m = 0;
                m < textInfo.meshInfo.Length;
                m++
            )
            {
                textInfo.meshInfo[m].mesh.vertices =
                    textInfo.meshInfo[m].vertices;

                tmp.UpdateGeometry(
                    textInfo.meshInfo[m].mesh,
                    m
                );
            }

            yield return null;
        }

        if (tmp.text == originalText)
        {
            textInfo = tmp.textInfo;

            for (
                int i = 0;
                i < textInfo.meshInfo.Length;
                i++
            )
            {
                textInfo.meshInfo[i].mesh.vertices =
                    cachedMeshInfo[i].vertices;

                tmp.UpdateGeometry(
                    textInfo.meshInfo[i].mesh,
                    i
                );
            }
        }

        tmp.maxVisibleCharacters = 99999;
    }
}