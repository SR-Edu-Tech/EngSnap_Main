using UnityEngine;

/// <summary>
/// GameManager_SchoolRules_game
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — this is the entry point the unit panel calls.
///
/// Screen 1 → MatchingGameController      (existing reused script)
/// Screen 2 → SchoolRulesController_game  (new script)
/// Screen 3 → CelebrationScreen_game      (new script)
///
/// HIERARCHY EXAMPLE:
///   SchoolRules                          ← this script  (contentGameObject in TopicData_BB2)
///     ├─ Screen1_Matching                ← MatchingGameController lives here
///     ├─ Screen2_SchoolRules             ← SchoolRulesController_game lives here
///     └─ Screen3_Celebration             ← CelebrationScreen_game lives here
///
/// INSPECTOR WIRING:
///   matchingController      → drag the GameObject that has MatchingGameController
///   schoolRulesController   → drag the GameObject that has SchoolRulesController_game
///   celebrationScreen       → drag the GameObject that has CelebrationScreen_game
/// </summary>
public class GameManager_SchoolRules_game : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Match the Following")]
    [SerializeField] private MatchingGameController matchingController;

    [Header("Screen 2 — School Rules")]
    [SerializeField] private SchoolRulesController_game schoolRulesController;

    [Header("Screen 3 — Celebration")]
    [SerializeField] private CelebrationScreen_game celebrationScreen;

    // ── Stored from OnUnitStart ──────────────────────────────────────────
    private SharedUnitPanelController _panel;
    private SharedUnitButton          _button;

    // ════════════════════════════════════════════════════════════════════
    //  IUnitCompletable
    //  Called every time the unit button is tapped — must fully reset
    // ════════════════════════════════════════════════════════════════════

    public void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button)
    {
        _panel  = panel;
        _button = button;

        ResetAndStart();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset — always start from Screen 1
    // ════════════════════════════════════════════════════════════════════

    private void ResetAndStart()
    {
        // ── Reset & hide Screen 3 ────────────────────────────────────────
        if (celebrationScreen != null)
        {
            celebrationScreen.ResetPanel();
            celebrationScreen.gameObject.SetActive(false);
        }

        // ── Reset & hide Screen 2 ────────────────────────────────────────
        if (schoolRulesController != null)
            schoolRulesController.gameObject.SetActive(false);

        // ── Show & restart Screen 1 ──────────────────────────────────────
        if (matchingController != null)
        {
            matchingController.gameObject.SetActive(true);

            // Wire the callback so MatchingGameController calls us when done.
            // This is the same OnFinished pattern used in the LetsAct GameManager.
            matchingController.OnFinished = OnScreen1Complete;

            matchingController.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_SchoolRules] matchingController not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called via MatchingGameController.OnFinished when all matching rounds
    /// are complete. MatchingGameController already hides itself before firing
    /// this callback, so we just activate Screen 2.
    /// </summary>
    public void OnScreen1Complete()
    {
        if (schoolRulesController != null)
        {
            schoolRulesController.gameObject.SetActive(true);
            schoolRulesController.RestartGame(this);
        }
        else
        {
            Debug.LogError("[GameManager_SchoolRules] schoolRulesController not assigned!");
        }
    }

    /// <summary>
    /// Called by SchoolRulesController_game when all 6 rule rounds are done.
    /// Transitions to Screen 3 (Celebration).
    /// </summary>
    public void OnScreen2Complete()
    {
        if (schoolRulesController != null)
            schoolRulesController.gameObject.SetActive(false);

        if (celebrationScreen != null)
        {
            celebrationScreen.gameObject.SetActive(true);
            celebrationScreen.Show(this);
        }
        else
        {
            Debug.LogError("[GameManager_SchoolRules] celebrationScreen not assigned — completing unit directly.");
            OnScreen3Complete();
        }
    }

    /// <summary>
    /// Called by CelebrationScreen_game when the Next button is pressed.
    /// Tells the unit panel this unit is finished → badge + reward check.
    /// </summary>
    public void OnScreen3Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_SchoolRules] _panel or _button is null — was OnUnitStart called?");
    }
}