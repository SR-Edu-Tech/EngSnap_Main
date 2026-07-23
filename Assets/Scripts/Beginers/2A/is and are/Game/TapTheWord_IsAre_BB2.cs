using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum IsAreWord_BB2 { Is, Are }

[System.Serializable]
public class GapSentenceData_IsAre_BB2
{
    [Tooltip("Text before the blank ONLY, e.g. 'He ' — do not include the rest of the sentence")]
    public string textBeforeGap;
    [Tooltip("Text after the blank ONLY, e.g. ' kind!' — do not include the rest of the sentence")]
    public string textAfterGap;
    [Tooltip("Correct answer for this sentence")]
    public IsAreWord_BB2 correctWord;
    [Tooltip("Picture cue — one kid for IS, many kids for ARE")]
    public Sprite pictureCue;
    [Tooltip("Optional narrator VO played BEFORE the sentence pops in. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
    [Tooltip("VO of the FULL sentence once filled, e.g. 'He is kind!'")]
    public AudioClip fullSentenceAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 1
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tap The Word — BB2 (IS/ARE).
/// Reveal sequence per sentence: narrator VO (or a short beat) → sentence
/// text + picture pop in together with a sound → the two answer buttons
/// pop in with a sound. Wrong taps are never penalised: button wobbles,
/// picture cue pulses, a gentle hint plays, student tries again.
/// When moving to the next sentence, everything currently on screen pops
/// out first, then the sequence repeats for the new sentence.
/// Fires OnFinished when Next is pressed after all 8 sentences.
/// </summary>
public class TapTheWord_IsAre_BB2 : MonoBehaviour
{
    [Header("Sentences — 8, IN ORDER")]
    public GapSentenceData_IsAre_BB2[] sentences = new GapSentenceData_IsAre_BB2[8];

    [Header("UI — Sentence")]
    public TMP_Text sentenceText;   // full sentence, e.g. "He ___ kind!" → "He is kind!"
    public RectTransform gapSlot;   // invisible marker placed roughly where the blank sits — used for the flying word landing + sparkle VFX position only
    public Image pictureCueImage;

    [Header("UI — Buttons (fixed, pink IS / blue ARE)")]
    public Button   isButton;
    public TMP_Text isButtonLabel;
    public Button   areButton;
    public TMP_Text areButtonLabel;

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'One friend or many? Try again!'")]
    public AudioClip   wrongTapHintClip;

    [Header("Pop FX")]
    [Tooltip("Short 'pop' sound played as the sentence text and picture appear")]
    public AudioClip contentPopSfx;
    [Tooltip("Short 'pop' sound played as the IS/ARE buttons appear")]
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

    void Awake()
    {
        if (isButton != null)
        {
            isButton.onClick.RemoveAllListeners();
            isButton.onClick.AddListener(() => OnWordTapped(IsAreWord_BB2.Is));
        }
        if (areButton != null)
        {
            areButton.onClick.RemoveAllListeners();
            areButton.onClick.AddListener(() => OnWordTapped(IsAreWord_BB2.Are));
        }
        if (isButtonLabel != null)  isButtonLabel.text  = "IS";
        if (areButtonLabel != null) areButtonLabel.text = "ARE";
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        // Start everything invisible so the very first sentence pops in too.
        SetScaleZero(sentenceText != null ? sentenceText.rectTransform : null);
        SetScaleZero(pictureCueImage != null ? pictureCueImage.rectTransform : null);
        SetScaleZero(isButton != null ? isButton.GetComponent<RectTransform>() : null);
        SetScaleZero(areButton != null ? areButton.GetComponent<RectTransform>() : null);
        SetButtonsInteractable(false);

        StartCoroutine(LoadSentenceSequence(0, isFirstLoad: true));

        Debug.Log("[TapTheWord_IsAre_BB2] RestartGame — starting from sentence 0");
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
        if (sentenceText    != null) sentenceText.text = data.textBeforeGap + "___" + data.textAfterGap;
        if (pictureCueImage != null) pictureCueImage.sprite = data.pictureCue;

        // Narrator audio, or a short beat if none is assigned for this sentence.
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

        // Pop in sentence text + picture cue together, with sfx.
        if (contentPopSfx != null) AudioManager.Instance?.PlaySFX(contentPopSfx);
        var contentRoutines = new List<Coroutine>();
        if (sentenceText    != null) contentRoutines.Add(StartCoroutine(PopIn(sentenceText.rectTransform)));
        if (pictureCueImage != null) contentRoutines.Add(StartCoroutine(PopIn(pictureCueImage.rectTransform)));
        foreach (var r in contentRoutines) yield return r;

        yield return new WaitForSeconds(delayBetweenContentAndButtons);

        // Pop in the two answer buttons together, with sfx.
        if (buttonPopSfx != null) AudioManager.Instance?.PlaySFX(buttonPopSfx);
        var buttonRoutines = new List<Coroutine>();
        if (isButton  != null) buttonRoutines.Add(StartCoroutine(PopIn(isButton.GetComponent<RectTransform>())));
        if (areButton != null) buttonRoutines.Add(StartCoroutine(PopIn(areButton.GetComponent<RectTransform>())));
        foreach (var r in buttonRoutines) yield return r;

        SetButtonsInteractable(true);
    }

    private IEnumerator PopOutCurrent()
    {
        var routines = new List<Coroutine>();
        if (sentenceText    != null) routines.Add(StartCoroutine(PopOut(sentenceText.rectTransform)));
        if (pictureCueImage != null) routines.Add(StartCoroutine(PopOut(pictureCueImage.rectTransform)));
        if (isButton  != null) routines.Add(StartCoroutine(PopOut(isButton.GetComponent<RectTransform>())));
        if (areButton != null) routines.Add(StartCoroutine(PopOut(areButton.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnWordTapped(IsAreWord_BB2 tapped)
    {
        var data = sentences[_currentIndex];
        if (tapped == data.correctWord)
            StartCoroutine(HandleCorrectTap(tapped));
        else
            StartCoroutine(HandleWrongTap(tapped));
    }

    private IEnumerator HandleCorrectTap(IsAreWord_BB2 word)
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        yield return StartCoroutine(FlyWordToGap(word));

        var data = sentences[_currentIndex];
        if (sentenceText != null)
            sentenceText.text = data.textBeforeGap + (word == IsAreWord_BB2.Is ? "is" : "are") + data.textAfterGap;

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

    private IEnumerator HandleWrongTap(IsAreWord_BB2 tapped)
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

        Button wrongButton = tapped == IsAreWord_BB2.Is ? isButton : areButton;
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

    private IEnumerator FlyWordToGap(IsAreWord_BB2 word)
    {
        if (gapSlot == null) yield break;

        Button sourceButton = word == IsAreWord_BB2.Is ? isButton : areButton;
        if (sourceButton == null) yield break;

        // Temporary flying label, visually matching the tapped button.
        var flyingGO = new GameObject("FlyingWord", typeof(RectTransform));
        flyingGO.transform.SetParent(mainCanvasGroup != null ? mainCanvasGroup.transform : transform, false);
        var flyingRect = flyingGO.GetComponent<RectTransform>();
        var flyingText = flyingGO.AddComponent<TextMeshProUGUI>();
        flyingText.text      = word == IsAreWord_BB2.Is ? "IS" : "ARE";
        flyingText.fontSize  = 48;
        flyingText.alignment = TextAlignmentOptions.Center;
        flyingText.color     = word == IsAreWord_BB2.Is ? new Color(1f, 0.4f, 0.7f) : new Color(0.3f, 0.6f, 1f);

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

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllSentencesComplete()
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxGameComplete);
        VFXManager.Instance?.SpawnConfetti();

        yield return new WaitForSeconds(delayBeforeNextButton);

        nextButton?.gameObject.SetActive(true);
    }

    private void SetButtonsInteractable(bool value)
    {
        if (isButton  != null) isButton.interactable  = value;
        if (areButton != null) areButton.interactable = value;
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
