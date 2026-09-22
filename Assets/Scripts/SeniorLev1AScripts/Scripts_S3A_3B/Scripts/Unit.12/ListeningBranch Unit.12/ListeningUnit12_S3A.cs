using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ListeningUnit12_S3A : MonoBehaviour
{
    [System.Serializable]
    public class PhraseCard
    {
        public Button button;

        public TMP_Text text;

        public AudioClip normalAudio;

        public AudioClip slowAudio;
        public GameObject speakerIcon;

        [HideInInspector]
        public bool clicked;
    }

    [Header("Title")]
    public TMP_Text titleText;

    [Header("Cards BG")]
    public RectTransform cardsBG;

    [Header("Cards")]
    public PhraseCard[] cards;

    [Header("Buttons")]
    public Button slowButton;
    public Button repeatButton;
    public Button nextButton;

    [Header("Button BGs")]
    public Image slowButtonBG;
    public Image repeatButtonBG;

    public Color normalButtonColor = Color.white;
    public Color activeButtonColor = Color.green;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip introAudio;

    [Header("Text Colors")]
    public Color normalTextColor = Color.black;
    public Color playingTextColor = new Color(0.35f, 0.18f, 0.05f); // Dark Brown

    [Header("BG Animation")]
    public Vector2 bgStartPos;
    public Vector2 bgTargetPos;

    [Header("Auto Scroll")]
    public float autoScrollAmount = 0.08f;


    private bool slowMode = false;

    private bool sequenceCompleted = false;

    private void Start()
    {
        nextButton.gameObject.SetActive(false);

        cardsBG.anchoredPosition = bgStartPos;

        slowButton.onClick.AddListener(ToggleSlowMode);

        repeatButton.onClick.AddListener(ReplaySequence);

        foreach (PhraseCard card in cards)
        {
            card.button.interactable = false;

            card.clicked = false;

            if (card.speakerIcon != null)
            {
                card.speakerIcon.SetActive(false);
            }

            card.button.onClick.RemoveAllListeners();

            PhraseCard currentCard = card;

            card.button.onClick.AddListener(() =>
            {
                OnCardClicked(currentCard);
            });
        }

        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence()
    {
        // TITLE POP
        titleText.transform.localScale = Vector3.zero;

        LeanTween.scale(titleText.gameObject, Vector3.one, 0.4f)
            .setEaseOutBack();

        // INTRO AUDIO
        if (introAudio != null)
        {
            audioSource.clip = introAudio;

            audioSource.Play();

            yield return new WaitForSeconds(introAudio.length);
        }

        // BG SLIDE
        LeanTween.move(cardsBG, bgTargetPos, 0.5f)
            .setEaseOutExpo();

        yield return new WaitForSeconds(0.6f);

        // PLAY CARDS
        yield return StartCoroutine(PlayCardsSequence());

        sequenceCompleted = true;

        foreach (PhraseCard card in cards)
        {
            card.button.interactable = true;
        }
    }

    IEnumerator PlayCardsSequence()
{
    ScrollRect scroll =
        cardsBG.GetComponentInChildren<ScrollRect>();

    foreach (PhraseCard card in cards)
    {
        Transform textTransform = card.text.transform;

        LeanTween.scale(textTransform.gameObject, Vector3.one * 1.08f, 0.15f);

        // Playing color
        card.text.color = playingTextColor;

        // Speaker ON
        if (card.speakerIcon != null)
        {
            card.speakerIcon.SetActive(true);

            Image img = card.speakerIcon.GetComponent<Image>();
            if (img != null)
                img.color = playingTextColor;
        }

        AudioClip clip =
            slowMode ? card.slowAudio : card.normalAudio;

        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();

            yield return new WaitForSeconds(clip.length);

            if (scroll != null)
            {
                float current = scroll.verticalNormalizedPosition;

                float target =
                    Mathf.Clamp01(current - autoScrollAmount);

                LeanTween.value(current, target, 0.25f)
                    .setOnUpdate((float val) =>
                    {
                        scroll.verticalNormalizedPosition = val;
                    });
            }
        }

        // Speaker OFF
        if (card.speakerIcon != null)
            card.speakerIcon.SetActive(false);

        // Back to normal color
        card.text.color = normalTextColor;

        LeanTween.scale(textTransform.gameObject, Vector3.one, 0.15f);

        yield return new WaitForSeconds(0.1f);
    }

    // Sequence finished
    if (!nextButton.gameObject.activeSelf)
    {
        FinishSequence();
    }
}

    void OnCardClicked(PhraseCard card)
    {
        if (!sequenceCompleted)
            return;

        AudioClip clip =
    slowMode ? card.slowAudio : card.normalAudio;

if (clip != null)
{
    audioSource.Stop();

    card.text.color = playingTextColor;

    if (card.speakerIcon != null)
    {
        card.speakerIcon.SetActive(true);

        Image img = card.speakerIcon.GetComponent<Image>();
        if (img != null)
            img.color = playingTextColor;
    }

    audioSource.clip = clip;
    audioSource.Play();

    StartCoroutine(ResetCardAfterAudio(card, clip.length));
}

        // CLICK SCALE
        Transform textTransform = card.text.transform;

        LeanTween.scale(textTransform.gameObject, Vector3.one * 1.08f, 0.12f)
            .setEaseOutBack();
    }

    IEnumerator ResetCardAfterAudio(PhraseCard card, float delay)
{
    yield return new WaitForSeconds(delay);

    Transform textTransform = card.text.transform;

    LeanTween.scale(textTransform.gameObject, Vector3.one, 0.12f);

    card.text.color = normalTextColor;

    if (card.speakerIcon != null)
        card.speakerIcon.SetActive(false);
}

    void FinishSequence()
    {
        nextButton.gameObject.SetActive(true);

        nextButton.transform.localScale = Vector3.zero;

        LeanTween.scale(nextButton.gameObject, Vector3.one, 0.3f)
            .setEaseOutBack();
    }

    void ToggleSlowMode()
{
    slowMode = !slowMode;

    if (slowButtonBG != null)
    {
        slowButtonBG.color =
            slowMode
            ? activeButtonColor
            : normalButtonColor;
    }
}

    void ReplaySequence()
{
    if (!sequenceCompleted)
        return;

    StopAllCoroutines();

    StartCoroutine(ReplayRoutine());
}

IEnumerator ReplayRoutine()
{
    ScrollRect scroll =
        cardsBG.GetComponentInChildren<ScrollRect>();

    if (scroll != null)
    {
        float start = scroll.verticalNormalizedPosition;
        float end = 1f;

        bool finished = false;

        LeanTween.value(start, end, 0.35f)
            .setEaseOutCubic()
            .setOnUpdate((float value) =>
            {
                scroll.verticalNormalizedPosition = value;
            })
            .setOnComplete(() =>
            {
                finished = true;
            });

        yield return new WaitUntil(() => finished);

        yield return new WaitForSeconds(0.15f);
    }

    yield return StartCoroutine(PlayCardsSequence());
}
}
