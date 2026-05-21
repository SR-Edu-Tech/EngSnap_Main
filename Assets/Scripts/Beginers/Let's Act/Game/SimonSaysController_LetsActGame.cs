using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimonSaysController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Round Data (assign 6)")]
    public SimonRoundData[] rounds = new SimonRoundData[6];

    [Header("Prefabs")]
    public SimonAnswerCard answerCardPrefab;

    [Header("References")]
    public SimonCharacterUI simonCharacter;
    public Transform        answerGrid;
    public TMP_Text         roundLabel;
    public TMP_Text         scoreLabel;
    public TimerBar         timerBar;
    public CanvasGroup      mainCanvasGroup;

    [Header("Feedback Banner")]
    public GameObject feedbackBanner;
    public TMP_Text   feedbackText;

    [Header("Buttons")]
    public JuicyButton nextRoundButton;

    [Header("Game Complete")]
    public GameObject gameCompletePanel;
    public TMP_Text   completeText;

    [Header("Timing")]
    [SerializeField] private float cardSpawnDelay      = 0.12f;
    [SerializeField] private float postAnswerDelay     = 1.4f;
    [SerializeField] private float feedbackBannerDelay = 0.6f;

    [Header("Feedback Messages")]
    [SerializeField] private string[] correctMessages = {
        "Amazing! 🌟", "Super Star! ⭐", "Woohoo! 🎉", "Brilliant! 🏆",
        "You got it! 🙌", "Fantastic! 🎊"
    };
    [SerializeField] private string[] wrongMessages = {
        "Oops! Try again! 😊", "Almost! Keep going! 💪",
        "Nice try! Let's keep going! 🌈"
    };

    // ── Callback — set this before calling StartGame() ────────────────────────
    /// Fired when all rounds are complete. Wire your next-screen logic here.
    [HideInInspector] public Action OnFinished;

    // ── Runtime ───────────────────────────────────────────────────────────────

    private int  _currentRoundIndex = 0;
    private int  _correctCount      = 0;
    private int  _wrongCount        = 0;
    private bool _waitingForTap     = false;
    private bool _speedRoundActive  = false;

    private readonly List<SimonAnswerCard> _spawnedCards = new();

    // ── Entry Point ───────────────────────────────────────────────────────────

    /// Called by MatchingGameController. GameObject must be ACTIVE before calling.
    public void StartGame()
    {
        Debug.Log("[SimonSaysController] StartGame called");

        // Validate critical references
        if (rounds == null || rounds.Length == 0)
            Debug.LogError("[SimonSaysController] No rounds assigned!");
        if (answerCardPrefab == null)
            Debug.LogError("[SimonSaysController] answerCardPrefab not assigned!");
        if (answerGrid == null)
            Debug.LogError("[SimonSaysController] answerGrid not assigned!");
        if (simonCharacter == null)
            Debug.LogWarning("[SimonSaysController] simonCharacter not assigned — command display will be skipped.");

        _currentRoundIndex = 0;
        _correctCount      = 0;
        _wrongCount        = 0;

        AudioManager.Instance?.PlayMusic(AudioManager.Instance.bgMusicSimon);
        gameCompletePanel?.SetActive(false);
        feedbackBanner?.SetActive(false);
        nextRoundButton?.gameObject.SetActive(false);
        UpdateScoreLabel();

        StartCoroutine(FadeIn(() => StartCoroutine(ShowRound(0))));
    }

    // ── Next Round Button ─────────────────────────────────────────────────────

    public void OnNextRoundPressed()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
        _currentRoundIndex++;
        nextRoundButton?.gameObject.SetActive(false);
        feedbackBanner?.SetActive(false);

        if (_currentRoundIndex < rounds.Length)
            StartCoroutine(TransitionToNextRound());
        else
            StartCoroutine(ShowGameComplete());
    }

    // ── Round Flow ────────────────────────────────────────────────────────────

    private IEnumerator ShowRound(int index)
    {
        Debug.Log($"[SimonSaysController] ShowRound {index}");

        if (index >= rounds.Length)
        {
            Debug.LogError($"[SimonSaysController] Round index {index} out of range (length {rounds.Length})");
            yield break;
        }

        SimonRoundData data = rounds[index];
        _waitingForTap = false;

        if (roundLabel != null)
            roundLabel.text = $"Round {index + 1} of {rounds.Length}";

        // Show Simon's command — SAFE: skip the WaitUntil if simonCharacter is null
        if (simonCharacter != null)
        {
            bool commandDone = false;
            simonCharacter.ShowCommand(data.commandText, data.voCommandIndex, () => commandDone = true);
            // Safety timeout: don't wait more than 10 seconds
            float timeout = 0f;
            while (!commandDone && timeout < 10f)
            {
                timeout += Time.deltaTime;
                yield return null;
            }
            if (timeout >= 10f)
                Debug.LogWarning("[SimonSaysController] ShowCommand timed out — simonCharacter may have missing refs.");
        }
        else
        {
            // No character UI — just show the command text directly if possible
            Debug.LogWarning("[SimonSaysController] simonCharacter is null, skipping command animation.");
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(0.3f);

        SpawnCards(data);
        Debug.Log($"[SimonSaysController] Spawned {_spawnedCards.Count} cards");

        yield return new WaitForSeconds(cardSpawnDelay * 4f + 0.15f);

        if (data.isSpeedRound)
        {
            _speedRoundActive = true;
            timerBar?.StartTimer(data.speedRoundTime, OnTimerExpired);
        }

        _waitingForTap = true;
        Debug.Log("[SimonSaysController] Waiting for tap");
    }

    private void SpawnCards(SimonRoundData data)
    {
        foreach (var c in _spawnedCards) if (c) Destroy(c.gameObject);
        _spawnedCards.Clear();

        if (answerCardPrefab == null) { Debug.LogError("[SimonSaysController] answerCardPrefab is null!"); return; }
        if (answerGrid == null)       { Debug.LogError("[SimonSaysController] answerGrid is null!"); return; }

        var entries = new List<(string word, Sprite sprite, bool isCorrect)>
        {
            (data.correctActionWord, data.correctSprite, true)
        };

        int decoyCount = data.decoyWords != null ? Mathf.Min(data.decoyWords.Length, 3) : 0;
        for (int i = 0; i < decoyCount; i++)
            entries.Add((data.decoyWords[i], data.decoySprites != null && i < data.decoySprites.Length ? data.decoySprites[i] : null, false));

        // Fill to 4 if short on decoys
        while (entries.Count < 4)
            entries.Add(("?", null, false));

        for (int i = entries.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (entries[i], entries[j]) = (entries[j], entries[i]);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var card = Instantiate(answerCardPrefab, answerGrid);
            card.gameObject.SetActive(true);  
            var (word, sprite, isCorrect) = entries[i];
            card.Initialise(word ?? "?", sprite, isCorrect, OnCardTapped);
            card.PlayEntrance(i * cardSpawnDelay);
            _spawnedCards.Add(card);
        }
    }

    // ── Answer Handling ───────────────────────────────────────────────────────

    private void OnCardTapped(SimonAnswerCard tapped)
    {
        if (!_waitingForTap) return;
        _waitingForTap = false;

        if (_speedRoundActive) { timerBar?.StopTimer(); _speedRoundActive = false; }

        foreach (var c in _spawnedCards) c.SetInteractable(false);
        simonCharacter?.SetVOPlaying(false);

        if (tapped.IsCorrect)
        {
            _correctCount++;
            tapped.ShowCorrect();
            ShowFeedback(true);
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);
            VFXManager.Instance?.SpawnConfetti();
        }
        else
        {
            _wrongCount++;
            tapped.ShowWrong();
            StartCoroutine(HighlightCorrectAfterDelay(0.6f));
            ShowFeedback(false);
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);
            VFXManager.Instance?.ScreenShake(10f, 0.25f);
        }

        UpdateScoreLabel();
        StartCoroutine(ShowNextButtonAfterDelay(postAnswerDelay));
    }

    private void OnTimerExpired()
    {
        if (!_waitingForTap) return;
        _waitingForTap = false;
        _speedRoundActive = false;
        _wrongCount++;

        foreach (var c in _spawnedCards) c.SetInteractable(false);
        StartCoroutine(HighlightCorrectAfterDelay(0.1f));
        ShowFeedback(false);
        UpdateScoreLabel();
        StartCoroutine(ShowNextButtonAfterDelay(postAnswerDelay));
    }

    private IEnumerator HighlightCorrectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var c in _spawnedCards)
            if (c.IsCorrect) c.HighlightAsAnswer();
    }

    private IEnumerator ShowNextButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        nextRoundButton?.gameObject.SetActive(true);
    }

    // ── Feedback Banner ───────────────────────────────────────────────────────

    private void ShowFeedback(bool correct)
    {
        if (feedbackBanner == null) return;
        StartCoroutine(FeedbackSequence(correct));
    }

    private IEnumerator FeedbackSequence(bool correct)
    {
        yield return new WaitForSeconds(feedbackBannerDelay);
        feedbackBanner.SetActive(true);
        if (feedbackText != null)
            feedbackText.text = correct
                ? correctMessages[UnityEngine.Random.Range(0, correctMessages.Length)]
                : wrongMessages[UnityEngine.Random.Range(0, wrongMessages.Length)];

        var rt = feedbackBanner.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.zero;
            float t = 0f, dur = 0.2f;
            while (t < dur) { t += Time.deltaTime; rt.localScale = Vector3.one * EaseOutBack(t / dur); yield return null; }
            rt.localScale = Vector3.one;
        }
    }

    // ── Game Complete ─────────────────────────────────────────────────────────

