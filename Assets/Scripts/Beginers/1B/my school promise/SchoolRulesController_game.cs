using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// SchoolRulesController_game  —  Screen 2
/// ─────────────────────────────────────────────────────────────────────────
/// Shows a situation scene + 3 rule buttons. 6 rounds total.
/// When all 6 rounds are done calls manager.OnScreen2Complete().
///
/// HIERARCHY EXAMPLE:
///   Screen2_SchoolRules
///     ├─ SituationImage          ← Image
///     ├─ RuleButtonsRow
///     │    ├─ RuleButton_0       ← RuleButton_game
///     │    ├─ RuleButton_1       ← RuleButton_game
///     │    └─ RuleButton_2       ← RuleButton_game
///     └─ RoundLabel              ← TMP_Text (optional)
/// </summary>
public class SchoolRulesController_game : MonoBehaviour
{
    // ── Data structs ─────────────────────────────────────────────────────

    [System.Serializable]
    public class RuleData
    {
        [Tooltip("Sprite shown on the rule button")]
        public Sprite    ruleSprite;
        [Tooltip("Text shown on the rule button, e.g. 'Be quiet.'")]
        public string    ruleText;
        [Tooltip("VO clip that reads this rule aloud when correctly matched")]
        public AudioClip ruleVoClip;
    }

    [System.Serializable]
    public class RoundData
    {
        [Tooltip("Situation BEFORE the rule is applied")]
        public Sprite situationSprite;
        [Tooltip("Situation AFTER the correct rule is applied")]
        public Sprite resolvedSprite;
        [Tooltip("Index into the rules[] array that is the CORRECT answer")]
        public int    correctRuleIndex;
    }

    // ── Inspector ────────────────────────────────────────────────────────

    [Header("Scene References")]
    [SerializeField] private Image             situationImage;
    [SerializeField] private RuleButton_game[] ruleButtons;       // exactly 3
    [SerializeField] private TMP_Text          roundLabel;

    [Header("Rules (6 total — shared across all rounds)")]
    [SerializeField] private RuleData[] rules = new RuleData[6];

    [Header("Rounds (6 total)")]
    [SerializeField] private RoundData[] rounds = new RoundData[6];

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   sfxCorrect;
    [SerializeField] private AudioClip   sfxWrong;
    [SerializeField] private AudioClip   notThisOneClip;

    [Header("Timing")]
    [SerializeField] private float afterVoDelay        = 0.5f;
    [SerializeField] private float situationPopDuration = 0.35f;
    [SerializeField] private float buttonEntranceDelay  = 0.08f;  // stagger between buttons

    // ── Runtime ──────────────────────────────────────────────────────────

    private GameManager_SchoolRules_game _manager;
    private int  _currentRound = 0;
    private bool _inputLocked  = false;

    // ════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame(GameManager_SchoolRules_game manager)
    {
        _manager      = manager;
        _currentRound = 0;               // ← always reset to 0
        _inputLocked  = false;

        StopAllCoroutines();
        StartCoroutine(LoadRoundAnimated(_currentRound));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Round loading
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator LoadRoundAnimated(int index)
    {
        _inputLocked = false;

        // ── All rounds done? ─────────────────────────────────────────────
        if (index >= rounds.Length)
        {
            // Tell the GameManager Screen 2 is done → goes to celebration
            _manager?.OnScreen2Complete();
            yield break;
        }

        RoundData round = rounds[index];

        // Update round label
        if (roundLabel != null)
            roundLabel.text = $"Round {index + 1}";

        // ── Pop situation image in ───────────────────────────────────────
        if (situationImage != null && round.situationSprite != null)
        {
            situationImage.sprite = round.situationSprite;
            yield return StartCoroutine(PopIn(situationImage.transform, situationPopDuration));
        }

        // ── Pick & shuffle buttons ───────────────────────────────────────
        List<int> wrongIndices    = PickWrongRules(round.correctRuleIndex, 2);
        List<int> buttonAssignment = new List<int>(wrongIndices) { round.correctRuleIndex };
        Shuffle(buttonAssignment);

        // ── Stagger button entrances ─────────────────────────────────────
        for (int i = 0; i < ruleButtons.Length; i++)
        {
            if (ruleButtons[i] == null) continue;

            int      ruleIdx  = buttonAssignment[i];
            bool     correct  = ruleIdx == round.correctRuleIndex;
            RuleData ruleData = rules[ruleIdx];

            ruleButtons[i].Setup(
                sprite:    ruleData.ruleSprite,
                labelText: ruleData.ruleText,
                isCorrect: correct,
                onTapped:  OnRuleButtonTapped
            );

            // Pop each button in with a small stagger
            StartCoroutine(PopIn(ruleButtons[i].transform, situationPopDuration, i * buttonEntranceDelay));
        }

        // Wait for all button entrances before allowing input
        yield return new WaitForSeconds(situationPopDuration + ruleButtons.Length * buttonEntranceDelay);
        _inputLocked = false;
    }

    // ════════════════════════════════════════════════════════════════════
    //  Button callback
    // ════════════════════════════════════════════════════════════════════

    private void OnRuleButtonTapped(RuleButton_game tappedButton, bool isCorrect)
    {
        if (_inputLocked) return;

        if (isCorrect)
        {
            _inputLocked = true;
            LockAllButtons();
            PlaySFX(sfxCorrect);
            StartCoroutine(CorrectSequence(tappedButton));
        }
        else
        {
            tappedButton.PlayWrongAnim();
            PlaySFX(sfxWrong);
            PlayVO(notThisOneClip);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Correct sequence
    //  1. Correct button pops + tints green
    //  2. Rule VO plays
    //  3. Wait for VO
    //  4. Situation image swaps to resolved (with pop)
    //  5. Short pause → next round
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator CorrectSequence(RuleButton_game correctButton)
    {
        correctButton.PlayCorrectAnim();

        RoundData round  = rounds[_currentRound];
        AudioClip voClip = rules[round.correctRuleIndex].ruleVoClip;

        PlayVO(voClip);

        if (voClip != null)
            yield return new WaitForSeconds(voClip.length);

        // Swap + pop resolved sprite
        if (situationImage != null && round.resolvedSprite != null)
        {
            situationImage.sprite = round.resolvedSprite;
            yield return StartCoroutine(PopIn(situationImage.transform, situationPopDuration));
        }

        yield return new WaitForSeconds(afterVoDelay);

        _currentRound++;
        StartCoroutine(LoadRoundAnimated(_currentRound));
    }

    // ════════════════════════════════════════════════════════════════════
    //  Pop-in animation  (scale 0 → overshoot → 1)
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator PopIn(Transform t, float duration, float delay = 0f)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            t.localScale = Vector3.one * EaseOutBack(p);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private void LockAllButtons()
    {
        foreach (var btn in ruleButtons)
            if (btn != null) btn.SetInteractable(false);
    }

    private List<int> PickWrongRules(int correctIndex, int count)
    {
        List<int> pool = new List<int>();
        for (int i = 0; i < rules.Length; i++)
            if (i != correctIndex) pool.Add(i);
        Shuffle(pool);

        List<int> result = new List<int>();
        for (int i = 0; i < count && i < pool.Count; i++)
            result.Add(pool[i]);
        return result;
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