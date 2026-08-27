using UnityEngine;

/// <summary>
/// Master Manager for Unit 1 — Letter Sounds, Vowels & Sound Wall.
/// Controls section panel visibility and ensures the U1 Completion/Reward Panel
/// is disabled on start and ONLY enabled upon Unit 1 completion.
/// </summary>
public class Unit1Manager : MonoBehaviour
{
    [Header("Section Panels")]
    [SerializeField] private GameObject sectionSelectionPanel;
    [SerializeField] private GameObject sectionAPanel;
    [SerializeField] private GameObject sectionBPanel;
    [SerializeField] private GameObject sectionCPanel;
    [SerializeField] private GameObject sectionDPanel;

    [Header("Reward / Completion Panel")]
    [SerializeField] private U1_RewardController rewardPanel;

    private void Awake()
    {
        EnsureInit();
        CloseAllSections();
    }

    private void OnEnable()
    {
        EnsureInit();
        // Ensure completion panel is disabled when Unit 1 opens
        if (rewardPanel != null)
        {
            rewardPanel.gameObject.SetActive(false);
        }
    }

    private void EnsureInit()
    {
        Transform root = transform;

        if (sectionSelectionPanel == null)
        {
            Transform t = root.Find("Unit_1_Section_Selection_Panels");
            if (t != null) sectionSelectionPanel = t.gameObject;
        }

        if (sectionAPanel == null)
        {
            Transform t = root.Find("Unit_1_Sections/Section A Panel");
            if (t != null) sectionAPanel = t.gameObject;
        }

        if (sectionBPanel == null)
        {
            Transform t = root.Find("Unit_1_Sections/Section B Panel");
            if (t != null) sectionBPanel = t.gameObject;
        }

        if (sectionCPanel == null)
        {
            Transform t = root.Find("Unit_1_Sections/Section C Panel");
            if (t != null) sectionCPanel = t.gameObject;
        }

        if (sectionDPanel == null)
        {
            Transform t = root.Find("Unit_1_Sections/Section D Panel");
            if (t != null) sectionDPanel = t.gameObject;
        }

        if (rewardPanel == null)
        {
            rewardPanel = GetComponentInChildren<U1_RewardController>(true);
            if (rewardPanel == null) rewardPanel = FindFirstObjectByType<U1_RewardController>(FindObjectsInactive.Include);
        }
    }

    public void CloseAllSections()
    {
        if (sectionAPanel != null) sectionAPanel.SetActive(false);
        if (sectionBPanel != null) sectionBPanel.SetActive(false);
        if (sectionCPanel != null) sectionCPanel.SetActive(false);
        if (sectionDPanel != null) sectionDPanel.SetActive(false);

        // Completion panel must stay disabled until Unit 1 completion
        if (rewardPanel != null) rewardPanel.gameObject.SetActive(false);
    }

    public void ShowLevelSelection()
    {
        CloseAllSections();
        if (sectionSelectionPanel != null) sectionSelectionPanel.SetActive(true);
    }

    public void OpenSectionA()
    {
        CloseAllSections();
        if (sectionSelectionPanel != null) sectionSelectionPanel.SetActive(false);
        if (sectionAPanel != null) sectionAPanel.SetActive(true);
    }

    public void OpenSectionB()
    {
        CloseAllSections();
        if (sectionSelectionPanel != null) sectionSelectionPanel.SetActive(false);
        if (sectionBPanel != null) sectionBPanel.SetActive(true);
    }

    public void OpenSectionC()
    {
        CloseAllSections();
        if (sectionSelectionPanel != null) sectionSelectionPanel.SetActive(false);
        if (sectionCPanel != null) sectionCPanel.SetActive(true);
    }

    public void OpenSectionD()
    {
        CloseAllSections();
        if (sectionSelectionPanel != null) sectionSelectionPanel.SetActive(false);
        if (sectionDPanel != null) sectionDPanel.SetActive(true);
    }

    /// <summary>
    /// Call this upon Unit 1 completion to trigger the Reward Panel.
    /// </summary>
    public void CompleteUnit1()
    {
        CloseAllSections();
        if (sectionSelectionPanel != null) sectionSelectionPanel.SetActive(false);

        if (rewardPanel != null)
        {
            rewardPanel.gameObject.SetActive(true);
            rewardPanel.ShowReward();
        }
    }
}
