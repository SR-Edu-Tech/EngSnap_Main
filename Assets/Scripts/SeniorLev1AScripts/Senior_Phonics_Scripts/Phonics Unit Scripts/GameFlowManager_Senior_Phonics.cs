using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class PanelsData_Senior_Phonics
{
    public string unitID;
    public int branchID;
    public GameObject panelToActivate;
    public GameObject unitParent;
    public Image completedTickImage;
    public GameObject unitCompleteScreen;
}

[Serializable]
public class UnitBranchSelectionData_Phonics
{
    public string unitID;
    public Canvas branchSelectionCanvas;
    public RectTransform branchSelectionScroll;
}

public class GameFlowManager_Senior_Phonics : MonoBehaviour
{
    [SerializeField] private PanelsData_Senior_Phonics[] allPanelsList;

    [Header("Canvases")]
    [SerializeField] private Canvas unitSelectionCanvas;
    [SerializeField] private UnitBranchSelectionData_Phonics[] unitBranchSelectionsList;

    [Header("Legacy Fallbacks (Optional)")] // this is the optional You can delete also
    [SerializeField] private Canvas branchSelectionCanvas;
    [SerializeField] private RectTransform branchSelectionScroll;
    //

    [Header("Global Audio")]
    [SerializeField] private AudioSource globalAudioSource; // Main Camera AudioSource

    private string unitIdStored;
    private int branchIdStored;

    private GameObject currentActivePanel;
    private Dictionary<string, GameObject> panelLookup = new Dictionary<string, GameObject>();
    private Dictionary<string, float> scrollWidthLookup = new Dictionary<string, float>();

    // -----------------------------
    void Awake()
    {
        //PlayerPrefs.DeleteAll();
        Application.targetFrameRate = 120;
        foreach (var panel in allPanelsList)
        {
            string key = GetKey(panel.unitID, panel.branchID);

            if (!panelLookup.ContainsKey(key))
                panelLookup.Add(key, panel.panelToActivate);
        }

        // Cache original scroll widths
        if (unitBranchSelectionsList != null)
        {
            foreach (var data in unitBranchSelectionsList)
            {
                if (data != null && data.branchSelectionScroll != null)
                {
                    scrollWidthLookup[data.unitID] = data.branchSelectionScroll.sizeDelta.x;
                }
            }
        }
        if (branchSelectionScroll != null)
        {
            scrollWidthLookup["default"] = branchSelectionScroll.sizeDelta.x;
        }
    }

