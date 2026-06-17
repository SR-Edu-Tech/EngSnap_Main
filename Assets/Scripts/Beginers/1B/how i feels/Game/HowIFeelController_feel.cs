using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HowIFeelController_feel  —  Screen 2 "How I Feel"
/// ─────────────────────────────────────────────────────────────────────────
/// 8 rounds. Each round:
///   Step 1 — Player taps bowl → chit flies out + unfolds → shows feeling word
///   Step 2 — FeelingKid appears on stage + enact audio plays
///   Step 3 — 3 sentence buttons appear → player picks correct one
///
/// HIERARCHY EXAMPLE:
///   Screen2_HowIFeel                 ← this script
///     ├─ Bowl                        ← Button  (tap to launch chit)
///     │    └─ BowlImage              ← Image   (bowl sprite)
///     ├─ TapPrompt                   ← GameObject (label + arrow, hidden after tap)
///     ├─ ChitImage                   ← Image   (the flying chit — starts hidden)
///     ├─ ChitWordText                ← TMP_Text (feeling word on chit — starts hidden)
///     ├─ StageArea
///     │    └─ FeelingKidImage        ← Image   (sprite swaps per round)
///     ├─ SentenceButtonsRoot         ← GameObject (hidden until Step 3)
///     │    ├─ SentenceButton_0       ← SentenceButton_feel
///     │    ├─ SentenceButton_1       ← SentenceButton_feel
///     │    └─ SentenceButton_2       ← SentenceButton_feel
///     └─ RoundLabel                  ← TMP_Text (optional)
///
/// INSPECTOR WIRING — assign all serialized fields below.
/// </summary>
public class HowIFeelController_feel : MonoBehaviour
{
    // ── Round data ───────────────────────────────────────────────────────

    [System.Serializable]
    public class RoundData
    {
        [Tooltip("e.g. 'happy'")]
        public string feelingWord;

        [Tooltip("Sprite for the FeelingKid in this feeling pose")]
        public Sprite feelingKidSprite;

        [Tooltip("Audio: 'Chit says — happy!'")]
        public AudioClip chitAudioClip;

        [Tooltip("Audio: 'Show me happy!'")]
        public AudioClip enactAudioClip;

        [Tooltip("The correct sentence, e.g. 'I am feeling happy.'")]
        public string correctSentence;

        [Tooltip("Audio: 'Yes! I am feeling happy!'")]
        public AudioClip correctAudioClip;

        [Tooltip("Two wrong sentences shown as distractors")]
        public string distractorA;
        public string distractorB;
    }

    // ── Inspector ────────────────────────────────────────────────────────

    [Header("Rounds (8 total)")]
    [SerializeField] private RoundData[] rounds = new RoundData[8];

    [Header("Bowl")]
    [SerializeField] private Button    bowlButton;
    [SerializeField] private Image     bowlImage;
    [SerializeField] private GameObject tapPrompt;       // 'TAP A CHIT!' label + arrow

    [Header("Chit")]
    [SerializeField] private RectTransform chitRect;     // the flying chit RectTransform
    [SerializeField] private Image         chitImage;    // chit image (paper/folded look)
    [SerializeField] private TMP_Text      chitWordText; // feeling word revealed on chit
    [SerializeField] private Sprite        chitFoldedSprite;   // chit before unfold
    [SerializeField] private Sprite        chitUnfoldedSprite; // chit after unfold

    [Header("Stage")]
    [SerializeField] private Image     feelingKidImage;  // sprite swaps per round
    [SerializeField] private GameObject stageArea;        // whole stage GO (hidden at start)

    [Header("Sentence Buttons")]
    [SerializeField] private GameObject          sentenceButtonsRoot; // hidden until Step 3
    [SerializeField] private SentenceButton_feel[] sentenceButtons;   // exactly 3

    [Header("Labels")]
    [SerializeField] private TMP_Text roundLabel;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   sfxChitPop;        // pop when chit launches
    [SerializeField] private AudioClip   sfxCorrect;        // short chime
    [SerializeField] private AudioClip   sfxWrong;          // soft thud
    [SerializeField] private AudioClip   tryAgainClip;      // "Try again!"
    [SerializeField] private AudioClip   allDoneClip;       // "You said all your feelings! Wonderful!"

