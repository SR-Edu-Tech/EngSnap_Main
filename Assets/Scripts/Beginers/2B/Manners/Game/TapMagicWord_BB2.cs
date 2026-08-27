using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum WordCategory_MagicWords_BB2 { Ask, Thank, Sorry }

[System.Serializable]
public class WordOptionData_MagicWords_BB2
{
    [Tooltip("Word shown on the button, e.g. 'Please' or 'Nice to meet you'")]
    public string wordLabel;
    [Tooltip("This option's category — determines its tint colour (Ask=blue, Thank=green, Sorry=pink)")]
    public WordCategory_MagicWords_BB2 category;
    [Tooltip("True if this is the correct reply for the round's situation")]
    public bool isCorrectWord;
}

[System.Serializable]
public class RoundData_MagicWords_BB2
{
    [Tooltip("Situation text shown on screen, e.g. 'When you meet someone…'")]
    [TextArea] public string situationText;
    [Tooltip("Optional illustration for the situation. Leave empty if not using pictures for this round.")]
    public Sprite situationImage;
    [Tooltip("Optional — swapped in on a correct tap (flower bloom / mascot answering pose). Leave empty to skip.")]
    public Sprite resultVisual;
    [Tooltip("Spoken situation, played before the buttons pop in. Leave empty to use a short pause instead.")]
    public AudioClip promptAudio;
    [Tooltip("VO of the correct word spoken politely, e.g. Tony saying 'Please!'")]
    public AudioClip revealAudio;
    [Tooltip("Exactly 3 word options for this round — one must have isCorrectWord = true")]
    public WordOptionData_MagicWords_BB2[] wordOptions = new WordOptionData_MagicWords_BB2[3];
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — used for BOTH Screen 1 (Which Magic Word?) and
//  Screen 2 (Answer The Situation) — same mechanic, different round data.
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tap The Magic Word — MagicWords_BB2.
/// A situation appears each round with 3 FIXED button slots that get
/// refilled with a new set of reply options each round (7 distinct magic
/// words appear across 8 rounds, so the correct + 2 other options are
/// supplied per round). Each button is tinted by its category colour
/// (ask blue / thank green / sorry pink) for that round. Correct tap: an
/// optional result visual swaps in (flower bloom / mascot pose), the
/// correct word is spoken aloud, chime plays. Wrong tap: gentle wobble +
/// hint, no penalty, retry within the same round.
/// Fires OnFinished after 8 rounds.
///
/// REUSE NOTE: place TWO separate instances of this script in the scene —
/// one on the Screen 1 GameObject, one on Screen 2 — each with its own
/// `rounds` data and its own UI references. The GameManager treats them
/// as two independent screens even though they share this one class.
/// </summary>
public class TapMagicWord_BB2 : MonoBehaviour
{
    [Header("Rounds — 8, IN ORDER")]
    public RoundData_MagicWords_BB2[] rounds = new RoundData_MagicWords_BB2[8];

    [Header("UI — Situation")]
    public TMP_Text situationText;
    [Tooltip("Optional — leave unassigned if this round set doesn't use pictures")]
    public Image situationImage;

    [Header("UI — Word Button Slots (fixed, 3 — refilled with new words + tint each round)")]
    public Button[]   wordButtons = new Button[3];
    public TMP_Text[] wordLabels  = new TMP_Text[3];

    [Header("Category Colors")]
    [SerializeField] private Color askColor   = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color thankColor = new Color(0.4f, 0.85f, 0.4f);
    [SerializeField] private Color sorryColor = new Color(1f, 0.4f, 0.7f);

    [Header("UI — Flow")]
    public CanvasGroup mainCanvasGroup;
    public Button      nextButton;
    public AudioSource dialogueAudioSource;
    [Tooltip("Generic VO for a wrong tap, e.g. 'What do we say here? Try again!'")]
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
    public AudioClip situationPopSfx;
    public AudioClip buttonPopSfx;

