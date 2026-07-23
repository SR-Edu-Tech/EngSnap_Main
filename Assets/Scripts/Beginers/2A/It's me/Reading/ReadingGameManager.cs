using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReadingGameManager : MonoBehaviour, IUnitCompletable
{
    [System.Serializable]
    public class Screen1TileData
    {
        public string sentenceText;
        public AudioClip audioClip;
        public Sprite defaultSprite;
        public Sprite actionSprite;
    }

    [System.Serializable]
    public class Screen2QuestionData
    {
        public string questionText;
        public AudioClip questionAudio;
        public Sprite kidSprite;
        public string[] optionReplies = new string[3];
        public int correctReplyIndex;
        public AudioClip correctReplyAudio;
    }

    [Header("Screen 1 Setup")]
    public GameObject screen1Container;
    public GameObject readingTilePrefab; // The Tile Prefab to instantiate dynamically
    public Transform gridContainer; // Grid Layout Group transform to parent tiles
    public List<Screen1TileData> screen1Data = new List<Screen1TileData>();

    [Header("Screen 1 UI")]
    public Image jasmineImage;
    public TextMeshProUGUI sentenceFrameText;
    public string sentenceFramePrefix = "About me: I ";
    public string sentenceFramePlaceholder = "____";
    public GameObject screen1NextButton;
    public Button screen1ReplayButton;

    [Header("Screen 2 Setup")]
    public GameObject screen2Container;
    public List<Screen2QuestionData> screen2Data = new List<Screen2QuestionData>();

    [Header("Screen 2 UI")]
    public TextMeshProUGUI questionTextUI;
    public Image questionKidImageUI;
    public Button[] replyButtons = new Button[3];
    public TextMeshProUGUI[] replyTexts = new TextMeshProUGUI[3];
    public Image[] replyBGs = new Image[3];
    public TextMeshProUGUI answeredCounterText;
    public GameObject screen2NextButton;
    public Button screen2ReplayButton;

    [Header("Instruction Audios")]
    public AudioClip jasmineIntroAudio; // "Let's learn all about you!"
    public AudioClip tapInstructionAudio; // "Tap any card to say it!"
    public AudioClip screen2IntroAudio; // "Listen to each friend. Tap the kind reply!"
    public AudioClip screen2InteractiveAudio; // "Now YOU reply! Tap the right one!"
    public AudioClip tryAgainAudio; // "Try again — pick the kind reply!"

    [Header("Feedback SFX")]
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip fanfareSFX;
    public AudioClip buttonTapSFX;

    [Header("Audio References")]
    public AudioSource voiceSource;
    public AudioSource sfxSource;

    [Header("Sliding Animation Setup")]
    public GameObject slidingTextPrefab; // Optional prefab with TextMeshProUGUI
    public Transform canvasTransform; // Parent canvas for sliding animation text

    // Private game flow references
    private SharedUnitPanelController _panel;
    private SharedUnitButton _button;

    // Cached once so repeated OnEnable/RestartGame calls never read a
    // mid-animation (shrunk) scale as if it were the "original" scale
    private Vector3 _jasmineOriginalScale;
    private bool _jasmineScaleCached = false;

    // Screen 1 states
    private int screen1TapsCount = 0;
    private bool screen1IsInteractive = false;
    private Coroutine screen1Coroutine;
    private List<ReadingTile> instantiatedTiles = new List<ReadingTile>();

    // Screen 2 states
    private int currentQuestionIndex = 0;
    private bool waitingForAnswer = false;
    private bool answerWasCorrect = false;   // set by OnReplyTapped, read inline
    private int  answeredIndex    = -1;      // which button was tapped
    private Coroutine screen2Coroutine;

    //═══════════════════════════════════════
    // UNITY LIFE CYCLE & IUNITCOMPLETABLE
    //═══════════════════════════════════════

    void Awake()
    {
        // Grab the character's authored scale exactly once, before any
        // OnEnable/RestartGame cycle has a chance to leave it mid-animation.
        if (jasmineImage != null && !_jasmineScaleCached)
        {
            _jasmineOriginalScale = jasmineImage.transform.localScale;
            _jasmineScaleCached = true;
        }
    }

    public void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button)
    {
        _panel = panel;
        _button = button;

        RestartGame();
    }

    void OnEnable()
    {
        // Safety check to ensure it starts clean whenever enabled
        RestartGame();
    }

    public void RestartGame()
    {
        StopAllCoroutinesAndEffects();

        if (voiceSource != null) voiceSource.Stop();
        if (sfxSource != null) sfxSource.Stop();

        StartScreen1();
    }

    private void StopAllCoroutinesAndEffects()
    {
        StopAllCoroutines();
    }

    //═══════════════════════════════════════
    // SCREEN 1 FLOW
    //═══════════════════════════════════════

    private void StartScreen1()
    {
        screen1Container.SetActive(true);
        screen2Container.SetActive(false);
        screen1NextButton.SetActive(false);
        screen1TapsCount = 0;
        screen1IsInteractive = false;

        // Reset sentence frame
        if (sentenceFrameText != null)
        {
            sentenceFrameText.text = sentenceFramePrefix + sentenceFramePlaceholder;
        }

        // Destroy previous tiles
        ClearInstantiatedTiles();

        // Instantiate new tiles from prefab based on screen1Data size
        for (int i = 0; i < screen1Data.Count; i++)
        {
            if (readingTilePrefab == null || gridContainer == null)
            {
                Debug.LogError("ReadingGameManager: readingTilePrefab or gridContainer is not assigned in the inspector!");
                break;
            }

            GameObject tileGO = Instantiate(readingTilePrefab, gridContainer);
            ReadingTile tile = tileGO.GetComponent<ReadingTile>();
            if (tile != null)
            {
                tile.SetData(screen1Data[i].sentenceText, screen1Data[i].defaultSprite);
                tile.button.interactable = false;

                int tileIndex = i;
                tile.button.onClick.RemoveAllListeners();
                tile.button.onClick.AddListener(() => OnTileTapped(tileIndex));

                instantiatedTiles.Add(tile);
            }
            else
            {
                Debug.LogError("ReadingGameManager: Instantiated prefab does not have a ReadingTile component attached!");
            }
        }

        // Replay button Setup
        if (screen1ReplayButton != null)
        {
            screen1ReplayButton.onClick.RemoveAllListeners();
            screen1ReplayButton.onClick.AddListener(() =>
            {
                PlaySFX(buttonTapSFX);
                RestartScreen1();
            });
        }

        // Next button Setup
        if (screen1NextButton != null)
        {
            var btn = screen1NextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    PlaySFX(buttonTapSFX);
                    StartScreen2();
                });
            }
        }

        screen1Coroutine = StartCoroutine(Screen1Flow());
    }

    private void ClearInstantiatedTiles()
    {
        foreach (var tile in instantiatedTiles)
        {
            if (tile != null)
            {
                Destroy(tile.gameObject);
            }
        }
        instantiatedTiles.Clear();

        // Safety fallback: destroy all child objects of the container
        if (gridContainer != null)
        {
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void RestartScreen1()
    {
        if (screen1Coroutine != null) StopCoroutine(screen1Coroutine);
        if (voiceSource != null) voiceSource.Stop();
        StartScreen1();
    }

    private IEnumerator Screen1Flow()
    {
        // 1. Jasmine waves from the right: 'Let's learn all about you!'
        if (jasmineImage != null)
        {
            StartCoroutine(JasmineWaveAnimation());
        }

        if (jasmineIntroAudio != null && voiceSource != null)
        {
            voiceSource.clip = jasmineIntroAudio;
            voiceSource.Play();
            yield return new WaitForSeconds(jasmineIntroAudio.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        // 2. Auto-play: tiles light up one by one in row order
        for (int i = 0; i < instantiatedTiles.Count; i++)
        {
            var tile = instantiatedTiles[i];
            if (tile == null) continue;
            var data = screen1Data[i];

            tile.SetHighlight(true);

            // Animate kid waving/wobbling
            if (tile.kidImage != null)
            {
                float duration = data.audioClip != null ? data.audioClip.length : 1.5f;
                StartCoroutine(WobbleTransform(tile.kidImage.transform, duration));
            }

            if (data.audioClip != null && voiceSource != null)
            {
                voiceSource.clip = data.audioClip;
                voiceSource.Play();
                yield return new WaitForSeconds(data.audioClip.length + 0.15f);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            tile.SetHighlight(false);
        }

        // 3. Auto-play finished: tiles become interactive
        screen1IsInteractive = true;

        for (int i = 0; i < instantiatedTiles.Count; i++)
        {
            if (instantiatedTiles[i] != null)
            {
                instantiatedTiles[i].button.interactable = true;
            }
        }

        // Audio: 'Tap any card to say it!'
        if (tapInstructionAudio != null && voiceSource != null)
        {
            voiceSource.clip = tapInstructionAudio;
            voiceSource.Play();
        }
    }

    private void OnTileTapped(int index)
    {
        if (!screen1IsInteractive) return;

        // Stop current playing audio
        if (voiceSource != null) voiceSource.Stop();

        var tile = instantiatedTiles[index];
        var data = screen1Data[index];

        // Animate kid acting (hugging pet, eating chocolate, etc.)
        if (data.actionSprite != null && tile.kidImage != null)
        {
            tile.kidImage.sprite = data.actionSprite;
        }

        // Squeeze & stretch bounce
        StartCoroutine(PulseTransform(tile.transform));

        // Click visual highlight
        tile.SetClickedVisual();

        // Play tile audio
        if (data.audioClip != null && voiceSource != null)
        {
            voiceSource.clip = data.audioClip;
            voiceSource.Play();
        }

        // Slide sentence text to the bottom frame
        StartCoroutine(SlideSentenceToFrame(tile, data.sentenceText));

        // Reset tile visual and sprite back to default after 1.5s
        StartCoroutine(ResetTileAfterDelay(tile, data.defaultSprite, 1.5f));

        screen1TapsCount++;

        // After 4 taps, show Next button
        if (screen1TapsCount >= 4)
        {
            if (screen1NextButton != null && !screen1NextButton.activeSelf)
            {
                screen1NextButton.SetActive(true);
                StartCoroutine(PulseTransform(screen1NextButton.transform));
            }
        }
    }

    private IEnumerator ResetTileAfterDelay(ReadingTile tile, Sprite defaultSprite, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (tile != null)
        {
            if (tile.kidImage != null) tile.kidImage.sprite = defaultSprite;
            tile.ResetVisuals();
        }
    }

    //═══════════════════════════════════════
    // SCREEN 2 FLOW
    //═══════════════════════════════════════

    private void StartScreen2()
    {
        screen1Container.SetActive(false);
        screen2Container.SetActive(true);
        screen2NextButton.SetActive(false);
        currentQuestionIndex = 0;
        waitingForAnswer = false;

        ResetReplyButtons();

        // Replay button setup
        if (screen2ReplayButton != null)
        {
            screen2ReplayButton.onClick.RemoveAllListeners();
            screen2ReplayButton.onClick.AddListener(() =>
            {
                PlaySFX(buttonTapSFX);
                RestartScreen2();
            });
        }

        // Next button setup
        if (screen2NextButton != null)
        {
            var btn = screen2NextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    PlaySFX(buttonTapSFX);
                    FinishGame();
                });
            }
        }

        screen2Coroutine = StartCoroutine(Screen2Flow());
    }

    private void RestartScreen2()
    {
        if (screen2Coroutine != null) StopCoroutine(screen2Coroutine);
        if (voiceSource != null) voiceSource.Stop();
        StartScreen2();
    }

    private IEnumerator Screen2Flow()
    {
        // 1. Audio: 'Listen to each friend. Tap the kind reply!'
        if (screen2IntroAudio != null && voiceSource != null)
        {
            voiceSource.clip = screen2IntroAudio;
            voiceSource.Play();
            yield return new WaitForSeconds(screen2IntroAudio.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        // 2. PHASE 1: Q1–Q5 auto-play with model reply
        int autoPlayCount = Mathf.Min(5, screen2Data.Count);
        for (int i = 0; i < autoPlayCount; i++)
        {
            yield return StartCoroutine(AutoPlayQuestion(i));
        }

        // 3. PHASE 2 starts. Audio: 'Now YOU reply! Tap the right one!'
        if (screen2InteractiveAudio != null && voiceSource != null)
        {
            voiceSource.clip = screen2InteractiveAudio;
            voiceSource.Play();
            yield return new WaitForSeconds(screen2InteractiveAudio.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        // Start Q1-Q10 interactive play — index is advanced INSIDE the coroutine
        currentQuestionIndex = 0;
        while (currentQuestionIndex < screen2Data.Count)
        {
            int beforeIndex = currentQuestionIndex;
            yield return StartCoroutine(InteractivePlayQuestion(currentQuestionIndex));
            // Safety: if the coroutine somehow didn't advance, force break to avoid infinite loop
            if (currentQuestionIndex == beforeIndex)
            {
                Debug.LogWarning("ReadingGameManager: question index did not advance — forcing exit.");
                break;
            }
        }

        // 4. Completed all questions
        PlaySFX(fanfareSFX);

        if (screen2NextButton != null)
        {
            screen2NextButton.SetActive(true);
            StartCoroutine(PulseTransform(screen2NextButton.transform));
        }
    }

    private IEnumerator AutoPlayQuestion(int qIndex)
    {
        var data = screen2Data[qIndex];

        SetupQuestionUI(data);
        UpdateAnsweredText(qIndex);
        ResetReplyBGs();
        SetReplyButtonsInteractable(false);

        // Highlight correct choice
        HighlightCorrectReply(data.correctReplyIndex);

        // Kid illustration waves
        if (questionKidImageUI != null)
        {
            StartCoroutine(WobbleTransform(questionKidImageUI.transform, 2.0f));
        }

        // Play question audio
        if (data.questionAudio != null && voiceSource != null)
        {
            voiceSource.clip = data.questionAudio;
            voiceSource.Play();
            yield return new WaitForSeconds(data.questionAudio.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // Play correct reply audio
        if (data.correctReplyAudio != null && voiceSource != null)
        {
            voiceSource.clip = data.correctReplyAudio;
            voiceSource.Play();
            yield return new WaitForSeconds(data.correctReplyAudio.length + 0.5f);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(0.4f);
    }

    private IEnumerator InteractivePlayQuestion(int qIndex)
    {
        var data = screen2Data[qIndex];

        SetupQuestionUI(data);
        UpdateAnsweredText(qIndex);
        ResetReplyBGs();
        SetReplyButtonsInteractable(true);

        // Bind reply options
        for (int i = 0; i < replyButtons.Length; i++)
        {
            if (replyButtons[i] == null) continue;
            int optionIndex = i;
            replyButtons[i].onClick.RemoveAllListeners();
            replyButtons[i].onClick.AddListener(() => OnReplyTapped(optionIndex));
        }

        // Play question audio
        if (data.questionAudio != null && voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = data.questionAudio;
            voiceSource.Play();
        }

        // Kid waves
        if (questionKidImageUI != null)
        {
            StartCoroutine(WobbleTransform(questionKidImageUI.transform, 1.5f));
        }

        // ── Wait for student to tap ──────────────────────────────────
        waitingForAnswer = true;
        answeredIndex    = -1;
        answerWasCorrect = false;

        while (waitingForAnswer)
        {
            yield return null;
        }

        // ── answeredIndex and answerWasCorrect are now set by OnReplyTapped ──
        SetReplyButtonsInteractable(false);

        if (answerWasCorrect)
        {
            // ── CORRECT: green flash, play reply audio fully, then advance ──
            if (answeredIndex >= 0 && answeredIndex < replyBGs.Length && replyBGs[answeredIndex] != null)
                replyBGs[answeredIndex].color = new Color(0.3f, 1f, 0.4f);

            PlaySFX(correctSFX);

            if (data.correctReplyAudio != null && voiceSource != null)
            {
                voiceSource.Stop();
                voiceSource.clip = data.correctReplyAudio;
                voiceSource.Play();
                // Wait for the full audio to finish before moving on
                yield return new WaitForSeconds(data.correctReplyAudio.length + 0.3f);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            currentQuestionIndex++;          // advance ONLY after audio done
            UpdateAnsweredText(currentQuestionIndex);
        }
        else
        {
            // Wrong is handled inside a loop — wait for correct answer
            // Red flash + shake already done. Try again loop:
            bool gotItRight = false;
            while (!gotItRight)
            {
                // Re-enable buttons for retry
                SetReplyButtonsInteractable(true);

                waitingForAnswer = true;
                answeredIndex    = -1;
                answerWasCorrect = false;

                // Rebind with same data
                for (int i = 0; i < replyButtons.Length; i++)
                {
                    if (replyButtons[i] == null) continue;
                    int optionIndex = i;
                    replyButtons[i].onClick.RemoveAllListeners();
                    replyButtons[i].onClick.AddListener(() => OnReplyTapped(optionIndex));
                }

                while (waitingForAnswer)
                {
                    yield return null;
                }

                SetReplyButtonsInteractable(false);

                if (answerWasCorrect)
                {
                    if (answeredIndex >= 0 && answeredIndex < replyBGs.Length && replyBGs[answeredIndex] != null)
                        replyBGs[answeredIndex].color = new Color(0.3f, 1f, 0.4f);

                    PlaySFX(correctSFX);

                    if (data.correctReplyAudio != null && voiceSource != null)
                    {
                        voiceSource.Stop();
                        voiceSource.clip = data.correctReplyAudio;
                        voiceSource.Play();
                        yield return new WaitForSeconds(data.correctReplyAudio.length + 0.3f);
                    }
                    else
                    {
                        yield return new WaitForSeconds(1.0f);
                    }

                    currentQuestionIndex++;
                    UpdateAnsweredText(currentQuestionIndex);
                    gotItRight = true;
                }
                else
                {
                    // Wrong again — shake + try-again audio then loop
                    if (answeredIndex >= 0 && answeredIndex < replyBGs.Length && replyBGs[answeredIndex] != null)
                        replyBGs[answeredIndex].color = new Color(1f, 0.3f, 0.3f);

                    PlaySFX(wrongSFX);

                    if (answeredIndex >= 0 && answeredIndex < replyButtons.Length && replyButtons[answeredIndex] != null)
                        yield return StartCoroutine(ShakeButton(replyButtons[answeredIndex].transform));

                    if (tryAgainAudio != null && voiceSource != null)
                    {
                        voiceSource.Stop();
                        voiceSource.clip = tryAgainAudio;
                        voiceSource.Play();
                        yield return new WaitForSeconds(tryAgainAudio.length + 0.2f);
                    }
                    else
                    {
                        yield return new WaitForSeconds(1.0f);
                    }

                    // Reset wrong tile color before retry
                    if (answeredIndex >= 0 && answeredIndex < replyBGs.Length && replyBGs[answeredIndex] != null)
                        replyBGs[answeredIndex].color = Color.white;
                }
            }
        }
    }

    /// <summary>
    /// Called by button listeners. Sets flags so the inline coroutine can proceed.
    /// currentData is captured by closure in the button binding above.
    /// </summary>
    private void OnReplyTapped(int index)
    {
        if (!waitingForAnswer) return;

        // Capture which button was pressed
        answeredIndex = index;

        // Determine correctness using the current question data
        if (currentQuestionIndex < screen2Data.Count)
        {
            answerWasCorrect = (index == screen2Data[currentQuestionIndex].correctReplyIndex);
        }
        else
        {
            answerWasCorrect = false;
        }

        // If wrong: show shake + try-again audio immediately so player sees feedback,
        // but do NOT release waitingForAnswer here — that happens inside the inline loop.
        if (!answerWasCorrect)
        {
            // Visual red flash
            if (index < replyBGs.Length && replyBGs[index] != null)
                replyBGs[index].color = new Color(1f, 0.3f, 0.3f);

            PlaySFX(wrongSFX);

            // Shake + tryAgain voice run as a separate short coroutine
            // They DON'T advance the question — the inline loop handles retry.
            StartCoroutine(WrongFeedbackThenRelease(index));
        }
        else
        {
            // Correct: release the wait immediately; audio is handled inline.
            waitingForAnswer = false;
        }
    }

    /// <summary>
    /// Plays shake + try-again audio, then sets waitingForAnswer = false
    /// so the inline retry loop in InteractivePlayQuestion can proceed.
    /// </summary>
    private IEnumerator WrongFeedbackThenRelease(int index)
    {
        SetReplyButtonsInteractable(false);

        if (index < replyButtons.Length && replyButtons[index] != null)
            yield return StartCoroutine(ShakeButton(replyButtons[index].transform));

        if (tryAgainAudio != null && voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = tryAgainAudio;
            voiceSource.Play();
            yield return new WaitForSeconds(tryAgainAudio.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // Reset red color
        if (index < replyBGs.Length && replyBGs[index] != null)
            replyBGs[index].color = Color.white;

        // Release: inline retry loop will re-enable buttons
        waitingForAnswer = false;
    }

    // CorrectReplySequence and WrongReplySequence are replaced.
    // All feedback is now handled inline inside InteractivePlayQuestion.
    // See WrongFeedbackThenRelease for the wrong-answer short animation.

    //═══════════════════════════════════════
    // SCREEN 2 HELPER METHODS
    //═══════════════════════════════════════

    private void SetupQuestionUI(Screen2QuestionData data)
    {
        if (questionTextUI != null) questionTextUI.text = data.questionText;

        if (questionKidImageUI != null)
        {
            questionKidImageUI.gameObject.SetActive(data.kidSprite != null);
            questionKidImageUI.sprite = data.kidSprite;
        }

        for (int i = 0; i < replyButtons.Length; i++)
        {
            if (replyButtons[i] == null) continue;

            if (i < data.optionReplies.Length)
            {
                replyButtons[i].gameObject.SetActive(true);
                if (replyTexts[i] != null) replyTexts[i].text = data.optionReplies[i];
            }
            else
            {
                replyButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void UpdateAnsweredText(int count)
    {
        if (answeredCounterText != null)
        {
            answeredCounterText.text = "Q" +"" + count  ;
        }
    }

    private void SetReplyButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < replyButtons.Length; i++)
        {
            if (replyButtons[i] != null) replyButtons[i].interactable = interactable;
        }
    }

    private void HighlightCorrectReply(int correctIndex)
    {
        for (int i = 0; i < replyBGs.Length; i++)
        {
            if (replyBGs[i] == null) continue;
            replyBGs[i].color = (i == correctIndex) ? new Color(0.8f, 1f, 0.8f) : Color.white; // soft green highlight
        }
    }

    private void ResetReplyBGs()
    {
        for (int i = 0; i < replyBGs.Length; i++)
        {
            if (replyBGs[i] != null) replyBGs[i].color = Color.white;
        }
    }

    private void ResetReplyButtons()
    {
        ResetReplyBGs();
        SetReplyButtonsInteractable(false);
    }

    //═══════════════════════════════════════
    // ANIMATIONS (COROUTINES)
    //═══════════════════════════════════════

    private IEnumerator JasmineWaveAnimation()
    {
        if (jasmineImage == null) yield break;

        Transform t = jasmineImage.transform;

        // Safety net in case Awake didn't run first for some reason
        if (!_jasmineScaleCached)
        {
            _jasmineOriginalScale = t.localScale;
            _jasmineScaleCached = true;
        }

        Vector3 origScale = _jasmineOriginalScale;
        t.localScale = Vector3.zero; // start invisible, stays at its placed position

        // Pop in with a slight overshoot
        float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);

            // Ease out back (overshoot then settle)
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            float ease = 1f + c3 * Mathf.Pow(p - 1f, 3f) + c1 * Mathf.Pow(p - 1f, 2f);

            t.localScale = origScale * ease;
            yield return null;
        }
        t.localScale = origScale;

        // Wave wobble
        float waveTime = 1.6f;
        elapsed = 0f;
        while (elapsed < waveTime)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * 16f) * 9f;
            t.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        t.localRotation = Quaternion.identity;
    }

    private IEnumerator WobbleTransform(Transform target, float duration)
    {
        if (target == null) yield break;

        Vector3 origScale = target.localScale;
        Quaternion origRot = target.localRotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;

            float scaleMod = 1f + Mathf.Abs(Mathf.Sin(elapsed * 10f)) * 0.08f;
            target.localScale = origScale * scaleMod;

            float rotAngle = Mathf.Sin(elapsed * 12f) * 6f;
            target.localRotation = Quaternion.Euler(0f, 0f, rotAngle);

            yield return null;
        }

        if (target != null)
        {
            target.localScale = origScale;
            target.localRotation = origRot;
        }
    }

    private IEnumerator PulseTransform(Transform target)
    {
        if (target == null) yield break;

        Vector3 origScale = target.localScale;
        float elapsed = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float p = elapsed / duration;

            float scaleY = 1f + Mathf.Sin(p * Mathf.PI) * 0.15f;
            float scaleX = 1f - Mathf.Sin(p * Mathf.PI) * 0.08f;
            target.localScale = new Vector3(origScale.x * scaleX, origScale.y * scaleY, origScale.z);
            yield return null;
        }

        if (target != null)
        {
            target.localScale = origScale;
        }
    }

    private IEnumerator ShakeButton(Transform t)
    {
        if (t == null) yield break;

        Vector3 original = t.localPosition;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            if (t == null) yield break;
            elapsed += Time.deltaTime;
            float x = Mathf.Sin(elapsed * 50f) * 12f;
            t.localPosition = original + new Vector3(x, 0f, 0f);
            yield return null;
        }

        if (t != null)
        {
            t.localPosition = original;
        }
    }

    private IEnumerator SlideSentenceToFrame(ReadingTile tile, string sentence)
    {
        if (sentenceFrameText == null) yield break;

        Vector3 startPos = tile.sentenceText.transform.position;
        Vector3 targetPos = sentenceFrameText.transform.position;

        GameObject tempGO = CreateTempSlidingText(sentence, startPos);
        TextMeshProUGUI tempText = tempGO.GetComponentInChildren<TextMeshProUGUI>();

        float duration = 0.45f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (tempGO == null) yield break;
            elapsed += Time.deltaTime;
            float p = elapsed / duration;

            // Ease out cubic
            float tVal = p - 1f;
            float ease = tVal * tVal * tVal + 1f;

            tempGO.transform.position = Vector3.Lerp(startPos, targetPos, ease);

            if (tempText != null)
            {
                Color c = tempText.color;
                c.a = Mathf.Lerp(1f, 0.1f, p);
                tempText.color = c;
            }

            yield return null;
        }

        // Update main text
        sentenceFrameText.text = sentenceFramePrefix + sentence;

        if (tempGO != null)
        {
            Destroy(tempGO);
        }

        // Pulse the sentence frame text on update
        StartCoroutine(PulseTransform(sentenceFrameText.transform));
    }

    private GameObject CreateTempSlidingText(string text, Vector3 startPos)
    {
        if (slidingTextPrefab != null)
        {
            GameObject temp = Instantiate(slidingTextPrefab, canvasTransform);
            temp.transform.position = startPos;
            var tmp = temp.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
            return temp;
        }
        else
        {
            // Dynamic fallback
            GameObject go = new GameObject("TempSlidingText", typeof(RectTransform));
            if (canvasTransform != null)
            {
                go.transform.SetParent(canvasTransform, false);
            }
            else
            {
                go.transform.SetParent(sentenceFrameText.transform.parent, false);
            }

            go.transform.position = startPos;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = sentenceFrameText.fontSize;
            tmp.color = sentenceFrameText.color;
            tmp.font = sentenceFrameText.font;
            tmp.alignment = TextAlignmentOptions.Center;

            return go;
        }
    }

    //═══════════════════════════════════════
    // HELPERS
    //═══════════════════════════════════════

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void FinishGame()
    {
        StopAllCoroutinesAndEffects();
        if (voiceSource != null) voiceSource.Stop();
        if (sfxSource != null) sfxSource.Stop();

        if (_panel != null && _button != null)
        {
            _panel.UnitFinished(_button);
        }
    }
}