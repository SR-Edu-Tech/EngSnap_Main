using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
//  WritingScreen_PanelA_MLDL_Game
// ─────────────────────────────────────────────────────────────────────────────
public class WritingScreen_PanelA_MLDL_Game : MonoBehaviour, IUnitCompletable
{
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton          unitButton;

    // Called directly by WritingGame_MLDL_Coordinator after activating this GameObject.
    public void OnUnitStart(SharedUnitPanelController p, SharedUnitButton b)
    {
        panel      = p;
        unitButton = b;
        gameObject.SetActive(true); // triggers OnEnable; if already active, call StartFresh below
    }

    // Public entry point — coordinator calls this after assigning panel/unitButton.
    // Safe to call whether the GameObject just became active or was already active.
    public void StartFresh()
    {
        StopAllCoroutines();
        SafeStop(dialogueAudio);
        ResetAll();
        Setup();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DATA
    // ─────────────────────────────────────────────────────────────────────
    [System.Serializable]
    public class FoodRound
    {
        public string    foodName;
        public Sprite    foodSprite;
        public AudioClip questionAudio;
        public AudioClip yesAudio;
        public AudioClip noAudio;
    }

    [Header("─── Food Rounds (6) ──────────────────────────────")]
    public List<FoodRound> rounds = new List<FoodRound>();

    // ─────────────────────────────────────────────────────────────────────
    //  SCENE REFERENCES
    // ─────────────────────────────────────────────────────────────────────
    [Header("─── Food Card ──────────────────────────────────────")]
    public RectTransform   foodCard;
    public Image           foodImage;
    public TextMeshProUGUI questionText;

    [Header("─── Buttons ────────────────────────────────────────")]
    public Button          yesButton;
    public Button          noButton;
    public RectTransform   yesButtonRect;
    public RectTransform   noButtonRect;

    [Header("─── Audio ──────────────────────────────────────────")]
    public AudioSource     dialogueAudio;
    public AudioSource     sfxAudio;

    [Header("─── SFX ────────────────────────────────────────────")]
    public AudioClip sfxCardAppear;
    public AudioClip sfxYesTap;
    public AudioClip sfxNoTap;
    public AudioClip sfxRoundComplete;
    public AudioClip sfxAllDone;

    [Header("─── Summary Panel ───────────────────────────────────")]
    public GameObject summaryPanel;   // shared summary root

    [Header("─── Panel B ────────────────────────────────────────────")]
    public GameObject panelBObject;   // Panel B root

    [Header("─── Animation ──────────────────────────────────────")]
    public float popDuration  = 0.45f;
    public float slideOffset  = 600f;

    // ─────────────────────────────────────────────────────────────────────
    //  RUNTIME
    // ─────────────────────────────────────────────────────────────────────
    private int          currentRound  = 0;
    private List<string> yesfoods      = new List<string>();
    private List<string> noFoods       = new List<string>();
    private bool         waitingForTap = false;

    private AnimationCurve bounceCurve = new AnimationCurve(
        new Keyframe(0f,    0f,   0f,  6f),
        new Keyframe(0.65f, 1.1f, 0f,  0f),
        new Keyframe(1f,    1f,   0f,  0f));

    // ─────────────────────────────────────────────────────────────────────
    void OnEnable()
    {
        // Guard: skip if panel not yet assigned (scene start before OnUnitStart called).
        // Coordinator calls StartFresh() explicitly after assigning refs.
        if (panel == null) return;
        StartFresh();
    }

    void OnDisable() { StopAllCoroutines(); SafeStop(dialogueAudio); }

    // ─────────────────────────────────────────────────────────────────────
    //  RESET — brings every panel back to a clean starting state
    // ─────────────────────────────────────────────────────────────────────
    void ResetAll()
    {
        // Hide summary and Panel B; Panel A (this) is already active
        if (summaryPanel != null) summaryPanel.SetActive(false);
        if (panelBObject != null) panelBObject.SetActive(false);
    }

    void Setup()
    {
        currentRound  = 0;
        yesfoods      = new List<string>();
        noFoods       = new List<string>();
        waitingForTap = false;

        SetButtonsInteractable(false);

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() => OnAnswer(true));
        noButton.onClick.AddListener(() => OnAnswer(false));

        SetCardPosition(-slideOffset);
        SetScale(foodCard, 0f);

        StartCoroutine(RunRound(currentRound));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ROUND FLOW
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator RunRound(int index)
    {
        if (index >= rounds.Count) yield break;
        var round = rounds[index];

        if (foodImage    != null) foodImage.sprite  = round.foodSprite;
        if (questionText != null) questionText.text = $"Do you like {round.foodName}?";

        PlaySFX(sfxCardAppear);
        yield return StartCoroutine(CardSlideIn());

        yield return new WaitForSeconds(0.25f);

        yield return StartCoroutine(PlayDialogue(round.questionAudio));

        yield return new WaitForSeconds(0.1f);

        yield return StartCoroutine(ButtonsPopIn());
        SetButtonsInteractable(true);

        waitingForTap = true;
        yield return new WaitWhile(() => waitingForTap);
    }

    void OnAnswer(bool isYes)
    {
        if (!waitingForTap) return;
        waitingForTap = false;
        StartCoroutine(HandleAnswer(isYes));
    }

    IEnumerator HandleAnswer(bool isYes)
    {
        SetButtonsInteractable(false);
        var round = rounds[currentRound];

        if (isYes)
        {
            PlaySFX(sfxYesTap);
            yesfoods.Add(round.foodName);
            StartCoroutine(ButtonBounce(yesButtonRect));
        }
        else
        {
            PlaySFX(sfxNoTap);
            noFoods.Add(round.foodName);
            StartCoroutine(ButtonBounce(noButtonRect));
        }

        yield return new WaitForSeconds(0.2f);

        AudioClip feedback = isYes ? round.yesAudio : round.noAudio;
        yield return StartCoroutine(PlayDialogue(feedback));

        yield return new WaitForSeconds(0.25f);
        PlaySFX(sfxRoundComplete);

        yield return StartCoroutine(CardSlideOut());

        currentRound++;
        if (currentRound < rounds.Count)
        {
            yield return new WaitForSeconds(0.2f);
            StartCoroutine(RunRound(currentRound));
        }
        else
        {
            PlaySFX(sfxAllDone);
            yield return new WaitForSeconds(0.6f);
            OpenSummary();
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  OPEN SUMMARY
    //  Summary is activated HERE (before Initialise) so its coroutines run.
    //  Initialise is called immediately after so text and animation start.
    // ─────────────────────────────────────────────────────────────────────
    void OpenSummary()
    {
        var summary = summaryPanel != null
                      ? summaryPanel.GetComponent<WritingScreen_Summary_MLDL_Game>()
                      : null;

        if (summary != null)
        {
            var capturedPanel      = panel;
            var capturedUnitButton = unitButton;

            // Activate FIRST so coroutines inside Initialise can run
            summaryPanel.SetActive(true);
            gameObject.SetActive(false);

            summary.Initialise(yesfoods, noFoods, () =>
            {
                // Next tapped → open Panel B
                if (panelBObject != null)
                {
                    var pb = panelBObject.GetComponent<WritingScreen_PanelB_MLDL_Game>();
                    if (pb != null) pb.OnUnitStart(capturedPanel, capturedUnitButton);
                    panelBObject.SetActive(true);
                }
                else
                {
                    if (capturedPanel != null && capturedUnitButton != null)
                        capturedPanel.UnitFinished(capturedUnitButton);
                }
            });
        }
        else
        {
            if (panel != null && unitButton != null) panel.UnitFinished(unitButton);
            else gameObject.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  ANIMATIONS
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator CardSlideIn()
    {
        if (foodCard == null) yield break;
        float dur = popDuration, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            float s = bounceCurve.Evaluate(t);
            float y = Mathf.Lerp(-slideOffset, 0f, bounceCurve.Evaluate(t));
            foodCard.localScale       = new Vector3(s, s, 1f);
            foodCard.anchoredPosition = new Vector2(foodCard.anchoredPosition.x, y);
            yield return null;
        }
        foodCard.localScale       = Vector3.one;
        foodCard.anchoredPosition = new Vector2(foodCard.anchoredPosition.x, 0f);
    }

    IEnumerator CardSlideOut()
    {
        if (foodCard == null) yield break;
        float dur = 0.3f, elapsed = 0f;
        Vector2 startPos = foodCard.anchoredPosition;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            foodCard.anchoredPosition = new Vector2(startPos.x, Mathf.Lerp(0f, slideOffset, t * t));
            foodCard.localScale       = new Vector3(Mathf.Lerp(1f, 0f, t), Mathf.Lerp(1f, 0f, t), 1f);
            yield return null;
        }
        foodCard.localScale = Vector3.zero;
    }

    IEnumerator ButtonsPopIn()
    {
        SetScale(yesButtonRect, 0f);
        SetScale(noButtonRect,  0f);
        StartCoroutine(ScalePop(yesButtonRect, popDuration));
        yield return new WaitForSeconds(0.1f);
        StartCoroutine(ScalePop(noButtonRect,  popDuration));
        yield return new WaitForSeconds(popDuration);
    }

    IEnumerator ScalePop(RectTransform rt, float dur)
    {
        if (rt == null) yield break;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = bounceCurve.Evaluate(Mathf.Clamp01(elapsed / dur));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    IEnumerator ButtonBounce(RectTransform rt)
    {
        if (rt == null) yield break;
        AnimationCurve c = new AnimationCurve(
            new Keyframe(0f,   1f,   0f, 6f),
            new Keyframe(0.4f, 1.2f, 0f, 0f),
            new Keyframe(1f,   1f,   0f, 0f));
        float dur = 0.35f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = c.Evaluate(Mathf.Clamp01(elapsed / dur));
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────
    IEnumerator PlayDialogue(AudioClip clip)
    {
        if (dialogueAudio == null || clip == null) yield break;
        dialogueAudio.Stop();
        dialogueAudio.clip = clip;
        dialogueAudio.Play();
        yield return new WaitUntil(() => dialogueAudio.isPlaying);
        while (dialogueAudio.isPlaying) yield return null;
    }

    void PlaySFX(AudioClip clip)              { if (sfxAudio    != null && clip != null) sfxAudio.PlayOneShot(clip); }
    void SetButtonsInteractable(bool v)       { if (yesButton   != null) yesButton.interactable = v; if (noButton != null) noButton.interactable = v; }
    void SetCardPosition(float y)             { if (foodCard    != null) foodCard.anchoredPosition = new Vector2(foodCard.anchoredPosition.x, y); }
    void SetScale(RectTransform rt, float s)  { if (rt          != null) rt.localScale = new Vector3(s, s, 1f); }
    void SafeStop(AudioSource a)              { if (a           != null) a.Stop(); }

    public void OnBackClicked()
    {
        StopAllCoroutines();
        SafeStop(dialogueAudio);
        gameObject.SetActive(false);
        if (panel != null) panel.gameObject.SetActive(true);
    }
}