    [Header("Chit Animation")]
    [SerializeField] private float chitFlightDuration  = 0.55f;  // bowl → stage travel
    [SerializeField] private float chitUnfoldDuration  = 0.35f;  // folded → unfolded
    [SerializeField] private float chitWordPopDuration = 0.25f;  // word pops in
    [SerializeField] private Vector2 chitLandOffset    = new Vector2(60f, 80f); // offset above bowl

    [Header("FeelingKid Animation")]
    [SerializeField] private float kidPopDuration = 0.4f;

    [Header("Button Entrance")]
    [SerializeField] private float buttonStaggerDelay = 0.09f;

    [Header("Timing")]
    [SerializeField] private float afterCorrectDelay = 1.0f;  // pause before next round

    // ── Runtime ──────────────────────────────────────────────────────────

    private GameManager_HowIFeel_feel _manager;
    private int  _currentRound = 0;
    private bool _inputLocked  = false;

    // Chit start position (bowl centre) — cached at start
    private Vector2 _bowlAnchoredPos;

    // ════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════

    public void StartGame(GameManager_HowIFeel_feel manager)
    {
        _manager = manager;
        _currentRound = 0;
        _inputLocked  = false;

        StopAllCoroutines();
        InitialiseUI();
        PrepareRound(_currentRound);
    }

    /// <summary>Called by GameManager before hiding Screen 2 — clean reset.</summary>
    public void ResetGame()
    {
        StopAllCoroutines();
        _currentRound = 0;
        _inputLocked  = false;
        if (audioSource != null) audioSource.Stop();
        InitialiseUI();
    }

    // ════════════════════════════════════════════════════════════════════
    //  UI initialise
    // ════════════════════════════════════════════════════════════════════