    [Header("Timing")]
    [SerializeField] private float resultSwapDelay          = 0.2f;
    [SerializeField] private float delayAfterCorrect         = 0.8f;
    [SerializeField] private float delayBeforeNextButton     = 0.6f;
    [SerializeField] private float popInDuration              = 0.35f;
    [SerializeField] private float popOutDuration             = 0.2f;
    [SerializeField] private float beatWithoutNarration       = 0.25f;
    [SerializeField] private float delayBetweenContentAndButtons = 0.15f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentIndex = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            int capturedIndex = i;
            if (wordButtons[i] != null)
            {
                wordButtons[i].onClick.RemoveAllListeners();
                wordButtons[i].onClick.AddListener(() => OnWordTapped(capturedIndex));
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning($"[TapMagicWord_BB2:{name}] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _currentIndex = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        SetScaleZero(situationText != null ? situationText.rectTransform : null);
        SetScaleZero(situationImage != null ? situationImage.rectTransform : null);
        foreach (var btn in wordButtons)
            SetScaleZero(btn != null ? btn.GetComponent<RectTransform>() : null);
        SetButtonsInteractable(false);

        StartCoroutine(IntroThenLoadRound(0));

        Debug.Log($"[TapMagicWord_BB2:{name}] RestartGame — starting from round 0");
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
    //  Round sequence: pop out old (if any) → refill data+colors → pop in
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadRoundSequence(int index, bool isFirstLoad)
    {
        SetButtonsInteractable(false);

        if (!isFirstLoad)
            yield return StartCoroutine(PopOutCurrent());

        var data = rounds[index];
        if (situationText  != null) situationText.text = data.situationText;
        if (situationImage != null) situationImage.sprite = data.situationImage;

        for (int i = 0; i < wordButtons.Length && i < data.wordOptions.Length; i++)
        {
            var option = data.wordOptions[i];
            if (wordLabels != null && i < wordLabels.Length && wordLabels[i] != null)
                wordLabels[i].text = option.wordLabel;

            var img = wordButtons[i] != null ? wordButtons[i].GetComponent<Image>() : null;
            if (img != null) img.color = ColorForCategory(option.category);
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

        if (situationPopSfx != null) AudioManager.Instance?.PlaySFX(situationPopSfx);
        var contentRoutines = new List<Coroutine>();
        if (situationText  != null) contentRoutines.Add(StartCoroutine(PopIn(situationText.rectTransform)));
        if (situationImage != null) contentRoutines.Add(StartCoroutine(PopIn(situationImage.rectTransform)));
        foreach (var r in contentRoutines) yield return r;

        yield return new WaitForSeconds(delayBetweenContentAndButtons);

        if (buttonPopSfx != null) AudioManager.Instance?.PlaySFX(buttonPopSfx);
        var buttonRoutines = new List<Coroutine>();
        foreach (var btn in wordButtons)
            if (btn != null) buttonRoutines.Add(StartCoroutine(PopIn(btn.GetComponent<RectTransform>())));
        foreach (var r in buttonRoutines) yield return r;

        SetButtonsInteractable(true);
    }

    private IEnumerator PopOutCurrent()
    {
        var routines = new List<Coroutine>();
        if (situationText  != null) routines.Add(StartCoroutine(PopOut(situationText.rectTransform)));
        if (situationImage != null) routines.Add(StartCoroutine(PopOut(situationImage.rectTransform)));
        foreach (var btn in wordButtons)
            if (btn != null) routines.Add(StartCoroutine(PopOut(btn.GetComponent<RectTransform>())));
        foreach (var r in routines) yield return r;
    }

    private Color ColorForCategory(WordCategory_MagicWords_BB2 category) => category switch
    {
        WordCategory_MagicWords_BB2.Ask   => askColor,
        WordCategory_MagicWords_BB2.Thank => thankColor,
        _                                   => sorryColor
    };

    // ════════════════════════════════════════════════════════════════════
    //  Tap handling
    // ════════════════════════════════════════════════════════════════════

    private void OnWordTapped(int index)
    {
        var data = rounds[_currentIndex];
        if (index >= data.wordOptions.Length) return;

        if (data.wordOptions[index].isCorrectWord)
            StartCoroutine(HandleCorrectTap());
        else
            StartCoroutine(HandleWrongTap(index));
    }

    private IEnumerator HandleCorrectTap()
    {
        SetButtonsInteractable(false);
        AudioManager.Instance?.PlaySFX(correctTapSfx != null ? correctTapSfx : AudioManager.Instance.sfxCorrect);

        var data = rounds[_currentIndex];

        yield return new WaitForSeconds(resultSwapDelay);

        if (situationImage != null && data.resultVisual != null)
            situationImage.sprite = data.resultVisual;

        if (situationImage != null) VFXManager.Instance?.SpawnCorrectBurst(situationImage.rectTransform);

        if (dialogueAudioSource != null && data.revealAudio != null)
        {
            dialogueAudioSource.clip = data.revealAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.revealAudio.length);
        }

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

        var rect = wordButtons[index] != null ? wordButtons[index].GetComponent<RectTransform>() : null;
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
        foreach (var btn in wordButtons)
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