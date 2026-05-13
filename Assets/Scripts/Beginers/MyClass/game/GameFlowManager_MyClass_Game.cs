using System.Collections;
using UnityEngine;

/// <summary>
/// GAME FLOW MANAGER — Single-scene panel navigation
///
/// ── WHAT THIS DOES ───────────────────────────────────────────────
///  Replaces SceneManager.LoadScene() calls with panel show/hide so
///  Screen 1 → Screen 2 → Unit Panel all live in ONE scene.
///
/// ── SCENE SETUP ──────────────────────────────────────────────────
///  1. Create three top-level panels under your Canvas:
///       Screen1Panel   — contains Screen1_ClassroomTapGame_MyClass_Game + its children
///       Screen2Panel   — contains Screen2_PackYourBagGame_MyClass_Game + its children
///       UnitPanel      — your existing Unit/reward panel
///
///  2. Add an EMPTY GameObject called "GameFlowManager" to the scene.
///     Attach THIS script to it.
///
///  3. In the Inspector, assign:
///       screen1Panel   → Screen1Panel
///       screen2Panel   → Screen2Panel
///       unitPanel      → UnitPanel
///       screen1Manager → Screen1Panel's Screen1_ClassroomTapGame_MyClass_Game component
///       screen2Manager → Screen2Panel's Screen2_PackYourBagGame_MyClass_Game component
///
///  4. On Screen1's Next button  onClick → GameFlowManager.GoToScreen2()
///     On Screen2's Done button  onClick → GameFlowManager.GoToUnitPanel()
///     On UnitPanel's Play Again button  onClick → GameFlowManager.PlayAgain()
///
/// ── HOW RESET WORKS ──────────────────────────────────────────────
///  PlayAgain() deactivates Screen2Panel and UnitPanel, then calls
///  ResetAndStart() on Screen1 — which is the same as a fresh Start().
///  Screen2 is similarly reset when GoToScreen2() is called, so a
///  second play-through is always clean.
///
/// ── BUG NOTE ─────────────────────────────────────────────────────
///  MonoBehaviourHost_MyClass_Game uses DontDestroyOnLoad, so it
///  survives any accidental scene reloads and already has a duplicate-
///  instance guard — no changes needed there.
/// </summary>
public class GameFlowManager_MyClass_Game : MonoBehaviour
{
    [Header("── PANELS ──")]
    [Tooltip("Root GameObject for Screen 1 content")]
    public GameObject screen1Panel;

    [Tooltip("Root GameObject for Screen 2 content")]
    public GameObject screen2Panel;

    [Tooltip("Root GameObject for the Unit / reward panel shown at the very end")]
    public GameObject unitPanel;

    [Header("── GAME MANAGERS ──")]
    [Tooltip("Screen1_ClassroomTapGame_MyClass_Game component on Screen1Panel")]
    public Screen1_ClassroomTapGame_MyClass_Game screen1Manager;

    [Tooltip("Screen2_PackYourBagGame_MyClass_Game component on Screen2Panel")]
    public Screen2_PackYourBagGame_MyClass_Game screen2Manager;

    // ─────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Start()
    {
        // Always begin at Screen 1 when the scene first loads.
        ShowScreen1Fresh();
    }

    // ─────────────────────────────────────────────────────────────
    //  NAVIGATION — wire these to button onClick events
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from ANY external button that opens this game panel
    /// (e.g. a "My Class" button on a hub screen).  It always starts
    /// from Screen 1 with a clean slate, no matter what state the game
    /// was left in last time.
    /// </summary>
    public void OnGamePanelOpened()
    {
        ShowScreen1Fresh();
    }

    /// <summary>
    /// Called by Screen 1's Next button after all rounds are complete.
    /// </summary>
    public void GoToScreen2()
    {
        // ── ORDER MATTERS ──────────────────────────────────────────
        // SetActive(true) BEFORE ResetAndStart() so that StartCoroutine
        // runs on an active GameObject. Calling ResetAndStart() on an
        // inactive panel would silently swallow every coroutine.
        screen1Panel.SetActive(false);
        unitPanel.SetActive(false);
        screen2Panel.SetActive(true);       // activate FIRST
        screen2Manager.ResetAndStart();     // then reset+start
    }

    /// <summary>
    /// Called by Screen 2's Done button after Round 3 is complete.
    /// </summary>
    public void GoToUnitPanel()
    {
        screen2Panel.SetActive(false);
        unitPanel.SetActive(true);
    }

    /// <summary>
    /// Called by the Unit Panel's "Play Again" button.
    /// Returns to Screen 1 with a completely clean state.
    /// </summary>
    public void PlayAgain()
    {
        ShowScreen1Fresh();
    }

    // ─────────────────────────────────────────────────────────────
    //  INTERNAL HELPER
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Hides Screen 2 and the Unit Panel, activates Screen 1,
    /// then resets it — always in that order so coroutines fire correctly.
    /// </summary>
    void ShowScreen1Fresh()
    {
        screen2Panel.SetActive(false);
        unitPanel.SetActive(false);
        screen1Panel.SetActive(true);       // activate FIRST
        screen1Manager.ResetAndStart();     // then reset+start
    }
}