    private void InitialiseUI()
    {
        // Hide chit
        if (chitRect  != null) { chitRect.gameObject.SetActive(false); }
        if (chitWordText != null) chitWordText.gameObject.SetActive(false);

        // Hide stage kid
        if (stageArea      != null) stageArea.SetActive(false);
        if (feelingKidImage != null) feelingKidImage.transform.localScale = Vector3.zero;

        // Hide sentence buttons
        if (sentenceButtonsRoot != null) sentenceButtonsRoot.SetActive(false);

        // Show tap prompt
        if (tapPrompt != null) tapPrompt.SetActive(true);

        // Bowl button wiring
        if (bowlButton != null)
        {
            bowlButton.onClick.RemoveAllListeners();
            bowlButton.onClick.AddListener(OnBowlTapped);
            bowlButton.interactable = true;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Round preparation
    // ════════════════════════════════════════════════════════════════════

    private void PrepareRound(int index)
    {
        if (index >= rounds.Length)
        {
            StartCoroutine(AllDoneSequence());
            return;
        }

        _inputLocked = false;

        if (roundLabel != null)
            roundLabel.text = $"Round {index + 1} / {rounds.Length}";

        // Reset chit to hidden at bowl position
        if (chitRect != null)
        {
            chitRect.gameObject.SetActive(false);
            chitRect.anchoredPosition = bowlButton != null
                ? ((RectTransform)bowlButton.transform).anchoredPosition
                : Vector2.zero;
            chitRect.localScale       = Vector3.one * 0.3f;
            chitRect.localEulerAngles = Vector3.zero;
        }
        if (chitWordText != null)
        {
            chitWordText.text  = "";
            chitWordText.gameObject.SetActive(false);
        }

        // Hide stage + buttons
        if (stageArea      != null) stageArea.SetActive(false);
        if (sentenceButtonsRoot != null) sentenceButtonsRoot.SetActive(false);

        // Show bowl + tap prompt
        if (tapPrompt  != null) tapPrompt.SetActive(true);
        if (bowlButton != null) bowlButton.interactable = true;

        // Bowl idle bounce
        StartCoroutine(BowlIdleBounce());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Step 1 — Bowl tapped → chit flies out
    // ════════════════════════════════════════════════════════════════════

    private void OnBowlTapped()
    {
        if (_inputLocked) return;
        _inputLocked = true;

        bowlButton.interactable = false;
        if (tapPrompt != null) tapPrompt.SetActive(false);
        StopCoroutine(nameof(BowlIdleBounce)); // stop idle bounce

        PlaySFX(sfxChitPop);
        StartCoroutine(ChitFlySequence());
    }

    private IEnumerator ChitFlySequence()
    {
        RoundData round = rounds[_currentRound];

        // ── Position chit at bowl centre ─────────────────────────────────
        RectTransform bowlRT = (RectTransform)bowlButton.transform;
        Vector2 startPos = bowlRT.anchoredPosition;

        // Target: chitLandOffset above bowl (the "stage" side)
        Vector2 targetPos = startPos + chitLandOffset;

        if (chitRect != null)
        {
            chitRect.anchoredPosition = startPos;
            chitRect.localScale       = Vector3.one * 0.2f;
            chitRect.localEulerAngles = new Vector3(0, 0, -15f);
            if (chitImage != null && chitFoldedSprite != null)
                chitImage.sprite = chitFoldedSprite;
            chitRect.gameObject.SetActive(true);
        }

        // ── Fly: move + spin + scale up ──────────────────────────────────
        float t = 0f;
        while (t < chitFlightDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / chitFlightDuration);
            float ease = EaseOutBack(p);

            if (chitRect != null)
            {
                chitRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, EaseOutCubic(p));
                chitRect.localScale       = Vector3.one * Mathf.Lerp(0.2f, 1f, ease);
                chitRect.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(-15f, 8f, EaseOutCubic(p)));
            }
            yield return null;
        }

        if (chitRect != null)
        {
            chitRect.anchoredPosition = targetPos;
            chitRect.localScale       = Vector3.one;
            chitRect.localEulerAngles = new Vector3(0, 0, 8f);
        }

        // ── Unfold: swap sprite + wobble rotation to 0 ───────────────────
        yield return StartCoroutine(ChitUnfold());

        // ── Show feeling word ────────────────────────────────────────────
        yield return StartCoroutine(ChitWordPop(round.feelingWord));

        // ── Play "Chit says — happy!" ────────────────────────────────────
        PlayVO(round.chitAudioClip);
        if (round.chitAudioClip != null)
            yield return new WaitForSeconds(round.chitAudioClip.length + 0.2f);

        // ── Step 2: FeelingKid appears ───────────────────────────────────
        yield return StartCoroutine(FeelingKidAppear(round));

        // ── Step 3: Sentence buttons appear ─────────────────────────────
        yield return StartCoroutine(SentenceButtonsAppear(round));

        _inputLocked = false;
    }

    private IEnumerator ChitUnfold()
    {
        if (chitImage != null && chitUnfoldedSprite != null)
            chitImage.sprite = chitUnfoldedSprite;

        // Wobble rotation 8° → 0° with overshoot
        float t = 0f;
        while (t < chitUnfoldDuration)
        {
            t += Time.deltaTime;
            float p   = Mathf.Clamp01(t / chitUnfoldDuration);
            float ang = Mathf.Lerp(8f, 0f, EaseOutBack(p));
            if (chitRect != null)
                chitRect.localEulerAngles = new Vector3(0, 0, ang);
            yield return null;
        }
        if (chitRect != null) chitRect.localEulerAngles = Vector3.zero;
    }

    private IEnumerator ChitWordPop(string word)
    {
        if (chitWordText == null) yield break;

        chitWordText.text = word.ToUpper();
        chitWordText.gameObject.SetActive(true);
        chitWordText.transform.localScale = Vector3.zero;

        float t = 0f;
        while (t < chitWordPopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / chitWordPopDuration);
            chitWordText.transform.localScale = Vector3.one * EaseOutBack(p);
            yield return null;
        }
        chitWordText.transform.localScale = Vector3.one;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Step 2 — FeelingKid appears
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator FeelingKidAppear(RoundData round)
    {
        if (stageArea      != null) stageArea.SetActive(true);
        if (feelingKidImage != null)
        {
            if (round.feelingKidSprite != null)
                feelingKidImage.sprite = round.feelingKidSprite;
            feelingKidImage.transform.localScale = Vector3.zero;
        }

        // Pop in
        float t = 0f;
        while (t < kidPopDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / kidPopDuration);
            if (feelingKidImage != null)
                feelingKidImage.transform.localScale = Vector3.one * EaseOutBack(p);
            yield return null;
        }
        if (feelingKidImage != null)
            feelingKidImage.transform.localScale = Vector3.one;

