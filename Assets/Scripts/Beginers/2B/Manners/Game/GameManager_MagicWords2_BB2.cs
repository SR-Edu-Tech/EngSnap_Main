using UnityEngine;

/// <summary>
/// GameManager_MagicWords2_BB2
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — the entry point the unit panel calls.
///
/// Screen 1 → TapMagicWord_BB2  (instance #1 — "Which Magic Word?")
/// Screen 2 → TapMagicWord_BB2  (instance #2 — "Answer The Situation")
///
/// Both screens use the SAME script class (TapMagicWord_BB2) because the
/// underlying mechanic is identical — a situation appears, 3 tinted
/// buttons refill each round, tap the right one. Only the round data and
/// on-screen flavour (flower bloom vs mascot answering, "ball pass" intro
/// line) differ. Place two separate GameObjects in the scene, each with
/// its own TapMagicWord_BB2 component and its own `rounds` array — do NOT
/// try to share one component instance between both screens.
///
/// No Screen 3 — pressing Next on Screen 2 finishes the unit directly.
///
/// HIERARCHY EXAMPLE:
///   MagicWordsUnit                      ← this script (contentGameObject)
///     ├─ Screen1_WhichMagicWord         ← TapMagicWord_BB2 instance #1
///     └─ Screen2_AnswerTheSituation     ← TapMagicWord_BB2 instance #2
///
/// INSPECTOR WIRING:
///   screen1Game   → drag the GameObject with the Screen 1 TapMagicWord_BB2
///   screen2Game   → drag the GameObject with the Screen 2 TapMagicWord_BB2
/// </summary>
public class GameManager_MagicWords2_BB2 : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Which Magic Word?")]
    [SerializeField] private TapMagicWord_BB2 screen1Game;

    [Header("Screen 2 — Answer The Situation")]
    [SerializeField] private TapMagicWord_BB2 screen2Game;

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
        if (screen2Game != null)
            screen2Game.gameObject.SetActive(false);

        if (screen1Game != null)
        {
            screen1Game.gameObject.SetActive(true);
            screen1Game.OnFinished = OnScreen1Complete;
            screen1Game.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_MagicWords2_BB2] screen1Game not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// Called via screen1Game.OnFinished after all 8 rounds.
    public void OnScreen1Complete()
    {
        if (screen1Game != null)
            screen1Game.gameObject.SetActive(false);

        if (screen2Game != null)
        {
            screen2Game.gameObject.SetActive(true);
            screen2Game.OnFinished = OnScreen2Complete;
            screen2Game.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_MagicWords2_BB2] screen2Game not assigned!");
        }
    }

    /// Called via screen2Game.OnFinished after all 8 rounds. Tells the
    /// unit panel this unit is finished.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_MagicWords2_BB2] _panel or _button is null — was OnUnitStart called?");
    }
}
