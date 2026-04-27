using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class PanelsData_SeniorLev1A
{
    public string unitID;
    public int branchID;
    public GameObject panelToActivate;
    public GameObject unitParent;
    public Image completedTickImage;
}

public class GameFlowManager_SeniorLev1A : MonoBehaviour
{
    [SerializeField] private PanelsData_SeniorLev1A[] allPanelsList;

    [Header("Canvases")]
    [SerializeField] private Canvas unitSelectionCanvas;
    [SerializeField] private Canvas branchSelectionCanvas;

    [Header("Global Audio")]
    [SerializeField] private AudioSource globalAudioSource; // Main Camera AudioSource

    private string unitIdStored;
    private int branchIdStored;

    private GameObject currentActivePanel;
    private Dictionary<string, GameObject> panelLookup = new Dictionary<string, GameObject>();

    // -----------------------------
    void Awake()
    {
        foreach (var panel in allPanelsList)
        {
            string key = GetKey(panel.unitID, panel.branchID);

            if (!panelLookup.ContainsKey(key))
                panelLookup.Add(key, panel.panelToActivate);
        }
    }

    void OnEnable()
    {
        unitSelectionCanvas.gameObject.SetActive(true);
        branchSelectionCanvas.gameObject.SetActive(false);

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
    // UNIT SELECT
    // -----------------------------
    public void SetUnitID(string unitId)
    {
        StopAudio(); // stop any playing audio

        unitIdStored = unitId;
        branchIdStored = 0;

        unitSelectionCanvas.gameObject.SetActive(false);
        branchSelectionCanvas.gameObject.SetActive(true);

        LoadAllTicksForUnit(unitIdStored);
    }

    // -----------------------------
    // BRANCH SELECT
    // -----------------------------
    public void SetBranchID(int branchId)
    {
        StopAudio(); //  important

        branchIdStored = branchId;

        branchSelectionCanvas.gameObject.SetActive(false);

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

        for (int i = 0; i < branch.childCount; i++)
        {
            branch.GetChild(i).gameObject.SetActive(i == index);
        }
    }

    // -----------------------------
    public void NextGameplay()
    {
        if (currentActivePanel == null) return;

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

            CompleteBranch();

            PlayerPrefs.SetInt(GetProgressKey(unitIdStored, branchIdStored), 0);

            currentActivePanel.SetActive(false);
            DeactivateAllUnits();

            branchSelectionCanvas.gameObject.SetActive(true);
            branchIdStored = 0;
        }
    }

    // -----------------------------
    public void Back()
    {
        StopAudio(); //  key fix

        if (branchIdStored != 0)
        {
            if (currentActivePanel != null)
                currentActivePanel.SetActive(false);

            DeactivateAllUnits();

            branchSelectionCanvas.gameObject.SetActive(true);
            branchIdStored = 0;
        }
        else
        {
            branchSelectionCanvas.gameObject.SetActive(false);
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

    //------------------------------------
    //BACK TO SENIOR MODULE MAIN MENU
    //-------------------------------------
    public void BackButtonClickMainMenu()
    {
        Resources.UnloadUnusedAssets();
        SceneManager.LoadSceneAsync("mainScene");
    }
}