using UnityEngine;

/// <summary>
/// GameManager_IsAre_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → TapTheWord_IsAre_BB2       (tap IS/ARE to fill the gap)
/// Screen 2 → BuildTheSentence_IsAre_BB2 (drag subject/verb/word into slots)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   IsAreUnit                       ← this script (contentGameObject)
///     ├─ Screen1_TapTheWord         ← TapTheWord_IsAre_BB2 lives here
///     └─ Screen2_BuildTheSentence   ← BuildTheSentence_IsAre_BB2 lives here
///
/// INSPECTOR WIRING:
///   tapGame     → drag the GameObject that has TapTheWord_IsAre_BB2
///   buildGame   → drag the GameObject that has BuildTheSentence_IsAre_BB2
/// </summary>
public class GameManager_IsAre_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Tap IS/ARE")]
    [SerializeField] private TapTheWord_IsAre_BB2 tapGame;

    [Header("Screen 2 — Build The Sentence")]
    [SerializeField] private BuildTheSentence_IsAre_BB2 buildGame;

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
        if (buildGame != null)
            buildGame.gameObject.SetActive(false);

        if (tapGame != null)
        {
            tapGame.gameObject.SetActive(true);
            tapGame.OnFinished = OnScreen1Complete;
            tapGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_IsAre_BB2] tapGame not assigned!");
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

        if (buildGame != null)
        {
            buildGame.gameObject.SetActive(true);
            buildGame.OnFinished = OnScreen2Complete;
            buildGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_IsAre_BB2] buildGame not assigned!");
        }
    }

    /// Called via BuildTheSentence_IsAre_BB2.OnFinished after all 6
    /// sentences are built. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_IsAre_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
