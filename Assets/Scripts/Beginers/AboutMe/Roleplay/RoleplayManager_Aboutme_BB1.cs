using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoleplayManager_Aboutme_BB1 : MonoBehaviour, IUnitCompletable
{
  // ─────────────────────────────────────────────────────────────────────────
    // DATA STRUCTURES
    // ─────────────────────────────────────────────────────────────────────────
 
    [System.Serializable]
    public class ChoiceOption
    {
        [TextArea] public string text;
        public bool isCorrect;
    }
 
    [System.Serializable]

public class DialogueTurn
{
    [Header("Optional Question")]
    [TextArea] public string questionText;
    public Sprite questionSprite;

    [Header("Girl auto-line")]
    [TextArea] public string girlText;
    public AudioClip girlAudio;

    [Header("Choice cards (3)")]
    public ChoiceOption[] choices;

    [Header("Boy correct reply (after correct choice)")]
    [TextArea] public string boyText;
    public AudioClip boyAudio;

    [Header("Girl follow-up (auto, after boy reply)")]
    [TextArea] public string girlFollowUpText;
    public AudioClip girlFollowUpAudio;
}
 
    // ─────────────────────────────────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────────────────────────────────
 
    [Header("── Dialogue Data ──")]
    public List<DialogueTurn> turns = new List<DialogueTurn>();
 
    [Header("── Chat Area ──")]
    public ScrollRect      chatScrollRect;
    public RectTransform   chatContent;

    [Header("── Question UI ──")]
public GameObject questionPanel;              // Optional
public TextMeshProUGUI questionTextUI;        // Optional
public Image questionImageUI;                 // Optional
 
    [Header("── Bubble Prefabs (create 2 simple prefabs) ──")]
    public GameObject      girlBubblePrefab;
    public GameObject      boyBubblePrefab;
 
    [Header("── Character Images ──")]
    public Image           girlCharacter;
    public Image           boyCharacter;
    public Sprite          girlTalkSprite;
    public Sprite          girlIdleSprite;
    public Sprite          boyTalkSprite;
    public Sprite          boyIdleSprite;
 
    [Header("── Choice Panel ──")]
    public GameObject      choicePanel;
    public Button[]        choiceButtons;
    public TextMeshProUGUI[] choiceTexts;
    public Image[]         choiceBGs;
 
    [Header("── Completion Panel ──")]
    public GameObject      completionPanel;
    public TextMeshProUGUI wellDoneText;
    public Button          nextButton;

    // ── CHANGED: Navigation fields ────────────────────────────────────────────
    [Header("── Navigation ──")]
    [Tooltip("Root GameObject wrapping ALL screens (Roleplay + IntroCard + Speech).\n" +
             "This gets disabled when the player finishes and returns to units.")]
    public GameObject      roleplayParent;

    [Tooltip("Screen 2 — IntroCard fill-in-blanks panel.\n" +
             "Shown when Next is pressed after roleplay completion.")]
    public GameObject      introCardPanel;

    [Tooltip("Units panel to re-enable after the full flow completes.\n" +
             "Also wire this same reference into Logic_BB1's unitPanel field.")]
    public GameObject      unitPanel;
    // ─────────────────────────────────────────────────────────────────────────
 
    [Header("── Audio ──")]
    public AudioSource     dialogueAudioSource;
    public AudioSource     sfxSource;
    public AudioClip       correctSFX;
    public AudioClip       wrongSFX;
    public AudioClip       cardPopSFX;
    public AudioClip       completeSFX;
    public AudioClip       bubblePopSFX;
 
    [Header("── Colors ──")]
    public Color cardNormal  = new Color(1f,    1f,    1f,    1f);
    public Color cardCorrect = new Color(0.22f, 0.85f, 0.42f, 1f);
    public Color cardWrong   = new Color(0.95f, 0.28f, 0.28f, 1f);
 
    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────────────
 
    private int  _turnIndex  = 0;
    private bool _canChoose  = false;
    private List<GameObject> _spawnedBubbles = new List<GameObject>();

    // Guards OnEnable from running the reset before Start() has executed once
    private bool _started = false;
 

     [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton unitButton;

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        panel      = sharedPanel;
        unitButton = sharedButton;
    }
    // ─────────────────────────────────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────
 
    void Start()
    {
        _started = true;

        choicePanel.SetActive(false);
        completionPanel.SetActive(false);
 
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int captured = i;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(captured));
        }
 
        nextButton.onClick.AddListener(OnNextPressed);
 
        SetCharacterState(girlCharacter, girlIdleSprite);
        SetCharacterState(boyCharacter,  boyIdleSprite);
 
        StartCoroutine(CharacterEntrance());
    }

    // ── CHANGED: Reset to Screen 1 every time roleplayParent is re-enabled ──
    void OnEnable()
    {
        if (!_started) return;  // skip on very first enable — Start() handles it

        StopAllCoroutines();

        // Clean up any leftover bubbles from the previous session
        foreach (var b in _spawnedBubbles)
            if (b != null) Destroy(b);
        _spawnedBubbles.Clear();

        _turnIndex = 0;
        _canChoose = false;

        choicePanel.SetActive(false);
        completionPanel.SetActive(false);

        // Hide Screen 2 so only this roleplay screen is visible
        if (introCardPanel != null) introCardPanel.SetActive(false);

        SetCharacterState(girlCharacter, girlIdleSprite);
        SetCharacterState(boyCharacter,  boyIdleSprite);

        StartCoroutine(CharacterEntrance());
    }
    // ─────────────────────────────────────────────────────────────────────────
 
    // ─────────────────────────────────────────────────────────────────────────
    // CHARACTER ENTRANCE
    // ─────────────────────────────────────────────────────────────────────────
 
    IEnumerator CharacterEntrance()
    {
        StartCoroutine(SlideIn(boyCharacter.transform,  new Vector2(-120f, 0f), 0.5f));
        yield return new WaitForSeconds(0.15f);
        StartCoroutine(SlideIn(girlCharacter.transform, new Vector2( 120f, 0f), 0.5f));
        yield return new WaitForSeconds(0.7f);
 
        StartCoroutine(PlayTurn(_turnIndex));
    }
 
    IEnumerator SlideIn(Transform t, Vector2 offset, float duration)
    {
        Vector3 target = t.localPosition;
        t.localPosition = target + new Vector3(offset.x, offset.y, 0f);
 
        CanvasGroup cg = t.GetComponent<CanvasGroup>();
        if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
 
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            t.localPosition = Vector3.Lerp(
                target + new Vector3(offset.x, offset.y, 0f), target, p);
            cg.alpha = p;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localPosition = target;
        cg.alpha = 1f;
    }
 
    // ─────────────────────────────────────────────────────────────────────────
    // TURN DRIVER
    // ─────────────────────────────────────────────────────────────────────────
 
    IEnumerator PlayTurn(int index)
    {
        if (index >= turns.Count) yield break;
        DialogueTurn turn = turns[index];
        UpdateQuestionUI(turn);
        if (index > 0)
            yield return StartCoroutine(ClearBubbles());
 
        yield return StartCoroutine(
            SpeakAndBubble(girlCharacter, girlTalkSprite, girlIdleSprite,
                           turn.girlText, turn.girlAudio, isGirl: true));
 
        yield return new WaitForSeconds(0.3f);
 
        yield return StartCoroutine(ShowChoiceCards(turn));
    }
 
    IEnumerator ClearBubbles()
    {
        List<Coroutine> fades = new List<Coroutine>();
        foreach (GameObject bubble in _spawnedBubbles)
        {
            if (bubble != null)
                fades.Add(StartCoroutine(FadeOutAndDestroy(bubble, 0.3f)));
        }
        yield return new WaitForSeconds(0.35f);
        _spawnedBubbles.Clear();
    }
 
    IEnumerator FadeOutAndDestroy(GameObject go, float duration)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
 
        Vector3 startScale = go.transform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float p = elapsed / duration;
            cg.alpha = 1f - p;
            go.transform.localScale = Vector3.Lerp(startScale, startScale * 0.7f, p);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(go);
    }
 
    // ─────────────────────────────────────────────────────────────────────────
    // SPEAK + BUBBLE
    // ─────────────────────────────────────────────────────────────────────────
 
    IEnumerator SpeakAndBubble(Image character, Sprite talkSprite, Sprite idleSprite,
                                string text, AudioClip audio, bool isGirl)
    {
        StartCoroutine(CharacterHop(character.transform));
        SetCharacterState(character, talkSprite);
 
        GameObject prefab = isGirl ? girlBubblePrefab : boyBubblePrefab;
        GameObject bubble = Instantiate(prefab, chatContent);
        _spawnedBubbles.Add(bubble);
 
        TextMeshProUGUI bubbleTMP = bubble.GetComponentInChildren<TextMeshProUGUI>();
        if (bubbleTMP != null) bubbleTMP.text = text;
 
        bubble.transform.localScale = Vector3.zero;
        if (bubblePopSFX != null) sfxSource.PlayOneShot(bubblePopSFX);
        yield return StartCoroutine(SpringScale(bubble.transform, 0.35f));
 
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
 
        if (bubbleTMP != null)
            StartCoroutine(TypewriterEffect(bubbleTMP, text));
 
        float audioDuration = 0f;
        if (audio != null)
        {
            dialogueAudioSource.clip = audio;
            dialogueAudioSource.Play();
            audioDuration = audio.length;
            StartCoroutine(PulseWhileSpeaking(character.transform, audioDuration));
        }
 
        yield return new WaitForSeconds(Mathf.Max(audioDuration, text.Length * 0.04f));
        SetCharacterState(character, idleSprite);
    }
 
    // ─────────────────────────────────────────────────────────────────────────
    // CHOICE CARDS
    // ─────────────────────────────────────────────────────────────────────────
 
    IEnumerator ShowChoiceCards(DialogueTurn turn)
    {
        _canChoose = false;
 
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceBGs[i].color = cardNormal;
            choiceTexts[i].text = turn.choices[i].text;
            choiceButtons[i].transform.localScale = Vector3.zero;
            choiceButtons[i].interactable = false;
        }
 
        choicePanel.SetActive(true);
        yield return StartCoroutine(SlidePanelUp(choicePanel.transform, 0.35f));
 
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (cardPopSFX != null) sfxSource.PlayOneShot(cardPopSFX);
            StartCoroutine(SpringScale(choiceButtons[i].transform, 0.3f));
            yield return new WaitForSeconds(0.18f);
        }
 
        foreach (var btn in choiceButtons) btn.interactable = true;
        _canChoose = true;
    }
 
    // ─────────────────────────────────────────────────────────────────────────
    // CHOICE SELECTED
    // ─────────────────────────────────────────────────────────────────────────
 
    void OnChoiceSelected(int index)
    {
        if (!_canChoose) return;
        _canChoose = false;
 
        foreach (var btn in choiceButtons) btn.interactable = false;
 
        bool isCorrect = turns[_turnIndex].choices[index].isCorrect;
 
        if (isCorrect)
            StartCoroutine(HandleCorrect(index));
        else
            StartCoroutine(HandleWrong(index));
    }
 
    IEnumerator HandleCorrect(int index)
    {
        sfxSource.PlayOneShot(correctSFX);
        yield return StartCoroutine(CorrectCardCelebrate(choiceButtons[index].transform, index));
 
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(SlidePanelDown(choicePanel.transform, 0.3f));
        choicePanel.SetActive(false);
        yield return new WaitForSeconds(0.2f);
 
        DialogueTurn turn = turns[_turnIndex];
        yield return StartCoroutine(
            SpeakAndBubble(boyCharacter, boyTalkSprite, boyIdleSprite,
                           turn.boyText, turn.boyAudio, isGirl: false));
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(
            SpeakAndBubble(girlCharacter, girlTalkSprite, girlIdleSprite,
                           turn.girlFollowUpText, turn.girlFollowUpAudio, isGirl: true));
        yield return new WaitForSeconds(0.5f);
 
        _turnIndex++;
        if (_turnIndex < turns.Count)
            StartCoroutine(PlayTurn(_turnIndex));
        else
            StartCoroutine(ShowCompletion());
    }
 
    IEnumerator HandleWrong(int index)
    {
        sfxSource.PlayOneShot(wrongSFX);
        choiceBGs[index].color = cardWrong;
        yield return StartCoroutine(ShakeCard(choiceButtons[index].transform));
        yield return StartCoroutine(ShrinkCard(choiceButtons[index].transform, 0.25f));
 
        int correctIdx = -1;
        for (int i = 0; i < turns[_turnIndex].choices.Length; i++)
            if (turns[_turnIndex].choices[i].isCorrect) { correctIdx = i; break; }
 
        if (correctIdx >= 0)
        {
            choiceBGs[correctIdx].color = cardCorrect;
            yield return StartCoroutine(CorrectCardCelebrate(choiceButtons[correctIdx].transform, correctIdx));
        }
 
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(SlidePanelDown(choicePanel.transform, 0.3f));
        choicePanel.SetActive(false);
        yield return new WaitForSeconds(0.2f);
 
        DialogueTurn turn = turns[_turnIndex];
        yield return StartCoroutine(
            SpeakAndBubble(boyCharacter, boyTalkSprite, boyIdleSprite,
                           turn.boyText, turn.boyAudio, isGirl: false));
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(
            SpeakAndBubble(girlCharacter, girlTalkSprite, girlIdleSprite,
                           turn.girlFollowUpText, turn.girlFollowUpAudio, isGirl: true));
        yield return new WaitForSeconds(0.5f);
 
        _turnIndex++;
        if (_turnIndex < turns.Count)
            StartCoroutine(PlayTurn(_turnIndex));
        else
            StartCoroutine(ShowCompletion());
    }
 
    // ─────────────────────────────────────────────────────────────────────────
    // COMPLETION
    // ─────────────────────────────────────────────────────────────────────────
 
    IEnumerator ShowCompletion()
    {
        if (completeSFX != null) sfxSource.PlayOneShot(completeSFX);
 
        completionPanel.SetActive(true);
        completionPanel.transform.localScale = Vector3.zero;
        yield return StartCoroutine(SpringScale(completionPanel.transform, 0.45f));
 
        if (wellDoneText != null)
            yield return StartCoroutine(BounceTextIn(wellDoneText.transform));
 
        StartCoroutine(CharacterHop(girlCharacter.transform, loops: 3));
        StartCoroutine(CharacterHop(boyCharacter.transform,  loops: 3, delay: 0.12f));
    }
 
    // ── CHANGED: Next now goes to IntroCard (Screen 2), not unitPanel ─────────
    void OnNextPressed()
    {
        completionPanel.SetActive(false);

        if (introCardPanel != null)
            introCardPanel.SetActive(true);
        else
            Debug.LogWarning("[RoleplayManager] introCardPanel is not assigned in the Inspector!");
    }

    /// <summary>
    /// Called at the very end of the full flow (after Speech game completes).
    /// Wire Logic_BB1's finishButton OnClick → this method in the Inspector.
    /// Disables the whole roleplay parent and shows the unit panel.
    /// </summary>
    public void GoToUnitPanel()
    {
        if (unitPanel      != null) unitPanel.SetActive(true);
        if (roleplayParent != null) roleplayParent.SetActive(false);
    }
    // ─────────────────────────────────────────────────────────────────────────
 
    // ─────────────────────────────────────────────────────────────────────────
    // ANIMATION COROUTINES  (unchanged)
    // ─────────────────────────────────────────────────────────────────────────
 
    IEnumerator SpringScale(Transform t, float duration)
    {
        t.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float p = elapsed / duration;
            float s = p < 0.6f
                ? Mathf.SmoothStep(0f, 1.25f, p / 0.6f)
                : Mathf.Lerp(1.25f, 1f, (p - 0.6f) / 0.4f);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
 
    IEnumerator CorrectCardCelebrate(Transform t, int bgIndex)
    {
        choiceBGs[bgIndex].color = cardCorrect;
        float elapsed = 0f, duration = 0.5f;
        while (elapsed < duration)
        {
            float p = elapsed / duration;
            float s = 1f + 0.35f * Mathf.Sin(p * Mathf.PI);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
 
    IEnumerator ShrinkCard(Transform t, float duration)
    {
        Vector3 startScale = t.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            t.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.zero;
        t.gameObject.SetActive(false);
    }
 
    IEnumerator ShakeCard(Transform t)
    {
        Vector3 origin = t.localPosition;
        float elapsed = 0f, duration = 0.45f, magnitude = 22f;
        while (elapsed < duration)
        {
            float x = Mathf.Sin(elapsed * Mathf.PI * 14f) * magnitude * (1f - elapsed / duration);
            t.localPosition = origin + new Vector3(x, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localPosition = origin;
    }

    void UpdateQuestionUI(DialogueTurn turn)
{
    // Entire panel optional
    if (questionPanel != null)
        questionPanel.SetActive(false);

    bool hasQuestionText =
        questionTextUI != null &&
        !string.IsNullOrEmpty(turn.questionText);

    bool hasQuestionImage =
        questionImageUI != null &&
        turn.questionSprite != null;

    // Update text
    if (questionTextUI != null)
    {
        questionTextUI.text = turn.questionText;

        questionTextUI.gameObject.SetActive(hasQuestionText);
    }

    // Update image
    if (questionImageUI != null)
    {
        if (turn.questionSprite != null)
        {
            questionImageUI.sprite = turn.questionSprite;
        }

        questionImageUI.gameObject.SetActive(hasQuestionImage);
    }

    // Enable panel only if something exists
    if (questionPanel != null)
    {
        questionPanel.SetActive(hasQuestionText || hasQuestionImage);
    }
}
 
    IEnumerator CharacterHop(Transform t, int loops = 1, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        Vector3 origin = t.localPosition;
        for (int loop = 0; loop < loops; loop++)
        {
            float elapsed = 0f, duration = 0.35f;
            while (elapsed < duration)
            {
                float p = elapsed / duration;
                float y = Mathf.Sin(p * Mathf.PI) * 22f;
                t.localPosition = origin + new Vector3(0f, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localPosition = origin;
            if (loop < loops - 1) yield return new WaitForSeconds(0.05f);
        }
    }
 
    IEnumerator PulseWhileSpeaking(Transform t, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float s = 1f + 0.06f * Mathf.Sin(elapsed * Mathf.PI * 3f);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
 
    IEnumerator SlidePanelUp(Transform t, float duration)
    {
        RectTransform rt = t as RectTransform;
        if (rt == null) yield break;
 
        Vector2 target = rt.anchoredPosition;
        Vector2 start  = target - new Vector2(0f, 220f);
        rt.anchoredPosition = start;
 
        CanvasGroup cg = t.GetComponent<CanvasGroup>();
        if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
 
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rt.anchoredPosition = Vector2.Lerp(start, target, p);
            cg.alpha = p;
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = target;
        cg.alpha = 1f;
    }
 
    IEnumerator SlidePanelDown(Transform t, float duration)
    {
        RectTransform rt = t as RectTransform;
        if (rt == null) yield break;
 
        CanvasGroup cg = t.GetComponent<CanvasGroup>();
        if (cg == null) cg = t.gameObject.AddComponent<CanvasGroup>();
 
        Vector2 start  = rt.anchoredPosition;
        Vector2 target = start - new Vector2(0f, 220f);
 
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float p = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            rt.anchoredPosition = Vector2.Lerp(start, target, p);
            cg.alpha = 1f - p;
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = start;
        cg.alpha = 1f;
    }
 
    IEnumerator TypewriterEffect(TextMeshProUGUI tmp, string fullText)
    {
        tmp.text = "";
        foreach (char c in fullText)
        {
            tmp.text += c;
            yield return new WaitForSeconds(0.03f);
        }
    }
 
    IEnumerator BounceTextIn(Transform t)
    {
        t.localScale = Vector3.zero;
        float elapsed = 0f, duration = 0.5f;
        while (elapsed < duration)
        {
            float p = elapsed / duration;
            float s = p < 0.65f
                ? Mathf.SmoothStep(0f, 1.4f, p / 0.65f)
                : Mathf.Lerp(1.4f, 1f, (p - 0.65f) / 0.35f);
            t.localScale = Vector3.one * s;
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
 
    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────
 
    void SetCharacterState(Image character, Sprite sprite)
    {
        if (character != null && sprite != null)
            character.sprite = sprite;
    }

    public void UnitFinished()
    {
        panel.UnitFinished(unitButton);
    }
}