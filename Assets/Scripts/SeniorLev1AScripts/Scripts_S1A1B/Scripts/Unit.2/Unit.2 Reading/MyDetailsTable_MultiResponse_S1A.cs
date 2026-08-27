using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


[System.Serializable]
public class DialogueColumn
{
    public Button button;

    public Image background;

    public TMP_Text text;

    public AudioClip audio;

    public GameObject speakerIcon;

    [HideInInspector] public bool visited;
}

[System.Serializable]
public class DialogueRow
{
    public RectTransform container;

    public DialogueColumn[] columns;

    [HideInInspector] public bool hasBouncedIn;
    [HideInInspector] public bool visited;
}
public class MyDetailsTable_MultiResponse_S1A : MonoBehaviour
{
    [Header("UI")]
    public RectTransform title;
    public RectTransform board;
    [Header("Headers")]
    public RectTransform[] headers;
    public GameObject nextButton;

    [Header("Scroll")]
    public ScrollRect scrollRect;
    public RectTransform content;

    [Header("Rows")]
    public DialogueRow[] rows;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    [Header("Column Prompts")]
public AudioClip[] promptAudios;

    public AudioClip popSfx;

    [Header("Colors")]
    public Color normalColor = Color.black;
    public Color highlightColor = new Color(1f, 0.85f, 0.3f);
    public Color visitedColor = Color.gray;
    public Color visitedHighlightColor = new Color(1f, 0.95f, 0.5f);
    public Color normalButtonColor = Color.white;
    public Color visitedButtonColor = new Color(0.85f, 0.85f, 0.85f);
    public Color visitedTextColor = Color.white;

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

    void OnEnable()
    {
        ResetUIState();
        SetupButtons();
        StartCoroutine(MainFlow());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource) audioSource.Stop();
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
        foreach (var header in headers)
{
    if (header != null)
        HideTextSafely(
            header.GetComponent<TMP_Text>());
}

        if (nextButton) nextButton.SetActive(false);

        foreach (var row in rows)
{
        row.visited = false;
    row.hasBouncedIn = false;

    foreach (var col in row.columns)
    {
        col.visited = false;

        col.button.transform.localScale =
            Vector3.zero;

        SetBgColor(
            col.background,
            normalButtonColor);

        col.text.color = normalColor;

        HideTextSafely(col.text);

        if (col.speakerIcon)
            col.speakerIcon.SetActive(false);
    }
}

