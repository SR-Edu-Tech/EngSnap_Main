using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class ShopOptionData_Shopping_BB2
{
    [Tooltip("Shop name shown on the button, e.g. 'Bakery'")]
    public string shopLabel;
    [Tooltip("True if this is the shop that sells the round's item")]
    public bool isCorrectShop;
}

[System.Serializable]
public class RoundData_Shopping_BB2
{
    [Tooltip("The item for this round, e.g. bread / flowers / a book")]
    public Sprite itemSprite;
    [Tooltip("Exactly 3 shop options for this round — one must have isCorrectShop = true")]
    public ShopOptionData_Shopping_BB2[] shopOptions = new ShopOptionData_Shopping_BB2[3];
    [Tooltip("Optional narrator VO for this round. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Which Shop? — Shopping_BB2.
/// An item appears each round with 3 FIXED shop-button slots that get
/// refilled with a new set of shop names each round (6 distinct shops
/// appear across 8 rounds, so the correct + 2 other shops are supplied
/// per round rather than hardcoded). Correct tap: the item flies into
/// that shop, the shop button glows, chime plays. Wrong tap: gentle
/// wobble + hint, no penalty, retry within the same round.
/// Fires OnFinished after 8 rounds.
/// </summary>
public class ShopMatch_BB2 : MonoBehaviour
{
    [Header("Rounds — 8, IN ORDER")]
    public RoundData_Shopping_BB2[] rounds = new RoundData_Shopping_BB2[8];

    [Header("UI — Item")]
    public Image itemImage;

    [Header("UI — Shop Button Slots (fixed, 3 — refilled with new shop names each round)")]
    public Button[]   shopButtons = new Button[3];
    public TMP_Text[] shopLabels  = new TMP_Text[3];

    [Header("Glow")]
    [SerializeField] private Color shopGlowColor = new Color(0.6f, 0.85f, 1f);

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'Where do you buy this? Try again!'")]
    public AudioClip   wrongTapHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Tap Feedback SFX")]
    [Tooltip("Played on a correct tap. Leave empty to fall back to AudioManager's default correct sound.")]
    public AudioClip correctTapSfx;
    [Tooltip("Played on a wrong tap. Leave empty to fall back to AudioManager's default wrong sound.")]
    public AudioClip wrongTapSfx;

    [Header("Pop FX")]
    public AudioClip itemPopSfx;
    public AudioClip buttonPopSfx;

    [Header("Timing")]
    [SerializeField] private float flyToShopDuration       = 0.4f;
    [SerializeField] private float glowDuration              = 0.4f;
    [SerializeField] private float delayAfterCorrect         = 0.7f;
    [SerializeField] private float delayBeforeNextButton     = 0.6f;
    [SerializeField] private float popInDuration              = 0.35f;
    [SerializeField] private float popOutDuration             = 0.2f;
    [SerializeField] private float beatWithoutNarration       = 0.25f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;
    private Vector2 _itemOriginalAnchoredPos;
    private Image[] _shopButtonGraphics;
    private Color[] _shopButtonOriginalColors;

    void Awake()
    {
        for (int i = 0; i < shopButtons.Length; i++)
        {
            int capturedIndex = i;
            if (shopButtons[i] != null)
            {
                shopButtons[i].onClick.RemoveAllListeners();
                shopButtons[i].onClick.AddListener(() => OnShopTapped(capturedIndex));
            }
        }

        if (itemImage != null)
            _itemOriginalAnchoredPos = itemImage.rectTransform.anchoredPosition;

        _shopButtonGraphics       = new Image[shopButtons.Length];
        _shopButtonOriginalColors = new Color[shopButtons.Length];
        for (int i = 0; i < shopButtons.Length; i++)
        {
            _shopButtonGraphics[i] = shopButtons[i] != null ? shopButtons[i].GetComponent<Image>() : null;
            _shopButtonOriginalColors[i] = _shopButtonGraphics[i] != null ? _shopButtonGraphics[i].color : Color.white;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[ShopMatch_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        SetScaleZero(itemImage != null ? itemImage.rectTransform : null);
        foreach (var btn in shopButtons)
            SetScaleZero(btn != null ? btn.GetComponent<RectTransform>() : null);
        SetButtonsInteractable(false);

        StartCoroutine(IntroThenLoadRound(0));

        Debug.Log("[ShopMatch_BB2] RestartGame — starting from round 0");
    }

    private IEnumerator IntroThenLoadRound(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadRoundSequence(index, isFirstLoad: true));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Round sequence: pop out old (if any) → refill data → pop in
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadRoundSequence(int index, bool isFirstLoad)
    {
        SetButtonsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCurrent());

        var data = rounds[index];
        if (itemImage != null)
        {
            itemImage.sprite = data.itemSprite;
            itemImage.rectTransform.anchoredPosition = _itemOriginalAnchoredPos;
        }

        for (int i = 0; i < shopButtons.Length && i < data.shopOptions.Length; i++)
        {
            if (shopLabels != null && i < shopLabels.Length && shopLabels[i] != null)
                shopLabels[i].text = data.shopOptions[i].shopLabel;
            if (_shopButtonGraphics[i] != null)
                _shopButtonGraphics[i].color = _shopButtonOriginalColors[i];
        }

        if (dialogueAudioSource != null && data.promptAudio != null)
        {
            dialogueAudioSource.clip = data.promptAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.promptAudio.length);
        }
        else
        {
            yield return new WaitForSeconds(beatWithoutNarration);
        }

        if (itemPopSfx != null) AudioManager.Instance?.PlaySFX(itemPopSfx);
        if (itemImage != null) yield return StartCoroutine(PopIn(itemImage.rectTransform));

        if (buttonPopSfx != null) AudioManager.Instance?.PlaySFX(buttonPopSfx);
        var routines = new List<Coroutine>();
        foreach (var btn in shopButtons)
            if (btn != null) routines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;

        SetButtonsInteractable(true);
    }

    private IEnumerator PopOutCurrent()
    {
        var routines = new List<Coroutine>();
        if (itemImage != null) routines.Add(StartCoroutine(PopOut(itemImage.rectTransform)));
        foreach (var btn in shopButtons)
            if (btn != null) routines.Add(StartCoroutine(PopOut(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnShopTapped(int index)
    {
        var data = rounds[_currentIndex];
        if (index >= data.shopOptions.Length) return;

        if (data.shopOptions[index].isCorrectShop)
            StartCoroutine(HandleCorrectTap(index));
        else
            StartCoroutine(HandleWrongTap(index));
    }

    private IEnumerator HandleCorrectTap(int index)
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(correctTapSfx != null ? correctTapSfx : AudioManager.Instance.sfxCorrect);

        var buttonRect = shopButtons[index] != null ? shopButtons[index].GetComponent<RectTransform>() : null;

        if (itemImage != null && buttonRect != null)
            yield return StartCoroutine(FlyToShop(itemImage.rectTransform, buttonRect));

        if (buttonRect != null) VFXManager.Instance?.SpawnCorrectBurst(buttonRect);

        if (_shopButtonGraphics[index] != null)
            yield return StartCoroutine(GlowShop(index));

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < rounds.Length)
            StartCoroutine(LoadRoundSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllRoundsComplete());
    }

    private IEnumerator HandleWrongTap(int index)
    {
        AudioManager.Instance?.PlaySFX(wrongTapSfx != null ? wrongTapSfx : AudioManager.Instance.sfxWrong);

        var rect = shopButtons[index] != null ? shopButtons[index].GetComponent<RectTransform>() : null;
        if (rect != null)
            yield return StartCoroutine(WobbleButton(rect));

        if (dialogueAudioSource != null && wrongTapHintClip != null)
        {
            dialogueAudioSource.clip = wrongTapHintClip;
            dialogueAudioSource.Play();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator FlyToShop(RectTransform itemRect, RectTransform shopRect)
    {
        Vector3 startPos = itemRect.position;
        Vector3 endPos   = shopRect.position;
        float e = 0f;
        while (e < flyToShopDuration)
        {
            e += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, e / flyToShopDuration);
            itemRect.position   = Vector3.Lerp(startPos, endPos, t);
            itemRect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }
        itemRect.localScale = Vector3.zero;
    }

    private IEnumerator GlowShop(int index)
    {
        Image img = _shopButtonGraphics[index];
        Color original = _shopButtonOriginalColors[index];
        float e = 0f, half = glowDuration / 2f;
        while (e < half)
        {
            e += Time.deltaTime;
            img.color = Color.Lerp(original, shopGlowColor, e / half);
            yield return null;
        }
        e = 0f;
        while (e < half)
        {
            e += Time.deltaTime;
            img.color = Color.Lerp(shopGlowColor, original, e / half);
            yield return null;
        }
        img.color = original;
    }

    private IEnumerator WobbleButton(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 originalScale = t.localScale;
        float e = 0f, dur = 0.3f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float wobble = Mathf.Sin(e * Mathf.PI * 8f) * 0.08f * (1f - e / dur);
            t.localScale = originalScale * (1f + wobble);
            yield return null;
        }
        t.localScale = originalScale;
    }

    private IEnumerator PopIn(RectTransform t)
    {
        if (t == null) yield break;
        t.localScale = Vector3.zero;
        float e = 0f;
        while (e < popInDuration)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.LerpUnclamped(Vector3.zero, Vector3.one, EaseOutBack(e / popInDuration));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private IEnumerator PopOut(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 start = t.localScale;
        float e = 0f;
        while (e < popOutDuration)
        {
            e += Time.deltaTime;
            t.localScale = Vector3.Lerp(start, Vector3.zero, e / popOutDuration);
            yield return null;
        }
        t.localScale = Vector3.zero;
    }

    private static void SetScaleZero(RectTransform t)
    {
        if (t != null) t.localScale = Vector3.zero;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllRoundsComplete()
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxGameComplete);
        VFXManager.Instance?.SpawnConfetti();

        if (dialogueAudioSource != null && outroAudioClip != null)
        {
            dialogueAudioSource.clip = outroAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(outroAudioClip.length);
        }
        else
        {
            yield return new WaitForSeconds(delayBeforeNextButton);
        }

        nextButton?.gameObject.SetActive(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        foreach (var btn in shopButtons)
            if (btn != null) btn.interactable = value;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Next button — wire this to the Button's OnClick() in the Inspector
    // ════════════════════════════════════════════════════════════════════

    public void OnNextButtonPressed()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
        OnFinished?.Invoke();
    }
}