        // Play "Show me happy!"
        PlayVO(round.enactAudioClip);
        if (round.enactAudioClip != null)
            yield return new WaitForSeconds(round.enactAudioClip.length + 0.3f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Step 3 — Sentence buttons appear
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator SentenceButtonsAppear(RoundData round)
    {
        if (sentenceButtonsRoot == null || sentenceButtons == null) yield break;

        // Build shuffled sentence list
        List<(string text, bool correct)> options = new List<(string, bool)>
        {
            (round.correctSentence, true),
            (round.distractorA,     false),
            (round.distractorB,     false),
        };
        Shuffle(options);

        sentenceButtonsRoot.SetActive(true);

        for (int i = 0; i < sentenceButtons.Length && i < options.Count; i++)
        {
            sentenceButtons[i].Setup(
                text:      options[i].text,
                isCorrect: options[i].correct,
                onTapped:  OnSentenceTapped
            );
            // Stagger pop-in
            StartCoroutine(PopIn(sentenceButtons[i].transform, 0.3f, i * buttonStaggerDelay));
        }

        // Wait for all to pop in before allowing input
        yield return new WaitForSeconds(0.3f + sentenceButtons.Length * buttonStaggerDelay);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Sentence button callback
    // ════════════════════════════════════════════════════════════════════

    private void OnSentenceTapped(SentenceButton_feel btn, bool isCorrect)
    {
        if (_inputLocked) return;

        if (isCorrect)
        {
            _inputLocked = true;
            LockAllSentenceButtons();
            PlaySFX(sfxCorrect);
            btn.PlayCorrectAnim();
            StartCoroutine(CorrectSequence(btn));
        }
        else
        {
            btn.PlayWrongAnim();
            PlaySFX(sfxWrong);
            PlayVO(tryAgainClip);
        }
    }

    private IEnumerator CorrectSequence(SentenceButton_feel correctBtn)
    {
        RoundData round = rounds[_currentRound];

        // Play "Yes! I am feeling happy!"
        PlayVO(round.correctAudioClip);
        if (round.correctAudioClip != null)
            yield return new WaitForSeconds(round.correctAudioClip.length);

        yield return new WaitForSeconds(afterCorrectDelay);

        _currentRound++;
        PrepareRound(_currentRound);
    }

    // ════════════════════════════════════════════════════════════════════
    //  All done
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllDoneSequence()
    {
        // Hide buttons and bowl
        if (sentenceButtonsRoot != null) sentenceButtonsRoot.SetActive(false);
        if (tapPrompt           != null) tapPrompt.SetActive(false);
        if (bowlButton          != null) bowlButton.interactable = false;

        PlayVO(allDoneClip);
        if (allDoneClip != null)
            yield return new WaitForSeconds(allDoneClip.length + 0.5f);

        _manager?.OnScreen2Complete();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Bowl idle bounce — gentle bob while waiting for tap
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator BowlIdleBounce()
    {
        if (bowlButton == null) yield break;
        Transform t   = bowlButton.transform;
        float     tim = 0f;
        while (true)
        {
            tim += Time.deltaTime * 1.8f;
            float s = 1f + Mathf.Sin(tim * Mathf.PI * 2f) * 0.04f;
            t.localScale = Vector3.one * s;
            yield return null;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private void LockAllSentenceButtons()
    {
        if (sentenceButtons == null) return;
        foreach (var btn in sentenceButtons)
            if (btn != null) btn.SetInteractable(false);
    }

    private IEnumerator PopIn(Transform t, float duration, float delay = 0f)
    {
        t.localScale = Vector3.zero;
        if (delay > 0f) yield return new WaitForSeconds(delay);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void PlayVO(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}
