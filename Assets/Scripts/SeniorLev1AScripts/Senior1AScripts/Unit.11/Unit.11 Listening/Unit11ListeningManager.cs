using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Unit11ListeningManager : MonoBehaviour
{
    [Header("TOP UI")]
    public TMP_Text titleText;

    [Header("CARD BG")]
    public RectTransform cardsBG;

    [Header("MAIN BUTTONS")]
    public Button whoButton;
    public Button whatButton;

    public Button slowButton;
    public Button repeatButton;
    public Button nextButton;

    [Header("CARD PARENTS")]
    public GameObject whoCardsParent;
    public GameObject whatCardsParent;

    [Header("AUDIO")]
    public AudioSource bgmSource;

    public AudioSource voiceSource;

    [Range(0f, 1f)]
    public float bgmNormalVolume = 0.35f;

    [Range(0f, 1f)]
    public float bgmVoiceVolume = 0.12f;

    [Header("WHO CARDS")]
    public CardData[] whoCards;

    [Header("WHAT CARDS")]
    public CardData[] whatCards;

    [Header("ANIMATION")]
    public float playScale = 1.08f;

    public float cardAnimTime = 0.2f;

    [Header("TIMINGS")]
    public float delayBetweenCards = 0.15f;

    private bool isPlaying;

    private bool whoCompleted;
    private bool whatCompleted;

    private CardData[] currentSequence;

    private bool currentIsWho;

    void Start()
    {
        StartCoroutine(IntroFlow());
    }

    IEnumerator IntroFlow()
    {
        nextButton.gameObject.SetActive(false);

        // INITIAL STATES
        whoCardsParent.SetActive(true);
        whatCardsParent.SetActive(false);

        whoButton.interactable = false;
        whatButton.interactable = false;

        slowButton.interactable = false;
        repeatButton.interactable = false;

        // START SCALE
        titleText.transform.localScale = Vector3.zero;

        whoButton.transform.localScale = Vector3.zero;
        whatButton.transform.localScale = Vector3.zero;

        nextButton.transform.localScale = Vector3.zero;

        // PANEL START POSITION
        Vector2 originalPos = cardsBG.anchoredPosition;

        cardsBG.anchoredPosition =
            new Vector2(0, -900);

        // TITLE POP
        LeanTween.scale(titleText.gameObject,
            Vector3.one,
            0.45f).setEaseOutBack();

        yield return new WaitForSeconds(0.5f);

        // PANEL SLIDE
        LeanTween.move(cardsBG,
            originalPos,
            0.5f).setEaseOutCubic();

        yield return new WaitForSeconds(0.55f);

        // WHO BUTTON POP
        LeanTween.scale(whoButton.gameObject,
            Vector3.one,
            0.3f).setEaseOutBack();

        yield return new WaitForSeconds(0.12f);

        // WHAT BUTTON POP
        LeanTween.scale(whatButton.gameObject,
            Vector3.one,
            0.3f).setEaseOutBack();

        yield return new WaitForSeconds(0.35f);

        // ONLY WHO ACTIVE
        whoButton.interactable = true;
        whatButton.interactable = false;

        // BUTTON LISTENERS
        whoButton.onClick.AddListener(() =>
        {
            if (!isPlaying)
            {
                StartCoroutine(
                    PlaySequence(whoCards, true));
            }
        });

        whatButton.onClick.AddListener(() =>
        {
            if (!isPlaying && whoCompleted)
            {
                StartCoroutine(
                    PlaySequence(whatCards, false));
            }
        });

        slowButton.onClick.AddListener(() =>
        {
            if (!isPlaying)
            {
                StartCoroutine(
                    ReplaySequence(true));
            }
        });

        repeatButton.onClick.AddListener(() =>
        {
            if (!isPlaying)
            {
                StartCoroutine(
                    ReplaySequence(false));
            }
        });

        // CARD CLICK SETUP
        SetupCardClicks(whoCards);
        SetupCardClicks(whatCards);

        // AUTO PLAY WHO
        yield return new WaitForSeconds(0.3f);

        StartCoroutine(
            PlaySequence(whoCards, true));
    }

    void SetupCardClicks(CardData[] cards)
    {
        foreach (CardData card in cards)
        {
            Button btn =
                card.cardObject.GetComponent<Button>();

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();

                btn.onClick.AddListener(() =>
                {
                    if (!isPlaying)
                    {
                        StartCoroutine(
                            PlaySingleCard(card, false));
                    }
                });
            }
        }
    }

    IEnumerator PlaySequence(CardData[] cards, bool isWho)
    {
        isPlaying = true;

        currentSequence = cards;

        currentIsWho = isWho;

        LockButtons();

        // ACTIVATE CORRECT CARDS
        if (isWho)
        {
            whoCardsParent.SetActive(true);
            whatCardsParent.SetActive(false);
        }
        else
        {
            whatCardsParent.SetActive(true);
            whoCardsParent.SetActive(false);
        }

        yield return new WaitForSeconds(0.2f);

        // PLAY IN SEQUENCE
        for (int i = 0; i < cards.Length; i++)
        {
            yield return StartCoroutine(
                PlaySingleCard(cards[i], false));

            yield return new WaitForSeconds(
                delayBetweenCards);
        }

        // WHO COMPLETE
        if (isWho)
        {
            whoCompleted = true;

            whatButton.interactable = true;
        }
        else
        {
            whatCompleted = true;
        }

        // ENABLE BUTTONS
        slowButton.interactable = true;
        repeatButton.interactable = true;

        whoButton.interactable = true;

        if (whoCompleted)
        {
            whatButton.interactable = true;
        }

        // NEXT BUTTON
        if (whoCompleted && whatCompleted)
        {
        nextButton.gameObject.SetActive(true);

        nextButton.transform.SetAsLastSibling();

    // FORCE REFRESH
        Canvas.ForceUpdateCanvases();

    // RESET SCALE
        nextButton.transform.localScale = Vector3.one;

    // SMALL START SCALE
        nextButton.transform.localScale = Vector3.one * 0.4f;

    // POP ANIMATION
        LeanTween.scale(nextButton.gameObject,
            Vector3.one,
            0.35f).setEaseOutBack();
        }
    }

    IEnumerator ReplaySequence(bool slowMode)
    {
        isPlaying = true;

        LockButtons();

        // KEEP CORRECT GROUP ACTIVE
        if (currentIsWho)
        {
            whoCardsParent.SetActive(true);
            whatCardsParent.SetActive(false);
        }
        else
        {
            whatCardsParent.SetActive(true);
            whoCardsParent.SetActive(false);
        }

        for (int i = 0; i < currentSequence.Length; i++)
        {
            yield return StartCoroutine(
                PlaySingleCard(
                    currentSequence[i],
                    slowMode));

            yield return new WaitForSeconds(
                delayBetweenCards);
        }

        UnlockButtons();

        isPlaying = false;
    }

    IEnumerator PlaySingleCard(
        CardData card,
        bool slowAudio)
    {
        Transform t =
            card.cardObject.transform;

        Image img =
            card.cardObject.GetComponent<Image>();

        Vector3 originalScale =
            Vector3.one;

        Color originalColor =
            Color.white;

        if (img != null)
        {
            originalColor = img.color;
        }

        // SCALE UP
        LeanTween.scale(card.cardObject,
            Vector3.one * playScale,
            cardAnimTime).setEaseOutBack();

        // DARKEN
        if (img != null)
        {
            img.color =
                new Color(0.72f, 0.72f, 0.72f, 1f);
        }

        // AUDIO
        AudioClip clip =
            slowAudio
            ? card.slowClip
            : card.normalClip;

        if (clip != null)
        {
            // LOWER BGM
            if (bgmSource != null)
            {
                bgmSource.volume =
                    bgmVoiceVolume;
            }

            // LOWER BGM
if (bgmSource != null)
{
    bgmSource.volume = bgmVoiceVolume;
}

// PLAY VOICE WITHOUT STOPPING BGM
voiceSource.volume = 1f;

voiceSource.PlayOneShot(clip);

// WAIT UNTIL AUDIO FINISHES
yield return new WaitForSeconds(clip.length);

// RESTORE BGM
if (bgmSource != null)
{
    bgmSource.volume = bgmNormalVolume;
}

            // RESTORE BGM
            if (bgmSource != null)
            {
                bgmSource.volume =
                    bgmNormalVolume;
            }
        }

        // RESET SCALE
        LeanTween.scale(card.cardObject,
            originalScale,
            cardAnimTime).setEaseOutBack();

        // RESET COLOR
        if (img != null)
        {
            img.color = originalColor;
        }
    }

    void LockButtons()
    {
        whoButton.interactable = false;
        whatButton.interactable = false;

        slowButton.interactable = false;
        repeatButton.interactable = false;
    }

    void UnlockButtons()
    {
        whoButton.interactable = true;

        if (whoCompleted)
        {
            whatButton.interactable = true;
        }

        slowButton.interactable = true;
        repeatButton.interactable = true;
    }
}

[System.Serializable]
public class CardData
{
    public GameObject cardObject;

    public AudioClip normalClip;

    public AudioClip slowClip;
}