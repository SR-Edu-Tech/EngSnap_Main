using UnityEngine;

/// <summary>
/// GameManager_HaveHas_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → TapTheWord_IsAre_BB2         (REUSED — same 2-button gap-fill
///            pattern as the IS/ARE unit; populate its `sentences` array
///            with the HAVE/HAS sentence data and relabel/recolor its 2
///            buttons: HAVE = orange, HAS = green)
/// Screen 2 → SortHabitsOrQualities_BB2    (REUSED — same 2-category drag-
///            sort pattern as the Good Habits unit; populate its `cards`
///            array with the 8 subject chits and relabel/recolor its 2
///            baskets: HAVE = orange, HAS = green)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   HaveHasUnit                    ← this script (contentGameObject)
///     ├─ Screen1_TapHaveHas        ← TapTheWord_IsAre_BB2 lives here
///     └─ Screen2_MatchHaveHas      ← SortHabitsOrQualities_BB2 lives here
///
/// INSPECTOR WIRING:
///   tapGame     → drag the GameObject that has TapTheWord_IsAre_BB2
///   matchGame   → drag the GameObject that has SortHabitsOrQualities_BB2
///
/// DATA MAPPING NOTES (no code changes needed, just how you fill in the
/// Inspector on the reused scripts):
///   - TapTheWord_IsAre_BB2's enum is literally "Is"/"Are" — just treat
///     Is-slot = HAVE and Are-slot = HAS (or vice versa) when filling in
///     `sentences`, and relabel the 2 button texts to say "HAVE"/"HAS" in
///     the scene. The enum names never show to the player.
///   - SortHabitsOrQualities_BB2's enum is "Habit"/"Quality" — treat
///     Habit-basket = HAVE and Quality-basket = HAS when filling in
///     `cards`, and relabel the 2 basket labels to say "HAVE"/"HAS".
/// </summary>
public class GameManager_HaveHas_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Tap Have/Has (reuses TapTheWord_IsAre_BB2)")]
    [SerializeField] private TapTheWord_IsAre_BB2 tapGame;

    [Header("Screen 2 — Match Have/Has (reuses SortHabitsOrQualities_BB2)")]
    [SerializeField] private SortHabitsOrQualities_BB2 matchGame;

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
            Debug.LogError("[GameManager_HaveHas_BB2] tapGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via TapTheWord_IsAre_BB2.OnFinished after all 8 sentences.
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
            Debug.LogError("[GameManager_HaveHas_BB2] matchGame not assigned!");
        }
    }

    /// Called via SortHabitsOrQualities_BB2.OnFinished after all 8 chits
    /// are matched. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_HaveHas_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
