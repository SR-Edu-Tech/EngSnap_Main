using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MatchTheFollowingGame_BB2 : MonoBehaviour
{
    [Header("Round Data")]
    public MatchingRoundData_BB2[] rounds = new MatchingRoundData_BB2[3];

    [Header("Prefabs")]
    public WordLabel_BB2        wordLabelPrefab;
    public IllustrationCard_BB2 illustrationCardPrefab;

    [Header("Layout Parents")]
    public Transform wordColumn;
    public Transform cardColumn;

    [Header("UI")]
    public TMP_Text      roundLabel;
    public LineDrawer_BB2 lineDrawer;
    public GameObject    roundCompletePanel;
    public TMP_Text      roundCompleteText;
    public Button         nextButton;
    public CanvasGroup   mainCanvasGroup;

    [Header("Progress")]
    public Image progressFill;

    [Header("Timing")]
    [SerializeField] private float roundCompleteDelay = 1.2f;

    /// Fired when ALL rounds are complete. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private int _currentRoundIndex = 0;
    private int _matchesThisRound  = 0;
    private WordLabel_BB2 _draggingWord;

    private readonly List<WordLabel_BB2>        _wordLabels = new();
    private readonly List<IllustrationCard_BB2> _cards      = new();

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        StopAllCoroutines();

        ClearSpawnedItems();
        lineDrawer?.ClearAll();

        _currentRoundIndex = 0;
        _matchesThisRound  = 0;
        _draggingWord      = null;

        roundCompletePanel?.SetActive(false);
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        if (lineDrawer == null)
        {
            lineDrawer = FindObjectOfType<LineDrawer_BB2>();
            if (lineDrawer == null) Debug.LogError("[MatchTheFollowingGame_BB2] LineDrawer_BB2 not found in scene!");
        }

        AudioManager.Instance?.PlayMusic(AudioManager.Instance.bgMusicMatching);

        StartCoroutine(FadeIn(() => LoadRound(0)));

        Debug.Log("[MatchTheFollowingGame_BB2] RestartGame — starting from round 0");
    }

    public void OnNextButtonPressed()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
        _currentRoundIndex++;
        if (_currentRoundIndex < rounds.Length)
            StartCoroutine(TransitionToNextRound());
        else
            OnPanelComplete();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Round loading
    // ════════════════════════════════════════════════════════════════════

    private void LoadRound(int index)
    {
        _matchesThisRound = 0;
        _draggingWord     = null;
        roundCompletePanel?.SetActive(false);
        nextButton?.gameObject.SetActive(false);
        if (roundLabel != null) roundLabel.text = rounds[index].roundName;
        UpdateProgress(0f);
        SpawnRound(rounds[index]);
    }

    private void SpawnRound(MatchingRoundData_BB2 data)
    {
        ClearSpawnedItems();
        lineDrawer?.ClearAll();

        int count = data.pairs.Length;
        for (int i = 0; i < count; i++)
        {
            var lbl = Instantiate(wordLabelPrefab, wordColumn);
            lbl.Initialise(i, data.pairs[i].wordLabel, data.pairs[i].wordAudioClip,
                OnDragBegin, OnDragging, OnDragEnd);
            _wordLabels.Add(lbl);
        }

        var shuffled = ShuffleIndices(count);
        foreach (int idx in shuffled)
        {
            var card = Instantiate(illustrationCardPrefab, cardColumn);
            card.Initialise(idx, data.pairs[idx].correctIllustrationSprite, OnCardDropped);
            _cards.Add(card);
            StartCoroutine(CardEntrance(card.GetComponent<RectTransform>(), _cards.Count * 0.06f));
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Drag handling
    // ════════════════════════════════════════════════════════════════════

    private void OnDragBegin(WordLabel_BB2 word)
    {
        _draggingWord = word;
        lineDrawer?.BeginDragLine(word.GetComponent<RectTransform>());
    }

    private void OnDragging(WordLabel_BB2 word, Vector2 screenPos) =>
        lineDrawer?.UpdateDragLine(screenPos);

    private void OnDragEnd(WordLabel_BB2 word, PointerEventData eventData)
    {
        lineDrawer?.EndDragLine();
        _draggingWord = null;
    }

    private void OnCardDropped(IllustrationCard_BB2 card)
    {
        if (_draggingWord == null)        return;
        if (card.IsMatched)               return;
        if (_draggingWord.IsMatched)      return;
        CheckMatch(_draggingWord, card);
    }

    private void CheckMatch(WordLabel_BB2 word, IllustrationCard_BB2 card)
    {
        bool correct = word.PairIndex == card.CorrectPairIndex;
        lineDrawer?.CommitLine(word.GetComponent<RectTransform>(), card.GetComponent<RectTransform>(), correct);

        if (correct)
        {
            word.SetMatched();
            card.SetMatched();
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);
            VFXManager.Instance?.SpawnCorrectBurst(card.GetComponent<RectTransform>());
            _matchesThisRound++;
            UpdateProgress((float)_matchesThisRound / rounds[_currentRoundIndex].pairs.Length);
            if (_matchesThisRound >= rounds[_currentRoundIndex].pairs.Length)
                StartCoroutine(DelayedRoundComplete(roundCompleteDelay));
        }
        else
        {
            word.SetWrong();
            card.SetWrong();
            AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);
            VFXManager.Instance?.ScreenShake(8f, 0.2f);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Round complete / panel complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator DelayedRoundComplete(float delay)
    {
        yield return new WaitForSeconds(delay);
        RoundComplete();
    }

    private void RoundComplete()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxRoundComplete);
        VFXManager.Instance?.SpawnConfetti();
        if (roundCompletePanel != null)
        {
            roundCompletePanel.SetActive(true);
            if (roundCompleteText != null)
                roundCompleteText.text = _currentRoundIndex < rounds.Length - 1
                    ? "Amazing! Keep going! 🎉" : "All done! 🎉";
        }
        nextButton?.gameObject.SetActive(true);
    }

    private void OnPanelComplete()
    {
        Debug.Log("[MatchTheFollowingGame_BB2] All rounds done — firing OnFinished");
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxGameComplete);

        StartCoroutine(FadeOut(() =>
        {
            gameObject.SetActive(false);
            OnFinished?.Invoke();
        }));
    }

    private IEnumerator TransitionToNextRound()
    {
        yield return FadeOut(null);
        ClearSpawnedItems();
        lineDrawer?.ClearAll();
        yield return FadeIn(() => LoadRound(_currentRoundIndex));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Progress / animation helpers
    // ════════════════════════════════════════════════════════════════════

    private void UpdateProgress(float t)
    {
        if (progressFill == null) return;
        StopCoroutine(nameof(AnimateProgress));
        StartCoroutine(AnimateProgress(t));
    }

    private IEnumerator AnimateProgress(float target)
    {
        float start = progressFill.fillAmount;
        float e = 0f, dur = 0.3f;
        while (e < dur) { e += Time.deltaTime; progressFill.fillAmount = Mathf.Lerp(start, target, e / dur); yield return null; }
        progressFill.fillAmount = target;
    }

    private IEnumerator CardEntrance(RectTransform rt, float delay)
    {
        yield return new WaitForSeconds(delay);
        Vector3 target = rt.localScale;
        rt.localScale  = Vector3.zero;
        float e = 0f, dur = 0.2f;
        while (e < dur) { e += Time.deltaTime; rt.localScale = Vector3.LerpUnclamped(Vector3.zero, target, EaseOutBack(e / dur)); yield return null; }
        rt.localScale = target;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private IEnumerator FadeIn(System.Action onDone)
    {
        if (mainCanvasGroup == null) { onDone?.Invoke(); yield break; }
        mainCanvasGroup.alpha = 0f;
        float t = 0f, dur = 0.35f;
        while (t < dur) { t += Time.deltaTime; mainCanvasGroup.alpha = t / dur; yield return null; }
        mainCanvasGroup.alpha = 1f;
        onDone?.Invoke();
    }

    private IEnumerator FadeOut(System.Action onDone)
    {
        if (mainCanvasGroup == null) { onDone?.Invoke(); yield break; }
        float t = 0f, dur = 0.25f;
        while (t < dur) { t += Time.deltaTime; mainCanvasGroup.alpha = 1f - t / dur; yield return null; }
        mainCanvasGroup.alpha = 0f;
        onDone?.Invoke();
    }

    private void ClearSpawnedItems()
    {
        foreach (var lbl  in _wordLabels) if (lbl)  Destroy(lbl.gameObject);
        foreach (var card in _cards)      if (card) Destroy(card.gameObject);
        _wordLabels.Clear();
        _cards.Clear();
    }

    private static List<int> ShuffleIndices(int count)
    {
        var list = new List<int>();
        for (int i = 0; i < count; i++) list.Add(i);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}
