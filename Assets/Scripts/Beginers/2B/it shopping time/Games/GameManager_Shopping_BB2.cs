using UnityEngine;

/// <summary>
/// GameManager_Shopping_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → ShopMatch_BB2      (tap the shop that sells the item)
/// Screen 2 → SortShopping_BB2   (drag cards into THINGS/SHOPS/CLOTHES basket)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   ShoppingUnit                    ← this script (contentGameObject)
///     ├─ Screen1_ShopMatch          ← ShopMatch_BB2 lives here
///     └─ Screen2_SortShopping       ← SortShopping_BB2 lives here
///
/// INSPECTOR WIRING:
///   shopGame   → drag the GameObject that has ShopMatch_BB2
///   sortGame   → drag the GameObject that has SortShopping_BB2
/// </summary>
public class GameManager_Shopping_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Which Shop?")]
    [SerializeField] private ShopMatch_BB2 shopGame;

    [Header("Screen 2 — Fill the Trolley")]
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

        if (shopGame != null)
        {
            shopGame.gameObject.SetActive(true);
            shopGame.OnFinished = OnScreen1Complete;
            shopGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_Shopping_BB2] shopGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via ShopMatch_BB2.OnFinished after all 8 rounds.
    public void OnScreen1Complete()
    {
        if (shopGame != null)
            shopGame.gameObject.SetActive(false);

        if (sortGame != null)
        {
            sortGame.gameObject.SetActive(true);
            sortGame.OnFinished = OnScreen2Complete;
            sortGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_Shopping_BB2] sortGame not assigned!");
        }
    }

    /// Called via SortShopping_BB2.OnFinished after all 9 cards are
    /// sorted. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_Shopping_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
