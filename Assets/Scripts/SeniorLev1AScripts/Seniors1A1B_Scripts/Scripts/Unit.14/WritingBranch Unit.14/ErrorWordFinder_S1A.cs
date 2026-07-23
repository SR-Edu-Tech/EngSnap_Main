using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ErrorWordFinder_S1A : MonoBehaviour
{
    [System.Serializable]
    public class ErrorWord
    {
        public Button button;
        public TMP_Text text;

        [HideInInspector] public bool found;
    }

    [Header("Error Words")]
    public ErrorWord[] errorWords;

    [Header("UI")]
    public GameObject nextButton;

    [Header("Containers")]
    public RectTransform title;
    public TMP_Text titleText;
    public RectTransform board;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip tapClip;
    public AudioClip finishClip;
    public AudioClip popClip;

    [Header("Colors")]
    public Color foundColor = new Color(0.18f, 0.55f, 0.34f);

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float slideSpeed = 3f;
    public float staggerDelay = 0.08f;

    [Header("Title Animation")]
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    private bool completed = false;
    private bool canInteract = false;

    private Vector2 boardOrigPos;

    void Awake()
    {
        if (titleText == null && title != null)
            titleText = title.GetComponentInChildren<TMP_Text>();

        boardOrigPos = board.anchoredPosition;
    }

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
        completed = false;
        canInteract = false;

        nextButton.SetActive(false);

        foreach (var word in errorWords)
        {
            word.found = false;

            if (word.button != null)
                word.button.interactable = true;
        }

        board.localScale = Vector3.one;

        board.anchoredPosition = boardOrigPos + new Vector2(-1500f, 0);

        TMP_Text[] boardTexts = board.GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text t in boardTexts)
        {
            CanvasGroup cg = t.GetComponent<CanvasGroup>();

            if (cg == null)
                cg = t.gameObject.AddComponent<CanvasGroup>();

            cg.alpha = 0f;
        }

        if (titleText != null)
        {
            CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();

            if (titleCG == null)
                titleCG = titleText.gameObject.AddComponent<CanvasGroup>();

            titleCG.alpha = 0f;
        }
    }

    void SetupButtons()
    {
        foreach (var word in errorWords)
        {
            ErrorWord captured = word;

            captured.button.onClick.RemoveAllListeners();
            captured.button.onClick.AddListener(() => OnWordTapped(captured));
        }
    }

    IEnumerator IntroFlow()
    {
        if (introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        StartCoroutine(TitleAnim());

        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(
            SlideIn(board, boardOrigPos, new Vector2(-1500f, 0))
        );

        TMP_Text[] boardTexts = board.GetComponentsInChildren<TMP_Text>();

        foreach (var t in boardTexts)
        {
            StartCoroutine(PopTextPerChar(t));
        }

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        canInteract = true;
    }

    void OnWordTapped(ErrorWord word)
    {
        if (!canInteract) return;
        if (completed) return;
        if (word.found) return;

        word.found = true;

        if (word.text != null)
            word.text.color = foundColor;

        word.button.interactable = false;

        if (tapClip && audioSource)
            audioSource.PlayOneShot(tapClip);

        CheckCompletion();
    }

    void CheckCompletion()
    {
        foreach (var word in errorWords)
        {
            if (!word.found)
                return;
        }

        completed = true;

        if (finishClip && audioSource)
            audioSource.PlayOneShot(finishClip);

        StartCoroutine(ShowNextButton());
    }

    IEnumerator ShowNextButton()
    {
        yield return new WaitForSeconds(0.5f);

        nextButton.SetActive(true);

        StartCoroutine(PopButton(nextButton.transform));
    }

    IEnumerator SlideIn(RectTransform target, Vector2 endPos, Vector2 slideOffset)
    {
        target.localScale = Vector3.one;

        Vector2 startPos = endPos + slideOffset;

        target.anchoredPosition = startPos;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * slideSpeed;

            float clamped = Mathf.Clamp01(t);

            float overshoot = 1.2f;
            float c1 = overshoot;
            float c3 = c1 + 1f;

            float ease =
                1f +
                c3 * Mathf.Pow(clamped - 1f, 3f) +
                c1 * Mathf.Pow(clamped - 1f, 2f);

            target.anchoredPosition =
                Vector2.LerpUnclamped(startPos, endPos, ease);

            yield return null;
        }

        target.anchoredPosition = endPos;
    }

    IEnumerator TitleAnim()
    {
        if (titleText == null)
            yield break;

        CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();

        if (titleCG == null)
            titleCG = titleText.gameObject.AddComponent<CanvasGroup>();

        titleCG.alpha = 0f;

        yield return new WaitForEndOfFrame();
        yield return null;

        titleText.ForceMeshUpdate();

        yield return null;

        titleText.ForceMeshUpdate();

        string originalText = titleText.text;

        TMP_TextInfo textInfo = titleText.textInfo;

        int charCount = textInfo.characterCount;

        if (charCount == 0)
            yield break;

        titleText.maxVisibleCharacters = charCount;

        TMP_MeshInfo[] cachedMeshInfo =
            textInfo.CopyMeshInfoVertexData();

        bool revealed = false;

        float elapsed = 0f;

        float expectedTime =
            (charCount * titlePopStagger) +
            Mathf.Max(0.5f, 1f / titlePopFrequency);

        float totalDuration =
            Mathf.Max(titlePopDuration, expectedTime);

        while (elapsed < totalDuration)
        {
            if (titleText.text != originalText)
                break;

            elapsed += Time.deltaTime;

            textInfo = titleText.textInfo;

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
                    (vertices[vertIndex] +
                    vertices[vertIndex + 2]) / 2f;

                float letterDelay =
                    i * titlePopStagger;

                float localTime =
                    elapsed - letterDelay;

                float scale = 0f;

                if (localTime > 0f)
                {
                    float letterDur =
                        Mathf.Max(0.1f,
                        1f / titlePopFrequency);

                    float t =
                        Mathf.Clamp01(localTime / letterDur);

                    float overshoot =
                        1.70158f *
                        (1f + titlePopAmplitude);

                    float c3 = overshoot + 1f;

                    scale =
                        1f +
                        c3 * Mathf.Pow(t - 1f, 3f) +
                        overshoot * Mathf.Pow(t - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig =
                        cachedMeshInfo[matIndex]
                        .vertices[vertIndex + v];

                    Vector3 offset =
                        orig - charMid;

                    vertices[vertIndex + v] =
                        charMid + offset * scale;
                }
            }

            for (int m = 0;
                m < textInfo.meshInfo.Length;
                m++)
            {
                textInfo.meshInfo[m].mesh.vertices =
                    textInfo.meshInfo[m].vertices;

                titleText.UpdateGeometry(
                    textInfo.meshInfo[m].mesh, m);
            }

            yield return null;

            if (!revealed && titleCG != null)
            {
                titleCG.alpha = 1f;
                revealed = true;
            }
        }

        titleText.maxVisibleCharacters = 99999;
    }

    IEnumerator PopTextPerChar(
        TMP_Text tmp,
        float popDur = 1.2f,
        float charStagger = 0.04f,
        float popAmp = 0.6f,
        float popFreq = 4f)
    {
        if (tmp == null)
            yield break;

        tmp.ForceMeshUpdate();

        yield return null;

        tmp.ForceMeshUpdate();

        CanvasGroup cg = tmp.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = tmp.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 1f;

        tmp.maxVisibleCharacters = 0;

        int total = tmp.textInfo.characterCount;

        for (int i = 0; i <= total; i++)
        {
            tmp.maxVisibleCharacters = i;

            if (popClip && audioSource)
                audioSource.PlayOneShot(popClip);

            yield return new WaitForSeconds(charStagger);
        }

        tmp.maxVisibleCharacters = 99999;
    }

    IEnumerator PopButton(Transform btn)
    {
        if (popClip && audioSource)
            audioSource.PlayOneShot(popClip);

        btn.localScale = Vector3.zero;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;

            float clamped = Mathf.Clamp01(t);

            float overshoot = 1.70158f;

            float c1 = overshoot + 1f;

            float ease =
                1f +
                c1 * Mathf.Pow(clamped - 1f, 3f) +
                overshoot * Mathf.Pow(clamped - 1f, 2f);

            btn.localScale = Vector3.one * ease;

            yield return null;
        }

        btn.localScale = Vector3.one;
    }
}
