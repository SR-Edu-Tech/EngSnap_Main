using UnityEngine;

/// <summary>
/// GameManager_AmIsAre_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → PronounTap_BB2       (REUSED — same fixed-3-button tap
///            pattern as the HE/SHE/IT unit; populate its `rounds` array
///            with the AM/IS/ARE sentence data and relabel its 3 buttons/
///            colors to AM (pink) / IS (blue) / ARE (green))
/// Screen 2 → ArrangeWords_BB2     (drag chits into order to build each sentence)
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   AmIsAreUnit                  ← this script (contentGameObject)
///     ├─ Screen1_TapPronoun      ← PronounTap_BB2 lives here
///     └─ Screen2_ArrangeWords    ← ArrangeWords_BB2 lives here
///
/// INSPECTOR WIRING:
///   tapGame       → drag the GameObject that has PronounTap_BB2
///   arrangeGame   → drag the GameObject that has ArrangeWords_BB2
/// </summary>
public class GameManager_AmIsAre_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Tap AM/IS/ARE (reuses PronounTap_BB2)")]
    [SerializeField] private PronounTap_BB2 tapGame;

    [Header("Screen 2 — Arrange the Words")]
    [SerializeField] private ArrangeWords_BB2 arrangeGame;

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
        if (arrangeGame != null)
            arrangeGame.gameObject.SetActive(false);

        if (tapGame != null)
        {
            tapGame.gameObject.SetActive(true);
            tapGame.OnFinished = OnScreen1Complete;
            tapGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_AmIsAre_BB2] tapGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via PronounTap_BB2.OnFinished after all 8 rounds.
    public void OnScreen1Complete()
    {
        if (tapGame != null)
            tapGame.gameObject.SetActive(false);

        if (arrangeGame != null)
        {
            arrangeGame.gameObject.SetActive(true);
            arrangeGame.OnFinished = OnScreen2Complete;
            arrangeGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_AmIsAre_BB2] arrangeGame not assigned!");
        }
    }

    /// Called via ArrangeWords_BB2.OnFinished after all 5 sentences are
    /// arranged. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_AmIsAre_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
