using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, IUnitCompletable
{
    public static GameManager Instance { get; private set; }

    [Header("Panel References")]
    public MatchingGameController matchingPanel;
    public SimonSaysController    simonSaysPanel;

    [Header("State")]
    public int currentRound = 0;
    [HideInInspector] public SharedUnitButton           ownerUnitButton;
    [HideInInspector] public SharedUnitPanelController  ownerUnitPanel;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        ownerUnitPanel  = sharedPanel;
        ownerUnitButton = sharedButton;

        // ✅ Every time the unit opens, do a full reset then start from Screen 1
        StartUnit();
    }

    /// Full reset — clears stale UI, then starts matching (Screen 1)
    public void StartUnit()
    {
        // ── Reset Simon (Screen 2) back to a clean hidden state ──
        if (simonSaysPanel != null)
        {
            simonSaysPanel.ResetPanel();                        // hides complete panel, resets counters
            simonSaysPanel.gameObject.SetActive(false);         // hide Screen 2
        }

        // ── Show and restart Screen 1 ──
        if (matchingPanel != null)
        {
            matchingPanel.gameObject.SetActive(true);
            matchingPanel.RestartGame();                        // resets counters + loads round 0
        }
    }

    public void LoadPanel(int panelIndex)
    {
        Debug.Log($"[GameManager] Loading panel {panelIndex}");
    }

    public void OnUnitComplete()
    {
        ownerUnitPanel.UnitFinished(ownerUnitButton);
    }
}