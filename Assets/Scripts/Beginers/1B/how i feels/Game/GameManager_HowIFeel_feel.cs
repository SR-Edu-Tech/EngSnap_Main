using System.Collections;
using UnityEngine;

/// <summary>
/// GameManager_HowIFeel_feel
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable — entry point the unit panel calls.
///
/// Screen 1 → MatchingGameController     (existing reused script)
/// Screen 2 → HowIFeelController_feel    (new script)
///
/// HIERARCHY EXAMPLE:
///   HowIFeel                             ← this script  (contentGameObject in TopicData_BB2)
///     ├─ Screen1_Matching                ← MatchingGameController lives here
///     └─ Screen2_HowIFeel               ← HowIFeelController_feel lives here
///
/// INSPECTOR WIRING:
///   matchingController   → drag the GO that has MatchingGameController
///   howIFeelController   → drag the GO that has HowIFeelController_feel
/// </summary>
public class GameManager_HowIFeel_feel : MonoBehaviour, IUnitCompletable
{
    [Header("Screen 1 — Match the Following")]
    [SerializeField] private MatchingGameController matchingController;

    [Header("Screen 2 — How I Feel")]
    [SerializeField] private HowIFeelController_feel howIFeelController;

    // ── Stored from OnUnitStart ──────────────────────────────────────────
    private SharedUnitPanelController _panel;
    private SharedUnitButton          _button;

    // ════════════════════════════════════════════════════════════════════
    //  IUnitCompletable — called every time the unit button is tapped
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
        // ── Hide & reset Screen 2 ────────────────────────────────────────
        if (howIFeelController != null)
        {
            howIFeelController.ResetGame();
            howIFeelController.gameObject.SetActive(false);
        }

        // ── Show & restart Screen 1 ──────────────────────────────────────
        if (matchingController != null)
        {
            matchingController.gameObject.SetActive(true);
            matchingController.OnFinished = OnScreen1Complete;
            matchingController.RestartGame();
        }
        else
        {
            Debug.LogError("[GameManager_HowIFeel] matchingController not assigned!");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Screen transitions
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Fired by MatchingGameController.OnFinished when all matching rounds done.
    /// OnFinished fires INSIDE MatchingGameController's FadeOut coroutine, so at
    /// that moment Screen 2 is still inactive and can't start coroutines directly.
    /// Fix: we run the activation on THIS GameManager (which is always active),
    /// wait one frame for Unity to fully initialise the newly active GO, then start.
    /// </summary>
    public void OnScreen1Complete()
    {
        if (howIFeelController != null)
            StartCoroutine(ActivateScreen2());
        else
            Debug.LogError("[GameManager_HowIFeel] howIFeelController not assigned!");
    }

    private IEnumerator ActivateScreen2()
    {
        // Step 1 — activate the GO so it exists in the scene
        howIFeelController.gameObject.SetActive(true);

        // Step 2 — wait one frame so Unity fully wakes up the newly active GameObject
        //          (Awake/OnEnable all run, coroutines can now be started on it)
        yield return null;

        // Step 3 — now safe to start coroutines on Screen 2
        howIFeelController.StartGame(this);
    }

    /// <summary>
    /// Called by HowIFeelController_feel when all 8 rounds are done.
    /// Marks the unit complete in the unit panel.
    /// </summary>
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else
            Debug.LogError("[GameManager_HowIFeel] _panel or _button is null — was OnUnitStart called?");
    }
}