private IEnumerator ShowGameComplete()
{
    Debug.Log("[SimonSaysController] Game complete!");
    yield return FadeOut(null);

    gameCompletePanel?.SetActive(true);
    if (completeText != null)
        completeText.text = _correctCount == rounds.Length
            ? "PERFECT SCORE! You're amazing! 🎉"
            : $"Well done! {_correctCount}/{rounds.Length} correct! 🎊";

    AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxGameComplete);
    VFXManager.Instance?.SpawnConfetti();

    yield return new WaitForSeconds(2f);

    OnFinished?.Invoke();

    // ✅ Return to unit panel and mark complete
    GameManager.Instance?.OnUnitComplete();
}
    // ── Transitions ───────────────────────────────────────────────────────────

    private IEnumerator TransitionToNextRound()
    {
        yield return new WaitForSeconds(0.1f);
        yield return ShowRound(_currentRoundIndex);
    }

    // ── UI Helpers ────────────────────────────────────────────────────────────

    private void UpdateScoreLabel()
    {
        if (scoreLabel != null) scoreLabel.text = $"✓ {_correctCount}";
    }

    private IEnumerator FadeIn(Action onDone)
    {
        if (mainCanvasGroup == null) { onDone?.Invoke(); yield break; }
        mainCanvasGroup.alpha = 0f;
        float t = 0f, dur = 0.4f;
        while (t < dur) { t += Time.deltaTime; mainCanvasGroup.alpha = t / dur; yield return null; }
        mainCanvasGroup.alpha = 1f;
        onDone?.Invoke();
    }

    private IEnumerator FadeOut(Action onDone)
    {
        if (mainCanvasGroup == null) { onDone?.Invoke(); yield break; }
        float t = 0f, dur = 0.3f;
        while (t < dur) { t += Time.deltaTime; mainCanvasGroup.alpha = 1f - t / dur; yield return null; }
        mainCanvasGroup.alpha = 0f;
        onDone?.Invoke();
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// Called by GameManager before hiding Screen 2.
/// Clears any leftover UI so next session starts clean.
public void ResetPanel()
{
    // Stop any running coroutines (timers, animations, etc.)
    StopAllCoroutines();

    // Hide stale UI
    gameCompletePanel?.SetActive(false);
    feedbackBanner?.SetActive(false);
    nextRoundButton?.gameObject.SetActive(false);
    timerBar?.StopTimer();

    // Destroy leftover answer cards
    foreach (var c in _spawnedCards) if (c) Destroy(c.gameObject);
    _spawnedCards.Clear();

    // Reset counters so StartGame() gets a clean slate
    _currentRoundIndex = 0;
    _correctCount      = 0;
    _wrongCount        = 0;
    _waitingForTap     = false;
    _speedRoundActive  = false;

    // Reset canvas alpha
    if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

    Debug.Log("[SimonSaysController] ResetPanel complete");
}
}