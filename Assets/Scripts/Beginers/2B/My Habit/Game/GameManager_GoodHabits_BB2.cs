using UnityEngine;

/// <summary>
/// GameManager_GoodHabits_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → GoodHabitPop_BB2            (tap the good-habit bubble)
/// Screen 2 → SortHabitsOrQualities_BB2   (drag cards into HABIT/QUALITY basket)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   GoodHabitsUnit                  ← this script (contentGameObject)
///     ├─ Screen1_CatchTheHabit      ← GoodHabitPop_BB2 lives here
///     └─ Screen2_SortItOut          ← SortHabitsOrQualities_BB2 lives here
///
/// INSPECTOR WIRING:
///   habitPopGame   → drag the GameObject that has GoodHabitPop_BB2
///   sortGame       → drag the GameObject that has SortHabitsOrQualities_BB2
/// </summary>
public class GameManager_GoodHabits_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Catch the Good Habit")]
    [SerializeField] private GoodHabitPop_BB2 habitPopGame;

    [Header("Screen 2 — Sort It Out")]
    [SerializeField] private SortHabitsOrQualities_BB2 sortGame;

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
        if (sortGame != null)
            sortGame.gameObject.SetActive(false);

        if (habitPopGame != null)
        {
            habitPopGame.gameObject.SetActive(true);
            habitPopGame.OnFinished = OnScreen1Complete;
            habitPopGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_GoodHabits_BB2] habitPopGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via GoodHabitPop_BB2.OnFinished after all 8 rounds.
    public void OnScreen1Complete()
    {
        if (habitPopGame != null)
            habitPopGame.gameObject.SetActive(false);

        if (sortGame != null)
        {
            sortGame.gameObject.SetActive(true);
            sortGame.OnFinished = OnScreen2Complete;
            sortGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_GoodHabits_BB2] sortGame not assigned!");
        }
    }

    /// Called via SortHabitsOrQualities_BB2.OnFinished after all 8 cards
    /// are sorted. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_GoodHabits_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
