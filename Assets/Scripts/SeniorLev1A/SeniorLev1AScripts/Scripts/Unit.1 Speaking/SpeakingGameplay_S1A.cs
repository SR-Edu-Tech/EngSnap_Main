using System.Collections;
using UnityEngine;
using TMPro;

public class SpeakingGameplay_S1A : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public Transform titleContainer;

    public TMP_Text instructionText;
    public Transform instructionContainer;

    public TMP_Text sentenceText;
    public Transform sentenceContainer;   // FULL BOARD (Image + Text)

    public Transform micIcon;

    public GameObject nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip noInputSFX;

    [Header("Sentences")]
    public string[] sentences;

    [Header("Animation Settings")]
    public float titleDropHeight = 300f;
    public float titleSpeed = 3f;
    public float popSpeed = 5f;
    public float bounceScale = 1.15f;
    public float shakeAmount = 10f;

    private int currentIndex = 0;
    private bool waitingForNext = false;
    private bool canInteract = false;

    // -----------------------------
    // HIDE EVERYTHING BEFORE FIRST FRAME
    // -----------------------------
    void Awake()
    {
        titleContainer.localScale = Vector3.zero;
        instructionContainer.localScale = Vector3.zero;
        sentenceContainer.localScale = Vector3.zero;
        micIcon.localScale = Vector3.zero;
    }

    void OnEnable()
    {
        ResetGameplay();

        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;

        StartCoroutine(IntroFlow());
    }

    void OnDisable()
    {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
    }

    // -----------------------------
    void ResetGameplay()
    {
        currentIndex = 0;
        waitingForNext = false;
        canInteract = false;

        nextButton.SetActive(false);

        ShowSentence();
    }

    // -----------------------------
    // INTRO FLOW
    // -----------------------------
    IEnumerator IntroFlow()
    {
        if (introClip && audioSource)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        // Title
        yield return StartCoroutine(TitleDrop());

        yield return new WaitForSeconds(0.2f);

        // Instruction
        yield return StartCoroutine(PopIn(instructionContainer));

        // Sentence board (IMPORTANT FIX)
        yield return StartCoroutine(PopIn(sentenceContainer));

        // Mic bounce
        yield return StartCoroutine(BounceIn(micIcon));

        // Wait for audio
        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        canInteract = true;
    }

    // -----------------------------
    void ShowSentence()
    {
        if (currentIndex < sentences.Length)
        {
            sentenceText.text = sentences[currentIndex];
        }
    }

    // -----------------------------
    void OnSpeechResult(string spokenText)
    {
        if (!canInteract || waitingForNext) return;

        if (string.IsNullOrEmpty(spokenText) || spokenText.Length < 2)
        {
            PlaySFX(noInputSFX);
            return;
        }

        string expected = CleanText(sentences[currentIndex]);
        string spoken = CleanText(spokenText);

        if (IsMatch(spoken, expected))
        {
            StartCoroutine(HandleCorrect());
        }
        else
        {
            PlaySFX(wrongSFX);
            StartCoroutine(Shake(sentenceContainer));
        }
    }

    // -----------------------------
    IEnumerator HandleCorrect()
    {
        waitingForNext = true;

        PlaySFX(correctSFX);

        yield return StartCoroutine(Pulse(sentenceContainer));

        yield return new WaitForSeconds(0.5f);

        currentIndex++;

        if (currentIndex >= sentences.Length)
        {
            nextButton.SetActive(true);
        }
        else
        {
            ShowSentence();
            waitingForNext = false;
        }
    }

    // -----------------------------
    void PlaySFX(AudioClip clip)
    {
        if (clip && audioSource)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // -----------------------------
    string CleanText(string text)
    {
        return text.ToLower().Trim();
    }

    bool IsMatch(string spoken, string expected)
    {
        return spoken.Contains(expected) || expected.Contains(spoken);
    }

    // -----------------------------
    // ANIMATIONS
    // -----------------------------
    IEnumerator TitleDrop()
    {
        Vector3 start = titleContainer.localPosition + Vector3.up * titleDropHeight;
        Vector3 end = titleContainer.localPosition;

        titleContainer.localPosition = start;
        titleContainer.localScale = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * titleSpeed;
            titleContainer.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }

        yield return StartCoroutine(Pulse(titleContainer));
    }

    IEnumerator PopIn(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
    }

    IEnumerator BounceIn(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * bounceScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.one * bounceScale, Vector3.one, t);
            yield return null;
        }
    }

    IEnumerator Pulse(Transform target)
    {
        Vector3 original = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(original, Vector3.one * bounceScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.one * bounceScale, original, t);
            yield return null;
        }
    }

    IEnumerator Shake(Transform target)
    {
        Vector3 original = target.localPosition;

        for (int i = 0; i < 10; i++)
        {
            target.localPosition = original + new Vector3(Random.Range(-shakeAmount, shakeAmount), 0, 0);
            yield return new WaitForSeconds(0.02f);
        }

        target.localPosition = original;
    }
}