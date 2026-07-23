using UnityEngine;

/// <summary>
/// GameManager_BB2_WishCircle
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → MatchTheFollowingGame_BB2   (REUSED as-is, no changes)
/// Screen 2 → WishCircleGame_BB2          (new gift-box/wish-circle gameplay)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   BB3Unit                        ← this script (contentGameObject)
///     ├─ Screen1_Matching          ← MatchTheFollowingGame_BB2 lives here
///     └─ Screen2_WishCircle        ← WishCircleGame_BB2 lives here
///
/// INSPECTOR WIRING:
///   matchingGame     → drag the GameObject that has MatchTheFollowingGame_BB2
///   wishCircleGame   → drag the GameObject that has WishCircleGame_BB2
/// </summary>
public class GameManager_BB2_WishCircle : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Match the Following (reused)")]
    [SerializeField] private MatchTheFollowingGame_BB2 matchingGame;

    [Header("Screen 2 — Wish Circle")]
    [SerializeField] private WishCircleGame_BB2 wishCircleGame;

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
        if (wishCircleGame != null)
            wishCircleGame.gameObject.SetActive(false);

        if (matchingGame != null)
        {
            matchingGame.gameObject.SetActive(true);
            matchingGame.OnFinished = OnScreen1Complete;
            matchingGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_BB2_WishCircle] matchingGame not assigned!");
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
        if (wishCircleGame != null)
        {
            wishCircleGame.gameObject.SetActive(true);
            wishCircleGame.OnFinished = OnScreen2Complete;
            wishCircleGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_BB2_WishCircle] wishCircleGame not assigned!");
        }
    }

    /// Called by WishCircleGame_BB2 when Next is pressed after all 7
    /// friends have been wished. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_BB2_WishCircle] _panel or _button is null — was OnUnitStart called?");
    }
}
