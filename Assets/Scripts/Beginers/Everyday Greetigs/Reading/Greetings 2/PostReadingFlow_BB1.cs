using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PostReadingFlow_BB1 : MonoBehaviour, IUnitCompletable
{
    private string saveKey;
    private int currentScreen = 1;

    // ── IUnitCompletable ──────────────────────────────────────────────────
    // These are filled automatically by SharedUnitPanelController.
    // Never assign in Inspector.
    [HideInInspector] public SharedUnitPanelController unitPanel;
    [HideInInspector] public SharedUnitButton          readingUnitButton;

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        unitPanel         = sharedPanel;
        readingUnitButton = sharedButton;
        saveKey           = $"{sharedButton.unitType}_flow";
        currentScreen     = PlayerPrefs.GetInt(saveKey, 1);
    }

    /// <summary>
    /// Called by Greetings_BB1 to hand off references BEFORE SetActive,
    /// so OnEnable already has valid panel and button.
    /// </summary>
    public void OpenFromGreetings(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        if (sharedPanel == null || sharedButton == null)
        {
            Debug.LogError("[PostReadingFlow] OpenFromGreetings called with null references! " +
                           "Make sure the Reading entry in TopicData_BB2 points to the Greetings_BB1 GameObject, " +
                           "not the PostReadingFlow_BB1 GameObject.");
            return;
        }

        unitPanel         = sharedPanel;
        readingUnitButton = sharedButton;
        saveKey           = $"{sharedButton.unitType}_flow";
        currentScreen     = PlayerPrefs.GetInt(saveKey, 1);
        gameObject.SetActive(true); // OnEnable fires here — references already set
    }

    // ═══════════════════════════════════════════════════════
    // ── SCREEN 1 — RESPONSE PRACTICE
    // ═══════════════════════════════════════════════════════
    [Header("── Screen 1: Response Practice ──")]
    public CanvasGroup            screenResponseCG;
    public FlowOptionButton_BB1[] responseButtons;
    public AudioClip[]            responseAudios;
    public AudioSource            responseAudioSource;
    public AudioClip              screen1QuestionAudio;
    public Button                 nextButtonResponse;
    public float autoEnableSeconds = 25f;

    [Header("Response Button Scale")]
    public float responseHighlightScale = 1.08f;
    public float responseScaleSpeed     = 2.5f;

    [Header("Response Colors")]
    public Color responseDefaultColor = Color.white;
    public Color responseTappedColor  = new Color(0.6f, 1f,    0.7f);
    public Color responsePlayingColor = new Color(1f,   0.92f, 0.4f);

    [Header("── Screen Transitions ──")]
    public float screenFadeDuration = 0.4f;

    // ═══════════════════════════════════════════════════════
    // ── SCREEN 2 — HOW DO YOU FEEL
    // ═══════════════════════════════════════════════════════
    [Header("── Screen 2: How Do You Feel ──")]
    public CanvasGroup            screenMoodCG;
    public AudioClip              moodQuestionAudio;
    public AudioSource            moodAudioSource;
    public GameObject             moodButtonsRoot;
    public FlowOptionButton_BB1[] moodButtons;
    public AudioClip[]            moodOptionAudios;
    public RectTransform          popupPanelRect;
    public CanvasGroup            popupPanelCG;
    public TMP_Text               popupText;
    public float popupHoldDuration  = 2.2f;
    public float popupScaleDuration = 0.4f;

    [Header("Mood Button Pulse (Screen 2)")]
    public float moodPulseScale = 1.06f;
    public float moodPulseSpeed = 2f;

    [Header("Mood Colors")]
    public Color moodDefaultColor  = Color.white;
    public Color moodSelectedColor = new Color(0.5f, 0.85f, 1f);

    // ═══════════════════════════════════════════════════════
    // ── SCREEN 3 — STUDENT DIALOGUE
    // ═══════════════════════════════════════════════════════
    [Header("── Screen 3: Student Dialogue ──")]
    public CanvasGroup  screenDialogueCG;
    public Image        studentAImage;
    public GameObject   studentABubble;
    public TMP_Text     studentAText;
    public Button       studentABubbleButton;
    public Image        studentBImage;
    public GameObject   studentBBubble;
    public TMP_Text     studentBText;
    public Button       studentBBubbleButton;

    public string[]    dialogueLines  = new string[3];
    public AudioClip[] dialogueAudios = new AudioClip[3];
    public AudioSource dialogueAudioSource;

    public Button nextButtonDialogue;
    public float  imageFadeDuration = 0.8f;
    public float  pauseBetween      = 0.3f;

    // ═══════════════════════════════════════════════════════
    // PRIVATE STATE
    // ═══════════════════════════════════════════════════════
    private HashSet<int> tappedResponses      = new HashSet<int>();
    private Coroutine    autoEnableCoroutine;
    private Coroutine    screen1Sequence;
    private Coroutine[]  responseScaleRoutines;
    private Coroutine[]  moodPulseRoutines;
    private bool         moodButtonsClickable  = false;
    private bool[]       dialogueLineReady;

    // ═══════════════════════════════════════════════════════
    void OnEnable()
    {
        // Debug: print stack trace so we can see what activated this GO
        Debug.Log("[PostReadingFlow] OnEnable called. unitPanel=" + (unitPanel != null ? "SET" : "NULL") +
                  " readingUnitButton=" + (readingUnitButton != null ? "SET" : "NULL") +
                  "\nStack: " + new System.Diagnostics.StackTrace().ToString());

        if (!string.IsNullOrEmpty(saveKey))
            currentScreen = PlayerPrefs.GetInt(saveKey, 1);
        else
            currentScreen = 1;

        PlayerPrefs.SetInt("reading_state", 2);
        RestoreScreen();
    }

    void RestoreScreen()
    {
        SetCG(screenResponseCG, 0f, false);
        SetCG(screenMoodCG,     0f, false);
        SetCG(screenDialogueCG, 0f, false);

        if      (currentScreen == 1) { SetCG(screenResponseCG, 1f, true); OpenScreen1(); }
        else if (currentScreen == 2) { SetCG(screenMoodCG,     1f, true); OpenScreen2(); }
        else                         { SetCG(screenDialogueCG, 1f, true); OpenScreen3(); }
    }

    // ═══════════════════════════════════════════════════════
    // SCREEN 1
    // ═══════════════════════════════════════════════════════
    void OpenScreen1()
    {
        tappedResponses.Clear();
        responseScaleRoutines = new Coroutine[responseButtons.Length];

        if (nextButtonResponse != null)
        {
            nextButtonResponse.gameObject.SetActive(false);
            nextButtonResponse.interactable = false;
            nextButtonResponse.onClick.RemoveAllListeners();
            nextButtonResponse.onClick.AddListener(OnScreen1Next);
        }

        for (int i = 0; i < responseButtons.Length; i++)
        {
            int idx = i;
            responseButtons[i].Initialize(responseDefaultColor, () => OnResponseTappedByPlayer(idx));
            responseButtons[i].SetScale(Vector3.one);
        }

        if (screen1Sequence     != null) StopCoroutine(screen1Sequence);
        if (autoEnableCoroutine != null) StopCoroutine(autoEnableCoroutine);

        screen1Sequence     = StartCoroutine(AutoPlaySequence());
        autoEnableCoroutine = StartCoroutine(AutoEnableTimer());
    }

    IEnumerator AutoPlaySequence()
    {
        if (responseAudioSource != null && screen1QuestionAudio != null)
        {
            responseAudioSource.Stop();
            responseAudioSource.clip = screen1QuestionAudio;
            responseAudioSource.Play();
            yield return new WaitForSeconds(screen1QuestionAudio.length + 0.25f);
        }
        else yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < responseButtons.Length; i++)
        {
            for (int j = 0; j < responseButtons.Length; j++)
            {
                responseButtons[j].SetColor(j == i ? responsePlayingColor : responseDefaultColor);
                if (responseScaleRoutines[j] != null) { StopCoroutine(responseScaleRoutines[j]); responseScaleRoutines[j] = null; }
                responseButtons[j].SetScale(Vector3.one);
            }

            responseScaleRoutines[i] = StartCoroutine(PulseScale(responseButtons[i], responseHighlightScale, responseScaleSpeed));

            if (responseAudioSource != null && i < responseAudios.Length && responseAudios[i] != null)
            {
                responseAudioSource.Stop();
                responseAudioSource.clip = responseAudios[i];
                responseAudioSource.Play();
                yield return new WaitForSeconds(responseAudios[i].length + 0.15f);
            }
            else yield return new WaitForSeconds(0.8f);

            if (responseScaleRoutines[i] != null) { StopCoroutine(responseScaleRoutines[i]); responseScaleRoutines[i] = null; }
            responseButtons[i].SetScale(Vector3.one);
            responseButtons[i].SetColor(responseTappedColor);
            tappedResponses.Add(i);
        }

        for (int j = 0; j < responseButtons.Length; j++) responseButtons[j].SetColor(responseTappedColor);
        EnableScreen1Next();
    }

    void OnResponseTappedByPlayer(int index)
    {
        for (int j = 0; j < responseButtons.Length; j++)
        {
            responseButtons[j].SetColor(j == index ? responsePlayingColor
                : (tappedResponses.Contains(j) ? responseTappedColor : responseDefaultColor));
            if (j != index && responseScaleRoutines != null && responseScaleRoutines[j] != null)
            {
                StopCoroutine(responseScaleRoutines[j]); responseScaleRoutines[j] = null;
                responseButtons[j].SetScale(Vector3.one);
            }
        }

        if (responseScaleRoutines != null)
        {
            if (responseScaleRoutines[index] != null) { StopCoroutine(responseScaleRoutines[index]); responseScaleRoutines[index] = null; }
            responseScaleRoutines[index] = StartCoroutine(PulseScale(responseButtons[index], responseHighlightScale, responseScaleSpeed));
        }

        if (responseAudioSource != null && index < responseAudios.Length && responseAudios[index] != null)
        {
            responseAudioSource.Stop();
            responseAudioSource.clip = responseAudios[index];
            responseAudioSource.Play();
        }

        StartCoroutine(RestoreAfterPlay(index));
    }

    IEnumerator RestoreAfterPlay(int index)
    {
        float len = (responseAudioSource != null && responseAudioSource.clip != null)
            ? responseAudioSource.clip.length + 0.1f : 0.8f;
        yield return new WaitForSeconds(len);

        if (responseScaleRoutines != null && responseScaleRoutines[index] != null)
        {
            StopCoroutine(responseScaleRoutines[index]); responseScaleRoutines[index] = null;
        }
        responseButtons[index].SetScale(Vector3.one);
        responseButtons[index].SetColor(responseTappedColor);
        tappedResponses.Add(index);
    }

    IEnumerator AutoEnableTimer() { yield return new WaitForSeconds(autoEnableSeconds); EnableScreen1Next(); }

    void EnableScreen1Next()
    {
        if (autoEnableCoroutine != null) { StopCoroutine(autoEnableCoroutine); autoEnableCoroutine = null; }
        if (responseScaleRoutines != null)
            for (int j = 0; j < responseScaleRoutines.Length; j++)
            {
                if (responseScaleRoutines[j] != null) { StopCoroutine(responseScaleRoutines[j]); responseScaleRoutines[j] = null; }
                if (j < responseButtons.Length) responseButtons[j].SetScale(Vector3.one);
            }
        if (nextButtonResponse != null) { nextButtonResponse.gameObject.SetActive(true); nextButtonResponse.interactable = true; }
    }

    void OnScreen1Next()
    {
        currentScreen = 2;
        if (!string.IsNullOrEmpty(saveKey)) { PlayerPrefs.SetInt(saveKey, currentScreen); PlayerPrefs.Save(); }
        if (screen1Sequence != null) StopCoroutine(screen1Sequence);
        StartCoroutine(CrossFade(screenResponseCG, screenMoodCG, OpenScreen2));
    }

    // ═══════════════════════════════════════════════════════
    // SCREEN 2
    // ═══════════════════════════════════════════════════════
    void OpenScreen2()
    {
        moodButtonsClickable = false;
        moodPulseRoutines    = new Coroutine[moodButtons.Length];

        if (moodButtonsRoot != null) moodButtonsRoot.SetActive(false);
        if (popupPanelRect  != null) popupPanelRect.gameObject.SetActive(false);

        for (int i = 0; i < moodButtons.Length; i++)
        {
            int idx = i;
            moodButtons[i].Initialize(moodDefaultColor, () => OnMoodSelected(idx));
            moodButtons[i].SetInteractable(false);
            moodButtons[i].SetScale(Vector3.one);
        }

        StartCoroutine(Screen2Sequence());
    }

    IEnumerator Screen2Sequence()
    {
        if (moodAudioSource != null && moodQuestionAudio != null)
        {
            moodAudioSource.Stop();
            moodAudioSource.clip = moodQuestionAudio;
            moodAudioSource.Play();
            yield return new WaitForSeconds(moodQuestionAudio.length + 0.2f);
        }
        else yield return new WaitForSeconds(0.5f);

        if (moodButtonsRoot != null) moodButtonsRoot.SetActive(true);
        foreach (var btn in moodButtons) btn.SetInteractable(true);
        moodButtonsClickable = true;

        for (int i = 0; i < moodButtons.Length; i++)
        {
            int idx = i;
            moodPulseRoutines[idx] = StartCoroutine(PulseScale(moodButtons[idx], moodPulseScale, moodPulseSpeed));
        }
    }

    void StopAllMoodPulses()
    {
        if (moodPulseRoutines == null) return;
        for (int i = 0; i < moodPulseRoutines.Length; i++)
        {
            if (moodPulseRoutines[i] != null) { StopCoroutine(moodPulseRoutines[i]); moodPulseRoutines[i] = null; }
            if (i < moodButtons.Length) moodButtons[i].SetScale(Vector3.one);
        }
    }

    void OnMoodSelected(int index)
    {
        if (!moodButtonsClickable) return;
        moodButtonsClickable = false;
        StopAllMoodPulses();

        for (int i = 0; i < moodButtons.Length; i++)
        {
            moodButtons[i].SetColor(i == index ? moodSelectedColor : moodDefaultColor);
            moodButtons[i].SetInteractable(false);
        }

        moodPulseRoutines[index] = StartCoroutine(PulseScale(moodButtons[index], moodPulseScale, moodPulseSpeed));
        if (popupText != null) popupText.text = moodButtons[index].label;

        if (moodAudioSource != null && index < moodOptionAudios.Length && moodOptionAudios[index] != null)
        {
            moodAudioSource.Stop();
            moodAudioSource.clip = moodOptionAudios[index];
            moodAudioSource.Play();
        }

        StartCoroutine(BouncePopupThenAdvance(index));
    }

    IEnumerator BouncePopupThenAdvance(int selectedIndex)
    {
        if (popupPanelRect != null) { popupPanelRect.gameObject.SetActive(true); popupPanelRect.localScale = Vector3.zero; }
        if (popupPanelCG  != null) popupPanelCG.alpha = 1f;

        float phase1 = popupScaleDuration * 0.65f, elapsed = 0f;
        while (elapsed < phase1)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (1f - Mathf.Clamp01(elapsed / phase1)) * (1f - Mathf.Clamp01(elapsed / phase1));
            if (popupPanelRect != null) popupPanelRect.localScale = Vector3.one * Mathf.Lerp(0f, 1.18f, t);
            yield return null;
        }

        float phase2 = popupScaleDuration * 0.35f; elapsed = 0f;
        while (elapsed < phase2)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / phase2);
            if (popupPanelRect != null) popupPanelRect.localScale = Vector3.one * Mathf.Lerp(1.18f, 1f, t);
            yield return null;
        }
        if (popupPanelRect != null) popupPanelRect.localScale = Vector3.one;

        yield return new WaitForSeconds(popupHoldDuration);

        if (moodPulseRoutines != null && moodPulseRoutines[selectedIndex] != null)
        {
            StopCoroutine(moodPulseRoutines[selectedIndex]); moodPulseRoutines[selectedIndex] = null;
            moodButtons[selectedIndex].SetScale(Vector3.one);
        }

        if (moodAudioSource != null) moodAudioSource.Stop();
        if (popupPanelRect  != null) popupPanelRect.gameObject.SetActive(false);

        currentScreen = 3;
        if (!string.IsNullOrEmpty(saveKey)) { PlayerPrefs.SetInt(saveKey, currentScreen); PlayerPrefs.Save(); }
        StartCoroutine(CrossFade(screenMoodCG, screenDialogueCG, OpenScreen3));
    }

    // ═══════════════════════════════════════════════════════
    // SCREEN 3
    // ═══════════════════════════════════════════════════════
    void OpenScreen3()
    {
        dialogueLineReady = new bool[dialogueLines.Length];
        SetImageAlpha(studentAImage, 0f);
        SetImageAlpha(studentBImage, 0f);
        if (studentABubble != null) studentABubble.SetActive(false);
        if (studentBBubble != null) studentBBubble.SetActive(false);

        WireBubbleButton(studentABubbleButton, 0, 2);
        WireBubbleButton(studentBBubbleButton, 1);
        DisableBubbleButton(studentABubbleButton);
        DisableBubbleButton(studentBBubbleButton);

        if (nextButtonDialogue != null)
        {
            nextButtonDialogue.gameObject.SetActive(false);
            nextButtonDialogue.interactable = false;
            nextButtonDialogue.onClick.RemoveAllListeners();
            nextButtonDialogue.onClick.AddListener(OnScreen3Next);
        }

        StartCoroutine(RunDialogue());
    }

    void WireBubbleButton(Button btn, params int[] lineIndices)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            for (int k = lineIndices.Length - 1; k >= 0; k--)
            {
                int li = lineIndices[k];
                if (dialogueLineReady != null && li < dialogueLineReady.Length && dialogueLineReady[li])
                    { PlayDialogueAudio(li); break; }
            }
        });
    }

    void EnableBubbleButton(Button btn)  { if (btn != null) btn.interactable = true;  }
    void DisableBubbleButton(Button btn) { if (btn != null) btn.interactable = false; }

    void PlayDialogueAudio(int lineIndex)
    {
        if (dialogueAudioSource == null) return;
        if (lineIndex >= dialogueAudios.Length || dialogueAudios[lineIndex] == null) return;
        dialogueAudioSource.Stop();
        dialogueAudioSource.clip = dialogueAudios[lineIndex];
        dialogueAudioSource.Play();
    }

    IEnumerator RunDialogue()
    {
        yield return StartCoroutine(FadeImage(studentAImage, 0f, 1f));
        yield return StartCoroutine(ShowAndSpeak(studentABubble, studentAText, 0));
        dialogueLineReady[0] = true; EnableBubbleButton(studentABubbleButton);
        yield return new WaitForSeconds(pauseBetween);

        yield return StartCoroutine(FadeImage(studentBImage, 0f, 1f));
        yield return StartCoroutine(ShowAndSpeak(studentBBubble, studentBText, 1));
        dialogueLineReady[1] = true; EnableBubbleButton(studentBBubbleButton);
        yield return new WaitForSeconds(pauseBetween);

        yield return StartCoroutine(ShowAndSpeak(studentABubble, studentAText, 2));
        dialogueLineReady[2] = true;
        yield return new WaitForSeconds(pauseBetween);

        if (nextButtonDialogue != null)
        {
            nextButtonDialogue.gameObject.SetActive(true);
            nextButtonDialogue.interactable = true;
        }
    }

    IEnumerator ShowAndSpeak(GameObject bubble, TMP_Text textComp, int lineIndex)
    {
        if (bubble   != null) bubble.SetActive(true);
        if (textComp != null && lineIndex < dialogueLines.Length) textComp.text = dialogueLines[lineIndex];

        if (dialogueAudioSource != null && lineIndex < dialogueAudios.Length && dialogueAudios[lineIndex] != null)
        {
            dialogueAudioSource.Stop();
            dialogueAudioSource.clip = dialogueAudios[lineIndex];
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(dialogueAudioSource.clip.length);
        }
        else yield return new WaitForSeconds(1f);
    }

    // ── KEY FIX: call UnitFinished BEFORE deactivating this GO ───────────
    void OnScreen3Next()
    {
        StopAllCoroutines();
        if (dialogueAudioSource != null) dialogueAudioSource.Stop();

        // Clear saved progress for this unit
        if (!string.IsNullOrEmpty(saveKey)) { PlayerPrefs.DeleteKey(saveKey); PlayerPrefs.Save(); }

        // Cache references before deactivating (SetActive(false) clears nothing but good practice)
        var cachedPanel  = unitPanel;
        var cachedButton = readingUnitButton;

        // Deactivate self first
        gameObject.SetActive(false);

        // Then notify panel — this shows unit buttons + marks badge complete
        if (cachedPanel != null && cachedButton != null)
            cachedPanel.UnitFinished(cachedButton);
        else
            Debug.LogWarning("PostReadingFlow_BB1: unitPanel or readingUnitButton is null on finish!");
    }

    // ═══════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════
    IEnumerator PulseScale(FlowOptionButton_BB1 btn, float targetScale, float speed)
    {
        float time = 0f;
        while (true)
        {
            time += Time.deltaTime * speed;
            float s = Mathf.Lerp(1f, targetScale, (Mathf.Sin(time * Mathf.PI * 2f) + 1f) * 0.5f);
            btn.SetScale(Vector3.one * s);
            yield return null;
        }
    }

    IEnumerator CrossFade(CanvasGroup outCG, CanvasGroup inCG, System.Action onComplete)
    {
        if (inCG != null) SetCG(inCG, 0f, true);
        float elapsed = 0f;
        while (elapsed < screenFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / screenFadeDuration);
            if (outCG != null) outCG.alpha = 1f - t;
            if (inCG  != null) inCG.alpha  = t;
            yield return null;
        }
        if (outCG != null) SetCG(outCG, 0f, false);
        if (inCG  != null) SetCG(inCG,  1f, true);
        onComplete?.Invoke();
    }

    IEnumerator FadeImage(Image img, float from, float to)
    {
        if (img == null) yield break;
        float elapsed = 0f; Color c = img.color;
        while (elapsed < imageFadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / imageFadeDuration);
            img.color = c;
            yield return null;
        }
        c.a = to; img.color = c;
    }

    static void SetCG(CanvasGroup cg, float alpha, bool interactive)
    {
        if (cg == null) return;
        cg.alpha = alpha; cg.interactable = interactive; cg.blocksRaycasts = interactive;
    }

    static void SetImageAlpha(Image img, float a)
    {
        if (img == null) return; Color c = img.color; c.a = a; img.color = c;
    }

    public void OnBackClicked()
    {
        if (!string.IsNullOrEmpty(saveKey)) { PlayerPrefs.SetInt(saveKey, currentScreen); PlayerPrefs.Save(); }
        StopAllCoroutines();
        if (responseAudioSource != null) responseAudioSource.Stop();
        if (moodAudioSource     != null) moodAudioSource.Stop();
        if (dialogueAudioSource != null) dialogueAudioSource.Stop();
        gameObject.SetActive(false);
        if (unitPanel != null) unitPanel.gameObject.SetActive(true);
    }

   // public void unitfinished()
    //{
      //  panel.UnitFinished(unitButton);
  //  }
}