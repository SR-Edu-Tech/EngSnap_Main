using System.Collections;
using TMPro;
using UnityEngine;

public class UnitIntro_S1A : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    [Header("UI")]
    public RectTransform unitBanner;
    public TextMeshProUGUI titleText;
    public RectTransform[] contentElements;
    public GameObject nextButton;

    [Header("Swing Settings")]
    public float swingAngle = 20f;
    public float swingFrequency = 1.2f;
    public float swingDuration = 6f;

    [Header("Title Settings")]
    public float letterDelay = 0.08f;   // slower for long audio

    [Header("Content Settings")]
    public float contentStagger = 0.3f; // slower spacing
    public float contentAnimSpeed = 2.5f;

    public float nextDelay = 1f;

    void OnEnable()
    {
        StartIntro();
    }

    void OnDisable()
    {
        if (audioSource != null)
            audioSource.Stop();

        StopAllCoroutines();
    }

    void StartIntro()
    {
        ResetUI();
        nextButton.SetActive(false);

        StartCoroutine(IntroFlow());
    }

    // -------------------------
    // RESET
    // -------------------------
    void ResetUI()
    {
        unitBanner.localRotation = Quaternion.identity;

        // IMPORTANT: ensure TMP is ready
        titleText.ForceMeshUpdate();
        titleText.maxVisibleCharacters = 0;

        foreach (var t in contentElements)
            t.localScale = Vector3.zero;
    }

    // -------------------------
    // MAIN FLOW
    // -------------------------
    IEnumerator IntroFlow()
    {
        if (introClip != null)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        // Banner swing runs in parallel
        StartCoroutine(SwingMotion());

        // Title reveal
        yield return StartCoroutine(AnimateTitle());

        // Content after title finishes
        yield return StartCoroutine(AnimateContent());

        // Wait for audio
        if (introClip != null)
            yield return new WaitWhile(() => audioSource.isPlaying);

        yield return new WaitForSeconds(nextDelay);

        nextButton.SetActive(true);
    }

    // -------------------------
    // SWING (FRONT-BACK)
    // -------------------------
    IEnumerator SwingMotion()
    {
        float elapsed = 0f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;

            float progress = elapsed / swingDuration;
            float damp = Mathf.Exp(-1.5f * progress); // slower decay

            float angle =
                Mathf.Cos(elapsed * swingFrequency * Mathf.PI * 2f)
                * swingAngle * damp;

            unitBanner.localRotation = Quaternion.Euler(angle, 0f, 0f);

            yield return null;
        }

        unitBanner.localRotation = Quaternion.identity;
    }

    // -------------------------
    // LETTER REVEAL (FIXED TMP)
    // -------------------------
    IEnumerator AnimateTitle()
    {
        // Wait 1 frame so TMP fully initializes
        yield return null;

        titleText.ForceMeshUpdate();

        int totalChars = titleText.textInfo.characterCount;

        if (totalChars == 0)
            yield break;

        for (int i = 0; i <= totalChars; i++)
        {
            titleText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(letterDelay);
        }
    }

    // -------------------------
    // CONTENT (SLOW SCALE IN)
    // -------------------------
    IEnumerator AnimateContent()
    {
        foreach (var t in contentElements)
        {
            StartCoroutine(ScaleIn(t));
            yield return new WaitForSeconds(contentStagger);
        }
    }

    IEnumerator ScaleIn(RectTransform t)
    {
        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * contentAnimSpeed;
            t.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time);
            yield return null;
        }

        t.localScale = Vector3.one;
    }
}