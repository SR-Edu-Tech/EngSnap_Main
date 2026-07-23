using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum PotWord_POT_BB2 { In, On, At }

[System.Serializable]
public class GapPhraseData_POT_BB2
{
    [Tooltip("Text before the blank ONLY, e.g. '' (blank is usually first) — do not include the rest of the phrase")]
    public string textBeforeGap;
    [Tooltip("Text after the blank ONLY, e.g. ' the morning'")]
    public string textAfterGap;
    [Tooltip("Correct answer for this phrase")]
    public PotWord_POT_BB2 correctWord;
    [Tooltip("Time picture cue, e.g. sunrise for 'in the morning'")]
    public Sprite pictureCue;
    [Tooltip("Optional narrator VO played BEFORE the phrase pops in. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
    [Tooltip("VO of the FULL phrase once filled, e.g. 'in the morning!'")]
    public AudioClip fullPhraseAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tap The Word — POT_BB2 (Preposition Of Time: IN / ON / AT).
/// Reveal sequence per phrase: narrator VO (or a short beat) → phrase text
/// + picture pop in together with a sound → the three answer buttons pop
/// in with a sound. Wrong taps are never penalised: button wobbles,
/// picture cue pulses, a gentle hint plays, student tries again.
/// When moving to the next phrase, everything currently on screen pops
/// out first, then the sequence repeats for the new phrase.
/// Fires OnFinished when Next is pressed after all 8 phrases.
/// </summary>
public class TapTheWord_POT_BB2 : MonoBehaviour
{
    [Header("Phrases — 8, IN ORDER")]
    public GapPhraseData_POT_BB2[] phrases = new GapPhraseData_POT_BB2[8];

    [Header("UI — Phrase")]
    public TMP_Text phraseText;     // full phrase, e.g. "___ the morning" → "in the morning!"
    public RectTransform gapSlot;   // invisible marker placed roughly where the blank sits — used for the flying word landing + sparkle VFX position only
    public Image pictureCueImage;

    [Header("UI — Buttons (fixed, pink IN / blue ON / purple AT)")]
    public Button   inButton;
    public TMP_Text inButtonLabel;
    public Button   onButton;
    public TMP_Text onButtonLabel;
    public Button   atButton;
    public TMP_Text atButtonLabel;

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'Look at the time — try again!'")]
    public AudioClip   wrongTapHintClip;

    [Header("Pop FX")]
    [Tooltip("Short 'pop' sound played as the phrase text and picture appear")]
    public AudioClip contentPopSfx;
    [Tooltip("Short 'pop' sound played as the IN/ON/AT buttons appear")]
    public AudioClip buttonPopSfx;

    [Header("Timing")]
    [SerializeField] private float flyDuration                   = 0.3f;
    [SerializeField] private float delayAfterCorrect              = 0.9f;
    [SerializeField] private float delayBeforeNextButton          = 0.6f;
    [SerializeField] private float popInDuration                  = 0.35f;
    [SerializeField] private float popOutDuration                 = 0.2f;
    [SerializeField] private float beatWithoutNarration           = 0.25f;
    [SerializeField] private float delayBetweenContentAndButtons  = 0.15f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        if (inButton != null)
        {
            inButton.onClick.RemoveAllListeners();
            inButton.onClick.AddListener(() => OnWordTapped(PotWord_POT_BB2.In));
        }
        if (onButton != null)
        {
            onButton.onClick.RemoveAllListeners();
            onButton.onClick.AddListener(() => OnWordTapped(PotWord_POT_BB2.On));
        }
        if (atButton != null)
        {
            atButton.onClick.RemoveAllListeners();
            atButton.onClick.AddListener(() => OnWordTapped(PotWord_POT_BB2.At));
        }
        if (inButtonLabel != null) inButtonLabel.text = "IN";
        if (onButtonLabel != null) onButtonLabel.text = "ON";
        if (atButtonLabel != null) atButtonLabel.text = "AT";
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        // Guards against RestartGame() firing twice in the same click
        // (e.g. a duplicate OnClick() listener) so intro/reveal audio
        // never gets cut off and restarted.
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[TapTheWord_POT_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        // Start everything invisible so the very first phrase pops in too.
        SetScaleZero(phraseText != null ? phraseText.rectTransform : null);
        SetScaleZero(pictureCueImage != null ? pictureCueImage.rectTransform : null);
        SetScaleZero(inButton != null ? inButton.GetComponent<RectTransform>() : null);
        SetScaleZero(onButton != null ? onButton.GetComponent<RectTransform>() : null);
        SetScaleZero(atButton != null ? atButton.GetComponent<RectTransform>() : null);
        SetButtonsInteractable(false);

        StartCoroutine(LoadPhraseSequence(0, isFirstLoad: true));

        Debug.Log("[TapTheWord_POT_BB2] RestartGame — starting from phrase 0");
    }

    // ════════════════════════════════════════════════════════════════════
    //  Phrase reveal sequence: narrator → pop content → pop buttons
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadPhraseSequence(int index, bool isFirstLoad)
    {
        SetButtonsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCurrent());

        var data = phrases[index];
        if (phraseText      != null) phraseText.text = data.textBeforeGap + "___" + data.textAfterGap;
        if (pictureCueImage != null) pictureCueImage.sprite = data.pictureCue;

        // Narrator audio, or a short beat if none is assigned for this phrase.
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

        // Pop in phrase text + picture cue together, with sfx.
        if (contentPopSfx != null) AudioManager.Instance?.PlaySFX(contentPopSfx);
        var contentRoutines = new List<Coroutine>();
        if (phraseText      != null) contentRoutines.Add(StartCoroutine(PopIn(phraseText.rectTransform)));
        if (pictureCueImage != null) contentRoutines.Add(StartCoroutine(PopIn(pictureCueImage.rectTransform)));
        foreach (var r in contentRoutines) yield return r;

        yield return new WaitForSeconds(delayBetweenContentAndButtons);

        // Pop in the three answer buttons together, with sfx.
        if (buttonPopSfx != null) AudioManager.Instance?.PlaySFX(buttonPopSfx);
        var buttonRoutines = new List<Coroutine>();
        if (inButton != null) buttonRoutines.Add(StartCoroutine(PopIn(inButton.GetComponent<RectTransform>())));
        if (onButton != null) buttonRoutines.Add(StartCoroutine(PopIn(onButton.GetComponent<RectTransform>())));
        if (atButton != null) buttonRoutines.Add(StartCoroutine(PopIn(atButton.GetComponent<RectTransform>())));
        foreach (var r in buttonRoutines) yield return r;

        SetButtonsInteractable(true);
    }

    private IEnumerator PopOutCurrent()
    {
        var routines = new List<Coroutine>();
        if (phraseText      != null) routines.Add(StartCoroutine(PopOut(phraseText.rectTransform)));
        if (pictureCueImage != null) routines.Add(StartCoroutine(PopOut(pictureCueImage.rectTransform)));
        if (inButton != null) routines.Add(StartCoroutine(PopOut(inButton.GetComponent<RectTransform>())));
        if (onButton != null) routines.Add(StartCoroutine(PopOut(onButton.GetComponent<RectTransform>())));
        if (atButton != null) routines.Add(StartCoroutine(PopOut(atButton.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnWordTapped(PotWord_POT_BB2 tapped)
    {
        var data = phrases[_currentIndex];
        if (tapped == data.correctWord)
            StartCoroutine(HandleCorrectTap(tapped));
        else
            StartCoroutine(HandleWrongTap(tapped));
    }

    private IEnumerator HandleCorrectTap(PotWord_POT_BB2 word)
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        yield return StartCoroutine(FlyWordToGap(word));

        var data = phrases[_currentIndex];
        if (phraseText != null)
            phraseText.text = data.textBeforeGap + WordText(word) + data.textAfterGap;

        if (gapSlot != null) VFXManager.Instance?.SpawnCorrectBurst(gapSlot);

        if (dialogueAudioSource != null && data.fullPhraseAudio != null)
        {
            dialogueAudioSource.clip = data.fullPhraseAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.fullPhraseAudio.length);
        }

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < phrases.Length)
            StartCoroutine(LoadPhraseSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllPhrasesComplete());
    }

    private IEnumerator HandleWrongTap(PotWord_POT_BB2 tapped)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

        Button wrongButton = tapped switch
        {
            PotWord_POT_BB2.In => inButton,
            PotWord_POT_BB2.On => onButton,
            _                  => atButton
        };
        if (wrongButton != null)
            yield return StartCoroutine(WobbleButton(wrongButton.GetComponent<RectTransform>()));

        if (pictureCueImage != null)
            yield return StartCoroutine(PulseImage(pictureCueImage.rectTransform));

        if (dialogueAudioSource != null && wrongTapHintClip != null)
        {
            dialogueAudioSource.clip = wrongTapHintClip;
            dialogueAudioSource.Play();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Animation
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator FlyWordToGap(PotWord_POT_BB2 word)
    {
        if (gapSlot == null) yield break;

        Button sourceButton = word switch
        {
            PotWord_POT_BB2.In => inButton,
            PotWord_POT_BB2.On => onButton,
            _                  => atButton
        };
        if (sourceButton == null) yield break;

        // Temporary flying label, visually matching the tapped button.
        var flyingGO = new GameObject("FlyingWord", typeof(RectTransform));
        flyingGO.transform.SetParent(mainCanvasGroup != null ? mainCanvasGroup.transform : transform, false);
        var flyingRect = flyingGO.GetComponent<RectTransform>();
        var flyingText = flyingGO.AddComponent<TextMeshProUGUI>();
        flyingText.text      = word.ToString().ToUpper();
        flyingText.fontSize  = 48;
        flyingText.alignment = TextAlignmentOptions.Center;
        flyingText.color     = word switch
        {
            PotWord_POT_BB2.In => new Color(1f, 0.4f, 0.7f),   // pink
            PotWord_POT_BB2.On => new Color(0.3f, 0.6f, 1f),   // blue
            _                  => new Color(0.6f, 0.4f, 0.9f)  // purple
        };

        flyingRect.position = sourceButton.GetComponent<RectTransform>().position;

        Vector3 startPos = flyingRect.position;
        Vector3 endPos   = gapSlot.position;
        float e = 0f;
        while (e < flyDuration)
        {
            e += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, e / flyDuration);
            flyingRect.position = Vector3.Lerp(startPos, endPos, p);
            yield return null;
        }

        Destroy(flyingGO);
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

    private IEnumerator PulseImage(RectTransform t)
    {
        if (t == null) yield break;
        Vector3 originalScale = t.localScale;
        float e = 0f, dur = 0.4f;
        while (e < dur)
        {
            e += Time.deltaTime;
            float pulse = Mathf.Sin(e / dur * Mathf.PI);
            t.localScale = originalScale * (1f + pulse * 0.15f);
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

    private static string WordText(PotWord_POT_BB2 word) => word switch
    {
        PotWord_POT_BB2.In => "in",
        PotWord_POT_BB2.On => "on",
        _                  => "at"
    };

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllPhrasesComplete()
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxGameComplete);
        VFXManager.Instance?.SpawnConfetti();

        yield return new WaitForSeconds(delayBeforeNextButton);

        nextButton?.gameObject.SetActive(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (inButton != null) inButton.interactable = value;
        if (onButton != null) onButton.interactable = value;
        if (atButton != null) atButton.interactable = value;
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
