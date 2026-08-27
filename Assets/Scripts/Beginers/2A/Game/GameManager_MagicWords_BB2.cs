using UnityEngine;

/// <summary>
/// GameManager_MagicWords_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → MatchTheFollowingGame_BB2   (existing, reused as-is)
/// Screen 2 → BalloonPop_MagicWords_BB2   (pop the right magic-word balloon)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   MagicWordsUnit                  ← this script (contentGameObject)
///     ├─ Screen1_Matching           ← MatchTheFollowingGame_BB2 lives here
///     └─ Screen2_BalloonPop         ← BalloonPop_MagicWords_BB2 lives here
///
/// INSPECTOR WIRING:
///   matchingGame   → drag the GameObject that has MatchTheFollowingGame_BB2
///   balloonGame    → drag the GameObject that has BalloonPop_MagicWords_BB2
/// </summary>
public class GameManager_MagicWords_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Match the Following")]
    [SerializeField] private MatchTheFollowingGame_BB2 matchingGame;

    [Header("Screen 2 — Pop the Balloon")]
    [SerializeField] private BalloonPop_MagicWords_BB2 balloonGame;

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
        if (balloonGame != null)
            balloonGame.gameObject.SetActive(false);

        if (matchingGame != null)
        {
            matchingGame.gameObject.SetActive(true);
            matchingGame.OnFinished = OnScreen1Complete;
            matchingGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_MagicWords_BB2] matchingGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via MatchTheFollowingGame_BB2.OnFinished when all rounds are
    /// complete. MatchTheFollowingGame_BB2 already hides itself before
    /// firing this, so we just activate Screen 2.
    public void OnScreen1Complete()
    {
        if (balloonGame != null)
        {
            balloonGame.gameObject.SetActive(true);
            balloonGame.OnFinished = OnScreen2Complete;
            balloonGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_MagicWords_BB2] balloonGame not assigned!");
        }
    }

    /// Called via BalloonPop_MagicWords_BB2.OnFinished after all 8
    /// situations are answered. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_MagicWords_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
