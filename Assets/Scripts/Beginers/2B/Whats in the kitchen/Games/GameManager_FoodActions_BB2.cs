using UnityEngine;

/// <summary>
/// GameManager_FoodActions_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → FoodActionMatch_BB2   (new — tap the action that goes with
///            the food; needed a new script because buttons must be tinted
///            per-category EACH ROUND, which ShopMatch_BB2 doesn't support)
/// Screen 2 → SortShopping_BB2      (REUSED — same 3-category drag-sort
///            pattern as the Shopping unit; its category colours already
///            line up: Shops[blue]→Cutting, Things[orange]→Heating,
///            Clothes[green]→Mixing. Just remap when filling `cards` and
///            relabel the 3 basket labels to CUTTING/HEATING/MIXING.)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   FoodActionsUnit                 ← this script (contentGameObject)
///     ├─ Screen1_FoodActionMatch    ← FoodActionMatch_BB2 lives here
///     └─ Screen2_SortActions        ← SortShopping_BB2 lives here
///
/// INSPECTOR WIRING:
///   actionGame   → drag the GameObject that has FoodActionMatch_BB2
///   sortGame     → drag the GameObject that has SortShopping_BB2
/// </summary>
public class GameManager_FoodActions_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Do The Right Action")]
    [SerializeField] private FoodActionMatch_BB2 actionGame;

    [Header("Screen 2 — Sort The Actions (reuses SortShopping_BB2)")]
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

        if (actionGame != null)
        {
            actionGame.gameObject.SetActive(true);
            actionGame.OnFinished = OnScreen1Complete;
            actionGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_FoodActions_BB2] actionGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via FoodActionMatch_BB2.OnFinished after all 8 rounds.
    public void OnScreen1Complete()
    {
        if (actionGame != null)
            actionGame.gameObject.SetActive(false);

        if (sortGame != null)
        {
            sortGame.gameObject.SetActive(true);
            sortGame.OnFinished = OnScreen2Complete;
            sortGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_FoodActions_BB2] sortGame not assigned!");
        }
    }

    /// Called via SortShopping_BB2.OnFinished after all 9 cards are
    /// sorted. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_FoodActions_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
