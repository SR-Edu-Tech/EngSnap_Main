using System.Collections;
using TMPro;
using UnityEngine;

public class IntroScreenUnit1_S1A : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    [Header("UI")]
    public RectTransform unitBanner;
    public RectTransform character;
    public TMP_Text titleText;
    public CanvasGroup[] contentTexts;
    public GameObject nextButton;

    [Header("Swing")]
    public float swingAngle = 90f;
    public float swingFrequency = 0.5f;
    public float swingDuration = 5f;

    [Header("Title Pop")]
    public float popDuration = 1f;
    public float popAmplitude = 1f;
    public float frequency = 4f;
    public float stagger = 0.05f;

    [Header("Timings")]
    public float contentDelay = 0.3f;
    public float fadeSpeed = 3f;
    public float nextDelay = 1f;

    void OnEnable()
    {
        ResetUI();
        StartCoroutine(IntroFlow());
    }

    void OnDisable()
    {
        if (audioSource) audioSource.Stop();
        StopAllCoroutines();
    }

    // -------------------------
    // RESET
    // -------------------------
    void ResetUI()
    {
        unitBanner.localRotation = Quaternion.identity;

        character.localScale = Vector3.zero;

        // Hide title until AnimateTitle is ready
        CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();
        if (titleCG != null)
            titleCG.alpha = 0f;

        titleText.ForceMeshUpdate();

        foreach (var c in contentTexts)
        {
            c.alpha = 0f;
        }

        nextButton.SetActive(false);
    }

    // -------------------------
    // MAIN FLOW
    // -------------------------
    IEnumerator IntroFlow()
    {
        if (introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        // Start banner swing
        StartCoroutine(SwingMotion());

        // IMPORTANT FIX: give banner a head-start
        yield return new WaitForSeconds(1f);

        // Title animation
        // Character pop starts alongside title (fire and forget)
        StartCoroutine(PopCharacter());
        yield return StartCoroutine(AnimateTitle());

        // Content fade
        yield return StartCoroutine(FadeContent());

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        yield return new WaitForSeconds(nextDelay);

        nextButton.SetActive(true);
    }

    // -------------------------
    // TITLE POP
    // -------------------------
    IEnumerator AnimateTitle()
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

        while (elapsed < popDuration)
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

                Vector3 mid = (vertices[vertIndex] + vertices[vertIndex + 2]) / 2f;

                float delay = i * stagger;
                float localTime = elapsed - delay;

                float scale = 0f;

                if (localTime > 0f)
                {
                    float t = Mathf.Clamp01(localTime * frequency);

                    float overshoot = 1.70158f * (1f + popAmplitude);
                    float c3 = overshoot + 1f;

                    scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIndex].vertices[vertIndex + v];
                    Vector3 offset = orig - mid;
                    vertices[vertIndex + v] = mid + offset * scale;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                titleText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return null;

            // Reveal after the first frame has rendered at alpha 0
            // (prevents TMP's internal mesh regeneration from flashing the full text)
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

    // -------------------------
    // SWING
    // -------------------------
    IEnumerator SwingMotion()
    {
        float elapsed = 0f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / swingDuration;
            float damp = Mathf.Exp(-2.2f * progress);

            float angle =
                Mathf.Cos(elapsed * swingFrequency * Mathf.PI * 2f)
                * swingAngle * damp;

            unitBanner.localRotation = Quaternion.Euler(angle, 0f, 0f);

            yield return null;
        }

        float t = 0f;
        Quaternion start = unitBanner.localRotation;

        while (t < 1f)
        {
            t += Time.deltaTime / 0.25f;
            unitBanner.localRotation = Quaternion.Slerp(start, Quaternion.identity, t);
            yield return null;
        }
    }

    // -------------------------
    // CHARACTER POP
    // -------------------------
    IEnumerator PopCharacter()
    {
        // Phase 1: Scale from 0 to 1.15 with ease-out (fast start, slows down)
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 3f;
            float clamped = Mathf.Clamp01(t);
            float easeOut = 1f - Mathf.Pow(1f - clamped, 3f);
            character.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, easeOut);
            yield return null;
        }

        // Phase 2: Settle from 1.15 back to 1.0
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 4f;
            float clamped = Mathf.Clamp01(t);
            float smooth = 1f - Mathf.Pow(1f - clamped, 2f);
            character.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, smooth);
            yield return null;
        }

        character.localScale = Vector3.one;
    }

    // -------------------------
    // CONTENT FADE
    // -------------------------
    IEnumerator FadeContent()
    {
        foreach (var c in contentTexts)
        {
            StartCoroutine(FadeIn(c));
            yield return new WaitForSeconds(contentDelay);
        }
    }

    IEnumerator FadeIn(CanvasGroup cg)
    {
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;
            cg.alpha = t;
            yield return null;
        }

        cg.alpha = 1;
    }
}