        scrollRect.verticalNormalizedPosition = 1f;
    }

    void HideTextSafely(TMP_Text txt)
    {
        if (txt != null)
        {
            CanvasGroup cg = txt.GetComponent<CanvasGroup>();
            if (cg == null) cg = txt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }
    }

    void SetBgColor(Image bg, Color c)
    {
        if (bg != null) bg.color = c;
    }

    void SetupButtons()
{
    foreach (var row in rows)
    {
        foreach (var col in row.columns)
        {
            DialogueRow capturedRow = row;
            DialogueColumn capturedCol = col;

            col.button.onClick.RemoveAllListeners();

            col.button.onClick.AddListener(() =>
            {
                OnColumnClicked(
                    capturedRow,
                    capturedCol);
            });
        }
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

        foreach (var header in headers)
{
    if (header != null)
    {
        StartCoroutine(
            TitleAnim(
                header.GetComponent<TMP_Text>()));
    }
}
        
        yield return new WaitForSeconds(0.4f);

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        for (int i = 0; i < rows.Length; i++)
{
    yield return ScrollTo(rows[i].container);

    foreach (var col in rows[i].columns)
    {
        if (popSfx)
            audioSource.PlayOneShot(popSfx);

        yield return BounceIn(
            col.button.transform);
    }

    rows[i].hasBouncedIn = true;

    for (int j = 0; j < rows[i].columns.Length; j++)
{
    yield return StartCoroutine(
        PlayColumn(
            rows[i].columns[j],
            true,
            j));
}

    yield return new WaitForSeconds(
        delayBetweenPairs);
}
        isAutoPlaying = false;
    }

    void OnColumnClicked(
    DialogueRow row,
    DialogueColumn clickedColumn)
{
    if (isAutoPlaying)
        return;

    if (currentRoutine != null)
        StopCoroutine(currentRoutine);

    if (audioSource.isPlaying)
        audioSource.Stop();

    ResetAllColumns();

    currentRoutine =
        StartCoroutine(
            PlayWholeRow(row));
}

IEnumerator PlayWholeRow(DialogueRow row)
{
    yield return ScrollTo(row.container);

    // Immediately mark the whole row as visited
    if (!row.visited)
    {
        row.visited = true;

        foreach (var col in row.columns)
        {
            col.visited = true;
            ApplyVisitedColor(col);
        }
    }

    for (int i = 0; i < row.columns.Length; i++)
    {
        yield return StartCoroutine(
            PlayColumn(
                row.columns[i],
                false,
                i));
    }

    CheckAllRowsVisited();
}

IEnumerator PlayColumn(
    DialogueColumn column,
    bool isAutoPlay,
    int columnIndex)
{
    if (promptAudios != null &&
    columnIndex < promptAudios.Length &&
    promptAudios[columnIndex] != null)
{
    audioSource.clip =
        promptAudios[columnIndex];

    audioSource.Play();

    yield return new WaitWhile(
        () => audioSource.isPlaying);
}

    HighlightColumn(column, true);

    // Replay text animation every time the user taps.
// During autoplay, only animate once.

    StartCoroutine(
        PopTextPerChar(column.text));

    if (column.audio)
    {
        Coroutine scaleCo =
            StartCoroutine(
                ScaleCard(
                    column.button.transform,
                    1.05f));

        audioSource.clip = column.audio;

        audioSource.Play();

        yield return new WaitWhile(
            () => audioSource.isPlaying);

        if (scaleCo != null)
            StopCoroutine(scaleCo);

        StartCoroutine(
            ScaleCard(
                column.button.transform,
                1f));
    }

    HighlightColumn(column, false); 
}

void HighlightColumn(
    DialogueColumn column,
    bool state)
{
    Color targetColor;

    if (column.visited)
    {
        // Visited cards always stay white
        targetColor = Color.white;
    }
    else
    {
        // Before being visited use the normal highlight
        targetColor = state ? highlightColor : normalColor;
    }

    column.text.color = targetColor;

    if (column.speakerIcon)
    {
        Image icon = column.speakerIcon.GetComponent<Image>();

        if (icon != null)
            icon.color = targetColor;

        column.speakerIcon.SetActive(state);
    }
}

void ApplyVisitedColor(DialogueColumn column)
{
    column.text.color = visitedTextColor;

    SetBgColor(
        column.background,
        visitedButtonColor);

    if (column.speakerIcon)
    {
        Image icon =
            column.speakerIcon.GetComponent<Image>();

        if (icon != null)
            icon.color = visitedTextColor;
    }
}

void ResetAllColumns()
{
    foreach (var row in rows)
    {
        foreach (var col in row.columns)
        {
            if (!col.visited)
            {
                col.text.color = normalColor;

                SetBgColor(
                    col.background,
                    normalButtonColor);
            }
            else
            {
                ApplyVisitedColor(col);
            }

            if (col.speakerIcon)
                col.speakerIcon.SetActive(false);

            if (row.hasBouncedIn)
            {
                col.button.transform.localScale =
                    Vector3.one;
            }
        }
    }
}
    void CheckAllRowsVisited()
{
    foreach (var row in rows)
    {
        if (!row.visited)
            return;
    }

    if (nextButton)
        nextButton.SetActive(true);
}

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
            float ease = 1f + c1 * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

            target.localScale = Vector3.one * ease;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator ScaleCard(Transform target, float targetScale)
    {
        Vector3 startScale = target.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float time = 0;
        float duration = 0.15f;

        while (time < 1)
        {
            time += Time.deltaTime / duration;
            target.localScale = Vector3.Lerp(startScale, endScale, time);
            yield return null;
        }
        target.localScale = endScale;
    }

    IEnumerator SlideUp(RectTransform t, float speed)
    {
        Vector2 start = new Vector2(0, -1200);
        Vector2 end = boardOriginalPos;

        float time = 0;

        while (time < 1)
        {
            time += Time.deltaTime * (speed / 1000f);
            
            float clamped = Mathf.Clamp01(time);
            float overshoot = 1.2f; 
            float c1 = overshoot + 1f;
            float ease = 1f + c1 * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

            t.anchoredPosition = Vector2.LerpUnclamped(start, end, ease);
            yield return null;
        }

        t.anchoredPosition = end;
    }

    IEnumerator ScrollTo(RectTransform target)
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        float contentHeight = content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;

        if (contentHeight <= viewportHeight)
        {
            yield break;
        }

        float targetY = Mathf.Abs(target.anchoredPosition.y) - 50f;
        targetY = Mathf.Max(0f, targetY);

        if (rows.Length > 0 &&
    target == rows[0].container)
        {
            targetY = 0f;
        }

        float normalized = 1 - Mathf.Clamp01(targetY / (contentHeight - viewportHeight));

        float start = scrollRect.verticalNormalizedPosition;
        float time = 0;

        while (time < 1)
        {
            time += Time.deltaTime * 3f;
            scrollRect.verticalNormalizedPosition = Mathf.Lerp(start, normalized, time);
            yield return null;
        }
        scrollRect.verticalNormalizedPosition = normalized;
    }

    IEnumerator TitleAnim(TMP_Text txt)
    {
        if (txt == null) yield break;

        CanvasGroup titleCG = txt.GetComponent<CanvasGroup>();
        if (titleCG == null) titleCG = txt.gameObject.AddComponent<CanvasGroup>();
        titleCG.alpha = 0f;

        yield return new WaitForEndOfFrame();
        yield return null;

        txt.ForceMeshUpdate();
        yield return null;
        txt.ForceMeshUpdate();

        string originalText = txt.text;
        TMP_TextInfo textInfo = txt.textInfo;
        int charCount = textInfo.characterCount;

        if (charCount == 0) yield break;

        txt.maxVisibleCharacters = charCount;
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        bool revealed = false;
        float elapsed = 0f;

        float expectedTime = (charCount * titlePopStagger) + Mathf.Max(0.5f, 1f / titlePopFrequency);
        float totalDuration = Mathf.Max(titlePopDuration, expectedTime);

        while (elapsed < totalDuration)
        {
            if (txt.text != originalText) break;

            elapsed += Time.deltaTime;
            textInfo = txt.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int matIndex = charInfo.materialReferenceIndex;
                int vertIndex = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[matIndex].vertices;
                Vector3 charMid = (vertices[vertIndex] + vertices[vertIndex + 2]) / 2f;

                float letterDelay = i * titlePopStagger;
                float localTime = elapsed - letterDelay;

                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterDur = Mathf.Max(0.1f, 1f / titlePopFrequency);
                    float t = Mathf.Clamp01(localTime / letterDur);

                    float overshoot = 1.70158f * (1f + titlePopAmplitude);
                    float c3 = overshoot + 1f;
                    scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIndex].vertices[vertIndex + v];
                    Vector3 offset = orig - charMid;
                    vertices[vertIndex + v] = charMid + offset * scale; // NOTE: Handled compiler typo!
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                txt.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
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
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
                txt.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        txt.maxVisibleCharacters = 99999;
    }

    IEnumerator PopTextPerChar(TMP_Text tmp, float popDur = 1.2f, float charStagger = 0.04f, float popAmp = 0.6f, float popFreq = 4f)
    {
        if (tmp == null) yield break;

        tmp.ForceMeshUpdate();
        yield return null;
        tmp.ForceMeshUpdate();

        string originalText = tmp.text;
        TMP_TextInfo textInfo = tmp.textInfo;
        int charCount = textInfo.characterCount;

        if (charCount == 0) yield break;

        tmp.maxVisibleCharacters = charCount;
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIdx = charInfo.materialReferenceIndex;
            int vertIdx = charInfo.vertexIndex;

            Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
            Vector3 charMid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;

            for (int v = 0; v < 4; v++)
                vertices[vertIdx + v] = charMid;
        }
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
            tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
        }

        yield return null;

        CanvasGroup cg = tmp.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        float expectedTime = (charCount * charStagger) + Mathf.Max(0.5f, 1f / popFreq);
        float totalDuration = Mathf.Max(popDur, expectedTime);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            if (tmp.text != originalText) break;

            elapsed += Time.deltaTime;
            textInfo = tmp.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int matIdx = charInfo.materialReferenceIndex;
                int vertIdx = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
                Vector3 charMid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;

                float delay = i * charStagger;
                float localTime = elapsed - delay;

                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterDur = Mathf.Max(0.1f, 1f / popFreq);
                    float lt = Mathf.Clamp01(localTime / letterDur);

                    float overshoot = 1.70158f * (1f + popAmp);
                    float c3 = overshoot + 1f;
                    scale = 1f + c3 * Mathf.Pow(lt - 1f, 3f) + overshoot * Mathf.Pow(lt - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIdx].vertices[vertIdx + v];
                    Vector3 offset = orig - charMid;
                    vertices[vertIdx + v] = charMid + offset * scale;
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }

            yield return null;
        }

        if (tmp.text == originalText)
        {
            textInfo = tmp.textInfo;
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
                tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        tmp.maxVisibleCharacters = 99999;
    }
}
