using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ReadingMultiLineScroll_S2A : MonoBehaviour
{
    [System.Serializable]
    public class Option
    {
        public Button button;

        [Header("MULTIPLE LINES")]
        public TMP_Text[] lines;

        public Image bg;
        public Image highlightImage;

        public AudioClip audioClip;

        [HideInInspector] public bool hasPlayed = false;
    }

    [Header("UI Containers")]
    public TMP_Text titleText;
    public Transform[] characterContainers;
    public Transform bubbleContainer;
    public Transform optionsContainer;

    [Header("Scroll View")]
    public ScrollRect scrollRect;

    [Range(0f, 1f)]
    public float autoScrollAmount = 0.18f;

    public float scrollDuration = 0.4f;

    [Header("Options")]
    public Option[] options;

    [Header("Buttons")]
    public GameObject nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip popClip;

    [Header("Colors")]
    public Color normalText = Color.black;
    public Color autoPlayText = Color.yellow;
    public Color manualPlayText = Color.cyan;

    public Color normalBG = Color.white;
    public Color visitedBG = Color.gray;

    [Header("Animation Settings")]
    public float popSpeed = 5f;
    public float staggerDelay = 0.1f;

    [Header("Title Pop")]
    public float popDuration = 1.75f;
    public float popAmplitude = 0.75f;
    public float popFrequency = 4f;
    public float popStagger = 0.05f;

    [Header("Smooth Settings")]
    public float manualColorFadeSpeed = 12f;

    private bool canInteract = false;

    private Coroutine currentAudioRoutine;
    private Option currentPlayingOption;

    void OnEnable()
    {
        ResetUIState();
        ResetGame();
        StartCoroutine(IntroFlow());
    }

    void OnDisable()
    {
        if (audioSource != null)
            audioSource.Stop();

        StopAllCoroutines();
    }

    void ResetUIState()
    {
        CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();

        if (titleCG == null)
            titleCG = titleText.gameObject.AddComponent<CanvasGroup>();

        titleCG.alpha = 0f;

        titleText.ForceMeshUpdate();

        foreach (var ch in characterContainers)
            ch.localScale = Vector3.zero;

        bubbleContainer.localScale = Vector3.zero;

        optionsContainer.localScale = Vector3.one;

        foreach (Transform child in optionsContainer)
            child.localScale = Vector3.zero;

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    void ResetGame()
    {
        nextButton.SetActive(false);
        canInteract = false;

        currentPlayingOption = null;

        foreach (var opt in options)
        {
            opt.hasPlayed = false;

            if (opt.bg != null)
                opt.bg.color = normalBG;

            foreach (var line in opt.lines)
            {
                if (line == null) continue;

                line.color = normalText;

                CanvasGroup cg = line.GetComponent<CanvasGroup>();

                if (cg == null)
                    cg = line.gameObject.AddComponent<CanvasGroup>();

                cg.alpha = 0f;
            }

            if (opt.highlightImage != null)
                opt.highlightImage.gameObject.SetActive(false);

            opt.button.onClick.RemoveAllListeners();

            Option captured = opt;

            opt.button.onClick.AddListener(() => OnOptionClicked(captured));
        }
    }

    IEnumerator IntroFlow()
    {
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        StartCoroutine(TitleAnim());

        yield return new WaitForSeconds(0.3f);

        foreach (var ch in characterContainers)
            StartCoroutine(PopCharacter(ch));

        yield return StartCoroutine(PopIn(bubbleContainer));

        yield return StartCoroutine(AnimateOptions());

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        yield return AutoPlayOptions();

        canInteract = true;
    }

    IEnumerator AutoPlayOptions()
    {
        foreach (var opt in options)
            ResetVisual(opt);

        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        yield return null;

        foreach (var opt in options)
        {
            yield return PlayAudio(opt, false);

            // AUTO SCROLL
            if (scrollRect != null)
            {
                float current =
                    scrollRect.verticalNormalizedPosition;

                float target =
                    current - autoScrollAmount;

                target = Mathf.Clamp01(target);

                float elapsed = 0f;

                while (elapsed < scrollDuration)
                {
                    elapsed += Time.deltaTime;

                    scrollRect.verticalNormalizedPosition =
                        Mathf.Lerp(
                            current,
                            target,
                            elapsed / scrollDuration
                        );

                    yield return null;
                }

                scrollRect.verticalNormalizedPosition = target;
            }

            yield return new WaitForSeconds(0.15f);
        }
    }

    void OnOptionClicked(Option opt)
    {
        if (!canInteract)
            return;

        bool wasPlayed = opt.hasPlayed;

        if (!wasPlayed)
        {
            opt.hasPlayed = true;

            if (opt.bg != null)
                opt.bg.color = visitedBG;
        }

        if (currentAudioRoutine != null)
            StopCoroutine(currentAudioRoutine);

        if (audioSource != null)
            audioSource.Stop();

        if (currentPlayingOption != null)
            ResetVisual(currentPlayingOption);

        currentPlayingOption = opt;

        currentAudioRoutine = StartCoroutine(PlayAudio(opt, true));

        if (!wasPlayed)
            CheckCompletion();
    }

    IEnumerator PlayAudio(Option opt, bool isManual)
    {
        Color targetColor = isManual ? manualPlayText : autoPlayText;

        foreach (var line in opt.lines)
        {
            if (line == null) continue;

            if (isManual)
                StartCoroutine(SmoothColorTransition(line, targetColor));
            else
                line.color = targetColor;
        }

        if (opt.highlightImage != null)
        {
            opt.highlightImage.color = targetColor;
            opt.highlightImage.gameObject.SetActive(true);
        }

        opt.button.transform.localScale = Vector3.one * 1.1f;

        if (opt.audioClip && audioSource)
        {
            audioSource.clip = opt.audioClip;
            audioSource.Play();

            yield return new WaitForSeconds(opt.audioClip.length);
        }

        if (!isManual)
        {
            ResetVisual(opt);
        }
        else
        {
            if (currentPlayingOption == opt)
            {
                ResetVisual(opt);
                currentPlayingOption = null;
            }
        }
    }

    IEnumerator SmoothColorTransition(TMP_Text text, Color target)
    {
        if (text == null)
            yield break;

        Color start = text.color;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * manualColorFadeSpeed;

            text.color = Color.Lerp(start, target, t);

            yield return null;
        }

        text.color = target;
    }

    void ResetVisual(Option opt)
    {
        foreach (var line in opt.lines)
        {
            if (line == null) continue;

            if (opt.hasPlayed)
                StartCoroutine(SmoothColorTransition(line, normalText));
            else
                line.color = normalText;
        }

        if (opt.highlightImage != null)
            opt.highlightImage.gameObject.SetActive(false);

        opt.button.transform.localScale = Vector3.one;

        if (opt.bg != null)
            opt.bg.color = opt.hasPlayed ? visitedBG : normalBG;
    }

    void CheckCompletion()
    {
        foreach (var opt in options)
        {
            if (!opt.hasPlayed)
                return;
        }

        if (!nextButton.activeSelf)
        {
            nextButton.SetActive(true);

            StartCoroutine(PopButton(nextButton.transform));
        }
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

        if (charCount == 0)
            yield break;

        titleText.maxVisibleCharacters = charCount;

        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();

        bool revealed = false;

        float elapsed = 0f;

        float expectedTime =
            (charCount * popStagger) +
            Mathf.Max(0.5f, 1f / popFrequency);

        float totalDuration =
            Mathf.Max(popDuration, expectedTime);

        while (elapsed < totalDuration)
        {
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

                float letterDelay = i * popStagger;

                float localTime =
                    elapsed - letterDelay;

                float scale = 0f;

                if (localTime > 0f)
                {
                    float letterDur =
                        Mathf.Max(0.1f, 1f / popFrequency);

                    float t =
                        Mathf.Clamp01(localTime / letterDur);

                    float overshoot =
                        1.70158f * (1f + popAmplitude);

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

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices =
                    textInfo.meshInfo[m].vertices;

                titleText.UpdateGeometry(
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

        textInfo = titleText.textInfo;

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices =
                cachedMeshInfo[i].vertices;

            titleText.UpdateGeometry(
                textInfo.meshInfo[i].mesh,
                i
            );
        }
    }

    IEnumerator PopIn(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;

            target.localScale =
                Vector3.Lerp(Vector3.zero, Vector3.one, t);

            yield return null;
        }
    }

    IEnumerator PopCharacter(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * 3f;

            float clamped = Mathf.Clamp01(t);

            float easeOut =
                1f - Mathf.Pow(1f - clamped, 3f);

            target.localScale =
                Vector3.one *
                Mathf.Lerp(0f, 1.15f, easeOut);

            yield return null;
        }

        t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * 4f;

            float clamped = Mathf.Clamp01(t);

            float smooth =
                1f - Mathf.Pow(1f - clamped, 2f);

            target.localScale =
                Vector3.one *
                Mathf.Lerp(1.15f, 1f, smooth);

            yield return null;
        }

        target.localScale = Vector3.one;
    }

    IEnumerator AnimateOptions()
    {
        int optIndex = 0;

        foreach (Transform child in optionsContainer)
        {
            Option opt =
                optIndex < options.Length
                ? options[optIndex]
                : null;

            if (opt != null)
            {
                foreach (var line in opt.lines)
                {
                    if (line != null)
                        StartCoroutine(PopTextPerChar(line));
                }
            }

            if (popClip && audioSource)
                audioSource.PlayOneShot(popClip);

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

                child.localScale = Vector3.one * ease;

                yield return null;
            }

            child.localScale = Vector3.one;

            optIndex++;

            yield return new WaitForSeconds(staggerDelay);
        }
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

        TMP_TextInfo textInfo = tmp.textInfo;

        int charCount = textInfo.characterCount;

        if (charCount == 0)
            yield break;

        tmp.maxVisibleCharacters = charCount;

        TMP_MeshInfo[] cachedMeshInfo =
            textInfo.CopyMeshInfoVertexData();

        CanvasGroup cg =
            tmp.GetComponent<CanvasGroup>();

        bool revealed = false;

        float expectedTime =
            (charCount * charStagger) +
            Mathf.Max(0.5f, 1f / popFreq);

        float totalDuration =
            Mathf.Max(popDur, expectedTime);

        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
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
                    (vertices[vertIdx] +
                    vertices[vertIdx + 2]) / 2f;

                float delay = i * charStagger;

                float localTime =
                    elapsed - delay;

                float scale = 0f;

                if (localTime > 0f)
                {
                    float letterDur =
                        Mathf.Max(0.1f, 1f / popFreq);

                    float lt =
                        Mathf.Clamp01(localTime / letterDur);

                    float overshoot =
                        1.70158f * (1f + popAmp);

                    float c3 = overshoot + 1f;

                    scale =
                        1f +
                        c3 * Mathf.Pow(lt - 1f, 3f) +
                        overshoot * Mathf.Pow(lt - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig =
                        cachedMeshInfo[matIdx]
                        .vertices[vertIdx + v];

                    Vector3 offset =
                        orig - charMid;

                    vertices[vertIdx + v] =
                        charMid + offset * scale;
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices =
                    textInfo.meshInfo[m].vertices;

                tmp.UpdateGeometry(
                    textInfo.meshInfo[m].mesh,
                    m
                );
            }

            yield return null;

            if (!revealed && cg != null)
            {
                cg.alpha = 1f;

                revealed = true;
            }
        }

        if (cg != null)
            cg.alpha = 1f;

        textInfo = tmp.textInfo;

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices =
                cachedMeshInfo[i].vertices;

            tmp.UpdateGeometry(
                textInfo.meshInfo[i].mesh,
                i
            );
        }
    }

    IEnumerator PopButton(Transform btn)
    {
        if (popClip && audioSource)
            audioSource.PlayOneShot(popClip);

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * 5f;

            float clamped = Mathf.Clamp01(t);

            btn.localScale =
                Vector3.Lerp(
                    Vector3.zero,
                    Vector3.one * 1.15f,
                    clamped
                );

            yield return null;
        }

        t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * 10f;

            float clamped = Mathf.Clamp01(t);

            float smooth =
                1f - Mathf.Pow(1f - clamped, 2f);

            btn.localScale =
                Vector3.Lerp(
                    Vector3.one * 1.15f,
                    Vector3.one,
                    smooth
                );

            yield return null;
        }

        btn.localScale = Vector3.one;
    }
}
