using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReadingListenAndTap_S1A : MonoBehaviour
{
    [System.Serializable]
    public class Option
    {
        public Button button;
        public TMP_Text label;
        public Image bg;              // background
        public AudioClip audioClip;

        [HideInInspector] public bool hasPlayed = false;
    }

    [Header("UI Containers")]
    public Transform titleContainer;
    public Transform characterContainer;
    public Transform bubbleContainer;
    public Transform optionsContainer;

    [Header("Options")]
    public Option[] options;

    [Header("Buttons")]
    public GameObject nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    [Header("Colors")]
    public Color normalText = Color.black;
    public Color playingText = Color.yellow;
    public Color normalBG = Color.white;
    public Color visitedBG = Color.gray;

    [Header("Animation Settings")]
    public float titleDropHeight = 300f;
    public float titleSpeed = 3f;
    public float popSpeed = 5f;
    public float bounceScale = 1.15f;
    public float staggerDelay = 0.1f;

    private bool canInteract = false;
    private bool isPlaying = false;

    // -----------------------------
    void Awake()
    {
        titleContainer.localScale = Vector3.zero;
        characterContainer.localScale = Vector3.zero;
        bubbleContainer.localScale = Vector3.zero;
        optionsContainer.localScale = Vector3.zero;
    }

    void OnEnable()
    {
        ResetGame();
        StartCoroutine(IntroFlow());
    }

    // -----------------------------
    void ResetGame()
    {
        nextButton.SetActive(false);
        canInteract = false;
        isPlaying = false;

        foreach (var opt in options)
        {
            opt.hasPlayed = false;

            // Reset visuals
            opt.bg.color = normalBG;
            opt.label.color = normalText;

            opt.button.onClick.RemoveAllListeners();

            Option captured = opt;
            opt.button.onClick.AddListener(() => OnOptionClicked(captured));
        }
    }

    // -----------------------------
    IEnumerator IntroFlow()
    {
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        yield return StartCoroutine(TitleDrop());
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(BounceIn(characterContainer));
        yield return StartCoroutine(PopIn(bubbleContainer));
        yield return StartCoroutine(AnimateOptions());

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        // Auto-play (NO VISIT MARK)
        yield return StartCoroutine(AutoPlayOptions());

        canInteract = true;
    }

    // -----------------------------
    IEnumerator AutoPlayOptions()
    {
        foreach (var opt in options)
        {
            yield return StartCoroutine(PlayAudio(opt, false));
            yield return new WaitForSeconds(0.2f);
        }
    }

    // -----------------------------
    void OnOptionClicked(Option opt)
    {
        if (!canInteract || isPlaying) return;

        StartCoroutine(PlayAudio(opt, true));
    }

    // -----------------------------
    IEnumerator PlayAudio(Option opt, bool markVisited)
    {
        isPlaying = true;

        // TEXT highlight only
        opt.label.color = playingText;

        // small pop
        opt.button.transform.localScale = Vector3.one * 1.1f;

        if (opt.audioClip && audioSource)
        {
            audioSource.PlayOneShot(opt.audioClip);
            yield return new WaitForSeconds(opt.audioClip.length);
        }

        opt.button.transform.localScale = Vector3.one;

        // AFTER PLAY
        if (markVisited)
        {
            opt.hasPlayed = true;

            opt.bg.color = visitedBG;      // grey background
            opt.label.color = normalText;  // text back to normal
        }
        else
        {
            // Auto play  no visit mark
            opt.label.color = normalText;
            opt.bg.color = normalBG;
        }

        isPlaying = false;

        CheckCompletion();
    }

    // -----------------------------
    void CheckCompletion()
    {
        foreach (var opt in options)
        {
            if (!opt.hasPlayed)
                return;
        }

        nextButton.SetActive(true);
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

    IEnumerator AnimateOptions()
    {
        optionsContainer.localScale = Vector3.one;

        foreach (Transform child in optionsContainer)
            child.localScale = Vector3.zero;

        foreach (Transform child in optionsContainer)
        {
            yield return StartCoroutine(BounceIn(child));
            yield return new WaitForSeconds(staggerDelay);
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
}