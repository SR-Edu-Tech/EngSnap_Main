using UnityEngine;

public class SharedUnitPanelController : MonoBehaviour
{
    [Header("Wiring")]
    public TopicSelectorRegistry registry;
    public GameObject            unitButtonsRoot;
    public RewardPanel_BB2       rewardPanel;

    private TopicData_BB2    _activeTopic;
    private SharedUnitButton _activeButton;
    private GameObject       _activeContentGO;
    private SharedUnitButton[] _buttons;

    public GameObject gamebg;
    public GameObject soundManager;

    void Awake()
    {
        _buttons = unitButtonsRoot.GetComponentsInChildren<SharedUnitButton>(true);

        if (registry || unitButtonsRoot.activeSelf == true)
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

        var completable = contentGO.GetComponentInChildren<IUnitCompletable>(true);
        if (completable != null)
            completable.OnUnitStart(this, unitButton);
        else
            Debug.LogWarning($"SharedUnitPanelController: No IUnitCompletable found on '{contentGO.name}' or its children.");

        var speaking = contentGO.GetComponentInChildren<SpeakingGameController>(true);
        //speaking?.ResetGame();
    }

    // ── Called by content screen (via IUnitCompletable) ───────────────────
    public void UnitFinished(SharedUnitButton unitButton)
    {
        TopicData_BB2 topicSnapshot  = _activeTopic;
        UnitType_BB1  justCompleted  = unitButton.unitType;   // capture before hiding

        HideActiveContent();
        unitButton.MarkCompleted(topicSnapshot);
        ShowButtons();

        // Pass justCompleted so the check doesn't depend on PlayerPrefs read-back
        // of the key that was written milliseconds ago in this same frame.
        if (AreAllUnitsComplete(topicSnapshot, justCompleted) && !WasRewardAlreadyShown(topicSnapshot))
        {
            MarkRewardShown(topicSnapshot);
            ShowRewardPanel(topicSnapshot);
        }
    }

    // ── All-units-complete check ──────────────────────────────────────────
    // justCompletedType is treated as complete in-memory, bypassing any
    // PlayerPrefs read-back delay for the key written in this same frame.
    private bool AreAllUnitsComplete(TopicData_BB2 topicData, UnitType_BB1 justCompletedType)
    {
        if (topicData == null || topicData.unitEntries == null) return false;
        foreach (var entry in topicData.unitEntries)
        {
            if (entry.unitType == justCompletedType) continue;   // just completed — treat as done
            string key = topicData.GetSaveKey(entry.unitType);
            if (PlayerPrefs.GetInt(key, 0) != 1) return false;
        }
        return true;
    }

    // ── Reward-shown flag (persisted so it survives app restart) ─────────
    private string RewardShownKey(TopicData_BB2 topicData) => $"{topicData.topicID}_rewardShown";

    private bool WasRewardAlreadyShown(TopicData_BB2 topicData)
    {
        if (topicData == null) return true;   // safe default — don't show
        return PlayerPrefs.GetInt(RewardShownKey(topicData), 0) == 1;
    }

    private void MarkRewardShown(TopicData_BB2 topicData)
    {
        if (topicData == null) return;
        PlayerPrefs.SetInt(RewardShownKey(topicData), 1);
        PlayerPrefs.Save();
    }

    private void ShowRewardPanel(TopicData_BB2 topicData)
    {
        if (rewardPanel == null) return;
        rewardPanel.Show(topicData, this);
    }

    /// <summary>Next button on reward panel — back to topic selection.</summary>
    public void OnRewardNext()
    {
        if (rewardPanel != null) rewardPanel.Hide();
        registry.BackToTopicSelection();
    }

    /// <summary>Replay button on reward panel — stay on unit panel.</summary>
    public void OnRewardReplay()
    {
        if (rewardPanel != null) rewardPanel.Hide();
    }

    // ── Back button on unit panel ─────────────────────────────────────────
    public void BackToTopics()
    {
        registry.BackToTopicSelection();
    }

    private void ShowButtons()
    {
        // Activate FIRST so all child GameObjects are live when Initialise()
        // calls completionBadge.SetActive() — avoids badges not showing on
        // first open because SetActive on inactive children is deferred by Unity.
        unitButtonsRoot.SetActive(true);
        foreach (var btn in _buttons)
            btn.Initialise(this, _activeTopic);
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