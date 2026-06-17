using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ListeningUnit12_S1A : MonoBehaviour
{
    [System.Serializable]
    public class PhraseCard
    {
        public Button button;

        public TMP_Text text;

        public AudioClip normalAudio;

        public AudioClip slowAudio;

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

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip introAudio;

    [Header("BG Animation")]
    public Vector2 bgStartPos;
    public Vector2 bgTargetPos;

    [Header("Auto Scroll")]
    public float autoScrollAmount = 0.08f;


    private bool slowMode = false;

    private int completedCards = 0;

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
        // SCALE TEXT ONLY
        GameObject textObj =
            card.button.transform.GetChild(0).gameObject;

        LeanTween.scale(textObj, Vector3.one * 1.08f, 0.15f)
            .setEaseOutBack();

        AudioClip clip =
            slowMode ? card.slowAudio : card.normalAudio;

        if (clip != null)
        {
            audioSource.clip = clip;

            audioSource.Play();

            yield return new WaitForSeconds(clip.length);

            // AUTO SCROLL
            if (scroll != null)
            {
                float current =
                    scroll.verticalNormalizedPosition;

                float target =
                    current - autoScrollAmount;

                target = Mathf.Clamp01(target);

                LeanTween.value(current, target, 0.25f)
                    .setOnUpdate((float val) =>
                    {
                        scroll.verticalNormalizedPosition = val;
                    });
            }
        }

        // SCALE BACK
        LeanTween.scale(textObj, Vector3.one, 0.15f)
            .setEaseOutBack();

        yield return new WaitForSeconds(0.1f);
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

            audioSource.clip = clip;

            audioSource.Play();
        }

        // CLICK SCALE
        GameObject textObj =
            card.button.transform.GetChild(0).gameObject;

        LeanTween.scale(textObj, Vector3.one * 1.08f, 0.12f)
            .setEaseOutBack()
            .setOnComplete(() =>
            {
                LeanTween.scale(textObj, Vector3.one, 0.12f);
            });

        if (!card.clicked)
        {
            card.clicked = true;

            completedCards++;

            if (completedCards >= cards.Length)
            {
                FinishSequence();
            }
        }
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
    }

    void ReplaySequence()
    {
        if (!sequenceCompleted)
            return;

        StopAllCoroutines();

        StartCoroutine(PlayCardsSequence());
    }
}
