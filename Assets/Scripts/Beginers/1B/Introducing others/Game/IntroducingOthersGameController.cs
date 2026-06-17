using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  IntroducingOthersGameController
///  Root controller for the "Introducing Others" game unit.
///  Implements IUnitCompletable so SharedUnitPanelController can start
///  and finish this unit automatically.
/// ════════════════════════════════════════════════════════════════════
///
///  SCENE HIERARCHY (Add this script to the root Game GameObject):
///  ─────────────────────────────────────────────────────────────────
///  Game                  [this script]  [IUnitCompletable]
///    ├─ Screen1_FamilyTree  [FamilyTreeGameScreen]
///    └─ Screen2_Gameplay2   (Placeholder for Screen 2)
/// </summary>
public class IntroducingOthersGameController : MonoBehaviour, IUnitCompletable
{
    [Header("Screens")]
    [Tooltip("Screen 1: Family Tree drag & drop game object")]
    public GameObject screen1GO;
    [Tooltip("Screen 2: Gameplay 2 game object")]
    public GameObject screen2GO;

    [Header("Screen Scripts")]
    [Tooltip("Reference to the FamilyTreeGameScreen script")]
    public FamilyTreeGameScreen screen1Script;

    // IUnitCompletable back-references
    private SharedUnitPanelController _panel;
    private SharedUnitButton          _button;

    // ── IUnitCompletable ─────────────────────────────────────────────
    public void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button)
    {
        _panel  = panel;
        _button = button;
        StartGame();
    }

    // ── Called each time the unit opens (fresh start every time) ────
    public void StartGame()
    {
        StopAllCoroutines();
        ShowScreen1();
    }

    // ── Screen transitions ───────────────────────────────────────────
    public void ShowScreen1()
    {
        if (screen2GO != null) screen2GO.SetActive(false);
        if (screen1GO != null) screen1GO.SetActive(true);
        
        if (screen1Script != null)
        {
            screen1Script.Initialise(this);
        }
        else
        {
            Debug.LogError("IntroducingOthersGameController: screen1Script is not wired in the Inspector!");
        }
    }

    public void ShowScreen2()
    {
        if (screen1GO != null) screen1GO.SetActive(false);
        if (screen2GO != null) screen2GO.SetActive(true);
        
        // When we implement screen 2, we will initialize it here.
        Debug.Log("IntroducingOthersGameController: Transitioning to Screen 2.");
    }

    // ── Called when the entire game (all screens) is finished ────────
    public void OnGameComplete()
    {
        // Signal back to the unit panel — this triggers badge + reward check
        _panel?.UnitFinished(_button);
    }
}
