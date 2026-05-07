using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class DialoguePair
{
    public Button greetingButton;
    public Button responseButton;

    public RectTransform container;

    public TMP_Text greetingText;
    public TMP_Text responseText;

    public AudioClip greetingAudio;
    public AudioClip responseAudio;

    public GameObject greetingSpeakerIcon;
    public GameObject responseSpeakerIcon;

    [HideInInspector] public Vector3 originalScale;
    [HideInInspector] public bool visited;
    [HideInInspector] public bool hasBouncedIn;
}

public class MyDetailsTable_S1A : MonoBehaviour
{
    [Header("UI")]
    public RectTransform title;
    public RectTransform board;
    public RectTransform greetingsHeader;
    public RectTransform responsesHeader;

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
    public Color normalColor = Color.black;
    public Color highlightColor = new Color(1f, 0.85f, 0.3f);
    public Color visitedColor = Color.gray;

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
        HideTextSafely(greetingsHeader.GetComponent<TMP_Text>());
        HideTextSafely(responsesHeader.GetComponent<TMP_Text>());

        foreach (var p in pairs)
        {
            p.originalScale = Vector3.one;
            p.container.localScale = Vector3.one; 
            
            p.greetingButton.transform.localScale = Vector3.zero;
            p.responseButton.transform.localScale = Vector3.zero;

            p.visited = false;
            p.hasBouncedIn = false;

            p.greetingText.color = normalColor;
            p.responseText.color = normalColor;

            HideTextSafely(p.greetingText);
            HideTextSafely(p.responseText);

            if (p.greetingSpeakerIcon) p.greetingSpeakerIcon.SetActive(false);
            if (p.responseSpeakerIcon) p.responseSpeakerIcon.SetActive(false);
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

    void SetupButtons()
    {
        foreach (var pair in pairs)
        {
            DialoguePair captured = pair;

            pair.greetingButton.onClick.RemoveAllListeners();
            pair.responseButton.onClick.RemoveAllListeners();

            pair.greetingButton.onClick.AddListener(() => OnPairClicked(captured));
            pair.responseButton.onClick.AddListener(() => OnPairClicked(captured));
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

        StartCoroutine(TitleAnim(greetingsHeader.GetComponent<TMP_Text>()));
        StartCoroutine(TitleAnim(responsesHeader.GetComponent<TMP_Text>()));
        
        yield return new WaitForSeconds(0.4f);

        for (int i = 0; i < pairs.Length; i++)
        {
            yield return ScrollTo(pairs[i].container);
            
            if (popSfx) audioSource.PlayOneShot(popSfx);
            yield return BounceIn(pairs[i].greetingButton.transform);
            
            if (popSfx) audioSource.PlayOneShot(popSfx);
            yield return BounceIn(pairs[i].responseButton.transform);
            
            pairs[i].hasBouncedIn = true;
        }

        if (pairs.Length > 0)
        {
            yield return ScrollTo(pairs[0].container);
        }

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        for (int i = 0; i < pairs.Length; i++)
        {
            yield return StartCoroutine(PlayPairSequence(pairs[i]));
            yield return new WaitForSeconds(delayBetweenPairs);
        }

        isAutoPlaying = false;
    }

    void OnPairClicked(DialoguePair pair)
    {
        if (isAutoPlaying) return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (audioSource.isPlaying)
            audioSource.Stop();

        ResetAllPairs();

        currentRoutine = StartCoroutine(PlayPairSequence(pair));
    }

    IEnumerator PlayPairSequence(DialoguePair pair)
    {
        yield return ScrollTo(pair.container);

        StartCoroutine(Pulse(pair.greetingButton.transform, 1.1f, 0.15f));
        StartCoroutine(Pulse(pair.responseButton.transform, 1.1f, 0.15f));

        // GREETING PART
        HighlightGreeting(pair, true);

        if (!pair.visited) 
            StartCoroutine(PopTextPerChar(pair.greetingText));

        if (promptGreeting)
        {
            audioSource.clip = promptGreeting;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        if (pair.greetingAudio)
        {
            audioSource.clip = pair.greetingAudio;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        HighlightGreeting(pair, false);

        // RESPONSE PART
        HighlightResponse(pair, true);

        if (!pair.visited) 
            StartCoroutine(PopTextPerChar(pair.responseText));

        if (promptResponse)
        {
            audioSource.clip = promptResponse;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        if (pair.responseAudio)
        {
            audioSource.clip = pair.responseAudio;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        HighlightResponse(pair, false);

        // MARK VISITED
        pair.visited = true;
        ApplyVisitedColor(pair);
    }

    void HighlightGreeting(DialoguePair pair, bool state)
    {
        pair.greetingText.color = state ? highlightColor : (pair.visited ? visitedColor : normalColor);

        if (pair.greetingSpeakerIcon)
            pair.greetingSpeakerIcon.SetActive(state);
    }

    void HighlightResponse(DialoguePair pair, bool state)
    {
        pair.responseText.color = state ? highlightColor : (pair.visited ? visitedColor : normalColor);

        if (pair.responseSpeakerIcon)
            pair.responseSpeakerIcon.SetActive(state);
    }

    void ApplyVisitedColor(DialoguePair pair)
    {
        pair.greetingText.color = visitedColor;
        pair.responseText.color = visitedColor;
    }

    void ResetAllPairs()
    {
        foreach (var p in pairs)
        {
            if (!p.visited)
            {
                p.greetingText.color = normalColor;
                p.responseText.color = normalColor;
            }
            else
            {
                ApplyVisitedColor(p);
            }

            if (p.greetingSpeakerIcon) p.greetingSpeakerIcon.SetActive(false);
            if (p.responseSpeakerIcon) p.responseSpeakerIcon.SetActive(false);

            if (p.hasBouncedIn)
            {
                p.greetingButton.transform.localScale = Vector3.one;
                p.responseButton.transform.localScale = Vector3.one;
            }
        }
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

    IEnumerator Pulse(Transform target, float scale, float time)
    {
        Vector3 original = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / time;
            target.localScale = Vector3.Lerp(original, Vector3.one * scale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / time;
            target.localScale = Vector3.Lerp(Vector3.one * scale, original, t);
            yield return null;
        }
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

        // Add 50 units of breathing room above the target so it's not glued to the top edge
        float targetY = Mathf.Abs(target.anchoredPosition.y) - 50f;
        targetY = Mathf.Max(0f, targetY);

        // If it's the very first pair, guarantee we scroll completely to the absolute top
        if (pairs.Length > 0 && target == pairs[0].container)
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
                    vertices[vertIndex + v] = charMid + offset * scale;
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