using UnityEngine;

/// <summary>
/// GameManager_MapDirections_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → DirectionSteps_MapDirections_BB2   (tap arrows to move the kid token)
/// Screen 2 → RouteBuilder_MapDirections_BB2      (build a route, tap GO! to send the friend)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   MapDirectionsUnit               ← this script (contentGameObject)
///     ├─ Screen1_DirectionSteps     ← DirectionSteps_MapDirections_BB2 lives here
///     └─ Screen2_RouteBuilder       ← RouteBuilder_MapDirections_BB2 lives here
///
/// INSPECTOR WIRING:
///   directionGame  → drag the GameObject that has DirectionSteps_MapDirections_BB2
///   routeGame      → drag the GameObject that has RouteBuilder_MapDirections_BB2
/// </summary>
public class GameManager_MapDirections_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Follow the Directions")]
    [SerializeField] private DirectionSteps_MapDirections_BB2 directionGame;

    [Header("Screen 2 — Guide the Friend")]
    [SerializeField] private RouteBuilder_MapDirections_BB2 routeGame;

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
        if (routeGame != null)
            routeGame.gameObject.SetActive(false);

        if (directionGame != null)
        {
            directionGame.gameObject.SetActive(true);
            directionGame.OnFinished = OnScreen1Complete;
            directionGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_MapDirections_BB2] directionGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via DirectionSteps_MapDirections_BB2.OnFinished after all 8 steps.
    public void OnScreen1Complete()
    {
        if (directionGame != null)
            directionGame.gameObject.SetActive(false);

        if (routeGame != null)
        {
            routeGame.gameObject.SetActive(true);
            routeGame.OnFinished = OnScreen2Complete;
            routeGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_MapDirections_BB2] routeGame not assigned!");
        }
    }

    /// Called via RouteBuilder_MapDirections_BB2.OnFinished after all 4
    /// trips reach their destination. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_MapDirections_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