    private void ResetScrollPosition(string unitId)
    {
        RectTransform currentScroll = GetBranchSelectionScroll(unitId);
        if (currentScroll != null)
        {
            float width = 3250f; // fallback default
            if (scrollWidthLookup.TryGetValue(unitId, out float cachedWidth))
            {
                width = cachedWidth;
            }
            else if (scrollWidthLookup.TryGetValue("default", out float defaultWidth))
            {
                width = defaultWidth;
            }
            else
            {
                width = currentScroll.sizeDelta.x;
            }

            currentScroll.offsetMin = new Vector2(0f, 0f);
            currentScroll.offsetMax = new Vector2(width, 0f);

            ScrollRect scrollRect = currentScroll.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.horizontalNormalizedPosition = 0f;
            }
        }
    }

    void OnEnable()
    {
        unitSelectionCanvas.gameObject.SetActive(true);
        DeactivateAllBranchSelectionCanvases();

        DeactivateAllUnits();
    }

    // -----------------------------
    string GetKey(string unitId, int branchId)
    {
        return unitId + "_" + branchId;
    }

    string GetProgressKey(string unitId, int branchId)
    {
        return unitId + "_" + branchId + "_PROGRESS";
    }

    string GetCompleteKey(string unitId, int branchId)
    {
        return unitId + "_" + branchId + "_DONE";
    }

    // -----------------------------
    // HELPERS FOR MULTIPLE CANVASES
    // -----------------------------
    private UnitBranchSelectionData_Phonics GetUnitBranchSelectionData(string unitId)
    {
        if (unitBranchSelectionsList != null)
        {
            foreach (var data in unitBranchSelectionsList)
            {
                if (data != null && data.unitID == unitId)
                {
                    return data;
                }
            }
        }
        return null;
    }

    private Canvas GetBranchSelectionCanvas(string unitId)
    {
        var data = GetUnitBranchSelectionData(unitId);
        if (data != null && data.branchSelectionCanvas != null)
        {
            return data.branchSelectionCanvas;
        }
        return branchSelectionCanvas;
    }

    private RectTransform GetBranchSelectionScroll(string unitId)
    {
        var data = GetUnitBranchSelectionData(unitId);
        if (data != null && data.branchSelectionScroll != null)
        {
            return data.branchSelectionScroll;
        }
        return branchSelectionScroll;
    }

    private void DeactivateAllBranchSelectionCanvases()
    {
        if (unitBranchSelectionsList != null)
        {
            foreach (var data in unitBranchSelectionsList)
            {
                if (data != null && data.branchSelectionCanvas != null)
                {
                    data.branchSelectionCanvas.gameObject.SetActive(false);
                }
            }
        }

        if (branchSelectionCanvas != null)
        {
            branchSelectionCanvas.gameObject.SetActive(false);
        }
    }

    // -----------------------------
    // UNIT SELECT
    // -----------------------------
    public void SetUnitID(string unitId)
    {
        StopAudio(); // stop any playing audio

        unitIdStored = unitId;
        branchIdStored = 0;

        unitSelectionCanvas.gameObject.SetActive(false);
        
        DeactivateAllBranchSelectionCanvases();
        Canvas currentCanvas = GetBranchSelectionCanvas(unitIdStored);
        if (currentCanvas != null)
        {
            currentCanvas.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("Branch selection canvas NOT found for unit: " + unitIdStored);
        }

        ResetScrollPosition(unitIdStored);
        LoadAllTicksForUnit(unitIdStored);
    }

    // -----------------------------
    // BRANCH SELECT
    // -----------------------------
    public void SetBranchID(int branchId)
    {
        StopAudio(); //  important

        branchIdStored = branchId;

        Canvas currentCanvas = GetBranchSelectionCanvas(unitIdStored);
        if (currentCanvas != null)
        {
            currentCanvas.gameObject.SetActive(false);
        }

        ActivateUnitParent(unitIdStored);
        ActivatePanel();

        int savedIndex = PlayerPrefs.GetInt(GetProgressKey(unitIdStored, branchIdStored), 0);
        ActivateGameplayScreen(savedIndex);
    }

    // -----------------------------
    void ActivatePanel()
    {
        string key = GetKey(unitIdStored, branchIdStored);

        if (currentActivePanel != null)
            currentActivePanel.SetActive(false);

        if (panelLookup.TryGetValue(key, out GameObject panel))
        {
            panel.SetActive(true);
            currentActivePanel = panel;
        }
        else
        {
            Debug.LogError("Panel NOT found: " + key);
        }
    }

    // -----------------------------
    void ActivateGameplayScreen(int index)
    {
        if (currentActivePanel == null) return;

        Transform branch = currentActivePanel.transform;

        // Safety check to handle out-of-bounds saved indices (e.g., from cached PlayerPrefs after hierarchy changes)
        if (index < 0 || index >= branch.childCount)
        {
            index = 0;
            PlayerPrefs.SetInt(GetProgressKey(unitIdStored, branchIdStored), 0);
        }

        for (int i = 0; i < branch.childCount; i++)
        {
            branch.GetChild(i).gameObject.SetActive(i == index);
        }
    }

    // -----------------------------
    public void NextGameplay()
    {
        if (currentActivePanel == null || branchIdStored == 0) return;

        Transform branch = currentActivePanel.transform;

        int current = PlayerPrefs.GetInt(GetProgressKey(unitIdStored, branchIdStored), 0);
        int next = current + 1;

        if (next < branch.childCount)
        {
            PlayerPrefs.SetInt(GetProgressKey(unitIdStored, branchIdStored), next);
            ActivateGameplayScreen(next);
        }
        else
        {
            StopAudio(); //  stop when finishing branch

            bool wasUnitComplete = IsUnitComplete(unitIdStored);

            CompleteBranch();

            bool isUnitComplete = IsUnitComplete(unitIdStored);

            PlayerPrefs.SetInt(GetProgressKey(unitIdStored, branchIdStored), 0);

            currentActivePanel.SetActive(false);

            GameObject specificScreen = GetUnitCompleteScreen(unitIdStored);

            int maxBranchId = GetMaxBranchID(unitIdStored);
            bool isFinalBranch = (branchIdStored == maxBranchId);

            Debug.Log($"[GameFlowManager] Checking completion: branchIdStored={branchIdStored}, maxBranchId={maxBranchId}, isFinalBranch={isFinalBranch}, wasUnitComplete={wasUnitComplete}, isUnitComplete={isUnitComplete}, screen={specificScreen != null}");

            if ((!wasUnitComplete || isFinalBranch) && isUnitComplete && specificScreen != null)
            {
                Debug.Log("UNIT COMPLETE -> SHOW REWARD SCREEN");

                specificScreen.SetActive(true);

                Debug.Log("Reward screen activated: " + specificScreen.name);
            }
            else
            {
                DeactivateAllUnits();

                Debug.Log(
                "Reward NOT shown | wasUnitComplete = " +
                wasUnitComplete +
                " | isUnitComplete = " +
                isUnitComplete +
                " | screen = " +
                (specificScreen != null)
                );

                Canvas currentCanvas = GetBranchSelectionCanvas(unitIdStored);
                if (currentCanvas != null)
                {
                    currentCanvas.gameObject.SetActive(true);
                }
            }
            
            branchIdStored = 0;
        }
    }

    private int GetMaxBranchID(string unitId)
    {
        int maxBranchId = 0;
        if (allPanelsList != null)
        {
            foreach (var data in allPanelsList)
            {
                if (data != null && data.unitID == unitId)
                {
                    if (data.branchID > maxBranchId)
                    {
                        maxBranchId = data.branchID;
                    }
                }
            }
        }
        return maxBranchId;
    }

    // -----------------------------
    GameObject GetUnitCompleteScreen(string unitId)
    {
        foreach (var data in allPanelsList)
        {
            if (data.unitID == unitId && data.unitCompleteScreen != null)
            {
                return data.unitCompleteScreen;
            }
        }
        return null;
    }

    // -----------------------------
    public void ReturnToUnitSelection()
    {
        StopAudio();
        
        // Ensure any active completion screen is hidden
        foreach (var data in allPanelsList)
        {
            if (data.unitCompleteScreen != null)
            {
                data.unitCompleteScreen.SetActive(false);
            }
        }

        if (currentActivePanel != null)
            currentActivePanel.SetActive(false);

        DeactivateAllUnits();

        DeactivateAllBranchSelectionCanvases();
        unitSelectionCanvas.gameObject.SetActive(true);
        
        branchIdStored = 0;
    }

    // -----------------------------
    public void Back()
    {
        StopAudio(); //  key fix

        ResetScrollPosition(unitIdStored);

        if (branchIdStored != 0)
        {
            if (currentActivePanel != null)
                currentActivePanel.SetActive(false);

            DeactivateAllUnits();

            Canvas currentCanvas = GetBranchSelectionCanvas(unitIdStored);
            if (currentCanvas != null)
            {
                currentCanvas.gameObject.SetActive(true);
            }
            branchIdStored = 0;
        }
        else
        {
            DeactivateAllBranchSelectionCanvases();
            unitSelectionCanvas.gameObject.SetActive(true);
        }
    }

    // -----------------------------
    void ActivateUnitParent(string unitId)
    {
        DeactivateAllUnits();

        foreach (var data in allPanelsList)
        {
            if (data.unitID == unitId && data.unitParent != null)
            {
                data.unitParent.SetActive(true);
                return;
            }
        }

        Debug.LogError("Unit parent NOT found for: " + unitId);
    }

    void DeactivateAllUnits()
    {
        foreach (var data in allPanelsList)
        {
            if (data.unitParent != null)
                data.unitParent.SetActive(false);
        }
    }

    // -----------------------------
    public bool IsUnitComplete(string unitId)
    {

    Debug.Log("===== CHECKING UNIT: " + unitId + " =====");

    foreach (var data in allPanelsList)
    {
        if (data.unitID == unitId)
        {
            int done = PlayerPrefs.GetInt(GetCompleteKey(unitId, data.branchID), 0);

            Debug.Log(
                "Branch ID: " + data.branchID +
                " | Done = " + done
            );

            if (done == 0)
            {
                Debug.Log("INCOMPLETE BRANCH FOUND: " + data.branchID);
                return false;
            }
        }
    }

    Debug.Log("ALL BRANCHES COMPLETE FOR UNIT: " + unitId);
    return true;
    }

    // -----------------------------
    void CompleteBranch()
    {
        PlayerPrefs.SetInt(GetCompleteKey(unitIdStored, branchIdStored), 1);
        ActivateTick(unitIdStored, branchIdStored);
    }

    void ActivateTick(string unitId, int branchId)
    {
        foreach (var data in allPanelsList)
        {
            if (data.unitID == unitId && data.branchID == branchId)
            {
                if (data.completedTickImage != null)
                    data.completedTickImage.gameObject.SetActive(true);
            }
        }
    }

    void LoadAllTicksForUnit(string unitId)
    {
        foreach (var data in allPanelsList)
        {
            if (data.completedTickImage == null) continue;

            data.completedTickImage.gameObject.SetActive(false);

            if (data.unitID == unitId)
            {
                bool done = PlayerPrefs.GetInt(GetCompleteKey(unitId, data.branchID), 0) == 1;
                data.completedTickImage.gameObject.SetActive(done);
            }
        }
    }

    // -----------------------------
    // AUDIO CONTROL
    // -----------------------------
    void StopAudio()
    {
        if (globalAudioSource != null && globalAudioSource.isPlaying)
        {
            globalAudioSource.Stop();
        }
    }
}