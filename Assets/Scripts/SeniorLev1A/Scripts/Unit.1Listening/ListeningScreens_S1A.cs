using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ListeningScreens_S1A : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    [System.Serializable]
    public class Card
    {
        public Button button;
        public TextMeshProUGUI text;
        public Image bg;
        public GameObject speakerIcon;
        public AudioClip normalAudio;
        public AudioClip slowAudio;

        [HideInInspector] public bool played;
        [HideInInspector] public float originalSize;
    }

    public Card[] cards;

    [Header("UI")]
    public Transform title;
    public Transform cardsParent;
    public Transform controlsParent;

    public Button slowButton;
    public Button repeatButton;
    public GameObject nextButton;

    public Image slowBG;
    public Image repeatBG;

    [Header("Colors")]
    public Color normalText = Color.black;
    public Color playingText = Color.yellow;
    public Color visitedPlayingText = Color.cyan;
    public Color normalBG = Color.white;
    public Color visitedBG = Color.gray;
    public Color activeToggle = Color.green;

    [Header("Text Size")]
    public float highlightSizeIncrease = 5f;

    [Header("Animation")]
    public float animSpeed = 5f;
    public float stagger = 0.1f;

    private bool isSlowOn = false;
    private bool isRepeatOn = false;

    private bool playerCanInteract = false;
    private bool isAutoPlaying = false;

    private Coroutine currentCoroutine;
    private Card currentPlayingCard = null;

    void Awake()
    {
        foreach (var card in cards)
        {
            card.originalSize = card.text.fontSize;
        }
    }

    void OnEnable()
    {
        StartGame();
    }

    void OnDisable()
    {
        if (audioSource != null)
            audioSource.Stop();

        StopAllCoroutines();
    }

    void StartGame()
    {
        ResetUIState();

        nextButton.SetActive(false);
        playerCanInteract = false;
        currentPlayingCard = null;

        isSlowOn = false;
        isRepeatOn = false;

        UpdateToggleVisuals();

        foreach (var card in cards)
            card.button.onClick.RemoveAllListeners();

        slowButton.onClick.RemoveAllListeners();
        slowButton.onClick.AddListener(ToggleSlow);

        repeatButton.onClick.RemoveAllListeners();
        repeatButton.onClick.AddListener(ToggleRepeat);

        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(FullFlow());
    }

    void ResetUIState()
    {
        title.localScale = Vector3.zero;
        controlsParent.localScale = Vector3.zero;

        foreach (Transform c in cardsParent)
            c.localScale = Vector3.zero;

        foreach (var card in cards)
        {
            card.played = false;

            card.text.color = normalText;
            card.text.fontSize = card.originalSize;

            if (card.bg != null)
                card.bg.color = normalBG;

            if (card.speakerIcon != null)
                card.speakerIcon.SetActive(false);
        }
    }

    IEnumerator FullFlow()
    {
        isAutoPlaying = true;

        if (introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        StartCoroutine(TitleAnim());
        yield return new WaitForSeconds(0.2f);

        StartCoroutine(CardsAnim());
        yield return new WaitForSeconds(0.2f);

        StartCoroutine(ControlsAnim());

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        for (int i = 0; i < cards.Length; i++)
        {
            PlayCard(cards[i], false);

            AudioClip clip = cards[i].normalAudio;
            yield return new WaitForSeconds(clip.length);
        }

        isAutoPlaying = false;
        EnableInteraction();
    }

    void EnableInteraction()
    {
        playerCanInteract = true;

        foreach (var card in cards)
        {
            card.button.onClick.RemoveAllListeners();
            card.button.onClick.AddListener(() => OnCardClicked(card));
        }
    }

    void OnCardClicked(Card card)
    {
        if (!playerCanInteract) return;
        if (isAutoPlaying) return;

        PlayCard(card, true);
    }

    void PlayCard(Card card, bool markVisited)
    {
        if (!isAutoPlaying)
            audioSource.Stop();

        if (currentPlayingCard != null)
            ResetVisual(currentPlayingCard);

        currentPlayingCard = card;

        SetPlaying(card);

        AudioClip clip = isSlowOn ? card.slowAudio : card.normalAudio;

        audioSource.clip = clip;
        audioSource.Play();

        StartCoroutine(ResetAfterAudio(card, clip.length, markVisited));
    }

    IEnumerator ResetAfterAudio(Card card, float duration, bool markVisited)
    {
        yield return new WaitForSeconds(duration);

        if (markVisited && !card.played)
        {
            card.played = true;
            card.bg.color = visitedBG;
            CheckCompletion();
        }

        if (currentPlayingCard == card)
        {
            ResetVisual(card);
            currentPlayingCard = null;
        }
    }

    void SetPlaying(Card card)
    {
        Color colorToUse = card.played ? visitedPlayingText : playingText;

        card.text.color = colorToUse;
        card.text.fontSize = card.originalSize + highlightSizeIncrease;

        if (card.speakerIcon != null)
        {
            card.speakerIcon.SetActive(true);

            Image iconImg = card.speakerIcon.GetComponent<Image>();
            if (iconImg != null)
                iconImg.color = colorToUse;
        }
    }

    void ResetVisual(Card card)
    {
        card.text.color = normalText;
        card.text.fontSize = card.originalSize;

        if (card.speakerIcon != null)
        {
            card.speakerIcon.SetActive(false);

            Image iconImg = card.speakerIcon.GetComponent<Image>();
            if (iconImg != null)
                iconImg.color = normalText;
        }

        card.bg.color = card.played ? visitedBG : normalBG;
    }

    void ToggleSlow()
    {
        if (!playerCanInteract) return;

        isSlowOn = !isSlowOn;
        UpdateToggleVisuals();
    }

    void ToggleRepeat()
    {
        if (!playerCanInteract) return;

        isRepeatOn = !isRepeatOn;
        UpdateToggleVisuals();
    }

    void UpdateToggleVisuals()
    {
        slowBG.color = isSlowOn ? activeToggle : normalBG;
        repeatBG.color = isRepeatOn ? activeToggle : normalBG;
    }

    void CheckCompletion()
    {
        foreach (var c in cards)
            if (!c.played) return;

        nextButton.SetActive(true);
    }

    IEnumerator TitleAnim()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * animSpeed;
            title.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.2f, t);
            yield return null;
        }
        title.localScale = Vector3.one;
    }

    IEnumerator CardsAnim()
    {
        foreach (Transform c in cardsParent)
        {
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * animSpeed;
                c.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
            yield return new WaitForSeconds(stagger);
        }
    }

    IEnumerator ControlsAnim()
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * animSpeed;
            controlsParent.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
    }
}