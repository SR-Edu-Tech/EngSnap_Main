using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum PronounWord_Pronouns_BB2 { He, She, It }

[System.Serializable]
public class RoundData_Pronouns_BB2
{
    [Tooltip("The picture for this round, e.g. a boy / a girl / a ball")]
    public Sprite pictureSprite;
    [Tooltip("Correct pronoun for this picture")]
    public PronounWord_Pronouns_BB2 correctPronoun;
    [Tooltip("Optional narrator VO for this round. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tap The Pronoun — Pronouns_BB2.
/// A picture appears each round. 3 FIXED buttons (blue HE / pink SHE /
/// green IT) are always on screen — student taps the matching pronoun.
/// Correct: the tapped button glows in its colour, the picture flies to
/// that pronoun's house, chime plays. Wrong: gentle wobble + hint, no
/// penalty, retry within the same round. Fires OnFinished after 8 rounds.
/// </summary>
public class PronounTap_BB2 : MonoBehaviour
{
    [Header("Rounds — 8, IN ORDER")]
    public RoundData_Pronouns_BB2[] rounds = new RoundData_Pronouns_BB2[8];

    [Header("UI — Picture")]
    public Image pictureImage;

    [Header("UI — Pronoun Buttons (fixed)")]
    public Button heButton;
    public Button sheButton;
    public Button itButton;

    [Header("UI — Houses (fly-to targets, one per pronoun)")]
    public RectTransform heHouseTarget;
    public RectTransform sheHouseTarget;
    public RectTransform itHouseTarget;

    [Header("Glow Colors")]
    [SerializeField] private Color heGlowColor  = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color sheGlowColor = new Color(1f, 0.4f, 0.7f);
    [SerializeField] private Color itGlowColor  = new Color(0.4f, 0.85f, 0.4f);

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'Boy, girl or thing? Try again!'")]
    public AudioClip   wrongTapHintClip;

    [Header("Narration — plays once each")]
    public AudioClip introAudioClip;
    public AudioClip outroAudioClip;

    [Header("Tap Feedback SFX")]
    [Tooltip("Played on a correct tap. Leave empty to fall back to AudioManager's default correct sound.")]
    public AudioClip correctTapSfx;
    [Tooltip("Played on a wrong tap. Leave empty to fall back to AudioManager's default wrong sound.")]
    public AudioClip wrongTapSfx;
    [Tooltip("Chime played when the picture lands in its house")]
    public AudioClip houseChimeSfx;
    public AudioClip picturePopSfx;

    [Header("Timing")]
    [SerializeField] private float glowDuration            = 0.3f;
    [SerializeField] private float flyToHouseDuration       = 0.45f;
    [SerializeField] private float delayAfterCorrect        = 0.7f;
    [SerializeField] private float delayBeforeNextButton    = 0.6f;
    [SerializeField] private float popInDuration             = 0.35f;
    [SerializeField] private float popOutDuration            = 0.2f;
    [SerializeField] private float beatWithoutNarration      = 0.25f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;
    private Vector2 _pictureOriginalAnchoredPos;
    private Dictionary<PronounWord_Pronouns_BB2, Image> _buttonGraphics;
    private Dictionary<PronounWord_Pronouns_BB2, Color> _buttonOriginalColors;

    void Awake()
    {
        if (heButton  != null) { heButton.onClick.RemoveAllListeners();  heButton.onClick.AddListener(() => OnPronounTapped(PronounWord_Pronouns_BB2.He)); }
        if (sheButton != null) { sheButton.onClick.RemoveAllListeners(); sheButton.onClick.AddListener(() => OnPronounTapped(PronounWord_Pronouns_BB2.She)); }
        if (itButton  != null) { itButton.onClick.RemoveAllListeners();  itButton.onClick.AddListener(() => OnPronounTapped(PronounWord_Pronouns_BB2.It)); }

        if (pictureImage != null)
            _pictureOriginalAnchoredPos = pictureImage.rectTransform.anchoredPosition;

        _buttonGraphics = new Dictionary<PronounWord_Pronouns_BB2, Image>
        {
            { PronounWord_Pronouns_BB2.He,  heButton  != null ? heButton.GetComponent<Image>()  : null },
            { PronounWord_Pronouns_BB2.She, sheButton != null ? sheButton.GetComponent<Image>() : null },
            { PronounWord_Pronouns_BB2.It,  itButton  != null ? itButton.GetComponent<Image>()  : null },
        };
        _buttonOriginalColors = new Dictionary<PronounWord_Pronouns_BB2, Color>();
        foreach (var kv in _buttonGraphics)
            _buttonOriginalColors[kv.Key] = kv.Value != null ? kv.Value.color : Color.white;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[PronounTap_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        SetScaleZero(pictureImage != null ? pictureImage.rectTransform : null);
        SetScaleZero(heButton  != null ? heButton.GetComponent<RectTransform>()  : null);
        SetScaleZero(sheButton != null ? sheButton.GetComponent<RectTransform>() : null);
        SetScaleZero(itButton  != null ? itButton.GetComponent<RectTransform>()  : null);
        SetButtonsInteractable(false);

        StartCoroutine(IntroThenLoadRound(0));

        Debug.Log("[PronounTap_BB2] RestartGame — starting from round 0");
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
    //  Round sequence: narrator → pop picture in → pop buttons in
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadRoundSequence(int index, bool isFirstLoad)
    {
        SetButtonsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCurrent());

        var data = rounds[index];
        if (pictureImage != null)
        {
            pictureImage.sprite = data.pictureSprite;
            pictureImage.rectTransform.anchoredPosition = _pictureOriginalAnchoredPos;
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

        if (picturePopSfx != null) AudioManager.Instance?.PlaySFX(picturePopSfx);
        if (pictureImage != null) yield return StartCoroutine(PopIn(pictureImage.rectTransform));

        var buttonRoutines = new List<Coroutine>();
        if (heButton  != null) buttonRoutines.Add(StartCoroutine(PopIn(heButton.GetComponent<RectTransform>())));
        if (sheButton != null) buttonRoutines.Add(StartCoroutine(PopIn(sheButton.GetComponent<RectTransform>())));
        if (itButton  != null) buttonRoutines.Add(StartCoroutine(PopIn(itButton.GetComponent<RectTransform>())));
        foreach (var r in buttonRoutines) yield return r;

        SetButtonsInteractable(true);
    }

    private IEnumerator PopOutCurrent()
    {
        var routines = new List<Coroutine>();
        if (pictureImage != null) routines.Add(StartCoroutine(PopOut(pictureImage.rectTransform)));
        if (heButton  != null) routines.Add(StartCoroutine(PopOut(heButton.GetComponent<RectTransform>())));
        if (sheButton != null) routines.Add(StartCoroutine(PopOut(sheButton.GetComponent<RectTransform>())));
        if (itButton  != null) routines.Add(StartCoroutine(PopOut(itButton.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnPronounTapped(PronounWord_Pronouns_BB2 tapped)
    {
        var data = rounds[_currentIndex];
        if (tapped == data.correctPronoun)
            StartCoroutine(HandleCorrectTap(tapped));
        else
            StartCoroutine(HandleWrongTap(tapped));
    }

    private IEnumerator HandleCorrectTap(PronounWord_Pronouns_BB2 word)
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(correctTapSfx != null ? correctTapSfx : AudioManager.Instance.sfxCorrect);

        yield return StartCoroutine(GlowButton(word));

        RectTransform house = word switch
        {
            PronounWord_Pronouns_BB2.He  => heHouseTarget,
            PronounWord_Pronouns_BB2.She => sheHouseTarget,
            _                             => itHouseTarget
        };

        if (pictureImage != null && house != null)
            yield return StartCoroutine(FlyToHouse(pictureImage.rectTransform, house));

        if (houseChimeSfx != null) AudioManager.Instance?.PlaySFX(houseChimeSfx);
        if (house != null) VFXManager.Instance?.SpawnCorrectBurst(house);

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < rounds.Length)
            StartCoroutine(LoadRoundSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllRoundsComplete());
    }

    private IEnumerator HandleWrongTap(PronounWord_Pronouns_BB2 tapped)
    {
        AudioManager.Instance?.PlaySFX(wrongTapSfx != null ? wrongTapSfx : AudioManager.Instance.sfxWrong);

        Button wrongButton = tapped switch
        {
            PronounWord_Pronouns_BB2.He  => heButton,
            PronounWord_Pronouns_BB2.She => sheButton,
            _                              => itButton
        };
        if (wrongButton != null)
            yield return StartCoroutine(WobbleButton(wrongButton.GetComponent<RectTransform>()));

        if (dialogueAudioSource != null && wrongTapHintClip != null)
        {
            dialogueAudioSource.clip = wrongTapHintClip;
            dialogueAudioSource.Play();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator GlowButton(PronounWord_Pronouns_BB2 word)
    {
        if (!_buttonGraphics.TryGetValue(word, out var image) || image == null) yield break;

        Color originalColor = _buttonOriginalColors[word];
        Color glowColor     = word switch
        {
            PronounWord_Pronouns_BB2.He  => heGlowColor,
            PronounWord_Pronouns_BB2.She => sheGlowColor,
            _                              => itGlowColor
        };

        float e = 0f, halfDur = glowDuration / 2f;
        while (e < halfDur)
        {
            e += Time.deltaTime;
            image.color = Color.Lerp(originalColor, glowColor, e / halfDur);
            yield return null;
        }
        e = 0f;
        while (e < halfDur)
        {
            e += Time.deltaTime;
            image.color = Color.Lerp(glowColor, originalColor, e / halfDur);
            yield return null;
        }
        image.color = originalColor;
    }

    private IEnumerator FlyToHouse(RectTransform pictureRect, RectTransform house)
    {
        Vector3 startPos = pictureRect.position;
        Vector3 endPos   = house.position;
        float e = 0f;
        while (e < flyToHouseDuration)
        {
            e += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, e / flyToHouseDuration);
            pictureRect.position   = Vector3.Lerp(startPos, endPos, t);
            pictureRect.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }
        pictureRect.localScale = Vector3.zero;
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
        if (heButton  != null) heButton.interactable  = value;
        if (sheButton != null) sheButton.interactable = value;
        if (itButton  != null) itButton.interactable  = value;
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
