using UnityEngine;

/// <summary>
/// GameManager_POT_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → TapTheWord_POT_BB2   (tap IN/ON/AT to fill the gap)
/// Screen 2 → SortTheChits_POT_BB2 (drag chits into the correct basket)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   PotUnit                     ← this script (contentGameObject)
///     ├─ Screen1_TapTheWord     ← TapTheWord_POT_BB2 lives here
///     └─ Screen2_SortTheChits   ← SortTheChits_POT_BB2 lives here
///
/// INSPECTOR WIRING:
///   tapGame   → drag the GameObject that has TapTheWord_POT_BB2
///   sortGame  → drag the GameObject that has SortTheChits_POT_BB2
/// </summary>
public class GameManager_POT_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Tap IN/ON/AT")]
    [SerializeField] private TapTheWord_POT_BB2 tapGame;

    [Header("Screen 2 — Sort The Chits")]
    [SerializeField] private SortTheChits_POT_BB2 sortGame;

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
            Debug.LogError("[GameManager_POT_BB2] tapGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via TapTheWord_POT_BB2.OnFinished after all 8 phrases.
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
            Debug.LogError("[GameManager_POT_BB2] sortGame not assigned!");
        }
    }

    /// Called via SortTheChits_POT_BB2.OnFinished after all 9 chits are
    /// sorted. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_POT_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
