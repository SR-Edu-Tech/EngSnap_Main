using System.Collections;
using UnityEngine;

/// <summary>
/// GAME FLOW MANAGER — Single-scene panel navigation
///
/// ── SCENE SETUP ──────────────────────────────────────────────────
///  1. Attach this script to the root "Game" GameObject (the same one
///     that SharedUnitPanelController looks at for IUnitCompletable).
///     This script NOW implements IUnitCompletable so the error
///     "No IUnitCompletable found on 'Game' or its children" is resolved.
///
///  2. Child panels under the "Game" GO:
///       Screen 1   — contains Screen1_ClassroomTapGame_MyClass_Game
///       Screen 2   — contains Screen2_PackYourBagGame_MyClass_Game
///     Set BOTH inactive in the Inspector. This script controls visibility.
///
///  3. Wire in Inspector:
///       screen1Panel   → "Screen 1" GameObject
///       screen2Panel   → "Screen 2" GameObject
///       unitPanel      → leave NULL (owned by SharedUnitPanelController)
///       screen1Manager → Screen1_ClassroomTapGame_MyClass_Game component
///       screen2Manager → Screen2_PackYourBagGame_MyClass_Game component
///
///  4. Button wiring:
///       Screen1 Next button  onClick → GameFlowManager.GoToScreen2()
///       Screen2 Done button  onClick → GameFlowManager.GoToUnitPanel()
///       (No separate Play button needed — OnUnitStart fires OpenGame automatically)
///
/// ── HOW THE GAME STARTS ──────────────────────────────────────────
///   SharedUnitPanelController calls OnUnitStart() when the player opens
///   MY CLASS. OnUnitStart() stores the panel/button references and calls
///   OpenGame() immediately → Screen 1 activates and gameplay begins.
///
/// ── FLOW ─────────────────────────────────────────────────────────
///   OnUnitStart()  → SharedUnitPanelController → OpenGame()
///   OpenGame()     → hides S2, shows S1, calls ResetAndStart()
///   GoToScreen2()  → hides S1, shows S2, calls ResetAndStart()
///   GoToUnitPanel()→ hides S1+S2, calls sharedPanel.UnitFinished()
///   PlayAgain()    → alias → OpenGame()
///
/// ── KEY ORDERING RULE ────────────────────────────────────────────
///   Always SetActive(true) BEFORE ResetAndStart(). StartCoroutine
///   silently swallows if the GameObject is inactive.
///   Screen1/Screen2 OnEnable are intentionally EMPTY.
/// </summary>
public class GameFlowManager_MyClass_Game : MonoBehaviour, IUnitCompletable
{
    [Header("── PANELS ──")]
    public GameObject screen1Panel;
    public GameObject screen2Panel;
    [Tooltip("Leave NULL — the unit/reward panel is owned by SharedUnitPanelController")]
    public GameObject unitPanel;

    [Header("── GAME MANAGERS ──")]
    public Screen1_ClassroomTapGame_MyClass_Game screen1Manager;
    public Screen2_PackYourBagGame_MyClass_Game  screen2Manager;

    // ─────────────────────────────────────────────────────────────
    //  IUnitCompletable  — wired automatically by SharedUnitPanelController
    // ─────────────────────────────────────────────────────────────

    private SharedUnitPanelController _sharedPanel;
    private SharedUnitButton          _sharedButton;

    /// <summary>
    /// Called by SharedUnitPanelController when the player opens this unit.
    /// This is the TRUE entry point for gameplay.
    ///
    /// ROOT CAUSE FIX: GameFlowManager was not implementing IUnitCompletable,
    /// so SharedUnitPanelController could not find it on the "Game" GameObject
    /// or its children. This produced the console error:
    ///   "No IUnitCompletable found on 'Game' or its children"
    /// and meant the game was never started — Screen 1 appeared frozen/disabled.
    /// Adding ": IUnitCompletable" and this method resolves it completely.
    /// </summary>
    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        _sharedPanel  = sharedPanel;
        _sharedButton = sharedButton;
        OpenGame();   // immediately start gameplay from Screen 1
    }

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        // Both panels must start hidden. OnUnitStart() → OpenGame() is the
        // sole entry point that shows Screen 1 and starts the coroutine.
        //
        // BUG FIX: was SetActive(true) on screen1Panel, which showed a static
        // frozen panel because ResetAndStart() was never called from Start().
        screen1Panel.SetActive(true);
        screen2Panel.SetActive(false);
        if (unitPanel != null) unitPanel.SetActive(false);
        OpenGame();
    }

    // ─────────────────────────────────────────────────────────────
    //  NAVIGATION
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts (or restarts) gameplay cleanly from Screen 1.
    /// Called by OnUnitStart() on first open, and PlayAgain() on replay.
    /// ORDER: SetActive(true) BEFORE ResetAndStart().
    /// </summary>
    public void OpenGame()
    {
        screen2Panel.SetActive(false);
        if (unitPanel != null) unitPanel.SetActive(false);

        screen1Panel.SetActive(true);    // activate FIRST
        screen1Manager.ResetAndStart();  // then start — panel is active
    }

    /// <summary>Called by Screen 1's Next button when all rounds are done.</summary>
    public void GoToScreen2()
    {
        screen1Panel.SetActive(false);
        if (unitPanel != null) unitPanel.SetActive(false);

        screen2Panel.SetActive(true);    // activate FIRST
        screen2Manager.ResetAndStart();  // then start — panel is active
    }

    /// <summary>
    /// Called by Screen 2's Done button.
    /// Hides game panels and returns control to SharedUnitPanelController
    /// so the reward / unit-complete flow can run.
    /// </summary>
    public void GoToUnitPanel()
    {
        screen1Panel.SetActive(false);
        screen2Panel.SetActive(false);

        if (unitPanel != null)
            unitPanel.SetActive(true);           // direct unit panel reference
        else if (_sharedPanel != null)
            _sharedPanel.UnitFinished(_sharedButton);  // shared system callback
    }

    /// <summary>Alias for Inspector wiring that calls PlayAgain().</summary>
    public void PlayAgain() => OpenGame();
}