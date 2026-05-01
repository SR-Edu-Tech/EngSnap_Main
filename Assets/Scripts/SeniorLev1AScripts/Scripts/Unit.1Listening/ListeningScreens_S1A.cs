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
        public TextMeshProUGUI extraText; // NEW (second text to highlight)
        public Image bg;
        public GameObject speakerIcon;
        public AudioClip normalAudio;
        public AudioClip slowAudio;

        [HideInInspector] public bool played;
        [HideInInspector] public float originalSize;
        [HideInInspector] public float extraOriginalSize;
    }

    public Card[] cards;

    [Header("UI")]
    public TMP_Text titleText;
    public Transform cardsParent;
    public Transform controlsParent;

    public Button slowButton;
    public Button repeatButton;
    public GameObject nextButton;

    public Image slowBG;
    public Image repeatBG;

    [Header("Colors")]
    public Color normalText = Color.black;
    public Color autoPlayText = Color.yellow;
    public Color manualPlayText = Color.cyan;
    public Color normalBG = Color.white;
    public Color visitedBG = Color.gray;
    public Color activeToggle = Color.green;

    [Header("Text Size")]
    public float highlightSizeIncrease = 5f;

    [Header("Smoothness")]
    public float colorLerpSpeed = 10f;

    [Header("Animation")]
    public float animSpeed = 5f;
    public float stagger = 0.1f;

    [Header("Title Pop")]
    public float popDuration = 1.75f;
    public float popAmplitude = 0.75f;
    public float popFrequency = 4f;
    public float popStagger = 0.05f;

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

            if (card.extraText != null)
                card.extraOriginalSize = card.extraText.fontSize;
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
        // Hide title via CanvasGroup (typewriter pop will reveal it)
        CanvasGroup titleCG = titleText.GetComponent<CanvasGroup>();
        if (titleCG == null)
            titleCG = titleText.gameObject.AddComponent<CanvasGroup>();
        titleCG.alpha = 0f;
        titleText.ForceMeshUpdate();

        // Cards start hidden
        foreach (Transform c in cardsParent)
            c.localScale = Vector3.zero;

        // Buttons start hidden individually (parent stays visible)
        controlsParent.localScale = Vector3.one;
        slowButton.transform.localScale = Vector3.zero;
        repeatButton.transform.localScale = Vector3.zero;

        foreach (var card in cards)
        {
            card.played = false;

            card.text.color = normalText;
            card.text.fontSize = card.originalSize;

            if (card.extraText != null)
            {
                card.extraText.color = normalText;
                card.extraText.fontSize = card.extraOriginalSize;
            }

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

        // Title typewriter pop (fire and forget)
        StartCoroutine(TitleAnim());
        yield return new WaitForSeconds(0.8f);

        // Cards scale in (wait for completion)
        yield return StartCoroutine(CardsAnim());
        yield return new WaitForSeconds(0.1f);
        // Buttons scale in individually after cards
        yield return StartCoroutine(ButtonsAnim());

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        for (int i = 0; i < cards.Length; i++)
        {
            PlayCard(cards[i], false);
            yield return new WaitForSeconds(cards[i].normalAudio.length);
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

        if (!card.played)
        {
            card.played = true;
            card.bg.color = visitedBG;
            CheckCompletion();
        }

        PlayCard(card, true);
    }

    void PlayCard(Card card, bool isManual)
    {
        if (!isAutoPlaying)
            audioSource.Stop();

        if (currentPlayingCard != null)
            ResetVisual(currentPlayingCard);

        currentPlayingCard = card;

        SetPlaying(card, isManual);

        AudioClip clip = isSlowOn ? card.slowAudio : card.normalAudio;

        audioSource.clip = clip;
        audioSource.Play();

        StartCoroutine(ResetAfterAudio(card, clip.length));
    }

    IEnumerator ResetAfterAudio(Card card, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (currentPlayingCard == card)
        {
            ResetVisual(card);
            currentPlayingCard = null;
        }
    }

    void SetPlaying(Card card, bool isManual)
    {
        Color targetColor = isManual ? manualPlayText : autoPlayText;

        Image icon = null;

        if (card.speakerIcon != null)
        {
            card.speakerIcon.SetActive(true);
            icon = card.speakerIcon.GetComponent<Image>();
        }

        StartCoroutine(LerpColor(card, targetColor, icon));
    }

    IEnumerator LerpColor(Card card, Color target, Image icon)
    {
        float t = 0;

        Color start = card.text.color;

        while (t < 1)
        {
            t += Time.deltaTime * colorLerpSpeed;

            Color current = Color.Lerp(start, target, t);

            card.text.color = current;

            if (card.extraText != null)
                card.extraText.color = current;

            if (icon != null)
                icon.color = current;

            yield return null;
        }

        card.text.color = target;

        if (card.extraText != null)
            card.extraText.color = target;

        if (icon != null)
            icon.color = target;

        card.text.fontSize = card.originalSize + highlightSizeIncrease;

        if (card.extraText != null)
            card.extraText.fontSize = card.extraOriginalSize + highlightSizeIncrease;
    }

    void ResetVisual(Card card)
    {
        card.text.color = normalText;
        card.text.fontSize = card.originalSize;

        if (card.extraText != null)
        {
            card.extraText.color = normalText;
            card.extraText.fontSize = card.extraOriginalSize;
        }

        if (card.speakerIcon != null)
            card.speakerIcon.SetActive(false);

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

        float expectedTime = (charCount * popStagger) + Mathf.Max(0.5f, 1f / popFrequency);
        float totalDuration = Mathf.Max(popDuration, expectedTime);

        while (elapsed < totalDuration)
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
                Vector3 charMid = (vertices[vertIndex] + vertices[vertIndex + 2]) / 2f;

                float letterDelay = i * popStagger;
                float localTime = elapsed - letterDelay;

                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterDur = Mathf.Max(0.1f, 1f / popFrequency);
                    float t = Mathf.Clamp01(localTime / letterDur);

                    float overshoot = 1.70158f * (1f + popAmplitude);
                    float c3 = overshoot + 1f;

                    scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIndex].vertices[vertIndex + v];
                    Vector3 offset = orig - charMid;
                    vertices[vertIndex + v] = charMid + offset * scale;
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                titleText.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }

            yield return null;

            // Deferred reveal to prevent TMP first-frame flash
            if (!revealed && titleCG != null)
            {
                titleCG.alpha = 1f;
                revealed = true;
            }
        }

        // Restore original vertices
        textInfo = titleText.textInfo;
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
            titleText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    IEnumerator CardsAnim()
    {
        int cardIndex = 0;
        foreach (Transform c in cardsParent)
        {
            Card card = cardIndex < cards.Length ? cards[cardIndex] : null;

            // Start per-character text pop in parallel
            if (card != null)
            {
                StartCoroutine(PopTextPerChar(card.text));
                if (card.extraText != null)
                    StartCoroutine(PopTextPerChar(card.extraText));
            }

            // Scale card in with bounce
            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * animSpeed;
                float clamped = Mathf.Clamp01(t);

                float overshoot = 1.70158f;
                float c1 = overshoot + 1f;
                float ease = 1f + c1 * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

                c.localScale = Vector3.one * ease;
                yield return null;
            }
            c.localScale = Vector3.one;

            cardIndex++;
            yield return new WaitForSeconds(stagger);
        }
    }

    IEnumerator PopTextPerChar(TMP_Text tmp, float popDur = 1.2f, float charStagger = 0.04f, float popAmp = 0.6f, float popFreq = 4f)
    {
        tmp.ForceMeshUpdate();
        yield return null;
        tmp.ForceMeshUpdate();

        TMP_TextInfo textInfo = tmp.textInfo;
        int charCount = textInfo.characterCount;

        if (charCount == 0) yield break;

        tmp.maxVisibleCharacters = charCount;
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();

        float expectedTime = (charCount * charStagger) + Mathf.Max(0.5f, 1f / popFreq);
        float totalDuration = Mathf.Max(popDur, expectedTime);
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            textInfo = tmp.textInfo;

            for (int i = 0; i < charCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int matIdx = charInfo.materialReferenceIndex;
                int vertIdx = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[matIdx].vertices;
                Vector3 charMid = (vertices[vertIdx] + vertices[vertIdx + 2]) / 2f;

                float delay = i * charStagger;
                float localTime = elapsed - delay;

                float scale = 0f;
                if (localTime > 0f)
                {
                    float letterDur = Mathf.Max(0.1f, 1f / popFreq);
                    float t = Mathf.Clamp01(localTime / letterDur);

                    float overshoot = 1.70158f * (1f + popAmp);
                    float c3 = overshoot + 1f;
                    scale = 1f + c3 * Mathf.Pow(t - 1f, 3f) + overshoot * Mathf.Pow(t - 1f, 2f);
                }

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = cachedMeshInfo[matIdx].vertices[vertIdx + v];
                    Vector3 offset = orig - charMid;
                    vertices[vertIdx + v] = charMid + offset * scale;
                }
            }

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                textInfo.meshInfo[m].mesh.vertices = textInfo.meshInfo[m].vertices;
                tmp.UpdateGeometry(textInfo.meshInfo[m].mesh, m);
            }

            yield return null;
        }

        // Restore original vertices
        textInfo = tmp.textInfo;
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = cachedMeshInfo[i].vertices;
            tmp.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    IEnumerator ButtonsAnim()
    {
        yield return StartCoroutine(PopButton(slowButton.transform));
        yield return new WaitForSeconds(stagger);
        yield return StartCoroutine(PopButton(repeatButton.transform));
    }

    IEnumerator PopButton(Transform btn)
    {
        // Phase 1: Scale from 0 to 1.15 (overshoot)
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * animSpeed;
            float clamped = Mathf.Clamp01(t);
            btn.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.15f, clamped);
            yield return null;
        }

        // Phase 2: Settle from 1.15 back to 1.0
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * (animSpeed * 2f);
            float clamped = Mathf.Clamp01(t);
            // Smooth ease-out
            float smooth = 1f - Mathf.Pow(1f - clamped, 2f);
            btn.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, smooth);
            yield return null;
        }
        btn.localScale = Vector3.one;
    }
}