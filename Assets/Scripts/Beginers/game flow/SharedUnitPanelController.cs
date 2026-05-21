using UnityEngine;

/// <summary>
/// Replaces UnitPanelController_BB1.
/// ONE shared Unit Panel that works for every topic.
///
/// WIRING IN INSPECTOR:
///   registry        → drag the GameObject with TopicSelectorRegistry
///   unitButtonsRoot → parent GameObject holding all SharedUnitButton children
///
/// FLOW:
///   1. TopicSelectorRegistry calls Open(topicData)
///      → panel activates, all buttons initialised for this topic
///   2. Player clicks a unit button → StartUnit() called
///      → buttons hidden, correct content GO shown
///   3. Content screen calls UnitFinished()
///      → content hidden, badge marked, buttons shown again
///   4. Player clicks Back → BackToTopics()
///      → returns to topic selection
/// </summary>
public class SharedUnitPanelController : MonoBehaviour
{
    [Header("Wiring")]
    public TopicSelectorRegistry registry;
    public GameObject            unitButtonsRoot;

    private TopicData_BB2  _activeTopic;
    private SharedUnitButton _activeButton;
    private GameObject       _activeContentGO;
    private SharedUnitButton[] _buttons;

    public GameObject gamebg;
    public GameObject soundManager;  

    void Awake()
    {
        _buttons = unitButtonsRoot.GetComponentsInChildren<SharedUnitButton>(true);

        if (registry||unitButtonsRoot.activeSelf==true)
        {
            gamebg.SetActive(false);
            soundManager.SetActive(false);
        }
        else
        {
            gamebg.SetActive(true);
            soundManager.SetActive(true);
        }
           
    }

    // ── Called by TopicSelectorRegistry ──────────────────────────────────
    public void Open(TopicData_BB2 topicData)
    {
        _activeTopic = topicData;
        gameObject.SetActive(true);
        ShowButtons();
    }

    public void Close()
    {
        HideActiveContent();
        gameObject.SetActive(false);
        _activeTopic = null;
    }

    // ── Called by SharedUnitButton on click ───────────────────────────────
    public void StartUnit(SharedUnitButton unitButton)
    {
        if (_activeTopic == null)
        {
            Debug.LogWarning("SharedUnitPanelController: No active topic set.");
            return;
        }

        GameObject contentGO = _activeTopic.GetContentObject(unitButton.unitType);
        if (contentGO == null) return;

        _activeButton    = unitButton;
        _activeContentGO = contentGO;

        unitButtonsRoot.SetActive(false);
        contentGO.SetActive(true);

        // Notify content screen via interface.
        // Use GetComponentInChildren so the script can live on a child GO
        // (e.g. Greetings_BB1 is a child of the Reading content root).
        var completable = contentGO.GetComponentInChildren<IUnitCompletable>(true);
        if (completable != null)
            completable.OnUnitStart(this, unitButton);
        else
            Debug.LogWarning($"SharedUnitPanelController: No IUnitCompletable found on '{contentGO.name}' or its children.");

        // Keep existing SpeakingGameController reset behaviour
        var speaking = contentGO.GetComponentInChildren<SpeakingGameController>(true);
        //speaking?.ResetGame();
    }

    // ── Called by content screen (via IUnitCompletable) ───────────────────
    public void UnitFinished(SharedUnitButton unitButton)
    {
         TopicData_BB2 topicSnapshot = _activeTopic; // capture before HideActiveContent clears it
    HideActiveContent();
    unitButton.MarkCompleted(topicSnapshot);     // use snapshot
    ShowButtons();   
    }

    // ── Back button on unit panel ─────────────────────────────────────────
    public void BackToTopics()
    {
        registry.BackToTopicSelection();
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void ShowButtons()
    {
        foreach (var btn in _buttons)
            btn.Initialise(this, _activeTopic);
        unitButtonsRoot.SetActive(true);
    }

    private void HideActiveContent()
    {
        if (_activeContentGO != null)
        {
            _activeContentGO.SetActive(false);
            _activeContentGO = null;
        }
        _activeButton = null;
    }

  
     void Update()
{
    if (unitButtonsRoot.activeSelf)
    {
        gamebg.SetActive(false);
        soundManager.SetActive(false);
    }
    else
    {
        gamebg.SetActive(true);
        soundManager.SetActive(true);
    }
}
    
}