using UnityEngine;

/// <summary>
/// GameManager_ActionListening_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → DoWhatISay_BB2      (tap the mascot doing the called action)
/// Screen 2 → GuessTheAction_BB2  (tap a chit, watch the mime, guess the word)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   ActionListeningUnit             ← this script (contentGameObject)
///     ├─ Screen1_DoWhatISay         ← DoWhatISay_BB2 lives here
///     └─ Screen2_GuessTheAction     ← GuessTheAction_BB2 lives here
///
/// INSPECTOR WIRING:
///   sayGame     → drag the GameObject that has DoWhatISay_BB2
///   guessGame   → drag the GameObject that has GuessTheAction_BB2
/// </summary>
public class GameManager_ActionListening_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Do What I Say")]
    [SerializeField] private DoWhatISay_BB2 sayGame;

    [Header("Screen 2 — Guess The Action")]
    [SerializeField] private GuessTheAction_BB2 guessGame;

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
        if (guessGame != null)
            guessGame.gameObject.SetActive(false);

        if (sayGame != null)
        {
            sayGame.gameObject.SetActive(true);
            sayGame.OnFinished = OnScreen1Complete;
            sayGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_ActionListening_BB2] sayGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via DoWhatISay_BB2.OnFinished after all 8 rounds.
    public void OnScreen1Complete()
    {
        if (sayGame != null)
            sayGame.gameObject.SetActive(false);

        if (guessGame != null)
        {
            guessGame.gameObject.SetActive(true);
            guessGame.OnFinished = OnScreen2Complete;
            guessGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_ActionListening_BB2] guessGame not assigned!");
        }
    }

    /// Called via GuessTheAction_BB2.OnFinished after all 8 chits are
    /// guessed. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_ActionListening_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
