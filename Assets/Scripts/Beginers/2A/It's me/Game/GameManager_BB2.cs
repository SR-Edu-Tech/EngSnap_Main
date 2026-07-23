using UnityEngine;

/// <summary>
/// GameManager_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → MatchTheFollowingGame_BB2
/// Screen 2 → IntroductionStrip_BB2
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   BB2Unit                        ← this script (contentGameObject)
///     ├─ Screen1_Matching          ← MatchTheFollowingGame_BB2 lives here
///     └─ Screen2_Introduction      ← IntroductionStrip_BB2 lives here
///
/// INSPECTOR WIRING:
///   matchingGame        → drag the GameObject that has MatchTheFollowingGame_BB2
///   introductionStrip    → drag the GameObject that has IntroductionStrip_BB2
/// </summary>
public class GameManager_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Match the Following")]
    [SerializeField] private MatchTheFollowingGame_BB2 matchingGame;

    [Header("Screen 2 — Introduction Strip")]
    [SerializeField] private IntroductionStrip_BB2 introductionStrip;

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
        if (introductionStrip != null)
            introductionStrip.gameObject.SetActive(false);

        if (matchingGame != null)
        {
            matchingGame.gameObject.SetActive(true);
            matchingGame.OnFinished = OnScreen1Complete;
            matchingGame.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_BB2] matchingGame not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via MatchTheFollowingGame_BB2.OnFinished when all rounds are
    /// complete. MatchTheFollowingGame_BB2 already hides itself before
    /// firing this, so we just activate Screen 2.
    public void OnScreen1Complete()
    {
        if (introductionStrip != null)
        {
            introductionStrip.gameObject.SetActive(true);
            introductionStrip.OnFinished = OnScreen2Complete;
            introductionStrip.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_BB2] introductionStrip not assigned!");
        }
    }

    /// Called by IntroductionStrip_BB2 when Next is pressed after the full
    /// introduction plays. Tells the unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
