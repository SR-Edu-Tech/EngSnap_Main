using UnityEngine;

/// <summary>
/// GameManager_BB2_Sorting
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → MatchTheFollowingGame_BB2   (REUSED as-is, no changes)
/// Screen 2 → SortingGame_BB2             (I Will / I Will Not bins)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   BB2Unit                        ← this script (contentGameObject)
///     ├─ Screen1_Matching          ← MatchTheFollowingGame_BB2 lives here
///     └─ Screen2_Sorting           ← SortingGame_BB2 lives here
///
/// INSPECTOR WIRING:
///   matchingGame   → drag the GameObject that has MatchTheFollowingGame_BB2
///   sortingGame    → drag the GameObject that has SortingGame_BB2
/// </summary>
public class GameManager_BB2_Sorting : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Match the Following (reused)")]
    [SerializeField] private MatchTheFollowingGame_BB2 matchingGame;

    [Header("Screen 2 — I Will / I Will Not Sorting")]
    [SerializeField] private SortingGame_BB2 sortingGame;

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
        if (sortingGame != null)
            sortingGame.gameObject.SetActive(false);

        if (matchingGame != null)
        {
            matchingGame.gameObject.SetActive(true);
            matchingGame.OnFinished = OnScreen1Complete;
            matchingGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_BB2_Sorting] matchingGame not assigned!");
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
        if (sortingGame != null)
        {
            sortingGame.gameObject.SetActive(true);
            sortingGame.OnFinished = OnScreen2Complete;
            sortingGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_BB2_Sorting] sortingGame not assigned!");
        }
    }

    /// Called by SortingGame_BB2 when Next is pressed after all 8 chits
    /// are correctly sorted. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_BB2_Sorting] _panel or _button is null — was OnUnitStart called?");
    }
}
