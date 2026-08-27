using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum TimeWord_TimeWords_BB2 { Yesterday, Today, Tomorrow }

[System.Serializable]
public class GapSentenceData_TimeWords_BB2
{
    [Tooltip("Text before the blank ONLY, e.g. '' (blank is usually first)")]
    public string textBeforeGap;
    [Tooltip("Text after the blank ONLY, e.g. ' was Friday.'")]
    public string textAfterGap;
    [Tooltip("Correct answer for this sentence")]
    public TimeWord_TimeWords_BB2 correctWord;
    [Tooltip("Picture cue for this sentence")]
    public Sprite pictureCue;
    [Tooltip("Optional narrator VO played before the sentence pops in. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
    [Tooltip("VO of the FULL sentence once filled, e.g. 'Yesterday was Friday!'")]
    public AudioClip fullSentenceAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tap YESTERDAY/TODAY/TOMORROW — TimeWords_BB2.
/// A gapped sentence + picture appears each round. 3 FIXED buttons
/// (orange YESTERDAY / blue TODAY / green TOMORROW) are always on screen —
/// student taps the one that fits (the verb is the clue: was→yesterday,
/// is/am→today, will→tomorrow). Correct: the word drops into the gap in
/// its colour, the full sentence reads aloud, chime plays. Wrong: gentle
/// wobble + hint, no penalty, retry within the same round.
/// Fires OnFinished after 8 rounds.
/// </summary>
public class TapTimeWord_BB2 : MonoBehaviour
{
    [Header("Sentences — 8, IN ORDER")]
    public GapSentenceData_TimeWords_BB2[] sentences = new GapSentenceData_TimeWords_BB2[8];

    [Header("UI — Sentence")]
    public TMP_Text sentenceText;   // full sentence, e.g. "___ was Friday." → "Yesterday was Friday."
    public RectTransform gapSlot;   // invisible marker for the flying word landing + sparkle VFX position only
    public Image pictureCueImage;

    [Header("UI — Buttons (fixed, orange YESTERDAY / blue TODAY / green TOMORROW)")]
    public Button   yesterdayButton;
    public TMP_Text yesterdayButtonLabel;
    public Button   todayButton;
    public TMP_Text todayButtonLabel;
    public Button   tomorrowButton;
    public TMP_Text tomorrowButtonLabel;

    [Header("Word Colors — used for the flying word AND the filled-in gap text")]
    [SerializeField] private Color yesterdayColor = new Color(1f, 0.65f, 0.3f);
    [SerializeField] private Color todayColor     = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color tomorrowColor  = new Color(0.4f, 0.85f, 0.4f);

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'Was, is or will? Try again!'")]
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
    public AudioClip contentPopSfx;
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
    private Color _sentenceTextOriginalColor;

    void Awake()
    {
        if (yesterdayButton != null)
        {
            yesterdayButton.onClick.RemoveAllListeners();
            yesterdayButton.onClick.AddListener(() => OnWordTapped(TimeWord_TimeWords_BB2.Yesterday));
        }
        if (todayButton != null)
        {
            todayButton.onClick.RemoveAllListeners();
            todayButton.onClick.AddListener(() => OnWordTapped(TimeWord_TimeWords_BB2.Today));
        }
        if (tomorrowButton != null)
        {
            tomorrowButton.onClick.RemoveAllListeners();
            tomorrowButton.onClick.AddListener(() => OnWordTapped(TimeWord_TimeWords_BB2.Tomorrow));
        }
        if (yesterdayButtonLabel != null) yesterdayButtonLabel.text = "YESTERDAY";
        if (todayButtonLabel     != null) todayButtonLabel.text     = "TODAY";
        if (tomorrowButtonLabel  != null) tomorrowButtonLabel.text  = "TOMORROW";

        if (sentenceText != null) _sentenceTextOriginalColor = sentenceText.color;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[TapTimeWord_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;
        if (sentenceText != null) sentenceText.color = _sentenceTextOriginalColor;

        SetScaleZero(sentenceText != null ? sentenceText.rectTransform : null);
        SetScaleZero(pictureCueImage != null ? pictureCueImage.rectTransform : null);
        SetScaleZero(yesterdayButton != null ? yesterdayButton.GetComponent<RectTransform>() : null);
        SetScaleZero(todayButton     != null ? todayButton.GetComponent<RectTransform>()     : null);
        SetScaleZero(tomorrowButton  != null ? tomorrowButton.GetComponent<RectTransform>()  : null);
        SetButtonsInteractable(false);

        StartCoroutine(IntroThenLoadSentence(0));

        Debug.Log("[TapTimeWord_BB2] RestartGame — starting from sentence 0");
    }

    private IEnumerator IntroThenLoadSentence(int index)
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        yield return StartCoroutine(LoadSentenceSequence(index, isFirstLoad: true));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Sentence reveal sequence: narrator → pop content → pop buttons
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadSentenceSequence(int index, bool isFirstLoad)
    {
        SetButtonsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCurrent());

        var data = sentences[index];
        if (sentenceText != null)
        {
            sentenceText.text  = data.textBeforeGap + "___" + data.textAfterGap;
            sentenceText.color = _sentenceTextOriginalColor;
        }
        if (pictureCueImage != null) pictureCueImage.sprite = data.pictureCue;

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

        if (contentPopSfx != null) AudioManager.Instance?.PlaySFX(contentPopSfx);
        var contentRoutines = new List<Coroutine>();
        if (sentenceText    != null) contentRoutines.Add(StartCoroutine(PopIn(sentenceText.rectTransform)));
        if (pictureCueImage != null) contentRoutines.Add(StartCoroutine(PopIn(pictureCueImage.rectTransform)));
        foreach (var r in contentRoutines) yield return r;

        yield return new WaitForSeconds(delayBetweenContentAndButtons);

        if (buttonPopSfx != null) AudioManager.Instance?.PlaySFX(buttonPopSfx);
        var buttonRoutines = new List<Coroutine>();
        if (yesterdayButton != null) buttonRoutines.Add(StartCoroutine(PopIn(yesterdayButton.GetComponent<RectTransform>())));
        if (todayButton     != null) buttonRoutines.Add(StartCoroutine(PopIn(todayButton.GetComponent<RectTransform>())));
        if (tomorrowButton  != null) buttonRoutines.Add(StartCoroutine(PopIn(tomorrowButton.GetComponent<RectTransform>())));
        foreach (var r in buttonRoutines) yield return r;

        SetButtonsInteractable(true);
    }

    private IEnumerator PopOutCurrent()
    {
        var routines = new List<Coroutine>();
        if (sentenceText    != null) routines.Add(StartCoroutine(PopOut(sentenceText.rectTransform)));
        if (pictureCueImage != null) routines.Add(StartCoroutine(PopOut(pictureCueImage.rectTransform)));
        if (yesterdayButton != null) routines.Add(StartCoroutine(PopOut(yesterdayButton.GetComponent<RectTransform>())));
        if (todayButton     != null) routines.Add(StartCoroutine(PopOut(todayButton.GetComponent<RectTransform>())));
        if (tomorrowButton  != null) routines.Add(StartCoroutine(PopOut(tomorrowButton.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnWordTapped(TimeWord_TimeWords_BB2 tapped)
    {
        var data = sentences[_currentIndex];
        if (tapped == data.correctWord)
            StartCoroutine(HandleCorrectTap(tapped));
        else
            StartCoroutine(HandleWrongTap(tapped));
    }

    private IEnumerator HandleCorrectTap(TimeWord_TimeWords_BB2 word)
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(correctTapSfx != null ? correctTapSfx : AudioManager.Instance.sfxCorrect);

        yield return StartCoroutine(FlyWordToGap(word));

        var data = sentences[_currentIndex];
        if (sentenceText != null)
        {
            sentenceText.text  = data.textBeforeGap + WordText(word) + data.textAfterGap;
            sentenceText.color = ColorFor(word);
        }

        if (gapSlot != null) VFXManager.Instance?.SpawnCorrectBurst(gapSlot);

        if (dialogueAudioSource != null && data.fullSentenceAudio != null)
        {
            dialogueAudioSource.clip = data.fullSentenceAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.fullSentenceAudio.length);
        }

        yield return new WaitForSeconds(delayAfterCorrect);

        _currentIndex++;
        if (_currentIndex < sentences.Length)
            StartCoroutine(LoadSentenceSequence(_currentIndex, isFirstLoad: false));
        else
            StartCoroutine(AllSentencesComplete());
    }

    private IEnumerator HandleWrongTap(TimeWord_TimeWords_BB2 tapped)
    {
        AudioManager.Instance?.PlaySFX(wrongTapSfx != null ? wrongTapSfx : AudioManager.Instance.sfxWrong);

        Button wrongButton = tapped switch
        {
            TimeWord_TimeWords_BB2.Yesterday => yesterdayButton,
            TimeWord_TimeWords_BB2.Today     => todayButton,
            _                                  => tomorrowButton
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

    private IEnumerator FlyWordToGap(TimeWord_TimeWords_BB2 word)
    {
        if (gapSlot == null) yield break;

        Button sourceButton = word switch
        {
            TimeWord_TimeWords_BB2.Yesterday => yesterdayButton,
            TimeWord_TimeWords_BB2.Today     => todayButton,
            _                                  => tomorrowButton
        };
        if (sourceButton == null) yield break;

        var flyingGO = new GameObject("FlyingWord", typeof(RectTransform));
        flyingGO.transform.SetParent(mainCanvasGroup != null ? mainCanvasGroup.transform : transform, false);
        var flyingRect = flyingGO.GetComponent<RectTransform>();
        var flyingText = flyingGO.AddComponent<TextMeshProUGUI>();
        flyingText.text      = WordText(word).ToUpper();
        flyingText.fontSize  = 40;
        flyingText.alignment = TextAlignmentOptions.Center;
        flyingText.color     = ColorFor(word);

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

    private static string WordText(TimeWord_TimeWords_BB2 word) => word switch
    {
        TimeWord_TimeWords_BB2.Yesterday => "yesterday",
        TimeWord_TimeWords_BB2.Today     => "today",
        _                                  => "tomorrow"
    };

    private Color ColorFor(TimeWord_TimeWords_BB2 word) => word switch
    {
        TimeWord_TimeWords_BB2.Yesterday => yesterdayColor,
        TimeWord_TimeWords_BB2.Today     => todayColor,
        _                                  => tomorrowColor
    };

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllSentencesComplete()
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
        if (yesterdayButton != null) yesterdayButton.interactable = value;
        if (todayButton     != null) todayButton.interactable     = value;
        if (tomorrowButton  != null) tomorrowButton.interactable  = value;
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
