using System.Collections;
using UnityEngine;

/// <summary>
/// GameManager_Reading_read
/// ─────────────────────────────────────────────────────────────────────────
/// Implements IUnitCompletable.
/// Screen 1 → FeelingsGalleryController_read
/// Screen 2 → ReadLearnActController_read
///
/// HIERARCHY:
///   ReadingUnit                          ← this script (contentGameObject in TopicData_BB2)
///     ├─ Screen1_Gallery                 ← FeelingsGalleryController_read
///     └─ Screen2_ReadLearnAct            ← ReadLearnActController_read
/// </summary>
public class GameManager_Reading_read : MonoBehaviour, IUnitCompletable
{
    [Header("Screens")]
    [SerializeField] private FeelingsGalleryController_read  galleryController;
    [SerializeField] private ReadLearnActController_read     readLearnActController;

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
        if (readLearnActController != null)
        {
            readLearnActController.ResetScreen();
            readLearnActController.gameObject.SetActive(false);
        }

        // Start Screen 1
        if (galleryController != null)
        {
            galleryController.gameObject.SetActive(true);
            galleryController.StartScreen(this);
        }
        else Debug.LogError("[GameManager_Reading] galleryController not assigned!");
    }

    // ── Transitions ──────────────────────────────────────────────────────

    /// Called by Screen 1 when NEXT is tapped.
    public void OnScreen1Complete()
    {
        if (readLearnActController != null)
            StartCoroutine(ActivateScreen2());
        else Debug.LogError("[GameManager_Reading] readLearnActController not assigned!");
    }

    private IEnumerator ActivateScreen2()
    {
        if (galleryController != null)
            galleryController.gameObject.SetActive(false);

        readLearnActController.gameObject.SetActive(true);
        yield return null;                              // one frame — let Unity wake the GO
        readLearnActController.StartScreen(this);
    }

    /// Called by Screen 2 when NEXT is tapped after all 10 acted.
    public void OnScreen2Complete()
    {
        if (_panel != null && _button != null)
            _panel.UnitFinished(_button);
        else Debug.LogError("[GameManager_Reading] _panel or _button null.");
    }
}
