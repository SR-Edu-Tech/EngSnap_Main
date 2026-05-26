using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
//  WritingGame_MLDL_Coordinator
//
//  Attach this to the ROOT "Game" GameObject (parent of Screen1, Screen2,
//  Feedback, etc.).
//
//  The SharedUnitPanelController calls OnUnitStart on this component.
//  This coordinator:
//    1. Hides Screen 2, Feedback, Summary — resets to clean state.
//    2. Activates Screen 1 and calls its OnUnitStart.
//
//  This means no matter how many times the student plays, opening the game
//  always starts fresh from Screen 1.
// ─────────────────────────────────────────────────────────────────────────────
public class WritingGame_MLDL_Coordinator : MonoBehaviour, IUnitCompletable
{
    [Header("─── Screen 1 (Panel A) ─────────────────────────────")]
    public GameObject screen1;          // drag Screen 1 root here

    [Header("─── Screen 2 (Panel B) ─────────────────────────────")]
    public GameObject screen2;          // drag Screen 2 root here

    [Header("─── Summary Panel ──────────────────────────────────")]
    public GameObject summaryPanel;     // drag Summary root here

    [Header("─── Any other panels to hide on reset ───────────────")]
    public GameObject feedbackPanel;    // drag Feedback here if separate

    // ─────────────────────────────────────────────────────────────────────
    //  Called by SharedUnitPanelController when the student opens this unit
    // ─────────────────────────────────────────────────────────────────────
    public void OnUnitStart(SharedUnitPanelController p, SharedUnitButton b)
    {
        // 1. Force everything to a known-clean state
        ResetAll();

        // 2. Activate Screen 1 — this fires its OnEnable → ResetAll → Setup
        if (screen1 != null)
        {
            screen1.SetActive(true);

            var panelA = screen1.GetComponent<WritingScreen_PanelA_MLDL_Game>();
            if (panelA != null)
            {
                panelA.panel      = p;
                panelA.unitButton = b;
                // OnEnable already ran from SetActive above but panel was null then,
                // so kick Setup manually now that refs are assigned.
                panelA.StartFresh();
            }
        }
    }

    void ResetAll()
    {
        if (screen2      != null) screen2.SetActive(false);
        if (summaryPanel != null) summaryPanel.SetActive(false);
        if (feedbackPanel!= null) feedbackPanel.SetActive(false);
        // Screen 1 will be set active by OnUnitStart right after this
        if (screen1      != null) screen1.SetActive(false);
    }
}
