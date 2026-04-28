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
        public TMP_Text extraLabel; // NEW (optional)
        public Image bg;
        public Image highlightImage;
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
    public Color autoPlayText = Color.yellow;
    public Color manualPlayText = Color.cyan;

    public Color normalBG = Color.white;
    public Color visitedBG = Color.gray;

    [Header("Animation Settings")]
    public float titleDropHeight = 300f;
    public float titleSpeed = 3f;
    public float popSpeed = 5f;
    public float bounceScale = 1.15f;
    public float staggerDelay = 0.1f;

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
        titleContainer.localScale = Vector3.zero;
        characterContainer.localScale = Vector3.zero;
        bubbleContainer.localScale = Vector3.zero;
        optionsContainer.localScale = Vector3.zero;

        foreach (Transform child in optionsContainer)
            child.localScale = Vector3.zero;
    }

    void ResetGame()
    {
        nextButton.SetActive(false);
        canInteract = false;
        currentPlayingOption = null;

        foreach (var opt in options)
        {
            opt.hasPlayed = false;

            opt.bg.color = normalBG;

            if (opt.label != null)
                opt.label.color = normalText;

            if (opt.extraLabel != null)
                opt.extraLabel.color = normalText;

            if (opt.highlightImage != null)
                opt.highlightImage.gameObject.SetActive(false);

            opt.button.transform.localScale = Vector3.one;

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

        yield return StartCoroutine(TitleDrop());
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(BounceIn(characterContainer));
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

        foreach (var opt in options)
        {
            yield return PlayAudio(opt, false);
            yield return new WaitForSeconds(0.2f);
        }
    }

    void OnOptionClicked(Option opt)
    {
        if (!canInteract) return;

        if (!opt.hasPlayed)
        {
            opt.hasPlayed = true;
            opt.bg.color = visitedBG;
            CheckCompletion();
        }

        if (currentAudioRoutine != null)
            StopCoroutine(currentAudioRoutine);

        if (audioSource != null)
            audioSource.Stop();

        if (currentPlayingOption != null)
            ResetVisual(currentPlayingOption);

        currentPlayingOption = opt;
        currentAudioRoutine = StartCoroutine(PlayAudio(opt, true));
    }

    IEnumerator PlayAudio(Option opt, bool isManual)
    {
        Color targetColor = isManual ? manualPlayText : autoPlayText;

        // MAIN TEXT
        if (opt.label != null)
        {
            if (isManual)
                StartCoroutine(SmoothColorTransition(opt.label, targetColor));
            else
                opt.label.color = targetColor;
        }

        // EXTRA TEXT (SAFE)
        if (opt.extraLabel != null)
        {
            if (isManual)
                StartCoroutine(SmoothColorTransition(opt.extraLabel, targetColor));
            else
                opt.extraLabel.color = targetColor;
        }

        // IMAGE
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
        if (text == null) yield break;

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
        if (opt.label != null)
        {
            if (opt.hasPlayed)
                StartCoroutine(SmoothColorTransition(opt.label, normalText));
            else
                opt.label.color = normalText;
        }

        if (opt.extraLabel != null)
        {
            if (opt.hasPlayed)
                StartCoroutine(SmoothColorTransition(opt.extraLabel, normalText));
            else
                opt.extraLabel.color = normalText;
        }

        if (opt.highlightImage != null)
            opt.highlightImage.gameObject.SetActive(false);

        opt.button.transform.localScale = Vector3.one;

        opt.bg.color = opt.hasPlayed ? visitedBG : normalBG;
    }

    void CheckCompletion()
    {
        foreach (var opt in options)
        {
            if (!opt.hasPlayed)
                return;
        }

        nextButton.SetActive(true);
    }

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

        yield return Pulse(titleContainer);
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
            yield return BounceIn(child);
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