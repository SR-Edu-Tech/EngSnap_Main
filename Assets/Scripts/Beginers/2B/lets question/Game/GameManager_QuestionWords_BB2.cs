using UnityEngine;

/// <summary>
/// GameManager_QuestionWords_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → TapQuestionWord_BB2   (tap WHO/WHAT/WHY/HOW/WHERE/WHEN to fill the gap)
/// Screen 2 → MatchQA_BB2           (drag each answer onto its matching question)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   QuestionWordsUnit               ← this script (contentGameObject)
///     ├─ Screen1_TapQuestionWord    ← TapQuestionWord_BB2 lives here
///     └─ Screen2_MatchQA            ← MatchQA_BB2 lives here
///
/// INSPECTOR WIRING:
///   tapGame     → drag the GameObject that has TapQuestionWord_BB2
///   matchGame   → drag the GameObject that has MatchQA_BB2
/// </summary>
public class GameManager_QuestionWords_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Tap the Question Word")]
    [SerializeField] private TapQuestionWord_BB2 tapGame;

    [Header("Screen 2 — Q&A Match")]
    [SerializeField] private MatchQA_BB2 matchGame;

    // ── Stored from OnUnitStart ──────────────────────────────────────────
    private SharedUnitPanelController _panel;
    private SharedUnitButton          _button;

    // ════════════════════════════════════════════════════════════════════
    //  IUnitCompletable
    //  Called every time the unit button is tapped — always starts fresh
    //  from Screen 1.
    // ════════════════════════════════════════════════════════════════════

    public void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button)
    {
        _panel  = panel;
        _button = button;

        ResetAndStart();
    }

    private void ResetAndStart()
    {
        if (matchGame != null)
            matchGame.gameObject.SetActive(false);

        if (tapGame != null)
        {
            tapGame.gameObject.SetActive(true);
            tapGame.OnFinished = OnScreen1Complete;
            tapGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_QuestionWords_BB2] tapGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via TapQuestionWord_BB2.OnFinished after all 8 rounds.
    public void OnScreen1Complete()
    {
        if (tapGame != null)
            tapGame.gameObject.SetActive(false);

        if (matchGame != null)
        {
            matchGame.gameObject.SetActive(true);
            matchGame.OnFinished = OnScreen2Complete;
            matchGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_QuestionWords_BB2] matchGame not assigned!");
        }
    }

    /// Called via MatchQA_BB2.OnFinished after all 6 pairs are matched.
    /// Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_QuestionWords_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
