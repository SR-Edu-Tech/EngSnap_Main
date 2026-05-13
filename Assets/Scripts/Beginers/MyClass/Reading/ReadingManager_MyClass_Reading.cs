using UnityEngine;

/// <summary>
/// Attach this to the ROOT "Reading" GameObject (the one that gets
/// activated/deactivated from the Unit Panel).
///
/// Every time Reading is opened (OnEnable), this script:
///   1. Immediately deactivates Screen 2 and Screen 3.
///   2. Activates Screen 1 — which triggers Screen1's own OnEnable to
///      reset and restart the full lesson from the beginning.
///
/// SCENE HIERARCHY expected:
///   Reading  ← this script lives here
///   ├─ Screen 1   (Screen1Controller_MyClass_Reading)
///   ├─ Screen 2   (Screen2Controller_MyClass_Reading)
///   └─ Screen 3   (Screen3Controller_MyClass_Reading)
///
/// No other scripts need to know about this manager.
/// </summary>
public class ReadingManager_MyClass_Reading : MonoBehaviour
{
    [Header("Screens — assign in Inspector")]
    [SerializeField] private GameObject screen1;
    [SerializeField] private GameObject screen2;
    [SerializeField] private GameObject screen3;

    private void OnEnable()
    {
        // Silence any leftover voice from a previous session
        if (AudioManager_MyClass_Reading.Instance != null)
            AudioManager_MyClass_Reading.Instance.StopVoice();

        // Make sure Screen 2 and 3 are off BEFORE Screen 1 activates,
        // so their OnDisable/coroutine cleanup runs in the right order.
        if (screen2 != null) screen2.SetActive(false);
        if (screen3 != null) screen3.SetActive(false);

        // Activate Screen 1 — its OnEnable handles the full reset.
        if (screen1 != null) screen1.SetActive(true);
    }
}
