using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MyDetailsTableColor_S3A : MonoBehaviour
{
    [System.Serializable]
    public class DialoguePair_NoColor
    {
        public Button greetingButton;
        public Button responseButton;

        public Image greetingBg;
        public Image responseBg;

        public RectTransform container;

        public TMP_Text greetingText;
        public TMP_Text responseText;

        public AudioClip greetingAudio;
        public AudioClip responseAudio;

        public GameObject greetingSpeakerIcon;
        public GameObject responseSpeakerIcon;

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
    public DialoguePair_NoColor[] pairs;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    public AudioClip promptGreeting;
    public AudioClip promptResponse;

    public AudioClip popSfx;

    [Header("Button Colors")]
    public Color normalButtonColor = Color.white;
    public Color visitedButtonColor = Color.gray;

    [Header("Animation")]
    public float popSpeed = 5f;
    public float delayBetweenPairs = 0.4f;

    [Header("Title Pop")]
    public float titlePopDuration = 1.75f;
    public float titlePopAmplitude = 0.75f;
    public float titlePopFrequency = 4f;
    public float titlePopStagger = 0.05f;

    private Coroutine currentRoutine;
    private bool isAutoPlaying = true;
    private Vector2 boardOriginalPos;
    private bool initialized;

    void OnEnable()
    {
        ResetUI();
        SetupButtons();
        StartCoroutine(MainFlow());
    }

    void OnDisable()
    {
        StopAllCoroutines();

        if (audioSource)
            audioSource.Stop();
    }

    void ResetUI()
    {
        if (!initialized)
        {
            boardOriginalPos = board.anchoredPosition;
            initialized = true;
        }

        board.anchoredPosition = new Vector2(0, -1200);

        HideText(title.GetComponent<TMP_Text>());
        HideText(greetingsHeader.GetComponent<TMP_Text>());
        HideText(responsesHeader.GetComponent<TMP_Text>());

        nextButton.SetActive(false);

        foreach (var p in pairs)
        {
            p.visited = false;
            p.hasBouncedIn = false;

            p.greetingButton.transform.localScale = Vector3.zero;
            p.responseButton.transform.localScale = Vector3.zero;

            SetBG(p.greetingBg, normalButtonColor);
            SetBG(p.responseBg, normalButtonColor);

            HideText(p.greetingText);
            HideText(p.responseText);

            if (p.greetingSpeakerIcon)
                p.greetingSpeakerIcon.SetActive(false);

            if (p.responseSpeakerIcon)
                p.responseSpeakerIcon.SetActive(false);
        }

        scrollRect.verticalNormalizedPosition = 1f;
    }

    void HideText(TMP_Text txt)
    {
        if (txt == null)
            return;

        CanvasGroup cg = txt.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = txt.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
    }

    void SetupButtons()
    {
        foreach (var pair in pairs)
        {
            DialoguePair_NoColor captured = pair;

            pair.greetingButton.onClick.RemoveAllListeners();
            pair.responseButton.onClick.RemoveAllListeners();

            pair.greetingButton.onClick.AddListener(() =>
            {
                OnPairClicked(captured);
            });

            pair.responseButton.onClick.AddListener(() =>
            {
                OnPairClicked(captured);
            });
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

        StartCoroutine(
            TitleAnim(title.GetComponent<TMP_Text>())
        );

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
            yield return new WaitWhile(() => audioSource.isPlaying);

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

            yield return new WaitForSeconds(delayBetweenPairs);
        }

        isAutoPlaying = false;
    }

    void OnPairClicked(DialoguePair_NoColor pair)
    {
        if (isAutoPlaying)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        if (audioSource.isPlaying)
            audioSource.Stop();

        if (!pair.visited)
        {
            pair.visited = true;

            SetBG(pair.greetingBg, visitedButtonColor);
            SetBG(pair.responseBg, visitedButtonColor);

            CheckCompletion();
        }

        ResetIcons();

        currentRoutine =
            StartCoroutine(
                PlayPairSequence(pair, false)
            );
    }

    IEnumerator PlayPairSequence(
        DialoguePair_NoColor pair,
        bool autoPlay)
    {
        yield return ScrollTo(pair.container);

        // GREETING
        if (pair.greetingSpeakerIcon)
            pair.greetingSpeakerIcon.SetActive(true);

        StartCoroutine(
            ScaleCard(
                pair.greetingButton.transform,
                1.05f
            )
        );

        if (!pair.visited || autoPlay)
            StartCoroutine(
                PopTextPerChar(pair.greetingText)
            );

        if (promptGreeting)
        {
            audioSource.clip = promptGreeting;
            audioSource.Play();

            yield return new WaitWhile(() =>
                audioSource.isPlaying);
        }

        if (pair.greetingAudio)
        {
            audioSource.clip = pair.greetingAudio;
            audioSource.Play();

            yield return new WaitWhile(() =>
                audioSource.isPlaying);
        }

        if (pair.greetingSpeakerIcon)
            pair.greetingSpeakerIcon.SetActive(false);

        // RESPONSE
        if (pair.responseSpeakerIcon)
            pair.responseSpeakerIcon.SetActive(true);

        StartCoroutine(
            ScaleCard(
                pair.responseButton.transform,
                1.05f
            )
        );

        if (!pair.visited || autoPlay)
            StartCoroutine(
                PopTextPerChar(pair.responseText)
            );

        if (promptResponse)
        {
            audioSource.clip = promptResponse;
            audioSource.Play();

            yield return new WaitWhile(() =>
                audioSource.isPlaying);
        }

        if (pair.responseAudio)
        {
            audioSource.clip = pair.responseAudio;
            audioSource.Play();

            yield return new WaitWhile(() =>
                audioSource.isPlaying);
        }

        if (pair.responseSpeakerIcon)
            pair.responseSpeakerIcon.SetActive(false);

        pair.greetingButton.transform.localScale =
            Vector3.one;

        pair.responseButton.transform.localScale =
            Vector3.one;
    }

    void ResetIcons()
    {
        foreach (var p in pairs)
        {
            if (p.greetingSpeakerIcon)
                p.greetingSpeakerIcon.SetActive(false);

            if (p.responseSpeakerIcon)
                p.responseSpeakerIcon.SetActive(false);

            p.greetingButton.transform.localScale =
                Vector3.one;

            p.responseButton.transform.localScale =
                Vector3.one;
        }
    }

    void CheckCompletion()
    {
        foreach (var p in pairs)
        {
            if (!p.visited)
                return;
        }

        nextButton.SetActive(true);

        nextButton.transform.localScale =
            Vector3.zero;

        LeanTween.scale(
            nextButton,
            Vector3.one,
            0.35f
        ).setEaseOutBack();
    }

    void SetBG(Image bg, Color color)
    {
        if (bg != null)
            bg.color = color;
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

            float ease =
                1f +
                c1 * Mathf.Pow(clamped - 1f, 3f) +
                overshoot * Mathf.Pow(clamped - 1f, 2f);

            target.localScale =
                Vector3.one * ease;

            yield return null;
        }

        target.localScale = Vector3.one;
    }

    IEnumerator ScaleCard(
        Transform target,
        float scale)
    {
        Vector3 start = target.localScale;
        Vector3 end = Vector3.one * scale;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / 0.15f;

            target.localScale =
                Vector3.Lerp(start, end, t);

            yield return null;
        }

        target.localScale = end;
    }

    IEnumerator SlideUp(
        RectTransform t,
        float speed)
    {
        Vector2 start = new Vector2(0, -1200);
        Vector2 end = boardOriginalPos;

        float time = 0;

        while (time < 1)
        {
            time +=
                Time.deltaTime * (speed / 1000f);

            float clamped =
                Mathf.Clamp01(time);

            float overshoot = 1.2f;
            float c1 = overshoot + 1f;

            float ease =
                1f +
                c1 * Mathf.Pow(clamped - 1f, 3f) +
                overshoot * Mathf.Pow(clamped - 1f, 2f);

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

    IEnumerator ScrollTo(RectTransform target)
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        float contentHeight = content.rect.height;
        float viewportHeight =
            scrollRect.viewport.rect.height;

        if (contentHeight <= viewportHeight)
            yield break;

        float targetY =
            Mathf.Abs(target.anchoredPosition.y) - 50f;

        targetY = Mathf.Max(0f, targetY);

        float normalized =
            1 -
            Mathf.Clamp01(
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
                Mathf.Lerp(start, normalized, time);

            yield return null;
        }

        scrollRect.verticalNormalizedPosition =
            normalized;
    }

    IEnumerator TitleAnim(TMP_Text txt)
    {
        txt.transform.localScale = Vector3.zero;

        LeanTween.scale(
            txt.gameObject,
            Vector3.one,
            0.4f
        ).setEaseOutBack();

        CanvasGroup cg =
            txt.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = txt.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 1f;

        yield return null;
    }

    IEnumerator PopTextPerChar(TMP_Text txt)
    {
        CanvasGroup cg =
            txt.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = txt.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 1f;

        txt.transform.localScale = Vector3.zero;

        LeanTween.scale(
            txt.gameObject,
            Vector3.one,
            0.3f
        ).setEaseOutBack();

        yield return null;
    }
}
