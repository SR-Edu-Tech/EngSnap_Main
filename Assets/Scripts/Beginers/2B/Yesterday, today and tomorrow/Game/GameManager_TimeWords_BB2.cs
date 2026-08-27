using UnityEngine;

/// <summary>
/// GameManager_TimeWords_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → TapTimeWord_BB2    (new — tap YESTERDAY/TODAY/TOMORROW to
///            fill the gap; needed a new script since this is a tap-based
///            gap-fill, structurally different from Screen 2's drag-sort)
/// Screen 2 → SortShopping_BB2   (REUSED — same 3-category drag-sort
///            pattern as the Shopping/Food-Actions units; its category
///            colours already line up: Things[orange]→Yesterday,
///            Shops[blue]→Today, Clothes[green]→Tomorrow. Just remap when
///            filling `cards` and relabel the 3 basket labels.)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   TimeWordsUnit                   ← this script (contentGameObject)
///     ├─ Screen1_TapTimeWord        ← TapTimeWord_BB2 lives here
///     └─ Screen2_SortDays           ← SortShopping_BB2 lives here
///
/// INSPECTOR WIRING:
///   tapGame    → drag the GameObject that has TapTimeWord_BB2
///   sortGame   → drag the GameObject that has SortShopping_BB2
/// </summary>
public class GameManager_TimeWords_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Tap Yesterday/Today/Tomorrow")]
    [SerializeField] private TapTimeWord_BB2 tapGame;

    [Header("Screen 2 — Sort The Days (reuses SortShopping_BB2)")]
    [SerializeField] private SortShopping_BB2 sortGame;

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

        if (tapGame != null)
        {
            tapGame.gameObject.SetActive(true);
            tapGame.OnFinished = OnScreen1Complete;
            tapGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_TimeWords_BB2] tapGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via TapTimeWord_BB2.OnFinished after all 8 sentences.
    public void OnScreen1Complete()
    {
        if (tapGame != null)
            tapGame.gameObject.SetActive(false);

        if (sortGame != null)
        {
            sortGame.gameObject.SetActive(true);
            sortGame.OnFinished = OnScreen2Complete;
            sortGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_TimeWords_BB2] sortGame not assigned!");
        }
    }

    /// Called via SortShopping_BB2.OnFinished after all 9 cards are
    /// sorted. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_TimeWords_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
