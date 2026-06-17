using System.Collections;
using UnityEngine;

/// <summary>
/// GameManager_EverydayIDo_do
/// ─────────────────────────────────────────────────────────────────────────
/// Screen 1 → MatchingGameController  (existing)
/// Screen 2 → ChitSortController_do   (new)
///
/// HIERARCHY:
///   EverydayIDo                      ← this script (contentGameObject in TopicData_BB2)
///     ├─ Screen1_Matching            ← MatchingGameController
///     └─ Screen2_ChitSort            ← ChitSortController_do
/// </summary>
public class GameManager_EverydayIDo_do : MonoBehaviour, IUnitCompletable
{
    [Header("Screens")]
    [SerializeField] private MatchingGameController matchingController;
    [SerializeField] private ChitSortController_do  chitSortController;

    private SharedUnitPanelController _panel;
    private SharedUnitButton          _button;

    // ── IUnitCompletable ─────────────────────────────────────────────────
    public void OnUnitStart(SharedUnitPanelController panel, SharedUnitButton button)
    {
        _panel  = panel;
        _button = button;
        ResetAndStart();
    }

    private void ResetAndStart()
    {
        // Reset + hide Screen 2
        if (chitSortController != null)
        {
            chitSortController.ResetGame();
            chitSortController.gameObject.SetActive(false);
        }

        // Start Screen 1
        if (matchingController != null)
        {
            matchingController.gameObject.SetActive(true);
            matchingController.OnFinished = OnScreen1Complete;
            matchingController.RestartGame();
        }
        else Debug.LogError("[GameManager_EverydayIDo] matchingController not assigned!");
    }

    // ── Transitions ──────────────────────────────────────────────────────

    public void OnScreen1Complete()
    {
        if (chitSortController != null)
            StartCoroutine(ActivateScreen2());
        else Debug.LogError("[GameManager_EverydayIDo] chitSortController not assigned!");
    }

    private IEnumerator ActivateScreen2()
    {
        if (matchingController != null)
            matchingController.gameObject.SetActive(false);

        chitSortController.gameObject.SetActive(true);
        yield return null;                          // one frame — let Unity wake the GO
        chitSortController.StartGame(this);
    }

    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else Debug.LogError("[GameManager_EverydayIDo] _panel or _button null.");
    }
}
