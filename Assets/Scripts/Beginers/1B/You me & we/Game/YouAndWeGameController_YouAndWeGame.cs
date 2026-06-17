using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  YouAndWeGameController_YouAndWeGame
///  Root controller for the "You & We" pronouns + affirmations game.
///  Implements IUnitCompletable so SharedUnitPanelController can start
///  and finish this unit automatically.
/// ════════════════════════════════════════════════════════════════════
///
///  SCENE HIERARCHY (all under one root GO, add this script there):
///  ─────────────────────────────────────────────────────────────────
///  YouAndWeGame          [this script]  [IUnitCompletable]
///    ├─ Screen1_PronounDrag   [PronounDragScreen_YouAndWeGame]
///    └─ Screen2_BubblePop    [BubblePopScreen_YouAndWeGame]
///
///  Wire in Inspector:
///    screen1          → drag Screen1_PronounDrag GO
///    screen2          → drag Screen2_BubblePop   GO
///    screen1Script    → drag PronounDragScreen_YouAndWeGame component
///    screen2Script    → drag BubblePopScreen_YouAndWeGame component
/// </summary>
public class YouAndWeGameController_YouAndWeGame : MonoBehaviour, IUnitCompletable
{
    [Header("Screens")]
    public GameObject              screen1GO;
    public GameObject              screen2GO;
    public PronounDragScreen_YouAndWeGame  screen1Script;
    public BubblePopScreen_YouAndWeGame   screen2Script;

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
        screen2GO.SetActive(false);
        screen1GO.SetActive(true);
        screen1Script.Initialise(this);
    }

    public void ShowScreen2()
    {
        screen1GO.SetActive(false);
        screen2GO.SetActive(true);
        screen2Script.Initialise(this);
    }

    // ── Called by Screen2 when all bubbles are popped ───────────────
    public void OnGameComplete()
    {
        // Signal back to the unit panel — this triggers badge + reward check
        _panel?.UnitFinished(_button);
    }
}
