using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DialogueGameplayManager
/// Manages all 3 screens of the greeting dialogue gameplay.
/// Attach to the root GameObject of this gameplay scene/prefab.
/// On Enable  → resumes from last saved screen (PlayerPrefs key: "DialogueGameplay_Screen")
/// On Disable → saves current screen progress.
/// </summary>
public class DialogueGameplayManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────

    [Header("── Screen Root GameObjects ──")]
    public GameObject screen1Root;
    public GameObject screen2Root;
    public GameObject screen3Root;

    [Header("── Shared UI ──")]
    public GameObject wellDoneBanner;      // shown on completion of each screen
    public Button nextButton;              // enabled only after screen is complete
    public GameObject completedScreen;     // final screen after screen 3

    // ─────────────────────────────────────────────────────────────
    //  SCREEN CONTROLLERS (assigned via Inspector or GetComponent)
    // ─────────────────────────────────────────────────────────────
    [Header("── Screen Controllers ──")]
    public Screen1Controller screen1;
    public Screen2Controller screen2;
    public Screen3Controller screen3;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────
    private const string PREFS_KEY = "DialogueGameplay_Screen";
    private int  currentScreen       = 1;   // 1, 2, or 3
    private bool allScreensCompleted = false; // prevents OnDisable overwriting the reset

    // ─────────────────────────────────────────────────────────────
    //  UNITY CALLBACKS
    // ─────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        // Load saved progress; default to screen 1
        allScreensCompleted = false;
        currentScreen = PlayerPrefs.GetInt(PREFS_KEY, 1);
        // Clamp just in case
        currentScreen = Mathf.Clamp(currentScreen, 1, 3);

        wellDoneBanner.SetActive(false);
        nextButton.gameObject.SetActive(false);
        completedScreen.SetActive(false);

        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNextClicked);

        LoadScreen(currentScreen);
    }

    private void OnDisable()
    {
        // Don't overwrite the reset — if all screens were completed, prefs is already set to 1
        if (!allScreensCompleted)
        {
            PlayerPrefs.SetInt(PREFS_KEY, currentScreen);
            PlayerPrefs.Save();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  SCREEN LOADING
    // ─────────────────────────────────────────────────────────────
    private void LoadScreen(int screenIndex)
    {
        screen1Root.SetActive(false);
        screen2Root.SetActive(false);
        screen3Root.SetActive(false);
        wellDoneBanner.SetActive(false);
        nextButton.gameObject.SetActive(false);

        switch (screenIndex)
        {
            case 1:
                screen1Root.SetActive(true);
                screen1.StartScreen(OnScreenComplete);
                break;
            case 2:
                screen2Root.SetActive(true);
                screen2.StartScreen(OnScreenComplete);
                break;
            case 3:
                screen3Root.SetActive(true);
                screen3.StartScreen(OnScreenComplete);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  CALLBACKS
    // ─────────────────────────────────────────────────────────────

    /// <summary>Called by each screen controller when gameplay is complete.</summary>
    public void OnScreenComplete()
    {
        if (currentScreen < 3)
        {
            wellDoneBanner.SetActive(true);
             nextButton.gameObject.SetActive(true);
        }
        else
        {
            wellDoneBanner.SetActive(false);
            nextButton.gameObject.SetActive(true);
        }
        
    }

    private void OnNextClicked()
    {
        wellDoneBanner.SetActive(false);
        nextButton.gameObject.SetActive(false);

        if (currentScreen < 3)
        {
            currentScreen++;
            // Save progress so we resume correctly if interrupted
            PlayerPrefs.SetInt(PREFS_KEY, currentScreen);
            PlayerPrefs.Save();
            LoadScreen(currentScreen);
        }
        else
        {
            // All screens done → reset progress, show completed screen
            allScreensCompleted = true;   // tell OnDisable not to overwrite this
            PlayerPrefs.SetInt(PREFS_KEY, 1);
            PlayerPrefs.Save();
            screen3Root.SetActive(false);
           // wellDoneBanner.SetActive(false);
            completedScreen.SetActive(true);
        }
    }
}